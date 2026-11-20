# Test results — Rơi gọn thao tác Bàn / Khu vực và chọn bàn ở POS

- Ngày: 20/11/2026
- Môi trường: Windows, .NET SDK 10.0.401, DPI hệ thống 125% (DeviceDpi 120)
- Source: working tree `main` sau triển khai `docs/tasks/roi-gon-thao-tac-ban/` (chưa commit)
- Phạm vi đợt này: **chỉ compile + probe kỹ thuật**. Đợt kiểm chức năng/GUI thật (DB, phiên bàn, POS)
  **chưa chạy** và cần người dùng duyệt riêng theo post-change-test-workflow.

## Kết quả compile

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| RG-B01 | `dotnet build CafeManager.slnx` (BaseOutputPath tạm vì bin đang bị app/VS khóa) | PASS — 0 error, 2 warning có sẵn (WebView2/WindowsBase) | `runs/row-action-20261120/build.log` |

## Probe kỹ thuật (không thay thế kiểm chức năng)

Chạy `RowActionProbe.exe` (project ngoài repo `C:/Users/Lenovo/.qoder/tmp/cm-rowaction-probe/`,
ProjectReference tới `CafeManager.WinForms`). Probe không gọi SQL, không chụp hình, không kiểm hình thức.

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| RG-P01 | `OrbitRecordListControl.AddRowDeleteColumn` tạo cột `Delete` không tiêu đề, rộng 55px ở DPI 125% | PASS | `probe.json`: `ProbeDeleteColumn=true`, `ProbeDeleteWidth=55`, `ProbeDeviceDpi=120` |
| RG-P02 | Click ô `Delete` của dòng 0 phát `RowDeleteRequested` đúng item; double-click ô Delete không phát `RowActivated` | PASS | `DeleteRequests=["B0001"]`, `Activations=["B0002"]`, `DeleteOnlyFromIcon=true`, `DoubleClickIgnoredOnDeleteCell=true` |
| RG-P03 | Double-click ô thường phát `RowActivated` đúng item dòng 1 | PASS | `Activations=["B0002"]` |
| RG-P04 | `TableList` (ADMIN): không còn nút Chọn bàn/Trả bàn/Sửa/Xóa, giữ action Thêm bàn, có cột Delete và 2 hook dòng | PASS | `RemovedActionsGb=true`, `AddKept=true`, `TableDeleteColumn=true`, `TableRowDeleteHook=true`, `TableRowActivateHook=true` |
| RG-P05 | `SalesControl`: không còn nút "Chọn bàn", vẫn còn "Trả bàn" | PASS | `SalesHasNoSelectTable=true`, `SalesKeepsCloseTable=true` |

Ghi chú phạm vi probe: chưa kiểm paint icon thùng rác (chỉ kiểm cột/tương tác), chưa kiểm
`AreaList` runtime và luồng tự mở bàn ở POS vì cần bàn liệu SQL thật.

## Chưa chạy (cần duyệt đợt test riêng)

| Mã | Nội dung | Trạng thái |
|---|---|---|
| RG-F01 | POS: chọn bàn trống trong ô chọn bàn → vào đúng bàn, kênh Tại quán | NOT RUN |
| RG-F02 | POS: chọn bàn đang có đơn → mở đơn; có nháp → hỏi xác nhận bỏ nháp | NOT RUN |
| RG-F03 | POS: Đơn mới/đổi kênh/tải lại/sau trả bàn không tự mở bàn lặp | NOT RUN |
| RG-F04 | Bàn: double-click dòng mở Sửa đúng dòng; icon thùng rác mở Xóa + xác nhận; nhân viên CASH không có icon/không sửa | NOT RUN |
| RG-F05 | Khu vực: double-click/icon thùng rác đúng dòng; chặn xóa khu vực còn bàn | NOT RUN |
| RG-F06 | Regression T34: trả bàn từ POS, "Trả bàn ngay?" sau thanh toán | NOT RUN |
| RG-F07 | Hình thức UI (màu icon, hover, spacing) | SKIPPED theo phân công — người dùng tự kiểm |

## Tồn đọng từ bug T34-02

UI thật và regression của T34-02 vẫn NOT RUN vì `bin` bị Microsoft Visual Studio/CafeManager.WinForms
khóa; xem `docs/tasks/g04-ban-hang-vong-doi-hoa-don/t34-phien-ban/test-results.md`.
