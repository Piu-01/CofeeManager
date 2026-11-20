DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @points int,@tier char(5);SELECT @rev=Revision,@points=DiemTichLuy,@tier=MaHang FROM dbo.KHACHHANG WHERE MaKH='KH001';
EXEC dbo.sp_CustomerUpdate @Actor=@a,@Stamp=@s,@RequestId=@id,@Code='KH001',@ExpectedRevision=@rev,@Name=N'Profile rename',@Phone=NULL;
IF NOT EXISTS(SELECT 1 FROM dbo.KHACHHANG WHERE MaKH='KH001' AND DiemTichLuy=@points AND MaHang=@tier AND SDT IS NULL) THROW 52999,'Profile changed points/tier',1;
