-- Runner maps #T02_ to dbo.T02_: helpers exist only in the isolated fixture DB.
-- Local temporary procedures cannot resolve this database's TVP from tempdb.
CREATE PROCEDURE #T02_Order
    @Id CHAR(10), @Product CHAR(5)='SP002', @Quantity INT=1,
    @Customer CHAR(5)='KH004', @Promotion CHAR(10)=NULL
AS
BEGIN
    INSERT dbo.HOADON(MaHD,NgayLap,MaNV,MaKH,MaKM,LoaiDon)
    VALUES(@Id,'2025-04-01','NV004',@Customer,@Promotion,N'Mang di');
    IF @Quantity>0
        INSERT dbo.CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan)
        SELECT @Id,@Product,@Quantity,GiaBan FROM dbo.SANPHAM WHERE MaSP=@Product;
END;
GO
CREATE PROCEDURE #T02_Pay
    @Id CHAR(10), @Amount DECIMAL(18,0)=NULL,
    @Method CHAR(5)='PT001', @Date DATETIME2(0)='2025-04-01'
AS
BEGIN
    IF @Amount IS NULL SELECT @Amount=ThanhTien FROM dbo.HOADON WHERE MaHD=@Id;
    DECLARE @tt dbo.DanhSachThanhToan;
    IF @Amount>0 INSERT @tt(MaPT,SoTien) VALUES(@Method,@Amount);
    EXEC dbo.sp_ThanhToanHoaDon @MaHD=@Id,@ThanhToan=@tt,@NgayTT=@Date;
END;
GO
CREATE PROCEDURE #T02_Debt @Id CHAR(10) AS
BEGIN
    INSERT dbo.PHIEUNHAP(MaPN,MaNV,MaNCC) VALUES(@Id,'NV001','NCC01');
    INSERT dbo.CHITIET_PN(MaPN,MaNVL,SoLuong,DonGiaNhap) VALUES(@Id,'NVL01',1,1000);
    INSERT dbo.CONGNO_NCC(MaCN,MaPN,TienNo) VALUES(@Id,@Id,1000);
END;
GO
CREATE PROCEDURE #T02_Count @Id CHAR(10), @Delta DECIMAL(18,4)=0.25, @DayOffset INT=0 AS
BEGIN
    INSERT dbo.PHIEUKIEMKE(MaKK,NgayKK,MaNV)
    VALUES(@Id,DATEADD(DAY,@DayOffset,CONVERT(DATE,SYSDATETIME())),'NV001');
    INSERT dbo.CHITIET_KK(MaKK,MaNVL,TonHeThong,TonThucTe,LyDo)
    SELECT @Id,MaNVL,SoLuongTon,SoLuongTon+@Delta,N'T02 count fixture'
    FROM dbo.NGUYENVATLIEU WHERE MaNVL IN ('NVL01','NVL02');
END;
GO
