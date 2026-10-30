$ErrorActionPreference='Stop'
$root='D:\Workspace\Nam_3\HK1\CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$out=$PSScriptRoot;$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8);$db="CoffeeManager_Test_G04_${runId}_demo"
$m=[ordered]@{RunId=$runId;Database=$db;Created=@();Cleaned=@();Retained=@();Cases=@();Status='Running';Schema=15;Kind='Independent remaining scenarios and observations; prior FAIL retained'}
function Save {$m|ConvertTo-Json -Depth 15|Set-Content "$out/supplement-results.json" -Encoding utf8}
function Check([string]$id,[string]$sql){$r=[ordered]@{Id=$id;Sql=$sql;Status='NOT RUN'};try{Run-TestSql $c $sql;$r.Status='PASS'}catch{$r.Status='FAIL';$r['Error']=$_.Exception.ToString()};if($id -like 'T17 fixture*'){$script:dateFixtureReady=$r.Status-eq'PASS'};$m.Cases+=$r;Save}
function Read([string]$id,[string]$sql,[int[]]$counts){
 if($id -like 'T17-K01*' -and -not $script:dateFixtureReady){$m.Cases+=@{Id=$id;Status='BLOCKED';Reason='Date/page fixture failed; no dependent assertion run'};Save;return}
 $r=[ordered]@{Id=$id;Sql=$sql;ExpectedRowCounts=$counts;Tables=@();Status='NOT RUN'};$cmd=$c.CreateCommand();$cmd.CommandText=$sql;$cmd.CommandTimeout=30;$adapter=[System.Data.SqlClient.SqlDataAdapter]::new($cmd);$set=[System.Data.DataSet]::new()
 try{[void]$adapter.Fill($set);$actual=@();foreach($table in $set.Tables){$actual+=$table.Rows.Count;$rows=@();foreach($row in $table.Rows){$v=[ordered]@{};foreach($col in $table.Columns){$value=$row[$col];$v[$col.ColumnName]=if($value-is[byte[]]){[Convert]::ToHexString($value)}elseif($value-is[DBNull]){$null}else{$value}};$rows+=$v};$r.Tables+=@{Rows=$rows;Count=$table.Rows.Count}};$r['ActualRowCounts']=$actual;$r.Status=if($counts.Count-eq0-or($actual-join',')-eq($counts-join',')){'PASS'}else{'FAIL'}}catch{$r.Status='FAIL';$r['Error']=$_.Exception.ToString()}finally{$adapter.Dispose();$cmd.Dispose();$set.Dispose()};$m.Cases+=$r;Save
}
$c=$null;Save
try{
 $c=Open-TestConnection 'master';if((Read-TestScalar $c "SELECT COUNT(*) FROM sys.databases WHERE name=N'$db'")-ne0){throw 'Exists'};Run-TestSql $c "CREATE DATABASE [$db]";$m.Created+=$db;Save;$c.ChangeDatabase($db)
 Run-TestSql $c "CREATE TABLE dbo.G04TestMarker(RunId varchar(40) PRIMARY KEY); INSERT dbo.G04TestMarker VALUES('$runId');"
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText("$root/database/DB_Schema.sql"),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$',''));Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText("$root/database/Insert_Data.sql"),'(?im)^USE QuanLyCafe_ERP_V2;\s*$',''))
 $legacyBefore=Snapshot-TestRows $c
 for($v=1;$v-le15;$v++){$f=@(Get-ChildItem "$root/database/migrations" -Filter ('{0:d3}_*.sql'-f$v));& "$root/tools/database/Apply-Migration.ps1" -Database $db -Version $v -File $f[0].FullName|ConvertTo-Json -Compress|Add-Content "$out/supplement-migrations.jsonl"}
 Run-TestSql $c ([IO.File]::ReadAllText("$out/helpers.sql"))
 foreach($part in [regex]::Split([IO.File]::ReadAllText("$out/supplement.sql"),'(?m)^-- CASE ')){if($part.Trim()){$lines=$part-split'\r?\n',2;Check $lines[0].Trim() $lines[1]}}
 $m.Status='Completed'
}catch{$m.Status='Aborted';$m['Error']=$_.Exception.ToString()}
finally{if($c){$c.Dispose()};if($m.Created.Count){$d=$null;try{$d=Open-TestConnection $db;if((Read-TestScalar $d 'SELECT RunId FROM dbo.G04TestMarker')-cne$runId-or$db-notmatch'^CoffeeManager_Test_G04_\d{8}_\d{6}_[a-f0-9]{8}_demo$'){throw 'Ownership mismatch'};$d.ChangeDatabase('master');Run-TestSql $d "ALTER DATABASE [$db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$db]";$m.Cleaned+=$db}catch{$m.Retained+=@{Database=$db;Reason=$_.Exception.Message}}finally{if($d){$d.Dispose()}}};Save;Write-Output "Remaining scenarios: $($m.Status), cases=$($m.Cases.Count), cleaned=$($m.Cleaned.Count), retained=$($m.Retained.Count)"}


