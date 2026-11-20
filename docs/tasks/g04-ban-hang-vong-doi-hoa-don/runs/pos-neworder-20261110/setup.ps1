$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
. "$root/tools/database/DatabaseTestTools.ps1"
$path = Join-Path $PSScriptRoot 'manifest.json'
if (Test-Path $path) { throw 'Manifest exists; inspect retained fixture before provisioning another.' }
$run = (Get-Date -Format 'yyyyMMdd_HHmmss') + '_' + [guid]::NewGuid().ToString('N').Substring(0,8)
$db = "CoffeeManager_Test_G04_${run}_posnew"
$m = [ordered]@{ RunId=$run; Server='localhost'; Database=$db; Marker=$run; Created=$false; Cleaned=$false; State='Preparing'; SchemaVersion=16; Purpose='G04-POS-01 isolated functional test for New Order free-table switching' }
function Save-Manifest { $m | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $path -Encoding utf8 }
Save-Manifest
$c = Open-TestConnection master
try {
    if ((Read-TestScalar $c "SELECT COUNT(*) FROM sys.databases WHERE name=N'$db'") -ne 0) { throw 'Target already exists' }
    Run-TestSql $c "CREATE DATABASE [$db]"; $m.Created=$true; Save-Manifest
    $c.ChangeDatabase($db)
    Run-TestSql $c "CREATE TABLE dbo.G04TestMarker(RunId varchar(40) PRIMARY KEY); INSERT dbo.G04TestMarker VALUES('$run');"
    foreach ($file in @('DB_Schema.sql','Insert_Data.sql')) {
        $sql = [regex]::Replace([IO.File]::ReadAllText("$root/database/$file"),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$','')
        Run-TestSql $c $sql
    }
    foreach ($file in Get-ChildItem "$root/database/migrations" -Filter '*.sql' | Sort-Object Name) {
        $v = [int]$file.Name.Substring(0,3); if ($v -gt 16) { continue }
        if ($v -eq 2) {
            Run-TestSql $c "UPDATE t SET MatKhauHash=d.MatKhauHash,HoatDong=1 FROM dbo.TAIKHOAN t JOIN CoffeeManager_Development.dbo.TAIKHOAN d ON d.MaNV=t.MaNV WHERE d.TenDangNhap IN('admin','thungan','kho');"
        }
        & "$root/tools/database/Apply-Migration.ps1" -Database $db -Version $v -File $file.FullName | ConvertTo-Json -Compress | Add-Content "$PSScriptRoot/migrations.jsonl"
        if ($v -eq 2) {
            Run-TestSql $c "DECLARE @id int,@hash varchar(255),@stamp binary(8); SELECT @id=MaTK,@hash=MatKhauHash,@stamp=SecurityStamp FROM dbo.TAIKHOAN WHERE TenDangNhap='admin' AND MaVT='ADMIN' AND HoatDong=1; EXEC dbo.sp_SetupConfirmLegacy @MaTK=@id,@ExpectedHash=@hash,@ExpectedStamp=@stamp;"
        }
    }
    Run-TestSql $c "CREATE USER G04PosNew FOR LOGIN CoffeeManager_Development_Runtime; ALTER ROLE CafeManagerApp ADD MEMBER G04PosNew;"
    if ((Read-TestScalar $c 'SELECT MAX(Version) FROM dbo.SchemaMigrations') -ne 16) { throw 'Schema mismatch' }
    if ((Read-TestScalar $c "SELECT COUNT(*) FROM dbo.AppSetupState WHERE State='Completed' AND AdminAccountId IS NOT NULL") -ne 1) { throw 'Setup incomplete' }
    Run-TestSql $c "EXECUTE AS USER='G04PosNew'; EXEC dbo.sp_AppReadState; REVERT;"
    $m.State='Ready for functional harness'; Save-Manifest
    Write-Output "Ready: $db"
} catch { $m.State='Setup failed'; $m['Error']=$_.Exception.Message; Save-Manifest; throw }
finally { $c.Dispose() }
