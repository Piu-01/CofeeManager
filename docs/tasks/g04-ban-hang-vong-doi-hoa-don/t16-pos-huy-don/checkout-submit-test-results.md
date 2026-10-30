# G04-UI05 và SelectAll — 25/10/2026

## Kết quả cuối sau người dùng chốt thật

Người dùng xác nhận: “Đã chốt, không lỗi, thối19.000 ₫”. Agent đọcSQL cùng DB
manual: HD20250411 Da thanh toan, payable81000, đúng một PT001=81000; khoNVL01
96.740→96.722 (−0.018), NVL03 72.705→72.695 (−0.010), khớp hai snapshot tiêu hao.
Không có khách hàng liên kết nên không dùng ca này chứng nhận điểm khách hàng.
Tiền đưa100000/thối19000 do người dùng quan sát; DB không lưu, không suy từ SQL.
Source207file không đổi. T16-K03 PASS; UI05 hết lỗi trong đường đã tái hiện và
handler Confirm thật đã kiểm. G0434PASS/0FAIL/22BLOCKED, vẫn Đang làm.

Evidence: [SQL verdict](../runs/checkout-submit-fix-20261025/manual-checkout-verdict.json),
[snapshot](../runs/checkout-submit-fix-20261025/manual-checkout-verified.json).
Các chữ PENDING/FAIL đang giữ bên dưới là lịch sử trước phản hồi người dùng;
không xóa lỗi cũ hoặc mở rộng thành PASS các nhánh khác.

Người dùng đã duyệt cả đợt sửa/debug. Triển khai trước, đóng băng source207file
rồi regression. Không đổi SQL/DAL/BLL/role, không gửi checkout bằngSQL để thayGUI.

## Thay đổi

- StarDustOverlay không đặt Enabled=false trước Show; giữ ShowWithoutActivation,
  WS_EX_NOACTIVATE/TRANSPARENT và hit-test xuyên chuột. Lỗi display giới hạn ở
  InvalidOperationException/Win32Exception được dọn hiệu ứng và ghi Trace warning,
  không thoát sang luồng tài chính hoặc tự gửi lại.
- CheckoutDialog đặt ShowState/Burst bên trong try/finally hiện có. Giữ nguyên
  RequestId/token, xác nhận, validate, CheckoutAsync, unknown/reconcile/commit.
- OrbitField.SelectAllOnEntry là helper opt-in, chỉ nối vào tendered. Enter chọn
  hết, mouse-up lần đầu chọn lại nếu native mouse-down đặt caret, click tiếp cho
  đặt caret; KeyDown/Leave kết thúc trạng thái click đầu. Không gán lại Text.

## Kiểm chứng trên source cố định

.NET10 WinForms thật, Darkmode, DeviceDpi120=125%; source chưa commit. Chỉ ba file
CheckoutDialog.cs/OrbitField.cs/StarDustOverlay.cs đổi so UI04.

| Kiểm tra | Actual | Kết quả |
|---|---|---|
| BuildDebug/Release |0 warning/0 error | PASS |
| Repro lỗi UI05 cũ chạy bằng DLL mới |OverlayEnabled=true, Error=null | PASS |
| Fixture component cũ |Nhập sai/thiếu/tràn, phân bổ, mixed/bank/0₫, busy/unknown, dispose, shared controls |32/32 PASS |
| Layout/binding cũ |Bounds đầy đủ, resize/scroll/nhiều dòng, mapping PT002 Darkmode125% |60/60 PASS |
| Effect/SelectAll |Focus chọn hết; mouse-up đầu chọn lại, click sau giữ caret; nhập đè100000/thối19000; Leave-format; Show không lấy focus; native flags; timer tự ẩn; fault display được dọn; lần Burst sau hoạt động |11/11 PASS |
| Handler Confirm thật + SQL |Đã mở bản mới đăng nhập thường vào DB manual, gửi hướng dẫn chốt cho người dùng |PENDING; không suyPASS từ32/60/11 |
| Source/binary |207file không đổi trong regression; cùng DLL frozen |PASS audit |
| Hình thức, DPI100/150% |Người dùng tự kiểm; không đổi Scale |SKIPPED/NOT RUN |
| Click/Tab vật lý trong modal |Helper trước đó không nhập/click được modal; test SelectAll nêu trên dùng Focus và gọi handler MouseUp tổng hợp |Chờ người dùng xác nhận |

Fixture effect đầu tiên9/11: fault injection disable owner thay vì overlay, nên
không tạo được lỗi Show mong đợi và làm mất focus owner; chưa chứng minh bug sản
phẩm mới. Giữ Program/log/results nguyên tại runs/checkout-submit-fix-20261025/.
Fixture follow-up tại runs/checkout-effect-regression-20261025/ chỉ đổi đối tượng
fault thành overlay.Disabled giống nguyên nhân đã tái hiện; expected không đổi,
source/DLL sản phẩm không đổi,11/11 PASS. Không xóa9/11 hoặc gọi nó là PASS.

Artifacts: [repro sau sửa](../runs/checkout-submit-fix-20261025/reproduction-after.json),
[32ca](../runs/checkout-submit-fix-20261025/component-results.json),
[60ca](../runs/checkout-submit-fix-20261025/layout-results.json),
[11ca](../runs/checkout-effect-regression-20261025/results.json),
[audit](../runs/checkout-submit-fix-20261025/progress-manifest.json).

T16-K03 hiện vẫnFAIL trước sửa cho tới khi đường bấm xác nhận thật được kiểm
và snapshot SQL chứng minh. G04 giữ33PASS/1FAIL/22BLOCKED; không nghiệm thu toàn
G04 hay mở rộng kết quả sang các nhánh POS chưa chạy.
