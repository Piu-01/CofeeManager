#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$Database,
    [string]$Server = 'localhost',
    [string]$File = (Join-Path $PSScriptRoot '..\..\database\migrations\001_contract_and_code_generation.sql'),
    [ValidateRange(1,2147483647)][int]$Version = 1
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
if ($Database -in @('master','model','msdb','tempdb') -or [string]::IsNullOrWhiteSpace($Database)) { throw 'Explicit user database required.' }
$filePath = (Resolve-Path -LiteralPath $File).Path
$checksum = (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash
$sql = [IO.File]::ReadAllText($filePath,[Text.Encoding]::UTF8)
if ($sql -match '(?im)^\s*(USE\s|CREATE\s+DATABASE\s)') { throw 'Migration cannot change the target database.' }
$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new()
$builder['Data Source']=$Server; $builder['Initial Catalog']=$Database
$builder['Integrated Security']=$true; $builder['Encrypt']=$true
$builder['TrustServerCertificate']=($Server -eq 'localhost'); $builder['Connect Timeout']=10
$connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
$transaction = $null
function Migration-Command([string]$Text) {
    $cmd=$connection.CreateCommand(); $cmd.Transaction=$transaction; $cmd.CommandText=$Text; $cmd.CommandTimeout=60
    return $cmd
}
try {
    $connection.Open()
    if ($connection.Database -cne $Database) { throw 'Unexpected database target.' }
    $transaction=$connection.BeginTransaction()
    $cmd=Migration-Command "DECLARE @r INT; EXEC @r=sys.sp_getapplock @Resource=N'CoffeeManager.SchemaMigration',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; SELECT @r;"
    try { if ([int]$cmd.ExecuteScalar() -lt 0) { throw 'Cannot acquire migration lock.' } } finally { $cmd.Dispose() }
    $cmd=Migration-Command "SELECT CASE WHEN OBJECT_ID(N'dbo.SchemaMigrations',N'U') IS NULL THEN 0 ELSE 1 END;"
    try { $exists=[int]$cmd.ExecuteScalar() } finally { $cmd.Dispose() }
    if ($exists -eq 1) {
        $cmd=Migration-Command 'SELECT Checksum FROM dbo.SchemaMigrations WHERE Version=@version;'
        [void]$cmd.Parameters.Add('@version',[Data.SqlDbType]::Int); $cmd.Parameters['@version'].Value=$Version
        try { $previous=$cmd.ExecuteScalar() } finally { $cmd.Dispose() }
        if ($null -ne $previous) {
            if ([string]$previous -cne $checksum) { throw 'Migration version exists with a different checksum; create a new version.' }
            $transaction.Commit()
            return [pscustomobject]@{Version=$Version;Checksum=$checksum;Status='AlreadyApplied';Database=$Database}
        }
    }
    foreach ($batch in [regex]::Split($sql,'(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?$')) {
        if ([string]::IsNullOrWhiteSpace($batch)) { continue }
        $cmd=Migration-Command $batch
        try { [void]$cmd.ExecuteNonQuery() } finally { $cmd.Dispose() }
    }
    $cmd=Migration-Command 'INSERT dbo.SchemaMigrations(Version,Checksum) VALUES(@version,@checksum);'
    [void]$cmd.Parameters.Add('@version',[Data.SqlDbType]::Int); $cmd.Parameters['@version'].Value=$Version
    [void]$cmd.Parameters.Add('@checksum',[Data.SqlDbType]::Char,64); $cmd.Parameters['@checksum'].Value=$checksum
    try { [void]$cmd.ExecuteNonQuery() } finally { $cmd.Dispose() }
    $transaction.Commit()
    [pscustomobject]@{Version=$Version;Checksum=$checksum;Status='Applied';Database=$Database}
} catch {
    if ($null -ne $transaction -and $null -ne $transaction.Connection) { $transaction.Rollback() }
    throw
} finally {
    if ($null -ne $transaction) { $transaction.Dispose() }
    $connection.Dispose()
}
