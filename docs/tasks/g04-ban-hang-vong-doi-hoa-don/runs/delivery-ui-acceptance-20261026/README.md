# Bằng chứng đợt test UI Giao hàng26/10/2026

User duyệt riêng bằng “đuyệt”. Product source142file cố định theo source-frozen.csv;
source-verification.json kiểm source và DLL trùng binary triển khai. Harness
nằm ngoài src, không chỉnh product source/migration hoặc expected trong lượt.

## Fixture và quyền sở hữu

Chỉ localhost/CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout,
G04TestMarker.RunId20261025_181739_98e5df59, schema016. Không tạo DB mới hoặc
ghi vào development. Fixture collision guard chạy trước provisioning.

- DG26000001–DG26000101:101 header giao hàng mở, đầy đủ người nhận/SDT/địa chỉ,
  không dòng món; chỉ dùng list/pager. SDT prefix098762 tách tập7mốc đếm mà không
  đổi tổng thật hoặc làm giả UI.0/1/49/50/51/100/101 là số dòng query SQL thực.
  Mã DG không qua SalesService, không dùng để kiểm Create/checkout/transition.
- HD96260001/HD96260002: tạo bằng AuthService/SalesService thực, SP001×1, người
  nhận/địa chỉ hợp lệ; chuyển bước qua UI/service, không thanh toán. Số tiền
  chính thức do SQL trả; không đặt/tự sửa tổng tiền trong fixture.
- Actor DGA01/DGC01/DGS01 có mật khẩu random/hash, không lưu mật khẩu. Runtime
  principal từ profile local chỉ được đổi target trong môi trường process.
- finally dọn đúng receipts actor/code của2đơn thực, dòng món,103headers và3actor;
  so before/after toàn orders/lines/payments/consumption/discounts/stock/points/
  receipts. Không reset identity/sequence hoặc sửa các đơn476–478 đang giữ.
- Regression dialog/shared lists dùng NNA01/NNC01/NNS01 rồi tự dọn; regression
  Invoice dùng IH26headers/IV426/IHA01/IHC01/IHS01 rồi tự dọn theo runner đã lưu.
  Các runner chạy tuần tự để snapshot không lẫn provisioning của lượt khác.

## Phạm vi bằng chứng và giới hạn

Program.cs:WinForms thật/UI thread125%, PerformClick/focus/OnKeyDown và gọi
handler Detail/Reconcile thực; Auth/runtimeSQL thật. Không mô phỏng kết quả SQL
cho search/filter/pager hoặc chuyển giao. Chỉ late/error/cancel/fallback dùng
delegate query seam trên instance test; không gây fault procedure/permission DB.

HD96260001: hủy xác nhận không chuyển; đồng ý hai lần theo hai bước riêng
Chờ→Đang→Đã, vẫn Chưa thanh toán. HD96260002: hai authenticated calls dùng hai
connection SQL, cùng revision, một OK/một52103. Sau đó đặt unknown/requestId/
pendingCode trên component để mô phỏng mất acknowledgement của commit/receipt
thật; lọc sang tập rỗng vẫn reconcile đúng pending, không thêm receipt/lệnh mới.
Đây không phải timeout mạng thật. Nếu ADMIN thắng race, xác minh receipt bằng
ADMIN và tạo bước kế tiếp bằng CASH trước ca pending của component CASH.

before-transitions/after-transitions so tài chính hai fixture (không so Delivery/
Revision vì được phép đổi), dòng/ưu đãi/payment/kho/điểm; before/after cleanup so
toàn dữ liệu hiện hữu gồm Revision và receipts. Giữ logs/results từng runner.

Regression native dùng cùng binary mới, thực thi constructor/dialog/query/role
read, không in/checkout/CRUDghi; invoice-regression giữ ma trận53ca độc lập.
Các check không thay số56ID G04/325ID và không tự nghiệm thuT18 từ kết quả cũ.

Hình thức do người dùng kiểm, agent SKIPPED. DPI100/150% NOT RUN. Không chụp ảnh/
hover/Designer visual. User manual câu hỏi đã gửi trên app PID6748, được kiểm
HWND ngoài sandbox (khác inventory Get-Process trong sandbox). Không yêu cầu
người dùng chuyển/thanh toán đơn474 trong ca manual.
