# Kiểm thử hỏi in/reset POS — 25/10/2026

Đợt test đã được người dùng duyệt riêng. Source208file đóng băng, không đổi
trong test. Đúng DLL Debug A8AD64781F337700FF9E60154CFB405892D8AF3A3B44ABD662DB5DABBA391BEA.
BuildDebug/Release sạch kế thừa đợt triển khai, không suyPASS chức năng từbuild.

## Phần đã thực hiện

| Phần | Actual | Kết quả |
|---|---|---|
| Reset/nội dung in component |Không còn nútXemIn; reset khi đangbusy xóa order/draft/header/product/line; paneltrống, caller vẫnbusy; kết thúc caller mở nút tạo nháp, khóa pay; nhãn in đầu/in lại và81000/ghi chú Việt đúng, không bịa100000/19000 |8/8 PASS |
| Guards trước commit |Bổ sung4ca gọi Checkout thật khi busy/unknown/nhápchưalưu/đơnpaid, giữ đơn/input và không mở modal/gọiDB |4/4 PASS; fixture12/12 bao gồm8ca trên, không cộng lặp |
| Checkout regression |Tiền/method/reference sai, bank/mixed/zero, busy/unknown, sharedcontrols/dispose vàfooter |32/32 PASS |
| SQL bridge |UI captures→SalesService trực tiếp:81000/100000, điểm+81 và kho, replay, mixed/bank/zero, competing checkout, reconcile, transaction0:0 |7/7 PASS; không là đường Confirm→hỏi in/reset |
| Source/DLL |208file không đổi; mọi fixture dùng đúngDLL |PASS audit |

Reset/guard là kiểm component trên control sản phẩm, không bấm Yes/No. Print
content là dữ liệu đầu vào document sản phẩm, chưa gửi máy in/kiểm từng trang.
Không mô tả các phần này là PASS nhánh native hoặc hình thức. Fixture32có kiểm
DeviceDpi120; reset/guard không hiển thị cả POS nên không chứng nhận layout/DPI POS.

## Tiến độ ca bổ sung

| Ca | Hiện trạng | Kết quả toàn ca |
|---|---|---|
| PC01 Không |Người dùng làm rõ chọnKhông trênHD20250419; UIstatus419đãthanhtoán/thối19000, panelChưatạođơn; SQL419paid81000/1PT001, tổngHD20khôngtăng |PASS |
| PC02 Có→hủy |ActualHD20250417: người dùng xác nhận; UIstatusĐãhủyin/panelnháp; SQLpaid81000/1PT001, tiêuhao/kho khớp |PASS |
| PC03 Có→in |HD20250421: userchốt/chọnCó, MicrosoftPrinttoPDF xuất1trang đúngmã/tiền/ghi chú, khôngnhãnINLẠI; POSnháp/Đãgửiin. SQL1PT00181000, KH011điểm0→81, tiêuhao/kho khớp |PASS PDF; không claim máy in giấy |
| PC04 Lỗi đọc/in sau commit |HD20250420: user xác nhận POS trống, Đã thanh toán / Chưa in được hóa đơn; SQL một PT00181000, kho giảm đúng snapshot. Đã gỡ DENY và kiểm runtime EXECUTE=1 |PASS lỗi đọc sau commit; chưa kiểm lỗi máy in vật lý |
| PC05 Trước commit |4guards và checkoutunknown/validation component đạt; hủy/unknown end-to-end chưa đủ |PARTIAL |
| PC06 Lịch sử |NútPOS/nhãn/nội dung component đạt; UI lịch sử→nativeprint/DB trước-sau chưa chạy |PARTIAL |
| PC07 Hồi quy |32component+7SQL đạt; tạo đơn tiếp ngay sau kết thúc native còn chờ |PARTIAL |

## Dữ liệu và an toàn tài nguyên

DB riêng CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout, schema016,
marker20261025_181739_98e5df59. Baseline/seed/migration001–016 được apply bằng
setup đã có; credential runtimeDPAPI chỉ đổi target trong process, không ghi secret.
Fixture actor tạo password ngẫu nhiên, hash và đăng nhập qua AuthService; tạo
bốn đơn qua SalesService, không là TEST login bypass/không sửa auth sản phẩm.

Đã chạy7SQLbridge trước khi tạo bốn đơn manual. Bốn đơn thuộc DB mới, không
nhầm HD20250411 đã trả trong DBmanual cũ. App bản mới đã mở bằng đăng nhập thường.
Lưu before-manual.json gồmorder/payment/kho/điểm trước người dùng thao tác.

Đang giữ DB này để kiểm native; lý do/hạn26/10/2026/cách dọn exactmarker ởmanifest
vàcleanup.ps1. Trước tạo có2DBtest đang giữ, sau tạo3; không dọnG03/DBmanual cũ
ngoài scope. Chưa dọn DBactive khi người dùng còn kiểm. Chưa gửi job in bởi agent.

Artifacts: [manifest](../runs/post-checkout-print-test-20261025/progress-manifest.json),
reset-results.json,guard-results.json,component-results.json,sql-results.json,
manual-cases.json,before-manual.json cùng thư mục run.

G04 vẫn34PASS/0FAIL/22BLOCKED. Không nâng cácID toàn phần từ bằng chứng partial.
Hình thức SKIPPED theo phân công người dùng, DPI100/150% NOT RUN. Source cố định;
nếu phát hiệnFAIL sản phẩm phải ghi và duyệt đợtbug trước sửa.

## Làm rõ mã ca thực tế

Người dùng xác nhận418lỡchọnCó,419chọnKhông. Ghi theo actual: PC01=419,
PC02=417. Không đánh FAIL sản phẩm chỉ vì dùng mã khác phân công ban đầu.
Ba đơn417/418/419 đềupaid81000, mỗi đơn đúng1PT001; tổngHDvẫn20, không tạo
billrỗng. Tổngtiêuhao/kho NVL01−0.054 vàNVL03−0.030 khớp snapshot của3đơn.
Tổng snapshot được cộng decimal, không dùng Measure-Object double để kết luận
chênh lệch0.05399999999999999 là bug. Source208file không đổi.

before-manual.json bị cắt2033ký tự do ExecuteScalar trực tiếp FORJSON; giữ file
gốc lỗi. TotalOrders20 và đầy đủ entriesNVL01/NVL03 còn trong file, đủ đối chiếu
hai nguyên liệu thực dùng; không khẳng định baselineđủmọinguyênliệu/điểm.
Đọc JSON sau bằng SELECT(scalar FORJSON) đầy đủ. Verdictgộp2paid=false trước đó
là418chưachốt ở lầnđọc trước, không phải bug; không sửa expected để né khác biệt.
Evidence: remapped-cases-verdict.json, after-user-remapped-cases.json,
after-no-419-ui.json và after-user-no-cancel-ui.json ở runfolder.

Người dùng đang yêu cầu thiết kế lại Đơn đang mở theo ZIP; chưa sửa source.
Giữ mốc đóng băng và binary/testDB cho phần còn lại, không suy toànG04PASS.
