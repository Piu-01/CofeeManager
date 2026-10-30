# Regression G05-B05 — 29/10/2026

Người dùng đã duyệt B05 kèm regression; xem approval.md. Chỉ sửa OrbitDialogLayout.EnableResponsiveActions và phép đo footer khi opt-in. Không đổi nghiệp vụ/SQL. Freeze: source-fixed.csv → source-freeze-check.json, SourceUnchanged=true.

Môi trường Windows, .NET10 WinForms, DeviceDpi120 (125%), SQL Server localhost, DB riêng CoffeeManager_Test_G05_20261029_182915_f5617a84_demo, schema001–021. Không đổi Windows Scale. Không nghiệm thu hình thức;100/150% NOT RUN.

| Ca/phạm vi | Expected / actual | Kết quả / bằng chứng |
|---|---|---|
| B05 helper:1/3/7 nút, leading, nút động/nhãn dài, resize640×420/1280×720/1366×768, legacy chưa opt-in | Nút nằm trọn ancestor client, không co rộng một nút/cắt hàng wrap | 28/28 PASS, matrix-results.json; matrix/Program.cs |
| B05 chín dialog G05 thật: loaded/resize và bật nút reconcile ở5 editor | Tất cả nút visible tiếp cận được; AcceptButton=null; DPI120 | 23/23 PASS, dialogs-final-results.json;23 checkpoint InsideWorkingArea=true. Đây là component geometry, không là thao tác native đầy đủ |
| T19/T20/T21/T22/T23 unit/SQL subset | Quy tắc hiện có, migration clean/demo, replay |22/22 xUnit PASS,5 SQL subcase PASS; ../acceptance_20261029_182915_f5617a84/manifest.json và unit.log |
| Build | Debug/Release không lỗi | Debug qua project probe/dialogs/matrix; Release0warning/0error tại release-build.log |
| T20-K01/K11/K12 nhánh bỏ nháp | ADMIN thường → Kho → Lập phiếu → thêm NVL12 → Quay lại → Không giữ dòng → Quay lại → Có về danh sách, không ghi kho/nợ | PASS nhánh bỏ nháp; native-discard-confirm.txt/no.txt/yes.txt; native-write-check.json xác nhận kho/PN/nợ không đổi |
| T20 luồng preview→xác nhận→chốt | Nhập lượng1, giá10000 và chốt một lần | BLOCKED bởi B06: click/double click ô lượng không mở editor, giá trị vẫn0; Tính tổng báo lượng/giá phải dương. native-purchase-validation.txt. Không seed giá trị vào editor để né lỗi |
| T19-K01/K05/K10 nhánh ghi native | Phiếu manual Chi2345, ghi chú B05-native-20261029; xác nhận có owner; đóng editor và reload đúng một phiếu | PASS nhánh này: TC20250508,19→20 phiếu toàn DB,0→1 receipt actor9001001; tổng danh sách6234→8579. native-finance-confirm.txt/after.txt, native-write-check.json; không thay tổng55ID |
| Owner/đóng native | MainForm → editor → confirmation, Không giữ nháp; Có đóng đúng editor, trả cha | PASS nhánh bỏ nháp và ghi thu chi nói trên. Chưa suy PASS mọi guard busy/unknown/session/phím/print-file |
| Cleanup | Không giữ DB/credential test | resource-audit.json: tạo2/dọn2/còn0; GUI23988 đã thoát, thư mục DPAPI đúng marker đã xóa. Không thao tác development |

Harness: các bản thử trước bị sai owner, dispose khi modal còn chạy, đóng không hoàn tất và một lần truyền marker sai; giữ log/v1–v5 và bản Program.v*.txt. Bản cuối dùng MainForm đúng owner, đo geometry, teardown bằng Abort nội bộ; do đó kết quả23 checkpoint không được dùng làm bằng chứng native CanClose. Không sửa product để né lỗi harness. CUA có một lần mất cache AX; lấy lại cùng cửa sổ đang chạy rồi tiếp tục được. gui-error.log rỗng, app đóng bình thường sau test.

B05 đã khắc phục clipping theo phạm vi kiểm. G05 vẫn Đang làm: B06 mới chờ duyệt và các nhánh nghiệm thu55ID/UX còn thiếu ở report.md. Lịch sử27/10 không thay đổi.
