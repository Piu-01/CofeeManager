# Plan t05 — Fixture, script SQL và runner kiểm thử

Phụ thuộc t01–t04. Chỉ chạy sau khi người dùng duyệt đợt test. Không sửa source
trong lượt test.

## Bước 1 — Bộ dữ liệu `database/tests/promo-v2/fixture.sql`

Trên DB test dựng từ `DB_Schema.sql` + `Insert_Data.sql` + migration 001–042:
- KM giảm % mùa (01/12–31/12, ALL, 2 món), KM giảm theo hạng (TIER 2 hạng, 0 hạng
  = mọi hạng), KM vãng lai WALKIN, KM tắt (TamDung), KM hết hạn (tháng trước),
  KM vắt năm (20/12–10/01), KM KT NULL.
- KM tặng: 1 món SL 2 (ALL), 2 món (TIER), KM tặng trùng món với KM giảm; KM tặng
  tương lai; KM tặng đã tạm ngưng.
- Khách: vãng lai (đơn không MaKH), khách hạng thấp/cao, khách chưa hạng.
- Đơn mở mẫu có món đủ/không đủ nguyên liệu để kiểm 52203.
- Ghi lại baseline: tổng hóa đơn đã trả, tồn NVL, điểm để so regression.

## Bước 2 — Script kiểm thử

`01-meta-date.sql` (PM01, PM02, PM03)
- Lưu KM chỉ dd/MM; đọc lại thấy BD năm 2000, KT năm 2000/2001 đúng quy ước.
- Hôm nay trong/ngoài mùa; mùa vắt năm (dùng ngày lập giả lập); KT NULL.
- KM `Nhap` cũ đã chuyển `DangChay` sau migration; FIRST/MEMBER → ALL.
- Đối tượng: ALL nhận vãng lai; WALKIN chỉ vãng lai; TIER khớp 1/nhiều hạng;
  TIER 0 hạng áp mọi hạng; FIRST/MEMBER không còn hợp lệ khi ghi mới (52001).

`02-discount-gift-preview.sql` (PM04, PM05, PM06)
- Giảm theo 1/nhiều món; bỏ trống = tất cả; trần 100%/dòng; ship không giảm.
- `sp_DiscountPreview` trả danh sách quà dự kiến; km tặng không cộng vào Discount.
- Token preview đổi khi sửa bảng `KHUYENMAI_MON_TANG` (revision/digest).

`03-checkout-gift.sql` (PM05, PM07)
- Checkout đơn có quà: chèn dòng `GiaBan=0, MaKMQuaTang`, tổng/ThanhTien không đổi,
  TIEUHAO cộng đúng công thức quà, tồn giảm, điểm chỉ tính tiền món bán.
- Hai KM tặng cùng lúc: mỗi KM một lần; hai dòng cùng món vẫn ghi đủ.
- Pause KM tặng sau preview: token cũ → 52201, rollback không có dòng quà/thanh toán.
- Thiếu nguyên liệu do quà: 52203, rollback sạch, đơn còn mở.
- Hóa đơn đã trả: sửa/xóa dòng quà bị chặn 51001/51002; xóa KM tặng đã dùng bị
  52006.
- `sp_InvoiceDetail` đơn mở trả set 6 quà dự kiến; đơn trả dòng quà có MaKMQuaTang;
  `sp_SalesReadCore`/`sp_InvoiceList` đọc không lỗi.

`04-regression.sql` (PM07)
- Hóa đơn seed và đã trả trước 042 giữ nguyên tổng/giảm/điểm; báo cáo ngày,
  sản phẩm, tồn kho chạy được; không tồn âm.
- KM 039 cũ (ALL/TIER, không quà) vẫn áp đúng sau migration.

## Bước 3 — Runner

`tools/database/Invoke-PromoV2.ps1` (mô phỏng `Invoke-PromoAuto.ps1`):
- Tạo DB `CoffeeManager_Test_PromoV2_<runId>`, chạy schema+seed, `Apply-Migration.ps1`
  tới 42, chạy fixture + 4 script, ghi `runs/<timestamp>_<hash>/manifest.json`
  (Status, SourceUnchanged, Cleaned, thời gian từng bước).
- Không ghi vào DB sử dụng thật; luôn drop DB test cuối lượt (kể cả fail, log mã).
- Không dùng NOLOCK; hai session cho ca pause-race nếu bổ sung.

## Bước 4 — Ca và biên bản

`docs/tasks/promotion-v2/test-results.md`:
- Bảng PM01–PM08, expected/actual, PASS/FAIL/BLOCKED/NOT RUN, bằng chứng run id.
- PM08 = giao diện/DPI 125% chức năng; hình thức ghi SKIPPED theo phân công người
  dùng tự kiểm.
- PM06 UI (F3 chọn khách, POS/checkout/in liệt kê KM/quà) chạy thủ công trên app
  thật nếu môi trường cho phép; agent không thao tác GUI WinForms — ghi BLOCKED và
  chuyển người dùng kiểm như tiền lệ PA14.

## Điều kiện hoàn tất

- PM01–PM07 PASS (hoặc lỗi ghi bug-report của task) trên DB test riêng; PM08/PM06-GUI
  theo phân công.
- Không đổi expected để né lỗi; không sửa source trong lượt test.
