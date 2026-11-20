# G06 — Bộ plan T24–T26

Ngày30/10/2026: requirements và cả bộ plan đã được người dùng duyệt. **Đã triển khai source T24 → T25 → T26; chờ duyệt đợt test riêng**, chưa nghiệm thu/commit/nâng development. Xem [report.md](report.md). Chỉ đạo chuyển G06 không thay kết quả G04/G05.

## 1. Thứ tự và tài liệu duyệt

| Thứ tự | Plan | Migration đề xuất | Đầu ra cho task sau |
|---|---|---|---|
| 1 | [T24 nhân viên/tài khoản](t24-nhan-vien-tai-khoan/plan.md) | 022_employees_accounts.sql | Hồ sơ có version, quản trị account và bảo vệ ADMIN cuối |
| 2 | [T25 chấm công](t25-cham-cong/plan.md) | 023_attendance.sql | Công hợp lệ, khóa nhân viên, batch nguyên tử và khóa kỳ hiện có |
| 3 | [T26 chốt/chi lương](t26-chot-chi-luong/plan.md) | 024_payroll.sql | Preview/snapshot bất biến, chi một lần và mở nguồn hai chiều |

Đọc cùng [requirements](requirements.md), kế hoạch mục 8, 11–13 và UX mục 6, 13, 15–18. Mỗi plan giữ nguyên phần task và checklist gốc: **10 + 11 + 12 = 33 ID**, không thay 325 ID toàn dự án. Triển khai tuần tự SQL → DAL → BLL → UI, một task con tại một thời điểm. Đầu ra kỹ thuật đủ mới sang task sau; thiếu contract chặn tiếp thì báo quyết định, không tự giả lập.

## 2. Cơ sở source đã đọc và ranh giới kiểm chứng

| Source hiện có | Kết luận từ source | Cách dùng trong G06 |
|---|---|---|
| DB_Schema.sql, Insert_Data.sql | NHANVIEN có DangLamViec, SDT unique filtered; CHAMCONG có PK ba cột, datetime2(0), ba trạng thái; BANGLUONG có identity MaBL, unique kỳ, ThucLinh computed | Mở rộng migration, giữ baseline/seed bất biến |
| migrations/002_auth_and_bootstrap.sql | LoginKey ASCII/case-insensitive, SecurityStamp rowversion; setup singleton Completed; khóa CoffeeManager.AdminBootstrap | Tái sử dụng username/hash/phiên; cùng khóa bảo vệ tập ADMIN, không mở bootstrap lại |
| migrations/003_reference_catalogs.sql | Danh mục Position/Shift có CRUD/version; sp_CatalogDemandActor kiểm active/employed/stamp; Payroll → TC005 | Không tạo lại danh mục hoặc suy chức vụ thành role |
| migrations/004_inventory_materials.sql | sp_G03Demand, G03OperationReceipt với EntityCode varchar(10) | Tái sử dụng receipt cho mã NV và MaBL dạng thập phân; batch công cần receipt riêng có khóa tổng hợp |
| migrations/017_finance_sources.sql | MaBL FK/unique, OriginKind Payroll, sp_FinanceSystemExpense, Begin/Finish/request lock | T26 gọi helper nội bộ trong transaction; không cấp EXECUTE trực tiếp helper cho runtime |
| migrations/018–021 và source G05 | Mẫu SQL/BLL/DAL/unknown/pending/nguồn; T23 có đầu ra | Giữ contract G05; nối nguồn bảng lương tại T26 |
| PermissionService, PasswordService, AuthService, SessionContext | Demand kiểm lại identity/stamp; đổi stamp sẽ thu hồi phiên; password 8–128 theo BootstrapService | Thêm permission HR cụ thể, giữ cơ chế hủy/thu hồi hiện hành |
| OrbitRecordListControl, OrbitListWorkspace, OrbitDialogHost/Form, OrbitDialogLayout, OrbitField | Có base list, modal owner, footer responsive và chọn hết input; B07 đã sửa vòng đời tải ở G05 | Dùng đúng API hiện tại, không sao chép lỗi CTS đã dispose |
| PendingOperationFile, PendingOperation, DocumentLinks | DPAPI lưu theo actor/operation, chưa có database identity; nguồn chưa mở MaBL | Thêm định danh DB cho pending G06 và đường nguồn lương, giữ tương thích pending G05 |

Đây là đối chiếu source, **chưa xác minh engine, SDK, DB đang chạy hoặc dữ liệu development**. Không coi contract mới đã tồn tại. Trước triển khai kiểm SDK/global.json và số migration còn trống; nếu 022–024 đã được dùng thì cập nhật version/tên file trong cả bộ trước chạy, không ghi đè checksum đã áp dụng.

## 3. Quyết định chung về contract và an toàn dữ liệu

- Public procedures đều nhận Actor int/Stamp binary(8), quyền ADMIN qua sp_G03Demand; BLL DemandAsync với EmployeeManage, AttendanceManage, PayrollManage mới. Actor lấy từ session; NV nhận lương khác NV lập phiếu chi. CafeManagerApp chỉ được EXECUTE public procedures/SELECT theo quyền sẵn có; DENY DML trực tiếp bảng HR. Không mở quyền helper cấp mã/chi hệ thống.
- RequestId UUID cố định cho mỗi lần xác nhận; JSON canonical phía SQL include null, hash payload sau normalize, kiểm actor/operation/content trước replay. Không dùng MAX+1, không đổi request khi Unknown. Mã NV từ sp_CapMa NHANVIEN; MaBL identity int, EntityCode trong receipt dùng varchar(10) và parse int có kiểm tra. Receipt không được chứa password/hash trong DTO hoặc log.
- T24 dùng khóa exclusive CoffeeManager.AdminBootstrap trước Demand/khóa row để tuần tự mọi thay đổi account/employment; request lock kế tiếp rồi khóa nhân viên mục tiêu theo mã tăng dần. T25/T26 dùng request lock rồi applock CafeManager:HR:Employee:<MaNV> theo thứ tự mã, rồi row/kỳ; T24 cũng lấy khóa nhân viên này trước thay hồ sơ/xóa. LockOwner=Transaction, timeout hữu hạn; không giữ khóa khi nhập/preview/xác nhận. Demand hiện có vẫn đọc actor trước các khóa nghiệp vụ của T25/T26; deadlock nếu có phải rollback/đối chiếu, không tự retry ghi. Không refactor toàn bộ thứ tự khóa các module khác.
- Đổi tên/lương không tự đổi account stamp; nghỉ việc khóa account trong cùng transaction. Đổi role/khóa/reset đổi SecurityStamp; thao tác tự đổi account thành công trả kết quả một lần rồi revoke phiên hiện tại. Khi kết quả chưa rõ, đăng nhập lại bằng credential mới hoặc ADMIN khác để đối chiếu, không phục hồi phiên cũ.
- SQL tiền decimal(18,0), nhận tham số rộng decimal(38,18) để chặn vượt precision/số lẻ trước cast; C# decimal. Ngày giờ công typed datetime2(0), không cắt giây ngầm. Kỳ 1–12/năm 2000–9999, xử lý năm9999 không DATEADD vượt miền. Không đưa chuỗi format vào SQL.
- Source cũ có bất thường thì migration dừng và ghi mã/PK cần rà soát, không sửa nghiệp vụ hoặc tự gán snapshot từ hồ sơ hiện tại. Chỉ backfill metadata có bằng chứng; giữ giá trị tài chính/số dư/lịch sử.
- Pending G06 lưu trước gửi, dưới DPAPI Windows user, scoped bằng database identity ổn định. Thêm singleton GUID DB ở022, public read có kiểm quyền; kết hợp endpoint/catalog đã chuẩn hóa để tránh bản restore cùng GUID nhầm DB. Mở rộng PendingOperation thêm trường tùy chọn/khóa scoped, giữ đường đọc pending G05; G06 chỉ nhận đúng DB/actor/operation/schema. Không lưu password/hash; cấp/reset account chỉ lưu request/target/action/version và trạng thái cần đối chiếu. I/O hỏng/không ghi được pending thì không gửi. Sau commit nhưng clear lỗi hiển thị đã ghi/cần dọn pending, không ghi lần nữa.
- Reconcile lấy cùng request applock trước lookup để phân biệt chưa commit/đang xử lý/đã ghi. Chỉ khi đã lấy khóa và không có receipt mới kết luận yêu cầu chưa ghi; đọc rỗng khi timeout lock vẫn là Unknown. Actor khác chỉ có ADMIN được đối chiếu metadata tối thiểu của thao tác quản trị account, không xem hash. Payroll/attendance chỉ actor tạo hoặc ADMIN có quyền cùng DB, trả nguồn theo contract.

## 4. Tổ chức UI và file tích hợp

Thêm NavigationTarget.HumanResources trong NavigationController.cs, mapping quyền EmployeeManage; Program.cs nối dependencies vào HumanResourcesControl; MainForm thêm mục Nhân sự chỉ ADMIN. Bên trong gồm tab Nhân viên/Tài khoản, Chấm công và Bảng lương; triển khai tab theo thứ tự task, không có nút giả cho tab chưa làm. Các handler từng tab vẫn kiểm service riêng. Danh mục chức vụ/ca dùng CatalogService và màn danh mục hiện có.

List theo docs/ui/README → guide mục11 → reference → tokens → references README → WINFORMS_IMPLEMENTATION → assets: search giãn hết hàng, filter wrap riêng, action riêng, grid fill, pager đáy với tổng thật. Dùng UseStackedSearch, TotalCountSelector; query với applied keyword, đổi filter về trang đầu, generation guard. Null list JSON/lịch sử phải được map rõ, không dựng tổng0 từ lỗi.

Editor/preview/xác nhận là OrbitDialogForm qua OrbitDialogHost; native titlebar/X, modal owner MainForm hoặc dialog cha, ShowInTaskbar=false. OrbitDialogLayout responsive footer opt-in; OrbitField.ApplyInputBehavior một lần, gồm input động/editor grid. Không gán AcceptButton cho chốt/chi; Esc/X đi guard nháp/busy/unknown, không xuyên phím. Không có bụi sao trên nút HR.

Client khởi tạo logical96DPI: editor hồ sơ/account620×480; công/batch và preview lương900×600; chi lương620×440. Clamp WorkingArea owner, body cuộn/footer wrap, không minimum vượt vùng khả dụng; hỗ trợ1280×720/1366×768, không ép DPI. Agent chỉ kiểm chức năng/layout kỹ thuật125%; hình thức SKIPPED theo phân công,100/150%NOT RUN. Không chụp/so ảnh hoặc nghiệm thu qua Designer.

## 5. File mới, fixture và kiểm chứng

Các plan chỉ rõ Models/Requests và Dtos/HumanResources, ba Repository, ba Service, Modules/HumanResources và các điểm tích hợp dùng chung. Bổ sung SqlErrorMapper 531xx/532xx/533xx: 01 input,02 trạng thái/nguồn,03 version/xung đột,05 precision,07 preflight; dùng51505/51605 cho phiên/quyền và51608 cho request mismatch. Deadlock1205 là rollback đã biết, transport/cancel sau gửi có thể Unknown. Lỗi không lộ SQL/credential.

Tạo tools/database/Invoke-G06Technical.ps1 (-ThroughVersion22..24), Test-G06.ps1, database/tests/g06/fixtures và tools/CafeManager.G06Probe dùng mẫu runner hiện hành; tests/CafeManager.Tests/HumanResources cho quy tắc có giá trị. Chưa tạo/chạy chúng trong pha plan. Sau triển khai mỗi task chỉ build Debug/Release, compile/apply migration và typed smoke phụ thuộc trên DB riêng; lưu technical-checks.md. Không chạy nghiệm thu33ID trước duyệt đợt test.

DB riêng CoffeeManager_Test_G06_<runId>_clean/demo/legacy, marker runID và manifest exact allowlist/server/DB/principal/schema/checksum/source hash/exe/commands. Clean baseline không seed +001–024/bootstrap/danh mục tối thiểu; Demo baseline+seed một lần+migrations; Legacy clone test021 có dữ liệu hợp lệ/sai có chủ đích. Dùng runtime DB principal thật, tách runner owner để chuẩn bị và đối chiếu; không dùng owner thay bằng chứng runtime. Không tạo Windows user/đổi SQL auth/service hoặc dùng SQLite.

Test đối chiếu snapshot tài chính/tồn/điểm/nợ trước/sau migrations; JSON dài phải stream đầy đủ trước hash theo sửa B04. Concurrent có barrier và hai session DB thật: ADMIN cuối, overlap, công/chốt, hồ sơ/chốt, hai chốt/chi cùng nguồn. Fault injection chỉ fixture/test, không đưa hook gây lỗi vào sản phẩm. Timeout trước/sau commit, app restart, pending I/O/DB mismatch, phiên tự đổi/mất quyền đều có subcase riêng. Component/reflection ghi rõ, không coi là thao tác chuột/phím native.

Sau lưu bằng chứng dọn DB/process/principal/file do lượt chạy tạo, xác minh absolute target/name+marker trước thao tác; không wildcard/drop development/G04/G05 hoặc thư mục ngoài allowlist. Manifest ghi tạo/dọn/còn giữ/lý do/cách dọn; fail cũng cleanup. Thiếu SDK/SQL/GUI ghi BLOCKED, không giả PASS.

## 6. Bàn giao và gate

Sau đủ source022–024 chuẩn bị development-guide.md và script nâng cấp review được, gồm preflight/backup COPY_ONLY CHECKSUM/đối chiếu dữ liệu; **chưa chạy nâng development trong phạm vi duyệt bộ plan này**. Chạy app dev schema cũ phải báo thiếu migration trước mở module HR, không sửa app thành fallback DB khác. Không đổi cấu hình local, reset tài khoản, seed lại hoặc bật bypass login.

Triển khai xong cả nhóm trình test-plan.md một lần; sau duyệt mới chạy từng task và lưu test-results.md, report.md chung đủ33ID cùng giới hạn. Bug tổng hợp tại task sở hữu, chờ duyệt cả đợt. Source đã triển khai; chưa đánh PASS/Hoàn tất và không commit/push khi chưa giao.



