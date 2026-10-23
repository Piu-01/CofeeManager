# Kết quả nghiệm thu — T11 — 18/10/2026

**ManualM26 ngày20/10 — PASS xóaKVđãhếtbàn:** xácnhậnngườidùng+ [SQL](../runs/manual-g03-20261019/M26-db.json) KV981vàbàncon0, giữAreaCreate/AreaDelete. BướcCRUDfixtuređãcóevidence, khôngtickrestart/role/matrixGUIchưa chạy.

**ManualM25 ngày20/10 — PASS xóabànchưalịchsử(T11-K06):** người dùngxóaB0981; [SQL](../runs/manual-g03-20261019/M25-db.json) bảng/viewđều0, KV981còn/0bàncon, giữreceiptCreate/Update/Delete. KhôngthaykếtquảcaFKbàncólịchsửchưachạymanual.

**ManualM24 ngày20/10 — PASS chặn xóaKVcóbàn(T11-K06):** GUIràngbuộcchungtừchối; [SQL](../runs/manual-g03-20261019/M24-db.json) KV981/B0981liênkếtnguyên, receiptarea1/bàn2khôngđổi. KhôngsuyPASSthôngbáochi tiết/vìlịch sửhóađơn từcaFKnày.

**ManualM21–M23 ngày20/10:** tạoKV981đúngtên/receipt1; B0981banđầuchọnmặcđịnhKV001đượcngườidùngxácnhận(khôngmappingbug), sauSửasangKV981thànhcông. [M23-db.json](../runs/manual-g03-20261019/M23-db.json) khớpquan hệ/tên, viewTrong/khôngđơn, receiptbàn2(create+update):PASScácbướctạoarea/bàn/chuyểnarea củaT11-K01; chưađủmọisubcase/restart/quyền/DPI.

Đợt test đã được người dùng duyệt. Trạng thái task: **Đang làm**; còn BLOCKED giao diện. Source giữ cố định; chưa debug. Các dòng PASS dưới đây chỉ nghiệm thu phạm vi ghi rõ, không đóng task hoặc bỏ regression G04.

Môi trường: Windows, .NET10, SQL Server localhost; DB test riêng clean/demo baseline→009. Snapshot 139 file sản phẩm/SQL/test không đổi, xem [audit](../runs/acceptance-extended-20261018/source-audit-final.json). Schema/source, input và câu SQL chính xác ở từng script/SQL fixture cạnh JSON. Tài khoản runtime WITHOUT LOGIN/CafeManagerApp cho ca đánh dấu RuntimePrincipal; các ca ADMIN khác đặt session role trên connection operator. Không coi kiểm SQL là bằng chứng BLL/native UI.

| ID | Expected gốc | Actual | Trạng thái | Evidence |
|---|---|---|---|---|
| T11-K01 | [I/M] CRUD bàn/khu vực hợp lệ và quan hệ khu vực được giữ sau reload. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T11-K02 | [I] Tên trống/quá dài/FK khu vực không có bị chặn. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T11-K03 | [I/M] Seed B0003 đang phục vụ, các bàn còn lại theo dữ liệu view thực tế. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T11-K04 | [I] Đơn đã trả/hủy không làm bàn bận; đơn mở tại quán làm bàn bận. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T11-K05 | [M] Không truy vấn/update BAN.TrangThai vì cột không còn. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T11-K06 | [I] Xóa bàn/khu vực có hóa đơn/bàn con bị từ chối, không xóa lịch sử. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T11-K07 | [M] Chọn bàn refresh và lấy đơn hiện tại, không tin trạng thái màu cũ trên UI. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T11-K08 | [U/M] Quyền quản trị danh mục bàn/khu vực và quyền xem POS đúng vai trò. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |

## Phép kiểm SQL bổ sung đã thực hiện

Các ID mở rộng là phép đo/subcase, không thêm hoặc thay 47 ID gốc. Expected/Actual dưới đây lấy nguyên dữ liệu đo. Chạy script PowerShell tương ứng trong thư mục evidence, bằng connection Windows tới DB đã ghi trong manifest; không chạy lại trên DB development. DB test đã dọn nên cần tạo fixture mới nếu chạy lại.

| Subcase | Expected | Actual | Status | Evidence |
|---|---|---|---|---|
| T11-K03-a | Seed view B0003 serving | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T11-K06-a | Area with tables cannot delete | SQL error 547; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T11-K08-a | STORE map read denied | SQL error 51605; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T11-K08-b | CASH map read succeeds | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T11-K04-view | Paid/cancelled do not occupy table; open does | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-history-results.json); SQL/script cùng thư mục |
| T11-K07-refresh | After second session cancels order select-current sees empty table | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-history-results.json); SQL/script cùng thư mục |
| T11-K07-two-session | A initially sees open HD09999005; B cancels; A reread has NULL | {"After":null,"Before":"HD09999005"} | PASS | [JSON](../runs/acceptance-extended-20261018/table-refresh-two.json); SQL/script cùng thư mục |
| T11-K02-empty | Empty area name rejected | SQL error 51801; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T11-K02-long | Overlength table name rejected | SQL error 51801; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T11-K02-fk | Invalid area FK rejected | SQL error 51801; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T11-K06-paid | Table with paid history cannot delete | SQL error 547; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T11-K06-cancel | Table with cancelled history cannot delete | SQL error 547; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |

## Giới hạn và phần còn lại

- Unit: bộ lọc G03/MaterialRulesTests 20/20 PASS, [TRX](../runs/acceptance-unit-20261018/g03-unit.trx). Đây là tổng của nhóm, không phải 20 ca cho từng task.
- BLOCKED: login/CRUD/selector bằng app thật, loading/rỗng/lỗi/xung đột/timeout, giữ input/chống gửi lặp, điều hướng100 lần/đăng xuất10 lần, Designer, kích thước1280×720/1366×768 và DPI100/125/150%, tab/focus/phím tắt. Những subcase manual trong plan chưa có thao tác thực tế; không suy PASS từ source/ảnh kỹ thuật.
- Công cụ Windows trả lỗi active request và ảnh không phải cửa sổ target sau reset. Dừng nhập ngay; chưa nhập mật khẩu hoặc bấm đăng nhập. App test đóng khi cleanup.
- Baseline checkout đã kiểm cạnh tranh công thức/policy; checkout/POS đích T14–T16 vẫn NOT RUN. Ca refresh SQL hai connection không thay kiểm chọn bàn ở giao diện.
- Log thử nghiệm harness đầu giữ nguyên: sql-results.json/concurrency-fault-results*.json có lỗi phép đo. Phiên bản hợp lệ là v2 SQL/v3 concurrency; transaction SELECT trực tiếp, context pooling, chuyển rowversion và lấy SPID được sửa ở script đo mới, không sửa source sản phẩm. Không tính lỗi harness thành lỗi sản phẩm hoặc xóa lịch sử.
