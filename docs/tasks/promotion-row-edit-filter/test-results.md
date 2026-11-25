# Kết quả test — promotion-row-edit-filter (đợt test được duyệt 25/11/2026)

- Source: commit `6ac94c6` + thay đổi chưa commit của task (UI `LoyaltyControl.cs`,
  `LoyaltyService.cs`, `LoyaltyRepository.cs`, migration 047). Source giữ nguyên
  trong suốt lượt test.
- Build: Debug (bản tách riêng `C:\Users\Lenovo\.qoder\tmp\coffee-build-debug`,
  không chiếm lock bản dev) — 0 lỗi; Release — 0 lỗi (đã build trước lượt test).
- DB test: `CoffeeManager_Test_Promo_20261125` (bản sao DB dev tại thời điểm test,
  `SchemaMigrations` 47/45/44, 17 khuyến mãi seed, 3.459 hóa đơn lịch sử).
  Không ghi gì vào DB đang sử dụng (`CoffeeManager_Development`).
- Tài khoản: `admin` (ADMIN), `thungan` (CASH).
- Hạn chế môi trường: máy không có dữ liệu bán hôm nay nên dashboard "Hôm nay" rỗng;
  không ảnh hưởng kết quả SQL.

## 1. SQL — `sp_PromotionList` sau migration 047

Chạy trực tiếp trên DB test bằng `sqlcmd` với `SecurityStamp` của tài khoản
(`C:\Users\Lenovo\.qoder\tmp\cm-dialog-tests\promo-sql.sql`).

| Mã | Ca | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| P-01 | Migration 047 trên DB test | Proc 6 tham số, grant cho app | Đã áp dụng, `SchemaMigrations` có 47; đủ `@Status` | PASS |
| P-02 | ADMIN, `@Status=NULL` | Trả đủ 17 KM kèm trạng thái tính theo ngày server | 17 dòng; 3 `DangChay`, 2 `ChuaToiKy`, 12 `HetHan` | PASS |
| P-03 | ADMIN, `DangChay` | Chỉ KM đang trong kỳ | KM26BF, KM26HRO, KM26STU | PASS |
| P-04 | ADMIN, `TamDung` | Chỉ KM tạm dừng | Tạm đặt KM26HRO=`TamDung`: trả đúng 1 dòng; `DangChay` còn 2 dòng; đã khôi phục `DangChay` | PASS |
| P-05 | ADMIN, `ChuaToiKy` | KM chưa tới kỳ | KM24BF, KM24NEL | PASS |
| P-06 | ADMIN, `HetHan` | KM đã qua kỳ | 12 dòng, không lẫn `DangChay` | PASS |
| P-07 | ADMIN, `@Status=''` | Rỗng = tất cả | 17 dòng như P-02 | PASS |
| P-08 | ADMIN, tìm "Black" + `DangChay` | Kết hợp tìm kiếm và lọc | Đúng 1 dòng KM26BF | PASS |
| P-09 | ADMIN, `@Status='Foo'` | Từ chối giá trị lạ | Lỗi 52001 "Trạng thái lọc không hợp lệ." | PASS |
| P-10 | CASH, `@Status='HetHan'` | Bỏ qua lọc, chỉ KM áp dụng hôm nay | Đúng 3 KM đang áp dụng | PASS |

## 2. UI — ADMIN

Chạy lại toàn bộ mục này ngày 25/11/2026 sau khi sửa BUG-01 (người dùng đã duyệt),
trên instance tách riêng của task trỏ DB test; script UIA trong
`C:\Users\Lenovo\.qoder\tmp\cm-dialog-tests\` (`Promo-B.ps1`, `Promo-E.ps1`,
`Promo-Dlg*.ps1`, `Promo-EditSave.ps1`, `Promo-DelFull.ps1`, `Promo-Page.ps1`).

| Mã | Ca | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| P-11 | Mở màn "Ưu đãi / Điểm" | Màn Khuyến mãi mở, có lưới và combo lọc | Mở bình thường; 1 combo "Lọc theo trạng thái", lưới 17 dòng, có "Thêm", không còn Sửa/Xóa | PASS |
| P-12 | Combo lọc có 5 nhãn Tất cả/Áp dụng/Tạm ngưng/Chưa tới kỳ/Hết hạn | Hiện đủ, mặc định "Tất cả" | Đủ 5 nhãn đúng thứ tự, mặc định "Tất cả"; chọn bằng bàn phím đọc lại đúng nhãn | PASS |
| P-13 | Lọc từng trạng thái + Tất cả trên lưới | Danh sách khớp trạng thái hiển thị | Áp dụng = 3 (KM26BF/HRO/STU), Tạm ngưng = 0, Chưa tới kỳ = 2 (KM24BF/NEL), Hết hạn = 12, Tất cả = 17; cột Trạng thái từng dòng khớp bộ lọc và khớp SQL P-03…P-06 | PASS |
| P-14 | Đổi lọc quay về trang 1 | Trang về 1 sau khi đổi | 17 dòng/1 trang: "Trang trước"/"Trang sau" luôn tắt; đổi Hết hạn (12) rồi Tất cả (17) vẫn trang 1; code `ResetAndReload` đặt lại trang 0 | PASS |
| P-15 | Double-click / F2 mở hộp sửa đúng dòng | Mở "Sửa khuyến mãi · <mã>", giữ Đối chiếu | Double-click dòng 1 → "Sửa khuyến mãi · KM24BF"; chọn dòng + F2 dòng 2 → "Sửa khuyến mãi · KM24NEL"; có Đối chiếu; Hủy không đổi dữ liệu, lưới giữ 17 dòng; đổi trạng thái rồi Hủy hiện xác nhận bỏ nháp (Đồng ý → đóng) | PASS |
| P-16 | Thùng rác xóa có xác nhận | Hộp "Xóa khuyến mãi" + xác nhận, xóa xong tải lại | KM tạo qua UI (`KM00000001`): rê chuột vào ô thùng rác → hộp "Xóa khuyến mãi" nêu mã/tên và ràng buộc hóa đơn; Lưu → xác nhận lồng "Xóa KM00000001?"; Đồng ý → hộp đóng, lưới 0 dòng, DB còn đúng 17 KM (đã xóa thật) | PASS |
| P-17 | Không còn nút Sửa/Xóa trên thanh công cụ | Chỉ còn Thêm + Đối chiếu theo dòng | Thanh công cụ chỉ có "Thêm"; Sửa/Xóa nằm ở double-click/F2 và thùng rác từng dòng | PASS |
| P-18 | Tìm kiếm kết hợp lọc trên UI | Kết quả giao của hai điều kiện | "Black" + Áp dụng = 1 dòng KM26BF; "Black" + Hết hạn = 0 dòng; xóa từ khóa + Tất cả = 17 dòng | PASS |
| P-22 | Combo Trạng thái trong hộp Sửa khuyến mãi | Hiện "Áp dụng"/"Tạm ngưng", chọn và lưu được | Combo đúng 2 mục (Down → "Tạm ngưng", Down tiếp đứng yên, Up về "Áp dụng"); KM24BF hiện "Áp dụng" (trạng thái cấu hình, khác nhãn hiệu lực do SQL tính — đúng thiết kế). Lưu thử trên mã số `KM00000001`: chọn "Tạm ngưng" → Lưu → hộp đóng, lưới hiện "Tạm ngưng", DB `TamDung`; mã cũ KM24NEL lưu được sau 048 (P-27) | PASS (phần combo + lưu mã hợp lệ); BUG-02 riêng |

Bổ sung khi chạy lại: nút "Thêm" tạo mới tròn vòng — điền tên "Kiem thu tao UI",
tỷ lệ 10, ngày mặc định → Lưu → sinh mã `KM00000001`, lưới 18 dòng, trạng thái
"Áp dụng"; sau khi sửa trạng thái và xóa qua thùng rác, DB về 17 KM như trước test.
Đổi trạng thái KM24NEL trên hộp sửa hiện lỗi chung nhưng nguyên nhân ở SQL (BUG-02),
không phải UI/DAL/BLL của task — đối chứng bằng `KM00000001` lưu thành công.

## 3. UI — CASH và hồi quy

| Mã | Ca | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| P-19 | CASH đăng nhập, menu điều hướng | CASH không có mục màn quản trị Khuyến mãi (giữ nguyên thiết kế menu) | Menu CASH: Bán hàng, Lịch sử hóa đơn, Giao hàng, Khách hàng, Tồn phục vụ bán; không có Ưu đãi / Điểm | PASS (theo thiết kế) |
| P-20 | CASH mở F3 "Khách / Ưu đãi" trong POS | Hộp chọn khách mở bình thường | Mở được, 23 dòng khách, nút Áp dụng/Quay lại/Thêm khách | PASS (hồi quy hẹp) |
| P-21 | Nhánh CASH của `sp_PromotionList` không đổi | Chỉ KM áp dụng hôm nay | Xem P-10 | PASS |

Ghi chú P-19/P-20: màn quản trị Khuyến mãi vốn không thuộc menu CASH, nên yêu cầu
"CASH không thấy bộ lọc" được bảo đảm ở mức điều hướng; nhánh lọc CASH trong SQL
giữ nguyên và đã kiểm ở P-10.

## 4. Hình thức và DPI

- SKIPPED theo phân công (người dùng tự kiểm hình thức). Không chạy 100%/150%.
- Không chụp ảnh đối chiếu thay người dùng.

## 5. Sửa BUG-02 — migration 048 (được duyệt 25/11/2026)

DB test `CoffeeManager_Test_Promo_20261125` và `CoffeeManager_Development` đều đã áp
`048_promotion_legacy_code_edit.sql` (`Apply-Migration.ps1 -Version 48`, cùng checksum).
SQL chạy bằng `sqlcmd` (`v48.sql`), UI chạy lại trên build Debug tách riêng trỏ DB test.

| Mã | Ca | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| P-23 | Sửa trạng thái KM mã cũ qua SQL (`sp_PromotionUpdate` KM24NEL → TamDung, rồi trả DangChay) | Cập nhật được, revision tăng | V01/V02 thành công; `TrangThai` lần lượt `TamDung` → `DangChay` | PASS |
| P-24 | Pause/Resume mã cũ KM24BF | Đổi trạng thái được | V03: `DangChay` → `TamDung` → `DangChay` | PASS |
| P-25 | Chặn vẫn đúng: tạo mới mã cũ, sửa mã không tồn tại | Bị từ chối đúng lý do | V04 lỗi 52001 "Mã KM và 8 chữ số"; V05 lỗi 51603 "Chương trình đã thay đổi" | PASS |
| P-26 | Tạo + xóa mã số `KM20260001` | Tròn vòng, dữ liệu sạch | V06 tạo/xóa OK, còn 0 dòng mã đó, tổng 17 KM | PASS |
| P-27 | UI: sửa KM24NEL sang "Tạm ngưng" rồi trả "Áp dụng" | Lưu được, lưới/DB khớp | Lưu lần 1: hộp đóng, lưới "Tạm ngưng", DB `TamDung`; lần 2: lưới "Chưa tới kỳ" (hiệu lực 20/12, đúng SQL), DB `DangChay` | PASS |
| P-28 | Hồi quy danh sách sau 048 | Lọc giữ đúng 3/0/2/12/17 | Đúng; 17 dòng khi Tất cả | PASS |

## 6. Tổng hợp

- PASS: P-01…P-28 (P-19…P-21 hồi quy CASH; P-23…P-28 cho BUG-02).
- Tiến trình: lượt 1 P-11 FAIL (BUG-01) → duyệt sửa → sửa BUG-01 (kèm lỗi lệch bậc
  `statusCodes`) → lượt 2 mục 2 PASS → phát hiện BUG-02 → duyệt sửa → migration 048 →
  lượt 3 P-23…P-28 PASS.
- Không còn lỗi mở trong phạm vi task. Đề xuất riêng chưa làm: tách thông điệp 52001
  thành câu cụ thể hơn; đổi nhãn nút chốt của hộp xóa từ "Lưu" thành "Xóa" (áp chung
  mọi màn master data, cần quyết định UI chung).
- Hình thức/DPI: SKIPPED theo phân công; không chụp ảnh đối chiếu, không chạy
  100%/150%.
- Sau đợt test (yêu cầu người dùng, ảnh chụp cho thấy dải trống dưới ô lọc):
  `PromotionList` đặt `Workspace.ShowMeta=false` để bỏ hàng meta 49px mà hai nút
  Sửa/Xóa cũ để lại (giống `TablesControl`/`MaterialsControl`). Build Debug 0 lỗi;
  hình thức sau đổi do người dùng tự kiểm, agent không nghiệm thu thị giác.
- Chưa commit/push thay đổi của task.
