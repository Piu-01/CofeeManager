$ErrorActionPreference='Stop'
$root=(Get-Location).Path;$run=Join-Path $root 'docs/tasks/g05-kho-nguon-chi-cong-no/runs/native-followup-20261029'
. (Join-Path $root 'tools/database/DatabaseTestTools.ps1')
$id=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$db="CoffeeManager_Test_G05_${id}_demo"
$manifest=[ordered]@{Database=$db;Marker=$id;Server='localhost';Created=$false;Cleaned=$false;Status='Provisioning'}
$manifest|ConvertTo-Json|Set-Content (Join-Path $run 'manifest.json')
Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'database'),(Join-Path $root 'tests') -Recurse -File|Where-Object{$_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs','.sql','.csproj'}|Sort-Object FullName|ForEach-Object{[pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}}|Export-Csv (Join-Path $run 'source-fixed.csv') -NoTypeInformation
$c=Open-TestConnection 'master'
try{
 if((Read-TestScalar $c "SELECT COUNT(*) FROM sys.databases WHERE name=N'$db'") -ne 0){throw 'Target already exists'}
 Run-TestSql $c "CREATE DATABASE [$db];";$manifest.Created=$true;$manifest|ConvertTo-Json|Set-Content (Join-Path $run 'manifest.json')
 $c.ChangeDatabase($db);Run-TestSql $c "CREATE TABLE dbo.G05TestMarker(RunId varchar(40) PRIMARY KEY);INSERT dbo.G05TestMarker VALUES('$id');"
 foreach($file in @('DB_Schema.sql','Insert_Data.sql')){$sql=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root "database/$file")),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$','');Run-TestSql $c $sql}
 for($v=1;$v -le 21;$v++){$file=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $v));if($file.Count -ne 1){throw 'Migration ambiguous'};& (Join-Path $root 'tools/database/Apply-Migration.ps1') -Database $db -Version $v -File $file[0].FullName|ConvertTo-Json -Compress|Add-Content (Join-Path $run 'migrations.jsonl')}
}finally{$c.Dispose()}
& dotnet run --project (Join-Path $root 'docs/tasks/g05-kho-nguon-chi-cong-no/runs/acceptance-full-20261027_110730/gui-probe') -- setup $db $id $run *> (Join-Path $run 'fixture-setup.log')
if($LASTEXITCODE -ne 0){throw 'GUI fixture setup failed'}
$c=Open-TestConnection $db
try{
 Run-TestSql $c "UPDATE dbo.AppSetupState SET State='LegacyReview' WHERE Id=1 AND State='Pending';DECLARE @h varchar(255),@s binary(8);SELECT @h=MatKhauHash,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=9001001;EXEC dbo.sp_SetupConfirmLegacy 9001001,@h,@s;"
 $sql="SELECT (SELECT (SELECT COUNT(*) FROM dbo.PHIEUNHAP) Purchases,(SELECT COUNT(*) FROM dbo.PHIEUXUAT) Issues,(SELECT COUNT(*) FROM dbo.CONGNO_NCC) Debts,(SELECT COUNT(*) FROM dbo.PHIEUTHUCHI) Finance,(SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE ActorAccountId IN(9001001,9001002)) Receipts,JSON_QUERY((SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)) Inventory FOR JSON PATH,WITHOUT_ARRAY_WRAPPER)"
 [string](Read-TestScalar $c $sql)|Set-Content (Join-Path $run 'native-before.json')
}finally{$c.Dispose()}
$previous=$env:CAFEMANAGER_APP_CONNECTION
try{$env:CAFEMANAGER_APP_CONNECTION="Server=localhost;Database=$db;Integrated Security=true;Encrypt=true;TrustServerCertificate=true;";$p=Start-Process -FilePath (Join-Path $root 'src/CafeManager.WinForms/bin/Debug/net10.0-windows/CafeManager.WinForms.exe') -WindowStyle Normal -PassThru -RedirectStandardError (Join-Path $run 'gui-error.log');$meta=Get-Content (Join-Path $run 'gui-fixture.json') -Raw|ConvertFrom-Json;$meta|Add-Member GuiProcessId $p.Id;$meta|ConvertTo-Json -Depth 5|Set-Content (Join-Path $run 'gui-fixture.json');$manifest['GuiProcessId']=$p.Id}finally{$env:CAFEMANAGER_APP_CONNECTION=$previous}
$manifest.Status='ReadyForNative';$manifest|ConvertTo-Json|Set-Content (Join-Path $run 'manifest.json');Write-Output "Ready $db / PID $($manifest.GuiProcessId)"
