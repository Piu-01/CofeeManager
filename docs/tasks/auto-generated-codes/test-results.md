# Test results — Tự sinh mã, bỏ ô nhập mã (auto-generated-codes)

- Ngày chạy: 21/11/2026
- Phạm vi: đợt test đã được người dùng duyệt (`duyêtk`); chỉ chạy test, không sửa source/migration.
- Kết luận nhanh: **25/25 ca chức năng chạy được đều PASS** (23 ca SQL chính + 2 ca gửi lặp),
  1 ca đồng thời PASS, build Debug/Release PASS, unit test 144/144 PASS. Ca UI chức năng
  **NOT RUN** (không có công cụ điều khiển WinForms trong phiên này); ca hình thức **SKIPPED**
  theo phân công người dùng tự kiểm.

## 1. Môi trường

| Mục | Giá trị |
|---|---|
| Máy chủ SQL | `localhost` (MSSQL17), xác thực Windows, `sqlcmd` + ODBC Driver 18 (`-C`) |
| DB test | `CoffeeManager_Test_040_20261121_214133` (bản sao `CoffeeManager_Development` bằng backup COPY_ONLY + RESTORE, không dùng DB thật) |
| Schema | `SchemaMigrations` = **40**; 21 sequence `Seq*` (10 cũ + 11 mới của 040) |
| Source | working tree tại commit `a4623f6` + thay đổi của task chưa commit; **không sửa gì trong lúc test** |
| Build | `dotnet build CafeManager.slnx -c Debug` và `-c Release` (output tạm `artifacts/` vì app đang mở khóa `bin`) |
| Kịch bản | `evidence/tests-040-auto-codes.sql`, `evidence/race-a.sql`, `evidence/race-b.sql`, `evidence/exhaust.sql`, `evidence/reconcile.sql` |
| Bằng chứng | `evidence/run-01.txt` (log 23 ca), các kết quả ghi trong mục 3–7 |

Tài khoản actor dùng chung: `MaTK=1` (ADMIN), SecurityStamp lấy từ `TAIKHOAN`.

## 2. Kết quả theo ca

### 2.1. Cấp mã + tạo bản ghi cho 11 nguồn (ca 1–2 của plan)

| Mã ca | Nội dung | Mã nhận | Kết quả |
|---|---|---|---|
| A01 | Nhà cung cấp: `sp_CapMa 'NHACUNGCAP'` → `sp_CatalogSupplierCreate` | NCC19 | PASS |
| A02 | Phương thức TT: `'PHUONGTHUCTT'` → create `@Supported=0` | PT042 | PASS |
| A03 | Đối tác giao hàng: `'DOITACGIAOHANG'` | DT007 | PASS |
| A04 | Loại thu/chi: `'DANHMUC_THUCHI'`, loại `Chi` | TC037 | PASS |
| A05 | Chức vụ: `'CHUCVU'`, phụ cấp 500000, hệ số 1.5 | CV015 | PASS |
| A06 | Ca làm: `'CALAMVIEC'`, 08:00–12:00 | CA015 | PASS |
| A07 | Sản phẩm: `'SANPHAM'` → `sp_ProductCreate`, công thức NVL01 0.05 | SP427 | PASS |
| A08 | Nguyên liệu: `'NGUYENVATLIEU'` | NVL25 | PASS |
| A09 | Bàn: `'BAN'`, thuộc KV001 | B0014 | PASS |
| A10 | Khu vực: `'KHUVUC'` | KV013 | PASS |
| A11 | Khuyến mãi: `'KHUYENMAI'`, 10% | KM00000003 | PASS |

Mọi mã đúng pattern (NCC/PT/DT/TC/CV/CA 3–2 số theo thiết kế, SP 3 số, NVL 2 số,
B 4 số, KV 3 số, KM + 8 số) và bản ghi tồn tại đúng mã sau khi gọi procedure create
hiện có (không sửa procedure).

### 2.2. Tăng dần, không trùng (ca 1–3)

| Mã ca | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| B01 | Cấp SP liên tiếp tăng | PASS | SP427 → SP428 |
| B02 | Mã NCC mới lớn hơn mọi mã NCC cũ | PASS | NCC20 > max cũ 18 |
| E01 | **Đồng thời 2 session**, mỗi session cấp 200 mã SP | PASS | 400 dòng, 400 mã khác nhau, 0 trùng, 0 trùng mã SP đã có; dải SP027–SP426 |

Lệnh E01: hai tiến trình `sqlcmd` chạy song song `race-a.sql`/`race-b.sql`, ghi vào
`dbo.TmpCodeRace`; kiểm bằng `COUNT(DISTINCT Ma)`, `GROUP BY Ma HAVING COUNT(*)>1`,
và JOIN với `SANPHAM`.

### 2.3. Mã dành riêng, mã cũ (ca 4)

| Mã ca | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| C01 | Cấp 25 lần PT không có PT010 | PASS | 25 lần, không lần nào = PT010 |
| C02 | Cấp 20 lần TC không có TC005/TC011/TC012 | PASS | không lần nào rơi mã hệ thống |
| D01 | Khuyến mãi legacy còn nguyên | PASS | KM24BF vẫn tồn tại, không bị đổi |
| D02 | PT010 còn nguyên | PASS | 1 dòng |
| D03 | TC005/TC011/TC012 còn nguyên | PASS | đủ 3 dòng |

### 2.4. Cấp mã rồi lưu thất bại (ca 5)

| Mã ca | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| F01 | Cấp SP rồi create với giá −1 | PASS | SQL từ chối (51701), **không có dòng rác** |
| F02 | Lần cấp kế tiếp vẫn hợp lệ, khác mã cũ | PASS | mã kế tiếp khác mã đã cấp cho lần lỗi |

### 2.5. Sửa/xóa theo mã đã cấp (ca 1, 4)

| Mã ca | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| G01 | Sửa NCC bằng đúng mã vừa cấp + revision | PASS | tên/SĐT đổi đúng, mã không đổi |
| G02 | Xóa NCC bằng đúng mã + revision mới | PASS | dòng biến mất, không ảnh hưởng dòng khác |

### 2.6. Hết dải mã (ca 6)

| Mã ca | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| H01 | `ALTER SEQUENCE SeqNhaCungCap RESTART WITH 99` (chỉ DB test); cấp lần 1 = NCC99, lần 2 vượt max | PASS | lần 2 báo lỗi **51302** "Code range exhausted; contact administrator." |
| H02 | Không phát sinh dòng NCC sau lỗi | PASS | số dòng NCC không đổi (14 tại thời điểm chạy) |
| — | Khôi phục `SeqNhaCungCap RESTART WITH 19` sau ca | — | test DB ở trạng thái dùng tiếp được |

### 2.7. Khôi phục sau gửi lặp/unknown (ca 7)

| Mã ca | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| R01 | Gửi lặp `sp_ProductCreate` cùng RequestId + cùng payload | PASS | trả cùng mã SP431, chỉ 1 dòng |
| R02 | Dùng lại RequestId với nội dung khác | PASS | bị chặn, lỗi 51608, không tạo dòng thứ 2 |
| — | Danh mục: đối chiếu lại theo mã đã cấp (dialog cấp mã trước `SaveAsync`) | PASS (mức SQL) | `sp_Catalog*Get/List` trả đúng dòng theo mã (đã dùng ở G01/G02) |

Phần đối chiếu trên giao diện (trạng thái unknown/timeout thật) chưa chạy — xem U01.

### 2.8. Nguồn cấp mã cũ không bị ảnh hưởng

| Mã ca | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| I01 | `sp_CapMa 'HOADON'` | PASS | HD26093456 (8 số), các nhánh cũ giữ nguyên |

## 3. Build và unit test

| Mã ca | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| T01 | `dotnet build CafeManager.slnx -c Debug` | PASS | 0 lỗi, 3 cảnh báo (WebView2 reference — có sẵn từ trước) |
| T02 | `dotnet build ... -c Release` | PASS | 0 lỗi, 3 cảnh báo như trên |
| T03 | `dotnet test CafeManager.Tests` (lọc bỏ bộ cần harness DB riêng T05/G02) | PASS | **144/144** |
| T04 | `dotnet test` toàn bộ (tham khảo) | NOT RUN hợp lệ | 461 ca fail do thiếu biến môi trường/harness `Test-DataAccess.ps1`/`Test-G02.ps1`; các bộ này cần DB test riêng có marker, ngoài phạm vi đợt này |

## 4. Phần chưa chạy

| Mã ca | Nội dung | Trạng thái | Lý do |
|---|---|---|---|
| U01 | UI chức năng: mở Thêm ở 6 danh mục + Sản phẩm/Nguyên liệu/Bàn/Khu vực/Khuyến mãi, Tab/focus, lưu, lỗi hết dải, sửa/xóa | **NOT RUN** | Phiên này không có công cụ điều khiển WinForms; cần người dùng mở app chạy thử |
| V01 | Hình thức/bố cục (không khoảng trống ô mã, thứ tự hàng) | **SKIPPED** | Theo AGENTS.md, người dùng tự kiểm hình thức |
| DPI 100%/150% | — | NOT RUN | Theo quy định chỉ chạy kỹ thuật ở 125% khi có điều kiện; phiên này không chạy GUI |

Ghi chú cho U01: source đã được rà bằng grep — không còn `codeInput` trong
`CatalogEditorDialog`, không còn ô nhập mã ở Products/Materials/Tables/Area/Promotion;
tiêu đề Sửa có mã; app cần khởi động lại để nạp bản build mới trước khi kiểm.

## 5. Tác động lên môi trường test

- Chỉ DB test `CoffeeManager_Test_040_20261121_214133` bị thêm dữ liệu fixture
  (NCC11–NCC20, PT011–PT042, DT003–DT007, TC013–TC037, CV011–CV015, CA011–CA015,
  SP020–SP431, NVL22–NVL25, B0012–B0014, KV011–KV013, KM…0001–0003)
  và bảng `dbo.TmpCodeRace`; DB thật/development không bị ghi bởi đợt test.
- Sequence của DB test đã tiến lên theo số lần cấp; `SeqNhaCungCap` được đặt lại 19.
- `SchemaMigrations` vẫn 40, không áp migration nào trong lúc test.

## 6. Ghi nhận (không phải lỗi của task)

- Log `evidence/run-01.txt` bị lỗi hiển thị tiếng Việt do codepage console của `sqlcmd`
  (cột PASS/FAIL và mã vẫn đọc được); file `.sql` là UTF-8 và chạy với `-f 65001`.
- Migration 039 (`promotion-dieu-kien-tu-ap`) vẫn lỗi biên dịch batch trên DB dev như đã
  ghi trong bug-report của task đó; 040 gate theo 38 và không phụ thuộc 039. Không sửa
  trong đợt test này.

## 7. Chờ duyệt

Kết quả chức năng đạt; đề nghị người dùng kiểm U01/V01 trên app thật (khởi động lại app
để dùng build mới). Nếu U01 phát hiện lỗi, ghi bug vào
`docs/tasks/auto-generated-codes/bug-report.md` và chờ duyệt đợt bug trước khi sửa.
DB test và bảng `TmpCodeRace` đang giữ làm bằng chứng; có thể xóa sau khi nghiệm thu.
