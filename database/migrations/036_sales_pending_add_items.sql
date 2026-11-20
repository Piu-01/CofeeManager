-- G04 T14: "In tạm" giữ đơn ở Chờ thanh toán nhưng vẫn nối thêm món.
-- Cho phép: thêm dòng mới và tăng số lượng dòng có sẵn (gộp về một dòng).
-- Vẫn khóa: xóa dòng, giảm số lượng, đổi ghi chú dòng, F3 vẫn sửa khách/ưu đãi/ship trước khi thu.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=35)
 THROW 52107,N'Cần schema035 và runner migration.',1;
IF OBJECT_ID(N'dbo.sp_SalesCheckPayable',N'P') IS NULL OR OBJECT_ID(N'dbo.sp_SuaDongHoaDon',N'P') IS NULL
 OR OBJECT_ID(N'dbo.sp_SuaThongTinHoaDon',N'P') IS NULL OR OBJECT_ID(N'dbo.TR_CTHD_TongTien',N'TR') IS NULL
 THROW 52107,N'Thiếu procedure/trigger bán hàng; cần đối chiếu trước nâng cấp.',1;
GO
ALTER PROCEDURE dbo.sp_SuaDongHoaDon @Actor int,@Stamp binary(8),@Code varchar(max),@ExpectedRevision binary(8),@RequestId uniqueidentifier,@Action varchar(10),@LineId int=NULL,@Product varchar(max)=NULL,@Quantity bigint=NULL,@Note nvarchar(max)=NULL AS
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
   -- Chưa thanh toán hoặc đã In tạm đều sửa được theo luật bên dưới; đã thu/hủy thì từ chối.
   EXEC dbo.sp_SalesCheckPayable @Code,@ExpectedRevision;
   IF @Action IS NULL OR @Action NOT IN('Add','Update','Delete') OR DATALENGTH(@Note)>200 OR (@Action<>'Delete' AND (@Quantity IS NULL OR @Quantity NOT BETWEEN 1 AND 2147483647)) THROW 52101,N'Dòng món không hợp lệ.',1;
   DECLARE @price decimal(18,0),@oldQuantity int=0,@oldNote nvarchar(100);
   IF @Action<>'Add'
   BEGIN
    SELECT @Product=MaSP,@price=GiaBan,@oldQuantity=SoLuong,@oldNote=GhiChu FROM dbo.CHITIET_HD WHERE MaHD=@Code AND MaDong=@LineId;
    IF @price IS NULL THROW 52101,N'Dòng không thuộc hóa đơn.',1;
   END;
   -- Đơn đã In tạm: món ra quầy coi như chốt, chỉ được nối thêm hoặc tăng số lượng.
   IF EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@Code AND TrangThai=N'Cho thanh toan')
    AND (@Action='Delete' OR (@Action='Update' AND (@Quantity<=@oldQuantity OR ISNULL(@Note,N'')<>ISNULL(@oldNote,N''))))
    THROW 52110,N'Đơn đã in tạm; chỉ thêm món hoặc tăng số lượng, không xóa/giảm/đổi ghi chú.',1;
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
   -- F3 còn sửa được khi đơn đã In tạm: khách/ưu đãi/ship chốt ở bước thu tiền.
   EXEC dbo.sp_SalesCheckPayable @Code,@ExpectedRevision;
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
-- Trigger gốc khóa mọi thay đổi CHITIET_HD ngoài Chưa thanh toán. Mở đúng đơn Chờ thanh toán
-- nhưng chỉ cho nối thêm/tăng số lượng: không xóa dòng, không giảm SL, không đổi món/giá/ghi chú.
ALTER TRIGGER dbo.TR_CTHD_TongTien ON CHITIET_HD AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM HOADON h WITH (UPDLOCK,HOLDLOCK)
               WHERE h.MaHD IN (SELECT MaHD FROM inserted UNION SELECT MaHD FROM deleted)
                 AND h.TrangThai NOT IN (N'Chua thanh toan', N'Cho thanh toan'))
        THROW 51001,N'Khong duoc sua mon cua hoa don da thanh toan hoac huy.',1;
    IF EXISTS (SELECT 1 FROM HOADON h
               WHERE h.MaHD IN (SELECT MaHD FROM inserted UNION SELECT MaHD FROM deleted)
                 AND h.TrangThai = N'Cho thanh toan')
       AND (EXISTS (SELECT 1 FROM deleted d
                    WHERE NOT EXISTS (SELECT 1 FROM inserted i WHERE i.MaDong=d.MaDong))
            OR EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON i.MaDong=d.MaDong
                       WHERE i.SoLuong<=d.SoLuong OR i.MaSP<>d.MaSP OR i.GiaBan<>d.GiaBan
                          OR ISNULL(i.GhiChu,N'')<>ISNULL(d.GhiChu,N'')))
        THROW 52110,N'Đơn đã in tạm; chỉ thêm món hoặc tăng số lượng, không xóa/giảm/đổi ghi chú.',1;
    UPDATE h SET TongTien=COALESCE(t.Tong,0)
    FROM HOADON h OUTER APPLY
      (SELECT SUM(CONVERT(DECIMAL(18,0),d.SoLuong)*d.GiaBan) AS Tong
       FROM CHITIET_HD d WHERE d.MaHD=h.MaHD) t
    WHERE h.MaHD IN (SELECT MaHD FROM inserted UNION SELECT MaHD FROM deleted);
END;
GO
