# Kết quả nghiệm thu — T09 — 18/10/2026

**ManualM14 — PASS nhánhxóachưadùng(T09-K08):** người dùngxóaNVL83; SQLentity/mốc0, receipt3giữđủCreate/Update/Delete. [Evidence](../runs/manual-g03-20261019/M14-db.json). KhôngđóngT09 hoặc tickma trậnquyền/DPI/timeoutchưa chạy.

**ManualM12/M13 ngày19/10 — PASS nhánhđổiđơnvịtrướcphátsinh(T09-K06):** NVL83tạoKg/tồn0/mốc0 rồi sửađơng→SQLg/tồn0/mốc0/receipt2. [Evidence](../runs/manual-g03-20261019/M13-db.json), thao tác theo xác nhận người dùng, không có ảnhmới. M05đãkiểmnhánhkhóasau mởsổriêng.

**ManualM11 ngày19/10 — PASS đơnvịtrống(T09-K04):** GUI từchốiNVL83, giữmã/tên; SQLkhôngcóentity/mốc/receipt. [Evidence](../runs/manual-g03-20261019/M11-db.json). Không sửa code trong lượt test.

**ManualM10 ngày19/10 — PASS nhánhGUI giữ4sốlẻ:** NVL82mởsổ qua app, SQL tồn/Quantity1.2345Kg, mộtmốc, receipt2(create+open). Nguồn thực tếKiểm thử số âm (người dùng giữinputcũ), khác nhãn gợiý nhưng hợp lệ. [Evidence](../runs/manual-g03-20261019/M10-db.json). T09-K01 có chứng cứ tạoADMIN/tồn0 rồi4sốlẻ; nhánhSTORE và ma trậnGUIchung chưa suyPASS. M09chờ xác nhận đãbấmLưu với5sốlẻ vì ảnhcó thểgiữ thông báo cũ.

**ManualM07/M08 ngày19/10:** người dùng tạoNVL82, SQLtồn0/mốc0/receipt1. GUI mởsổ-0,0001 bịtừchối và giữinput; SQL sau lỗi vẫn0/0/1:PASS nhánhsốâm T09-K04. [Evidence](../runs/manual-g03-20261019/M08-db.json). Các boundary khác vàfocus/ErrorProvider chưa suyPASS từ ảnh này.

**ManualM06 ngày19/10 — PASS nhánhchặn xóa sau mởsổ(T09-K08):** GUIbáo từchối đãcómốc; SQLNVL81vẫncòn/Kg/tồn0.0180/mốc1/receipt3 không đổi. [Evidence](../runs/manual-g03-20261019/M06-db.json). Source không sửa; không thêmFAIL cho lỗi nghiệp vụ dự kiến.

**ManualM05 ngày19/10 — PASS nhánhkhóa đơn vị sau mởsổ (T09-K06):** GUI từchốiKg→g và giữinput; SQL vẫnKg/tồn0.0180/mốc1/receipt3. [Evidence](../runs/manual-g03-20261019/M05-db.json). NhánhrenameM04 và nhánhkhóađơnvịM05 đềuPASS; không suyPASS các nhánh khác chưa chạy.

**ManualM03/M04 bổ sung19/10:** mở sổ lần2 bị từ chối, input giữ, SQL tồn0.0180/mốc1/receipt2 không đổi:PASS nhánhGUI T09-K03. Đổi tênNVL81 thànhCà phê kiểm thử GUI đổi tên thành công, SQL tên đầy đủ/tồn0.0180/mốc1, receipt3:PASS nhánhGUI renameT09-K06. EvidenceM03-db.json/M04-db.json trong thư mụcmanual-g03-20261019 và ảnh người dùng. Đang kiểm đổi đơn vị sau mở sổ; chưa kết luận ca chưa chạy.

**Bổ sung manual19/10:** [biên bản từng bước](../runs/manual-g03-20261019/progress.md). M01createNVL81 qua GUI và đối chiếuSQLPASS: tồn0/mốc0/receipt1. M02mở sổ0,0180Kg qua GUI vàSQLPASS: tồn/lượngmốc0.0180, Manual, nguồnđúng, mộtmốc, tổngreceipt2(create+open). EvidenceM01-db.json/M02-db.json và ảnh người dùng trong chat. Chưa suyPASS mọi subcaseID từ luồng này; đang kiểmM03mở sổ lại bị chặn.

Đợt test đã được người dùng duyệt. Trạng thái task: **Đang làm**; còn BLOCKED giao diện. Source giữ cố định; chưa debug. Các dòng PASS dưới đây chỉ nghiệm thu phạm vi ghi rõ, không đóng task hoặc bỏ regression G04.

Môi trường: Windows, .NET10, SQL Server localhost; DB test riêng clean/demo baseline→009. Snapshot 139 file sản phẩm/SQL/test không đổi, xem [audit](../runs/acceptance-extended-20261018/source-audit-final.json). Schema/source, input và câu SQL chính xác ở từng script/SQL fixture cạnh JSON. Tài khoản runtime WITHOUT LOGIN/CafeManagerApp cho ca đánh dấu RuntimePrincipal; các ca ADMIN khác đặt session role trên connection operator. Không coi kiểm SQL là bằng chứng BLL/native UI.

| ID | Expected gốc | Actual | Trạng thái | Evidence |
|---|---|---|---|---|
| T09-K01 | [I/M] Nguyên liệu mới có tồn 0, tên/đơn vị hợp lệ; số lượng 4 chữ số lẻ không bị mất. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T09-K02 | [I] Mở sổ số dư hợp lệ ghi tồn và dấu vết/mốc trong cùng transaction; có lỗi rollback cả hai. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T09-K03 | [I] Mở sổ lần hai hoặc sau khi đã phát sinh giao dịch bị chặn. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T09-K04 | [I/M] Số âm/quá precision/đơn vị trống bị từ chối, không âm thầm làm tròn sai. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T09-K05 | [M] Ví dụ 18g quy đổi nhập 0,018Kg; preview/DB cùng đơn vị. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T09-K06 | [I] Sửa tên không đổi tồn; đổi đơn vị sau có công thức/giao dịch bị chặn hoặc qua migration có kiểm soát. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T09-K07 | [I/M] User CASH chỉ đọc; ADMIN/STORE không sửa trực tiếp tồn từ editor sau mở sổ. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T09-K08 | [I] Xóa nguyên liệu đã được công thức/phiếu/tiêu hao tham chiếu bị chặn. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T09-K09 | [M] Danh sách tồn refresh sau giao dịch, không dùng cache làm kết quả chốt. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |

## Phép kiểm SQL bổ sung đã thực hiện

Các ID mở rộng là phép đo/subcase, không thêm hoặc thay 47 ID gốc. Expected/Actual dưới đây lấy nguyên dữ liệu đo. Chạy script PowerShell tương ứng trong thư mục evidence, bằng connection Windows tới DB đã ghi trong manifest; không chạy lại trên DB development. DB test đã dọn nên cần tạo fixture mới nếu chạy lại.

| Subcase | Expected | Actual | Status | Evidence |
|---|---|---|---|---|
| T09-K01-a | Create zero, exact opening, replay once | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T09-K03-a | Second opening rejected | SQL error 51602; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T09-K04-a | Scale5 rejected | SQL error 51601; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T09-K04-b | Negative rejected | SQL error 51601; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T09-K04-c | Blank unit rejected | SQL error 51601; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T09-K06-a | Rename keeps stock | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T09-K06-b | Unit change after opening rejected | SQL error 51602; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T09-K07-a | CASH runtime read | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T09-K07-b | CASH create denied | SQL error 51605; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T09-K07-c | STORE opening denied | SQL error 51605; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T09-K07-d | Direct stock DML denied | SQL error 229; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T09-K08-a | Delete referenced material rejected | SQL error 51602; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T09-K03-two | Second request blocks; then error 51603; final SQL assertions hold | Blocked at LCK_M_RS_U:56; SQL 51603; assertions and transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/concurrency-fault-results-v3.json); SQL/script cùng thư mục |
| T09-K02-fault | SQL 52990; rollback invariants checked separately | Assertions/error expectation completed | PASS | [JSON](../runs/acceptance-extended-20261018/fault-rollback-results.json); SQL/script cùng thư mục |
| T09-K02-invariants | All assertions hold; transaction 0:0 | Assertions/error expectation completed | PASS | [JSON](../runs/acceptance-extended-20261018/fault-rollback-results.json); SQL/script cùng thư mục |
| Migration-rerun | AlreadyApplied and all inventory balances unchanged | AlreadyApplied | PASS | [JSON](../runs/acceptance-extended-20261018/migration-results.json); SQL/script cùng thư mục |
| Migration-checksum | Changed checksum rejected before DDL | True | PASS | [JSON](../runs/acceptance-extended-20261018/migration-results.json); SQL/script cùng thư mục |
| T09-preflight | Invalid legacy name rejects004 without normalization/DDL/version | Invariant=1 | PASS | [JSON](../runs/acceptance-extended-20261018/migration-results.json); SQL/script cùng thư mục |
| T09-K04-maximum | Maximum decimal18,4 persists exactly | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-material-edge-results.json); SQL/script cùng thư mục |
| T09-K04-overflow | Quantity over decimal18,4 maximum rejected | SQL error 51601; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-material-edge-results.json); SQL/script cùng thư mục |
| T09-K06-before-source | Unit changes before any source allowed and stock remains zero | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-material-edge-results.json); SQL/script cùng thư mục |
| T09-K06-recipe | Unit changes after recipe blocked without opening marker | SQL error 51602; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-material-edge-results.json); SQL/script cùng thư mục |
| T09-K08-recipe | Delete recipe-referenced material blocked | SQL error 51602; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-material-edge-results.json); SQL/script cùng thư mục |
| T09-K03-transaction-setup | Valid baseline purchase increases stock after material creation | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-material-edge-results.json); SQL/script cùng thư mục |
| T09-K03-after-transaction | Opening after transaction rejected without preexisting opening marker | SQL error 51602; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-material-edge-results.json); SQL/script cùng thư mục |
| T09-K06-transaction | Unit change after transaction blocked | SQL error 51602; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-material-edge-results.json); SQL/script cùng thư mục |
| T09-K08-transaction | Delete material with transaction source blocked | SQL error 51602; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-material-edge-results.json); SQL/script cùng thư mục |
| T09-K03-other-sources-setup | Valid PX, KK and consumption fixture sources exist | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-source-guards-results.json); SQL/script cùng thư mục |
| T09-K03-source-NVL85 | Opening rejected for material with transaction source | SQL error 51602; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-source-guards-results.json); SQL/script cùng thư mục |
| T09-K08-source-NVL85 | Deleting referenced material rejected | SQL error 51602; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-source-guards-results.json); SQL/script cùng thư mục |
| T09-K03-source-NVL86 | Opening rejected for material with transaction source | SQL error 51602; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-source-guards-results.json); SQL/script cùng thư mục |
| T09-K08-source-NVL86 | Deleting referenced material rejected | SQL error 51602; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-source-guards-results.json); SQL/script cùng thư mục |
| T09-K03-source-NVL87 | Opening rejected for material with transaction source | SQL error 51602; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-source-guards-results.json); SQL/script cùng thư mục |
| T09-K08-source-NVL87 | Deleting referenced material rejected | SQL error 51602; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-source-guards-results.json); SQL/script cùng thư mục |

## Giới hạn và phần còn lại

- Unit: bộ lọc G03/MaterialRulesTests 20/20 PASS, [TRX](../runs/acceptance-unit-20261018/g03-unit.trx). Đây là tổng của nhóm, không phải 20 ca cho từng task.
- BLOCKED: login/CRUD/selector bằng app thật, loading/rỗng/lỗi/xung đột/timeout, giữ input/chống gửi lặp, điều hướng100 lần/đăng xuất10 lần, Designer, kích thước1280×720/1366×768 và DPI100/125/150%, tab/focus/phím tắt. Những subcase manual trong plan chưa có thao tác thực tế; không suy PASS từ source/ảnh kỹ thuật.
- Công cụ Windows trả lỗi active request và ảnh không phải cửa sổ target sau reset. Dừng nhập ngay; chưa nhập mật khẩu hoặc bấm đăng nhập. App test đóng khi cleanup.
- Baseline checkout đã kiểm cạnh tranh công thức/policy; checkout/POS đích T14–T16 vẫn NOT RUN. Ca refresh SQL hai connection không thay kiểm chọn bàn ở giao diện.
- Log thử nghiệm harness đầu giữ nguyên: sql-results.json/concurrency-fault-results*.json có lỗi phép đo. Phiên bản hợp lệ là v2 SQL/v3 concurrency; transaction SELECT trực tiếp, context pooling, chuyển rowversion và lấy SPID được sửa ở script đo mới, không sửa source sản phẩm. Không tính lỗi harness thành lỗi sản phẩm hoặc xóa lịch sử.
