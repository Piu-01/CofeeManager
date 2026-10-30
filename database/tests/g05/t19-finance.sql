IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G05_%' OR OBJECT_ID('dbo.G05TestMarker') IS NULL THROW 53900,N'Chỉ dùng fixture G05.',1;
DECLARE @actor int=(SELECT MIN(MaTK) FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1),@stamp binary(8),@id uniqueidentifier=NEWID(),@code varchar(10),@ok bit;
SELECT @stamp=SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=@actor;
IF @actor IS NULL THROW 53900,N'Thiếu actor fixture.',1;
EXEC dbo.sp_FinanceCreate @actor,@stamp,@id,'20261026','TC001',1234,N'G05 fixture',NULL;
EXEC dbo.sp_FinanceCreate @actor,@stamp,@id,'20261026','TC001',1234,N'G05 fixture',NULL;
SELECT @code=EntityCode FROM dbo.G03OperationReceipt WHERE RequestId=@id;
SET @ok=CASE WHEN (SELECT COUNT(*) FROM dbo.PHIEUTHUCHI WHERE MaTC=@code AND SoTien=1234 AND OriginKind='Manual')=1 THEN 1 ELSE 0 END;
EXEC dbo.sp_G05Assert 'T19-K01.SQL-replay',@ok,N'Finance create/replay 1234 VND; native chưa kiểm.';
