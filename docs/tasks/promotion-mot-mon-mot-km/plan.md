# Plan — Mỗi món một khuyến mãi, hạng tính sau khuyến mãi

- Ngày: 23/11/2026
- Dựa trên: `requirements.md` cùng thư mục (đã duyệt)
- Migration kế tiếp: **044** (043 đã là `043_purchase_receiving.sql`)
- Thứ tự triển khai: SQL → DAL/BLL/UI rà soát → test → tài liệu → build/áp dev

## 0. Quyết định thiết kế chốt trong plan

1. **Hàm chọn tổ hợp** `fn_UuDaiChon(@BusinessDate,@Customer,@LegacyPromotion,@LinesJson)`
   trả `(No, MaKM, [Percent], Amount)` là các cặp (dòng, KM) của tổ hợp tối ưu:
   - Ứng viên: KM `Giam`, `DangChay`, đúng ngày/đối tượng, `PhanTramGiam>0`; KM legacy
     được thêm như ứng viên phạm vi `ALL` (chỉ khi đang trong kỳ, đúng như 039 đang đọc).
   - Phủ dòng: `PhamVi='ALL'` phủ mọi dòng; `ITEM` phủ dòng có `MaSP` trong
     `KHUYENMAI_MON`; dòng giá 0/không có vẫn giữ trong danh sách nhưng không tạo giảm.
   - Tách **nhóm xung đột** theo đồ thị "hai KM cùng phủ ít nhất một dòng"; mỗi nhóm
     giải độc lập để giảm tổ hợp cần duyệt.
   - Trong mỗi nhóm: duyệt chính xác các tập con rời nhau bằng bitmask khi số KM của
     nhóm ≤ 15 (2^15 = 32.768 tập, chấp nhận được); chọn tổng `ROUND(amount × pct/100)`
     lớn nhất; hòa chọn ít KM hơn rồi so chuỗi mã tăng dần. Nhóm > 15 KM: dùng tham lam
     tất định (KM có phần giảm riêng cao trước, rồi bỏ KM xung đột; thử thay thế một lượt)
     và ghi rõ trong tài liệu là giới hạn cấu hình, không cộng dồn sai luật.
   - Kết quả tất định, không phụ thuộc thứ tự đọc bảng.
2. **Giảm tuần tự**: KM chọn giảm từng dòng trước
   (`promoLine = ROUND(amount × pct/100)`), `PromotionDiscount = min(Σ promoLine, TongTien)`;
   `TierDiscount = ROUND((TongTien − PromotionDiscount) × tier/100)`;
   `Discount = PromotionDiscount + TierDiscount`. Ship/điểm/phi đối tác giữ nguyên.
3. **`fn_UuDaiCap`** giữ chữ ký, nhưng chỉ trả cặp của tổ hợp được chọn (mỗi dòng tối đa
   một KM). `fn_UuDaiApDung`/`fn_UuDaiChia` giữ contract phân bổ như 039.
4. **`fn_UuDaiDong`** đổi nghĩa nội bộ: `[Percent]`/`Discount` là phần KM của dòng (không
   còn cộng hạng); hạng do `fn_UuDaiTong` tính trên tổng còn lại. Ghi chú trong comment SQL
   và contract để tránh hiểu nhầm.
5. **`PercentDiscount`** (hiển thị) = mức giảm thực tế `ROUND(100 × Discount / TongTien)`
   làm tròn 0 chữ số; ràng buộc `CK_DiscountSnapshot_Known` bỏ công thức cộng cũ, chỉ còn
   kiểm khoảng 0–100 và `Discount = TierDiscount + PromotionDiscount` (snapshot cũ không
   backfill).
6. **Legacy**: đơn mở có `HOADON.MaKM` tham gia tổ hợp như KM `ALL`; không còn ép cộng
   thêm. Đơn đã chốt đọc snapshot cũ không đổi.

## Bước 1 — Migration 044: hàm chọn tổ hợp và luật tính mới

**File:** `database/migrations/044_promotion_one_per_item.sql` (mới)

- Kiểm phụ thuộc: `SchemaMigrations` có Version 43; tồn tại `KHUYENMAI`,
  `KHUYENMAI_MON`, `HoaDonUuDaiApDung`, `HoaDonUuDaiSnapshot`, các `fn_UuDai*`,
  `fn_CheckoutPreview`, `sp_DiscountPreview`, `sp_ThanhToanHoaDon`, `sp_InvoiceDetail`,
  `sp_SalesReadCore`; nếu thiếu thì THROW như 042.
- `CREATE FUNCTION fn_UuDaiChon(...)` theo mục 0.1 (dùng bảng tạm, `WHILE` duyệt bitmask,
  không mở transaction).
- `ALTER FUNCTION fn_UuDaiCap` gọi `fn_UuDaiChon`, trả đúng các cột cũ.
- `ALTER FUNCTION fn_UuDaiDong` (KM theo dòng), `fn_UuDaiTong` (tier sau KM),
  `fn_UuDaiChia`/`fn_UuDaiApDung` (giữ phân bổ, làm tròn dồn KM cuối),
  `fn_HoaDonGiamGia`, `fn_HoaDonUuDaiApDung`, `fn_HoaDonUuDaiDigest` nếu giá trị đổi.
- `ALTER FUNCTION fn_CheckoutPreview`: `PercentDiscount` theo mục 0.5, token vẫn gồm
  digest catalog/ưu đãi/quà; không đổi cột trả về.
- Bỏ/tạo lại `CK_DiscountSnapshot_Known` với luật mới (không backfill dòng cũ).
- `GRANT EXECUTE` cho các procedure bị ALTER; giữ `DENY` bảng nguồn.
- Không sửa file migration 001–043.

**Verify:** áp 044 trên DB test mới dựng 001–043; áp lần 2 báo `AlreadyApplied`;
`sp_helptext` các hàm đúng bản mới; gọi tay `fn_UuDaiTong` với ví dụ 100.000/20%/hạng 5%
ra 24.000 (hạng 4.000); không còn `min(100, tier+promo)` trong file.

## Bước 2 — Rà procedure preview/checkout/đọc hóa đơn

**Khu vực:** `sp_DiscountPreview`, `sp_SalesCheckoutPreview`, `sp_ThanhToanHoaDon`,
`sp_SalesReadCore`, `sp_InvoiceDetail`, `sp_SalesRefreshDiscount` (nếu 044 chưa phủ).

- Không chép lại công thức trong procedure: mọi nơi lấy từ `fn_UuDaiTong`,
  `fn_UuDaiChia`, `fn_HoaDonUuDaiApDung`, `fn_CheckoutPreview`.
- `sp_ThanhToanHoaDon`: ghi snapshot với `PromotionDiscount/TierDiscount/Discount` mới;
  `HoaDonUuDaiApDung` vẫn lấy từ hàm; trigger 039 kiểm tổng vẫn khớp; quà chèn trước
  tính tiêu hao như 042.
- `sp_SalesReadCore`/`sp_InvoiceDetail`: `PreviewPercent`/`PercentDiscount` lấy giá trị
  mới; danh sách KM đã áp trả đúng tập được chọn; hóa đơn đã chốt vẫn đọc snapshot.
- Nếu phát hiện nơi nào còn công thức cũ ngoài phạm vi hàm → ghi blocker, không tự mở rộng.

**Verify:** grep toàn `database/` không còn `TierPercent+PromotionPercent`/công thức cộng;
chạy preview → checkout trên DB test, số liệu khớp giữa ba đường đọc.

## Bước 3 — C#/UI rà soát (không đổi công thức)

**Khu vực:** `src/CafeManager.Business/Services/LoyaltyService.cs`,
`src/CafeManager.DataAccess/Repositories/LoyaltyRepository.cs`,
`SalesRepository.Checkout.cs`, `CheckoutDialog.cs`, `InvoiceDetailDialog.cs`,
`Models/Dtos/Sales/CheckoutDtos.cs`, `InvoiceDtos.cs`.

- Xác nhận UI chỉ hiển thị DTO (số tiền giảm, % hiển thị, danh sách KM/quà), không tự
  cộng % hoặc tự tính hạng.
- Nếu nhãn hiển thị "Giảm giá" gây hiểu nhầm cộng dồn, chỉ sửa text/tooltip tối thiểu,
  không đổi layout; hình thức vẫn do người dùng nghiệm thu.
- Không thêm màn chọn KM tay, không đổi request/response của preview ngoài giá trị.

**Verify:** build Debug/Release sạch; chạy app mở preview/checkout với dữ liệu test do
người dùng thao tác (agent không nghiệm thu hình thức); không có thay đổi SQL trong UI.

## Bước 4 — Bộ kiểm thử mới và bộ cũ

**Mới:** `database/tests/promo-one-per-item/`:

| File | Nội dung |
|---|---|
| `fixture.sql` | DB test `CoffeeManager_Test_PromoOnePerItem_<runId>` + marker; món A/B/C giá lẻ; KH hạng 5%; KM1(A 10%), KM2(B 30%), KM3(B+C 40%), KM ALL 5%, KM tặng, KM tạm ngưng; đơn legacy mở; baseline hóa đơn seed/tồn kho. |
| `01-selection.sql` | PP01 ví dụ a/b/c chọn tổ hợp lớn hơn, không bao giờ KM2+KM3; PP02 ALL vs KM món; PP03 nhiều dòng cùng món; PP05 hòa tổng/tie-break, chạy lặp cùng kết quả. |
| `02-tier-after.sql` | PP04 hạng sau KM (100.000 − 20.000, hạng 5% = 4.000), chặn 100% tự nhiên, `BaseAmount`/chia KM khớp `PromotionDiscount`, ship không giảm, điểm đúng. |
| `03-checkout.sql` | PP06 quà độc lập; PP07 token đổi khi tổ hợp/tiền đổi, token cũ 52201 rollback sạch, snapshot + `HoaDonUuDaiApDung` khớp, đọc lại POS/chi tiết. |
| `04-regression.sql` | Hóa đơn seed/đã chốt không đổi; legacy mở tính theo luật mới; báo cáo `SalesDay/Products/Payments/Inventory` chạy; tồn kho khớp tiêu hao. |

**Runner:** `tools/database/Invoke-PromoOnePerItem.ps1` (mẫu `Invoke-PromoV2.ps1`):
dựng DB từ schema + seed, áp 001–044, chạy fixture + 4 script, lưu manifest/evidence ở
`docs/tasks/promotion-mot-mon-mot-km/runs/<runId>/`, băm source trước/sau, drop DB.

**Bộ cũ:** giữ `promo-auto` (schema 039) và `promo-v2` (schema 042) làm bộ lịch sử đúng
phiên bản, không đổi kỳ vọng. Lý do: chúng ghim version nên vẫn PASS và bảo toàn lịch sử;
bộ mới trên schema 044 phủ lại các hành vi còn hiệu lực (042: ngày, trạng thái, quà,
snapshot) + luật mới. Nếu bạn muốn chạy bộ cũ trên 044 thì phải viết lại toàn bộ kỳ vọng
số học — mình tách thành việc riêng khi có yêu cầu.

**Verify:** chạy runner mới trên DB test riêng, đủ PP01–PP07 PASS; lưu `test-results.md`
cạnh task với mã ca, môi trường/schema 44, lệnh, expected/actual, bằng chứng; PP08 ghi số
đo thời gian fixture/01/02/03/04 và kết luận. Không chạy test khi chưa có duyệt đợt test.

## Bước 5 — Tài liệu

- `docs/database/loyalty-contract.md`: mục mới "044 — một món một KM, hạng sau KM":
  quy tắc phủ dòng, chọn tổ hợp, công thức tuần tự, ý nghĩa `fn_UuDaiDong`,
  `PercentDiscount`, legacy, giới hạn tìm kiếm.
- `docs/database/migrations.md`: thêm 044 (mục đích, phụ thuộc 043, không backfill).
- Ghi rõ thay đổi thay thế quyết định cộng dồn additive 038 và luật cộng dồn 039/042.
- Cập nhật câu nghiệp vụ "nhiều KM cộng dồn" trong kế hoạch/UX nếu có, đúng phạm vi.

**Verify:** đọc chéo plan/requirements/contract không mâu thuẫn; không sửa lịch sử test.

## Bước 6 — Build và áp dụng

1. Build Debug/Release `dotnet build` (không cảnh báo mới).
2. Chạy runner mới trên DB test; xác nhận schema 44, pass, DB drop sạch.
3. Xin duyệt riêng trước khi áp 044 lên `CoffeeManager_Development`:
   backup theo `tools/database/Backup-Database.ps1`, áp bằng `Apply-Migration.ps1
   -Version 44`, kiểm dữ liệu hóa đơn/ưu đãi cũ không đổi, ghi bằng chứng vào task.
4. DPI/UI: agent chỉ kiểm chức năng ở 125% nếu cần; hình thức và 100%/150% do người dùng
   tự nghiệm thu (không ghi PASS thay).

## Rủi ro và cách xử lý

- **Tìm tổ hợp chính xác có thể bùng nổ tổ hợp** nếu có nhiều KM chồng chéo: tách nhóm,
  ngưỡng 15 KM/nhóm, fallback tham lam tất định + tài liệu; PP08 đo thời gian.
- **Test cũ mã hóa luật cộng dồn**: giữ nguyên theo version; bộ mới phủ lại; nêu rõ để
  tránh hiểu nhầm "test cũ fail".
- **Snapshot cũ và ràng buộc mới**: không backfill, constraint chỉ kiểm tổng hợp lệ và
  `Discount` khớp breakdown; đọc/in hóa đơn cũ giữ nguyên.
- **Lệch 1 đồng do làm tròn** giữa tổng và từng KM: dùng floor cho từng KM, dồn phần lẻ
  cho KM cuối theo mã như 039; có ca kiểm.
