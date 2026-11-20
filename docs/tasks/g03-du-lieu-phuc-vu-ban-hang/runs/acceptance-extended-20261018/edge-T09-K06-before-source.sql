DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
EXEC dbo.sp_MaterialCreate @a,@s,'NVL89',@id,NULL,N'Unit test',N'Kg';SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL89';SET @id=NEWID();EXEC dbo.sp_MaterialUpdate @a,@s,'NVL89',@id,@rev,N'Unit test',N'g';
IF NOT EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL89' AND DonViTinh=N'g' AND SoLuongTon=0) THROW 52999,'Unit conversion corrupted stock',1;
DECLARE @r dbo.RecipeLineInput;INSERT @r VALUES('NVL89',1);SET @id=NEWID();EXEC dbo.sp_ProductCreate @Actor=@a,@Stamp=@s,@Code='SP993',@RequestId=@id,@Recipe=@r,@Name=N'Unit recipe test',@Category='DM001',@Price=10000,@Status=N'Ngung ban';
