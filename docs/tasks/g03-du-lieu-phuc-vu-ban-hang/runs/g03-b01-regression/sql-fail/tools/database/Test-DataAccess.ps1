#requires -Version 5.1
[CmdletBinding()]param()
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
. (Join-Path $PSScriptRoot 'DatabaseTestTools.ps1')
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$directory=Join-Path $root 'docs\tasks\g01-nen-tang-ky-thuat-du-lieu\t05-dal-nen-kiem-thu'
$history=Join-Path $directory "runs\$runId"; [void][IO.Directory]::CreateDirectory($history)
$previous=$env:CAFEMANAGER_TEST_CONNECTION; $database=$null; $setupError=$null; $exitCode=$null; $started=(Get-Date).ToString('o')
function Save-Progress([string]$Stage) {
    [IO.File]::WriteAllText((Join-Path $history 'progress.json'),([ordered]@{RunId=$runId;Database=$database;Stage=$Stage;UpdatedAt=(Get-Date).ToString('o')} | ConvertTo-Json),[Text.UTF8Encoding]::new($false))
    Write-Host $Stage
}
try {
    $database=New-IsolatedDatabase 'T05' $runId $true $false {param($name) $script:database=$name; Save-Progress "SETUP $name"}
    Save-Progress 'MIGRATION'
    [void](& (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $database)
    $connection=Open-TestConnection $database
    try {
        Assert-TestTarget $connection 'T05'
        Run-TestSql $connection ([IO.File]::ReadAllText((Join-Path $root 'database\tests\t05\fixtures.sql'),[Text.Encoding]::UTF8))
    } finally { $connection.Dispose() }
    $env:CAFEMANAGER_TEST_CONNECTION="Server=localhost;Initial Catalog=$database;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
    Save-Progress 'XUNIT'
    $project=Join-Path $root 'tests\CafeManager.Tests\CafeManager.Tests.csproj'
    $process=Start-Process dotnet -ArgumentList @('test',"`"$project`"",'--filter','Group!=G02','--logger','trx;LogFileName=tests.trx','--results-directory',"`"$history`"") -WorkingDirectory $root -RedirectStandardOutput (Join-Path $history 'console.log') -RedirectStandardError (Join-Path $history 'stderr.txt') -Wait -PassThru
    $exitCode=$process.ExitCode
    Get-Content (Join-Path $history 'console.log')
    Get-Content (Join-Path $history 'stderr.txt')
    Save-Progress "COMPLETED exit=$exitCode"
} catch { $setupError=$_.Exception.Message } finally {
    $env:CAFEMANAGER_TEST_CONNECTION=$previous
    $results=@()
    $trx=Join-Path $history 'tests.trx'
    if(Test-Path $trx) {
        [xml]$xml=Get-Content -LiteralPath $trx -Raw -Encoding UTF8
        $results=@($xml.SelectNodes('//*[local-name()="UnitTestResult"]') | ForEach-Object {
            [ordered]@{Name=$_.testName;Outcome=$_.outcome;Duration=$_.duration;Error=$_.Output.ErrorInfo.Message}
        })
    }
    $sourceHashes=@(Get-ChildItem (Join-Path $root 'src'),(Join-Path $root 'tests'),(Join-Path $root 'database\tests\t05') -File -Recurse | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object { [ordered]@{Path=$_.FullName.Substring($root.Length+1);Hash=(Get-FileHash $_.FullName).Hash} })
    $report=[ordered]@{RunId=$runId;StartedAt=$started;CompletedAt=(Get-Date).ToString('o');Database=$database;SetupError=$setupError;DotnetExitCode=$exitCode;Results=$results;SourceCommit='uncommitted';SourceHashes=$sourceHashes;ManualCases=@('T05-K07','T05-K08','T05-K09','T05-K10');SchemaHash=(Get-FileHash (Join-Path $root 'database\DB_Schema.sql')).Hash;SeedHash=(Get-FileHash (Join-Path $root 'database\Insert_Data.sql')).Hash;MigrationHash=(Get-FileHash (Join-Path $root 'database\migrations\001_contract_and_code_generation.sql')).Hash}
    $json=$report | ConvertTo-Json -Depth 8
    [IO.File]::WriteAllText((Join-Path $history 'results.json'),$json,[Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $directory 'test-results.json'),$json,[Text.UTF8Encoding]::new($false))
    $md=@('# T05 - test results',"Run: $runId; DB: $database; dotnet exit: $exitCode",'| Test | Outcome | Actual |','|---|---|---|')
    foreach($result in $results){$actual=([string]$result.Error).Replace('|','/').Replace("`n",' ');$md+="| $($result.Name) | $($result.Outcome) | $actual |"}
    $md+='Manual UI/Designer/DPI/log/guard evidence: NOT RUN until recorded separately. Automated assertions do not substitute for WinForms manual acceptance.'
    if($setupError){$md+="Setup blocker: $setupError"}
    [IO.File]::WriteAllText((Join-Path $directory 'test-results.md'),($md -join "`n"),[Text.UTF8Encoding]::new($false))
}
if($setupError -or $exitCode -ne 0){exit 1}
