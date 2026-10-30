# G05 — Bộ plan T19–T23

Ngày26/10/2026: requirements và cả bộ plan đã duyệt (phản hồi “đuyẹt”, sau đó “continue”), triển khai tuần tựT19–T23. Cập nhật27/10: đã duyệt nâng đúng development và đợt test; development021; kết quả38PASS/5FAIL/10BLOCKED/2NOTRUN, chưa nghiệm thu hoàn tất. 29/10 đã duyệt và sửa B01–B04, regression theo ảnh hưởng PASS; B05 footer đã duyệt/sửa và regression theo phạm vi; B06 grid nhập/xuất đã duyệt/sửa và regression chính PASS; còn nhánh nghiệm thu chưa đủ tại [report](report.md). Form modal native có owner và SelectAllOnEntry áp dụng cho mọi màn G05.

## 1. Chỉ mục và thứ tự

| Task | Plan | Migration dự kiến | Đầu ra cho task tiếp |
|---|---|---|---|
| T19 | [Thu chi và nguồn hệ thống](t19-thu-chi/plan.md) | 017_finance_sources.sql | FK nguồn, ghi chi nội bộ, tra cứu/đối chiếu phiếu |
| T20 | [Nhập kho/trả ngay/nợ](t20-nhap-kho/plan.md) | 018_purchase_receipts.sql | Nhập chốt và nợ nguồn đúng, bộ khóa kho |
| T21 | [Xuất hủy/trả hàng](t21-xuat-kho/plan.md) | 019_stock_issues.sql | Xuất chốt, bảo vệ chứng từ, tranh tồn với checkout |
| T22 | [Kiểm kê](t22-kiem-ke/plan.md) | 020_stocktakes.sql | Snapshot, chốt một lần, bảo vệ header/chi tiết |
| T23 | [Công nợ/trả nợ](t23-cong-no/plan.md) | 021_supplier_debts.sql | Trả nợ nguyên tử, số dư hiện tại/tại ngày, nguồn cho báo cáo |

Các số migration dựa trên source hiện có đến016, chưa kiểm DB runtime. Nếu có migration mới trước triển khai, rà xung đột và cập nhật số/file trong bộ plan trước áp dụng; không sửa checksum001–016. Một task đang triển khai tại một thời điểm. Mỗi plan đọc kèm tài liệu này và [requirements](requirements.md); giữ đủ55ID tại mục nghiệm thu từng plan.

## 2. Cơ sở đã đọc và phạm vi chuyển tiếp

- Kế hoạch mục2–12 và13; nghiệp vụ7,8.3,9,10; UX mục2–5,11–12,15–18. G04 hiện40PASS/0FAIL/16BLOCKED; T18 đủ8/8. Người dùng đã đồng ý chuyển tiếp. Giữ16ca T16/T17 và lịch sử, G04 Đang làm; không chạy lại toànG04 chỉ để lập G05.
- Baseline DB_Schema.sql và Insert_Data.sql: nhập/xuất chưa có cờ chốt; PHIEUTHUCHI chưa có MaPN/MaBL; kiểm kê có DaDieuChinh nhưng thiếu bảo vệ header; trả nợ chưa có actor/request hoặc kiểm ngày nguồn đầy đủ.
- Migrations001/003/004/010–016: CafeManagerApp, SystemExpenseCategory, PaymentMethodSupport, NGUYENVATLIEU.Revision, MaterialOpening, G03OperationReceipt và sp_G03Demand/BeginReceipt đã tồn tại. Checkout khóa NVL theo MaNVL tăng dần rồi trigger ghi tiêu hao trừ kho. Không thay trigger/procedure checkout nếu không có bằng chứng cần đổi.
- Mã hệ thống thực: Payroll→TC005, SupplierDebt→TC011, PurchaseImmediate→TC012 từ003. TC008 là danh mục chi thường, không tự đổi thành trả ngay. TC009 tên seed có đặt cọc không cấp quyền xây chức năng đặt bàn.
- Cấp mã001 chỉ hỗ trợ HOADON/DANHMUC_SP/NHANVIEN; mã thu chi/PN/PX/KK/CN phải bổ sung sequence tại task sở hữu, không mặc định sp_CapMa đã hỗ trợ.
- Source hiện chỉ có MaterialsControl/MaterialRepository/MaterialService trong kho; chưa có module thu chi/nhập/xuất/kiểm kê/nợ. NavigationTarget và Program chưa nối các màn này. PermissionService đã có Inventory, DebtRead, DebtPay, Finance đủ ma trận; ưu tiên dùng lại.
- Đã đọc docs/ui/ theo thứ tự README → HuongDan → reference.html → tokens → references/README → WINFORMS_IMPLEMENTATION → assets/README. Dùng AppTheme, OrbitManagementTheme, OrbitManagementLayout/ListWorkspace, OrbitDialogLayout/Field, native grid/input; không tạo theme mới. Các mẫu management trong source được kế thừa, không tự sửa toàn theme khi phát hiện khác token; ghi rõ nếu cần phần phát sinh. Bụi sao giữ tắt trên mọi nút G05.

## 3. Contract chung được chọn

**Ghi và đối chiếu:** mỗi write có @Actor int, @Stamp binary(8), @RequestId uniqueidentifier; actor MaNV lấy từ TAIKHOAN đang hoạt động/DangLamViec, không nhận nhân viên tự chọn. SQL sp_G03Demand kiểm role trước replay; mã nguồn cấp trong transaction sau nhận request, receipt lưu cùng commit. Dùng G03OperationReceipt và sp_G03BeginReceipt hiện có; payload canonical gồm mọi trường nghiệp vụ/TVP sắp mã NVL, có NULL rõ ràng, hash và operation khác nhau cho từng use case. Replay đúng actor/operation/hash trả mã cũ; khác nội dung/actor lỗi51608. UI giữ request khi chưa rõ; sửa nội dung chỉ tạo request mới sau xác minh request cũ không commit.

Bổ sung các procedure đọc receipt theo phạm vi tại task sở hữu (Finance/Inventory/Debt), chỉ trả actor của request và operation có quyền hiện tại. Không mở quyền đọc raw receipt. Receipt vắng ngay sau timeout chưa chứng minh rollback: giữ trạng thái chưa rõ nếu phiên trước còn chạy, chỉ gửi lại cùng request/payload sau đối chiếu và thao tác chủ động; không tạo nguồn mới để né timeout. Lưu pending request/mã/operation tối thiểu trong file local theo Windows user bằng DPAPI để khôi phục sau crash, không lưu mật khẩu/connection string. BLL kiểm quyền/session lại, UI khóa gửi và hỏi bỏ nháp theo IUnsavedChanges; logout/đổi phiên không áp dụng pending của actor khác.

**Kiểu và validation:** tiền chính thức decimal(18,0), lượng decimal(18,4); input SQL/TVP có miền rộng decimal(38,18) để phát hiện quá precision trước CONVERT, không âm thầm làm tròn input lượng hoặc tiền nguyênVND. Tổng dòng nhập ROUND(lượng×giá,0) tính SQL dùng lại preview/chốt. TVP không dựa PK để nuốt/gộp NVL trùng; procedure phát hiện trùng trước ghi. Text nhận miền rộng, kiểm độ dài trước ép về cột, trim tùy contract; optional rỗng→NULL. MaPT phải được hỗ trợ và khácPT010. Mọi query phân trang50dòng, thứ tự ngày+mã ổn định, filter ngày nửa mở; tổng tính trên tập lọc, không trên một trang.

**Khóa:** write không nhận transaction ngoài; XACT_ABORT/TRY-CATCH rollback sở hữu transaction, @@TRANCOUNT về0 sau lỗi. Khóa request trước, khóa header nguồn nếu có, khóa các NVL theo mã tăng dần bằng UPDLOCK,HOLDLOCK trước ghi chi tiết. Nhập/xuất không lấy khóa đơn/khách/recipe sau NVL; không tạo chu trình ngược với checkout. Dùng khóa request hiện hữu; range-lock receipt hai request khác nhau phải kiểm concurrency, nếu cần khóa theo request bằng applock rồi exact lookup trong phần G05, không tự refactor helper G03. Không coi ORDER BY trong một UPDATE set-based là bảo đảm thứ tự lấy khóa.

**Mã mới:** sequence không cycle cho TC/PN/PX/KK/CN +8 chữ số, khởi tạo tránh mã hiện có có cùng pattern; mã cũ TR vẫn giữ. Sequence dùng trong procedure sở hữu, không MAX+1 khi ghi thường, chấp nhận khoảng trống rollback. Chỉ extension case tương ứng vào sp_CapMa tại task sở hữu; runtime không được chèn nguồn tùy ý bằng mã được cấp.

**Quyền SQL:** chỉ GRANT public procedure đúng task và type cần thiết; helper ghi chi không GRANT runtime. DENY DML trên chứng từ/lịch sử/receipt; ownership chaining trong procedure, không tin SESSION_CONTEXT do client tự đặt. Runtime không gọi bản baseline sp_TraCongNo/sp_ChotKiemKe thiếu actor: T19 thu hồi GRANT cũ, T22/T23 chỉ cấp lại sau đổi signature và kiểm quyền. Chủ cài dùng quyền riêng; seed cũ không chạy sau migration. Bổ sung danh mục hệ thống tối thiểu chỉ khi thiếu và mapping đúng; dữ liệu sai loại/mapping phải dừng, không tự sửa lịch sử.

**Lỗi:** dự kiến52601–52608 finance,52701–52708 nhập,52801–52808 xuất,52901–52908 kiểm kê,53001–53008 nợ; suffix01=input,02=trạng thái/nguồn,03=xung đột/số dư,04=quyền nghiệp vụ,05=precision/giới hạn,07=preflight/cutover,08=request/contract (không dùng hết nếu không cần). Giữ51505/51605/51608 và các lỗi baseline còn hợp lệ; SqlErrorMapper dịch rõ, không lộ raw SQL. Timeout/lỗi transport sau gửi là Unknown, không thông báo thất bại chắc chắn. Deadlock rollback có bằng chứng không tự ghi lại tiền/kho vô điều kiện.

## 4. UI và đường chạy dùng thử

MainForm + UserControl, navigation tạo module theo role, tab nội bộ Kho: tồn/nhập/xuất/kiểm kê; Công nợ và Thu chi có entry theo role. STORE vào tổng quan kho đọc dữ liệu thật/lối vào chứng từ, thay hướng dẫn “sẽ bổ sung”; ADMIN giữ Home, CASH giữPOS và tồn phục vụ bán. Không thêm dashboard doanh thu giả hoặc lương/tài sản mới.

Màn danh sách dùng grid vùng co giãn; editor chứng từ gồm header, grid nhập dòng, summary và footer chốt; thu chi/trả nợ là dialog nhỏ. Kích thước logical96DPI: MainForm giữ mức tối thiểu hiện hữu; vùng module dưới760px chuyển filter thành hàng dọc/cuộn, sidebar thu gọn theo cơ chế hiện có. Editor nhiều dòng client khởi tạo900×600, có đường thu nhỏ tới640×420; editor đơn giản/trả nợ khởi tạo620×440, co tới480×360. Kích thước khả dụng luôn clamp theo WorkingArea owner; không ép min lớn hơn viewport thực và không dùng min để chặn ca resize. Body cuộn, footer tự xếp dọc khi chật, grid cho cuộn ngang; không scale bitmap hay cố định DPI. Plan triển khai đo PreferredSize/bounds để điều chỉnh trong phạm vi hỗ trợ, không hardcode device px.

Kiểm chức năng/layout kỹ thuật trên Windows125%: desktop thực tế, full/cửa sổ thường/thu nhỏ/chia đôi, cấu hình tương đương1280×720 và1366×768 khi môi trường hỗ trợ; ghi desktop/client/DeviceDpi thực. Chia đôi có thể cuộn ngang/dọc, CTA vẫn tiếp cận được. Không đổi Windows Scale;100/150%NOT RUN, hình thức/hover/spacing/ảnh đối chiếu SKIPPED theo phân công người dùng22/10. Không nghiệm thu visual quaDesigner. Tái sử dụng icon PNG cần dùng vàlicense, không thêm Internet runtime.

Development: giữ CoffeeManager.slnx, startup CafeManager.WinForms, profile CoffeeManager.Development và DB localhost/CoffeeManager_Development theo tài liệu hiện có; xác minh marker/DB/schema trước áp dụng. Bổ sung development-guide.md trong nhóm. tools/database/Update-DevelopmentG05.ps1 dự kiến có preflight backup COPY_ONLY CHECKSUM và đối chiếu trước/sau; chạy đúng đường triển khai đã duyệt, không tự cấp quyền server/resetaccount. Triển khai không ghi fixture nghiệm thu vào development. Không can thiệp DBmanualG04 đang giữ hoặc process test khác.

## 5. Fixture, kiểm kỹ thuật và đợt test

Mỗi task SQL→DAL→BLL→UI. Trong triển khai chỉ build Debug/Release, compile migration và smoke kỹ thuật tối thiểu trên DB riêng theo plan; lưu technical-checks.md, không tick nghiệm thu. Hoàn thành cả nhóm mới trình test-plan.md riêng. Duyệt bộ plan này không duyệt đợt nghiệm thu/debug.

Dự kiến thêm tools/database/Invoke-G05Technical.ps1, Test-G05.ps1 và fixtures database/tests/g05/; dùng runner Apply-Migration.ps1 hiện có. Không tạo tài nguyên khi đang lập plan. Khi chạy có manifest runID/server/DB/marker/principal/hash/schema/exe/commands; DB CafeManager_G05_<runId>_Clean/Demo/Legacy, chủ định giữ sốDB tối thiểu bằng tái dùng fixture. SQL runtime principal giới hạn trong DB; không tạo Windows user/đổi auth mode/service/registry. Bảo vệ config DPAPI development và không in bí mật.

FixtureClean=baseline không seed+001–021, bootstrap actor cần thiết và dữ liệu tối thiểu. Demo=baseline+seed một lần rồi migrations, so checksum/số dư và history. LegacyInvalid=clone test cố tình hỏng loại nguồn/liên kết/tổng/khóa cũ để xác minh migration dừng/không ghi version, không sửa dữ liệu. LegacyValid có khoản nợ cũ và tiền nhập chưa rõ nguồn để kiểm giữ nguyên. Phần chứng minh sau007–016 phải đọc actual contract hiện hành, không gọi baseline signature đã thay.

Test từng task giữ source/migration cố định; mỗi assertion đối chiếu bảng liên quan sau lỗi. Barrier hai session với thời điểm/lệnh cụ thể và @@TRANCOUNT/XACT_STATE; không coi Task.WhenAll tự chứng minh tranh chấp. XUnit chỉ quy tắc chuẩn hóa/role/decimal/state có giá trị; nativeWinForms kiểm validation/binding/handler/phím/busy/unknown và nguồn. Harness reflection/component ghi riêng, không gán là chuột/phím thật. Log actual và mã lỗi, không đổi expected để néFAIL. Bug ghi tại task sở hữu, trình cả đợt trước sửa, regression đúng ảnh hưởng.

Lưu test-results.md từng task, report.md chung đủ55ID và UX; các subcase phải ghi thiếu evidence thay vì nâng cảID. Nguồn MaBL kiểm fixture ởT19, tích hợp chi lươngT26 ghi NOT RUN tới đúngtask. Hồi quy G04 kho/checkout/tổngtiền/quyền theo thay đổi, không đánhPASS16ca còn thiếu. Cleanup DB/config/process/file/principal đúngallowlist+marker của lượt chạy sau lưu bằng chứng, kể cảFAIL; manifest ghi trước/tạo/dọn/còn giữ/lýdo/hạn và cách dọn. Không DROP wildcard hoặc chạm development/G04manual.

## 6. Gate duyệt và hoàn tất

Người dùng đã duyệt **cả bộ năm plan một lần** ngày26/10/2026; source đã triển khai theo thứ tự. Đủ đầu ra task trước mới sang task sau, blocker thật báo để quyết định. Sau cả nhóm chờ duyệt test riêng, không xin duyệt từngtask. Không commit/push khi chưa giao. G05 chỉ hoàn tất theo requirements/55ca và phân công; ngoại lệ ghi riêng, không biến NOT RUN/BLOCKED thànhPASS.





## Cập nhật30/10/2026 — Native follow-up và B07

STORE nhập21000/trả ngay0, xem nợ chỉ đọc và xuất trả2 đơn vị PASS; ADMIN trả10000+11000, lịch sử và hai link phiếu chi đúng nguồn PASS. Hai subcase runtime T19-K02.Name51/T20-K02.ActorMissing PASS, rollback0:0/dữ liệu không đổi. Đối chiếu tiền/kho/nợ cuối PASS. Khi đăng xuất Công nợ phát sinh B07 CTS đã dispose, chờ duyệt bổ sung trước sửa; CASH native tiếp theo BLOCKED, native keyboard chưa đủ. Chi tiết [biên bản](runs/native-followup-20261029/test-results.md), [bug B07](t23-cong-no/bug-report.md). Source cố định; đã dọn1/1DB và credentials, development không bị ghi fixture. Không đổi55ID lịch sử hay chốt nghiệm thu toàn nhóm.

## B07 — Đã duyệt, sửa và regression30/10/2026

Người dùng duyệt bổ sung B07; đã sửa vòng đời CTS trong đúng năm control G05, không đổi SQL/quyền/ghi nghiệp vụ. Debug/Release0warning/0error,22/22unit,47/47lifecycle và5/5CASH-BLL PASS. Native chuyển năm màn, nháp modal, đóng lịch sử nợ→logout, CASH login/logout, ADMIN login lại/đóng MainForm PASS theo phạm vi. Counts/receipt/tồn không đổi; source cố định;1DB test dọn1/còn0, credentials đã xóa. [Biên bản B07](runs/b07-20261030/test-results.md), [bug](t23-cong-no/bug-report.md). Nội dung “B07 chờ duyệt” ở cập nhật trước là lịch sử trước phê duyệt. CASH native không còn bị chặn bởi B07; native keyboard vẫn chưa đủ do công cụ. Không đổi55ID lịch sử, G05 Đang làm do các nhánh nghiệm thu còn thiếu.
