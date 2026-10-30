param([ValidateSet('Before','After')][string]$Phase='After')
$ErrorActionPreference='Stop'
$root='D:\Workspace\Nam_3\HK1\CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$out=Join-Path $PSScriptRoot $Phase;New-Item -ItemType Directory -Path $out -Force|Out-Null
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$db="CoffeeManager_Test_G04_${runId}_demo";$through=if($Phase-eq'After'){16}else{15}
$m=[ordered]@{Phase=$Phase;RunId=$runId;Database=$db;Schema=$through;Approval='User duyệt24/10/2026 G04-TEST-06/G04-CONC-01 + regression';Created=@();Cleaned=@();Retained=@();Cases=@();Plans=@();Status='Running'}
function Save {$m|ConvertTo-Json -Depth 14|Set-Content "$out/manifest.json" -Encoding utf8}
function Check([string]$id,[string]$sql){$r=[ordered]@{Id=$id;Sql=$sql;Status='NOT RUN'};try{Run-TestSql $c $sql;$r.Status='PASS'}catch{$r.Status='FAIL';$r['Error']=$_.Exception.ToString()};$m.Cases+=$r;Save}
function NumbersEqual([object[]]$expected,[object[]]$actual){
 if($expected.Count-ne$actual.Count){return $false}
 $left=@($expected|ForEach-Object{[int]$_}|Sort-Object);$right=@($actual|ForEach-Object{[int]$_}|Sort-Object)
 for($i=0;$i-lt$left.Count;$i++){if($left[$i]-ne$right[$i]){return $false}}
 return $true
}
function Pair([string]$id,[string]$one,[string]$two,[int]$secondExpected,[int]$firstExpected=0,[int]$leadMs=0){
 $r=[ordered]@{Id=$id;Sql1=$one;Sql2=$two;Expected=@($firstExpected,$secondExpected);Actual=@();Exceptions=@();TransactionStates=@();Status='NOT RUN'}
 $a=$null;$b=$null;$ca=$null;$cb=$null
 try{
  $a=Open-TestConnection $db;$b=Open-TestConnection $db;$ca=$a.CreateCommand();$cb=$b.CreateCommand();$ca.CommandText=$one;$cb.CommandText=$two;$ca.CommandTimeout=$cb.CommandTimeout=40
  $ta=$ca.BeginExecuteNonQuery();if($leadMs){[Threading.Thread]::Sleep($leadMs)};$tb=$cb.BeginExecuteNonQuery()
  foreach($item in @(@($ca,$ta),@($cb,$tb))){try{[void]$item[0].EndExecuteNonQuery($item[1]);$r.Actual+=0}catch{$r.Exceptions+=$_.Exception.ToString();$e=$_.Exception;while($e-and$e-isnot[Data.SqlClient.SqlException]){$e=$e.InnerException};$r.Actual+=if($e){[int]$e.Number}else{-99999}}}
  foreach($connection in @($a,$b)){$r.TransactionStates+=[string](Read-TestScalar $connection "DECLARE @tc int=@@TRANCOUNT,@xs int=XACT_STATE(); SELECT CONCAT(@tc,':',@xs)")}
  $numbersMatch = NumbersEqual $r.Expected $r.Actual
  $uncleanSessions = @($r.TransactionStates | Where-Object { $_ -ne '0:0' })
  $r.Status = if ($numbersMatch -and $uncleanSessions.Count -eq 0) { 'PASS' } else { 'FAIL' }
 }catch{$r.Status='FAIL';$r['Error']=$_.Exception.ToString()}
 finally{foreach($x in @($ca,$cb,$a,$b)){if($x){$x.Dispose()}}}
 $m.Cases+=$r;Save
}
function CapturePlan([string]$name,[string]$sql){
 $cmd=$c.CreateCommand();$cmd.CommandText="SET STATISTICS XML ON; $sql SET STATISTICS XML OFF;";$cmd.CommandTimeout=40
 $da=[Data.SqlClient.SqlDataAdapter]::new($cmd);$ds=[Data.DataSet]::new()
 try{
  [void]$da.Fill($ds);$i=0
  foreach($t in $ds.Tables){foreach($row in $t.Rows){foreach($v in $row.ItemArray){$xml=[string]$v;if($xml-like'*<ShowPlanXML*'){$i++;$file="$name-$i.sqlplan";[IO.File]::WriteAllText((Join-Path $out $file),$xml);$m.Plans+=$file}}}}
  if($i-eq0){throw 'No actual XML plan returned'}
 }finally{$da.Dispose();$ds.Dispose();$cmd.Dispose();Run-TestSql $c 'SET STATISTICS XML OFF;'}
 Save
}
function HeaderRaceSql([string]$code,[string]$kind){
 $prefix="DECLARE @a int,@s binary(8),@r binary(8),@id uniqueidentifier=NEWID(); SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; SELECT @r=Rev FROM dbo.G04Revision WHERE Code='$code'; "
 if($kind-eq'Delivery'){return $prefix+"EXEC dbo.sp_ChuyenTrangThaiGiaoHang @a,@s,'$code',@r,N'Cho giao',N'Dang giao',@id;"}
 return $prefix+"EXEC dbo.sp_SuaDongHoaDon @a,@s,'$code',@r,@id,'Add',@Product='SP001',@Quantity=1;"
}
$c=$null;Save
try{
 $negative=(NumbersEqual @(0,52103) @(102,102));$positive=(NumbersEqual @(0,52103) @(52103,0));$length=(NumbersEqual @(0,52103) @(0));
 if($negative-or-not$positive-or$length){throw 'Numeric comparator self-check failed'}
 $m.Cases+=@{Id='G04-TEST-06 validator negative/positive/count controls';Status='PASS';Negative102_102=$negative;ReorderedPositive=$positive;DifferentCount=$length};Save
 $c=Open-TestConnection 'master';if((Read-TestScalar $c "SELECT COUNT(*) FROM sys.databases WHERE name=N'$db'")-ne0){throw 'Database exists'}
 Run-TestSql $c "CREATE DATABASE [$db]";$m.Created+=$db;Save;$c.ChangeDatabase($db)
 Run-TestSql $c "CREATE TABLE dbo.G04TestMarker(RunId varchar(40) PRIMARY KEY); INSERT dbo.G04TestMarker VALUES('$runId');"
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText("$root/database/DB_Schema.sql"),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$',''))
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText("$root/database/Insert_Data.sql"),'(?im)^USE QuanLyCafe_ERP_V2;\s*$',''))
 for($v=1;$v-le$through;$v++){$f=@(Get-ChildItem "$root/database/migrations" -Filter ('{0:d3}_*.sql'-f$v));& "$root/tools/database/Apply-Migration.ps1" -Database $db -Version $v -File $f[0].FullName|ConvertTo-Json -Compress|Add-Content "$out/migrations.jsonl"}
 Run-TestSql $c ([IO.File]::ReadAllText("$PSScriptRoot/helpers.sql"))
 Run-TestSql $c "EXEC dbo.G04New 'HD92000001'; CREATE TABLE dbo.G04Revision(Code char(10) PRIMARY KEY,Rev binary(8));"
 CapturePlan 'checkout-trigger' "EXEC dbo.G04Pay 'HD92000001';"
 # Populate receipt gaps and neighboring open orders; both sessions must really overlap.
 Run-TestSql $c "DECLARE @i int=1,@code varchar(10); WHILE @i<=30 BEGIN SET @code='HD91'+RIGHT('000000'+CONVERT(varchar,@i),6); EXEC dbo.G04New @code; SET @i+=1; END;"
 if($Phase-eq'After'){
  foreach($file in @('scenarios.sql','supplement.sql')){foreach($part in [regex]::Split([IO.File]::ReadAllText("$PSScriptRoot/$file"),'(?m)^-- CASE ')){if($part.Trim()){$lines=$part-split'\r?\n',2;Check $lines[0].Trim() $lines[1]}}}
 }
 $rounds=if($Phase-eq'Before'){2}else{10}
 for($i=1;$i-le$rounds;$i++){
  $one='HD92'+('{0:d6}'-f(100+$i*2));$two='HD92'+('{0:d6}'-f(101+$i*2))
  Run-TestSql $c "EXEC dbo.G04New '$one'; EXEC dbo.G04New '$two'; UPDATE n SET SoLuongTon=r.DinhMuc FROM dbo.NGUYENVATLIEU n JOIN dbo.CONGTHUC r ON r.MaNVL=n.MaNVL WHERE r.MaSP='SP001';"
  Run-TestSql $c "CREATE TRIGGER dbo.G04Delay ON dbo.HOADON AFTER UPDATE AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE MaHD='$one' AND TrangThai=N'Chua thanh toan') WAITFOR DELAY '00:00:01'; END;"
  try{if($i%2){Pair "T15-K11 last stock round$i forward" "EXEC dbo.G04Pay '$one';" "EXEC dbo.G04Pay '$two';" 52203 0 100}else{Pair "T15-K11 last stock round$i reversed" "EXEC dbo.G04Pay '$two';" "EXEC dbo.G04Pay '$one';" 52203 0 100}}
  finally{Run-TestSql $c 'DROP TRIGGER dbo.G04Delay;'}
  Check "T15-K11 money-stock-points-receipt round$i" "IF (SELECT COUNT(*) FROM dbo.HOADON WHERE MaHD IN('$one','$two') AND TrangThai=N'Da thanh toan')<>1 OR EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE SoLuongTon<0) OR EXISTS(SELECT 1 FROM dbo.HOADON h WHERE MaHD IN('$one','$two') AND TrangThai=N'Chua thanh toan' AND (EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN p WHERE p.MaHD=h.MaHD) OR EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON x WHERE x.MaHD=h.MaHD) OR EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiSnapshot s WHERE s.MaHD=h.MaHD))) THROW 52990,'Stock race partial state',1; IF (SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE EntityCode IN('$one','$two') AND Operation='SalesCheckout')<>1 THROW 52990,'Checkout receipt count',1;"
 }
 if($Phase-eq'After'){
  Run-TestSql $c "UPDATE dbo.NGUYENVATLIEU SET SoLuongTon=10000;"
  foreach($round in 1..4){
   $code='HD92'+('{0:d6}'-f(300+$round));Run-TestSql $c "EXEC dbo.G04New '$code',N'Giao hang'; INSERT dbo.G04Revision SELECT MaHD,Revision FROM dbo.HOADON WHERE MaHD='$code';"
   $sql=HeaderRaceSql $code 'Delivery';Pair "T18-K05 typed delivery round$round" $sql $sql 52103
   Check "T18-K05 invariants round$round" "IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='$code' AND TrangThaiGiao=N'Dang giao' AND TrangThai=N'Chua thanh toan') OR EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaHD='$code') OR (SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE EntityCode='$code' AND Operation='SalesDelivery')<>1 THROW 52990,'Delivery race overwrite',1;"
  }
  Run-TestSql $c "EXEC dbo.G04New 'HD92000401'; INSERT dbo.G04Revision SELECT MaHD,Revision FROM dbo.HOADON WHERE MaHD='HD92000401';"
  $edit=HeaderRaceSql 'HD92000401' 'Edit';Pair 'T14-K10 typed line revision' $edit $edit 52103
  Run-TestSql $c "EXEC dbo.G04New 'HD92000402',@customer='KH001';"
  Pair 'T15-K10 same invoice different requests' "EXEC dbo.G04Pay 'HD92000402';" "EXEC dbo.G04Pay 'HD92000402';" 52102
  Check 'T15-K10 exactly one payment/history' "IF (SELECT COUNT(*) FROM dbo.CHITIET_THANHTOAN WHERE MaHD='HD92000402')<>1 OR (SELECT COUNT(*) FROM dbo.LICHSU_DIEM WHERE MaHD='HD92000402')<>1 THROW 52990,'Duplicate payment or points',1;"
  Run-TestSql $c "EXEC dbo.G04New 'HD92000403',@customer='KH001'; CREATE TABLE dbo.G04CheckoutFixed(Code char(10),Rev binary(8),Token binary(32),Amount decimal(18,0),Request uniqueidentifier); INSERT dbo.G04CheckoutFixed SELECT h.MaHD,h.Revision,p.PreviewToken,p.Payable,NEWID() FROM dbo.HOADON h CROSS APPLY dbo.fn_CheckoutPreview(h.MaHD) p WHERE h.MaHD='HD92000403'; SELECT MaNVL,SoLuongTon INTO dbo.G04StockBefore FROM dbo.NGUYENVATLIEU;"
  $same="DECLARE @a int,@s binary(8),@r binary(8),@t binary(32),@amount decimal(18,0),@id uniqueidentifier,@p dbo.DanhSachThanhToan; SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; SELECT @r=Rev,@t=Token,@amount=Amount,@id=Request FROM dbo.G04CheckoutFixed; INSERT @p VALUES('PT001',@amount,NULL); EXEC dbo.sp_ThanhToanHoaDon @a,@s,'HD92000403',@r,@id,@t,@p;"
  Pair 'T15-K10 same request typed replay concurrent' $same $same 0
  Check 'T15-K10 same request inventory points once' "IF (SELECT COUNT(*) FROM dbo.CHITIET_THANHTOAN WHERE MaHD='HD92000403')<>1 OR (SELECT COUNT(*) FROM dbo.LICHSU_DIEM WHERE MaHD='HD92000403')<>1 OR (SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE EntityCode='HD92000403' AND Operation='SalesCheckout')<>1 OR EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON x JOIN dbo.NGUYENVATLIEU n ON n.MaNVL=x.MaNVL JOIN dbo.G04StockBefore b ON b.MaNVL=n.MaNVL WHERE x.MaHD='HD92000403' AND n.SoLuongTon<>b.SoLuongTon-x.SoLuong) THROW 52990,'Replay duplicated side effects',1;"
  Pair 'T14-K09 same table two sessions' "EXEC dbo.G04New 'HD92000404',N'Tai quan',@table='B009';" "EXEC dbo.G04New 'HD92000405',N'Tai quan',@table='B009';" 52104
  Check 'T14-K09 actual existing order has first line only' "IF (SELECT COUNT(*) FROM dbo.HOADON WHERE MaBan='B009' AND TrangThai=N'Chua thanh toan')<>1 OR (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD IN('HD92000404','HD92000405'))<>1 THROW 52990,'Table conflict leaked line',1; DECLARE @a int,@s binary(8); SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; EXEC dbo.sp_TableSelectCurrent @a,@s,'B009';"
  Check 'T15-K13 paid payment UPDATE DELETE blocked' ([IO.File]::ReadAllText("$PSScriptRoot/payment-guards.sql"))
 }
 $m.Status = if (@($m.Cases | Where-Object { $_.Status -eq 'FAIL' }).Count) { 'CompletedWithFailures' } else { 'Succeeded' }
}catch{$m.Status='Aborted';$m['Error']=$_.Exception.ToString()}
finally{
 if($c){$c.Dispose()}
 if($m.Created.Count){$d=$null;try{$d=Open-TestConnection $db;if((Read-TestScalar $d 'SELECT RunId FROM dbo.G04TestMarker')-cne$runId-or$db-notmatch'^CoffeeManager_Test_G04_\d{8}_\d{6}_[a-f0-9]{8}_demo$'){throw 'Ownership mismatch'};$d.ChangeDatabase('master');Run-TestSql $d "ALTER DATABASE [$db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$db]";$m.Cleaned+=$db}catch{$m.Retained+=@{Database=$db;Reason=$_.Exception.Message}}finally{if($d){$d.Dispose()}}}
 Save;Write-Output "$Phase status=$($m.Status); cases=$($m.Cases.Count); cleaned=$($m.Cleaned.Count); retained=$($m.Retained.Count)"
}
