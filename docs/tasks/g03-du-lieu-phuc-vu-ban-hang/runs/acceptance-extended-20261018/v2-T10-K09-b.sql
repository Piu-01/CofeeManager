DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='CASH' AND HoatDong=1 ORDER BY MaTK;EXECUTE AS USER='G03Runtime';
DECLARE @r dbo.RecipeLineInput;EXEC dbo.sp_ProductReplaceRecipe @a,@s,'SP995',@id,@r;
