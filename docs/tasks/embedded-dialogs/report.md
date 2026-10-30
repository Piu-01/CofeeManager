# Hộp nổi trong CoffeeManager — triển khai26/10/2026
**Native user xác nhận:** kiểuFormWindows/owner/titleX/khôngtaskbar đúng ý;
nestedF3bỏnháp giữ/bỏ đúng.475chốt→Khôngin→POStrống, chọnhết100000/thối19000,
Enterkhôngchốt; SQL mộtPT00181000,kho/tiêuhao khớp,điểmkhôngđổi. Tiếp tục các
subcase còn lại theo native-test-results.md, chưa nghiệm thu toànND01–ND13.
**Native test đã duyệt, đang chạy:**87dialogcomponent +18ownercomponent (follow-up
fixtureraceND-TEST-01, giữFAILraw) +18unit đạt. Source210file cố định. Chưa đủ
ND01–ND13; chờ user xác nhận đúng kiểuWindows/nestedF3/checkout475 trênPID27260.
[Xem kết quả native](native-test-results.md). Các dòng chờ duyệt test dưới đây
là lịch sử trước lần duyệt mới; không suy mọi ca đãPASS từ sốcomponent.
**Bản hiện tại đã chuyển native:** Form modal Windows titlebar/X/owner, không
taskbar riêng, helper chọn hết giữ nguyên. Debug/Release sạch, smoke18/18; chờ
duyệt đợt test mới. Xem [bàn giao native](native-implementation-report.md) và
[native-test-plan](native-test-plan.md). Các trạng thái host/card bên dưới là
lịch sử trước điều chỉnh được user duyệt, không mô tả source native hiện tại.

**Làm rõ yêu cầu mới nhất:** user chọn Form modal Windows có titlebar/X và owner, không taskbar riêng; từ chối card/container hiện tại. Source vẫn giữ nguyên lượt test host. Chờ duyệt [plan điều chỉnh](native-dialog-adjustment-plan.md) trước sửa; các kiểm tra dưới đây là bằng chứng bản host cũ, không nghiệm thu cách hiển thị mới.


Trạng thái: ĐỢT TEST ĐÃ DUYỆT, ĐANG KIỂM.119componentchecks và18unit đạt; còn các subcase native/CRUD/giao dịch chưa đủ, chưa nghiệm thu ED01–ED13. Xem test-results.md và runs/acceptance-20261026/.
Requirements/plan và bổ sung chọn hết input đã được người dùng duyệt.

## Đã làm

- OrbitDialogHost chứa màn nền và các card con trong MainForm, khóa nền/sidebar,
  stack chỉ cho hộp trên cùng tương tác. Giữ nội dung nền để người dùng thấy ngữ
  cảnh, không dùng screenshot/bitmap để giả giao diện.
- OrbitDialogForm giữ validation/layout, trả kết quả qua await, guard đóng bất
  đồng bộ; abort khi phiên kết thúc hoàn tất các task đang chờ. Host không dùng
  ShowDialog/DoEvents hoặc polling DialogResult.
- Đổi toàn bộ caller editor/picker/detail/preview hiện có sang host, kể cả F3/F4/
  F8, thêm khách từF3, picker trong preview ưu đãi, chi tiết/giao hàng vềPOS.
- Xác nhận dùng hộp chung, guard nháp/busy/unknown giữ nghiệp vụ; MainForm đóng
  và điều hướng theo trạng thái host. Catalogeditor hoàn tất result có Close rõ.
- Native PrintDialog có ownerMainForm. StarDustOverlay vẫn là tool window hiệu
  ứng không nhận input/taskbar, ownerMainForm và vị trí theo child checkout;
  ngoại lệ hiệu ứng này không phải cửa sổ nghiệp vụ. Login/bootstrap/startup
  thuộc vòng đời app; thông báo bootstrap/CLIsetup không chuyển vào host.
- OrbitField chọn hết input khi vào bằng click/Tab, giữ caret các click tiếp,
  idempotent và theo dõi input tạo động/editorgrid; bỏ qua readonly/dropdown chỉ
  chọn. Helper được gắn từ AppTheme, managementinput, recordlist và MainForm.

Source chỉ thay WinForms:29file có sẵn và2file host/base mới; không sửa SQL,
Models/DAL/BLL, quyền hoặc cấu hình runtime. Inventory: runs/implementation-20261026/
changed-product-files.txt và source-final.csv. Không commit.

## Kiểm chứng đã chạy

| Phần | Actual | Giới hạn |
|---|---|---|
| Build Debug/Release cuối |0warning/0error cả hai | Không suy PASS chức năng |
| Smoke host |9/9 assertion true, DeviceDpi120 | Form thật/call component, không click native, không DB, không hình thức |
| Smoke nội dung | Parent embedded/top-level chỉMainForm; child result; parent input giữ; cancel; idle; guard từ chối/rồi đồng ý; abort hoàn tất task | Chỉ prototype host, không thay ED01–ED13 |
| Rà source | Chỉ còn ShowDialog ở native PrintDialog; MessageBox ở bootstrap/CLIsetup | Rà source không thay thao tác thật |
| Hình thức | SKIPPED theo phân công | Người dùng tự kiểm |
|100/150%DPI | NOT RUN | Không đổi Windows Scale |

Smoke đã chạy lại sau bố trí host chứa nền/card; hai thay đổi cuối là guard
shortcutPOS khi host mở và bỏ callback muộn catalog. Source cuối build lại sạch;
không mô tả smoke prototype là đã chạy mọi đường POS/catalog sau các thay đổi đó.
Log compile đầu có lỗi chuyển chữ ký/serializer đã sửa trong pha triển khai;
giữ build-initial.log/build-followup.log, không xóa bằng chứng.

## Chờ kiểm

ED01–ED13 chưa chạy nghiệm thu; xem test-plan.md. Chưa mở bản sản phẩm mới vào
DBtest cho native acceptance. App cũ của user không được tự đóng/reset. Lượt
triển khai không tạo DB hoặc ghi giao dịch, smoke actor không cần SQL/account.
Build/runner nằm trong task này; processsmoke đã kết thúc, không giữ service mới.

Kết quả G03/G04 trước đây vẫn giữ lịch sử theo đúng binary/schema. UI mới phải
hồi quy, đặc biệt T18 và checkout;40PASS/16BLOCKED của G04 là mốc trước refactor,
không tự gán lại PASS đó cho host mới. G05 chưa được triển khai trong task này.





