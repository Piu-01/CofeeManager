$ErrorActionPreference='Stop'
$root=(Get-Location).Path
. "$root/tools/database/DatabaseTestTools.ps1"
$folder=$PSScriptRoot
$path=Join-Path $folder 'manifest.json'
$m=Get-Content $path -Raw | ConvertFrom-Json
$expected='CoffeeManager_Test_G03_20261018_220716_a66f73ea_demo'
if($m.Database -cne $expected -or $m.Marker -cne '20261018_220716_a66f73ea'){throw 'Unexpected resource manifest'}
$p=Get-Process -Id $m.ProcessId -ErrorAction SilentlyContinue
if($p){
 $exe=Join-Path $root 'src/CafeManager.WinForms/bin/Debug/net10.0-windows/CafeManager.WinForms.exe'
 if($p.Path -ine $exe){throw 'Owned process path mismatch'}
 Stop-Process -Id $p.Id
 $p.WaitForExit(10000) | Out-Null
}
$c=Open-TestConnection $expected
try {
 if((Read-TestScalar $c 'SELECT RunId FROM dbo.G03TestMarker;') -cne $m.Marker){throw 'Marker mismatch'}
 $c.ChangeDatabase('master')
 Run-TestSql $c "ALTER DATABASE [$expected] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$expected];"
 if((Read-TestScalar $c "SELECT COUNT(*) FROM sys.databases WHERE name=N'$expected';") -ne 0){throw 'Cleanup incomplete'}
}finally{$c.Dispose()}
$m.Cleaned=$true; $m.State='Completed; database removed; owned GUI process stopped';$m.RetainReason='None'
$m | ConvertTo-Json | Set-Content $path -Encoding utf8
$before=Import-Csv "$root/docs/tasks/g03-du-lieu-phuc-vu-ban-hang/runs/acceptance_20261018_201052_07b32f2a/source-before.csv"
$changed=@($before | Where-Object { (Get-FileHash -LiteralPath $_.Path -Algorithm SHA256).Hash -cne $_.Hash })
[ordered]@{ComparedFiles=$before.Count; ChangedFiles=$changed.Count; AcceptanceSourceUnchanged=($changed.Count -eq 0); ChangedPaths=@($changed.Path)} | ConvertTo-Json | Set-Content (Join-Path $folder 'source-audit-final.json') -Encoding utf8
Write-Output "Supplemental database cleaned; source changes: $($changed.Count)"


