# Plan t01 — Migration 042

File: `database/migrations/042_promotion_v2.sql`. Kiểm tra tiền tố: bắt buộc
`SchemaMigrations` có Version 41 và các object 039 hiện diện; lỗi 53307 như 039.
Mỗi `ALTER TABLE`/`ADD CONSTRAINT` một batch `GO` riêng.

## Bước 1 — DDL

1. `KHUYENMAI`:
   - Thêm `LoaiUuDai varchar(10) NOT NULL DEFAULT 'Giam'`, CHECK `IN('Giam','Tang')`.
   - Drop `CK_KHUYENMAI_TrangThai`, tạo lại `IN('DangChay','TamDung')`.
   - Drop `CK_KHUYENMAI_DoiTuong`, tạo lại `IN('ALL','TIER','WALKIN')`.
   - Drop `CK_KHUYENMAI_NgayHopLe` (không còn so trực tiếp do vắt năm).
2. Bảng `KHUYENMAI_MON_TANG(MaKM char(10), MaSP char(5), SoLuong int CHECK 1..999,
   PK(MaKM,MaSP), FK KM ON DELETE CASCADE, FK SANPHAM)`; `DENY INSERT/UPDATE/DELETE`
   cho `CafeManagerApp`.
3. `CHITIET_HD`:
   - Thêm `MaKMQuaTang char(10) NULL` + FK `KHUYENMAI` (NO ACTION).
   - Drop `CHK_CTHD_GiaBan`, tạo `CHECK(GiaBan>=0)`; index lọc
     `IX_CHITIET_HD_MaKMQuaTang` (WHERE MaKMQuaTang IS NOT NULL) để tra quà.
4. `HoaDonUuDaiApDung`: drop/recreate `CK_HDUD_Audience` còn `ALL/TIER/WALKIN`.

## Bước 2 — Chuẩn hóa dữ liệu cũ (một lần)

```sql
UPDATE KHUYENMAI SET TrangThai='DangChay' WHERE TrangThai='Nhap';
UPDATE KHUYENMAI SET DoiTuongKhach='ALL' WHERE DoiTuongKhach IN('FIRST','MEMBER');
-- Ngày: BD → năm 2000; KT → năm 2001 nếu dd/MM nhỏ hơn BD, ngược lại năm 2000.
```
Viết bằng `DATEFROMPARTS(2000,MONTH(NgayBD),DAY(NgayBD))` và CASE cho `NgayKT`.
Không đổi `PhanTramGiam`, không tạo bản ghi ưu đãi mới.

## Bước 3 — Hàm mới

- `fn_NgayTrongKy(@Day date,@Start date,@End date) RETURNS bit`: so `m*100+d`;
  `@End NULL` → `>= start`; vắt năm khi `end<start`.
- `fn_KhachThoaDoiTuong(@Audience varchar(10),@Customer char(5),@MaKM char(10)) RETURNS bit`:
  ALL; WALKIN = khách NULL; TIER = khách có hồ sơ và (không có dòng `KHUYENMAI_HANG`
  hoặc trùng hạng).
- `fn_QuaTangApDung(@BusinessDate date,@Customer char(5))` trả
  `MaKM,MaSP,SoLuong,TenKM`: join `KHUYENMAI` (`LoaiUuDai='Tang'`,
  `TrangThai='DangChay'`, `fn_NgayTrongKy`), `KHUYENMAI_MON_TANG`, `SANPHAM` đang bán.
- `fn_HoaDonQuaTang(@Code char(10))` trả `MaKM,MaSP,SoLuong,TenKM`:
  - Đơn mở: gọi `fn_QuaTangApDung` bằng ngày lập/khách của đơn.
  - Đơn đã trả: đọc dòng `CHITIET_HD.MaKMQuaTang IS NOT NULL`.
- `fn_HoaDonQuaTangDigest(@Code char(10))`: hash danh sách trên (MaKM:MaSP:SL).

## Bước 4 — Sửa hàm ưu đãi 039

- `fn_UuDaiCap`: thêm `k.LoaiUuDai='Giam'`; thay điều kiện `BETWEEN` bằng
  `fn_NgayTrongKy`; thay khối đối tượng bằng `fn_KhachThoaDoiTuong`; bỏ nhánh
  FIRST/MEMBER. Nhánh legacy `MaKM` giữ nguyên (vẫn kiểm `fn_NgayTrongKy`).
- `fn_HoaDonGiamGia`, `fn_HoaDonUuDaiApDung`, `fn_UuDai*`: chỉ đổi điều kiện ngày
  trong chuỗi gọi (gián tiếp qua `fn_UuDaiCap`, không đổi chữ ký).
- `fn_PromotionCatalogDigest`: giữ; thêm digest bảng `KHUYENMAI_MON_TANG`
  (MaKM:MaSP:SL) vào chuỗi hash để sửa quà làm đổi token.
- `sp_DiscountPreview`: truyền ngày qua, dùng cùng hàm mới; kết quả thêm cột
  `GiftCount`, set 3 trả quà dự kiến (MaKM,TenKM,MaSP,TenSP,SoLuong).
- `sp_PromotionList`: thêm `k.LoaiUuDai`, `GiftItemCount`, `GiftQuantity`;
  lọc CASH dùng `fn_NgayTrongKy`; trạng thái `HetHan` suy theo hôm nay.
- `sp_PromotionGet`: set 1 thêm `LoaiUuDai,GiftItemCount,GiftQuantity`; set 4 trả
  `KHUYENMAI_MON_TANG` (MaSP,TenSP,GiaBan,TrangThai,SoLuong).
- `sp_PromotionWrite` (Create/Update):
  - Tham số mới `@Kind varchar(10)`, `@GiftJson nvarchar(max)`.
  - Chuẩn hóa ngày: `@StartDate` bắt buộc, đưa về năm 2000; `@EndDate` NULL hoặc
    cùng ngày/tháng, tự chọn năm 2001 nếu dd/MM kết thúc < bắt đầu.
  - Hợp lệ: Kind `Giam/Tang`; status `DangChay/TamDung`; audience
    `ALL/TIER/WALKIN`; TIER cho phép 0 hạng (= mọi hạng); non-TIER cấm hạng.
  - `Giam`: giữ `ItemsJson` (0–500 món), `Discount` 0–100, `GiftJson` phải rỗng.
  - `Tang`: bắt buộc 1–50 dòng quà, SL 1–999, món đang bán; `Discount=0`,
    `ItemsJson` rỗng, `PhamVi='ALL'`.
  - Ghi `KHUYENMAI_MON_TANG` cùng transaction; payload hash/idempotency thêm
    `Kind`+`GiftJson`; giữ action Pause/Resume (không dùng từ UI).
  - `Delete`: chặn nếu KM đã có trong `HoaDonUuDaiApDung` **hoặc**
    `CHITIET_HD.MaKMQuaTang`.

## Bước 5 — Checkout và hóa đơn

- `fn_CheckoutPreview`: thêm `GiftCount`/`GiftDigest` vào token; vẫn tính tiền
  giảm như 039.
- `sp_SalesCheckoutPreview`: trả thêm set danh sách quà dự kiến.
- `sp_ThanhToanHoaDon`, sau khi kiểm token và trước bước tính tiêu hao:
  1. Lấy quà từ `fn_QuaTangApDung` (ngày lập/khách).
  2. Chèn dòng `CHITIET_HD(MaHD,MaSP,SoLuong,GiaBan=0,GhiChu=N'Tặng kèm',MaKMQuaTang)`.
  3. Sau đó mới tính `@consume` để quà trừ kho đúng.
  4. Không thêm điểm/không đổi `ThanhTien`; snapshot 039 giữ nguyên.
- `sp_InvoiceDetail`: set 2 thêm cột `MaKMQuaTang` (giữ 7 cột cũ + 1 cột cuối);
  thêm set 6 `GiftProposal` chỉ khi đơn mở.
- `sp_SalesReadCore`: set 2 thêm `MaKMQuaTang`; không đổi 28 cột header.
- `sp_InvoiceList`: không đổi cột (quà không ảnh hưởng tổng).
- Grant EXECUTE lại các object đã ALTER; grant SELECT cột mới cho app qua role hiện có.

## Đầu ra và kiểm chứng kỹ thuật

- `042` áp được trên DB test dựng mới từ schema+seed (chưa test đầy đủ — thuộc t05).
- Kiểm tra tĩnh: không còn tham chiếu `FIRST`/`MEMBER`/`Nhap` trong object 042;
  `BETWEEN NgayBD` không còn trong các hàm ưu đãi.
- Build không đổi (task SQL thuần); cập nhật `docs/tasks/promotion-v2/report.md`
  ghi kết quả áp thử nếu được phép.
- Rủi ro: nới `CHK_CTHD_GiaBan` cho phép dòng 0 ₫ ngoài quà; kiểm soát bằng quy ước
  `MaKMQuaTang` và chỉ procedure checkout ghi; ghi rõ trong báo cáo.
