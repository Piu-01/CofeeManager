[CmdletBinding()]param([ValidateRange(4,9)][int]$ThroughVersion=4,[switch]$Acceptance,[switch]$ApprovedAcceptance)
$ErrorActionPreference='Stop'
if ($Acceptance -and -not $ApprovedAcceptance) { throw 'Acceptance requires the separately approved G03 test batch.' }
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$kind=if($Acceptance){"acceptance"}else{"technical"}
$output=Join-Path $root "docs/tasks/g03-du-lieu-phuc-vu-ban-hang/runs/${kind}_$runId"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$manifest=[ordered]@{ RunId=$runId; Kind=$(if($Acceptance){'Approved acceptance; partial automation plus manual cases'}else{'Technical compile/dependency smoke only'}); Server='localhost'; Operator=[Security.Principal.WindowsIdentity]::GetCurrent().Name; ThroughVersion=$ThroughVersion; Created=@(); Cleaned=@(); Retained=@(); Status='Running' }
$manifest['SqlFailed']=0
$files=@('001_contract_and_code_generation.sql','002_auth_and_bootstrap.sql','003_reference_catalogs.sql','004_inventory_materials.sql','005_products_recipes.sql','006_tables_areas.sql','007_customers.sql','008_promotions_loyalty.sql','009_promotion_legacy_codes.sql')
$manifest['Checksums']=@($files[0..($ThroughVersion-1)] | ForEach-Object { $path=Join-Path $root "database/migrations/$_"; @{ File=$_; SHA256=(Get-FileHash $path -Algorithm SHA256).Hash } })
function Save-Manifest { $manifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $output 'manifest.json') -Encoding utf8 }
Save-Manifest
function Source-Snapshot {
 Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'database'),(Join-Path $root 'tests') -Recurse -File |
 Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs','.sql','.csproj' } |
 Sort-Object FullName | ForEach-Object { [pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash $_.FullName -Algorithm SHA256).Hash} }
}
$sourceBefore=@(Source-Snapshot);$sourceBefore | Export-Csv (Join-Path $output 'source-before.csv') -NoTypeInformation
try {
 foreach($mode in @('clean','demo')) {
  $name="CoffeeManager_Test_G03_${runId}_$mode"
  $master=Open-TestConnection 'master'
  try {
   if((Read-TestScalar $master "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name'") -ne 0) { throw 'Target exists' }
   Run-TestSql $master "CREATE DATABASE [$name];"
   $manifest.Created+=@{ Database=$name; Marker=$runId; Mode=$mode }; Save-Manifest
   $master.ChangeDatabase($name)
   Run-TestSql $master "CREATE TABLE dbo.G03TestMarker(RunId varchar(40) PRIMARY KEY); INSERT dbo.G03TestMarker VALUES('$runId');"
   $schema=[IO.File]::ReadAllText((Join-Path $root 'database/DB_Schema.sql'))
   $schema=[regex]::Replace($schema,'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$','')
   Run-TestSql $master $schema
   if($mode -eq 'demo') {
    $seed=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/Insert_Data.sql')),'(?im)^USE QuanLyCafe_ERP_V2;\s*$','')
    Run-TestSql $master $seed
   }
   $before=[string](Read-TestScalar $master 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')
   for($v=1;$v -le $ThroughVersion;$v++) {
    & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $name -Version $v -File (Join-Path $root ('database/migrations/'+$files[$v-1])) | ConvertTo-Json -Compress | Add-Content (Join-Path $output "$mode-migrations.jsonl")
   }
   if($before -cne [string](Read-TestScalar $master 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')) { throw 'Migration changed inventory balance' }
   if($mode -eq 'demo') {
    $smoke=Join-Path $root "database/tests/g03/technical-v$ThroughVersion.sql"
        if(-not $Acceptance -and (Test-Path $smoke)) { Run-TestSql $master ([IO.File]::ReadAllText($smoke)) }
    if($Acceptance) {
     foreach($task in @('t09','t10','t11','t12','t13')) {
      $fixture=Join-Path $root "database/tests/$task/acceptance-contract.sql"
      $result=[ordered]@{ Task=$task; Batch='SQL contract subset (not full ID acceptance)'; Expected='All SQL assertions complete; transaction clean'; Status='NOT RUN'; Fixture=$fixture }
      try { Run-TestSql $master ([IO.File]::ReadAllText($fixture)); $result.Status='PASS'; $result.Actual='Assertions completed' }
      catch { $result.Status='FAIL'; $result.Actual=$_.Exception.Message; $manifest.SqlFailed++ }
      $result | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $output "$task-sql.json") -Encoding utf8
     }
    }
   }
   $manifest["${mode}Compile"]='Succeeded'; Save-Manifest
  } finally { $master.Dispose() }
 }
 if($Acceptance) {
  $unitDirectory=Join-Path $output 'unit'
  New-Item -ItemType Directory -Path $unitDirectory -Force | Out-Null
  $manifest['Unit']=[ordered]@{ Status='Running'; ExitCode=$null; Filter='FullyQualifiedName~CafeManager.Tests.G03|FullyQualifiedName~MaterialRulesTests'; Trx='unit/g03-unit.trx'; Log='unit/dotnet-test.log' }
  Save-Manifest
  & dotnet test (Join-Path $root 'tests/CafeManager.Tests/CafeManager.Tests.csproj') --filter $manifest.Unit.Filter --logger 'trx;LogFileName=g03-unit.trx' --results-directory $unitDirectory *> (Join-Path $unitDirectory 'dotnet-test.log')
  $manifest.Unit.ExitCode=$LASTEXITCODE
  $trxPath=Join-Path $unitDirectory 'g03-unit.trx'
  if($LASTEXITCODE -eq 0 -and (Test-Path $trxPath)) {
   [xml]$trx=Get-Content $trxPath -Raw
   $counters=$trx.TestRun.ResultSummary.Counters
   $manifest.Unit['Total']=[int]$counters.total
   $manifest.Unit['Passed']=[int]$counters.passed
   $manifest.Unit['Failed']=[int]$counters.failed
   $manifest.Unit.Status=if([int]$counters.total -gt 0 -and [int]$counters.passed -eq [int]$counters.total){'PASS'}else{'FAIL'}
  } else { $manifest.Unit.Status='FAIL' }
 }
 $manifest.Status=if($manifest.SqlFailed -gt 0 -or ($Acceptance -and $manifest.Unit.Status -ne 'PASS')){'Failed'}else{'Succeeded'}
} catch { $manifest.Status='Failed'; $manifest['Error']=$_.Exception.Message }
finally {
 $sourceAfter=@(Source-Snapshot);$sourceAfter | Export-Csv (Join-Path $output 'source-after.csv') -NoTypeInformation
 $manifest['SourceUnchanged']=(@(Compare-Object $sourceBefore $sourceAfter -Property Path,Hash).Count -eq 0)
 foreach($item in $manifest.Created) {
  $cleanup=$null
  try {
   $cleanup=Open-TestConnection 'master'
   $name=$item.Database
   if($name -notmatch '^CoffeeManager_Test_G03_\d{8}_\d{6}_[a-f0-9]{8}_(clean|demo)$') { throw 'Cleanup name invalid' }
   $cleanup.ChangeDatabase($name)
   if((Read-TestScalar $cleanup 'SELECT RunId FROM dbo.G03TestMarker') -cne $runId) { throw 'Cleanup marker mismatch' }
   $cleanup.ChangeDatabase('master')
   Run-TestSql $cleanup "ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$name];"
   $manifest.Cleaned+=$name
  } catch { $manifest.Retained+=@{ Database=$item.Database; Reason=$_.Exception.Message } }
  finally { if($null -ne $cleanup){$cleanup.Dispose()} }
 }
 if(-not $manifest.SourceUnchanged -or $manifest.Retained.Count -gt 0) { $manifest.Status='Failed' }
 Save-Manifest
 Write-Output "Technical evidence: $output; status=$($manifest.Status); created=$($manifest.Created.Count); cleaned=$($manifest.Cleaned.Count); retained=$($manifest.Retained.Count)"
}
if($manifest.Status -ne 'Succeeded') { throw "G03 runner failed; inspect $output/manifest.json and logs." }




