# CoffeeManager — bản ứng viên G08

**Chưa nghiệm thu/bàn giao hoàn tất.** Xem package-manifest.json và verification/G08-report.md để biết phiên bản, checksum và giới hạn. Đừng dùng build/publish thành công để suy mọi ca đã PASS.

- App Windows x64, framework-dependent net10.0-windows, không cần Visual Studio khi chạy. Cài Desktop Runtime .NET10 x64 với bản vá từ10.0.13 trở lên đáp ứng runtimeconfig của package; installed runtime/version thực ghi manifest. SDK10.0.401 chỉ cần khi build source.
- SQL Server cài riêng. Môi trường phát triển đã kiểm trước đây SQL Server17.0.1000.7 Developer; kết quả G08 ghi môi trường thực của từng lượt, không cam kết mọi SQL/máy. Runtime không dùng quyền operator/sysadmin/db_owner.
- Đọc docs/operations/install.md: clean baseline→migrations001–027→bootstrap; demo baseline→seed duy nhất→migrations→bootstrap/review. Không chạy lại seed vào DB có dữ liệu.
- Đặt cấu hình runtime local, không đóng gói secret; app thường không tự chọn development. Không có bỏ qua Login. Setup dùng Windows identity CafeManagerSetup riêng và thu hồi sau bootstrap.
- Đọc docs/operations/backup-restore.md và security.md trước vận hành. Backup phía SQL Server, restore test vào DB mới; không copy MDF/LDF, không REPLACE DB đang dùng.
- Demo tại docs/operations/demo.md. Report doanh thu theo ngày thanh toán, thu/chi là quản trị, không lợi nhuận chính xác; giao hàng chỉ lưu hiện trạng. Không có API/hoàn tiền/đổi điểm.

Package chỉ chứa app/dependencies/resources/license, SQL, công cụ operator, hướng dẫn và báo cáo đã lọc. Source đang phát triển trong repo CoffeeManager; commit chỉ khi người dùng yêu cầu. Không kèm DB backup, cấu hình DPAPI, credential thật hoặc hồ sơ raw chứa dữ liệu riêng.

Trước dùng chính thức cần nghiệm thu T30/T31/T32 trên đúng package: clean/demo/restore, login/logout/phiên/quyền, bán/in/nhập/nợ/công/lương/report và kiểm bypass kể cả biến test cũ. Native/thiết bị/máy khác chưa thử phải công khai. Hình thức do người dùng kiểm;100/150%DPI chưa chạy không suy PASS.
