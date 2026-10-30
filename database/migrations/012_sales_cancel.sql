-- T16: versioned, authorized cancellation of an unpaid order.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=11) THROW 52301,N'Migration011 bắt buộc.',1;
GO
ALTER PROCEDURE dbo.sp_HuyHoaDon @Actor int,@Stamp binary(8),@Code varchar(max),@ExpectedRevision binary(8),@RequestId uniqueidentifier AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @@TRANCOUNT<>0 THROW 52101,N'Không gọi ghi trong transaction bên ngoài.',1;
 BEGIN TRY
  BEGIN TRAN;
  DECLARE @payload nvarchar(max)=(SELECT @Code c,@ExpectedRevision r FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES),@replay bit;
  EXEC dbo.sp_SalesBegin @Actor,@Stamp,@RequestId,'SalesCancel',@Code,@payload,@replay OUTPUT;
  IF @replay=0
  BEGIN
   DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource;
   EXEC dbo.sp_SalesCheckOpen @Code,@ExpectedRevision;
   UPDATE dbo.HOADON SET TrangThai=N'Huy',TrangThaiGiao=CASE WHEN LoaiDon=N'Giao hang' THEN N'Huy giao' END WHERE MaHD=@Code;
   EXEC dbo.sp_SalesFinish @Actor,@RequestId,'SalesCancel',@Code,@payload;
  END;
  COMMIT; SELECT EntityCode,ResultRevision FROM dbo.G03OperationReceipt WHERE RequestId=@RequestId;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
