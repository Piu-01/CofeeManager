DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @before nvarchar(max)=(SELECT MaDong,GiaBan FROM dbo.CHITIET_HD WHERE MaSP='SP001' ORDER BY MaDong FOR JSON PATH),@r dbo.RecipeLineInput;
SELECT @rev=Revision FROM dbo.SANPHAM WHERE MaSP='SP001';
EXEC dbo.sp_ProductUpdate @Actor=@a,@Stamp=@s,@Code='SP001',@RequestId=@id,@Recipe=@r,@ExpectedRevision=@rev,@Name=N'Price test',@Category='DM001',@Price=12345,@Status=N'Dang ban';
IF @before<>(SELECT MaDong,GiaBan FROM dbo.CHITIET_HD WHERE MaSP='SP001' ORDER BY MaDong FOR JSON PATH) THROW 52999,'History price changed',1;
