-- T22: snapshot bền vững, số đếm NULL và chốt một lần.
SET NOCOUNT ON;SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=19) THROW 52907,N'Cần schema019.',1;
ALTER TABLE dbo.PHIEUKIEMKE ADD SnapshotAt datetime2(7) NULL,Revision rowversion,CutoverKind varchar(10) NOT NULL CONSTRAINT DF_StocktakeCutover DEFAULT 'Legacy';
ALTER TABLE dbo.CHITIET_KK ADD MaterialRevision binary(8) NULL;
ALTER TABLE dbo.CHITIET_KK ALTER COLUMN TonThucTe decimal(18,4) NULL;
DECLARE @next bigint=(SELECT COALESCE(MAX(TRY_CONVERT(bigint,SUBSTRING(MaKK,3,8))),0)+1 FROM dbo.PHIEUKIEMKE WHERE MaKK COLLATE Latin1_General_100_BIN2 LIKE 'KK[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]');
IF @next>99999999 THROW 52907,N'Hết miền mã kiểm kê.',1;
DECLARE @sql nvarchar(max)=N'CREATE SEQUENCE dbo.SeqKiemKe AS bigint START WITH '+CONVERT(varchar(20),@next)+N' INCREMENT BY 1 MINVALUE 1 MAXVALUE 99999999 NO CYCLE NO CACHE;';EXEC sys.sp_executesql @sql;
DENY INSERT,UPDATE,DELETE ON dbo.CHITIET_KK TO CafeManagerApp;
GO
CREATE TYPE dbo.StocktakeScope AS TABLE(MaNVL varchar(100));
GO
CREATE TYPE dbo.StocktakeCounts AS TABLE(MaNVL varchar(100),Quantity decimal(38,18) NULL,Reason nvarchar(max) NULL);
GO
CREATE TRIGGER dbo.TR_StocktakeHeader ON dbo.PHIEUKIEMKE AFTER UPDATE,DELETE AS
BEGIN
 IF EXISTS(SELECT 1 FROM deleted WHERE DaDieuChinh=1) THROW 52902,N'Phiếu kiểm kê đã chốt không sửa/xóa/reset.',1;
 IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.MaKK=i.MaKK WHERE i.NgayKK<>d.NgayKK OR i.MaNV<>d.MaNV OR ISNULL(i.SnapshotAt,'19000101')<>ISNULL(d.SnapshotAt,'19000101') OR i.CutoverKind<>d.CutoverKind) THROW 52902,N'Không đổi nguồn hoặc thời điểm snapshot.',1;
END;
GO
CREATE PROCEDURE dbo.sp_StocktakeCreate @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Scope dbo.StocktakeScope READONLY AS
BEGIN
 SET NOCOUNT ON;SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52901,N'Không gọi trong transaction ngoài.',1;
 BEGIN TRY BEGIN TRAN;
 DECLARE @json nvarchar(max)=(SELECT MaNVL FROM @Scope ORDER BY MaNVL FOR JSON PATH,INCLUDE_NULL_VALUES),@code varchar(10),@replay bit;
 EXEC dbo.sp_FinanceBegin @Actor,@Stamp,'ADMIN,STORE',@RequestId,'StocktakeCreate',@json,@code OUTPUT,@replay OUTPUT;
 IF @replay=0
 BEGIN
  IF NOT EXISTS(SELECT 1 FROM @Scope) OR EXISTS(SELECT 1 FROM @Scope WHERE MaNVL IS NULL OR LEN(MaNVL)<>5) OR EXISTS(SELECT MaNVL FROM @Scope GROUP BY MaNVL HAVING COUNT(*)>1) THROW 52901,N'Phạm vi kiểm kê không hợp lệ.',1;
  DECLARE @material char(5),@stock decimal(18,4);
  DECLARE materials CURSOR LOCAL FAST_FORWARD FOR SELECT MaNVL FROM @Scope ORDER BY MaNVL;
  OPEN materials;FETCH NEXT FROM materials INTO @material;
  WHILE @@FETCH_STATUS=0 BEGIN SET @stock=NULL;SELECT @stock=SoLuongTon FROM dbo.NGUYENVATLIEU WITH(HOLDLOCK) WHERE MaNVL=@material;IF @stock IS NULL THROW 52901,N'Nguyên liệu không tồn tại.',1;FETCH NEXT FROM materials INTO @material;END;CLOSE materials;DEALLOCATE materials;
  DECLARE @n bigint=NEXT VALUE FOR dbo.SeqKiemKe,@now datetime2(7)=SYSDATETIME(),@employee char(5)=(SELECT MaNV FROM dbo.TAIKHOAN WHERE MaTK=@Actor);
  SET @code='KK'+RIGHT('00000000'+CONVERT(varchar(20),@n),8);
  INSERT dbo.PHIEUKIEMKE(MaKK,NgayKK,MaNV,DaDieuChinh,SnapshotAt,CutoverKind) VALUES(@code,CONVERT(date,@now),@employee,0,@now,'New');
  INSERT dbo.CHITIET_KK(MaKK,MaNVL,TonHeThong,TonThucTe,LyDo,MaterialRevision) SELECT @code,n.MaNVL,n.SoLuongTon,NULL,NULL,n.Revision FROM dbo.NGUYENVATLIEU n JOIN @Scope s ON s.MaNVL=n.MaNVL;
  DECLARE @revision binary(8)=(SELECT Revision FROM dbo.PHIEUKIEMKE WHERE MaKK=@code);
  EXEC dbo.sp_FinanceFinish @Actor,@RequestId,'StocktakeCreate',@json,@code,@revision;
 END;COMMIT;SELECT @code EntityCode;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_StocktakeSaveCounts @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Code varchar(max),@ExpectedRevision binary(8),@Counts dbo.StocktakeCounts READONLY AS
BEGIN
 SET NOCOUNT ON;SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52901,N'Không gọi trong transaction ngoài.',1;
 BEGIN TRY BEGIN TRAN;
 DECLARE @json nvarchar(max)=(SELECT MaNVL,Quantity,Reason FROM @Counts ORDER BY MaNVL FOR JSON PATH,INCLUDE_NULL_VALUES),@payload nvarchar(max),@result varchar(10),@replay bit;
 SET @payload=(SELECT @Code c,@ExpectedRevision r,@json a FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
 EXEC dbo.sp_FinanceBegin @Actor,@Stamp,'ADMIN,STORE',@RequestId,'StocktakeSave',@payload,@result OUTPUT,@replay OUTPUT;
 IF @replay=0
 BEGIN
  IF NOT EXISTS(SELECT 1 FROM dbo.PHIEUKIEMKE WITH(UPDLOCK,HOLDLOCK) WHERE MaKK=@Code AND DaDieuChinh=0 AND Revision=@ExpectedRevision AND CutoverKind='New') THROW 52903,N'Phiếu đã đổi, đã chốt hoặc là dữ liệu cũ; tải lại.',1;
  IF EXISTS(SELECT 1 FROM @Counts WHERE MaNVL IS NULL OR LEN(MaNVL)<>5 OR Quantity<0 OR Quantity>99999999999999.9999 OR Quantity<>ROUND(Quantity,4) OR DATALENGTH(Reason)>200) OR EXISTS(SELECT MaNVL FROM @Counts GROUP BY MaNVL HAVING COUNT(*)>1)
  OR EXISTS(SELECT MaNVL FROM @Counts EXCEPT SELECT MaNVL FROM dbo.CHITIET_KK WHERE MaKK=@Code) OR EXISTS(SELECT MaNVL FROM dbo.CHITIET_KK WHERE MaKK=@Code EXCEPT SELECT MaNVL FROM @Counts) THROW 52901,N'Số đếm hoặc phạm vi sai.',1;
  UPDATE k SET TonThucTe=CONVERT(decimal(18,4),c.Quantity),LyDo=NULLIF(LTRIM(RTRIM(c.Reason)),N'') FROM dbo.CHITIET_KK k JOIN @Counts c ON c.MaNVL=k.MaNVL WHERE k.MaKK=@Code;
  UPDATE dbo.PHIEUKIEMKE SET DaDieuChinh=0 WHERE MaKK=@Code;
  DECLARE @revision binary(8)=(SELECT Revision FROM dbo.PHIEUKIEMKE WHERE MaKK=@Code);SET @result=@Code;
  EXEC dbo.sp_FinanceFinish @Actor,@RequestId,'StocktakeSave',@payload,@result,@revision;
 END;COMMIT;SELECT @result EntityCode;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH;
END;
GO
ALTER PROCEDURE dbo.sp_ChotKiemKe @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Code varchar(max),@ExpectedRevision binary(8) AS
BEGIN
 SET NOCOUNT ON;SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52901,N'Không gọi trong transaction ngoài.',1;
 BEGIN TRY BEGIN TRAN;
 DECLARE @payload nvarchar(max)=(SELECT @Code c,@ExpectedRevision r FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@result varchar(10),@replay bit;
 EXEC dbo.sp_FinanceBegin @Actor,@Stamp,'ADMIN,STORE',@RequestId,'StocktakeCommit',@payload,@result OUTPUT,@replay OUTPUT;
 IF @replay=0
 BEGIN
  IF NOT EXISTS(SELECT 1 FROM dbo.PHIEUKIEMKE WITH(UPDLOCK,HOLDLOCK) WHERE MaKK=@Code AND Revision=@ExpectedRevision AND DaDieuChinh=0 AND NgayKK=CONVERT(date,SYSDATETIME()) AND CutoverKind='New' AND SnapshotAt IS NOT NULL) THROW 52902,N'Chỉ chốt phiếu hôm nay, chưa chốt và đúng revision.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.CHITIET_KK WHERE MaKK=@Code) OR EXISTS(SELECT 1 FROM dbo.CHITIET_KK WHERE MaKK=@Code AND (TonThucTe IS NULL OR MaterialRevision IS NULL OR (TonThucTe<>TonHeThong AND NULLIF(LTRIM(RTRIM(LyDo)),N'') IS NULL))) THROW 52901,N'Cần đủ số đếm và lý do chênh lệch.',1;
  DECLARE @material char(5),@expected decimal(18,4),@revision binary(8),@stock decimal(18,4),@current binary(8);
  DECLARE materials CURSOR LOCAL FAST_FORWARD FOR SELECT MaNVL,TonHeThong,MaterialRevision FROM dbo.CHITIET_KK WHERE MaKK=@Code ORDER BY MaNVL;
  OPEN materials;FETCH NEXT FROM materials INTO @material,@expected,@revision;
  WHILE @@FETCH_STATUS=0
  BEGIN
   SET @current=NULL;SELECT @stock=SoLuongTon,@current=Revision FROM dbo.NGUYENVATLIEU WITH(UPDLOCK,HOLDLOCK) WHERE MaNVL=@material;
   IF @current IS NULL OR @stock<>@expected OR @current<>@revision THROW 52903,N'Kho đã thay đổi sau snapshot; lấy số liệu và xác nhận đếm lại.',1;
   FETCH NEXT FROM materials INTO @material,@expected,@revision;
  END;CLOSE materials;DEALLOCATE materials;
  UPDATE n SET SoLuongTon=k.TonThucTe FROM dbo.NGUYENVATLIEU n JOIN dbo.CHITIET_KK k ON k.MaNVL=n.MaNVL WHERE k.MaKK=@Code;
  UPDATE dbo.PHIEUKIEMKE SET DaDieuChinh=1 WHERE MaKK=@Code;
  SET @result=@Code;SELECT @revision=Revision FROM dbo.PHIEUKIEMKE WHERE MaKK=@Code;
  EXEC dbo.sp_FinanceFinish @Actor,@RequestId,'StocktakeCommit',@payload,@result,@revision;
 END;COMMIT;SELECT @result EntityCode;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH;
END;
GO
CREATE VIEW dbo.vw_StocktakeRead AS SELECT p.MaKK Code,p.NgayKK Date,p.DaDieuChinh Closed,p.SnapshotAt,p.CutoverKind,p.Revision,
 (SELECT k.MaNVL Code,n.TenNVL Name,n.DonViTinh Unit,k.TonHeThong Expected,k.TonThucTe Actual,k.LyDo Reason FROM dbo.CHITIET_KK k JOIN dbo.NGUYENVATLIEU n ON n.MaNVL=k.MaNVL WHERE k.MaKK=p.MaKK ORDER BY k.MaNVL FOR JSON PATH,INCLUDE_NULL_VALUES) LinesJson FROM dbo.PHIEUKIEMKE p;
GO
CREATE PROCEDURE dbo.sp_StocktakeList @Actor int,@Stamp binary(8),@Search nvarchar(max)=N'',@Skip int=0 AS
BEGIN EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE';IF @Skip<0 OR LEN(@Search)>100 THROW 52901,N'Bộ lọc sai.',1; SELECT *,COUNT(*) OVER() TotalRows FROM dbo.vw_StocktakeRead WHERE Code LIKE N'%'+@Search+N'%' ORDER BY Date DESC,Code DESC OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;END;
GO
CREATE PROCEDURE dbo.sp_StocktakeGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE';SELECT *,1 TotalRows FROM dbo.vw_StocktakeRead WHERE Code=@Code;END;
GO
CREATE PROCEDURE dbo.sp_StocktakeReceipt @Actor int,@Stamp binary(8),@RequestId uniqueidentifier AS
BEGIN EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE';SELECT EntityCode FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId AND ActorAccountId=@Actor AND Operation IN('StocktakeCreate','StocktakeSave','StocktakeCommit');END;
GO
GRANT EXECUTE ON dbo.sp_StocktakeCreate TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_StocktakeSaveCounts TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_ChotKiemKe TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_StocktakeList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_StocktakeGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_StocktakeReceipt TO CafeManagerApp;
GRANT EXECUTE,REFERENCES ON TYPE::dbo.StocktakeScope TO CafeManagerApp;
GRANT EXECUTE,REFERENCES ON TYPE::dbo.StocktakeCounts TO CafeManagerApp;

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
        ELSE IF @Loai='PHIEUKIEMKE'
        BEGIN
            SET @number=NEXT VALUE FOR dbo.SeqKiemKe;
            SELECT @Loai AS Loai,CAST('KK'+RIGHT('00000000'+CONVERT(VARCHAR(20),@number),8) AS CHAR(10)) AS Ma;
        END
        ELSE THROW 51301,'Unsupported code source.',1;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER()=11728 THROW 51302,'Code range exhausted; contact administrator.',1;
        THROW;
    END CATCH
END;
GO
