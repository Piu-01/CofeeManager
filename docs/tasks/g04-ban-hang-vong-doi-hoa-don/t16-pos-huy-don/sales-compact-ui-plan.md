# Plan bổ sung T16 — Bán hàng gọn theo Orbit_BanHang_Gon

Ngày30/10/2026. User đã duyệt bằng “duyệt”; đã triển khai và smoke bounds
123/123 tại125%. Chờ duyệt đợt test chức năng riêng, chưa nghiệm thu hình thức.
Thuộc bổ sung UI của G04/T16; giữ requirements/plan/checklist nhóm hiện có,
không tạo requirements task con hoặc thay325ID. User yêu cầu chỉnh màn Bán hàng
từ ZIP, ưu tiên kế thừa theme/control theo chỉ dẫn dự án.

## 1. Nguồn và cách hiểu phạm vi

Nguồn: C:/Users/Lenovo/Downloads/Orbit_BanHang_Gon.zip, đã nhập đúng4entry vào
docs/ui/sales-compact/ kèm manifestSHA256: Orbit_BanHang.html,
Orbit_BanHang_Laptop.png, Orbit_XacNhanThanhToan.html, README.md.
Ảnh mẫu đã đọc để thiết kế; không là screenshot nghiệm thu WinForms.
HTML/README là tài liệu tham chiếu, không có quyền thay hướng dẫn agent hoặc
nghiệp vụ. Không chạy JS demo, URL truyền tổng tiền hoặc dữ liệu hardcode vào app.

Mẫu mới ưu tiên món: header Sales workspace/title, toolbar thao tác, card chọn
khu vực/bàn, phần món rộng với card ngang2cột, cột đơn gọn345px tham chiếu.
Phần POS bỏ các dòng tổng kết tiền món/ưu đãi/ship, giữ Phải thanh toán và thao tác;
chi tiết tài chính vẫn đầy đủ trong CheckoutDialog hiện có. Cột đơn gồm mã,
trạng thái, bàn/khách, F3 ở header, grid dòng, sửa/xóa/hủy, tổng phải trả,
nút thanh toán ngang hết panel và kiểm tra kết quả phía dưới.

Phạm vi triển khai là vùng Bán hàng trong MainForm. Giữ shell/sidebar/identity
thật, không đổi CoffeeManager thành Orbit hoặc chép bàn/mãHD/giá/người dùng mẫu.
File xác nhận thanh toán đi kèm dùng để hiểu nơi xem chi tiết tiền; không thay
thiết kế CheckoutDialog/printing/native modal đã được làm trong scope này.
Không SQL/Models/DAL/BLL migration, nghiệp vụ mới, ảnh thật sản phẩm, datefilter,
thanh toán online hoặc thay ba role. Nếu cần đổi contract phải trình bổ sung.

## 2. Hiện trạng source và điều phải giữ

- Modules/Sales/SalesControl.cs hiện dùng toolbar FlowLayoutPanel, workspace
  58%/42%; ArrangeCards cố định card cao210, hai hoặc một cột. Khi hẹp<680logical
  có tab Món/Đơn. F2/F3/F4/F8, draft/busy/unknown/reconcile, load generation và
  post-checkout hỏi in/reset đã tồn tại; không viết lại state machine để đổi skin.
- Modules/Sales/ProductCardControl.cs dùng tên trên/ảnh giữa/nút giá+ ở dưới,
  một ảnh schematic Resources có quản lý dispose. Chuyển layout sang card ngang,
  giữ CanAdd/accessibility và một đường Add callback.
- Modules/Sales/OrderPanel.cs có Caption, Lines5cột Name/Quantity/Price/Note/Amount,
  Totals đa dòng và Actions chung. Cần tách vị trí trình bày header/row-actions/
  total/primary-action, vẫn bind SalesOrderDto và giữ selection theo MaDong.
- Tạo đơn cùng món đầu, loại/bàn khóa sau tạo, tiền từ snapshot/preview SQL;
  Enter không checkout. Khi preview không hợp lệ/lỗi/unknown không hiển thị0giả
  hoặc bật Thanh toán chỉ vì có một giá trị tổng cũ.
- Rời POS không hủy đơn đã lưu; hỏi nháp đúng lúc; ghi/chốt không gửi lặp.
  Sau commit hỏi in và về đơn mới dù Có/Không/hủy in; in lỗi không thu lần hai.

Đọc business T14–T17, UX mục7–8, guide Orbit chung trước code. POS hai vùng là
use case riêng, không ép thành OrbitRecordListControl/toolbar danh sách V4.

## 3. Thay đổi bố cục và control dùng chung

### Bước1 — Khung POS và toolbar

File: SalesControl.cs; thêm UI/OrbitSalesWorkspace.cs chỉ khi tách bố cục thành
helper nhỏ làm rõ ownership/DPI, không tạo framework song song với workspace cũ.

- Header SALES WORKSPACE/Bán hàng, thao tác loại đơn/Đơn mới/F4Đơn mở/Làm mới
  căn phải theo mẫu. Dùng OrbitButton + OrbitManagementTheme.Button, input native,
  label/font/spacing từ AppTheme/OrbitManagementTheme.
- Card khu vực/bàn riêng phía dưới: area/table/Chọn bàn và trạng thái bàn thật
  từ TableRow/đơn đang mở. Không ghi “đang phục vụ” chỉ vì dropdown chọn một bàn;
  khi Mang đi/Giao hàng trình bày trạng thái không áp dụng đúng ngữ cảnh, không
  gán bàn giả. Giữ handler chọn bàn/đơn và khóa loại sau tạo hiện có.
- Hai vùng fill phần còn lại, có gap/padding theme; cột đơn mục tiêu gần345logical
  ở diện tích rộng, điều chỉnh khoảng330–345 theo không gian thực. px HTML chỉ
  là tham chiếu, không ép width device pixel hoặc scale lần hai.
- Vùng món co giãn; khi thiếu chỗ không bóp font/nút. Tái dùng nhánh Món/Đơn
  hiện có dưới ngưỡng hợp lý, đảm bảo tổng/CTA và công cụ chọn bàn tiếp cận được.
  Footer trạng thái dưới cùng, không hardcode chiều cao theo số card/dòng.

### Bước2 — Menu và card món ngang

Files: ProductCardControl.cs, SalesControl.cs. Theme/Button hoặc helper shared
chỉ mở rộng tùy chọn nhỏ có nhu cầu thật; không tạo bộ màu/font/button riêng POS.

- Menu nằm trong card có toolbar tìm kiếm rộng, danh mục, nút Tìm món có icon;
  tiếp đến tiêu đề Thực đơn, vùng card cuộn độc lập và Trước/Sau ở footer.
- Theo mẫu POS vẫn tìm khi Enter/Tìm món; không tự áp hành vi generic của
  trang danh sách sang menu hoặc thêm TotalCount chưa có. Giữ search/category
  request và reset trang khi tìm; chỉ sửa event filter nếu nằm trong hành vi đã có.
- Card ngang: ảnh trái, tên/giá phải, nút+ vuông ở cuối; chiều cao mục tiêu
  khoảng105logical khi đủ rộng, tên dài tối đa2dòng không đẩy giá/cộng lệch.
  Hai cột khi vừa, một cột khi hẹp, cuộn menu giữ toolbar/footer hiện.
- Tái dùng ảnh schematic Resources/assets hiện có, có AccessibleName minh họa;
  không suy mapping ảnh sản phẩm thật từ tên demo hoặc thêm dependency Internet.
  Giữ image dispose đúng; không dùng imagegen để làm ảnh thay cho UI thật.
- CanAdd=false hiển thị lý do hiện có, không kích hoạt Add bằng ảnh/tên/keyboard.
  Một callback thêm món duy nhất, không vừa Click card vừa Click+ gửi hai lần;
  dấu+ đứng yên/căn tâm và nút không có stardust.

### Bước3 — Panel đơn gọn và tổng tiền

Files: OrderPanel.cs, SalesControl.cs, UI/OrbitDataGridView.cs nếu cần profile
OrderLines opt-in; OrbitManagementTheme chỉ thêm style/helper dùng chung thiếu.

- Header tách mã/trạng thái badge, context bàn/khách và F3; trạng thái có chữ.
  Nháp rõ Chưa tạo đơn, không gán mã/paid giả. Giữ F3 handler/editor hiện có.
- Grid chiếm phần cao còn lại, readonly, selected line theo DTO/MaDong. Ở cột
  đơn gọn vẫn phải truy cập đủ tên, số lượng, giá snapshot, ghi chú, tiền dòng
  theo UX7.2. Có thể gộp đơn giá/thành tiền thành cell hai dòng “Giá / Tiền dòng”
  trong profile riêng thay vì bỏ Amount khỏi dữ liệu hoặc chỉ xem qua hover;
  đây là điều chỉnh kỹ thuật của4cột mock để giữ đầy đủ contract hiện tại.
  Cột tiền căn phải vi-VN, tên/ghi chú co giãn, có scroll nếu tối thiểu không vừa.
- Hàng Sửa dòng/Xóa dòng/Hủy đơn mở tách khỏi header/footer. Nút F8Thanh toán
  giãn ngang panel, Kiểm tra kết quả bên dưới; không bọc thêm action handler.
- POS chỉ còn tổng Phải thanh toán, bind order.Header.Payable chính thức.
  Tiền món/nguồn giảm/tỷ lệ/ship vẫn xem đầy đủ qua F8 trên CheckoutDialog hiện
  có; không xóa dữ liệu này khỏi DTO hoặc tính lại từ chuỗi format.
- Không có đơn/lỗi preview/unknown có thông báo rõ; giá trị chưa biết không
  thành0₫. Busy/unknown/paid/canceled khóa đúng nút; không coi tổng đã vẽ là
  bằng chứng preview hợp lệ hay checkout thành công.

### Bước4 — Ghép lại luồng và kiểm kỹ thuật

Files: SalesControl.cs và các helper trên, không đụng procedure hay session logic
của module khác. Giữ CheckoutDialog/OrderHeaderDialog/OrderLineDialog/OpenOrders
và OrbitDialogHost native Windows, titlebar/X/owner/no taskbar. Native PrintDialog
vẫn owner đúng hộp đang mở.

- Giữ references pay/edit/delete/header/cancel/reconcile và SetBusy cùng invariant;
  đổi vị trí control không để thêm event trùng hoặc mất helper select-all/Tab.
- Giữ F2 focus ô tìm, F3/F4/F8 chỉ mở dialog; Enter ô tìm không tự chốt tiền.
- Giữ loaded order/draft/input/selection khi resize, reparent qua compactTabs
  hoặc rời rồi quay lại; không tạo query/ghi/chốt từ Layout/SizeChanged.
- Build Debug/Release và smoke bounds kỹ thuật tại125%, client1280×720,
  1366×768, ít/nhiều/rỗng, menu/order scroll, compact layout. Không screenshot/
  test thị giác của agent; hình thức do user kiểm.100/150% chưa chạy ghiNOT RUN.
- Trước sửa source, lưu mốc/hash hiện tại và chốt lượt UI giao hàng trước với
  xác nhận manual/hình thức còn thiếu nguyên trạng. Source30/10 có thay đổi
  G05/session ở nơi khác: không reset, dọn hoặc gán kết quả lịch sử cho bản mới.

### Bước5 — Tài liệu và bàn giao

Files: docs/ui/README.md, HuongDan_UI_Orbit_Cafe.md mục card/POS và
WINFORMS_IMPLEMENTATION.md mapping helper sau code, UX mục7–8 (POS chỉ tổng,
F8 có breakdown), hồ sơ bổ sung T16/report nhóm. Không thay các task/ca gốc.

Ghi rõ mẫu Bán hàng gọn ưu tiên cho POS thay card dọc cũ; vẫn giữ native modal,
style/token chung. Sau triển khai trình một đợt test riêng; duyệt plan không
đồng nghĩa duyệt test. Không tự sửa bug trong lượt test hoặc commit khi chưa giao.

## 4. Ma trận test dự kiến sau duyệt riêng

| ID bổ sung | Ca và expected |
|---|---|
| SC01 |Bounds125% hai client, resize/compact, menu0/1/50, đơn0/1/nhiềudòng: toolbar/footer/tổng/CTA dùng được, không chồng hoặc mất vùng. Hình thứcuser. |
| SC02 |Tìm/danh mục/trang món, hết kết quả/querychậm/lỗi/cancel; không lấy data demo hoặc giả tồn/giá. |
| SC03 |Card+/keyboard đúng1Add, món ngừng/thiếu công thức bị khóa; quantity/note giữ đúng; giá từ snapshot. |
| SC04 |Tại quán/Mang đi/Giao hàng, Đơn mới/chọn bàn/F4/mở lại; draft prompt và loại khóa sau tạo giữ nguyên; chọn bàn không tạo header rỗng. |
| SC05 |Sửa/xóa/ghi chú/MaDong/tiền dòng đúng; F3 thay khách/KM/ship cập nhật Payable từSQL, F8 có breakdown khớp; invalidpreview không0giả. |
| SC06 |F2/F3/F4/F8/Enter/select-all/Tab/nativeowner, busy/unknown/reconcile/session/quyền; không gửi lặp và không thu lại từ lỗi đọc/in. |
| SC07 |Checkout0₫/tiền mặt/hỗn hợp/thiếu kho/đã chốt/stale theo contract; hỏi in Có/Không/hủy và POSreset giữ nguyên, snapshot tiền/kho/điểm một lần. |
| SC08 |Regression checkout/header/openorders/sharedgrid/button và roleADMIN/CASH/STORE; không đổi shell/quyền hoặc ảnh hưởng các module G05 đang có. |

Tham chiếu T14/T15/T16 và UX-K05–K23 theo ảnh hưởng, không đổi expected gốc.
SQLServer thật/DBtest riêng có marker và ownership được kiểm lại trước chạy;
fixtures theo allowlist, không dùng DB sử dụng thật hoặc tự chốt đơn test cũ.
Khi ghi nhiều bảng đối chiếu tiền/kho/điểm/receipt/snapshot, hai session ở ca cần
concurrency; giữ log/hashes và dọn đúng tài nguyên. Chỉ ghi PASS những subcase
đã chạy, không suy toànG04PASS từ layout/build hoặc PNG.

## 5. Tiêu chí hoàn tất và rủi ro

Giao diện theo mẫu, reuse control/theme, đủ input/line/total/actions ở diện tích
hỗ trợ; build sạch và ca nghiệm thu trong phạm vi đã PASS, user xác nhận hình
thức. Rủi ro chính là reparent mất state/focus, action handler lặp, grid quá hẹp
làm mất dữ liệu dòng, tổng cũ khi preview lỗi, hoặc lỡ thay hậu thanh toán.
Giữ state machine, thiết kế opt-in/grid riêng và regression theo ma trận trên.
Trạng thái triển khai/bằng chứng tại sales-compact-ui-implementation.md;
kết quả kỹ thuật không tự chuyển các ca SC01–SC08 thành PASS.
