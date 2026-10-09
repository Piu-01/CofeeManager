# B09 — Regression DPI và Designer ngày 09/10/2026

Người dùng duyệt sửa B09 bằng “duyệt”; sau đó cho phép **Nạp lại từ đĩa**
đối với StartupForm đang có thay đổi chưa lưu trong Visual Studio.

## Thay đổi trước kiểm chứng

- MinimumSize từ 800×560 xuống 640×400.
- Nhãn trạng thái rỗng Dock Top, tính chiều cao theo bề rộng thực tế;
  panel tự cuộn khi không đủ chỗ. Padding nhãn 12.
- Chọn hiển thị grid/nhãn theo số dòng, không đọc Visible khi Form chưa hiện.

## Các lượt và bằng chứng

Các đường dẫn run dưới đây nằm trong `../runs/` của nhóm G01.

| Lượt | Kết quả |
|---|---|
| G01_B09_20261009_123826 | Bounds DPI và bàn phím đạt, nhưng thông báo chưa kết nối bị cắt ở 150%; chưa giải quyết đầy đủ B09. |
| G01_B09_R2_20261009_124500 | Bounds đạt; nhãn/trạng thái hiển thị và cuộn chưa đúng. JSON scroll với handle không hợp lệ không được dùng làm chứng cứ PASS. |
| G01_B09_R3_20261009_125037 — trước lưu Designer | Debug/Release 0 warning/0 error; bounds đúng ở 100/125/150%; Tab/Shift+Tab đúng; nhãn cuộn được tại 150%. |
| R3 — lưu/đóng/mở lại Designer | Designer mở được, nhưng Save viết lại Designer.cs và resx: source-stability.json ghi **2 file đổi**. Không coi cả lượt R3 là source cố định. |
| G01_B09_Designer_20261009_131956 — sau lưu | Source cố định, bounds vẫn đúng ở ba DPI; hình thức grid **FAIL B10**, Debug build **1 warning CS0649**, 0 error. |

R1/R2 được kết thúc trước khi sửa tiếp, giữ iteration1/2-results.json.
R3 có manifest, screenshot Designer, designer-evidence.json và các states100/125/150.
Lượt sau lưu có manifest riêng, bản Designer/resx sau lưu, screenshot và source-stability.json.

## Kết quả chi tiết

Môi trường Windows, SDK 10.0.401; native GetDpiForWindow trả 96/120/144.
Windows Display Scale thật, không mô phỏng DPI. Host tạm mở StartupForm compiled,
bind 4 DTO **minh họa**; không dùng để chứng minh SQL hoặc app principal.
Các trạng thái thiếu cấu hình/lỗi kết nối chạy exe WinForms thật, kết nối lỗi
chủ động `127.0.0.1,1`, không ghi dữ liệu vào DB.

| Ca bổ sung | Expected | Actual | Status |
|---|---|---|---|
| Bounds 100/125/150%, mỗi mức 1280×720 và 1366×768 | Kích thước đúng | Đúng ở R3 và sau lưu Designer | PASS |
| Tab/Shift+Tab ở ba DPI, trước lưu | Input → nút tải → grid → nút tải → input | SendInput, focused PID đúng; grid đi tới dòng 4 bằng Down | PASS |
| Nhãn chưa kết nối 150%, 1280×720, trước lưu | Nội dung truy cập được | ScrollInfo valid; range max 131, page 108, pos 0→24; ảnh sau cuộn hiển thị câu cuối | PASS |
| Thiếu cấu hình/lỗi kết nối/loading ở ba DPI, trước lưu | Nút/trạng thái nhìn được, không che mất nội dung | Có native ui-evidence.json và PNG; nhãn rỗng không còn grid đè | PASS phần trước lưu |
| Designer save/close/reopen | Mở được và giữ cấu hình UI | Mở được, nhưng viết lại source, mất cấu hình grid; cần B10 | FAIL regression tổng thể |
| Hình thức grid sau lưu, 100% | Header tiếng Việt, màu theo guide | Header MaBan/TenBan/MaKV/TrangThai/MaHD, chọn xanh mặc định | FAIL B10 |
| Grid sau lưu, 150%, 1280×720 | Có ít nhất phần dòng grid truy cập được | Ảnh chỉ còn header, toolbar wrap chiếm thêm chiều cao | FAIL B10 |
| Build sau lưu | Không warning/error | 1 CS0649: components không được gán; 0 error | FAIL tiêu chí zero-warning |
| Minimize/restore riêng sau lưu | Layout phục hồi | Chưa chạy riêng | NOT RUN |

Ảnh đen lần capture đầu ở 100% không được dùng; capture lại sau resize có ảnh
hợp lệ. Máy đã trả **125% (Recommended)**, có restored-scale.json ở lượt sau lưu.
Lượt sau lưu không sửa source/migration trong test; manifest count 0.

## Kết luận nhóm

B09 sửa được giới hạn kích thước và nhãn trước lưu, nhưng regression Designer
phát hiện **B10 mới**, chưa được duyệt sửa. B09 chưa chốt trọn regression;
G01 vẫn **Đang làm**, 45/45 mã nghiệm thu nguyên bản PASS được giữ lịch sử.
Kết quả UI trước lưu không đại diện bản source hiện tại sau Designer serialization.
