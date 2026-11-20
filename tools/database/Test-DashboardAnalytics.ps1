[CmdletBinding()]param([Parameter(Mandatory=$true)][switch]$RunApprovedTests)
# Chỉ gọi sau khi người dùng duyệt đợt test riêng. Không dùng development.
$ErrorActionPreference='Stop'
if(-not $RunApprovedTests){throw 'Cần duyệt đợt test trước khi chạy.'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$output=Join-Path $root "docs/tasks/g07-tai-san-bao-cao-hieu-nang-du-lieu/runs/dashboard_$runId"
New-Item -ItemType Directory -Path $output | Out-Null
$manifest=[ordered]@{RunId=$runId;Status='Running';Created=@();Cleaned=@();Retained=@();Results=@()}
function Save-Manifest {$manifest|ConvertTo-Json -Depth 8|Set-Content (Join-Path $output 'manifest.json') -Encoding utf8}
function Sources {
 Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'database'),(Join-Path $root 'tools/CafeManager.DashboardProbe') -Recurse -File |
 Where-Object {$_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs','.csproj','.sql'} |
 Sort-Object FullName | ForEach-Object {[pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}}
}
function Schema-Snapshot($connection) {
 [string](Read-TestScalar $connection "SELECT (SELECT t.name TableName,c.column_id,c.name ColumnName,TYPE_NAME(c.user_type_id) TypeName,c.max_length,c.precision,c.scale,c.is_nullable,c.is_identity,c.is_computed,cc.definition FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id LEFT JOIN sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id WHERE t.is_ms_shipped=0 ORDER BY t.name,c.column_id FOR JSON PATH,INCLUDE_NULL_VALUES)")
}
function Index-Snapshot($connection) {
 [string](Read-TestScalar $connection "SELECT (SELECT t.name TableName,i.name,i.type,i.is_unique,i.has_filter,i.filter_definition,ic.index_column_id,c.name ColumnName,ic.is_descending_key,ic.is_included_column FROM sys.tables t JOIN sys.indexes i ON i.object_id=t.object_id JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE t.is_ms_shipped=0 ORDER BY t.name,i.index_id,ic.index_column_id FOR JSON PATH,INCLUDE_NULL_VALUES)")
}
$before=@(Sources);$before|Export-Csv (Join-Path $output 'source-before.csv') -NoTypeInformation;Save-Manifest
try {
 & dotnet build (Join-Path $root 'tools/CafeManager.DashboardProbe') -c Debug -m:1 -nr:false -p:UseSharedCompilation=false -v minimal *> (Join-Path $output 'build-probe.log')
 if($LASTEXITCODE -ne 0){throw 'Probe build failed'}
 foreach($mode in @('clean','demo')) {
  $name="CoffeeManager_Test_G07_${runId}_$mode";$connection=Open-TestConnection 'master'
  try {
   if((Read-TestScalar $connection "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name'") -ne 0){throw 'DB exists'}
   Run-TestSql $connection "CREATE DATABASE [$name]";$manifest.Created+=@{Database=$name;Marker=$runId};Save-Manifest
   $connection.ChangeDatabase($name);Run-TestSql $connection "CREATE TABLE dbo.G07TestMarker(RunId varchar(40) PRIMARY KEY);INSERT dbo.G07TestMarker VALUES('$runId')"
   Run-TestSql $connection ([regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/DB_Schema.sql')),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$',''))
   if($mode -eq 'demo') {
    Run-TestSql $connection ([regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/Insert_Data.sql')),'(?im)^USE QuanLyCafe_ERP_V2;\s*$',''))
    Run-TestSql $connection ([IO.File]::ReadAllText((Join-Path $root 'database/tests/g07/dashboard-analytics-fixture.sql')))
   }
   for($v=1;$v -le 27;$v++) {
    $file=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $v));if($file.Count -ne 1){throw "Missing migration$v"}
    & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $name -Version $v -File $file[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $output "$mode-migrations.jsonl")
   }
   $schema=Schema-Snapshot $connection;$indexes=Index-Snapshot $connection;$data=Snapshot-TestRows $connection
   $schema|Set-Content (Join-Path $output "$mode-schema-before.json") -Encoding utf8
   $migration=Join-Path $root 'database/migrations/028_dashboard_analytics_read.sql'
   foreach($attempt in 1..2){& (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $name -Version 28 -File $migration | ConvertTo-Json -Compress | Add-Content (Join-Path $output "$mode-migrations.jsonl")}
   if($schema -cne (Schema-Snapshot $connection) -or $indexes -cne (Index-Snapshot $connection)){throw 'Schema tables/columns/indexes changed'}
   # Snapshot-TestRows bỏ SchemaMigrations; version28 là lần ghi duy nhất được chấp nhận.
   if($data -cne (Snapshot-TestRows $connection)){throw 'Migration changed data'}
   $manifest.Results+=@{Id='DA01';Mode=$mode;Status='PASS';Actual='028+rerun; tables/columns/computed/indexes and business data unchanged'};Save-Manifest
   if($mode -eq 'demo') {
    & (Join-Path $root 'tools/CafeManager.DashboardProbe/bin/Debug/net10.0-windows/CafeManager.DashboardProbe.exe') $name $runId (Join-Path $output 'typed-results.json') *> (Join-Path $output 'typed-probe.log')
    if($LASTEXITCODE -ne 0){$manifest.Results+=@{Id='typed';Status='FAIL';Actual='See typed-results.json'}}
   }
  } finally {$connection.Dispose()}
 }
 $manifest.Status=if(@($manifest.Results|Where-Object Status -eq 'FAIL').Count){'Failed'}else{'SubsetCompleted'}
} catch {$manifest.Status='Failed';$manifest['Error']=$_.Exception.ToString()}
finally {
 $after=@(Sources);$after|Export-Csv (Join-Path $output 'source-after.csv') -NoTypeInformation
 $manifest['SourceUnchanged']=(@(Compare-Object $before $after -Property Path,Hash).Count -eq 0)
 if(-not $manifest.SourceUnchanged){$manifest.Status='Failed'}
 foreach($item in $manifest.Created) {
  $cleanup=$null
  try {
   $name=$item.Database
   if($name -notmatch '^CoffeeManager_Test_G07_\d{8}_\d{6}_[a-f0-9]{8}_(clean|demo)$'){throw 'Invalid cleanup name'}
   $cleanup=Open-TestConnection $name
   if((Read-TestScalar $cleanup 'SELECT RunId FROM dbo.G07TestMarker') -cne $runId){throw 'Marker mismatch'}
   $cleanup.ChangeDatabase('master');Run-TestSql $cleanup "ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [$name]";$manifest.Cleaned+=$name
  } catch {$manifest.Retained+=@{Database=$item.Database;Reason=$_.Exception.Message}}
  finally {if($null -ne $cleanup){$cleanup.Dispose()}}
 }
 if($manifest.Retained.Count){$manifest.Status='Failed'};Save-Manifest
 Write-Output "Dashboard SQL subset: $output; $($manifest.Status). Concurrency/UI/performance recorded separately; no full acceptance implied."
}
if($manifest.Status -eq 'Failed'){throw "Test failed: $output/manifest.json"}
