-- T19: nguồn thu chi bất biến; chạy bằng Apply-Migration.ps1.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=16)
 THROW 52607,N'Cần migration016 và transaction migration.',1;
IF EXISTS(SELECT 1 FROM dbo.SystemExpenseCategory s JOIN dbo.DANHMUC_THUCHI d ON d.MaDMTC=s.MaDMTC WHERE d.Loai<>N'Chi')
 OR (SELECT COUNT(*) FROM dbo.SystemExpenseCategory WHERE (UseCase='Payroll' AND MaDMTC='TC005') OR (UseCase='SupplierDebt' AND MaDMTC='TC011') OR (UseCase='PurchaseImmediate' AND MaDMTC='TC012'))<>3
 THROW 52607,N'Mapping danh mục chi hệ thống không hợp lệ.',1;
ALTER TABLE dbo.PHIEUTHUCHI ADD MaPN char(10) NULL,MaBL int NULL,
 OriginKind varchar(20) NOT NULL CONSTRAINT DF_FinanceOrigin DEFAULT 'LegacyUnknown';
GO
-- Chỉ backfill nhãn nguồn dưới khóa migration; không thay tiền hoặc liên kết.
DISABLE TRIGGER dbo.TR_PhieuChi_LichSu ON dbo.PHIEUTHUCHI;
UPDATE p SET OriginKind=CASE WHEN EXISTS(SELECT 1 FROM dbo.THANHTOAN_CONGNO t WHERE t.MaTC=p.MaTC) THEN 'SupplierDebt'
 WHEN NOT EXISTS(SELECT 1 FROM dbo.SystemExpenseCategory s WHERE s.MaDMTC=p.MaDMTC) THEN 'Manual' ELSE 'LegacyUnknown' END FROM dbo.PHIEUTHUCHI p;
ENABLE TRIGGER dbo.TR_PhieuChi_LichSu ON dbo.PHIEUTHUCHI;
ALTER TABLE dbo.PHIEUTHUCHI ADD CONSTRAINT FK_FinancePN FOREIGN KEY(MaPN) REFERENCES dbo.PHIEUNHAP(MaPN),
 CONSTRAINT FK_FinanceBL FOREIGN KEY(MaBL) REFERENCES dbo.BANGLUONG(MaBL),
 CONSTRAINT CK_FinanceSources CHECK(MaPN IS NULL OR MaBL IS NULL),
 CONSTRAINT CK_FinanceOrigin CHECK(OriginKind IN('Manual','PurchaseImmediate','Payroll','SupplierDebt','LegacyUnknown'));
CREATE UNIQUE INDEX UX_FinancePN ON dbo.PHIEUTHUCHI(MaPN) WHERE MaPN IS NOT NULL;
CREATE UNIQUE INDEX UX_FinanceBL ON dbo.PHIEUTHUCHI(MaBL) WHERE MaBL IS NOT NULL;
DECLARE @next bigint=(SELECT COALESCE(MAX(TRY_CONVERT(bigint,SUBSTRING(MaTC,3,8))),0)+1 FROM dbo.PHIEUTHUCHI WITH(TABLOCKX,HOLDLOCK) WHERE MaTC COLLATE Latin1_General_100_BIN2 LIKE 'TC[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]');
IF @next>99999999 THROW 52607,N'Hết miền mã thu chi.',1;
DECLARE @sequenceSql nvarchar(max)=N'CREATE SEQUENCE dbo.SeqPhieuThuChi AS bigint START WITH '+CAST(@next AS varchar(20))+N' INCREMENT BY 1 MINVALUE 1 MAXVALUE 99999999 NO CYCLE NO CACHE;';
EXEC sys.sp_executesql @sequenceSql;
REVOKE EXECUTE ON dbo.sp_TraCongNo FROM CafeManagerApp;
REVOKE EXECUTE ON dbo.sp_ChotKiemKe FROM CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_FinanceNextCode @Code char(10) OUTPUT AS
BEGIN
 DECLARE @n bigint=NEXT VALUE FOR dbo.SeqPhieuThuChi;
 SET @Code='TC'+RIGHT('00000000'+CONVERT(varchar(20),@n),8);
END;
GO
-- Khóa theo request trước lookup để hai request mới không tranh range receipt.
CREATE PROCEDURE dbo.sp_FinanceBegin @Actor int,@Stamp binary(8),@Roles varchar(30),@RequestId uniqueidentifier,
 @Operation varchar(40),@Payload nvarchar(max),@Code varchar(10) OUTPUT,@Replay bit OUTPUT AS
BEGIN
 SET NOCOUNT ON;
 IF @@TRANCOUNT=0 OR @RequestId IS NULL THROW 52601,N'Thiếu transaction hoặc mã yêu cầu.',1;
 EXEC dbo.sp_G03Demand @Actor,@Stamp,@Roles;
 DECLARE @lock int,@resource nvarchar(255)=N'CafeManager:Request:'+CONVERT(nvarchar(36),@RequestId);
 EXEC @lock=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
 IF @lock<0 THROW 52603,N'Yêu cầu đang được xử lý; kiểm tra kết quả trước khi gửi lại.',1;
 DECLARE @hash binary(32)=HASHBYTES('SHA2_256',CONVERT(varbinary(max),@Payload));
 SET @Replay=0;
 IF EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId)
 BEGIN
  IF NOT EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId AND ActorAccountId=@Actor AND Operation=@Operation AND PayloadHash=@hash)
   THROW 51608,N'Mã yêu cầu đã dùng với nội dung hoặc tài khoản khác.',1;
  SELECT @Code=EntityCode FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
  SET @Replay=1;
 END;
END;
GO
CREATE PROCEDURE dbo.sp_FinanceFinish @Actor int,@RequestId uniqueidentifier,@Operation varchar(40),@Payload nvarchar(max),@Code varchar(10),@Revision binary(8)=NULL AS
BEGIN
 IF @@TRANCOUNT=0 THROW 52601,N'Thiếu transaction.',1;
 INSERT dbo.G03OperationReceipt(RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,ResultRevision)
 VALUES(@RequestId,@Actor,@Operation,@Code,HASHBYTES('SHA2_256',CONVERT(varbinary(max),@Payload)),@Revision);
END;
GO
CREATE TRIGGER dbo.TR_FinanceSource ON dbo.PHIEUTHUCHI AFTER INSERT AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.DANHMUC_THUCHI d ON d.MaDMTC=i.MaDMTC
 LEFT JOIN dbo.SystemExpenseCategory s ON s.MaDMTC=i.MaDMTC
 WHERE i.OriginKind='LegacyUnknown'
 OR (i.OriginKind='Manual' AND (s.UseCase IS NOT NULL OR i.MaPN IS NOT NULL OR i.MaBL IS NOT NULL))
 OR (i.OriginKind<>'Manual' AND (d.Loai<>N'Chi' OR s.UseCase IS NULL))
 OR (i.OriginKind='PurchaseImmediate' AND (s.UseCase<>'PurchaseImmediate' OR i.MaPN IS NULL OR i.MaBL IS NOT NULL))
 OR (i.OriginKind='Payroll' AND (s.UseCase<>'Payroll' OR i.MaBL IS NULL OR i.MaPN IS NOT NULL))
 OR (i.OriginKind='SupplierDebt' AND (s.UseCase<>'SupplierDebt' OR i.MaPN IS NOT NULL OR i.MaBL IS NOT NULL)))
 THROW 52602,N'Nguồn hoặc loại phiếu thu chi không hợp lệ.',1;
END;
GO
ALTER TRIGGER dbo.TR_PhieuChi_LichSu ON dbo.PHIEUTHUCHI AFTER UPDATE,DELETE AS
BEGIN
 IF EXISTS(SELECT 1 FROM deleted) THROW 52602,N'Phiếu đã xác nhận không được sửa hoặc xóa.',1;
END;
GO
ALTER TRIGGER dbo.TR_DMTC_LichSu ON dbo.DANHMUC_THUCHI AFTER UPDATE AS
BEGIN
 IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON i.MaDMTC=d.MaDMTC JOIN dbo.PHIEUTHUCHI p ON p.MaDMTC=i.MaDMTC WHERE i.Loai<>d.Loai)
 THROW 52602,N'Không đổi loại danh mục đã có lịch sử thu chi.',1;
END;
GO
CREATE PROCEDURE dbo.sp_FinanceSystemExpense @UseCase varchar(20),@Employee char(5),@Amount decimal(18,0),@Date datetime2(0),
 @MaPN char(10)=NULL,@MaBL int=NULL,@MaCN char(10)=NULL,@Code char(10) OUTPUT AS
BEGIN
 SET NOCOUNT ON;
 IF @@TRANCOUNT=0 OR @Amount<=0 OR @Date IS NULL THROW 52601,N'Chi hệ thống cần transaction và số tiền dương.',1;
 DECLARE @category char(5),@kind varchar(20);
 SELECT @category=s.MaDMTC FROM dbo.SystemExpenseCategory s WITH(HOLDLOCK) JOIN dbo.DANHMUC_THUCHI d WITH(HOLDLOCK) ON d.MaDMTC=s.MaDMTC WHERE s.UseCase=@UseCase AND d.Loai=N'Chi';
 IF @category IS NULL THROW 52602,N'Thiếu danh mục chi hệ thống đúng loại.',1;
 IF @UseCase='PurchaseImmediate' AND @MaPN IS NOT NULL AND @MaBL IS NULL AND @MaCN IS NULL SET @kind='PurchaseImmediate';
 IF @UseCase='Payroll' AND @MaBL IS NOT NULL AND @MaPN IS NULL AND @MaCN IS NULL SET @kind='Payroll';
 IF @UseCase='SupplierDebt' AND @MaCN IS NOT NULL AND @MaPN IS NULL AND @MaBL IS NULL AND EXISTS(SELECT 1 FROM dbo.CONGNO_NCC WHERE MaCN=@MaCN) SET @kind='SupplierDebt';
 IF @kind IS NULL THROW 52602,N'Nguồn chi không hợp lệ.',1;
 EXEC dbo.sp_FinanceNextCode @Code OUTPUT;
 INSERT dbo.PHIEUTHUCHI(MaTC,NgayLap,MaDMTC,MaNV,SoTien,GhiChu,MaPN,MaBL,OriginKind)
 VALUES(@Code,@Date,@category,@Employee,@Amount,CONVERT(nvarchar(200),@UseCase),@MaPN,@MaBL,@kind);
END;
GO
CREATE PROCEDURE dbo.sp_FinanceCreate @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Date datetime2(0),
 @Category varchar(max),@Amount decimal(38,18),@Note nvarchar(max)=NULL,@Asset varchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52601,N'Không gọi trong transaction ngoài.',1;
 SET @Category=LTRIM(RTRIM(@Category)); SET @Asset=NULLIF(LTRIM(RTRIM(@Asset)),''); SET @Note=NULLIF(LTRIM(RTRIM(@Note)),N'');
 IF @Date IS NULL OR @Category IS NULL OR @Amount IS NULL OR @Amount<=0 OR @Amount>999999999999999999 OR @Amount<>ROUND(@Amount,0) OR LEN(@Category)<>5 OR DATALENGTH(@Note)>400 OR LEN(@Asset)>5
 THROW 52601,N'Ngày, số tiền nguyên VND, danh mục hoặc ghi chú không hợp lệ.',1;
 BEGIN TRY
  BEGIN TRAN;
  DECLARE @payload nvarchar(max)=(SELECT @Date d,@Category c,@Amount a,@Note n,@Asset s FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@code varchar(10),@replay bit;
  EXEC dbo.sp_FinanceBegin @Actor,@Stamp,'ADMIN',@RequestId,'FinanceCreate',@payload,@code OUTPUT,@replay OUTPUT;
  IF @replay=0
  BEGIN
   IF NOT EXISTS(SELECT 1 FROM dbo.DANHMUC_THUCHI WITH(HOLDLOCK) WHERE MaDMTC=@Category)
    OR EXISTS(SELECT 1 FROM dbo.SystemExpenseCategory WITH(HOLDLOCK) WHERE MaDMTC=@Category)
    OR (@Asset IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.TAISAN_CODINH WITH(HOLDLOCK) WHERE MaTS=@Asset)) THROW 52602,N'Danh mục ghi tay hoặc tài sản không hợp lệ.',1;
   DECLARE @new char(10),@employee char(5)=(SELECT MaNV FROM dbo.TAIKHOAN WHERE MaTK=@Actor);
   EXEC dbo.sp_FinanceNextCode @new OUTPUT; SET @code=@new;
   INSERT dbo.PHIEUTHUCHI(MaTC,NgayLap,MaDMTC,MaNV,MaTS,SoTien,GhiChu,OriginKind) VALUES(@code,@Date,@Category,@employee,@Asset,CONVERT(decimal(18,0),@Amount),@Note,'Manual');
   EXEC dbo.sp_FinanceFinish @Actor,@RequestId,'FinanceCreate',@payload,@code;
  END;
  COMMIT; SELECT @code EntityCode;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_FinanceCategories @Actor int,@Stamp binary(8) AS
BEGIN
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 SELECT MaDMTC Code,TenDMTC Name,Loai Type FROM dbo.DANHMUC_THUCHI d WHERE NOT EXISTS(SELECT 1 FROM dbo.SystemExpenseCategory s WHERE s.MaDMTC=d.MaDMTC) ORDER BY Loai,MaDMTC;
END;
GO
CREATE PROCEDURE dbo.sp_FinanceList @Actor int,@Stamp binary(8),@From date,@To date,@Search nvarchar(max)=N'',@Type nvarchar(10)=NULL,@Origin varchar(20)=NULL,@Skip int=0 AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 IF @From IS NULL OR @To IS NULL OR @From>@To OR @Skip<0 OR LEN(@Search)>100 THROW 52601,N'Bộ lọc không hợp lệ.',1;
 DECLARE @until datetime2(0)=NULL; IF @To<'99991231' SET @until=DATEADD(day,1,CONVERT(datetime2(0),@To));
 SELECT p.MaTC Code,p.NgayLap Date,d.TenDMTC Category,d.Loai Type,p.SoTien Amount,p.GhiChu Note,p.OriginKind Origin,p.MaPN,p.MaBL,p.MaTS,
 (SELECT TOP(1) t.MaCN FROM dbo.THANHTOAN_CONGNO t WHERE t.MaTC=p.MaTC) DebtCode,
 SUM(CASE WHEN d.Loai=N'Thu' THEN p.SoTien ELSE CONVERT(decimal(18,0),0) END) OVER() TotalIncome,
 SUM(CASE WHEN d.Loai=N'Chi' THEN p.SoTien ELSE CONVERT(decimal(18,0),0) END) OVER() TotalExpense,COUNT(*) OVER() TotalRows
 FROM dbo.PHIEUTHUCHI p JOIN dbo.DANHMUC_THUCHI d ON d.MaDMTC=p.MaDMTC
 WHERE p.NgayLap>=@From AND (@until IS NULL OR p.NgayLap<@until) AND (@Type IS NULL OR d.Loai=@Type) AND (@Origin IS NULL OR p.OriginKind=@Origin)
 AND (p.MaTC LIKE N'%'+@Search+N'%' OR p.GhiChu LIKE N'%'+@Search+N'%') ORDER BY p.NgayLap DESC,p.MaTC DESC OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_FinanceGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 SELECT p.MaTC Code,p.NgayLap Date,d.TenDMTC Category,d.Loai Type,p.SoTien Amount,p.GhiChu Note,p.OriginKind Origin,p.MaPN,p.MaBL,p.MaTS,
 (SELECT TOP(1) t.MaCN FROM dbo.THANHTOAN_CONGNO t WHERE t.MaTC=p.MaTC) DebtCode,
 CONVERT(decimal(18,0),0) TotalIncome,CONVERT(decimal(18,0),0) TotalExpense,1 TotalRows
 FROM dbo.PHIEUTHUCHI p JOIN dbo.DANHMUC_THUCHI d ON d.MaDMTC=p.MaDMTC WHERE p.MaTC=@Code;
END;
GO
CREATE PROCEDURE dbo.sp_FinanceReceipt @Actor int,@Stamp binary(8),@RequestId uniqueidentifier AS
BEGIN
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 SELECT EntityCode FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId AND ActorAccountId=@Actor AND Operation='FinanceCreate';
END;
GO
GRANT EXECUTE ON dbo.sp_FinanceList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_FinanceGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_FinanceCategories TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_FinanceCreate TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_FinanceReceipt TO CafeManagerApp;

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
        ELSE THROW 51301,'Unsupported code source.',1;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER()=11728 THROW 51302,'Code range exhausted; contact administrator.',1;
        THROW;
    END CATCH
END;
GO
