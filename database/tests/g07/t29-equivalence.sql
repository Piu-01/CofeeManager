-- Chỉ đọc invariant của dataset test; parity báo cáo trước/sau index do probe typed ghi.
SET NOCOUNT ON;
IF DB_NAME() NOT LIKE 'CoffeeManager_Test_G07%' OR OBJECT_ID('dbo.G07TestMarker') IS NULL THROW 53501,N'Chỉ DBtest G07 có marker.',1;
IF EXISTS(SELECT 1 FROM dbo.NGUYENVATLIEU WHERE SoLuongTon<0) THROW 53502,N'Tồn âm.',1;
IF EXISTS(SELECT 1 FROM dbo.HOADON h WHERE h.TrangThai=N'Da thanh toan' AND h.ThanhTien<>COALESCE((SELECT SUM(t.SoTien) FROM dbo.CHITIET_THANHTOAN t WHERE t.MaHD=h.MaHD),0)) THROW 53502,N'Tiền phân bổ không khớp hóa đơn.',1;
IF EXISTS(SELECT 1 FROM dbo.vw_CongNoNCC WHERE ConNo<0) THROW 53502,N'Nợ âm.',1;
