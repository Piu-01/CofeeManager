# T07 — Kiểm tra kỹ thuật trong triển khai

09/10/2026: `dotnet build CafeManager.slnx -c Debug --no-restore` thành công,
0 warning/0 error. Chạy exe WinForms với CAFEMANAGER_APP_CONNECTION absent:
process có cửa sổ Login “CoffeeManager · Đăng nhập”, mở/đóng được.

Đầu ra shell/session/theme/navigation đã compile để T08 nối control thật.
Smoke này không kiểm login3role, Designer, DPI, nháp/lifecycle hoặc event count;
không đánh dấu PASS T07-Kxx. Ma trận nghiệm thu vẫn NOT RUN/chờ duyệt nhóm.

T08 đã nối CatalogsControl thật; ADMIN bắt đầu ở Danh mục, CASH/STORE ở Trang
phiên. Build source cuối tại `../runs/build_20261009_212611_777cf7c4/`, Debug và
Release đều0warning/0error. Native3role/lifecycle/Designer/DPI vẫn chưa nghiệm thu.
