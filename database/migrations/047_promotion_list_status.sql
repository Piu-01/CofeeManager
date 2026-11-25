-- 047: Lọc danh sách khuyến mãi theo trạng thái hiệu lực.
-- sp_PromotionList nhận @Status (NULL = tất cả) và trả cột TrangThai thống nhất
-- 4 mã DangChay/TamDung/ChuaToiKy/HetHan tính theo ngày hôm nay của server.
-- Chạy sau 045 bằng tools/database/Apply-Migration.ps1 -Version 47.
-- Độc lập với 046: chỉ dùng contract khuyến mãi của 042.
-- CASH giữ nguyên: chỉ thấy KM đang áp dụng hôm nay, bỏ qua @Status.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=45)
    THROW 54701,N'Migration 045 bắt buộc.',1;
IF OBJECT_ID(N'dbo.sp_PromotionList',N'P') IS NULL OR OBJECT_ID(N'dbo.fn_NgayTrongKy',N'FN') IS NULL
 OR OBJECT_ID(N'dbo.fn_NgaySauKy',N'FN') IS NULL
    THROW 54701,N'Thiếu contract khuyến mãi của 042; cần đối chiếu trước nâng cấp.',1;
GO
/* ===== 1. sp_PromotionList thêm bộ lọc trạng thái hiệu lực =====
   ADMIN: TrangThai trả về đúng trạng thái đang hiển thị trên lưới, lọc theo @Status.
   CASH: chỉ KM đang áp dụng hôm nay (@OnDate), không lọc theo @Status. */
ALTER PROCEDURE dbo.sp_PromotionList @Actor int,@Stamp binary(8),@Search nvarchar(max)=N'',@Skip int=0,@OnDate date=NULL,@Status varchar(10)=NULL AS
BEGIN
 SET NOCOUNT ON; EXEC dbo.sp_G03Demand @Actor,@Stamp,'ADMIN,CASH';
 IF LEN(@Search)>100 OR @Skip<0 THROW 52001,N'Bộ lọc không hợp lệ.',1;
 SET @Status=NULLIF(LTRIM(RTRIM(@Status)),'');
 IF @Status IS NOT NULL AND @Status NOT IN('DangChay','TamDung','ChuaToiKy','HetHan')
  THROW 52001,N'Trạng thái lọc không hợp lệ.',1;
 IF EXISTS(SELECT 1 FROM dbo.TAIKHOAN WHERE MaTK=@Actor AND MaVT='CASH')
 BEGIN
  SET @OnDate=COALESCE(@OnDate,CONVERT(date,SYSDATETIME()));
  SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
  SELECT k.MaKM Code,k.TenKM Name,k.NgayBD StartDate,k.NgayKT EndDate,k.PhanTramGiam Discount,
   CASE WHEN dbo.fn_NgaySauKy(@OnDate,k.NgayBD,k.NgayKT)=1 THEN 'HetHan' ELSE k.TrangThai END TrangThai,
   k.DoiTuongKhach Audience,k.PhamVi Scope,
   (SELECT COUNT(*) FROM dbo.KHUYENMAI_MON m WHERE m.MaKM=k.MaKM) ItemCount,k.Revision,
   k.LoaiUuDai Kind,(SELECT COUNT(*) FROM dbo.KHUYENMAI_MON_TANG g WHERE g.MaKM=k.MaKM) GiftItemCount,
   (SELECT COALESCE(SUM(g.SoLuong),0) FROM dbo.KHUYENMAI_MON_TANG g WHERE g.MaKM=k.MaKM) GiftQuantity
  FROM dbo.KHUYENMAI k
  WHERE (k.TenKM LIKE N'%'+@Search+N'%' ESCAPE N'~' OR k.MaKM LIKE N'%'+@Search+N'%' ESCAPE N'~')
   AND k.TrangThai='DangChay'
   AND dbo.fn_NgayTrongKy(@OnDate,k.NgayBD,k.NgayKT)=1
  ORDER BY k.MaKM OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
  RETURN;
 END;
 SET @Search=REPLACE(REPLACE(REPLACE(REPLACE(@Search,N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[');
 DECLARE @Today date=CONVERT(date,SYSDATETIME());
 SELECT k.MaKM Code,k.TenKM Name,k.NgayBD StartDate,k.NgayKT EndDate,k.PhanTramGiam Discount,
  s.TrangThai,k.DoiTuongKhach Audience,k.PhamVi Scope,
  (SELECT COUNT(*) FROM dbo.KHUYENMAI_MON m WHERE m.MaKM=k.MaKM) ItemCount,k.Revision,
  k.LoaiUuDai Kind,(SELECT COUNT(*) FROM dbo.KHUYENMAI_MON_TANG g WHERE g.MaKM=k.MaKM) GiftItemCount,
  (SELECT COALESCE(SUM(g.SoLuong),0) FROM dbo.KHUYENMAI_MON_TANG g WHERE g.MaKM=k.MaKM) GiftQuantity
 FROM dbo.KHUYENMAI k
 CROSS APPLY (SELECT CASE
   WHEN k.TrangThai='TamDung' THEN 'TamDung'
   WHEN dbo.fn_NgayTrongKy(@Today,k.NgayBD,k.NgayKT)=1 THEN 'DangChay'
   WHEN dbo.fn_NgaySauKy(@Today,k.NgayBD,k.NgayKT)=1 THEN 'HetHan'
   ELSE 'ChuaToiKy' END TrangThai) s
 WHERE (k.TenKM LIKE N'%'+@Search+N'%' ESCAPE N'~' OR k.MaKM LIKE N'%'+@Search+N'%' ESCAPE N'~')
  AND (@OnDate IS NULL OR dbo.fn_NgayTrongKy(@OnDate,k.NgayBD,k.NgayKT)=1)
  AND (@Status IS NULL OR s.TrangThai=@Status)
 ORDER BY k.MaKM OFFSET @Skip ROWS FETCH NEXT 50 ROWS ONLY;
END;
GO
GRANT EXECUTE ON dbo.sp_PromotionList TO CafeManagerApp;
GO
