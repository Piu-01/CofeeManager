DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
SELECT @rev=Revision FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL86';EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='NVL86',@RequestId=@id,@ExpectedRevision=@rev,@Quantity=5,@Source=N'Not allowed';
