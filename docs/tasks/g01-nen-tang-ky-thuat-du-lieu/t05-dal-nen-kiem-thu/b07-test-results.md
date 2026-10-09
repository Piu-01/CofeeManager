# G01-B07 — Regression UI StartupForm

**Duyệt:** Người dùng “ok duyệt”; sửa UI và regression trong phạm vi B07.
**Run:** `G01_B07_20261008_232240`, source chưa commit, fixed trong lượt;
manifest/stability ở runs nhóm, count=0 file đổi. SQL/schema/migration không đổi.

## Thay đổi

- StartupForm.Designer.cs: header Espresso, subtitle, filter panel, input/nút
  40px, status band, content heading và empty-state panel; palette guide.
- Grid: bỏ viền đen, header/chọn Caramel kem, viền #DDD0C2, dòng xen kẽ,
  padding và row/header height; đổi nhãn “Trạng thái DB” thành “Trạng thái”.
- StartupForm.cs: màu disabled sáng, nội dung config/loading/empty/error/hủy;
  giữ dữ liệu cũ nếu có, không bind rỗng để thay lỗi, guard dispose/version giữ nguyên.
- Program.cs: lời giải thích thiếu cấu hình ngắn gọn, không in tên biến môi trường
  lên vùng thông báo. Cấu hình/principal/service/query vẫn theo contract hiện có.

## Kết quả

**Cập nhật:** Designer source B07 đã lưu/đóng/mở lại thành công trong VS18 bằng
DTE, đúng solution; UIAutomation/ảnh native xác nhận control mới không màn lỗi.
Source không đổi sau save/reopen. Mục Designer NOT RUN trong bảng dưới là lịch
sử trước lượt này, nay **PASS**. Bằng chứng designer-reopen.json,
designer-reopened.png, designer-source-stability.json ở runs nhóm B07.
DPI100/150, Tab/focus và app identity/slow-query vẫn chờ.

Windows thực tế DPI 120 = 125%; native Form, .NET SDK 10.0.401.

| Kiểm chứng | Expected | Actual | Trạng thái |
|---|---|---|---|
| Build Debug/Release toàn solution | 0 errors, không warning mới | Cả hai 0 warning/0 error | PASS |
| Thiếu cấu hình | Nút disabled dễ đọc, thông báo rõ, không bảng giả rỗng | App thật hiển thị panel chưa kết nối, nền disabled sáng | PASS |
| Ready/loading/lỗi kết nối | Trạng thái có chữ, loading khóa gửi lặp, lỗi không thành empty success | UIAutomation bấm tải; loading; sau lỗi mã258 thông báo Chưa cập nhật, nút tải enabled | PASS |
| Resize 1280×720 và 1366×768 ở125% | Control/nội dung chính không bị che | PrintWindow PNG + bounds UIAutomation đúng hai kích thước, ảnh không cắt control chính | PASS |
| Hình thức grid | Header/viền/chọn dòng/spacing đúng palette | Host ngoài repo mở StartupForm thật, bind 4 DTO minh họa; ảnh native có header/row styles mới | PASS hình thức |
| Dữ liệu grid từ SQL với app identity riêng | Query thật dưới quyền app | Chưa có Windows account app; preview không thay ca SQL/runtime | BLOCKED |
| Designer sau thay đổi B07 | Save/close/reopen không lỗi | Chưa có ảnh/xác nhận cho source mới; xác nhận trước thuộc source trước B07 | NOT RUN |
| DPI100/150% và keyboard Tab/focus | Layout/focus đúng trên app thật | Máy hiện125%, chưa thao tác manual ở100/150%; không giả lập Scale để ghi PASS | NOT RUN |
| Empty/hủy/đóng khi query chậm | Empty khác lỗi, không late update sau hủy/đóng | Chưa có principal riêng để query/lock fixture; đóng app sau lỗi và thiếu config thành công, không thay ca slow-query | BLOCKED |

Không chạy lại SQL/xUnit không bị ảnh hưởng bởi sửa hình thức. Kết quả T03/T05
SQL 10/10 và20/20 trước đó được kế thừa đúng phần DAL/fixture không đổi.

## Bằng chứng/lệnh

- `dotnet build CafeManager.slnx -c Debug/Release --no-restore`;
  build-debug.txt/build-release.txt.
- App thật qua Program: unconfigured-1280x720.png, unconfigured-1366x768.png,
  connection-error-*.png, loading.png, connection-failed.png và ui-evidence.json.
- Failure test dùng 127.0.0.1:1, database không có thật, Connect Timeout1;
  không ghi DB sử dụng thật, không thay admin làm app principal cho success.
- Grid-demo-*.png/grid-demo-evidence.json: chỉ **4 dòng minh họa**, không SQL.
  Host tạm trong Temp/opencode/B07GridPreview, source app không thêm test mode.
- Capture đầu CopyFromScreen bị cửa sổ khác che; đã thay bằng PrintWindow và
  ghi đè ảnh thử sai bằng ảnh đúng. Harness tìm button lần đầu thất bại do
  literal Unicode của script PowerShell; sửa harness ngoài repo, lượt cuối thành công.

B07 đã sửa source và kiểm chứng phần hình thức ở125%; chưa tuyên bố PASS toàn
DPI/Designer/slow-query cho source mới. Cần người dùng mở lại Designer và phối hợp
account app. Không commit.
