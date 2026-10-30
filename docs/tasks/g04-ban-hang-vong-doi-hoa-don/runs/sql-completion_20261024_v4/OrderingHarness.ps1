$ErrorActionPreference = 'Stop'
$root = 'D:\Workspace\Nam_3\HK1\CoffeeManager'
. "$root/tools/database/DatabaseTestTools.ps1"
$out = $PSScriptRoot
$runId = (Get-Date -Format 'yyyyMMdd_HHmmss') + '_' + [guid]::NewGuid().ToString('N').Substring(0,8)
$m = [ordered]@{RunId=$runId;Approval='User24/10/2026: kiểm phần agent; existing approved G04 test';Schema=16;Created=@();Cleaned=@();Retained=@();Cases=@();Status='Running'}
function Save { $m | ConvertTo-Json -Depth 14 | Set-Content "$out/ordering-manifest.json" -Encoding utf8 }
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
  for($v=1;$v -le $through;$v++) { $f=@(Get-ChildItem "$root/database/migrations" -Filter ('{0:d3}_*.sql' -f $v)); & "$root/tools/database/Apply-Migration.ps1" -Database $name -Version $v -File $f[0].FullName | ConvertTo-Json -Compress | Add-Content "$out/ordering-$mode-migrations.jsonl" }
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

