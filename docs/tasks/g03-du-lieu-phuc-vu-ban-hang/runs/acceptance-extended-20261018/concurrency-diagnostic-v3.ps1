$ErrorActionPreference='Stop'
$root='D:/Workspace/Nam_3/HK1/CoffeeManager';. "$root/tools/database/DatabaseTestTools.ps1"
$out=Join-Path $root 'docs/tasks/g03-du-lieu-phuc-vu-ban-hang/runs/acceptance-extended-20261018'
$m=Get-Content "$out/manifest.json" -Raw|ConvertFrom-Json;$db=$m.Database
$results=[Collections.Generic.List[object]]::new()
function Connection { $b=[System.Data.SqlClient.SqlConnectionStringBuilder]::new("Server=localhost;Initial Catalog=$db;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False");$c=[System.Data.SqlClient.SqlConnection]::new($b.ConnectionString);$c.Open();return $c }
function Prefix {return "DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID();SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;"}
function Save { $results|ConvertTo-Json -Depth 6|Set-Content "$out/concurrency-fault-results-v3.json" -Encoding utf8 }
function Two([string]$Id,[string]$First,[string]$Second,[int]$ExpectedError,[string]$AssertSql){
 $one=Connection;$two=Connection;$monitor=Connection;$r=[ordered]@{Id=$Id;Expected="Second request blocks; then error $ExpectedError; final SQL assertions hold";Status='NOT RUN';Actual=$null;Wait=$null}
 try{
  ((Prefix)+$First)|Set-Content "$out/v3-$Id-first.sql" -Encoding utf8
  ((Prefix)+$Second)|Set-Content "$out/v3-$Id-second.sql" -Encoding utf8
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
$control=Connection
try{
 if((Read-TestScalar $control 'SELECT RunId FROM dbo.G03TestMarker') -cne $m.RunId){throw 'Wrong marker'}
 Run-TestSql $control ((Prefix)+"EXEC dbo.sp_MaterialCreate @a,@s,'NVL98',@id,NULL,N'Concurrent material',N'Kg';")
 $rev=Read-TestScalar $control "SELECT CONVERT(varchar(18),CONVERT(binary(8),Revision),1) FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL98'"
 Two T09-K03-two "DECLARE @r binary(8)=$rev;BEGIN TRAN;EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='NVL98',@RequestId=@id,@ExpectedRevision=@r,@Quantity=1,@Source=N'Session1';" "DECLARE @r binary(8)=$rev;EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='NVL98',@RequestId=@id,@ExpectedRevision=@r,@Quantity=2,@Source=N'Session2';" 51603 "IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL98')<>1 OR (SELECT COUNT(*) FROM dbo.MaterialOpening WHERE MaNVL='NVL98')<>1 THROW 52999,'Double opening changed stock',1;"
 Run-TestSql $control ((Prefix)+"EXEC dbo.sp_CustomerCreate @Actor=@a,@Stamp=@s,@RequestId=@id,@Name=N'Concurrent points v3',@Phone=NULL;")
 $kh=Read-TestScalar $control "SELECT MaKH FROM dbo.KHACHHANG WHERE TenKH=N'Concurrent points v3'"
 $rev=Read-TestScalar $control "SELECT CONVERT(varchar(18),CONVERT(binary(8),Revision),1) FROM dbo.KHACHHANG WHERE MaKH='$kh'"
 Run-TestSql $control ((Prefix)+"DECLARE @r binary(8)=$rev;EXEC dbo.sp_CustomerOpen @Actor=@a,@Stamp=@s,@RequestId=@id,@Code='$kh',@ExpectedRevision=@r,@Points=100,@Source=N'Opening';")
 $rev=Read-TestScalar $control "SELECT CONVERT(varchar(18),CONVERT(binary(8),Revision),1) FROM dbo.KHACHHANG WHERE MaKH='$kh'";$policy=Read-TestScalar $control "SELECT CONVERT(varchar(18),CONVERT(binary(8),Revision),1) FROM dbo.LoyaltyPolicyState WHERE Id=1"
 Two T13-K11-two "DECLARE @r binary(8)=$rev,@p binary(8)=$policy;BEGIN TRAN;EXEC dbo.sp_LoyaltyAdjust @a,@s,'$kh',N'Tru',80,N'Session1',@r,@p,@id;" "DECLARE @r binary(8)=$rev,@p binary(8)=$policy;EXEC dbo.sp_LoyaltyAdjust @a,@s,'$kh',N'Tru',80,N'Session2',@r,@p,@id;" 51603 "IF (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='$kh')<>20 OR (SELECT COUNT(*) FROM dbo.LICHSU_DIEM WHERE MaKH='$kh')<>1 THROW 52999,'Concurrent points v3 mismatch',1;"
 Run-TestSql $control ((Prefix)+"DECLARE @r dbo.RecipeLineInput;INSERT @r VALUES('NVL01',0.018),('NVL02',0.02);EXEC dbo.sp_ProductCreate @Actor=@a,@Stamp=@s,@Code='SP996',@RequestId=@id,@Recipe=@r,@Name=N'Concurrent recipe v3',@Category='DM001',@Price=10000,@Status=N'Dang ban';")
 # Actual baseline checkout versus recipe writer, both directions.
 Run-TestSql $control "INSERT dbo.HOADON(MaHD,MaNV,LoaiDon) VALUES('HD09999003','NV001',N'Mang di'),('HD09999004','NV001',N'Mang di');INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan) VALUES('HD09999003','SP996',1,10000),('HD09999004','SP996',1,10000);"
 $rev=Read-TestScalar $control "SELECT CONVERT(varchar(18),CONVERT(binary(8),Revision),1) FROM dbo.SANPHAM WHERE MaSP='SP996'"
 Two T10-K08-checkout-first "DECLARE @tt dbo.DanhSachThanhToan;INSERT @tt(MaPT,SoTien)VALUES('PT001',10000);BEGIN TRAN;EXEC dbo.sp_ThanhToanHoaDon 'HD09999003',@tt;" "DECLARE @r binary(8)=$rev;DECLARE @recipe dbo.RecipeLineInput;INSERT @recipe VALUES('NVL01',0.03),('NVL02',0.04);EXEC dbo.sp_ProductReplaceRecipe @a,@s,'SP996',@id,@recipe,@r;" 0 "IF (SELECT SoLuong FROM dbo.TIEUHAO_HOADON WHERE MaHD='HD09999003' AND MaNVL='NVL01')<>0.0180 OR (SELECT SoLuong FROM dbo.TIEUHAO_HOADON WHERE MaHD='HD09999003' AND MaNVL='NVL02')<>0.0200 THROW 52999,'Mixed recipe at checkout',1;"
 $rev=Read-TestScalar $control "SELECT CONVERT(varchar(18),CONVERT(binary(8),Revision),1) FROM dbo.SANPHAM WHERE MaSP='SP996'"
 Two T10-K08-writer-first "DECLARE @r binary(8)=$rev;DECLARE @recipe dbo.RecipeLineInput;INSERT @recipe VALUES('NVL01',0.05),('NVL02',0.06);BEGIN TRAN;EXEC dbo.sp_ProductReplaceRecipe @a,@s,'SP996',@id,@recipe,@r;" "DECLARE @tt dbo.DanhSachThanhToan;INSERT @tt(MaPT,SoTien)VALUES('PT001',10000);EXEC dbo.sp_ThanhToanHoaDon 'HD09999004',@tt;" 0 "IF (SELECT SoLuong FROM dbo.TIEUHAO_HOADON WHERE MaHD='HD09999004' AND MaNVL='NVL01')<>0.05 OR (SELECT SoLuong FROM dbo.TIEUHAO_HOADON WHERE MaHD='HD09999004' AND MaNVL='NVL02')<>0.06 THROW 52999,'Recipe writer not atomic',1;"
}finally{$control.Dispose()}


