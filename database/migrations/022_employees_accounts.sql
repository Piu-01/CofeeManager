-- T24: quản trị nhân viên/tài khoản; không thay baseline hoặc mật khẩu cũ.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=21) THROW 53107,N'Cần schema021 và runner migration.',1;
IF EXISTS(SELECT 1 FROM dbo.NHANVIEN WHERE NULLIF(LTRIM(RTRIM(TenNV)),N'') IS NULL OR TenNV LIKE N'%'+NCHAR(10)+N'%' OR TenNV LIKE N'%'+NCHAR(13)+N'%' OR TenNV LIKE N'%'+NCHAR(9)+N'%'
 OR (SDT IS NOT NULL AND (DATALENGTH(SDT) NOT BETWEEN 1 AND 15 OR SDT COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9]%')))
 OR EXISTS(SELECT 1 FROM dbo.TAIKHOAN t JOIN dbo.NHANVIEN n ON n.MaNV=t.MaNV WHERE t.HoatDong=1 AND n.DangLamViec=0)
 THROW 53107,N'Hồ sơ cũ cần rà soát tên, SDT hoặc tài khoản nhân viên nghỉ việc.',1;
IF EXISTS(SELECT 1 FROM dbo.AppSetupState WHERE State='Completed') AND NOT EXISTS(SELECT 1 FROM dbo.TAIKHOAN t JOIN dbo.NHANVIEN n ON n.MaNV=t.MaNV WHERE t.MaVT='ADMIN' AND t.HoatDong=1 AND n.DangLamViec=1 AND t.MatKhauHash<>'!DISABLED_REPLACE_WITH_APP_PASSWORD_HASH!')
 THROW 53107,N'Không có ADMIN hoạt động; cần đối soát trước nâng cấp.',1;
ALTER TABLE dbo.NHANVIEN ADD Revision rowversion;
ALTER TABLE dbo.NHANVIEN ADD CONSTRAINT CK_EmployeeName CHECK(NULLIF(LTRIM(RTRIM(TenNV)),N'') IS NOT NULL),
 CONSTRAINT CK_EmployeePhone CHECK(SDT IS NULL OR (DATALENGTH(SDT) BETWEEN 1 AND 15 AND SDT COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'));
CREATE TABLE dbo.AppDatabaseIdentity(Id int NOT NULL PRIMARY KEY CHECK(Id=1),IdentityId uniqueidentifier NOT NULL DEFAULT NEWID());
INSERT dbo.AppDatabaseIdentity(Id) VALUES(1);
DENY INSERT,UPDATE,DELETE ON dbo.AppDatabaseIdentity TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.NHANVIEN TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_HRLockEmployee @Code char(5) AS
BEGIN
 DECLARE @r int,@resource nvarchar(255)=N'CafeManager:HR:Employee:'+RTRIM(@Code);
 EXEC @r=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
 IF @r<0 THROW 52603,N'Nhân viên đang được xử lý; đối chiếu trước khi thử lại.',1;
END;
GO
CREATE PROCEDURE dbo.sp_HRProtectAdmin AS
BEGIN
 DECLARE @r int;
 EXEC @r=sys.sp_getapplock @Resource=N'CoffeeManager.AdminBootstrap',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
 IF @r<0 THROW 52603,N'Quản trị đang được xử lý.',1;
 IF EXISTS(SELECT 1 FROM dbo.AppSetupState WHERE State='Completed') AND NOT EXISTS(
 SELECT 1 FROM dbo.TAIKHOAN t WITH(UPDLOCK,HOLDLOCK) JOIN dbo.NHANVIEN n WITH(HOLDLOCK) ON n.MaNV=t.MaNV
 WHERE t.MaVT='ADMIN' AND t.HoatDong=1 AND n.DangLamViec=1 AND t.MatKhauHash<>'!DISABLED_REPLACE_WITH_APP_PASSWORD_HASH!')
 THROW 53102,N'Phải giữ ít nhất một ADMIN hoạt động.',1;
END;
GO
CREATE TRIGGER dbo.TR_AccountLastAdmin ON dbo.TAIKHOAN AFTER INSERT,UPDATE,DELETE AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_HRProtectAdmin; END;
GO
CREATE TRIGGER dbo.TR_EmployeeLastAdmin ON dbo.NHANVIEN AFTER UPDATE,DELETE AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_HRProtectAdmin; END;
GO
CREATE PROCEDURE dbo.sp_HRContext @Actor int,@Stamp binary(8) AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 SELECT CONVERT(varchar(36),IdentityId) DatabaseId,CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')) ServerName,DB_NAME() DatabaseName,(SELECT MAX(Version) FROM dbo.SchemaMigrations) SchemaVersion FROM dbo.AppDatabaseIdentity;
END;
GO
CREATE VIEW dbo.vw_EmployeeRead AS
SELECT n.MaNV Code,n.TenNV Name,n.SDT Phone,n.LoaiHopDong Contract,n.LuongCung MonthlyRate,n.LuongTheoGio HourlyRate,
 n.MaCV Position,c.TenCV PositionName,n.DangLamViec Employed,n.Revision,t.MaTK AccountId,t.TenDangNhap Username,t.MaVT Role,t.HoatDong Active,t.SecurityStamp AccountStamp
FROM dbo.NHANVIEN n JOIN dbo.CHUCVU c ON c.MaCV=n.MaCV LEFT JOIN dbo.TAIKHOAN t ON t.MaNV=n.MaNV;
GO
CREATE PROCEDURE dbo.sp_EmployeeList @Actor int,@Stamp binary(8),@Search nvarchar(max)=N'',@Skip int=0,@Position varchar(5)=NULL,@Contract nvarchar(20)=NULL,@Employed bit=NULL,@Role varchar(5)=NULL,@Active bit=NULL AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 IF @Skip<0 OR LEN(@Search)>100 THROW 53101,N'Bộ lọc không hợp lệ.',1;
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 SELECT *,COUNT(*) OVER() TotalRows FROM dbo.vw_EmployeeRead WHERE (@Position IS NULL OR Position=@Position) AND (@Contract IS NULL OR Contract=@Contract)
 AND (@Employed IS NULL OR Employed=@Employed) AND (@Role IS NULL OR Role=@Role) AND (@Active IS NULL OR Active=@Active)
 AND (Code LIKE N'%'+@Search+N'%' ESCAPE N'~' OR Name LIKE N'%'+@Search+N'%' ESCAPE N'~' OR Phone LIKE N'%'+@Search+N'%' ESCAPE N'~' OR Username LIKE N'%'+@Search+N'%' ESCAPE N'~')
 ORDER BY Code OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_EmployeeGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN'; SELECT *,1 TotalRows FROM dbo.vw_EmployeeRead WHERE Code=@Code; END;
GO
CREATE PROCEDURE dbo.sp_EmployeeLookups @Actor int,@Stamp binary(8) AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN'; SELECT MaCV Code,TenCV Name,PhuCap Allowance FROM dbo.CHUCVU ORDER BY MaCV; END;
GO
CREATE PROCEDURE dbo.sp_HumanResourcesReceipt @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Operation varchar(40) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @RequestId IS NULL OR @Operation NOT IN('EmployeeCreate','EmployeeUpdate','EmployeeDelete','EmployeeEmployment','AccountCreate','AccountChange','AccountReset','PayrollCommit','PayrollPay') THROW 53101,N'Yêu cầu đối chiếu không hợp lệ.',1;
 BEGIN TRY BEGIN TRAN;
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 DECLARE @r int,@resource nvarchar(255)=N'CafeManager:Request:'+CONVERT(nvarchar(36),@RequestId);
 EXEC @r=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=1000;
 IF @r<0 THROW 52603,N'Yêu cầu còn xử lý; chưa được gửi lại.',1;
 SELECT EntityCode,Operation,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId AND Operation=@Operation;
 COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_EmployeeWrite @Actor int,@Stamp binary(8),@Action varchar(12),@RequestId uniqueidentifier,@Code varchar(max)=NULL,
 @ExpectedRevision binary(8)=NULL,@ExpectedAccountStamp binary(8)=NULL,@Name nvarchar(max)=NULL,@Phone nvarchar(max)=NULL,
 @Contract nvarchar(max)=NULL,@MonthlyRate decimal(38,18)=NULL,@HourlyRate decimal(38,18)=NULL,@Position varchar(max)=NULL,@Employed bit=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 OR @Action IS NULL OR @Action NOT IN('Create','Update','Delete','Employment') THROW 53101,N'Thao tác không hợp lệ.',1;
 SET @Name=LTRIM(RTRIM(@Name)); SET @Phone=NULLIF(LTRIM(RTRIM(@Phone)),N'');
 IF @Action IN('Create','Update') AND (@Name IS NULL OR LEN(@Name)=0 OR DATALENGTH(@Name)>100 OR @Name LIKE N'%'+NCHAR(9)+N'%' OR @Name LIKE N'%'+NCHAR(10)+N'%' OR @Name LIKE N'%'+NCHAR(13)+N'%'
 OR (@Phone IS NOT NULL AND (DATALENGTH(@Phone)>30 OR @Phone COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9]%')) OR @Contract IS NULL OR @Contract NOT IN('Full-time','Part-time')
 OR @MonthlyRate IS NULL OR @HourlyRate IS NULL OR @MonthlyRate<0 OR @HourlyRate<0 OR @MonthlyRate>999999999999999999 OR @HourlyRate>999999999999999999 OR ROUND(@MonthlyRate,0)<>@MonthlyRate OR ROUND(@HourlyRate,0)<>@HourlyRate OR @Position IS NULL OR LEN(@Position)<>5)
 THROW 53101,N'Tên, SDT, hợp đồng, lương hoặc chức vụ không hợp lệ.',1;
 IF @Action<>'Create' AND (@Code IS NULL OR LEN(@Code)<>5 OR @ExpectedRevision IS NULL) THROW 53101,N'Thiếu mã hoặc phiên bản hồ sơ.',1;
 IF @Action='Employment' AND @Employed IS NULL THROW 53101,N'Thiếu trạng thái làm việc.',1;
 BEGIN TRY BEGIN TRAN;
 DECLARE @l int; EXEC @l=sys.sp_getapplock @Resource=N'CoffeeManager.AdminBootstrap',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
 IF @l<0 THROW 52603,N'Quản trị đang xử lý.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.AppSetupState WHERE State='Completed') THROW 53102,N'Cần hoàn tất setup.',1;
 DECLARE @payload nvarchar(max)=(SELECT @Action a,@Code c,@ExpectedRevision r,@ExpectedAccountStamp s,@Name n,@Phone p,@Contract h,@MonthlyRate m,@HourlyRate g,@Position v,@Employed e FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),
 @result varchar(10),@replay bit,@operation varchar(40)='Employee'+@Action,@revision binary(8);
 EXEC dbo.sp_FinanceBegin @Actor,@Stamp,'ADMIN',@RequestId,@operation,@payload,@result OUTPUT,@replay OUTPUT;
 IF @replay=0
 BEGIN
  IF @Action='Create'
  BEGIN
   DECLARE @codes TABLE(Loai varchar(20),Ma char(5)); INSERT @codes EXEC dbo.sp_CapMa 'NHANVIEN'; SELECT @Code=Ma FROM @codes;
   INSERT dbo.NHANVIEN(MaNV,TenNV,SDT,LoaiHopDong,LuongCung,LuongTheoGio,MaCV) VALUES(@Code,@Name,CONVERT(varchar(15),@Phone),@Contract,@MonthlyRate,@HourlyRate,@Position);
  END
  ELSE
  BEGIN
   DECLARE @employee char(5)=@Code; EXEC dbo.sp_HRLockEmployee @employee;
   IF NOT EXISTS(SELECT 1 FROM dbo.NHANVIEN WITH(UPDLOCK,HOLDLOCK) WHERE MaNV=@Code AND Revision=@ExpectedRevision) THROW 53103,N'Hồ sơ đã thay đổi; tải lại.',1;
   IF @Action='Update' UPDATE dbo.NHANVIEN SET TenNV=@Name,SDT=CONVERT(varchar(15),@Phone),LoaiHopDong=@Contract,LuongCung=@MonthlyRate,LuongTheoGio=@HourlyRate,MaCV=@Position WHERE MaNV=@Code;
   IF @Action='Employment'
   BEGIN
    IF EXISTS(SELECT 1 FROM dbo.TAIKHOAN WITH(UPDLOCK,HOLDLOCK) WHERE MaNV=@Code AND (@ExpectedAccountStamp IS NULL OR SecurityStamp<>@ExpectedAccountStamp)) THROW 53103,N'Tài khoản đã thay đổi; tải lại.',1;
    -- Khóa account trước khi nghỉ để không có khoảng thời gian nhân viên nghỉ vẫn đăng nhập.
    IF @Employed=0 UPDATE dbo.TAIKHOAN SET HoatDong=0 WHERE MaNV=@Code;
    UPDATE dbo.NHANVIEN SET DangLamViec=@Employed WHERE MaNV=@Code;
   END;
   IF @Action='Delete' DELETE dbo.NHANVIEN WHERE MaNV=@Code;
  END;
  SELECT @revision=Revision FROM dbo.NHANVIEN WHERE MaNV=@Code; SET @result=@Code;
  EXEC dbo.sp_FinanceFinish @Actor,@RequestId,@operation,@payload,@result,@revision;
 END;
 COMMIT; SELECT @result EntityCode;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_AccountWrite @Actor int,@Stamp binary(8),@Action varchar(10),@RequestId uniqueidentifier,@Employee varchar(max),@ExpectedStamp binary(8)=NULL,
 @Username nvarchar(max)=NULL,@NewHash varchar(max)=NULL,@Role varchar(max)=NULL,@Active bit=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 OR @Action IS NULL OR @Action NOT IN('Create','Change','Reset') OR @Employee IS NULL OR LEN(@Employee)<>5 THROW 53101,N'Thao tác tài khoản không hợp lệ.',1;
 IF @Action='Create' AND (@Username IS NULL OR DATALENGTH(@Username) NOT BETWEEN 2 AND 100 OR DATALENGTH(@Username)<>DATALENGTH(LTRIM(RTRIM(@Username))) OR @Username COLLATE Latin1_General_100_BIN2 LIKE N'%[^-A-Za-z0-9._]%') THROW 53101,N'Tên đăng nhập không hợp lệ.',1;
 IF @Action IN('Create','Reset') AND (@NewHash IS NULL OR DATALENGTH(@NewHash) NOT BETWEEN 20 AND 255 OR @NewHash='!DISABLED_REPLACE_WITH_APP_PASSWORD_HASH!') THROW 53101,N'Hash mật khẩu không hợp lệ.',1;
 IF @Action IN('Create','Change') AND (@Role IS NULL OR @Role NOT IN('ADMIN','CASH','STORE') OR @Active IS NULL) THROW 53101,N'Vai trò hoặc trạng thái không hợp lệ.',1;
 BEGIN TRY BEGIN TRAN;
 DECLARE @l int; EXEC @l=sys.sp_getapplock @Resource=N'CoffeeManager.AdminBootstrap',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
 IF @l<0 THROW 52603,N'Quản trị đang xử lý.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.AppSetupState WHERE State='Completed') THROW 53102,N'Cần hoàn tất setup.',1;
 DECLARE @payload nvarchar(max)=(SELECT @Action a,@Employee e,@ExpectedStamp s,@Username u,@NewHash h,@Role r,@Active v FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@result varchar(10),@replay bit,@operation varchar(40)='Account'+@Action;
 EXEC dbo.sp_FinanceBegin @Actor,@Stamp,'ADMIN',@RequestId,@operation,@payload,@result OUTPUT,@replay OUTPUT;
 IF @replay=0
 BEGIN
  DECLARE @employeeCode char(5)=@Employee; EXEC dbo.sp_HRLockEmployee @employeeCode;
  IF NOT EXISTS(SELECT 1 FROM dbo.NHANVIEN WITH(UPDLOCK,HOLDLOCK) WHERE MaNV=@Employee AND (DangLamViec=1 OR (@Action<>'Create' AND ISNULL(@Active,0)=0))) THROW 53102,N'Nhân viên đã nghỉ hoặc không tồn tại.',1;
  IF @Action='Create'
  BEGIN INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT,HoatDong) VALUES(@Employee,CONVERT(varchar(50),@Username),@NewHash,@Role,@Active); SET @result=CONVERT(varchar(10),SCOPE_IDENTITY()); END
  ELSE
  BEGIN
   IF NOT EXISTS(SELECT 1 FROM dbo.TAIKHOAN WITH(UPDLOCK,HOLDLOCK) WHERE MaNV=@Employee AND SecurityStamp=@ExpectedStamp) THROW 53103,N'Tài khoản đã thay đổi; tải lại.',1;
   IF @Action='Reset' UPDATE dbo.TAIKHOAN SET MatKhauHash=@NewHash WHERE MaNV=@Employee;
   ELSE UPDATE dbo.TAIKHOAN SET MaVT=@Role,HoatDong=@Active WHERE MaNV=@Employee;
   SELECT @result=CONVERT(varchar(10),MaTK) FROM dbo.TAIKHOAN WHERE MaNV=@Employee;
  END;
  DECLARE @newStamp binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaNV=@Employee);
  EXEC dbo.sp_FinanceFinish @Actor,@RequestId,@operation,@payload,@result,@newStamp;
 END;
 COMMIT; SELECT @result EntityCode;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
GRANT EXECUTE ON dbo.sp_HRContext TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_EmployeeList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_EmployeeGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_EmployeeLookups TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_EmployeeWrite TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_AccountWrite TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_HumanResourcesReceipt TO CafeManagerApp;
