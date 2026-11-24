-- 04 (PP08): regression sau bộ chọn mới: dữ liệu seed, tồn kho, điểm, doanh thu, snapshot khóa.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_PromoOnePerItem[_]%' OR OBJECT_ID('dbo.PromoOnePerItemTestMarker') IS NULL
 THROW 53805,N'Chỉ DB test PromoOnePerItem có marker.',1;
IF OBJECT_ID('dbo.PromoOnePerItemBaseline') IS NULL THROW 53805,N'Thiếu baseline fixture.',1;
DECLARE @today date=CONVERT(date,SYSDATETIME());
DECLARE @base TABLE(Name varchar(25) PRIMARY KEY,Value decimal(38,4));
INSERT @base SELECT Name,Value FROM dbo.PromoOnePerItemBaseline;

-- Dữ liệu seed trước hôm nay không đổi.
IF (SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today)
 <> (SELECT Value FROM @base WHERE Name='SeedPaidCount')
 OR (SELECT COALESCE(SUM(CONVERT(decimal(38,4),TongTien)),0) FROM dbo.HOADON
     WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today)
 <> (SELECT Value FROM @base WHERE Name='SeedPaidGross')
 OR (SELECT COALESCE(SUM(CONVERT(decimal(38,4),GiamGia)),0) FROM dbo.HOADON
     WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today)
 <> (SELECT Value FROM @base WHERE Name='SeedPaidDiscount')
 OR (SELECT COALESCE(SUM(CONVERT(decimal(38,4),ThanhTien)),0) FROM dbo.HOADON
     WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today)
 <> (SELECT Value FROM @base WHERE Name='SeedPaidAmount')
 THROW 53805,N'PP08: hóa đơn seed bị đổi.',1;
IF (SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai=N'Chua thanh toan')
 <> (SELECT Value FROM @base WHERE Name='OpenOrderCount')
 THROW 53805,N'PP08: số đơn mở cuối cùng phải bằng baseline.',1;

-- Hai đơn test hôm nay: tổng 300 000, giảm 86 600, thu 213 400.
IF (SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)=@today)<>2
 OR (SELECT SUM(CONVERT(decimal(38,4),TongTien)) FROM dbo.HOADON
     WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)=@today)<>300000
 OR (SELECT SUM(CONVERT(decimal(38,4),GiamGia)) FROM dbo.HOADON
     WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)=@today)<>86600
 OR (SELECT SUM(CONVERT(decimal(38,4),ThanhTien)) FROM dbo.HOADON
     WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)=@today)<>213400
 THROW 53805,N'PP08: doanh thu hai đơn test sai.',1;

-- Snapshot khóa đúng: tổng KM đã áp bằng PromotionDiscount của snapshot.
IF EXISTS(SELECT 1 FROM dbo.HOADON h JOIN dbo.HoaDonUuDaiSnapshot s ON s.MaHD=h.MaHD
 WHERE h.TrangThai=N'Da thanh toan' AND CONVERT(date,h.NgayThanhToan)=@today
   AND s.Discount<>s.TierDiscount+s.PromotionDiscount)
 OR EXISTS(SELECT 1 FROM dbo.HOADON h JOIN dbo.HoaDonUuDaiSnapshot s ON s.MaHD=h.MaHD
 WHERE h.TrangThai=N'Da thanh toan' AND CONVERT(date,h.NgayThanhToan)=@today
   AND s.PromotionDiscount<>(SELECT COALESCE(SUM(a.Discount),0) FROM dbo.HoaDonUuDaiApDung a WHERE a.MaHD=h.MaHD))
 THROW 53805,N'PP08: snapshot không bằng tổng KM đã áp.',1;

-- Tồn kho: đơn 1 dùng 3 NVL93+1 NVL94, đơn legacy 1 NVL93+1 NVL94.
IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL93')
 <> (SELECT Value FROM @base WHERE Name='StockNVL93')-4
 OR (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL94')
 <> (SELECT Value FROM @base WHERE Name='StockNVL94')-2
 THROW 53805,N'PP08: tồn kho cuối sai (NVL93 -4, NVL94 -2).',1;
IF (SELECT SUM(SoLuong) FROM dbo.TIEUHAO_HOADON x JOIN dbo.HOADON h ON h.MaHD=x.MaHD
     WHERE h.TrangThai=N'Da thanh toan' AND CONVERT(date,h.NgayThanhToan)=@today AND x.MaNVL='NVL93')<>4
 OR (SELECT SUM(SoLuong) FROM dbo.TIEUHAO_HOADON x JOIN dbo.HOADON h ON h.MaHD=x.MaHD
     WHERE h.TrangThai=N'Da thanh toan' AND CONVERT(date,h.NgayThanhToan)=@today AND x.MaNVL='NVL94')<>2
 THROW 53805,N'PP08: bút toán tiêu hao hai đơn test sai.',1;

-- Điểm: KH932 +163, KH931 +50; mỗi đơn đúng một dòng quà.
IF (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH932')<>(SELECT Value FROM @base WHERE Name='PointsKH932')+163
 OR (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH931')<>(SELECT Value FROM @base WHERE Name='PointsKH931')+50
 THROW 53805,N'PP08: điểm khách sau hai đơn sai.',1;
IF EXISTS(SELECT 1 FROM(SELECT MaHD,COUNT(*) c FROM dbo.CHITIET_HD WHERE MaKMQuaTang IS NOT NULL
 GROUP BY MaHD) g WHERE g.c<>1)
 THROW 53805,N'PP08: mỗi hóa đơn chỉ có một dòng quà.',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 53805,N'Giao dịch không sạch.',1;
