# Kết quả nghiệm thu — T12 — 18/10/2026

**ManualM35 ngày20/10 — PASS xóakháchchưanguồn(T12-K07):** KH012đãxóa, giữCreate/DeleteReceipt; KH011đúngtên/300/BACkhôngđổi. [Evidence](../runs/manual-g03-20261019/M35-db.json). M34đãkiểmriêngchặnKH011cómốc; cáccaFKkhác/role/GUIchungvẫntheohồsơchưađủ.

**ManualM34 ngày20/10 — PASS chặnxóakháchcómốc(T12-K07):** GUItừchốiràngbuộcchung; [SQL](../runs/manual-g03-20261019/M34-db.json) KH011vẫncòn/300/BAC/mốc1/receipt4, khôngghi mới. ThôngbáoUXchi tiếtvẫntạmgác, khôngcoiFAILđộngcơdữliệu.

**ManualM33 ngày20/10 — PASS sửaprofilekhicóđiểm(T12-K01):** KH011tênKhách kiểm thử sau mở sổ, SDTgiữ, điểm300/BAC/mốcManual300giữ, receiptUpdate1lần(tổng4). [Evidence](../runs/manual-g03-20261019/M33-db.json). KhôngsuyPASSquyềnCASH/STOREtừphiênADMIN.

**ManualM32 ngày20/10 — PASS chặnmởsổlại(T12-K05):** GUIbáokháchđãcómốchoặcgiaodịch/giữinput; [SQL](../runs/manual-g03-20261019/M32-db.json) vẫn300/BAC/mốc1/receipt3, khôngghi thêm.

**ManualM31 ngày20/10 — PASS mởsổ300/hạngBAC:** ảnhGUI300/BAC/3khớp[SQL](../runs/manual-g03-20261019/M31-db.json), mốcManual1/nguồnđúng/Actor1/receipt3. Nhánhngưỡng300T12-K04vàmởsổT12-K05 cóevidence; chưakếtluậnmọicabắtbuộcqua1mốc.

**ManualM30 ngày20/10 — PASS trùngSDTthật(T12-K02):** GUIKH012từchối0900000981vàgiữinput; [SQL](../runs/manual-g03-20261019/M30-db.json) KH012vẫnNULL/receipt1, KH011giữSDT/receipt2, điểm/hạngđềukhôngđổi. HaiNULLvà0đầuđãcóM28/M29riêng; giữhồsơchưahoàntấtcáccachung.

**ManualM29 ngày20/10 — PASS profile/0đầu:** KH011sửađúngtên/SDT0900000981, giữđiểm0/DONG, receiptUpdate1lần; KH012NULLkhôngđổi. [Evidence](../runs/manual-g03-20261019/M29-db.json). KhôngsuyPASSmọiquyềnGUI/tạikháccóđiểm chỉtừca0điểm.

**ManualM27/M28 ngày20/10 — PASS createđiểm0/hạng0 và2NULL:** KH011/Khách kiểm thử GUI, KH012/Khách kiểm thử không SDT 2, mãkhác/SDTNULL/điểm0/DONG/receipt1mỗikhách. [Evidence](../runs/manual-g03-20261019/M28-db.json), ảnhM27vàxácnhậnM28ngườidùng. ChưađóngmọisubcaseT12-K01/K02(profileupdate/trùngSDTthật cònkiểmtiếp).

Đợt test đã được người dùng duyệt. Trạng thái task: **Đang làm**; còn BLOCKED giao diện. Source giữ cố định; chưa debug. Các dòng PASS dưới đây chỉ nghiệm thu phạm vi ghi rõ, không đóng task hoặc bỏ regression G04.

Môi trường: Windows, .NET10, SQL Server localhost; DB test riêng clean/demo baseline→009. Snapshot 139 file sản phẩm/SQL/test không đổi, xem [audit](../runs/acceptance-extended-20261018/source-audit-final.json). Schema/source, input và câu SQL chính xác ở từng script/SQL fixture cạnh JSON. Tài khoản runtime WITHOUT LOGIN/CafeManagerApp cho ca đánh dấu RuntimePrincipal; các ca ADMIN khác đặt session role trên connection operator. Không coi kiểm SQL là bằng chứng BLL/native UI.

| ID | Expected gốc | Actual | Trạng thái | Evidence |
|---|---|---|---|---|
| T12-K01 | [I/M] Thêm khách điểm 0 nhận hạng ngưỡng 0; chỉnh tên/SDT không đổi điểm/hạng. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T12-K02 | [I] SDT rỗng thành NULL và nhiều khách NULL hợp lệ; SDT thực trùng bị chặn. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T12-K03 | [I/M] Tên trống/quá dài và điểm mở sổ âm bị chặn; không dùng tên khách làm khóa. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T12-K04 | [I] Ngưỡng 0, ngay dưới/bằng/trên ngưỡng chọn đúng hạng. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T12-K05 | [I] Điểm mở sổ có nguồn và chỉ khởi tạo một lần; không reset khách có lịch sử. | Các kiểm SQL liên quan bên dưới PASS; riêng tích hợp checkout đích G04 chưa chạy. | PASS | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T12-K06 | [M] Tìm SDT nhanh, chọn đúng MaKH; khách vãng lai không cần tạo khách giả. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T12-K07 | [I/M] Xóa khách đã có hóa đơn/lịch sử điểm bị chặn, không mất lịch sử bán. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T12-K08 | [U/M] CASH thêm/tìm khách được nhưng không sửa số dư/hạng tùy ý. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |
| T12-K09 | [M] Danh sách/chi tiết khách reload đúng điểm và hạng từ DB, không tự tính từ cache. | Phần SQL liên quan có kết quả bên dưới; thao tác native UI/Designer/DPI/bàn phím chưa thực hiện được. Tích hợp checkout đích G04 NOT RUN nếu có. | BLOCKED | JSON/SQL chi tiết bên dưới; [báo cáo nhóm](../report.md) |

## Phép kiểm SQL bổ sung đã thực hiện

Các ID mở rộng là phép đo/subcase, không thêm hoặc thay 47 ID gốc. Expected/Actual dưới đây lấy nguyên dữ liệu đo. Chạy script PowerShell tương ứng trong thư mục evidence, bằng connection Windows tới DB đã ghi trong manifest; không chạy lại trên DB development. DB test đã dọn nên cần tạo fixture mới nếu chạy lại.

| Subcase | Expected | Actual | Status | Evidence |
|---|---|---|---|---|
| T12-K01-a | Customer zero gets threshold zero | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T12-K02-a | Multiple NULL phones accepted | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T12-K02-b | Duplicate phone denied | SQL error 2601; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T12-K03-a | Overlength name rejected | SQL error 51901; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T12-K08-a | CASH opening denied | SQL error 51605; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T12-K08-b | STORE customer read denied | SQL error 51605; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-results-v2.json); SQL/script cùng thư mục |
| T12-K05-fault | SQL 52990; rollback invariants checked separately | Assertions/error expectation completed | PASS | [JSON](../runs/acceptance-extended-20261018/fault-rollback-results.json); SQL/script cùng thư mục |
| T12-K05-invariants | All assertions hold; transaction 0:0 | Assertions/error expectation completed | PASS | [JSON](../runs/acceptance-extended-20261018/fault-rollback-results.json); SQL/script cùng thư mục |
| T12-K04-boundaries | 299/300/301/799/800/801 choose thresholds 0/300/300/300/800/800 | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-boundary-policy-results.json); SQL/script cùng thư mục |
| T12-K05-repeat-legacy | Existing legacy customer cannot reopen points | SQL error 51902; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-history-results.json); SQL/script cùng thư mục |
| T12-K07-history | Existing customer source prevents deletion | SQL error 547; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-history-results.json); SQL/script cùng thư mục |
| T12-K01-profile | Profile changes preserve points and tier | Assertions completed; transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/sql-history-results.json); SQL/script cùng thư mục |
| T12-preflight | Missing zero legacy tier rejects007; does not change policy | Invariant=1 | PASS | [JSON](../runs/acceptance-extended-20261018/migration-results.json); SQL/script cùng thư mục |
| T12-K05-two | Second request blocks; then error 51603; final SQL assertions hold | Blocked at LCK_M_RS_U:67; SQL 51603; assertions and transaction 0:0 | PASS | [JSON](../runs/acceptance-extended-20261018/customer-race-results.json); SQL/script cùng thư mục |

## Giới hạn và phần còn lại

- Unit: bộ lọc G03/MaterialRulesTests 20/20 PASS, [TRX](../runs/acceptance-unit-20261018/g03-unit.trx). Đây là tổng của nhóm, không phải 20 ca cho từng task.
- BLOCKED: login/CRUD/selector bằng app thật, loading/rỗng/lỗi/xung đột/timeout, giữ input/chống gửi lặp, điều hướng100 lần/đăng xuất10 lần, Designer, kích thước1280×720/1366×768 và DPI100/125/150%, tab/focus/phím tắt. Những subcase manual trong plan chưa có thao tác thực tế; không suy PASS từ source/ảnh kỹ thuật.
- Công cụ Windows trả lỗi active request và ảnh không phải cửa sổ target sau reset. Dừng nhập ngay; chưa nhập mật khẩu hoặc bấm đăng nhập. App test đóng khi cleanup.
- Baseline checkout đã kiểm cạnh tranh công thức/policy; checkout/POS đích T14–T16 vẫn NOT RUN. Ca refresh SQL hai connection không thay kiểm chọn bàn ở giao diện.
- Log thử nghiệm harness đầu giữ nguyên: sql-results.json/concurrency-fault-results*.json có lỗi phép đo. Phiên bản hợp lệ là v2 SQL/v3 concurrency; transaction SELECT trực tiếp, context pooling, chuyển rowversion và lấy SPID được sửa ở script đo mới, không sửa source sản phẩm. Không tính lỗi harness thành lỗi sản phẩm hoặc xóa lịch sử.
