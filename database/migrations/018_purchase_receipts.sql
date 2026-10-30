-- T20: nhập kho nguyên tử, không ghi nháp hoặc cộng tồn hai lần.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=17) THROW 52707,N'Cần schema017.',1;
IF EXISTS(SELECT 1 FROM dbo.PHIEUNHAP p WHERE p.TongTien<>COALESCE((SELECT SUM(ROUND(SoLuong*DonGiaNhap,0)) FROM dbo.CHITIET_PN d WHERE d.MaPN=p.MaPN),0) OR NOT EXISTS(SELECT 1 FROM dbo.CHITIET_PN d WHERE d.MaPN=p.MaPN)) THROW 52707,N'Phiếu nhập cũ rỗng hoặc tổng sai.',1;
ALTER TABLE dbo.PHIEUNHAP ADD DaChot bit NOT NULL CONSTRAINT DF_PurchaseClosed DEFAULT 1,Revision rowversion,
 CutoverKind varchar(10) NOT NULL CONSTRAINT DF_PurchaseCutover DEFAULT 'Legacy',NgayChot datetime2(0) NULL;
DECLARE @pn bigint=(SELECT COALESCE(MAX(TRY_CONVERT(bigint,SUBSTRING(MaPN,3,8))),0)+1 FROM dbo.PHIEUNHAP WHERE MaPN COLLATE Latin1_General_100_BIN2 LIKE 'PN[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
 @cn bigint=(SELECT COALESCE(MAX(TRY_CONVERT(bigint,SUBSTRING(MaCN,3,8))),0)+1 FROM dbo.CONGNO_NCC WHERE MaCN COLLATE Latin1_General_100_BIN2 LIKE 'CN[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]');
IF @pn>99999999 OR @cn>99999999 THROW 52707,N'Hết miền mã nhập/nợ.',1;
DECLARE @sql nvarchar(max)=N'CREATE SEQUENCE dbo.SeqPhieuNhap AS bigint START WITH '+CONVERT(varchar(20),@pn)+N' INCREMENT BY 1 MINVALUE 1 MAXVALUE 99999999 NO CYCLE NO CACHE; CREATE SEQUENCE dbo.SeqCongNo AS bigint START WITH '+CONVERT(varchar(20),@cn)+N' INCREMENT BY 1 MINVALUE 1 MAXVALUE 99999999 NO CYCLE NO CACHE;'; EXEC sys.sp_executesql @sql;
DENY INSERT,UPDATE,DELETE ON dbo.PHIEUNHAP TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.CHITIET_PN TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.CONGNO_NCC TO CafeManagerApp;
GO
CREATE TYPE dbo.PurchaseLinesInput AS TABLE(MaNVL varchar(100),Quantity decimal(38,18),UnitPrice decimal(38,18));
GO
CREATE PROCEDURE dbo.sp_PurchaseValidate @Lines dbo.PurchaseLinesInput READONLY,@Total decimal(18,0) OUTPUT AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM @Lines) OR EXISTS(SELECT 1 FROM @Lines WHERE MaNVL IS NULL OR LEN(MaNVL)<>5 OR Quantity IS NULL OR UnitPrice IS NULL OR Quantity<=0 OR Quantity>99999999999999.9999 OR Quantity<>ROUND(Quantity,4) OR UnitPrice<=0 OR UnitPrice>999999999999999999 OR UnitPrice<>ROUND(UnitPrice,0))
 OR EXISTS(SELECT MaNVL FROM @Lines GROUP BY MaNVL HAVING COUNT(*)>1)
 OR EXISTS(SELECT 1 FROM @Lines l WHERE NOT EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU n WHERE n.MaNVL=l.MaNVL)) THROW 52701,N'Dòng nhập không hợp lệ hoặc trùng nguyên liệu.',1;
 DECLARE @sum decimal(38,0)=(SELECT SUM(ROUND(CONVERT(decimal(18,4),Quantity)*CONVERT(decimal(18,0),UnitPrice),0)) FROM @Lines);
 IF @sum<=0 OR @sum>999999999999999999 THROW 52705,N'Tổng nhập ngoài giới hạn.',1;
 SET @Total=CONVERT(decimal(18,0),@sum);
END;
GO
CREATE PROCEDURE dbo.sp_PurchasePreview @Actor int,@Stamp binary(8),@Lines dbo.PurchaseLinesInput READONLY AS
BEGIN
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE'; DECLARE @total decimal(18,0); EXEC dbo.sp_PurchaseValidate @Lines,@total OUTPUT; SELECT @total Total;
END;
GO
CREATE TRIGGER dbo.TR_PurchaseClosed ON dbo.PHIEUNHAP AFTER UPDATE,DELETE AS
BEGIN
 IF EXISTS(SELECT 1 FROM deleted WHERE DaChot=1) THROW 52702,N'Phiếu nhập đã chốt không sửa/xóa.',1;
END;
GO
CREATE TRIGGER dbo.TR_PurchaseLinesClosed ON dbo.CHITIET_PN AFTER INSERT,UPDATE,DELETE AS
BEGIN
 IF EXISTS(SELECT 1 FROM dbo.PHIEUNHAP p WHERE p.DaChot=1 AND p.MaPN IN(SELECT MaPN FROM inserted UNION SELECT MaPN FROM deleted)) THROW 52702,N'Chi tiết phiếu nhập đã chốt chỉ đọc.',1;
END;
GO
ALTER TRIGGER dbo.TR_CongNo ON dbo.CONGNO_NCC AFTER INSERT,UPDATE,DELETE AS
BEGIN
 IF EXISTS(SELECT 1 FROM deleted d JOIN dbo.PHIEUNHAP p ON p.MaPN=d.MaPN WHERE p.DaChot=1)
 OR EXISTS(SELECT 1 FROM inserted i JOIN dbo.PHIEUNHAP p ON p.MaPN=i.MaPN WHERE p.DaChot=1 OR i.TienNo>p.TongTien) THROW 52702,N'Nguồn nợ đã chốt bất biến hoặc nợ vượt tổng nhập.',1;
END;
GO
CREATE PROCEDURE dbo.sp_LapPhieuNhap @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Supplier varchar(max),@Date datetime2(0),
 @Due date=NULL,@Immediate decimal(38,18),@ExpectedTotal decimal(38,18),@Lines dbo.PurchaseLinesInput READONLY AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 OR @Date IS NULL OR @Supplier IS NULL OR LEN(@Supplier)<>5 OR @Immediate IS NULL OR @Immediate<0 OR @Immediate>999999999999999999 OR @Immediate<>ROUND(@Immediate,0) THROW 52701,N'Thông tin nhập không hợp lệ.',1;
 BEGIN TRY
 BEGIN TRAN;
 DECLARE @json nvarchar(max)=(SELECT MaNVL,Quantity,UnitPrice FROM @Lines ORDER BY MaNVL FOR JSON PATH,INCLUDE_NULL_VALUES),@payload nvarchar(max),@code varchar(10),@replay bit,@total decimal(18,0);
 SET @payload=(SELECT @Supplier s,@Date d,@Due due,@Immediate i,@ExpectedTotal e,@json l FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
 EXEC dbo.sp_FinanceBegin @Actor,@Stamp,'ADMIN,STORE',@RequestId,'PurchaseCreate',@payload,@code OUTPUT,@replay OUTPUT;
 IF @replay=0
 BEGIN
  EXEC dbo.sp_PurchaseValidate @Lines,@total OUTPUT;
  IF @ExpectedTotal IS NULL OR @ExpectedTotal<>@total OR @Immediate>@total THROW 52703,N'Preview hoặc phần trả ngay không hợp lệ.',1;
  IF @Immediate<>0 AND NOT EXISTS(SELECT 1 FROM dbo.TAIKHOAN WHERE MaTK=@Actor AND MaVT='ADMIN') THROW 51605,N'Chỉ quản lý được ghi chi trả ngay.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.NHACUNGCAP WITH(HOLDLOCK) WHERE MaNCC=@Supplier) THROW 52701,N'Nhà cung cấp không tồn tại.',1;
  DECLARE @material char(5),@stock decimal(18,4),@employee char(5)=(SELECT MaNV FROM dbo.TAIKHOAN WHERE MaTK=@Actor),@num bigint=NEXT VALUE FOR dbo.SeqPhieuNhap;
  SET @code='PN'+RIGHT('00000000'+CONVERT(varchar(20),@num),8);
  DECLARE materials CURSOR LOCAL FAST_FORWARD FOR SELECT MaNVL FROM @Lines ORDER BY MaNVL;
  OPEN materials; FETCH NEXT FROM materials INTO @material;
  WHILE @@FETCH_STATUS=0 BEGIN SELECT @stock=SoLuongTon FROM dbo.NGUYENVATLIEU WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@material; FETCH NEXT FROM materials INTO @material; END;
  CLOSE materials; DEALLOCATE materials;
  INSERT dbo.PHIEUNHAP(MaPN,NgayNhap,MaNV,MaNCC,TongTien,DaChot,CutoverKind) VALUES(@code,@Date,@employee,@Supplier,0,0,'New');
  INSERT dbo.CHITIET_PN SELECT @code,MaNVL,CONVERT(decimal(18,4),Quantity),CONVERT(decimal(18,0),UnitPrice) FROM @Lines;
  IF @Immediate>0 BEGIN DECLARE @tc char(10),@money decimal(18,0)=CONVERT(decimal(18,0),@Immediate),@pn char(10)=@code; EXEC dbo.sp_FinanceSystemExpense 'PurchaseImmediate',@employee,@money,@Date,@pn,NULL,NULL,@tc OUTPUT; END;
  IF @Immediate<@total BEGIN SET @num=NEXT VALUE FOR dbo.SeqCongNo; DECLARE @cn char(10)='CN'+RIGHT('00000000'+CONVERT(varchar(20),@num),8); INSERT dbo.CONGNO_NCC(MaCN,MaPN,TienNo,HanTra) VALUES(@cn,@code,@total-@Immediate,@Due); END;
  UPDATE dbo.PHIEUNHAP SET DaChot=1,NgayChot=SYSDATETIME() WHERE MaPN=@code;
  DECLARE @revision binary(8)=(SELECT Revision FROM dbo.PHIEUNHAP WHERE MaPN=@code);
  EXEC dbo.sp_FinanceFinish @Actor,@RequestId,'PurchaseCreate',@payload,@code,@revision;
 END;
 COMMIT; SELECT @code EntityCode;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE VIEW dbo.vw_PurchaseRead AS
 SELECT p.MaPN Code,p.NgayNhap Date,p.MaNCC Supplier,p.TongTien Total,p.CutoverKind,
 CASE WHEN p.CutoverKind='New' THEN COALESCE(f.SoTien,0) ELSE NULL END Immediate,f.MaTC FinanceCode,c.MaCN DebtCode,c.TienNo Debt,p.Revision,
 (SELECT d.MaNVL Code,n.TenNVL Name,n.DonViTinh Unit,d.SoLuong Quantity,d.DonGiaNhap Price FROM dbo.CHITIET_PN d JOIN dbo.NGUYENVATLIEU n ON n.MaNVL=d.MaNVL WHERE d.MaPN=p.MaPN ORDER BY d.MaNVL FOR JSON PATH) LinesJson
 FROM dbo.PHIEUNHAP p LEFT JOIN dbo.PHIEUTHUCHI f ON f.MaPN=p.MaPN LEFT JOIN dbo.CONGNO_NCC c ON c.MaPN=p.MaPN;
GO
CREATE PROCEDURE dbo.sp_PurchaseList @Actor int,@Stamp binary(8),@Search nvarchar(max)=N'',@Skip int=0 AS
BEGIN
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE'; IF @Skip<0 OR LEN(@Search)>100 THROW 52701,N'Bộ lọc sai.',1;
 SELECT *,COUNT(*) OVER() TotalRows FROM dbo.vw_PurchaseRead WHERE Code LIKE N'%'+@Search+N'%' OR Supplier LIKE N'%'+@Search+N'%' ORDER BY Date DESC,Code DESC OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_PurchaseGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE'; SELECT *,1 TotalRows FROM dbo.vw_PurchaseRead WHERE Code=@Code; END;
GO
CREATE PROCEDURE dbo.sp_PurchaseLookups @Actor int,@Stamp binary(8),@Kind varchar(10) AS
BEGIN
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE';
 IF @Kind='Supplier' SELECT MaNCC Code,TenNCC Name,CAST(NULL AS nvarchar(20)) Unit FROM dbo.NHACUNGCAP ORDER BY TenNCC;
 ELSE IF @Kind='Material' SELECT MaNVL Code,TenNVL Name,DonViTinh Unit FROM dbo.NGUYENVATLIEU ORDER BY TenNVL;
 ELSE THROW 52701,N'Loại tra cứu sai.',1;
END;
GO
CREATE PROCEDURE dbo.sp_PurchaseReceipt @Actor int,@Stamp binary(8),@RequestId uniqueidentifier AS
BEGIN EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE'; SELECT EntityCode FROM dbo.G03OperationReceipt WHERE ActorAccountId=@Actor AND RequestId=@RequestId AND Operation='PurchaseCreate'; END;
GO
GRANT EXECUTE ON dbo.sp_LapPhieuNhap TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PurchasePreview TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PurchaseList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PurchaseGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PurchaseLookups TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PurchaseReceipt TO CafeManagerApp;
GRANT EXECUTE,REFERENCES ON TYPE::dbo.PurchaseLinesInput TO CafeManagerApp;

GO
ALTER PROCEDURE dbo.sp_CapMa @Loai VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @number BIGINT;
    BEGIN TRY
        IF @Loai='HOADON'
        BEGIN
            SET @number=NEXT VALUE FOR dbo.SeqHoaDon;
            SELECT @Loai AS Loai,CAST('HD'+RIGHT('00000000'+CONVERT(VARCHAR(20),@number),8) AS CHAR(10)) AS Ma;
        END
        ELSE IF @Loai='DANHMUC_SP'
        BEGIN
            SET @number=NEXT VALUE FOR dbo.SeqDanhMuc;
            SELECT @Loai AS Loai,CAST('DM'+RIGHT('000'+CONVERT(VARCHAR(20),@number),3) AS CHAR(5)) AS Ma;
        END
        ELSE IF @Loai='NHANVIEN'
        BEGIN
            SET @number=NEXT VALUE FOR dbo.SeqNhanVien;
            SELECT @Loai AS Loai,CAST('NV'+RIGHT('000'+CONVERT(VARCHAR(20),@number),3) AS CHAR(5)) AS Ma;
        END
        ELSE IF @Loai='PHIEUTHUCHI'
        BEGIN
            SET @number=NEXT VALUE FOR dbo.SeqPhieuThuChi;
            SELECT @Loai AS Loai,CAST('TC'+RIGHT('00000000'+CONVERT(VARCHAR(20),@number),8) AS CHAR(10)) AS Ma;
        END
        ELSE IF @Loai='PHIEUNHAP'
        BEGIN
            SET @number=NEXT VALUE FOR dbo.SeqPhieuNhap;
            SELECT @Loai AS Loai,CAST('PN'+RIGHT('00000000'+CONVERT(VARCHAR(20),@number),8) AS CHAR(10)) AS Ma;
        END
        ELSE IF @Loai='CONGNO_NCC'
        BEGIN
            SET @number=NEXT VALUE FOR dbo.SeqCongNo;
            SELECT @Loai AS Loai,CAST('CN'+RIGHT('00000000'+CONVERT(VARCHAR(20),@number),8) AS CHAR(10)) AS Ma;
        END
        ELSE THROW 51301,'Unsupported code source.',1;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER()=11728 THROW 51302,'Code range exhausted; contact administrator.',1;
        THROW;
    END CATCH
END;
GO
