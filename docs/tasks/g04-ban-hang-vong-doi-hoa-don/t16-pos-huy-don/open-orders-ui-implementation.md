# Triển khai Đơn đang mở — 25/10/2026

Phạm vi/plan đã được người dùng duyệt. Chỉ hai file sản phẩm thay đổi:
Modules/Sales/OpenOrdersDialog.cs và UI/OrbitDataGridView.cs.

## Kết quả triển khai

- OpenOrdersDialog dùng OrbitDialogLayout với header/giải thích, OrbitField tìm
  kiếm và số đơn; bảng cuộn bên trong; footer mã đang chọn, Trước/Sau/trang/Mở đơn.
  Theme/button/notice dùng chung, không tạo bộ control riêng hoặc nhúng HTML.
- ProfileOpenOrders read-only tách khỏiInventory/Payment. Code có minimum150px
  logical hoặc độ rộng text mã thực/HD99999999+padding nếu lớn hơn; đo với font
  đãscaleDPI. Cột CodeFrozen/AutoSizeNone, không bị Fill ép nhỏ hoặc metrics tồn
  kho áp lại khi binding. Footer dùng nguyên row.Code, không cắt….
- Created180logical, Channel110, Payable140, CustomerFill tối thiểu140. Header,
  font và padding từsharedtheme; Created dd/MM/yyyy HH:mm, Payable vi-VN+₫ cănphải,
  Channel label/badge tiếng Việt. Các profile cũ giữ metrics/painting cũ.
- Header/search/footer nằm ngoài vùng bảng nhận phần chiều cao còn lại. Form
  giới hạnWorkingArea, hỗtrợ cửa sổ hẹp bằng cuộn, không dùng Percent trong AutoSize
  như lỗiCheckout trước. Không claim bố cục đạt từ source.
- Giữ service.OpenAsync/paging50; Trước/Sau theo tổng thật. Mở đơn/Entergrid/
  doubleclick chỉkhi dữliệu sẵn và córowhợplệ; Entersearch chỉtìm, Escđóng.
  Loading/error khóa chọnmở, giữ input/nhãnChưa cập nhật; generation loại responsecũ.
  Trang cuối hếtđơn thì đọc lại trangđầu để không giả khôngcònđơn vìpage vượtphạmvi.

## Kiểm chứng trong pha triển khai

Build Debug/Release0warning/0error, artifact tại
../runs/open-orders-ui-20261025/{Debug,Release}. Source/manifest/hash được lưu
cùng thư mục. Chưa chạy functional test, app mới, ghiDB hoặc nghiệmthu hìnhthức.
Không sửaSQL/DAL/BLL/role hoặc luồng checkout/in mới đã triển khai trước đó.

Chuẩn userZIP tại docs/ui/open-orders/; ảnhdemoHD2… bị ghi đè bởi yêu cầu người
dùng về mã đầy đủ. Không dùng screenshotHTML hoặc source review làm PASSWinForms.
G04 giữ34PASS/0FAIL/22BLOCKED. Đợt hỏiin/reset giữbinary cũ và kết quả riêng;
PC01Không419/PC02CóHủy417 đãPASS, in thật/lỗi in còn chưa hoàn tất.

## Bước tiếp theo

Chờ duyệt open-orders-ui-test-plan.md, đóng băng source mới và kiểmWinForms125%,
text/cell bounds cả sau binding/paging/resize; tìm/chọn/mở đúng mã và regression
profile cũ. Hình thức do người dùng tự kiểm, không tự chụpảnhQA.
