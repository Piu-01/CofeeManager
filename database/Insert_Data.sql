-- DU LIEU MAU cho QuanLyCafe_ERP_V2; chay SAU DB_Schema.sql.
-- Chi chay mot lan tren schema rong. Khong xoa hay ghi de du lieu hien co.
-- Mat khau mau bi khoa: tao hash that tu app va bat HoatDong khi can dang nhap.
-- Ton va diem ban dau la so du dau ky; nhap/xuat/ban/diem phat sinh tu dong.
-- Tat ca INSERT va thanh toan mau nam trong mot transaction.
USE QuanLyCafe_ERP_V2;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRAN;
    IF EXISTS (SELECT 1 FROM dbo.NHANVIEN)
        THROW 51200,N'Du lieu mau da ton tai; khong chay lai file INSERT.',1;

INSERT INTO dbo.CHUCVU (MaCV, TenCV, PhuCap, HeSoTangCa) VALUES
(N'CV001', N'Quan ly', 2000000, 2.0),
(N'CV002', N'Pha che', 500000, 1.5),
(N'CV003', N'Thu ngan', 300000, 1.5),
(N'CV004', N'Phuc vu', 200000, 1.5),
(N'CV005', N'Bao ve', 100000, 1.5),
(N'CV006', N'Tap vu', 100000, 1.5),
(N'CV007', N'Giao hang', 150000, 1.5),
(N'CV008', N'Ke toan', 800000, 1.75),
(N'CV009', N'Marketing', 600000, 1.75),
(N'CV010', N'Quan ly kho', 400000, 1.5);

INSERT INTO dbo.CALAMVIEC (MaCa, TenCa, GioBatDau, GioKetThuc) VALUES
(N'CA001', N'Ca sang', N'06:00', N'14:00'),
(N'CA002', N'Ca chieu', N'14:00', N'22:00'),
(N'CA003', N'Ca toi', N'18:00', N'23:00'),
(N'CA004', N'Ca hanh chinh', N'08:00', N'17:00'),
(N'CA005', N'Ca cuoi tuan sang', N'07:00', N'15:00'),
(N'CA006', N'Ca cuoi tuan chieu', N'15:00', N'23:00'),
(N'CA007', N'Ca ngan sang', N'06:00', N'10:00'),
(N'CA008', N'Ca ngan trua', N'10:00', N'14:00'),
(N'CA009', N'Ca ngan chieu', N'14:00', N'18:00'),
(N'CA010', N'Ca ngan toi', N'18:00', N'22:00');

INSERT INTO dbo.NHANVIEN (MaNV, TenNV, SDT, LoaiHopDong, LuongCung, LuongTheoGio, MaCV) VALUES
(N'NV001', N'Nguyen Van An', N'0901111111', N'Full-time', 12000000, 0, N'CV001'),
(N'NV002', N'Tran Thi Binh', N'0902222222', N'Full-time', 9000000, 0, N'CV002'),
(N'NV003', N'Le Van Cuong', N'0903333333', N'Part-time', 0, 35000, N'CV002'),
(N'NV004', N'Pham Thi Dung', N'0904444444', N'Full-time', 8500000, 0, N'CV003'),
(N'NV005', N'Hoang Van Em', N'0905555555', N'Part-time', 0, 30000, N'CV004'),
(N'NV006', N'Vu Thi Phuong', N'0906666666', N'Full-time', 8000000, 0, N'CV003'),
(N'NV007', N'Dang Van Giang', N'0907777777', N'Part-time', 0, 28000, N'CV004'),
(N'NV008', N'Bui Thi Huong', N'0908888888', N'Full-time', 7500000, 0, N'CV004'),
(N'NV009', N'Do Van Kien', N'0909999999', N'Part-time', 0, 32000, N'CV002'),
(N'NV010', N'Nguyen Thi Lan', N'0910000000', N'Full-time', 10000000, 0, N'CV008');

INSERT INTO dbo.CHAMCONG (MaNV, NgayLam, MaCa, GioVao, GioRa, TrangThai) VALUES
(N'NV002', N'2025-04-01', N'CA001', N'2025-04-01 06:05', N'2025-04-01 14:10', N'Di lam'),
(N'NV004', N'2025-04-01', N'CA001', N'2025-04-01 05:58', N'2025-04-01 14:00', N'Di lam'),
(N'NV003', N'2025-04-01', N'CA002', N'2025-04-01 14:02', N'2025-04-01 22:05', N'Di lam'),
(N'NV006', N'2025-04-02', N'CA002', N'2025-04-02 14:00', N'2025-04-02 22:00', N'Di lam'),
(N'NV005', N'2025-04-02', N'CA002', N'2025-04-02 14:00', N'2025-04-02 22:00', N'Di lam'),
(N'NV002', N'2025-04-03', N'CA001', N'2025-04-03 06:03', N'2025-04-03 14:05', N'Di lam'),
(N'NV003', N'2025-04-03', N'CA002', NULL, NULL, N'Vang'),
(N'NV004', N'2025-04-04', N'CA001', NULL, NULL, N'Nghi phep'),
(N'NV002', N'2025-04-05', N'CA001', N'2025-04-05 06:00', N'2025-04-05 14:00', N'Di lam'),
(N'NV009', N'2025-04-05', N'CA002', N'2025-04-05 14:00', N'2025-04-05 22:00', N'Di lam');

INSERT INTO dbo.DANHMUC_SP (MaDM, TenDM) VALUES
(N'DM001', N'Ca phe'),
(N'DM002', N'Tra sua'),
(N'DM003', N'Sinh to'),
(N'DM004', N'Banh ngot'),
(N'DM005', N'Nuoc ep'),
(N'DM006', N'Nuoc ngot'),
(N'DM007', N'Tra trai cay'),
(N'DM008', N'Do uong nong'),
(N'DM009', N'Kem - Trang mien'),
(N'DM010', N'Mon an nhe');

INSERT INTO dbo.NGUYENVATLIEU (MaNVL, TenNVL, DonViTinh, SoLuongTon) VALUES
(N'NVL01', N'Ca phe hat', N'Kg', 50.0),
(N'NVL02', N'Sua tuoi', N'Lit', 30.0),
(N'NVL03', N'Duong', N'Kg', 20.0),
(N'NVL04', N'Tra xanh', N'Kg', 15.0),
(N'NVL05', N'Bot matcha', N'Kg', 5.0),
(N'NVL06', N'Tran chau', N'Kg', 10.0),
(N'NVL07', N'Bo', N'Kg', 8.0),
(N'NVL08', N'Xoai', N'Kg', 12.0),
(N'NVL09', N'Kem tuoi', N'Lit', 6.0),
(N'NVL10', N'Ly nhua', N'Cai', 500.0);

INSERT INTO dbo.NHACUNGCAP (MaNCC, TenNCC, SDT) VALUES
(N'NCC01', N'Cty Ca phe Trung Nguyen', N'02873001234'),
(N'NCC02', N'Cty Thuc Pham Mien Nam', N'02838886666'),
(N'NCC03', N'Cty Tra Thai Nguyen', N'02459991111'),
(N'NCC04', N'Cty Sua Vinamilk', N'02836001234'),
(N'NCC05', N'Cty Duong Bien Hoa', N'02512345678'),
(N'NCC06', N'Cty Trai Cay Mien Tay', N'07091234567'),
(N'NCC07', N'Cty Bao Bi Xanh', N'02866778899'),
(N'NCC08', N'Cty Kem Kido', N'02812345678'),
(N'NCC09', N'Cty Nguyen Lieu Banh Ngot', N'02899887766'),
(N'NCC10', N'Cty Ca phe Highlands', N'02845678901');

INSERT INTO dbo.SANPHAM (MaSP, TenSP, MaDM, GiaBan, TrangThai) VALUES
(N'SP001', N'Ca phe den', N'DM001', 25000, N'Dang ban'),
(N'SP002', N'Ca phe sua', N'DM001', 30000, N'Dang ban'),
(N'SP003', N'Bac xiu', N'DM001', 32000, N'Dang ban'),
(N'SP004', N'Cappuccino', N'DM001', 55000, N'Dang ban'),
(N'SP005', N'Tra sua tran chau', N'DM002', 45000, N'Dang ban'),
(N'SP006', N'Tra xanh', N'DM002', 40000, N'Dang ban'),
(N'SP007', N'Sinh to bo', N'DM003', 55000, N'Dang ban'),
(N'SP008', N'Sinh to xoai', N'DM003', 50000, N'Dang ban'),
(N'SP009', N'Banh tiramisu', N'DM004', 65000, N'Dang ban'),
(N'SP010', N'Banh croissant', N'DM004', 35000, N'Ngung ban');

INSERT INTO dbo.NGUYENVATLIEU (MaNVL, TenNVL, DonViTinh, SoLuongTon) VALUES
(N'NVL11', N'Banh tiramisu thanh pham', N'Cai', 30),
(N'NVL12', N'Banh croissant thanh pham', N'Cai', 30);

INSERT INTO dbo.CONGTHUC (MaSP, MaNVL, DinhMuc) VALUES
(N'SP001', N'NVL01', 0.018),
(N'SP001', N'NVL03', 0.01),
(N'SP002', N'NVL01', 0.018),
(N'SP002', N'NVL02', 0.12),
(N'SP002', N'NVL03', 0.015),
(N'SP003', N'NVL01', 0.012),
(N'SP003', N'NVL02', 0.15),
(N'SP004', N'NVL01', 0.02),
(N'SP004', N'NVL02', 0.1),
(N'SP004', N'NVL09', 0.03),
(N'SP005', N'NVL04', 0.005),
(N'SP005', N'NVL02', 0.15),
(N'SP005', N'NVL03', 0.02),
(N'SP005', N'NVL06', 0.05),
(N'SP006', N'NVL04', 0.005),
(N'SP006', N'NVL03', 0.01),
(N'SP007', N'NVL07', 0.15),
(N'SP007', N'NVL02', 0.1),
(N'SP007', N'NVL03', 0.015),
(N'SP008', N'NVL08', 0.15),
(N'SP008', N'NVL02', 0.1),
(N'SP008', N'NVL03', 0.015),
(N'SP009', N'NVL11', 1),
(N'SP010', N'NVL12', 1);

INSERT INTO dbo.PHIEUNHAP (MaPN, NgayNhap, MaNV, MaNCC, TongTien) VALUES
(N'PN20250101', N'2025-01-05', N'NV001', N'NCC01', 0),
(N'PN20250102', N'2025-01-20', N'NV001', N'NCC04', 0),
(N'PN20250201', N'2025-02-03', N'NV001', N'NCC02', 0),
(N'PN20250202', N'2025-02-15', N'NV010', N'NCC05', 0),
(N'PN20250301', N'2025-03-01', N'NV001', N'NCC01', 0),
(N'PN20250302', N'2025-03-18', N'NV001', N'NCC09', 0),
(N'PN20250401', N'2025-04-01', N'NV001', N'NCC03', 0),
(N'PN20250402', N'2025-04-10', N'NV002', N'NCC02', 0),
(N'PN20250403', N'2025-04-22', N'NV001', N'NCC06', 0),
(N'PN20250501', N'2025-05-02', N'NV001', N'NCC01', 0);

INSERT INTO dbo.CHITIET_PN (MaPN, MaNVL, SoLuong, DonGiaNhap) VALUES
(N'PN20250101', N'NVL01', 20.0, 180000),
(N'PN20250101', N'NVL02', 50.0, 30000),
(N'PN20250102', N'NVL02', 40.0, 30000),
(N'PN20250201', N'NVL03', 30.0, 25000),
(N'PN20250202', N'NVL03', 25.0, 25000),
(N'PN20250301', N'NVL01', 25.0, 185000),
(N'PN20250301', N'NVL09', 15.0, 80000),
(N'PN20250401', N'NVL04', 10.0, 90000),
(N'PN20250402', N'NVL07', 15.0, 80000),
(N'PN20250403', N'NVL08', 30.0, 40000),
(N'PN20250302', N'NVL11', 30, 60000),
(N'PN20250501', N'NVL01', 24, 200000);

-- Phieu xuat mau chi mo ta hao hut/huy/tra NCC; khong phai chuyen ra quay.
INSERT INTO dbo.PHIEUXUAT (MaPX, NgayXuat, MaNV, LoaiXuat) VALUES
(N'PX20250101', N'2025-01-31', N'NV002', N'Huy do hong'),
(N'PX20250201', N'2025-02-28', N'NV002', N'Huy do hong'),
(N'PX20250301', N'2025-03-05', N'NV001', N'Huy do hong'),
(N'PX20250302', N'2025-03-31', N'NV002', N'Huy do hong'),
(N'PX20250401', N'2025-04-03', N'NV001', N'Huy do hong'),
(N'PX20250402', N'2025-04-30', N'NV002', N'Huy do hong'),
(N'PX20250403', N'2025-04-15', N'NV001', N'Tra hang'),
(N'PX20250501', N'2025-05-01', N'NV002', N'Huy do hong'),
(N'PX20250502', N'2025-05-10', N'NV001', N'Huy do hong'),
(N'PX20250503', N'2025-05-25', N'NV002', N'Huy do hong');

INSERT INTO dbo.CHITIET_PX (MaPX, MaNVL, SoLuongXuat) VALUES
(N'PX20250101', N'NVL01', 5.0),
(N'PX20250201', N'NVL02', 8.0),
(N'PX20250301', N'NVL03', 2.0),
(N'PX20250302', N'NVL01', 6.0),
(N'PX20250401', N'NVL02', 1.5),
(N'PX20250402', N'NVL01', 7.0),
(N'PX20250403', N'NVL04', 3.0),
(N'PX20250501', N'NVL02', 10.0),
(N'PX20250502', N'NVL09', 0.5),
(N'PX20250503', N'NVL01', 4.0);

INSERT INTO dbo.HANGTHANHVIEN (MaHang, DiemToiThieu, PhanTramGiam) VALUES
(N'DONG', 0, 0),
(N'BAC', 300, 3),
(N'VANG', 800, 7),
(N'BKIM', 2000, 10),
(N'KIM', 5000, 15),
(N'PLAT', 8000, 18),
(N'DIAM', 12000, 20),
(N'TITA', 20000, 25),
(N'ROYA', 35000, 30),
(N'LEGE', 50000, 35);

INSERT INTO dbo.KHACHHANG (MaKH, TenKH, SDT, DiemTichLuy, MaHang) VALUES
(N'KH001', N'Nguyen Minh Khoa', N'0911111111', 3200, N'BKIM'),
(N'KH002', N'Tran Lan Anh', N'0922222222', 750, N'BAC'),
(N'KH003', N'Le Thanh Dat', N'0933333333', 6100, N'KIM'),
(N'KH004', N'Pham Thu Ha', N'0944444444', 120, N'DONG'),
(N'KH005', N'Hoang Bao Chau', N'0955555555', 1800, N'VANG'),
(N'KH006', N'Vo Quoc Hung', N'0966666666', 9200, N'PLAT'),
(N'KH007', N'Nguyen Thi Mai', N'0977777777', 350, N'BAC'),
(N'KH008', N'Tran Duc Thinh', N'0988888888', 50, N'DONG'),
(N'KH009', N'Ly Khanh Linh', N'0999999999', 14500, N'DIAM'),
(N'KH010', N'Do Van Minh', N'0912345678', 2500, N'BKIM');

INSERT INTO dbo.KHUYENMAI (MaKM, TenKM, NgayBD, NgayKT, PhanTramGiam) VALUES
(N'KM24TET', N'Khuyen mai Tet Giap Thin 2024', N'2024-01-20', N'2024-02-10', 20),
(N'KM24SAL', N'Flash Sale He 2024', N'2024-06-01', N'2024-06-30', 10),
(N'KM24BF', N'Black Friday 2024', N'2024-11-29', N'2024-12-01', 30),
(N'KM24NEL', N'Giang Sinh 2024', N'2024-12-20', N'2024-12-31', 15),
(N'KM25TET', N'Khuyen mai Tet At Ti 2025', N'2025-01-20', N'2025-02-10', 25),
(N'KM25VLT', N'Valentine 14/2', N'2025-02-12', N'2025-02-14', 12),
(N'KM25WM', N'Ngay Phu nu 8/3', N'2025-03-07', N'2025-03-08', 8),
(N'KM25BD', N'Sinh nhat quan 5 tuoi', N'2025-05-01', N'2025-05-31', 25),
(N'KM25SAL', N'Flash Sale He 2025', N'2025-06-01', N'2025-06-30', 10),
(N'KM25BCK', N'Back to School 2025', N'2025-08-15', N'2025-09-15', 5);

INSERT INTO dbo.KHUYENMAI (MaKM, TenKM, NgayBD, NgayKT, PhanTramGiam) VALUES
(N'KM25ONE', N'Uu dai mot ngay', N'2025-04-01', N'2025-04-01', 10);

INSERT INTO dbo.KHUVUC (MaKV, TenKV) VALUES
(N'KV001', N'Trong nha'),
(N'KV002', N'San vuon'),
(N'KV003', N'Tang 2'),
(N'KV004', N'Khu VIP'),
(N'KV005', N'Ban bar'),
(N'KV006', N'Hien ngoai'),
(N'KV007', N'Phong rieng'),
(N'KV008', N'Tang thuong'),
(N'KV009', N'Khu khoi dong'),
(N'KV010', N'Khu khong khoi thuoc');

INSERT INTO dbo.BAN (MaBan, TenBan, MaKV) VALUES
(N'B0001', N'Ban 01', N'KV001'),
(N'B0002', N'Ban 02', N'KV001'),
(N'B0003', N'Ban 03', N'KV001'),
(N'B0004', N'Ban 04', N'KV002'),
(N'B0005', N'Ban 05', N'KV002'),
(N'B0006', N'Ban 06', N'KV003'),
(N'B0007', N'Ban 07', N'KV003'),
(N'B0008', N'Ban 08', N'KV004'),
(N'B0009', N'Ban 09', N'KV004'),
(N'B0010', N'Ban 10', N'KV005');

INSERT INTO dbo.DOITACGIAOHANG (MaDT, TenDT, ChietKhau) VALUES
(N'DT001', N'ShopeeFood', 20.0),
(N'DT002', N'GrabFood', 25.0);

INSERT INTO dbo.PHUONGTHUCTT (MaPT, TenPT) VALUES
(N'PT001', N'Tien mat'),
(N'PT002', N'Chuyen khoan ngan hang'),
(N'PT003', N'Quet the noi dia'),
(N'PT004', N'Vi MoMo'),
(N'PT005', N'ZaloPay'),
(N'PT006', N'VNPay QR'),
(N'PT007', N'ShopeePay'),
(N'PT008', N'Moca (Grab Pay)'),
(N'PT009', N'The tin dung quoc te'),
(N'PT010', N'Diem thuong doi');

UPDATE dbo.SANPHAM SET TrangThai=N'Dang ban' WHERE MaSP='SP010';

INSERT INTO dbo.HOADON (MaHD, NgayLap, MaNV, MaKH, MaBan, MaDT, TongTien, PhiShip, GiamGia, TrangThai, LoaiDon, TenNguoiNhan, SDTNguoiNhan, DiaChiGiao, TrangThaiGiao, MaKM) VALUES
(N'HD20250401', N'2025-04-01 09:15', N'NV004', N'KH001', N'B0001', NULL, 0, 0, 0, N'Chua thanh toan', N'Tai quan', NULL, NULL, NULL, NULL, N'KM25ONE');

INSERT INTO dbo.CHITIET_HD (MaHD, MaSP, SoLuong, GiaBan, GhiChu) VALUES
(N'HD20250401', N'SP002', 2, 30000, NULL),
(N'HD20250401', N'SP009', 1, 65000, N'It duong');

UPDATE dbo.HOADON SET GiamGia=12500 WHERE MaHD='HD20250401';
DECLARE @tt dbo.DanhSachThanhToan;
INSERT INTO @tt(MaPT,SoTien) VALUES('PT001',112500);
EXEC dbo.sp_ThanhToanHoaDon @MaHD='HD20250401', @ThanhToan=@tt, @NgayTT='2025-04-01 09:15';

INSERT INTO dbo.HOADON (MaHD, NgayLap, MaNV, MaKH, MaBan, MaDT, TongTien, PhiShip, GiamGia, TrangThai, LoaiDon, TenNguoiNhan, SDTNguoiNhan, DiaChiGiao, TrangThaiGiao, MaKM) VALUES
(N'HD20250402', N'2025-04-01 10:30', N'NV004', NULL, N'B0003', NULL, 0, 0, 0, N'Chua thanh toan', N'Tai quan', NULL, NULL, NULL, NULL, NULL);

INSERT INTO dbo.CHITIET_HD (MaHD, MaSP, SoLuong, GiaBan, GhiChu) VALUES
(N'HD20250402', N'SP001', 3, 25000, NULL);

UPDATE dbo.HOADON SET GiamGia=0 WHERE MaHD='HD20250402';
DELETE FROM @tt;
INSERT INTO @tt(MaPT,SoTien) VALUES('PT001',75000);
EXEC dbo.sp_ThanhToanHoaDon @MaHD='HD20250402', @ThanhToan=@tt, @NgayTT='2025-04-01 10:30';

INSERT INTO dbo.HOADON (MaHD, NgayLap, MaNV, MaKH, MaBan, MaDT, TongTien, PhiShip, GiamGia, TrangThai, LoaiDon, TenNguoiNhan, SDTNguoiNhan, DiaChiGiao, TrangThaiGiao, MaKM) VALUES
(N'HD20250403', N'2025-04-02 14:00', N'NV006', N'KH002', NULL, N'DT001', 0, 15000, 0, N'Chua thanh toan', N'Giao hang', N'Tran Lan Anh', N'0922222222', N'Dia chi giao hang mau - Thu Duc', N'Cho giao', NULL);

INSERT INTO dbo.CHITIET_HD (MaHD, MaSP, SoLuong, GiaBan, GhiChu) VALUES
(N'HD20250403', N'SP005', 2, 45000, NULL);

UPDATE dbo.HOADON SET GiamGia=2700 WHERE MaHD='HD20250403';
DELETE FROM @tt;
INSERT INTO @tt(MaPT,SoTien) VALUES('PT002',102300);
EXEC dbo.sp_ThanhToanHoaDon @MaHD='HD20250403', @ThanhToan=@tt, @NgayTT='2025-04-02 14:00';
UPDATE dbo.HOADON SET TrangThaiGiao=N'Da giao' WHERE MaHD='HD20250403';

INSERT INTO dbo.HOADON (MaHD, NgayLap, MaNV, MaKH, MaBan, MaDT, TongTien, PhiShip, GiamGia, TrangThai, LoaiDon, TenNguoiNhan, SDTNguoiNhan, DiaChiGiao, TrangThaiGiao, MaKM) VALUES
(N'HD20250404', N'2025-04-03 08:45', N'NV004', N'KH003', N'B0002', NULL, 0, 0, 0, N'Chua thanh toan', N'Tai quan', NULL, NULL, NULL, NULL, NULL);

INSERT INTO dbo.CHITIET_HD (MaHD, MaSP, SoLuong, GiaBan, GhiChu) VALUES
(N'HD20250404', N'SP004', 2, 55000, NULL),
(N'HD20250404', N'SP007', 1, 55000, NULL);

UPDATE dbo.HOADON SET GiamGia=24750 WHERE MaHD='HD20250404';
DELETE FROM @tt;
INSERT INTO @tt(MaPT,SoTien) VALUES('PT001',140250);
EXEC dbo.sp_ThanhToanHoaDon @MaHD='HD20250404', @ThanhToan=@tt, @NgayTT='2025-04-03 08:45';

INSERT INTO dbo.HOADON (MaHD, NgayLap, MaNV, MaKH, MaBan, MaDT, TongTien, PhiShip, GiamGia, TrangThai, LoaiDon, TenNguoiNhan, SDTNguoiNhan, DiaChiGiao, TrangThaiGiao, MaKM) VALUES
(N'HD20250405', N'2025-04-05 16:20', N'NV006', N'KH001', NULL, N'DT002', 0, 20000, 0, N'Chua thanh toan', N'Giao hang', N'Nguyen Minh Khoa', N'0911111111', N'Dia chi giao hang mau - Thu Duc', N'Cho giao', NULL);

INSERT INTO dbo.CHITIET_HD (MaHD, MaSP, SoLuong, GiaBan, GhiChu) VALUES
(N'HD20250405', N'SP005', 1, 45000, NULL),
(N'HD20250405', N'SP007', 1, 55000, NULL);

UPDATE dbo.HOADON SET GiamGia=10000 WHERE MaHD='HD20250405';
DELETE FROM @tt;
INSERT INTO @tt(MaPT,SoTien) VALUES('PT004',110000);
EXEC dbo.sp_ThanhToanHoaDon @MaHD='HD20250405', @ThanhToan=@tt, @NgayTT='2025-04-05 16:20';
UPDATE dbo.HOADON SET TrangThaiGiao=N'Da giao' WHERE MaHD='HD20250405';

INSERT INTO dbo.HOADON (MaHD, NgayLap, MaNV, MaKH, MaBan, MaDT, TongTien, PhiShip, GiamGia, TrangThai, LoaiDon, TenNguoiNhan, SDTNguoiNhan, DiaChiGiao, TrangThaiGiao, MaKM) VALUES
(N'HD20250406', N'2025-04-10 09:30', N'NV004', N'KH004', N'B0001', NULL, 0, 0, 0, N'Chua thanh toan', N'Tai quan', NULL, NULL, NULL, NULL, NULL);

INSERT INTO dbo.CHITIET_HD (MaHD, MaSP, SoLuong, GiaBan, GhiChu) VALUES
(N'HD20250406', N'SP002', 2, 30000, NULL),
(N'HD20250406', N'SP010', 1, 35000, NULL);

UPDATE dbo.HOADON SET GiamGia=0 WHERE MaHD='HD20250406';
DELETE FROM @tt;
INSERT INTO @tt(MaPT,SoTien) VALUES('PT002',95000);
EXEC dbo.sp_ThanhToanHoaDon @MaHD='HD20250406', @ThanhToan=@tt, @NgayTT='2025-04-10 09:30';

INSERT INTO dbo.HOADON (MaHD, NgayLap, MaNV, MaKH, MaBan, MaDT, TongTien, PhiShip, GiamGia, TrangThai, LoaiDon, TenNguoiNhan, SDTNguoiNhan, DiaChiGiao, TrangThaiGiao, MaKM) VALUES
(N'HD20250407', N'2025-04-12 15:00', N'NV006', N'KH002', N'B0004', NULL, 0, 0, 0, N'Chua thanh toan', N'Tai quan', NULL, NULL, NULL, NULL, NULL);

INSERT INTO dbo.CHITIET_HD (MaHD, MaSP, SoLuong, GiaBan, GhiChu) VALUES
(N'HD20250407', N'SP005', 1, 45000, NULL),
(N'HD20250407', N'SP006', 1, 40000, NULL);

UPDATE dbo.HOADON SET GiamGia=5950 WHERE MaHD='HD20250407';
DELETE FROM @tt;
INSERT INTO @tt(MaPT,SoTien) VALUES('PT003',79050);
EXEC dbo.sp_ThanhToanHoaDon @MaHD='HD20250407', @ThanhToan=@tt, @NgayTT='2025-04-12 15:00';

INSERT INTO dbo.HOADON (MaHD, NgayLap, MaNV, MaKH, MaBan, MaDT, TongTien, PhiShip, GiamGia, TrangThai, LoaiDon, TenNguoiNhan, SDTNguoiNhan, DiaChiGiao, TrangThaiGiao, MaKM) VALUES
(N'HD20250408', N'2025-04-20 13:30', N'NV006', N'KH003', N'B0003', NULL, 0, 0, 0, N'Chua thanh toan', N'Tai quan', NULL, NULL, NULL, NULL, NULL);

INSERT INTO dbo.CHITIET_HD (MaHD, MaSP, SoLuong, GiaBan, GhiChu) VALUES
(N'HD20250408', N'SP004', 2, 55000, NULL),
(N'HD20250408', N'SP005', 2, 45000, NULL);

UPDATE dbo.HOADON SET GiamGia=30000 WHERE MaHD='HD20250408';
DELETE FROM @tt;
INSERT INTO @tt(MaPT,SoTien) VALUES('PT001',170000);
EXEC dbo.sp_ThanhToanHoaDon @MaHD='HD20250408', @ThanhToan=@tt, @NgayTT='2025-04-20 13:30';

INSERT INTO dbo.HOADON (MaHD, NgayLap, MaNV, MaKH, MaBan, MaDT, TongTien, PhiShip, GiamGia, TrangThai, LoaiDon, TenNguoiNhan, SDTNguoiNhan, DiaChiGiao, TrangThaiGiao, MaKM) VALUES
(N'HD20250409', N'2025-05-01 08:00', N'NV004', N'KH001', N'B0002', NULL, 0, 0, 0, N'Chua thanh toan', N'Tai quan', NULL, NULL, NULL, NULL, NULL);

INSERT INTO dbo.CHITIET_HD (MaHD, MaSP, SoLuong, GiaBan, GhiChu) VALUES
(N'HD20250409', N'SP002', 1, 30000, N'It da'),
(N'HD20250409', N'SP002', 2, 30000, N'Khong da');

UPDATE dbo.HOADON SET GiamGia=9000 WHERE MaHD='HD20250409';
DELETE FROM @tt;
INSERT INTO @tt(MaPT,SoTien) VALUES('PT002',30000),('PT005',51000);
EXEC dbo.sp_ThanhToanHoaDon @MaHD='HD20250409', @ThanhToan=@tt, @NgayTT='2025-05-01 08:00';

INSERT INTO dbo.HOADON (MaHD, NgayLap, MaNV, MaKH, MaBan, MaDT, TongTien, PhiShip, GiamGia, TrangThai, LoaiDon, TenNguoiNhan, SDTNguoiNhan, DiaChiGiao, TrangThaiGiao, MaKM) VALUES
(N'HD20250410', N'2025-05-10 09:00', N'NV004', NULL, N'B0003', NULL, 0, 0, 0, N'Chua thanh toan', N'Tai quan', NULL, NULL, NULL, NULL, NULL);

INSERT INTO dbo.CHITIET_HD (MaHD, MaSP, SoLuong, GiaBan, GhiChu) VALUES
(N'HD20250410', N'SP005', 1, 45000, NULL),
(N'HD20250410', N'SP006', 1, 40000, NULL);

UPDATE dbo.HOADON SET GiamGia=0 WHERE MaHD='HD20250410';

INSERT INTO dbo.LICHSU_DIEM (MaKH, NgayGD, Loai, SoDiem, LyDo) VALUES
(N'KH001', N'2025-04-01', N'Tru', 50, N'Doi diem lay voucher giam 50k'),
(N'KH003', N'2025-01-15', N'Cong', 500, N'Thuong sinh nhat khach hang');

INSERT INTO dbo.DANHMUC_THUCHI (MaDMTC, TenDMTC, Loai) VALUES
(N'TC001', N'Tien dien', N'Chi'),
(N'TC002', N'Tien nuoc', N'Chi'),
(N'TC003', N'Thue mat bang', N'Chi'),
(N'TC004', N'Mua cong cu dung cu', N'Chi'),
(N'TC005', N'Luong nhan vien', N'Chi'),
(N'TC006', N'Sua chua thiet bi', N'Chi'),
(N'TC007', N'Phi Internet - dien thoai', N'Chi'),
(N'TC008', N'Mua nguyen lieu phat sinh', N'Chi'),
(N'TC009', N'Thu tien dat coc ban', N'Thu'),
(N'TC010', N'Thu phi to chuc su kien', N'Thu'),
(N'TC011', N'Tra no nha cung cap', N'Chi');

INSERT INTO dbo.TAISAN_CODINH (MaTS, TenTS, NgayMua, GiaTri, SoThangKH) VALUES
(N'TS001', N'May pha ca phe La Marzocco Linea', N'2022-01-15', 85000000, 60),
(N'TS002', N'May xay ca phe Mahlkonig E65S', N'2022-01-15', 25000000, 48),
(N'TS003', N'Tu mat Sanaky VH-408W', N'2022-03-01', 12000000, 60),
(N'TS004', N'Dieu hoa Daikin 18000BTU', N'2022-06-01', 15000000, 84),
(N'TS005', N'He thong camera Hikvision 8 mat', N'2023-01-10', 8000000, 36),
(N'TS006', N'May pha tra Jura E8', N'2023-03-15', 18000000, 48),
(N'TS007', N'May tinh thu ngan POS Dell', N'2023-06-01', 12000000, 36),
(N'TS008', N'Bang hieu LED mat tien', N'2022-08-01', 5000000, 24),
(N'TS009', N'Bo ban ghe ngoai troi 10 bo', N'2022-09-01', 20000000, 60),
(N'TS010', N'He thong am thanh Bose', N'2023-01-01', 15000000, 48);

INSERT INTO dbo.PHIEUTHUCHI (MaTC, NgayLap, MaDMTC, MaNV, MaTS, SoTien, GhiChu) VALUES
(N'TC20250401', N'2025-04-01', N'TC003', N'NV001', NULL, 15000000, N'Thue mat bang thang 4/2025'),
(N'TC20250402', N'2025-04-03', N'TC001', N'NV001', NULL, 2500000, N'Tien dien thang 4/2025'),
(N'TC20250403', N'2025-04-05', N'TC002', N'NV001', NULL, 400000, N'Tien nuoc thang 4/2025'),
(N'TC20250404', N'2025-04-07', N'TC007', N'NV001', NULL, 350000, N'Phi Internet thang 4'),
(N'TC20250405', N'2025-04-10', N'TC004', N'NV001', NULL, 850000, N'Mua them ly va ong hut'),
(N'TC20250406', N'2025-04-15', N'TC006', N'NV001', N'TS007', 500000, N'Bao tri may tinh POS'),
(N'TC20250501', N'2025-05-01', N'TC003', N'NV001', NULL, 15000000, N'Thue mat bang thang 5/2025'),
(N'TC20250502', N'2025-05-05', N'TC001', N'NV001', NULL, 2800000, N'Tien dien thang 5/2025'),
(N'TC20250503', N'2025-05-08', N'TC006', N'NV001', N'TS001', 800000, N'Bao tri may pha ca phe'),
(N'TC20250504', N'2025-05-12', N'TC004', N'NV001', NULL, 450000, N'Mua hop dung thuc pham');

INSERT INTO dbo.CONGNO_NCC (MaCN, MaPN, TienNo, HanTra) VALUES
(N'CN20250101', N'PN20250101', 2000000, N'2025-02-15'),
(N'CN20250102', N'PN20250102', 1000000, N'2025-03-01'),
(N'CN20250201', N'PN20250201', 750000, N'2025-03-20'),
(N'CN20250301', N'PN20250301', 3000000, N'2025-04-30'),
(N'CN20250302', N'PN20250302', 1800000, N'2025-05-01'),
(N'CN20250401', N'PN20250401', 900000, N'2025-05-15'),
(N'CN20250402', N'PN20250402', 1200000, N'2025-06-01'),
(N'CN20250403', N'PN20250403', 1200000, N'2025-06-10'),
(N'CN20250404', N'PN20250501', 2500000, N'2025-07-01');

EXEC dbo.sp_TraCongNo 'CN20250101','TR00000001','NV001','PT002',2000000,'2025-02-15';
EXEC dbo.sp_TraCongNo 'CN20250102','TR00000002','NV001','PT002',1000000,'2025-03-01';
EXEC dbo.sp_TraCongNo 'CN20250201','TR00000003','NV001','PT002',750000,'2025-03-20';
EXEC dbo.sp_TraCongNo 'CN20250301','TR00000004','NV001','PT002',3000000,'2025-04-30';
EXEC dbo.sp_TraCongNo 'CN20250302','TR00000005','NV001','PT002',1800000,'2025-05-01';
EXEC dbo.sp_TraCongNo 'CN20250401','TR00000006','NV001','PT002',300000,'2025-05-10';

INSERT INTO dbo.VAITRO (MaVT, TenVT) VALUES
(N'ADMIN', N'Quan tri'),
(N'CASH', N'Thu ngan'),
(N'STORE', N'Quan ly kho');

INSERT INTO dbo.TAIKHOAN (MaNV, TenDangNhap, MatKhauHash, MaVT, HoatDong) VALUES
(N'NV001', N'admin', N'!DISABLED_REPLACE_WITH_APP_PASSWORD_HASH!', N'ADMIN', 0),
(N'NV004', N'thungan', N'!DISABLED_REPLACE_WITH_APP_PASSWORD_HASH!', N'CASH', 0),
(N'NV010', N'kho', N'!DISABLED_REPLACE_WITH_APP_PASSWORD_HASH!', N'STORE', 0);

INSERT INTO dbo.BANGLUONG (MaNV, Thang, Nam, NgayChot, LuongCoBan, SoGioLam, DonGiaGio, TienTangCa, PhuCap, Thuong, KhauTru) VALUES
(N'NV001', 4, 2025, N'2025-04-30', 12000000, 0, 0, 0, 2000000, 500000, 0),
(N'NV003', 4, 2025, N'2025-04-30', 0, 80, 35000, 0, 500000, 0, 0);

INSERT INTO dbo.PHIEUKIEMKE(MaKK,NgayKK,MaNV)
VALUES('KKDEMO0001',CONVERT(DATE,SYSDATETIME()),'NV001');
INSERT INTO dbo.CHITIET_KK(MaKK,MaNVL,TonHeThong,TonThucTe,LyDo)
SELECT 'KKDEMO0001',MaNVL,SoLuongTon,SoLuongTon,NULL FROM dbo.NGUYENVATLIEU;

-- Kiem tra bat bien truoc khi COMMIT. Neu loi thi rollback toan bo seed.
IF EXISTS (SELECT 1 FROM dbo.HOADON h WHERE h.TongTien<>
  COALESCE((SELECT SUM(CONVERT(DECIMAL(18,0),SoLuong)*GiaBan) FROM dbo.CHITIET_HD WHERE MaHD=h.MaHD),0))
    THROW 51201,N'Tong hoa don sai.',1;
IF EXISTS (SELECT 1 FROM dbo.HOADON h WHERE h.TrangThai=N'Da thanh toan' AND h.ThanhTien<>
  COALESCE((SELECT SUM(SoTien) FROM dbo.CHITIET_THANHTOAN WHERE MaHD=h.MaHD),0))
    THROW 51202,N'Phan bo thanh toan sai.',1;
IF EXISTS (SELECT 1 FROM dbo.PHIEUNHAP p WHERE p.TongTien<>
  COALESCE((SELECT SUM(ROUND(SoLuong*DonGiaNhap,0)) FROM dbo.CHITIET_PN WHERE MaPN=p.MaPN),0))
    THROW 51203,N'Tong phieu nhap sai.',1;
IF EXISTS (SELECT 1 FROM dbo.vw_CongNoNCC WHERE ConNo<0)
    THROW 51204,N'Tra no vuot muc.',1;
IF EXISTS (SELECT 1 FROM dbo.KHACHHANG k CROSS APPLY
  (SELECT TOP(1) MaHang FROM dbo.HANGTHANHVIEN WHERE DiemToiThieu<=k.DiemTichLuy ORDER BY DiemToiThieu DESC) h
  WHERE k.MaHang<>h.MaHang)
    THROW 51205,N'Hang thanh vien sai.',1;
COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
GO
-- Ket qua mau: 9 don da thanh toan, 1 don dang mo; ban B0003 dang phuc vu.
SELECT MaHD,TongTien,GiamGia,PhiShip,ThanhTien,TrangThai FROM dbo.HOADON ORDER BY MaHD;
SELECT * FROM dbo.vw_TrangThaiBan ORDER BY MaBan;
SELECT * FROM dbo.vw_CongNoNCC ORDER BY MaCN;
SELECT MaNVL,TenNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL;
GO
