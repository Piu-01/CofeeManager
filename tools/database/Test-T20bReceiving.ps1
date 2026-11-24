# T20b receiving acceptance runner: isolated real SQL Server DB, schema 001-045.
# ASCII only (Windows PowerShell 5.1 safe). Creates, tests and drops CoffeeManager_Test_G05_<runId>_demo.
[CmdletBinding()]param([switch]$Keep)
$ErrorActionPreference='Stop'
$ThroughVersion=45
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$name="CoffeeManager_Test_G05_${runId}_demo"
$output=Join-Path $root "docs/tasks/g05-kho-nguon-chi-cong-no/nhan-hang/runs/acceptance_$runId"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$manifest=[ordered]@{RunId=$runId;Kind='T20b SQL acceptance; GUI/print not implied';Server='localhost';Database=$name;ThroughVersion=$ThroughVersion;Operator=[Security.Principal.WindowsIdentity]::GetCurrent().Name;Created=$false;Cleaned=$false;InventoryUnchanged=$false;SqlFailed=$null;Status='Running'}
function Save-Manifest { $manifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $output 'manifest.json') -Encoding utf8 }
Save-Manifest
$connection=$null
try {
 $master=Open-TestConnection 'master'
 try {
  if((Read-TestScalar $master "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name'") -ne 0){throw 'Target already exists'}
  Run-TestSql $master "CREATE DATABASE [$name];"
  $manifest.Created=$true; Save-Manifest
  $master.ChangeDatabase($name)
  Run-TestSql $master "CREATE TABLE dbo.G05TestMarker(RunId varchar(40) PRIMARY KEY); INSERT dbo.G05TestMarker VALUES('$runId');"
  $schema=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/DB_Schema.sql')),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$','')
  Run-TestSql $master $schema
  $seed=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/Insert_Data.sql')),'(?im)^USE QuanLyCafe_ERP_V2;\s*$','')
  Run-TestSql $master $seed
 } finally { $master.Dispose() }
 $connection=Open-TestConnection $name
 $inventoryBefore=[string](Read-TestScalar $connection 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')
 for($version=1;$version -le $ThroughVersion;$version++) {
  $migration=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $version))
  if($migration.Count -ne 1){throw "Missing/ambiguous migration $version"}
  & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $name -Version $version -File $migration[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $output 'migrations.jsonl')
 }
 $inventoryAfter=[string](Read-TestScalar $connection 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')
 $manifest['InventoryUnchanged']=($inventoryBefore -ceq $inventoryAfter)
 if(-not $manifest.InventoryUnchanged){throw 'Migrations changed inventory'}
 foreach($file in @('fixture.sql','t20-purchases.sql','t20b-receiving.sql')){
  $script=[IO.File]::ReadAllText((Join-Path $root "database/tests/g05/$file"))
  try { Run-TestSql $connection $script }
  catch { $manifest['Error']="Script $file : $($_.Exception.ToString())"; throw }
 }
 $raw=[string](Read-TestScalar $connection 'SELECT (SELECT * FROM dbo.G05AcceptanceLog ORDER BY RecordedAt,Part FOR JSON PATH)')
 Set-Content (Join-Path $output 'sql-results.json') $raw -Encoding utf8
 $manifest['SqlTotal']=[int](Read-TestScalar $connection 'SELECT COUNT(*) FROM dbo.G05AcceptanceLog')
 $manifest['SqlFailed']=[int](Read-TestScalar $connection "SELECT COUNT(*) FROM dbo.G05AcceptanceLog WHERE Result='FAIL'")
 $manifest['SchemaVersion']=[int](Read-TestScalar $connection 'SELECT MAX(Version) FROM dbo.SchemaMigrations')
 if($manifest.SqlFailed -ne 0){throw "SQL subcases failed: $($manifest.SqlFailed)"}
 $manifest.Status='Succeeded'
} catch { $manifest.Status='Failed'; if(-not $manifest.Error){$manifest['Error']=$_.Exception.ToString()} }
finally {
 if($null -ne $connection){$connection.Dispose()}
 if($manifest.Created -and -not $Keep){
  $cleanup=$null
  try {
   $cleanup=Open-TestConnection 'master'
   $cleanup.ChangeDatabase($name)
   if((Read-TestScalar $cleanup 'SELECT RunId FROM dbo.G05TestMarker') -cne $runId){throw 'Ownership marker mismatch'}
   $cleanup.ChangeDatabase('master')
   Run-TestSql $cleanup "ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$name];"
   $manifest.Cleaned=$true
  } catch { $manifest['CleanupError']=$_.Exception.Message }
  finally { if($null -ne $cleanup){$cleanup.Dispose()} }
 }
 Save-Manifest
 Write-Output "Evidence: $output; status=$($manifest.Status); sqlFailed=$($manifest.SqlFailed); db=$name; cleaned=$($manifest.Cleaned)"
}
if($manifest.Status -ne 'Succeeded'){throw "T20b acceptance failed: $output/manifest.json"}
