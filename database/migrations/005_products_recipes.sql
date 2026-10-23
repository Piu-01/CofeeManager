-- T10; run through migration runner, after 004.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=4) THROW 51707,N'Migration004 bắt buộc.',1;
IF EXISTS(SELECT 1 FROM dbo.SANPHAM s WITH(TABLOCKX,HOLDLOCK) WHERE NULLIF(LTRIM(RTRIM(s.TenSP)),N'') IS NULL OR (s.TrangThai=N'Dang ban' AND NOT EXISTS(SELECT 1 FROM dbo.CONGTHUC c WHERE c.MaSP=s.MaSP)))
 THROW 51707,N'Sản phẩm cũ không hợp lệ hoặc đang bán nhưng thiếu công thức.',1;
ALTER TABLE dbo.SANPHAM ADD Revision rowversion;
DENY INSERT,UPDATE,DELETE ON dbo.SANPHAM TO CafeManagerApp;
DENY INSERT,UPDATE,DELETE ON dbo.CONGTHUC TO CafeManagerApp;
GO
CREATE TYPE dbo.RecipeLineInput AS TABLE(MaNVL varchar(100) NOT NULL,Quantity decimal(38,18) NOT NULL);
GO
CREATE PROCEDURE dbo.sp_ProductList @Actor int,@Stamp binary(8),@Search nvarchar(max)=N'',@Skip int=0,@Category varchar(max)=NULL,@OnlySelling bit=0 AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH,STORE';
 IF @Skip<0 OR LEN(@Search)>100 THROW 51701,N'Bộ lọc không hợp lệ.',1;
 IF EXISTS(SELECT 1 FROM dbo.TAIKHOAN WHERE MaTK=@Actor AND MaVT='CASH') SET @OnlySelling=1;
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 SELECT MaSP,TenSP,MaDM,GiaBan,TrangThai,Revision FROM dbo.SANPHAM s
 WHERE (TenSP LIKE N'%'+@Search+N'%' ESCAPE N'~' OR MaSP LIKE N'%'+@Search+N'%' ESCAPE N'~') AND (@Category IS NULL OR MaDM=@Category)
 AND (@OnlySelling=0 OR (TrangThai=N'Dang ban' AND EXISTS(SELECT 1 FROM dbo.CONGTHUC c WHERE c.MaSP=s.MaSP)))
 ORDER BY MaSP OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_ProductGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH,STORE';
 SELECT MaSP,TenSP,MaDM,GiaBan,TrangThai,Revision FROM dbo.SANPHAM WHERE MaSP=@Code;
END;
GO
CREATE PROCEDURE dbo.sp_RecipeList @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,STORE';
 SELECT c.MaNVL,n.TenNVL,n.DonViTinh,c.DinhMuc FROM dbo.CONGTHUC c JOIN dbo.NGUYENVATLIEU n ON n.MaNVL=c.MaNVL WHERE c.MaSP=@Code ORDER BY c.MaNVL;
END;
GO
CREATE PROCEDURE dbo.sp_ProductWrite @Actor int,@Stamp binary(8),@Action varchar(20),@Code varchar(max),@RequestId uniqueidentifier,
 @Recipe dbo.RecipeLineInput READONLY,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Category varchar(max)=NULL,@Price decimal(38,18)=NULL,@Status nvarchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
  IF @Action NOT IN('Create','Update','Delete','ReplaceRecipe') THROW 51701,N'Thao tác không hợp lệ.',1;
  SET @Code=UPPER(LTRIM(RTRIM(@Code))); SET @Name=LTRIM(RTRIM(@Name));
  IF @Code IS NULL OR DATALENGTH(@Code)<>5 OR @Code COLLATE Latin1_General_100_BIN2 NOT LIKE 'SP[0-9][0-9][0-9]' OR @Code='SP000' THROW 51701,N'Mã sản phẩm từ SP001 đến SP999.',1;
  IF @Action IN('Create','Update') AND (@Name IS NULL OR LEN(@Name)=0 OR DATALENGTH(@Name)>100 OR @Name LIKE N'%'+NCHAR(9)+N'%' OR @Name LIKE N'%'+NCHAR(10)+N'%' OR @Name LIKE N'%'+NCHAR(13)+N'%'
   OR @Price IS NULL OR @Price<=0 OR @Price>999999999999999999 OR ROUND(@Price,0)<>@Price OR @Status IS NULL OR @Status NOT IN(N'Dang ban',N'Ngung ban') OR NOT EXISTS(SELECT 1 FROM dbo.DANHMUC_SP WHERE MaDM=@Category)) THROW 51701,N'Thông tin sản phẩm không hợp lệ.',1;
  IF EXISTS(SELECT 1 FROM @Recipe WHERE DATALENGTH(MaNVL)<>5 OR Quantity<=0 OR Quantity>99999999999999.9999 OR ROUND(Quantity,4)<>Quantity)
   OR EXISTS(SELECT MaNVL FROM @Recipe GROUP BY MaNVL HAVING COUNT(*)>1)
   OR EXISTS(SELECT 1 FROM @Recipe r WHERE NOT EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU n WHERE n.MaNVL=r.MaNVL)) THROW 51701,N'Dòng công thức không hợp lệ hoặc trùng.',1;
  DECLARE @lines nvarchar(max)=(SELECT MaNVL,Quantity FROM @Recipe ORDER BY MaNVL FOR JSON PATH);
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@Name n,@Category cat,@Price p,@Status s,@ExpectedRevision r,@lines lines FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
  DECLARE @hash binary(32)=HASHBYTES('SHA2_256',CONVERT(varbinary(max),@payload)),@resultCode varchar(10)=@Code,@revision binary(8),@replay bit,@operation varchar(40)='Product'+@Action,@lock int;
  EXEC dbo.sp_G03BeginReceipt @Actor,@operation,@RequestId,@hash,@resultCode OUTPUT,@revision OUTPUT,@replay OUTPUT;
  IF @replay=0
  BEGIN
   EXEC @lock=sys.sp_getapplock @Resource=N'CafeManager:Recipes',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
   IF @lock<0 THROW 51704,N'Công thức đang được sử dụng; thử lại sau đối chiếu.',1;
   IF @Action='Create'
    INSERT dbo.SANPHAM(MaSP,TenSP,MaDM,GiaBan,TrangThai) VALUES(@Code,@Name,@Category,CONVERT(decimal(18,0),@Price),@Status);
   ELSE
   BEGIN
    IF NOT EXISTS(SELECT 1 FROM dbo.SANPHAM WITH(UPDLOCK,HOLDLOCK) WHERE MaSP=@Code AND Revision=@ExpectedRevision) THROW 51603,N'Sản phẩm đã thay đổi.',1;
    IF @Action='Update' UPDATE dbo.SANPHAM SET TenSP=@Name,MaDM=@Category,GiaBan=CONVERT(decimal(18,0),@Price),TrangThai=@Status WHERE MaSP=@Code;
    IF @Action='Delete'
    BEGIN
     IF EXISTS(SELECT 1 FROM dbo.CHITIET_HD WITH(UPDLOCK,HOLDLOCK) WHERE MaSP=@Code) THROW 51702,N'Sản phẩm có lịch sử bán; không thể xóa.',1;
     DELETE dbo.CONGTHUC WHERE MaSP=@Code; DELETE dbo.SANPHAM WHERE MaSP=@Code;
    END;
   END;
   IF @Action IN('Create','ReplaceRecipe')
   BEGIN
    DELETE dbo.CONGTHUC WHERE MaSP=@Code;
    INSERT dbo.CONGTHUC(MaSP,MaNVL,DinhMuc) SELECT @Code,MaNVL,CONVERT(decimal(18,4),Quantity) FROM @Recipe;
    UPDATE dbo.SANPHAM SET TenSP=TenSP WHERE MaSP=@Code;
   END;
   IF EXISTS(SELECT 1 FROM dbo.SANPHAM WHERE MaSP=@Code AND TrangThai=N'Dang ban') AND NOT EXISTS(SELECT 1 FROM dbo.CONGTHUC WHERE MaSP=@Code) THROW 51702,N'Không bật bán món chưa có công thức.',1;
   SELECT @revision=Revision FROM dbo.SANPHAM WHERE MaSP=@Code;
   INSERT dbo.G03OperationReceipt(RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,ResultRevision) VALUES(@RequestId,@Actor,@operation,@Code,@hash,@revision);
  END;
  COMMIT; SELECT @resultCode Code,@revision Revision;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_ProductCreate @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @Recipe dbo.RecipeLineInput READONLY,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Category varchar(max)=NULL,@Price decimal(38,18)=NULL,@Status nvarchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_ProductWrite @Actor,@Stamp,'Create',@Code,@RequestId,@Recipe,@ExpectedRevision,@Name,@Category,@Price,@Status; END;
GO
GRANT EXECUTE ON dbo.sp_ProductCreate TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_ProductUpdate @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @Recipe dbo.RecipeLineInput READONLY,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Category varchar(max)=NULL,@Price decimal(38,18)=NULL,@Status nvarchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_ProductWrite @Actor,@Stamp,'Update',@Code,@RequestId,@Recipe,@ExpectedRevision,@Name,@Category,@Price,@Status; END;
GO
GRANT EXECUTE ON dbo.sp_ProductUpdate TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_ProductDelete @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @Recipe dbo.RecipeLineInput READONLY,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Category varchar(max)=NULL,@Price decimal(38,18)=NULL,@Status nvarchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_ProductWrite @Actor,@Stamp,'Delete',@Code,@RequestId,@Recipe,@ExpectedRevision,@Name,@Category,@Price,@Status; END;
GO
GRANT EXECUTE ON dbo.sp_ProductDelete TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_ProductReplaceRecipe @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @Recipe dbo.RecipeLineInput READONLY,@ExpectedRevision binary(8)=NULL,@Name nvarchar(max)=NULL,@Category varchar(max)=NULL,@Price decimal(38,18)=NULL,@Status nvarchar(max)=NULL AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_ProductWrite @Actor,@Stamp,'ReplaceRecipe',@Code,@RequestId,@Recipe,@ExpectedRevision,@Name,@Category,@Price,@Status; END;
GO
GRANT EXECUTE ON dbo.sp_ProductReplaceRecipe TO CafeManagerApp;
GO
ALTER PROCEDURE dbo.sp_ThanhToanHoaDon
    @MaHD CHAR(10), @ThanhToan dbo.DanhSachThanhToan READONLY,
    @NgayTT DATETIME2(0)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @recipeLock int;
        EXEC @recipeLock=sys.sp_getapplock @Resource=N'CafeManager:Recipes',@LockMode='Shared',@LockOwner='Transaction',@LockTimeout=10000;
        IF @recipeLock<0 THROW 51704,N'Công thức đang được cập nhật; đối chiếu trước thử lại.',1;
        DECLARE @TrangThai NVARCHAR(30),@Tong DECIMAL(18,0),@Giam DECIMAL(18,0),
                @PhaiTra DECIMAL(18,0),@MaKH CHAR(5),@MaDT CHAR(5),@MaKM CHAR(10),@Ngay DATE;
        SELECT @TrangThai=TrangThai,@Tong=TongTien,@Giam=GiamGia,@PhaiTra=ThanhTien,
               @MaKH=MaKH,@MaDT=MaDT,@MaKM=MaKM,@Ngay=CONVERT(DATE,NgayLap)
        FROM HOADON WITH (UPDLOCK,HOLDLOCK) WHERE MaHD=@MaHD;
        IF @TrangThai IS NULL OR @TrangThai<>N'Chua thanh toan'
            THROW 51101,N'Hoa don khong ton tai hoac da chot/huy.',1;
        IF @Tong<=0 THROW 51102,N'Hoa don phai co mon.',1;
        IF EXISTS (SELECT 1 FROM CHITIET_THANHTOAN WHERE MaHD=@MaHD)
            THROW 51103,N'Thu tuc nhan toan bo phan bo TT; don nay da co dong TT.',1;
        IF @PhaiTra<>COALESCE((SELECT SUM(SoTien) FROM @ThanhToan),0)
            THROW 51104,N'Tong phan bo thanh toan khong bang ThanhTien.',1;
        IF @MaKM IS NOT NULL AND NOT EXISTS
           (SELECT 1 FROM KHUYENMAI WHERE MaKM=@MaKM AND @Ngay BETWEEN NgayBD AND NgayKT)
            THROW 51105,N'Khuyen mai khong con hieu luc tai ngay lap don.',1;
        IF EXISTS (SELECT 1 FROM CHITIET_HD d JOIN SANPHAM s ON s.MaSP=d.MaSP
                   WHERE d.MaHD=@MaHD AND s.TrangThai<>N'Dang ban')
            THROW 51106,N'Hoa don co mon da ngung ban.',1;
        IF EXISTS (SELECT 1 FROM CHITIET_HD d WHERE d.MaHD=@MaHD
                   AND NOT EXISTS (SELECT 1 FROM CONGTHUC c WHERE c.MaSP=d.MaSP))
            THROW 51107,N'Mon chua co cong thuc. Hang ban san can nguyen lieu thanh pham.',1;
        SET @NgayTT=COALESCE(@NgayTT,SYSDATETIME());
        IF @NgayTT < (SELECT NgayLap FROM HOADON WHERE MaHD=@MaHD)
            THROW 51108,N'Ngay thanh toan khong duoc truoc ngay lap.',1;
        INSERT INTO TIEUHAO_HOADON(MaHD,MaNVL,SoLuong)
        SELECT @MaHD,c.MaNVL,SUM(d.SoLuong*c.DinhMuc)
        FROM CHITIET_HD d JOIN CONGTHUC c ON c.MaSP=d.MaSP
        WHERE d.MaHD=@MaHD GROUP BY c.MaNVL;
        INSERT INTO CHITIET_THANHTOAN(MaHD,MaPT,SoTien,NgayTT,MaThamChieu)
        SELECT @MaHD,MaPT,SoTien,@NgayTT,MaThamChieu FROM @ThanhToan;
        DECLARE @Phi DECIMAL(5,2)=0;
        IF @MaDT IS NOT NULL SELECT @Phi=ChietKhau FROM DOITACGIAOHANG WHERE MaDT=@MaDT;
        UPDATE HOADON SET TrangThai=N'Da thanh toan',NgayThanhToan=@NgayTT,
               PhanTramPhiDoiTac=@Phi,PhiDoiTac=ROUND((@Tong-@Giam)*@Phi/100,0)
        WHERE MaHD=@MaHD;
        DECLARE @Diem INT=CONVERT(INT,FLOOR((@Tong-@Giam)/1000));
        IF @MaKH IS NOT NULL AND @Diem>0
            INSERT INTO LICHSU_DIEM(MaKH,MaHD,NgayGD,Loai,SoDiem,LyDo)
            VALUES(@MaKH,@MaHD,@NgayTT,N'Cong',@Diem,N'Tich diem mua hang');
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

GRANT EXECUTE ON dbo.sp_ProductList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_ProductGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_RecipeList TO CafeManagerApp;
GRANT EXECUTE,REFERENCES ON TYPE::dbo.RecipeLineInput TO CafeManagerApp;

