-- Technical table-session dependency smoke only; test-owned demo database.
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G04_%' OR OBJECT_ID('dbo.G04TestMarker') IS NULL THROW 52107,'Owned test database required',1;
UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV IN ('NV001','NV004');
DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@code varchar(10),@r binary(8),@session int,@expected binary(8),@token binary(32),@amount decimal(18,0),@rev binary(8);
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';

-- Cấu trúc schema 034: bảng phiên, khóa ngoại, unique filtered index, DENY, cột liên kết và view.
IF OBJECT_ID('dbo.PHIEN_BAN','U') IS NULL OR OBJECT_ID('dbo.sp_TraBan') IS NULL THROW 52107,'PHIEN_BAN/sp_TraBan missing',1;
IF COL_LENGTH('dbo.HOADON','MaPhien') IS NULL OR COL_LENGTH('dbo.PHIEN_BAN','Revision') IS NULL THROW 52107,'MaPhien/Revision column missing',1;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_HD_Phien' AND parent_object_id=OBJECT_ID('dbo.HOADON')) THROW 52107,'FK_HD_Phien missing',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.PHIEN_BAN') AND name='UX_PHIEN_BAN_Mo' AND is_unique=1 AND filter_definition LIKE N'%GioRa%IS NULL%')
 THROW 52107,'UX_PHIEN_BAN_Mo missing',1;
IF (SELECT COUNT(*) FROM sys.database_permissions dp JOIN sys.database_principals pr ON pr.principal_id=dp.grantee_principal_id
 WHERE pr.name='CafeManagerApp' AND dp.state='D' AND dp.class=1 AND dp.major_id=OBJECT_ID('dbo.PHIEN_BAN'))<>3
 THROW 52107,'PHIEN_BAN DENY missing',1;
IF (SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID('dbo.vw_TrangThaiBan') AND name IN ('MaPhien','SessionRevision','Retained'))<>3
 THROW 52107,'vw_TrangThaiBan session columns missing',1;

-- Tạo đơn tại quán mở phiên; dữ liệu cũ không backfill.
DECLARE @codes TABLE(Loai varchar(20),Ma char(10)); INSERT @codes EXEC dbo.sp_CapMa 'HOADON'; SELECT @code=Ma FROM @codes;
DECLARE @sessionsBefore int=(SELECT COUNT(*) FROM dbo.PHIEN_BAN);
EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@code,@RequestId=@id,@Channel=N'Tai quan',@TableCode='B0009',@Product='SP001',@Quantity=1;
SELECT @session=MaPhien FROM dbo.HOADON WHERE MaHD=@code; SELECT @expected=Revision FROM dbo.PHIEN_BAN WHERE MaPhien=@session;
IF @session IS NULL OR (SELECT COUNT(*) FROM dbo.PHIEN_BAN)<>@sessionsBefore+1 THROW 52107,'Session auto-open mismatch',1;
IF NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0009' AND TrangThai=N'Dang phuc vu' AND MaPhien=@session AND Retained=0 AND MaHD=@code)
 THROW 52107,'Table view missing session row',1;
EXEC dbo.sp_TableList @a,@s,N'',0;
EXEC dbo.sp_TableSelectCurrent @a,@s,'B0009';

-- Trả bàn khi còn đơn mở bị 52110; thu xong trả bàn được, receipt TableClose, lặp lại 52111.
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_TraBan @a,@s,@session,@expected,@id;
 THROW 52107,'Close accepted with open order',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52110 THROW; END CATCH;
SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@code;
DECLARE @preview TABLE(Code char(10),Revision binary(8),Total decimal(18,0),Ship decimal(18,0),Discount decimal(18,0),Payable decimal(38,0),Source varchar(20),PercentDiscount int,PartnerPercent decimal(5,2),PartnerFee decimal(18,0),Points bigint,PreviewToken binary(32));
INSERT @preview EXEC dbo.sp_SalesCheckoutPreview @a,@s,@code,@rev;
SELECT @token=PreviewToken,@amount=Payable FROM @preview;
DECLARE @payments dbo.DanhSachThanhToan; INSERT @payments VALUES('PT001',@amount,NULL); SET @id=NEWID();
EXEC dbo.sp_ThanhToanHoaDon @a,@s,@code,@rev,@id,@token,@payments;
IF NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0009' AND TrangThai=N'Dang phuc vu' AND Retained=1) THROW 52107,'Paid table not retained',1;
SET @id=NEWID(); EXEC dbo.sp_TraBan @a,@s,@session,@expected,@id;
IF NOT EXISTS(SELECT 1 FROM dbo.PHIEN_BAN WHERE MaPhien=@session AND GioRa IS NOT NULL AND MaNVTra='NV001')
 OR NOT EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@id AND Operation='TableClose' AND EntityCode=CONVERT(varchar(10),@session))
 THROW 52107,'Close session/receipt mismatch',1;
IF NOT EXISTS(SELECT 1 FROM dbo.vw_TrangThaiBan WHERE MaBan='B0009' AND TrangThai=N'Trong') THROW 52107,'Closed table not empty',1;
BEGIN TRY
 SET @id=NEWID(); EXEC dbo.sp_TraBan @a,@s,@session,@expected,@id;
 THROW 52107,'Second close accepted',1;
END TRY BEGIN CATCH IF ERROR_NUMBER()<>52111 THROW; END CATCH;
DECLARE @dashboard TABLE(BusinessDate date,ReadAt datetime2(7),Revenue decimal(38,0),OpenOrders int,Debt decimal(38,0));
INSERT @dashboard EXEC dbo.sp_ReportDashboard @a,@s;
IF (SELECT OpenOrders FROM @dashboard)<>(SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai IN (N'Chua thanh toan',N'Cho thanh toan'))
 THROW 52107,'Dashboard open count drifted',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 52107,'Transaction not clean',1;
