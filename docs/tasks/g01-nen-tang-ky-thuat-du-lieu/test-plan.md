# G01 — Đề nghị duyệt đợt test sau triển khai

**Trạng thái:** Đã test/debug theo duyệt B01–B06; regression G01_Regression_20261008_222427 đạt 42 PASS/3 BLOCKED manual.  
**Duyệt test:** Người dùng xác nhận “duyệt” ngày 08/10/2026.  
**Bộ plan:** Đã duyệt ngày 08/10/2026; đầu ra T03–T05 đã viết.  
**Source:** Chưa commit; khi bắt đầu test chụp hash source/schema/fixture/migration.

## Phạm vi — 26 ca chạy mới, 19 ca kế thừa

| Task | Ca | Cách chạy |
|---|---|---|
| T01 | 7 ca PASS kế thừa | Giữ bằng chứng T01; regression build sạch/SDK trong T04 |
| T02 | 12 ca PASS kế thừa | Giữ JSON/runs T02; schema/seed không đổi. T03/T05 setup mới kiểm chứng compatibility; không tự ghi đè kết quả T02 |
| T03 | T03-K01–K09 | `tools/database/Test-ContractMigration.ps1` + review config/contract K08/K09 |
| T04 | T04-K01–K07 | Restore, Debug/Release build, reference/SQL review, chạy Form, Designer và source-copy sạch |
| T05 | T05-K01–K10 | `tools/database/Test-DataAccess.ps1` (xUnit) + UI/log/guard/manual theo plan |

## Thứ tự trong đợt đã duyệt

1. Chụp hashes và môi trường. Giữ source cố định toàn lượt; chưa có kết quả PASS
   cho T03/T04/T05 chỉ từ việc đọc/viết code.
2. T03: DB `CoffeeManager_Test_T03_<run>` và DB `_empty` mới; schema/seed nguyên
   logic, migration version/checksum/rollback, hai session mã, app user giới hạn,
   DML bị chặn. Lưu JSON/Markdown và danh sách mã đồng thời.
3. T03-K08/K09: review principals/local config không secrets; đối chiếu CHECK,
   computed/DTO, TVP/procedure, tài liệu ownership. Ghi actual, không PASS tự động.
4. T04: `dotnet restore CafeManager.slnx`, build Debug/Release; mở StartupForm;
   kiểm reference/namespace/composition root/UI không SQL; copy sạch sang thư
   mục kiểm chứng rồi build. Designer và bố cục cần WinForms thật.
5. T05: runner tạo DB test T05 mới, schema/seed/migration/fixtures và xUnit; dùng
   app user WITHOUT LOGIN qua impersonation để kiểm quyền SQL. App runtime UI
   dùng Windows principal riêng được provision, không mặc định admin/test DB.
6. Manual T04/T05: mở VS Designer, Form 1280×720/1366×768, DPI 100/125/150%,
   Tab/focus/loading/rỗng/lỗi/giữ input. `database/tests/t05/slow-ui.sql` giữ khóa
   đọc 20 giây ở session SQL test riêng để thử app không freeze, Hủy tải/đóng
   Form; xác nhận không update control đã dispose/kết quả muộn.
7. Đối chiếu log thao tác/mã/correlation, lỗi known/unknown/timeout; guard từ
   chối production/system DB và kiểm cleanup/sentinel. Ghi manual evidence.

Thiếu quyền/GUI/principal riêng → BLOCKED đúng ca, gom yêu cầu người dùng.
Runner giữ DB mới để tra cứu, không drop/reset DB có sẵn. Một lần duyệt đợt này
cho phép chạy các bước trên và các thao tác manual thuộc plan, không debug.

## Kết quả và bug

Lưu từng task tại `t0x-.../test-results.md`, JSON/TRX/log/runs khi đã chạy.
Hoàn tất lượt test → báo cáo chung `report.md` kể cả FAIL/BLOCKED. Bug ghi tại
task sở hữu, có bằng chứng/hướng sửa/ảnh hưởng/regression, liên kết trong report.
Chờ duyệt cả đợt bug một lần mới debug/regression; không sửa giữa lượt test
hoặc điều chỉnh expected để né lỗi. Bug mới chưa được duyệt cần duyệt bổ sung.
