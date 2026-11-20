$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager';. "$root/tools/database/DatabaseTestTools.ps1"
$out=Join-Path $root 'docs/tasks/g03-du-lieu-phuc-vu-ban-hang/runs/acceptance-extended-20261018'
$m=Get-Content "$out/manifest.json" -Raw|ConvertFrom-Json;$db=$m.Database
$results=[Collections.Generic.List[object]]::new()
function Connection { $b=[System.Data.SqlClient.SqlConnectionStringBuilder]::new("Server=localhost;Initial Catalog=$db;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False");$c=[System.Data.SqlClient.SqlConnection]::new($b.ConnectionString);$c.Open();return $c }
function Prefix {return "DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID();SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;"}
function Save { $results|ConvertTo-Json -Depth 6|Set-Content "$out/customer-race-results.json" -Encoding utf8 }
function Two([string]$Id,[string]$First,[string]$Second,[int]$ExpectedError,[string]$AssertSql){
 $one=Connection;$two=Connection;$monitor=Connection;$r=[ordered]@{Id=$Id;Expected="Second request blocks; then error $ExpectedError; final SQL assertions hold";Status='NOT RUN';Actual=$null;Wait=$null}
 try{
  ((Prefix)+$First)|Set-Content "$out/customer-$Id-first.sql" -Encoding utf8
  ((Prefix)+$Second)|Set-Content "$out/customer-$Id-second.sql" -Encoding utf8
  $sid1=Read-TestScalar $one 'SELECT @@SPID';$sid2=Read-TestScalar $two 'SELECT @@SPID'
  Run-TestSql $one ((Prefix)+$First)
  $command=$two.CreateCommand();$command.CommandText=(Prefix)+$Second;$command.CommandTimeout=20
  $task=$command.ExecuteNonQueryAsync();$wait=$null
  for($attempt=0;$attempt -lt 20;$attempt++){
   $wait=Read-TestScalar $monitor "SELECT CONCAT(wait_type,':',blocking_session_id) FROM sys.dm_exec_requests WHERE session_id=$sid2 AND blocking_session_id=$sid1"
   if($wait){break};if($task.IsCompleted){break};[Threading.Thread]::Sleep(100)
  }
  $r.Wait=[string]$wait;Run-TestSql $one 'COMMIT;'
  $observed=0
  try{[void]$task.GetAwaiter().GetResult()}catch{$e=$_.Exception;while($e -and $e -isnot [System.Data.SqlClient.SqlException]){$e=$e.InnerException};if($e){$observed=$e.Number}else{throw}}
  if($observed -ne $ExpectedError){throw "Expected SQL $ExpectedError, observed $observed"}
  if(-not $wait){throw 'No blocking barrier observed'}
  Run-TestSql $monitor $AssertSql
  $state=Read-TestScalar $two "DECLARE @t int=@@TRANCOUNT,@x int=XACT_STATE();SELECT CONCAT(@t,':',@x)"
  if($state -ne '0:0'){throw "Transaction state $state"}
  $r.Status='PASS';$r.Actual="Blocked at $wait; SQL $observed; assertions and transaction 0:0"
 }catch{$r.Status='FAIL';$r.Actual=$_.Exception.Message;try{Run-TestSql $one 'IF @@TRANCOUNT>0 ROLLBACK;'}catch{}}
 finally{if($command){$command.Dispose()};$one.Dispose();$two.Dispose();$monitor.Dispose();$results.Add($r);Save}
}


