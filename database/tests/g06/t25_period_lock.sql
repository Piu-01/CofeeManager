IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G06[_]%' OR NOT EXISTS(SELECT 1 FROM dbo.G06TestMarker) THROW 53400,N'Sai DB test.',1;
DECLARE @code char(5)=(SELECT MaNV FROM dbo.TAIKHOAN WHERE TenDangNhap='smoke.account'),@actor int=1,@stamp binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=1),@request uniqueidentifier=NEWID(),@lines dbo.AttendanceBatch;
INSERT @lines SELECT 1,'Update',MaNV,NgayLam,MaCa,GioVao,GioRa,TrangThai,Revision FROM dbo.CHAMCONG WHERE MaNV=@code;
DECLARE @caught bit=0;
EXECUTE AS USER='G06Runtime';
BEGIN TRY EXEC dbo.sp_AttendanceSaveBatch @actor,@stamp,@request,@lines; END TRY BEGIN CATCH IF ERROR_NUMBER()=53202 SET @caught=1; ELSE BEGIN REVERT;THROW;END; END CATCH;
REVERT;
IF @caught<>1 OR @@TRANCOUNT<>0 OR EXISTS(SELECT 1 FROM dbo.AttendanceBatchReceipt WHERE RequestId=@request) THROW 53402,N'T25 khóa kỳ/rollback sai.',1;
