# G05 native follow-up29–30/10/2026

Source giữ cố định theo source-fixed.csv/source-freeze-check.json; không sửa product/migration trong lượt test. Windows WinForms Debug net10.0-windows, runtime10.0.13, DPI125%; SQL Server localhost, baseline+seed+migrations001–021, DB/marker trong manifest.json. Tài khoản fixture đăng nhập thường; không dùng bỏ qua đăng nhập. Các mục dưới là subcase bổ sung, không thay55ID lịch sử27/10 hoặc suy PASS cả ID.

| Ca/phạm vi | Thao tác, expected/actual | Kết quả / bằng chứng |
|---|---|---|
|T20 native STORE|Nhập NVL12 qty3×7000, trả ngay bị khóa0; preview21000, nợ21000; lưu một PN đúng NV992, không phiếu chi.|PASS; store-purchase-open/preview/confirm/after.txt, store-purchase-sql.json. PN20250502/CN20250405.|
|T20/T23 STORE đọc nguồn|PN chỉ đọc → nợ chưa trả21000, lịch sử rỗng; không nút trả/phiếu chi; đóng về đúng hộp cha.|PASS; store-purchase-detail.txt, store-debt-readonly.txt.|
|T21 native STORE trả hàng|NVL12 qty2; xác nhận không hoàn tiền/cấn nợ. PX20250504 loại Tra hang, NV992; tồn29+3−2=30, nợ vẫn21000, finance giữ16.|PASS; store-return-confirm/after.txt, store-after-sql.json.|
|T23 ADMIN trả một phần|Trả10000 từ nợ21000, xác nhận và reload còn11000, trạng thái trả một phần.|PASS; admin-debt-confirm.txt, admin-debt-partial-after.txt.|
|T23 ADMIN trả hết|Trả11000 còn lại, reload đã trả21000/còn0; hai lần trả, không nhân đôi.|PASS; admin-debt-final-confirm/after.txt, admin-debt-final-sql.json.|
|T23 lịch sử/link chi|Lịch sử hai dòng10000/11000; mở lần lượt TC20250505/TC20250506 chỉ đọc, đúng SupplierDebt/CN20250405, owner lồng và đóng về cha.|PASS; admin-debt-history.txt, admin-debt-finance-link1/2.txt.|
|Giữ nháp trả nợ|Một thao tác AX stale chọn Quay lại thay Trả; chọn Không ở xác nhận bỏ nháp, giá trị11000 giữ nguyên.|PASS theo thao tác thực tế; admin-debt-draft-retained.txt. Không coi thao tác stale là bug sản phẩm.|
|T19-K02.Name51|Runtime principal gọi public procedure tên danh mục51 ký tự; expected/actual51501, dữ liệu không đổi, transaction0:0.|PASS; boundary-results.json, verify.ps1.|
|T20-K02.ActorMissing|Runtime principal gọi nhập với actor không tồn tại; expected/actual51505, dữ liệu không đổi, transaction0:0.|PASS; boundary-results.json, verify.ps1.|
|Đối chiếu cuối|PN+1/PX+1/nợ+1/finance+2/receipt4; tồn chỉ NVL12+1; hai phiếu chi đúng ADMIN NV991, tổng21000, nợ0.|PASS; native-final-check.json.|
|Đăng xuất từ Công nợ|Expected về Login không lỗi; actual hộp Unhandled ObjectDisposedException CTS tại SupplierDebtsControl.cs:21.|FAIL mới B07; logout-exception.txt, logout-exception-details.txt. Chưa sửa.|
|CASH native đăng nhập sau ADMIN|Không tiếp tục bằng nút Continue bỏ qua exception.|BLOCKED bởi B07; quyền BLL từ đợt trước không thay native này.|
|Native Tab/select-all đầy đủ|CUA không thiết lập ổn định focus/owner; chưa có bằng chứng đầy đủ.|BLOCKED công cụ; không suy PASS từ component54/54 đợt B06.|

Instrumentation: bản đo đầu dùng SELECT CONCAT(@@TRANCOUNT, XACT_STATE()) cho telemetry0:1 trong ngữ cảnh câu SELECT, trong khi helper đã assert transaction0:0. Giữ verify.v1.ps1/boundary-results.v1.json; bản hiện tại chụp hai biến trước SELECT cho0:0. Chỉ sửa phép đo, chạy lại hai boundary, không sửa source sản phẩm/expected.

Không chạy lại unit/build đã PASS B06 vì source không đổi. Không nghiệm thu hình thức (SKIPPED theo phân công); DPI100/150 NOT RUN. Chưa chạy đầy đủ các nhánh UX/keyboard còn lại, G05 Đang làm. Dọn đúng1DB/1DB, còn0, process test thoát, thư mục DPAPI xóa theo marker; resource-audit.json. Development không bị ghi fixture. Cleanup đầu bị sandbox chặn kết nối, lần có quyền truy cập localhost thành công. Không commit.
