-- T11. Apply with runner after005.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=5) THROW 51807,N'Migration005 bắt buộc.',1;
IF EXISTS(SELECT 1 FROM dbo.BAN WITH(TABLOCKX,HOLDLOCK) WHERE NULLIF(LTRIM(RTRIM(TenBan)),N'') IS NULL)
 OR EXISTS(SELECT 1 FROM dbo.KHUVUC WITH(TABLOCKX,HOLDLOCK) WHERE NULLIF(LTRIM(RTRIM(TenKV)),N'') IS NULL) THROW 51807,N'Tên bàn/khu vực cũ không hợp lệ.',1;
ALTER TABLE dbo.BAN ADD Revision rowversion;
ALTER TABLE dbo.KHUVUC ADD Revision rowversion;
DENY INSERT,UPDATE,DELETE ON dbo.BAN TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.KHUVUC TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_AreaList @Actor int,@Stamp binary(8),@Search nvarchar(max)=N'',@Skip int=0 AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 IF LEN(@Search)>100 OR @Skip<0 THROW 51801,N'Bộ lọc không hợp lệ.',1;
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 SELECT MaKV Code,TenKV Name,Revision FROM dbo.KHUVUC WHERE TenKV LIKE N'%'+@Search+N'%' ESCAPE N'~' OR MaKV LIKE N'%'+@Search+N'%' ESCAPE N'~' ORDER BY MaKV OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_AreaGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH'; SELECT MaKV Code,TenKV Name,Revision FROM dbo.KHUVUC WHERE MaKV=@Code; END;
GO
CREATE PROCEDURE dbo.sp_TableList @Actor int,@Stamp binary(8),@Search nvarchar(max)=N'',@Skip int=0,@Area varchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 IF LEN(@Search)>100 OR @Skip<0 THROW 51801,N'Bộ lọc không hợp lệ.',1;
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 SELECT b.MaBan Code,b.TenBan Name,b.MaKV Area,v.TrangThai Status,v.MaHD OrderCode,b.Revision,SYSDATETIME() ReadAt
 FROM dbo.BAN b JOIN dbo.vw_TrangThaiBan v ON v.MaBan=b.MaBan
 WHERE (@Area IS NULL OR b.MaKV=@Area) AND (b.TenBan LIKE N'%'+@Search+N'%' ESCAPE N'~' OR b.MaBan LIKE N'%'+@Search+N'%' ESCAPE N'~')
 ORDER BY b.MaBan OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_TableSelectCurrent @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 SELECT b.MaBan Code,b.TenBan Name,b.MaKV Area,v.TrangThai Status,v.MaHD OrderCode,b.Revision,SYSDATETIME() ReadAt
 FROM dbo.BAN b JOIN dbo.vw_TrangThaiBan v ON v.MaBan=b.MaBan WHERE b.MaBan=@Code;
END;
GO
CREATE PROCEDURE dbo.sp_TableGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_TableSelectCurrent @Actor,@Stamp,@Code; END;
GO
CREATE PROCEDURE dbo.sp_TableAreaWrite @Actor int,@Stamp binary(8),@Kind varchar(5),@Action varchar(10),@Code varchar(max),@RequestId uniqueidentifier,
 @ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Area varchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
  IF @Kind NOT IN('Table','Area') OR @Action NOT IN('Create','Update','Delete') THROW 51801,N'Thao tác không hợp lệ.',1;
  SET @Code=UPPER(LTRIM(RTRIM(@Code))); SET @Name=LTRIM(RTRIM(@Name));
  IF @Code IS NULL OR DATALENGTH(@Code)<>5 OR (@Kind='Table' AND (@Code COLLATE Latin1_General_100_BIN2 NOT LIKE 'B[0-9][0-9][0-9][0-9]' OR @Code='B0000')) OR (@Kind='Area' AND (@Code COLLATE Latin1_General_100_BIN2 NOT LIKE 'KV[0-9][0-9][0-9]' OR @Code='KV000')) THROW 51801,N'Mã bàn/khu vực không hợp lệ.',1;
  IF @Action<>'Delete' AND (@Name IS NULL OR LEN(@Name)=0 OR DATALENGTH(@Name)>100 OR @Name LIKE N'%'+NCHAR(9)+N'%' OR @Name LIKE N'%'+NCHAR(10)+N'%' OR @Name LIKE N'%'+NCHAR(13)+N'%') THROW 51801,N'Tên không hợp lệ.',1;
  IF @Action<>'Delete' AND @Kind='Table' AND NOT EXISTS(SELECT 1 FROM dbo.KHUVUC WHERE MaKV=@Area) THROW 51801,N'Khu vực không tồn tại.',1;
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@Name n,@Area a,@ExpectedRevision r FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
  DECLARE @hash binary(32)=HASHBYTES('SHA2_256',CONVERT(varbinary(max),@payload)),@resultCode varchar(10)=@Code,@revision binary(8),@replay bit,@operation varchar(40)=@Kind+@Action;
  EXEC dbo.sp_G03BeginReceipt @Actor,@operation,@RequestId,@hash,@resultCode OUTPUT,@revision OUTPUT,@replay OUTPUT;
  IF @replay=0
  BEGIN
   IF @Kind='Area'
   BEGIN
    IF @Action='Create' INSERT dbo.KHUVUC(MaKV,TenKV) VALUES(@Code,@Name);
    ELSE
    BEGIN
     IF NOT EXISTS(SELECT 1 FROM dbo.KHUVUC WITH(UPDLOCK,HOLDLOCK) WHERE MaKV=@Code AND Revision=@ExpectedRevision) THROW 51603,N'Khu vực đã thay đổi.',1;
     IF @Action='Update' UPDATE dbo.KHUVUC SET TenKV=@Name WHERE MaKV=@Code;
     ELSE DELETE dbo.KHUVUC WHERE MaKV=@Code;
    END;
    SELECT @revision=Revision FROM dbo.KHUVUC WHERE MaKV=@Code;
   END
   ELSE
   BEGIN
    IF @Action='Create' INSERT dbo.BAN(MaBan,TenBan,MaKV) VALUES(@Code,@Name,@Area);
    ELSE
    BEGIN
     IF NOT EXISTS(SELECT 1 FROM dbo.BAN WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@Code AND Revision=@ExpectedRevision) THROW 51603,N'Bàn đã thay đổi.',1;
     IF @Action='Update' UPDATE dbo.BAN SET TenBan=@Name,MaKV=@Area WHERE MaBan=@Code;
     ELSE DELETE dbo.BAN WHERE MaBan=@Code;
    END;
    SELECT @revision=Revision FROM dbo.BAN WHERE MaBan=@Code;
   END;
   INSERT dbo.G03OperationReceipt(RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,ResultRevision) VALUES(@RequestId,@Actor,@operation,@Code,@hash,@revision);
  END;
  COMMIT; SELECT @resultCode Code,@revision Revision;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_TableCreate @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Area varchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_TableAreaWrite @Actor,@Stamp,'Table','Create',@Code,@RequestId,@ExpectedRevision,@Name,@Area; END;
GO
GRANT EXECUTE ON dbo.sp_TableCreate TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_TableUpdate @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Area varchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_TableAreaWrite @Actor,@Stamp,'Table','Update',@Code,@RequestId,@ExpectedRevision,@Name,@Area; END;
GO
GRANT EXECUTE ON dbo.sp_TableUpdate TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_TableDelete @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Area varchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_TableAreaWrite @Actor,@Stamp,'Table','Delete',@Code,@RequestId,@ExpectedRevision,@Name,@Area; END;
GO
GRANT EXECUTE ON dbo.sp_TableDelete TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_AreaCreate @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Area varchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_TableAreaWrite @Actor,@Stamp,'Area','Create',@Code,@RequestId,@ExpectedRevision,@Name,@Area; END;
GO
GRANT EXECUTE ON dbo.sp_AreaCreate TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_AreaUpdate @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Area varchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_TableAreaWrite @Actor,@Stamp,'Area','Update',@Code,@RequestId,@ExpectedRevision,@Name,@Area; END;
GO
GRANT EXECUTE ON dbo.sp_AreaUpdate TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_AreaDelete @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Area varchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_TableAreaWrite @Actor,@Stamp,'Area','Delete',@Code,@RequestId,@ExpectedRevision,@Name,@Area; END;
GO
GRANT EXECUTE ON dbo.sp_AreaDelete TO CafeManagerApp;
GO
GRANT EXECUTE ON dbo.sp_AreaList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_AreaGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_TableList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_TableGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_TableSelectCurrent TO CafeManagerApp;
