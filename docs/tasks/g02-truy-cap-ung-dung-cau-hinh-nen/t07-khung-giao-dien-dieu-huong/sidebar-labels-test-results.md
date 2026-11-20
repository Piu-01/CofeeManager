# Sidebar — label phân nhóm, không thu gọn tab

Ngày09/11/2026. Người dùng làm rõ sau bản đầu: “chỉ là kiểu label thôi”,
“không ẩn tab”. Chỉ dẫn này thay giả định nhóm accordion trong đề xuất trước;
thực hiện trực tiếp chỉnh sửa và regression liên quan trong phạm vi sidebar.

**Kết quả: đã sửa đúng hành vi label, regression299/299PASS tại125%.**
Không nâng trạng thái nghiệm thu G02/T01–T32 từ kết quả này; hình thức người
dùng tự kiểm. [Kết quả297/297 cũ](sidebar-ui-test-results.md) là lịch sử phiên
bản accordion đã được thay thế, không là bằng chứng hành vi label hiện tại.

## Thay đổi

- OrbitSidebar dùng Label thường làm tiêu đề nhóm, TabStop=false; không có
  nút nhóm, chevron, Click handler, trạng thái expanded hoặc logic thu gọn.
- Mọi button đã được phép theo role luôn Visible trong danh sách cuộn ở cả
  chế độ đầy đủ và compact. Chuyển màn chỉ cuộn tới mục được chọn, không ẩn
  các tab khác. Label chỉ phân cách thị giác, không nhận focus bàn phím.
- Bỏ OpenDefaultGroup và lời gọi tại MainForm. Giữ sửa header/footer cố định,
  thứ tự5nhóm, quyền và mọi handler nghiệp vụ hiện có.
- Chỉ đổi source OrbitSidebar.cs và MainForm.cs so với bản trước; không đổi
  Designer, BLL/DAL/SQL hoặc quyền theo vai trò.

## Môi trường, lệnh và bằng chứng

Windows, SDK10.0.401, net10.0-windows, DeviceDpi120=125%; desktop1920×1080,
WorkingArea1920×1020. Client1280×720/1366×768/800×640 rồi về1366×768.
DB test schema016 trên localhost:
`CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout`, marker G04TestMarker.
Chỉ đọc identity/quyền qua AuthRepository/PermissionService thật; không ghi DB.

MainForm/sidebar/controller/modal/logout thật; module body dùng ProbePage có
TextBox và trạng thái nháp/busy. Không suy thành kiểm CRUD/POS/login thật.
Tái dùng runner của đợt trước, thay các ca toggle/accordion bằng ca label
không tương tác và mọi tab luôn Visible. Giữ kỳ vọng cũ cho guard, module
cache, header/footer và quyền. Source cố định230file trong lượt test.

Run directory: [sidebar-labels-20261109](../runs/sidebar-labels-20261109/).

```powershell
dotnet build <run>/SidebarRegression.csproj -c Debug -o <run>/bin -m:1 -nr:false -p:UseSharedCompilation=false -p:NuGetAudit=false -v minimal
dotnet <run>/bin/SidebarRegression.dll <run>
dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj -c Debug -o <run>/app-debug --no-restore -m:1 -nr:false -p:UseSharedCompilation=false -v minimal
dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj -c Release -o <run>/app-release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false -v minimal
```

`<run>` là `docs/tasks/g02-truy-cap-ung-dung-cau-hinh-nen/runs/sidebar-labels-20261109`.
Build runner đầu gặp CS0136 do trùng tên biến local của runner; sửa trước
khi bắt đầu test, không đổi source sản phẩm/expected. Build runner cuối dùng
--no-restore và BuildProjectReferences=false,0warning/0error. Không chạy audit
dependency trong lượt này. Không thay môi trường desktop, service hay cấu hình.

## Kết quả

| Phạm vi / tham chiếu | Expected và actual | Trạng thái |
|---|---|---|
| LB01 — T07-K01/K02 | Đúng menu/startup3role; label5nhómADMIN/4CASH/3STORE; Catalogs chỉADMIN | PASS |
| LB02 — T07-K02/K04 | Tất cả tab được phép luôn Visible ngay khi mở, sau click label, resize, cuộn, đổi compact và từng lần chuyển màn | PASS |
| LB03 — T07-K02/K04 | Label không CanSelect/TabStop; gửi WM_LBUTTONDOWN/UP vào label không ẩn tab/chuyển màn | PASS |
| LB04 — T07-K04 | SelectNextControl forward/backward bỏ qua label, tới button và quay lại Trang phiên | PASS; kiểm cơ chế focus WinForms, không suy thành thao tác Shift vật lý |
| LB05 — T07-K04 | Header/footer/menu không chồng; menu cuộn riêng, logout luôn trong footer, row button nằm trong container | PASS toàn ma trận client/role |
| LB06 — T07-K02/K03 | Điều hướng đưa selected target trọn vào viewport, một selection; tab khác vẫn hiện; input/cache giữ nguyên | PASS |
| LB07 — T07-K05/K07 | Click lặp không tạo module trùng; guard busy/nháp/modal; bỏ nháp đúng một lần; logout/revoke đúng một event | PASS |
| LB08 — toàn vẹn lượt kiểm |230file source không đổi; connection DB test không transaction treo | PASS,0file đổi, transaction0:0 |
| Build Debug/Release | Compile bản mới không warning/error | PASS cả hai0warning/0error; product-debug.log/product-release.log |
| Hình thức, hover, spacing, ảnh đối chiếu | Người dùng tự kiểm | SKIPPED theo phân công |
| DPI100/150%, Designer save/reopen, CRUD/POS/login và SQL runtime least privilege | Ngoài lượt kiểm sidebar | NOT RUN |

Raw [results.json](../runs/sidebar-labels-20261109/results.json):299/299PASS,
0FAIL; đây là các biến thể kiểm component, không phải299ID Txx-Kxx.
[source-audit.json](../runs/sidebar-labels-20261109/source-audit.json) giữ manifest
và hash binary. Không xóa hoặc sửa raw evidence297/297 của phiên bản trước.

Tạo mới0DB/0principal/0account/0fixture dữ liệu; không ghi DB hiện có. Các form
và process runner đã kết thúc. Giữ evidence/runner; output binary được ignore.
Chưa commit/push. Hành vi label đã regressionPASS, hình thức chờ người dùng kiểm.
