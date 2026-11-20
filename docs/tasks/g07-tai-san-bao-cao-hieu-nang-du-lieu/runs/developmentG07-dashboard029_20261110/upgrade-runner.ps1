param([string]$Root='D:/Workspace/Nam_3/HK1/CoffeeManager')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Data
$out=$PSScriptRoot
if(Get-Process -Name CafeManager.WinForms -ErrorAction SilentlyContinue){throw 'Close the development app first.'}
$db='CoffeeManager_Development';$server='localhost'
$manifest=[ordered]@{Server=$server;Database=$db;Status='Preflight';BeforeVersion=$null;AfterVersion=$null;Backup=$null;DataUnchanged=$null;SchemaTablesColumnsUnchanged=$null;FixtureWritten=$false;AccountReset=$false}
function SaveManifest { $manifest|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $out 'upgrade-manifest.json') -Encoding utf8 }
function NewConn {
  $b=[System.Data.SqlClient.SqlConnectionStringBuilder]::new()
  $b['Data Source']=$server;$b['Initial Catalog']=$db;$b['Integrated Security']=$true;$b['Encrypt']=$true;$b['TrustServerCertificate']=$true;$b['Connect Timeout']=10
  $c=[System.Data.SqlClient.SqlConnection]::new($b.ConnectionString);$c.Open();return $c
}
function Table($c,[string]$sql,[hashtable]$p=@{}) {
  $cmd=$c.CreateCommand();$cmd.CommandText=$sql;$cmd.CommandTimeout=180
  foreach($k in $p.Keys){[void]$cmd.Parameters.AddWithValue($k,$p[$k])}
  try{$t=[System.Data.DataTable]::new();$r=$cmd.ExecuteReader();try{$t.Load($r)}finally{$r.Dispose()};return ,$t}finally{$cmd.Dispose()}
}
function Exec($c,[string]$sql,[hashtable]$p=@{}) {
  $cmd=$c.CreateCommand();$cmd.CommandText=$sql;$cmd.CommandTimeout=180
  foreach($k in $p.Keys){[void]$cmd.Parameters.AddWithValue($k,$p[$k])}
  try{[void]$cmd.ExecuteNonQuery()}finally{$cmd.Dispose()}
}
function Snapshot($c) {
  $tables=Table $c "SELECT t.name FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name='dbo' AND t.is_ms_shipped=0 AND t.name NOT IN('SchemaMigrations','OperationInstallMarker','G08TestMarker') ORDER BY t.name;"
  $res=@()
  foreach($row in $tables.Rows){
    $name=[string]$row.name
    $k=Table $c "SELECT STRING_AGG(CONVERT(nvarchar(max),QUOTENAME(c.name)),N',') WITHIN GROUP(ORDER BY ic.key_ordinal) KeysList FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE i.object_id=OBJECT_ID(@name) AND i.is_primary_key=1;" @{'@name'="dbo.$name"}
    $order=[string]$k.Rows[0].KeysList
    if([string]::IsNullOrWhiteSpace($order)){throw "No primary key for snapshot: $name"}
    $q=$name.Replace(']',']]')
    $h=Table $c "SELECT COUNT_BIG(*) [RowCount],CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT * FROM dbo.[$q] ORDER BY $order FOR JSON PATH,INCLUDE_NULL_VALUES))),2) [Hash] FROM dbo.[$q];"
    $res+=[pscustomobject]@{Table=$name;Rows=[long]$h.Rows[0].RowCount;Hash=[string]$h.Rows[0].Hash}
  }
  return $res
}
function SchemaJson($c) {
  (Table $c "SELECT (SELECT t.name TableName,c.column_id,c.name ColumnName,TYPE_NAME(c.user_type_id) TypeName,c.max_length,c.precision,c.scale,c.is_nullable,c.is_identity,c.is_computed,cc.definition FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id LEFT JOIN sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id WHERE t.is_ms_shipped=0 ORDER BY t.name,c.column_id FOR JSON PATH,INCLUDE_NULL_VALUES) SchemaJson").Rows[0].SchemaJson
}
function ProcJson($c) {
  (Table $c "SELECT (SELECT p.name,OBJECT_DEFINITION(p.object_id) Body FROM sys.procedures p WHERE p.name IN ('sp_DashboardAnalyticsRead','sp_DashboardAnalyticsSources') ORDER BY p.name FOR JSON PATH,INCLUDE_NULL_VALUES) ProcJson").Rows[0].ProcJson
}
function ApplyMigration($c,[int]$version,[string]$file) {
  if($file -notmatch '^(?:[A-Za-z]:[\\/]|\\\\)' -or $file.Contains('..')){throw 'Absolute migration path required.'}
  $checksum=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash
  $sql=[IO.File]::ReadAllText($file,[Text.Encoding]::UTF8)
  if($sql -match '(?im)^\s*(USE\s|CREATE\s+DATABASE\s)'){throw 'Migration cannot switch database.'}
  $tx=$c.BeginTransaction()
  try{
    $cmd=$c.CreateCommand();$cmd.Transaction=$tx
    $cmd.CommandText="DECLARE @r INT; EXEC @r=sys.sp_getapplock @Resource=N'CoffeeManager.SchemaMigration',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; SELECT @r;"
    try{ if([int]$cmd.ExecuteScalar() -lt 0){throw 'Cannot acquire migration lock.'} }finally{$cmd.Dispose()}
    $q=$c.CreateCommand();$q.Transaction=$tx;$q.CommandText='SELECT Checksum FROM dbo.SchemaMigrations WHERE Version=@version;'
    [void]$q.Parameters.AddWithValue('@version',$version)
    try{$prev=$q.ExecuteScalar()}finally{$q.Dispose()}
    if($null -ne $prev){
      if([string]$prev -cne $checksum){throw "Version $version exists with different checksum."}
      $tx.Commit();return [ordered]@{Version=$version;Checksum=$checksum;Status='AlreadyApplied'}
    }
    foreach($batch in [regex]::Split($sql,'(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?$')){
      if([string]::IsNullOrWhiteSpace($batch)){continue}
      $cmd=$c.CreateCommand();$cmd.Transaction=$tx;$cmd.CommandText=$batch;$cmd.CommandTimeout=180
      try{[void]$cmd.ExecuteNonQuery()}finally{$cmd.Dispose()}
    }
    $cmd=$c.CreateCommand();$cmd.Transaction=$tx
    $cmd.CommandText='INSERT dbo.SchemaMigrations(Version,Checksum) VALUES(@version,@checksum);'
    [void]$cmd.Parameters.Add('@version',[System.Data.SqlDbType]::Int);$cmd.Parameters['@version'].Value=$version
    [void]$cmd.Parameters.Add('@checksum',[System.Data.SqlDbType]::Char,64);$cmd.Parameters['@checksum'].Value=$checksum
    try{[void]$cmd.ExecuteNonQuery()}finally{$cmd.Dispose()}
    $tx.Commit();return [ordered]@{Version=$version;Checksum=$checksum;Status='Applied'}
  }catch{if($tx.Connection){$tx.Rollback()};throw}finally{$tx.Dispose()}
}
$c=$null;SaveManifest
try{
  $c=NewConn
  $m=Table $c "SELECT COUNT(*) N FROM dbo.DevelopmentMarker WHERE Id=1 AND Application='CoffeeManager.Development.v1' AND SchemaReady=1"
  if([int]$m.Rows[0].N -ne 1){throw 'Development marker mismatch.'}
  $v=Table $c 'SELECT MAX(Version) V FROM dbo.SchemaMigrations'
  if([int]$v.Rows[0].V -ne 27){throw 'Expected development schema027.'}
  $manifest.BeforeVersion=27
  for($ver=1;$ver -le 27;$ver++){
    $f=@(Get-ChildItem (Join-Path $Root 'database/migrations') -Filter ('{0:d3}_*.sql' -f $ver))
    if($f.Count -ne 1){throw "Migration file missing: $ver"}
    $stored=Table $c 'SELECT Checksum FROM dbo.SchemaMigrations WHERE Version=@v' @{'@v'=$ver}
    if($stored.Rows.Count -ne 1 -or [string]$stored.Rows[0].Checksum -cne (Get-FileHash $f[0].FullName -Algorithm SHA256).Hash){throw "Checksum mismatch: $ver"}
  }
  $manifest['ChecksumsVerified']=@(1..27)
  $beforeSnap=@(Snapshot $c)
  $beforeSchema=SchemaJson $c
  $beforeSchema|Set-Content -LiteralPath (Join-Path $out 'schema-before.json') -Encoding utf8
  $backupDir=[string](Table $c "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath')) Path").Rows[0].Path
  $backup=[IO.Path]::Combine($backupDir,('CoffeeManager_Development_before_dashboard029_'+(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)+'.bak'))
  if($backup -notmatch '^(?:[A-Za-z]:[\\/]|\\\\)' -or $backup.Contains('..')){throw 'Bad backup path.'}
  $fe=Table $c 'EXEC master.dbo.xp_fileexist @path;' @{'@path'=$backup}
  if([int]$fe.Rows[0][0] -ne 0 -or [int]$fe.Rows[0][2] -ne 1){throw 'Backup file already exists or folder unavailable.'}
  $manifest.Backup=$backup;SaveManifest
  $snapA=@(Snapshot $c)
  Exec $c 'BACKUP DATABASE [CoffeeManager_Development] TO DISK=@path WITH COPY_ONLY,CHECKSUM;' @{'@path'=$backup}
  Exec $c 'RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM,FILE=1;' @{'@path'=$backup}
  $h=Table $c 'RESTORE HEADERONLY FROM DISK=@path;' @{'@path'=$backup}
  if($h.Rows.Count -ne 1 -or [string]$h.Rows[0].DatabaseName -cne $db -or -not [bool]$h.Rows[0].IsCopyOnly -or -not [bool]$h.Rows[0].HasBackupChecksums){throw 'Backup metadata mismatch.'}
  $manifest['BackupSetGUID']=[string]$h.Rows[0].BackupSetGUID
  $manifest['BackupVerified']=$true
  $snapB=@(Snapshot $c)
  if(($snapA|ConvertTo-Json -Depth 5 -Compress) -cne ($snapB|ConvertTo-Json -Depth 5 -Compress)){throw 'Source changed during backup.'}
  $manifest['SourceStable']=$true
  $m28=ApplyMigration $c 28 (Join-Path $Root 'database/migrations/028_dashboard_analytics_read.sql');$m28|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $out '028.json') -Encoding utf8
  $m29=ApplyMigration $c 29 (Join-Path $Root 'database/migrations/029_dashboard_source_lock_order.sql');$m29|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $out '029.json') -Encoding utf8
  $manifest['Migrations']=@($m28,$m29)
  $afterSnap=@(Snapshot $c)
  $afterSchema=SchemaJson $c
  $afterSchema|Set-Content -LiteralPath (Join-Path $out 'schema-after.json') -Encoding utf8
  ProcJson $c|Set-Content -LiteralPath (Join-Path $out 'procedures-after.json') -Encoding utf8
  $manifest.DataUnchanged=(($beforeSnap|ConvertTo-Json -Depth 5 -Compress) -ceq ($afterSnap|ConvertTo-Json -Depth 5 -Compress))
  $manifest.SchemaTablesColumnsUnchanged=($beforeSchema -ceq $afterSchema)
  if(-not $manifest.DataUnchanged -or -not $manifest.SchemaTablesColumnsUnchanged){throw 'Parity failed; keep backup, no automatic restore.'}
  $nv=[int](Table $c 'SELECT MAX(Version) V FROM dbo.SchemaMigrations').Rows[0].V
  $manifest.AfterVersion=$nv
  $proc=Table $c "SELECT COUNT(*) N FROM sys.procedures WHERE name IN ('sp_DashboardAnalyticsRead','sp_DashboardAnalyticsSources')"
  $manifest['ProcedureCount']=[int]$proc.Rows[0].N
  $grant=Table $c "SELECT COUNT(*) N FROM sys.database_permissions p WHERE p.permission_name='EXECUTE' AND p.grantee_principal_id=DATABASE_PRINCIPAL_ID('CafeManagerApp') AND p.major_id IN (OBJECT_ID('dbo.sp_DashboardAnalyticsRead'),OBJECT_ID('dbo.sp_DashboardAnalyticsSources'))"
  $manifest['ExecuteGrants']=[int]$grant.Rows[0].N
  if($nv -ne 29 -or $manifest.ProcedureCount -ne 2 -or $manifest.ExecuteGrants -ne 2){throw 'Post-upgrade verification failed.'}
  $manifest.Status='Succeeded'
}catch{$manifest.Status='Failed';$manifest['Error']=$_.Exception.Message;throw}
finally{if($c){$c.Dispose()};SaveManifest}
Write-Output ('STATUS='+$manifest.Status)
