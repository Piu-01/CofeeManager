-- Test-only plumbing objects, never migrated to an application database.
CREATE USER T05_App WITHOUT LOGIN;
ALTER ROLE CafeManagerApp ADD MEMBER T05_App;
CREATE TABLE dbo.T05_Sentinel(Id INT NOT NULL PRIMARY KEY);
INSERT dbo.T05_Sentinel VALUES(1);
CREATE TABLE dbo.T05_Values(
    Id INT IDENTITY PRIMARY KEY,
    TextValue NVARCHAR(100) NOT NULL,
    OptionalValue NVARCHAR(100) NULL,
    Code CHAR(5) NOT NULL,
    Money DECIMAL(18,0) NOT NULL,
    Quantity DECIMAL(18,4) NOT NULL,
    At DATETIME2(0) NULL
);
GO
CREATE PROCEDURE dbo.T05_SaveValues
    @Text NVARCHAR(100),@Optional NVARCHAR(100),@Code CHAR(5),
    @Money DECIMAL(18,0),@Quantity DECIMAL(18,4),@At DATETIME2(0)
AS
BEGIN
    INSERT dbo.T05_Values(TextValue,OptionalValue,Code,Money,Quantity,At)
    VALUES(@Text,@Optional,@Code,@Money,@Quantity,@At);
END;
GO
CREATE PROCEDURE dbo.T05_ReadValues @Text NVARCHAR(100)
AS SELECT TextValue,OptionalValue,Code,Money,Quantity,At FROM dbo.T05_Values WHERE TextValue=@Text ORDER BY Id;
GO
CREATE PROCEDURE dbo.T05_Tvp @ThanhToan dbo.DanhSachThanhToan READONLY
AS SELECT MaPT,SoTien,MaThamChieu FROM @ThanhToan ORDER BY MaPT,SoTien;
GO
CREATE PROCEDURE dbo.T05_Unknown AS THROW 52905,'Test-only unknown error with sensitive text that must never be logged.',1;
GO
CREATE PROCEDURE dbo.T05_Slow AS WAITFOR DELAY '00:00:05';
GO
GRANT EXECUTE ON dbo.T05_SaveValues TO CafeManagerApp;
GRANT EXECUTE ON dbo.T05_ReadValues TO CafeManagerApp;
GRANT EXECUTE ON dbo.T05_Tvp TO CafeManagerApp;
GRANT EXECUTE ON dbo.T05_Unknown TO CafeManagerApp;
GRANT EXECUTE ON dbo.T05_Slow TO CafeManagerApp;
INSERT dbo.HOADON(MaHD,NgayLap,MaNV,MaKH,LoaiDon) VALUES
('Z5PAY00001','2025-04-01','NV004','KH004',N'Mang di'),
('Z5PAY00002','2025-04-01','NV004','KH004',N'Mang di'),
('Z5PAY00003','2025-04-01','NV004','KH004',N'Mang di');
INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan) VALUES
('Z5PAY00001','SP002',1,30000),('Z5PAY00002','SP002',1,30000),('Z5PAY00003','SP002',1,30000);
GO
CREATE TRIGGER dbo.TR_T05_FaultPoint ON dbo.LICHSU_DIEM AFTER INSERT AS
BEGIN
    IF EXISTS(SELECT 1 FROM inserted WHERE MaHD='Z5PAY00003') THROW 52906,'Injected late point failure',1;
END;
