DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
IF (SELECT COUNT(*) FROM dbo.CONGTHUC WHERE MaSP='SP991')<>2 OR (SELECT DinhMuc FROM dbo.CONGTHUC WHERE MaSP='SP991' AND MaNVL='NVL01')<>0.0180 THROW 52999,'Recipe mutated',1;
