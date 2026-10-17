-- Owner-only, one-time fixture; no app workflow is implemented by this script.
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G02[_]%' OR OBJECT_ID(N'dbo.G02TestMarker',N'U') IS NULL
 THROW 52500,'Isolated marked G02 DB required.',1;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @employee CHAR(5)=(SELECT TOP(1) MaNV FROM dbo.TAIKHOAN WHERE TenDangNhap='g02.admin');
 IF @employee IS NULL THROW 52501,'Bootstrap fixture admin first.',1;
 INSERT dbo.DANHMUC_SP(MaDM,TenDM) VALUES('DM990',N'History fixture DM');
 INSERT dbo.NHACUNGCAP(MaNCC,TenNCC,SDT) VALUES('NCC90',N'History fixture NCC','0199000090');
 INSERT dbo.PHUONGTHUCTT(MaPT,TenPT) VALUES('PT990',N'History fixture payment');
 INSERT dbo.PaymentMethodSupport VALUES('PT990',0);
 INSERT dbo.DOITACGIAOHANG(MaDT,TenDT,ChietKhau) VALUES('DT990',N'History fixture partner',10);
 INSERT dbo.DANHMUC_THUCHI(MaDMTC,TenDMTC,Loai) VALUES('TC990',N'History fixture expense',N'Chi');
 INSERT dbo.CHUCVU(MaCV,TenCV,PhuCap,HeSoTangCa) VALUES('CV990',N'History fixture position',0,1);
 INSERT dbo.CALAMVIEC(MaCa,TenCa,GioBatDau,GioKetThuc) VALUES('CA990',N'History fixture shift','22:00','06:00');
 INSERT dbo.NHANVIEN(MaNV,TenNV,LoaiHopDong,MaCV) VALUES('NV990',N'History fixture employee','Full-time','CV990');
 INSERT dbo.CHAMCONG(MaNV,NgayLam,MaCa,TrangThai) VALUES('NV990','2026-10-01','CA990',N'Vang');
 INSERT dbo.SANPHAM(MaSP,TenSP,MaDM,GiaBan) VALUES('SP990',N'History fixture product','DM990',100000);
 INSERT dbo.NGUYENVATLIEU(MaNVL,TenNVL,DonViTinh,SoLuongTon) VALUES('NVL90',N'History fixture ingredient',N'Cái',100);
 INSERT dbo.CONGTHUC VALUES('SP990','NVL90',1);
 INSERT dbo.PHIEUNHAP(MaPN,MaNV,MaNCC) VALUES('PN20991201',@employee,'NCC90');
 INSERT dbo.PHIEUTHUCHI(MaTC,MaDMTC,MaNV,SoTien) VALUES('TC20991201','TC990',@employee,1000);
 INSERT dbo.HOADON(MaHD,MaNV,MaDT,LoaiDon,TenNguoiNhan,SDTNguoiNhan,DiaChiGiao,TrangThaiGiao)
 VALUES('HD20991201',@employee,'DT990',N'Giao hang',N'Fixture','0199000091',N'Fixture only',N'Cho giao');
 INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan) VALUES('HD20991201','SP990',1,100000);
 DECLARE @payments dbo.DanhSachThanhToan;
 INSERT @payments(MaPT,SoTien) VALUES('PT990',100000);
 EXEC dbo.sp_ThanhToanHoaDon @MaHD='HD20991201',@ThanhToan=@payments;
 -- Existing open order for T07-K07.b; leaving shell must not cancel it.
 INSERT dbo.HOADON(MaHD,MaNV,LoaiDon) VALUES('HD20991202',@employee,N'Mang di');
 INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan) VALUES('HD20991202','SP990',1,100000);
 COMMIT;
END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
