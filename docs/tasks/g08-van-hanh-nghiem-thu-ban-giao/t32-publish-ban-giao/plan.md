# T32 — Publish và bàn giao hoàn chỉnh

02/11/2026 · **Đã duyệt cả bộ G08 ngày02/11/2026**. Phụ thuộc T31; requirements đã duyệt. Pha triển khai tạo package ứng viên, nghiệm thu và phát hành chính thức phải đạt T31/T32 và điều kiện G08. Tuân thủ [plans.md](../plans.md).

## 1. Quyết định đóng gói

- Target net10.0-windows, RID win-x64, Release/framework-dependent; không trim/single-file/ReadyToRun hoặc đổi DPI. Dùng SDK global.json hiện có, không tự nâng thư viện. Không lựa chọn self-contained trong bộ plan này; nếu người dùng cần sẽ cập nhật phạm vi và test runtime tương ứng.
- Publish chỉ project src/CafeManager.WinForms/CafeManager.WinForms.csproj, không cả solution/probe. Bundle gồm app và dependencies/resource/licenses, database baseline/migrations001–027 theo hash, security scripts, hướng dẫn operator/demo, manifest và bản tóm tắt kết quả/giới hạn. Không kèm tests/probe/development secrets, DPAPI, config local, .git, bin/obj cả repo, backup/MDF/LDF hoặc evidence raw có dữ liệu riêng.
- Output dưới artifacts/g08/<runID>/package và candidate.zip (đã được artifacts ignore); manifest công khai trong hồ sơ task chỉ ghi đường dẫn tương đối, hash, phiên bản thực, không đường dẫn máy tác giả. Dùng allowlist từng nguồn, không Copy-Item toàn workspace. Không commit package/binary nếu chưa được giao.
- Runtime cấu hình bằng CAFEMANAGER_APP_CONNECTION trong môi trường local của tiến trình, không embed credential hoặc tự chọn development. --setup vẫn dùng Windows identity được cấp quyền, không password commandline. Launch hướng dẫn trung tính không bật bypass/development; biến test cũ không có tác dụng. Không thay source config/UI để phục vụ đóng gói.

## 2. Bước triển khai và file

| Bước | File | Nội dung và kiểm kỹ thuật |
|---|---|---|
| 1. Chốt inventory | tools/release/Publish-G08.ps1 | Nhận output thuộc artifacts/g08 và runID; kiểm path tuyệt đối trong root đích trước thao tác file; output mới, không xóa đè package cũ. Ghi SDK/OS/SQL schema expected/source hash, kiểm bộ migrations liên tục/checksum |
| 2. Build/publish | Cùng script; csproj giữ nguyên | dotnet build CafeManager.slnx -c Debug; Release; dotnet publish src/CafeManager.WinForms/CafeManager.WinForms.csproj -c Release -r win-x64 --self-contained false -o <newoutput>. Lưu exitcode/log; không chạy test ngầm. Nếu restore cần mạng/quyền không có, báo blocker thay vì thay SDK/package |
| 3. Bundle allowlist | package-manifest.json; docs/operations/* từ T30 | App output và license hiện có, schema/seed demo riêng/migrations/security/install/backup-restore/security. Seed demo đặt khu demo có nhãn, không chạy khi clean. Ghi executable/runtimeconfig/deps/migrations SHA256 và file list. Không ghi connection string |
| 4. Tài liệu bàn giao | README.md; docs/operations/demo.md, release.md; database/demo/README.md | Cập nhật chỉ mục trạng thái thật theo report mới, không sửa lịch sử. Nêu SQL cần cài riêng, Desktop Runtime .NET10 x64/version thực, clean vs demo, config/bootstrap/revoke, backup/restore, chạy ngoài VS, giới hạn giao hàng/report và cách lấy log an toàn. Demo đi đủ5luồng T31, phân vai và ngày filter đúng |
| 5. Audit bypass và bí mật | tools/release/Test-ReleaseInventory.ps1; bypass-removal-checklist.md trong task | Quét source/hooks/launcher/profile/config và type/string DLL Debug/Release/publish bằng metadata/probe đọc assembly; positive controls LoginForm/AuthService còn. Phân biệt hồ sơ lịch sử được giữ với đường chạy hiện hành. Quét allowlist package không credentials thật/local path; không in chuỗi nghi bí mật vào evidence |
| 6. Chuẩn bị chạy độc lập | native-publish-guide.md trong task | Unzip vào thư mục mới artifacts/g08/<runID>/run-publish, xác minh hash; môi trường con tách profile/connection test; giữ môi trường cha. Không cần VS; thử với Desktop Runtime thực, DB demo/clean/restore của lượt. Máy khác chưa thử ghi NOT RUN |
| 7. Hồ sơ và tổng hợp | technical-checks.md; ../test-plan.md; sau test test-results.md và ../report.md | Ghi build/publish kỹ thuật riêng; tổng hợp29ID và điều kiện bổ sung, link325ID/44UX. Chỉ phát hành bản khớp source đã nghiệm thu; sửa source/migration sau đó phải tạo ứng viên mới và kiểm lại theo ảnh hưởng |

Không migration mới hoặc sửa UI. Nếu phát hiện đường bypass còn tồn tại trong sản phẩm hiện hành, ghi bằng chứng/bug đúng phạm vi và trình đợt sửa; không xóa lịch sử hoặc đơn thuần ẩn nút để vượt gate. Checklist gỡ có nguồn tại ../../temporary-test-login/README.md và removal-results.md; kết quả24/10 không chứng nhận package cuối.

## 3. Đợt nghiệm thu sau duyệt riêng

| ID | Thao tác/dataset | Expected/bằng chứng |
|---|---|---|
| T32-K01 | Publish Release win-x64, giải nén/chạy thư mục mới không mở VS | Đúng file hash/RID, Login/MainForm mở và chức năng hoạt động; ghi OS/kiến trúc/thư mục tương đối. Máy khác thiếu bằng chứng ghi rõ, không gọi cùng máy là máy khác |
| T32-K02 | Ghi dotnet --list-runtimes và runtimeconfig; chạy exe framework-dependent | Desktop Runtime đúng .NET10/bản vá đáp ứng README thực tế, app chạy thật. Self-contained không chọn, ghi N/A có lý do lựa chọn, không tuyên bố đã test |
| T32-K03 | Package + DB clean bootstrap thật; DB demo fixture riêng; login/bán/in | Không mật khẩu mặc định clean; login bằng tài khoản được tạo, đủ quyền; bán và in/preview theo thiết bị thật có sẵn. Thiếu máy in ghi riêng phần chưa chạy, không suy in giấy từ preview |
| T32-K04 | Audit inventory/archive/config/license/evidence | Không secret thật, backup/dữ liệu nhạy cảm hoặc absolute path máy tác giả. Demo credential mock ghi nhãn, không cấp runtime SQL qua mock |
| T32-K05 | Trên exe package: bán/checkout/nhập/nợ/công/lương/report | UI thao tác thật và snapshot nguồn SQL đúng; không thay bằng Debug/probe. Gắn MaHD/RequestId/nguồn vào transcript |
| T32-K06 | Package trỏ DB restore T30 bằng runtime identity được map đúng | Login/use case/report đọc dữ liệu khôi phục đúng, không sửa source/quyền rộng. Hash package không đổi; đối chiếu dữ liệu trước/sau các thao tác |
| T32-K07 | Tái lập hướng dẫn clean/demo trên DB mới và kiểm manifest version | Baseline→migrations→dữ liệu nền hoặc nhánh seed demo trước migrations ghi đúng; SQL/runtime/schema và checksum khớp bản được chạy, không áp hướng dẫn development003 cũ cho bản027 |
| T32-K08 | Audit source/status/test-results/migration inventory | Từng nhóm công khai kết quả thật, bug/ca chưa chạy; không tự tick Hoàn tất từ publish. Chưa đủ ca bắt buộc thì T31/G08 chưa PASS |
| T32-K09 | Chạy demo5luồng theo tài liệu với report đúng ngày/source | Kịch bản đi trọn nguồn và quyền, không quảng cáo hoàn tiền/lợi nhuận chính xác/API; giao hàng chỉ hiện trạng theo T18 |

UX-K44→K03/K04/K06/K07;UX-K01–04→K03 và regression phiên dưới đây; UX-K19/K23→K03/K05, UX-K37→K05/K09. Giữ source/package cố định trong lượt, không patch file exe/config đóng gói để né lỗi.

## 4. Gate bổ sung bắt buộc: hết bypass trên bản cuối

Bổ sung09/11/2026: người dùng duyệt tiện ích DEV đăng nhập nhanh bằng tài khoản thật. Gate cuối phải gỡ cả tiện ích mới theo [checklist](../../dev-role-quick-login/README.md), gồm helper/dialog/UI, dependency Program/ApplicationSessionContext/LoginForm, entry point host chỉ dùng cho DEV và cờ profile. Debug-only không thay nghĩa vụ gỡ source. Chạy lại build/publish và regression phiên/quyền trên bản cuối; không đổi325ID hoặc lịch sử nghiệm thu.

1. Source hiện hành không còn nút “Bỏ qua đăng nhập — TEST”, helper tạo session không xác thực, hooks Program/ApplicationSessionContext/LoginForm, profile/launcher/cờ vận hành. Quét có allowlist lịch sử cụ thể, không loại toàn bộ tools để bỏ sót launcher.
2. DLL Debug/Release/publish không type/flag/helper bypass; positive controls auth bình thường còn. Không coi zero keyword là bằng chứng đầy đủ: kiểm đường gọi thực và UI.
3. Chạy package với các tên biến test cũ lấy từ checklist lịch sử đã đặt ở tiến trình con; vẫn phải Login, không tạo phiên. Không log secret hoặc sửa môi trường Windows toàn cục; khôi phục môi trường con/đóng tiến trình sau ca.
4. Native login đúng/sai, logout/relogin; khóa/thu hồi phiên/đổi role trên DB test bởi session operator, ADMIN/CASH/STORE và callback sau đổi phiên. Đối chiếu BLL chặn quyền/phiên cũ; không chỉ nhìn nút ẩn. Lưu expected/actual từng subcase trong test-results.
5. Nếu bản cuối có đổi source sau kiểm, audit/build/publish và regression liên quan chạy lại trên package mới; trước đó không chốt G08.

## 5. Giới hạn và kết luận bàn giao

Agent kiểm chức năng/layout125%, không test hình thức/ảnh/hover/Designer; hình thức SKIPPED,100/150% NOT RUN. Phiên native hoặc thiết bị/máy đích thiếu thì giữ BLOCKED/NOT RUN, không suy từ build. Package có thể được bàn giao dưới nhãn ứng viên với giới hạn để xem xét, nhưng không gọi hoàn tất G08 khi T31/T32/gate bypass chưa đạt. Không commit/push hoặc tự publish lên dịch vụ ngoài.

