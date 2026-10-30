$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
. "$root/tools/database/DatabaseTestTools.ps1"
$path = Join-Path $PSScriptRoot 'manifest.json'
$m = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
if ($m.Cleaned) { Write-Output 'Already cleaned'; return }
if ($m.Server -cne 'localhost' -or $m.Database -cnotmatch '^CoffeeManager_Test_G04_\d{8}_\d{6}_[a-f0-9]{8}_manual$' -or $m.Database -cne "CoffeeManager_Test_G04_$($m.Marker)_manual") { throw 'Unexpected exact test target' }
foreach ($pidValue in $m.ProcessIds) {
    if (Get-Process -Id $pidValue -ErrorAction SilentlyContinue) { throw "Close test app PID $pidValue first; cleanup does not stop user processes." }
}
$c = Open-TestConnection $m.Database
try {
    if ((Read-TestScalar $c 'SELECT RunId FROM dbo.G04TestMarker') -cne $m.Marker) { throw 'Marker mismatch' }
    $db=$m.Database; $c.ChangeDatabase('master')
    Run-TestSql $c "ALTER DATABASE [$db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$db];"
} finally { $c.Dispose() }
$m.Cleaned=$true; $m.RetainReason='None'; $m.State='Manual test database removed'
$m | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $path -Encoding utf8
