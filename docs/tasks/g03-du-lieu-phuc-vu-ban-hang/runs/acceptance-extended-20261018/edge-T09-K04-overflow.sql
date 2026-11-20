DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
EXEC dbo.sp_MaterialOpen @Actor=@a,@Stamp=@s,@Code='NVL88',@RequestId=@id,@Quantity=100000000000000,@Source=N'Overflow';
