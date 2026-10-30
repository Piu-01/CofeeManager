-- T14: runtime orders; apply using the versioned migration runner after 009.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=9) THROW 52107,N'Migration009 và transaction bắt buộc.',1;
IF EXISTS(SELECT 1 FROM dbo.HOADON h WITH(TABLOCKX,HOLDLOCK) WHERE h.TongTien<>COALESCE((SELECT SUM(CONVERT(decimal(38,0),SoLuong)*GiaBan) FROM dbo.CHITIET_HD WHERE MaHD=h.MaHD),0)
 OR h.GiamGia>h.TongTien OR (h.LoaiDon=N'Giao hang' AND (NULLIF(LTRIM(RTRIM(h.TenNguoiNhan)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(h.SDTNguoiNhan)),'') IS NULL OR NULLIF(LTRIM(RTRIM(h.DiaChiGiao)),N'') IS NULL)))
 THROW 52107,N'Hóa đơn cũ không hợp lệ; không tự sửa dữ liệu.',1;
ALTER TABLE dbo.HOADON ADD Revision rowversion;
DENY INSERT,UPDATE,DELETE ON dbo.CHITIET_HD TO CafeManagerApp;
GO
CREATE PROCEDURE dbo.sp_SalesLock @Resource nvarchar(255),@Mode varchar(32)='Exclusive' AS
BEGIN
 SET NOCOUNT ON;
 DECLARE @result int;
 EXEC @result=sys.sp_getapplock @Resource=@Resource,@LockMode=@Mode,@LockOwner='Transaction',@LockTimeout=10000;
 IF @result<0 THROW 52106,N'Đang xử lý ở phiên khác. Đối chiếu trước khi thử lại.',1;
END;
GO
CREATE PROCEDURE dbo.sp_SalesBegin @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Operation varchar(40),@Code varchar(10),@Payload nvarchar(max),@Replay bit OUTPUT AS
BEGIN
 SET NOCOUNT ON;
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 IF @RequestId IS NULL OR @Code IS NULL OR LEN(@Code)<>10 THROW 52101,N'Thiếu mã hóa đơn/yêu cầu.',1;
 DECLARE @resource nvarchar(255)=N'CafeManager:SalesRequest:'+CONVERT(nvarchar(36),@RequestId),@revision binary(8),@hash binary(32)=HASHBYTES('SHA2_256',CONVERT(varbinary(max),@Payload));
 EXEC dbo.sp_SalesLock @resource;
 EXEC dbo.sp_G03BeginReceipt @Actor,@Operation,@RequestId,@hash,@Code OUTPUT,@revision OUTPUT,@Replay OUTPUT;
END;
GO
CREATE PROCEDURE dbo.sp_SalesFinish @Actor int,@RequestId uniqueidentifier,@Operation varchar(40),@Code varchar(10),@Payload nvarchar(max) AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.G03OperationReceipt(RequestId,ActorAccountId,Operation,EntityCode,PayloadHash,ResultRevision)
 SELECT @RequestId,@Actor,@Operation,@Code,HASHBYTES('SHA2_256',CONVERT(varbinary(max),@Payload)),Revision FROM dbo.HOADON WHERE MaHD=@Code;
END;
GO
CREATE PROCEDURE dbo.sp_SalesCheckOpen @Code varchar(max),@ExpectedRevision binary(8) AS
BEGIN
 SET NOCOUNT ON;
 IF DATALENGTH(@Code)<>10 OR NOT EXISTS(SELECT 1 FROM dbo.HOADON WITH(UPDLOCK,HOLDLOCK) WHERE MaHD=@Code AND TrangThai=N'Chua thanh toan') THROW 52102,N'Đơn không có hoặc đã chốt/hủy.',1;
 IF @ExpectedRevision IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@Code AND Revision=@ExpectedRevision) THROW 52103,N'Đơn đã thay đổi ở phiên khác. Tải lại trước khi tiếp tục.',1;
 IF EXISTS(SELECT 1 FROM dbo.CHITIET_THANHTOAN WHERE MaHD=@Code) OR EXISTS(SELECT 1 FROM dbo.TIEUHAO_HOADON WHERE MaHD=@Code) THROW 52102,N'Đơn đã có tiền/tiêu hao; cần đối chiếu.',1;
END;
GO
CREATE PROCEDURE dbo.sp_SalesValidateHeader @Channel nvarchar(max),@TableCode varchar(max),@Customer varchar(max),@Promotion varchar(max),@Partner varchar(max),@Recipient nvarchar(max),@Phone varchar(max),@Address nvarchar(max),@Ship decimal(38,18),@BusinessDate date AS
BEGIN
 SET NOCOUNT ON;
 IF @Channel IS NULL OR @Channel NOT IN(N'Tai quan',N'Mang di',N'Giao hang') OR @Ship IS NULL OR @Ship<0 OR @Ship>999999999999999999 OR ROUND(@Ship,0)<>@Ship THROW 52101,N'Loại đơn/phí ship không hợp lệ.',1;
 IF (@TableCode IS NOT NULL AND (DATALENGTH(@TableCode)>5 OR NOT EXISTS(SELECT 1 FROM dbo.BAN WITH(HOLDLOCK) WHERE MaBan=@TableCode)))
 OR (@Customer IS NOT NULL AND (DATALENGTH(@Customer)>5 OR NOT EXISTS(SELECT 1 FROM dbo.KHACHHANG WITH(HOLDLOCK) WHERE MaKH=@Customer)))
 OR (@Partner IS NOT NULL AND (DATALENGTH(@Partner)>5 OR NOT EXISTS(SELECT 1 FROM dbo.DOITACGIAOHANG WITH(HOLDLOCK) WHERE MaDT=@Partner)))
 OR (@Promotion IS NOT NULL AND (DATALENGTH(@Promotion)>10 OR NOT EXISTS(SELECT 1 FROM dbo.KHUYENMAI WITH(HOLDLOCK) WHERE MaKM=@Promotion AND @BusinessDate BETWEEN NgayBD AND NgayKT))) THROW 52105,N'Bàn/khách/đối tác/chương trình không hợp lệ.',1;
 IF (@Channel=N'Tai quan' AND (@TableCode IS NULL OR @Partner IS NOT NULL)) OR (@Channel<>N'Tai quan' AND @TableCode IS NOT NULL)
 OR (@Channel<>N'Giao hang' AND (@Partner IS NOT NULL OR @Ship<>0 OR @Recipient IS NOT NULL OR @Phone IS NOT NULL OR @Address IS NOT NULL)) THROW 52101,N'Thông tin không phù hợp loại đơn.',1;
 IF @Channel=N'Giao hang' AND (@Recipient IS NULL OR LEN(@Recipient)=0 OR DATALENGTH(@Recipient)>200 OR @Phone IS NULL OR DATALENGTH(@Phone) NOT BETWEEN 1 AND 15 OR @Phone COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9]%' OR @Address IS NULL OR LEN(@Address)=0 OR DATALENGTH(@Address)>500) THROW 52101,N'Cần tên, số điện thoại và địa chỉ giao hợp lệ.',1;
END;
GO
CREATE PROCEDURE dbo.sp_SalesRefreshDiscount @Code char(10) AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM dbo.HOADON h CROSS APPLY dbo.fn_DiscountPreview(h.TongTien,h.PhiShip,CONVERT(date,h.NgayLap),h.MaKH,h.MaKM) p WHERE h.MaHD=@Code AND p.Payable>999999999999999999) THROW 52101,N'Tổng tiền vượt giới hạn.',1;
 UPDATE h SET GiamGia=p.Discount FROM dbo.HOADON h CROSS APPLY dbo.fn_DiscountPreview(h.TongTien,h.PhiShip,CONVERT(date,h.NgayLap),h.MaKH,h.MaKM) p WHERE h.MaHD=@Code;
END;
GO
-- Caller owns a short read transaction; shared header lock keeps all result sets consistent.
CREATE PROCEDURE dbo.sp_SalesReadCore @Code varchar(max) AS
BEGIN
 SET NOCOUNT ON;
 SELECT h.MaHD,h.NgayLap,h.MaNV,h.MaKH,h.MaBan,h.MaDT,h.MaKM,h.LoaiDon,h.TenNguoiNhan,h.SDTNguoiNhan,h.DiaChiGiao,h.TrangThaiGiao,h.NgayThanhToan,h.PhanTramPhiDoiTac,h.PhiDoiTac,h.TongTien,h.PhiShip,
 CASE WHEN h.TrangThai=N'Chua thanh toan' AND v.Valid=1 THEN p.Discount ELSE h.GiamGia END GiamGia,
 CASE WHEN h.TrangThai=N'Chua thanh toan' AND v.Valid=1 THEN p.Payable ELSE CONVERT(decimal(38,0),h.ThanhTien) END Payable,h.TrangThai,h.Revision,v.Valid PreviewValid,
 CASE WHEN h.TrangThai=N'Chua thanh toan' AND v.Valid=1 THEN p.Source END PreviewSource,CASE WHEN h.TrangThai=N'Chua thanh toan' AND v.Valid=1 THEN p.PercentDiscount END PreviewPercent,SYSDATETIME() ReadAt
 FROM dbo.HOADON h WITH(HOLDLOCK)
 CROSS APPLY dbo.fn_DiscountPreview(h.TongTien,h.PhiShip,CONVERT(date,h.NgayLap),h.MaKH,h.MaKM) p
 CROSS APPLY(SELECT CONVERT(bit,CASE WHEN h.MaKM IS NULL OR EXISTS(SELECT 1 FROM dbo.KHUYENMAI k WHERE k.MaKM=h.MaKM AND CONVERT(date,h.NgayLap) BETWEEN k.NgayBD AND k.NgayKT) THEN 1 ELSE 0 END) Valid) v WHERE h.MaHD=@Code;
 SELECT d.MaDong,d.MaSP,s.TenSP,d.SoLuong,d.GiaBan,d.GhiChu,d.SoLuong*d.GiaBan Amount FROM dbo.CHITIET_HD d JOIN dbo.SANPHAM s ON s.MaSP=d.MaSP WHERE d.MaHD=@Code ORDER BY d.MaDong;
 SELECT t.MaTT,t.MaPT,p.TenPT,t.SoTien,t.MaThamChieu FROM dbo.CHITIET_THANHTOAN t JOIN dbo.PHUONGTHUCTT p ON p.MaPT=t.MaPT WHERE t.MaHD=@Code ORDER BY t.MaTT;
END;
GO
CREATE PROCEDURE dbo.sp_SalesOrderGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH'; EXEC dbo.sp_SalesLock N'CafeManager:LoyaltyPolicy','Shared';
  DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource,'Shared';
  EXEC dbo.sp_SalesReadCore @Code; COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_TaoHoaDon @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,@Channel nvarchar(max),@TableCode varchar(max)=NULL,@Customer varchar(max)=NULL,@Promotion varchar(max)=NULL,@Partner varchar(max)=NULL,@Recipient nvarchar(max)=NULL,@Phone varchar(max)=NULL,@Address nvarchar(max)=NULL,@Ship decimal(38,18)=0,@Product varchar(max),@Quantity bigint,@Note nvarchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  IF @Code IS NULL OR DATALENGTH(@Code)<>10 OR @Code COLLATE Latin1_General_100_BIN2 NOT LIKE 'HD[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' THROW 52101,N'Mã hóa đơn không hợp lệ.',1;
  SELECT @Recipient=NULLIF(LTRIM(RTRIM(@Recipient)),N''),@Phone=NULLIF(LTRIM(RTRIM(@Phone)),''),@Address=NULLIF(LTRIM(RTRIM(@Address)),N''),@Note=NULLIF(LTRIM(RTRIM(@Note)),N'');
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
CREATE PROCEDURE dbo.sp_SuaDongHoaDon @Actor int,@Stamp binary(8),@Code varchar(max),@ExpectedRevision binary(8),@RequestId uniqueidentifier,@Action varchar(10),@LineId int=NULL,@Product varchar(max)=NULL,@Quantity bigint=NULL,@Note nvarchar(max)=NULL AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN; SET @Note=NULLIF(LTRIM(RTRIM(@Note)),N'');
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@ExpectedRevision r,@Action a,@LineId l,@Product p,@Quantity q,@Note note FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit;
  EXEC dbo.sp_SalesBegin @Actor,@Stamp,@RequestId,'SalesLine',@Code,@payload,@replay OUTPUT;
  IF @replay=0
  BEGIN
   EXEC dbo.sp_SalesLock N'CafeManager:LoyaltyPolicy','Shared'; EXEC dbo.sp_SalesLock N'CafeManager:Recipes','Shared';
   DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource;
   EXEC dbo.sp_SalesCheckOpen @Code,@ExpectedRevision;
   IF @Action IS NULL OR @Action NOT IN('Add','Update','Delete') OR DATALENGTH(@Note)>200 OR (@Action<>'Delete' AND (@Quantity IS NULL OR @Quantity NOT BETWEEN 1 AND 2147483647)) THROW 52101,N'Dòng món không hợp lệ.',1;
   DECLARE @price decimal(18,0),@oldQuantity int=0;
   IF @Action<>'Add'
   BEGIN
    SELECT @Product=MaSP,@price=GiaBan,@oldQuantity=SoLuong FROM dbo.CHITIET_HD WHERE MaHD=@Code AND MaDong=@LineId;
    IF @price IS NULL THROW 52101,N'Dòng không thuộc hóa đơn.',1;
   END;
   IF @Action='Add' OR (@Action='Update' AND @Quantity>@oldQuantity)
   BEGIN
    IF DATALENGTH(@Product)>5 OR NOT EXISTS(SELECT 1 FROM dbo.SANPHAM WITH(HOLDLOCK) WHERE MaSP=@Product AND TrangThai=N'Dang ban' AND EXISTS(SELECT 1 FROM dbo.CONGTHUC WHERE MaSP=@Product)) THROW 52105,N'Món ngừng bán hoặc chưa có công thức.',1;
    IF @Action='Add' SELECT @price=GiaBan FROM dbo.SANPHAM WHERE MaSP=@Product;
   END;
   IF @Action<>'Delete' AND (SELECT CONVERT(decimal(38,0),TongTien)-CONVERT(decimal(38,0),@oldQuantity)*COALESCE(@price,0)+CONVERT(decimal(38,0),@Quantity)*@price FROM dbo.HOADON WHERE MaHD=@Code)>999999999999999999 THROW 52101,N'Tổng tiền vượt giới hạn.',1;
   UPDATE dbo.HOADON SET GiamGia=0 WHERE MaHD=@Code;
   IF @Action='Add' INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan,GhiChu) VALUES(@Code,@Product,CONVERT(int,@Quantity),@price,@Note);
   IF @Action='Update' UPDATE dbo.CHITIET_HD SET SoLuong=CONVERT(int,@Quantity),GhiChu=@Note WHERE MaHD=@Code AND MaDong=@LineId;
   IF @Action='Delete' DELETE dbo.CHITIET_HD WHERE MaHD=@Code AND MaDong=@LineId;
   IF EXISTS(SELECT 1 FROM dbo.HOADON h JOIN dbo.KHUYENMAI k ON k.MaKM=h.MaKM WHERE h.MaHD=@Code AND CONVERT(date,h.NgayLap) NOT BETWEEN k.NgayBD AND k.NgayKT) THROW 52105,N'Chương trình đã thay đổi; bỏ/chọn lại chương trình trước.',1;
   EXEC dbo.sp_SalesRefreshDiscount @Code;
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesLine',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_SuaThongTinHoaDon @Actor int,@Stamp binary(8),@Code varchar(max),@ExpectedRevision binary(8),@RequestId uniqueidentifier,@Customer varchar(max)=NULL,@Promotion varchar(max)=NULL,@Partner varchar(max)=NULL,@Recipient nvarchar(max)=NULL,@Phone varchar(max)=NULL,@Address nvarchar(max)=NULL,@Ship decimal(38,18)=0 AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  SELECT @Recipient=NULLIF(LTRIM(RTRIM(@Recipient)),N''),@Phone=NULLIF(LTRIM(RTRIM(@Phone)),''),@Address=NULLIF(LTRIM(RTRIM(@Address)),N'');
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
CREATE PROCEDURE dbo.sp_SalesReconcile @Actor int,@Stamp binary(8),@RequestId uniqueidentifier,@Code varchar(max),@Operation varchar(40) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
  IF @RequestId IS NULL OR @Operation NOT IN('SalesCreate','SalesLine','SalesHeader','SalesCheckout','SalesCancel','SalesDelivery') THROW 52101,N'Yêu cầu đối chiếu không hợp lệ.',1;
  DECLARE @resource nvarchar(255)=N'CafeManager:SalesRequest:'+CONVERT(nvarchar(36),@RequestId); EXEC dbo.sp_SalesLock @resource;
  SELECT EntityCode,ResultRevision,Operation,CompletedAt FROM dbo.G03OperationReceipt WITH(HOLDLOCK) WHERE RequestId=@RequestId AND ActorAccountId=@Actor AND Operation=@Operation AND EntityCode=@Code;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
CREATE PROCEDURE dbo.sp_SalesOpenOrders @Actor int,@Stamp binary(8),@Search nvarchar(100)=N'',@Channel nvarchar(20)=NULL,@Skip int=0 AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 IF @Skip<0 THROW 52101,N'Trang không hợp lệ.',1;
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 SELECT h.MaHD Code,h.NgayLap Created,h.LoaiDon Channel,h.MaBan TableCode,k.TenKH Customer,h.ThanhTien Payable,h.TrangThai Status,h.TrangThaiGiao Delivery,h.Revision,COUNT(*) OVER() TotalCount
 FROM dbo.HOADON h LEFT JOIN dbo.KHACHHANG k ON k.MaKH=h.MaKH WHERE h.TrangThai=N'Chua thanh toan' AND (@Channel IS NULL OR h.LoaiDon=@Channel) AND (h.MaHD LIKE N'%'+@Search+N'%' ESCAPE N'~' OR k.TenKH LIKE N'%'+@Search+N'%' ESCAPE N'~') ORDER BY h.NgayLap DESC,h.MaHD OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_SalesMenu @Actor int,@Stamp binary(8),@Search nvarchar(100)=N'',@Category varchar(5)=NULL,@Skip int=0 AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH'; IF @Skip<0 THROW 52101,N'Trang không hợp lệ.',1;
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 SELECT MaSP Code,TenSP Name,GiaBan Price,TrangThai Status,CONVERT(bit,CASE WHEN EXISTS(SELECT 1 FROM dbo.CONGTHUC c WHERE c.MaSP=s.MaSP) THEN 1 ELSE 0 END) HasRecipe FROM dbo.SANPHAM s WHERE (@Category IS NULL OR MaDM=@Category) AND TenSP LIKE N'%'+@Search+N'%' ESCAPE N'~' ORDER BY MaSP OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_SalesCategories @Actor int,@Stamp binary(8) AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH'; SELECT MaDM Code,TenDM Name FROM dbo.DANHMUC_SP ORDER BY TenDM; END;
GO
CREATE PROCEDURE dbo.sp_SalesPartners @Actor int,@Stamp binary(8) AS
BEGIN SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH'; SELECT MaDT Code,TenDT Name,ChietKhau Ratio FROM dbo.DOITACGIAOHANG ORDER BY TenDT; END;
GO
GRANT EXECUTE ON dbo.sp_TaoHoaDon TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_SuaDongHoaDon TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_SuaThongTinHoaDon TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_SalesOrderGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_SalesOpenOrders TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_SalesMenu TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_SalesCategories TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_SalesPartners TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_SalesReconcile TO CafeManagerApp;

