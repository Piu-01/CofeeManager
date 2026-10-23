# Kết quả nghiệm thu — T10 — 18/10/2026

**ManualM20 ngày20/10 — PASS trùngNVL(T10-K02):** người dùngbáoNguyênliệubịtrùng; [SQL](../runs/manual-g03-20261019/M20-db.json) vẫnrecipe2dòng0.0200/0.0180, receipt3, trạngtháiDangban. Khôngghi thêm. Không đóngT10hoặcvisualB03từnhánhnày.

**ManualM19 ngày20/10 — PASS địnhmức0(T10-K02):** GUIbáoĐịnhmứcphảidương/giữinput; [SQLsauvalidation](../runs/manual-g03-20261019/M19-db.json) vẫn2dòng0.0200/0.0180, receipt3, tồnkhôngđổi. Đây làvalidationkhôngghi, không thay bằngchứngrollbackengineT10-K03. VisualB03vẫntạmgác.

**ManualM17/M18 ngày20/10:** người dùngxácnhận lượngnhập NVL01=0.0200/NVL02=0.0180, SQLkhớpinput(PASSmapping/lưu2dòng), khôngcóbugđảolượng. Sau bậtbánGUI, [M18-db.json](../runs/manual-g03-20261019/M18-db.json) xácnhậnDangban/25000/recipegiữnguyên/tồnkhôngđổi/receiptUpdate1lần:PASSnhánhbậtbánđủrecipeT10-K04. UIvisualB03vẫntạmgác/chưađủregression, khôngxóalịch sửM17UXFAIL.

**20/10 — M17 UX FAIL:** ảnhngườidùng/source xácnhận editorrecipe chưađạt: combo/editortheme, unitdelay, affordanceôlượng, thao tácxóadòng. Lưu2dòngNOTRUN, SQLSP981chưacórecipe/receipt1. [BugđợtG03-B03](bug-report.md), [SQL](../runs/manual-g03-20261019/M17-investigation-db.json). KếtquảSQL/unitPASS cũ không thay thếUXFAIL này; chưađóngT10 hoặc sửa trong lượt test. Ma trận47IDphía dưới là snapshottrướcmanual, không phủnhậnFAILsubcase mới.

**ManualM16 ngày20/10 — PASS nhánhGUI không bật bán khi thiếu recipe(T10-K04):** sau cuộn vùnginput, người dùngchọnĐangbán/Lưu, appbáocầncôngthức vàgiữinput. SQLvẫnNgungban/recipe0/receipt1, giá25000. [Evidence](../runs/manual-g03-20261019/M16-db.json). PhầncheckoutđíchG04vẫnNOTRUN.

**ManualM15 ngày20/10 — PASS bướctạometadata(T09fixture→T10-K01):** người dùngtạoSP981/Cà phê thử GUI/DM001/25000/Ngừngbán. ẢnhGUIkhớpSQL: Ngungban,recipe0,receipt1. [Evidence](../runs/manual-g03-20261019/M15-db.json). ChưaPASS toànIDvìcònthêmrecipe/nhiềudòng vànhánhchưa chạy; đangkiểmchặnbậtbánthiếurecipeT10-K04.

Đợt test đã được người dùng duyệt. Trạng thái task: **Đang làm**; còn BLOCKED giao diện. Source giữ cố định; chưa debug. Các dòng PASS dưới đây chỉ nghiệm thu phạm vi ghi rõ, không đóng task hoặc bỏ regression G04.

Môi trường: Windows, .NET10, SQL Server localhost; DB test riêng clean/demo baseline→009. Snapshot 139 file sản phẩm/SQL/test không đổi, xem [audit](../runs/acceptance-extended-20261018/source-audit-final.json). Schema/source, input và câu SQL chính xác ở từng script/SQL fixture cạnh JSON. Tài khoản runtime WITHOUT LOGIN/CafeManagerApp cho ca đánh dấu RuntimePrincipal; các ca ADMIN khác đặt session role trên connection operator. Không coi kiểm SQL là bằng chứng BLL/native UI.

| ID | Expected gốc | Actual | Trạng thái | Evidence |
|---|---|---|---|---|
| T10-K01 | [I/M] Tạo sản phẩm giá dương, đúng danh mục; thêm nhiều định mức hợp lệ. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T10-K02 | [I] Giá 0/âm, định mức 0/âm, FK sai hoặc cặp SP/NVL trùng bị chặn. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T10-K03 | [I] Một dòng công thức lỗi khiến toàn bộ thay công thức rollback, công thức cũ còn nguyên. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T10-K04 | [I/M] Không bật bán/checkout món không công thức; bánh thành phẩm định mức 1 hoạt động. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T10-K05 | [M] Ngừng bán làm món không chọn mới; đơn mở đang chứa món cần sửa, không tự bỏ món. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T10-K06 | [I] Đổi giá menu không đổi GiaBan của dòng hóa đơn cũ. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T10-K07 | [I] Đổi công thức không đổi TIEUHAO_HOADON đã lưu. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T10-K08 | [I] Lưu công thức đồng thời checkout bị khóa/đọc snapshot nhất quán, không công thức nửa cũ nửa mới. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T10-K09 | [U/M] CASH không sửa menu, STORE chỉ đọc công thức; ADMIN quản trị được. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T10-K10 | [I/M] Xóa sản phẩm có lịch sử bị chặn; tìm/lọc danh mục và mở lại hiển thị đúng. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |

## Phép kiểm SQL bổ sung đã thực hiện

Các ID mở rộng là phép đo/subcase, không thêm hoặc thay 47 ID gốc. Expected/Actual dưới đây lấy nguyên dữ liệu đo. Chạy script PowerShell tương ứng trong thư mục evidence, bằng connection Windows tới DB đã ghi trong manifest; không chạy lại trên DB development. DB test đã dọn nên cần tạo fixture mới nếu chạy lại.

| Subcase | Expected | Actual | Status | Evidence |
|---|---|---|---|---|
| T10-K01-a | Multi-line recipe saved | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T10-K02-a | Invalid recipe rejected | SQL error 51701; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T10-K03-a | Old recipe remains after error | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T10-K04-a | Cannot sell without recipe | SQL error 51702; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T10-K09-a | STORE runtime recipe read | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T10-K09-b | CASH recipe mutation denied | SQL error 51605; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T10-K08-checkout-first | Second request blocks; then error 0; final SQL assertions hold | Blocked at LCK_M_S:56; SQL 0; assertions and transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/concurrency-fault-results-v3.json); SQL/script cùng thư mục |
| T10-K08-writer-first | Second request blocks; then error 0; final SQL assertions hold | Blocked at LCK_M_S:56; SQL 0; assertions and transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/concurrency-fault-results-v3.json); SQL/script cùng thư mục |
| T10-K03-fault | SQL 52990; rollback invariants checked separately | Assertions/error expectation completed | PASS | [JSON](../runs/acceptance-extended-20261018/fault-rollback-results.json); SQL/script cùng thư mục |
| T10-K03-invariants | All assertions hold; transaction 0:0 | Assertions/error expectation completed | PASS | [JSON](../runs/acceptance-extended-20261018/fault-rollback-results.json); SQL/script cùng thư mục |
| T10-K03-recipe-compare | Identical old recipe | [{"MaNVL":"NVL01","DinhMuc":0.0500},{"MaNVL":"NVL02","DinhMuc":0.0600}] | PASS | [JSON](../runs/acceptance-extended-20261018/fault-rollback-results.json); SQL/script cùng thư mục |
| T10-K05-stop | Stop selling removes selector entry and preserves existing invoice lines | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-history-results.json); SQL/script cùng thư mục |
| T10-K06-price | Changing menu price preserves all historical snapshot prices | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-history-results.json); SQL/script cùng thư mục |
| T10-K07-snapshot | Replacing recipe preserves consumption snapshots and stock | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-history-results.json); SQL/script cùng thư mục |
| T10-K10-delete-history | Product with paid invoices cannot delete | SQL error 51702; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-history-results.json); SQL/script cùng thư mục |
| T10-K10-delete-unused | Unused product deletes atomically, receipt remains | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-history-results.json); SQL/script cùng thư mục |
| T10-preflight | Selling legacy product without recipe rejects005; no auto stop | Invariant=1 | PASS | [JSON](../runs/acceptance-extended-20261018/migration-results.json); SQL/script cùng thư mục |
| T10-K02-price-0 | Invalid price rejected | SQL error 51701; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T10-K02-price--1 | Invalid price rejected | SQL error 51701; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T10-K02-price-0.5 | Invalid price rejected | SQL error 51701; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T10-K02-qty-0 | Invalid recipe quantity rejected | SQL error 51701; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T10-K02-qty--1 | Invalid recipe quantity rejected | SQL error 51701; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T10-K02-qty-0.00001 | Invalid recipe quantity rejected | SQL error 51701; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T10-K02-qty-100000000000000 | Invalid recipe quantity rejected | SQL error 51701; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T10-K02-duplicate | Duplicate recipe ingredient rejected | SQL error 51701; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T10-K02-fk | Invalid material FK rejected | SQL error 51701; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |

## Giới hạn và phần còn lại

- Unit: bộ lọc G03/MaterialRulesTests 20/20 PASS, [TRX](../runs/acceptance-unit-20261018/g03-unit.trx). Đây là tổng của nhóm, không phải 20 ca cho từng task.
- BLOCKED: login/CRUD/selector bằng app thật, loading/rỗng/lỗi/xung đột/timeout, giữ input/chống gửi lặp, điều hướng100 lần/đăng xuất10 lần, Designer, kích thước1280×720/1366×768 và DPI100/125/150%, tab/focus/phím tắt. Những subcase manual trong plan chưa có thao tác thực tế; không suy PASS từ source/ảnh kỹ thuật.
- Công cụ Windows trả lỗi active request và ảnh không phải cửa sổ target sau reset. Dừng nhập ngay; chưa nhập mật khẩu hoặc bấm đăng nhập. App test đóng khi cleanup.
- Baseline checkout đã kiểm cạnh tranh công thức/policy; checkout/POS đích T14–T16 vẫn NOT RUN. Ca refresh SQL hai connection không thay kiểm chọn bàn ở giao diện.
- Log thử nghiệm harness đầu giữ nguyên: sql-results.json/concurrency-fault-results*.json có lỗi phép đo. Phiên bản hợp lệ là v2 SQL/v3 concurrency; transaction SELECT trực tiếp, context pooling, chuyển rowversion và lấy SPID được sửa ở script đo mới, không sửa source sản phẩm. Không tính lỗi harness thành lỗi sản phẩm hoặc xóa lịch sử.
