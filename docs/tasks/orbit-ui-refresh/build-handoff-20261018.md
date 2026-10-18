# Đối chiếu bản chạy cũ — 18/10/2026

Người dùng gửi ảnh Login nâu–kem, input trải rộng và status cao. Ảnh khác source
Orbit hiện tại; không sao chép thông tin tài khoản nhìn thấy trong ảnh vào hồ sơ.

## Bằng chứng

- LoginForm hiện gọi OrbitField.Wrap và OrbitContentHost480, AppTheme dùng nền tối.
- Trước xử lý: DLL chuẩn Debug có thời gian17/10/2026 16:58:44,
  SHA256 DA608995386C9DE52989F1C7AA4EE39D9F2308BCE2CCA789EB1DB76DF97EFA85.
- DLL Release chuẩn:17/10/2026 18:01:35, trong khi bản delivery-release riêng là
  17/10/2026 19:18:57. Các vòng build trước dùng -o thư mục kiểm tra.
- Không lấy được process/path của cửa sổ trong ảnh: CIM bị từ chối, Get-Process
  không trả process phù hợp. Vì vậy chưa chứng minh đường dẫn exe người dùng mở.

## Xử lý bàn giao

Đã build lại project WinForms tại đầu ra chuẩn, không dùng -o:

`dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj -c Debug --no-restore -m:1 -p:UseSharedCompilation=false -v minimal`

Lặp với Release. Cả hai PASS,0warning/error. Metadata DLL Debug mới có
OrbitContentHost và OrbitTimeInput; timestamp18/10/2026 13:13:46.
Không sửa source ứng dụng, DB hoặc cấu hình kết nối; không commit.

Đầu ra chạy chuẩn:
`src/CafeManager.WinForms/bin/Debug/net10.0-windows/CafeManager.WinForms.exe`.
Thông báo chưa cấu hình kết nối trong ảnh là trạng thái riêng, không phải nguyên
nhân đổi theme; chưa tự thay connection string hoặc nhập tài khoản xác thực.

Chưa mở và quan sát lại executable chuẩn bằng GUI trong lượt này; build/metadata
không thay thế chứng cứ GUI. Các giới hạn nghiệm thu trước vẫn được giữ nguyên.
