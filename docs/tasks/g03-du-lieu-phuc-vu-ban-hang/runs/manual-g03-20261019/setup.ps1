$ErrorActionPreference='Stop'
$root=(Get-Location).Path
. "$root/tools/database/DatabaseTestTools.ps1"
$out=$PSScriptRoot
$run=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$db="CoffeeManager_Test_G03_${run}_demo"
$m=[ordered]@{RunId=$run;Database=$db;Server='localhost';Marker=$run;Created=$false;Cleaned=$false;Purpose='Approved G03 guided manual GUI acceptance';RetainReason='Active manual GUI session; cleanup.ps1 exact name and marker';Expiry='2026-10-21';Principal='G03Gui mapped existing runtime login; no new server login';ProcessId=$null;State='Preparing'}
function Save {$m | ConvertTo-Json -Depth 5 | Set-Content "$out/manifest.json"}
Save
$c=Open-TestConnection master
try {
 if((Read-TestScalar $c "SELECT COUNT(*) FROM sys.databases WHERE name=N'$db'") -ne 0){throw 'Target exists'}
 Run-TestSql $c "CREATE DATABASE [$db]";$m.Created=$true;Save;$c.ChangeDatabase($db)
 Run-TestSql $c "CREATE TABLE dbo.G03TestMarker(RunId varchar(40) PRIMARY KEY);INSERT dbo.G03TestMarker VALUES('$run');"
 foreach($f in @('DB_Schema.sql','Insert_Data.sql')){
  $sql=[regex]::Replace([IO.File]::ReadAllText("$root/database/$f"),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$','')
  Run-TestSql $c $sql
 }
 foreach($file in Get-ChildItem "$root/database/migrations" -Filter '*.sql' | Sort-Object Name){
  $v=[int]$file.Name.Substring(0,3)
  # Credential fixture được chuẩn bị trước002, không sửa AppSetupState để vượt guard.
  if($v -eq 2){Run-TestSql $c "UPDATE t SET MatKhauHash=d.MatKhauHash,HoatDong=1 FROM dbo.TAIKHOAN t JOIN CoffeeManager_Development.dbo.TAIKHOAN d ON d.MaNV=t.MaNV WHERE d.TenDangNhap IN('admin','thungan','kho');"}
  & "$root/tools/database/Apply-Migration.ps1" -Database $db -Version $v -File $file.FullName | ConvertTo-Json -Compress | Add-Content "$out/migrations.jsonl"
  if($v -eq 2){
   if((Read-TestScalar $c 'SELECT State FROM dbo.AppSetupState WHERE Id=1') -cne 'LegacyReview'){throw 'Expected LegacyReview'}
   Run-TestSql $c "DECLARE @id int,@hash varchar(255),@stamp binary(8); SELECT @id=MaTK,@hash=MatKhauHash,@stamp=SecurityStamp FROM dbo.TAIKHOAN WHERE TenDangNhap='admin' AND MaVT='ADMIN' AND HoatDong=1; EXEC dbo.sp_SetupConfirmLegacy @MaTK=@id,@ExpectedHash=@hash,@ExpectedStamp=@stamp;"
  }
 }
 Run-TestSql $c 'CREATE USER G03Gui FOR LOGIN CoffeeManager_Development_Runtime;ALTER ROLE CafeManagerApp ADD MEMBER G03Gui;'
 $json=Read-TestScalar $c "SELECT State,AdminAccountId,CompletedAt FROM dbo.AppSetupState WHERE Id=1 FOR JSON PATH"
 $json | Set-Content "$out/setup-state.json"
 if((Read-TestScalar $c "SELECT COUNT(*) FROM dbo.AppSetupState WHERE State='Completed' AND AdminAccountId IS NOT NULL") -ne 1){throw 'Setup incomplete'}
 Run-TestSql $c "EXECUTE AS USER='G03Gui'; EXEC dbo.sp_AppReadState; REVERT;"
 $m.State='Completed verified; runtime state read succeeded; ready for manual login';Save
}catch{$m.State='SetupFailed';$m['Error']=$_.Exception.Message;Save;throw}finally{$c.Dispose()}
Write-Output "Fixture ready: $db; Completed and runtime read verified"

