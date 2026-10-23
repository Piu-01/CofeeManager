# Kết quả nghiệm thu — T13 — 18/10/2026

Đợt test đã được người dùng duyệt. Trạng thái task: **Đang làm**; còn BLOCKED giao diện. Source giữ cố định; chưa debug. Các dòng PASS dưới đây chỉ nghiệm thu phạm vi ghi rõ, không đóng task hoặc bỏ regression G04.

Môi trường: Windows, .NET10, SQL Server localhost; DB test riêng clean/demo baseline→009. Snapshot 139 file sản phẩm/SQL/test không đổi, xem [audit](../runs/acceptance-extended-20261018/source-audit-final.json). Schema/source, input và câu SQL chính xác ở từng script/SQL fixture cạnh JSON. Tài khoản runtime WITHOUT LOGIN/CafeManagerApp cho ca đánh dấu RuntimePrincipal; các ca ADMIN khác đặt session role trên connection operator. Không coi kiểm SQL là bằng chứng BLL/native UI.

| ID | Expected gốc | Actual | Trạng thái | Evidence |
|---|---|---|---|---|
| T13-K01 | [I] Không khách/không KM giảm 0; chỉ khách dùng hạng; có KM dùng KM, không cộng dồn. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T13-K02 | [I] Ngày bắt đầu/kết thúc đều hợp lệ, chương trình cùng ngày hợp lệ; ngoài ngày bị từ chối. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T13-K03 | [I/M] Giảm 0%, 100%, số tiền cần làm tròn 0,5 VND đúng chính sách SQL; UI hiển thị nguyên kết quả preview SQL, không tự tính lại. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T13-K04 | [I] Ngưỡng nhỏ nhất khác 0, ngưỡng trùng/âm hoặc % ngoài 0..100 bị chặn. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T13-K05 | [I] Sửa ngưỡng áp dụng tái tính khách nguyên tử; một lỗi rollback chính sách và hạng. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T13-K06 | [I] Cộng/trừ điểm thủ công ghi lịch sử; trừ vượt số dư rollback, lý do rỗng bị từ chối. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T13-K07 | [I] UPDATE/DELETE lịch sử bị chặn; MaHD sai khách/chưa trả và cộng mua hàng trùng bị chặn. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T13-K08 | [I] Không UPDATE DiemTichLuy hoặc MaHang trực tiếp qua màn hình chỉnh thông tin. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T13-K09 | [I] Chương trình đổi tỷ lệ sau khi chốt không đổi tiền giảm/snapshot đơn cũ. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T13-K10 | [U/M] CASH/STORE không sửa chính sách/điểm; quyền ADMIN đúng. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T13-K11 | [I] Hai điều chỉnh trừ đồng thời không cùng tiêu số dư; kết quả còn điểm không âm. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |

## Phép kiểm SQL bổ sung đã thực hiện

Các ID mở rộng là phép đo/subcase, không thêm hoặc thay 47 ID gốc. Expected/Actual dưới đây lấy nguyên dữ liệu đo. Chạy script PowerShell tương ứng trong thư mục evidence, bằng connection Windows tới DB đã ghi trong manifest; không chạy lại trên DB development. DB test đã dọn nên cần tạo fixture mới nếu chạy lại.

| Subcase | Expected | Actual | Status | Evidence |
|---|---|---|---|---|
| T13-K01-a | KM replaces tier; ship not discounted | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T13-K02-a | Outside KM date rejected | SQL error 52001; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T13-K03-a | No discount adds whole ship | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T13-K04-a | Missing zero threshold rejected | SQL error 52001; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T13-K06-a | Blank reason rejected | SQL error 52001; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T13-K07-a | Runtime history DML denied | SQL error 229; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T13-K08-a | Runtime points DML denied | SQL error 229; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T13-K10-a | CASH policy operation denied | SQL error 51605; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T13-K10-b | STORE KM read denied | SQL error 51605; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T13-K11-two | Second request blocks; then error 51603; final SQL assertions hold | Blocked at LCK_M_RS_U:56; SQL 51603; assertions and transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/concurrency-fault-results-v3.json); SQL/script cùng thư mục |
| T13-K05-fault | SQL 52990; rollback invariants checked separately | Assertions/error expectation completed | PASS | [JSON](../runs/acceptance-extended-20261018/fault-rollback-results.json); SQL/script cùng thư mục |
| T13-K05-invariants | All assertions hold; transaction 0:0 | Assertions/error expectation completed | PASS | [JSON](../runs/acceptance-extended-20261018/fault-rollback-results.json); SQL/script cùng thư mục |
| T13-K05-policy-customer-compare | Policy and all customer points/tiers unchanged | Compared ordered JSON before/after | PASS | [JSON](../runs/acceptance-extended-20261018/fault-rollback-results.json); SQL/script cùng thư mục |
| T13-K01-all-sources | None0, tier10, KM20, tier+KM20; ship excluded | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-boundary-policy-results.json); SQL/script cùng thư mục |
| T13-K02-endpoints | Both endpoints and same-day valid | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-boundary-policy-results.json); SQL/script cùng thư mục |
| T13-K02-before | Before start rejected | SQL error 52001; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-boundary-policy-results.json); SQL/script cùng thư mục |
| T13-K02-after | After end rejected | SQL error 52001; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-boundary-policy-results.json); SQL/script cùng thư mục |
| T13-K03-zero-full | 0/100 discounts, half-VND and ship | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-boundary-policy-results.json); SQL/script cùng thư mục |
| T13-K04-duplicate | Duplicate thresholds rejected | SQL error 52001; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-boundary-policy-results.json); SQL/script cùng thư mục |
| T13-K04-negative | Negative threshold rejected | SQL error 52001; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-boundary-policy-results.json); SQL/script cùng thư mục |
| T13-K04-percent | Percent101 rejected | SQL error 52001; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-boundary-policy-results.json); SQL/script cùng thư mục |
| T13-K06-overdraw | Subtract more than balance rejected | SQL error 52002; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-boundary-policy-results.json); SQL/script cùng thư mục |
| T13-K06-valid | Add/subtract history updates once and replay stable | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-boundary-policy-results.json); SQL/script cùng thư mục |
| T13-K07-history-delete | DELETE history rejected by trigger | SQL error 51011; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-boundary-policy-results.json); SQL/script cùng thư mục |
| T13-K07-unpaid | Unpaid order points rejected | SQL error 51012; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-boundary-policy-results.json); SQL/script cùng thư mục |
| T13-K07-duplicate | Purchase points duplicated rejected | SQL error 2601; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-boundary-policy-results.json); SQL/script cùng thư mục |
| T13-K09-snapshot | Changing referenced legacy KM preserves paid finance | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-boundary-policy-results.json); SQL/script cùng thư mục |
| T13-K05-reclassify | Replace policy reclassifies all customers atomically and preserves points | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-boundary-policy-results.json); SQL/script cùng thư mục |
| T13-policy-first-create | Second request blocks; then error 0; final SQL assertions hold | Blocked at LCK_M_S:66; SQL 0; assertions and transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/policy-race-results.json); SQL/script cùng thư mục |
| T13-create-first-policy | Second request blocks; then error 0; final SQL assertions hold | Blocked at LCK_M_S:66; SQL 0; assertions and transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/policy-race-results.json); SQL/script cùng thư mục |
| T13-policy-first-open | Second request blocks; then error 0; final SQL assertions hold | Blocked at LCK_M_S:66; SQL 0; assertions and transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/policy-race-results.json); SQL/script cùng thư mục |
| T13-open-first-policy | Second request blocks; then error 0; final SQL assertions hold | Blocked at LCK_M_S:66; SQL 0; assertions and transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/policy-race-results.json); SQL/script cùng thư mục |
| T13-policy-first-adjust | Second request blocks; then error 0; final SQL assertions hold | Blocked at LCK_M_S:66; SQL 0; assertions and transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/policy-race-results.json); SQL/script cùng thư mục |
| T13-adjust-first-policy | Second request blocks; then error 0; final SQL assertions hold | Blocked at LCK_M_S:66; SQL 0; assertions and transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/policy-race-results.json); SQL/script cùng thư mục |
| T13-policy-first-checkout | Second request blocks; then error 0; final SQL assertions hold | Blocked at LCK_M_S:66; SQL 0; assertions and transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/policy-race-results.json); SQL/script cùng thư mục |
| T13-checkout-first-policy | Second request blocks; then error 0; final SQL assertions hold | Blocked at LCK_M_S:66; SQL 0; assertions and transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/policy-race-results.json); SQL/script cùng thư mục |
| T13-K02-fk-customer | Unknown customer rejected | SQL error 52001; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T13-K02-fk-promotion | Unknown promotion rejected | SQL error 52001; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T13-K04-percent-negative | Negative percent rejected | SQL error 52001; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T13-K04-code-long | Overlength tier code rejected before cast | SQL error 52001; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T13-K04-points-overflow | Threshold exceeding int rejected before cast | SQL error 52001; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T13-K05-stale | Policy changes invalidate earlier preview | SQL error 51603; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T13-K07-nonexistent | Nonexistent order FK rejected | SQL error 547; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T13-K07-wrong-customer | Paid order with zero earned points still rejects wrong customer | SQL error 51012; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T13-K09-date-rate | Date, percentage and name changes preserve paid snapshot | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-final-results.json); SQL/script cùng thư mục |
| T13-K06-write-overdraw | Real adjust overdraw rejects and rolls back points/history/receipt | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-source-guards-results.json); SQL/script cùng thư mục |

## Giới hạn và phần còn lại

- Unit: bộ lọc G03/MaterialRulesTests 20/20 PASS, [TRX](../runs/acceptance-unit-20261018/g03-unit.trx). Đây là tổng của nhóm, không phải 20 ca cho từng task.
- BLOCKED: login/CRUD/selector bằng app thật, loading/rỗng/lỗi/xung đột/timeout, giữ input/chống gửi lặp, điều hướng100 lần/đăng xuất10 lần, Designer, kích thước1280×720/1366×768 và DPI100/125/150%, tab/focus/phím tắt. Những subcase manual trong plan chưa có thao tác thực tế; không suy PASS từ source/ảnh kỹ thuật.
- Công cụ Windows trả lỗi active request và ảnh không phải cửa sổ target sau reset. Dừng nhập ngay; chưa nhập mật khẩu hoặc bấm đăng nhập. App test đóng khi cleanup.
- Baseline checkout đã kiểm cạnh tranh công thức/policy; checkout/POS đích T14–T16 vẫn NOT RUN. Ca refresh SQL hai connection không thay kiểm chọn bàn ở giao diện.
- Log thử nghiệm harness đầu giữ nguyên: sql-results.json/concurrency-fault-results*.json có lỗi phép đo. Phiên bản hợp lệ là v2 SQL/v3 concurrency; transaction SELECT trực tiếp, context pooling, chuyển rowversion và lấy SPID được sửa ở script đo mới, không sửa source sản phẩm. Không tính lỗi harness thành lỗi sản phẩm hoặc xóa lịch sử.
