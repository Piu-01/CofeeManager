-- T12. Apply after006 through migration runner.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=6) THROW 51907,N'Migration006 bắt buộc.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.HANGTHANHVIEN WITH(TABLOCKX,HOLDLOCK)) INSERT dbo.HANGTHANHVIEN(MaHang,DiemToiThieu,PhanTramGiam) VALUES('DONG',0,0);
IF NOT EXISTS(SELECT 1 FROM dbo.HANGTHANHVIEN WHERE DiemToiThieu=0) THROW 51907,N'Chính sách cũ thiếu hạng ngưỡng0; cần duyệt xử lý dữ liệu.',1;
IF EXISTS(SELECT 1 FROM dbo.KHACHHANG k WITH(TABLOCKX,HOLDLOCK) WHERE NULLIF(LTRIM(RTRIM(k.TenKH)),N'') IS NULL
 OR k.MaHang<>(SELECT TOP(1) MaHang FROM dbo.HANGTHANHVIEN WHERE DiemToiThieu<=k.DiemTichLuy ORDER BY DiemToiThieu DESC)) THROW 51907,N'Khách cũ có tên/hạng sai; cần duyệt xử lý dữ liệu.',1;
ALTER TABLE dbo.KHACHHANG ADD Revision rowversion;
CREATE TABLE dbo.CustomerOpening(MaKH char(5) NOT NULL PRIMARY KEY REFERENCES dbo.KHACHHANG(MaKH),Points int NOT NULL CHECK(Points>=0),RecordedAt datetime2(7) NOT NULL DEFAULT SYSDATETIME(),Source nvarchar(100) NOT NULL,Kind varchar(20) NOT NULL CHECK(Kind IN('Manual','LegacyCutover')),ActorAccountId int NULL REFERENCES dbo.TAIKHOAN(MaTK),RequestId uniqueidentifier NULL);
CREATE UNIQUE INDEX UX_CustomerOpening_Request ON dbo.CustomerOpening(RequestId) WHERE RequestId IS NOT NULL;
INSERT dbo.CustomerOpening(MaKH,Points,Source,Kind) SELECT MaKH,DiemTichLuy,N'Migration007: điểm tại thời điểm chuyển đổi','LegacyCutover' FROM dbo.KHACHHANG;
DECLARE @next bigint=(SELECT COALESCE(MAX(TRY_CONVERT(int,SUBSTRING(MaKH,3,3))),0)+1 FROM dbo.KHACHHANG WHERE MaKH COLLATE Latin1_General_100_BIN2 LIKE 'KH[0-9][0-9][0-9]');
IF @next>999 THROW 51907,N'Hết miền cấp mã khách KH001–KH999.',1;
DECLARE @sequence nvarchar(max)=N'CREATE SEQUENCE dbo.SeqKhachHang AS int START WITH '+CONVERT(nvarchar(20),@next)+N' INCREMENT BY 1 MINVALUE 1 MAXVALUE 999 NO CYCLE NO CACHE;';EXEC sys.sp_executesql @sequence;
DENY INSERT,UPDATE,DELETE ON dbo.KHACHHANG TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.CustomerOpening TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_CustomerList @Actor int,@Stamp binary(8),@Search nvarchar(max)=N'',@Skip int=0 AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 IF LEN(@Search)>100 OR @Skip<0 THROW 51901,N'Bộ lọc không hợp lệ.',1;
 DECLARE @exact nvarchar(max)=@Search;
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 SELECT k.MaKH Code,k.TenKH Name,k.SDT Phone,k.DiemTichLuy Points,k.MaHang Tier,h.PhanTramGiam Discount,k.Revision,SYSDATETIME() ReadAt
 FROM dbo.KHACHHANG k JOIN dbo.HANGTHANHVIEN h ON h.MaHang=k.MaHang
 WHERE k.SDT LIKE @Search+N'%' ESCAPE N'~' OR k.TenKH LIKE N'%'+@Search+N'%' ESCAPE N'~' OR k.MaKH LIKE N'%'+@Search+N'%' ESCAPE N'~'
 ORDER BY CASE WHEN k.SDT=@exact THEN 0 ELSE 1 END,k.MaKH OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_CustomerGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 SELECT k.MaKH Code,k.TenKH Name,k.SDT Phone,k.DiemTichLuy Points,k.MaHang Tier,h.PhanTramGiam Discount,k.Revision,SYSDATETIME() ReadAt
 FROM dbo.KHACHHANG k JOIN dbo.HANGTHANHVIEN h ON h.MaHang=k.MaHang WHERE k.MaKH=@Code;
END;
GO
CREATE PROCEDURE dbo.sp_CustomerOrders @Actor int,@Stamp binary(8),@Code varchar(max),@Skip int=0 AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 IF @Skip<0 THROW 51901,N'Trang không hợp lệ.',1;
 SELECT MaHD,NgayLap,TrangThai FROM dbo.HOADON WHERE MaKH=@Code ORDER BY NgayLap DESC,MaHD OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_CustomerWrite @Actor int,@Stamp binary(8),@Action varchar(10),@RequestId uniqueidentifier,@Code varchar(max)=NULL,
 @ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Phone varchar(max)=NULL,@Points bigint=NULL,@Source nvarchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN;
  IF @Action='Create' EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH'; ELSE EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
  IF @Action NOT IN('Create','Update','Delete','Open') THROW 51901,N'Thao tác không hợp lệ.',1;
  SET @Name=LTRIM(RTRIM(@Name));SET @Phone=NULLIF(LTRIM(RTRIM(@Phone)),'');SET @Source=LTRIM(RTRIM(@Source));
  IF @Action<>'Create' AND (@Code IS NULL OR DATALENGTH(@Code)>5) THROW 51901,N'Mã khách không hợp lệ.',1;
  IF @Action IN('Create','Update') AND (@Name IS NULL OR LEN(@Name)=0 OR DATALENGTH(@Name)>100 OR @Name LIKE N'%'+NCHAR(9)+N'%' OR @Name LIKE N'%'+NCHAR(10)+N'%' OR @Name LIKE N'%'+NCHAR(13)+N'%' OR (@Phone IS NOT NULL AND (DATALENGTH(@Phone)>15 OR @Phone COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9]%'))) THROW 51901,N'Tên/SDT không hợp lệ.',1;
  IF @Action='Open' AND (@Points IS NULL OR @Points<0 OR @Points>2147483647 OR @Source IS NULL OR LEN(@Source)=0 OR DATALENGTH(@Source)>200) THROW 51901,N'Điểm/nguồn mở sổ không hợp lệ.',1;
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@Name n,@Phone p,@Points pts,@Source s,@ExpectedRevision r FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
  DECLARE @hash binary(32)=HASHBYTES('SHA2_256',CONVERT(varbinary(max),@payload)),@resultCode varchar(10)=@Code,@revision binary(8),@replay bit,@operation varchar(40)='Customer'+@Action,@lock int,@tier char(5);
  EXEC dbo.sp_G03BeginReceipt @Actor,@operation,@RequestId,@hash,@resultCode OUTPUT,@revision OUTPUT,@replay OUTPUT;
  IF @replay=0
  BEGIN
   IF @Action IN('Create','Open')
   BEGIN
    EXEC @lock=sys.sp_getapplock @Resource=N'CafeManager:LoyaltyPolicy',@LockMode='Shared',@LockOwner='Transaction',@LockTimeout=10000;
    IF @lock<0 THROW 51904,N'Chính sách đang thay đổi; đối chiếu trước thử lại.',1;
   END;
   IF @Action='Create'
   BEGIN
    DECLARE @number int=NEXT VALUE FOR dbo.SeqKhachHang;
    SET @Code='KH'+RIGHT('000'+CONVERT(varchar(10),@number),3);SET @resultCode=@Code;
    SELECT TOP(1) @tier=MaHang FROM dbo.HANGTHANHVIEN WHERE DiemToiThieu<=0 ORDER BY DiemToiThieu DESC;
    IF @tier IS NULL THROW 51907,N'Thiếu hạng ngưỡng0.',1;
    INSERT dbo.KHACHHANG(MaKH,TenKH,SDT,DiemTichLuy,MaHang) VALUES(@Code,@Name,@Phone,0,@tier);
   END
   ELSE
   BEGIN
    IF NOT EXISTS(SELECT 1 FROM dbo.KHACHHANG WITH(UPDLOCK,HOLDLOCK) WHERE MaKH=@Code AND Revision=@ExpectedRevision) THROW 51603,N'Khách đã thay đổi; tải lại.',1;
    IF @Action='Update' UPDATE dbo.KHACHHANG SET TenKH=@Name,SDT=@Phone WHERE MaKH=@Code;
    ELSE IF @Action='Delete' DELETE dbo.KHACHHANG WHERE MaKH=@Code;
    ELSE
    BEGIN
     IF EXISTS(SELECT 1 FROM dbo.CustomerOpening WITH(UPDLOCK,HOLDLOCK) WHERE MaKH=@Code) OR EXISTS(SELECT 1 FROM dbo.LICHSU_DIEM WITH(UPDLOCK,HOLDLOCK) WHERE MaKH=@Code) OR EXISTS(SELECT 1 FROM dbo.HOADON WITH(UPDLOCK,HOLDLOCK) WHERE MaKH=@Code) THROW 51902,N'Khách đã mở sổ hoặc phát sinh; không thể mở sổ lại.',1;
     SELECT TOP(1) @tier=MaHang FROM dbo.HANGTHANHVIEN WHERE DiemToiThieu<=@Points ORDER BY DiemToiThieu DESC;
     IF @tier IS NULL THROW 51907,N'Không tìm thấy hạng phù hợp.',1;
     INSERT dbo.CustomerOpening(MaKH,Points,Source,Kind,ActorAccountId,RequestId) VALUES(@Code,CONVERT(int,@Points),@Source,'Manual',@Actor,@RequestId);
     UPDATE dbo.KHACHHANG SET DiemTichLuy=CONVERT(int,@Points),MaHang=@tier WHERE MaKH=@Code;
    END;
   END;
   SELECT @revision=Revision FROM dbo.KHACHHANG WHERE MaKH=@Code;
   INSERT dbo.G03OperationReceipt(RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,ResultRevision) VALUES(@RequestId,@Actor,@operation,@Code,@hash,@revision);
  END;
  COMMIT; SELECT @resultCode Code,@revision Revision;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; IF ERROR_NUMBER()=11728 THROW 51302,N'Hết mã KH001–KH999.',1; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_CustomerCreate @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Code varchar(max)=NULL,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Phone varchar(max)=NULL,@Points bigint=NULL,@Source nvarchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_CustomerWrite @Actor,@Stamp,'Create',@RequestId,@Code,@ExpectedRevision,@Name,@Phone,@Points,@Source; END;
GO
GRANT EXECUTE ON dbo.sp_CustomerCreate TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_CustomerUpdate @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Code varchar(max)=NULL,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Phone varchar(max)=NULL,@Points bigint=NULL,@Source nvarchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_CustomerWrite @Actor,@Stamp,'Update',@RequestId,@Code,@ExpectedRevision,@Name,@Phone,@Points,@Source; END;
GO
GRANT EXECUTE ON dbo.sp_CustomerUpdate TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_CustomerDelete @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Code varchar(max)=NULL,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Phone varchar(max)=NULL,@Points bigint=NULL,@Source nvarchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_CustomerWrite @Actor,@Stamp,'Delete',@RequestId,@Code,@ExpectedRevision,@Name,@Phone,@Points,@Source; END;
GO
GRANT EXECUTE ON dbo.sp_CustomerDelete TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_CustomerOpen @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Code varchar(max)=NULL,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Phone varchar(max)=NULL,@Points bigint=NULL,@Source nvarchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_CustomerWrite @Actor,@Stamp,'Open',@RequestId,@Code,@ExpectedRevision,@Name,@Phone,@Points,@Source; END;
GO
GRANT EXECUTE ON dbo.sp_CustomerOpen TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_CustomerReconcile @Actor int,@Stamp binary(8),@RequestId uniqueidentifier AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_G03Receipt @Actor,@Stamp,@RequestId; END;
GO
GRANT EXECUTE ON dbo.sp_CustomerList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_CustomerGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_CustomerOrders TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_CustomerReconcile TO CafeManagerApp;
