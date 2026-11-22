# Biên bản test — Ưu đãi tự áp (migration039)

Trạng thái: **PA01–PA13 PASS** (lượt 22/11/2026, DB test riêng, tự dọn).
PA14 chưa chạy (cần thao tác GUI), PA15 do người dùng tự kiểm theo phân công.

## Môi trường và bằng chứng

- SQL Server thật `localhost`; runner `tools/database/Invoke-PromoAuto.ps1`.
- DB mới `CoffeeManager_Test_PromoAuto_<runId>` từ `DB_Schema.sql` + `Insert_Data.sql`,
  áp migration 001–039 bằng `Apply-Migration.ps1` (journal đủ 39 version), drop sau lượt.
- Bộ SQL: `database/tests/promo-auto/` (fixture + 4 script + race 2 session).
- Lượt PASS cuối: `runs/20261122_135707_532a959f/manifest.json` — Status `Succeeded`,
  `SourceUnchanged=true`, `Cleaned=true`; 6/6 bước PASS (fixture 865 ms,
  01 552 ms, 02 688 ms, 03 2.589 ms, 04 580 ms, 05-pause-race 1.280 ms).
- Các lượt fail trước đó giữ trong `runs/` làm lịch sử điều tra (xem `bug-report.md`).

## Ca kiểm thử

| Mã | Ca | Script | Kỳ vọng | Trạng thái |
|---|---|---|---|---|
| PA01 | Fixture 039: object mới, bỏ `fn_DiscountPreview`, 13 KM, đơn legacy mở | fixture.sql | Dựng đủ, giảm legacy 6.250 | PASS |
| PA02 | Nhiều ưu đãi cùng hóa đơn (FIRST + TIER + ALL) và tách hạng/KM | 01-preview-multi.sql | Hạng 4.000, KM 17.000, giảm 21.000 | PASS |
| PA03 | Khách vãng lai/DONG không nhận FIRST/TIER; AP chỉ gồm KM đủ điều kiện | 01-preview-multi.sql | Vãng lai 4.000; KH901 14.000; đúng danh sách | PASS |
| PA04 | TVP rỗng dùng `@Money`; ship không giảm; danh sách ADMIN/CASH | 01-preview-multi.sql | 8.000; ADMIN 13 KM; CASH 8 KM đang chạy | PASS |
| PA05 | FIRST loại khách đã có hóa đơn đã trả; MEMBER/TIER đúng hồ sơ/hạng | 02-conditions.sql | KH001 không FIRST; MEMBER 5.500; TIER 3.000 | PASS |
| PA06 | Phạm vi món; biên ngày; NgàyKT NULL; Nháp/Tạm dừng/Hết hạn/Tương lai | 02-conditions.sql | Ngày cuối còn áp, sau ngày KT hết; chỉ KM hợp lệ | PASS |
| PA07 | Làm tròn theo dòng và chặn 100%/dòng | 02-conditions.sql | 105 → 13 (không 12); 32.000 → giảm 32.000, KM 30.400 | PASS |
| PA08 | Legacy `HOADON.MaKM` áp một lần, không nhân đôi | 02-conditions.sql | Giảm 6.250; KM 5.000 + ALL 1.250 | PASS |
| PA09 | Pause làm đổi token; checkout token cũ bị 52201 và rollback sạch | 03-checkout-snapshot.sql | 52201; không payment/snapshot; đơn còn mở | PASS |
| PA10 | Resume + checkout: snapshot TierAndPromotion, AP, điểm, tiêu hao | 03-checkout-snapshot.sql | Giảm 15.000; thu 35.000; điểm 35; NVL01 −0.036, NVL03 −0.02 | PASS |
| PA11 | Trigger cấm sửa/xóa AP và snapshot; POS/chi tiết đọc lại | 03-checkout-snapshot.sql | 53307/52207; `sp_SalesOrderGet`/`sp_InvoiceDetail` đọc không lỗi | PASS |
| PA12 | Hóa đơn seed/báo cáo không đổi số; tồn kho khớp tiêu hao | 04-report-unchanged.sql | Seed giữ nguyên; SalesDay/Products/Inventory chạy; không tồn âm | PASS |
| PA13 | Hai session Pause cùng ExpectedRevision | runner | Đúng 1 OK, 1 nhận 51603 | PASS |
| PA14 | UI chức năng: F3 chỉ khách/giao hàng, POS/checkout/in liệt kê KM | thủ công | Không còn chọn KM tay; danh sách đúng; DPI 125% | NOT RUN — cần thao tác GUI trên app; DB dev đã có 039 để người dùng kiểm |
| PA15 | Hình thức UI (màu, khoảng cách, DPI 100/150%) | người dùng | Theo phân công, agent không nghiệm thu | SKIPPED (phân công người dùng) |

## Áp DB development (sau PASS)

- Backup `COPY_ONLY,CHECKSUM,INIT` + `RESTORE VERIFYONLY`:
  `C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\Backup\CoffeeManager_Development_before039_20261122_135842.bak`.
- App đóng khi áp; `Apply-Migration.ps1 -Database CoffeeManager_Development -Version 39`
  trả `Applied`, checksum `D2D1E735…86F0D1`.
- Sau áp: `SchemaMigrations` 1–40 (039 bổ sung sau 040, runner không bắt thứ tự);
  `KHUYENMAI` có TrangThai/DoiTuongKhach/PhamVi; 3 bảng mới; 6 hàm `fn_*`;
  `fn_DiscountPreview` đã bỏ; grant EXECUTE đủ cho CafeManagerApp.
- Không đổi dữ liệu: 3.442 hóa đơn đã trả giữ tổng 336.840.000/19.617.820/331.182.180,
  tồn NVL01 68,7280, `HoaDonUuDaiApDung` 0 dòng.
- Smoke engine trên dev: `sp_InvoiceDetail` trả 5 set đúng thứ tự (hóa đơn trả cũ),
  `sp_PromotionList` 17 KM, `sp_PromotionGet` 3 set, `sp_DiscountPreview` 1 header + 2 ưu đãi.

## Regression B06 — đọc chi tiết đơn chưa có snapshot (22/11/2026)

- Build Release sau fix: `dotnet build ... -c Release` → 0 lỗi (1 cảnh báo WebView2 có sẵn).
- Harness đọc thật (ngoài repo, chỉ đọc DB dev, dùng `InvoiceRepository.DetailAsync`
  với tài khoản 1): gọi `sp_InvoiceDetail` cho 3 đơn đại diện.
  - `HD26093456` (`Cho thanh toan`, đơn trong ảnh lỗi): OK, `PromotionCount=0`,
    2 KM đang áp từ `fn_HoaDonUuDaiApDung`; trước fix cột 27 NULL gây
    `SqlNullValueException`.
  - `HD26093455` (`Chua thanh toan`): OK, count 0, 2 KM.
  - `HD26093449` (`Da thanh toan`): OK, count 0, không KM; đọc từ snapshot.
- GUI (In tạm/In lại/nối món/thanh toán/in hóa đơn): **BLOCKED** — app đang mở
  (PID 11128) khóa DLL Debug và agent không thao tác GUI WinForms; cần người dùng
  đóng app, chạy bản mới và kiểm theo phân công. Không ghi PASS hình thức.

## Ghi chú rủi ro đã biết

- `database/demo/seed-demo-daily.sql:180` và `seed-demo-operational.sql:349` còn gọi
  `fn_DiscountPreview` đã bị 039 bỏ; chỉ ảnh hưởng seed demo trên schema 039, cần task
  seed riêng.
- Migration 040/041 ngoài phạm vi; dev đã có 040 từ trước, 039 áp sau nhưng độc lập.
- PA14 chưa chạy: agent không thao tác GUI WinForms; cần người dùng mở app kiểm F3,
  POS/checkout/in; phần hình thức PA15 do người dùng.
