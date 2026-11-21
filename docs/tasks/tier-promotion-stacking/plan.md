# Plan — Cộng dồn ưu đãi hạng + 1 khuyến mãi mỗi hóa đơn

- Ngày: 21/11/2026
- Trạng thái: plan đã duyệt và triển khai 21/11/2026; đợt test 21/11 phát hiện
  1 bug tại `sp_DiscountPreview` — xem [test-results.md](test-results.md) và
  [bug-report.md](bug-report.md), đang chờ duyệt đợt bug.
- Requirements đã duyệt: [requirements.md](requirements.md).
- Thực hiện theo đúng thứ tự SQL → Models/DataAccess → Business → UI → tài liệu.
  Test chính thức là đợt riêng theo `post-change-test-workflow`; plan này dừng ở
  "triển khai xong, build sạch", không tự chạy test đợt.

## 0. Trạng thái code hiện tại (đã rà)

- Tỷ lệ giảm đang tính ở `dbo.fn_DiscountPreview` (008): KM thay hạng, không cộng dồn.
- `dbo.fn_CheckoutPreview` (011) bọc `fn_DiscountPreview`, trả `Source/Tier/Promotion/
  PercentDiscount/Discount/Payable/Partner*/Points/Revision/PreviewToken`.
- `sp_SalesCheckoutPreview`, `sp_ThanhToanHoaDon` bản mới nhất ở 032; snapshot ghi
  `HoaDonUuDaiSnapshot` (011, trigger siết ở 032).
- `sp_SalesReadCore` (034) và `sp_InvoiceDetail` (035) đọc snapshot, không cần đổi
  chữ ký; chỉ `sp_InvoiceDetail` cần trả thêm cột breakdown.
- C#: `DiscountPreview`, `CheckoutPreview`, `InvoiceDetailDto`, `SalesStates.Label`,
  `LoyaltyRepository.DiscountAsync`, `SalesRepository.PreviewAsync`,
  `InvoiceRepository.ReadDetail`, 4 chỗ hiển thị UI/PDF.
- `sp_SalesRefreshDiscount` (010) và `sp_SalesReadCore` chỉ dùng Source/%/Discount/
  Payable nên **không** phải sửa khi hàm trả thêm cột.

## 1. Bước 1 — Migration SQL 038 (một file, một transaction)

File mới: `database/migrations/038_tier_promotion_stacking.sql`

Phía trên cùng giữ chuẩn cũ: `SET NOCOUNT ON; SET XACT_ABORT ON;` chặn nếu
`@@TRANCOUNT<>0` hoặc thiếu `SchemaMigrations Version=37`; kiểm object đã tồn tại
trước khi ALTER; toàn bộ DDL nằm trong `BEGIN TRAN … COMMIT` có `TRY/CATCH` rollback.

Thứ tự object trong file:

1. `ALTER FUNCTION dbo.fn_DiscountPreview`
   - Giữ chữ ký 5 tham số.
   - `Source`: `None` | `Tier` | `Promotion` | `TierAndPromotion`
     (`TierAndPromotion` khi có cả khách và KM hợp lệ).
   - Trả thêm `TierPercent`, `PromotionPercent`; `PercentDiscount` =
     `CASE WHEN tổng > 100 THEN 100 ELSE tổng END`.
   - `Discount = ROUND(Money × PercentDiscount / 100, 0)`, `Payable = Money + Ship − Discount`
     (giữ nguyên cách làm tròn một lần); ship vẫn không bị giảm.
   - KM ngoài hiệu lực tại ngày lập coi như không có KM (PromotionPercent = 0, Source
     theo khách) — cùng hành vi "KM thay hạng" cũ đang dựa vào JOIN theo ngày.
2. `ALTER FUNCTION dbo.fn_CheckoutPreview`
   - Thêm `p.TierPercent`, `p.PromotionPercent` vào output.
   - Thêm hai tỷ lệ này vào `PreviewToken` (hash) để preview cũ mất hiệu lực khi
     chính sách/KM thay đổi cách tính; vẫn giữ đủ thành phần cũ (Revision đơn,
     policy, khách, KM, đối tác, Discount, Payable, PercentDiscount).
   - Không đổi `Points = FLOOR((TongTien − Discount)/1000)`.
3. `HoaDonUuDaiSnapshot` (011:5–14)
   - Thêm `TierPercent int NULL`, `PromotionPercent int NULL`.
   - Drop `CK_DiscountSnapshot_Known`, drop CHECK inline của `Source` (tìm theo
     `sys.check_constraints` của bảng + `definition LIKE '%Source%'`), rồi tạo lại
     bằng tên tường minh:
     - `CK_DiscountSnapshot_Source`: `None/Tier/Promotion/TierAndPromotion` hoặc NULL.
     - `CK_DiscountSnapshot_Known`: `Checkout` bắt buộc `Source`, `PercentDiscount`,
       `TierPercent`, `PromotionPercent`, `PolicyRevision` NOT NULL và
       `PercentDiscount = CASE WHEN TierPercent+PromotionPercent>100 THEN 100
       ELSE TierPercent+PromotionPercent END`; `LegacyUnknown` vẫn NULL như cũ.
   - Không backfill dòng cũ; trigger `TR_DiscountSnapshot` (bản 032) không phải sửa.
4. `ALTER PROCEDURE dbo.sp_SalesCheckoutPreview`
   - SELECT thêm `p.TierPercent` và `p.PromotionPercent` ngay sau `p.PercentDiscount`;
     giữ nguyên các cột còn lại và thứ tự còn lại.
5. `ALTER PROCEDURE dbo.sp_ThanhToanHoaDon` (copy nguyên bản 032 rồi sửa tối thiểu)
   - Khai báo thêm `@tierPercent int, @promotionPercent int`; SELECT từ
     `fn_CheckoutPreview` lấy thêm hai cột.
   - `INSERT HoaDonUuDaiSnapshot` thêm `TierPercent, PromotionPercent`.
   - Không đổi phân bổ, kho, điểm, token check, receipt, khóa.
6. `ALTER PROCEDURE dbo.sp_InvoiceDetail` (copy bản 035, chỉ thêm 2 cột ở result set
   thứ 4): `s.Source, s.PercentDiscount, s.TierPercent, s.PromotionPercent, Points,
   RecordedAt`.
7. Cuối file: `GRANT EXECUTE` lại cho `CafeManagerApp` trên các procedure đã ALTER
   (ALTER thường giữ grant, nhưng ghi lại đúng chuẩn 031/032 để tránh hụt quyền).

Kiểm chứng bước 1 (chưa coi là nghiệm thu):
- Áp 001→038 trên DB test rỗng mới bằng `tools/database/Apply-Migration.ps1`
  (`-Version 38 -File .\database\migrations\038_tier_promotion_stacking.sql`).
- Gọi SQL trực tiếp: khách KH001 (hạng BAC 3%) + một KM 10% trên tiền món
  250.000 → TierPercent 3, PromotionPercent 10, PercentDiscount 13,
  Discount 32.500, Payable 217.500; không KM → chỉ hạng; không khách → chỉ KM;
  KM 100% + hạng 8% → 100%/0₫ (ship cộng riêng).
- Thử INSERT snapshot `Checkout` thiếu `TierPercent` → CHECK chặn; dòng
  `LegacyUnknown` cũ vẫn đọc được.
- `sqlcmd`/SSMS kiểm tra `sys.sql_modules` chứa đúng định nghĩa mới.

## 2. Bước 2 — Models + DataAccess

- `src/CafeManager.Models/Dtos/Loyalty/LoyaltyRows.cs`: `DiscountPreview` thêm
  `int TierPercent, int PromotionPercent` ngay sau `PercentDiscount`.
- `src/CafeManager.Models/Dtos/Sales/CheckoutDtos.cs`: `CheckoutPreview` thêm
  `int TierPercent, int PromotionPercent` ngay sau `PercentDiscount`.
- `src/CafeManager.Models/Dtos/Sales/InvoiceDtos.cs`: `InvoiceDetailDto` thêm
  `int? TierPercent, int? PromotionPercent` sau `PercentDiscount`.
- `src/CafeManager.Models/States/SalesStates.cs`: thêm nhãn
  `"TierAndPromotion" => L.T("Hạng + khuyến mãi")`.
- `src/CafeManager.DataAccess/Repositories/LoyaltyRepository.cs` — `DiscountAsync`:
  thêm đọc cột 4,5; `Discount/Payable/ReadAt/PolicyRevision` dời sang 6..9.
- `src/CafeManager.DataAccess/Repositories/SalesRepository.Checkout.cs` —
  `PreviewAsync`: đọc lại đúng thứ tự mới (Source 6, Percent 7, TierPercent 8,
  PromotionPercent 9, PartnerPercent 10, PartnerFee 11, Points 12, Token 13).
- `src/CafeManager.DataAccess/Repositories/InvoiceRepository.cs` — `ReadDetail`:
  kind 0, Source 1, Percent 2, TierPercent 3, PromotionPercent 4, Points 5,
  RecordedAt 6; nhánh không có snapshot trả thêm hai null.
- `SalesOrderHeader`/`ReadOrder` **không đổi** (PreviewPercent tiếp tục là tổng).

Kiểm chứng: `dotnet build CafeManager.slnx` sạch sau bước này và không còn chỗ đọc
sai index (rà lại toàn bộ `CheckoutPreview`/`DiscountPreview`/`InvoiceDetailDto`).

## 3. Bước 3 — Business

- `src/CafeManager.Business/Services/LoyaltyService.cs` và
  `SalesService.Checkout.cs` chỉ truyền DTO; không đổi quy tắc, quyền hay validate.
- Xác nhận không có công thức giảm giá nào ở BLL (đúng kiến trúc hiện tại) nên
  không thêm bản sao tính toán.

Kiểm chứng: build sạch, không xuất hiện `PercentDiscount` mới được tính trong C#.

## 4. Bước 4 — UI hiển thị breakdown

- `Modules/Loyalty/LoyaltyControl.cs` (dialog Preview ưu đãi): khi
  `Source = "TierAndPromotion"` hiển thị `Hạng X% + KM Y% = Z%`; các nguồn khác
  giữ câu hiện tại. Không đổi nút, validate, version-invalidate.
- `Modules/Sales/CheckoutDialog.cs` `UpdateSummary`: dòng nguồn giảm hiển thị
  `Hạng + khuyến mãi · 13% (hạng 3% + KM 10%)`; trường hợp một nguồn giữ như cũ.
- `Modules/Sales/InvoiceDetailDialog.cs`: câu tóm tắt nguồn giảm thêm breakdown khi
  hai tỷ lệ khác 0; `LegacyUnknown` vẫn báo chưa lưu.
- `Printing/FastReport/InvoicePdfReport.cs`: dòng "Nguồn giảm" thêm
  `(hạng X% + KM Y%)` khi có breakdown; hóa đơn cũ (PercentDiscount null hoặc
  TierPercent/PromotionPercent null) in đúng như trước.
- Không đụng layout, màu, font, DPI; chỉ đổi nội dung text.

Kiểm chứng: build sạch; đọc code xác nhận mọi chỗ gọi `SalesStates.Label` xử lý giá
trị mới; kiểm UI thật thuộc đợt test và phần hình thức do người dùng tự kiểm.

## 5. Bước 5 — Tài liệu nghiệp vụ/contract

- `docs/KeHoach_QuanLiQuanCafe.md` mục 6.2 bảng công thức: đổi dòng "Tỷ lệ giảm"
  thành cộng thẳng hạng + 1 KM, chặn 100%, làm tròn một lần; ghi rõ lịch sử đơn cũ
  giữ nguyên.
- `docs/UX_QuanLiQuanCafe.md` mục 7.4 và dòng `UX-K10`: bỏ "thay/không cộng dồn",
  ghi cộng dồn và nguồn `Hạng + khuyến mãi`.
- `docs/database/loyalty-contract.md` dòng mô tả `fn_DiscountPreview`/`sp_DiscountPreview`
  cập nhật Source mới + hai tỷ lệ.
- `docs/database/checkout-contract.md`: token/snapshot có breakdown; đơn cũ không đổi.
- `docs/database/invoice-contract.md`: `sp_InvoiceDetail` trả breakdown, LegacyUnknown
  giữ NULL.
- Không sửa lịch sử test T13/G03, không đổi seed/migration 037.

Kiểm chứng: đọc lại 5 chỗ, không còn câu "KM thay ưu đãi hạng" ngoài phần mô tả
lịch sử.

## 6. Bước 6 — Build và tổng rà (kết thúc pha triển khai)

- `dotnet build CafeManager.slnx` Debug sạch, không warning mới.
- Rà `git diff` chỉ gồm các file trong plan; không có file phát sinh.
- Đối chiếu checklist mục 6 của requirements: các mục 1–5 và 7 chứng minh được bằng
  SQL/đọc code; mục 5–6 (checkout thật, hóa đơn cũ) và đợt test để pha test riêng.

Dừng tại đây, báo cáo và chờ người dùng duyệt đợt test.

## 7. Bước 7 — Áp DB phát triển (sau khi test xong, có duyệt riêng)

- Đóng app, backup `CoffeeManager_Development`, chạy migration 038 bằng runner.
- Kiểm tra nhanh một preview + một hóa đơn cũ; không sửa dữ liệu điểm/hóa đơn.

## 8. Rủi ro và xử lý

| Rủi ro | Xử lý |
|---|---|
| Đổi thứ tự cột procedure làm lệch mapper C# | Sửa mapper cùng bước 2, build sạch trước khi sang UI |
| Preview token đổi làm đơn đang mở báo 52201 | Đúng thiết kế: yêu cầu xác nhận lại số mới; không thu theo số cũ |
| Snapshot cũ đọc lỗi khi thêm cột | Cột mới NULL, CHECK cho phép `LegacyUnknown`; không backfill |
| Tổng % vượt 100 | Chặn ở hàm (min 100) và CHECK snapshot |
| Hụt quyền EXECUTE sau ALTER | GRANT lại cuối migration như 031/032 |

## 9. Ngoài phạm vi

Không đổi ngưỡng/tỷ lệ hạng, không sửa công thức tích điểm, không thêm dùng
điểm/voucher/hoàn tiền, không cho nhiều KM một đơn, không sửa dữ liệu lịch sử,
không đổi quyền hay luồng F3/F8 ngoài phần hiển thị.

## 10. Kết quả triển khai (21/11/2026)

- Đã tạo `database/migrations/038_tier_promotion_stacking.sql`; đã sửa DTO/DataAccess/
  UI/PDF/tài liệu theo bước 1–5.
- Áp thử trên bản copy DB test version 37: `CoffeeManager_Test_038_stacking`
  (backup `cm038_tierstack.bak`), runner ghi version 38.
- Đã kiểm chứng trực tiếp bằng SQL trên bản copy:
  - KH001 (PLAT 6%) + KM 10% trên 250.000 → nguồn `TierAndPromotion`, 16%,
    giảm 40.000, phải trả 210.000; ship 30.000 không bị giảm.
  - Chỉ khách → `Tier` 6%; chỉ KM → `Promotion` 10%; KM hết hiệu lực → `Tier`.
  - KM 100% + hạng 6% → chặn 100%, phải trả 0.
  - Snapshot `Checkout` thiếu breakdown hoặc sai tổng % bị CHECK chặn; dòng hợp lệ
    ghi được; 3.442 dòng snapshot cũ đọc bình thường, không backfill
    (`CK_DiscountSnapshot_Known` để `is_not_trusted=1` đúng chủ đích).
  - `fn_CheckoutPreview` trả đủ cột mới và token 32 byte.
- `dotnet build src/CafeManager.WinForms` sạch. Build toàn solution còn 1 lỗi
  **có sẵn, ngoài phạm vi**: `tests/.../G03/RulesTests.cs` dùng
  `PointAdjustmentRequest` đã bị xoá trong thay đổi chưa commit về bỏ điểm khách
  hàng; chưa sửa test đó.
- Đã áp 038 lên `CoffeeManager_Development` (app không chạy) sau khi backup
  `CoffeeManager_Development_pre038_20261121.bak` (COPY_ONLY, CHECKSUM): runner ghi
  version 38, checksum `F964DB16…`; preview KH001+KM26HRO trả 13% = 3 + 10 /
  32.500 / 217.500; `HD20250401` vẫn giảm 12.500, snapshot cũ 3.442 dòng không đổi.
- Đợt test 21/11/2026: 30/35 PASS, phát hiện hoán vị cột ở `sp_DiscountPreview`;
  đã sửa trong 038 (checksum mới `F964DB16…`) và regression 35/35 PASS trên
  `CoffeeManager_Test_038_accept8` — xem [test-results.md](test-results.md) và
  [bug-report.md](bug-report.md). Hình thức UI và DPI 100/150% do người dùng tự kiểm.
