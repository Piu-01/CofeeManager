-- G04 T34: phiên bàn (PHIEN_BAN) và trả bàn. Phiên do SQL sở hữu; app không ghi trực tiếp.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=32)
 THROW 52107,N'Cần schema032 (T33) và runner migration.',1;
IF OBJECT_ID(N'dbo.PHIEN_BAN',N'U') IS NOT NULL
 THROW 52107,N'PHIEN_BAN đã tồn tại; cần đối chiếu trước nâng cấp.',1;
-- Không backfill: đơn cũ MaPhien NULL vẫn làm bàn bận qua view.
IF EXISTS(SELECT 1 FROM dbo.HOADON h WITH(TABLOCKX,HOLDLOCK)
 WHERE h.MaBan IS NOT NULL AND h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan')
 GROUP BY h.MaBan HAVING COUNT(*)>1)
 THROW 52107,N'Có bàn mở nhiều đơn; cần đối chiếu trước nâng cấp.',1;
GO
CREATE TABLE dbo.PHIEN_BAN(
 MaPhien int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PHIEN_BAN PRIMARY KEY,
 MaBan char(5) NOT NULL CONSTRAINT FK_PHIEN_BAN_BAN REFERENCES dbo.BAN(MaBan),
 GioVao datetime2(0) NOT NULL CONSTRAINT DF_PHIEN_BAN_GioVao DEFAULT SYSDATETIME(),
 GioRa datetime2(0) NULL,
 MaNVMo char(5) NOT NULL CONSTRAINT FK_PHIEN_BAN_Mo REFERENCES dbo.NHANVIEN(MaNV),
 MaNVTra char(5) NULL CONSTRAINT FK_PHIEN_BAN_Tra REFERENCES dbo.NHANVIEN(MaNV),
 Revision rowversion NOT NULL,
 CONSTRAINT CK_PHIEN_BAN_Gio CHECK (GioRa IS NULL OR GioRa>=GioVao));
CREATE UNIQUE INDEX UX_PHIEN_BAN_Mo ON dbo.PHIEN_BAN(MaBan) WHERE GioRa IS NULL;
DENY INSERT,UPDATE,DELETE ON dbo.PHIEN_BAN TO CafeManagerApp;
GO
ALTER TABLE dbo.HOADON ADD MaPhien int NULL CONSTRAINT FK_HD_Phien REFERENCES dbo.PHIEN_BAN(MaPhien);
CREATE INDEX IX_HD_Phien ON dbo.HOADON(MaPhien);
GO
ALTER VIEW dbo.vw_TrangThaiBan AS
SELECT b.MaBan,b.TenBan,b.MaKV,
       CASE WHEN o.MaHD IS NULL THEN CASE WHEN s.MaPhien IS NULL THEN N'Trong' ELSE N'Dang phuc vu' END
            WHEN o.TrangThai=N'Cho thanh toan' THEN N'Cho thanh toan'
            ELSE N'Dang phuc vu' END AS TrangThai,
       o.MaHD,
       s.MaPhien,
       s.Revision SessionRevision,
       CONVERT(bit,CASE WHEN o.MaHD IS NULL AND s.MaPhien IS NOT NULL THEN 1 ELSE 0 END) Retained
FROM dbo.BAN b
OUTER APPLY(SELECT TOP(1) h.MaHD,h.TrangThai FROM dbo.HOADON h
 WHERE h.MaBan=b.MaBan AND h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan')
 ORDER BY CASE WHEN h.TrangThai=N'Chua thanh toan' THEN 0 ELSE 1 END,h.NgayLap DESC,h.MaHD DESC) o
OUTER APPLY(SELECT TOP(1) p.MaPhien,p.Revision FROM dbo.PHIEN_BAN p
 WHERE p.MaBan=b.MaBan AND p.GioRa IS NULL) s;
GO
ALTER PROCEDURE dbo.sp_TableList @Actor int,@Stamp binary(8),@Search nvarchar(max)=N'',@Skip int=0,@Area varchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 IF LEN(@Search)>100 OR @Skip<0 THROW 51801,N'Bộ lọc không hợp lệ.',1;
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 SELECT b.MaBan Code,b.TenBan Name,b.MaKV Area,v.TrangThai Status,v.MaHD OrderCode,b.Revision,SYSDATETIME() ReadAt,
  v.MaPhien SessionCode,v.SessionRevision,v.Retained
 FROM dbo.BAN b JOIN dbo.vw_TrangThaiBan v ON v.MaBan=b.MaBan
 WHERE (@Area IS NULL OR b.MaKV=@Area) AND (b.TenBan LIKE N'%'+@Search+N'%' ESCAPE N'~' OR b.MaBan LIKE N'%'+@Search+N'%' ESCAPE N'~')
 ORDER BY b.MaBan OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
ALTER PROCEDURE dbo.sp_TableSelectCurrent @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 SELECT b.MaBan Code,b.TenBan Name,b.MaKV Area,v.TrangThai Status,v.MaHD OrderCode,b.Revision,SYSDATETIME() ReadAt,
  v.MaPhien SessionCode,v.SessionRevision,v.Retained
 FROM dbo.BAN b JOIN dbo.vw_TrangThaiBan v ON v.MaBan=b.MaBan WHERE b.MaBan=@Code;
END;
GO
ALTER PROCEDURE dbo.sp_TaoHoaDon @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,@Channel nvarchar(max),@TableCode varchar(max)=NULL,@Customer varchar(max)=NULL,@Promotion varchar(max)=NULL,@Partner varchar(max)=NULL,@Recipient nvarchar(max)=NULL,@Phone varchar(max)=NULL,@Address varchar(max)=NULL,@Ship decimal(38,18)=0,@Product varchar(max),@Quantity bigint,@Note nvarchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  IF @Code IS NULL OR DATALENGTH(@Code)<>10 OR @Code COLLATE Latin1_General_100_BIN2 NOT LIKE 'HD[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' THROW 52101,N'Mã hóa đơn không hợp lệ.',1;
  SELECT @Recipient=NULLIF(LTRIM(RTRIM(@Recipient)),N''),@Phone=NULLIF(LTRIM(RTRIM(@Phone)),''),@Address=NULLIF(LTRIM(RTRIM(@Address)),N''),@Note=NULLIF(LTRIM(RTRIM(@Note)),N'');
  SELECT @Customer=NULLIF(LTRIM(RTRIM(@Customer)),''),@Promotion=NULLIF(LTRIM(RTRIM(@Promotion)),''),@Partner=NULLIF(LTRIM(RTRIM(@Partner)),'');
  SET @TableCode=NULLIF(LTRIM(RTRIM(@TableCode)),'');
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@Channel ch,@TableCode b,@Customer kh,@Promotion km,@Partner dt,@Recipient n,@Phone ph,@Address a,@Ship ship,@Product sp,@Quantity q,@Note note FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit;
  EXEC dbo.sp_SalesBegin @Actor,@Stamp,@RequestId,'SalesCreate',@Code,@payload,@replay OUTPUT;
  IF @replay=0
  BEGIN
   EXEC dbo.sp_SalesLock N'CafeManager:LoyaltyPolicy','Shared'; EXEC dbo.sp_SalesLock N'CafeManager:Recipes','Shared';
   DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource;
   DECLARE @date date=CONVERT(date,SYSDATETIME());
   EXEC dbo.sp_SalesValidateHeader @Channel,@TableCode,@Customer,@Promotion,@Partner,@Recipient,@Phone,@Address,@Ship,@date;
   IF @TableCode IS NOT NULL
   BEGIN
    DECLARE @lockedTable char(5); SELECT @lockedTable=MaBan FROM dbo.BAN WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode;
    IF EXISTS(SELECT 1 FROM dbo.HOADON WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode AND TrangThai IN (N'Chua thanh toan',N'Cho thanh toan')) THROW 52104,N'Bàn đã có đơn. Tải đơn hiện tại trước khi thêm món.',1;
   END;
   DECLARE @session int;
   IF @TableCode IS NOT NULL
   BEGIN
    SELECT @session=MaPhien FROM dbo.PHIEN_BAN WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode AND GioRa IS NULL;
    IF @session IS NULL
    BEGIN
     INSERT dbo.PHIEN_BAN(MaBan,MaNVMo) SELECT @TableCode,MaNV FROM dbo.TAIKHOAN WHERE MaTK=@Actor;
     SET @session=SCOPE_IDENTITY();
    END;
   END;
   IF @Quantity IS NULL OR @Quantity NOT BETWEEN 1 AND 2147483647 OR DATALENGTH(@Product)>5 OR DATALENGTH(@Note)>200 THROW 52101,N'Số lượng/ghi chú/mã món không hợp lệ.',1;
   DECLARE @price decimal(18,0); SELECT @price=GiaBan FROM dbo.SANPHAM WITH(HOLDLOCK) WHERE MaSP=@Product AND TrangThai=N'Dang ban' AND EXISTS(SELECT 1 FROM dbo.CONGTHUC WHERE MaSP=@Product);
   IF @price IS NULL THROW 52105,N'Món ngừng bán hoặc chưa có công thức.',1;
   IF CONVERT(decimal(38,0),@price)*@Quantity>999999999999999999 THROW 52101,N'Tổng tiền vượt giới hạn.',1;
   INSERT dbo.HOADON(MaHD,MaNV,MaKH,MaBan,MaDT,MaKM,LoaiDon,TenNguoiNhan,SDTNguoiNhan,DiaChiGiao,TrangThaiGiao,PhiShip,MaPhien)
   SELECT @Code,MaNV,@Customer,@TableCode,@Partner,@Promotion,@Channel,@Recipient,@Phone,@Address,CASE WHEN @Channel=N'Giao hang' THEN N'Cho giao' END,CONVERT(decimal(18,0),@Ship),@session FROM dbo.TAIKHOAN WHERE MaTK=@Actor;
   INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan,GhiChu) VALUES(@Code,@Product,CONVERT(int,@Quantity),@price,@Note);
   EXEC dbo.sp_SalesRefreshDiscount @Code;
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesCreate',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
ALTER PROCEDURE dbo.sp_TaoHoaDonTuNhap
 @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @Channel nvarchar(max),@TableCode varchar(max)=NULL,@Customer varchar(max)=NULL,@Promotion varchar(max)=NULL,
 @Partner varchar(max)=NULL,@Recipient nvarchar(max)=NULL,@Phone varchar(max)=NULL,@Address varchar(max)=NULL,
 @Ship decimal(38,18)=0,@Lines dbo.SalesDraftLines READONLY AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  IF @Code IS NULL OR DATALENGTH(@Code)<>10 OR @Code COLLATE Latin1_General_100_BIN2 NOT LIKE 'HD[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' THROW 52101,N'Mã hóa đơn không hợp lệ.',1;
  SELECT @Recipient=NULLIF(LTRIM(RTRIM(@Recipient)),N''),@Phone=NULLIF(LTRIM(RTRIM(@Phone)),''),@Address=NULLIF(LTRIM(RTRIM(@Address)),N'');
  DECLARE @lineJson nvarchar(max)=(SELECT RowNo,Product,Quantity,Note,Price FROM @Lines ORDER BY RowNo FOR JSON PATH,INCLUDE_NULL_VALUES);
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@Channel ch,@TableCode b,@Customer kh,@Promotion km,@Partner dt,@Recipient n,@Phone ph,@Address a,@Ship ship,JSON_QUERY(@lineJson) lines FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit;
  EXEC dbo.sp_SalesBegin @Actor,@Stamp,@RequestId,'SalesCreate',@Code,@payload,@replay OUTPUT;
  IF @replay=0
  BEGIN
   EXEC dbo.sp_SalesLock N'CafeManager:LoyaltyPolicy','Shared'; EXEC dbo.sp_SalesLock N'CafeManager:Recipes','Shared';
   DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource;
   DECLARE @date date=CONVERT(date,SYSDATETIME());
   EXEC dbo.sp_SalesValidateHeader @Channel,@TableCode,@Customer,@Promotion,@Partner,@Recipient,@Phone,@Address,@Ship,@date;
   IF @TableCode IS NOT NULL
   BEGIN
    DECLARE @lockedTable char(5); SELECT @lockedTable=MaBan FROM dbo.BAN WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode;
    IF EXISTS(SELECT 1 FROM dbo.HOADON WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode AND TrangThai IN (N'Chua thanh toan',N'Cho thanh toan')) THROW 52104,N'Bàn đã có đơn. Tải đơn hiện tại trước khi thêm món.',1;
   END;
   DECLARE @session int;
   IF @TableCode IS NOT NULL
   BEGIN
    SELECT @session=MaPhien FROM dbo.PHIEN_BAN WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode AND GioRa IS NULL;
    IF @session IS NULL
    BEGIN
     INSERT dbo.PHIEN_BAN(MaBan,MaNVMo) SELECT @TableCode,MaNV FROM dbo.TAIKHOAN WHERE MaTK=@Actor;
     SET @session=SCOPE_IDENTITY();
    END;
   END;
   IF NOT EXISTS(SELECT 1 FROM @Lines) THROW 52101,N'Đơn nháp không có dòng món.',1;
   IF EXISTS(SELECT 1 FROM @Lines WHERE Product IS NULL OR Quantity NOT BETWEEN 1 AND 2147483647 OR Price<0 OR Price>999999999999999999 OR (Note IS NOT NULL AND DATALENGTH(Note)>200))
    THROW 52101,N'Số lượng/ghi chú/mã món không hợp lệ.',1;
   IF EXISTS(SELECT 1 FROM @Lines l WHERE NOT EXISTS(SELECT 1 FROM dbo.SANPHAM s WITH(HOLDLOCK) WHERE s.MaSP=l.Product AND s.TrangThai=N'Dang ban' AND EXISTS(SELECT 1 FROM dbo.CONGTHUC c WHERE c.MaSP=l.Product)))
    THROW 52105,N'Món ngừng bán hoặc chưa có công thức.',1;
   IF EXISTS(SELECT 1 FROM @Lines l JOIN dbo.SANPHAM s ON s.MaSP=l.Product WHERE l.Price<>s.GiaBan)
    THROW 52105,N'Giá món đã thay đổi; tải lại menu và thêm lại dòng.',1;
   IF EXISTS(SELECT 1 FROM @Lines WHERE CONVERT(decimal(38,0),Price)*Quantity>999999999999999999)
    OR (SELECT SUM(CONVERT(decimal(38,0),Price)*Quantity) FROM @Lines)>999999999999999999
    THROW 52101,N'Tổng tiền vượt giới hạn.',1;
   INSERT dbo.HOADON(MaHD,MaNV,MaKH,MaBan,MaDT,MaKM,LoaiDon,TenNguoiNhan,SDTNguoiNhan,DiaChiGiao,TrangThaiGiao,PhiShip,MaPhien)
   SELECT @Code,MaNV,@Customer,@TableCode,@Partner,@Promotion,@Channel,@Recipient,@Phone,@Address,CASE WHEN @Channel=N'Giao hang' THEN N'Cho giao' END,CONVERT(decimal(18,0),@Ship),@session FROM dbo.TAIKHOAN WHERE MaTK=@Actor;
   INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan,GhiChu)
   SELECT @Code,Product,Quantity,Price,NULLIF(LTRIM(RTRIM(Note)),N'') FROM @Lines ORDER BY RowNo;
   EXEC dbo.sp_SalesRefreshDiscount @Code;
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesCreate',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END
GO
ALTER PROCEDURE dbo.sp_SalesReadCore @Code varchar(max) AS
BEGIN
 SET NOCOUNT ON;
 SELECT h.MaHD,h.NgayLap,h.MaNV,h.MaKH,h.MaBan,h.MaDT,h.MaKM,h.LoaiDon,h.TenNguoiNhan,h.SDTNguoiNhan,h.DiaChiGiao,h.TrangThaiGiao,h.NgayThanhToan,h.PhanTramPhiDoiTac,h.PhiDoiTac,h.TongTien,h.PhiShip,
 CASE WHEN h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan') AND v.Valid=1 THEN p.Discount ELSE h.GiamGia END GiamGia,
 CASE WHEN h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan') AND v.Valid=1 THEN p.Payable ELSE CONVERT(decimal(38,0),h.ThanhTien) END Payable,h.TrangThai,h.Revision,v.Valid PreviewValid,
 CASE WHEN h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan') AND v.Valid=1 THEN p.Source ELSE snapshot.Source END PreviewSource,CASE WHEN h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan') AND v.Valid=1 THEN p.PercentDiscount ELSE snapshot.PercentDiscount END PreviewPercent,SYSDATETIME() ReadAt,
 h.MaPhien,sessionRow.Revision SessionRevision
 FROM dbo.HOADON h WITH(HOLDLOCK) LEFT JOIN dbo.HoaDonUuDaiSnapshot snapshot ON snapshot.MaHD=h.MaHD
 CROSS APPLY dbo.fn_DiscountPreview(CASE WHEN h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan') THEN h.TongTien ELSE 0 END,CASE WHEN h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan') THEN h.PhiShip ELSE 0 END,CONVERT(date,h.NgayLap),CASE WHEN h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan') THEN h.MaKH END,CASE WHEN h.TrangThai IN (N'Chua thanh toan',N'Cho thanh toan') THEN h.MaKM END) p
 CROSS APPLY(SELECT CONVERT(bit,CASE WHEN h.MaKM IS NULL OR EXISTS(SELECT 1 FROM dbo.KHUYENMAI k WHERE k.MaKM=h.MaKM AND CONVERT(date,h.NgayLap) BETWEEN k.NgayBD AND k.NgayKT) THEN 1 ELSE 0 END) Valid) v
 OUTER APPLY(SELECT TOP(1) s.Revision FROM dbo.PHIEN_BAN s WHERE s.MaPhien=h.MaPhien) sessionRow
 WHERE h.MaHD=@Code;
 SELECT d.MaDong,d.MaSP,s.TenSP,d.SoLuong,d.GiaBan,d.GhiChu,d.SoLuong*d.GiaBan Amount FROM dbo.CHITIET_HD d JOIN dbo.SANPHAM s ON s.MaSP=d.MaSP WHERE d.MaHD=@Code ORDER BY d.MaDong;
 SELECT t.MaTT,t.MaPT,p.TenPT,t.SoTien,t.MaThamChieu FROM dbo.CHITIET_THANHTOAN t JOIN dbo.PHUONGTHUCTT p ON p.MaPT=t.MaPT WHERE t.MaHD=@Code ORDER BY t.MaTT;
END;
GO
CREATE PROCEDURE dbo.sp_TraBan @Actor int,@Stamp binary(8),@MaPhien int,@ExpectedRevision binary(8),@RequestId uniqueidentifier AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
  IF @MaPhien IS NULL OR @MaPhien<=0 THROW 52101,N'Phiên bàn không hợp lệ.',1;
  DECLARE @entity varchar(10)=CONVERT(varchar(10),@MaPhien);
  DECLARE @requestLock nvarchar(255)=N'CafeManager:SalesRequest:'+CONVERT(nvarchar(36),@RequestId),@sessionLock nvarchar(255)=N'CafeManager:TableSession:'+CONVERT(nvarchar(10),@MaPhien);
  DECLARE @payload nvarchar(max)=(SELECT @MaPhien p,@ExpectedRevision r FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit,@receiptRevision binary(8),@hash binary(32);
  SET @hash=HASHBYTES('SHA2_256',CONVERT(varbinary(max),@payload));
  EXEC dbo.sp_SalesLock @requestLock;
  EXEC dbo.sp_G03BeginReceipt @Actor,'TableClose',@RequestId,@hash,@entity OUTPUT,@receiptRevision OUTPUT,@replay OUTPUT;
  IF @replay=0
  BEGIN
   EXEC dbo.sp_SalesLock @sessionLock;
   IF NOT EXISTS(SELECT 1 FROM dbo.PHIEN_BAN WITH(UPDLOCK,HOLDLOCK) WHERE MaPhien=@MaPhien AND GioRa IS NULL)
    THROW 52111,N'Phiên bàn đã đóng hoặc không còn mở.',1;
   IF @ExpectedRevision IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.PHIEN_BAN WHERE MaPhien=@MaPhien AND Revision=@ExpectedRevision)
    THROW 52103,N'Phiên bàn đã thay đổi ở phiên khác. Tải lại trước khi tiếp tục.',1;
   IF EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaPhien=@MaPhien AND TrangThai IN (N'Chua thanh toan',N'Cho thanh toan'))
    THROW 52110,N'Bàn còn đơn chưa chốt; thu hoặc hủy trước khi trả bàn.',1;
   UPDATE dbo.PHIEN_BAN SET GioRa=SYSDATETIME(),MaNVTra=(SELECT MaNV FROM dbo.TAIKHOAN WHERE MaTK=@Actor) WHERE MaPhien=@MaPhien;
   INSERT dbo.G03OperationReceipt(RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,ResultRevision)
   SELECT @RequestId,@Actor,'TableClose',@entity,@hash,Revision FROM dbo.PHIEN_BAN WHERE MaPhien=@MaPhien;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
GRANT EXECUTE ON dbo.sp_TraBan TO CafeManagerApp;
GO
