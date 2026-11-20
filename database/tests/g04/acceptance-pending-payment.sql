-- T33 subcases for the pending-payment lifecycle; run only after separate approval in an owned G04 test DB.
-- Not a claim of whole-checklist PASS; PP12 (UI) stays manual.
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G04_%' OR OBJECT_ID('dbo.G04TestMarker') IS NULL THROW 52107,'Owned test database required',1;
UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV IN ('NV001','NV004');
DECLARE @actor int,@stamp binary(8),@cash int,@cashStamp binary(8),@id uniqueidentifier=NEWID(),@code varchar(10),@rev binary(8),@revOld binary(8);
SELECT @actor=MaTK,@stamp=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
SELECT @cash=MaTK,@cashStamp=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV004';
DECLARE @baseOpen int=(SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai IN (N'Chua thanh toan',N'Cho thanh toan'));
DECLARE @baseRevenue decimal(38,0)=(SELECT COALESCE(SUM(CONVERT(decimal(38,0),ThanhTien)),0) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND NgayThanhToan>=CONVERT(date,SYSDATETIME()) AND NgayThanhToan<DATEADD(day,1,CONVERT(datetime2(7),CONVERT(date,SYSDATETIME()))));
DECLARE @codes TABLE(Loai varchar(20),Ma char(10));
DECLARE @preview TABLE(Code char(10),Revision binary(8),Total decimal(18,0),Ship decimal(18,0),Discount decimal(18,0),Payable decimal(38,0),Source varchar(20),PercentDiscount int,PartnerPercent decimal(5,2),PartnerFee decimal(18,0),Points bigint,PreviewToken binary(32));
DECLARE @payments dbo.DanhSachThanhToan;

-- PP01: đơn tại bàn có món chuyển đúng sang Chờ thanh toán, giữ receipt và tiền chưa thu.
INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes; SET @id=NEWID();
EXEC dbo.sp_TaoHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@RequestId=@id,@Channel=N'Tai quan',@TableCode='B0002',@Product='SP001',@Quantity=1;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Add',@Product='SP001',@Quantity=1;
SELECT @revOld=Revision FROM dbo.HOADON WHERE MaHD=@code; SET @id=NEWID();
EXEC dbo.sp_YeuCauThanhToan @actor,@stamp,@code,@revOld,@id;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TrangThai=N'Cho thanh toan' AND NgayThanhToan IS NULL AND Revision<>@revOld)
 THROW 52901,'PP01 status/revision mismatch',1;
IF NOT EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@id AND Operation='SalesRequestPay' AND EntityCode=@code AND ActorAccountId=@actor)
 THROW 52901,'PP01 receipt mismatch',1;
IF EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@code) OR EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WHERE MaHD=@code)
 THROW 52901,'PP01 money/consumption leaked at request',1;
DECLARE @pp01 char(10)=@code;

-- PP10: replay đúng RequestId không đổi trạng thái/revision lần hai.
DECLARE @revAfterRequest binary(8)=(SELECT Revision FROM dbo.HOADON WHERE MaHD=@code);
EXEC dbo.sp_YeuCauThanhToan @actor,@stamp,@code,@revOld,@id;
IF (SELECT Revision FROM dbo.HOADON WHERE MaHD=@code)<>@revAfterRequest
 OR (SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE Operation='SalesRequestPay' AND EntityCode=@code)<>1
 THROW 52901,'PP10 replay changed state',1;

-- PP03 (sửa 20/11/2026 — requirements-cho-thanh-toan-phien-ban.md §9): đơn Chờ vẫn nối thêm/tăng SL
-- và F3, nhưng khóa xóa/giảm/đổi ghi chú dòng (52110); thao tác lỗi không ghi một phần.
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Add',@Product='SP001',@Quantity=1;
IF (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@code)<>3 THROW 52901,'PP03 add in Cho missing',1;
DECLARE @pp03Line int=(SELECT MIN(MaDong) FROM dbo.CHITIET_HD WHERE MaHD=@code),@pp03Qty int=(SELECT SoLuong FROM dbo.CHITIET_HD WHERE MaDong=(SELECT MIN(MaDong) FROM dbo.CHITIET_HD WHERE MaHD=@code)),@pp03Next int;
SET @pp03Next=@pp03Qty+1;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Update',@LineId=@pp03Line,@Quantity=@pp03Next;
IF (SELECT SoLuong FROM dbo.CHITIET_HD WHERE MaDong=@pp03Line)<>@pp03Next THROW 52901,'PP03 update increase missing',1;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Update',@LineId=@pp03Line,@Quantity=@pp03Qty;
 THROW 52901,'PP03 decrease accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52110 THROW; END CATCH;
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Delete',@LineId=@pp03Line;
 THROW 52901,'PP03 delete accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52110 THROW; END CATCH;
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Update',@LineId=@pp03Line,@Quantity=@pp03Next,@Note=N'it da';
 THROW 52901,'PP03 note edit accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52110 THROW; END CATCH;
IF (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@code)<>3
 OR (SELECT SoLuong FROM dbo.CHITIET_HD WHERE MaDong=@pp03Line)<>@pp03Qty+1
 OR EXISTS(SELECT 1 FROM dbo.CHITIET_HD WHERE MaDong=@pp03Line AND GhiChu IS NOT NULL)
 THROW 52901,'PP03 blocked edit leaked',1;
SET @id=NEWID(); EXEC dbo.sp_SuaThongTinHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Customer='KH001';
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND MaKH='KH001')
 THROW 52901,'PP03 header edit in Cho missing',1;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON h CROSS APPLY dbo.fn_DiscountPreview(h.TongTien,h.PhiShip,CONVERT(date,h.NgayLap),h.MaKH,h.MaKM) p
              WHERE h.MaHD=@code AND h.GiamGia=p.Discount)
 OR (SELECT TongTien FROM dbo.HOADON WHERE MaHD=@code)<>(SELECT SUM(CONVERT(decimal(18,0),SoLuong)*GiaBan) FROM dbo.CHITIET_HD WHERE MaHD=@code)
 THROW 52901,'PP03 discount/total not recalculated over whole order',1;

-- PP07: bàn đang chờ hiện đúng trên sơ đồ và không nhận đơn thứ hai.
IF NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0002' AND TrangThai=N'Cho thanh toan')
 THROW 52901,'PP07 view status mismatch',1;
BEGIN TRY
 DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; DECLARE @dup char(10); SELECT @dup=Ma FROM @codes; SET @id=NEWID();
 EXEC dbo.sp_TaoHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@dup,@RequestId=@id,@Channel=N'Tai quan',@TableCode='B0002',@Product='SP001',@Quantity=1;
 THROW 52901,'PP07 duplicate open order accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52104 THROW; END CATCH;

-- PP08: danh sách đơn mở và lịch sử lọc theo Chờ thanh toán trả đúng.
DECLARE @open TABLE(Code char(10),Created datetime2(7),Channel nvarchar(20),TableCode char(5),Customer nvarchar(100),Payable decimal(18,0),Status nvarchar(20),Delivery nvarchar(20),Revision binary(8),TotalCount int);
INSERT @open EXEC dbo.sp_SalesOpenOrders @actor,@stamp;
IF NOT EXISTS(SELECT 1 FROM @open WHERE Code=@pp01 AND Status=N'Cho thanh toan') THROW 52901,'PP08 open list missing Cho',1;
DECLARE @invoices TABLE(Code char(10),Created datetime2(7),Channel nvarchar(20),TableCode char(5),Customer nvarchar(100),Payable decimal(18,0),Status nvarchar(20),Delivery nvarchar(20),Revision binary(8),TotalCount int);
DECLARE @fromDay date=CONVERT(date,SYSDATETIME());
INSERT @invoices EXEC dbo.sp_InvoiceList @actor,@stamp,'Created',@fromDay,@fromDay,@Status=N'Cho thanh toan',@Channel=NULL,@Customer=NULL,@Search=N'',@Skip=0;
IF NOT EXISTS(SELECT 1 FROM @invoices WHERE Code=@pp01 AND Status=N'Cho thanh toan') THROW 52901,'PP08 invoice filter missing Cho',1;
EXEC dbo.sp_SalesOrderGet @actor,@stamp,@pp01;
EXEC dbo.sp_InvoiceDetail @actor,@stamp,@pp01;

-- PP04: thu tiền từ Chờ thanh toán đủ tiền/kho/điểm/snapshot.
DECLARE @custBalance int=(SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH001');
DECLARE @stockBefore nvarchar(max)=(SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH);
DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes; SET @id=NEWID();
EXEC dbo.sp_TaoHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@RequestId=@id,@Channel=N'Mang di',@Customer='KH001',@Product='SP001',@Quantity=2;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
DECLARE @totalBefore decimal(18,0)=(SELECT TongTien FROM dbo.HOADON WHERE MaHD=@code),@discountBefore decimal(18,0)=(SELECT GiamGia FROM dbo.HOADON WHERE MaHD=@code);
SET @id=NEWID(); EXEC dbo.sp_YeuCauThanhToan @actor,@stamp,@code,@rev,@id;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
IF (SELECT TongTien FROM dbo.HOADON WHERE MaHD=@code)<>@totalBefore OR (SELECT GiamGia FROM dbo.HOADON WHERE MaHD=@code)<>@discountBefore
 THROW 52901,'PP04 request changed money',1;
INSERT @preview EXEC dbo.sp_SalesCheckoutPreview @actor,@stamp,@code,@rev;
DECLARE @token binary(32),@payable decimal(38,0),@net decimal(18,0);
SELECT @token=PreviewToken,@payable=Payable FROM @preview;
INSERT @payments VALUES('PT001',@payable,NULL); SET @id=NEWID();
EXEC dbo.sp_ThanhToanHoaDon @actor,@stamp,@code,@rev,@id,@token,@payments;
SELECT @net=TongTien-GiamGia FROM dbo.HOADON WHERE MaHD=@code;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TrangThai=N'Da thanh toan' AND NgayThanhToan IS NOT NULL)
 OR (SELECT SUM(SoTien) FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@code)<>@payable
 THROW 52901,'PP04 checkout from Cho mismatch',1;
IF (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH001')<>@custBalance+CONVERT(int,FLOOR(@net/1000))
 THROW 52901,'PP04 points mismatch',1;
IF @stockBefore=(SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH)
 THROW 52901,'PP04 stock not deducted',1;
IF NOT EXISTS(SELECT 1 FROM dbo.HoaDonUuDaiSnapshot WHERE MaHD=@code AND Kind='Checkout') THROW 52901,'PP04 snapshot missing',1;

-- PP05: hủy từ Chờ thanh toán khi chưa thu/chưa trừ kho.
DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes; SET @id=NEWID();
EXEC dbo.sp_TaoHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@RequestId=@id,@Channel=N'Mang di',@Product='SP001',@Quantity=1;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_YeuCauThanhToan @actor,@stamp,@code,@rev,@id;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_HuyHoaDon @actor,@stamp,@code,@rev,@id;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TrangThai=N'Huy') THROW 52901,'PP05 cancel from Cho failed',1;

-- PP06: ADMIN mở lại có receipt; CASH bị 51605; revision cũ 52103; mở lại đơn không Chờ 52109.
DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes; SET @id=NEWID();
EXEC dbo.sp_TaoHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@RequestId=@id,@Channel=N'Mang di',@Product='SP001',@Quantity=1;
SELECT @revOld=Revision FROM dbo.HOADON WHERE MaHD=@code;
SET @id=NEWID(); EXEC dbo.sp_YeuCauThanhToan @actor,@stamp,@code,@revOld,@id;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_MoLaiHoaDon @cash,@cashStamp,@code,@rev,@id;
 THROW 52901,'PP06 CASH reopened',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>51605 THROW; END CATCH;
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_MoLaiHoaDon @actor,@stamp,@code,@revOld,@id;
 THROW 52901,'PP06 stale revision accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52103 THROW; END CATCH;
SET @id=NEWID(); EXEC dbo.sp_MoLaiHoaDon @actor,@stamp,@code,@rev,@id;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TrangThai=N'Chua thanh toan')
 OR NOT EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@id AND Operation='SalesReopen' AND EntityCode=@code)
 THROW 52901,'PP06 ADMIN reopen mismatch',1;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_MoLaiHoaDon @actor,@stamp,@code,@rev,@id;
 THROW 52901,'PP06 reopen non-pending accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52109 THROW; END CATCH;
SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Add',@Product='SP001',@Quantity=1;
IF (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@code)<>2 THROW 52901,'PP06 edit after reopen failed',1;

-- PP02: đơn rỗng bị 52108, trạng thái/revision giữ nguyên, transaction sạch.
DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes; SET @id=NEWID();
EXEC dbo.sp_TaoHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@RequestId=@id,@Channel=N'Mang di',@Product='SP001',@Quantity=1;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
DECLARE @line int=(SELECT MIN(MaDong) FROM dbo.CHITIET_HD WHERE MaHD=@code);
SET @id=NEWID(); EXEC dbo.sp_SuaDongHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@ExpectedRevision=@rev,@RequestId=@id,@Action='Delete',@LineId=@line;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
DECLARE @extrId uniqueidentifier=NEWID();
BEGIN TRY
 EXEC dbo.sp_YeuCauThanhToan @actor,@stamp,@code,@rev,@extrId;
 THROW 52901,'PP02 empty order accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52108 THROW; END CATCH;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND TrangThai=N'Chua thanh toan' AND Revision=@rev)
 OR EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@extrId)
 THROW 52901,'PP02 failed request leaked',1;

-- PP09: số đếm đơn mở/tổng quan gồm Chờ thanh toán; doanh thu chỉ tăng theo hóa đơn đã thu.
-- sp_DashboardAnalyticsSources từ chối chạy khi @@TRANCOUNT<>0; INSERT…EXEC mở transaction ngầm
-- nên không thu được resultset ở SQL subset. Kiểm qua sp_ReportDashboard và ghi phần nguồn là giới hạn.
DECLARE @dashboard TABLE(BusinessDate date,ReadAt datetime2(7),Revenue decimal(38,0),OpenOrders int,Debt decimal(38,0));
INSERT @dashboard EXEC dbo.sp_ReportDashboard @actor,@stamp;
IF (SELECT OpenOrders FROM @dashboard)<>@baseOpen+3
 THROW 52901,'PP09 open counts mismatch',1;
IF (SELECT Revenue FROM @dashboard)<>@baseRevenue+@payable
 THROW 52901,'PP09 revenue must only include paid checkout',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 52901,'Transaction not clean',1;
