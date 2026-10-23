# Audit màn mẫu Orbit 9

Ngày 21/10/2026. Phạm vi đã duyệt: bộ control chung, Nguyên liệu / Tồn và sidebar. Không triển khai tám màn tiếp theo.

## Nguồn và kiến trúc

Đã đọc README_FIRST.md, design-tokens.json, chín HTML và ảnh references trong bộ được lưu tại `docs/ui/orbit-9-html/`, cùng tài liệu UI chung. HTML/CSS dùng lấy thông số; ảnh dùng đối chiếu. Target hiện có giữ nguyên `net10.0-windows`, SDK 10.0.401. Không WebView hoặc ảnh nền giao diện.

MainForm tiếp tục sở hữu shell và điều hướng. MaterialsControl giữ MaterialService, session, binding và các handler Thêm / Mở sổ / Sửa / Xóa. Các dialog nghiệp vụ hiện có tiếp tục được gọi. BLL, DAL, Models và SQL không đổi so với snapshot đầu đợt (`runs/source-audit.json`).

## File production

Sửa năm file:

- `src/CafeManager.WinForms/Forms/MainForm.cs`
- `src/CafeManager.WinForms/Forms/MainForm.Designer.cs`
- `src/CafeManager.WinForms/Modules/Inventory/MaterialsControl.cs`
- `src/CafeManager.WinForms/Modules/Inventory/MaterialsControl.Designer.cs`
- `src/CafeManager.WinForms/UI/OrbitButton.cs`

Thêm tám file:

- `src/CafeManager.WinForms/Theme/OrbitManagementTheme.cs`
- `src/CafeManager.WinForms/UI/OrbitDataGridView.cs`
- `src/CafeManager.WinForms/UI/OrbitEmptyState.cs`
- `src/CafeManager.WinForms/UI/OrbitFilterBox.cs`
- `src/CafeManager.WinForms/UI/OrbitListWorkspace.cs`
- `src/CafeManager.WinForms/UI/OrbitPagination.cs`
- `src/CafeManager.WinForms/UI/OrbitSearchBox.cs`
- `src/CafeManager.WinForms/UI/OrbitSidebar.cs`

OrbitButton dùng biến thể opt-in để không đổi cách vẽ các nút ở tám màn còn lại. Tabs chung hiện có được giữ; màn mẫu không có tab nên chưa áp dụng hay nghiệm thu lại Tabs. MainForm là AppShell hiện có, không tạo một lớp shell trùng chức năng.

## Quyết định đã duyệt và dọn mã

SQL vẫn trả tối đa 50 dòng/lần. UI tải snapshot qua cùng BLL theo từng batch 50, lọc đơn vị trên snapshot và trình bày bảy dòng/trang. Tổng hiển thị chỉ chốt sau khi tải đủ; lỗi giữ dữ liệu trước đó và báo chưa cập nhật. Không suy ra tổng từ một batch. Cột mốc mở sổ được bỏ khỏi phần nhìn theo mẫu; dữ liệu và tooltip vẫn giữ.

Sửa/Xóa liền nhau; Mở sổ vẫn có. Giữ đầy đủ menu theo quyền và Đăng xuất; profile dùng session thật. Không thay tên ứng dụng thành tên thương hiệu minh họa. Không sửa dữ liệu tên không dấu/đơn vị trong DB để giống ảnh.

Đã bỏ wiring bố cục cũ trong màn mẫu và style sidebar dư thừa. Không xóa OrbitManagementLayout/RecordListControl hoặc helper khác vì các màn còn lại vẫn tham chiếu. Không xóa lịch sử test. Không commit/push.

## Vòng tự sửa

Giữ bằng chứng các vòng trước trong runs: sửa nhãn có dấu &, kích thước filter, font bảng/header, icon sidebar và lỗi màu hover lưu lại sau khi rời dòng. Source R4 đóng băng ở `runs/source-r4-final.csv`; không sửa production trong lượt kiểm tra R4.

Lượt native R4 150% đã kết thúc trước sửa: sidebar tràn hồ sơ và border dọc còn lệch mẫu. R5–R8 chỉ sửa hai control chung OrbitSidebar/OrbitDataGridView. Sidebar giảm khoảng trống đầu theo chiều cao, giữ nguyên kích thước nút; grid vẽ nội dung readonly từ FormattedValue/CellStyle và đường ngang, khôi phục GraphicsState của radio để không giữ anti-alias sang cell. Không thay binding/format/handler. Raw FAIL giữ ở runs; source cuối hiện là `runs/source-r8-final.csv`.

R9 chỉ thêm repaint sidebar để xóa avatar cũ khi resize; native100% maximize/restore đãPASS. R10 theo yêu cầu mới kiểm Program.cs/DPI: thêm ApplicationHighDpiMode=PerMonitorV2 trong `src/CafeManager.WinForms/CafeManager.WinForms.csproj`, thêm comment nguồn cấu hình trong `src/CafeManager.WinForms/Program.cs`. Tổng production so đầu pilot hiện bảy file sửa và tám file thêm. Business/DAL/Models/SQL vẫn không sửa bởi đợt UI. Source mới `runs/source-r10-final.csv`; generated files nằm trong obj, không commit.
