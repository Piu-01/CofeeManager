param([string]$Database='CoffeeManager_Test_G07Bench_20261110_120959_21ead801')
$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
if($Database -notmatch '^CoffeeManager_Test_G07Bench_(\d{8}_\d{6}_[a-f0-9]{8})$'){throw 'Unexpected benchmark database.'}
$runId=$Matches[1]
$out=Join-Path $root "docs/tasks/g07-tai-san-bao-cao-hieu-nang-du-lieu/runs/dashboard-da03-perf_$runId"
if(-not (Test-Path (Join-Path $out 'manifest.json'))){throw 'Run folder missing'}
$report=[ordered]@{Database=$Database;RunId=$runId;Status='Running'}
function Save{$report|ConvertTo-Json -Depth 8|Set-Content (Join-Path $out 'measure.json') -Encoding utf8}
function Source-Snapshot {
 Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'database'),(Join-Path $root 'tools/CafeManager.G07Probe') -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs','.sql','.csproj' } | Sort-Object FullName | ForEach-Object {[pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}}
}
$kinds=@('revenue','invoices','average','trend','channel','product','orders','delivery','material','debt','expense')
$beforeSnapshot=@(Source-Snapshot)
function Measure-Kind([string]$tag){
 foreach($k in $kinds){
  $log=Join-Path $out "$tag-$k.log"
  & sqlcmd -S localhost -E -C -d $Database -i (Join-Path $PSScriptRoot 'measure-sources.sql') -v Kind=$k -W -h -1 -o $log
  if($LASTEXITCODE -ne 0 -or -not (Test-Path $log)){throw "sqlcmd failed $tag $k"}
 }
}
function Summarize([string]$tag){
 $r=[ordered]@{}
 foreach($k in $kinds){
  $p=Join-Path $out "$tag-$k.log"
  if(-not (Test-Path $p)){throw "missing $p"}
  $t=[IO.File]::ReadAllText($p)
  $reads=0;foreach($m in [regex]::Matches($t,'logical reads (\d+)')){$reads+=[int]$m.Groups[1].Value}
  $execs=[regex]::Matches($t,'SQL Server Execution Times:\s*CPU time = (\d+) ms,\s*elapsed time = (\d+) ms')
  $ms=0;foreach($m in $execs){$ms+=[int]$m.Groups[2].Value}
  $wall=@();foreach($m in [regex]::Matches($t,'REP\|[^|]*\|\d+\|(\d+)')){$wall+=[int]$m.Groups[1].Value/1000.0}
  if($wall.Count -ne 5 -or $execs.Count -eq 0){throw "Unexpected measurement log $p"}
  $r[$k]=[ordered]@{LogicalReadsPerExec=[int][math]::Round($reads/5);SqlElapsedMsPerExec=[math]::Round($ms/5,1);WallMsPerExec=[math]::Round((($wall|Measure-Object -Average).Average),1);Statements=$execs.Count}
 }
 return $r
}
function ResultRows([string]$tag,[string]$k){
 @(Get-Content (Join-Path $out "$tag-$k.log") -Encoding UTF8 | Where-Object {$_ -match '^(Invoice|Finance|Material|Debt)\b'} | Sort-Object)
}
try{
 $c=Open-TestConnection $Database
 try{
  if((Read-TestScalar $c "SELECT RunId FROM dbo.G07TestMarker") -cne $runId){throw 'Marker mismatch'}
  $report['Bills']=[int](Read-TestScalar $c 'SELECT COUNT(*) FROM dbo.HOADON')
 }finally{$c.Dispose()}
 if($report.Bills -lt 10000){throw "Dataset incomplete: $($report.Bills)"}
 foreach($v in 27,28){
  $f=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $v));if($f.Count -ne 1){throw "Migration $v missing"}
  & "$root/tools/database/Apply-Migration.ps1" -Database $Database -Version $v -File $f[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $out 'migrations.jsonl')
 }
 Measure-Kind '028'
 $report['Before028']=Summarize '028'
 & "$root/tools/database/Apply-Migration.ps1" -Database $Database -Version 29 -File (Resolve-Path "$root/database/migrations/029_dashboard_source_lock_order.sql").Path | ConvertTo-Json -Compress | Add-Content (Join-Path $out 'migrations.jsonl')
 Measure-Kind '029'
 $report['After029']=Summarize '029'
 $parity=[ordered]@{}
 foreach($k in $kinds){$a=ResultRows '028' $k;$b=ResultRows '029' $k;$diff=@(Compare-Object $a $b);$parity[$k]=[ordered]@{Rows=$a.Count;Identical=($diff.Count -eq 0)}}
 $report['ResultParity']=$parity
 $afterSnapshot=@(Source-Snapshot)
 $report['SourceUnchanged']=((@(Compare-Object $beforeSnapshot $afterSnapshot -Property Path,Hash).Count -eq 0))
 $report['Status']=if($report.SourceUnchanged -and @($parity.Values|Where-Object{-not $_.Identical}).Count -eq 0){'Passed'}else{'Failed'}
}catch{$report['Status']='Failed';$report['Error']=$_.Exception.ToString()}
finally{
 Save
 $m=Get-Content (Join-Path $out 'manifest.json') -Raw | ConvertFrom-Json
 $m.Status=$report.Status
 $m|Add-Member -NotePropertyName MeasurementPhase -NotePropertyValue 'measure-only.ps1 (dataset reused; first attempt failed on PowerShell alias Measure)' -Force
 $m|ConvertTo-Json -Depth 8|Set-Content (Join-Path $out 'manifest.json') -Encoding utf8
}
Write-Output "measure-only: $($report.Status); $out"
