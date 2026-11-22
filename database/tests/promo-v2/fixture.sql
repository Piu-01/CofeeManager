-- Fixture bộ promo-v2 (migration 042): chỉ DB test riêng, dựng qua procedure công khai.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_PromoV2[_]%' OR OBJECT_ID('dbo.PromoV2TestMarker') IS NULL
 THROW 53701,N'Chỉ DB test PromoV2 có marker.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=42) THROW 53701,N'Cần schema042.',1;
IF OBJECT_ID('dbo.KHUYENMAI_MON_TANG') IS NULL OR COL_LENGTH('dbo.KHUYENMAI','LoaiUuDai') IS NULL
 OR COL_LENGTH('dbo.CHITIET_HD','MaKMQuaTang') IS NULL OR OBJECT_ID('dbo.fn_NgayTrongKy') IS NULL
 OR OBJECT_ID('dbo.fn_QuaTangApDung') IS NULL
 THROW 53701,N'Thiếu object 042.',1;
IF EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE MaKM LIKE 'KM92%') THROW 53701,N'DB đã có dữ liệu KM92; dùng DB mới.',1;
IF EXISTS(SELECT 1 FROM dbo.KHACHHANG WHERE MaKH IN('KH921','KH922','KH923')) THROW 53701,N'DB đã có khách test.',1;
IF OBJECT_ID('dbo.PromoV2Baseline') IS NULL
 CREATE TABLE dbo.PromoV2Baseline(Name varchar(25) NOT NULL PRIMARY KEY,Value decimal(38,4) NOT NULL);

UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV='NV001';
DECLARE @a int,@s binary(8),@id uniqueidentifier,@today date=CONVERT(date,SYSDATETIME());
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';

-- Nguyên liệu và món riêng của bộ test (không đụng seed).
INSERT dbo.NGUYENVATLIEU(MaNVL,TenNVL,DonViTinh,SoLuongTon) VALUES
('NVL91',N'Promo v2 ban thanh pham',N'Kg',100),
('NVL92',N'Promo v2 qua tang',N'Kg',10);
INSERT dbo.SANPHAM(MaSP,TenSP,MaDM,GiaBan,TrangThai) VALUES
('SP920',N'Promo v2 mon ban',N'DM001',50000,N'Dang ban'),
('SP921',N'Promo v2 qua A',N'DM004',12000,N'Dang ban'),
('SP922',N'Promo v2 qua B',N'DM004',15000,N'Dang ban'),
('SP923',N'Promo v2 mon theo hang',N'DM004',20000,N'Dang ban');
INSERT dbo.CONGTHUC(MaSP,MaNVL,DinhMuc) VALUES
('SP920','NVL91',1.0),
('SP921','NVL92',1.0),
('SP922','NVL92',2.0),
('SP923','NVL91',0.5);

-- Khách: KH921 hạng DONG 0%, KH922 hạng VANG 5%, KH923 khách mới hạng DONG 0%.
INSERT dbo.KHACHHANG(MaKH,TenKH,SDT,DiemTichLuy,MaHang) VALUES
('KH921',N'Promo v2 khach thuong',N'0900000921',0,'DONG'),
('KH922',N'Promo v2 khach vang',N'0900000922',0,'VANG'),
('KH923',N'Promo v2 khach moi',N'0900000923',0,'DONG');

-- KM: giảm theo món/hạng/vãng lai, tặng món, tắt, hết hạn, tương lai, vắt năm, KT trống.
DECLARE @km TABLE(Code char(10),Name nvarchar(100),StartMonth int,StartDay int,EndMonth int,EndDay int,
 Discount bigint NULL,Audience varchar(10),Status varchar(10),Tiers nvarchar(max),Items nvarchar(max),
 Kind varchar(10),Gifts nvarchar(max));
INSERT @km VALUES
('KM92000001',N'Promo v2 mua vuot nam 10%',11,1,2,28,10,'ALL','DangChay',NULL,NULL,'Giam',NULL),
('KM92000002',N'Promo v2 giam 20% SP920',1,1,12,31,20,'ALL','DangChay',NULL,'["SP920"]','Giam',NULL),
('KM92000003',N'Promo v2 hang BAC/VANG 15% SP920',1,1,12,31,15,'TIER','DangChay','["BAC","VANG"]','["SP920"]','Giam',NULL),
('KM92000004',N'Promo v2 khach vang lai 5%',1,1,12,31,5,'WALKIN','DangChay',NULL,NULL,'Giam',NULL),
('KM92000005',N'Promo v2 tang SP921 x2',1,1,12,31,0,'ALL','DangChay',NULL,NULL,'Tang','[{"Product":"SP921","Quantity":2}]'),
('KM92000006',N'Promo v2 tang hai mon hang VANG',1,1,12,31,0,'TIER','DangChay','["VANG"]',NULL,'Tang','[{"Product":"SP921","Quantity":1},{"Product":"SP922","Quantity":1}]'),
('KM92000007',N'Promo v2 tang SP922 hang VANG',1,1,12,31,0,'TIER','DangChay','["VANG"]',NULL,'Tang','[{"Product":"SP922","Quantity":1}]'),
('KM92000008',N'Promo v2 tang tuong lai',6,1,6,30,0,'ALL','DangChay',NULL,NULL,'Tang','[{"Product":"SP922","Quantity":1}]'),
('KM92000009',N'Promo v2 tang da tam dung',1,1,12,31,0,'ALL','TamDung',NULL,NULL,'Tang','[{"Product":"SP921","Quantity":3}]'),
('KM92000010',N'Promo v2 giam da tam dung',1,1,12,31,50,'ALL','TamDung',NULL,NULL,'Giam',NULL),
('KM92000011',N'Promo v2 het han hom qua',9,1,11,22,50,'ALL','DangChay',NULL,NULL,'Giam',NULL),
('KM92000012',N'Promo v2 tuong lai 50%',12,1,0,0,50,'ALL','DangChay',NULL,NULL,'Giam',NULL),
('KM92000013',N'Promo v2 tu hom nay khong KT',0,0,0,0,6,'TIER','DangChay','["KIM"]',NULL,'Giam',NULL),
('KM92000014',N'Promo v2 giam 25% mon qua SP921',1,1,12,31,25,'ALL','DangChay',NULL,'["SP921"]','Giam',NULL),
('KM92000015',N'Promo v2 hang moi 4% SP923',1,1,12,31,4,'TIER','DangChay',NULL,'["SP923"]','Giam',NULL);
DECLARE @code char(10),@name nvarchar(100),@sm int,@sday int,@em int,@eday int,@d bigint,@au varchar(10),
 @st varchar(10),@tj nvarchar(max),@ij nvarchar(max),@kd varchar(10),@gj nvarchar(max),@sd date,@ed date;
DECLARE c CURSOR LOCAL FAST_FORWARD FOR
 SELECT Code,Name,StartMonth,StartDay,EndMonth,EndDay,Discount,Audience,Status,Tiers,Items,Kind,Gifts FROM @km ORDER BY Code;
OPEN c; FETCH NEXT FROM c INTO @code,@name,@sm,@sday,@em,@eday,@d,@au,@st,@tj,@ij,@kd,@gj;
WHILE @@FETCH_STATUS=0
BEGIN
 -- StartMonth=0 hoặc EndMonth=0: bắt đầu hôm nay / không có ngày kết thúc.
 SET @sd=IIF(@sm=0,@today,DATEFROMPARTS(2000,@sm,@sday));
 SET @ed=IIF(@em=0,NULL,DATEFROMPARTS(2000,@em,@eday));
 SET @id=NEWID();
 EXEC dbo.sp_PromotionWrite @Actor=@a,@Stamp=@s,@Action='Create',@Code=@code,@RequestId=@id,
  @Name=@name,@StartDate=@sd,@EndDate=@ed,@Discount=@d,@Audience=@au,@Status=@st,
  @TiersJson=@tj,@ItemsJson=@ij,@Kind=@kd,@GiftJson=@gj;
 FETCH NEXT FROM c INTO @code,@name,@sm,@sday,@em,@eday,@d,@au,@st,@tj,@ij,@kd,@gj;
END;
CLOSE c; DEALLOCATE c;

-- Lưu ngày đúng quy ước: BD năm 2000; KT năm 2001 khi vắt năm; KT trống giữ NULL.
IF EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE YEAR(NgayBD)<>2000
 OR (NgayKT IS NOT NULL AND YEAR(NgayKT) NOT IN(2000,2001)))
 THROW 53701,N'Ngày KM chưa chuẩn hóa theo năm tham chiếu.',1;
IF (SELECT CONCAT(NgayBD,'|',NgayKT) FROM dbo.KHUYENMAI WHERE MaKM='KM92000001')<>'2000-11-01|2001-02-28'
 THROW 53701,N'KM vắt năm 01/11-28/02 lưu sai.',1;
IF (SELECT CONCAT(NgayBD,'|',NgayKT) FROM dbo.KHUYENMAI WHERE MaKM='KM92000005')<>'2000-01-01|2000-12-31'
 THROW 53701,N'KM trong năm lưu sai.',1;
IF EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE MaKM='KM92000013' AND (NgayKT IS NOT NULL OR MONTH(NgayBD)<>MONTH(@today) OR DAY(NgayBD)<>DAY(@today)))
 THROW 53701,N'KM bắt đầu hôm nay, không ngày kết thúc lưu sai.',1;
IF (SELECT COUNT(*) FROM dbo.KHUYENMAI WHERE MaKM LIKE 'KM92%')<>15 THROW 53701,N'Thiếu KM test.',1;

-- Baseline để so regression ở 04.
INSERT dbo.PromoV2Baseline(Name,Value)
 SELECT 'SeedPaidCount',CONVERT(decimal(38,4),COUNT(*)) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today
 UNION ALL SELECT 'SeedPaidGross',COALESCE(SUM(CONVERT(decimal(38,4),TongTien)),0) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today
 UNION ALL SELECT 'SeedPaidDiscount',COALESCE(SUM(CONVERT(decimal(38,4),GiamGia)),0) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today
 UNION ALL SELECT 'SeedPaidAmount',COALESCE(SUM(CONVERT(decimal(38,4),ThanhTien)),0) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today
 UNION ALL SELECT 'StockNVL91',CONVERT(decimal(38,4),(SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL91'))
 UNION ALL SELECT 'StockNVL92',CONVERT(decimal(38,4),(SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL92'))
 UNION ALL SELECT 'PointsKH922',CONVERT(decimal(38,4),(SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH922'))
 UNION ALL SELECT 'PointsKH923',CONVERT(decimal(38,4),(SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH923'))
 UNION ALL SELECT 'OpenOrderCount',CONVERT(decimal(38,4),COUNT(*)) FROM dbo.HOADON WHERE TrangThai=N'Chua thanh toan';
IF (SELECT COUNT(*) FROM dbo.PromoV2Baseline)<>9 THROW 53701,N'Baseline thiếu.',1;
