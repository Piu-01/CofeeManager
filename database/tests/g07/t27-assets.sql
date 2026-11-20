-- Subset nghiệm thu chỉ chạy sau duyệt đợt test, DB marker của runner.
SET NOCOUNT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G07[_]%' OR OBJECT_ID('dbo.G07TestMarker') IS NULL THROW 53401,N'Chỉ DBtest G07.',1;
DECLARE @stamp binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=1),@id uniqueidentifier=NEWID(),@before int=(SELECT COUNT(*) FROM dbo.PHIEUTHUCHI);
EXECUTE AS USER='G07Runtime';
EXEC dbo.sp_AssetWrite @Actor=1,@Stamp=@stamp,@Action='Create',@RequestId=@id,@Name=N'Fixture tài sản',@PurchasedOn='20260115',@Value=100,@Months=3;
REVERT;
IF @before<>(SELECT COUNT(*) FROM dbo.PHIEUTHUCHI) THROW 53402,N'Lưu tài sản đã sinh chi.',1;
DECLARE @code varchar(10)=(SELECT EntityCode FROM dbo.G03OperationReceipt WHERE RequestId=@id);
IF NOT EXISTS(SELECT 1 FROM dbo.TAISAN_CODINH WHERE MaTS=@code AND GiaTri=100 AND SoThangKH=3) THROW 53402,N'Hồ sơ không khớp.',1;
IF (SELECT Accumulated FROM dbo.fn_AssetDepreciation(100,3,'20260115','20260301'))<>100 OR (SELECT Monthly FROM dbo.fn_AssetDepreciation(100,3,'20260115','20260301'))<>34 THROW 53402,N'Khấu hao không bù đúng tháng cuối.',1;
EXECUTE AS USER='G07Runtime';
EXEC dbo.sp_AssetWrite @Actor=1,@Stamp=@stamp,@Action='Create',@RequestId=@id,@Name=N'Fixture tài sản',@PurchasedOn='20260115',@Value=100,@Months=3;
REVERT;
IF (SELECT COUNT(*) FROM dbo.TAISAN_CODINH WHERE MaTS=@code)<>1 OR (SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE RequestId=@id)<>1 THROW 53402,N'Replay tạo trùng.',1;
