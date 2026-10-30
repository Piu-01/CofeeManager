[CmdletBinding()]param([switch]$KeepForGui)
$ThroughVersion=21
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$output=Join-Path $root "docs/tasks/g05-kho-nguon-chi-cong-no/runs/acceptance_$runId"
New-Item -ItemType Directory -Path $output -Force | Out-Null
$manifest=[ordered]@{RunId=$runId;Kind='Acceptance SQL subset; full55ID/native/parallel not implied';Server='localhost';ThroughVersion=$ThroughVersion;Operator=[Security.Principal.WindowsIdentity]::GetCurrent().Name;Created=@();Cleaned=@();Retained=@();Status='Running'}
function Save-Manifest { $manifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $output 'manifest.json') -Encoding utf8 }
function Source-Snapshot {
 Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'database'),(Join-Path $root 'tests') -Recurse -File |
 Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs','.sql','.csproj' } |
 Sort-Object FullName | ForEach-Object { [pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash $_.FullName -Algorithm SHA256).Hash} }
}
$before=@(Source-Snapshot); $before | Export-Csv (Join-Path $output 'source-before.csv') -NoTypeInformation
Save-Manifest
try {
 & dotnet test (Join-Path $root 'tests/CafeManager.Tests') -c Debug --no-restore --filter 'FullyQualifiedName~CafeManager.Tests.Finance|FullyQualifiedName~CafeManager.Tests.Inventory' --logger 'trx;LogFileName=g05-unit.trx' --results-directory (Join-Path $output 'unit') *> (Join-Path $output 'unit.log')
 $manifest['UnitExitCode']=$LASTEXITCODE
 foreach($mode in @('clean','demo')) {
  $name="CoffeeManager_Test_G05_${runId}_$mode"
  $connection=Open-TestConnection 'master'
  try {
   if((Read-TestScalar $connection "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name'") -ne 0){throw 'Target already exists'}
   Run-TestSql $connection "CREATE DATABASE [$name];"
   $manifest.Created+=@{Database=$name;Marker=$runId;Mode=$mode}; Save-Manifest
   $connection.ChangeDatabase($name)
   Run-TestSql $connection "CREATE TABLE dbo.G05TestMarker(RunId varchar(40) PRIMARY KEY); INSERT dbo.G05TestMarker VALUES('$runId');"
   $schema=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/DB_Schema.sql')),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$','')
   Run-TestSql $connection $schema
   if($mode -eq 'demo') {
    $seed=[regex]::Replace([IO.File]::ReadAllText((Join-Path $root 'database/Insert_Data.sql')),'(?im)^USE QuanLyCafe_ERP_V2;\s*$','')
    Run-TestSql $connection $seed
   }
   $balances=[string](Read-TestScalar $connection 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')
   for($version=1;$version -le $ThroughVersion;$version++) {
    $migration=@(Get-ChildItem (Join-Path $root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $version))
    if($migration.Count -ne 1){throw "Missing/ambiguous migration $version"}
    & (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $name -Version $version -File $migration[0].FullName | ConvertTo-Json -Compress | Add-Content (Join-Path $output "$mode-migrations.jsonl")
   }
   if($balances -cne [string](Read-TestScalar $connection 'SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)')){throw 'Migration changed inventory'}
   if($mode -eq 'demo'){
    foreach($file in @('fixture.sql','t19-finance.sql','t20-purchases.sql','t21-issues.sql','t22-stocktakes.sql','t23-debts.sql')){
     Run-TestSql $connection ([IO.File]::ReadAllText((Join-Path $root "database/tests/g05/$file")))
    }
    $raw=[string](Read-TestScalar $connection 'SELECT (SELECT * FROM dbo.G05AcceptanceLog ORDER BY RecordedAt,Part FOR JSON PATH)')
    Set-Content (Join-Path $output 'sql-subcases.json') $raw -Encoding utf8
    $manifest['SqlFailed']=[int](Read-TestScalar $connection "SELECT COUNT(*) FROM dbo.G05AcceptanceLog WHERE Result='FAIL'")
    $manifest['SqlScope']='Only suffix.SQL subcases. Runtime permissions, concurrency, faults and native55ID need separate evidence.'
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
   if($KeepForGui -and $item.Mode -eq 'demo'){
    $manifest.Retained+=@{Database=$item.Database;RunId=$runId;Reason='Explicit KeepForGui';ExpiresUtc=[DateTime]::UtcNow.AddHours(6).ToString('o');Cleanup='Verify exact database name and G05TestMarker.RunId, then close own connections and DROP only this database.'};continue
   }
   $cleanup=Open-TestConnection 'master'; $name=$item.Database
   if($name -notmatch '^CoffeeManager_Test_G05_\d{8}_\d{6}_[a-f0-9]{8}_(clean|demo)$'){throw 'Cleanup name invalid'}
   $cleanup.ChangeDatabase($name)
   if((Read-TestScalar $cleanup 'SELECT RunId FROM dbo.G05TestMarker') -cne $runId){throw 'Ownership marker mismatch'}
   $cleanup.ChangeDatabase('master')
   Run-TestSql $cleanup "ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$name];"
   $manifest.Cleaned+=$name
  } catch { $manifest.Retained+=@{Database=$item.Database;Reason=$_.Exception.Message} }
  finally { if($null -ne $cleanup){$cleanup.Dispose()} }
 }
 if(-not $manifest.SourceUnchanged -or ($manifest.Retained.Count -gt 0 -and -not $KeepForGui) -or $manifest.UnitExitCode -ne 0 -or $manifest.SqlFailed -ne 0){$manifest.Status='Failed'}
 Save-Manifest
 Write-Output "Acceptance subset evidence: $output; status=$($manifest.Status); created=$($manifest.Created.Count); cleaned=$($manifest.Cleaned.Count); retained=$($manifest.Retained.Count)"
}
if($manifest.Status -ne 'Succeeded'){throw "Acceptance subset failed: $output/manifest.json"}




