# Tiếp tục G04 sau chỉnh pager — 25/10/2026

Phản hồi người dùng sau lượt này: “Đúng51đơn, trangcuối1dòng, mởđúngHD20250421”.
Chuỗi native tìm/paging/mở đã được xácnhận, khôngcòn chờ nhánh này. In418người
dùng nói chưa biết vìkhôngcómáyin; khôngtựghiPASSin. Agentđọc printers cài sẵn
thấy MicrosoftPrinttoPDF, nên chuẩn bị kiểm PC03bằngPDF màkhôngcầnmáyingiấy.
Các dòng chờnative ởdưới giữlịchsử trước phản hồi này.

Người dùng yêu cầu “ok test tiếp nè”. Đóng băng208sourcefile trước test, source
không đổi. Dùng đúng compact-pager DLL
51FDC28FD3F600D1621997F7E462FE36F26427ECD04002ECE5005F99E6AE72C2.

Đã chạy lại59ca form/SQL củađợtOpenOrders trên binary mới. Tái dùng51đơn mở và
kháchKH011củaDBtestcómarker, không tạo51đơn nữa/không tạo DBthứ4. TạoactorNPG02
chỉtrongDBtest, đăng nhậpAuthService thật; không là evidence loginUIngười dùng.
59/59PASS, gồm fullcode/bounds, bốnsize, paging50/1, tìm/rỗng, readfault và
controlledresponseinversion. Snapshotpayment/tiêuhao/kho/pointshistorykhôngđổi.
Expected giữ nguyên. Harness mới chỉ đổi fixture provisioning sang tái dùng data.

## UI người dùng đang mở

Đúng executable compactpager, MainWindow11537712, CASHđã đăng nhập. Agent đọc
khôngscreenshot: modalĐơnđangmở có53đơnmở tổng,1/2, currentHD20250471.
set_value ởsearch thật nhận “OO kiểm thử20261025”; chưa đồngnghĩa đã tìm.
PressReturn nhắmMainWindowkhông làm rõ được tìm kiếm: capture kế tiếp mất
accessibility, đãrefreshbinding; tree cóhai ownedmodal Đơnđangmở và vẫn53đơn.
Không gửi tiếp phím vào root, không ghi PASSnativeEnter/tìm hoặc mở đơn.
Điều này được ghi là lỗi/giới hạn điều khiển target modal củahelper, chưa đủ
chứng minh lỗi nhập phím bình thường của sản phẩm. Không sửa source trong test.

Đã yêu cầu người dùng EscvềPOS→F4mộtlần→tìmnhómOO51đơn→trangcuối1dòng→mở
HD20250421. Đồngthời hỏi kết quả printthậtHD20250418 (đãchọnCótrướcđó), không
giả đãin hoặc tựgửi lại job/checkout. PC04faultsaucommit vẫnNOTRUN, không nâng
G04 chỉ từ59subset. G04giữ34PASS/0FAIL/22BLOCKED.

Evidence: results.json,fixture.json,native-before.json,native-search-set.json,
native-after-enter.json,native-refreshed.json,source-frozen.csv vàfinal-manifest.
HìnhthứcSKIPPEDchoagent, DPI100/150%NOT RUN; formtestDeviceDpi120 thật.
