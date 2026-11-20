-- G04 T34-01: chi tiết hóa đơn trả đủ 27 cột header (MaPhien/SessionRevision) như sp_SalesReadCore.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=34)
 THROW 52107,N'Cần schema034 (T34) và runner migration.',1;
IF OBJECT_ID(N'dbo.sp_InvoiceDetail',N'P') IS NULL
 THROW 52107,N'Thiếu sp_InvoiceDetail; cần đối chiếu trước nâng cấp.',1;
GO
ALTER PROCEDURE dbo.sp_InvoiceDetail @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
  DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource,'Shared';
  SELECT h.MaHD,h.NgayLap,h.MaNV,h.MaKH,h.MaBan,h.MaDT,h.MaKM,h.LoaiDon,h.TenNguoiNhan,h.SDTNguoiNhan,h.DiaChiGiao,h.TrangThaiGiao,h.NgayThanhToan,h.PhanTramPhiDoiTac,h.PhiDoiTac,h.TongTien,h.PhiShip,h.GiamGia,CONVERT(decimal(38,0),h.ThanhTien) Payable,h.TrangThai,h.Revision,CONVERT(bit,1) PreviewValid,s.Source PreviewSource,s.PercentDiscount PreviewPercent,SYSDATETIME() ReadAt,h.MaPhien,sessionRow.Revision SessionRevision
  FROM dbo.HOADON h WITH(HOLDLOCK) LEFT JOIN dbo.HoaDonUuDaiSnapshot s ON s.MaHD=h.MaHD
  OUTER APPLY(SELECT TOP(1) p.Revision FROM dbo.PHIEN_BAN p WHERE p.MaPhien=h.MaPhien) sessionRow
  WHERE h.MaHD=@Code;
  SELECT d.MaDong,d.MaSP,s.TenSP,d.SoLuong,d.GiaBan,d.GhiChu,d.SoLuong*d.GiaBan Amount FROM dbo.CHITIET_HD d JOIN dbo.SANPHAM s ON s.MaSP=d.MaSP WHERE d.MaHD=@Code ORDER BY d.MaDong;
  SELECT t.MaTT,t.MaPT,p.TenPT,t.SoTien,t.MaThamChieu FROM dbo.CHITIET_THANHTOAN t JOIN dbo.PHUONGTHUCTT p ON p.MaPT=t.MaPT WHERE t.MaHD=@Code ORDER BY t.MaTT;
  SELECT s.Kind,s.Source,s.PercentDiscount,COALESCE((SELECT SUM(SoDiem) FROM dbo.LICHSU_DIEM WHERE MaHD=@Code AND Loai=N'Cong'),0) Points,s.RecordedAt FROM dbo.HoaDonUuDaiSnapshot s WHERE s.MaHD=@Code;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
GRANT EXECUTE ON dbo.sp_InvoiceDetail TO CafeManagerApp;
GO
