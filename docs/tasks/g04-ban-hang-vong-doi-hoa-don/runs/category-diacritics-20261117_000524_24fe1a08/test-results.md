# Kết quả test — nhãn danh mục trang Bán hàng (033 + Insert_Data)

Ngày chạy: 17/11/2026. RunId: `20261117_000524_24fe1a08`.
Phạm vi được duyệt: chỉ fix nhãn danh mục (`033_category_diacritics.sql` +
`Insert_Data.sql`); không chạy PP/TS của T33/T34, không sửa source/SQL, không ghi vào
`CoffeeManager_Development`.

## Môi trường

| Mục | Giá trị |
|---|---|
| SQL Server | `localhost`, collation `SQL_Latin1_General_CP1_CI_AS` |
| Commit nguồn | `8a38f86` (source đóng băng trong lượt chạy) |
| SHA256 `033_category_diacritics.sql` | `8B7DE26D7415A96D858BDD7121AB1A2C831A8A5CE720C20726C1CD5A6149E25A` |
| SHA256 `Insert_Data.sql` | `574B48F60ED6C4E2AE060FA16B962DF4A33041AE882A81941337466030E9D554` |
| DB clean | `CoffeeManager_Test_033_20261117_000524_24fe1a08_clean` |
| DB upgrade | `CoffeeManager_Test_033_20261117_000524_24fe1a08_upgrade` |
| Công cụ | `Install-Database.ps1 -Mode Demo -ThroughVersion 27`, `Apply-Migration.ps1`, `sqlcmd` (bản BOM trong LocalAppData theo ghi nhớ PS 5.1) |

## Kết quả theo ca

| Mã | Nội dung | Expected | Actual | Kết quả |
|---|---|---|---|---|
| DIA-01 | Static 033/seed | Guard `Version=32` + runner; UTF-8; 10 chuỗi `N'...'` khớp; không `USE`/đường dẫn tuyệt đối | 033 UTF-8 không BOM, guard dòng 3, VALUES 10 dòng; seed UTF-8 BOM, đổi đúng 10 dòng (+10/−10); không có `USE`/path | PASS |
| DIA-02 | Cài mới Demo→027 (seed mới) | 10 dòng có dấu, unique, đủ DM001–DM010, SP join đủ | CatRows=10, Missing=0, Mismatch=0, Dup=0, BadEncoding=0, Product 10/10 join | PASS |
| DIA-03 | Chạy 033 khi chưa có 032 (qua runner và trực tiếp) | FAIL `52107`, rollback sạch | Runner: exception "Cần schema032 và runner migration."; sqlcmd trực tiếp: `Msg 52107`; `Version 33`=0, dữ liệu không đổi | PASS |
| DIA-04 | Áp 028–032 rồi 033 trên DB clean | `Applied`, checksum đúng file | `Applied`; Version 33 + Checksum = SHA256 file; nhãn vẫn đúng 10/10 | PASS |
| DIA-05 | Chạy lại 033; chạy lại câu UPDATE trực tiếp | Runner `AlreadyApplied`; dữ liệu không đổi | `AlreadyApplied` trên cả 2 DB; direct UPDATE giữ nguyên nhãn (Mismatch=0) nhưng **bump `CatalogRevision`** (xem ghi chú 2) | PASS (có ghi chú) |
| DIA-06 | DB upgrade: fixture 10 nhãn ASCII cũ + DM011 tùy biến, snapshot | ASCII đúng seed HEAD; 11 danh mục; SP theo danh mục ổn định | CatRows=11, Dup=0, AsciiMismatch=0, DM011 OK; SP 10, join 10; DM001=4, DM002–DM004=2, còn lại 0 | PASS |
| DIA-07 | Áp 033 trên DB upgrade | 10 dòng đúng chuỗi; DM011 giữ nguyên; liên kết/unique không đổi | Version 33 + checksum đúng; DiacriticMismatch=0; CatRows=11; Dup=0; DM011 untouched; SP và số SP theo danh mục y hệt trước | PASS |
| DIA-08 | Va chạm unique: DM011=`N'Cà phê'`, áp 033 | FAIL 2601, rollback toàn bộ | Exception `UQ_DANHMUC_SP_Ten`; Version 33=0; 10 nhãn vẫn ASCII; DM011 vẫn là bản ghi va chạm | PASS |
| DIA-09 | Đường app: `EXECUTE AS USER` thuộc role `CafeManagerApp` gọi `sp_SalesCategories` | 10 nhãn đúng, thứ tự `ORDER BY TenDM`, có DM011 | ProcRows=11; ExactLabels=10; OrderViolations=0; CustomRow=1 | PASS |
| DIA-10 | Regression lọc món/lọc danh mục | Tập/count sản phẩm không đổi; nhãn danh mục có dấu | `sp_SalesMenu` all=10, DM001=4; `sp_ProductList`=10, `MaDM` không đổi; `sp_CatalogLookup(ProductCategory)` trả 11 nhãn có dấu theo mã; source không hard-code nhãn ASCII | PASS |
| DIA-11 | So sánh seed (DB clean) với kết quả 033 (DB upgrade) | 10/10 chuỗi trùng chính xác | PairRows=10, SeedVsMigrationMismatch=0 (so sánh `Latin1_General_100_BIN2`) | PASS |
| DIA-12 | Collation/encoding | Không mất dấu, không `?`, accent-sensitive | `BadLenOrQuestion=0`; binary và DB collation đều `distinct` cho `Ca phe`/`Cà phê`; mã code point DM001 ký tự 2 = 224 (`à`) | PASS |
| UI | Tab POS/hiển thị | Người dùng tự xem | Không chạy phía agent theo phân công | SKIPPED |

Tổng: **12/12 PASS ở phần chạy được**, UI SKIPPED do người dùng tự kiểm.

## Bằng chứng

- `clean-install.json`, `upgrade-install.json`: manifest cài DB và 27 migration.
- `migrations-clean.log`, `migrations-upgrade.log`: 028–032 `Applied`.
- `dia02-seed-check.log`, `dia03-guard.log`, `dia04-apply.log`, `dia05-rerun.log`,
  `dia06-before.log`, `dia07-apply.log`, `dia08-collision.log`,
  `dia09-app-principal.log`, `dia11-12-check.log`.
- `fixture-ascii-prefix.sql`: fixture ASCII + DM011 (chỉ dùng cho DB upgrade của run).
- `source-hashes.txt`, `manifest.json`.

## Ghi chú và rủi ro

1. **Thiếu tài liệu (ISSUE, không sửa trong lượt test):**
   `docs/database/migrations.md` có mục 032 và 034 nhưng không có mục 033. Đề nghị bổ
   sung mô tả migration 033 trong lần cập nhật tài liệu kế tiếp.
2. **Chạy lại câu UPDATE trực tiếp** (ngoài runner) vẫn được SQL Server báo 10 dòng
   affected và bump `CatalogRevision` dù nhãn không đổi. Đường chuẩn `Apply-Migration`
   trả `AlreadyApplied` và không chạm dữ liệu; không chạy tay câu UPDATE trên DB thật.
3. **Ghi đè nhãn tùy biến:** 033 đặt lại nhãn 10 mã DM001–DM010 vô điều kiện; danh mục
   khác (đã kiểm DM011) giữ nguyên. Nếu operator đã đổi tên một trong 10 mã chuẩn, tên
   đó sẽ bị thay theo mục tiêu của fix.
4. **Va chạm unique:** nếu DB đã có danh mục khác trùng một trong 10 nhãn mới, 033 fail
   `2601` và rollback sạch (đã kiểm); cần đổi tên danh mục trùng rồi chạy lại.
5. **Thứ tự tab thay đổi:** `sp_SalesCategories` sắp `ORDER BY TenDM`; thứ tự tab theo
   nhãn có dấu khác thứ tự ASCII cũ. Chọn lại danh mục vẫn giữ theo `MaDM` (source
   `BuildCategoryTabs`/`categoryCode`), không phụ thuộc nhãn.
6. **Thiếu dòng đích:** nếu một mã trong 10 mã không tồn tại, UPDATE chỉ sửa các dòng có
   mặt và migration vẫn ghi Version 33 (không có assertion đủ 10). Không gặp trong seed
   chuẩn; ghi nhận để biết.
7. Không dùng `CoffeeManager_Development` làm bằng chứng: DB này đang schema 31 và đã có
   nhãn có dấu do chỉnh tay, chưa có Version 33.
8. DB test và thư mục bằng chứng được giữ lại, chưa xóa; quyết định dọn sau báo cáo.
