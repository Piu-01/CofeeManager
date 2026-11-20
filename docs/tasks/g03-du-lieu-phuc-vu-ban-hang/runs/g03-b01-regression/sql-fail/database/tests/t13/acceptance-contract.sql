IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G03_%' OR OBJECT_ID('dbo.G03TestMarker') IS NULL THROW 52999,'Test DB required',1;
DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@today date=CONVERT(date,SYSDATETIME());SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
EXEC dbo.sp_PromotionCreate @Actor=@a,@Stamp=@s,@Code='KM99999997',@RequestId=@id,@Name=N'Acceptance50%',@StartDate=@today,@EndDate=@today,@Discount=50;
DECLARE @discount TABLE(Source varchar(20),Tier char(5),Promotion char(10),PercentDiscount int,Discount decimal(18,0),Payable decimal(18,0),ReadAt datetime2(7),PolicyRevision binary(8));
INSERT @discount EXEC dbo.sp_DiscountPreview @a,@s,1,15000,@today,NULL,'KM99999997';
IF NOT EXISTS(SELECT 1 FROM @discount WHERE Discount=1 AND Payable=15000 AND Source='Promotion') THROW 52999,'Half-VND/ship mismatch',1;
DECLARE @before int=(SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH001');
DECLARE @preview TABLE(Balance bigint,CustomerRevision binary(8),PolicyRevision binary(8),Tier char(5));
INSERT @preview EXEC dbo.sp_LoyaltyAdjustPreview @a,@s,'KH001',N'Tru',1;
DECLARE @cr binary(8),@pr binary(8);SELECT @cr=CustomerRevision,@pr=PolicyRevision FROM @preview;
SET @id=NEWID();EXEC dbo.sp_LoyaltyAdjust @a,@s,'KH001',N'Tru',1,N'Acceptance decrement',@cr,@pr,@id;
EXEC dbo.sp_LoyaltyAdjust @a,@s,'KH001',N'Tru',1,N'Acceptance decrement',@cr,@pr,@id;
IF (SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH001')<>@before-1 OR (SELECT COUNT(*) FROM dbo.LICHSU_DIEM WHERE MaKH='KH001' AND LyDo=N'Acceptance decrement')<>1 THROW 52999,'Point receipt duplicated',1;
DECLARE @gd int=(SELECT MAX(MaGD) FROM dbo.LICHSU_DIEM WHERE MaKH='KH001');
BEGIN TRY UPDATE dbo.LICHSU_DIEM SET LyDo=N'Invalid mutation' WHERE MaGD=@gd;THROW 52999,'History mutation accepted',1;END TRY BEGIN CATCH IF ERROR_NUMBER()<>51011 THROW;END CATCH;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 52999,'Transaction leaked',1;

