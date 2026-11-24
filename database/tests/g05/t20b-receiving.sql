-- T20b: nhận hàng 2 bước. Chạy sau fixture.sql trên DB CoffeeManager_Test_G05_*.
-- Mỗi khối ghi G05AcceptanceLog; mọi thao tác lỗi được bắt riêng để tiếp tục các ca sau.
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G05_%' OR OBJECT_ID('dbo.G05TestMarker') IS NULL THROW 53900,N'Chỉ dùng fixture G05.',1;
DECLARE @admin int=(SELECT MIN(MaTK) FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1),
 @store int=(SELECT MIN(MaTK) FROM dbo.TAIKHOAN WHERE MaVT='STORE' AND HoatDong=1),
 @cash int=(SELECT MIN(MaTK) FROM dbo.TAIKHOAN WHERE MaVT='CASH' AND HoatDong=1),
 @as binary(8),@ss binary(8),@cs binary(8),
 @err int,@ok bit,@msg nvarchar(500),@code varchar(10),@code3 varchar(10),@code4 varchar(10),
 @id uniqueidentifier,@idCount uniqueidentifier,@idPost uniqueidentifier,@idCancel uniqueidentifier,
 @rev binary(8),@revDraft binary(8),
 @today datetime2(0)=CONVERT(datetime2(0),SYSDATETIME()),@tomorrow datetime2(0)=DATEADD(day,1,CONVERT(datetime2(0),SYSDATETIME())),
 @before1 decimal(18,4)=(SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01'),
 @before2 decimal(18,4)=(SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL02'),
 @before4 decimal(18,4)=(SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL04');
SELECT @as=SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=@admin;
SELECT @ss=SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=@store;
SELECT @cs=SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=@cash;
IF @admin IS NULL OR @store IS NULL OR @cash IS NULL THROW 53900,N'Thiếu actor fixture.',1;
DECLARE @lines dbo.PurchaseLinesInput;INSERT @lines VALUES('NVL01',10,10000),('NVL02',4,20000);
DECLARE @receive dbo.PurchaseReceiveInput;
/* ===== K01: lập phiếu Chờ nhận không đụng kho/chi/nợ, replay cùng request ===== */
SET @id=NEWID();
EXEC dbo.sp_TaoPhieuNhap @admin,@as,@id,'NCC01',@today,NULL,4000,180000,@lines;
SELECT @code=EntityCode FROM dbo.G03OperationReceipt WHERE RequestId=@id;
SELECT @revDraft=Revision FROM dbo.PHIEUNHAP WHERE MaPN=@code;
EXEC dbo.sp_TaoPhieuNhap @admin,@as,@id,'NCC01',@today,NULL,4000,180000,@lines;
SET @ok=CASE WHEN @code IS NOT NULL
 AND (SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE RequestId=@id)=1
 AND EXISTS(SELECT 1 FROM dbo.PHIEUNHAP WHERE MaPN=@code AND DaChot=0 AND DaHuy=0 AND TongTien=0 AND TrangThai='ChoNhan' AND TraNgayDuKien=4000 AND HanTraDuKien IS NULL)
 AND (SELECT COUNT(*) FROM dbo.CHITIET_PN WHERE MaPN=@code)=2 AND NOT EXISTS(SELECT 1 FROM dbo.CHITIET_PN WHERE MaPN=@code AND DaGhiKho=1)
 AND (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01')=@before1
 AND (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL02')=@before2
 AND NOT EXISTS(SELECT 1 FROM dbo.PHIEUTHUCHI WHERE MaPN=@code) AND NOT EXISTS(SELECT 1 FROM dbo.CONGNO_NCC WHERE MaPN=@code)
 THEN 1 ELSE 0 END;
EXEC dbo.sp_G05Assert 'T20b-K01.SQL-draft',@ok,N'Draft once, status ChoNhan, no stock/expense/debt.';
/* ===== K02: kiểm nhận STORE, chặn thiếu dòng/lệch thiếu lý do/vượt đặt/số lẻ/quyền ===== */
SET @err=NULL;INSERT @receive VALUES('NVL01',8,N'Thiếu 2');
SET @idCount=NEWID();
BEGIN TRY EXEC dbo.sp_LuuKiemNhan @store,@ss,@idCount,@code,@revDraft,@receive; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=52701 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K02.SQL-missing-line',@ok,@msg;
DELETE @receive;INSERT @receive VALUES('NVL01',12,N'Thừa'),('NVL02',4,NULL);SET @err=NULL;SET @idCount=NEWID();
BEGIN TRY EXEC dbo.sp_LuuKiemNhan @store,@ss,@idCount,@code,@revDraft,@receive; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=52704 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K02.SQL-over-ordered',@ok,@msg;
DELETE @receive;INSERT @receive VALUES('NVL01',7.5,N'Lẻ'),('NVL02',4,NULL);SET @err=NULL;SET @idCount=NEWID();
BEGIN TRY EXEC dbo.sp_LuuKiemNhan @store,@ss,@idCount,@code,@revDraft,@receive; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=52701 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K02.SQL-fraction',@ok,@msg;
DELETE @receive;INSERT @receive VALUES('NVL01',8,NULL),('NVL02',4,NULL);SET @err=NULL;SET @idCount=NEWID();
BEGIN TRY EXEC dbo.sp_LuuKiemNhan @store,@ss,@idCount,@code,@revDraft,@receive; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=52704 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K02.SQL-diff-reason',@ok,@msg;
DELETE @receive;INSERT @receive VALUES('NVL01',8,N'Thiếu 2'),('NVL02',4,NULL);SET @err=NULL;SET @idCount=NEWID();
BEGIN TRY EXEC dbo.sp_LuuKiemNhan @cash,@cs,@idCount,@code,@revDraft,@receive; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=51605 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K02.SQL-cash-denied',@ok,@msg;
SET @ok=CASE WHEN (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01')=@before1 THEN 1 ELSE 0 END;
EXEC dbo.sp_G05Assert 'T20b-K02.SQL-no-stock-before-count',@ok,N'Stock unchanged before valid count.';
EXEC dbo.sp_LuuKiemNhan @store,@ss,@idCount,@code,@revDraft,@receive;
SELECT @rev=Revision FROM dbo.PHIEUNHAP WHERE MaPN=@code;
EXEC dbo.sp_LuuKiemNhan @store,@ss,@idCount,@code,@revDraft,@receive;
SET @ok=CASE WHEN EXISTS(SELECT 1 FROM dbo.PHIEUNHAP WHERE MaPN=@code AND TrangThai='DaKiem' AND NgayKiem IS NOT NULL AND MaNVKiem=(SELECT MaNV FROM dbo.TAIKHOAN WHERE MaTK=@store))
 AND (SELECT SoLuongNhan FROM dbo.CHITIET_PN WHERE MaPN=@code AND MaNVL='NVL01')=8
 AND (SELECT SoLuongNhan FROM dbo.CHITIET_PN WHERE MaPN=@code AND MaNVL='NVL02')=4
 AND (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01')=@before1
 AND (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL02')=@before2
 AND (SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE RequestId=@idCount)=1 THEN 1 ELSE 0 END;
EXEC dbo.sp_G05Assert 'T20b-K02.SQL-count',@ok,N'Count stored, no stock, replay once.';
/* ===== K05: revision cũ sau khi kiểm phải bị chặn ===== */
SET @err=NULL;SET @idCount=NEWID();
BEGIN TRY EXEC dbo.sp_LuuKiemNhan @store,@ss,@idCount,@code,@revDraft,@receive; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=52706 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K05.SQL-stale-count',@ok,@msg;
/* ===== K03/K04/K08: STORE không chốt, revision cũ bị chặn, ADMIN chốt cộng tồn theo thực nhận ===== */
SET @err=NULL;SET @idPost=NEWID();
BEGIN TRY EXEC dbo.sp_ChotNhanHang @store,@ss,@idPost,@code,@rev,4000; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=51605 AND (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01')=@before1 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K04.SQL-store-cannot-post',@ok,@msg;
SET @err=NULL;SET @idPost=NEWID();
BEGIN TRY EXEC dbo.sp_ChotNhanHang @admin,@as,@idPost,@code,@revDraft,4000; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=52706 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K05.SQL-stale-post',@ok,@msg;
SET @idPost=NEWID();
EXEC dbo.sp_ChotNhanHang @admin,@as,@idPost,@code,@rev,4000;
EXEC dbo.sp_ChotNhanHang @admin,@as,@idPost,@code,@rev,4000;
SET @ok=CASE WHEN (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01')=@before1+8
 AND (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL02')=@before2+4
 AND EXISTS(SELECT 1 FROM dbo.PHIEUNHAP WHERE MaPN=@code AND TrangThai='DaChot' AND TongTien=160000)
 AND (SELECT COUNT(*) FROM dbo.PHIEUTHUCHI WHERE MaPN=@code AND SoTien=4000)=1
 AND (SELECT COUNT(*) FROM dbo.CONGNO_NCC WHERE MaPN=@code AND TienNo=156000)=1
 AND NOT EXISTS(SELECT 1 FROM dbo.CHITIET_PN WHERE MaPN=@code AND DaGhiKho=0)
 AND (SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE RequestId=@idPost)=1 THEN 1 ELSE 0 END;
EXEC dbo.sp_G05Assert 'T20b-K03.SQL-post',@ok,N'Stock +8/+4, total160000, expense4000, debt156000, replay once.';
SET @ok=CASE WHEN EXISTS(SELECT 1 FROM dbo.vw_ReportStockEvents e JOIN dbo.G03OperationReceipt r ON r.Operation='PurchaseReceive' AND r.EntityCode=e.Code
 WHERE e.Code=@code AND e.Material='NVL01' AND e.Quantity=8 AND e.At=r.CompletedAt AND e.FallbackTime=0) THEN 1 ELSE 0 END;
EXEC dbo.sp_G05Assert 'T20b-K08.SQL-stock-report',@ok,N'Stock report uses actual quantity and receive time.';
/* ===== K04: CASH/STORE bị chặn lập phiếu; CASH bị chặn danh sách ===== */
SET @err=NULL;SET @id=NEWID();
BEGIN TRY EXEC dbo.sp_TaoPhieuNhap @cash,@cs,@id,'NCC01',@today,NULL,0,10000,@lines; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=51605 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K04.SQL-cash-create',@ok,@msg;
SET @err=NULL;SET @id=NEWID();
BEGIN TRY EXEC dbo.sp_TaoPhieuNhap @store,@ss,@id,'NCC01',@today,NULL,0,10000,@lines; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=51605 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K04.SQL-store-create',@ok,@msg;
SET @err=NULL;
BEGIN TRY EXEC dbo.sp_PurchaseList @cash,@cs,N'',0,NULL; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=51605 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K04.SQL-cash-list',@ok,@msg;
/* ===== K06: huỷ phiếu chờ nhận, bắt buộc lý do, không đụng kho, phiếu huỷ bất biến ===== */
DECLARE @lines4 dbo.PurchaseLinesInput;INSERT @lines4 VALUES('NVL04',2,7000);
SET @id=NEWID();
EXEC dbo.sp_TaoPhieuNhap @admin,@as,@id,'NCC02',@today,NULL,0,14000,@lines4;
SELECT @code3=EntityCode FROM dbo.G03OperationReceipt WHERE RequestId=@id;
SELECT @rev=Revision FROM dbo.PHIEUNHAP WHERE MaPN=@code3;
SET @err=NULL;SET @idCancel=NEWID();
BEGIN TRY EXEC dbo.sp_HuyPhieuNhap @admin,@as,@idCancel,@code3,@rev,N'   '; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=52701 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K06.SQL-reason-required',@ok,@msg;
SET @err=NULL;SET @idCancel=NEWID();
BEGIN TRY EXEC dbo.sp_HuyPhieuNhap @cash,@cs,@idCancel,@code3,@rev,N'Test'; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=51605 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K06.SQL-cash-cancel',@ok,@msg;
SET @idCancel=NEWID();
EXEC dbo.sp_HuyPhieuNhap @admin,@as,@idCancel,@code3,@rev,N'Nhà cung cấp giao thiếu';
SET @ok=CASE WHEN EXISTS(SELECT 1 FROM dbo.PHIEUNHAP WHERE MaPN=@code3 AND TrangThai='DaHuy' AND LyDoHuy=N'Nhà cung cấp giao thiếu' AND NgayHuy IS NOT NULL)
 AND (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL04')=@before4 AND NOT EXISTS(SELECT 1 FROM dbo.PHIEUTHUCHI WHERE MaPN=@code3) AND NOT EXISTS(SELECT 1 FROM dbo.CONGNO_NCC WHERE MaPN=@code3) THEN 1 ELSE 0 END;
EXEC dbo.sp_G05Assert 'T20b-K06.SQL-cancel',@ok,N'Cancelled, no stock/expense/debt.';
SELECT @rev=Revision FROM dbo.PHIEUNHAP WHERE MaPN=@code3;
SET @err=NULL;SET @idCancel=NEWID();
BEGIN TRY EXEC dbo.sp_HuyPhieuNhap @admin,@as,@idCancel,@code3,@rev,N'Lần hai'; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=52706 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K06.SQL-double-cancel',@ok,@msg;
SET @err=NULL;
BEGIN TRY UPDATE dbo.CHITIET_PN SET LyDoLech=N'x' WHERE MaPN=@code3; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=52702 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K06.SQL-cancel-locked',@ok,@msg;
SET @err=NULL;SET @idCancel=NEWID();
BEGIN TRY EXEC dbo.sp_HuyPhieuNhap @admin,@as,@idCancel,@code,@revDraft,N'Sau chốt'; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=52706 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K06.SQL-posted-cancel',@ok,@msg;
/* ===== K07: dữ liệu cũ đã backfill đúng ===== */
SET @ok=CASE WHEN EXISTS(SELECT 1 FROM dbo.PHIEUNHAP WHERE CutoverKind='Legacy' AND DaChot=1)
 AND NOT EXISTS(SELECT 1 FROM dbo.CHITIET_PN d JOIN dbo.PHIEUNHAP p ON p.MaPN=d.MaPN WHERE p.CutoverKind='Legacy' AND p.DaChot=1 AND (d.SoLuongNhan IS NULL OR d.SoLuongNhan<>d.SoLuong OR d.DaGhiKho=0)) THEN 1 ELSE 0 END;
EXEC dbo.sp_G05Assert 'T20b-K07.SQL-legacy-backfill',@ok,N'Legacy lines backfilled SoLuongNhan=SoLuong, DaGhiKho=1.';
/* ===== K09: validate số nguyên/giá nguyên/trùng mã ===== */
DECLARE @bad dbo.PurchaseLinesInput;
INSERT @bad VALUES('NVL01',1.5,10000);SET @err=NULL;
BEGIN TRY EXEC dbo.sp_PurchasePreview @admin,@as,@bad; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=52701 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K09.SQL-frac-qty',@ok,@msg;
DELETE @bad;INSERT @bad VALUES('NVL01',1,10000.5);SET @err=NULL;
BEGIN TRY EXEC dbo.sp_PurchasePreview @admin,@as,@bad; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=52701 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K09.SQL-frac-price',@ok,@msg;
DELETE @bad;INSERT @bad VALUES('NVL01',1,10000),('NVL01',2,10000);SET @err=NULL;
BEGIN TRY EXEC dbo.sp_PurchasePreview @admin,@as,@bad; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=52701 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K09.SQL-duplicate',@ok,@msg;
/* ===== K10: chặn ngày tương lai, trả ngay vượt tổng, trạng thái danh sách ===== */
SET @err=NULL;SET @id=NEWID();
BEGIN TRY EXEC dbo.sp_TaoPhieuNhap @admin,@as,@id,'NCC01',@tomorrow,NULL,0,10000,@lines; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=52701 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K10.SQL-future-date',@ok,@msg;
SET @err=NULL;SET @id=NEWID();
BEGIN TRY EXEC dbo.sp_TaoPhieuNhap @admin,@as,@id,'NCC01',@today,NULL,180001,180000,@lines; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=52703 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K10.SQL-immediate-over',@ok,@msg;
DECLARE @lines3 dbo.PurchaseLinesInput;INSERT @lines3 VALUES('NVL03',1,5000);
DECLARE @receive3 dbo.PurchaseReceiveInput;INSERT @receive3 VALUES('NVL03',1,NULL);
DECLARE @before3 decimal(18,4)=(SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL03');
SET @id=NEWID();
EXEC dbo.sp_TaoPhieuNhap @admin,@as,@id,'NCC03',@today,NULL,0,5000,@lines3;
SELECT @code4=EntityCode FROM dbo.G03OperationReceipt WHERE RequestId=@id;
SELECT @rev=Revision FROM dbo.PHIEUNHAP WHERE MaPN=@code4;
SET @idCount=NEWID();EXEC dbo.sp_LuuKiemNhan @admin,@as,@idCount,@code4,@rev,@receive3;
SELECT @rev=Revision FROM dbo.PHIEUNHAP WHERE MaPN=@code4;
SET @err=NULL;SET @idPost=NEWID();
BEGIN TRY EXEC dbo.sp_ChotNhanHang @admin,@as,@idPost,@code4,@rev,6000; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=52703 THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K10.SQL-post-immediate-over',@ok,@msg;
EXEC dbo.sp_ChotNhanHang @admin,@as,@idPost,@code4,@rev,5000;
SET @ok=CASE WHEN (SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL03')=@before3+1 AND (SELECT COUNT(*) FROM dbo.PHIEUTHUCHI WHERE MaPN=@code4 AND SoTien=5000)=1 AND NOT EXISTS(SELECT 1 FROM dbo.CONGNO_NCC WHERE MaPN=@code4) THEN 1 ELSE 0 END;
EXEC dbo.sp_G05Assert 'T20b-K10.SQL-paid-full',@ok,N'Paid full: stock +1, one expense, no debt.';
SET @err=NULL;
BEGIN TRY EXEC dbo.sp_PurchaseList @admin,@as,N'',0,'Sai'; END TRY BEGIN CATCH SET @err=ERROR_NUMBER(); END CATCH;
SET @ok=CASE WHEN @err=52701 AND EXISTS(SELECT 1 FROM dbo.vw_PurchaseRead WHERE Code=@code AND TrangThai='DaChot') AND EXISTS(SELECT 1 FROM dbo.vw_PurchaseRead WHERE Code=@code3 AND TrangThai='DaHuy') AND EXISTS(SELECT 1 FROM dbo.vw_PurchaseRead WHERE Code=@code4 AND TrangThai='DaChot') THEN 1 ELSE 0 END;SET @msg=CONCAT(N'err=',ISNULL(CONVERT(nvarchar(10),@err),N'none'));
EXEC dbo.sp_G05Assert 'T20b-K10.SQL-status-filter',@ok,@msg;
SET @ok=CASE WHEN (SELECT COUNT(*) FROM dbo.PHIEUNHAP p WHERE p.TongTien<>(SELECT COALESCE(SUM(ROUND(COALESCE(d.SoLuongNhan,d.SoLuong)*d.DonGiaNhap,0)),0) FROM dbo.CHITIET_PN d WHERE d.MaPN=p.MaPN AND d.DaGhiKho=1))=0 THEN 1 ELSE 0 END;
EXEC dbo.sp_G05Assert 'T20b-K03.SQL-totals-intact',@ok,N'All posted totals match committed lines.';
SET @ok=CASE WHEN (SELECT COUNT(DISTINCT o.name) FROM sys.database_permissions pm JOIN sys.objects o ON o.object_id=pm.major_id JOIN sys.database_principals u ON u.principal_id=pm.grantee_principal_id
 WHERE u.name='CafeManagerApp' AND pm.permission_name='EXECUTE' AND o.name IN('sp_TaoPhieuNhap','sp_LuuKiemNhan','sp_ChotNhanHang','sp_HuyPhieuNhap','sp_PurchaseReceipt'))=5
 AND EXISTS(SELECT 1 FROM sys.database_permissions pm JOIN sys.types t ON t.user_type_id=pm.major_id JOIN sys.database_principals u ON u.principal_id=pm.grantee_principal_id WHERE u.name='CafeManagerApp' AND t.name='PurchaseReceiveInput') THEN 1 ELSE 0 END;
EXEC dbo.sp_G05Assert 'T20b-K04.SQL-grants',@ok,N'CafeManagerApp can execute new procedures and TVP.';
