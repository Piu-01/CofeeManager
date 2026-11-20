DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @r dbo.RecipeLineInput;EXEC dbo.sp_ProductCreate @Actor=@a,@Stamp=@s,@Code='SP994',@RequestId=@id,@Recipe=@r,@Name=N'Unused',@Category='DM001',@Price=10000,@Status=N'Ngung ban';
SELECT @rev=Revision FROM dbo.SANPHAM WHERE MaSP='SP994';SET @id=NEWID();EXEC dbo.sp_ProductDelete @a,@s,'SP994',@id,@r,@rev;
IF EXISTS(SELECT 1 FROM dbo.SANPHAM WHERE MaSP='SP994') OR NOT EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@id AND EntityCode='SP994') THROW 52999,'Delete receipt mismatch',1;
