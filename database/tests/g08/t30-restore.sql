-- Audit trạng thái/nguồn cơ bản sau restore; parity từng bảng do OperationsTools snapshot hash.
SET NOCOUNT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G08[_]%' OR OBJECT_ID('dbo.G08TestMarker') IS NULL
 THROW 53601,N'Chỉ DB test G08 có marker.',1;
IF EXISTS(SELECT 1 FROM dbo.HOADON h WHERE h.TrangThai=N'Da thanh toan'
 AND h.ThanhTien<>COALESCE((SELECT SUM(t.SoTien) FROM dbo.CHITIET_THANHTOAN t WHERE t.MaHD=h.MaHD),0))
 THROW 53602,N'Nguồn tiền trả không khớp.',1;
DECLARE @transactions int=@@TRANCOUNT,@state int=XACT_STATE();
SELECT @transactions TransactionCount,@state TransactionState;
