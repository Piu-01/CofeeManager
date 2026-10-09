#requires -Version 5.1
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$testDirectory = Join-Path $root 'database\tests\t02'
$reportDirectory = Join-Path $root 'docs\tasks\g01-nen-tang-ky-thuat-du-lieu\t02-sql-baseline'
$runId = (Get-Date -Format 'yyyyMMdd_HHmmss') + '_' + [guid]::NewGuid().ToString('N').Substring(0, 8)
$results = [Collections.Generic.List[object]]::new()
$connections = [Collections.Generic.List[System.Data.SqlClient.SqlConnection]]::new()
$tableOrders = @{}
$utf8 = [Text.UTF8Encoding]::new($false)

function Open-Connection([string]$Database) {
    $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new()
    $builder['Data Source'] = 'localhost'
    $builder['Initial Catalog'] = $Database
    $builder['Integrated Security'] = $true
    $builder['Encrypt'] = $true
    $builder['TrustServerCertificate'] = $true
    $builder['Connect Timeout'] = 10
    $builder['Application Name'] = 'CoffeeManager.T02.BaselineTest'
    $connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
    $connection.Open()
    $connections.Add($connection)
    return $connection
}

function Invoke-Scalar($Connection, [string]$Sql) {
    $command = $Connection.CreateCommand()
    try {
        $command.CommandText = $Sql
        $command.CommandTimeout = 60
        return $command.ExecuteScalar()
    } finally { $command.Dispose() }
}

function Invoke-Batches($Connection, [string]$Sql) {
    $batches = [regex]::Split($Sql, '(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?$')
    $batchIndex = 0
    foreach ($batch in $batches) {
        $batchIndex++
        if ([string]::IsNullOrWhiteSpace($batch)) { continue }
        $command = $Connection.CreateCommand()
        try {
            $command.CommandText = $batch
            $command.CommandTimeout = 60
            [void]$command.ExecuteNonQuery()
        } catch {
            $_.Exception.Data['BatchIndex'] = $batchIndex
            throw
        } finally { $command.Dispose() }
    }
}

function Assert-TestDatabase($Connection, [string]$Expected) {
    if ($Expected -notmatch '^CoffeeManager_Test_T02_(baseline|fixture)_\d{8}_\d{6}_[a-f0-9]{8}$') {
        throw "Invalid test database name: $Expected"
    }
    if ((Invoke-Scalar $Connection 'SELECT DB_NAME();') -cne $Expected) {
        throw 'Connection is not targeting the expected T02 test database'
    }
}

function Convert-Baseline([string]$Sql, [string]$Database, [bool]$Schema) {
    $usePattern = '(?im)^USE QuanLyCafe_ERP_V2;\s*$'
    $createPattern = '(?im)^CREATE DATABASE QuanLyCafe_ERP_V2;\s*$'
    if ([regex]::Matches($Sql, $usePattern).Count -ne 1) { throw 'Expected one baseline USE statement' }
    if ($Schema -and [regex]::Matches($Sql, $createPattern).Count -ne 1) {
        throw 'Expected one schema CREATE DATABASE statement'
    }
    $converted = [regex]::Replace($Sql, $usePattern, "USE [$Database];")
    if ($Schema) { $converted = [regex]::Replace($converted, $createPattern, "CREATE DATABASE [$Database];") }
    return $converted
}

function Get-Hash([string]$Text) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($utf8.GetBytes($Text))).Replace('-', '') }
    finally { $sha.Dispose() }
}

function Get-Snapshot($Connection) {
    $database = $Connection.Database
    if (-not $tableOrders.ContainsKey($database)) {
        $json = Invoke-Scalar $Connection @'
SELECT (SELECT t.name AS TableName,
    (SELECT STRING_AGG(CONVERT(NVARCHAR(MAX),QUOTENAME(c.name)),N',')
        WITHIN GROUP (ORDER BY ic.key_ordinal)
     FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
     JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
     WHERE i.object_id=t.object_id AND i.is_primary_key=1) AS KeyColumns
FROM sys.tables t WHERE t.is_ms_shipped=0 ORDER BY t.name FOR JSON PATH);
'@
        $tableOrders[$database] = $json | ConvertFrom-Json
    }
    $snapshot = [ordered]@{}
    foreach ($table in $tableOrders[$database]) {
        if (-not $table.KeyColumns) { throw "No primary key for snapshot: $($table.TableName)" }
        $quotedName = $table.TableName.Replace(']', ']]')
        $snapshot[$table.TableName] = [string](Invoke-Scalar $Connection (
            "SELECT (SELECT * FROM dbo.[$quotedName] ORDER BY $($table.KeyColumns) FOR JSON PATH, INCLUDE_NULL_VALUES);"))
    }
    return $snapshot
}

function Get-SqlException($ErrorRecord) {
    $exception = $ErrorRecord.Exception
    while ($null -ne $exception) {
        if ($exception -is [System.Data.SqlClient.SqlException]) { return $exception }
        $exception = $exception.InnerException
    }
    return $null
}

function Invoke-TestCase($Connection, [string]$Database, $Case, [string]$SourceFile) {
    $parts = $Case.Groups['header'].Value.Split('|') | ForEach-Object { $_.Trim() }
    if ($parts.Count -ne 4) { throw "Invalid test header: $($Case.Groups['header'].Value)" }
    $id, $description, $errorText, $messageFragment = $parts
    $expectedError = [int]$errorText
    $sectionPattern = '(?ms)^-- (SETUP|ACT|ASSERT|CLEANUP)\s*\r?\n(.*?)(?=^-- (?:SETUP|ACT|ASSERT|CLEANUP)\s*\r?$|\z)'
    $sections = @{}
    foreach ($section in [regex]::Matches($Case.Groups['body'].Value, $sectionPattern)) {
        $sections[$section.Groups[1].Value] = $section.Groups[2].Value.Trim()
    }
    if (-not $sections.ContainsKey('ACT')) { throw "Missing ACT for $id" }
    $result = [ordered]@{
        Id = $id; Description = $description; Status = 'FAIL'; Database = $Database
        Source = $SourceFile; ExpectedError = $expectedError; ExpectedMessage = $messageFragment
        SetupSql = $sections['SETUP']; ActSql = $sections['ACT']; AssertSql = $sections['ASSERT']
        SqlErrors = @(); TransactionState = $null; ChangedTables = @(); SnapshotBefore = $null
        SnapshotAfter = $null; Failure = $null; DurationMs = 0
    }
    $timer = [Diagnostics.Stopwatch]::StartNew()
    try {
        Assert-TestDatabase $Connection $Database
        if ($sections['SETUP']) { Invoke-Batches $Connection $sections['SETUP'] }
        if ((Invoke-Scalar $Connection 'SELECT @@TRANCOUNT;') -ne 0) { throw 'Fixture left a transaction open' }
        $before = $null
        if ($expectedError -gt 0) { $before = Get-Snapshot $Connection }
        $observedError = $null
        try { Invoke-Batches $Connection $sections['ACT'] }
        catch {
            $observedError = Get-SqlException $_
            if ($null -eq $observedError) { throw }
            $result.SqlErrors = @($observedError.Errors | ForEach-Object {
                [ordered]@{ Number = $_.Number; Message = $_.Message; Procedure = $_.Procedure; Line = $_.LineNumber }
            })
        }
        $state = [string](Invoke-Scalar $Connection "DECLARE @tc INT=@@TRANCOUNT,@xs INT=XACT_STATE(); SELECT CONCAT(@tc,':',@xs);")
        $result.TransactionState = $state
        if ($expectedError -gt 0) {
            $after = Get-Snapshot $Connection
            $result.SnapshotBefore = Get-Hash ($before | ConvertTo-Json -Compress)
            $result.SnapshotAfter = Get-Hash ($after | ConvertTo-Json -Compress)
            $changed = @($before.Keys | Where-Object { $before[$_] -cne $after[$_] })
            $result.ChangedTables = $changed
            if ($changed.Count -gt 0) {
                $result['ChangedRows'] = @($changed | ForEach-Object {
                    [ordered]@{ Table = $_; Before = $before[$_]; After = $after[$_] }
                })
            }
            if ($null -eq $observedError -or $expectedError -notin @($observedError.Errors | ForEach-Object { $_.Number })) {
                throw "Expected SQL error $expectedError was not observed"
            }
            if ($messageFragment -and $observedError.Message -notlike "*$messageFragment*") {
                throw "Expected constraint/message fragment not observed: $messageFragment"
            }
            if ($changed.Count -gt 0) { throw "Failed operation changed tables: $($changed -join ', ')" }
        } elseif ($null -ne $observedError) { throw $observedError }
        if ($state -ne '0:0') { throw "Transaction state is $state, expected 0:0" }
        if ($sections['ASSERT']) { Invoke-Batches $Connection $sections['ASSERT'] }
        $result.Status = 'PASS'
    } catch {
        $result.Failure = $_.Exception.Message
        if ($null -eq $result.TransactionState -and $Connection.State -eq 'Open') {
            $result.TransactionState = [string](Invoke-Scalar $Connection "DECLARE @tc INT=@@TRANCOUNT,@xs INT=XACT_STATE(); SELECT CONCAT(@tc,':',@xs);")
        }
    } finally {
        try {
            if ($Connection.State -eq 'Open') {
                Invoke-Batches $Connection 'IF @@TRANCOUNT > 0 ROLLBACK;'
                if ($sections['CLEANUP']) { Invoke-Batches $Connection $sections['CLEANUP'] }
            }
        } catch { $result.Status = 'FAIL'; $result.Failure = "Cleanup: $($_.Exception.Message)" }
        $result.DurationMs = $timer.ElapsedMilliseconds
        $results.Add([pscustomobject]$result)
        Write-Host "$($result.Status) $id $description"
    }
}

function Invoke-TestFile($Connection, [string]$Database, [string]$File) {
    $text = [IO.File]::ReadAllText((Join-Path $testDirectory $File), [Text.Encoding]::UTF8)
    $text = $text.Replace('#T02_', 'dbo.T02_')
    $cases = [regex]::Matches($text, '(?ms)^-- CASE (?<header>[^\r\n]+)\r?\n(?<body>.*?)(?=^-- CASE |\z)')
    if ($cases.Count -eq 0) { throw "No test cases in $File" }
    foreach ($case in $cases) { Invoke-TestCase $Connection $Database $case $File }
}

$schemaPath = Join-Path $root 'database\DB_Schema.sql'
$seedPath = Join-Path $root 'database\Insert_Data.sql'
$schemaText = [IO.File]::ReadAllText($schemaPath, [Text.Encoding]::UTF8)
$seedText = [IO.File]::ReadAllText($seedPath, [Text.Encoding]::UTF8)
$run = [ordered]@{
    RunId = $runId; StartedAt = (Get-Date).ToString('o'); CompletedAt = $null
    Server = 'localhost'; Engine = $null; Databases = @(); SetupError = $null
    SchemaSHA256 = (Get-FileHash -LiteralPath $schemaPath).Hash
    SeedSHA256 = (Get-FileHash -LiteralPath $seedPath).Hash
    SourceCommit = 'uncommitted'; Cases = @(); Summary = @()
}
try {
    $master = Open-Connection 'master'
    $run.Engine = [string](Invoke-Scalar $master "SELECT CONCAT(CONVERT(NVARCHAR(128),SERVERPROPERTY('ServerName')),' | ',CONVERT(NVARCHAR(128),SERVERPROPERTY('ProductVersion')),' | ',CONVERT(NVARCHAR(128),SERVERPROPERTY('Edition')));")
    foreach ($role in @('baseline', 'fixture')) {
        $database = "CoffeeManager_Test_T02_${role}_$runId"
        if ((Invoke-Scalar $master "SELECT COUNT(*) FROM sys.databases WHERE name=N'$database';") -ne 0) {
            throw "Test database already exists: $database"
        }
        Write-Host "Setup $database"
        $setup = Open-Connection 'master'
        Invoke-Batches $setup (Convert-Baseline $schemaText $database $true)
        # ChangeDatabase synchronizes SqlConnection.Database with the USE done in the script.
        $setup.ChangeDatabase($database)
        Assert-TestDatabase $setup $database
        Invoke-Batches $setup (Convert-Baseline $seedText $database $false)
        $run.Databases += [ordered]@{ Role = $role; Name = $database }
        if ($role -eq 'baseline') {
            $baseline = $setup
            Invoke-TestFile $baseline $database 'catalog-seed.sql'
            $repeat = [regex]::Match("-- CASE T02-K05-repeat | Seed refuses repeat and preserves all rows | 51200 |`n-- ACT`n$(Convert-Baseline $seedText $database $false)", '(?ms)^-- CASE (?<header>[^\r\n]+)\r?\n(?<body>.*)')
            Invoke-TestCase $baseline $database $repeat 'Insert_Data.sql (second invocation)'
        } else {
            $fixture = $setup
            Invoke-Batches $fixture ([IO.File]::ReadAllText((Join-Path $testDirectory 'fixtures.sql'), [Text.Encoding]::UTF8).Replace('#T02_', 'dbo.T02_'))
            foreach ($file in @('constraints.sql', 'batch-triggers.sql', 'procedure-cases.sql')) {
                Invoke-TestFile $fixture $database $file
            }
        }
    }
} catch {
    $run.SetupError = $_.Exception.Message
    $sqlError = Get-SqlException $_
    if ($null -ne $sqlError) { $run['SetupSqlErrors'] = @($sqlError.Errors | ForEach-Object { "$($_.Number): $($_.Message)" }) }
    Write-Host "Setup/runner error: $($run.SetupError)"
} finally {
    foreach ($connection in $connections) { $connection.Dispose() }
    $run.CompletedAt = (Get-Date).ToString('o')
    $run.Cases = @($results.ToArray())
    $summaries = foreach ($number in 1..12) {
        $id = 'T02-K{0:d2}' -f $number
        $related = @($results | Where-Object { $_.Id.StartsWith($id + '-') })
        if ($id -eq 'T02-K12') {
            $related += @($results | Where-Object { $_.Id -match '^T02-K(09|10|11)-' -and $_.ExpectedError -gt 0 })
        }
        $status = if ($related.Count -eq 0) { 'BLOCKED' } elseif (@($related | Where-Object { $_.Status -ne 'PASS' }).Count) { 'FAIL' } else { 'PASS' }
        if ($run.SetupError -and $status -eq 'PASS') { $status = 'BLOCKED' }
        [pscustomobject]@{ Id = $id; Status = $status; Subcases = @($related | ForEach-Object { $_.Id }) }
    }
    $run.Summary = @($summaries)
    $json = $run | ConvertTo-Json -Depth 12
    [IO.Directory]::CreateDirectory($reportDirectory) | Out-Null
    # Keep earlier runs rather than overwriting evidence of failures.
    $history = Join-Path $reportDirectory 'runs'
    [IO.Directory]::CreateDirectory($history) | Out-Null
    [IO.File]::WriteAllText((Join-Path $history "$runId.json"), $json, $utf8)
    [IO.File]::WriteAllText((Join-Path $reportDirectory 'test-results.json'), $json, $utf8)
    $md = [Collections.Generic.List[string]]::new()
    $md.Add('# T02 - SQL baseline test results')
    $md.Add('')
    $md.Add("Run: ``$runId``. Engine: $($run.Engine). Commit: uncommitted.")
    $md.Add("Schema SHA256: ``$($run.SchemaSHA256)``. Seed SHA256: ``$($run.SeedSHA256)``.")
    $md.Add("Started: $($run.StartedAt). Completed: $($run.CompletedAt).")
    $md.Add('Test databases are retained for inspection; existing databases are never reset/dropped.')
    foreach ($db in $run.Databases) { $md.Add("- $($db.Role): ``$($db.Name)``") }
    if ($run.SetupError) { $md.Add("Setup/runner error: $($run.SetupError)") }
    $md.Add(''); $md.Add('| Case | Status | Subcases |'); $md.Add('|---|---|---|')
    foreach ($summary in $summaries) { $md.Add("| $($summary.Id) | $($summary.Status) | $($summary.Subcases.Count) |") }
    $md.Add(''); $md.Add('| Subcase | Description / expected | Status | Actual |'); $md.Add('|---|---|---|---|')
    foreach ($result in $results) {
        $actual = if ($result.Failure) { $result.Failure } else { "SQL errors: $($result.SqlErrors.Number -join ','); transaction $($result.TransactionState); changed tables: $($result.ChangedTables.Count)" }
        $actual = $actual.Replace('|', '/').Replace("`r", ' ').Replace("`n", ' ')
        $md.Add("| $($result.Id) | $($result.Description); expected error $($result.ExpectedError) | $($result.Status) | $actual |")
    }
    $md.Add(''); $md.Add('Setup/act/assert, errors, snapshot hashes and changed rows: test-results.json; prior runs: runs/. Snapshots compare all rows/columns of 35 tables ordered by PK, excluding identity next value. Transaction state is read on the same connection before cleanup.')
    [IO.File]::WriteAllText((Join-Path $reportDirectory 'test-results.md'), ($md -join "`n"), $utf8)
}
if ($run.SetupError -or @($run.Summary | Where-Object { $_.Status -ne 'PASS' }).Count) { exit 1 }
Write-Host 'T02: all 12 acceptance cases PASS.'
