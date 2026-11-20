-- Subset contract/permission, không thay ma trận13ID hoặc dữ liệu kỳ vọng tính tay.
SET NOCOUNT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G07[_]%' OR OBJECT_ID('dbo.G07TestMarker') IS NULL THROW 53501,N'Chỉ DBtest G07.',1;
DECLARE @stamp binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=1);
EXECUTE AS USER='G07Runtime';
EXEC dbo.sp_ReportSales @Actor=1,@Stamp=@stamp,@From='20250101',@To='99991231';
EXEC dbo.sp_ReportInventory @Actor=1,@Stamp=@stamp,@From='20260101',@To='99991231';
EXEC dbo.sp_ReportDashboard @Actor=1,@Stamp=@stamp;
IF HAS_PERMS_BY_NAME('dbo.vw_ReportStockEvents','OBJECT','SELECT')=1 THROW 53502,N'Runtime được đọc view nội bộ ngoài endpoint.',1;
REVERT;
