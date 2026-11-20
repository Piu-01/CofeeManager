[CmdletBinding()]param([ValidateRange(10,18)][int]$ThroughVersion=10)
# Mốc v18 (T34-01) cần schema035; v17 (T34) cần schema034; v16 (T33) cần schema032; các mốc cũ giữ nguyên ánh xạ version=schema.
$SchemaVersion=if($ThroughVersion -ge 18){35}elseif($ThroughVersion -ge 17){34}elseif($ThroughVersion -ge 16){32}else{$ThroughVersion}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$output=Join-Path $root "docs/tasks/g04-ban-hang-vong-doi-hoa-don/runs/technical_$runId"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$manifest=[ordered]@{RunId=$runId;Kind='Technical migration/dependency smoke only; not acceptance';Server='localhost';ThroughVersion=$ThroughVersion;SchemaVersion=$SchemaVersion;Operator=[Security.Principal.WindowsIdentity]::GetCurrent().Name;Created=@();Cleaned=@();Retained=@();Status='Running'}
function Save-Manifest { $manifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $output 'manifest.json') -Encoding utf8 }
function Source-Snapshot {
 Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'database'),(Join-Path $root 'tests') -Recurse -File |
 Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs','.sql','.csproj' } |
 Sort-Object FullName | ForEach-Object { [pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash $_.FullName -Algorithm SHA256).Hash} }
}
$before=@(Source-Snapshot); $before | Export-Csv (Join-Path $output 'source-before.csv') -NoTypeInformation
Save-Manifest
try {
 if($ThroughVersion -ge 15){
  & dotnet build (Join-Path $root 'tools/CafeManager.G04Probe') --no-restore -c Debug -v minimal *> (Join-Path $output 'probe-build.log')
  $manifest['ProbeBuildExitCode']=$LASTEXITCODE
  if($LASTEXITCODE -ne 0){throw 'Probe build failed; no stale binary smoke allowed'}
 }
 foreach($mode in @('clean','demo')) {
  $name="CoffeeManager_Test_G04_${runId}_$mode"
  $connection=Open-TestConnection 'master'
  try {
   if((Read-TestScalar $connection "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name'") -ne 0){throw 'Target already exists'}
   Run-TestSql $connection "CREATE DATABASE [$name];"
   $manifest.Created+=@{Database=$name;Marker=$runId;Mode=$mode}; Save-Manifest
   $connection.ChangeDatabase($name)
   Run-TestSql $connection "CREATE TABLE dbo.G04TestMarker(RunId varchar(40) PRIMARY KEY); INSERT dbo.G04TestMarker VALUES('$runId');"
   $schema=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/DB_Schema.sql')),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$','')
   Run-TestSql $connection $schema
   if($mode -eq 'demo') {
    $seed=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/Insert_Data.sql')),'(?im)^USE QuanLyCafe_ERP_V2;\s*$','')
    Run-TestSql $connection $seed
   }
   $balances=[string](Read-TestScalar $connection 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')
   for($version=1;$version -le $SchemaVersion;$version++) {
    $migration=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $version))
    if($migration.Count -ne 1){throw "Missing/ambiguous migration $version"}
    & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $name -Version $version -File $migration[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $output "$mode-migrations.jsonl")
   }
   if($balances -cne [string](Read-TestScalar $connection 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')){throw 'Migration changed inventory'}
   if($mode -eq 'demo') {
    Run-TestSql $connection ([IO.File]::ReadAllText((Join-Path $root "database/tests/g04/technical-v$ThroughVersion.sql")))
    if($ThroughVersion -ge 15){
     & dotnet run --no-build --project (Join-Path $root 'tools/CafeManager.G04Probe') -- --dal $name (Join-Path $output 'typed-smoke.json') *> (Join-Path $output 'typed-smoke.log')
     if($LASTEXITCODE -ne 0){throw 'Typed dependency smoke failed; inspect typed-smoke.json'}
     & dotnet run --no-build --project (Join-Path $root 'tools/CafeManager.G04Probe') -- --workspace $name (Join-Path $output 'native-workspace.json') *> (Join-Path $output 'native-workspace.log')
     if($LASTEXITCODE -ne 0){throw 'Native dependency smoke failed; inspect native-workspace.json/log'}
    }
   }
  } finally { $connection.Dispose() }
 }
 $manifest.Status='Succeeded'
} catch { $manifest.Status='Failed'; $manifest['Error']=$_.Exception.ToString() }
finally {
 $after=@(Source-Snapshot); $after | Export-Csv (Join-Path $output 'source-after.csv') -NoTypeInformation
 $manifest['SourceUnchanged']=(@(Compare-Object $before $after -Property Path,Hash).Count -eq 0)
 foreach($item in $manifest.Created) {
  $cleanup=$null
  try {
   $cleanup=Open-TestConnection 'master'; $name=$item.Database
   if($name -notmatch '^CoffeeManager_Test_G04_\d{8}_\d{6}_[a-f0-9]{8}_(clean|demo)$'){throw 'Cleanup name invalid'}
   $cleanup.ChangeDatabase($name)
   if((Read-TestScalar $cleanup 'SELECT RunId FROM dbo.G04TestMarker') -cne $runId){throw 'Ownership marker mismatch'}
   $cleanup.ChangeDatabase('master')
   Run-TestSql $cleanup "ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$name];"
   $manifest.Cleaned+=$name
  } catch { $manifest.Retained+=@{Database=$item.Database;Reason=$_.Exception.Message} }
  finally { if($null -ne $cleanup){$cleanup.Dispose()} }
 }
 if(-not $manifest.SourceUnchanged -or $manifest.Retained.Count -gt 0){$manifest.Status='Failed'}
 Save-Manifest
 Write-Output "Technical evidence: $output; status=$($manifest.Status); created=$($manifest.Created.Count); cleaned=$($manifest.Cleaned.Count); retained=$($manifest.Retained.Count)"
}
if($manifest.Status -ne 'Succeeded'){throw "Technical smoke failed: $output/manifest.json"}

