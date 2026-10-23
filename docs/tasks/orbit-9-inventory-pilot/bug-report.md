# B09 — Avatar cũ còn lại trên sidebar khi resize

Người dùng báo ngày21/10/2026, ảnh `C:/Users/Lenovo/AppData/Local/Temp/codex-clipboard-0e89e34f-7f13-4b88-8cbe-08c39f5c93f6.png`, mức100%, cửa sổ maximized. Có một avatar ở cạnh profile cuối sidebar và một avatar cũ gần y746.

Source OrbitSidebar.OnPaint chỉ vẽ một ellipse theo Person.Top. OnLayout di chuyển Person/Role theo Height nhưng không invalidate nền custom; constructor chưa bật ResizeRedraw. Vùng cũ không còn child control ở đó để yêu cầu xóa avatar. Bằng chứng phù hợp stale paint khi đổi chiều cao, không phải tạo hai control avatar.

Sửa trong scope tự hiệu chỉnh hình thức pilot đã được người dùng giao: bật ResizeRedraw và yêu cầu vẽ lại sidebar sau layout để xóa vị trí cũ. Chỉ OrbitSidebar.cs, không đổi bounds/nút/handler/backend. Không mở rộng tám màn. Rủi ro: repaint sidebar nhiều hơn khi layout, không tạo timer hay repaint toàn Form.

Regression: build, kiểm invalidation vùng avatar cũ khi tăng chiều cao bằng control sở hữu; native restore/maximize/restore ở100%, chụp trước/sau. Chưa có ảnh native sau sửa thì không ghi PASS native. Giữ lịch sử R8/test FAIL, lượt mới source cố định R9. Hướng sửa nằm trong yêu cầu gốc tự sửa lệch hình thức, không cần gate duyệt lại phạm vi đã giao.

## Kết quả B09

PASS đúng lỗi avatar ở100%: R9 Debug/Release build sạch; probe DeviceDpi96 xác nhận vùng avatar cũ được invalidate. Người dùng đăng nhập thủ công, mở inventory, từ cửa sổ restore phóng to1920×1032 rồi thu về1368×800 (bao gồm khung/titlebar). Quan sát trước, sau và sau khi ổn định đều chỉ một avatar cạnh profile; vị trí cũ y746 không còn vết. Ảnh `runs/native-avatar-r9-restored-before-100-0.png`, `runs/native-avatar-r9-maximized-100-settled-0.png`, `runs/native-avatar-r9-restored-after-100-0.png`. Source R9 Drift0. Không suy rộng thành toàn bộ DPI/UX của pilot đãPASS.

# B10 — Người dùng báo mờ, yêu cầu kiểm DPI trong Program.cs

Program.cs chỉ gọi ApplicationConfiguration.Initialize, không có125%. MainForm/MaterialsControl dùng AutoScaleDimensions96. csproj chưa khai báo ApplicationHighDpiMode. StartupForm.Designer còn120 nhưng không được tạo trong luồng ApplicationSessionContext hiện tại, nên không quy lỗi màn inventory cho giá trị đó và không sửa form ngoài phạm vi.

Theo yêu cầu chỉnh cấu hình khởi động, khai báo ApplicationHighDpiMode=PerMonitorV2 trong csproj, Program.cs ghi rõ chuẩn96/100% và nguồn cấu hình. ApplicationConfiguration sinh lệnh DPI trước khi tạo HWND; không thêm hai SetHighDpiMode mâu thuẫn. Không dùng DpiUnaware để ép100 vì Windows có thể kéo giãn bitmap ở màn DPI cao. Nguồn kỹ thuật: https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.highdpimode?view=windowsdesktop-10.0

Đây là sửa cấu hình DPI toàn process theo yêu cầu mới của người dùng, không đổi business. Chưa chứng minh đây là nguyên nhân duy nhất ảnh mờ: chất lượng ảnh công cụ/ảnh preview và quá trình đổi Scale giữa phiên cũng có thể ảnh hưởng. Cần kiểm startupDPI96 và ảnh native sau mở lại, sau đó kiểm chuyểnDPI trước khi kết luận hết mờ. Giữ kết quả R9; lượt mới R10 cố địnhsource.
