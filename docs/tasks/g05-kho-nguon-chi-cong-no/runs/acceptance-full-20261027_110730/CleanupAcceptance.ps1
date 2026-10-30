$ErrorActionPreference='Stop';$root='D:/Workspace/Nam_3/HK1/CoffeeManager';. "$root/tools/database/DatabaseTestTools.ps1"
$metadata=Get-Content "$PSScriptRoot/gui-fixture.json" -Raw|ConvertFrom-Json
$database='CoffeeManager_Test_G05_20261027_110753_fb67dcbb_demo';$marker='20261027_110753_fb67dcbb'
if($metadata.Database -cne $database -or $metadata.Marker -cne $marker){throw 'Metadata mismatch'}
if(Get-Process -Id 27396 -ErrorAction SilentlyContinue){throw 'Own GUI process still running; no cleanup'}
$result=[ordered]@{Database=$database;Marker=$marker;GuiProcessExited=$true;DatabaseDropped=$false;PrivateDirectoryDeleted=$false;PendingFilesDeleted=@();BackupRetained=$true;CleanedUtc=[DateTime]::UtcNow.ToString('o')}
$connection=Open-TestConnection $database
try{
 if((Read-TestScalar $connection 'SELECT RunId FROM dbo.G05TestMarker') -cne $marker){throw 'Wrong database marker'}
 $environment=[ordered]@{Database=$database;Marker=$marker;SqlVersion=[string](Read-TestScalar $connection 'SELECT @@VERSION');Schema=Read-TestScalar $connection 'SELECT MAX(Version) FROM dbo.SchemaMigrations';Sdk=(& dotnet --version);Windows=[Environment]::OSVersion.VersionString;GuiFixtureActor=9001001;GuiDpi='NOT VERIFIED; no DPI PASS inferred';Connection='localhost Integrated Security; G05Runtime WITHOUT LOGIN/CafeManagerApp for permission subcases'}
 $environment|ConvertTo-Json -Depth 5|Set-Content "$PSScriptRoot/environment.json" -Encoding utf8
 [string](Read-TestScalar $connection "SELECT (SELECT Code,CutoverKind,CASE WHEN LinesJson IS NULL THEN 1 ELSE 0 END NullLines FROM dbo.vw_StocktakeRead WHERE Code LIKE 'KK9900000%' ORDER BY Code FOR JSON PATH)")|Set-Content "$PSScriptRoot/stocktake-null-evidence.json" -Encoding utf8
}finally{$connection.Dispose();[Data.SqlClient.SqlConnection]::ClearAllPools()}
$master=Open-TestConnection master
try{
 if($database -cne 'CoffeeManager_Test_G05_20261027_110753_fb67dcbb_demo'){throw 'Cleanup name mismatch'}
 Run-TestSql $master "ALTER DATABASE [$database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [$database];";$result.DatabaseDropped=$true
 $result.RemainingOwnedDatabases=[int](Read-TestScalar $master "SELECT COUNT(*) FROM sys.databases WHERE name IN('CoffeeManager_Test_G05_20261027_110753_fb67dcbb_clean','CoffeeManager_Test_G05_20261027_110753_fb67dcbb_demo')")
}finally{$master.Dispose()}
$tempRoot=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')
$expected=[IO.Path]::GetFullPath([IO.Path]::Combine($tempRoot,'CoffeeManager_G05_'+$marker))
$target=[IO.Path]::GetFullPath([string]$metadata.PrivateDirectory)
if($target -cne $expected -or -not $target.StartsWith($tempRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Private cleanup path mismatch'}
if((Get-Content -LiteralPath (Join-Path $target 'marker.txt') -Raw).Trim() -cne $marker){throw 'Private directory marker mismatch'}
Remove-Item -LiteralPath $target -Recurse -Force;$result.PrivateDirectoryDeleted=$true
$pendingRoot=[IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'CoffeeManager/Pending'))
foreach($leaf in @('9001001_FinanceCreate.bin','9001001_FinanceCreate.bin.lock','9001001_FinanceCreate.bin.tmp')){
 $path=[IO.Path]::GetFullPath((Join-Path $pendingRoot $leaf));if(-not $path.StartsWith($pendingRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Pending path mismatch'}
 if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path -Force;$result.PendingFilesDeleted+=$leaf}
}
$result|ConvertTo-Json -Depth 6|Set-Content "$PSScriptRoot/cleanup.json" -Encoding utf8
Write-Output "Test database dropped=$($result.DatabaseDropped); private credentials deleted=$($result.PrivateDirectoryDeleted); GUI exited=$($result.GuiProcessExited)"

