# Shared only by T03/T05 tools; importing does not connect to SQL.
Add-Type -AssemblyName System.Data
function Open-TestConnection([string]$Database) {
    $b=[System.Data.SqlClient.SqlConnectionStringBuilder]::new()
    $b['Data Source']='localhost'; $b['Initial Catalog']=$Database
    $b['Integrated Security']=$true; $b['Encrypt']=$true; $b['TrustServerCertificate']=$true
    $b['Connect Timeout']=10
    $c=[System.Data.SqlClient.SqlConnection]::new($b.ConnectionString); $c.Open(); return $c
}
function Assert-TestTarget($Connection,[string]$Task) {
    $db=$Connection.Database
    if ($db -notmatch ('^CoffeeManager_Test_'+$Task+'_\d{8}_\d{6}_[a-f0-9]{8}(?:_empty)?$')) { throw 'Refusing non-test database.' }
    if ((Read-TestScalar $Connection 'SELECT DB_NAME();') -cne $db) { throw 'Test target changed.' }
}
function Read-TestScalar($Connection,[string]$Sql) {
    $cmd=$Connection.CreateCommand(); $cmd.CommandText=$Sql; $cmd.CommandTimeout=60
    try { return $cmd.ExecuteScalar() } finally { $cmd.Dispose() }
}
function Run-TestSql($Connection,[string]$Sql) {
    foreach ($batch in [regex]::Split($Sql,'(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?$')) {
        if ([string]::IsNullOrWhiteSpace($batch)) { continue }
        $cmd=$Connection.CreateCommand(); $cmd.CommandText=$batch; $cmd.CommandTimeout=60
        try { [void]$cmd.ExecuteNonQuery() } finally { $cmd.Dispose() }
    }
}
function New-IsolatedDatabase([string]$Task,[string]$RunId,[bool]$IncludeSeed=$true,[bool]$Empty=$false,[scriptblock]$OnCreated=$null) {
    $name="CoffeeManager_Test_${Task}_$RunId"; if ($Empty) { $name+='_empty' }
    $master=Open-TestConnection 'master'
    try {
        if ($name -notmatch '^CoffeeManager_Test_T0[35]_\d{8}_\d{6}_[a-f0-9]{8}(?:_empty)?$') { throw 'Invalid test name.' }
        if ((Read-TestScalar $master "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name';") -ne 0) { throw 'Database already exists.' }
        $schema=[IO.File]::ReadAllText((Join-Path $root 'database\DB_Schema.sql'),[Text.Encoding]::UTF8)
        foreach ($pattern in @('(?im)^CREATE DATABASE QuanLyCafe_ERP_V2;\s*$','(?im)^USE QuanLyCafe_ERP_V2;\s*$')) {
            if ([regex]::Matches($schema,$pattern).Count -ne 1) { throw 'Unexpected baseline target statements.' }
        }
        $schema=[regex]::Replace($schema,'(?im)^CREATE DATABASE QuanLyCafe_ERP_V2;\s*$',"CREATE DATABASE [$name];")
        $schema=[regex]::Replace($schema,'(?im)^USE QuanLyCafe_ERP_V2;\s*$',"USE [$name];")
        if ($null -ne $OnCreated) { & $OnCreated $name }
        Run-TestSql $master $schema
        $master.ChangeDatabase($name); Assert-TestTarget $master $Task
        if ($IncludeSeed) {
            $seedSql=[IO.File]::ReadAllText((Join-Path $root 'database\Insert_Data.sql'),[Text.Encoding]::UTF8)
            if ([regex]::Matches($seedSql,'(?im)^USE QuanLyCafe_ERP_V2;\s*$').Count -ne 1) { throw 'Unexpected seed USE.' }
            Run-TestSql $master ([regex]::Replace($seedSql,'(?im)^USE QuanLyCafe_ERP_V2;\s*$',"USE [$name];"))
        }
        return $name
    } finally { $master.Dispose() }
}
function Snapshot-TestRows($Connection) {
    $tables=(Read-TestScalar $Connection "SELECT (SELECT name FROM sys.tables WHERE is_ms_shipped=0 AND name<>'SchemaMigrations' ORDER BY name FOR JSON PATH);") | ConvertFrom-Json
    $rows=[ordered]@{}
    foreach($table in $tables) {
        $name=$table.name.Replace(']',']]')
        $keys=Read-TestScalar $Connection "SELECT STRING_AGG(CONVERT(NVARCHAR(MAX),QUOTENAME(c.name)),N',') WITHIN GROUP(ORDER BY ic.key_ordinal) FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE i.object_id=OBJECT_ID(N'dbo.[$name]') AND i.is_primary_key=1;"
        if (-not $keys) { throw "No snapshot primary key for $name" }
        $rows[$table.name]=[string](Read-TestScalar $Connection "SELECT (SELECT * FROM dbo.[$name] ORDER BY $keys FOR JSON PATH,INCLUDE_NULL_VALUES);")
    }
    return ($rows | ConvertTo-Json -Compress -Depth 6)
}
function Expect-TestSqlError($Connection,[string]$Sql,[int]$Number) {
    $observed=$false
    try { Run-TestSql $Connection $Sql }
    catch {
        $e=$_.Exception
        while($e -and $e -isnot [System.Data.SqlClient.SqlException]) { $e=$e.InnerException }
        if (-not $e -or $Number -notin @($e.Errors | ForEach-Object Number)) { throw }
        $observed=$true
    }
    if (-not $observed) { throw "Expected SQL error $Number was not observed." }
    $state=Read-TestScalar $Connection 'DECLARE @t INT=@@TRANCOUNT,@s INT=XACT_STATE(); SELECT CONCAT(@t,'':'',@s);'
    if ($state -ne '0:0') { throw "Transaction state $state" }
}
