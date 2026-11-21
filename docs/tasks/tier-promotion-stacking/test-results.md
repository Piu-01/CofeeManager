# Test results — Cộng dồn ưu đãi hạng + 1 khuyến mãi

- Ngày: 21/11/2026
- Môi trường: Windows 11, .NET SDK 10.0.401, SQL Server 2025 `localhost` (integrated security), DPI hệ thống 125%
- Source: working tree chưa commit gồm `database/migrations/038_tier_promotion_stacking.sql` và các file C#/docs của task; không sửa source trong lúc test
- Duyệt đợt test: người dùng đồng ý ngày 21/11/2026
- Kịch bản và log: [`runs/stack-20261121/verify-stacking.ps1`](runs/stack-20261121/verify-stacking.ps1), [`runs/stack-20261121/output.log`](runs/stack-20261121/output.log)
- DB test: `CoffeeManager_Test_038_accept7` = bản copy `COPY_ONLY` của `CoffeeManager_Development` (đang ở version 37, chính sách hạng DONG 0/0, BAC 2.000/3, VANG 10.000/5, KIM 30.000/8), sau đó áp migration 038 bằng `tools/database/Apply-Migration.ps1`
  - Runner ghi `Version=38`, checksum `FB421E48BB224E26F0E2AD71DF65D5FFDCEFF6891D28938AAEBC1C18429F216B`
  - Không ghi test vào `CoffeeManager_Development`; DB phát triển vẫn ở version 37

## Kết quả tổng hợp

- **30 PASS / 5 FAIL** trong 35 ca chạy được; 5 FAIL cùng một nguyên nhân ở `sp_DiscountPreview` (xem [bug-report.md](bug-report.md)).
- Luồng checkout thật, snapshot, token, điểm, kho, hóa đơn cũ, báo cáo đều PASS.
- Hình thức giao diện: người dùng tự kiểm theo phân công (SKIPPED trong biên bản này).
- DPI 100%/150%: NOT RUN (chỉ kiểm kỹ thuật ở 125% theo AGENTS.md).

## Chi tiết

| Mã | Nội dung | Kết quả | Bằng chứng (log) |
|---|---|---|---|
| ST-00 | Schema v38, chính sách hạng đúng 4 hạng | PASS | `schema v38; tiers=DONG\|0\|0;BAC\|2000\|3;VANG\|10000\|5;KIM\|30000\|8` |
| ST-01 | Preview KH001 (BAC 3%) + KM26HRO (10%) trên 250.000 → 13%, 32.500, 217.500 | **FAIL** | Trả về `TierAndPromotion/3%=10+13/32500/217500` — tiền đúng, nhưng 3 cột tỷ lệ bị hoán vị (PercentDiscount=3, TierPercent=10, PromotionPercent=13) |
| ST-02 | Chỉ hạng: 3%, 7.500, 242.500 | **FAIL** | `Tier/3/7500/242500` — tiền đúng nhưng TierPercent/PromotionPercent sai vị trí |
| ST-03 | Chỉ KM: 10%, 25.000, 225.000 | **FAIL** | `Promotion/0/25000/225000` — tiền đúng nhưng cột tỷ lệ sai vị trí |
| ST-04 | Không khách, không KM → 0 | PASS | `None/0/0/250000` |
| ST-05 | Ship 30.000 không bị giảm: 32.500 / 247.500 | **FAIL** | `discount=32500 payable=247500` đúng nhưng cột tỷ lệ hoán vị như ST-01 |
| ST-06a | KM24BF hết hạn bị preview từ chối | PASS | `err=52001` (giữ hành vi cũ) |
| ST-06b | `fn_DiscountPreview` trả đúng breakdown | PASS | `fn breakdown=3+10=13/32500` |
| ST-07 | KMSTK100 100% + KIM 8% → chặn 100%, phải trả 0 | **FAIL** | `100+100=8` (hoán vị cột), tiền 250.000/0 đúng |
| ST-08a | Làm tròn một lần: 33.333 × 13% → 4.333 / 29.000 | PASS | `4333/29000` |
| ST-08b | 33.335 × 13% → 4.334 / 29.001 | PASS | `4334/29001` |
| ST-09 | Preview không đổi dữ liệu khách | PASS | `KH001=8281/BAC` |
| ST-10 | Gửi hai mã KM trong một đơn bị chặn | PASS | `err=52105 rows=0` |
| ST-11a | Checkout preview A: 13%, 32.500, 217.500, 217 điểm, token 32 byte, 14 cột | PASS | `TierAndPromotion/13%/32500/217500/pts217/token32/cols14` |
| ST-11b | Thanh toán A: đã trả, giảm 32.500, KH001 8.498 điểm, 1 dòng điểm, 1 dòng thanh toán | PASS | `paid A: giam=32500 KH001=8498/BAC hist=1 pays=1` |
| ST-11c | Snapshot A đủ breakdown | PASS | `Checkout\|TierAndPromotion\|BAC\|KM26HRO\|13\|3\|10\|32500\|1` |
| ST-11d | Trừ kho, một hóa đơn, transaction sạch | PASS | `stock consumed; single invoice row; tx=0/0` |
| ST-12 | Gửi lặp cùng RequestId không nhân đôi điểm/thanh toán/kho | PASS | `no duplicate points/payment/stock` |
| ST-13a | Đơn giao hàng ship 30.000: giảm 32.500, phải trả 247.500, 217 điểm | PASS | `TierAndPromotion/32500/247500/pts217` |
| ST-13b | Thanh toán B: giữ ship 30.000, điểm không tính ship (8.715) | PASS | `paid B: giam=32500 ship=30000 KH001=8715` |
| ST-14a | Sửa KM 10%→11% sau preview: 13%→14%, token đổi | PASS | `13%->14%; token changed` |
| ST-14b | Token cũ bị từ chối, không rò rỉ dòng | PASS | `err=52201 no leaked rows` |
| ST-14c | Thanh toán lại với token mới | PASS | `Checkout\|TierAndPromotion\|BAC\|KMSTK10\|14\|3\|11\|35000\|1` |
| ST-14d | KM hết hạn sau khi lập đơn → checkout preview báo lỗi | PASS | `err=51105` |
| ST-15a | Sửa chính sách hạng (revision) → token đổi, số tiền giữ nguyên | PASS | `token changed; amounts stay 3%/7500` |
| ST-15b | Token cũ sau đổi chính sách bị từ chối | PASS | `err=52201` |
| ST-15c | Thanh toán với token mới, snapshot chỉ hạng | PASS | `Checkout\|Tier\|BAC\|\|3\|3\|0\|7500\|1` |
| ST-16a | `sp_InvoiceDetail` trả breakdown | PASS | `Checkout/TierAndPromotion/13=3+10/pts217` |
| ST-16b | Hóa đơn cũ `HD20250401` trả `LegacyUnknown`, tỷ lệ NULL | PASS | `LegacyUnknown////` |
| ST-16c | `sp_InvoiceList` vẫn đọc hóa đơn đã trả | PASS | `HD26990002/217500/Da thanh toan` |
| ST-17a | Checkout 100% + KIM 8%: chặn 100%, phải trả chỉ ship 30.000, 0 điểm | PASS | `100%=8+100/250000/30000/pts0` |
| ST-17b | Snapshot ghi 100/8/100 | PASS | `Checkout\|TierAndPromotion\|KIM\|KMSTK100\|100\|8\|100\|250000\|1` |
| ST-17c | Chèn/sửa trực tiếp snapshot bị chặn, dữ liệu không đổi | PASS | `err=547/547; snapshot intact` |
| ST-18 | Hóa đơn + snapshot cũ không đổi (12.500; LegacyUnknown; RecordedAt gốc) | PASS | `giam=12500 kind=LegacyUnknown recorded=2026-10-23 15:53:42.1362922` |
| ST-19 | Quyền EXECUTE trên 4 procedure đã sửa | PASS | `EXECUTE granted on all four altered procedures` |
| ST-20a | `sp_ReportSales` giống nhau giữa DB v37 và v38 (01/01–20/11/2026) | PASS | `identical (v37 vs v38), length=18299` |
| ST-20b | `sp_ReportPayments` giống nhau | PASS | `identical, length=2988` |
| ST-20c | Định nghĩa `sp_ReportRead` không bị 038 thay đổi | PASS | `definition untouched by 038` |

## Build

| Mục | Kết quả | Bằng chứng |
|---|---|---|
| `dotnet build src/CafeManager.WinForms` | PASS | 0 error, 1 warning WebView2/WindowsBase có sẵn |
| `dotnet build CafeManager.slnx` | FAIL (có sẵn, ngoài phạm vi) | `tests/CafeManager.Tests/G03/RulesTests.cs(29): CS0246 PointAdjustmentRequest` do thay đổi chưa commit về bỏ điểm khách hàng của người dùng |

## Phần chưa chạy / để người dùng tự kiểm

- Hình thức UI (màu, khoảng cách, hover, dialog): SKIPPED theo phân công người dùng.
- DPI 100% và 150%: NOT RUN; chỉ kiểm kỹ thuật ở 125%.
- In PDF thật từ app cho hóa đơn cũ/mới: cùng bị chặn bởi lỗi build test có sẵn ở trên nên chưa chạy smoke test in; đường đọc breakdown của `InvoicePdfReport` đã rà code và dùng chung `InvoiceDetailDto` như ST-16.
- Migration 038 trên `CoffeeManager_Development`: chưa chạy (chờ duyệt riêng, sau khi xử lý bug).

## Kết luận

Đợt test đầu phát hiện **1 bug** trong `sp_DiscountPreview` (hoán vị cột tỷ lệ hạng/KM). Các phần còn lại của tính năng — công thức hàm, checkout, snapshot, token, điểm, kho, hóa đơn cũ, báo cáo — đạt yêu cầu. Bug đã được người dùng duyệt sửa ngày 21/11/2026; chi tiết tại [bug-report.md](bug-report.md).

## Regression sau sửa bug (21/11/2026)

- Sửa: đổi thứ tự cột bảng đệm và `INSERT` tường minh trong `sp_DiscountPreview` (chỉ migration 038; không đổi C#).
- DB test mới: `CoffeeManager_Test_038_accept8` = copy `COPY_ONLY` của DB phát triển v37 + áp 038 mới; checksum `F964DB1655C253B317D22F0E113C9A84445B517CF7AA05A5E5E6084355B1B0C8`.
- Kết quả: **35/35 PASS**, `SUMMARY|FAIL=0` — log [`runs/stack-20261121/output-fix.log`](runs/stack-20261121/output-fix.log). ST-01 trả `TierAndPromotion/13%=3+10/32500/217500`; ST-07 trả `8+100=100`; toàn bộ ca checkout/snapshot/legacy/báo cáo giữ nguyên PASS.
- `dotnet build src/CafeManager.WinForms`: 0 lỗi.
- Đã áp 038 lên `CoffeeManager_Development` (app đóng, backup trước:
  `CoffeeManager_Development_pre038_20261121.bak`): version 38, preview
  KH001+KM26HRO trả `13% = 3 + 10 / 32.500 / 217.500`; `HD20250401` vẫn giảm
  12.500 và 3.442 dòng snapshot cũ giữ nguyên.
