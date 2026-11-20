-- G07-DA03: nguồn theo món đọc header trước rồi mới lọc dòng món, cùng thứ tự khóa với tạo hóa đơn.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=28)
 THROW 53502,N'Cần schema028 và runner migration.',1;
GO
ALTER PROCEDURE dbo.sp_DashboardAnalyticsSources
 @Actor int,@Stamp binary(8),@Kind varchar(20),@From datetime2(7)=NULL,@Until datetime2(7)=NULL,
 @Channel nvarchar(20)=NULL,@Product varchar(5)=NULL,@Skip int=0
WITH EXECUTE AS OWNER AS
BEGIN
 SET NOCOUNT ON;SET XACT_ABORT ON;
 EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN';
 DECLARE @read datetime2(7)=SYSDATETIME();
 IF @Kind IS NULL OR @Kind NOT IN('revenue','invoices','average','trend','channel','product','expense','orders','delivery','material','debt')
  OR @Skip<0 OR @@TRANCOUNT<>0 OR (@Kind NOT IN('orders','delivery','material','debt') AND (@From IS NULL OR @Until IS NULL OR @From>@Until OR @Until>@read))
  OR (@Channel IS NOT NULL AND @Channel NOT IN(N'Tai quan',N'Mang di',N'Giao hang'))
  OR (@Kind='channel' AND @Channel IS NULL) OR (@Channel IS NOT NULL AND @Kind<>'channel')
  OR (@Kind='product' AND (@Product IS NULL OR LEN(@Product)<>5)) OR (@Product IS NOT NULL AND @Kind<>'product')
  THROW 53501,N'Nguồn hoặc kỳ tổng quan không hợp lệ.',1;
 CREATE TABLE #sources(SourceType varchar(20) NOT NULL,Code varchar(100) NOT NULL,Name nvarchar(200) NOT NULL,
  SourceDate datetime2(7) NULL,Amount decimal(38,0) NULL,Quantity decimal(38,4) NULL);
 CREATE TABLE #headers(MaHD char(10) NOT NULL,LoaiDon nvarchar(20) NOT NULL,SourceDate datetime2(7) NULL,Amount decimal(38,0) NOT NULL);
 BEGIN TRY
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;BEGIN TRAN;
  IF @Kind IN('revenue','invoices','average','trend','channel','product','orders','delivery')
  BEGIN
   -- Chặng một chỉ đọc HOADON; EXISTS CHITIET_HD trước đây cho phép optimizer đảo thứ tự khóa.
   INSERT #headers(MaHD,LoaiDon,SourceDate,Amount)
   SELECT h.MaHD,h.LoaiDon,CASE WHEN @Kind IN('orders','delivery') THEN h.NgayLap ELSE h.NgayThanhToan END,h.ThanhTien
   FROM dbo.HOADON h WHERE
    (@Kind='orders' AND h.TrangThai=N'Chua thanh toan') OR
    (@Kind='delivery' AND h.LoaiDon=N'Giao hang' AND h.TrangThai=N'Da thanh toan' AND h.TrangThaiGiao IN(N'Cho giao',N'Dang giao')) OR
    (@Kind NOT IN('orders','delivery') AND h.TrangThai=N'Da thanh toan' AND h.NgayThanhToan>=@From AND h.NgayThanhToan<@Until
     AND (@Channel IS NULL OR h.LoaiDon=@Channel));
   IF @Kind='product'
    INSERT #sources(SourceType,Code,Name,SourceDate,Amount)
    SELECT 'Invoice',h.MaHD,h.LoaiDon,h.SourceDate,h.Amount FROM #headers h
    WHERE EXISTS(SELECT 1 FROM dbo.CHITIET_HD l WHERE l.MaHD=h.MaHD AND l.MaSP=@Product);
   ELSE
    INSERT #sources(SourceType,Code,Name,SourceDate,Amount)
    SELECT 'Invoice',h.MaHD,h.LoaiDon,h.SourceDate,h.Amount FROM #headers h;
  END
  IF @Kind='expense'
   INSERT #sources(SourceType,Code,Name,SourceDate,Amount)
   SELECT 'Finance',p.MaTC,d.TenDMTC,p.NgayLap,p.SoTien FROM dbo.PHIEUTHUCHI p JOIN dbo.DANHMUC_THUCHI d ON d.MaDMTC=p.MaDMTC
   WHERE d.Loai=N'Chi' AND p.NgayLap>=@From AND p.NgayLap<@Until;
  IF @Kind='material'
   INSERT #sources(SourceType,Code,Name,Quantity) SELECT 'Material',MaNVL,TenNVL+N' · '+DonViTinh,SoLuongTon FROM dbo.NGUYENVATLIEU WHERE SoLuongTon=0;
  IF @Kind='debt'
   INSERT #sources(SourceType,Code,Name,SourceDate,Amount)
   SELECT 'Debt',v.MaCN,n.TenNCC,p.NgayNhap,v.ConNo FROM dbo.vw_CongNoNCC v JOIN dbo.NHACUNGCAP n ON n.MaNCC=v.MaNCC JOIN dbo.PHIEUNHAP p ON p.MaPN=v.MaPN WHERE v.ConNo>0;
  COMMIT;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH;
 SELECT COUNT(*) TotalCount,@read ReadAt FROM #sources;
 SELECT SourceType,Code,Name,SourceDate,Amount,Quantity FROM #sources ORDER BY SourceDate DESC,SourceType,Code OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
