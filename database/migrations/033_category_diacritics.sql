-- Hiển thị tên danh mục món có dấu tiếng Việt trên trang Bán hàng; chỉ đổi nhãn TenDM, giữ nguyên MaDM.
SET NOCOUNT ON; SET XACT_ABORT ON;
IF @@TRANCOUNT=0 OR NOT EXISTS(SELECT 1 FROM dbo.SchemaMigrations WHERE Version=32)
 THROW 52107,N'Cần schema032 và runner migration.',1;
GO
UPDATE dbo.DANHMUC_SP SET TenDM=v.TenDM
FROM (VALUES (N'DM001',N'Cà phê'),(N'DM002',N'Trà sữa'),(N'DM003',N'Sinh tố'),(N'DM004',N'Bánh ngọt'),
 (N'DM005',N'Nước ép'),(N'DM006',N'Nước ngọt'),(N'DM007',N'Trà trái cây'),(N'DM008',N'Đồ uống nóng'),
 (N'DM009',N'Kem - Tráng miệng'),(N'DM010',N'Món ăn nhẹ')) v(MaDM,TenDM)
WHERE v.MaDM=DANHMUC_SP.MaDM;
GO
