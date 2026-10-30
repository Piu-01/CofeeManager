-- T21: xuất nguyên tử, khóa NVL theo cùng thứ tự với checkout.
SET NOCOUNT ON;SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=18) THROW 52807,N'Cần schema018.',1;
IF EXISTS(SELECT 1 FROM dbo.PHIEUXUAT p WHERE LoaiXuat NOT IN(N'Huy do hong',N'Tra hang') OR NOT EXISTS(SELECT 1 FROM dbo.CHITIET_PX d WHERE d.MaPX=p.MaPX)) THROW 52807,N'Phiếu xuất cũ không hợp lệ.',1;
ALTER TABLE dbo.PHIEUXUAT ADD DaChot bit NOT NULL CONSTRAINT DF_IssueClosed DEFAULT 1,Revision rowversion,
 CutoverKind varchar(10) NOT NULL CONSTRAINT DF_IssueCutover DEFAULT 'Legacy',NgayChot datetime2(0) NULL,LyDo nvarchar(200) NULL;
DECLARE @next bigint=(SELECT COALESCE(MAX(TRY_CONVERT(bigint,SUBSTRING(MaPX,3,8))),0)+1 FROM dbo.PHIEUXUAT WHERE MaPX COLLATE Latin1_General_100_BIN2 LIKE 'PX[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]');
IF @next>99999999 THROW 52807,N'Hết miền mã xuất.',1;
DECLARE @sql nvarchar(max)=N'CREATE SEQUENCE dbo.SeqPhieuXuat AS bigint START WITH '+CONVERT(varchar(20),@next)+N' INCREMENT BY 1 MINVALUE 1 MAXVALUE 99999999 NO CYCLE NO CACHE;';EXEC sys.sp_executesql @sql;
DENY INSERT,UPDATE,DELETE ON dbo.PHIEUXUAT TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.CHITIET_PX TO CafeManagerApp;
GO
CREATE TYPE dbo.StockIssueLinesInput AS TABLE(MaNVL varchar(100),Quantity decimal(38,18));
GO
CREATE TRIGGER dbo.TR_IssueClosed ON dbo.PHIEUXUAT AFTER UPDATE,DELETE AS
BEGIN IF EXISTS(SELECT 1 FROM deleted WHERE DaChot=1) THROW 52802,N'Phiếu xuất đã chốt bất biến.',1; END;
GO
CREATE TRIGGER dbo.TR_IssueLinesClosed ON dbo.CHITIET_PX AFTER INSERT,UPDATE,DELETE AS
BEGIN IF EXISTS(SELECT 1 FROM dbo.PHIEUXUAT p WHERE p.DaChot=1 AND p.MaPX IN(SELECT MaPX FROM inserted UNION SELECT MaPX FROM deleted)) THROW 52802,N'Chi tiết xuất đã chốt chỉ đọc.',1; END;
GO
CREATE PROCEDURE dbo.sp_LapPhieuXuat @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Date datetime2(0),@Kind nvarchar(max),@Reason nvarchar(max),@Lines dbo.StockIssueLinesInput READONLY AS
BEGIN
 SET NOCOUNT ON;SET XACT_ABORT ON;
 SET @Reason=NULLIF(LTRIM(RTRIM(@Reason)),N'');
 IF @@TRANCOUNT<>0 OR @Date IS NULL OR @Kind IS NULL OR @Kind NOT IN(N'Huy do hong',N'Tra hang') OR @Reason IS NULL OR DATALENGTH(@Reason)>400 THROW 52801,N'Loại xuất, ngày hoặc lý do không hợp lệ.',1;
 BEGIN TRY BEGIN TRAN;
 DECLARE @json nvarchar(max)=(SELECT MaNVL,Quantity FROM @Lines ORDER BY MaNVL FOR JSON PATH,INCLUDE_NULL_VALUES),@payload nvarchar(max),@code varchar(10),@replay bit;
 SET @payload=(SELECT @Date d,@Kind k,@Reason r,@json l FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
 EXEC dbo.sp_FinanceBegin @Actor,@Stamp,'ADMIN,STORE',@RequestId,'StockIssueCreate',@payload,@code OUTPUT,@replay OUTPUT;
 IF @replay=0
 BEGIN
  IF NOT EXISTS(SELECT 1 FROM @Lines) OR EXISTS(SELECT 1 FROM @Lines WHERE MaNVL IS NULL OR LEN(MaNVL)<>5 OR Quantity IS NULL OR Quantity<=0 OR Quantity>99999999999999.9999 OR Quantity<>ROUND(Quantity,4)) OR EXISTS(SELECT MaNVL FROM @Lines GROUP BY MaNVL HAVING COUNT(*)>1) THROW 52801,N'Dòng xuất không hợp lệ hoặc trùng.',1;
  DECLARE @material char(5),@needed decimal(38,18),@stock decimal(18,4),@employee char(5)=(SELECT MaNV FROM dbo.TAIKHOAN WHERE MaTK=@Actor),@num bigint=NEXT VALUE FOR dbo.SeqPhieuXuat;
  SET @code='PX'+RIGHT('00000000'+CONVERT(varchar(20),@num),8);
  DECLARE materials CURSOR LOCAL FAST_FORWARD FOR SELECT MaNVL,Quantity FROM @Lines ORDER BY MaNVL;
  OPEN materials;FETCH NEXT FROM materials INTO @material,@needed;
  WHILE @@FETCH_STATUS=0
  BEGIN
   SET @stock=NULL;SELECT @stock=SoLuongTon FROM dbo.NGUYENVATLIEU WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@material;
   IF @stock IS NULL OR @stock<@needed THROW 52803,N'Không đủ nguyên liệu; toàn phiếu chưa chốt.',1;
   FETCH NEXT FROM materials INTO @material,@needed;
  END;CLOSE materials;DEALLOCATE materials;
  INSERT dbo.PHIEUXUAT(MaPX,NgayXuat,MaNV,LoaiXuat,DaChot,CutoverKind,LyDo) VALUES(@code,@Date,@employee,@Kind,0,'New',@Reason);
  INSERT dbo.CHITIET_PX SELECT @code,MaNVL,CONVERT(decimal(18,4),Quantity) FROM @Lines;
  UPDATE dbo.PHIEUXUAT SET DaChot=1,NgayChot=SYSDATETIME() WHERE MaPX=@code;
  DECLARE @revision binary(8)=(SELECT Revision FROM dbo.PHIEUXUAT WHERE MaPX=@code);
  EXEC dbo.sp_FinanceFinish @Actor,@RequestId,'StockIssueCreate',@payload,@code,@revision;
 END;COMMIT;SELECT @code EntityCode;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH;
END;
GO
CREATE VIEW dbo.vw_StockIssueRead AS SELECT p.MaPX Code,p.NgayXuat Date,p.LoaiXuat Kind,p.LyDo Reason,p.CutoverKind,p.Revision,
 (SELECT d.MaNVL Code,n.TenNVL Name,n.DonViTinh Unit,d.SoLuongXuat Quantity FROM dbo.CHITIET_PX d JOIN dbo.NGUYENVATLIEU n ON n.MaNVL=d.MaNVL WHERE d.MaPX=p.MaPX ORDER BY d.MaNVL FOR JSON PATH) LinesJson FROM dbo.PHIEUXUAT p;
GO
CREATE PROCEDURE dbo.sp_StockIssueList @Actor int,@Stamp binary(8),@Search nvarchar(max)=N'',@Skip int=0 AS
BEGIN EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE';IF @Skip<0 OR LEN(@Search)>100 THROW 52801,N'Bộ lọc sai.',1; SELECT *,COUNT(*) OVER() TotalRows FROM dbo.vw_StockIssueRead WHERE Code LIKE N'%'+@Search+N'%' OR Reason LIKE N'%'+@Search+N'%' ORDER BY Date DESC,Code DESC OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;END;
GO
CREATE PROCEDURE dbo.sp_StockIssueGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE'; SELECT *,1 TotalRows FROM dbo.vw_StockIssueRead WHERE Code=@Code;END;
GO
CREATE PROCEDURE dbo.sp_StockIssueReceipt @Actor int,@Stamp binary(8),@RequestId uniqueidentifier AS
BEGIN EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE';SELECT EntityCode FROM dbo.G03OperationReceipt WHERE ActorAccountId=@Actor AND RequestId=@RequestId AND Operation='StockIssueCreate';END;
GO
GRANT EXECUTE ON dbo.sp_LapPhieuXuat TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_StockIssueList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_StockIssueGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_StockIssueReceipt TO CafeManagerApp;
GRANT EXECUTE,REFERENCES ON TYPE::dbo.StockIssueLinesInput TO CafeManagerApp;

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
        ELSE IF @Loai='PHIEUXUAT'
        BEGIN
            SET @number=NEXT VALUE FOR dbo.SeqPhieuXuat;
            SELECT @Loai AS Loai,CAST('PX'+RIGHT('00000000'+CONVERT(VARCHAR(20),@number),8) AS CHAR(10)) AS Ma;
        END
        ELSE THROW 51301,'Unsupported code source.',1;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER()=11728 THROW 51302,'Code range exhausted; contact administrator.',1;
        THROW;
    END CATCH
END;
GO
