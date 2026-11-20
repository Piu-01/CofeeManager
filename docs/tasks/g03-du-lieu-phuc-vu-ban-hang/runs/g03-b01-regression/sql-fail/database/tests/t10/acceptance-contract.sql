IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G03_%' OR OBJECT_ID('dbo.G03TestMarker') IS NULL THROW 52999,'Test DB required',1;
DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8),@recipe dbo.RecipeLineInput;
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';
INSERT @recipe VALUES('NVL97',0.0180);
EXEC dbo.sp_ProductCreate @Actor=@a,@Stamp=@s,@Code='SP997',@RequestId=@id,@Recipe=@recipe,@Name=N'Món kiểm thử',@Category='DM001',@Price=10000,@Status=N'Dang ban';
SELECT @rev=Revision FROM dbo.SANPHAM WHERE MaSP='SP997';DELETE @recipe;INSERT @recipe VALUES('NVL97',-1);SET @id=NEWID();
BEGIN TRY EXEC dbo.sp_ProductReplaceRecipe @Actor=@a,@Stamp=@s,@Code='SP997',@RequestId=@id,@Recipe=@recipe,@ExpectedRevision=@rev;THROW 52999,'Invalid recipe accepted',1;END TRY BEGIN CATCH IF ERROR_NUMBER()<>51701 THROW;END CATCH;
IF (SELECT DinhMuc FROM dbo.CONGTHUC WHERE MaSP='SP997' AND MaNVL='NVL97')<>0.0180 OR NOT EXISTS(SELECT 1 FROM dbo.SANPHAM WHERE MaSP='SP997' AND Revision=@rev) OR EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@id) THROW 52999,'Recipe rollback mismatch',1;
IF @@TRANCOUNT<>0 OR XACT_STATE()<>0 THROW 52999,'Transaction leaked',1;
