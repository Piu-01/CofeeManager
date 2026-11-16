-- Đo thời gian thực thi procedure đọc của các màn hình chính.
-- Chạy trên DB development/test (chỉ đọc), không ghi dữ liệu.
-- Actor 1 = admin, stamp lấy từ dbo.TAIKHOAN.
SET NOCOUNT ON;
SET STATISTICS IO OFF;
SET STATISTICS TIME ON;

DECLARE @Actor int = 1;
DECLARE @Stamp binary(8) = 0x0000000000000862;
DECLARE @From date = DATEADD(day, -30, CAST(GETDATE() AS date));
DECLARE @To date = CAST(GETDATE() AS date);

-- Làm nóng cache trước khi đo.
EXEC sp_FinanceList @Actor=@Actor, @Stamp=@Stamp, @From=@From, @To=@To, @Search=N'', @Type=NULL, @Origin=NULL, @Skip=0;
EXEC sp_MaterialList @Actor=@Actor, @Stamp=@Stamp, @Search=N'', @Skip=0;
EXEC sp_CustomerList @Actor=@Actor, @Stamp=@Stamp, @Search=N'', @Skip=0;
EXEC sp_InvoiceList @Actor=@Actor, @Stamp=@Stamp, @DateBasis='Paid', @FromDate=@From, @ToDate=@To, @Status=NULL, @Channel=NULL, @Customer=NULL, @Search=N'', @Skip=0;
EXEC sp_SalesMenu @Actor=@Actor, @Stamp=@Stamp, @Search=N'', @Category=NULL, @Skip=0;
EXEC sp_SalesOpenOrders @Actor=@Actor, @Stamp=@Stamp, @Search=N'', @Channel=NULL, @Skip=0;
EXEC sp_TableList @Actor=@Actor, @Stamp=@Stamp;
EXEC sp_ReportDashboard @Actor=@Actor, @Stamp=@Stamp;
EXEC sp_DashboardAnalyticsRead @Actor=@Actor, @Stamp=@Stamp, @Month=0;
GO

PRINT '=== ROUND 1: FINANCE ===';
EXEC sp_FinanceList @Actor=1, @Stamp=0x0000000000000862, @From=DATEADD(day,-30,CAST(GETDATE() AS date)), @To=CAST(GETDATE() AS date), @Search=N'', @Type=NULL, @Origin=NULL, @Skip=0;
PRINT '=== ROUND 1: MATERIAL ===';
EXEC sp_MaterialList @Actor=1, @Stamp=0x0000000000000862, @Search=N'', @Skip=0;
PRINT '=== ROUND 1: CUSTOMER ===';
EXEC sp_CustomerList @Actor=1, @Stamp=0x0000000000000862, @Search=N'', @Skip=0;
PRINT '=== ROUND 1: INVOICE ===';
EXEC sp_InvoiceList @Actor=1, @Stamp=0x0000000000000862, @DateBasis='Paid', @FromDate=DATEADD(day,-30,CAST(GETDATE() AS date)), @ToDate=CAST(GETDATE() AS date), @Status=NULL, @Channel=NULL, @Customer=NULL, @Search=N'', @Skip=0;
PRINT '=== ROUND 1: SALES MENU ===';
EXEC sp_SalesMenu @Actor=1, @Stamp=0x0000000000000862, @Search=N'', @Category=NULL, @Skip=0;
PRINT '=== ROUND 1: OPEN ORDERS ===';
EXEC sp_SalesOpenOrders @Actor=1, @Stamp=0x0000000000000862, @Search=N'', @Channel=NULL, @Skip=0;
PRINT '=== ROUND 1: TABLES ===';
EXEC sp_TableList @Actor=1, @Stamp=0x0000000000000862;
PRINT '=== ROUND 1: REPORT DASHBOARD ===';
EXEC sp_ReportDashboard @Actor=1, @Stamp=0x0000000000000862;
PRINT '=== ROUND 1: DASHBOARD ANALYTICS (month=0) ===';
EXEC sp_DashboardAnalyticsRead @Actor=1, @Stamp=0x0000000000000862, @Month=0;
GO

PRINT '=== ROUND 2: FINANCE ===';
EXEC sp_FinanceList @Actor=1, @Stamp=0x0000000000000862, @From=DATEADD(day,-30,CAST(GETDATE() AS date)), @To=CAST(GETDATE() AS date), @Search=N'', @Type=NULL, @Origin=NULL, @Skip=0;
PRINT '=== ROUND 2: MATERIAL ===';
EXEC sp_MaterialList @Actor=1, @Stamp=0x0000000000000862, @Search=N'', @Skip=0;
PRINT '=== ROUND 2: CUSTOMER ===';
EXEC sp_CustomerList @Actor=1, @Stamp=0x0000000000000862, @Search=N'', @Skip=0;
PRINT '=== ROUND 2: INVOICE ===';
EXEC sp_InvoiceList @Actor=1, @Stamp=0x0000000000000862, @DateBasis='Paid', @FromDate=DATEADD(day,-30,CAST(GETDATE() AS date)), @ToDate=CAST(GETDATE() AS date), @Status=NULL, @Channel=NULL, @Customer=NULL, @Search=N'', @Skip=0;
PRINT '=== ROUND 2: SALES MENU ===';
EXEC sp_SalesMenu @Actor=1, @Stamp=0x0000000000000862, @Search=N'', @Category=NULL, @Skip=0;
PRINT '=== ROUND 2: OPEN ORDERS ===';
EXEC sp_SalesOpenOrders @Actor=1, @Stamp=0x0000000000000862, @Search=N'', @Channel=NULL, @Skip=0;
PRINT '=== ROUND 2: TABLES ===';
EXEC sp_TableList @Actor=1, @Stamp=0x0000000000000862;
PRINT '=== ROUND 2: REPORT DASHBOARD ===';
EXEC sp_ReportDashboard @Actor=1, @Stamp=0x0000000000000862;
PRINT '=== ROUND 2: DASHBOARD ANALYTICS (month=0) ===';
EXEC sp_DashboardAnalyticsRead @Actor=1, @Stamp=0x0000000000000862, @Month=0;
GO

PRINT '=== ROUND 3: DASHBOARD ANALYTICS (month=1) ===';
EXEC sp_DashboardAnalyticsRead @Actor=1, @Stamp=0x0000000000000862, @Month=1;
PRINT '=== ROUND 3: REPORT (dashboard period) ===';
EXEC sp_Report @Actor=1, @Stamp=0x0000000000000862, @From=DATEADD(day,-30,CAST(GETDATE() AS date)), @To=CAST(GETDATE() AS date);
GO
