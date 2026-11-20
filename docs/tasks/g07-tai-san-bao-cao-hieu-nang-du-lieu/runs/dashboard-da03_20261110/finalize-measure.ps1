$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager'
$runId='20261110_120959_21ead801'
$out=Join-Path $root "docs/tasks/g07-tai-san-bao-cao-hieu-nang-du-lieu/runs/dashboard-da03-perf_$runId"
$report=Get-Content (Join-Path $out 'measure.json') -Raw | ConvertFrom-Json
$kinds=@('revenue','invoices','average','trend','channel','product','orders','delivery','material','debt','expense')
function ResultRows([string]$tag,[string]$k){
 @(Get-Content (Join-Path $out "$tag-$k.log") -Encoding UTF8 | Where-Object {$_ -match '^(Invoice|Finance|Material|Debt)\b'})
}
$first=(Get-Item (Join-Path $out '028-revenue.log')).LastWriteTime
$parity=[ordered]@{}
foreach($k in $kinds){
 $a=ResultRows '028' $k;$b=ResultRows '029' $k
 $identical=(($a -join "`n") -ceq ($b -join "`n"))
 $parity[$k]=[ordered]@{Rows=$a.Count;Identical=$identical}
}
$touched=@(Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'database'),(Join-Path $root 'tools/CafeManager.G07Probe') -Recurse -File |
 Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs','.sql','.csproj' -and $_.LastWriteTime -gt $first })
$report | Add-Member -NotePropertyName ResultParity -NotePropertyValue $parity -Force
$report | Add-Member -NotePropertyName FilesChangedDuringMeasurement -NotePropertyValue @($touched.FullName) -Force
$report | Add-Member -NotePropertyName SourceUnchanged -NotePropertyValue ($touched.Count -eq 0) -Force
$report.Status=if($report.SourceUnchanged -and @($parity.Values|Where-Object{-not $_.Identical}).Count -eq 0){'Passed'}else{'Failed'}
$report | Add-Member -NotePropertyName ParityMethod -NotePropertyValue 'Joined lines compared case-sensitively; ReadAt/timing lines excluded because only source rows are matched' -Force
$report | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $out 'measure.json') -Encoding utf8
$m=Get-Content (Join-Path $out 'manifest.json') -Raw | ConvertFrom-Json
$m.Status=$report.Status
$m | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $out 'manifest.json') -Encoding utf8
Write-Output "finalize: $($report.Status)"
foreach($k in $kinds){
 $b=$report.Before028.$k;$a=$report.After029.$k
 Write-Output ("{0,-9} reads {1,7} -> {2,7} | sql {3,8} -> {4,8} ms | wall {5,7} -> {6,7} ms | parity {7}" -f $k,$b.LogicalReadsPerExec,$a.LogicalReadsPerExec,$b.SqlElapsedMsPerExec,$a.SqlElapsedMsPerExec,$b.WallMsPerExec,$a.WallMsPerExec,$parity[$k].Identical)
}
