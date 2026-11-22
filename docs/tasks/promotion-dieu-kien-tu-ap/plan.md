# Plan — Ưu đãi có điều kiện, tự áp dụng, bật/tạm dừng

- Ngày: 21/11/2026
- Trạng thái: đã duyệt cùng requirements 21/11/2026; bắt đầu triển khai theo thứ tự dưới đây
- Tiền đề: migration 038 đã ship, DB development schema 38; 039 guard `IF NOT EXISTS(Version=38) THROW`; không sửa 038

## Quyết định thiết kế

| Chủ đề | Quyết định |
|---|---|
| Hiệu lực | `NgayBD` bắt buộc, `NgayKT` NULL = vô hạn; so theo `HOADON.NgayLap`, hai đầu inclusive |
| Trạng thái | `Nhap` / `DangChay` / `TamDung`; `HetHan` suy ra; chỉ `DangChay` tự áp |
| Bật/tắt | `Pause`/`Resume` qua procedure có RequestId + ExpectedRevision (audit/replay) |
| Khách lần đầu | Có hồ sơ và chưa từng có hóa đơn `Da thanh toan`; vãng lai không đạt |
| Phạm vi giảm | Ưu đãi món chỉ trên dòng đủ điều kiện |
| Nhiều ưu đãi cùng dòng | Cộng %, chặn 100%/dòng; làm tròn một lần/dòng; phân bổ tiền theo tỷ trọng %, lẻ dồn KM cuối |
| Hạng | `TierDiscount = ROUND(TongTien × %hạng / 100)`; vượt tiền món thì cắt phần KM |
| Ship / điểm | Ship không giảm; điểm `FLOOR((TongTien − GiamGia)/1000)` |
| Đơn cũ | Không backfill; hóa đơn đã trả giữ nguyên |
| Đơn mở có `MaKM` trước 039 | Legacy "tất cả món", tiếp tục tính, UI không sửa |
| Dữ liệu hiện có sau 039 | `DangChay / ALL / ALL`, giữ ngày ⇒ tự áp cho đơn còn hạn |

### Quy tắc nhiều khuyến mãi trên cùng hóa đơn (bắt buộc)

- Một hóa đơn áp **0..N ưu đãi**; N chỉ giới hạn bởi điều kiện, không cần chọn tay, không loại trừ nhau.
- Ưu đãi khác đối tượng cùng thỏa thì cùng áp; khác phạm vi món thì mỗi ưu đãi giảm trên dòng của mình.
- Nhiều ưu đãi trùng cùng dòng: cộng % và chặn 100% cho dòng; không chọn "cái cao nhất".
- Snapshot/đọc đơn/checkout/in đều trả **danh sách** ưu đãi.
- Điều kiện được xét lại mỗi lần mở đơn/đổi khách/đổi món/đổi trạng thái.

## Các bước

### Bước 1 — DDL trong migration 039

1. `KHUYENMAI`: `TrangThai varchar(10) NOT NULL DEFAULT 'DangChay'` + CHECK; `ALTER COLUMN NgayKT date NULL`; CHECK ngày mới `(NgayKT IS NULL OR NgayKT>=NgayBD)`; `DoiTuongKhach varchar(10) DEFAULT 'ALL'` (`ALL/FIRST/MEMBER/TIER`); `PhamVi varchar(4) DEFAULT 'ALL'` (`ALL/ITEM`).
2. `KHUYENMAI_HANG(MaKM, MaHang, PK)`; `KHUYENMAI_MON(MaKM, MaSP, PK)`.
3. `HoaDonUuDaiApDung(MaHD, MaKM, PromotionRevision, Audience, Scope, Percent, BaseAmount, Discount, RecordedAt, PK(MaHD,MaKM))`.
4. `HoaDonUuDaiSnapshot` thêm `TierDiscount, PromotionDiscount, PromotionCount, PromotionDigest`; tạo lại CHECK Source/Known cho cả dòng 038 cũ và dòng mới (`WITH NOCHECK`).
5. Trigger `TR_HoaDonUuDaiApDung` cấm UPDATE/DELETE, chặn INSERT sai trạng thái/tổng Discount lệch snapshot.
6. `TYPE dbo.PreviewLines(MaSP, SoLuong)` + GRANT.
7. DENY DML trực tiếp trên 3 bảng mới cho `CafeManagerApp`.
8. Thay đổi bảng con/trạng thái bump `Revision` bằng `UPDATE KHUYENMAI SET TenKM=TenKM`.

### Bước 2 — Hàm dùng chung

`fn_HoaDonDong`, `fn_UuDaiDong`, `fn_UuDaiTong`, `fn_HoaDonGiamGia`, `fn_HoaDonUuDaiApDung`. Điều kiện duy nhất trong `fn_UuDaiDong`: `TrangThai='DangChay'` + ngày + đối tượng + phạm vi + legacy. Bỏ `fn_DiscountPreview`.

### Bước 3 — Procedure ưu đãi

`sp_DiscountPreview` nhận TVP dòng món; `sp_PromotionList` trả trạng thái/điều kiện; `sp_PromotionGet` 3 result set; `sp_PromotionWrite` nhận điều kiện + action Pause/Resume, ghi bảng con, bump Revision.

### Bước 4 — Procedure bán hàng

Bỏ `@Promotion` khỏi validate/header; bỏ kiểm 52105; refresh bằng `fn_HoaDonGiamGia`; `sp_SalesReadCore` + `sp_SalesOrderGet` trả `PromotionCount` và result set 4 danh sách ưu đãi.

### Bước 5 — Preview, checkout, hóa đơn

`fn_CheckoutPreview` thêm cột tiền thành phần; token gồm catalog digest KM + digest ưu đãi áp; `sp_SalesCheckoutPreview` khóa catalog, trả danh sách; `sp_ThanhToanHoaDon` ghi snapshot 1 dòng + N dòng con; `sp_InvoiceDetail` trả danh sách + cột mới.

### Bước 6–8 — Models, DataAccess, Business

- DTO `AppliedPromotion`, `PromotionDetail`, `PreviewLine`; mở rộng `PromotionRow/Request`, `CheckoutPreview`, `SalesOrderHeader`, `SalesOrderDto`, `InvoiceDetailDto`; bỏ `Promotion` khỏi `OrderHeaderInput`.
- Repo map cột mới, TVP preview, state action, đọc thêm result set.
- Service chuẩn hóa điều kiện, truyền dòng món, bật/tạm dừng.

### Bước 9 — UI Ưu đãi

Cột Trạng thái/Đối tượng/Phạm vi; action Bật–Tạm dừng; editor đối tượng/hạng/món/không giới hạn; preview có dòng món giả lập.

### Bước 10 — POS/checkout

Bỏ combo KM và load KM trong `OrderHeaderDialog`; chuyển layout sang `OrbitDialogLayout` + `FitToContent`; tiêu đề "Khách hàng / Giao hàng"; hiển thị danh sách ưu đãi tự áp ở POS/checkout.

### Bước 11 — In/chi tiết hóa đơn

Liệt kê từng ưu đãi; giữ nhánh legacy.

### Bước 12 — Localization + tài liệu

`en.json`, KeHoach, UX, các contract DB, migrations.md.

### Bước 13 — Test mới (chờ duyệt đợt test riêng)

Bộ `database/tests/promo-auto/*` + biên bản `test-results.md`; gồm đa ưu đãi, điều kiện, trạng thái, làm tròn, token, báo cáo, legacy, đồng thời.

Đã chạy 22/11/2026: PA01–PA13 PASS (`runs/20261122_135707_532a959f`);
PA14 NOT RUN (cần GUI), PA15 do người dùng tự kiểm.

### Bước 14 — Build, áp DB development

Build sạch; chờ duyệt đợt test; PASS mới áp 039 lên `CoffeeManager_Development`.

Đã làm 22/11/2026: build 0 lỗi; áp 039 lên development khi app đóng
(`Applied`, checksum `D2D1E735…86F0D1`, backup trước áp), dữ liệu không đổi.

## Kiểm chứng đầu-cuối

- DB test: baseline + 038 + 039; 3 ưu đãi (khách lần đầu 20% SP001, hạng VANG 10% SP002, mọi khách 5% toàn bộ); đơn khách mới hạng VANG (5%) SP001 50k + SP002 30k → 4k + 10k + 3k + 4k = 21k.
- Tạm dừng ưu đãi mọi khách → tổng tăng đúng 4k.
- Token đổi khi sửa/tạm dừng; checkout cũ → 52201.
- POS F3 gọn, không còn chọn KM; đổi khách cập nhật ưu đãi/tổng.
- Báo cáo tồn kho/doanh thu cùng dữ liệu không đổi số.
