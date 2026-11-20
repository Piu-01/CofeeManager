-- G04-POS-02: flush nháp đơn mới (header + toàn bộ dòng) trong một transaction.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=29)
 THROW 52107,N'Cần schema029 và runner migration.',1;
GO
CREATE TYPE dbo.SalesDraftLines AS TABLE(
 RowNo int NOT NULL PRIMARY KEY,
 Product varchar(5) NOT NULL,
 Quantity int NOT NULL,
 Note nvarchar(100) NULL,
 Price decimal(18,0) NOT NULL);
GO
CREATE PROCEDURE dbo.sp_TaoHoaDonTuNhap
 @Actor int,@Stamp binary(8),@Code varchar(max),@RequestId uniqueidentifier,
 @Channel nvarchar(max),@TableCode varchar(max)=NULL,@Customer varchar(max)=NULL,@Promotion varchar(max)=NULL,
 @Partner varchar(max)=NULL,@Recipient nvarchar(max)=NULL,@Phone varchar(max)=NULL,@Address nvarchar(max)=NULL,
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
    IF EXISTS(SELECT 1 FROM dbo.HOADON WITH(UPDLOCK,HOLDLOCK) WHERE MaBan=@TableCode AND TrangThai=N'Chua thanh toan') THROW 52104,N'Bàn đã có đơn. Tải đơn hiện tại trước khi thêm món.',1;
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
   INSERT dbo.HOADON(MaHD,MaNV,MaKH,MaBan,MaDT,MaKM,LoaiDon,TenNguoiNhan,SDTNguoiNhan,DiaChiGiao,TrangThaiGiao,PhiShip)
   SELECT @Code,MaNV,@Customer,@TableCode,@Partner,@Promotion,@Channel,@Recipient,@Phone,@Address,CASE WHEN @Channel=N'Giao hang' THEN N'Cho giao' END,CONVERT(decimal(18,0),@Ship) FROM dbo.TAIKHOAN WHERE MaTK=@Actor;
   INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan,GhiChu)
   SELECT @Code,Product,Quantity,Price,NULLIF(LTRIM(RTRIM(Note)),N'') FROM @Lines ORDER BY RowNo;
   EXEC dbo.sp_SalesRefreshDiscount @Code;
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesCreate',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END
GO
GRANT EXECUTE ON dbo.sp_TaoHoaDonTuNhap TO CafeManagerApp;
GRANT EXECUTE,REFERENCES ON TYPE::dbo.SalesDraftLines TO CafeManagerApp;
