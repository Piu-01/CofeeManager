# G04-UI01 — Regression sau sửa25/10/2026

Bổ sung thao tác người dùng: ảnh sau Áp dụng cóHD20250411, app báo đã ghi/tảiSQL.
Agent truy vấn chỉ đọc đúngDB/marker: Mang di,chưa thanh toán,1dòngSP001×1,
giá81000,ghi chú `G04 ít đá, không đường.`,khách/bànNULL,giảm0,ship0,
0payment,ngàyTTNULL. [Evidence](../runs/header-fix-20261025/manual-order-HD20250411.json).
Nhánh tạo món đầu Mang đi qua GUI sau sửa đã đạt; chưa suy hoàn tất3kênh/restart.

Người dùng đã duyệt sửa G04-UI01 và regression cả3kênh. Chỉ sửa
OrderHeaderDialog.cs: bỏ tải/binding partner cho kênh không giao; hiển thị trạng
thái loading/lỗi; lỗi tìm khách lúc khởi tạo được chuyển lên handler tải, không
nuốt lỗi rồi bật form; Save bị chặn cho đến ready. Không sửa BLL/SQL/role.

Môi trường: .NET10WinForms thật, DPI120=125%, localhost SQL Server, DB riêng
CoffeeManager_Test_G04_20261024_152759_12204643_manual, schema016, source chưa commit.
BusinessDate fixture01/04/2025 để có KM25ONE hợp lệ; customerKH001, bànB0001,
đối tácDT001. Không suy kiểm chính sách theo ngày hiện tại từ fixture ngày lịch sử.

Test tạo một account/employee CASH NHF01, mật khẩu ngẫu nhiên chỉ trong bộ nhớ,
đăng nhập qua AuthService/runtime login thật rồi dựng OrderHeaderDialog bằng
reflection vì class internal. Form thật được show; gọi Save handler trực tiếp,
đóng form bằng Close để kiểm vòng đời; không giả là đã click chuột/ấn phím.
Không chạy handler AddProduct/CreateAsync của POS hoặc checkout trong bộ này.

| Ca regression / liên quan | Expected và actual | Kết quả |
|---|---|---|
| H01/T16-K01 Mang đi | Ready/bật Áp dụng; vãng lai/khôngKM→header đúng, partner không được bind | PASS |
| H02/T16-K01 Tại quán | Ready/lưu header giữ bànB0001 | PASS |
| H03/T18-K01 Giao hàng tự giao | Header trả người nhận/SDT/địa chỉ/ship10000,Partner=null | PASS |
| H04/T18-K01 Giao đối tác | Header giữDT001,người nhận vàship | PASS |
| H05 Khách/KM | Mang đi giữKH001/KM25ONE, không mất dữ liệu chọn ban đầu | PASS |
| H06 Quay lại/đóng | Ready,back enabled;đóng không trảResult | PASS componentClose |
| H07 Lỗi lookup | Customer repository nối catalog test không tồn tại; báoKhông tải được…,Save disabled/back enabled; gọiSave không trảResult | PASS |
| H08 Mở lại | Form mới với kết nối đúng trở vềReady/Apply được sau ca lỗi | PASS |
| H09 Đóng đang tải | Đóng trong lúc tải,Result=null,lifetime hủy,không exception ra ngoài | PASS |
| H10/UX-K21 | Giao hàng thiếu người nhận,Save không đóng/chưaResult,thông báo cần thông tin | PASS |
| H11 Không ghi dở | Tổng sốHOADON trước/sau không đổi | PASS |
| H12 Dọn fixture |0employeeNHF01 còn lại sau lượt kiểm | PASS |
| H13 Transaction |Owner@@TRANCOUNT:XACT_STATE=0:0 | PASS |

13/13PASS. [Raw results](../runs/header-fix-20261025/results.json),
[source audit](../runs/header-fix-20261025/source-audit.json):203file sản phẩm,
0file đổi trong lượt kiểm. ProbeFAIL trước sửa giữ tại
`../runs/header-investigation-20261025/`.

Debug/Release build sạch0warning/0error, logs ở `../runs/header-fix-20261025/`.
Launcher manual chọn bảnDebug mới, vẫn đăng nhập thường. Không đóng hay ép dừng
process cũ có nháp của người dùng. DB test giữ cho phiên G04 manual, không đổi
development/account người dùng hoặc tạo SQLserver login.

Mouse/keyboard trên app hoàn chỉnh và tạo/mở lại3kênh sau restart vẫn chưa kiểm
đủ; T16-K01 trở vềBLOCKED chờ hoàn tất các nhánh đó, không ghiPASS toàn ca từ13
kiểm tra này. G04 hiện32PASS/0FAIL/24BLOCKED; lịch sử32/1/23 tại thời điểm phát
hiện lỗi được giữ. Hình thứcSKIPPED;100/150%NOT RUN,không đổiWindowsScale.
