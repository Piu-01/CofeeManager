[CmdletBinding()]param()
# Runs the promo-v2 SQL suite on a throwaway SQL Server database. It never touches
# CoffeeManager_Development and never modifies source; evidence is written under
# docs/tasks/promotion-v2/runs/.
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$name="CoffeeManager_Test_PromoV2_$runId"
$output=Join-Path $root "docs/tasks/promotion-v2/runs/$runId"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$manifest=[ordered]@{RunId=$runId;Server='localhost';Database=$name;SchemaVersion=42;Operator=[Security.Principal.WindowsIdentity]::GetCurrent().Name;Steps=@();Status='Running';Created=$false;Cleaned=$false}
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
  Run-TestSql $master "CREATE TABLE dbo.PromoV2TestMarker(RunId varchar(40) NOT NULL PRIMARY KEY); INSERT dbo.PromoV2TestMarker VALUES('$runId');"
  $schema=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/DB_Schema.sql')),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$','')
  Run-TestSql $master $schema
  $seed=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/Insert_Data.sql')),'(?im)^USE QuanLyCafe_ERP_V2;\s*$','')
  Run-TestSql $master $seed
  for($version=1;$version -le 42;$version++) {
   $migration=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $version))
   if($migration.Count -ne 1){throw "Missing/ambiguous migration $version"}
   & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $name -Version $version -File $migration[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $output 'migrations.jsonl')
  }
  Invoke-Step $master 'fixture' 'database/tests/promo-v2/fixture.sql'
  Invoke-Step $master '01-meta-date' 'database/tests/promo-v2/01-meta-date.sql'
  Invoke-Step $master '02-discount-gift-preview' 'database/tests/promo-v2/02-discount-gift-preview.sql'
  Invoke-Step $master '03-checkout-gift' 'database/tests/promo-v2/03-checkout-gift.sql'
  Invoke-Step $master '04-regression' 'database/tests/promo-v2/04-regression.sql'
  $manifest.Status='Succeeded'
 } finally { $master.Dispose() }
} catch {
 $manifest.Status='Failed'; $manifest['Error']=$_.Exception.ToString()
} finally {
 $after=@(Source-Snapshot); $after | Export-Csv (Join-Path $output 'source-after.csv') -NoTypeInformation
 $manifest['SourceUnchanged']=(@(Compare-Object $before $after -Property Path,Hash).Count -eq 0)
 if($manifest.Created -and $name -match '^CoffeeManager_Test_PromoV2_\d{8}_\d{6}_[a-f0-9]{8}$') {
  $cleanup=$null
  try {
   $cleanup=Open-TestConnection 'master'
   $cleanup.ChangeDatabase($name)
   if((Read-TestScalar $cleanup 'SELECT RunId FROM dbo.PromoV2TestMarker;') -cne $runId){throw 'Ownership marker mismatch'}
   $cleanup.ChangeDatabase('master')
   Run-TestSql $cleanup "ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$name];"
   $manifest.Cleaned=$true
  } catch { $manifest['Retained']=@{Database=$name;Reason=$_.Exception.Message} }
  finally { if($cleanup){$cleanup.Dispose()} }
 }
 if(-not $manifest.SourceUnchanged -or -not $manifest.Cleaned){$manifest.Status='Failed'}
 Save-Manifest
 Write-Output "PromoV2 evidence: $output; status=$($manifest.Status)"
}
if($manifest.Status -ne 'Succeeded'){throw "PromoV2 suite failed: $output/manifest.json"}
