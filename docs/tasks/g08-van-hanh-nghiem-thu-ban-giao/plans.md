# G08 — Bộ plan T30–T32

02/11/2026. **Requirements đã được người dùng duyệt bằng “duyệt”; cả bộ plan đã được người dùng duyệt bằng “duyệt” ngày02/11/2026. Đã viết đầu ra triển khai tuần tự; đợt nghiệm thu G08 đã duyệt/chạy:16PASS/0FAIL/13BLOCKED; G08-B01 đã sửa/regression PASS.**

| Thứ tự | Plan | Đầu ra chuyển tiếp |
|---|---|---|
| 1 | [T30 — Backup/restore/cài sạch](t30-backup-restore-cai-sach/plan.md) | Bộ cài DB clean/demo, hướng dẫn operator, công cụ backup/restore và fixture có manifest |
| 2 | [T31 — Nghiệm thu xuyên suốt](t31-nghiem-thu-regression/plan.md) | Ma trận325ID/44UX-ID, runner và hướng dẫn E2E, dữ liệu đối chiếu |
| 3 | [T32 — Publish/bàn giao](t32-publish-ban-giao/plan.md) | Package win-x64, tài liệu và manifest đúng phiên bản; checklist kiểm bản cuối |

## Quyết định chung của bộ plan

- Dựa trên [requirements](requirements.md), kế hoạch mục11–13, UX01/UX20/UX-K44, source và migrations001–027. Không tạo migration028 theo dự phòng. Bất kỳ thiếu contract/đổi nghiệp vụ phát hiện trong triển khai đều ghi bằng chứng và trình phần phát sinh trước sửa.
- Tái sử dụng Apply-Migration.ps1 (transaction/applock/checksum), role CafeManagerApp/CafeManagerSetup, BootstrapForm/SetupRepository và các probe nhóm trước. Không sửa checksum migration cũ, không mở lại AppSetupState.Completed. Đọc lại SQL liên quan trước triển khai từng bước.
- Công cụ vận hành chạy bởi operator ngoài ứng dụng, không thêm màn quản trị SQL/backup/restore hoặc thay UI. Kết nối runtime hiện dùng CAFEMANAGER_APP_CONNECTION; --setup --server --database dùng Windows identity riêng đã được cấp CafeManagerSetup. Hướng dẫn đặt cấu hình trong phiên local, không ghi giá trị bí mật vào lệnh lưu evidence/log. DevelopmentConnection chỉ dùng khi chọn cờ development rõ ràng.
- Publish mặc định **win-x64/framework-dependent, không single-file/trim**, giữ PerMonitorV2 và resource/license. Cần Desktop Runtime .NET10 bản vá đáp ứng README của source; chốt phiên bản thực lúc tạo package. Không tự cập nhật SDK/NuGet/runtime hoặc tải phần mềm ngoài phạm vi. Máy khác chưa có để thử thì ghi NOT RUN, không suy PASS từ cùng máy.
- Không tự chạy nâng DB CoffeeManager_Development. T30 backup nguồn cố định này chỉ sau xác minh DB/marker và đường dẫn SQL Server; restore luôn sang DB test mới. Không đổi SQL mode, service, registry, Windows Scale hoặc tạo Windows user. Không thêm quyền sysadmin/db_owner cho runtime. Không tự commit/push.
- G07 có đầu ra T29 nhưng còn9ID BLOCKED; các nhóm trước cũng còn ca thiếu. Cho phép chuẩn bị/triển khai đầu ra G08 theo thứ tự, không nghiệm thu thay nhóm trước. T31-K10/T32 và G08 chưa PASS khi phần bắt buộc chưa đủ bằng chứng. Nếu thiếu đầu ra kỹ thuật ngăn bước sau thì ghi blocker và xin quyết định, không dựng đầu ra giả.
- Pha triển khai chỉ build, kiểm parse/compile và smoke tối thiểu đầu ra chuyển tiếp, ghi technical-checks.md. T31 vốn là task nghiệm thu: pha triển khai chỉ chuẩn bị runner/kịch bản/ma trận; thực thi E2E và kiểm publish nằm trong **đợt test G08 được duyệt riêng sau triển khai cả nhóm**. Có thể tạo package ứng viên trước nghiệm thu; chỉ bàn giao chính thức sau T31 và T32 đạt điều kiện.
- Nghiệm thu giữ source/migration/harness cố định, snapshot SHA256 trước/sau; mỗi subcase có expected/actual riêng. FAIL không tự sửa giữa lượt; bug tại task sở hữu, duyệt cả đợt trước debug/regression. Báo cáo chung kể cả còn BLOCKED/NOT RUN.
- Hình thức SKIPPED theo phân công người dùng; chỉ chức năng/layout kỹ thuật125%, không đổi Scale;100/150% chưa chạy NOT RUN. Đọc trọn bộ docs/ui theo thứ tự bắt buộc trước bất kỳ phát sinh thiết kế/sửa UI; việc đó không được mặc định nằm trong plan vận hành này.

## Bằng chứng và tài nguyên

Runner mới Test-G08.ps1 có chế độ Technical và RunAcceptance loại trừ nhau; acceptance không gọi ngầm từ build/publish. Mỗi lượt có runID, server, DB nguồn/đích, marker, principals, version/checksum, source/package hash, file tạm, thời gian và kết quả. Dùng SQL Server thật, DB test riêng; SqlClient của sản phẩm trong probe, helpers PowerShell chỉ dùng orchestration theo hiện trạng.

DB tạo mới dạng CoffeeManager_Test_G08_<runID>_<purpose>; ghi allowlist trước CREATE, kiểm không tồn tại, đặt marker ngay sau tạo. Restore cần manifest đích mới, xác minh file backup/logical files rồi MOVE tới file mới; không WITH REPLACE. Marker sao chép từ backup không đủ chứng minh ownership đích: phải kiểm tên đích, manifest tạo của lượt, danh sách file vật lý và marker sau restore. Cleanup chỉ exact name+ownership, không wildcard; báo tạo/dọn/giữ, kể cả FAIL. File backup ở thư mục SQL được chọn có quyền service, không commit; chỉ lưu metadata/hash không nhạy cảm trong hồ sơ.

## Trình tự sau duyệt cả bộ

1. T30: viết hướng dẫn/công cụ và xác minh kỹ thuật tối thiểu cho DB clean/demo, không nâng development.
2. T31: chuẩn bị ma trận kế thừa, fixture/runner và transcript thao tác native; không ghi PASS từ kế hoạch.
3. T32: tạo package ứng viên và tài liệu/manifest, quét thành phần được đóng gói; chuẩn bị đợt test chung.
4. Trình duyệt đợt test G08 một lần; chạy T30→T31→T32, test-results từng task/report chung, xử lý bug theo gate riêng.

Không thay29ID G08 hoặc325ID toàn dự án. Ba plan chứa ánh xạ đủ mã ca; nội dung gốc giữ tại requirements mục6 và kế hoạch.




