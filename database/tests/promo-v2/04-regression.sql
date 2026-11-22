-- 04: hóa đơn seed trước 042 không đổi; báo cáo, tồn kho, điểm sau đơn có quà; KM 039 cũ vẫn áp.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_PromoV2[_]%' OR OBJECT_ID('dbo.PromoV2TestMarker') IS NULL
 THROW 53705,N'Chỉ DB test PromoV2 có marker.',1;
DECLARE @a int,@s binary(8),@today date=CONVERT(date,SYSDATETIME());
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';

-- 1) Hóa đơn seed (trả trước 042) giữ nguyên tổng/giảm/phải trả.
DECLARE @seedCount int,@seedGross decimal(38,0),@seedDiscount decimal(38,0),@seedAmount decimal(38,0);
SELECT @seedCount=COUNT(*),@seedGross=SUM(CONVERT(decimal(38,0),TongTien)),@seedDiscount=SUM(CONVERT(decimal(38,0),GiamGia)),
 @seedAmount=SUM(CONVERT(decimal(38,0),ThanhTien)) FROM dbo.HOADON
 WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today;
IF @seedCount<>(SELECT Value FROM dbo.PromoV2Baseline WHERE Name='SeedPaidCount')
 OR @seedGross<>(SELECT Value FROM dbo.PromoV2Baseline WHERE Name='SeedPaidGross')
 OR @seedDiscount<>(SELECT Value FROM dbo.PromoV2Baseline WHERE Name='SeedPaidDiscount')
 OR @seedAmount<>(SELECT Value FROM dbo.PromoV2Baseline WHERE Name='SeedPaidAmount')
 THROW 53705,N'Hóa đơn lịch sử bị đổi số sau 042.',1;
IF EXISTS(SELECT 1 FROM dbo.CHITIET_HD d JOIN dbo.HOADON h ON h.MaHD=d.MaHD
 WHERE d.MaKMQuaTang IS NOT NULL AND CONVERT(date,h.NgayThanhToan)<@today)
 THROW 53705,N'Hóa đơn cũ bị gắn dòng quà mới.',1;

-- 2) Chỉ hai hóa đơn test hôm nay phát sinh doanh thu: 100000/35000/65000 và 50000/25000/25000.
DECLARE @todayCount int,@todayGross decimal(38,0),@todayDiscount decimal(38,0),@todayAmount decimal(38,0);
SELECT @todayCount=COUNT(*),@todayGross=SUM(CONVERT(decimal(38,0),TongTien)),@todayDiscount=SUM(CONVERT(decimal(38,0),GiamGia)),
 @todayAmount=SUM(CONVERT(decimal(38,0),ThanhTien)) FROM dbo.HOADON
 WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)=@today;
IF @todayCount<>2 OR @todayGross<>150000 OR @todayDiscount<>60000 OR @todayAmount<>90000
 THROW 53705,N'Doanh thu hóa đơn test không khớp 150000/60000/90000.',1;

-- 3) Quà không tạo doanh thu món và không đổi tổng đơn.
IF EXISTS(SELECT 1 FROM dbo.CHITIET_HD d JOIN dbo.HOADON h ON h.MaHD=d.MaHD
 WHERE d.MaKMQuaTang IS NOT NULL AND CONVERT(date,h.NgayThanhToan)=@today
 AND (d.GiaBan<>0 OR CONVERT(decimal(38,0),d.SoLuong)*d.GiaBan<>0))
 THROW 53705,N'Dòng quà phải giá 0 và không vào doanh thu.',1;
IF EXISTS(SELECT 1 FROM dbo.HOADON h WHERE h.TrangThai=N'Da thanh toan' AND CONVERT(date,h.NgayThanhToan)=@today
 AND h.TongTien<>COALESCE((SELECT SUM(CONVERT(decimal(38,0),d.SoLuong)*d.GiaBan) FROM dbo.CHITIET_HD d WHERE d.MaHD=h.MaHD),0))
 THROW 53705,N'Tổng tiền hóa đơn không khớp các dòng.',1;
IF (SELECT COUNT(*) FROM dbo.HoaDonUuDaiSnapshot s JOIN dbo.HOADON h ON h.MaHD=s.MaHD
 WHERE CONVERT(date,h.NgayThanhToan)=@today AND s.Kind='Checkout')<>2
 THROW 53705,N'Thiếu snapshot ưu đãi cho hóa đơn test.',1;

-- 4) Điểm: chỉ cộng cho tiền món bán sau giảm; quà không cộng.
IF (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH922')
 <> (SELECT Value FROM dbo.PromoV2Baseline WHERE Name='PointsKH922')+25
 OR (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH923')
 <> (SELECT Value FROM dbo.PromoV2Baseline WHERE Name='PointsKH923')
 OR (SELECT COUNT(*) FROM dbo.LICHSU_DIEM WHERE MaKH='KH922')<>1
 THROW 53705,N'Điểm sau đơn có quà sai.',1;

-- 5) Tồn kho = baseline - tiêu hao của đơn test đã trả của hôm nay.
DECLARE @use91 decimal(38,4)=COALESCE((SELECT SUM(t.SoLuong) FROM dbo.TIEUHAO_HOADON t JOIN dbo.HOADON h ON h.MaHD=t.MaHD
 WHERE t.MaNVL='NVL91' AND h.TrangThai=N'Da thanh toan' AND CONVERT(date,h.NgayThanhToan)=@today),0);
DECLARE @use92 decimal(38,4)=COALESCE((SELECT SUM(t.SoLuong) FROM dbo.TIEUHAO_HOADON t JOIN dbo.HOADON h ON h.MaHD=t.MaHD
 WHERE t.MaNVL='NVL92' AND h.TrangThai=N'Da thanh toan' AND CONVERT(date,h.NgayThanhToan)=@today),0);
IF @use91<>3 OR @use92<>9 THROW 53705,N'Tiêu hao hôm nay phải là NVL91 3, NVL92 9.',1;
IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL91')<>(SELECT Value FROM dbo.PromoV2Baseline WHERE Name='StockNVL91')-@use91
 OR (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL92')<>(SELECT Value FROM dbo.PromoV2Baseline WHERE Name='StockNVL92')-@use92
 THROW 53705,N'Tồn kho lệch khỏi tiêu hao hóa đơn test.',1;
IF EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE SoLuongTon<0) THROW 53705,N'Tồn kho âm.',1;

-- 6) Báo cáo chạy được trên dữ liệu sau quà; nguồn tiền vẫn là hóa đơn đã trả.
EXEC dbo.sp_ReportRead @Actor=@a,@Stamp=@s,@Kind='SalesDay',@From=@today,@To=@today;
EXEC dbo.sp_ReportRead @Actor=@a,@Stamp=@s,@Kind='Products',@From=@today,@To=@today;
EXEC dbo.sp_ReportRead @Actor=@a,@Stamp=@s,@Kind='Payments',@From=@today,@To=@today;
EXEC dbo.sp_ReportRead @Actor=@a,@Stamp=@s,@Kind='Inventory',@From=@today,@To=@today;

-- 7) KM 039 cũ (ALL, không quà) sau chuẩn hóa ngày vẫn áp đúng, cộng hạng như 038.
--    Tháng 6: KM24SAL 10% + KM25SAL 10%; KH001 hạng BAC 3%: 23% của 25000 = 5750.
DECLARE @tier int,@pp int,@td decimal(18,0),@pd decimal(18,0),@discount decimal(18,0);
SELECT @tier=TierPercent,@pp=PromotionPercent,@td=TierDiscount,@pd=PromotionDiscount,@discount=Discount
 FROM dbo.fn_UuDaiTong('2027-06-15','KH001',NULL,N'[{"Product":"SP001","Amount":25000}]');
IF @discount<>5750 OR @tier<>3 OR @pp<>20 OR @td<>750 OR @pd<>5000
 OR (SELECT COUNT(*) FROM dbo.fn_UuDaiChia('2027-06-15','KH001',NULL,N'[{"Product":"SP001","Amount":25000}]'))<>2
 THROW 53705,N'KM seed 039 không còn áp đúng sau 042.',1;
IF dbo.fn_NgayTrongKy('2027-06-15',(SELECT NgayBD FROM dbo.KHUYENMAI WHERE MaKM='KM24SAL'),
 (SELECT NgayKT FROM dbo.KHUYENMAI WHERE MaKM='KM24SAL'))<>1
 THROW 53705,N'KM seed 039 không lặp hằng năm.',1;

-- 8) Contract hiện hành.
IF NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=42)
 OR OBJECT_ID('dbo.fn_DiscountPreview') IS NOT NULL
 OR OBJECT_ID('dbo.sp_DiscountPreview') IS NULL OR OBJECT_ID('dbo.KHUYENMAI_MON_TANG') IS NULL
 OR COL_LENGTH('dbo.CHITIET_HD','MaKMQuaTang') IS NULL
 THROW 53705,N'Thiếu/sai contract 042.',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 53705,N'Giao dịch không sạch.',1;
