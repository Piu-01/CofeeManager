DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @before nvarchar(max)=(SELECT * FROM dbo.TIEUHAO_HOADON ORDER BY MaHD,MaNVL FOR JSON PATH),@stock nvarchar(max)=(SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH),@r dbo.RecipeLineInput;
INSERT @r VALUES('NVL01',0.01);SELECT @rev=Revision FROM dbo.SANPHAM WHERE MaSP='SP001';EXEC dbo.sp_ProductReplaceRecipe @a,@s,'SP001',@id,@r,@rev;
IF @before<>(SELECT * FROM dbo.TIEUHAO_HOADON ORDER BY MaHD,MaNVL FOR JSON PATH) OR @stock<>(SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) THROW 52999,'Consumption recalculated',1;
