# Kiểm chứng màn mẫu Orbit 9

Trạng thái: đang kiểm chứng, chưa nghiệm thu toàn bộ. Ngày 21/10/2026.

Môi trường: Windows, .NET SDK 10.0.401, WinForms net10.0-windows; SQL Server localhost, DB riêng `CoffeeManager_Test_G03_20261019_215457_29e552a9_demo`, marker `20261019_215457_29e552a9`, schema đến migration 009. Không ghi thử vào DB sử dụng thật. Retention manifest đến 21/10/2026, đang giữ cho review màn mẫu.

| Ca | Thực hiện / actual | Kết quả và bằng chứng |
|---|---|---|
| Build R4 | Debug và Release, 0 warning / 0 error | PASS: `runs/build-r4-debug-final.log`, `runs/build-r4-release-final.log` |
| Rules | xUnit G03.RulesTests và Inventory.MaterialRulesTests | 20/20 PASS: `runs/unit-final.log`, `runs/unit.trx`; không phải toàn bộ integration suite |
| Native tìm/lọc/phân trang | App R1 trên DB test: tìm NVL02, lọc Lit ra NVL02/NVL09; hai trang 1–7 và 8–14 | PASS tại R1: các ảnh `runs/native-*-r1-125-0.png`; cần phân biệt với ảnh R4 cuối |
| Native action | Mở/hủy Thêm, Sửa, Mở sổ, Xóa; lưu form thêm trống nhận validation | PASS luồng mở/hủy và validation tại R1; không tính là GUI CRUD hợp lệ đã chạy |
| Typed paging | Snapshot DTO 0/1/7/8/14/49/50/51/99; không lặp/mất dòng, tối đa 7/trang, khóa biên | 9/9 PASS trong process sở hữu: `runs/probe-r3-search.json`; không phải DB thật có 99 dòng |
| Search lỗi/rỗng/mã | Input 101 ký tự giữ snapshot và báo chưa cập nhật; mã không tồn tại ra empty; NVL02 ra một dòng | 3/3 PASS trong process: `runs/probe-r3-search.json` |
| Role | ADMIN/CASH/STORE, tải dữ liệu và kiểm visibility control | PASS probe trong process: `runs/probe-r3-search.json`; chưa nghiệm thu GUI đăng nhập riêng từng role |
| Resize | Host thu còn 1050×530; footer nằm trong host, row không giãn | PASS probe R3; native resize/scroll cuối chưa đủ bằng chứng |
| CRUD regression | Tạo/đổi/xóa fixture NVL99 bằng BLL; thử mở sổ lại NVL01 bị chặn, số lượng và revision giữ nguyên | 5/5 PASS: `runs/crud-regression.json`; fixture đã dọn qua BLL, receipts giữ nguyên; không phải thao tác GUI lưu |
| Hover R4 | Vẽ control grid thật, vào/rời dòng, pixel trở lại Surface | PASS: `runs/probe-r4-hover.json`; DrawToBitmap trong process, không thay thế ảnh desktop |
| DPI 125% | App thật R1/R3, probe DeviceDpi 120; ảnh có đủ menu/actions | Có bằng chứng; R3 còn hover trail đã sửa ở R4. Chưa dùng ảnh R3 để tuyên bố R4 đạt |
| DPI 100% | Người dùng đổi Windows; probe R4 DeviceDpi 96, MainForm mặc định client 1366×768, sidebar 200. App thật R4 đăng nhập admin, Restore về kích thước mặc định, ảnh toàn cửa sổ 1368×800 gồm khung/title bar | Đã chụp: `runs/native-inventory-r4-100-0.png`; probe `runs/probe-r4-native100.json`. Không dùng fixture96 ảo hóa cũ làm bằng chứng này |
| Native R4 search/filter/paging | NVL02 ra 1 dòng; xóa tìm tải lại 14; chọn mục Lit trong dropdown ra NVL02/NVL09; trang 2 ra NVL08..NVL82, range 8–14/14, Sau khóa | PASS: `runs/native-search-r4-100-0.png`, `runs/native-filter-lit-r4-100-0.png`, `runs/native-page2-r4-100-0.png` |
| DPI 150% R4 | Probe DeviceDpi144; app thật thay DPI từ 100 sang150, ảnh 1895×994 bị giới hạn vùng màn hình | Đã chạy, phát hiện profile sidebar xuống dưới viewport; FAIL hình thức, không chốt PASS. `runs/native-inventory-r4-150-0.png` |
| Cuộn native R4 150% | Chọn NVL02, cuộn bảng từ value0 đến138; dòng trên đổi NVL01 thànhNVL04; header/tools/pager giữ vị trí | PASS: `runs/native-grid-scroll-r4-150-0.png`; không suy thành native resize PASS |
| R8 sidebar/biên/hover 150% | Fixture sidebar đúng TopDown, tám nút+logout, viewport630logical; profilebottom940 trong client945; pixel biên và hover đúng màu | 3/3 PASS trong process: `runs/probe-r8-native150.json`. R5/R6/R7 FAIL giữ riêng; native ảnh R8 đang chờ đăng nhập |
| Motion/keyboard đầy đủ | Có code hover/press; chưa có video/đủ chuỗi giữ-thả-ngoài, Enter/Space và lướt nhanh | NOT RUN đầy đủ; không suy PASS từ ảnh tĩnh |
| Shell regression | Diff không đổi tám module; sidebar thực tế có đầy đủ mục theo admin | Chưa smoke toàn bộ navigation/leave/logout; không ghi PASS toàn bộ |

Các lần build lỗi và FAIL hình thức giữ trong `runs/r1-results.md` cùng raw logs. Build sandbox trả FAILED không có chẩn đoán đã chạy lại ngoài sandbox và thành công; không tính lần thất bại là PASS. Probe R3 và GUI R1 là bằng chứng phiên bản trước; R4 cần ảnh native cuối riêng.

Lượt R4 native 100%: thao tác kéo cạnh resize qua công cụ không thay đổi kích thước thực tế; không ghi ca native resize/scroll là PASS. Ảnh có halo xanh quanh con trỏ do công cụ thao tác, không phải hiệu ứng bụi sao của app. Accessibility tree có lúc còn snapshot màn trước; quyết định thao tác theo ảnh mới và chỉ dùng index sau khi tree đã cập nhật. SetValue ComboBox chỉ đổi text, chưa chọn mục; test lọc chỉ tính sau khi bấm đúng list item Lit và xác nhận hai dòng thực tế.

Đối chiếu hình thức R4: panel/header/search/action/footer nằm theo anchors mẫu, dòng cố định và đủ nút. Chưa khẳng định pixel-identical: ComboBox vẫn có phần native/selection xanh, còn đường phân ô dọc mảnh trong ảnh, thương hiệu CoffeeManager và dữ liệu DB khác nội dung minh họa. Các điểm này được ghi để review, không che bằng ảnh HTML.

Sau lượt R4 đã tự sửa trong scope được giao, build R8 Release0warning0error (`runs/build-r8-release.log`), Debug được build bởi probe R8. Grid border dọc đã đạt pixel probe sau khôi phục GraphicsState; không dùng ảnh R4 cũ để chứng nhận native R8. App R8 PID38108 đã mở trên cùng DB test, đang chờ đăng nhập thủ công ở150%.

Regression R8 nguồn cố định (`runs/probe-r8-regression150.json`): ba role ADMIN/CASH/STORE đều tải7dòng, trang đầu/sau không trùng, filter khớp và footer trong host sau resize; rowheight69px cố định tại144DPI. Typed paging9/9 và search/error/empty3/3PASS. Source audit163file, Drift0 (`runs/source-r8-verification.json`). Lượt R8 chưa có ảnh desktop sau sửa; còn chờ đăng nhập, không ghi native R8 PASS từ kết quả probe.

## Cập nhật native R8 sau đăng nhập

Đã đăng nhập thủ công admin, mở Nguyên liệu / Tồn. `probe-r8-current-dpi.json` xác nhận DeviceDpi144. Ảnh do Sky trả về1274×720, lưu nguyên bản tại `runs/native-inventory-r8-150-final-0.png`; metadata `runs/native-r8-150-capture-metadata.json`. Không gọi kích thước ảnh này là client1366×768 hoặc ảnh pixel vật lý150%; công cụ trả kích thước khác ảnh R4 trước đó. Không chỉnh sửa/upscale ảnh để tạo chứng cứ.

- Sidebar hiện trọn tên, vai trò, Đăng xuất và trạng thái phiên; không còn thanh cuộn sidebar trong tree. Không thấy đường chia ô dọc trên ảnh mới; probe pixel đãPASS riêng.
- Click chọn NVL02, phím Down chọn NVL03 đúng; Ctrl+End chọn NVL07 và cuộn bảng (scrollvalue138), Ctrl+Home trở về NVL01. Header/tools/meta/pager không đổi vị trí. PASS phần chọn dòng/bàn phím/cuộn đã chạy: `runs/native-keyboard-scroll-r8-150-0.png` vàtxt.
- Đủ Thêm nguyên liệu, Mở sổ, Sửa, Xóa; Sửa/Xóa liền nhau. Không ghi GUI valid CRUD làPASS ởR8 vì chưa lặp lưu nghiệp vụ. Có evidenceBLLfixture trước đó và handlerdiff không đổi.
- 100% và125% có ảnh R4/R3 trước sửa; chưa chụp lại hai mức này bằng R8. Native resize bằng kéo cạnh chưa thành công, không tínhPASS. Chuỗi hover/press động đầy đủ chưa có video, vẫnNOTRUN đầy đủ. Không tuyên bố tất cả tiêu chí nghiệm thu đã hoàn tất.
- Máy đang giữ Scale150% do người dùng đổi. Settings automation từngtimeout; chưa tự trả125%. Không thay bằng registry/shell.

Source R8 vẫn163file Drift0 sau native check. Dừng ở màn mẫu để người dùng review, chưa triển khai tám màn tiếp theo. Trạng thái task vẫnĐang làm/chờreview, không cập nhậtPASSG03 hoặcT01–T32.

## B09 / R9 — Avatar còn vết cũ khi maximize ở100%

Ảnh người dùng chứng minh hai avatar tại vị trí cũ/mới; R8 resize native chưa đủ kiểm chứng đã ghi trước đó. Điều tra và hướng sửa tại `bug-report.md`. R9 chỉ thêm ResizeRedraw và Invalidate sau layout trong OrbitSidebar, không thay layout/nghiệp vụ.

Debug/Release R9 build0warning0error: `runs/build-r9-debug.log`, `runs/build-r9-release.log`. Probe thực DeviceDpi96: `runs/probe-r9-avatar.json` — sidebar profile trong khung, resize chiều cao630→900 làm vùng avatar cũ y569..601 được invalidate, Person.Top mới836; PASS cơ chế xóa vùng cũ. Hover/biên grid regression vẫnPASS. Đây là control trong process, chưa thay cho ảnh native maximize/restore sau sửa.

App R9 PID33676 mở lại cùng DB test, chờ đăng nhập thủ công ở100% để kiểm native. Source cố định tại `runs/source-r9-final.csv`. Chưa đánh dấu B09 hoàn tất hoặc nativePASS.

**Cập nhật sau đăng nhập R9:** người dùng đã đăng nhập, mở inventory ở100%. Click Maximize, đợi trạng thái Restore, lưu ảnh1920×1032; click Restore về1368×800. Cả ba ảnh native trước/phóng to/thu nhỏ chỉ một avatar cạnh tên, không có vết cũ giữa sidebar. B09 native resize PASS đúng chuỗi đã chạy. Bằng chứng `runs/native-avatar-r9-restored-before-100-0.png`, `runs/native-avatar-r9-maximized-100-settled-0.png`, `runs/native-avatar-r9-restored-after-100-0.png`; source163file Drift0 ở `runs/source-r9-verification.json`. Không kiểm lại chuyểnDPI125/150 bằng R9; giữ giới hạn pilot còn lại đã ghi.

## B10 / R10 — Cấu hình DPI khởi động

Yêu cầu người dùng: xem Program.cs, đưa về100 thay125 do thấy mờ. Điều tra: không có125 trong Program; MainForm/Materials96DPI; StartupForm120 không thuộc luồng chạy. csproj chưa khai báo ApplicationHighDpiMode. Đã khai báo PerMonitorV2 để96DPI khi Windows100%, theo DPI monitor thay vì giữSystemAware cũ; không épDpiUnaware vì có thể làm bitmapstretch mờ hơn. Không tự đổiWindowsScale.

BuildDebug/Release R10:0warning0error, `runs/build-r10-debug.log`, `runs/build-r10-release.log`. Generated ApplicationConfiguration.g.cs ở cảDebug/Release có Application.SetHighDpiMode(HighDpiMode.PerMonitorV2) trước khi tạoForm. Đây là kiểm cấu hình sinh ra, chưa phải bằng chứng hết mờ native. AppR10 PID31272 mở trênDBtest, chờđăngnhập. Chưa ghiB10PASS/độnétdạt, chưa kiểm live chuyểnDPI vớiR10. Source cố định `runs/source-r10-final.csv`.

Giới hạn công cụ: mở Settings/Control Panel tự động ban đầu không lộ cửa sổ; người dùng tự đổi Scale 100%. Lần đọc Settings sau đó báo `Computer Use app approval timed out`, không tự thay display/registry bằng cách khác. Fixture mở rộng factory ngoài màn mẫu từng bị automatic approval review chặn; không thực hiện, giữ fixture chỉ inventory và kiểm sidebar trên app thật.
