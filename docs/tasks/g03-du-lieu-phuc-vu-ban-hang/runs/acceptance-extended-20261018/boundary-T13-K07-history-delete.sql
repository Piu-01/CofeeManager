DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
DELETE dbo.LICHSU_DIEM WHERE MaGD=(SELECT MAX(MaGD) FROM dbo.LICHSU_DIEM);
