IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G05_%' OR OBJECT_ID('dbo.G05TestMarker') IS NULL THROW 53900,N'Chỉ chạy trên fixture G05 có marker.',1;
CREATE TABLE dbo.G05AcceptanceLog(Part varchar(40) NOT NULL,Result varchar(10) NOT NULL,Actual nvarchar(500) NULL,RecordedAt datetime2(0) NOT NULL DEFAULT SYSDATETIME());
GO
CREATE PROCEDURE dbo.sp_G05Assert @Part varchar(40),@Condition bit,@Actual nvarchar(500) AS
BEGIN INSERT dbo.G05AcceptanceLog(Part,Result,Actual) VALUES(@Part,CASE WHEN @Condition=1 THEN 'PASS' ELSE 'FAIL' END,@Actual);END;
GO
UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaVT IN('ADMIN','CASH','STORE');
