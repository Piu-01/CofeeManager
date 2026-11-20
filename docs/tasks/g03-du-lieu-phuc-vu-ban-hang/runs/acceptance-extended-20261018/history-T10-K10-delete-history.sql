DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @r dbo.RecipeLineInput;SELECT @rev=Revision FROM dbo.SANPHAM WHERE MaSP='SP996';EXEC dbo.sp_ProductDelete @a,@s,'SP996',@id,@r,@rev;
