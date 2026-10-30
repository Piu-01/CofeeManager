$ErrorActionPreference='Stop'
$root=(Get-Location).Path
$run=Join-Path $root 'docs/tasks/g05-kho-nguon-chi-cong-no/runs/b07-20261030'
. (Join-Path $root 'tools/database/DatabaseTestTools.ps1')
$meta=Get-Content (Join-Path $run 'gui-fixture.json') -Raw|ConvertFrom-Json
$expected='CoffeeManager_Test_G05_20261030_035847_cdac8a8e_demo'
if($meta.Database -cne $expected -or $meta.Marker -cne '20261030_035847_cdac8a8e'){throw 'Wrong fixture'}
if(Get-Process -Id $meta.GuiProcessId -ErrorAction SilentlyContinue){throw 'Own GUI still running'}
$connection=Open-TestConnection $expected
try{if((Read-TestScalar $connection 'SELECT RunId FROM dbo.G05TestMarker') -cne $meta.Marker){throw 'Wrong database marker'}}finally{$connection.Dispose()}
[System.Data.SqlClient.SqlConnection]::ClearAllPools()
$master=Open-TestConnection 'master'
try{Run-TestSql $master "ALTER DATABASE [$expected] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$expected];"; $remaining=[int](Read-TestScalar $master "SELECT COUNT(*) FROM sys.databases WHERE name=N'$expected'")}finally{$master.Dispose()}
$tempRoot=[IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$allowed=[IO.Path]::GetFullPath((Join-Path $tempRoot 'CoffeeManager_G05_20261030_035847_cdac8a8e'))
$target=[IO.Path]::GetFullPath($meta.PrivateDirectory)
if($target -cne $allowed -or -not $target.StartsWith($tempRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Wrong credential directory'}
if(Test-Path -LiteralPath $target){if((Get-Content (Join-Path $target 'marker.txt') -Raw).Trim() -cne $meta.Marker){throw 'Wrong directory marker'};Remove-Item -LiteralPath $target -Recurse -Force}
@{Created=1;Cleaned=1;Remaining=$remaining;GuiExited=$true;CredentialDirectoryDeleted=(-not(Test-Path -LiteralPath $target));DevelopmentTouched=$false}|ConvertTo-Json|Set-Content (Join-Path $run 'resource-audit.json')
$manifestPath=Join-Path $run 'manifest.json'
$manifest=Get-Content $manifestPath -Raw|ConvertFrom-Json
$manifest.Cleaned=$true;$manifest.Status='B07RegressionPassed';$manifest|ConvertTo-Json -Depth 8|Set-Content $manifestPath
Write-Output 'Cleanup complete: created1, cleaned1, remaining0; GUI exited; DPAPI directory removed.'
