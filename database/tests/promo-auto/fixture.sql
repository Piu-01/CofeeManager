-- Fixture bộ promo-auto (migration039): chỉ DB test riêng, dựng qua procedure công khai.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_PromoAuto[_]%' OR OBJECT_ID('dbo.PromoAutoTestMarker') IS NULL
 THROW 53701,N'Chỉ DB test PromoAuto có marker.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=39) THROW 53701,N'Cần schema039.',1;
IF OBJECT_ID('dbo.fn_DiscountPreview') IS NOT NULL THROW 53701,N'fn_DiscountPreview phải đã bị bỏ.',1;
IF OBJECT_ID('dbo.KHUYENMAI_HANG') IS NULL OR OBJECT_ID('dbo.KHUYENMAI_MON') IS NULL
 OR OBJECT_ID('dbo.HoaDonUuDaiApDung') IS NULL OR OBJECT_ID('dbo.sp_PromotionWrite') IS NULL
 THROW 53701,N'Thiếu object 039.',1;
IF EXISTS(SELECT 1 FROM dbo.KHUYENMAI WHERE MaKM LIKE 'KM9%') THROW 53701,N'DB đã có dữ liệu KM9; dùng DB mới.',1;
IF EXISTS(SELECT 1 FROM dbo.KHACHHANG WHERE MaKH IN('KH901','KH902')) THROW 53701,N'DB đã có khách test.',1;
IF OBJECT_ID('dbo.PromoAutoBaseline') IS NULL
 CREATE TABLE dbo.PromoAutoBaseline(Name varchar(20) NOT NULL PRIMARY KEY,Value decimal(38,4) NOT NULL);

UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV IN('NV001','NV004','NV010');
DECLARE @a int,@s binary(8),@id uniqueidentifier,@today date=CONVERT(date,SYSDATETIME());
SELECT @a=MaTK,@s=SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV='NV001';

-- Khách test: KH901 hạng DONG 0%, KH902 hạng VANG 5%; cả hai chưa có hóa đơn đã trả.
INSERT dbo.KHACHHANG(MaKH,TenKH,SDT,DiemTichLuy,MaHang) VALUES
('KH901',N'Promo Auto Fresh',N'0900000901',0,'DONG'),
('KH902',N'Promo Auto Vang',N'0900000902',0,'VANG');

-- Món giá lẻ để kiểm làm tròn theo dòng (105 = 5% + 7% ra 13 khi gộp, 5 + 7 khi tách).
INSERT dbo.SANPHAM(MaSP,TenSP,MaDM,GiaBan,TrangThai) VALUES
('SP901',N'Promo round odd',N'DM004',105,N'Dang ban'),
('SP902',N'Promo unlimited',N'DM004',33333,N'Dang ban');

DECLARE @km TABLE(Code char(10),Name nvarchar(100),StartDate date,EndDate date,Discount bigint,Audience varchar(10),Status varchar(10),Tiers nvarchar(max),Items nvarchar(max));
INSERT @km VALUES
('KM90000001',N'Khach lan dau 20% SP001',DATEADD(day,-30,@today),DATEADD(day,30,@today),20,'FIRST','DangChay',NULL,'["SP001"]'),
('KM90000002',N'Hang VANG 10% SP002',DATEADD(day,-30,@today),DATEADD(day,30,@today),10,'TIER','DangChay','["VANG"]','["SP002"]'),
('KM90000003',N'Moi khach 5% toan bo',DATEADD(day,-30,@today),DATEADD(day,30,@today),5,'ALL','DangChay',NULL,NULL),
('KM90000004',N'Khach co ho so 10% SP004',DATEADD(day,-30,@today),DATEADD(day,30,@today),10,'MEMBER','DangChay',NULL,'["SP004"]'),
('KM90000005',N'Moi khach 7% SP901 lam tron',DATEADD(day,-30,@today),DATEADD(day,30,@today),7,'ALL','DangChay',NULL,'["SP901"]'),
('KM90000006',N'Moi khach 30% SP902 khong gioi han',DATEADD(day,-30,@today),NULL,30,'ALL','DangChay',NULL,'["SP902"]'),
('KM90000007',N'Da tam dung 50%',DATEADD(day,-30,@today),DATEADD(day,30,@today),50,'ALL','TamDung',NULL,NULL),
('KM90000008',N'Con nhap 50%',DATEADD(day,-30,@today),DATEADD(day,30,@today),50,'ALL','Nhap',NULL,NULL),
('KM90000009',N'Het han 50%',DATEADD(day,-30,@today),DATEADD(day,-1,@today),50,'ALL','DangChay',NULL,NULL),
('KM90000010',N'Tuong lai 50%',DATEADD(day,60,@today),DATEADD(day,90,@today),50,'ALL','DangChay',NULL,NULL),
('KM90000011',N'Cap dong 60% SP003 A',DATEADD(day,-30,@today),DATEADD(day,30,@today),60,'ALL','DangChay',NULL,'["SP003"]'),
('KM90000012',N'Cap dong 60% SP003 B',DATEADD(day,-30,@today),DATEADD(day,30,@today),60,'ALL','DangChay',NULL,'["SP003"]'),
-- Legacy: tạm dừng để không tự áp cho đơn mới; đơn cũ giữ MaKM vẫn tính theo nhánh legacy.
('KM90000013',N'Legacy 20% toan bo',DATEADD(day,-30,@today),DATEADD(day,30,@today),20,'ALL','TamDung',NULL,NULL);
DECLARE @code char(10),@name nvarchar(100),@sd date,@ed date,@d bigint,@au varchar(10),@st varchar(10),@tj nvarchar(max),@ij nvarchar(max);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Code,Name,StartDate,EndDate,Discount,Audience,Status,Tiers,Items FROM @km ORDER BY Code;
OPEN c; FETCH NEXT FROM c INTO @code,@name,@sd,@ed,@d,@au,@st,@tj,@ij;
WHILE @@FETCH_STATUS=0
BEGIN
 SET @id=NEWID();
 EXEC dbo.sp_PromotionWrite @Actor=@a,@Stamp=@s,@Action='Create',@Code=@code,@RequestId=@id,
  @Name=@name,@StartDate=@sd,@EndDate=@ed,@Discount=@d,@Audience=@au,@Status=@st,@TiersJson=@tj,@ItemsJson=@ij;
 FETCH NEXT FROM c INTO @code,@name,@sd,@ed,@d,@au,@st,@tj,@ij;
END;
CLOSE c; DEALLOCATE c;

-- Đơn mở legacy: tạo hợp lệ rồi gán MaKM của 039 như dữ liệu 038 còn mở.
DECLARE @ids TABLE(Loai varchar(20),Ma char(10));
INSERT @ids EXEC dbo.sp_CapMa 'HOADON';
DECLARE @hd char(10); SELECT @hd=Ma FROM @ids;
SET @id=NEWID();
EXEC dbo.sp_TaoHoaDon @Actor=@a,@Stamp=@s,@Code=@hd,@RequestId=@id,@Channel=N'Mang di',@Product='SP001',@Quantity=1;
IF (SELECT GiamGia FROM dbo.HOADON WHERE MaHD=@hd)<>1250 THROW 53701,N'Giảm tự áp ban đầu của đơn legacy sai.',1;
UPDATE dbo.HOADON SET MaKM='KM90000013' WHERE MaHD=@hd;
EXEC dbo.sp_SalesRefreshDiscount @hd;
IF (SELECT GiamGia FROM dbo.HOADON WHERE MaHD=@hd)<>6250 THROW 53701,N'Giảm legacy (20% KM + 5% ALL) phải là 6250.',1;
-- Hóa đơn lịch sử = đã trả trước hôm nay; đơn test của bộ này trả trong hôm nay.
INSERT dbo.PromoAutoBaseline(Name,Value)
 SELECT 'SeedPaidCount',CONVERT(decimal(38,4),COUNT(*)) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today
 UNION ALL SELECT 'SeedPaidGross',COALESCE(SUM(CONVERT(decimal(38,4),TongTien)),0) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today
 UNION ALL SELECT 'SeedPaidDiscount',COALESCE(SUM(CONVERT(decimal(38,4),GiamGia)),0) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today
 UNION ALL SELECT 'SeedPaidAmount',COALESCE(SUM(CONVERT(decimal(38,4),ThanhTien)),0) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND CONVERT(date,NgayThanhToan)<@today
 UNION ALL SELECT 'StockNVL01',CONVERT(decimal(38,4),(SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01'))
 UNION ALL SELECT 'StockNVL03',CONVERT(decimal(38,4),(SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL03'));
IF (SELECT COUNT(*) FROM dbo.PromoAutoBaseline)<>6 THROW 53701,N'Baseline thiếu.',1;
