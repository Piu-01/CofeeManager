DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @before nvarchar(max)=(SELECT MaHD,MaDong,SoLuong,GiaBan FROM dbo.CHITIET_HD WHERE MaSP='SP996' ORDER BY MaDong FOR JSON PATH),@r dbo.RecipeLineInput;
SELECT @rev=Revision FROM dbo.SANPHAM WHERE MaSP='SP996';
EXEC dbo.sp_ProductUpdate @Actor=@a,@Stamp=@s,@Code='SP996',@RequestId=@id,@Recipe=@r,@ExpectedRevision=@rev,@Name=N'Concurrent recipe v3',@Category='DM001',@Price=10000,@Status=N'Ngung ban';
DECLARE @rows TABLE(Code char(5),Name nvarchar(50),Category char(5),Price decimal(18,0),Status nvarchar(20),Revision binary(8));
INSERT @rows EXEC dbo.sp_ProductList @a,@s,N'SP996',0,NULL,1;
IF EXISTS(SELECT 1 FROM @rows) THROW 52999,'Stopped product still selectable',1;
IF @before<>(SELECT MaHD,MaDong,SoLuong,GiaBan FROM dbo.CHITIET_HD WHERE MaSP='SP996' ORDER BY MaDong FOR JSON PATH) THROW 52999,'Existing lines changed',1;
