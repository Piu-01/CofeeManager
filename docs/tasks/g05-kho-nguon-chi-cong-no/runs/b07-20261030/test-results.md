# B07 — Sửa và regression30/10/2026

Người dùng duyệt “duyệt” sau báo cáo B07, bao gồm sửa năm control và regression theo ảnh hưởng. Áp dụng karpathy-guidelines/bug-investigation-workflow/post-change-test-workflow cục bộ. Không xin duyệt lại từng ca trong đợt đã duyệt.

## Thay đổi và môi trường

Chỉ năm file sản phẩm: FinanceControl, SupplierDebtsControl, PurchasesControl, StockIssuesControl, StocktakesControl. StopLoading tăng generation, tách loading về null trước Cancel/Dispose; dùng khi ẩn/thay lần tải/dispose. LoadRows bỏ qua màn đang dispose/đã dispose hoặc phiên revoked; kết quả và lỗi cũ không cập nhật UI khi phiên/màn đã kết thúc. Không đổi MainForm, SQL/migration, dữ liệu, quyền hoặc hình thức.

Windows WinForms thật tại125% (DeviceDpi120), .NET10.0.13, Debug native, SQL Server localhost baseline+seed+migrations001–021. DB `CoffeeManager_Test_G05_20261030_035847_cdac8a8e_demo`, marker `20261030_035847_cdac8a8e`. Fixture ADMIN9001001/STORE9001002/CASH9001003. DB riêng; không ghi fixture vào development. source-fixed.csv chụp sau fix trước fixture/test; source-freeze-check.json xác nhận không đổi trong lượt test. changed-source.json đối chiếu với source trước B07, đúng năm file.

## Kết quả

| Ca regression | Dữ liệu/thao tác, expected và actual | Kết quả/bằng chứng |
|---|---|---|
|Build Debug/Release|`dotnet build CafeManager.slnx -c Debug/Release --no-restore`; cả hai0warning/0error.|PASS; debug-build.log, release-build.log.|
|Unit G05|`dotnet test ... -c Release --no-build --no-restore --filter FullyQualifiedName~CafeManager.Tests.Finance\|FullyQualifiedName~CafeManager.Tests.Inventory`.|22/22 PASS; unit.log.|
|B07 component: năm màn|Mỗi màn tải thật, hide phải giải phóng loading; show tải lại hợp lệ; dispose hai lần không lỗi; gọi refresh sau dispose không tạo read/generation mới.|PASS; component-results.json/probe/Program.cs.|
|B07 component: read đang chờ|SQL session thứ hai giữ TABLOCKX/HOLDLOCK trên TAIKHOAN; refresh phải thực sự pending. Dispose, rollback session khóa, await hoàn tất; không thay DataSource cũ/không giữ CTS/không async exception.|PASS cho cả năm màn; component-results.json. Không mô phỏng bằng SQLite hoặc task giả.|
|B07 component: phiên revoked|Mỗi màn tải xong, Revoke, refresh không tạo lần đọc mới; dispose lặp an toàn.|PASS cho cả năm màn; tổng toàn harness47/47 gồm DPI và no async exceptions.|
|T19–T23 quyền CASH BLL|Đăng nhập identity CASH fixture, gọi trực tiếp service.ListAsync của cả năm phân hệ; expected/actual51605.|5/5 PASS; permissions-results.json, permissions/Program.cs.|
|Native chuyển màn|ADMIN đăng nhập thường → Thu chi → Kho/Nhập hàng → Xuất kho → Kiểm kê → Công nợ; các danh sách tải từ SQL, không hộp lỗi.|PASS theo luồng; native-finance/purchases/issues/stocktakes/debts-before-logout.txt. Thu chi rỗng vì bộ lọc ngày hiện tại, không coi lỗi DB là rỗng.|
|Native modal/nháp|Nhập Trả ngay1234 ở editor nhập, Quay lại → Bỏ nháp, Không giữ1234 trong hộp cha; Quay lại/Có đóng về danh sách. Không chốt nhập.|PASS; native-draft-close-confirm/retained/discarded.txt.|
|Native tái hiện B07|Công nợ mở lịch sử nợ CN20250404, đóng hộp rồi Đăng xuất; expected Login không exception. Actual đúng Login, chỉ một cửa sổ app; không dùng Continue để bỏ qua lỗi.|PASS; native-debt-detail-closed.txt, native-logout-debts-login.txt và quan sát công cụ native. native-debt-detail.txt bắt state quá sớm trước dialog hiện, không dùng file đó làm bằng chứng nội dung dialog.|
|Native đăng nhập lại/quyền|Sau logout ADMIN, CASH đăng nhập thường, menu không Thu chi/Kho/Công nợ; logout về Login. ADMIN đăng nhập lại có đúng menu.|PASS; native-cash-shell.txt, native-cash-logout-login.txt, native-admin-relogin.txt.|
|Native đóng MainForm|Phiên ADMIN mới tải Thu chi rồi đóng nút X; app thoát, không exception/process còn lại.|PASS; native-before-app-close.txt, resource-audit.json. Logout phiên đầu cũng dispose các màn G05 đã tạo. Không tuyên bố đã đăng xuất riêng từng tab bằng năm phiên native khác nhau.|
|Đối chiếu DB cuối|PN10/PX10/nợ9/finance16/receipt fixture0, mọi tồn NVL giữ nguyên so native-before.json.|PASS; native-write-check.json. Đây là đối chiếu counts/receipt/tồn, không tuyên bố hash mọi dòng tiền/nợ.|
|Native Tab/chọn hết đầy đủ|CUA gửi Tab từ binding owner nhưng focus không chuyển ổn định, cache index/geometry lỗi; không đủ bằng chứng kiểm keyboard.|BLOCKED công cụ, giữ gap cũ; không ghi FAIL sản phẩm hoặc suy PASS từ component B06.|

## Lệnh lỗi và giới hạn được giữ

- Lệnh build đầu ghi nhầm CoffeeManager.slnx; file thực tế CafeManager.slnx. Log debug-build-command-error.log. Một lượt sandbox build FAILED0warning/0error; chạy với quyền SDK phù hợp đã thành công, giữ debug-build-sandbox.log.
- Lệnh test đầu thiếu filter chạy560 ca:99PASS/461FAIL do fixture integration riêng không được chuẩn bị (xem unfiltered-tests-command-error.log). Đây là lỗi chọn lệnh; không dùng để kết luận regression toàn dự án hay ghi bug sản phẩm. Bộ22G05 sau đó chạy đúng filter PASS.
- Harness đầu thiếu using Models.Dtos, không build được; bổ sung namespace trước khi chạy harness. Giữ probe-build-missing-using.log; bản cuối probe-build.log0warning/0error. Product source không đổi sau snapshot.
- Không sửa test/expected để né FAIL. Permissions probe bổ sung chỉ đọc sau component, không thay47 expected. Không test thị giác (SKIPPED), không đổi Windows Scale; DPI100/150 NOT RUN. Các nhánh crash/restart/I/O/print-file và55ID đầy đủ không được suy PASS từ B07. Chặn gửi lặp/busy/unknown giữ bằng chứng B05/B06, không chạy lại toàn bộ trong B07 vì handler ghi không đổi.
- Tạo1DB/dọn1/còn0; PID10988 thoát bằng đóng native. Credential DPAPI và file đăng nhập native tạm nằm trong cùng thư mục riêng đã xóa đúng marker; resource-audit.json. Không lưu mật khẩu vào repo/log. Không commit/push.

**B07 đã sửa và PASS theo phạm vi. G05 vẫn Đang làm**, giữ lịch sử55ID27/10 và325ID/T01–T32.
