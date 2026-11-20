# Read-only audit. No test setup, DDL, account changes or cleanup.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
$connection = [System.Data.SqlClient.SqlConnection]::new('Server=localhost;Initial Catalog=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=8')
function Read-Table([string]$query) {
    $command = $connection.CreateCommand()
    $command.CommandText = $query; $command.CommandTimeout = 15
    $table = [System.Data.DataTable]::new()
    try { $reader = $command.ExecuteReader(); try { $table.Load($reader) } finally { $reader.Dispose() } }
    finally { $command.Dispose() }
    return ,$table
}
$rows = @()
try {
    $connection.Open()
    $databases = Read-Table @'
SELECT d.name,d.create_date,CONVERT(decimal(12,1),SUM(CONVERT(bigint,f.size))*8.0/1024) AS AllocatedMB
FROM sys.databases d JOIN sys.master_files f ON f.database_id=d.database_id
WHERE d.name LIKE 'CoffeeManager[_]Test[_]%'
GROUP BY d.name,d.create_date ORDER BY d.create_date;
'@
    foreach ($database in $databases.Rows) {
        $record = [ordered]@{ Database=[string]$database.name; Created=$database.create_date.ToString('s'); AllocatedMB=$database.AllocatedMB; G02MarkerPresent=$false; MarkerMatchesName=$false; CafeWindowsUsers=0; G02TemporaryUsers=0; Error='' }
        try {
            $connection.ChangeDatabase($record.Database)
            $meta = Read-Table "SELECT CASE WHEN OBJECT_ID(N'dbo.G02TestMarker',N'U') IS NULL THEN 0 ELSE 1 END AS MarkerPresent;"
            $record.G02MarkerPresent = [bool]$meta.Rows[0].MarkerPresent
            if ($record.G02MarkerPresent) {
                $marker = Read-Table 'SELECT RunId FROM dbo.G02TestMarker;'
                $record.MarkerMatchesName = $marker.Rows.Count -eq 1 -and $record.Database -ceq ('CoffeeManager_Test_G02_' + [string]$marker.Rows[0].RunId)
            }
            $users = Read-Table @'
SELECT SUM(CASE WHEN name LIKE '%CafeManagerApp%' THEN 1 ELSE 0 END) AS CafeUsers,
       SUM(CASE WHEN name LIKE '%CMG02%' THEN 1 ELSE 0 END) AS G02Users
FROM sys.database_principals WHERE type IN ('U','G');
'@
            if ($users.Rows[0].CafeUsers -isnot [DBNull]) { $record.CafeWindowsUsers = [int]$users.Rows[0].CafeUsers }
            if ($users.Rows[0].G02Users -isnot [DBNull]) { $record.G02TemporaryUsers = [int]$users.Rows[0].G02Users }
        }
        catch { $record.Error = $_.Exception.GetBaseException().Message }
        $rows += [pscustomobject]$record
    }
}
finally { $connection.Dispose() }
$rows | Export-Csv (Join-Path $PSScriptRoot 'database-inventory.csv') -NoTypeInformation -Encoding utf8
[pscustomobject]@{ Count=$rows.Count; AllocatedMB=($rows | Measure-Object AllocatedMB -Sum).Sum; G02Markers=@($rows | Where-Object G02MarkerPresent).Count; MatchingMarkers=@($rows | Where-Object MarkerMatchesName).Count; CafeWindowsUserMappings=($rows | Measure-Object CafeWindowsUsers -Sum).Sum; TemporaryG02Mappings=($rows | Measure-Object G02TemporaryUsers -Sum).Sum; Errors=@($rows | Where-Object Error).Count } | ConvertTo-Json | Set-Content (Join-Path $PSScriptRoot 'database-summary.json') -Encoding utf8
Get-Content (Join-Path $PSScriptRoot 'database-summary.json')
