$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
. "$root/tools/database/DatabaseTestTools.ps1"
$path=Join-Path $PSScriptRoot 'manifest.json'
$m=Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
if($m.Cleaned){Write-Output 'Already cleaned';return}
if($m.Server -cne 'localhost' -or $m.Database -cnotmatch '^CoffeeManager_Test_G04_\d{8}_\d{6}_[a-f0-9]{8}_checkout$' -or $m.Database -cne "CoffeeManager_Test_G04_$($m.Marker)_checkout"){throw 'Unexpected exact checkout fixture'}
$c=Open-TestConnection $m.Database
try{
 if((Read-TestScalar $c 'SELECT COUNT(*) FROM dbo.G04TestMarker') -ne 1 -or (Read-TestScalar $c 'SELECT RunId FROM dbo.G04TestMarker') -cne $m.Marker){throw 'Marker mismatch'}
 $target=$m.Database;$c.ChangeDatabase('master')
 Run-TestSql $c "ALTER DATABASE [$target] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$target];"
 if((Read-TestScalar $c "SELECT COUNT(*) FROM sys.databases WHERE name=N'$target'") -ne 0){throw 'Cleanup incomplete'}
}finally{$c.Dispose()}
$m.Cleaned=$true;$m.State='Completed; isolated checkout DB and fixture actor removed';$m.RetainReason='None'
$m | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $path -Encoding utf8
Write-Output "Removed only $target"
