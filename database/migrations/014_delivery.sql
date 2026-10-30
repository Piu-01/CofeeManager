-- T18: current delivery status only. No invented timeline/refund.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=13) THROW 52501,N'Migration013 bắt buộc.',1;
GO
CREATE PROCEDURE dbo.sp_DeliveryList @Actor int,@Stamp binary(8),@Search nvarchar(100)=N'',@Delivery nvarchar(20)=NULL,@Status nvarchar(30)=NULL,@Skip int=0 AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 IF @Skip<0 THROW 52101,N'Trang không hợp lệ.',1;
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 SELECT MaHD Code,NgayLap Created,TenNguoiNhan Recipient,SDTNguoiNhan Phone,DiaChiGiao Address,MaDT Partner,TrangThaiGiao Delivery,TrangThai PaymentStatus,Revision,COUNT(*) OVER() TotalCount
 FROM dbo.HOADON WHERE LoaiDon=N'Giao hang' AND (@Delivery IS NULL OR TrangThaiGiao=@Delivery) AND (@Status IS NULL OR TrangThai=@Status)
 AND (MaHD LIKE N'%'+@Search+N'%' ESCAPE N'~' OR SDTNguoiNhan LIKE @Search+N'%' ESCAPE N'~')
 ORDER BY NgayLap DESC,MaHD OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_DeliveryGet @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@Code AND LoaiDon=N'Giao hang') THROW 52501,N'Không có đơn giao hàng này.',1;
 EXEC dbo.sp_InvoiceDetail @Actor,@Stamp,@Code;
END;
GO
CREATE PROCEDURE dbo.sp_ChuyenTrangThaiGiaoHang @Actor int,@Stamp binary(8),@Code varchar(max),@ExpectedRevision binary(8),@ExpectedDeliveryStatus nvarchar(20),@TargetStatus nvarchar(20),@RequestId uniqueidentifier AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@ExpectedRevision r,@ExpectedDeliveryStatus old,@TargetStatus next FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit;
  EXEC dbo.sp_SalesBegin @Actor,@Stamp,@RequestId,'SalesDelivery',@Code,@payload,@replay OUTPUT;
  IF @replay=0
  BEGIN
   DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource;
   IF NOT EXISTS(SELECT 1 FROM dbo.HOADON WITH(UPDLOCK,HOLDLOCK) WHERE MaHD=@Code AND LoaiDon=N'Giao hang' AND TrangThai<>N'Huy') THROW 52501,N'Đơn giao không có hoặc đã hủy.',1;
   IF @ExpectedRevision IS NULL OR @ExpectedDeliveryStatus IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@Code AND Revision=@ExpectedRevision AND TrangThaiGiao=@ExpectedDeliveryStatus) THROW 52103,N'Trạng thái đã thay đổi. Tải lại.',1;
   IF @TargetStatus IS NULL OR NOT ((@ExpectedDeliveryStatus=N'Cho giao' AND @TargetStatus=N'Dang giao') OR (@ExpectedDeliveryStatus=N'Dang giao' AND @TargetStatus=N'Da giao')) THROW 52502,N'Chỉ chuyển bước kế tiếp, không nhảy/lùi trạng thái.',1;
   IF EXISTS(SELECT 1 FROM dbo.HOADON WHERE MaHD=@Code AND (NULLIF(LTRIM(RTRIM(TenNguoiNhan)),N'') IS NULL OR NULLIF(SDTNguoiNhan,'') IS NULL OR SDTNguoiNhan COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9]%' OR NULLIF(LTRIM(RTRIM(DiaChiGiao)),N'') IS NULL)) THROW 52503,N'Thiếu thông tin giao hợp lệ.',1;
   UPDATE dbo.HOADON SET TrangThaiGiao=@TargetStatus WHERE MaHD=@Code;
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesDelivery',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
GRANT EXECUTE ON dbo.sp_DeliveryList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_DeliveryGet TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_ChuyenTrangThaiGiaoHang TO CafeManagerApp;
