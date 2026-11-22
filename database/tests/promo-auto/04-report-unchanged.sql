-- 04: dữ liệu lịch sử/báo cáo không đổi số do ưu đãi tự áp; tồn kho chỉ đổi theo tiêu hao.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_PromoAuto[_]%' OR OBJECT_ID('dbo.PromoAutoTestMarker') IS NULL
 THROW 53705,N'Chỉ DB test PromoAuto có marker.',1;
DECLARE @a int,@s binary(8),@today date=CONVERT(date,SYSDATETIME());
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';

-- Hóa đơn seed (trước 039) phải giữ nguyên số tiền/giảm/trạng thái.
DECLARE @seedCount int,@seedGross decimal(38,0),@seedDiscount decimal(38,0),@seedAmount decimal(38,0);
SELECT @seedCount=COUNT(*),@seedGross=SUM(CONVERT(decimal(38,0),TongTien)),@seedDiscount=SUM(CONVERT(decimal(38,0),GiamGia)),
 @seedAmount=SUM(CONVERT(decimal(38,0),ThanhTien)) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today;
IF @seedCount<>(SELECT Value FROM dbo.PromoAutoBaseline WHERE Name='SeedPaidCount')
 OR @seedGross<>(SELECT Value FROM dbo.PromoAutoBaseline WHERE Name='SeedPaidGross')
 OR @seedDiscount<>(SELECT Value FROM dbo.PromoAutoBaseline WHERE Name='SeedPaidDiscount')
 OR @seedAmount<>(SELECT Value FROM dbo.PromoAutoBaseline WHERE Name='SeedPaidAmount')
 THROW 53705,N'Hóa đơn lịch sử bị đổi số sau 039.',1;
IF EXISTS(SELECT 1 FROM dbo.HOADON h JOIN dbo.HoaDonUuDaiApDung a ON a.MaHD=h.MaHD WHERE CONVERT(date,h.NgayThanhToan)<@today)
 THROW 53705,N'Hóa đơn cũ bị gắn danh sách ưu đãi mới.',1;

-- Chỉ hóa đơn test hôm nay phát sinh doanh thu mới, đúng 50000/15000/35000.
DECLARE @todayCount int,@todayGross decimal(38,0),@todayDiscount decimal(38,0),@todayAmount decimal(38,0);
SELECT @todayCount=COUNT(*),@todayGross=SUM(CONVERT(decimal(38,0),TongTien)),@todayDiscount=SUM(CONVERT(decimal(38,0),GiamGia)),
 @todayAmount=SUM(CONVERT(decimal(38,0),ThanhTien)) FROM dbo.HOADON
 WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)=@today;
IF @todayCount<>1 OR @todayGross<>50000 OR @todayDiscount<>15000 OR @todayAmount<>35000
 THROW 53705,N'Doanh thu hóa đơn test không khớp 50000/15000/35000.',1;

-- Báo cáo chạy được trên dữ liệu sau ưu đãi; nguồn tiền vẫn là hóa đơn đã trả.
EXEC dbo.sp_ReportRead @Actor=@a,@Stamp=@s,@Kind='SalesDay',@From=@today,@To=@today;
EXEC dbo.sp_ReportRead @Actor=@a,@Stamp=@s,@Kind='Products',@From=@today,@To=@today;
EXEC dbo.sp_ReportRead @Actor=@a,@Stamp=@s,@Kind='Inventory',@From=@today,@To=@today;

-- Tồn kho = baseline - tiêu hao của hóa đơn test (seed đã nằm trong baseline).
DECLARE @useNVL01 decimal(38,4)=COALESCE((SELECT SUM(CONVERT(decimal(38,4),t.SoLuong))
 FROM dbo.TIEUHAO_HOADON t JOIN dbo.HOADON h ON h.MaHD=t.MaHD
 WHERE t.MaNVL='NVL01' AND h.TrangThai=N'Da thanh toan' AND CONVERT(date,h.NgayThanhToan)=@today),0);
DECLARE @useNVL03 decimal(38,4)=COALESCE((SELECT SUM(CONVERT(decimal(38,4),t.SoLuong))
 FROM dbo.TIEUHAO_HOADON t JOIN dbo.HOADON h ON h.MaHD=t.MaHD
 WHERE t.MaNVL='NVL03' AND h.TrangThai=N'Da thanh toan' AND CONVERT(date,h.NgayThanhToan)=@today),0);
IF (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01')<>(SELECT Value FROM dbo.PromoAutoBaseline WHERE Name='StockNVL01')-@useNVL01
 OR (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL03')<>(SELECT Value FROM dbo.PromoAutoBaseline WHERE Name='StockNVL03')-@useNVL03
 THROW 53705,N'Tồn kho lệch khỏi tiêu hao hóa đơn test.',1;
IF EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE SoLuongTon<0) THROW 53705,N'Tồn kho âm.',1;

-- Không còn tham chiếu hàm cũ; hợp đồng hiện tại là procedure + bảng áp dụng.
IF OBJECT_ID('dbo.fn_DiscountPreview') IS NOT NULL THROW 53705,N'fn_DiscountPreview vẫn còn.',1;
IF OBJECT_ID('dbo.sp_DiscountPreview') IS NULL OR OBJECT_ID('dbo.HoaDonUuDaiApDung') IS NULL
 THROW 53705,N'Thiếu contract 039.',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 53705,N'Giao dịch không sạch.',1;
