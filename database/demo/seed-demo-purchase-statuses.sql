-- Bo sung phieu nhap mau o cac trang thai Chua kiem/Da kiem/Da huy de thu man Nhap hang.
-- Chay sau seed-demo-operational.sql tren CoffeeManager_Development. Chi THEM du lieu.
-- Ma phieu co dinh PN26000101..PN26000105 nam ngoai dai sequence 2609xxxx de de nhan biet
-- va guard chong chay lap. Khong tao ton/chi/cong no vi cac phieu nay chua chot.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
IF DB_NAME() NOT LIKE 'CoffeeManager%'
    THROW 51960,N'Chi chay tren DB CoffeeManager.',1;
IF NOT EXISTS (SELECT 1 FROM dbo.DevelopmentMarker)
    THROW 51960,N'Chi chay tren DB development co marker.',1;
IF EXISTS (SELECT 1 FROM dbo.PHIEUNHAP WHERE MaPN LIKE 'PN260001%')
    THROW 51960,N'Du lieu phieu nhap theo trang thai da duoc nap; khong chay lap.',1;
IF NOT EXISTS (SELECT 1 FROM dbo.NHACUNGCAP WHERE MaNCC IN ('NCC01','NCC02','NCC03','NCC05'))
    OR NOT EXISTS (SELECT 1 FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01')
    OR NOT EXISTS (SELECT 1 FROM dbo.NHANVIEN WHERE MaNV='NV002')
    THROW 51960,N'Can chay seed-demo-operational.sql truoc.',1;

DECLARE @Now datetime2(0)=SYSDATETIME();
DECLARE @Today date=CONVERT(date,@Now);
DECLARE @Actor char(5)='NV001',@Store char(5)='NV002';

BEGIN TRY
BEGIN TRAN;

-- 1) Cho nhan: mot phieu hen tra 7 ngay, mot phieu giao gap khong hen.
INSERT INTO dbo.PHIEUNHAP (MaPN,NgayNhap,MaNV,MaNCC,TongTien,DaChot,CutoverKind,TraNgayDuKien,HanTraDuKien)
VALUES
('PN26000101',DATEADD(day,-1,@Now),@Actor,'NCC01',0,0,'New',2000000,DATEADD(day,7,@Today)),
('PN26000102',DATEADD(hour,-4,@Now),@Actor,'NCC03',0,0,'New',0,NULL);

INSERT INTO dbo.CHITIET_PN (MaPN,MaNVL,SoLuong,DonGiaNhap,SoLuongNhan,LyDoLech,DaGhiKho)
VALUES
('PN26000101','NVL01',12,195000,NULL,NULL,0),
('PN26000101','NVL02',80,31000,NULL,NULL,0),
('PN26000102','NVL08',10,42000,NULL,NULL,0),
('PN26000102','NVL15',2,220000,NULL,NULL,0);

-- 2) Da kiem: so thuc nhan co le (kg) va chenh lech da ghi ly do; chua ghi kho.
INSERT INTO dbo.PHIEUNHAP (MaPN,NgayNhap,MaNV,MaNCC,TongTien,DaChot,CutoverKind,TraNgayDuKien,HanTraDuKien,NgayKiem,MaNVKiem)
VALUES
('PN26000103',DATEADD(day,-3,@Now),@Actor,'NCC02',0,0,'New',2000000,DATEADD(day,10,@Today),DATEADD(day,-2,@Now),@Store),
('PN26000104',DATEADD(day,-5,@Now),@Actor,'NCC05',0,0,'New',0,NULL,DATEADD(day,-4,@Now),@Store);

INSERT INTO dbo.CHITIET_PN (MaPN,MaNVL,SoLuong,DonGiaNhap,SoLuongNhan,LyDoLech,DaGhiKho)
VALUES
('PN26000103','NVL02',120,31000,118,N'Thieu 2 lit do hao hut van chuyen',0),
('PN26000103','NVL05',2.5,380000,2.25,N'Thieu 0,25 kg do bi vo',0),
('PN26000103','NVL20',10,28000,10,NULL,0),
('PN26000104','NVL12',60,33000,60,NULL,0),
('PN26000104','NVL21',40,18000,39,N'Vo 1 hop khi boc hang',0);

-- 3) Da huy: chi de doi chieu bo loc trang thai, khong dung ton/tien.
INSERT INTO dbo.PHIEUNHAP (MaPN,NgayNhap,MaNV,MaNCC,TongTien,DaChot,CutoverKind,TraNgayDuKien,HanTraDuKien)
VALUES ('PN26000105',DATEADD(day,-6,@Now),@Actor,'NCC02',0,0,'New',0,NULL);
INSERT INTO dbo.CHITIET_PN (MaPN,MaNVL,SoLuong,DonGiaNhap,SoLuongNhan,LyDoLech,DaGhiKho)
VALUES ('PN26000105','NVL09',2,85000,NULL,NULL,0);
UPDATE dbo.PHIEUNHAP SET DaHuy=1,LyDoHuy=N'Nha cung cap bao het hang, hen giao dot sau',NgayHuy=DATEADD(day,-5,@Now)
WHERE MaPN='PN26000105';

COMMIT;
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK;
THROW;
END CATCH;
