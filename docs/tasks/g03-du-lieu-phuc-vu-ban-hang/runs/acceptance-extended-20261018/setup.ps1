$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$out=Join-Path $root 'docs/tasks/g03-du-lieu-phuc-vu-ban-hang/runs/acceptance-extended-20261018'
$run=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$db="CoffeeManager_Test_G03_${run}_demo"
$manifest=[ordered]@{RunId=$run;Database=$db;Server='localhost';Marker=$run;Created=$false;Cleaned=$false;Purpose='Approved SQL supplemental + native GUI';RetainReason='Active acceptance GUI/2-session cases; cleanup at end of this turn';Expiry='2026-10-19';Principal='G03Runtime WITHOUT LOGIN + G03Gui mapped existing development runtime login';ProcessId=$null}
function Save { $manifest|ConvertTo-Json -Depth 5|Set-Content "$out/manifest.json" -Encoding utf8 }
Save
$c=Open-TestConnection master
try {
 if((Read-TestScalar $c "SELECT COUNT(*) FROM sys.databases WHERE name=N'$db'") -ne 0){throw 'Target exists'}
 Run-TestSql $c "CREATE DATABASE [$db]";$manifest.Created=$true;Save;$c.ChangeDatabase($db)
 Run-TestSql $c "CREATE TABLE dbo.G03TestMarker(RunId varchar(40) PRIMARY KEY);INSERT dbo.G03TestMarker VALUES('$run');"
 $schema=[regex]::Replace([IO.File]::ReadAllText("$root/database/DB_Schema.sql"),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$','');Run-TestSql $c $schema
 $seed=[regex]::Replace([IO.File]::ReadAllText("$root/database/Insert_Data.sql"),'(?im)^USE QuanLyCafe_ERP_V2;\s*$','');Run-TestSql $c $seed
 $files=Get-ChildItem "$root/database/migrations" -Filter '*.sql'|Sort-Object Name
 foreach($file in $files){$v=[int]$file.Name.Substring(0,3);& "$root/tools/database/Apply-Migration.ps1" -Database $db -Version $v -File $file.FullName|ConvertTo-Json -Compress|Add-Content "$out/migrations.jsonl"}
 Run-TestSql $c "CREATE USER G03Runtime WITHOUT LOGIN;ALTER ROLE CafeManagerApp ADD MEMBER G03Runtime;CREATE USER G03Gui FOR LOGIN CoffeeManager_Development_Runtime;ALTER ROLE CafeManagerApp ADD MEMBER G03Gui;UPDATE t SET MatKhauHash=d.MatKhauHash,HoatDong=1 FROM dbo.TAIKHOAN t JOIN CoffeeManager_Development.dbo.TAIKHOAN d ON d.MaNV=t.MaNV WHERE d.TenDangNhap IN('admin','thungan','kho');"
 $manifest.State='Ready';Save
} catch{$manifest.State='SetupFailed';$manifest.Error=$_.Exception.Message;Save;throw}finally{$c.Dispose()}
Write-Output "Supplemental DB ready: $db; manifest=$out/manifest.json"
