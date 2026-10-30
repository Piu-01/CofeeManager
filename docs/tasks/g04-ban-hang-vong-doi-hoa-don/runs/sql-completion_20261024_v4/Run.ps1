$ErrorActionPreference = 'Stop'
$root = 'D:\Workspace\Nam_3\HK1\CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$out = $PSScriptRoot
$runId = (Get-Date -Format 'yyyyMMdd_HHmmss') + '_' + [guid]::NewGuid().ToString('N').Substring(0,8)
$m = [ordered]@{RunId=$runId;Approval='User24/10/2026: kiểm phần agent; existing approved G04 test';Schema=16;Created=@();Cleaned=@();Retained=@();Cases=@();Status='Running'}
function Save { $m | ConvertTo-Json -Depth 14 | Set-Content "$out/manifest.json" -Encoding utf8 }
function Case([string]$id,[scriptblock]$action) {
 $script:evidence = [ordered]@{Id=$id;Database=$c.Database;Procedure=$action.ToString();Status='NOT RUN'}
 try { & $action; $script:evidence.Status='PASS' }
 catch { $script:evidence.Status='FAIL'; $script:evidence['Error']=$_.Exception.ToString() }
 $m.Cases += $script:evidence; Save
}
function NewDb([string]$mode,[int]$through) {
 $name="CoffeeManager_Test_G04_${runId}_$mode"
 $connection=Open-TestConnection 'master'
 try {
  if ((Read-TestScalar $connection "SELECT COUNT(*) FROM sys.databases WHERE name=N'$name'") -ne 0) { throw 'Database already exists' }
  Run-TestSql $connection "CREATE DATABASE [$name]"; $m.Created+=@{Database=$name;Marker=$runId;Schema=$through}; Save
  $connection.ChangeDatabase($name)
  Run-TestSql $connection "CREATE TABLE dbo.G04TestMarker(RunId varchar(40) PRIMARY KEY); INSERT dbo.G04TestMarker VALUES('$runId');"
  Run-TestSql $connection ([regex]::Replace([IO.File]::ReadAllText("$root/database/DB_Schema.sql"),'(?im)^(CREATE DATABASE|USE) QuanLyCafe_ERP_V2;\s*$',''))
  Run-TestSql $connection ([regex]::Replace([IO.File]::ReadAllText("$root/database/Insert_Data.sql"),'(?im)^USE QuanLyCafe_ERP_V2;\s*$',''))
  for($v=1;$v -le $through;$v++) { $f=@(Get-ChildItem "$root/database/migrations" -Filter ('{0:d3}_*.sql' -f $v)); & "$root/tools/database/Apply-Migration.ps1" -Database $name -Version $v -File $f[0].FullName | ConvertTo-Json -Compress | Add-Content "$out/$mode-migrations.jsonl" }
  return $connection
 } catch { $connection.Dispose(); throw }
}
function DisposeDb($connection) {
 $name=$connection.Database
 try {
  if ($name -notmatch '^CoffeeManager_Test_G04_\d{8}_\d{6}_[a-f0-9]{8}_(demo|legacy9|legacy10)$' -or (Read-TestScalar $connection 'SELECT RunId FROM dbo.G04TestMarker') -cne $runId) { throw 'Ownership mismatch' }
  $connection.ChangeDatabase('master'); Run-TestSql $connection "ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$name]"; $m.Cleaned+=$name
 } catch { $m.Retained+=@{Database=$name;Reason=$_.Exception.Message} }
 finally { $connection.Dispose(); Save }
}
function State($connection) { return [string](Read-TestScalar $connection 'DECLARE @s nvarchar(max); EXEC dbo.G04State @s OUTPUT; SELECT @s;') }
function CleanState($connection) {
 $state=[string](Read-TestScalar $connection "DECLARE @t int=@@TRANCOUNT,@s int=XACT_STATE(); SELECT CONCAT(@t,':',@s);")
 if ($state -ne '0:0') { throw "Transaction not clean: $state" }; return $state
}
function Query($connection,[string]$sql) {
 $cmd=$connection.CreateCommand();$cmd.CommandText=$sql;$cmd.CommandTimeout=20
 $adapter=[Data.SqlClient.SqlDataAdapter]::new($cmd);$set=[Data.DataSet]::new()
 try { [void]$adapter.Fill($set); return ,$set } finally { $adapter.Dispose();$cmd.Dispose() }
}
function FinancialSnapshot($connection) {
 return [string](Read-TestScalar $connection @'
SELECT (SELECT
 (SELECT MaHD,NgayLap,MaNV,MaKH,MaBan,MaDT,MaKM,LoaiDon,TongTien,GiamGia,PhiShip,ThanhTien,TrangThai,NgayThanhToan,PhiDoiTac,PhanTramPhiDoiTac FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH,INCLUDE_NULL_VALUES) h,
 (SELECT MaDong,MaHD,MaSP,SoLuong,GiaBan,GhiChu FROM dbo.CHITIET_HD ORDER BY MaDong FOR JSON PATH,INCLUDE_NULL_VALUES) d,
 (SELECT MaTT,MaHD,MaPT,SoTien,NgayTT,MaThamChieu FROM dbo.CHITIET_THANHTOAN ORDER BY MaTT FOR JSON PATH,INCLUDE_NULL_VALUES) p,
 (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) stock,
 (SELECT MaKH,DiemTichLuy,MaHang FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH) points,
 (SELECT MaHD,MaNVL,SoLuong FROM dbo.TIEUHAO_HOADON ORDER BY MaHD,MaNVL FOR JSON PATH) consumption,
 (SELECT MaGD,MaKH,MaHD,NgayGD,Loai,SoDiem,LyDo FROM dbo.LICHSU_DIEM ORDER BY MaGD FOR JSON PATH,INCLUDE_NULL_VALUES) history
 FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
'@)
}
function CreateSql([string]$code,[string]$request,[int]$quantity=1,[string]$employee='NV001') {
 return "DECLARE @a int,@s binary(8),@id uniqueidentifier='$request'; SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='$employee'; EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code='$code',@RequestId=@id,@Channel=N'Mang di',@Product='SP001',@Quantity=$quantity;"
}
function AssertReconcile([string]$code,[string]$request) {
 $sql="DECLARE @a int,@s binary(8); SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; EXEC dbo.sp_SalesReconcile @a,@s,'$request','$code','SalesCreate'; SELECT MaHD,TrangThai FROM dbo.HOADON WHERE MaHD='$code'; SELECT COUNT(*) Lines FROM dbo.CHITIET_HD WHERE MaHD='$code';"
 $set=Query $c $sql
 try { if ($set.Tables.Count -ne 3 -or $set.Tables[0].Rows.Count -ne 1 -or $set.Tables[1].Rows.Count -ne 1 -or [int]$set.Tables[2].Rows[0]['Lines'] -ne 1) { throw 'Reconciliation not one receipt/header/line' };$script:evidence['Reconciled']=@{Code=$code;ReceiptCount=1;HeaderCount=1;LineCount=1;Status=[string]$set.Tables[1].Rows[0]['TrangThai']} }
 finally { $set.Dispose() }
}
$c=$null;Save
try {
 $c=NewDb 'demo' 0
 Case 'T15-K15 baseline seed then001-016 preserves historical money-stock-points' {
  $before=FinancialSnapshot $c
  for($v=1;$v -le 16;$v++) { $f=@(Get-ChildItem "$root/database/migrations" -Filter ('{0:d3}_*.sql' -f $v)); & "$root/tools/database/Apply-Migration.ps1" -Database $c.Database -Version $v -File $f[0].FullName | ConvertTo-Json -Compress | Add-Content "$out/demo-upgrade.jsonl" }
  $after=FinancialSnapshot $c
  if ($before -cne $after) { throw 'Legacy financial balances changed by upgrade' }
  $script:evidence['Before']=$before;$script:evidence['After']=$after
  Run-TestSql $c "IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan') OR EXISTS(SELECT 1 FROM dbo.HOADON h LEFT JOIN dbo.HoaDonUuDaiSnapshot s ON s.MaHD=h.MaHD WHERE h.TrangThai=N'Da thanh toan' AND (s.MaHD IS NULL OR s.Kind<>'LegacyUnknown' OR s.Source IS NOT NULL OR s.PercentDiscount IS NOT NULL OR s.Discount<>h.GiamGia)) THROW 52990,'Invalid legacy snapshot',1;"
 }
 Case 'T15-K15 baseline seed rerun rejected atomically' {
  $before=Snapshot-TestRows $c
  $seed=[regex]::Replace([IO.File]::ReadAllText("$root/database/Insert_Data.sql"),'(?im)^USE QuanLyCafe_ERP_V2;\s*$','')
  Expect-TestSqlError $c $seed 51200
  if ($before -cne (Snapshot-TestRows $c)) { throw 'Rejected seed changed rows' }
  $script:evidence['ExpectedError']=51200;$script:evidence['AllTableRowsUnchanged']=$true
 }
 Run-TestSql $c ([IO.File]::ReadAllText("$out/helpers.sql"))
 Case 'T14-K02 anonymous bad FK missing actor stale inactive resigned' { Run-TestSql $c ([IO.File]::ReadAllText("$out/actor-validation.sql"));$script:evidence['RejectedModes']=@('BadTable52105','BadCustomer52105','MissingActor51505','StaleStamp51505','Inactive51505','Resigned51505');$script:evidence['Transaction']=CleanState $c }
 # Each losing session itself performs the table/order readback after conflict.
 foreach($round in 1..2) {
  Case "T14-K09 two sessions loser loads actual order round$round" {
   $a=$null;$b=$null;$ca=$null;$cb=$null
   $one='HD8700100'+[string]($round*2-1);$two='HD8700100'+[string]($round*2)
   try {
    $a=Open-TestConnection $c.Database;$b=Open-TestConnection $c.Database;$ca=$a.CreateCommand();$cb=$b.CreateCommand()
    $ca.CommandText="EXEC dbo.G04New '$one',N'Tai quan',@table='B009';";$cb.CommandText="EXEC dbo.G04New '$two',N'Tai quan',@table='B009';";$ca.CommandTimeout=$cb.CommandTimeout=30
    if ($round -eq 1) { $ta=$ca.BeginExecuteNonQuery();$tb=$cb.BeginExecuteNonQuery() } else { $tb=$cb.BeginExecuteNonQuery();$ta=$ca.BeginExecuteNonQuery() }
    $results=@();foreach($item in @(@($ca,$ta),@($cb,$tb))) { try { [void]$item[0].EndExecuteNonQuery($item[1]);$results+=0 } catch { $e=$_.Exception;while($e -and $e -isnot [Data.SqlClient.SqlException]){$e=$e.InnerException};if(-not $e){throw};$results += [int]$e.Number } }
    $actualCodes = ($results | Sort-Object) -join ','
    if ($actualCodes -cne '0,52104') { throw "Expected0/52104,got $results" }
    $winner=[string](Read-TestScalar $c "SELECT RTRIM(MaHD) FROM dbo.HOADON WHERE MaBan='B009' AND TrangThai=N'Chua thanh toan'")
    $loser=if($results[0] -eq 52104){$a}else{$b}
    $read=Query $loser "DECLARE @a int,@s binary(8); SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; EXEC dbo.sp_TableSelectCurrent @a,@s,'B009';"
    try { if ($read.Tables[0].Rows.Count -ne 1 -or ([string]$read.Tables[0].Rows[0]['OrderCode']).Trim() -cne $winner) { throw 'Loser did not load winning code' } } finally { $read.Dispose() }
    $detail=Query $loser "DECLARE @a int,@s binary(8); SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; EXEC dbo.sp_SalesOrderGet @a,@s,'$winner';"
    try { if ($detail.Tables[0].Rows.Count -ne 1 -or ([string]$detail.Tables[0].Rows[0]['MaHD']).Trim() -cne $winner -or $detail.Tables[1].Rows.Count -ne 1) { throw 'Loser loaded wrong order or extra item' } } finally { $detail.Dispose() }
    $script:evidence['Actual']=$results;$script:evidence['WinnerCode']=$winner;$script:evidence['LoserLoadedCode']=$winner;$script:evidence['SessionStates']=@((CleanState $a),(CleanState $b))
    Run-TestSql $c "IF (SELECT COUNT(*) FROM dbo.HOADON WHERE MaBan='B009' AND TrangThai=N'Chua thanh toan')<>1 OR (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD IN('$one','$two'))<>1 THROW 52990,'Table race partial state',1; DECLARE @a int,@s binary(8),@r binary(8),@id uniqueidentifier=NEWID(); SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001'; SELECT @r=Revision FROM dbo.HOADON WHERE MaHD='$winner'; EXEC dbo.sp_HuyHoaDon @a,@s,'$winner',@r,@id;"
   } finally { foreach($x in @($ca,$cb,$a,$b)){if($x){$x.Dispose()}} }
  }
 }
 Case 'T14-K11 fault after discount recompute rolls back header line discount receipt' {
  Run-TestSql $c "CREATE TRIGGER dbo.G04DiscountFault ON dbo.HOADON AFTER UPDATE AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE MaHD='HD87002001' AND GiamGia>0) THROW 52992,'Injected discount-stage failure',1; END;"
  try { $before=State $c;Expect-TestSqlError $c "EXEC dbo.G04New 'HD87002001',@promotion='KM90000002';" 52992;if($before -cne (State $c)){throw 'Discount fault leaked changes'};$script:evidence['ExpectedError']=52992;$script:evidence['SnapshotUnchanged']=$true;$script:evidence['Transaction']=CleanState $c } finally { Run-TestSql $c 'DROP TRIGGER dbo.G04DiscountFault;' }
 }
 Case 'T14-K11 timeout before commit rolls back complete create' {
  Run-TestSql $c "CREATE TRIGGER dbo.G04WaitBefore ON dbo.HOADON AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE MaHD='HD87003001') WAITFOR DELAY '00:00:05'; END;"
  $writer=$null;$cmd=$null
  try { $before=State $c;$writer=Open-TestConnection $c.Database;$cmd=$writer.CreateCommand();$request=[guid]::NewGuid().ToString();$cmd.CommandText=CreateSql 'HD87003001' $request;$cmd.CommandTimeout=1;$observed=$false
   try { [void]$cmd.ExecuteNonQuery() } catch { $script:evidence['Exception']=$_.Exception.ToString();$e=$_.Exception;while($e -and $e -isnot [Data.SqlClient.SqlException]){$e=$e.InnerException};if(-not $e -or $e.Number -ne -2){throw};$observed=$true }
   if(-not $observed -or $before -cne (State $c)){throw 'Precommit timeout not atomic'};$script:evidence['ActualError']=-2;$script:evidence['WriterState']=CleanState $writer;$script:evidence['ObserverState']=CleanState $c
   Run-TestSql $c "IF EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='HD87003001') OR EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId='$request') THROW 52990,'Precommit timeout persisted',1;"
  } finally {if($cmd){$cmd.Dispose()};if($writer){$writer.Dispose()};Run-TestSql $c 'DROP TRIGGER dbo.G04WaitBefore;' }
 }
 Case 'T14-K11 disconnect after commit reconcile replay and mismatched hash actor' {
  $writer=$null;$cmd=$null
  try { $request=[guid]::NewGuid().ToString();$code='HD87003002';$writer=Open-TestConnection $c.Database;$cmd=$writer.CreateCommand();$cmd.CommandText=(CreateSql $code $request)+" WAITFOR DELAY '00:00:05';";$cmd.CommandTimeout=15;$pending=$cmd.BeginExecuteNonQuery()
   $seen=$false;for($i=0;$i -lt 40;$i++){if((Read-TestScalar $c "SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE RequestId='$request'") -eq 1){$seen=$true;break};[Threading.Thread]::Sleep(100)}
   if(-not $seen){throw 'Commit not observed before disconnect'};$writer.Close();try{[void]$cmd.EndExecuteNonQuery($pending)}catch{$script:evidence['ClientCloseException']=$_.Exception.ToString()}
   AssertReconcile $code $request
   $before=State $c;Run-TestSql $c (CreateSql $code $request);if($before -cne (State $c)){throw 'Replay wrote again'}
   Expect-TestSqlError $c (CreateSql $code $request 2) 51608
   Expect-TestSqlError $c (CreateSql $code $request 1 'NV004') 51608
   if($before -cne (State $c)){throw 'Mismatch changed rows'};$script:evidence['ReplayUnchanged']=$true;$script:evidence['MismatchErrors']=@(51608,51608);$script:evidence['ReadbackState']=CleanState $c
  } finally {if($cmd){$cmd.Dispose()};if($writer){$writer.Dispose()}}
 }
 Case 'T14-K11 timeout after commit readback uses original request' {
  $writer=$null;$cmd=$null
  try { $request=[guid]::NewGuid().ToString();$writer=Open-TestConnection $c.Database;$cmd=$writer.CreateCommand();$cmd.CommandText=(CreateSql 'HD87003003' $request)+" WAITFOR DELAY '00:00:05';";$cmd.CommandTimeout=1;$observed=$false
   try{[void]$cmd.ExecuteNonQuery()}catch{$e=$_.Exception;while($e -and $e -isnot [Data.SqlClient.SqlException]){$e=$e.InnerException};if(-not $e -or $e.Number -ne -2){throw};$observed=$true;$script:evidence['Exception']=$_.Exception.ToString()}
   if(-not $observed){throw 'No timeout observed'};AssertReconcile 'HD87003003' $request;$script:evidence['ActualError']=-2;$script:evidence['WriterState']=CleanState $writer
  } finally {if($cmd){$cmd.Dispose()};if($writer){$writer.Dispose()}}
 }
 foreach($version in @(9,10)) {
  $legacy=$null
  try { $legacy=NewDb "legacy$version" $version;$main=$c;$c=$legacy
   Case "T15-K15 invalid legacy preflight after schema$version preserves original rows" {
    if($version -eq 9){Run-TestSql $c "INSERT dbo.HOADON(MaHD,MaNV,LoaiDon,TenNguoiNhan,SDTNguoiNhan,DiaChiGiao,TrangThaiGiao) VALUES('HD87004001','NV001',N'Giao hang',N'   ',' ',N' ',N'Cho giao');";$next=10;$expected=52107}
    else {Run-TestSql $c "UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV='NV001';DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID();SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';EXEC dbo.sp_TaoHoaDon @a,@s,'HD87004002',@id,N'Mang di',@Product='SP001',@Quantity=1;INSERT dbo.CHITIET_THANHTOAN(MaHD,MaPT,SoTien) VALUES('HD87004002','PT001',10000);";$next=11;$expected=52207}
    $before=Snapshot-TestRows $c;$file=@(Get-ChildItem "$root/database/migrations" -Filter ('{0:d3}_*.sql' -f $next));$seen=$false
    try { & "$root/tools/database/Apply-Migration.ps1" -Database $c.Database -Version $next -File $file[0].FullName | Out-Null } catch { $e=$_.Exception;while($e -and $e -isnot [Data.SqlClient.SqlException]){$e=$e.InnerException};if(-not $e -or $expected -notin @($e.Errors|ForEach-Object Number)){throw};$seen=$true;$script:evidence['Error']=$_.Exception.ToString() }
    if(-not $seen -or $before -cne (Snapshot-TestRows $c)){throw 'Preflight did not preserve invalid legacy data'}
    if((Read-TestScalar $c "SELECT COUNT(*) FROM dbo.SchemaMigrations WHERE Version=$next") -ne 0){throw 'Failed migration version recorded'};$script:evidence['ExpectedError']=$expected;$script:evidence['AllOriginalRowsUnchanged']=$true;$script:evidence['NoInvalidDataRepaired']=$true;$script:evidence['Transaction']=CleanState $c
   }
  } finally {if($legacy){DisposeDb $legacy};$c=$main}
 }
 $m.Status=if(@($m.Cases | Where-Object {$_.Status -eq 'FAIL'}).Count){'CompletedWithFailures'}else{'Succeeded'}
} catch { $m.Status='Aborted';$m['Error']=$_.Exception.ToString() }
finally {if($c){DisposeDb $c};Save;Write-Output "SQL completion: $($m.Status),cases=$($m.Cases.Count),created=$($m.Created.Count),cleaned=$($m.Cleaned.Count),retained=$($m.Retained.Count)"}
