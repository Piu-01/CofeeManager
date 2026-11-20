-- T13 compatibility: existing CHAR10 seed identifiers remain editable and immutable.
-- New promotion codes still use KM + eight digits; never rewrites historical keys.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=8) THROW 52007,N'Migration008 bắt buộc.',1;
GO
ALTER PROCEDURE dbo.sp_PromotionWrite @Actor int,@Stamp binary(8),@Action varchar(10),@Code varchar(max),@RequestId uniqueidentifier,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@StartDate date=NULL,@EndDate date=NULL,@Discount bigint=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
  IF @Action NOT IN('Create','Update','Delete') THROW 52001,N'Thao tác không hợp lệ.',1;
  SET @Name=LTRIM(RTRIM(@Name));SET @Code=UPPER(LTRIM(RTRIM(@Code)));
  IF @Code IS NULL OR DATALENGTH(@Code)=0 OR DATALENGTH(@Code)>10 OR (@Action='Create' AND (DATALENGTH(@Code)<>10 OR @Code COLLATE Latin1_General_100_BIN2 NOT LIKE 'KM[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]')) THROW 52001,N'Mã tạo mới là KM và8chữ số; mã cũ được giữ khi sửa/xóa.',1;
  IF @Action<>'Delete' AND (@Name IS NULL OR LEN(@Name)=0 OR DATALENGTH(@Name)>200 OR @Name LIKE N'%'+NCHAR(9)+N'%' OR @Name LIKE N'%'+NCHAR(10)+N'%' OR @Name LIKE N'%'+NCHAR(13)+N'%' OR @StartDate IS NULL OR @EndDate IS NULL OR @EndDate<@StartDate OR @Discount IS NULL OR @Discount<0 OR @Discount>100) THROW 52001,N'Thông tin chương trình không hợp lệ.',1;
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@Name n,@StartDate sd,@EndDate ed,@Discount d,@ExpectedRevision r FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
  DECLARE @hash binary(32)=HASHBYTES('SHA2_256',CONVERT(varbinary(max),@payload)),@resultCode varchar(10)=@Code,@revision binary(8),@replay bit,@operation varchar(40)='Promotion'+@Action;
  EXEC dbo.sp_G03BeginReceipt @Actor,@operation,@RequestId,@hash,@resultCode OUTPUT,@revision OUTPUT,@replay OUTPUT;
  IF @replay=0
  BEGIN
   IF @Action='Create' INSERT dbo.KHUYENMAI(MaKM,TenKM,NgayBD,NgayKT,PhanTramGiam) VALUES(@Code,@Name,@StartDate,@EndDate,CONVERT(int,@Discount));
   ELSE
   BEGIN
    IF NOT EXISTS(SELECT 1 FROM dbo.KHUYENMAI WITH(UPDLOCK,HOLDLOCK) WHERE MaKM=@Code AND Revision=@ExpectedRevision) THROW 51603,N'Chương trình đã thay đổi.',1;
    IF @Action='Update' UPDATE dbo.KHUYENMAI SET TenKM=@Name,NgayBD=@StartDate,NgayKT=@EndDate,PhanTramGiam=CONVERT(int,@Discount) WHERE MaKM=@Code;
    ELSE DELETE dbo.KHUYENMAI WHERE MaKM=@Code;
   END;
   SELECT @revision=Revision FROM dbo.KHUYENMAI WHERE MaKM=@Code;
   INSERT dbo.G03OperationReceipt(RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,ResultRevision) VALUES(@RequestId,@Actor,@operation,@Code,@hash,@revision);
  END;
  COMMIT; SELECT @resultCode Code,@revision Revision;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO

