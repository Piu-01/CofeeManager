-- CASE T02-K01-catalog | Exact baseline object counts and enabled trusted constraints | 0 |
-- ACT
IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0)<>35 THROW 52001,'Expected 35 tables',1;
IF (SELECT COUNT(*) FROM sys.views WHERE is_ms_shipped=0)<>2 THROW 52001,'Expected 2 views',1;
IF (SELECT COUNT(*) FROM sys.procedures WHERE is_ms_shipped=0)<>4 THROW 52001,'Expected 4 procedures',1;
IF (SELECT COUNT(*) FROM sys.table_types WHERE is_user_defined=1)<>1 THROW 52001,'Expected 1 table type',1;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) THROW 52001,'Untrusted/disabled FK',1;
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1) THROW 52001,'Untrusted/disabled CHECK',1;
IF EXISTS(SELECT 1 FROM sys.triggers WHERE is_disabled=1) THROW 52001,'Disabled trigger',1;
-- CASE T02-K01-modules | SQL modules resolve dependencies and compile on engine | 0 |
-- ACT
DECLARE @name NVARCHAR(776), @definition NVARCHAR(MAX);
DECLARE modules CURSOR LOCAL FAST_FORWARD FOR
SELECT QUOTENAME(OBJECT_SCHEMA_NAME(object_id))+N'.'+QUOTENAME(OBJECT_NAME(object_id)),definition
FROM sys.sql_modules WHERE OBJECTPROPERTY(object_id,'IsMSShipped')=0;
OPEN modules;
FETCH NEXT FROM modules INTO @name,@definition;
WHILE @@FETCH_STATUS=0
BEGIN
    EXEC sys.sp_refreshsqlmodule @name;
    FETCH NEXT FROM modules INTO @name,@definition;
END;
CLOSE modules; DEALLOCATE modules;
SELECT * FROM dbo.vw_TrangThaiBan;
SELECT * FROM dbo.vw_CongNoNCC;
-- CASE T02-K02-seed | Seed commits 10 bills 18 lines 9 paid and one open table B0003 | 0 |
-- ACT
IF (SELECT COUNT(*) FROM dbo.HOADON)<>10 THROW 52002,'Expected 10 invoices',1;
IF (SELECT COUNT(*) FROM dbo.CHITIET_HD)<>18 THROW 52002,'Expected 18 lines',1;
IF (SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan')<>9 THROW 52002,'Expected 9 paid',1;
IF (SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai=N'Chua thanh toan')<>1 THROW 52002,'Expected 1 open',1;
IF NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0003' AND MaHD='HD20250410' AND TrangThai=N'Dang phuc vu') THROW 52002,'Wrong open table',1;
IF EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan<>'B0003' AND TrangThai<>N'Trong') THROW 52002,'Unexpected occupied table',1;
-- CASE T02-K03-money | Sample bills and split payments match explicit amounts | 0 |
-- ACT
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='HD20250401' AND TongTien=125000 AND GiamGia=12500 AND ThanhTien=112500) THROW 52003,'HD01 mismatch',1;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD='HD20250409' AND ThanhTien=81000) THROW 52003,'HD09 mismatch',1;
IF (SELECT COUNT(*) FROM dbo.CHITIET_THANHTOAN WHERE MaHD='HD20250409')<>2 THROW 52003,'Expected 2 allocations',1;
IF NOT EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaHD='HD20250409' AND MaPT='PT002' AND SoTien=30000) THROW 52003,'Missing 30000',1;
IF NOT EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaHD='HD20250409' AND MaPT='PT005' AND SoTien=51000) THROW 52003,'Missing 51000',1;
-- CASE T02-K04-invariants | Every invoice receipt allocation stock and customer tier is consistent | 0 |
-- ACT
IF EXISTS(SELECT 1 FROM dbo.HOADON h WHERE h.TongTien<>COALESCE((SELECT SUM(CONVERT(DECIMAL(18,0),SoLuong)*GiaBan) FROM dbo.CHITIET_HD WHERE MaHD=h.MaHD),0)) THROW 52004,'Invoice total mismatch',1;
IF EXISTS(SELECT 1 FROM dbo.HOADON h WHERE h.TrangThai=N'Da thanh toan' AND h.ThanhTien<>COALESCE((SELECT SUM(SoTien) FROM dbo.CHITIET_THANHTOAN WHERE MaHD=h.MaHD),0)) THROW 52004,'Payment mismatch',1;
IF EXISTS(SELECT 1 FROM dbo.PHIEUNHAP p WHERE p.TongTien<>COALESCE((SELECT SUM(ROUND(SoLuong*DonGiaNhap,0)) FROM dbo.CHITIET_PN WHERE MaPN=p.MaPN),0)) THROW 52004,'Receipt total mismatch',1;
IF EXISTS(SELECT 1 FROM dbo.vw_CongNoNCC WHERE ConNo<0) THROW 52004,'Negative debt',1;
IF EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE SoLuongTon<0) THROW 52004,'Negative stock',1;
IF EXISTS(SELECT 1 FROM dbo.KHACHHANG k CROSS APPLY(SELECT TOP(1) MaHang FROM dbo.HANGTHANHVIEN WHERE DiemToiThieu<=k.DiemTichLuy ORDER BY DiemToiThieu DESC) h WHERE k.MaHang<>h.MaHang) THROW 52004,'Tier mismatch',1;
IF (SELECT COUNT(*) FROM dbo.TIEUHAO_HOADON)=0 THROW 52004,'Missing consumption snapshots',1;
