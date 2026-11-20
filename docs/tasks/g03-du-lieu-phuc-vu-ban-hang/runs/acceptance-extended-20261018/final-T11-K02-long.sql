DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @n nvarchar(max)=REPLICATE(N'X',51);EXEC dbo.sp_TableCreate @a,@s,'B0999',@id,NULL,@n,'KV001';
