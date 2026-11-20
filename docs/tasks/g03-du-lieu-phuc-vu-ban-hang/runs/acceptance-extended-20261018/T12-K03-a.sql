DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DECLARE @name nvarchar(max)=REPLICATE(N'X',51);EXEC dbo.sp_CustomerCreate @Actor=@a,@Stamp=@s,@RequestId=@id,@Name=@name;
