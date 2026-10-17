#requires -Version 5.1
[CmdletBinding()]param([ValidateSet('T06','T08','All')][string]$Task='All',[switch]$AcceptanceApproved)
$ErrorActionPreference='Stop'
if(-not $AcceptanceApproved){throw 'Run only after approval of the G02 acceptance round; pass -AcceptanceApproved.'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$history=Join-Path $root "docs\tasks\g02-truy-cap-ung-dung-cau-hinh-nen\runs\$runId"
[void][IO.Directory]::CreateDirectory($history)
$names=@('CAFEMANAGER_G02_CONNECTION','CAFEMANAGER_G02_RACE_CONNECTION','CAFEMANAGER_G02_ROLLBACK_CONNECTION')
$previousRoot=$env:CAFEMANAGER_G02_ROOT
$previous=@{};foreach($name in $names){$previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
$databases=@();$setupError=$null;$exitCode=$null;$results=@();$started=(Get-Date).ToString('o')
function Source-Manifest {
 @(Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'tests'),(Join-Path $root 'database'),(Join-Path $root 'tools') -File -Recurse |
 Where-Object {$_.FullName -notmatch '\\(bin|obj)\\'} | Sort-Object FullName | ForEach-Object {
 [ordered]@{Path=$_.FullName.Substring($root.Length+1);Hash=(Get-FileHash $_.FullName).Hash}})
}
$before=Source-Manifest
try {
 $env:CAFEMANAGER_G02_ROOT=$root
 foreach($name in $names){
  $fixtureRun=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
  $db=New-IsolatedDatabase 'G02' $fixtureRun $false;$databases+=$db
  [void](& (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $db)
  [void](& (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $db -Version 2 -File (Join-Path $root 'database\migrations\002_auth_and_bootstrap.sql'))
  [void](& (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $db -Version 3 -File (Join-Path $root 'database\migrations\003_reference_catalogs.sql'))
  $c=Open-TestConnection $db
  try {
   Assert-TestTarget $c 'G02'
   Run-TestSql $c ([IO.File]::ReadAllText((Join-Path $root 'database\tests\g02\security-fixtures.sql'),[Text.Encoding]::UTF8))
   if($name -like '*ROLLBACK*'){Run-TestSql $c ([IO.File]::ReadAllText((Join-Path $root 'database\tests\t06\rollback-fixture.sql'),[Text.Encoding]::UTF8))}
  }finally{$c.Dispose()}
  [Environment]::SetEnvironmentVariable($name,"Server=localhost;Initial Catalog=$db;Integrated Security=True;Encrypt=True;TrustServerCertificate=True",'Process')
 }
 $filter='Group=G02';if($Task -ne 'All'){$filter+="&Task=$Task"}
 $project=Join-Path $root 'tests\CafeManager.Tests\CafeManager.Tests.csproj'
 $process=Start-Process dotnet -ArgumentList @('test',"`"$project`"",'--filter',"`"$filter`"",'--logger','trx;LogFileName=tests.trx','--results-directory',"`"$history`"") -WorkingDirectory $root -RedirectStandardOutput (Join-Path $history 'stdout.txt') -RedirectStandardError (Join-Path $history 'stderr.txt') -Wait -PassThru
 $exitCode=$process.ExitCode
 Get-Content (Join-Path $history 'stdout.txt');Get-Content (Join-Path $history 'stderr.txt')
}catch{$setupError=$_.Exception.Message}finally{
 $env:CAFEMANAGER_G02_ROOT=$previousRoot
 foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$previous[$name],'Process')}
 $trx=Join-Path $history 'tests.trx'
 if(Test-Path $trx){
  [xml]$xml=Get-Content $trx -Raw -Encoding UTF8
  $results=@($xml.SelectNodes('//*[local-name()="UnitTestResult"]')|ForEach-Object{[ordered]@{Name=$_.testName;Outcome=$_.outcome;Duration=$_.duration;Error=$_.Output.ErrorInfo.Message}})
 }
 $after=Source-Manifest
 $unchanged=($before|ConvertTo-Json -Depth 5 -Compress) -ceq ($after|ConvertTo-Json -Depth 5 -Compress)
 $report=[ordered]@{RunId=$runId;Task=$Task;StartedAt=$started;CompletedAt=(Get-Date).ToString('o');Databases=$databases;SetupError=$setupError;DotnetExitCode=$exitCode;Results=$results;SourceUnchanged=$unchanged;SourceBefore=$before;SourceAfter=$after;SourceCommit='uncommitted';ManualStatus='NOT RUN; see complete mandatory subcases in approved task plans'}
 [IO.File]::WriteAllText((Join-Path $history 'results.json'),($report|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
 $md=@('# G02 — Automated run evidence',"Run: $runId; task: $Task; exit: $exitCode; source unchanged: $unchanged",'','| Test | Outcome | Actual |','|---|---|---|')
 foreach($result in $results){$md+="| $($result.Name.Replace('|','/')) | $($result.Outcome) | $(([string]$result.Error).Replace('|','/').Replace("`n",' ')) |"}
 if($setupError){$md+="SetupError: $setupError"}
 $md+='Automated evidence only; no aggregate T06/T07/T08 PASS. Record every remaining mandatory manual/SQL subcase separately at its owning task. Test DBs retained for inspection; no automatic source edits or cleanup.'
 [IO.File]::WriteAllText((Join-Path $history 'automated-results.md'),($md -join "`n"),[Text.UTF8Encoding]::new($false))
 Write-Output "Evidence: $history"
}
if($setupError -or $exitCode -ne 0 -or -not $unchanged -or $results.Count -eq 0){exit 1}
