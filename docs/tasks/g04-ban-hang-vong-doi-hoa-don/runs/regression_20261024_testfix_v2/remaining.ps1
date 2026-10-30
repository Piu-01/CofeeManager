$ErrorActionPreference='Stop'
$root='D:\Workspace\Nam_3\HK1\CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$out=$PSScriptRoot;$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8);$db="CoffeeManager_Test_G04_${runId}_demo"
$m=[ordered]@{RunId=$runId;Database=$db;Created=@();Cleaned=@();Retained=@();Cases=@();Status='Running';Schema=15;Kind='Independent remaining scenarios and observations; prior FAIL retained'}
function Save {$m|ConvertTo-Json -Depth 15|Set-Content "$out/remaining-results.json" -Encoding utf8}
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
 for($v=1;$v-le15;$v++){$f=@(Get-ChildItem "$root/database/migrations" -Filter ('{0:d3}_*.sql'-f$v));& "$root/tools/database/Apply-Migration.ps1" -Database $db -Version $v -File $f[0].FullName|ConvertTo-Json -Compress|Add-Content "$out/remaining-migrations.jsonl"}
 Run-TestSql $c ([IO.File]::ReadAllText("$out/helpers.sql"))
 foreach($part in [regex]::Split([IO.File]::ReadAllText("$out/remaining.sql"),'(?m)^-- CASE ')){if($part.Trim()){$lines=$part-split'\r?\n',2;Check $lines[0].Trim() $lines[1]}}
 $prefix="DECLARE @today date=CONVERT(date,SYSDATETIME()),@yesterday date=DATEADD(day,-1,CONVERT(date,SYSDATETIME())),@tomorrow date=DATEADD(day,1,CONVERT(date,SYSDATETIME())),@a int,@s binary(8); SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; "
 Read 'T17-K01 created full last day versus paid date' ($prefix+"EXEC dbo.sp_InvoiceList @a,@s,'Created',@today,@today,@Search='HD971'; EXEC dbo.sp_InvoiceList @a,@s,'Paid',@today,@today,@Search='HD971';") @(2,1)
 Read 'T17-K01 status channel customer combined filters' ($prefix+"EXEC dbo.sp_InvoiceList @a,@s,'Created',@yesterday,@tomorrow,@Status=N'Da thanh toan',@Channel=N'Mang di',@Customer='KH001',@Search='HD971';") @(1)
 Read 'T17-K01 51 order pagination no duplicate boundary' ($prefix+"EXEC dbo.sp_InvoiceList @a,@s,'Created',@today,@today,@Search='HD98',@Skip=0; EXEC dbo.sp_InvoiceList @a,@s,'Created',@today,@today,@Search='HD98',@Skip=50;") @(50,1)
 Read 'T17-K02 detail stored price and discount after catalog change' ($prefix+"EXEC dbo.sp_InvoiceDetail @a,@s,'HD97100002';") @(1,1,1,1)
 Read 'T17-K09 missing detail empty sets' ($prefix+"EXEC dbo.sp_InvoiceDetail @a,@s,'HD99999998';") @(0,0,0,0)
 Read 'T18-K07 delivery filters by code phone status' ($prefix+"EXEC dbo.sp_DeliveryList @a,@s,@Search='HD97000004',@Delivery=N'Da giao',@Status=N'Da thanh toan'; EXEC dbo.sp_DeliveryList @a,@s,@Search='0901234567',@Delivery=N'Da giao';") @(1,1)
 Read 'Investigate T15-K08 fixture tier versus expected gross' "SELECT k.MaKH,k.MaHang,t.PhanTramGiam,k.DiemTichLuy FROM dbo.KHACHHANG k JOIN dbo.HANGTHANHVIEN t ON t.MaHang=k.MaHang WHERE MaKH='KH001'; SELECT MaHD,TongTien,GiamGia,TongTien-GiamGia Net,COALESCE((SELECT SUM(SoDiem) FROM dbo.LICHSU_DIEM WHERE MaHD=h.MaHD),0) ActualPoints FROM dbo.HOADON h WHERE MaHD='HD97000005';" @(1,1)
 # Server commits, response is then delayed beyond client timeout: reconcile by original code/request.
 $request=[guid]::NewGuid().ToString();$sql=$prefix+"DECLARE @id uniqueidentifier='$request'; EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code='HD99000001',@RequestId=@id,@Channel=N'Mang di',@Product='SP001',@Quantity=1; WAITFOR DELAY '00:00:03';"
 $r=[ordered]@{Id='T14-K11 timeout after commit';Sql=$sql;ExpectedError=-2;Status='NOT RUN'};$cmd=$c.CreateCommand();$cmd.CommandText=$sql;$cmd.CommandTimeout=1
 try{[void]$cmd.ExecuteNonQuery();$r.Status='FAIL';$r['Error']='Timeout not observed'}catch{$r['ExceptionChain']=$_.Exception.ToString();$e=$_.Exception;while($e -and $e -isnot [System.Data.SqlClient.SqlException]){$e=$e.InnerException};$r['ActualError']=if($e){$e.Number}else{$null};$r.Status=if($e -and $e.Number-eq-2){'PASS'}else{'FAIL'}}finally{$cmd.Dispose()};$m.Cases+=$r;Save
 Read 'T14-K11 reconcile committed timed out request' ($prefix+"EXEC dbo.sp_SalesReconcile @a,@s,'$request','HD99000001','SalesCreate'; SELECT @@TRANCOUNT TranCount,XACT_STATE() XactState; SELECT COUNT(*) HeaderCount FROM dbo.HOADON WHERE MaHD='HD99000001';") @(1,1,1)
 $m.Status='Completed'
}catch{$m.Status='Aborted';$m['Error']=$_.Exception.ToString()}
finally{if($c){$c.Dispose()};if($m.Created.Count){$d=$null;try{$d=Open-TestConnection $db;if((Read-TestScalar $d 'SELECT RunId FROM dbo.G04TestMarker')-cne$runId-or$db-notmatch'^CoffeeManager_Test_G04_\d{8}_\d{6}_[a-f0-9]{8}_demo$'){throw 'Ownership mismatch'};$d.ChangeDatabase('master');Run-TestSql $d "ALTER DATABASE [$db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$db]";$m.Cleaned+=$db}catch{$m.Retained+=@{Database=$db;Reason=$_.Exception.Message}}finally{if($d){$d.Dispose()}}};Save;Write-Output "Remaining scenarios: $($m.Status), cases=$($m.Cases.Count), cleaned=$($m.Cleaned.Count), retained=$($m.Retained.Count)"}

