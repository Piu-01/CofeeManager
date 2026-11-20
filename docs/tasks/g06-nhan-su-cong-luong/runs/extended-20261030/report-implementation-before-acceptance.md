# G06 — Đã triển khai source, chờ duyệt đợt nghiệm thu

30/10/2026. Requirements và cả bộ plan được người dùng duyệt; đã triển khai tuần tự T24 → T25 → T26. **G06 Đang làm, chưa nghiệm thu/Hoàn tất.** Nguồn chưa commit; chưa nâng DB development, không seed lại hoặc reset tài khoản development. G04/G05 giữ nguyên kết quả/hồ sơ cũ.

## Phần đã triển khai

| Task | Source/SQL và hành vi |
|---|---|
| T24 | Migration022; EmployeeRepository/EmployeeAccountRepository, EmployeeService/EmployeeAccountService; quản trị hồ sơ/account, username/hash hiện có, version, nghỉ việc khóa account, ADMIN cuối; không suy chức vụ thành vai trò |
| T25 | Migration023; AttendanceRepository/Service và tab batch công; typed TVP, kiểm trạng thái cuối, interval qua đêm/mở, employee applock, version và khóa kỳ; batch một dòng lỗi rollback toàn bộ |
| T26 | Migration024; PayrollRepository/Service, tab preview/chốt/chi và chi tiết; SQL tính/snapshot giờ/lương, kiểm lại preview, kỳ bất biến, MaBL liên kết phiếu chi duy nhất, legacy không giả snapshot; thực lĩnh0 không tạo chi |

Models tại `src/CafeManager.Models/{Dtos,Requests}/HumanResources/`; UI tại `src/CafeManager.WinForms/Modules/HumanResources/`. Menu Nhân sự chỉ ADMIN, BLL/SQL kiểm quyền riêng. Editor dùng chung HrEditorDialog thay cho chép nhiều dialog gần giống nhau; hai list hồ sơ/account dùng chung EmployeesControl và EmployeeList/Get, account vẫn chỉ có metadata. Batch một dòng và nhiều dòng dùng chung TVP/đường lưu. Đây là cách tổ chức file trong phạm vi plan, không thêm nghiệp vụ.

Pending G06 dùng file DPAPI scoped theo GUID DB/server/catalog và actor/operation, giữ tương thích đường pendingG05. Không lưu password/hash; cấp/reset sau restart chỉ đối chiếu. Các request đối chiếu lấy request applock trước khi kết luận chưa ghi. Source phiếu chi↔bảng lương được nối trong DocumentLinks/FinanceDetailDialog.

## Kiểm kỹ thuật đã chạy

Không dùng các kết quả dưới đây làm PASS Txx-Kxx.

| Kiểm tra | Actual và bằng chứng |
|---|---|
| Môi trường/build | SDK10.0.401. [Debug/probe](runs/technical_20261030_154603_b2bd4fb6/probe-build.log), [Release](release-build.log), [compile test project](tests-build.log):0 warning/0 error. Chưa chạy dotnet test |
| SQL sạch/demo | Baseline không seed và baseline+seed một lần, migrations001–024 chạy qua SQL Server thật ở localhost, DB test riêng; [manifest](runs/technical_20261030_154603_b2bd4fb6/manifest.json), [clean](runs/technical_20261030_154603_b2bd4fb6/clean-migrations.jsonl), [demo](runs/technical_20261030_154603_b2bd4fb6/demo-migrations.jsonl) |
| Smoke phụ thuộc typed | Dưới DB user G06Runtime/CafeManagerApp qua EXECUTE AS USER, connection không pooling; tạo NV011/accountCASH và receipt; ghi8 giờ; preview/chốt MaBL3 =280.000₫; chi một phiếu TC20250505 gắn nguồn. [Actual](runs/technical_20261030_154603_b2bd4fb6/typed-smoke.json). Owner chỉ bootstrap/provision/đối chiếu fixture, không thay quyền runtime của các lời gọi sản phẩm |
| WinForms component thật | UI thread STA, MainForm content thực. Bốn tab bind11/11/1/1 dòng; grid có bounds dương; module1030×658 với client1280×720 và1116×706 với client1366×768; DeviceDpi120 (125%). Payroll modal owner đúng, HWND owner disabled, ShowInTaskbar=false, native border, AcceptButton=null. Đây là component smoke, chưa là chuột/phím native hoặc đầy đủ handler editor |
| Vòng đời tài nguyên/source | Lượt cuối tạo2 DB/dọn2/còn0; SourceUnchanged=true giữa hai snapshot nguồn. Không ghi fixture vào development. Các lượt kỹ thuật trước cũng có manifest cleanup riêng; không giữ DB sau lỗi |

Lỗi compile/contract được xử lý trong pha triển khai và giữ log: WFO1000 annotation, batch GO trước constraint024, JSON mảng rỗng preview phải trả[]; probe lỗi596 do pooling của phiên impersonation, UUID so không phân biệt hoa/thường; probe owner kiểm HWND thay vì suy từ Form.Enabled. Lượt cuối ở trên thành công; không gọi những lần thử đó là lượt nghiệm thu hoặc sửa bug trong đợt test đã duyệt.

## Nghiệm thu và giới hạn

| Task | ID gốc | PASS nghiệm thu | NOT RUN |
|---|---|---|---|
| T24 | 10 | 0 | 10 |
| T25 | 11 | 0 | 11 |
| T26 | 12 | 0 | 12 |
| Tổng G06 | 33 | 0 | 33 |

[T24](t24-nhan-vien-tai-khoan/test-results.md), [T25](t25-cham-cong/test-results.md), [T26](t26-chot-chi-luong/test-results.md) giữ expected gốc và actual chưa chạy. Không có bug nghiệm thu được kết luận vì đợt nghiệm thu chưa bắt đầu. Concurrency hai session, fault/rollback/timeout/restart, ADMIN cuối đầy đủ, role/session, toàn bộ UI editor/bàn phím/nguồn lồng và nâng cấp legacy bất thường vẫn NOT RUN trong nghiệm thu. Hình thức SKIPPED theo phân công người dùng; DPI100/150%NOT RUN, không đổi Windows Scale.

## Bước tiếp theo

Duyệt **[đợt test G06](test-plan.md) một lần**, sau đó chạy từng task với source cố định, ghi subcase/bằng chứng và báo cáo chung kể cả FAIL/BLOCKED. Có bug thì ghi task sở hữu, tổng hợp đợt để duyệt trước debug; không vá trong lượt test. [Hướng dẫn development](development-guide.md) và script nâng schema024 đã chuẩn bị review, chưa chạy. Không commit/push/publish khi chưa giao.

Sau smoke cuối chỉ chuẩn hóa newline cuối file của4 file tích hợp; không đổi logic. [Delta hash và xác minh chỉ newline](delivery-whitespace.csv). Không chạy lại SQL/UI chỉ vì thay đổi whitespace này.
