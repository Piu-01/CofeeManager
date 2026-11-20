# G07 — Bộ plan T27–T29

31/10/2026. Requirements đã được người dùng duyệt bằng “duyệt”. **Bộ plan đã được người dùng duyệt bằng “duyệt” ngày31/10/2026; đã triển khai tuần tự**; đợt test riêng đã được duyệt và chạy,24PASS/0FAIL/9BLOCKED; xem [report.md](report.md). G07-B01 đã được duyệt sửa và regressionPASS/schema027; native cònBLOCKED, chưa Hoàn tất.

## 1. Thứ tự triển khai và điểm duyệt

| Thứ tự | Plan | Đầu vào → đầu ra |
|---|---|---|
| 1 | [T27 — Tài sản](t27-tai-san-khau-hao/plan.md) | T26/schema024 → schema025, CRUD tài sản, khấu hao tham khảo và liên kết chi |
| 2 | [T28 — Báo cáo](t28-bao-cao-doi-chieu/plan.md) | T27/schema025 → schema026, báo cáo SQL/DTO/UI và tổng quan ADMIN |
| 3 | [T29 — Hiệu năng](t29-hieu-nang-sql/plan.md) | T28/schema026 → dataset/số đo trước–sau, tối ưu được chứng minh; schema027 chỉ khi cần |

Một task tại một thời điểm. Duyệt cả bộ → triển khai tuần tự, kiểm kỹ thuật tối thiểu và đo phục vụ lựa chọn tối ưu T29 → báo cáo triển khai → chờ duyệt đợt nghiệm thu G07 riêng. Không coi kiểm kỹ thuật là PASS 33 ID. Nếu thiếu đầu ra thực sự chặn task sau, báo bằng chứng và quyết định cần chốt. Không tự đổi trạng thái G06 hoặc các nhóm trước.

## 2. Đối chiếu source và tài liệu

| Nguồn đã đọc | Hiện trạng | Quyết định trong plan |
|---|---|---|
| Kế hoạch mục 8–13, T27–T29; UX mục 2, 14–18 | 9 + 13 + 11 = 33 ID; UX-K37/K38, UX-K39–K42 | Giữ nguyên checklist gốc trong requirements; bổ sung subcase, không thay 325 ID |
| DB_Schema.sql, Insert_Data.sql | TAISAN_CODINH có MaTS/TenTS/NgayMua/GiaTri/SoThangKH, không có trạng thái; seed đã có TS001… và phiếu chi MaTS | Không thêm nghiệp vụ trạng thái/thanh lý; giữ lịch sử chi và hồ sơ cũ |
| Migrations001/004/017–024 | Cấp mã SQL, MaterialOpening, G03OperationReceipt, nguồn thu chi, snapshot kho và lương | Tận dụng demand/receipt/pending/nguồn; không giả procedure tương lai đã có |
| FinanceRequest/Service/Repository, FinanceEntryDialog, DocumentLinks | Chi ghi tay đã nhận MaTS; editor hiện nhập mã tài sản, có liên kết PN/nợ/lương | Thêm picker/chọn tài sản và liên kết tài sản–chi đúng quyền, giữ contract ghi chi |
| Program/MainForm/NavigationController/SessionHomeControl | Chưa có module Assets/Reports; ADMIN Home chỉ có lời dẫn | Thêm hai đích điều hướng, dashboard ADMIN dùng report service thật |
| PermissionService/SessionContext, pending G06 | Có Finance/FinancialReports, demand kiểm identity/stamp; pending có scope DB | Tài sản dùng Finance; báo cáo dùng FinancialReports, ADMIN; STORE giữ module kho/nợ hiện có |
| Source HR và SqlErrorMapper | Money helper dùng SqlDecimal để giữ precision; mã lỗi đến533xx | Giữ cách truyền SqlDecimal cho tiền; bổ sung534xx/535xx, không sửa helper của module khác |
| Bộ docs/ui/ theo README | Theme/token tối, list chuẩn, native modal owner, select-all, footer responsive | Dùng control hiện có; không sao chép demo JS, không thêm bụi sao |

Đây là khảo sát source, chưa xác minh SDK/engine/schema DB đang chạy. Trước triển khai ghi global.json/SDK, root Git đúng CoffeeManager trước mọi lệnh Git, hash source, migration hiện có và DB đích. Version025/026/027 là dự kiến theo source001–024; nếu đã được dùng, cập nhật tên/version trong bộ plan trước chạy, không ghi đè migration/checksum.

## 3. Contract chung

- Models không phụ thuộc UI/SqlClient; DAL query async có tham số, dispose connection; BLL chuẩn hóa và demand quyền; UI binding/điều hướng. Không EF/Dapper/API/generic repository, không mở transaction C# bọc procedure.
- Public SQL nhận Actor int/Stamp binary(8), demand qua sp_G03Demand với ADMIN. Chỉ cấp EXECUTE public endpoint, không cấp DML hoặc helper nội bộ. Phiên đổi role/stamp hoặc nghỉ việc bị thu hồi theo cơ chế hiện hành. SQL credential chung vẫn có giới hạn bảo mật đã công bố trong kế hoạch.
- Tiền SQL decimal(18,0); tổng nhiều chứng từ dùng decimal(38,0), DAL đọc SqlDecimal và chỉ chuyển sang decimal C# khi nằm trong miền. Vượt miền trả lỗi rõ, không tràn/clip về0. Input tiền dùng tham số rộng và SqlDecimal như G06 để chặn phần lẻ/vượt18 chữ số trước cast; không chuyển chuỗi tiền đã format xuống SQL.
- Tài sản dùng RequestId cố định, payload canonical/hash gồm action/code/revision và trường nhập. Receipt lấy khóa request trước tra cứu, kiểm actor/operation/hash; replay không ghi lại. Pending scope DB/actor/operation, lưu trước gửi; I/O thất bại thì không gửi. Unknown chỉ cho đối chiếu; NotRecorded chỉ khi đã lấy được cùng request lock và không thấy receipt. Không retry mù sau timeout.
- Ngày nhận typed DateOnly/DateTime. Khoảng nửa mở và biên9999-12-31 dùng upper bound NULL có kiểm soát, không DATEADD vượt miền. Thứ tự trang ổn định có mã khóa phụ; tổng thật trả được cả khi trang rỗng, không chỉ COUNT OVER trên dòng.
- Báo cáo đọc dữ liệu đã commit với ReadAt và metadata nguồn; không bật snapshot isolation/đổi setting SQL để chạy. Kết quả từng báo cáo lấy từ tập trung gian SQL đã materialize một lần cho tổng/count/page, tránh đọc lại ba tập khác nhau. Đối chiếu chứng từ tại dataset đứng yên; số đọc khi đang phát sinh được ghi thời điểm và cho refresh, không cam kết snapshot đồng thời xuyên mọi báo cáo/dashboard.
- Lỗi mới53401/02/03/05/07 cho input/nguồn/version/miền số/preflight tài sản;53501/02/05 cho filter/nguồn lịch sử/miền tổng báo cáo. Tiếp tục51505/51605 cho phiên/quyền,51608 cho request mismatch. Không lộ stack/connection string; read timeout là lỗi tải, không là giao dịch tài chính Unknown.

## 4. UI và đường dùng thử

Assets và Reports là UserControl trong MainForm. Menu ADMIN, mapping quyền tại NavigationController và BLL; không mở FinancialReports cho STORE. STORE vẫn dùng tồn/nợ được phép. Program nối dependencies; Home ADMIN dùng AdminDashboardControl, CASH/STORE giữ điểm vào theo role hiện có.

Danh sách tài sản dùng OrbitRecordListControl.UseStackedSearch, search cả hàng, filter hàng riêng nếu có, action riêng, grid Dock.Fill và pager đáy. Search draft/applied theo chuẩn; filter/pager không submit nháp. Báo cáo dùng OrbitListWorkspace, filter riêng và nút Xem báo cáo theo UX14: đổi filter đánh dấu cần tải, vô hiệu drill-down số cũ, chỉ đổi metadata khi kết quả mới được chấp nhận. Không áp cơ chế autoquery của list tài sản vào báo cáo nếu làm mất bước Xem báo cáo.

Dialog tài sản/picker/chi tiết/nguồn dùng OrbitDialogHost và OrbitDialogForm: native titlebar/X, ShowInTaskbar=false, owner MainForm hoặc hộp đang mở; giữ filter/selection/focus, chặn vòng mở nguồn trùng theo Tag hiện có. Dùng OrbitDialogLayout footer responsive, OrbitField.ApplyInputBehavior idempotent. AcceptButton=null cho thao tác ghi; Esc/X qua guard dirty/busy/unknown/session. Các nút G07 không có bụi sao.

Kích thước logical96DPI khởi tạo: editor620×440, picker760×500, chi tiết900×580; clamp WorkingArea của owner, body cuộn, footer wrap; không minimum vượt vùng khả dụng. Trang danh sách/report grid cuộn ngang khi hẹp; filter wrap, summary thành hàng dọc, sidebar theo shell. Hỗ trợ full/cửa sổ thường/chia đôi trên màn máy thử; ghi riêng resolution/client/DPI. Agent chỉ kiểm chức năng/layout kỹ thuật125% tại1280×720/1366×768 và kích thước khả dụng thực; không ép scale/DPI, không test thị giác/Designer. Hình thức SKIPPED theo phân công,100/150% chưa chạy NOT RUN.

Hướng dẫn `development-guide.md` ghi solution/startup/profile/exe, server/DB/schema thực tế sau triển khai, bước F5 và kịch bản tài sản→chi, report→nguồn, dashboard. Chuẩn bị script nâng từ schema thực lên025/026/(027) có preflight/backup/đối chiếu, nhưng không chạy trên development khi chưa được giao phù hợp. Không đưa credential vào repo/chat; không bắt người dùng dùng DB fixture làm đường chạy thường.

## 5. Fixture, bằng chứng và gate kiểm thử

- Bổ sung tools/database/Test-G07.ps1 với chế độ technical và RunAcceptance tách rõ; benchmark T29 có script riêng. Tận dụng Apply-Migration.ps1, DatabaseTestTools.ps1, không sửa runner cũ để đổi kết quả lịch sử.
- DB test SQL Server thật: clean/demo/upgrade; manifest exact DBname/runID/marker/principal/config/hash/schema và allowlist cleanup. Dùng user CafeManagerApp trong DB test để kiểm quyền; owner chỉ provision/fixture/đối chiếu. Không tạo Windows user hoặc đổi service/registry/SQL mode. Clean baseline không seed demo; demo seed đúng một lần trước migrations.
- Snapshot source/migration/test sản phẩm trước/sau lượt nghiệm thu; fixture/harness chốt trước chạy. FAIL ghi lại, không sửa source/migration hoặc expected giữa lượt. Bug tại task sở hữu, gom đợt xin duyệt trước debug/regression.
- Mỗi task có technical-checks.md riêng sau triển khai; test-results.md khi được duyệt nghiệm thu ghi từng ID/subcase: dữ liệu, thao tác/lệnh, expected/actual, môi trường/schema/hash và chứng cứ. Report nhóm đủ33ID, UX và regression theo ảnh hưởng; không cộng số subcase thay ID.
- Cleanup finally exact resource + marker; lưu artifact trước xóa, kể cả FAIL. Không DROP wildcard/DB development; báo tạo/dọn/còn giữ, lý do và cách dọn nếu lỗi. Không tự giữ DB hoặc pending synthetic ngoài manifest.
- Test chưa chạy vì SDK/SQL/GUI/modal bị hạn chế ghi BLOCKED/NOT RUN; G06 native blocker không tự thành PASS. Chỉ Hoàn tất khi đủ đầu ra và ca bắt buộc PASS theo phân công. Commit/push cần yêu cầu riêng.

## 6. Phạm vi thay đổi và bàn giao

Các file cụ thể, migration, fixture và ma trận33ca ở từng plan liên kết đầu tài liệu. Sửa chung giới hạn ở Program, MainForm, navigation, error mapper, DocumentLinks và picker MaTS cần cho G07. T29 được sửa truy vấn/index hiện có đúng query có số đo; không refactor hàng loạt, đổi công nghệ, tài chính hay thứ tự khóa nếu chưa có bằng chứng và phạm vi duyệt.

Nếu có dữ liệu legacy không đủ nguồn để chứng minh tồn/nợ/lương quá khứ, trả completeness/nhãn và chặn khẳng định đối chiếu đầy đủ; không backfill lịch sử giả. Thiếu contract ngăn đầu ra bắt buộc hoặc cần đổi nghiệp vụ thì trình phần phát sinh trước triển khai. Không gộp backlog bug G04–G06 hoặc vận hành G08 vào nhóm này.


