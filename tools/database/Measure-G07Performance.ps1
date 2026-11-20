[CmdletBinding()]param()
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$output=Join-Path $root "docs/tasks/g07-tai-san-bao-cao-hieu-nang-du-lieu/runs/performance_$runId"
New-Item -ItemType Directory -Path $output -Force|Out-Null
$name="CoffeeManager_Test_G07Bench_$runId"
$manifest=[ordered]@{RunId=$runId;Kind='T29 implementation measurement; not acceptance';Server='localhost';Database=$name;Created=$false;Cleaned=$false;Status='Running'}
function Save-Manifest{$manifest|ConvertTo-Json -Depth 6|Set-Content (Join-Path $output 'manifest.json') -Encoding utf8}
function Source-Snapshot {
 Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'database'),(Join-Path $root 'tools/CafeManager.G07Probe') -Recurse -File | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs','.sql','.csproj' } | Sort-Object FullName | ForEach-Object {[pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}}
}
$before=@(Source-Snapshot);$before|Export-Csv (Join-Path $output 'source-before.csv') -NoTypeInformation
Save-Manifest
try{
 & dotnet build (Join-Path $root 'tools/CafeManager.G07Probe') -c Debug -v minimal *> (Join-Path $output 'build.log');if($LASTEXITCODE -ne 0){throw 'Probe build failed'}
 $connection=Open-TestConnection 'master'
 try{
  if((Read-TestScalar $connection "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name'") -ne 0){throw 'Target exists'}
  Run-TestSql $connection "CREATE DATABASE [$name]";$manifest.Created=$true;Save-Manifest;$connection.ChangeDatabase($name)
  Run-TestSql $connection "CREATE TABLE dbo.G07TestMarker(RunId varchar(40) PRIMARY KEY);INSERT dbo.G07TestMarker VALUES('$runId')"
  Run-TestSql $connection ([regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/DB_Schema.sql')),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$',''))
 }finally{$connection.Dispose()}
 for($version=1;$version -le 26;$version++){
  $migration=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $version));if($migration.Count -ne 1){throw 'Ambiguous migration'}
  & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $name -Version $version -File $migration[0].FullName | ConvertTo-Json -Compress|Add-Content (Join-Path $output 'migrations.jsonl')
 }
 & dotnet run --no-build --project (Join-Path $root 'tools/CafeManager.G07Probe') -- $name $runId $output benchmark
 if($LASTEXITCODE -ne 0){throw 'Benchmark probe failed'}
 $manifest.Status='Succeeded'
}catch{$manifest.Status='Failed';$manifest['Error']=$_.Exception.ToString()}
finally{
 $after=@(Source-Snapshot);$after|Export-Csv (Join-Path $output 'source-after.csv') -NoTypeInformation
 $manifest['SourceUnchanged']=(@(Compare-Object $before $after -Property Path,Hash).Count -eq 0)
 if(-not $manifest.SourceUnchanged){$manifest.Status='Failed'}
 if($manifest.Created){$cleanup=$null;try{
  if($name -notmatch '^CoffeeManager_Test_G07Bench_\d{8}_\d{6}_[a-f0-9]{8}$'){throw 'Invalid cleanup name'}
  $cleanup=Open-TestConnection $name;if((Read-TestScalar $cleanup 'SELECT RunId FROM dbo.G07TestMarker') -cne $runId){throw 'Marker mismatch'}
  $cleanup.ChangeDatabase('master');Run-TestSql $cleanup "ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [$name]";$manifest.Cleaned=$true
 }catch{$manifest['RetainedReason']=$_.Exception.Message;$manifest.Status='Failed'}finally{if($null -ne $cleanup){$cleanup.Dispose()}}}
 Save-Manifest;Write-Output "G07 performance: $output; $($manifest.Status); created=$($manifest.Created)/cleaned=$($manifest.Cleaned)"
}
if($manifest.Status -ne 'Succeeded'){throw "T29 measurement failed: $output"}

