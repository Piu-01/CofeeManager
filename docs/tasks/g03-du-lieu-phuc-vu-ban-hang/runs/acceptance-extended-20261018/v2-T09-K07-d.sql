DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;EXECUTE AS USER='G03Runtime';
UPDATE dbo.NGUYENVATLIEU SET SoLuongTon=0 WHERE MaNVL='NVL95';
