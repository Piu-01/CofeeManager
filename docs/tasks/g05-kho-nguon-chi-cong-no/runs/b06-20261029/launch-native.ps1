$ErrorActionPreference='Stop'
$root=(Get-Location).Path;$run=Join-Path $root 'docs/tasks/g05-kho-nguon-chi-cong-no/runs/b06-20261029'
. (Join-Path $root 'tools/database/DatabaseTestTools.ps1')
$db='CoffeeManager_Test_G05_20261029_194514_8bfcd1ce_demo';$c=Open-TestConnection $db
try{
 if((Read-TestScalar $c 'SELECT RunId FROM dbo.G05TestMarker') -cne '20261029_194514_8bfcd1ce'){throw 'Wrong marker'}
 Run-TestSql $c "UPDATE dbo.AppSetupState SET State='LegacyReview' WHERE Id=1 AND State='Pending'; DECLARE @h varchar(255),@s binary(8);SELECT @h=MatKhauHash,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=9001001;EXEC dbo.sp_SetupConfirmLegacy 9001001,@h,@s;"
 $before=[ordered]@{Purchases=[int](Read-TestScalar $c 'SELECT COUNT(*) FROM dbo.PHIEUNHAP');Issues=[int](Read-TestScalar $c 'SELECT COUNT(*) FROM dbo.PHIEUXUAT');Debts=[int](Read-TestScalar $c 'SELECT COUNT(*) FROM dbo.CONGNO_NCC');Finance=[int](Read-TestScalar $c 'SELECT COUNT(*) FROM dbo.PHIEUTHUCHI');ActorReceipts=[int](Read-TestScalar $c 'SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE ActorAccountId IN(9001001,9001002)');Inventory=[string](Read-TestScalar $c 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')}
 $before|ConvertTo-Json|Set-Content (Join-Path $run 'native-before.json')
}finally{$c.Dispose()}
$previous=$env:CAFEMANAGER_APP_CONNECTION
try{$env:CAFEMANAGER_APP_CONNECTION="Server=localhost;Database=$db;Integrated Security=true;Encrypt=true;TrustServerCertificate=true;";$process=Start-Process -FilePath (Join-Path $root 'src/CafeManager.WinForms/bin/Debug/net10.0-windows/CafeManager.WinForms.exe') -WindowStyle Normal -PassThru -RedirectStandardError (Join-Path $run 'gui-error.log');$meta=Get-Content (Join-Path $run 'gui-fixture.json') -Raw|ConvertFrom-Json;$meta|Add-Member GuiProcessId $process.Id;$meta|ConvertTo-Json -Depth 5|Set-Content (Join-Path $run 'gui-fixture.json');Write-Output "Own GUI process $($process.Id)"}finally{$env:CAFEMANAGER_APP_CONNECTION=$previous}
