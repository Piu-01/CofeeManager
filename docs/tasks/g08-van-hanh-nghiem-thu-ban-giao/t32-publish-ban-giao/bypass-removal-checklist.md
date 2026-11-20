# Gate bản cuối — không bypass Login

Lịch sử gỡ24/10 giữ tại ../../temporary-test-login/README.md và removal-results.md. Không thêm lại tiện ích trong G08. Checklist này không thay9ID T32 hoặc29ID G08.

| Kiểm tra | Trạng thái triển khai |
|---|---|
| Source/hooks/launcher/profile hiện hành không type/nút/helper bypass | Audit kỹ thuật tự động đã có; xem inventory-audit của ứng viên cuối |
| Metadata type DLL Debug/Release/publish không helper bypass, có positive LoginForm/AuthService/AuthRepository | Audit kỹ thuật tự động đã có; không thay native |
| Package không config/credential thật/local path, kể cả PDB CodeView | Publish PathMap và bỏ symbol, audit PE debug directory; xem ứng viên cuối |
| Cờ test cũ còn trong môi trường tiến trình con vẫn phải Login | NOT RUN — thuộc đợt test được duyệt riêng |
| Login đúng/sai, logout/relogin, khóa/nghỉ việc/đổi role/password/thu hồi phiên và BLL3role trên package | BLOCKED — native ADMIN đúng/sai/logout và SQL quyền/khóa phiên đã chạy; các phần còn thiếu theo test-results, không suy từ type scan |
| Source/package sau kiểm không đổi, đúng manifest/checksum | Chốt theo lượt nghiệm thu, hiện chưa nghiệm thu |

Nếu không truy ra tên biến cũ từ hồ sơ còn lại, ghi thiếu input/bằng chứng, không tự bịa tên rồi coi đã kiểm đầy đủ. Profile hiện chỉ còn CoffeeManager.Development; không tự đổi cấu hình Windows để mô phỏng cờ cũ. G08 chưa PASS đến khi kiểm lại bản cuối đủ bằng chứng.

