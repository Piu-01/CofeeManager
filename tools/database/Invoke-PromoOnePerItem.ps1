[CmdletBinding()]param()
# Runs the promo-one-per-item SQL suite (migration 044) on a throwaway SQL Server database.
# It never touches CoffeeManager_Development and never modifies source; evidence is written under
# docs/tasks/promotion-mot-mon-mot-km/runs/.
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$name="CoffeeManager_Test_PromoOnePerItem_$runId"
$output=Join-Path $root "docs/tasks/promotion-mot-mon-mot-km/runs/$runId"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$manifest=[ordered]@{RunId=$runId;Server='localhost';Database=$name;SchemaVersion=44;Operator=[Security.Principal.WindowsIdentity]::GetCurrent().Name;Steps=@();Status='Running';Created=$false;Cleaned=$false}
function Save-Manifest { $manifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $output 'manifest.json') -Encoding utf8 }
function Source-Snapshot {
 Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'database'),(Join-Path $root 'tests') -Recurse -File |
 Where-Object { $_.FullName -notmatch '[\\/](bin|obj|runs)[\\/]' -and $_.Extension -in '.cs','.sql','.csproj','.json' } |
 Sort-Object FullName | ForEach-Object { [pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash $_.FullName -Algorithm SHA256).Hash} }
}
function Invoke-Step($Connection,[string]$Label,[string]$File) {
 $sw=[Diagnostics.Stopwatch]::StartNew()
 try {
  Run-TestSql $Connection ([IO.File]::ReadAllText((Join-Path $root $File)))
  $sw.Stop(); $manifest.Steps+=@{Step=$Label;File=$File;Status='PASS';Ms=$sw.ElapsedMilliseconds}
 } catch {
  $sw.Stop(); $manifest.Steps+=@{Step=$Label;File=$File;Status='FAIL';Ms=$sw.ElapsedMilliseconds;Error=$_.Exception.Message}
  throw
 } finally { Save-Manifest }
}
$before=@(Source-Snapshot); $before | Export-Csv (Join-Path $output 'source-before.csv') -NoTypeInformation
Save-Manifest
try {
 $master=Open-TestConnection 'master'
 try {
  if((Read-TestScalar $master "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name';") -ne 0){throw 'Target already exists'}
  Run-TestSql $master "CREATE DATABASE [$name];"
  $manifest.Created=$true; Save-Manifest
  $master.ChangeDatabase($name)
  Run-TestSql $master "CREATE TABLE dbo.PromoOnePerItemTestMarker(RunId varchar(40) NOT NULL PRIMARY KEY); INSERT dbo.PromoOnePerItemTestMarker VALUES('$runId');"
  $schema=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/DB_Schema.sql')),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$','')
  Run-TestSql $master $schema
  $seed=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/Insert_Data.sql')),'(?im)^USE QuanLyCafe_ERP_V2;\s*$','')
  Run-TestSql $master $seed
  for($version=1;$version -le 44;$version++) {
   $migration=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $version))
   if($migration.Count -ne 1){throw "Missing/ambiguous migration $version"}
   & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $name -Version $version -File $migration[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $output 'migrations.jsonl')
  }
  Invoke-Step $master 'fixture' 'database/tests/promo-one-per-item/fixture.sql'
  Invoke-Step $master '01-selection' 'database/tests/promo-one-per-item/01-selection.sql'
  Invoke-Step $master '02-tier-after' 'database/tests/promo-one-per-item/02-tier-after.sql'
  Invoke-Step $master '03-checkout' 'database/tests/promo-one-per-item/03-checkout.sql'
  Invoke-Step $master '04-regression' 'database/tests/promo-one-per-item/04-regression.sql'
  Invoke-Step $master '05-performance' 'database/tests/promo-one-per-item/05-performance.sql'
  $timings=@(); $cmd=$master.CreateCommand()
  $cmd.CommandText="SELECT RunLabel,Ms FROM dbo.PromoOnePerItemTiming ORDER BY RunLabel"
  $r=$cmd.ExecuteReader()
  while($r.Read()){ $timings+=@{Label=[string]$r.GetValue(0);Ms=[decimal]$r.GetValue(1)} }
  $r.Close(); $cmd.Dispose()
  $manifest['Timings']=$timings
  $manifest.Status='Succeeded'
 } finally { $master.Dispose() }
} catch {
 $manifest.Status='Failed'; $manifest['Error']=$_.Exception.ToString()
} finally {
 $after=@(Source-Snapshot); $after | Export-Csv (Join-Path $output 'source-after.csv') -NoTypeInformation
 $manifest['SourceUnchanged']=(@(Compare-Object $before $after -Property Path,Hash).Count -eq 0)
 if($manifest.Created -and $name -match '^CoffeeManager_Test_PromoOnePerItem_\d{8}_\d{6}_[a-f0-9]{8}$') {
  $cleanup=$null
  try {
   $cleanup=Open-TestConnection 'master'
   $cleanup.ChangeDatabase($name)
   if((Read-TestScalar $cleanup 'SELECT RunId FROM dbo.PromoOnePerItemTestMarker;') -cne $runId){throw 'Ownership marker mismatch'}
   $cleanup.ChangeDatabase('master')
   Run-TestSql $cleanup "ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$name];"
   $manifest.Cleaned=$true
  } catch { $manifest['Retained']=@{Database=$name;Reason=$_.Exception.Message} }
  finally { if($cleanup){$cleanup.Dispose()} }
 }
 if(-not $manifest.SourceUnchanged -or -not $manifest.Cleaned){$manifest.Status='Failed'}
 Save-Manifest
 Write-Output "PromoOnePerItem evidence: $output; status=$($manifest.Status)"
}
if($manifest.Status -ne 'Succeeded'){throw "PromoOnePerItem suite failed: $output/manifest.json"}
