$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$setup=Get-Content (Join-Path $PSScriptRoot 'setup.json') -Raw | ConvertFrom-Json
$demo=$setup.Demo
$result=[ordered]@{Database=$demo;Status='Running';Steps=@()}
function Save{$result|ConvertTo-Json -Depth 8|Set-Content (Join-Path $PSScriptRoot 'parity.json') -Encoding utf8}
function Snapshot($c){(Read-TestScalar $c "SELECT (SELECT t.name TableName,c.column_id ColumnId,c.name ColumnName,TYPE_NAME(c.user_type_id) TypeName,c.max_length MaxLength,c.precision PrecisionValue,c.scale ScaleValue,c.is_nullable IsNullable,c.is_computed IsComputed,cc.definition Definition FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id LEFT JOIN sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id WHERE t.is_ms_shipped=0 ORDER BY t.name,c.column_id FOR JSON PATH,INCLUDE_NULL_VALUES)")+(Read-TestScalar $c "SELECT (SELECT t.name TableName,i.name IndexName,i.type TypeValue,i.is_unique IsUnique,ic.index_column_id ColumnId,c.name ColumnName,ic.is_descending_key IsDescending FROM sys.tables t JOIN sys.indexes i ON i.object_id=t.object_id JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE t.is_ms_shipped=0 ORDER BY t.name,i.index_id,ic.index_column_id FOR JSON PATH,INCLUDE_NULL_VALUES)")}
function Cell($v){if($v -is [DBNull]){return 'NULL'};if($v -is [datetime]){return $v.ToString('o')};if($v -is [decimal]){return $v.ToString([Globalization.CultureInfo]::InvariantCulture)};'['+$v+']'}
function Invoke-Capture($c,[string]$sql,$tr=$null){
 $cmd=$c.CreateCommand();$cmd.CommandText=$sql;$cmd.CommandTimeout=120;if($null -ne $tr){$cmd.Transaction=$tr}
 $sets=@()
 try{$r=$cmd.ExecuteReader();do{$rows=@();$keep=@();for($i=0;$i -lt $r.FieldCount;$i++){$keep+=($r.GetName($i) -ne 'ReadAt')};while($r.Read()){$vals=@();for($i=0;$i -lt $r.FieldCount;$i++){if($keep[$i]){$vals+=Cell $r.GetValue($i)}};$rows+=($vals -join '|')};$sets+=@($rows)}while($r.NextResult());$r.Close()}finally{$cmd.Dispose()}
 return $sets
}
function Canon($sets){return ((@($sets|ForEach-Object{@($_)}) -join "`n"))}
$c=Open-TestConnection $demo
try{
 # Activate seeded admin once (before snapshots) so both captures see identical data.
 Run-TestSql $c "IF (SELECT State FROM dbo.AppSetupState WHERE Id=1)='Pending' DECLARE @s binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=1);IF (SELECT State FROM dbo.AppSetupState WHERE Id=1)='Pending' EXEC dbo.sp_SetupBootstrap @Mode='ActivateDemo',@Username=N'admin',@NewHash='!REGRESSION_ONLY_HASH!',@MaNV='NV001',@ExpectedStamp=@s;IF NOT EXISTS(SELECT 1 FROM sys.database_principals WHERE name='G07Runtime') CREATE USER G07Runtime WITHOUT LOGIN;IF NOT EXISTS(SELECT 1 FROM sys.database_role_members r JOIN sys.database_principals p ON p.principal_id=r.member_principal_id WHERE p.name='G07Runtime') ALTER ROLE CafeManagerApp ADD MEMBER G07Runtime;"
 $schemaBefore=Snapshot $c;$rowsBefore=Snapshot-TestRows $c
 $schemaBefore|Set-Content (Join-Path $PSScriptRoot 'schema-before.json') -Encoding utf8
 $rowsBefore|Set-Content (Join-Path $PSScriptRoot 'data-before.json') -Encoding utf8
 $from='2025-04-01T00:00:00';$until='2026-11-01T00:00:00'
 $pre="DECLARE @a int=1,@s binary(8);SELECT @s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=1;"
 function Call([string]$tail){$pre+"EXEC dbo.sp_DashboardAnalyticsSources @Actor=@a,@Stamp=@s,"+$tail}
 $span="@From='$from',@Until='$until'"
 $cases=[ordered]@{
  'revenue-day'=(Call "@Kind='revenue',$span")
  'revenue-month'=(Call "@Kind='revenue',@From='2025-04-01',@Until='2025-05-02'")
  'invoices'=(Call "@Kind='invoices',$span")
  'average'=(Call "@Kind='average',$span")
  'trend'=(Call "@Kind='trend',$span")
  'channel-tai-quan'=(Call "@Kind='channel',$span,@Channel=N'Tai quan'")
  'channel-mang-di'=(Call "@Kind='channel',$span,@Channel=N'Mang di'")
  'channel-giao-hang'=(Call "@Kind='channel',$span,@Channel=N'Giao hang'")
  'product-X0001'=(Call "@Kind='product',$span,@Product='X0001'")
  'product-absent'=(Call "@Kind='product',$span,@Product='SP999'")
  'orders'=(Call "@Kind='orders'")
  'delivery'=(Call "@Kind='delivery'")
  'material'=(Call "@Kind='material'")
  'debt'=(Call "@Kind='debt'")
  'expense'=(Call "@Kind='expense',$span")
  'revenue-skip12'=(Call "@Kind='revenue',$span,@Skip=12")
  'trend-skip0'=(Call "@Kind='trend',$span,@Skip=0")
  'product-skip12'=(Call "@Kind='product',$span,@Product='X0001',@Skip=12")
  'product-skip50'=(Call "@Kind='product',$span,@Product='X0001',@Skip=50")
 }
 $before=@{};foreach($k in $cases.Keys){$before[$k]=Canon (Invoke-Capture $c $cases[$k])}
 $before|ConvertTo-Json -Depth 4|Set-Content (Join-Path $PSScriptRoot 'sources-before.json') -Encoding utf8
 $errors=@()
 foreach($case in @(
  @{Id='invalid-kind';Sql=(Call "@Kind='bogus',$span");Number=53501},
  @{Id='invalid-range';Sql=(Call "@Kind='revenue',@From='2026-01-02',@Until='2026-01-01'");Number=53501},
  @{Id='product-required';Sql=(Call "@Kind='product',$span");Number=53501},
  @{Id='channel-required';Sql=(Call "@Kind='channel',$span");Number=53501},
  @{Id='product-code-length';Sql=(Call "@Kind='product',$span,@Product='X001'");Number=53501},
  @{Id='inactive-actor';Sql="DECLARE @s binary(8)=0x0000000000000000;EXEC dbo.sp_DashboardAnalyticsSources @Actor=2,@Stamp=@s,@Kind='orders'";Number=51505}
 )){
  $observed=$false
  try{Invoke-Capture $c $case.Sql|Out-Null}
  catch{$e=$_.Exception;while($e -and $e -isnot [System.Data.SqlClient.SqlException]){$e=$e.InnerException}
   if($e -and $case.Number -in @($e.Errors|ForEach-Object Number)){$observed=$true}else{throw}}
  $errors+=@{Id=$case.Id;Expected=$case.Number;Observed=$observed}
 }
 # Outer transaction must be rejected.
 $tr=$c.BeginTransaction();$observed=$false
 try{Invoke-Capture $c (Call "@Kind='revenue',$span") $tr|Out-Null}
 catch{$e=$_.Exception;while($e -and $e -isnot [System.Data.SqlClient.SqlException]){$e=$e.InnerException};if($e -and 53501 -in @($e.Errors|ForEach-Object Number)){$observed=$true}else{throw}}
 finally{$tr.Rollback()}
 $errors+=@{Id='outer-transaction';Expected=53501;Observed=$observed}
 # EXECUTE through role CafeManagerApp only.
 $granted=Read-TestScalar $c "SELECT COUNT(*) FROM sys.database_permissions p WHERE p.major_id=OBJECT_ID(N'dbo.sp_DashboardAnalyticsSources') AND p.state IN('G','W') AND p.grantee_principal_id=USER_ID('CafeManagerApp')"
 $b=[System.Data.SqlClient.SqlConnectionStringBuilder]::new();$b['Data Source']='localhost';$b['Initial Catalog']=$demo;$b['Integrated Security']=$true;$b['Encrypt']=$true;$b['TrustServerCertificate']=$true;$b['Pooling']=$false
 $pc=[System.Data.SqlClient.SqlConnection]::new($b.ConnectionString);$pc.Open()
 try{$permission=Invoke-Capture $pc ($pre+"EXECUTE AS USER='G07Runtime';EXEC dbo.sp_DashboardAnalyticsSources @Actor=@a,@Stamp=@s,"+"@Kind='revenue',$span"+";REVERT;")}
 finally{try{Run-TestSql $pc 'REVERT'}catch{};$pc.Dispose()}
 $result.Steps+=@{Id='DA03-parity-before';Status='Captured';Cases=$cases.Count;Grant=$granted;Errors=$errors;PermissionRows=($permission[1].Count)}
 Save
 $migration=(Resolve-Path "$root/database/migrations/029_dashboard_source_lock_order.sql").Path
 foreach($attempt in 1..2){
  $applied=& "$root/tools/database/Apply-Migration.ps1" -Database $demo -Version 29 -File $migration
  $result.Steps+=@{Id="DA03-apply-029-attempt$attempt";Status=$applied.Status;Checksum=$applied.Checksum};Save
 }
 $schemaAfter=Snapshot $c;$rowsAfter=Snapshot-TestRows $c
 $after=@{};foreach($k in $cases.Keys){$after[$k]=Canon (Invoke-Capture $c $cases[$k])}
 $after|ConvertTo-Json -Depth 4|Set-Content (Join-Path $PSScriptRoot 'sources-after.json') -Encoding utf8
 $diff=@();foreach($k in $cases.Keys){if($before[$k] -cne $after[$k]){$diff+=$k}}
 $result.Steps+=@{Id='DA03-parity-after';Status=if($diff.Count){'FAIL'}else{'PASS'};Different=$diff;SchemaUnchanged=($schemaBefore -ceq $schemaAfter);DataUnchanged=($rowsBefore -ceq $rowsAfter)}
 $grantedAfter=Read-TestScalar $c "SELECT COUNT(*) FROM sys.database_permissions p WHERE p.major_id=OBJECT_ID(N'dbo.sp_DashboardAnalyticsSources') AND p.state IN('G','W') AND p.grantee_principal_id=USER_ID('CafeManagerApp')"
 $result.Steps+=@{Id='DA03-grant';Status=if($grantedAfter -ge 1){'PASS'}else{'FAIL'};Grants=$grantedAfter}
 $errorsAfter=@()
 foreach($case in @(
  @{Id='invalid-kind';Sql=(Call "@Kind='bogus',$span")},
  @{Id='product-required';Sql=(Call "@Kind='product',$span")}
 )){
  $observed=$false;try{Invoke-Capture $c $case.Sql|Out-Null}catch{$e=$_.Exception;while($e -and $e -isnot [System.Data.SqlClient.SqlException]){$e=$e.InnerException};if($e -and 53501 -in @($e.Errors|ForEach-Object Number)){$observed=$true}else{throw}}
  $errorsAfter+=@{Id=$case.Id;Observed=$observed}
 }
 $result.Steps+=@{Id='DA03-contract-errors-after';Status=if(@($errorsAfter|Where-Object{-not $_.Observed}).Count){'FAIL'}else{'PASS'};Cases=$errorsAfter}
 $text=Read-TestScalar $c "SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.sp_DashboardAnalyticsSources'))"
 $headerStatement=$text.Substring(0,$text.IndexOf("IF @Kind='product'",[StringComparison]::Ordinal))
 $result.Steps+=@{Id='DA03-lock-order-source';Status=if($headerStatement -match 'dbo\.CHITIET_HD'){'FAIL'}else{'PASS'};Note='Header materialization statement does not reference the line table; product filter runs in a later statement.'}
 $result.Status=if(@($result.Steps|Where-Object Status -eq 'FAIL').Count){'Failed'}else{'Passed'}
}catch{$result.Status='Failed';$result['Error']=$_.Exception.ToString();throw}
finally{$c.Dispose();Save}
Write-Output "parity: $($result.Status)"
