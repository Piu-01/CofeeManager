# Biên bản kiểm thử — một món một khuyến mãi, hạng sau KM (migration 044)

Đợt test được người dùng duyệt ngày 23/11/2026. Chỉ chạy trên DB test tạo mới và drop
sau khi đối chiếu marker; không chạm `CoffeeManager_Development`, không sửa source
trong lúc chạy (runner băm source trước/sau, `SourceUnchanged=true`).

## 1. Môi trường

| Mục | Giá trị |
|---|---|
| Máy / SQL Server | `localhost`, Windows 10.0.26200, operator `LAPTOP-79UNRIRC\Lenovo` |
| Schema | 44 (schema gốc + seed + migration 001–044 qua `Apply-Migration.ps1`) |
| SDK | .NET SDK 10.0.401 |
| Runner | `tools/database/Invoke-PromoOnePerItem.ps1` |
| DB mỗi lượt | `CoffeeManager_Test_PromoOnePerItem_<runId>` (tạo mới, có marker, drop sau lượt) |
| Bằng chứng | `docs/tasks/promotion-mot-mon-mot-km/runs/<runId>/` (manifest, migrations.jsonl, source-before/after.csv) |

## 2. Lượt chạy

| RunId | Kết quả | Ghi chú |
|---|---|---|
| `20261123_220705_2096dc6b` | FAIL | Setup: guard 044 kiểm `OBJECT_ID('IF')` trong khi `fn_UuDaiCap/fn_UuDaiTong` là TVF `'TF'`; DB drop sạch, source không đổi |
| `20261123_220739_245054d6` | FAIL | 02: `INSERT @ap EXEC` sai số cột (đã sửa khai báo bảng test) |
| `20261123_221347_ff42a380` | FAIL | 03: kỳ vọng PP06a đảo logic `NOT EXISTS` trong bản viết lại harness |
| `20261123_221738_da26a35c` | FAIL | 03: `sp_TaoHoaDonTuNhap` từ 039 không còn `@Promotion` |
| `20261123_221825_48255b08` | FAIL | 05: kỳ vọng `Payable` ghi nhầm 300000 (tổng) thay vì 252000 (phải trả) |
| **`20261123_221933_99152c8e`** | **PASS** | Đủ 6 bước, `Cleaned=true`, `SourceUnchanged=true` |

Lượt PASS (manifest `20261123_221933_99152c8e`):

| Bước | Trạng thái | ms |
|---|---|---|
| fixture | PASS | 919 |
| 01-selection | PASS | 430 |
| 02-tier-after | PASS | 903 |
| 03-checkout | PASS | 2860 |
| 04-regression | PASS | 158 |
| 05-performance | PASS | 452 |

Số đo PP08 ghi trong bảng `PromoOnePerItemTiming` và manifest:

| Nhãn | ms |
|---|---|
| Preview17Candidates (preview 3 dòng, nhóm 17 ứng viên) | 68,478 |
| Function20Lines17Candidates (`fn_UuDaiTong` 20 dòng, 17 ứng viên) | 28,633 |

## 3. Ca nghiệm thu

| Ca | Nội dung (rút gọn) | Kỳ vọng chính | Thực tế | Kết quả |
|---|---|---|---|---|
| PP01 | Ví dụ a/b/c: KM1(A), KM2(B), KM3(B+C) | A+B+C giảm 50 000 = KM1 10 000 + KM3 40 000; không bao giờ KM2+KM3. Bộ A2/B2/C2 tương ứng KM11 10 000 + KM12 30 000, loại KM13 | Đúng phân bổ và số tiền | PASS |
| PP02 | KM `ALL` tranh chấp bình thường với KM món | D 20 000: ALL 1% (200) thua D 1% thắng tổng… D chọn ALL 5% = 1 000; E chọn 2 500; G 100% giảm 20 000; `ALL` không đè | Đúng từng ca | PASS |
| PP03 | Hai dòng cùng `MaSP` | KM1 áp một lần, `BaseAmount` 150 000, giảm 15 000; `fn_UuDaiDong` 10%/10 000/5 000 | Đúng | PASS |
| PP04 | Hạng tính sau KM | 100k − KM 20k, hạng 5% = 4 000, tổng 24 000 (không 25 000); A: 14 500; A+B+C: 50 000 + 7 500 = 57 500, % thực tế 29; ĐỒNG 0%; KM 100% chặn hạng 0; danh sách KM khớp | Đúng toàn bộ, kể cả preview không dòng món (`@Money`) | PASS |
| PP05 | Hòa tổng | KM93000008 thắng KM93000009 cùng 10%; lặp lại cho cùng kết quả | Đúng, tất định | PASS |
| PP06 | Token/preview/thanh toán khi tổ hợp đổi | Pause KM3: token cũ 52201, rollback sạch (trạng thái, thanh toán, tồn kho); preview lại 36 600 (KM1 10 000 + KM2 18 000 + hạng 8 600), 163 điểm; snapshot `PercentDiscount=18`, `PromotionPercent=14`, `Discount=Tier+Promo`; Resume KM3 không đổi hóa đơn đã trả | Đúng toàn bộ | PASS |
| PP07 | Quà tặng và snapshot legacy | SP944×1 giá 0 `MaKMQuaTang=KM93000005`, tiêu hao NVL93 3 + NVL94 1, không nằm trong danh sách KM giảm; đơn mở có `HOADON.MaKM=KM93000006` (tạm dừng, ALL 50%) dùng như ứng viên ALL thắng ALL 5%, chốt 50 000/50 điểm, snapshot legacy khớp | Đúng toàn bộ | PASS |
| PP08 | Hiệu năng nhóm >15 ứng viên | 16 KM món 1–16% + ALL 5% trên 3 dòng: nhánh tham lam chọn đúng KM 16% = 48 000, phải trả 252 000; đo thời gian preview và hàm 20 dòng | Đúng số học; 68 ms preview, 29 ms hàm | PASS |

Ca 04-regression (PASS) đối chiếu thêm: hóa đơn seed/đã chốt không đổi so với baseline;
doanh thu hôm nay đúng 2 đơn 300 000/86 600/213 400; snapshot `Discount=Tier+Promo`,
`PromotionDiscount` bằng tổng danh sách áp; tồn NVL93 −4 / NVL94 −2 khớp tiêu hao; điểm
KH932 163, KH931 50.

## 4. Sai lệch so với plan và cách xử lý (đã ghi nhận)

1. **Guard 044**: `OBJECT_ID(...,'IF')` cho `fn_UuDaiCap/fn_UuDaiTong` đổi thành `'TF'`
   (đây là lỗi của chính migration vừa viết, lộ ra ngay bước setup của lượt đầu, chưa có
   kết quả nghiệp vụ nào bị ảnh hưởng).
2. **Harness không capture proc preview bằng `INSERT..EXEC`**: `sp_DiscountPreview` và
   `sp_SalesCheckoutPreview` mở applock transaction + `ROLLBACK` trong CATCH, nên
   `INSERT @t EXEC` trả Msg 3915 (che lỗi thật) dù `EXEC` trực tiếp chạy đúng và trả
   3 result set 14/5/5 cột. Ứng dụng C# đọc bằng `SqlDataReader`, không dùng
   `INSERT..EXEC`, nên đây là giới hạn của harness chứ không phải lỗi sản phẩm. Test
   đọc `fn_CheckoutPreview/fn_UuDaiTong/fn_UuDaiChia` và gọi proc không capture làm smoke.
3. **PP07b đơn legacy**: từ 039 `sp_TaoHoaDonTuNhap` bỏ `@Promotion`, nên mô phỏng bản
   nháp legacy bằng `UPDATE HOADON.MaKM` trên đơn đang mở (trigger chỉ khóa đơn đã
   trả/hủy), có assert `@@ROWCOUNT=1`.
4. **Kỳ vọng PP08**: `Payable` sửa 300000 → 252000 (300 000 là tổng, phải trả sau giảm
   48 000 là 252 000). Đây là lỗi kỳ vọng của test, không phải lỗi engine.
5. Hai lần đầu tiên sửa khai báo bảng `@ap` (5 → 4 cột) và đảo `NOT EXISTS` → `EXISTS`
   là lỗi harness trong lúc viết test, đã ghi ở mục 2.

## 5. Áp migration 044 lên `CoffeeManager_Development` (duyệt 24/11/2026)

Script `tools/database/Update-DevelopmentPromoOnePerItem.ps1 -Apply`, bằng chứng tại
`runs/dev044_20261124_092502_e00e9748/manifest.json`:

| Mục | Kết quả |
|---|---|
| Phiên bản | 43 → 44 (`SchemaMigrations`) |
| Backup | `...\MSSQL\Backup\CoffeeManager_Development_before044_20261124_092502_e00e9748.bak` (COPY_ONLY + CHECKSUM + RESTORE VERIFYONLY, snapshot nguồn ổn định); manifest `%LOCALAPPDATA%\CoffeeManager\backups\before044_20261124_092502_e00e9748.json` |
| Checksum 001–043 | Khớp (AlreadyApplied) trước khi backup |
| Hash dữ liệu nghiệp vụ | Trước = sau `2A3C0880…416E5A` (HOADON, CHITIET_HD, thanh toán, snapshot/áp dụng ưu đãi, KHUYENMAI + món/quà, điểm, tồn kho, tiêu hao, lịch sử điểm) |
| Object | `fn_UuDaiChon` (TF), `CK_DiscountSnapshot_Known` tồn tại; GRANT EXECUTE `CafeManagerApp` đủ 4 proc |
| Hóa đơn đã trả | 5 hóa đơn mới nhất: `GiamGia` khớp `fn_HoaDonGiamGia` (0 lệch) |

Hai lượt đầu của script lỗi trước khi chạm schema (một do PowerShell 5.1 đọc sai script
không BOM ở bản nháp, một do sai tên cột `LICHSU_DIEM` trong câu hash); manifest giữ lại
ở `runs/dev044_20261124_092421_*` và `runs/dev044_20261124_092429_*` với `Status=Failed`,
chưa có backup/áp dụng nào xảy ra ở hai lượt đó.

## 6. Build

- `dotnet build -c Release`: **0 lỗi**.
- `dotnet build -c Debug`: thư viện (Models/DataAccess/Business) biên dịch sạch; bước copy
  vào `src\CafeManager.WinForms\bin\Debug\net10.0-windows` lỗi MSB3021/MSB3027 vì tệp DLL
  đang bị khóa bởi `Microsoft Visual Studio (23000)` và `CafeManager.WinForms (19536)` —
  app do người dùng mở lúc 09:25 để kiểm giao diện. Không phải lỗi biên dịch; cần đóng
  app/VS rồi build Debug lại.

## 7. Chưa chạy / BLOCKED

- **Hình thức UI, DPI 100%/150%**: theo phân công, người dùng tự nghiệm thu; agent không
  ghi PASS hình thức.
- **Bộ cũ `promo-auto`/`promo-v2`**: giữ nguyên theo version như plan; chưa chạy lại trên
  schema 044 (nếu cần phải viết lại kỳ vọng số học, tách việc riêng).
- **Build Debug**: chờ đóng app/Visual Studio (lý do ở mục 6).

## 8. Kết luận

PP01–PP08 PASS trên DB test riêng, schema 44, lượt `20261123_221933_99152c8e`; source
không đổi trong lượt, DB đã drop. Migration 044 đã áp lên `CoffeeManager_Development`
sau duyệt, có backup kiểm chứng và hash dữ liệu không đổi. Còn lại: build Debug sau khi
đóng app và người dùng nghiệm thu giao diện.
