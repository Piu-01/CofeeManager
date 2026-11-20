DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @r dbo.RecipeLineInput;INSERT @r VALUES('NVL01',0.01);EXEC dbo.sp_ProductCreate @Actor=@a,@Stamp=@s,@Code='SP993',@RequestId=@id,@Recipe=@r,@Name=N'Invalid price',@Category='DM001',@Price=-1,@Status=N'Dang ban';
