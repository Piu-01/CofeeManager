DECLARE @a int,@s binary(8),@id uniqueidentifier=NEWID(),@rev binary(8);SELECT TOP(1) @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1 ORDER BY MaTK;
INSERT dbo.HOADON(MaHD,MaNV,MaKH,LoaiDon)VALUES('HD09999201','NV001','KH002',N'Mang di');
INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan)VALUES('HD09999201','SP995',1,500);
DECLARE @tt dbo.DanhSachThanhToan;INSERT @tt(MaPT,SoTien)VALUES('PT001',500);EXEC dbo.sp_ThanhToanHoaDon 'HD09999201',@tt;
INSERT dbo.LICHSU_DIEM(MaKH,MaHD,Loai,SoDiem,LyDo)VALUES('KH001','HD09999201',N'Cong',1,N'Wrong customer');
