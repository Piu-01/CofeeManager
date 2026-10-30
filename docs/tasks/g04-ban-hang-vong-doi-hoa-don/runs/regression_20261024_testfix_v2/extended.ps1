$ErrorActionPreference='Stop'
$root='D:\Workspace\Nam_3\HK1\CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$out=$PSScriptRoot
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[guid]::NewGuid().ToString('N').Substring(0,8)
$db="CoffeeManager_Test_G04_${runId}_demo"
$manifest=[ordered]@{RunId=$runId;Database=$db;Marker=$runId;Created=@();Cleaned=@();Retained=@();Cases=@();Status='Running';Principal=[Security.Principal.WindowsIdentity]::GetCurrent().Name;Schema=15}
function Save { $manifest|ConvertTo-Json -Depth 9|Set-Content "$out/extended-results.json" -Encoding utf8 }
function Case([string]$id,[string]$sql,[int]$error=0) {
 $result=[ordered]@{Id=$id;Sql=$sql;ExpectedError=$error;Status='NOT RUN'}
 try { if($error){Expect-TestSqlError $c $sql @($error)}else{Run-TestSql $c $sql};$result.Status='PASS' }
 catch {$result.Status='FAIL';$result['Error']=$_.Exception.ToString()}
 $manifest.Cases+=$result;Save
}
function Pair([string]$id,[string]$one,[string]$two,[int]$winnerError=0,[int]$loserError=0) {
 $r=[ordered]@{Id=$id;Sql1=$one;Sql2=$two;Expected=@($winnerError,$loserError);Actual=@();Status='NOT RUN'}
 $a=$null;$b=$null;$ca=$null;$cb=$null
 try {
  $a=Open-TestConnection $db;$b=Open-TestConnection $db
  $ca=$a.CreateCommand();$ca.CommandText=$one;$ca.CommandTimeout=30
  $cb=$b.CreateCommand();$cb.CommandText=$two;$cb.CommandTimeout=30
  $ta=$ca.BeginExecuteNonQuery();$tb=$cb.BeginExecuteNonQuery()
  foreach($item in @(@($ca,$ta),@($cb,$tb))){try{[void]$item[0].EndExecuteNonQuery($item[1]);$r.Actual+=0}catch{$e=$_.Exception;while($e.InnerException){$e=$e.InnerException};$r.Actual+=$e.Number}}
  $r.Status=if(($r.Actual|Sort-Object)-join ',' -eq (@($winnerError,$loserError)|Sort-Object)-join ','){'PASS'}else{'FAIL'}
 }catch{$r.Status='FAIL';$r['Error']=$_.Exception.ToString()}
 finally{foreach($x in @($ca,$cb,$a,$b)){if($null-ne$x){$x.Dispose()}}}
 $manifest.Cases+=$r;Save
}
Save
$c=$null
try {
 $c=Open-TestConnection 'master'
 if((Read-TestScalar $c "SELECT COUNT(*) FROM sys.databases WHERE name=N'$db'")-ne 0){throw 'Database exists'}
 Run-TestSql $c "CREATE DATABASE [$db]";$manifest.Created+= $db;Save;$c.ChangeDatabase($db)
 Run-TestSql $c "CREATE TABLE dbo.G04TestMarker(RunId varchar(40) PRIMARY KEY); INSERT dbo.G04TestMarker VALUES('$runId');"
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText("$root/database/DB_Schema.sql"),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$',''))
 Run-TestSql $c ([regex]::Replace([IO.File]::ReadAllText("$root/database/Insert_Data.sql"),'(?im)^USE QuanLyCafe_ERP_V2;\s*$',''))
 for($v=1;$v-le15;$v++){$f=@(Get-ChildItem "$root/database/migrations" -Filter ('{0:d3}_*.sql'-f$v)); & "$root/tools/database/Apply-Migration.ps1" -Database $db -Version $v -File $f[0].FullName|ConvertTo-Json -Compress|Add-Content "$out/extended-migrations.jsonl"}
 Run-TestSql $c ([IO.File]::ReadAllText("$out/helpers.sql"))
 $cases=[regex]::Split([IO.File]::ReadAllText("$out/scenarios.sql"),'(?m)^-- CASE ')
 foreach($part in $cases){if($part.Trim()){$lines=$part -split '\r?\n',2;$id=$lines[0].Trim();Case $id $lines[1]}}
 foreach($table in @('HOADON','CHITIET_HD','G03OperationReceipt')){
  Run-TestSql $c "CREATE TRIGGER dbo.G04Fault ON dbo.[$table] AFTER INSERT AS BEGIN THROW 52991,'G04 injected create failure',1; END;"
  try {Case "T14-K11 rollback fault $table" "DECLARE @b nvarchar(max),@e nvarchar(max); EXEC dbo.G04State @b OUTPUT; EXEC dbo.G04Expect N'EXEC dbo.G04New ''HD95000001'';',52991; EXEC dbo.G04State @e OUTPUT; IF @b<>@e THROW 52990,'Create rollback mismatch',1;"}
  finally{Run-TestSql $c 'DROP TRIGGER dbo.G04Fault;'}
 }
 Run-TestSql $c "EXEC dbo.G04New 'HD95000002',@customer='KH001';"
 foreach($table in @('TIEUHAO_HOADON','CHITIET_THANHTOAN','LICHSU_DIEM')){
  Run-TestSql $c "CREATE TRIGGER dbo.G04Fault ON dbo.[$table] AFTER INSERT AS BEGIN THROW 52991,'G04 injected checkout failure',1; END;"
  try {Case "T15-K12 rollback fault $table" "DECLARE @b nvarchar(max),@e nvarchar(max); EXEC dbo.G04State @b OUTPUT; EXEC dbo.G04Expect N'EXEC dbo.G04Pay ''HD95000002'';',52991; EXEC dbo.G04State @e OUTPUT; IF @b<>@e THROW 52990,'Checkout rollback mismatch',1;"}
  finally{Run-TestSql $c 'DROP TRIGGER dbo.G04Fault;'}
 }
 # Two real connections overlap; test-only delay in create/checkout holds transaction.
 Run-TestSql $c "CREATE TRIGGER dbo.G04Delay ON dbo.HOADON AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE MaHD LIKE 'HD96%') WAITFOR DELAY '00:00:02'; END;"
 Pair 'T14-K09 concurrent same table' "EXEC dbo.G04New 'HD96000001',N'Tai quan',@table='B009';" "EXEC dbo.G04New 'HD96000002',N'Tai quan',@table='B009';" 0 52104
 Case 'T14-K09 invariants' "IF (SELECT COUNT(*) FROM dbo.HOADON WHERE MaBan='B009' AND TrangThai=N'Chua thanh toan')<>1 OR (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD IN('HD96000001','HD96000002'))<>1 THROW 52990,'Competing table leaked rows',1;"
 Run-TestSql $c 'DROP TRIGGER dbo.G04Delay;'
 Run-TestSql $c "EXEC dbo.G04New 'HD96000003'; CREATE TABLE dbo.G04Revision(Code char(10),Rev binary(8)); INSERT dbo.G04Revision SELECT MaHD,Revision FROM dbo.HOADON WHERE MaHD='HD96000003';"
 $edit="DECLARE @a int,@s binary(8),@r binary(8),@id uniqueidentifier=NEWID(); SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; SELECT @r=Rev FROM dbo.G04Revision; EXEC dbo.sp_SuaDongHoaDon @a,@s,'HD96000003',@r,@id,'Add',@Product='SP001',@Quantity=1;"
 Pair 'T14-K10 concurrent same revision' $edit $edit 0 52103
 Run-TestSql $c "EXEC dbo.G04New 'HD96000004',@customer='KH001';"
 Run-TestSql $c "CREATE TRIGGER dbo.G04Delay ON dbo.TIEUHAO_HOADON AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE MaHD LIKE 'HD96%') WAITFOR DELAY '00:00:02'; END;"
 Pair 'T15-K10 concurrent same invoice' "EXEC dbo.G04Pay 'HD96000004';" "EXEC dbo.G04Pay 'HD96000004';" 0 52102
 Case 'T15-K10 invariants' "IF (SELECT COUNT(*) FROM dbo.CHITIET_THANHTOAN WHERE MaHD='HD96000004')<>1 OR (SELECT COUNT(*) FROM dbo.LICHSU_DIEM WHERE MaHD='HD96000004')<>1 THROW 52990,'Double checkout',1;"
 Run-TestSql $c "DROP TRIGGER dbo.G04Delay; EXEC dbo.G04New 'HD96000005'; EXEC dbo.G04New 'HD96000006'; UPDATE n SET SoLuongTon=c.DinhMuc FROM dbo.NGUYENVATLIEU n JOIN dbo.CONGTHUC c ON c.MaNVL=n.MaNVL WHERE c.MaSP='SP001';"
 Pair 'T15-K11 competing last stock' "EXEC dbo.G04Pay 'HD96000005';" "EXEC dbo.G04Pay 'HD96000006';" 0 52203
 Case 'T15-K11 invariants' "IF (SELECT COUNT(*) FROM dbo.HOADON WHERE MaHD IN('HD96000005','HD96000006') AND TrangThai=N'Da thanh toan')<>1 OR EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE SoLuongTon<0) OR EXISTS(SELECT 1 FROM dbo.HOADON h JOIN dbo.CHITIET_THANHTOAN t ON t.MaHD=h.MaHD WHERE h.MaHD IN('HD96000005','HD96000006') AND h.TrangThai=N'Chua thanh toan') THROW 52990,'Stock race invariant',1;"
 Run-TestSql $c "EXEC dbo.G04New 'HD96000007',N'Giao hang';"
 $rev=Read-TestScalar $c "SELECT CONVERT(varchar(18),Revision,1) FROM dbo.HOADON WHERE MaHD='HD96000007'"
 $delivery="DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@r binary(8)=$rev; SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; EXEC dbo.sp_ChuyenTrangThaiGiaoHang @a,@s,'HD96000007',@r,N'Cho giao',N'Dang giao',@id;"
 Pair 'T18-K05 concurrent delivery same revision' $delivery $delivery 0 52103
 $manifest.Status='Completed'
}catch{$manifest.Status='Aborted';$manifest['Error']=$_.Exception.ToString()}
finally {
 if($null-ne$c){$c.Dispose()}
 if($manifest.Created.Count){$d=$null;try{$d=Open-TestConnection $db;if((Read-TestScalar $d 'SELECT RunId FROM dbo.G04TestMarker')-cne$runId){throw 'Marker mismatch'};if($db-notmatch'^CoffeeManager_Test_G04_\d{8}_\d{6}_[a-f0-9]{8}_demo$'){throw 'Target invalid'};$d.ChangeDatabase('master');Run-TestSql $d "ALTER DATABASE [$db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$db];";$manifest.Cleaned+=$db}catch{$manifest.Retained+=@{Database=$db;Reason=$_.Exception.Message}}finally{if($d){$d.Dispose()}}}
 Save
 $manifest|ConvertTo-Json -Depth 2
}
