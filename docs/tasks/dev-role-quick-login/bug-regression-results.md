# Regression DQ-B01 — 09/11/2026

**DQ-B01 đã sửa theo duyệt và PASS trong phạm vi regression:57/57 subcase,142/142 unit.** Không còn bug mới phát hiện trong lượt này; tính năng tổng thể chưa nghiệm thu vì các nhánh native còn thiếu bằng chứng.

## Thay đổi

Chỉ thêm `InvalidDataException` vào filter `IsStorageError` của `src/CafeManager.WinForms/Configuration/DevelopmentQuickLogin.cs`. Get/Save/Delete nay dùng thông báo Việt hiện có khi envelope không hợp lệ. Không đổi format/version, quyền, AuthService, SQL hoặc dữ liệu development; không tự xóa cấu hình lỗi.

Người dùng duyệt sửa DQ-B01 kèm regression bằng “duyệt”. Giữ nguyên raw FAIL của đợt nghiệm thu trước.

## Môi trường và nguồn thực thi

Run:[regression_b01_20261109_211932_790ab603](runs/regression_b01_20261109_211932_790ab603/manifest.json). Windows, SDK10.0.401, SQL Serverlocalhost17.0.1000.7, DB test riêng `CoffeeManager_Test_G08_20261109_211932_790ab603_demo`, demo baseline và migrations001–027 qua Install-Database.ps1. Install marker được đối chiếu trước sử dụng/dọn. Runtime SQL dùng DQRuntime WITHOUT LOGIN/CafeManagerApp qua SessionInitializer; owner chỉ provision và đối chiếu.

WinForms thật trong harness ở DeviceDpi120=125%. Control API/PerformClick được ghi riêng với native. Không đổi Windows Scale, không kiểm hình thức.

`source-before.csv`, `source-after.csv`, `source-audit.json`: **SourceUnchanged=true và HarnessUnchanged=true** trong lượt regression. Debug DLL harness hashDBDBF930112C55F001A1C66780B98F316C71C3D0A5BBD865FF620C029292DF02. DLL Debug ở đường F5 mặc định giống nguyên byte với DLL đã kiểm. Release/publish hashF5E6C6E2736EEFC2EADBAFD30A39109A6A65D18AD61DED0DFE0C40AC04D5D424.

## Ca đã chạy

| Nhóm | Dữ liệu/thao tác và expected | Actual |
|---|---|---|
| 19 subcase đợt cũ | Lưu/thay/xóa/scope/ciphertext, gate Debug, auth ba role và quyền, sai password/khóa/nghỉ/sai role, login thường, thu hồi phiên, SQL chờ/double request/đóng, dialog lưu | **19/19 PASS**, gồm CorruptAndVersion từng FAIL |
| 33 ca Get/Save/Delete |11 payload lỗi ×3 thao tác: ciphertext hỏng, Version2, server khác, DB khác, AccountsNULL, rootNULL, JSON sai, role lạ, username rỗng, password rỗng, file65537byte | **33/33 PASS**: InvalidOperationException có thông báo Việt đúng, không inner exception; file cũ còn nguyên theo SHA256 và không temp bị sót |
| Login lỗi cấu hình | Ba nút role đọc file Version2; expected không LoggedIn, hiện lỗi, nút vẫn dùng được | **3/3 PASS**, form/control thật qua API |
| Dialog lỗi lưu/xóa | Auth CASH hợp lệ, file Version2; bấm Lưu/Xóa bằng PerformClick | **2/2 PASS**, giữ username/password, hiện lỗi, mở lại nút, file không đổi; không báo lưu/xóa thành công giả |
| Unit regression | Chạy unit; không kéo integrationT05/G02 đòi fixture riêng | **142/142 PASS** |
| Debug/Release | Build solution tuần tự với output riêng; build WinForms Debug ở đường mặc định F5 | **0 warning/0 error** ở cả ba lượt |
| Publish/gate | Publish Release mới, gọi factory trên DLL package với đủ hai cờ và target development | **PASS**, factory=null/defaultLogin0nútDEV; DLL publish=Release |

Raw:[57 record](runs/regression_b01_20261109_211932_790ab603/regression-results.json), [unit TRX](runs/regression_b01_20261109_211932_790ab603/unit/unit.trx), [publish gate](runs/regression_b01_20261109_211932_790ab603/release-publish-gate.json). Không cộng subcase/unit thành325ID nghiệm thu dự án.

Lệnh build dùng `-m:1 -nr:false -p:UseSharedCompilation=false`; unit `dotnet test … --no-build --no-restore --filter "FullyQualifiedName!~CafeManager.Tests.G02&FullyQualifiedName!~CafeManager.Tests.Integration"`. DB/install, probe source, process exit và build log lưu trong thư mục run.

Publish đầu thiếu chế độ tuần tự dừng exit1 không output; giữ publish.log. Lượt tuần tự `dotnet publish … --no-build --no-restore -m:1 -nr:false` thành công, logpublish-serial.log. Không thay source/expected để xử lý lần gọi công cụ này. Harness chuẩn bị xong trước khi chạy và không chỉnh trong lượt.

## Giới hạn và tài nguyên

Native chuột/phím/modal và logout đổi role đầu cuối chưa chạy lại; blocker geometry/focus của đợt trước vẫn giữ, không suy PASS từ component. Windows user thứ hai chưa kiểm. Hình thức SKIPPED; DPI100/150%NOT RUN. Không nghiệm thu thay G08/T32 hoặc các nhóm trước.

[Resource audit](runs/regression_b01_20261109_211932_790ab603/resource-audit.json): **1 DB tạo/1 dọn/0 còn,0 process fixture, không còn file DPAPI fixture**. Không tạo Windowsaccount/SQLlogin, không thay development. Không commit/push. DQ-B01 đóng theo phạm vi regression; trạng thái tính năng vẫn Đang làm do phần native chưa đủ.
