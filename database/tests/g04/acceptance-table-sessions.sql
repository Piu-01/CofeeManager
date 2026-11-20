-- T34 subcases for table sessions / close table; run only after separate approval in an owned G04 test DB.
-- Not a claim of whole-checklist PASS. TS04 (two real connections) runs in the separate concurrency batch;
-- TS08/TS09/TS11 (dialog/POS/DPI) stay manual; this file covers the SQL-observable parts of TS01-TS03, TS05-TS07, TS10, TS12.
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G04_%' OR OBJECT_ID('dbo.G04TestMarker') IS NULL THROW 52107,'Owned test database required',1;
IF OBJECT_ID('dbo.PHIEN_BAN') IS NULL OR OBJECT_ID('dbo.sp_TraBan') IS NULL THROW 52903,'T34 objects missing',1;
UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV IN ('NV001','NV004');
DECLARE @actor int,@stamp binary(8),@id uniqueidentifier=NEWID(),@code varchar(10),@rev binary(8),@token binary(32),@amount decimal(18,0),@session int,@sessionRev binary(8),@closeRev binary(8);
SELECT @actor=MaTK,@stamp=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
DECLARE @codes TABLE(Loai varchar(20),Ma char(10));
DECLARE @preview TABLE(Code char(10),Revision binary(8),Total decimal(18,0),Ship decimal(18,0),Discount decimal(18,0),Payable decimal(38,0),Source varchar(20),PercentDiscount int,PartnerPercent decimal(5,2),PartnerFee decimal(18,0),Points bigint,PreviewToken binary(32));
DECLARE @payments dbo.DanhSachThanhToan;
DECLARE @baseOpen int=(SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai IN (N'Chua thanh toan',N'Cho thanh toan'));
DECLARE @baseRevenue decimal(38,0)=(SELECT COALESCE(SUM(CONVERT(decimal(38,0),ThanhTien)),0) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan');
DECLARE @baseSessions int=(SELECT COUNT(*) FROM dbo.PHIEN_BAN);

-- Bàn dùng cho T34 phải trống và chưa có phiên; script khác dùng B0001-B0004.
IF EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan IN ('B0005','B0006','B0007','B0008') AND (TrangThai<>N'Trong' OR MaPhien IS NOT NULL))
 THROW 52903,'T34 tables must start empty',1;

-- TS01: đơn tại quán tự mở đúng một phiên, gắn MaPhien/MaNVMo; đơn mang đi không tạo phiên.
INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes; SET @id=NEWID();
EXEC dbo.sp_TaoHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@RequestId=@id,@Channel=N'Tai quan',@TableCode='B0005',@Product='SP001',@Quantity=1;
SELECT @session=MaPhien FROM dbo.HOADON WHERE MaHD=@code;
IF @session IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.HOADON h JOIN dbo.PHIEN_BAN p ON p.MaPhien=h.MaPhien
 WHERE h.MaHD=@code AND p.MaBan='B0005' AND p.GioRa IS NULL AND p.MaNVMo='NV001')
 THROW 52903,'TS01 session not attached or wrong opener',1;
IF (SELECT COUNT(*) FROM dbo.PHIEN_BAN WHERE MaBan='B0005' AND GioRa IS NULL)<>1 THROW 52903,'TS01 duplicate open session',1;
DECLARE @ts01 char(10)=@code,@ts01Session int=@session;
DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes; SET @id=NEWID();
EXEC dbo.sp_TaoHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@RequestId=@id,@Channel=N'Mang di',@Product='SP001',@Quantity=1;
IF (SELECT MaPhien FROM dbo.HOADON WHERE MaHD=@code) IS NOT NULL OR (SELECT COUNT(*) FROM dbo.PHIEN_BAN)<>@baseSessions+1
 THROW 52903,'TS01 takeaway created a session',1;

-- TS02: flush nháp tại quán mở phiên trong cùng transaction; nháp chưa Giữ không tạo phiên.
IF EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0006' AND TrangThai<>N'Trong')
 OR EXISTS(SELECT 1 FROM dbo.PHIEN_BAN WHERE MaBan='B0006')
 THROW 52903,'TS02 unflushed draft touched table',1;
DECLARE @spPrice decimal(18,0)=(SELECT GiaBan FROM dbo.SANPHAM WHERE MaSP='SP001');
DECLARE @lines dbo.SalesDraftLines; INSERT @lines VALUES(1,'SP001',2,NULL,@spPrice);
DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes; SET @id=NEWID();
EXEC dbo.sp_TaoHoaDonTuNhap @Actor=@actor,@Stamp=@stamp,@Code=@code,@RequestId=@id,@Channel=N'Tai quan',@TableCode='B0006',@Lines=@lines;
SELECT @session=MaPhien FROM dbo.HOADON WHERE MaHD=@code;
IF @session IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.PHIEN_BAN WHERE MaPhien=@session AND MaBan='B0006' AND GioRa IS NULL)
 OR (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@code)<>1
 THROW 52903,'TS02 draft flush session/order mismatch',1;
DECLARE @ts02 char(10)=@code,@ts02Session int=@session;

-- TS03: thu tiền nhưng bàn vẫn giữ phiên; gọi thêm tạo hóa đơn mới cùng phiên, không gộp.
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@ts01; DELETE @preview;
INSERT @preview EXEC dbo.sp_SalesCheckoutPreview @actor,@stamp,@ts01,@rev;
SELECT @token=PreviewToken,@amount=Payable FROM @preview; DELETE @payments; INSERT @payments VALUES('PT001',@amount,NULL); SET @id=NEWID();
EXEC dbo.sp_ThanhToanHoaDon @actor,@stamp,@ts01,@rev,@id,@token,@payments;
IF NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0005' AND TrangThai=N'Dang phuc vu' AND Retained=1 AND MaPhien=@ts01Session AND MaHD IS NULL)
 THROW 52903,'TS03 paid table must stay busy as retained',1;
IF (SELECT COUNT(*) FROM dbo.HOADON WHERE MaPhien=@ts01Session AND TrangThai IN (N'Chua thanh toan',N'Cho thanh toan'))<>0
 THROW 52903,'TS03 paid order still counted open',1;
DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes; SET @id=NEWID();
EXEC dbo.sp_TaoHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@code,@RequestId=@id,@Channel=N'Tai quan',@TableCode='B0005',@Product='SP001',@Quantity=1;
IF (SELECT MaPhien FROM dbo.HOADON WHERE MaHD=@code)<>@ts01Session
 OR (SELECT COUNT(DISTINCT MaHD) FROM dbo.HOADON WHERE MaPhien=@ts01Session)<>2
 OR (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@ts01)<>1
 OR (SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD=@code)<>1
 THROW 52903,'TS03 add-on did not join session or merged invoices',1;
DECLARE @ts03b char(10)=@code;

-- TS05: trả bàn khi còn đơn chưa chốt bị 52110, không đổi GioRa/đơn, không ghi receipt.
DECLARE @ts05 char(10),@ts05Session int;
DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @ts05=Ma FROM @codes; SET @id=NEWID();
EXEC dbo.sp_TaoHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@ts05,@RequestId=@id,@Channel=N'Tai quan',@TableCode='B0007',@Product='SP001',@Quantity=1;
SELECT @ts05Session=MaPhien FROM dbo.HOADON WHERE MaHD=@ts05;
SELECT @sessionRev=Revision FROM dbo.PHIEN_BAN WHERE MaPhien=@ts05Session;
DECLARE @blockedId uniqueidentifier=NEWID();
BEGIN TRY
 EXEC dbo.sp_TraBan @actor,@stamp,@ts05Session,@sessionRev,@blockedId;
 THROW 52903,'TS05 close accepted with open order',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52110 THROW; END CATCH;
IF EXISTS(SELECT 1 FROM dbo.PHIEN_BAN WHERE MaPhien=@ts05Session AND GioRa IS NOT NULL)
 OR EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@blockedId)
 OR NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@ts05 AND TrangThai=N'Chua thanh toan')
 THROW 52903,'TS05 blocked close leaked changes',1;

-- TS05 (tiếp): thu đơn cuối rồi phiên sạch, sẵn sàng cho TS06.
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@ts05; DELETE @preview;
INSERT @preview EXEC dbo.sp_SalesCheckoutPreview @actor,@stamp,@ts05,@rev;
SELECT @token=PreviewToken,@amount=Payable FROM @preview; DELETE @payments; INSERT @payments VALUES('PT001',@amount,NULL); SET @id=NEWID();
EXEC dbo.sp_ThanhToanHoaDon @actor,@stamp,@ts05,@rev,@id,@token,@payments;
IF NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0007' AND TrangThai=N'Dang phuc vu' AND Retained=1 AND MaPhien=@ts05Session)
 THROW 52903,'TS05 paid table lost its session',1;

-- TS06: trả bàn khi phiên sạch ghi GioRa/MaNVTra + receipt TableClose; replay cùng RequestId không đóng lần hai.
SELECT @sessionRev=Revision FROM dbo.PHIEN_BAN WHERE MaPhien=@ts05Session;
DECLARE @closeId uniqueidentifier=NEWID();
EXEC dbo.sp_TraBan @actor,@stamp,@ts05Session,@sessionRev,@closeId;
IF NOT EXISTS(SELECT 1 FROM dbo.PHIEN_BAN WHERE MaPhien=@ts05Session AND GioRa IS NOT NULL AND MaNVTra='NV001')
 OR NOT EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@closeId AND Operation='TableClose' AND EntityCode=CONVERT(varchar(10),@ts05Session) AND ActorAccountId=@actor)
 THROW 52903,'TS06 close session/receipt mismatch',1;
IF NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0007' AND TrangThai=N'Trong' AND MaPhien IS NULL AND Retained=0)
 THROW 52903,'TS06 closed table not empty',1;
DECLARE @closedAt datetime2(0)=(SELECT GioRa FROM dbo.PHIEN_BAN WHERE MaPhien=@ts05Session);
EXEC dbo.sp_TraBan @actor,@stamp,@ts05Session,@sessionRev,@closeId;
IF (SELECT GioRa FROM dbo.PHIEN_BAN WHERE MaPhien=@ts05Session)<>@closedAt
 OR (SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE Operation='TableClose' AND EntityCode=CONVERT(varchar(10),@ts05Session))<>1
 THROW 52903,'TS06 replay closed twice',1;

-- TS07: trả bàn lần hai khác RequestId bị 52111; revision cũ trên phiên còn mở bị 52103.
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_TraBan @actor,@stamp,@ts05Session,@sessionRev,@id;
 THROW 52903,'TS07 second close accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52111 THROW; END CATCH;
SELECT @sessionRev=Revision FROM dbo.PHIEN_BAN WHERE MaPhien=@ts02Session;
DECLARE @wrongRev binary(8)=CAST(0x0000000000000001 AS binary(8));
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_TraBan @actor,@stamp,@ts02Session,@wrongRev,@id;
 THROW 52903,'TS07 stale session revision accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52103 THROW; END CATCH;
IF (SELECT GioRa FROM dbo.PHIEN_BAN WHERE MaPhien=@ts02Session) IS NOT NULL
 THROW 52903,'TS07 stale close changed session',1;

-- TS03 (tiếp): thu đơn gọi thêm rồi trả bàn; bàn B0005 phải trống.
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@ts03b; DELETE @preview;
INSERT @preview EXEC dbo.sp_SalesCheckoutPreview @actor,@stamp,@ts03b,@rev;
SELECT @token=PreviewToken,@amount=Payable FROM @preview; DELETE @payments; INSERT @payments VALUES('PT001',@amount,NULL); SET @id=NEWID();
EXEC dbo.sp_ThanhToanHoaDon @actor,@stamp,@ts03b,@rev,@id,@token,@payments;
SELECT @closeRev=Revision FROM dbo.PHIEN_BAN WHERE MaPhien=@ts01Session;
SET @id=NEWID(); EXEC dbo.sp_TraBan @actor,@stamp,@ts01Session,@closeRev,@id;
IF NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0005' AND TrangThai=N'Trong' AND MaPhien IS NULL)
 THROW 52903,'TS03 session not closed after both invoices paid',1;

-- TS10: đơn cũ không MaPhien vẫn làm bàn bận, không backfill và không sinh phiên giả.
DECLARE @ts10 char(10),@ts10Session int;
DELETE @codes; INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @ts10=Ma FROM @codes; SET @id=NEWID();
EXEC dbo.sp_TaoHoaDon @Actor=@actor,@Stamp=@stamp,@Code=@ts10,@RequestId=@id,@Channel=N'Tai quan',@TableCode='B0008',@Product='SP001',@Quantity=1;
SELECT @ts10Session=MaPhien FROM dbo.HOADON WHERE MaHD=@ts10;
UPDATE dbo.HOADON SET MaPhien=NULL WHERE MaHD=@ts10;
UPDATE dbo.PHIEN_BAN SET GioRa=SYSDATETIME(),MaNVTra='NV001' WHERE MaPhien=@ts10Session;
IF NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0008' AND TrangThai=N'Dang phuc vu' AND MaPhien IS NULL AND Retained=0 AND MaHD=@ts10)
 THROW 52903,'TS10 legacy order no longer holds table',1;
IF EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@ts10 AND MaPhien IS NOT NULL) OR EXISTS(SELECT 1 FROM dbo.PHIEN_BAN WHERE MaBan='B0008' AND GioRa IS NULL)
 THROW 52903,'TS10 backfilled session for legacy order',1;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@ts10; DELETE @preview;
INSERT @preview EXEC dbo.sp_SalesCheckoutPreview @actor,@stamp,@ts10,@rev;
SELECT @token=PreviewToken,@amount=Payable FROM @preview; DELETE @payments; INSERT @payments VALUES('PT001',@amount,NULL); SET @id=NEWID();
EXEC dbo.sp_ThanhToanHoaDon @actor,@stamp,@ts10,@rev,@id,@token,@payments;
IF NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0008' AND TrangThai=N'Trong')
 THROW 52903,'TS10 paid legacy order did not free table',1;

-- TS12: phiên không đổi đếm đơn mở/doanh thu dashboard; chỉ hóa đơn đã thu vào doanh thu.
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@ts02; DELETE @preview;
INSERT @preview EXEC dbo.sp_SalesCheckoutPreview @actor,@stamp,@ts02,@rev;
SELECT @token=PreviewToken,@amount=Payable FROM @preview; DELETE @payments; INSERT @payments VALUES('PT001',@amount,NULL); SET @id=NEWID();
EXEC dbo.sp_ThanhToanHoaDon @actor,@stamp,@ts02,@rev,@id,@token,@payments;
SELECT @closeRev=Revision FROM dbo.PHIEN_BAN WHERE MaPhien=@ts02Session;
SET @id=NEWID(); EXEC dbo.sp_TraBan @actor,@stamp,@ts02Session,@closeRev,@id;
DECLARE @dashboard TABLE(BusinessDate date,ReadAt datetime2(7),Revenue decimal(38,0),OpenOrders int,Debt decimal(38,0));
INSERT @dashboard EXEC dbo.sp_ReportDashboard @actor,@stamp;
IF (SELECT OpenOrders FROM @dashboard)<>(SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai IN (N'Chua thanh toan',N'Cho thanh toan'))
 OR (SELECT Revenue FROM @dashboard)<>(SELECT COALESCE(SUM(CONVERT(decimal(38,0),ThanhTien)),0) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND NgayThanhToan>=CONVERT(date,SYSDATETIME()) AND NgayThanhToan<DATEADD(day,1,CONVERT(datetime2(7),CONVERT(date,SYSDATETIME()))))
 THROW 52903,'TS12 dashboard drifted from invoice state',1;
-- Chỉ bốn bàn T34 phải sạch phiên; script khác trong đợt có thể còn phiên mở trên bàn khác.
IF EXISTS(SELECT 1 FROM dbo.PHIEN_BAN WHERE MaBan IN ('B0005','B0006','B0007','B0008') AND GioRa IS NULL)
 OR (SELECT COUNT(*) FROM dbo.PHIEN_BAN)<>@baseSessions+4
 THROW 52903,'TS12 session rows leaked after close',1;
-- TS01 để lại một đơn mang đi chưa thu; ngoài đơn đó số đơn mở không đổi.
IF (SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai IN (N'Chua thanh toan',N'Cho thanh toan'))<>@baseOpen+1
 THROW 52903,'TS12 open invoice count changed outside session flow',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 52903,'Transaction not clean',1;
