-- Fixture test-only: đưa nhãn 10 mã chuẩn về giá trị seed trước fix (git HEAD) và thêm DM011 tùy biến.
-- Chỉ chạy trên DB test của run này. Không dùng cho DB development/sử dụng thật.
SET NOCOUNT ON;
UPDATE dbo.DANHMUC_SP SET TenDM = v.TenDM
FROM (VALUES
 (N'DM001', N'Ca phe'),
 (N'DM002', N'Tra sua'),
 (N'DM003', N'Sinh to'),
 (N'DM004', N'Banh ngot'),
 (N'DM005', N'Nuoc ep'),
 (N'DM006', N'Nuoc ngot'),
 (N'DM007', N'Tra trai cay'),
 (N'DM008', N'Do uong nong'),
 (N'DM009', N'Kem - Trang mien'),
 (N'DM010', N'Mon an nhe')) v(MaDM, TenDM)
WHERE v.MaDM = DANHMUC_SP.MaDM;

IF NOT EXISTS (SELECT 1 FROM dbo.DANHMUC_SP WHERE MaDM = N'DM011')
    INSERT dbo.DANHMUC_SP (MaDM, TenDM) VALUES (N'DM011', N'Danh muc tu tao');
