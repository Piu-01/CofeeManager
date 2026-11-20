-- T27: hồ sơ tài sản, không tự ghi tiền chi hoặc lịch sử khấu hao.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=24) THROW 53407,N'Cần schema024 và runner migration.',1;
IF EXISTS(SELECT 1 FROM dbo.TAISAN_CODINH WITH(TABLOCKX,HOLDLOCK) WHERE NULLIF(LTRIM(RTRIM(TenTS)),N'') IS NULL OR GiaTri<=0 OR SoThangKH<=0)
 THROW 53407,N'Hồ sơ tài sản cũ cần đối soát.',1;
ALTER TABLE dbo.TAISAN_CODINH ADD Revision rowversion;
DECLARE @next int=(SELECT COALESCE(MAX(TRY_CONVERT(int,SUBSTRING(MaTS,3,3))),0)+1 FROM dbo.TAISAN_CODINH WHERE MaTS COLLATE Latin1_General_100_BIN2 LIKE 'TS[0-9][0-9][0-9]');
IF @next>999 THROW 53407,N'Hết miền mã tài sản.',1;
DECLARE @sql nvarchar(max)=N'CREATE SEQUENCE dbo.SeqTaiSan AS int START WITH '+CONVERT(varchar(10),@next)+N' INCREMENT BY 1 MINVALUE 1 MAXVALUE 999 NO CYCLE NO CACHE;'; EXEC sys.sp_executesql @sql;
DENY INSERT,UPDATE,DELETE ON dbo.TAISAN_CODINH TO CafeManagerApp;
GO
CREATE FUNCTION dbo.fn_AssetDepreciation(@Value decimal(18,0),@Months int,@PurchasedOn date,@AsOf date)
RETURNS TABLE AS RETURN
 SELECT CONVERT(decimal(18,0),CASE WHEN k.Elapsed>=@Months THEN @Value ELSE b.BaseValue*k.Elapsed END) Accumulated,
 CONVERT(decimal(18,0),CASE WHEN k.Elapsed=0 OR k.Elapsed>@Months THEN 0 WHEN k.Elapsed=@Months THEN @Value-b.BaseValue*(@Months-1) ELSE b.BaseValue END) Monthly
 FROM (SELECT CASE WHEN @AsOf<DATEFROMPARTS(YEAR(@PurchasedOn),MONTH(@PurchasedOn),1) THEN CONVERT(bigint,0)
 ELSE CONVERT(bigint,DATEDIFF(month,@PurchasedOn,@AsOf))+1 END Elapsed) k
 CROSS APPLY(SELECT FLOOR(@Value/CONVERT(decimal(18,0),NULLIF(@Months,0))) BaseValue) b;
GO
CREATE PROCEDURE dbo.sp_AssetList @Actor int,@Stamp binary(8),@Search nvarchar(max)=N'',@Skip int=0 AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 IF @Search IS NULL OR LEN(@Search)>100 OR @Skip<0 THROW 53401,N'Bộ lọc không hợp lệ.',1;
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 SELECT MaTS Code,TenTS Name,NgayMua PurchasedOn,GiaTri Value,SoThangKH Months,Revision INTO #rows FROM dbo.TAISAN_CODINH
 WHERE MaTS LIKE N'%'+@Search+N'%' ESCAPE N'~' OR TenTS LIKE N'%'+@Search+N'%' ESCAPE N'~';
 SELECT COUNT(*) TotalCount,SYSDATETIME() ReadAt FROM #rows;
 SELECT * FROM #rows ORDER BY Code OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_AssetGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 SELECT MaTS Code,TenTS Name,NgayMua PurchasedOn,GiaTri Value,SoThangKH Months,Revision FROM dbo.TAISAN_CODINH WHERE MaTS=@Code;
END;
GO
CREATE PROCEDURE dbo.sp_AssetWrite @Actor int,@Stamp binary(8),@Action varchar(10),@RequestId uniqueidentifier,
 @Code varchar(max)=NULL,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@PurchasedOn date=NULL,@Value decimal(38,18)=NULL,@Months int=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 OR @Action IS NULL OR @Action NOT IN('Create','Update','Delete') THROW 53401,N'Thao tác không hợp lệ.',1;
 SET @Name=LTRIM(RTRIM(@Name)); SET @Code=LTRIM(RTRIM(@Code));
 IF @Action<>'Create' AND (@Code IS NULL OR LEN(@Code)<>5 OR @ExpectedRevision IS NULL) THROW 53401,N'Thiếu mã hoặc revision.',1;
 IF @Action IN('Create','Update') AND (@Name IS NULL OR LEN(@Name)=0 OR DATALENGTH(@Name)>200 OR @PurchasedOn IS NULL OR @Months IS NULL OR @Months<=0
 OR @Value IS NULL OR @Value<=0 OR @Value>999999999999999999 OR @Value<>ROUND(@Value,0)
 OR @Name LIKE N'%'+NCHAR(9)+N'%' OR @Name LIKE N'%'+NCHAR(10)+N'%' OR @Name LIKE N'%'+NCHAR(13)+N'%') THROW 53401,N'Thông tin tài sản không hợp lệ.',1;
 BEGIN TRY BEGIN TRAN;
 DECLARE @payload nvarchar(max)=(SELECT @Action a,@Code c,@ExpectedRevision r,@Name n,@PurchasedOn d,@Value v,@Months m FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),
 @operation varchar(40)='Asset'+@Action,@result varchar(10),@replay bit,@revision binary(8);
 EXEC dbo.sp_FinanceBegin @Actor,@Stamp,'ADMIN',@RequestId,@operation,@payload,@result OUTPUT,@replay OUTPUT;
 IF @replay=0
 BEGIN
  IF @Action='Create'
  BEGIN
   DECLARE @number int=NEXT VALUE FOR dbo.SeqTaiSan; SET @result='TS'+RIGHT('000'+CONVERT(varchar(10),@number),3);
   INSERT dbo.TAISAN_CODINH(MaTS,TenTS,NgayMua,GiaTri,SoThangKH) VALUES(@result,@Name,@PurchasedOn,CONVERT(decimal(18,0),@Value),@Months);
  END
  ELSE
  BEGIN
   SELECT @revision=Revision FROM dbo.TAISAN_CODINH WITH(UPDLOCK,HOLDLOCK) WHERE MaTS=@Code;
   IF @revision IS NULL OR @revision<>@ExpectedRevision THROW 53403,N'Tài sản đã thay đổi hoặc không còn tồn tại; tải lại.',1;
   SET @result=@Code;
   IF @Action='Update' UPDATE dbo.TAISAN_CODINH SET TenTS=@Name,NgayMua=@PurchasedOn,GiaTri=CONVERT(decimal(18,0),@Value),SoThangKH=@Months WHERE MaTS=@Code;
   ELSE BEGIN
    IF EXISTS(SELECT 1 FROM dbo.PHIEUTHUCHI WITH(HOLDLOCK) WHERE MaTS=@Code) THROW 53402,N'Tài sản đã có phiếu thu chi, không được xóa.',1;
    DELETE dbo.TAISAN_CODINH WHERE MaTS=@Code;
   END;
  END;
  IF @Action<>'Delete' SELECT @revision=Revision FROM dbo.TAISAN_CODINH WHERE MaTS=@result;
  EXEC dbo.sp_FinanceFinish @Actor,@RequestId,@operation,@payload,@result,@revision;
 END;
 COMMIT; SELECT EntityCode Code,Operation,ResultRevision Revision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; IF ERROR_NUMBER()=11728 THROW 53405,N'Đã hết miền mã tài sản TS001–TS999.',1; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_AssetReceipt @Actor int,@Stamp binary(8),@RequestId uniqueidentifier AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 OR @RequestId IS NULL THROW 53401,N'Yêu cầu đối chiếu không hợp lệ.',1;
 BEGIN TRY BEGIN TRAN;
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 DECLARE @lock int,@resource nvarchar(255)=N'CafeManager:Request:'+CONVERT(nvarchar(36),@RequestId);
 EXEC @lock=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
 IF @lock<0 THROW 52603,N'Yêu cầu còn xử lý; chưa thể xác định kết quả.',1;
 IF EXISTS(SELECT 1 FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId AND (ActorAccountId<>@Actor OR Operation NOT IN('AssetCreate','AssetUpdate','AssetDelete')))
  THROW 51608,N'Yêu cầu thuộc tài khoản hoặc nghiệp vụ khác.',1;
 SELECT EntityCode Code,Operation,ResultRevision Revision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId AND ActorAccountId=@Actor AND Operation IN('AssetCreate','AssetUpdate','AssetDelete');
 COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_AssetExpenses @Actor int,@Stamp binary(8),@Code varchar(max),@From date,@To date,@Skip int=0 AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 IF @From IS NULL OR @To IS NULL OR @From>@To OR @Skip<0 THROW 53401,N'Bộ lọc chi không hợp lệ.',1;
 DECLARE @until datetime2(0)=NULL; IF @To<'99991231' SET @until=DATEADD(day,1,CONVERT(datetime2(0),@To));
 SELECT p.MaTC Code,p.NgayLap Date,d.TenDMTC Category,p.SoTien Amount,p.GhiChu Note INTO #rows
 FROM dbo.PHIEUTHUCHI p JOIN dbo.DANHMUC_THUCHI d ON d.MaDMTC=p.MaDMTC
 WHERE p.MaTS=@Code AND d.Loai=N'Chi' AND p.NgayLap>=@From AND (@until IS NULL OR p.NgayLap<@until);
 SELECT COUNT(*) TotalCount,COALESCE(SUM(CONVERT(decimal(38,0),Amount)),0) TotalAmount,SYSDATETIME() ReadAt FROM #rows;
 SELECT * FROM #rows ORDER BY Date DESC,Code DESC OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
GRANT EXECUTE ON dbo.sp_AssetList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_AssetGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_AssetWrite TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_AssetReceipt TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_AssetExpenses TO CafeManagerApp;


