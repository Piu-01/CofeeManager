$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$name="CoffeeManager_Test_G07Bench_$runId"
$out=Join-Path $root "docs/tasks/g07-tai-san-bao-cao-hieu-nang-du-lieu/runs/dashboard-da03-perf_$runId"
New-Item -ItemType Directory -Path $out -Force|Out-Null
$manifest=[ordered]@{RunId=$runId;Kind='G07-DA03 I/O measurement 028 vs 029 on 10,000 invoices; not acceptance';Database=$name;Created=$false;Status='Running'}
function Save{$manifest|ConvertTo-Json -Depth 8|Set-Content (Join-Path $out 'manifest.json') -Encoding utf8}
function Source-Snapshot {
 Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'database'),(Join-Path $root 'tools/CafeManager.G07Probe') -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs','.sql','.csproj' } | Sort-Object FullName | ForEach-Object {[pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}}
}
$kinds=@('revenue','invoices','average','trend','channel','product','orders','delivery','material','debt','expense')
$beforeSnapshot=@(Source-Snapshot)
function Apply-Range([int]$from,[int]$to){
 for($v=$from;$v -le $to;$v++){
  $f=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $v));if($f.Count -ne 1){throw "Migration $v missing"}
  & "$root/tools/database/Apply-Migration.ps1" -Database $name -Version $v -File $f[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $out 'migrations.jsonl')
 }
}
function Measure([string]$tag){
 foreach($k in $kinds){
  & sqlcmd -S localhost -E -C -d $name -i (Join-Path $PSScriptRoot 'measure-sources.sql') -v Kind=$k -W -h -1 -o (Join-Path $out "$tag-$k.log")
  if($LASTEXITCODE -ne 0){throw "sqlcmd failed $tag $k"}
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
 @(Get-Content (Join-Path $out "$tag-$k.log") | Where-Object {$_ -match '^(Invoice|Finance|Material|Debt)\b'} | Sort-Object)
}
try{
 $m=Open-TestConnection 'master'
 try{
  if((Read-TestScalar $m "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name'") -ne 0){throw 'Target exists'}
  Run-TestSql $m "CREATE DATABASE [$name]"
 }finally{$m.Dispose()}
 $manifest.Created=$true;Save
 $c=Open-TestConnection $name
 try{
  Run-TestSql $c "CREATE TABLE dbo.G07TestMarker(RunId varchar(40) PRIMARY KEY);INSERT dbo.G07TestMarker VALUES('$runId')"
  Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/DB_Schema.sql')),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$',''))
 }finally{$c.Dispose()}
 # Khong nap seed: probe tu tao catalog qua contract; seed co san NVL01..NVL50 se trung ma fixture.
 Apply-Range 1 26
 $manifest['MeasurementActor']='Benchmark bootstrap admin created by probe (TAIKHOAN identity 1)'
 # App WinForms dang chay khoa bin cua no; build probe ra thu muc rieng de khong cham file dang mo.
 $binDir=Join-Path $root 'build/g07probe'
 & dotnet build (Join-Path $root 'tools/CafeManager.G07Probe') -c Debug -o $binDir -v minimal *> (Join-Path $out 'probe-build.log')
 if($LASTEXITCODE -ne 0){throw 'Probe build failed'}
 & dotnet (Join-Path $binDir 'CafeManager.G07Probe.dll') $name $runId (Join-Path $out 'probe') benchmark *> (Join-Path $out 'probe-run.log')
 $probeExit=$LASTEXITCODE
 $c=Open-TestConnection $name
 try{
  $bills=[int](Read-TestScalar $c 'SELECT COUNT(*) FROM dbo.HOADON')
  $manifest['ProbeExit']=$probeExit
  $manifest['Dataset']=[ordered]@{Bills=$bills;Paid=[int](Read-TestScalar $c "SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan'");Lines=[int](Read-TestScalar $c 'SELECT COUNT(*) FROM dbo.CHITIET_HD');FirstPaid=[string](Read-TestScalar $c 'SELECT CONVERT(varchar(30),MIN(NgayThanhToan),126) FROM dbo.HOADON');LastPaid=[string](Read-TestScalar $c 'SELECT CONVERT(varchar(30),MAX(NgayThanhToan),126) FROM dbo.HOADON')}
 }finally{$c.Dispose()}
 if($manifest.Dataset.Bills -lt 10000){throw "Dataset incomplete: $($manifest.Dataset.Bills)"}
 Apply-Range 27 28
 Measure '028'
 $manifest['Before028']=Summarize '028'
 & "$root/tools/database/Apply-Migration.ps1" -Database $name -Version 29 -File (Resolve-Path "$root/database/migrations/029_dashboard_source_lock_order.sql").Path | ConvertTo-Json -Compress | Add-Content (Join-Path $out 'migrations.jsonl')
 Measure '029'
 $manifest['After029']=Summarize '029'
 $parity=[ordered]@{}
 foreach($k in $kinds){$a=ResultRows '028' $k;$b=ResultRows '029' $k;$diff=@(Compare-Object $a $b);$parity[$k]=[ordered]@{Rows=$a.Count;Identical=($diff.Count -eq 0)}}
 $manifest['ResultParity']=$parity
 $afterSnapshot=@(Source-Snapshot)
 $manifest['SourceUnchanged']=((@(Compare-Object $beforeSnapshot $afterSnapshot -Property Path,Hash).Count -eq 0))
 $manifest['Status']=if($manifest.SourceUnchanged -and @($parity.Values|Where-Object{-not $_.Identical}).Count -eq 0){'Passed'}else{'Failed'}
}catch{$manifest['Status']='Failed';$manifest['Error']=$_.Exception.ToString()}
finally{Save}
Write-Output "measure10k: $($manifest.Status); $out"
