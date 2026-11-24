-- Fixture bộ promo-one-per-item (migration 044): chỉ DB test riêng, dựng qua procedure công khai.
-- Quy ước: mỗi món chỉ nhận một KM giảm; hạng tính sau KM. Số liệu kỳ vọng ghi trong từng script.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_PromoOnePerItem[_]%' OR OBJECT_ID('dbo.PromoOnePerItemTestMarker') IS NULL
 THROW 53801,N'Chỉ DB test PromoOnePerItem có marker.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=44) THROW 53801,N'Cần schema044.',1;
IF OBJECT_ID('dbo.fn_UuDaiChon') IS NULL OR OBJECT_ID('dbo.fn_UuDaiCap') IS NULL
 OR OBJECT_ID('dbo.fn_UuDaiTong') IS NULL OR OBJECT_ID('dbo.fn_UuDaiChia') IS NULL
 THROW 53801,N'Thiếu hàm chọn tổ hợp 044.',1;
IF EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE MaKM LIKE 'KM93%') THROW 53801,N'DB đã có dữ liệu KM93; dùng DB mới.',1;
IF EXISTS(SELECT 1 FROM dbo.KHACHHANG WHERE MaKH IN('KH931','KH932')) THROW 53801,N'DB đã có khách test.',1;
IF OBJECT_ID('dbo.PromoOnePerItemBaseline') IS NULL
 CREATE TABLE dbo.PromoOnePerItemBaseline(Name varchar(25) NOT NULL PRIMARY KEY,Value decimal(38,4) NOT NULL);

UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV IN('NV001','NV004');
DECLARE @a int,@s binary(8),@id uniqueidentifier,@today date=CONVERT(date,SYSDATETIME());
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';

-- Tạm dừng KM seed để bộ test chỉ phụ thuộc KM93 (tránh trùng cửa sổ ngày lễ).
UPDATE dbo.KHUYENMAI SET TrangThai='TamDung' WHERE MaKM NOT LIKE 'KM93%';

-- Nguyên liệu và món riêng của bộ test (không đụng seed).
INSERT dbo.NGUYENVATLIEU(MaNVL,TenNVL,DonViTinh,SoLuongTon) VALUES
('NVL93',N'One-per-item nguyên liệu bán',N'Kg',100000),
('NVL94',N'One-per-item nguyên liệu quà',N'Kg',10000);
INSERT dbo.SANPHAM(MaSP,TenSP,MaDM,GiaBan,TrangThai) VALUES
('SP940',N'One-per-item A',N'DM004',100000,N'Dang ban'),
('SP941',N'One-per-item B',N'DM004',60000,N'Dang ban'),
('SP942',N'One-per-item C',N'DM004',40000,N'Dang ban'),
('SP943',N'One-per-item D',N'DM004',20000,N'Dang ban'),
('SP944',N'One-per-item quà',N'DM004',10000,N'Dang ban'),
('SP945',N'One-per-item A2',N'DM004',100000,N'Dang ban'),
('SP946',N'One-per-item B2',N'DM004',60000,N'Dang ban'),
('SP947',N'One-per-item C2',N'DM004',40000,N'Dang ban'),
('SP948',N'One-per-item E',N'DM004',50000,N'Dang ban'),
('SP949',N'One-per-item F',N'DM004',30000,N'Dang ban'),
('SP950',N'One-per-item G',N'DM004',20000,N'Dang ban'),
('SP951',N'One-per-item H (PP04)',N'DM004',100000,N'Dang ban'),
('SP952',N'One-per-item tải chồng chéo',N'DM004',100000,N'Dang ban');
INSERT dbo.CONGTHUC(MaSP,MaNVL,DinhMuc) VALUES
('SP940','NVL93',1.0),('SP941','NVL93',1.0),('SP942','NVL93',1.0),('SP943','NVL93',1.0),
('SP944','NVL94',1.0),
('SP945','NVL93',1.0),('SP946','NVL93',1.0),('SP947','NVL93',1.0),('SP948','NVL93',1.0),
('SP949','NVL93',1.0),('SP950','NVL93',1.0),('SP951','NVL93',1.0),('SP952','NVL93',1.0);

-- Khách: KH931 hạng DONG 0%, KH932 hạng VANG 5%.
INSERT dbo.KHACHHANG(MaKH,TenKH,SDT,DiemTichLuy,MaHang) VALUES
('KH931',N'One-per-item khach thuong',N'0900000931',0,'DONG'),
('KH932',N'One-per-item khach vang',N'0900000932',0,'VANG');

-- KM giảm toàn năm, đối tượng ALL; một KM tặng; một KM ALL tạm dừng dùng cho legacy.
DECLARE @km TABLE(Code char(10),Name nvarchar(100),Discount int,Items nvarchar(max),Kind varchar(10),
 Status varchar(10),Gifts nvarchar(max));
INSERT @km VALUES
('KM93000001',N'One-per-item A 10%',10,'["SP940"]','Giam','DangChay',NULL),
('KM93000002',N'One-per-item B 30%',30,'["SP941"]','Giam','DangChay',NULL),
('KM93000003',N'One-per-item B+C 40%',40,'["SP941","SP942"]','Giam','DangChay',NULL),
('KM93000004',N'One-per-item ALL 5%',5,NULL,'Giam','DangChay',NULL),
('KM93000005',N'One-per-item tặng quà',0,NULL,'Tang','DangChay','[{"Product":"SP944","Quantity":1}]'),
('KM93000006',N'One-per-item ALL 50% tạm dừng (legacy)',50,NULL,'Giam','TamDung',NULL),
('KM93000007',N'One-per-item D 1%',1,'["SP943"]','Giam','DangChay',NULL),
('KM93000008',N'One-per-item F 10% (a)',10,'["SP949"]','Giam','DangChay',NULL),
('KM93000009',N'One-per-item F 10% (b)',10,'["SP949"]','Giam','DangChay',NULL),
('KM93000010',N'One-per-item G 100%',100,'["SP950"]','Giam','DangChay',NULL),
('KM93000011',N'One-per-item A2 10%',10,'["SP945"]','Giam','DangChay',NULL),
('KM93000012',N'One-per-item B2 50%',50,'["SP946"]','Giam','DangChay',NULL),
('KM93000013',N'One-per-item B2+C2 25%',25,'["SP946","SP947"]','Giam','DangChay',NULL),
('KM93000014',N'One-per-item H 20%',20,'["SP951"]','Giam','DangChay',NULL);
-- 16 KM chồng chéo trên cùng món SP952 (1%–16%) để vượt ngưỡng duyệt chính xác 15.
INSERT @km
 SELECT CONCAT('KM',93000019+n),N'One-per-item tải '+CONVERT(nvarchar(2),n)+N'%',
  n,'["SP952"]','Giam','DangChay',NULL FROM(VALUES(1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16))v(n);
DECLARE @code char(10),@name nvarchar(100),@d int,@ij nvarchar(max),@kd varchar(10),@st varchar(10),@gj nvarchar(max);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Code,Name,Discount,Items,Kind,Status,Gifts FROM @km ORDER BY Code;
OPEN c; FETCH NEXT FROM c INTO @code,@name,@d,@ij,@kd,@st,@gj;
WHILE @@FETCH_STATUS=0
BEGIN
 SET @id=NEWID();
 EXEC dbo.sp_PromotionWrite @Actor=@a,@Stamp=@s,@Action='Create',@Code=@code,@RequestId=@id,
  @Name=@name,@StartDate='2000-01-01',@EndDate='2000-12-31',@Discount=@d,@Audience='ALL',@Status=@st,
  @TiersJson=NULL,@ItemsJson=@ij,@Kind=@kd,@GiftJson=@gj;
 FETCH NEXT FROM c INTO @code,@name,@d,@ij,@kd,@st,@gj;
END;
CLOSE c; DEALLOCATE c;
IF (SELECT COUNT(*) FROM dbo.KHUYENMAI WHERE MaKM LIKE 'KM93%')<>30 THROW 53801,N'Thiếu KM test.',1;
IF EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE MaKM LIKE 'KM93%' AND (YEAR(NgayBD)<>2000 OR YEAR(NgayKT)<>2000))
 THROW 53801,N'Ngày KM test chưa chuẩn hóa năm tham chiếu.',1;

-- Kiểm tra nhanh fixture: A+B+C chọn KM1+KM3 = 50 000 (không cộng KM2 trên B).
DECLARE @json nvarchar(max)=(SELECT * FROM(VALUES('SP940',CONVERT(decimal(18,0),100000)),
 ('SP941',CONVERT(decimal(18,0),60000)),('SP942',CONVERT(decimal(18,0),40000)))v(Product,Amount)
 FOR JSON PATH,INCLUDE_NULL_VALUES);
IF EXISTS(SELECT 1 FROM dbo.fn_UuDaiTong(@today,'KH931',NULL,@json) WHERE PromotionDiscount<>50000)
 THROW 53801,N'Fixture: tổ hợp A+B+C phải giảm 50 000.',1;
IF (SELECT COUNT(*) FROM dbo.fn_UuDaiChia(@today,'KH931',NULL,@json))<>2
 THROW 53801,N'Fixture: A+B+C phải chọn đúng 2 KM.',1;

-- Baseline để so regression ở 04.
INSERT dbo.PromoOnePerItemBaseline(Name,Value)
 SELECT 'SeedPaidCount',CONVERT(decimal(38,4),COUNT(*)) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today
 UNION ALL SELECT 'SeedPaidGross',COALESCE(SUM(CONVERT(decimal(38,4),TongTien)),0) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today
 UNION ALL SELECT 'SeedPaidDiscount',COALESCE(SUM(CONVERT(decimal(38,4),GiamGia)),0) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today
 UNION ALL SELECT 'SeedPaidAmount',COALESCE(SUM(CONVERT(decimal(38,4),ThanhTien)),0) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today
 UNION ALL SELECT 'StockNVL93',CONVERT(decimal(38,4),(SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL93'))
 UNION ALL SELECT 'StockNVL94',CONVERT(decimal(38,4),(SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL94'))
 UNION ALL SELECT 'PointsKH931',CONVERT(decimal(38,4),(SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH931'))
 UNION ALL SELECT 'PointsKH932',CONVERT(decimal(38,4),(SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH932'))
 UNION ALL SELECT 'OpenOrderCount',CONVERT(decimal(38,4),COUNT(*)) FROM dbo.HOADON WHERE TrangThai=N'Chua thanh toan';
IF (SELECT COUNT(*) FROM dbo.PromoOnePerItemBaseline)<>9 THROW 53801,N'Baseline thiếu.',1;
