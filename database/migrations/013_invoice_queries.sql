-- T17: read-only invoice search and immutable financial detail.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=12) THROW 52107,N'Migration012 bắt buộc.',1;
GO
CREATE PROCEDURE dbo.sp_InvoiceList @Actor int,@Stamp binary(8),@DateBasis varchar(10),@FromDate date,@ToDate date,@Status nvarchar(30)=NULL,@Channel nvarchar(20)=NULL,@Customer varchar(5)=NULL,@Search nvarchar(100)=N'',@Skip int=0 AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 IF @DateBasis IS NULL OR @DateBasis NOT IN('Created','Paid') OR @FromDate IS NULL OR @ToDate IS NULL OR @ToDate<@FromDate OR @Skip<0 THROW 52101,N'Bộ lọc không hợp lệ.',1;
 DECLARE @end datetime2(0)=CASE WHEN @ToDate='99991231' THEN CONVERT(datetime2(0),'9999-12-31 23:59:59') ELSE DATEADD(day,1,CONVERT(datetime2(0),@ToDate)) END;
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 SELECT h.MaHD Code,h.NgayLap Created,h.LoaiDon Channel,h.MaBan TableCode,k.TenKH Customer,h.ThanhTien Payable,h.TrangThai Status,h.TrangThaiGiao Delivery,h.Revision,COUNT(*) OVER() TotalCount
 FROM dbo.HOADON h LEFT JOIN dbo.KHACHHANG k ON k.MaKH=h.MaKH
 CROSS APPLY(SELECT CASE WHEN @DateBasis='Paid' THEN h.NgayThanhToan ELSE h.NgayLap END FilterDate) d
 WHERE d.FilterDate>=@FromDate AND (d.FilterDate<@end OR (@ToDate='99991231' AND d.FilterDate=@end))
 AND (@Status IS NULL OR h.TrangThai=@Status) AND (@Channel IS NULL OR h.LoaiDon=@Channel) AND (@Customer IS NULL OR h.MaKH=@Customer)
 AND (h.MaHD LIKE N'%'+@Search+N'%' ESCAPE N'~' OR k.TenKH LIKE N'%'+@Search+N'%' ESCAPE N'~' OR k.SDT LIKE @Search+N'%' ESCAPE N'~')
 ORDER BY d.FilterDate DESC,h.MaHD OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
CREATE PROCEDURE dbo.sp_InvoiceDetail @Actor int,@Stamp binary(8),@Code varchar(max) AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
  BEGIN TRAN; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
  DECLARE @resource nvarchar(255)=N'CafeManager:SalesOrder:'+CONVERT(nvarchar(10),@Code); EXEC dbo.sp_SalesLock @resource,'Shared';
  SELECT h.MaHD,h.NgayLap,h.MaNV,h.MaKH,h.MaBan,h.MaDT,h.MaKM,h.LoaiDon,h.TenNguoiNhan,h.SDTNguoiNhan,h.DiaChiGiao,h.TrangThaiGiao,h.NgayThanhToan,h.PhanTramPhiDoiTac,h.PhiDoiTac,h.TongTien,h.PhiShip,h.GiamGia,CONVERT(decimal(38,0),h.ThanhTien) Payable,h.TrangThai,h.Revision,CONVERT(bit,1) PreviewValid,s.Source PreviewSource,s.PercentDiscount PreviewPercent,SYSDATETIME() ReadAt
  FROM dbo.HOADON h WITH(HOLDLOCK) LEFT JOIN dbo.HoaDonUuDaiSnapshot s ON s.MaHD=h.MaHD WHERE h.MaHD=@Code;
  SELECT d.MaDong,d.MaSP,s.TenSP,d.SoLuong,d.GiaBan,d.GhiChu,d.SoLuong*d.GiaBan Amount FROM dbo.CHITIET_HD d JOIN dbo.SANPHAM s ON s.MaSP=d.MaSP WHERE d.MaHD=@Code ORDER BY d.MaDong;
  SELECT t.MaTT,t.MaPT,p.TenPT,t.SoTien,t.MaThamChieu FROM dbo.CHITIET_THANHTOAN t JOIN dbo.PHUONGTHUCTT p ON p.MaPT=t.MaPT WHERE t.MaHD=@Code ORDER BY t.MaTT;
  SELECT s.Kind,s.Source,s.PercentDiscount,COALESCE((SELECT SUM(SoDiem) FROM dbo.LICHSU_DIEM WHERE MaHD=@Code AND Loai=N'Cong'),0) Points,s.RecordedAt FROM dbo.HoaDonUuDaiSnapshot s WHERE s.MaHD=@Code;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
END;
GO
GRANT EXECUTE ON dbo.sp_InvoiceList TO CafeManagerApp;
GRANT EXECUTE ON dbo.sp_InvoiceDetail TO CafeManagerApp;
