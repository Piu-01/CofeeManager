-- Fixture prerequisite audit, không sửa paid/date/receipt hoặc bỏ trigger để dựng kết quả.
SET NOCOUNT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G08[_]%' OR OBJECT_ID('dbo.G08TestMarker') IS NULL
 THROW 53601,N'Chỉ DB test G08 có marker.',1;
IF (SELECT MAX(Version) FROM dbo.SchemaMigrations)<>27 THROW 53601,N'Cần schema027.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.SANPHAM WHERE MaSP='SP001' AND GiaBan=25000)
 OR NOT EXISTS(SELECT 1 FROM dbo.CONGTHUC WHERE MaSP='SP001' AND MaNVL='NVL01' AND DinhMuc=0.018)
 OR NOT EXISTS(SELECT 1 FROM dbo.CONGTHUC WHERE MaSP='SP001' AND MaNVL='NVL03' AND DinhMuc=0.010)
 THROW 53601,N'Giá/công thức khác fixture E2E, không thay expected để né lỗi.',1;
-- Phát sinh mới được tạo qua BLL/public procedure trong E2EProbe.

