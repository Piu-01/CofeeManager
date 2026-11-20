# T07-SIDEBAR-20261109 — Sửa header và phân nhóm sidebar

**Đã được thay thế theo làm rõ của người dùng09/11/2026:** nhóm chỉ là label,
không thu gọn/ẩn tab. Bản mới đã sửa và299/299regressionPASS; xem
[biên bản label](sidebar-labels-test-results.md). Kết quả dưới đây giữ nguyên
làm lịch sử bản accordion, không là trạng thái UI hiện hành.

Ngày09/11/2026. Người dùng đã duyệt đề xuất sửa và regression liên quan bằng
“duyệt”. **Đã triển khai; regression phạm vi sidebar297/297 kiểm tra PASS.**
Hình thức giao diện do người dùng kiểm; không mở lại nghiệm thu toàn G02 hoặc
đổi trạng thái/checklist lịch sử T01–T32.

## Thay đổi

- `UI/OrbitSidebar.cs`: thay FlowLayoutPanel bằng Panel chứa layout ba vùng;
  header/footer cố định, menu giữa cuộn. Header dùng tham chiếu rõ ràng.
- Nhóm Bán hàng, Khách hàng & ưu đãi, Menu & kho, Tài chính & báo cáo, Quản trị;
  chỉ một nhóm mở ở chế độ đầy đủ. Trang phiên giữ riêng; nhóm rỗng ẩn theo role.
  Compact giữ tất cả lối vào bằng icon, tooltip/tên truy cập và phân cách nhóm.
- `Forms/MainForm.cs/.Designer.cs`: giữ các button/handler/module hiện có, đưa
  vào nhóm, cập nhật selection/reveal; không thay NavigationController, BLL,
  DAL, SQL hoặc quyền. Các bổ sung Nhân sự/Tài sản/Báo cáo và OpenWorkspaceAsync
  đã có trong working tree trước lượt này được giữ nguyên.

## Môi trường, fixture và giới hạn

- Windows; SDK10.0.401; WinForms net10.0-windows, PerMonitorV2;
  DeviceDpi120=125% thực. Desktop1920×1080, WorkingArea1920×1020.
- Kiểm client size1280×720,1366×768,800×640 rồi về1366×768 ở cả3role.
  Đây là kích thước client, không đổi độ phân giải desktop/Windows Scale.
- DB test sẵn có trên localhost:
  `CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout`, marker G04TestMarker,
  schema016. Chỉ SELECT/EXEC thủ tục đọc identity/quyền; không thêm/sửa/xóa
  account, không thay mật khẩu, không có giao dịch nghiệp vụ.
- MainForm/OrbitSidebar/NavigationController/modal/logout thật; nội dung các
  module dùng ProbePage có input và trạng thái nháp/busy để cô lập kiểm shell.
  Session dùng identity đọc từ fixture và PermissionService thật kiểm lại SQL;
  không chạy luồng nhập mật khẩu/login hoặc kiểm quyền SQL runtime tối thiểu.
- Không kiểm lại CRUD/POS/checkout hoặc hình thức module nghiệp vụ. Không dùng
  kết quả component để nâng nghiệm thu các ca chức năng module.
- HEAD tham chiếu `8a38f8691d37089feec149b71a8f5718d37167f3`, working tree chưa
  commit. Manifest230file .cs/.csproj trong src cố định trong cả hai lượt.

## Lệnh và bằng chứng

Artifacts tại [runs/sidebar-ui-20261109](../runs/sidebar-ui-20261109/).

```powershell
dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj -c Debug -o <run>/app-debug --no-restore -m:1 -nr:false -v minimal
dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj -c Release -o <run>/app-release --no-restore -m:1 -nr:false -v minimal
dotnet build <run>/SidebarRegression.csproj -c Debug -o <run>/bin --no-restore -m:1 -nr:false -p:BuildProjectReferences=false -p:UseSharedCompilation=false -v minimal
dotnet <run>/bin/SidebarRegression.dll <run>
```

`<run>` là `docs/tasks/g02-truy-cap-ung-dung-cau-hinh-nen/runs/sidebar-ui-20261109`.
Runner native cần quyền kết nối SQL Server/desktop; sandbox mặc định không kết
nối được Named Pipes, lượt chạy được cấp quyền qua công cụ. Không đổi SQL service,
Windows Scale hoặc cấu hình ứng dụng.

## Kết quả lượt cuối

| Phạm vi / tham chiếu | Expected | Actual / kết quả |
|---|---|---|
| Build kỹ thuật | Debug/Release compile được | PASS, cả hai0warning/0error; build-debug-final.log, build-release.log |
| SB01 — T07-K01/K02, UX-K01 | Giữ đủ menu/startup ADMIN/CASH/STORE, Catalogs chỉADMIN, nhóm rỗng ẩn | PASS; ADMIN5nhóm, CASH4, STORE3; startup giữ nguyên |
| SB02 — T07-K04 | Header nằm trên menu, footer dưới menu, không chồng sau resize/cuộn | PASS ở ma trận client trên; tại1280×720 headerY0/H105, menuY105/H455, footerY560/H160 |
| SB03 — T07-K02/K04 | Click nhóm chỉ mở/thu, không đổi màn; tối đa một nhóm mở; Space/Enter toggle một lần | PASS cả3role, native WM_KEYDOWN/UP cho Space/Enter qua message loop |
| SB04 — T07-K04 | Tab tới control visible/enabled, đảo chiều quay lại header | PASS qua SelectNextControl forward/backward của WinForms; không suy thành kiểm phím Shift vật lý |
| SB05 — T07-K02/K04 | Compact giữ đủ target, tên truy cập/tooltip; icon còn chỗ dù có scrollbar; logout tiếp cận được | PASS tại800×640, sau quay lại chế độ đầy đủ vẫn đúng |
| SB06 — T07-K02/K03 | Điều hướng ngoài sidebar mở nhóm đúng và đưa mục chọn trọn vào viewport; chỉ một selection | PASS tất cả target được phép của từng role |
| SB07 — T07-K03/K05 | Button điều hướng và click lặp không tạo module trùng; quay lại giữ input | PASS, factory tạo một lần/target/phiên, input được giữ |
| SB08 — T07-K05/K07, UX-K03 | Busy chặn rời; từ chối bỏ nháp giữ trang/selection; đồng ý bỏ một lần rồi chuyển | PASS bằng controller/modal thật và ProbePage có trạng thái nháp/busy |
| SB09 — T07-K05/K07 | Modal đang mở chặn điều hướng; logout một event và revoke session | PASS cả3role |
| SB10 — tính toàn vẹn lượt kiểm | Source giữ nguyên; DB test không có transaction treo | PASS,230file không đổi, transaction0:0 |
| Hình thức/hover/spacing/ảnh đối chiếu | Người dùng tự kiểm | SKIPPED theo AGENTS.md, agent không chụp ảnh/nghiệm thu visual |
| DPI100%/150%, Designer save/reopen, tương tác POS/CRUD thật | Không suy PASS ngoài lượt này | NOT RUN |

Raw [results.json](../runs/sidebar-ui-20261109/results.json):297/297 kiểm tra
PASS,0FAIL. Số kiểm tra gồm các biến thể bounds/role/target; không phải297ID
Txx-Kxx và không thay checklist325ID. Debug DLL trong runner khớp SHA256 với
app-debug; [source-audit.json](../runs/sidebar-ui-20261109/source-audit.json)
ghi hash binary và230file không đổi.

## Lịch sử lượt đầu và lỗi công cụ

- Lượt1:267/268PASS, một FAIL ở kiểm tra cuối của runner:
  `SELECT CONCAT(@@TRANCOUNT,':',XACT_STATE())`. Không có lỗi sidebar.
  Đối chiếu sqlcmd thật cùng DB: biểu thức trực tiếp trả `0:1`; lấy hai trạng
  thái vào biến bằng DECLARE trước SELECT trả count0/state0, chuỗi `0:0`.
- Kết thúc lượt1, giữ nguyên [results-r1.json](../runs/sidebar-ui-20261109/results-r1.json),
  runner-r1.log, Program-r1.txt, source-audit-r1.json. Sau đó sửa phép đo runner
  sang DECLARE, giữ expected transaction sạch0:0; bổ sung kiểm viewport,
  tooltip và click lặp theo phạm vi đã duyệt. Không sửa source ứng dụng giữa
  hoặc trong các lượt. Lượt2 chạy lại toàn bộ297kiểm tra đềuPASS.
- Build runner có NU1900 do không truy cập được feed NuGet để lấy thông tin
  vulnerability; không coi là đã kiểm audit dependency. Một lần rebuild gặp
  CS2012 khóa DLL trong obj, log probe-build-r2.log được giữ; build runner lại
  không rebuild project references và không dùng compiler server thì thành công.
  Build sản phẩm Debug/Release cuối không có warning/error.
- Lần build mặc định đầu báo FAILED0warning/0error; chạy MSBuild một node,
  tắt node reuse thì build thành công. Không đổi target framework/package hoặc
  đóng process ứng dụng của người dùng để né khóa file.

## Tài nguyên và bàn giao

Tạo mới0DB/0SQL principal/0Windows account/0fixture dữ liệu. Test forms/process
runner đã kết thúc; DB test có sẵn giữ nguyên. Giữ source runner, manifest,
raw JSON và log làm evidence; thư mục binary Debug/Release/bin/obj được ignore.
Chưa commit/push. Kết luận phạm vi sửa sidebar: đã sửa và regression chức năng
PASS; chờ người dùng kiểm hình thức, không có blocker kỹ thuật hiện hành.
