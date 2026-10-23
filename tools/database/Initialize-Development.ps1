# Development provisioning only. Never targets an existing unmarked database.
[CmdletBinding()]param()
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$database = 'CoffeeManager_Development'
Add-Type -AssemblyName System.Data
$connection = [System.Data.SqlClient.SqlConnection]::new('Server=localhost;Initial Catalog=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=10')
function Execute([string]$sql) {
    $command = $connection.CreateCommand()
    $command.CommandText = $sql; $command.CommandTimeout = 90
    try { return $command.ExecuteScalar() }
    catch { Write-Host ("Setup SQL stage: " + ($sql.Trim() -split "`n")[0]); throw }
    finally { $command.Dispose() }
}
try {
    $connection.Open()
    $exists = Execute "SELECT COUNT(*) FROM sys.databases WHERE name=N'$database';"
    if ($exists -eq 0) {
        [void](Execute "CREATE DATABASE [$database];")
        $connection.ChangeDatabase($database)
        [void](Execute "CREATE TABLE dbo.DevelopmentMarker(Id int PRIMARY KEY CHECK(Id=1),Application varchar(60) NOT NULL,SchemaReady bit NOT NULL); INSERT dbo.DevelopmentMarker VALUES(1,'CoffeeManager.Development.v1',0);")
    }
    else {
        $connection.ChangeDatabase($database)
        if ((Execute "SELECT CASE WHEN OBJECT_ID(N'dbo.DevelopmentMarker',N'U') IS NULL THEN 0 ELSE 1 END;") -ne 1) { throw 'Existing database is not owned by development setup. Refusing changes.' }
    }
    if ((Execute "SELECT COUNT(*) FROM dbo.DevelopmentMarker WHERE Id=1 AND Application='CoffeeManager.Development.v1';") -ne 1) { throw 'Development marker mismatch.' }
    if ((Execute 'SELECT SchemaReady FROM dbo.DevelopmentMarker WHERE Id=1;') -eq 0) {
        $schema = [IO.File]::ReadAllText((Join-Path $projectRoot 'database/DB_Schema.sql'))
        $seed = [IO.File]::ReadAllText((Join-Path $projectRoot 'database/Insert_Data.sql'))
        if ([regex]::Matches($schema,'(?im)^CREATE DATABASE QuanLyCafe_ERP_V2;\s*$').Count -ne 1 -or [regex]::Matches($seed,'(?im)^USE QuanLyCafe_ERP_V2;\s*$').Count -ne 1) { throw 'Unexpected baseline target statements.' }
        $schema = [regex]::Replace($schema,'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$','')
        $seed = [regex]::Replace($seed,'(?im)^USE QuanLyCafe_ERP_V2;\s*$','')
        # DDL phải commit trước khi seed gọi procedure dùng TVP vừa tạo.
        # Giữ transaction dữ liệu do seed sở hữu, không bọc thêm transaction DDL.
        $tables = Execute 'SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0;'
        if ($tables -eq 1) {
            foreach ($batch in [regex]::Split($schema,'(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?$')) {
                if (-not [string]::IsNullOrWhiteSpace($batch)) { [void](Execute $batch) }
            }
        }
        if ((Execute "SELECT CASE WHEN OBJECT_ID(N'dbo.TR_KK_LichSu',N'TR') IS NOT NULL AND (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0)=36 THEN 1 ELSE 0 END;") -ne 1) { throw 'Partial development schema requires review; no reset performed.' }
        if ((Execute 'SELECT COUNT(*) FROM dbo.NHANVIEN;') -ne 0) { throw 'Unmarked seed data requires review; no reset performed.' }
        if ([regex]::Matches($seed,'(?im)^COMMIT;\s*$').Count -ne 1) { throw 'Unexpected seed transaction.' }
        $seed = [regex]::Replace($seed,'(?im)^COMMIT;\s*$',"UPDATE dbo.DevelopmentMarker SET SchemaReady=1 WHERE Id=1;`nCOMMIT;")
        foreach ($batch in [regex]::Split($seed,'(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?$')) {
            if (-not [string]::IsNullOrWhiteSpace($batch)) { [void](Execute $batch) }
        }
    }
}
finally { $connection.Dispose() }
& (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $database
& (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $database -Version 2 -File (Join-Path $projectRoot 'database/migrations/002_auth_and_bootstrap.sql')
& (Join-Path $PSScriptRoot 'Apply-Migration.ps1') -Database $database -Version 3 -File (Join-Path $projectRoot 'database/migrations/003_reference_catalogs.sql')
& (Join-Path $PSScriptRoot 'Update-DevelopmentG03.ps1')
& dotnet build (Join-Path $projectRoot 'tools/CafeManager.DevelopmentSetup') --no-restore -m:1 -p:UseSharedCompilation=false -o (Join-Path $projectRoot 'artifacts/development-setup') -v minimal
if ($LASTEXITCODE -ne 0) { throw 'Development tool build failed.' }
& dotnet (Join-Path $projectRoot 'artifacts/development-setup/CafeManager.DevelopmentSetup.dll') --initialize
if ($LASTEXITCODE -ne 0) { throw 'Development account provisioning did not complete. No reset was performed.' }

