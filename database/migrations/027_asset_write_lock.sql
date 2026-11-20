-- G07-B01: cùng thứ tự khóa theo MaTS trước tài sản/phiếu, không thay contract hoặc retry.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=26)
 THROW 53407,N'Cần schema026 và transaction migration.',1;
GO
CREATE PROCEDURE dbo.sp_AssetLock @Code varchar(max) AS
BEGIN
 SET NOCOUNT ON;
 IF @@TRANCOUNT=0 OR @Code IS NULL OR LEN(LTRIM(RTRIM(@Code)))<>5
  THROW 53401,N'Khóa tài sản cần mã và transaction.',1;
 -- Resource applock phân biệt hoa/thường; chuẩn hóa mã để dùng cùng khóa như PK.
 DECLARE @result int,@resource nvarchar(255)=N'CafeManager:Asset:'+UPPER(CONVERT(nvarchar(5),LTRIM(RTRIM(@Code))));
 EXEC @result=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
 IF @result<0 THROW 52603,N'Tài sản đang xử lý; đối chiếu kết quả trước khi gửi lại.',1;
END;
GO
ALTER PROCEDURE dbo.sp_AssetWrite @Actor int,@Stamp binary(8),@Action varchar(10),@RequestId uniqueidentifier,
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
   EXEC dbo.sp_AssetLock @result;
   INSERT dbo.TAISAN_CODINH(MaTS,TenTS,NgayMua,GiaTri,SoThangKH) VALUES(@result,@Name,@PurchasedOn,CONVERT(decimal(18,0),@Value),@Months);
  END
  ELSE
  BEGIN
   EXEC dbo.sp_AssetLock @Code;
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
ALTER PROCEDURE dbo.sp_FinanceCreate @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Date datetime2(0),
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
   IF @Asset IS NOT NULL EXEC dbo.sp_AssetLock @Asset;
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
