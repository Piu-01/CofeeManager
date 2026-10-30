-- G04 technical hardening after014; no financial data rewrite.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=14) THROW 52107,N'Migration014 bắt buộc.',1;
GO
ALTER PROCEDURE dbo.sp_SalesReadCore @Code varchar(max) AS
BEGIN
 SET NOCOUNT ON;
 SELECT h.MaHD,h.NgayLap,h.MaNV,h.MaKH,h.MaBan,h.MaDT,h.MaKM,h.LoaiDon,h.TenNguoiNhan,h.SDTNguoiNhan,h.DiaChiGiao,h.TrangThaiGiao,h.NgayThanhToan,h.PhanTramPhiDoiTac,h.PhiDoiTac,h.TongTien,h.PhiShip,
 CASE WHEN h.TrangThai=N'Chua thanh toan' AND v.Valid=1 THEN p.Discount ELSE h.GiamGia END GiamGia,
 CASE WHEN h.TrangThai=N'Chua thanh toan' AND v.Valid=1 THEN p.Payable ELSE CONVERT(decimal(38,0),h.ThanhTien) END Payable,h.TrangThai,h.Revision,v.Valid PreviewValid,
 CASE WHEN h.TrangThai=N'Chua thanh toan' AND v.Valid=1 THEN p.Source ELSE snapshot.Source END PreviewSource,CASE WHEN h.TrangThai=N'Chua thanh toan' AND v.Valid=1 THEN p.PercentDiscount ELSE snapshot.PercentDiscount END PreviewPercent,SYSDATETIME() ReadAt
 FROM dbo.HOADON h WITH(HOLDLOCK) LEFT JOIN dbo.HoaDonUuDaiSnapshot snapshot ON snapshot.MaHD=h.MaHD
 CROSS APPLY dbo.fn_DiscountPreview(CASE WHEN h.TrangThai=N'Chua thanh toan' THEN h.TongTien ELSE 0 END,CASE WHEN h.TrangThai=N'Chua thanh toan' THEN h.PhiShip ELSE 0 END,CONVERT(date,h.NgayLap),CASE WHEN h.TrangThai=N'Chua thanh toan' THEN h.MaKH END,CASE WHEN h.TrangThai=N'Chua thanh toan' THEN h.MaKM END) p
 CROSS APPLY(SELECT CONVERT(bit,CASE WHEN h.MaKM IS NULL OR EXISTS(SELECT 1 FROM dbo.KHUYENMAI k WHERE k.MaKM=h.MaKM AND CONVERT(date,h.NgayLap) BETWEEN k.NgayBD AND k.NgayKT) THEN 1 ELSE 0 END) Valid) v WHERE h.MaHD=@Code;
 SELECT d.MaDong,d.MaSP,s.TenSP,d.SoLuong,d.GiaBan,d.GhiChu,d.SoLuong*d.GiaBan Amount FROM dbo.CHITIET_HD d JOIN dbo.SANPHAM s ON s.MaSP=d.MaSP WHERE d.MaHD=@Code ORDER BY d.MaDong;
 SELECT t.MaTT,t.MaPT,p.TenPT,t.SoTien,t.MaThamChieu FROM dbo.CHITIET_THANHTOAN t JOIN dbo.PHUONGTHUCTT p ON p.MaPT=t.MaPT WHERE t.MaHD=@Code ORDER BY t.MaTT;
END;

GO
ALTER PROCEDURE dbo.sp_TaoHoaDon @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,@Channel nvarchar(max),@TableCode varchar(max)=NULL,@Customer varchar(max)=NULL,@Promotion varchar(max)=NULL,@Partner varchar(max)=NULL,@Recipient nvarchar(max)=NULL,@Phone varchar(max)=NULL,@Address nvarchar(max)=NULL,@Ship decimal(38,18)=0,@Product varchar(max),@Quantity bigint,@Note nvarchar(max)=NULL AS
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
    IF EXISTS(SELECT 1 FROM dbo.HOADON WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode AND TrangThai=N'Chua thanh toan') THROW 52104,N'Bàn đã có đơn. Tải đơn hiện tại trước khi thêm món.',1;
   END;
   IF @Quantity IS NULL OR @Quantity NOT BETWEEN 1 AND 2147483647 OR DATALENGTH(@Product)>5 OR DATALENGTH(@Note)>200 THROW 52101,N'Số lượng/ghi chú/mã món không hợp lệ.',1;
   DECLARE @price decimal(18,0); SELECT @price=GiaBan FROM dbo.SANPHAM WITH(HOLDLOCK) WHERE MaSP=@Product AND TrangThai=N'Dang ban' AND EXISTS(SELECT 1 FROM dbo.CONGTHUC WHERE MaSP=@Product);
   IF @price IS NULL THROW 52105,N'Món ngừng bán hoặc chưa có công thức.',1;
   IF CONVERT(decimal(38,0),@price)*@Quantity>999999999999999999 THROW 52101,N'Tổng tiền vượt giới hạn.',1;
   INSERT dbo.HOADON(MaHD,MaNV,MaKH,MaBan,MaDT,MaKM,LoaiDon,TenNguoiNhan,SDTNguoiNhan,DiaChiGiao,TrangThaiGiao,PhiShip)
   SELECT @Code,MaNV,@Customer,@TableCode,@Partner,@Promotion,@Channel,@Recipient,@Phone,@Address,CASE WHEN @Channel=N'Giao hang' THEN N'Cho giao' END,CONVERT(decimal(18,0),@Ship) FROM dbo.TAIKHOAN WHERE MaTK=@Actor;
   INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan,GhiChu) VALUES(@Code,@Product,CONVERT(int,@Quantity),@price,@Note);
   EXEC dbo.sp_SalesRefreshDiscount @Code;
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesCreate',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;

GO
ALTER PROCEDURE dbo.sp_SuaThongTinHoaDon @Actor int,@Stamp binary(8),@Code varchar(max),@ExpectedRevision binary(8),@RequestId uniqueidentifier,@Customer varchar(max)=NULL,@Promotion varchar(max)=NULL,@Partner varchar(max)=NULL,@Recipient nvarchar(max)=NULL,@Phone varchar(max)=NULL,@Address nvarchar(max)=NULL,@Ship decimal(38,18)=0 AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  SELECT @Recipient=NULLIF(LTRIM(RTRIM(@Recipient)),N''),@Phone=NULLIF(LTRIM(RTRIM(@Phone)),''),@Address=NULLIF(LTRIM(RTRIM(@Address)),N'');
  SELECT @Customer=NULLIF(LTRIM(RTRIM(@Customer)),''),@Promotion=NULLIF(LTRIM(RTRIM(@Promotion)),''),@Partner=NULLIF(LTRIM(RTRIM(@Partner)),'');
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@ExpectedRevision r,@Customer kh,@Promotion km,@Partner dt,@Recipient n,@Phone ph,@Address a,@Ship ship FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit;
  EXEC dbo.sp_SalesBegin @Actor,@Stamp,@RequestId,'SalesHeader',@Code,@payload,@replay OUTPUT;
  IF @replay=0
  BEGIN
   EXEC dbo.sp_SalesLock N'CafeManager:LoyaltyPolicy','Shared';
   DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource;
   EXEC dbo.sp_SalesCheckOpen @Code,@ExpectedRevision;
   DECLARE @channel nvarchar(20),@tableCode varchar(5),@date date,@delivery nvarchar(20);
   SELECT @channel=LoaiDon,@tableCode=MaBan,@date=CONVERT(date,NgayLap),@delivery=TrangThaiGiao FROM dbo.HOADON WHERE MaHD=@Code;
   IF @channel=N'Giao hang' AND @delivery<>N'Cho giao' THROW 52102,N'Đã bắt đầu giao, không sửa thông tin đơn.',1;
   EXEC dbo.sp_SalesValidateHeader @channel,@tableCode,@Customer,@Promotion,@Partner,@Recipient,@Phone,@Address,@Ship,@date;
   UPDATE dbo.HOADON SET MaKH=@Customer,MaKM=@Promotion,MaDT=@Partner,TenNguoiNhan=@Recipient,SDTNguoiNhan=@Phone,DiaChiGiao=@Address,PhiShip=CONVERT(decimal(18,0),@Ship),GiamGia=0 WHERE MaHD=@Code;
   EXEC dbo.sp_SalesRefreshDiscount @Code;
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesHeader',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;

GO
