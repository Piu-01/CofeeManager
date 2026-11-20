DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
EXEC dbo.sp_CustomerCreate @Actor=@a,@Stamp=@s,@RequestId=@id,@Name=N'Duplicate',@Phone='0911111111';
