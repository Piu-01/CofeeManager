-- TOP-UP DU LIEU MAU HANG NGAY cho CoffeeManager_Development.
-- Bo sung phan con thieu khi seed 60 ngay da cu theo ngay chay:
--   * Hoa don cho 7 ngay gan nhat (ke ca hom nay) theo khung gio thuc te
--     (sang, trua, chieu, toi), du 3 kenh va nhieu phuong thuc thanh toan.
--   * Cham cong hom nay cho ca da bat dau.
--   * Bang luong thang hien tai cho nhan vien part-time (tra theo gio).
--   * Tai san moi mua va chi phi bao tri gan tai san trong 30 ngay.
-- Chi THEM du lieu, khong sua/xoa du lieu cu; chay lai trong ngay khong nhan doi.
-- Viet khong dau de dong bo danh muc goc va tranh loi encoding sqlcmd.
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
    THROW 51950,N'Chi chay tren DB CoffeeManager.',1;
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE Version>=29)
    THROW 51950,N'Can schema 029 tro len.',1;
IF NOT EXISTS (SELECT 1 FROM dbo.DevelopmentMarker)
    THROW 51950,N'Chi chay tren DB development co marker.',1;
IF NOT EXISTS (SELECT 1 FROM dbo.KHUYENMAI WHERE MaKM='KM26HRO')
    THROW 51950,N'Can chay seed-demo-operational.sql truoc.',1;

DECLARE @Now datetime2(0)=SYSDATETIME();
DECLARE @Today date=CONVERT(date,@Now);
DECLARE @Actor int=(SELECT MIN(MaTK) FROM dbo.TAIKHOAN);
DECLARE @ActorEmployee char(5)=N'NV001';

BEGIN TRY
BEGIN TRAN;

/* ============================================================
   1. HOA DON TOP-UP 7 NGAY GAN NHAT
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

-- Ban khong co don mo; hoa don tao o day duoc thu ngay trong transaction.
CREATE TABLE #FreeBan(MaBan char(5) PRIMARY KEY);
INSERT INTO #FreeBan
SELECT b.MaBan FROM dbo.BAN b
WHERE NOT EXISTS (SELECT 1 FROM dbo.HOADON h WHERE h.MaBan=b.MaBan AND h.TrangThai=N'Chua thanh toan');

-- Khung gio ban hang: 06:30-10:30, 11:00-13:30, 14:00-17:00, 18:00-21:30.
CREATE TABLE #Bucket(Id int,FromMin int,ToMin int,Cum int);
INSERT INTO #Bucket VALUES (1,390,630,30),(2,660,810,55),(3,840,1020,75),(4,1080,1290,100);

DECLARE @day date=DATEADD(day,-6,@Today),@dow int,@base int,@nowMin int,@maxMin int,
    @target int,@existing int,@need int,@i int,@r int,@minute int,@cap int,@t datetime2(0),
    @hd char(10),@channel nvarchar(20),@ban char(5),@kh char(5),@km char(10),@maDt char(5),
    @ship decimal(18,0),@rec nvarchar(100),@phone varchar(15),@addr nvarchar(250),@nv char(5),
    @tong decimal(18,0),@disc decimal(18,0),@pct int,@src varchar(20),@tier char(5),@promoOut char(10),
    @partnerPct decimal(5,2),@fee decimal(18,0),@payable decimal(18,0),@p1 decimal(18,0),
    @pm1 char(5),@pm2 char(5),@pts int,@rev binary(8),@gs nvarchar(20),
    @lines int,@sp char(5),@price decimal(18,0),@qty int,@note nvarchar(100),@try int;

WHILE @day<=@Today
BEGIN
    SET @dow=DATEDIFF(day,0,@day)%7; -- 0=Thu 2, 5=Thu 7, 6=Chu nhat
    SET @base=CASE @dow WHEN 5 THEN 68 WHEN 6 THEN 62 WHEN 4 THEN 58
                        WHEN 3 THEN 50 WHEN 2 THEN 48 WHEN 1 THEN 47 ELSE 50 END;
    SET @nowMin=DATEDIFF(minute,CONVERT(datetime2(0),@day),@Now);
    IF @day=@Today
        SET @maxMin=CASE WHEN @nowMin>=1290 THEN 1290 ELSE @nowMin-15 END;
    ELSE
        SET @maxMin=1290;
    IF @maxMin<390
    BEGIN
        SET @day=DATEADD(day,1,@day);
        CONTINUE;
    END;
    SET @target=CASE WHEN @day=@Today
        THEN (SELECT CASE WHEN v<3 THEN 3 ELSE v END FROM (SELECT CONVERT(int,ROUND(CONVERT(decimal(10,2),@base)*(@maxMin-390)/900.0,0)) v) t)
        ELSE @base END;
    SET @existing=ISNULL((SELECT COUNT(*) FROM dbo.HOADON WHERE CONVERT(date,NgayLap)=@day),0);
    SET @need=@target-@existing;
    SET @i=0;
    WHILE @i<@need
    BEGIN
        SET @i+=1;
        SET @minute=NULL;
        SET @r=ABS(CHECKSUM(NEWID())%100);
        SELECT TOP(1) @minute=FromMin+ABS(CHECKSUM(NEWID()))%(CASE WHEN ToMin>@maxMin THEN @maxMin ELSE ToMin END-FromMin+1)
        FROM #Bucket
        WHERE Cum>@r AND FromMin<=@maxMin
        ORDER BY Cum;
        IF @minute IS NULL
            SET @minute=390+ABS(CHECKSUM(NEWID()))%(@maxMin-390+1);
        SET @t=DATEADD(minute,@minute,CONVERT(datetime2(0),@day));
        IF @t>=@Now
            CONTINUE;

        SET @ban=NULL; SET @kh=NULL; SET @km=NULL; SET @maDt=NULL; SET @ship=0;
        SET @rec=NULL; SET @phone=NULL; SET @addr=NULL; SET @partnerPct=0;
        SET @r=ABS(CHECKSUM(NEWID())%100);
        IF @r<50 SET @channel=N'Tai quan';
        ELSE IF @r<80 SET @channel=N'Mang di';
        ELSE SET @channel=N'Giao hang';

        IF @channel=N'Tai quan'
        BEGIN
            SELECT TOP(1) @ban=MaBan FROM #FreeBan ORDER BY CHECKSUM(NEWID());
            IF @ban IS NULL SET @channel=N'Mang di';
        END;

        IF ABS(CHECKSUM(NEWID())%100) < CASE WHEN @channel=N'Giao hang' THEN 60 ELSE 35 END
            SELECT TOP(1) @kh=MaKH FROM dbo.KHACHHANG ORDER BY CHECKSUM(NEWID());

        IF ABS(CHECKSUM(NEWID())%100)<12
            SELECT TOP(1) @km=MaKM FROM dbo.KHUYENMAI
            WHERE CONVERT(date,@t) BETWEEN NgayBD AND NgayKT AND MaKM LIKE 'KM26%'
            ORDER BY CHECKSUM(NEWID());

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
        SELECT @disc=Discount,@pct=PercentDiscount,@src=Source,@tier=Tier,@promoOut=Promotion
        FROM dbo.fn_DiscountPreview(@tong,@ship,CONVERT(date,@t),@kh,@km);
        SET @disc=COALESCE(@disc,0); SET @pct=COALESCE(@pct,0); SET @src=COALESCE(@src,'None');
        IF @disc>0 UPDATE dbo.HOADON SET GiamGia=@disc WHERE MaHD=@hd;

        INSERT INTO dbo.HoaDonUuDaiSnapshot (MaHD,Kind,Source,TierCode,PromotionCode,PercentDiscount,Discount,RecordedAt,PolicyRevision,CustomerRevision,PromotionRevision)
        SELECT @hd,'Checkout',@src,@tier,@promoOut,@pct,@disc,@t,
            (SELECT Revision FROM dbo.LoyaltyPolicyState),
            (SELECT Revision FROM dbo.KHACHHANG WHERE MaKH=@kh),
            (SELECT Revision FROM dbo.KHUYENMAI WHERE MaKM=@promoOut);

        INSERT INTO dbo.TIEUHAO_HOADON (MaHD,MaNVL,SoLuong)
        SELECT @hd,c.MaNVL,CONVERT(decimal(18,4),SUM(CONVERT(decimal(18,4),l.SoLuong)*c.DinhMuc))
        FROM dbo.CHITIET_HD l JOIN dbo.CONGTHUC c ON c.MaSP=l.MaSP
        WHERE l.MaHD=@hd GROUP BY c.MaNVL;

        SET @payable=@tong+@ship-@disc;
        SET @r=ABS(CHECKSUM(NEWID())%100);
        SET @pm1=CASE WHEN @r<30 THEN 'PT001' WHEN @r<60 THEN 'PT002' WHEN @r<74 THEN 'PT004'
                      WHEN @r<81 THEN 'PT005' WHEN @r<87 THEN 'PT006' WHEN @r<91 THEN 'PT003'
                      WHEN @r<97 THEN 'PT007' ELSE 'PT009' END;
        IF ABS(CHECKSUM(NEWID())%100)<82
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
            SET @gs=CASE WHEN @t<DATEADD(hour,-2,@Now) THEN N'Da giao'
                         WHEN ABS(CHECKSUM(NEWID())%100)<65 THEN N'Dang giao' ELSE N'Cho giao' END;
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
    SET @day=DATEADD(day,1,@day);
END;

/* ============================================================
   2. CHAM CONG HOM NAY (ca da bat dau)
   ============================================================ */
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

DECLARE @aMaNV char(5),@aCa char(5),@aStart int,@aEnd int,@aOff int,@aPart bit,
    @attStart datetime2(0),@attEnd datetime2(0),@aIn datetime2(0),@aOut datetime2(0),@aStatus nvarchar(30),@aRnd int;
DECLARE attCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT MaNV,MaCa,StartMin,EndMin,OffDow,PartTime FROM #EmpShift ORDER BY MaNV;
OPEN attCursor;
FETCH NEXT FROM attCursor INTO @aMaNV,@aCa,@aStart,@aEnd,@aOff,@aPart;
WHILE @@FETCH_STATUS=0
BEGIN
    IF DATEDIFF(day,0,@Today)%7<>@aOff
       AND NOT EXISTS (SELECT 1 FROM dbo.CHAMCONG WHERE MaNV=@aMaNV AND NgayLam=@Today)
    BEGIN
        SET @attStart=DATEADD(minute,@aStart,CONVERT(datetime2(0),@Today));
        SET @attEnd=DATEADD(minute,@aEnd,CONVERT(datetime2(0),@Today));
        IF @Now>=@attEnd
        BEGIN
            SET @aRnd=ABS(CHECKSUM(NEWID())%100);
            SET @aIn=DATEADD(minute,ABS(CHECKSUM(NEWID())%9),@attStart);
            SET @aOut=DATEADD(minute,@aEnd+ABS(CHECKSUM(NEWID())%26),CONVERT(datetime2(0),@Today));
            SET @aStatus=CASE WHEN @aRnd<2 THEN N'Vang' WHEN @aRnd<4 THEN N'Nghi phep' ELSE N'Di lam' END;
            INSERT INTO dbo.CHAMCONG (MaNV,NgayLam,MaCa,GioVao,GioRa,TrangThai)
            VALUES(@aMaNV,@Today,@aCa,
                CASE WHEN @aStatus=N'Di lam' THEN @aIn END,
                CASE WHEN @aStatus=N'Di lam' THEN @aOut END,
                @aStatus);
        END
        ELSE IF @Now>=@attStart
        BEGIN
            SET @aIn=DATEADD(minute,ABS(CHECKSUM(NEWID())%9),@attStart);
            INSERT INTO dbo.CHAMCONG (MaNV,NgayLam,MaCa,GioVao,GioRa,TrangThai)
            VALUES(@aMaNV,@Today,@aCa,@aIn,NULL,N'Di lam');
        END;
    END;
    FETCH NEXT FROM attCursor INTO @aMaNV,@aCa,@aStart,@aEnd,@aOff,@aPart;
END;
CLOSE attCursor;
DEALLOCATE attCursor;

/* ============================================================
   3. BANG LUONG THANG HIEN TAI CHO NHAN VIEN PART-TIME
   ============================================================ */
DECLARE @pThang int=MONTH(@Today),@pNam int=YEAR(@Today),
    @pEmp char(5),@pBonus decimal(18,0),@snap nvarchar(max),@tok binary(32),@amt decimal(18,0),
    @bl int;
DECLARE payCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT MaNV,CASE WHEN LoaiHopDong=N'Full-time' THEN 300000 ELSE 100000 END
    FROM dbo.NHANVIEN
    WHERE DangLamViec=1 AND LoaiHopDong=N'Part-time'
      AND NOT EXISTS (SELECT 1 FROM dbo.BANGLUONG b WHERE b.MaNV=NHANVIEN.MaNV AND b.Thang=@pThang AND b.Nam=@pNam)
    ORDER BY MaNV;
OPEN payCursor;
FETCH NEXT FROM payCursor INTO @pEmp,@pBonus;
WHILE @@FETCH_STATUS=0
BEGIN
    SET @snap=NULL; SET @tok=NULL; SET @amt=NULL;
    EXEC dbo.sp_PayrollCalculate
        @Employee=@pEmp,@Month=@pThang,@Year=@pNam,
        @Regular=NULL,@Overtime=0,@OvertimePay=0,@Allowance=NULL,
        @Bonus=@pBonus,@Deduction=0,
        @Snapshot=@snap OUTPUT,@Token=@tok OUTPUT,@Amount=@amt OUTPUT;
    IF NOT EXISTS (SELECT 1 FROM OPENJSON(@snap,'$.Issues'))
    BEGIN
        INSERT INTO dbo.BANGLUONG (MaNV,Thang,Nam,NgayChot,LuongCoBan,SoGioLam,DonGiaGio,TienTangCa,PhuCap,Thuong,KhauTru,
            SnapshotKind,ContractSnapshot,EmployeeNameSnapshot,PositionSnapshot,PositionNameSnapshot,
            MonthlyRateSnapshot,HourlyRateSnapshot,TotalHours,RegularHours,OvertimeHours,TotalSeconds,ActorAccountId,SourceSnapshot)
        SELECT @pEmp,@pThang,@pNam,@Now,BasePay,PaidHours,HourlyPayRate,OvertimePay,Allowance,Bonus,Deduction,
            'New',Contract,EmployeeName,Position,PositionName,
            MonthlyRate,HourlyRate,TotalHours,RegularHours,OvertimeHours,TotalSeconds,@Actor,@snap
        FROM OPENJSON(@snap) WITH(
            BasePay decimal(18,0),PaidHours decimal(10,2),HourlyPayRate decimal(18,0),
            OvertimePay decimal(18,0),Allowance decimal(18,0),Bonus decimal(18,0),Deduction decimal(18,0),
            Contract nvarchar(20),EmployeeName nvarchar(50),Position char(5),PositionName nvarchar(50),
            MonthlyRate decimal(18,0),HourlyRate decimal(18,0),TotalHours decimal(10,2),
            RegularHours decimal(10,2),OvertimeHours decimal(10,2),TotalSeconds bigint);
        SET @bl=CONVERT(int,SCOPE_IDENTITY());

        INSERT INTO dbo.PayrollAttendanceSnapshot (MaBL,MaNV,NgayLam,MaCa,GioVao,GioRa,TrangThai,SourceRevision)
        SELECT @bl,MaNV,NgayLam,MaCa,GioVao,GioRa,TrangThai,Revision
        FROM dbo.CHAMCONG
        WHERE MaNV=@pEmp AND NgayLam BETWEEN DATEFROMPARTS(@pNam,@pThang,1) AND EOMONTH(DATEFROMPARTS(@pNam,@pThang,1));

        INSERT INTO dbo.G03OperationReceipt (RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,CompletedAt)
        VALUES(NEWID(),@Actor,'PayrollCommit',CONVERT(varchar(10),@bl),HASHBYTES('SHA2_256',CONVERT(varbinary(max),@snap)),@Now);
    END;
    FETCH NEXT FROM payCursor INTO @pEmp,@pBonus;
END;
CLOSE payCursor;
DEALLOCATE payCursor;

/* ============================================================
   4. TAI SAN MOI + CHI PHI BAO TRI GAN TAI SAN
   ============================================================ */
DECLARE @ts char(5),@tc char(10),@buyDt datetime2(0),@assetName nvarchar(50),
    @assetValue decimal(18,0),@assetMonths int,@daysAgo int;

CREATE TABLE #NewAsset(Name nvarchar(50),Value decimal(18,0),Months int,DaysAgo int);
INSERT INTO #NewAsset VALUES
(N'May xay sinh to cong nghiep',4200000,36,25),
(N'Ke trung bay banh nuong',6500000,60,18);

DECLARE assetCursor CURSOR LOCAL FAST_FORWARD FOR SELECT Name,Value,Months,DaysAgo FROM #NewAsset ORDER BY DaysAgo DESC;
OPEN assetCursor;
FETCH NEXT FROM assetCursor INTO @assetName,@assetValue,@assetMonths,@daysAgo;
WHILE @@FETCH_STATUS=0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.TAISAN_CODINH WHERE TenTS=@assetName)
    BEGIN
        SET @ts=CAST('TS'+RIGHT('000'+CONVERT(varchar(10),NEXT VALUE FOR dbo.SeqTaiSan),3) AS char(5));
        SET @buyDt=DATEADD(hour,10,CONVERT(datetime2(0),DATEADD(day,-@daysAgo,@Today)));
        INSERT INTO dbo.TAISAN_CODINH (MaTS,TenTS,NgayMua,GiaTri,SoThangKH)
        VALUES(@ts,@assetName,CONVERT(date,@buyDt),@assetValue,@assetMonths);
        SET @tc=CAST('TC'+RIGHT('00000000'+CONVERT(varchar(20),NEXT VALUE FOR dbo.SeqPhieuThuChi),8) AS char(10));
        INSERT INTO dbo.PHIEUTHUCHI (MaTC,NgayLap,MaDMTC,MaNV,SoTien,GhiChu,MaTS,OriginKind)
        VALUES(@tc,@buyDt,'TC004',@ActorEmployee,@assetValue,N'Mua tai san: '+@assetName,@ts,'Manual');
    END;
    FETCH NEXT FROM assetCursor INTO @assetName,@assetValue,@assetMonths,@daysAgo;
END;
CLOSE assetCursor;
DEALLOCATE assetCursor;

CREATE TABLE #AssetExpense(Ts char(5),DaysAgo int,Amount decimal(18,0),Note nvarchar(200));
INSERT INTO #AssetExpense VALUES
('TS001',12,850000,N'Bao tri may pha ca phe Linea: ve sinh va thay gioang'),
('TS007',8,450000,N'Bao tri may tinh tien POS: ve sinh va cap nhat'),
('TS003',5,1200000,N'Bao tri tu mat: nap gas va ve sinh dan lanh');

DECLARE @eTs char(5),@eDays int,@eAmt decimal(18,0),@eNote nvarchar(200),@eDt datetime2(0);
DECLARE expCursor CURSOR LOCAL FAST_FORWARD FOR SELECT Ts,DaysAgo,Amount,Note FROM #AssetExpense ORDER BY DaysAgo DESC;
OPEN expCursor;
FETCH NEXT FROM expCursor INTO @eTs,@eDays,@eAmt,@eNote;
WHILE @@FETCH_STATUS=0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.PHIEUTHUCHI WHERE GhiChu=@eNote)
    BEGIN
        SET @eDt=DATEADD(hour,15,CONVERT(datetime2(0),DATEADD(day,-@eDays,@Today)));
        SET @tc=CAST('TC'+RIGHT('00000000'+CONVERT(varchar(20),NEXT VALUE FOR dbo.SeqPhieuThuChi),8) AS char(10));
        INSERT INTO dbo.PHIEUTHUCHI (MaTC,NgayLap,MaDMTC,MaNV,SoTien,GhiChu,MaTS,OriginKind)
        VALUES(@tc,@eDt,'TC006',@ActorEmployee,@eAmt,@eNote,@eTs,'Manual');
    END;
    FETCH NEXT FROM expCursor INTO @eTs,@eDays,@eAmt,@eNote;
END;
CLOSE expCursor;
DEALLOCATE expCursor;

-- Chi phi van hanh trong ngay (mua da, trai cay, vat tu nho quay bar).
IF NOT EXISTS (SELECT 1 FROM dbo.PHIEUTHUCHI
    WHERE GhiChu=N'Mua da, trai cay va vat tu nho trong ngay'
      AND CONVERT(date,NgayLap)=@Today)
BEGIN
    SET @eDt=CASE WHEN DATEADD(hour,9,CONVERT(datetime2(0),@Today))<@Now
        THEN DATEADD(hour,9,CONVERT(datetime2(0),@Today))
        ELSE DATEADD(hour,-1,@Now) END;
    SET @tc=CAST('TC'+RIGHT('00000000'+CONVERT(varchar(20),NEXT VALUE FOR dbo.SeqPhieuThuChi),8) AS char(10));
    INSERT INTO dbo.PHIEUTHUCHI (MaTC,NgayLap,MaDMTC,MaNV,SoTien,GhiChu,MaTS,OriginKind)
    VALUES(@tc,@eDt,'TC008',@ActorEmployee,320000,N'Mua da, trai cay va vat tu nho trong ngay',NULL,'Manual');
END;

/* ============================================================
   5. KIEM TRA BAT BIEN TRUOC COMMIT
   ============================================================ */
IF EXISTS (SELECT 1 FROM dbo.HOADON h
    WHERE CONVERT(date,h.NgayLap)=@Today AND h.TrangThai=N'Da thanh toan'
      AND h.ThanhTien<>COALESCE((SELECT SUM(p.SoTien) FROM dbo.CHITIET_THANHTOAN p WHERE p.MaHD=h.MaHD),0))
    THROW 51951,N'Top-up: tong thanh toan khong khop hoa don hom nay.',1;
IF EXISTS (SELECT 1 FROM dbo.HOADON h
    WHERE CONVERT(date,h.NgayLap)=@Today AND h.TrangThai=N'Da thanh toan'
      AND NOT EXISTS (SELECT 1 FROM dbo.TIEUHAO_HOADON t WHERE t.MaHD=h.MaHD))
    THROW 51951,N'Top-up: hoa don hom nay thieu tieu hao.',1;
IF EXISTS (SELECT 1 FROM dbo.NGUYENVATLIEU WHERE SoLuongTon<0)
    THROW 51951,N'Top-up: ton kho am sau khi them hoa don.',1;
IF EXISTS (SELECT 1 FROM dbo.BANGLUONG WHERE Thang=@pThang AND Nam=@pNam AND ThucLinh<0)
    THROW 51951,N'Top-up: bang luong am.',1;

/* ============================================================
   6. TONG KET
   ============================================================ */
SELECT CONVERT(date,@Now) AS Ngay,
    (SELECT COUNT(*) FROM dbo.HOADON WHERE CONVERT(date,NgayLap)=@Today) AS HoaDonHomNay,
    (SELECT COUNT(*) FROM dbo.HOADON WHERE CONVERT(date,NgayLap)=@Today AND TrangThai=N'Da thanh toan') AS DaThuHomNay,
    (SELECT COUNT(*) FROM dbo.HOADON WHERE CONVERT(date,NgayLap)=@Today AND LoaiDon=N'Giao hang') AS GiaoHangHomNay,
    (SELECT COUNT(*) FROM dbo.CHAMCONG WHERE NgayLam=@Today) AS ChamCongHomNay,
    (SELECT COUNT(*) FROM dbo.BANGLUONG WHERE Thang=@pThang AND Nam=@pNam) AS BangLuongThangNay,
    (SELECT COUNT(*) FROM dbo.TAISAN_CODINH) AS TaiSan,
    (SELECT COUNT(*) FROM dbo.PHIEUTHUCHI WHERE MaTS IS NOT NULL AND NgayLap>=DATEADD(day,-30,@Now)) AS ChiPhiTaiSan30Ngay,
    (SELECT SUM(ThanhTien) FROM dbo.HOADON WHERE CONVERT(date,NgayLap)=@Today AND TrangThai=N'Da thanh toan') AS DoanhThuHomNay;

COMMIT;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
