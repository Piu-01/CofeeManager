-- Audit dataset sau GenerateBills trong PerformanceProbe; không ghi/sửa chứng từ.
SET NOCOUNT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G07Bench[_]%' OR OBJECT_ID('dbo.G07TestMarker') IS NULL THROW 53501,N'Chỉ DB benchmark có marker.',1;
IF (SELECT COUNT(*) FROM dbo.HOADON)<>10000
 OR (SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan')<>8000
 OR (SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai=N'Chua thanh toan')<>1000
 OR (SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai=N'Huy')<>1000
 OR (SELECT COUNT(*) FROM dbo.CHITIET_THANHTOAN)<>14000
 OR (SELECT COUNT(*) FROM dbo.CHITIET_HD l JOIN dbo.HOADON h ON h.MaHD=l.MaHD WHERE h.TrangThai=N'Da thanh toan')<>24000
 THROW 53502,N'Dataset benchmark chưa đủ phân bố đã chốt.',1;
IF EXISTS(SELECT 1 FROM dbo.HOADON h WHERE h.TongTien<>(SELECT SUM(CONVERT(decimal(38,0),l.SoLuong*l.GiaBan)) FROM dbo.CHITIET_HD l WHERE l.MaHD=h.MaHD)
 OR (h.TrangThai=N'Da thanh toan' AND h.ThanhTien<>(SELECT SUM(CONVERT(decimal(38,0),t.SoTien)) FROM dbo.CHITIET_THANHTOAN t WHERE t.MaHD=h.MaHD))) THROW 53502,N'Không khớp tiền dataset.',1;
