-- Chỉ chạy trên baseline demo của runner DashboardAnalytics, trước migration001.
-- Giao dịch lịch sử được chốt qua procedure baseline; không sửa bill đã chốt/tắt trigger.
SET NOCOUNT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G07[_]%' OR OBJECT_ID('dbo.G07TestMarker') IS NULL
 THROW 53501,N'Chỉ DB test có marker G07.',1;
INSERT dbo.NGUYENVATLIEU(MaNVL,TenNVL,DonViTinh,SoLuongTon) VALUES('DZ001',N'Fixture analytics',N'g',100000);
DECLARE @category char(5)=(SELECT TOP(1) MaDM FROM dbo.DANHMUC_SP ORDER BY MaDM),@n int=1,
 @product char(5),@bill char(10),@time datetime2(0),@payments dbo.DanhSachThanhToan;
WHILE @n<=61
BEGIN
 SET @product='X'+RIGHT('0000'+CONVERT(varchar(4),@n),4);
 INSERT dbo.SANPHAM(MaSP,TenSP,MaDM,GiaBan) VALUES(@product,N'Món analytics '+CONVERT(nvarchar(4),@n),@category,1000);
 INSERT dbo.CONGTHUC VALUES(@product,'DZ001',0.001);
 SET @bill='DA'+RIGHT('00000000'+CONVERT(varchar(8),@n),8);
 INSERT dbo.HOADON(MaHD,NgayLap,MaNV,LoaiDon) VALUES(@bill,'20260114 08:00','NV004',N'Mang di');
 INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan) VALUES(@bill,@product,2,1000);
 DELETE FROM @payments;INSERT @payments(MaPT,SoTien) VALUES('PT001',2000);
 SET @time=DATEADD(second,@n,CONVERT(datetime2(0),'20260115 10:00'));
 EXEC dbo.sp_ThanhToanHoaDon @bill,@payments,@time;
 SET @n+=1;
END;
-- 2 dòng món × 2 phương thức, giảm500 + ship1000: khách trả5500.
INSERT dbo.HOADON(MaHD,NgayLap,MaNV,LoaiDon,PhiShip,MaDT,TenNguoiNhan,SDTNguoiNhan,DiaChiGiao,TrangThaiGiao)
 VALUES('DA00000062','20260114 08:00','NV004',N'Giao hang',1000,'DT001',N'Fixture','0900000000',N'Test',N'Cho giao');
INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan) VALUES('DA00000062','X0001',2,1000),('DA00000062','X0002',3,1000);
UPDATE dbo.HOADON SET GiamGia=500 WHERE MaHD='DA00000062';
DELETE FROM @payments;INSERT @payments(MaPT,SoTien) VALUES('PT001',3000),('PT002',2500);
EXEC dbo.sp_ThanhToanHoaDon 'DA00000062',@payments,'20260115 11:59:59';
-- Hai mốc kỳ trước; hóa đơn đúng Until bị loại khỏi kỳ hiện tại.
SET @n=63;
WHILE @n<=65
BEGIN
 SET @bill='DA'+RIGHT('00000000'+CONVERT(varchar(8),@n),8);
 SET @time=CASE @n WHEN 63 THEN '20260114 10:00' WHEN 64 THEN '20251215 10:00' ELSE '20260115 12:00' END;
 INSERT dbo.HOADON(MaHD,NgayLap,MaNV,LoaiDon) VALUES(@bill,DATEADD(day,-1,@time),'NV004',N'Mang di');
 INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan) VALUES(@bill,'X0001',CASE WHEN @n=64 THEN 20 ELSE 10 END,1000);
 DELETE FROM @payments;INSERT @payments(MaPT,SoTien) VALUES('PT001',CASE WHEN @n=64 THEN 20000 ELSE 10000 END);
 EXEC dbo.sp_ThanhToanHoaDon @bill,@payments,@time;
 SET @n+=1;
END;
INSERT dbo.HOADON(MaHD,NgayLap,MaNV,LoaiDon,TrangThai) VALUES
 ('DA00000066','20260114 08:00','NV004',N'Mang di',N'Chua thanh toan'),
 ('DA00000067','20260114 08:00','NV004',N'Mang di',N'Huy');
DECLARE @expense char(5)=(SELECT TOP(1) MaDMTC FROM dbo.DANHMUC_THUCHI WHERE Loai=N'Chi' ORDER BY MaDMTC);
INSERT dbo.PHIEUTHUCHI(MaTC,NgayLap,MaDMTC,MaNV,SoTien) VALUES
 ('DA00000001','20260115 10:00',@expense,'NV001',1000),
 ('DA00000002','20260114 10:00',@expense,'NV001',2000),
 ('DA00000003','20251215 10:00',@expense,'NV001',500);
-- Thay giá sau bán: aggregate phải dùng snapshot dòng1000, không giá menu999999.
UPDATE dbo.SANPHAM SET GiaBan=999999 WHERE MaSP LIKE 'X00%';
