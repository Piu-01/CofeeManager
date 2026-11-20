# Bán hàng gọn — triển khai30/10/2026

Đã triển khai theo plan user duyệt; chờ duyệt đợt test riêng. Không thay sốca
nghiệm thuT16/G04 hoặc coi mock/technical smoke là PASS hình thức.

## Thay đổi trong phạm vi

- SalesControl: header và actiontoolbar, khu vực/bàn có status từ TableRow;
  menu card/toolbar/footer, cột đơn330–345logical, tab Món/Đơn khi hẹp. Giữ các
  callback nghiệp vụ, Guard/SetBusy, F2/F3/F4/F8, draft và post-checkout/in/reset.
- ProductCardControl: card ngang105logical, tên2dòng/ảnh schematic/giá và+ vuông.
  Chỉ nút+ có Add handler; CanAdd và image dispose giữ đúng vai trò.
- OrderPanel: mã/status/context/F3 riêng, grid fill phần còn lại, rowactions,
  chỉ Payable, F8 và reconcile giãn ngang. ShowOrder giữ line selection theoMaDong;
  SetResultUnknown che tổng chưa biết; PreviewValid quyết định tổng đơn mở.
- OrbitDataGridView thêm profile OrderLines opt-in (readonly/Price+Amount cùng
  cell2dòng, qty/note riêng), không sửa profile module khác.
- OrbitSurface/OrbitBadge mới là control nhỏ dùng chung dựa palette/fonts/Px
  hiện có; không tạo theme, repository hoặc framework điều hướng mới.

6file source trong scope (4sửa+2mới), không Models/DAL/BLL/SQL/CheckoutDialog/
Printing/session changes. Workspace đồng thời có thay đổi module khác; giữ
nguyên, ghi concurrent-out-of-scope-changes.csv, không tự sửa warning ngoàiPOS.

## Kiểm kỹ thuật trong triển khai

Build Debug và Release đều exit0/0error qua Rebuild cuối. Mỗi cấu hình còn1warning
CS8602 tại Modules/HumanResources/AttendanceControl.cs:53 thuộc thay đổi đồng thời
ngoàiPOS; đã giữ nguyên, không gọi build sạch warning.6filePOS không có warning
được báo trong log. Source toànworkspace205file tại mốc cuối, không đồng nghĩa
task này sửa205file hoặc sở hữu các module nhân sự/tài chính/session mới.
Smoke123/123bounds tại DeviceDpi120: MainForm/controls thật, session constructor
đã revoke trước dùng để chặn Guard/query, DTO/card trong bộ nhớ, không Auth/SQL/
chuyển ghi hoặc chụp ảnh.1280×720 và1366×768 với0/1/50món/dòng, cardchildren,
toolbar/footer/actions, gridheader+ít nhất1row đều nằm trong ancestor. Nhánh
hẹp960×900 dùng Món/Đơn, đổi lại desktop giữ parent/selectedLine. Không đổi Scale;
100/150% và các kích thước nhánh hẹp khác NOT RUN.

Smoke đầu đo grid9px/57px ở2desktop, không đủ header+row. Đã thu gọn header/
footer/order row trong pha triển khai theo cùng expected, rawFAIL tại
layout-smoke-initial.json và source-before-layout-adjustment; không thay expected.
Bản sau grid104px/152px, invariant gốc đạt. Wide111/111 lưu riêng rồi thêm12check
compact/restore thành123/123. Đây là bước hội tụ layout của plan triển khai,
không một đợt nghiệm thu có source cố định. Chưa chứng nhận SQL/keyboard/menu/
total/unknown/checkout/printing chức năng hoặc uservisual.

[Bằng chứng run](../runs/sales-compact-implementation-20261030/manifest.json):
build-debug-initial/final.log, build-release.log, layout-smoke.json, source-before/
pre-final-build/final.csv, owned-source-final.csv, dll-hashes.json và concurrent
changes riêng. Source trước khi sửaPOS đã lưu; lượt giao hàng cũ được đóng theo
hash cũ với manual/hình thức thiếu giữ nguyên. Không kế thừa8/8T18 hoặc sốcheck
V4/Giao hàng thành kết quả POS mới; không đụng tiến trình/fixture G05/G06.

Chưa mở binary mới dùng thử, chưa tạo DB/fixture hoặc commit. Đợt test đề xuất:
[sales-compact-ui-test-plan.md](sales-compact-ui-test-plan.md).
