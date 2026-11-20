-- Technical invoice-detail/session dependency smoke only; test-owned demo database.
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G04_%' OR OBJECT_ID('dbo.G04TestMarker') IS NULL THROW 52107,'Owned test database required',1;
UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV IN ('NV001','NV004');
DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@code varchar(10),@rev binary(8),@session int,@sessionRev binary(8),@cancelId uniqueidentifier;
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
DECLARE @codes TABLE(Loai varchar(20),Ma char(10));

-- Contract 035: sp_InvoiceDetail trả thêm MaPhien/SessionRevision như sp_SalesReadCore.
IF OBJECT_DEFINITION(OBJECT_ID('dbo.sp_InvoiceDetail')) NOT LIKE N'%h.MaPhien,sessionRow.Revision SessionRevision%'
 OR OBJECT_DEFINITION(OBJECT_ID('dbo.sp_InvoiceDetail')) NOT LIKE N'%OUTER APPLY(SELECT TOP(1) p.Revision FROM dbo.PHIEN_BAN p WHERE p.MaPhien=h.MaPhien)%'
 THROW 52107,'sp_InvoiceDetail session columns missing',1;
IF OBJECT_DEFINITION(OBJECT_ID('dbo.sp_SalesReadCore')) NOT LIKE N'%h.MaPhien,sessionRow.Revision SessionRevision%'
 THROW 52107,'sp_SalesReadCore session columns missing',1;

-- Đơn tại quán có phiên: chi tiết chạy được; DAL đọc đúng 27 cột ở typed smoke.
INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes; SET @id=NEWID();
EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@RequestId=@id,@Channel=N'Tai quan',@TableCode='B0009',@Product='SP001',@Quantity=1;
SELECT @session=MaPhien FROM dbo.HOADON WHERE MaHD=@code;
IF @session IS NULL THROW 52107,'Session not opened for dine-in order',1;
SELECT @sessionRev=Revision FROM dbo.PHIEN_BAN WHERE MaPhien=@session;
EXEC dbo.sp_InvoiceDetail @a,@s,@code;
IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@code AND MaPhien=@session)
 THROW 52107,'Invoice detail session link missing',1;

-- Dọn trạng thái: hủy đơn rồi trả bàn.
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code; SET @cancelId=NEWID();
EXEC dbo.sp_HuyHoaDon @a,@s,@code,@rev,@cancelId;
SET @id=NEWID(); EXEC dbo.sp_TraBan @a,@s,@session,@sessionRev,@id;
IF EXISTS(SELECT 1 FROM dbo.PHIEN_BAN WHERE MaPhien=@session AND GioRa IS NULL) THROW 52107,'Session not closed after smoke',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 52107,'Transaction not clean',1;
