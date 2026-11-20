# Helpers chỉ dành cho operator; dot-source không mở kết nối hoặc thay đổi DB.
Add-Type -AssemblyName System.Data
function Open-OperationConnection([string]$Server,[string]$Database) {
    $b=[System.Data.SqlClient.SqlConnectionStringBuilder]::new()
    $b['Data Source']=$Server;$b['Initial Catalog']=$Database;$b['Integrated Security']=$true
    $b['Encrypt']=$true;$b['TrustServerCertificate']=($Server -eq 'localhost');$b['Connect Timeout']=10
    $c=[System.Data.SqlClient.SqlConnection]::new($b.ConnectionString)
    try{$c.Open();return $c}catch{$c.Dispose();throw}
}
function Assert-OperationDatabase([string]$Database) {
    if($Database -notmatch '^[A-Za-z][A-Za-z0-9_]{0,127}$' -or $Database -in @('master','model','msdb','tempdb')){throw 'Cần tên user database tường minh gồm chữ, số hoặc dấu gạch dưới.'}
}
function Invoke-OperationTable($Connection,[string]$Sql,[hashtable]$Parameters=@{}) {
    $cmd=$Connection.CreateCommand();$cmd.CommandText=$Sql;$cmd.CommandTimeout=180
    foreach($key in $Parameters.Keys){[void]$cmd.Parameters.AddWithValue($key,$Parameters[$key])}
    try{$table=[Data.DataTable]::new();$reader=$cmd.ExecuteReader();try{$table.Load($reader)}finally{$reader.Dispose()};return ,$table}finally{$cmd.Dispose()}
}
function Invoke-OperationSql($Connection,[string]$Sql,[hashtable]$Parameters=@{}) {
    $cmd=$Connection.CreateCommand();$cmd.CommandText=$Sql;$cmd.CommandTimeout=180
    foreach($key in $Parameters.Keys){[void]$cmd.Parameters.AddWithValue($key,$Parameters[$key])}
    try{[void]$cmd.ExecuteNonQuery()}finally{$cmd.Dispose()}
}
function Invoke-OperationBatches($Connection,[string]$Sql) {
    foreach($batch in [regex]::Split($Sql,'(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?$')){if(-not [string]::IsNullOrWhiteSpace($batch)){Invoke-OperationSql $Connection $batch}}
}
function Assert-NewServerFile($Connection,[string]$Path) {
    if($Path -notmatch '^(?:[A-Za-z]:[\\/]|\\\\)' -or $Path -match "[\r\n]" -or $Path.Contains('..')){throw 'Cần đường dẫn tuyệt đối phía SQL Server, không có .. hoặc xuống dòng.'}
    $result=Invoke-OperationTable $Connection 'EXEC master.dbo.xp_fileexist @path;' @{'@path'=$Path}
    if($result.Rows.Count -ne 1 -or [int]$result.Rows[0][0] -ne 0 -or [int]$result.Rows[0][2] -ne 1){throw 'File đã tồn tại hoặc thư mục server chưa tồn tại/không truy cập được.'}
}
function Get-OperationSnapshot($Connection) {
    # Chỉ xuất hash theo PK, không xuất password hash hoặc dữ liệu cá nhân.
    $tables=Invoke-OperationTable $Connection "SELECT t.name FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name='dbo' AND t.is_ms_shipped=0 AND t.name NOT IN('SchemaMigrations','OperationInstallMarker','G08TestMarker') ORDER BY t.name;"
    $results=@()
    foreach($row in $tables.Rows){
        $name=[string]$row.name
        $keys=Invoke-OperationTable $Connection "SELECT STRING_AGG(CONVERT(nvarchar(max),QUOTENAME(c.name)),N',') WITHIN GROUP(ORDER BY ic.key_ordinal) KeysList FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE i.object_id=OBJECT_ID(@name) AND i.is_primary_key=1;" @{'@name'="dbo.$name"}
        $order=[string]$keys.Rows[0].KeysList
        if([string]::IsNullOrWhiteSpace($order)){throw "Không có PK cho snapshot: $name"}
        $quoted=$name.Replace(']',']]')
        $hash=Invoke-OperationTable $Connection "SELECT COUNT_BIG(*) [RowCount],CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT * FROM dbo.[$quoted] ORDER BY $order FOR JSON PATH,INCLUDE_NULL_VALUES))),2) [Hash] FROM dbo.[$quoted];"
        $results+= [pscustomobject]@{Table=$name;Rows=[long]$hash.Rows[0].RowCount;Hash=[string]$hash.Rows[0].Hash}
    }
    return $results
}
