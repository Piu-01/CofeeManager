[CmdletBinding()]param([ValidateSet('Setup','Cleanup')][string]$Action='Setup')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../../..'))
. (Join-Path $root 'tools/database/DatabaseTestTools.ps1')
$manifestPath=Join-Path $PSScriptRoot 'fixture.json'
if($Action -eq 'Setup') {
 if(Test-Path -LiteralPath $manifestPath){throw 'Manifest already exists; do not create another fixture.'}
 $runId='20261110_'+[guid]::NewGuid().ToString('N').Substring(0,8)
 $name='CoffeeManager_Test_UiTabs_'+$runId
 $m=[ordered]@{RunId=$runId;Server='localhost';Database=$name;Marker='UiTabsTestMarker';Schema=24;Created=$false;Cleaned=$false;Principal='UiTabsRuntime (database-only, no login)';Status='Preparing';Expires='End of approved before/after regression in this chat'}
 $m|ConvertTo-Json|Set-Content $manifestPath
 $connection=Open-TestConnection 'master'
 try {
  if((Read-TestScalar $connection "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name'") -ne 0){throw 'Database exists.'}
  Run-TestSql $connection "CREATE DATABASE [$name];"; $m.Created=$true; $m|ConvertTo-Json|Set-Content $manifestPath
  $connection.ChangeDatabase($name)
  Run-TestSql $connection "CREATE TABLE dbo.UiTabsTestMarker(RunId varchar(40) PRIMARY KEY); INSERT dbo.UiTabsTestMarker VALUES('$runId');"
  $schema=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/DB_Schema.sql')),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$','')
  Run-TestSql $connection $schema
  $seed=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/Insert_Data.sql')),'(?im)^USE QuanLyCafe_ERP_V2;\s*$','')
  Run-TestSql $connection $seed
  for($version=1;$version -le 24;$version++) {
   $file=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $version))
   if($file.Count -ne 1){throw 'Ambiguous migration.'}
   & (Join-Path $root 'tools/database/Apply-Migration.ps1') -Database $name -Version $version -File $file[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $PSScriptRoot 'migrations.jsonl')
  }
  Run-TestSql $connection 'CREATE USER UiTabsRuntime WITHOUT LOGIN; ALTER ROLE CafeManagerApp ADD MEMBER UiTabsRuntime;'
  $m.Status='Ready'; $m|ConvertTo-Json|Set-Content $manifestPath
 } catch { $m.Status='SetupFailed';$m['Error']=$_.Exception.Message;$m|ConvertTo-Json|Set-Content $manifestPath;throw } finally {$connection.Dispose()}
} else {
 $m=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
 if($m.Database -cne ('CoffeeManager_Test_UiTabs_'+$m.RunId) -or $m.RunId -notmatch '^20261110_[a-f0-9]{8}$'){throw 'Fixture allowlist mismatch.'}
 if($m.Cleaned){Write-Output 'Fixture already cleaned.';return}
 $connection=Open-TestConnection $m.Database
 try {
  if((Read-TestScalar $connection 'SELECT RunId FROM dbo.UiTabsTestMarker') -cne $m.RunId){throw 'Marker mismatch; refuse cleanup.'}
  $connection.ChangeDatabase('master');$name=$m.Database
  Run-TestSql $connection "ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$name];"
  $m.Cleaned=$true;$m.Status='Cleaned';$m|ConvertTo-Json|Set-Content $manifestPath
 } finally {$connection.Dispose()}
}
Get-Content -LiteralPath $manifestPath
