-- Apply through Apply-Migration.ps1 -Version 3 after 001 and 002.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=2) THROW 51507,'Version 2 and migration transaction required.',1;
IF EXISTS(SELECT 1 FROM dbo.DANHMUC_THUCHI WHERE MaDMTC IN('TC005','TC011') AND Loai<>N'Chi') THROW 51507,'System expense type conflict.',1;
IF EXISTS(SELECT 1 FROM dbo.DANHMUC_THUCHI WHERE MaDMTC='TC012') THROW 51507,'Reserved immediate purchase code already exists; review required.',1;
INSERT dbo.DANHMUC_THUCHI(MaDMTC,TenDMTC,Loai)
 SELECT v.Code,v.Name,N'Chi' FROM (VALUES('TC005',N'Lương nhân viên'),('TC011',N'Trả nợ nhà cung cấp'),('TC012',N'Trả ngay nhập hàng')) v(Code,Name)
 WHERE NOT EXISTS(SELECT 1 FROM dbo.DANHMUC_THUCHI d WHERE d.MaDMTC=v.Code);
CREATE TABLE dbo.SystemExpenseCategory(UseCase VARCHAR(20) NOT NULL PRIMARY KEY,
 MaDMTC CHAR(5) NOT NULL UNIQUE REFERENCES dbo.DANHMUC_THUCHI(MaDMTC));
INSERT dbo.SystemExpenseCategory VALUES('Payroll','TC005'),('SupplierDebt','TC011'),('PurchaseImmediate','TC012');
INSERT dbo.PHUONGTHUCTT(MaPT,TenPT) SELECT v.Code,v.Name FROM(VALUES('PT001',N'Tiền mặt'),('PT002',N'Chuyển khoản ngân hàng'))v(Code,Name)
 WHERE NOT EXISTS(SELECT 1 FROM dbo.PHUONGTHUCTT p WHERE p.MaPT=v.Code);
CREATE TABLE dbo.PaymentMethodSupport(MaPT CHAR(5) NOT NULL PRIMARY KEY REFERENCES dbo.PHUONGTHUCTT(MaPT),
 IsSupported BIT NOT NULL DEFAULT 0, CONSTRAINT CK_PaymentSupport CHECK(MaPT<>'PT010' OR IsSupported=0));
INSERT dbo.PaymentMethodSupport SELECT MaPT,CASE WHEN MaPT IN('PT001','PT002') THEN 1 ELSE 0 END FROM dbo.PHUONGTHUCTT;
CREATE TABLE dbo.CatalogCreateReceipt(RequestId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
 ActorAccountId INT NOT NULL REFERENCES dbo.TAIKHOAN(MaTK),Kind VARCHAR(30) NOT NULL,
 EntityCode CHAR(5) NOT NULL,PayloadHash CHAR(64) NOT NULL,CreatedAt DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME());
DENY INSERT,UPDATE,DELETE ON dbo.SystemExpenseCategory TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.PaymentMethodSupport TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.CatalogCreateReceipt TO CafeManagerApp;
GO
CREATE TRIGGER dbo.TR_SystemExpense_Immutable ON dbo.DANHMUC_THUCHI AFTER UPDATE,DELETE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM deleted d JOIN dbo.SystemExpenseCategory s ON s.MaDMTC=d.MaDMTC
  LEFT JOIN inserted i ON i.MaDMTC=d.MaDMTC WHERE i.MaDMTC IS NULL OR i.Loai<>d.Loai)
  THROW 51502,'System expense code/type/deletion is protected.',1;
END;
GO
CREATE PROCEDURE dbo.sp_CatalogDemandActor @Actor INT,@Stamp BINARY(8),@Role VARCHAR(5)='ADMIN' AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM dbo.TAIKHOAN t WITH(HOLDLOCK) JOIN dbo.NHANVIEN n WITH(HOLDLOCK) ON n.MaNV=t.MaNV
 WHERE t.MaTK=@Actor AND t.SecurityStamp=@Stamp AND t.HoatDong=1 AND n.DangLamViec=1 AND (@Role='ANY' OR t.MaVT=@Role))
  THROW 51505,'Actor inactive or changed.',1;
END;
GO
-- Closed migration metadata, never identifiers supplied by a client. Produces 35 concrete procedures.
DECLARE @catalogs TABLE(Kind VARCHAR(30),TableName SYSNAME,CodeName SYSNAME,NameName SYSNAME,NameLength INT,
 Prefix VARCHAR(3),Digits INT,ExtraDefs NVARCHAR(500),ExtraColumns NVARCHAR(300),ExtraValues NVARCHAR(300),ExtraUpdates NVARCHAR(500),Projection NVARCHAR(1000),ValidateExtra NVARCHAR(1000));
INSERT @catalogs VALUES
('ProductCategory','DANHMUC_SP','MaDM','TenDM',50,'DM',3,N'',N'',N'',N'',N'CAST(NULL AS varchar(15)) Phone,CAST(NULL AS decimal(5,2)) Ratio,CAST(NULL AS decimal(18,0)) Allowance,CAST(NULL AS decimal(5,2)) Factor,CAST(NULL AS time) StartTime,CAST(NULL AS time) EndTime,CAST(NULL AS nvarchar(10)) ExpenseType,CAST(NULL AS bit) Supported',N''),
('Supplier','NHACUNGCAP','MaNCC','TenNCC',100,'NCC',2,N',@Phone varchar(max)=NULL',N',SDT',N',@Phone',N',SDT=@Phone',N'SDT Phone,CAST(NULL AS decimal(5,2)) Ratio,CAST(NULL AS decimal(18,0)) Allowance,CAST(NULL AS decimal(5,2)) Factor,CAST(NULL AS time) StartTime,CAST(NULL AS time) EndTime,CAST(NULL AS nvarchar(10)) ExpenseType,CAST(NULL AS bit) Supported',N'IF @Phone IS NULL OR DATALENGTH(@Phone) NOT BETWEEN 1 AND 15 OR @Phone COLLATE Latin1_General_100_BIN2 LIKE ''%[^0-9]%'' THROW 51501,''Supplier phone required.'',1;'),
('PaymentMethod','PHUONGTHUCTT','MaPT','TenPT',50,'PT',3,N',@Supported bit=0',N'',N'',N'',N'CAST(NULL AS varchar(15)) Phone,CAST(NULL AS decimal(5,2)) Ratio,CAST(NULL AS decimal(18,0)) Allowance,CAST(NULL AS decimal(5,2)) Factor,CAST(NULL AS time) StartTime,CAST(NULL AS time) EndTime,CAST(NULL AS nvarchar(10)) ExpenseType,(SELECT IsSupported FROM dbo.PaymentMethodSupport s WHERE s.MaPT=x.MaPT) Supported',N'IF @Code=''PT010'' AND @Supported=1 THROW 51504,''Points payment unsupported.'',1;'),
('DeliveryPartner','DOITACGIAOHANG','MaDT','TenDT',50,'DT',3,N',@Ratio decimal(5,2)=NULL',N',ChietKhau',N',@Ratio',N',ChietKhau=@Ratio',N'CAST(NULL AS varchar(15)) Phone,ChietKhau Ratio,CAST(NULL AS decimal(18,0)) Allowance,CAST(NULL AS decimal(5,2)) Factor,CAST(NULL AS time) StartTime,CAST(NULL AS time) EndTime,CAST(NULL AS nvarchar(10)) ExpenseType,CAST(NULL AS bit) Supported',N'IF @Ratio IS NULL OR @Ratio NOT BETWEEN 0 AND 100 THROW 51501,''Invalid ratio.'',1;'),
('ExpenseCategory','DANHMUC_THUCHI','MaDMTC','TenDMTC',50,'TC',3,N',@ExpenseType nvarchar(10)=NULL',N',Loai',N',@ExpenseType',N',Loai=@ExpenseType',N'CAST(NULL AS varchar(15)) Phone,CAST(NULL AS decimal(5,2)) Ratio,CAST(NULL AS decimal(18,0)) Allowance,CAST(NULL AS decimal(5,2)) Factor,CAST(NULL AS time) StartTime,CAST(NULL AS time) EndTime,Loai ExpenseType,CAST(NULL AS bit) Supported',N'IF @ExpenseType IS NULL OR @ExpenseType NOT IN(N''Thu'',N''Chi'') THROW 51501,''Invalid expense type.'',1;'),
('Position','CHUCVU','MaCV','TenCV',50,'CV',3,N',@Allowance decimal(18,0)=NULL,@Factor decimal(5,2)=NULL',N',PhuCap,HeSoTangCa',N',@Allowance,@Factor',N',PhuCap=@Allowance,HeSoTangCa=@Factor',N'CAST(NULL AS varchar(15)) Phone,CAST(NULL AS decimal(5,2)) Ratio,PhuCap Allowance,HeSoTangCa Factor,CAST(NULL AS time) StartTime,CAST(NULL AS time) EndTime,CAST(NULL AS nvarchar(10)) ExpenseType,CAST(NULL AS bit) Supported',N'IF @Allowance IS NULL OR @Allowance<0 OR @Factor IS NULL OR @Factor<1 THROW 51501,''Invalid allowance/factor.'',1;'),
('Shift','CALAMVIEC','MaCa','TenCa',30,'CA',3,N',@StartTime time=NULL,@EndTime time=NULL',N',GioBatDau,GioKetThuc',N',@StartTime,@EndTime',N',GioBatDau=@StartTime,GioKetThuc=@EndTime',N'CAST(NULL AS varchar(15)) Phone,CAST(NULL AS decimal(5,2)) Ratio,CAST(NULL AS decimal(18,0)) Allowance,CAST(NULL AS decimal(5,2)) Factor,GioBatDau StartTime,GioKetThuc EndTime,CAST(NULL AS nvarchar(10)) ExpenseType,CAST(NULL AS bit) Supported',N'IF @StartTime IS NULL OR @EndTime IS NULL OR @StartTime=@EndTime OR DATEPART(SECOND,@StartTime)<>0 OR DATEPART(SECOND,@EndTime)<>0 OR DATEPART(NANOSECOND,@StartTime)<>0 OR DATEPART(NANOSECOND,@EndTime)<>0 THROW 51501,''Invalid shift.'',1;');
DECLARE @kind VARCHAR(30),@table SYSNAME,@code SYSNAME,@name SYSNAME,@length INT,@prefix VARCHAR(3),@digits INT,
 @defs NVARCHAR(500),@cols NVARCHAR(300),@vals NVARCHAR(300),@updates NVARCHAR(500),@projection NVARCHAR(1000),@extra NVARCHAR(1000),@sql NVARCHAR(MAX),@select NVARCHAR(MAX),@action VARCHAR(10),@body NVARCHAR(MAX);
DECLARE catalog CURSOR LOCAL FAST_FORWARD FOR SELECT * FROM @catalogs;
OPEN catalog;
FETCH NEXT FROM catalog INTO @kind,@table,@code,@name,@length,@prefix,@digits,@defs,@cols,@vals,@updates,@projection,@extra;
WHILE @@FETCH_STATUS=0
BEGIN
 SET @sql=N'IF EXISTS(SELECT 1 FROM dbo.'+QUOTENAME(@table)+N' WHERE NULLIF(LTRIM(RTRIM('+QUOTENAME(@name)+N')),N'''') IS NULL) THROW 51507,''Invalid existing catalog name.'',1; ALTER TABLE dbo.'+QUOTENAME(@table)+N' ADD CatalogRevision rowversion; ALTER TABLE dbo.'+QUOTENAME(@table)+N' ADD CONSTRAINT '+QUOTENAME('CK_G02_'+@table+'_Name')+N' CHECK(NULLIF(LTRIM(RTRIM('+QUOTENAME(@name)+N')),N'''') IS NOT NULL); DENY INSERT,UPDATE,DELETE ON dbo.'+QUOTENAME(@table)+N' TO CafeManagerApp;';
 EXEC sys.sp_executesql @sql;
 SET @select=N'SELECT x.'+QUOTENAME(@code)+N' Code,x.'+QUOTENAME(@name)+N' Name,'+@projection+N',x.CatalogRevision Revision FROM dbo.'+QUOTENAME(@table)+N' x';
 SET @sql=N'CREATE PROCEDURE dbo.sp_Catalog'+@kind+N'List @Actor int,@Stamp binary(8),@Search nvarchar(100)=N'''',@Skip int=0 AS BEGIN SET NOCOUNT ON; EXEC dbo.sp_CatalogDemandActor @Actor,@Stamp; IF @Skip<0 THROW 51501,''Invalid page.'',1; SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N''~'',N''~~''),N''%'',N''~%''),N''_'',N''~_''),N''['',N''~[''); '+@select+N' WHERE x.'+QUOTENAME(@name)+N' LIKE N''%''+@Search+N''%'' ESCAPE N''~'' ORDER BY x.'+QUOTENAME(@code)+N' OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY; END;';
 EXEC sys.sp_executesql @sql;
 SET @sql=N'CREATE PROCEDURE dbo.sp_Catalog'+@kind+N'Get @Actor int,@Stamp binary(8),@Code char(5) AS BEGIN SET NOCOUNT ON; EXEC dbo.sp_CatalogDemandActor @Actor,@Stamp; '+@select+N' WHERE x.'+QUOTENAME(@code)+N'=@Code; END;';
 EXEC sys.sp_executesql @sql;
 DECLARE actions CURSOR LOCAL FAST_FORWARD FOR SELECT v FROM(VALUES('Create'),('Update'),('Delete')) a(v);
 OPEN actions; FETCH NEXT FROM actions INTO @action;
 WHILE @@FETCH_STATUS=0
 BEGIN
  -- Wide input parameters preserve overlength values until validation; storage columns stay bounded.
  SET @body=N'CREATE PROCEDURE dbo.sp_Catalog'+@kind+@action+N' @Actor int,@Stamp binary(8),@Code char(5)=NULL,@Name nvarchar(max)=NULL,@ExpectedRevision binary(8)=NULL,@RequestId uniqueidentifier=NULL'+@defs+N' AS BEGIN SET NOCOUNT ON; SET XACT_ABORT ON; BEGIN TRY BEGIN TRANSACTION; EXEC dbo.sp_CatalogDemandActor @Actor,@Stamp;';
  IF @action<>'Delete'
   SET @body+=N'IF @Name IS NULL OR LEN(LTRIM(RTRIM(@Name)))=0 OR LEN(@Name)>'+CONVERT(VARCHAR(10),@length)+N' OR @Name LIKE N''%''+NCHAR(9)+N''%'' OR @Name LIKE N''%''+NCHAR(10)+N''%'' OR @Name LIKE N''%''+NCHAR(13)+N''%'' THROW 51501,''Invalid catalog name.'',1; SET @Name=LTRIM(RTRIM(@Name)); '+@extra;
  IF @action='Create'
  BEGIN
   IF @kind='ProductCategory'
    SET @body+=N'IF @RequestId IS NULL THROW 51501,''Request ID required.'',1; DECLARE @hash char(64)=CONVERT(char(64),HASHBYTES(''SHA2_256'',CONVERT(varbinary(max),@Name)),2),@old char(5); SELECT @old=EntityCode FROM dbo.CatalogCreateReceipt WITH(UPDLOCK,HOLDLOCK) WHERE RequestId=@RequestId; IF @old IS NOT NULL BEGIN IF NOT EXISTS(SELECT 1 FROM dbo.CatalogCreateReceipt WHERE RequestId=@RequestId AND ActorAccountId=@Actor AND Kind=''ProductCategory'' AND PayloadHash=@hash) THROW 51508,''Request reused.'',1; COMMIT; SELECT MaDM Code,CatalogRevision Revision FROM dbo.DANHMUC_SP WHERE MaDM=@old; RETURN; END; DECLARE @allocated TABLE(Loai varchar(20),Ma char(5)); INSERT @allocated EXEC dbo.sp_CapMa ''DANHMUC_SP''; SELECT @Code=Ma FROM @allocated;';
   ELSE
    SET @body+=N'IF @Code IS NULL OR @Code COLLATE Latin1_General_100_BIN2 NOT LIKE '''+@prefix+REPLICATE('[0-9]',@digits)+N''' OR TRY_CONVERT(int,SUBSTRING(@Code,'+CONVERT(VARCHAR(2),LEN(@prefix)+1)+N',3))<1 THROW 51506,''Invalid catalog code.'',1;';
   IF @kind='ExpenseCategory' SET @body+=N'IF @Code IN(''TC005'',''TC011'',''TC012'') THROW 51502,''Reserved system code.'',1;';
   IF @kind='PaymentMethod' SET @body+=N'IF @Code=''PT010'' OR @Supported<>0 OR @Supported IS NULL THROW 51504,''New method must be unsupported.'',1;';
   SET @body+=N'INSERT dbo.'+QUOTENAME(@table)+N'('+QUOTENAME(@code)+N','+QUOTENAME(@name)+@cols+N') VALUES(@Code,@Name'+@vals+N');';
   IF @kind='ProductCategory' SET @body+=N'INSERT dbo.CatalogCreateReceipt(RequestId,ActorAccountId,Kind,EntityCode,PayloadHash) VALUES(@RequestId,@Actor,''ProductCategory'',@Code,@hash);';
   IF @kind='PaymentMethod' SET @body+=N'INSERT dbo.PaymentMethodSupport(MaPT,IsSupported) VALUES(@Code,@Supported);';
  END
  ELSE
  BEGIN
   SET @body+=N'IF NOT EXISTS(SELECT 1 FROM dbo.'+QUOTENAME(@table)+N' WITH(UPDLOCK,HOLDLOCK) WHERE '+QUOTENAME(@code)+N'=@Code AND CatalogRevision=@ExpectedRevision) THROW 51503,''Catalog changed or deleted.'',1;';
   IF @action='Update'
   BEGIN
    SET @body+=N'UPDATE dbo.'+QUOTENAME(@table)+N' SET '+QUOTENAME(@name)+N'=@Name'+@updates+N' WHERE '+QUOTENAME(@code)+N'=@Code;';
    IF @kind='PaymentMethod' SET @body+=N'UPDATE dbo.PaymentMethodSupport SET IsSupported=@Supported WHERE MaPT=@Code;';
   END
   ELSE
   BEGIN
    IF @kind='PaymentMethod'
     SET @body+=N'IF EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaPT=@Code) OR EXISTS(SELECT 1 FROM dbo.THANHTOAN_CONGNO WHERE MaPT=@Code) THROW 51502,''Payment method has history.'',1; DELETE dbo.PaymentMethodSupport WHERE MaPT=@Code;';
    SET @body+=N'DELETE dbo.'+QUOTENAME(@table)+N' WHERE '+QUOTENAME(@code)+N'=@Code;';
   END;
  END;
  SET @body+=N'COMMIT; SELECT @Code Code,'+CASE WHEN @action='Delete' THEN N'CAST(NULL AS binary(8))' ELSE N'(SELECT CatalogRevision FROM dbo.'+QUOTENAME(@table)+N' WHERE '+QUOTENAME(@code)+N'=@Code)' END+N' Revision; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH END;';
  EXEC sys.sp_executesql @body;
  FETCH NEXT FROM actions INTO @action;
 END;
 CLOSE actions; DEALLOCATE actions;
 SET @sql=N'GRANT EXECUTE ON dbo.sp_Catalog'+@kind+N'List TO CafeManagerApp; GRANT EXECUTE ON dbo.sp_Catalog'+@kind+N'Get TO CafeManagerApp; GRANT EXECUTE ON dbo.sp_Catalog'+@kind+N'Create TO CafeManagerApp; GRANT EXECUTE ON dbo.sp_Catalog'+@kind+N'Update TO CafeManagerApp; GRANT EXECUTE ON dbo.sp_Catalog'+@kind+N'Delete TO CafeManagerApp;';
 EXEC sys.sp_executesql @sql;
 FETCH NEXT FROM catalog INTO @kind,@table,@code,@name,@length,@prefix,@digits,@defs,@cols,@vals,@updates,@projection,@extra;
END;
CLOSE catalog; DEALLOCATE catalog;
GO
CREATE PROCEDURE dbo.sp_PaymentMethodListSupported @Actor INT,@Stamp BINARY(8),@Purpose VARCHAR(20) AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_CatalogDemandActor @Actor,@Stamp,'ANY';
 IF @Purpose NOT IN('Checkout','Inventory') OR NOT EXISTS(SELECT 1 FROM dbo.TAIKHOAN WHERE MaTK=@Actor
  AND(MaVT='ADMIN' OR(@Purpose='Checkout' AND MaVT='CASH') OR(@Purpose='Inventory' AND MaVT='STORE')))
  THROW 51505,'Payment lookup not allowed.',1;
 SELECT p.MaPT Code,p.TenPT Name FROM dbo.PHUONGTHUCTT p JOIN dbo.PaymentMethodSupport s ON s.MaPT=p.MaPT
 WHERE s.IsSupported=1 AND p.MaPT<>'PT010' ORDER BY p.MaPT;
END;
GO
CREATE PROCEDURE dbo.sp_PaymentMethodSetSupport @Actor INT,@Stamp BINARY(8),@Code CHAR(5),@Supported BIT,@ExpectedRevision BINARY(8) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY BEGIN TRANSACTION;
 EXEC dbo.sp_CatalogDemandActor @Actor,@Stamp;
 IF @Code='PT010' AND @Supported=1 THROW 51504,'Points payment unsupported.',1;
 UPDATE dbo.PHUONGTHUCTT SET TenPT=TenPT WHERE MaPT=@Code AND CatalogRevision=@ExpectedRevision;
 IF @@ROWCOUNT<>1 THROW 51503,'Method changed.',1;
 UPDATE dbo.PaymentMethodSupport SET IsSupported=@Supported WHERE MaPT=@Code;
 COMMIT; SELECT MaPT Code,CatalogRevision Revision FROM dbo.PHUONGTHUCTT WHERE MaPT=@Code;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END;
GO
CREATE PROCEDURE dbo.sp_CatalogLookup @Actor INT,@Stamp BINARY(8),@Kind VARCHAR(30) AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_CatalogDemandActor @Actor,@Stamp,'ANY';
 DECLARE @role CHAR(5)=(SELECT MaVT FROM dbo.TAIKHOAN WHERE MaTK=@Actor);
 IF @Kind='ProductCategory' AND @role IN('ADMIN','CASH','STORE') SELECT MaDM Code,TenDM Name FROM dbo.DANHMUC_SP ORDER BY MaDM;
 ELSE IF @Kind='Supplier' AND @role IN('ADMIN','STORE') SELECT MaNCC Code,TenNCC Name,SDT Phone FROM dbo.NHACUNGCAP ORDER BY MaNCC;
 ELSE IF @Kind='DeliveryPartner' AND @role IN('ADMIN','CASH') SELECT MaDT Code,TenDT Name FROM dbo.DOITACGIAOHANG ORDER BY MaDT;
 ELSE IF @Kind='ExpenseCategory' AND @role='ADMIN' SELECT MaDMTC Code,TenDMTC Name,Loai ExpenseType FROM dbo.DANHMUC_THUCHI
  WHERE MaDMTC NOT IN(SELECT MaDMTC FROM dbo.SystemExpenseCategory) ORDER BY MaDMTC;
 ELSE IF @Kind='Position' AND @role='ADMIN' SELECT MaCV Code,TenCV Name FROM dbo.CHUCVU ORDER BY MaCV;
 ELSE IF @Kind='Shift' AND @role='ADMIN' SELECT MaCa Code,TenCa Name,GioBatDau StartTime,GioKetThuc EndTime FROM dbo.CALAMVIEC ORDER BY MaCa;
 ELSE THROW 51505,'Lookup not allowed.',1;
END;
GO
GRANT EXECUTE ON dbo.sp_PaymentMethodListSupported TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_PaymentMethodSetSupport TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_CatalogLookup TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_CatalogProductCategoryReceipt @Actor INT,@Stamp BINARY(8),@RequestId UNIQUEIDENTIFIER AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_CatalogDemandActor @Actor,@Stamp;
 SELECT EntityCode FROM dbo.CatalogCreateReceipt WITH(HOLDLOCK)
 WHERE RequestId=@RequestId AND ActorAccountId=@Actor AND Kind='ProductCategory';
END;
GO
GRANT EXECUTE ON dbo.sp_CatalogProductCategoryReceipt TO CafeManagerApp;
