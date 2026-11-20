DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
INSERT dbo.LICHSU_DIEM(MaKH,MaHD,Loai,SoDiem,LyDo)VALUES('KH001','HD20250401',N'Cong',1,N'Duplicate purchase');
