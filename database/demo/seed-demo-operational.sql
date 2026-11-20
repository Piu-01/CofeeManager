-- DU LIEU MAU VAN HANH (demo) cho CoffeeManager_Development.
-- Chay SAU schema 029 + Insert_Data.sql + bootstrap. Chi THEM du lieu, khong xoa.
-- Moc thoi gian theo SYSDATETIME(): 60 ngay gan nhat tinh tu luc chay.
-- Mot transaction, guard chong chay lap bang ma khuyen mai KM26HRO.
-- Noi dung viet khong dau de dong bo danh muc goc va tranh loi encoding sqlcmd.
-- Tat ca bat bien (tong tien, thanh toan, tieu hao, ton, diem, cong no, luong)
-- duoc ghi dung thu tu nghiep vu va kiem tra lai truoc COMMIT.
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
    THROW 51900,N'Chi chay tren DB CoffeeManager.',1;
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE Version=29)
    THROW 51900,N'Can schema 029 tro len.',1;
IF NOT EXISTS (SELECT 1 FROM dbo.DevelopmentMarker)
    THROW 51900,N'Chi chay tren DB development co marker.',1;
IF EXISTS (SELECT 1 FROM dbo.KHUYENMAI WHERE MaKM='KM26HRO')
    THROW 51900,N'Du lieu mau van hanh da duoc nap; khong chay lap.',1;

DECLARE @Now datetime2(0)=SYSDATETIME();
DECLARE @Today date=CONVERT(date,@Now);
DECLARE @Start date=DATEADD(day,-59,@Today);
DECLARE @Actor int=(SELECT MIN(MaTK) FROM dbo.TAIKHOAN);
DECLARE @Stamp binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=@Actor);

BEGIN TRY
BEGIN TRAN;

-- Cap ma moi de khong trung ma lich su va ma nguoi dung da tao.
ALTER SEQUENCE dbo.SeqHoaDon RESTART WITH 26090001;
ALTER SEQUENCE dbo.SeqPhieuNhap RESTART WITH 26090001;
ALTER SEQUENCE dbo.SeqCongNo RESTART WITH 26090001;
ALTER SEQUENCE dbo.SeqPhieuThuChi RESTART WITH 26090001;
ALTER SEQUENCE dbo.SeqPhieuXuat RESTART WITH 26090001;
ALTER SEQUENCE dbo.SeqKiemKe RESTART WITH 26090001;

/* ============================================================
   1. DANH MUC NEN MO RONG (nguyen lieu, mon, cong thuc, khach, khuyen mai)
   ============================================================ */
INSERT INTO dbo.NGUYENVATLIEU (MaNVL,TenNVL,DonViTinh,SoLuongTon) VALUES
(N'NVL13',N'Dao ngam',N'Kg',12),
(N'NVL14',N'Vai thieu',N'Kg',12),
(N'NVL15',N'Hat sen',N'Kg',4),
(N'NVL16',N'Dua tuoi',N'Kg',5),
(N'NVL17',N'Muoi bien',N'Kg',1),
(N'NVL19',N'Cam tuoi',N'Kg',30),
(N'NVL20',N'Sua chua',N'Lit',10),
(N'NVL21',N'Banh flan thanh pham',N'Cai',40);

INSERT INTO dbo.SANPHAM (MaSP,TenSP,MaDM,GiaBan,TrangThai) VALUES
(N'SP011',N'Tra dao cam sa',N'DM007',45000,N'Dang ban'),
(N'SP012',N'Tra vai',N'DM007',42000,N'Dang ban'),
(N'SP013',N'Ca phe muoi',N'DM001',35000,N'Dang ban'),
(N'SP014',N'Matcha latte',N'DM002',50000,N'Dang ban'),
(N'SP015',N'Nuoc cam tuoi',N'DM005',40000,N'Dang ban'),
(N'SP016',N'Sua chua trai cay',N'DM003',38000,N'Dang ban'),
(N'SP017',N'Banh flan',N'DM009',30000,N'Dang ban'),
(N'SP018',N'Tra sen',N'DM007',40000,N'Dang ban');

INSERT INTO dbo.CONGTHUC (MaSP,MaNVL,DinhMuc) VALUES
(N'SP011',N'NVL04',0.006),(N'SP011',N'NVL13',0.080),(N'SP011',N'NVL19',0.100),(N'SP011',N'NVL03',0.012),
(N'SP012',N'NVL04',0.006),(N'SP012',N'NVL14',0.100),(N'SP012',N'NVL03',0.012),
(N'SP013',N'NVL01',0.018),(N'SP013',N'NVL02',0.100),(N'SP013',N'NVL16',0.020),(N'SP013',N'NVL17',0.002),
(N'SP014',N'NVL05',0.012),(N'SP014',N'NVL02',0.150),(N'SP014',N'NVL03',0.015),
(N'SP015',N'NVL19',0.250),(N'SP015',N'NVL03',0.010),
(N'SP016',N'NVL20',0.150),(N'SP016',N'NVL02',0.050),(N'SP016',N'NVL03',0.010),(N'SP016',N'NVL08',0.080),
(N'SP017',N'NVL21',1.000),
(N'SP018',N'NVL04',0.005),(N'SP018',N'NVL15',0.050),(N'SP018',N'NVL03',0.010);

-- Mo so nguyen lieu moi cung moc bootstrap voi cac nguyen lieu cu.
DECLARE @OpenMatAt datetime2(7)=(SELECT MAX(RecordedAt) FROM dbo.MaterialOpening);
INSERT INTO dbo.MaterialOpening (MaNVL,Quantity,UnitSnapshot,RecordedAt,Source,Kind,ActorAccountId)
SELECT MaNVL,SoLuongTon,DonViTinh,@OpenMatAt,N'Demo: mo so nguyen lieu moi','LegacyCutover',@Actor
FROM dbo.NGUYENVATLIEU WHERE MaNVL>='NVL13';

CREATE TABLE #NewCust(Seq int IDENTITY(1,1),Code char(5) NULL,Name nvarchar(50),Phone varchar(15),Points int);
INSERT INTO #NewCust (Name,Phone,Points) VALUES
(N'Pham Quoc Bao','0903111222',120),
(N'Nguyen Thi Ngoc','0903222333',450),
(N'Tran Van Tai','0903333444',1500),
(N'Le Hoang Yen','0903444555',2800),
(N'Vo Minh Tuan','0903555666',5200),
(N'Dang Thu Trang','0903666777',8600),
(N'Bui Anh Tuan','0903777888',12500),
(N'Ho Ngoc Diep','0903888999',26000),
(N'Ngo Van Phuc','0903999000',60),
(N'Dinh Thi Thom','0904111222',180),
(N'Cao Minh Tri','0904222333',6400),
(N'Truong Bao Ngoc','0904333444',3200);

DECLARE @nc int=1;
WHILE @nc<=(SELECT MAX(Seq) FROM #NewCust)
BEGIN
    UPDATE #NewCust
    SET Code=CAST('KH'+RIGHT('000'+CONVERT(varchar(3),NEXT VALUE FOR dbo.SeqKhachHang),3) AS char(5))
    WHERE Seq=@nc;
    SET @nc+=1;
END;

INSERT INTO dbo.KHACHHANG (MaKH,TenKH,SDT,DiemTichLuy,MaHang)
SELECT c.Code,c.Name,c.Phone,c.Points,
    (SELECT TOP(1) MaHang FROM dbo.HANGTHANHVIEN WHERE DiemToiThieu<=c.Points ORDER BY DiemToiThieu DESC)
FROM #NewCust c;

DECLARE @OpenCustAt datetime2(7)=(SELECT MAX(RecordedAt) FROM dbo.CustomerOpening);
INSERT INTO dbo.CustomerOpening (MaKH,Points,RecordedAt,Source,Kind,ActorAccountId)
SELECT Code,Points,@OpenCustAt,N'Demo: diem dau ky','LegacyCutover',@Actor FROM #NewCust;

INSERT INTO dbo.KHUYENMAI (MaKM,TenKM,NgayBD,NgayKT,PhanTramGiam) VALUES
('KM26HRO',N'Happy Hour 14-17',DATEADD(day,-75,@Today),DATEADD(day,50,@Today),10),
('KM26STU',N'Uu dai sinh vien',DATEADD(day,-70,@Today),DATEADD(day,20,@Today),5),
('KM26BF',N'Black Friday 2026',DATEADD(day,15,@Today),DATEADD(day,20,@Today),25),
('KM26XMAS',N'Giang sinh 2026',DATEADD(day,40,@Today),DATEADD(day,52,@Today),15),
('KM26TET',N'Tet Nguyen Dan 2027',DATEADD(day,70,@Today),DATEADD(day,80,@Today),20),
('KM26SUM',N'He ruc ro 2026',DATEADD(day,-150,@Today),DATEADD(day,-90,@Today),12);

-- Sua ten bi loi ma hoa cua danh muc chi he thong.
UPDATE dbo.DANHMUC_THUCHI SET TenDMTC=N'Tra ngay nhap hang' WHERE MaDMTC='TC012';

/* ============================================================
   2. NHAP KHO + CONG NO + TRA NO
   ============================================================ */
CREATE TABLE #Rcp(Id int IDENTITY(1,1) PRIMARY KEY,NCC char(5),Dt datetime2(0),PayRatio int);
INSERT INTO #Rcp (NCC,Dt,PayRatio) VALUES
('NCC02',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-57,@Today))),50),
('NCC06',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-55,@Today))),0),
('NCC04',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-52,@Today))),40),
('NCC07',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-49,@Today))),0),
('NCC09',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-46,@Today))),0),
('NCC06',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-43,@Today))),0),
('NCC03',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-40,@Today))),100),
('NCC05',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-37,@Today))),50),
('NCC07',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-34,@Today))),0),
('NCC08',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-31,@Today))),0),
('NCC01',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-28,@Today))),0),
('NCC04',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-25,@Today))),100),
('NCC06',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-22,@Today))),0),
('NCC09',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-19,@Today))),30),
('NCC07',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-16,@Today))),50),
('NCC08',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-13,@Today))),0),
('NCC01',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-10,@Today))),100),
('NCC04',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-7,@Today))),0),
('NCC06',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-4,@Today))),0),
('NCC02',DATEADD(hour,9,CONVERT(datetime2(0),DATEADD(day,-2,@Today))),0);

CREATE TABLE #RcpLine(RcpId int,MaNVL char(5),Qty decimal(18,4),Price decimal(18,0));
INSERT INTO #RcpLine (RcpId,MaNVL,Qty,Price) VALUES
(1,'NVL02',150,31000),(1,'NVL03',15,24000),(1,'NVL12',100,33000),(1,'NVL11',60,62000),
(1,'NVL19',60,35000),(1,'NVL13',25,60000),(1,'NVL14',25,55000),(1,'NVL08',20,42000),
(2,'NVL07',25,95000),(2,'NVL06',20,70000),(2,'NVL05',3,380000),(2,'NVL16',8,45000),
(3,'NVL01',15,195000),(3,'NVL09',3,85000),
(4,'NVL02',120,31000),(4,'NVL03',15,24000),(4,'NVL20',20,28000),
(5,'NVL15',8,220000),(5,'NVL21',80,18000),(5,'NVL11',60,62000),
(6,'NVL07',25,95000),(6,'NVL16',8,45000),(6,'NVL20',20,28000),(6,'NVL06',20,70000),(6,'NVL17',1,12000),
(7,'NVL04',4,95000),(7,'NVL05',3,380000),
(8,'NVL12',100,33000),(8,'NVL21',80,18000),
(9,'NVL08',15,42000),(9,'NVL02',120,31000),(9,'NVL03',15,24000),
(10,'NVL19',45,35000),(10,'NVL13',15,60000),(10,'NVL14',15,55000),(10,'NVL15',8,220000),
(11,'NVL01',15,195000),(11,'NVL02',120,31000),(11,'NVL03',15,24000),
(12,'NVL09',3,85000),
(13,'NVL04',4,95000),(13,'NVL20',20,28000),
(14,'NVL12',90,33000),(14,'NVL11',60,62000),(14,'NVL21',70,18000),
(15,'NVL02',120,31000),(15,'NVL08',15,42000),
(16,'NVL19',45,35000),(16,'NVL13',15,60000),(16,'NVL14',15,55000),
(17,'NVL01',15,195000),(17,'NVL04',4,95000),
(18,'NVL02',110,31000),
(19,'NVL20',20,28000),
(20,'NVL03',15,24000);

CREATE TABLE #Debt(RcpId int,MaCN char(10),MaPN char(10),TienNo decimal(18,0),SourceDt datetime2(0));
DECLARE @pi int=1,@pncc char(5),@pdt datetime2(0),@pratio int,@ptotal decimal(18,0),
    @pimmediate decimal(18,0),@pn char(10),@ptc char(10),@pcn char(10),@pnv char(5)='NV001';
WHILE @pi<=(SELECT MAX(Id) FROM #Rcp)
BEGIN
    SELECT @pncc=NCC,@pdt=Dt,@pratio=PayRatio FROM #Rcp WHERE Id=@pi;
    SET @ptotal=(SELECT SUM(ROUND(Qty*Price,0)) FROM #RcpLine WHERE RcpId=@pi);
    SET @pn=CAST('PN'+RIGHT('00000000'+CONVERT(varchar(20),NEXT VALUE FOR dbo.SeqPhieuNhap),8) AS char(10));
    INSERT INTO dbo.PHIEUNHAP (MaPN,NgayNhap,MaNV,MaNCC,TongTien,DaChot,CutoverKind,NgayChot)
        VALUES(@pn,@pdt,@pnv,@pncc,0,0,'New',@pdt);
    INSERT INTO dbo.CHITIET_PN (MaPN,MaNVL,SoLuong,DonGiaNhap)
        SELECT @pn,MaNVL,Qty,Price FROM #RcpLine WHERE RcpId=@pi ORDER BY MaNVL;
    SET @pimmediate=CONVERT(decimal(18,0),ROUND(CONVERT(decimal(28,4),@ptotal)*@pratio/100.0,0));
    IF @pimmediate>0
    BEGIN
        SET @ptc=CAST('TC'+RIGHT('00000000'+CONVERT(varchar(20),NEXT VALUE FOR dbo.SeqPhieuThuChi),8) AS char(10));
        INSERT INTO dbo.PHIEUTHUCHI (MaTC,NgayLap,MaDMTC,MaNV,SoTien,GhiChu,MaPN,OriginKind)
            VALUES(@ptc,@pdt,'TC012',@pnv,@pimmediate,N'Thanh toan ngay khi nhap hang',@pn,'PurchaseImmediate');
    END;
    IF @pimmediate<@ptotal
    BEGIN
        SET @pcn=CAST('CN'+RIGHT('00000000'+CONVERT(varchar(20),NEXT VALUE FOR dbo.SeqCongNo),8) AS char(10));
        INSERT INTO dbo.CONGNO_NCC (MaCN,MaPN,TienNo,HanTra)
            VALUES(@pcn,@pn,@ptotal-@pimmediate,DATEADD(day,30,CONVERT(date,@pdt)));
        INSERT INTO #Debt VALUES(@pi,@pcn,@pn,@ptotal-@pimmediate,@pdt);
    END;
    UPDATE dbo.PHIEUNHAP SET DaChot=1 WHERE MaPN=@pn;
    INSERT INTO dbo.G03OperationReceipt (RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,CompletedAt)
        VALUES(NEWID(),@Actor,'PurchaseCreate',@pn,HASHBYTES('SHA2_256',CONVERT(varbinary(max),@pn)),@pdt);
    SET @pi+=1;
END;

-- Tra mot phan 5 cong no moi bang phieu chi he thong.
CREATE TABLE #PayDebt(RcpId int,PayPct int,PayDelay int);
INSERT INTO #PayDebt VALUES (3,100,10),(5,50,14),(8,80,9),(10,40,20),(16,60,12);
DECLARE @dRcp int,@dPct int,@dDelay int,@dpay decimal(18,0),@ddate datetime2(0),
    @dtc char(10),@dpt char(5),@di int=1;
WHILE @di<=(SELECT COUNT(*) FROM #PayDebt)
BEGIN
    SELECT @dRcp=RcpId,@dPct=PayPct,@dDelay=PayDelay
    FROM (SELECT RcpId,PayPct,PayDelay,ROW_NUMBER() OVER (ORDER BY RcpId) rn FROM #PayDebt) x
    WHERE rn=@di;
    SELECT @dpay=CONVERT(decimal(18,0),ROUND(TienNo*@dPct/100.0,0)),@ddate=DATEADD(day,@dDelay,SourceDt)
    FROM #Debt WHERE RcpId=@dRcp;
    IF @ddate>DATEADD(hour,-2,@Now) SET @ddate=DATEADD(hour,-2,@Now);
    SET @dpt=CASE WHEN @di%2=1 THEN 'PT002' ELSE 'PT001' END;
    SET @dtc=CAST('TC'+RIGHT('00000000'+CONVERT(varchar(20),NEXT VALUE FOR dbo.SeqPhieuThuChi),8) AS char(10));
    INSERT INTO dbo.PHIEUTHUCHI (MaTC,NgayLap,MaDMTC,MaNV,SoTien,GhiChu,OriginKind)
        VALUES(@dtc,@ddate,'TC011',@pnv,@dpay,N'Tra no nha cung cap','SupplierDebt');
    SELECT @pcn=MaCN FROM #Debt WHERE RcpId=@dRcp;
    INSERT INTO dbo.THANHTOAN_CONGNO (MaCN,MaTC,MaPT) VALUES(@pcn,@dtc,@dpt);
    SET @di+=1;
END;

/* ============================================================
   3. BAN HANG 60 NGAY GAN NHAT
   ============================================================ */
CREATE TABLE #Prod(MaSP char(5) PRIMARY KEY,GiaBan decimal(18,0),W int);
INSERT INTO #Prod VALUES
('SP001',25000,12),('SP002',30000,22),('SP003',32000,8),('SP004',55000,4),('SP005',45000,9),
('SP006',40000,4),('SP007',55000,3),('SP008',50000,3),('SP009',65000,2),('SP010',35000,3),
('SP011',45000,7),('SP012',42000,5),('SP013',35000,6),('SP014',50000,4),('SP015',40000,4),
('SP016',38000,3),('SP017',30000,2),('SP018',40000,3);
CREATE TABLE #ProdCum(MaSP char(5),GiaBan decimal(18,0),Cum int);
INSERT INTO #ProdCum (MaSP,GiaBan,Cum)
SELECT MaSP,GiaBan,SUM(W) OVER (ORDER BY MaSP ROWS UNBOUNDED PRECEDING) FROM #Prod;
DECLARE @wt int=(SELECT SUM(W) FROM #Prod);

CREATE TABLE #Ship(Name nvarchar(100),Phone varchar(15),Addr nvarchar(250));
INSERT INTO #Ship VALUES
(N'Nguyen Van Hung','0912000111',N'12 Nguyen Trai, Quan 1, TP.HCM'),
(N'Tran Thi Kim Ngan','0912000222',N'45 Le Loi, Quan 3, TP.HCM'),
(N'Le Quoc Cuong','0912000333',N'88 Cach Mang Thang 8, Quan 10, TP.HCM'),
(N'Pham Thi Lan','0912000444',N'23 Phan Xich Long, Phu Nhuan, TP.HCM'),
(N'Vo Thanh Nam','0912000555',N'156 Vo Van Tan, Quan 3, TP.HCM'),
(N'Dang Minh Duc','0912000666',N'9 Nguyen Huu Canh, Binh Thanh, TP.HCM'),
(N'Bui Ngoc Anh','0912000777',N'77 Tran Hung Dao, Quan 5, TP.HCM'),
(N'Hoang Van Son','0912000888',N'301 Dien Bien Phu, Quan 3, TP.HCM'),
(N'Ngo Thi Hoa','0912000999',N'18 Truong Sa, Binh Thanh, TP.HCM'),
(N'Dinh Tuan Kiet','0912001000',N'64 Hoang Dieu, Quan 4, TP.HCM'),
(N'Luong Bao Tram','0912001111',N'5 Pasteur, Quan 1, TP.HCM'),
(N'Cao Van Loc','0912001222',N'210 Nguyen Dinh Chieu, Quan 3, TP.HCM');

CREATE TABLE #FreeBan(MaBan char(5) PRIMARY KEY);
INSERT INTO #FreeBan
SELECT b.MaBan FROM dbo.BAN b
WHERE NOT EXISTS (SELECT 1 FROM dbo.HOADON h WHERE h.MaBan=b.MaBan AND h.TrangThai=N'Chua thanh toan');

DECLARE @off int=59,@d date,@dow int,@n int,@i int,@minute int,@maxMinute int,@t datetime2(0);
DECLARE @hd char(10),@channel nvarchar(20),@ban char(5),@kh char(5),@km char(10),@maDt char(5),
    @ship decimal(18,0),@rec nvarchar(100),@phone varchar(15),@addr nvarchar(250),@nv char(5);
DECLARE @tong decimal(18,0),@disc decimal(18,0),@pct int,@src varchar(20),@tier char(5),@promo char(10),
    @partnerPct decimal(5,2),@fee decimal(18,0),@payable decimal(18,0),@p1 decimal(18,0),
    @pm1 char(5),@pm2 char(5),@pts int,@rev binary(8),@gs nvarchar(20);
DECLARE @lines int,@sp char(5),@price decimal(18,0),@qty int,@note nvarchar(100),@r int,@try int;

WHILE @off>=0
BEGIN
    SET @d=DATEADD(day,-@off,@Today);
    SET @dow=DATEDIFF(day,0,@d)%7; -- 0=Thu 2, 5=Thu 7, 6=Chu nhat
    SET @maxMinute=CASE WHEN @off=0 THEN DATEDIFF(minute,CONVERT(datetime2(0),@d),@Now)-15 ELSE 840 END;
    IF @maxMinute<=400
    BEGIN
        SET @off-=1;
        CONTINUE;
    END;
    SET @n=CASE @dow WHEN 5 THEN 66 WHEN 6 THEN 56 WHEN 4 THEN 57
                     WHEN 3 THEN 49 WHEN 2 THEN 47 WHEN 1 THEN 45 ELSE 47 END
           +ABS(CHECKSUM(NEWID())%9);
    IF @off=0 SET @n=CONVERT(int,@n*1.0*(@maxMinute-390)/840.0)+3;
    SET @i=0;
    WHILE @i<@n
    BEGIN
        SET @i+=1;
        SET @minute=390+ABS(CHECKSUM(NEWID())%(@maxMinute-390));
        SET @t=DATEADD(minute,@minute,CONVERT(datetime2(0),@d));
        IF @t>=@Now CONTINUE;

        SET @ban=NULL; SET @kh=NULL; SET @km=NULL; SET @maDt=NULL; SET @ship=0;
        SET @rec=NULL; SET @phone=NULL; SET @addr=NULL; SET @partnerPct=0;
        SET @r=ABS(CHECKSUM(NEWID())%100);
        IF @r<50 SET @channel=N'Tai quan';
        ELSE IF @r<80 SET @channel=N'Mang di';
        ELSE SET @channel=N'Giao hang';

        IF @channel=N'Tai quan'
            SELECT TOP(1) @ban=MaBan FROM #FreeBan ORDER BY CHECKSUM(NEWID());

        IF ABS(CHECKSUM(NEWID())%100) < CASE WHEN @channel=N'Giao hang' THEN 60 ELSE 35 END
            SELECT TOP(1) @kh=MaKH FROM dbo.KHACHHANG ORDER BY CHECKSUM(NEWID());

        IF ABS(CHECKSUM(NEWID())%100)<12
            SELECT TOP(1) @km=MaKM FROM dbo.KHUYENMAI
            WHERE @d BETWEEN NgayBD AND NgayKT AND MaKM LIKE 'KM26%' ORDER BY CHECKSUM(NEWID());

        IF @channel=N'Giao hang'
        BEGIN
            SET @ship=CONVERT(decimal(18,0),15000+5000*ABS(CHECKSUM(NEWID())%3));
            SELECT TOP(1) @rec=Name,@phone=Phone,@addr=Addr FROM #Ship ORDER BY CHECKSUM(NEWID());
            IF ABS(CHECKSUM(NEWID())%100)<55
                SET @maDt=CASE WHEN ABS(CHECKSUM(NEWID())%2)=0 THEN 'DT001' ELSE 'DT002' END;
            SELECT @partnerPct=COALESCE(ChietKhau,0) FROM dbo.DOITACGIAOHANG WHERE MaDT=@maDt;
            SET @partnerPct=COALESCE(@partnerPct,0);
        END;

        SET @nv=CASE ABS(CHECKSUM(NEWID())%4) WHEN 0 THEN 'NV001' WHEN 1 THEN 'NV002' WHEN 2 THEN 'NV004' ELSE 'NV006' END;
        SET @hd=CAST('HD'+RIGHT('00000000'+CONVERT(varchar(20),NEXT VALUE FOR dbo.SeqHoaDon),8) AS char(10));
        INSERT INTO dbo.HOADON (MaHD,NgayLap,MaNV,MaKH,MaBan,MaDT,MaKM,LoaiDon,TenNguoiNhan,SDTNguoiNhan,DiaChiGiao,TrangThaiGiao,PhiShip)
        VALUES(@hd,@t,@nv,@kh,@ban,@maDt,@km,@channel,@rec,@phone,@addr,
            CASE WHEN @channel=N'Giao hang' THEN N'Cho giao' END,@ship);

        SET @lines=1+ABS(CHECKSUM(NEWID())%3);
        IF @lines=3 AND ABS(CHECKSUM(NEWID())%100)<15 SET @lines=4;
        WHILE @lines>0
        BEGIN
            SET @try=0;
            WHILE @try<5
            BEGIN
                SET @r=ABS(CHECKSUM(NEWID())%@wt);
                SELECT TOP(1) @sp=MaSP,@price=GiaBan FROM #ProdCum WHERE Cum>@r ORDER BY Cum;
                IF NOT EXISTS (SELECT 1 FROM dbo.CHITIET_HD WHERE MaHD=@hd AND MaSP=@sp) BREAK;
                SET @try+=1;
            END;
            SET @qty=CASE WHEN ABS(CHECKSUM(NEWID())%100)<82 THEN 1 ELSE 2+ABS(CHECKSUM(NEWID())%2) END;
            SET @note=CASE WHEN ABS(CHECKSUM(NEWID())%100)<12
                THEN (SELECT TOP(1) v FROM (VALUES(N'It duong'),(N'Khong da'),(N'It da'),(N'Nhieu da'),(N'Khong duong')) t(v) ORDER BY CHECKSUM(NEWID()))
                ELSE NULL END;
            INSERT INTO dbo.CHITIET_HD (MaHD,MaSP,SoLuong,GiaBan,GhiChu) VALUES(@hd,@sp,@qty,@price,@note);
            SET @lines-=1;
        END;

        SELECT @tong=SUM(CONVERT(decimal(18,0),SoLuong)*GiaBan) FROM dbo.CHITIET_HD WHERE MaHD=@hd;
        SELECT @disc=Discount,@pct=PercentDiscount,@src=Source,@tier=Tier,@promo=Promotion
        FROM dbo.fn_DiscountPreview(@tong,@ship,CONVERT(date,@t),@kh,@km);
        SET @disc=COALESCE(@disc,0); SET @pct=COALESCE(@pct,0); SET @src=COALESCE(@src,'None');
        IF @disc>0 UPDATE dbo.HOADON SET GiamGia=@disc WHERE MaHD=@hd;

        INSERT INTO dbo.HoaDonUuDaiSnapshot (MaHD,Kind,Source,TierCode,PromotionCode,PercentDiscount,Discount,RecordedAt,PolicyRevision,CustomerRevision,PromotionRevision)
        SELECT @hd,'Checkout',@src,@tier,@promo,@pct,@disc,@t,
            (SELECT Revision FROM dbo.LoyaltyPolicyState),
            (SELECT Revision FROM dbo.KHACHHANG WHERE MaKH=@kh),
            (SELECT Revision FROM dbo.KHUYENMAI WHERE MaKM=@promo);

        INSERT INTO dbo.TIEUHAO_HOADON (MaHD,MaNVL,SoLuong)
        SELECT @hd,c.MaNVL,CONVERT(decimal(18,4),SUM(CONVERT(decimal(18,4),l.SoLuong)*c.DinhMuc))
        FROM dbo.CHITIET_HD l JOIN dbo.CONGTHUC c ON c.MaSP=l.MaSP
        WHERE l.MaHD=@hd GROUP BY c.MaNVL;

        SET @payable=@tong+@ship-@disc;
        SET @r=ABS(CHECKSUM(NEWID())%100);
        SET @pm1=CASE WHEN @r<40 THEN 'PT001' WHEN @r<65 THEN 'PT002' WHEN @r<77 THEN 'PT004'
                      WHEN @r<84 THEN 'PT005' WHEN @r<90 THEN 'PT006' WHEN @r<95 THEN 'PT003' ELSE 'PT009' END;
        IF ABS(CHECKSUM(NEWID())%100)<80
            INSERT INTO dbo.CHITIET_THANHTOAN (MaHD,MaPT,NgayTT,SoTien) VALUES(@hd,@pm1,@t,@payable);
        ELSE
        BEGIN
            SET @p1=CONVERT(decimal(18,0),ROUND(CONVERT(decimal(28,4),@payable)*60/100.0/1000,0)*1000);
            IF @p1<=0 OR @p1>=@payable SET @p1=CONVERT(decimal(18,0),ROUND(@payable/2.0,0));
            SET @pm2=CASE WHEN @pm1='PT001' THEN 'PT002' ELSE 'PT001' END;
            INSERT INTO dbo.CHITIET_THANHTOAN (MaHD,MaPT,NgayTT,SoTien) VALUES
                (@hd,@pm1,@t,@p1),(@hd,@pm2,@t,@payable-@p1);
        END;

        SET @fee=CONVERT(decimal(18,0),ROUND(CONVERT(decimal(28,4),@tong-@disc)*@partnerPct/100,0));
        SET @gs=NULL;
        IF @channel=N'Giao hang'
            SET @gs=CASE WHEN @t<DATEADD(day,-3,@Now) THEN N'Da giao'
                         WHEN ABS(CHECKSUM(NEWID())%100)<70 THEN N'Da giao'
                         WHEN ABS(CHECKSUM(NEWID())%100)<60 THEN N'Dang giao' ELSE N'Cho giao' END;
        UPDATE dbo.HOADON
        SET TrangThai=N'Da thanh toan',NgayThanhToan=@t,PhanTramPhiDoiTac=@partnerPct,PhiDoiTac=@fee,TrangThaiGiao=@gs
        WHERE MaHD=@hd;

        SET @pts=CONVERT(int,FLOOR((@tong-@disc)/1000.0));
        IF @kh IS NOT NULL AND @pts>0
            INSERT INTO dbo.LICHSU_DIEM (MaKH,MaHD,NgayGD,Loai,SoDiem,LyDo)
            VALUES(@kh,@hd,@t,N'Cong',@pts,N'Tich diem mua hang');

        SELECT @rev=Revision FROM dbo.HOADON WHERE MaHD=@hd;
        INSERT INTO dbo.G03OperationReceipt (RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,CompletedAt,ResultRevision)
        VALUES(NEWID(),@Actor,'SalesCheckout',@hd,HASHBYTES('SHA2_256',CONVERT(varbinary(max),@hd)),@t,@rev);
    END;
    SET @off-=1;
END;

-- Don dang mo hom nay: 2 tai quan + 1 mang di + 1 giao hang (chua thu tien).
DECLARE @ob1 char(5)=(SELECT MIN(MaBan) FROM #FreeBan);
DECLARE @ob2 char(5)=(SELECT MIN(MaBan) FROM #FreeBan WHERE MaBan>@ob1);

SET @hd=CAST('HD'+RIGHT('00000000'+CONVERT(varchar(20),NEXT VALUE FOR dbo.SeqHoaDon),8) AS char(10));
INSERT INTO dbo.HOADON (MaHD,NgayLap,MaNV,MaBan,LoaiDon) VALUES(@hd,DATEADD(minute,-28,@Now),'NV004',@ob1,N'Tai quan');
INSERT INTO dbo.CHITIET_HD (MaHD,MaSP,SoLuong,GiaBan,GhiChu) VALUES(@hd,'SP002',2,30000,N'It da'),(@hd,'SP009',1,65000,NULL);

SET @hd=CAST('HD'+RIGHT('00000000'+CONVERT(varchar(20),NEXT VALUE FOR dbo.SeqHoaDon),8) AS char(10));
INSERT INTO dbo.HOADON (MaHD,NgayLap,MaNV,MaBan,LoaiDon) VALUES(@hd,DATEADD(minute,-16,@Now),'NV006',@ob2,N'Tai quan');
INSERT INTO dbo.CHITIET_HD (MaHD,MaSP,SoLuong,GiaBan,GhiChu) VALUES(@hd,'SP004',1,55000,NULL),(@hd,'SP014',1,50000,NULL);

SET @hd=CAST('HD'+RIGHT('00000000'+CONVERT(varchar(20),NEXT VALUE FOR dbo.SeqHoaDon),8) AS char(10));
INSERT INTO dbo.HOADON (MaHD,NgayLap,MaNV,LoaiDon) VALUES(@hd,DATEADD(minute,-9,@Now),'NV004',N'Mang di');
INSERT INTO dbo.CHITIET_HD (MaHD,MaSP,SoLuong,GiaBan,GhiChu) VALUES(@hd,'SP005',2,45000,NULL),(@hd,'SP010',1,35000,NULL);

SET @hd=CAST('HD'+RIGHT('00000000'+CONVERT(varchar(20),NEXT VALUE FOR dbo.SeqHoaDon),8) AS char(10));
INSERT INTO dbo.HOADON (MaHD,NgayLap,MaNV,MaDT,LoaiDon,TenNguoiNhan,SDTNguoiNhan,DiaChiGiao,TrangThaiGiao,PhiShip)
VALUES(@hd,DATEADD(minute,-5,@Now),'NV006','DT002',N'Giao hang',N'Tran Thi Mai','0909888777',N'25 Nguyen Hue, Quan 1, TP.HCM',N'Cho giao',20000);
INSERT INTO dbo.CHITIET_HD (MaHD,MaSP,SoLuong,GiaBan,GhiChu) VALUES(@hd,'SP010',2,35000,NULL),(@hd,'SP001',1,25000,NULL);

/* ============================================================
   4. CHAM CONG (tu dau thang truoc truoc den hom qua)
   ============================================================ */
DECLARE @AttStart date=DATEADD(month,-2,DATEFROMPARTS(YEAR(@Today),MONTH(@Today),1));
CREATE TABLE #EmpShift(MaNV char(5) PRIMARY KEY,MaCa char(5),StartMin int,EndMin int,OffDow int,PartTime bit);
INSERT INTO #EmpShift VALUES
('NV001','CA004',480,1020,0,0),
('NV002','CA001',360,840,2,0),
('NV003','CA007',360,600,6,1),
('NV004','CA002',840,1320,1,0),
('NV005','CA010',1080,1320,0,1),
('NV006','CA001',360,840,3,0),
('NV007','CA009',840,1080,1,1),
('NV008','CA002',840,1320,4,0),
('NV009','CA008',600,840,2,1),
('NV010','CA004',480,1020,5,0);

DECLARE @aMaNV char(5),@aCa char(5),@aStart int,@aEnd int,@aOff int,@aPart bit,@aDate date;
DECLARE @aRnd int,@aIn datetime2(0),@aOut datetime2(0),@aStatus nvarchar(30);
DECLARE attCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT MaNV,MaCa,StartMin,EndMin,OffDow,PartTime FROM #EmpShift ORDER BY MaNV;
OPEN attCursor;
FETCH NEXT FROM attCursor INTO @aMaNV,@aCa,@aStart,@aEnd,@aOff,@aPart;
WHILE @@FETCH_STATUS=0
BEGIN
    SET @aDate=@AttStart;
    WHILE @aDate<@Today
    BEGIN
        IF DATEDIFF(day,0,@aDate)%7<>@aOff
        BEGIN
            SET @aRnd=ABS(CHECKSUM(NEWID())%100);
            SET @aIn=DATEADD(minute,@aStart+(ABS(CHECKSUM(NEWID())%21)-8),CONVERT(datetime2(0),@aDate));
            SET @aOut=DATEADD(minute,@aEnd+ABS(CHECKSUM(NEWID())%26),CONVERT(datetime2(0),@aDate));
            IF @aRnd<2 SET @aStatus=N'Vang';
            ELSE IF @aRnd<4 SET @aStatus=N'Nghi phep';
            ELSE SET @aStatus=N'Di lam';
            INSERT INTO dbo.CHAMCONG (MaNV,NgayLam,MaCa,GioVao,GioRa,TrangThai)
            VALUES(@aMaNV,@aDate,@aCa,
                CASE WHEN @aStatus=N'Di lam' THEN @aIn END,
                CASE WHEN @aStatus=N'Di lam' THEN @aOut END,
                @aStatus);
        END;
        SET @aDate=DATEADD(day,1,@aDate);
    END;
    FETCH NEXT FROM attCursor INTO @aMaNV,@aCa,@aStart,@aEnd,@aOff,@aPart;
END;
CLOSE attCursor;
DEALLOCATE attCursor;

/* ============================================================
   5. BANG LUONG 2 THANG DA QUA + CHI LUONG
   ============================================================ */
DECLARE @Per1 date=DATEADD(month,-2,DATEFROMPARTS(YEAR(@Today),MONTH(@Today),1));
DECLARE @Per2 date=DATEADD(month,-1,DATEFROMPARTS(YEAR(@Today),MONTH(@Today),1));
CREATE TABLE #PayMonths(Thang int,Nam int,NgayChot datetime2(0),NgayLap datetime2(0));
INSERT INTO #PayMonths VALUES
(MONTH(@Per1),YEAR(@Per1),
    CASE WHEN DATEADD(day,1,CONVERT(datetime2(0),EOMONTH(@Per1)))<@Now THEN DATEADD(day,1,CONVERT(datetime2(0),EOMONTH(@Per1))) ELSE DATEADD(hour,-30,@Now) END,
    CASE WHEN DATEADD(day,2,CONVERT(datetime2(0),EOMONTH(@Per1)))<@Now THEN DATEADD(day,2,CONVERT(datetime2(0),EOMONTH(@Per1))) ELSE DATEADD(hour,-2,@Now) END),
(MONTH(@Per2),YEAR(@Per2),
    CASE WHEN DATEADD(day,1,CONVERT(datetime2(0),EOMONTH(@Per2)))<@Now THEN DATEADD(day,1,CONVERT(datetime2(0),EOMONTH(@Per2))) ELSE DATEADD(hour,-30,@Now) END,
    CASE WHEN DATEADD(day,2,CONVERT(datetime2(0),EOMONTH(@Per2)))<@Now THEN DATEADD(day,2,CONVERT(datetime2(0),EOMONTH(@Per2))) ELSE DATEADD(hour,-2,@Now) END);

DECLARE @pe char(5),@pThang int,@pNam int,@pChot datetime2(0),@pLap datetime2(0),
    @pBonus decimal(18,0),@snap nvarchar(max),@tok binary(32),@amt decimal(18,0),
    @bl int,@ptc2 char(10),@pmEnd date,@pmStart date;
DECLARE payCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT n.MaNV,m.Thang,m.Nam,m.NgayChot,m.NgayLap,
           CASE WHEN n.LoaiHopDong=N'Full-time' THEN 300000 ELSE 100000 END
    FROM dbo.NHANVIEN n CROSS JOIN #PayMonths m
    WHERE n.DangLamViec=1
    ORDER BY m.Nam,m.Thang,n.MaNV;
OPEN payCursor;
FETCH NEXT FROM payCursor INTO @pe,@pThang,@pNam,@pChot,@pLap,@pBonus;
WHILE @@FETCH_STATUS=0
BEGIN
    EXEC dbo.sp_PayrollCalculate
        @Employee=@pe,@Month=@pThang,@Year=@pNam,
        @Regular=NULL,@Overtime=0,@OvertimePay=0,@Allowance=NULL,
        @Bonus=@pBonus,@Deduction=0,
        @Snapshot=@snap OUTPUT,@Token=@tok OUTPUT,@Amount=@amt OUTPUT;
    IF EXISTS (SELECT 1 FROM OPENJSON(@snap,'$.Issues'))
        THROW 51924,N'Bang luong mau co canh bao cham cong khong hop le.',1;

    INSERT INTO dbo.BANGLUONG (MaNV,Thang,Nam,NgayChot,LuongCoBan,SoGioLam,DonGiaGio,TienTangCa,PhuCap,Thuong,KhauTru,
        SnapshotKind,ContractSnapshot,EmployeeNameSnapshot,PositionSnapshot,PositionNameSnapshot,
        MonthlyRateSnapshot,HourlyRateSnapshot,TotalHours,RegularHours,OvertimeHours,TotalSeconds,ActorAccountId,SourceSnapshot)
    SELECT @pe,@pThang,@pNam,@pChot,BasePay,PaidHours,HourlyPayRate,OvertimePay,Allowance,Bonus,Deduction,
        'New',Contract,EmployeeName,Position,PositionName,
        MonthlyRate,HourlyRate,TotalHours,RegularHours,OvertimeHours,TotalSeconds,@Actor,@snap
    FROM OPENJSON(@snap) WITH(
        BasePay decimal(18,0),PaidHours decimal(10,2),HourlyPayRate decimal(18,0),
        OvertimePay decimal(18,0),Allowance decimal(18,0),Bonus decimal(18,0),Deduction decimal(18,0),
        Contract nvarchar(20),EmployeeName nvarchar(50),Position char(5),PositionName nvarchar(50),
        MonthlyRate decimal(18,0),HourlyRate decimal(18,0),TotalHours decimal(10,2),
        RegularHours decimal(10,2),OvertimeHours decimal(10,2),TotalSeconds bigint);
    SET @bl=CONVERT(int,SCOPE_IDENTITY());

    SET @pmStart=DATEFROMPARTS(@pNam,@pThang,1);
    SET @pmEnd=EOMONTH(@pmStart);
    INSERT INTO dbo.PayrollAttendanceSnapshot (MaBL,MaNV,NgayLam,MaCa,GioVao,GioRa,TrangThai,SourceRevision)
    SELECT @bl,MaNV,NgayLam,MaCa,GioVao,GioRa,TrangThai,Revision
    FROM dbo.CHAMCONG WHERE MaNV=@pe AND NgayLam BETWEEN @pmStart AND @pmEnd;

    INSERT INTO dbo.G03OperationReceipt (RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,CompletedAt)
    VALUES(NEWID(),@Actor,'PayrollCommit',CONVERT(varchar(10),@bl),HASHBYTES('SHA2_256',CONVERT(varbinary(max),@snap)),@pChot);

    -- Hai nhan vien part-time thang gan nhat chua chi luong (dang cho doi soat).
    IF NOT (@pThang=MONTH(@Per2) AND @pe IN ('NV005','NV009'))
    BEGIN
        SET @ptc2=CAST('TC'+RIGHT('00000000'+CONVERT(varchar(20),NEXT VALUE FOR dbo.SeqPhieuThuChi),8) AS char(10));
        INSERT INTO dbo.PHIEUTHUCHI (MaTC,NgayLap,MaDMTC,MaNV,SoTien,GhiChu,MaBL,OriginKind)
        VALUES(@ptc2,@pLap,'TC005',@pnv,@amt,CONCAT(N'Chi luong thang ',@pThang,N'/',@pNam),@bl,'Payroll');
    END;
    FETCH NEXT FROM payCursor INTO @pe,@pThang,@pNam,@pChot,@pLap,@pBonus;
END;
CLOSE payCursor;
DEALLOCATE payCursor;

/* ============================================================
   6. TAI SAN + CHI PHI VAN HANH
   ============================================================ */
DECLARE @Ts1 char(5)=CAST('TS'+RIGHT('000'+CONVERT(varchar(10),NEXT VALUE FOR dbo.SeqTaiSan),3) AS char(5));
DECLARE @Ts2 char(5)=CAST('TS'+RIGHT('000'+CONVERT(varchar(10),NEXT VALUE FOR dbo.SeqTaiSan),3) AS char(5));
DECLARE @Ts3 char(5)=CAST('TS'+RIGHT('000'+CONVERT(varchar(10),NEXT VALUE FOR dbo.SeqTaiSan),3) AS char(5));
INSERT INTO dbo.TAISAN_CODINH (MaTS,TenTS,NgayMua,GiaTri,SoThangKH) VALUES
(@Ts1,N'May pha ca phe be mat',DATEADD(day,-72,@Today),18000000,60),
(@Ts2,N'Tu lanh trung bay banh',DATEADD(day,-72,@Today),12500000,60),
(@Ts3,N'Loa bluetooth phong khach',DATEADD(day,-40,@Today),3200000,24);

DECLARE @MonthStart date=DATEFROMPARTS(YEAR(@Today),MONTH(@Today),1);
DECLARE @PrevStart date=DATEADD(month,-1,@MonthStart);
DECLARE @RentCur datetime2(0)=CASE WHEN DATEADD(hour,9,CONVERT(datetime2(0),@MonthStart))<@Now
    THEN DATEADD(hour,9,CONVERT(datetime2(0),@MonthStart)) ELSE DATEADD(hour,-2,@Now) END;
DECLARE @RentPrev datetime2(0)=DATEADD(hour,9,CONVERT(datetime2(0),@PrevStart));
DECLARE @ElecPrev datetime2(0)=DATEADD(hour,10,CONVERT(datetime2(0),DATEADD(day,2,@PrevStart)));
DECLARE @WaterPrev datetime2(0)=DATEADD(hour,10,CONVERT(datetime2(0),DATEADD(day,2,@PrevStart)));
DECLARE @NetPrev datetime2(0)=DATEADD(hour,10,CONVERT(datetime2(0),DATEADD(day,4,@PrevStart)));
DECLARE @RepairPrev datetime2(0)=DATEADD(hour,15,CONVERT(datetime2(0),DATEADD(day,18,@PrevStart)));
DECLARE @SuppliesPrev datetime2(0)=DATEADD(hour,15,CONVERT(datetime2(0),DATEADD(day,26,@PrevStart)));

CREATE TABLE #Exp(Dt datetime2(0),MaDMTC char(5),Amount decimal(18,0),Note nvarchar(200),MaTS char(5) NULL);
INSERT INTO #Exp (Dt,MaDMTC,Amount,Note,MaTS) VALUES
(@RentPrev,'TC003',25000000,N'Thue mat bang thang truoc',NULL),
(@ElecPrev,'TC001',7200000,N'Tien dien thang truoc',NULL),
(@WaterPrev,'TC002',780000,N'Tien nuoc thang truoc',NULL),
(@NetPrev,'TC007',660000,N'Internet va dien thoai thang truoc',NULL),
(@RepairPrev,'TC006',1200000,N'Sua may nen tu lanh',NULL),
(@SuppliesPrev,'TC008',600000,N'Van phong pham va do dung nho',NULL),
(@RentCur,'TC003',25000000,N'Thue mat bang thang nay',NULL),
(DATEADD(hour,-32,@Now),'TC001',6800000,N'Tien dien thang nay',NULL),
(DATEADD(hour,-30,@Now),'TC002',850000,N'Tien nuoc thang nay',NULL),
(DATEADD(day,-4,@Now),'TC007',660000,N'Internet va dien thoai thang nay',NULL),
(DATEADD(day,-12,@Now),'TC006',450000,N'Sua may xay sinh to',NULL),
(DATEADD(day,-6,@Now),'TC008',850000,N'Mua vat tu phat sinh quay bar',NULL),
(DATEADD(day,-40,@Now),'TC004',3200000,N'Mua loa bluetooth phong khach',@Ts3);

DECLARE @eDt datetime2(0),@eCat char(5),@eAmt decimal(18,0),@eNote nvarchar(200),@eTs char(5),@etc char(10);
DECLARE expCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT Dt,MaDMTC,Amount,Note,MaTS FROM #Exp ORDER BY Dt;
OPEN expCursor;
FETCH NEXT FROM expCursor INTO @eDt,@eCat,@eAmt,@eNote,@eTs;
WHILE @@FETCH_STATUS=0
BEGIN
    SET @etc=CAST('TC'+RIGHT('00000000'+CONVERT(varchar(20),NEXT VALUE FOR dbo.SeqPhieuThuChi),8) AS char(10));
    INSERT INTO dbo.PHIEUTHUCHI (MaTC,NgayLap,MaDMTC,MaNV,SoTien,GhiChu,MaTS,OriginKind)
    VALUES(@etc,@eDt,@eCat,@pnv,@eAmt,@eNote,@eTs,'Manual');
    FETCH NEXT FROM expCursor INTO @eDt,@eCat,@eAmt,@eNote,@eTs;
END;
CLOSE expCursor;
DEALLOCATE expCursor;

/* ============================================================
   7. XUAT KHO + KIEM KE
   ============================================================ */
DECLARE @px char(10),@pxDt datetime2(0);
SET @px=CAST('PX'+RIGHT('00000000'+CONVERT(varchar(20),NEXT VALUE FOR dbo.SeqPhieuXuat),8) AS char(10));
SET @pxDt=DATEADD(hour,10,CONVERT(datetime2(0),DATEADD(day,-45,@Today)));
INSERT INTO dbo.PHIEUXUAT (MaPX,NgayXuat,MaNV,LoaiXuat,DaChot,CutoverKind,LyDo)
VALUES(@px,@pxDt,'NV010',N'Huy do hong',0,'New',N'Sua tuoi va xoai qua han, khong dung duoc');
INSERT INTO dbo.CHITIET_PX (MaPX,MaNVL,SoLuongXuat) VALUES(@px,'NVL02',2.0),(@px,'NVL08',1.5);
UPDATE dbo.PHIEUXUAT SET DaChot=1,NgayChot=@pxDt WHERE MaPX=@px;
INSERT INTO dbo.G03OperationReceipt (RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,CompletedAt)
VALUES(NEWID(),@Actor,'StockIssueCreate',@px,HASHBYTES('SHA2_256',CONVERT(varbinary(max),@px)),@pxDt);

SET @px=CAST('PX'+RIGHT('00000000'+CONVERT(varchar(20),NEXT VALUE FOR dbo.SeqPhieuXuat),8) AS char(10));
SET @pxDt=DATEADD(hour,16,CONVERT(datetime2(0),DATEADD(day,-8,@Today)));
INSERT INTO dbo.PHIEUXUAT (MaPX,NgayXuat,MaNV,LoaiXuat,DaChot,CutoverKind,LyDo)
VALUES(@px,@pxDt,'NV010',N'Tra hang',0,'New',N'Tra dua tuoi loi cho nha cung cap');
INSERT INTO dbo.CHITIET_PX (MaPX,MaNVL,SoLuongXuat) VALUES(@px,'NVL16',2.0);
UPDATE dbo.PHIEUXUAT SET DaChot=1,NgayChot=@pxDt WHERE MaPX=@px;
INSERT INTO dbo.G03OperationReceipt (RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,CompletedAt)
VALUES(NEWID(),@Actor,'StockIssueCreate',@px,HASHBYTES('SHA2_256',CONVERT(varbinary(max),@px)),@pxDt);

DECLARE @kk char(10)=CAST('KK'+RIGHT('00000000'+CONVERT(varchar(20),NEXT VALUE FOR dbo.SeqKiemKe),8) AS char(10));
DECLARE @kkAt datetime2(0)=DATEADD(hour,14,CONVERT(datetime2(0),DATEADD(day,-5,@Today)));
INSERT INTO dbo.PHIEUKIEMKE (MaKK,NgayKK,MaNV,DaDieuChinh,SnapshotAt,CutoverKind)
VALUES(@kk,CONVERT(date,@kkAt),'NV010',0,@kkAt,'New');
INSERT INTO dbo.CHITIET_KK (MaKK,MaNVL,TonHeThong,TonThucTe,LyDo,MaterialRevision)
SELECT @kk,MaNVL,SoLuongTon,
    SoLuongTon+CASE MaNVL WHEN 'NVL01' THEN 0.5 WHEN 'NVL03' THEN -0.2 ELSE 0 END,
    CASE MaNVL WHEN 'NVL01' THEN N'Kiem ke thua 0,5 kg ca phe' WHEN 'NVL03' THEN N'Kiem ke thieu 0,2 kg duong' ELSE NULL END,
    Revision
FROM dbo.NGUYENVATLIEU;
UPDATE n SET SoLuongTon=SoLuongTon+CASE n.MaNVL WHEN 'NVL01' THEN 0.5 WHEN 'NVL03' THEN -0.2 ELSE 0 END
FROM dbo.NGUYENVATLIEU n WHERE n.MaNVL IN ('NVL01','NVL03');
UPDATE dbo.PHIEUKIEMKE SET DaDieuChinh=1 WHERE MaKK=@kk;
INSERT INTO dbo.G03OperationReceipt (RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,CompletedAt,ResultRevision)
VALUES(NEWID(),@Actor,'StocktakeCommit',@kk,HASHBYTES('SHA2_256',CONVERT(varbinary(max),@kk)),@kkAt,
    (SELECT Revision FROM dbo.PHIEUKIEMKE WHERE MaKK=@kk));

/* ============================================================
   8. KIEM TRA BAT BIEN TRUOC COMMIT
   ============================================================ */
IF (SELECT COUNT(*) FROM dbo.HOADON WHERE NgayLap>=@Start AND TrangThai=N'Da thanh toan')<2500
    THROW 51910,N'So hoa don da thanh toan trong 60 ngay qua it hon 2500.',1;
IF EXISTS (SELECT 1 FROM dbo.HOADON h WHERE h.NgayLap>=@Start AND h.TrangThai=N'Da thanh toan'
    AND h.ThanhTien<>COALESCE((SELECT SUM(p.SoTien) FROM dbo.CHITIET_THANHTOAN p WHERE p.MaHD=h.MaHD),0))
    THROW 51911,N'Co hoa don da thu khong khop tong thanh toan.',1;
IF EXISTS (SELECT 1 FROM dbo.HOADON h WHERE h.NgayLap>=@Start AND h.TrangThai=N'Da thanh toan'
    AND NOT EXISTS (SELECT 1 FROM dbo.TIEUHAO_HOADON t WHERE t.MaHD=h.MaHD))
    THROW 51912,N'Co hoa don da thu thieu tieu hao nguyen lieu.',1;
IF EXISTS (SELECT 1 FROM dbo.HOADON h WHERE h.NgayLap>=@Start AND h.TrangThai=N'Da thanh toan'
    AND NOT EXISTS (SELECT 1 FROM dbo.HoaDonUuDaiSnapshot s WHERE s.MaHD=h.MaHD AND s.Discount=h.GiamGia))
    THROW 51913,N'Co hoa don da thu thieu hoac lech snapshot uu dai.',1;
IF EXISTS (SELECT 1 FROM dbo.HOADON h WHERE h.NgayLap>=@Start
    AND h.TongTien<>COALESCE((SELECT SUM(CONVERT(decimal(18,0),l.SoLuong)*l.GiaBan) FROM dbo.CHITIET_HD l WHERE l.MaHD=h.MaHD),0))
    THROW 51914,N'Co hoa don lech tong tien chi tiet mon.',1;
IF EXISTS (SELECT 1 FROM dbo.KHACHHANG k JOIN dbo.CustomerOpening o ON o.MaKH=k.MaKH
    LEFT JOIN (SELECT MaKH,SUM(CASE WHEN Loai=N'Cong' THEN SoDiem ELSE -SoDiem END) D FROM dbo.LICHSU_DIEM GROUP BY MaKH) x
      ON x.MaKH=k.MaKH
    WHERE k.MaKH>='KH011' AND k.DiemTichLuy<>o.Points+COALESCE(x.D,0))
    THROW 51915,N'Diem khach hang moi lech so mo va lich su diem.',1;
IF EXISTS (SELECT 1 FROM dbo.CONGNO_NCC c
    WHERE (SELECT COALESCE(SUM(tc.SoTien),0) FROM dbo.THANHTOAN_CONGNO t
           JOIN dbo.PHIEUTHUCHI tc ON tc.MaTC=t.MaTC WHERE t.MaCN=c.MaCN)>c.TienNo)
    THROW 51916,N'Co cong no bi tra vuot so goc.',1;
IF EXISTS (SELECT 1 FROM dbo.BANGLUONG b
    WHERE b.Nam=YEAR(@Per1) AND b.Thang IN (MONTH(@Per1),MONTH(@Per2))
      AND COALESCE((SELECT SUM(tc.SoTien) FROM dbo.PHIEUTHUCHI tc WHERE tc.MaBL=b.MaBL AND tc.OriginKind='Payroll'),0) NOT IN (0,b.ThucLinh))
    THROW 51917,N'Phieu chi luong khong khop thuc linh bang luong.',1;
IF EXISTS (SELECT 1 FROM dbo.NGUYENVATLIEU WHERE SoLuongTon<0)
    THROW 51918,N'Ton kho am sau khi nap du lieu mau.',1;

COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;

/* ============================================================
   9. TONG KET
   ============================================================ */
SELECT @Start AS TuNgay,@Today AS DenNgay,
    (SELECT COUNT(*) FROM dbo.HOADON WHERE NgayLap>=@Start) AS SoHoaDon,
    (SELECT COUNT(*) FROM dbo.HOADON WHERE NgayLap>=@Start AND TrangThai=N'Da thanh toan') AS HoaDonDaThu,
    (SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai=N'Chua thanh toan') AS HoaDonDangMo,
    (SELECT SUM(ThanhTien) FROM dbo.HOADON WHERE NgayLap>=@Start AND TrangThai=N'Da thanh toan') AS DoanhThu;

SELECT (SELECT COUNT(*) FROM dbo.PHIEUNHAP WHERE NgayNhap>=@Start) AS SoPhieuNhap,
    (SELECT SUM(TongTien) FROM dbo.PHIEUNHAP WHERE NgayNhap>=@Start) AS TienNhap,
    (SELECT COUNT(*) FROM dbo.PHIEUTHUCHI WHERE NgayLap>=DATEADD(day,-30,@Now)) AS SoPhieuChi30Ngay,
    (SELECT SUM(SoTien) FROM dbo.PHIEUTHUCHI WHERE NgayLap>=DATEADD(day,-30,@Now)) AS TienChi30Ngay,
    (SELECT SUM(c.TienNo)-COALESCE((SELECT SUM(tc.SoTien) FROM dbo.THANHTOAN_CONGNO t JOIN dbo.PHIEUTHUCHI tc ON tc.MaTC=t.MaTC),0)
       FROM dbo.CONGNO_NCC c) AS CongNoConLai;

SELECT (SELECT COUNT(*) FROM dbo.CHAMCONG WHERE NgayLam>=@AttStart) AS DongChamCong,
    (SELECT COUNT(*) FROM dbo.BANGLUONG WHERE Nam=YEAR(@Per1) AND Thang IN (MONTH(@Per1),MONTH(@Per2))) AS BangLuong,
    (SELECT COUNT(*) FROM dbo.PHIEUTHUCHI WHERE OriginKind='Payroll') AS PhieuChiLuong,
    (SELECT COUNT(*) FROM dbo.PHIEUXUAT WHERE NgayXuat>=@Start) AS PhieuXuat,
    (SELECT COUNT(*) FROM dbo.PHIEUKIEMKE WHERE NgayKK>=@Start) AS PhieuKiemKe;

SELECT (SELECT COUNT(*) FROM dbo.KHACHHANG) AS KhachHang,
    (SELECT COUNT(*) FROM dbo.SANPHAM) AS SanPham,
    (SELECT COUNT(*) FROM dbo.NGUYENVATLIEU) AS NguyenVatLieu,
    (SELECT COUNT(*) FROM dbo.KHUYENMAI) AS KhuyenMai,
    (SELECT COUNT(*) FROM dbo.TAISAN_CODINH) AS TaiSan,
    (SELECT COUNT(*) FROM dbo.NGUYENVATLIEU WHERE SoLuongTon<0) AS NguyenLieuAm;
