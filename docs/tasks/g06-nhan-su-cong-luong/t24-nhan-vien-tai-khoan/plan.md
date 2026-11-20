# T24 — Nhân viên, chức vụ và quản lý tài khoản

Ngày 30/10/2026. **Cả bộ plan G06 đã được duyệt ngày30/10/2026**; requirements nhóm đã duyệt. Phụ thuộc T 23; áp dụng các quyết định chung tại [plans.md](../plans.md). Đã triển khai source; chưa chạy đợt nghiệm thu. Checklist gốc ở cuối file là tiêu chí nghiệm thu, không phải kết quả.

## 1. Đầu vào và quyết định contract

Đã đọc baseline/seed, migrations 001–004/017–021, AuthRepository/AuthService, PermissionService, PasswordService/BootstrapService, SessionContext, Program/MainForm/NavigationController và UI chung. NHANVIEN có TenNV nvarchar 50, SDT varchar 15 NULL unique, hai mức lương decimal 18,0, LoaiHopDong Full-time/Part-time, MaCV và DangLamViec. TAIKHOAN có MaTK identity, unique MaNV, LoginKey canonical và SecurityStamp. Không thêm cột role vào nhân viên.

- Tạo hồ sơ dùng sequence NHANVIEN, không yêu cầu người dùng tự chọn mã. Tên trim bắt buộc 1–50 ký tự, không tab/newline; SDT trim blank→NULL,1–15 chữ số ASCII nếu có. Hợp đồng chỉ hai giá trị chuẩn, hai mức lương nguyênVND không âm; giữ cả hai mức trên hồ sơ, dùng đúng mức theo loại hợp đồng khi chốt, không tự xóa mức không dùng.
- Danh sách hồ sơ tìm mã/tên/SDT, lọc chức vụ/hợp đồng/đang làm; account tab tìm username/mã/tên, lọc role/trạng thái. Trả 50 dòng/trang, TotalRows thật, Code/Name/Phone/Contract/MonthlyRate/HourlyRate/Position/Employed/Revision; account DTO chỉ identity/role/active/stamp, không có hash/password.
- Tạo account cho nhân viên đang làm chưa có account; username theo PasswordService.Username 1–50 ASCII và LoginKey SQL không phân biệt hoa thường. UI không sửa username sau tạo để giữ nhận dạng; được đổi role, khóa/mở và reset. Password 8–128 ký tự, xác nhận khớp, giữ nguyên dấu cách; hash ở PasswordService, DAL chỉ gửi hash tham số. Không có signup công khai, không nút xóa account; giữ lịch sử/receipt, dùng khóa.
- Hồ sơ có Save riêng, nghỉ việc là hành động xác nhận riêng với ExpectedRevision; chuyển DangLamViec=0 và HoatDong=0 nguyên tử. Cho phép chuyển lại đang làm qua thao tác xác nhận, account vẫn khóa tới khi ADMIN mở rõ ràng; không tự phục hồi quyền. Reset không tự mở account hoặc đổi role.
- Xóa chỉ hồ sơ chưa có account/receipt nghiệp vụ/tham chiếu nào; kiểm rõ nguồn chính và để FK là bảo vệ cuối. Có account thì dùng nghỉ việc thay xóa, không xóa account/receipt để lách FK. AppSetupState.AdminAccountId và G03 OperationReceipt là lịch sử được giữ.
- ADMIN hoạt động được định nghĩa roleADMIN + HoatDong 1 + DangLamViec 1 + hash không phải placeholder demo. Khi setupCompleted, mọi thao tác có thể giảm tập này (khóa, đổi role, nghỉ, xóa) phải giữ ít nhất 1. Khi setupPending/LegacyReview, runtime HR từ chối và hướng dẫn setup; không biến việc hết ADMIN thành Pending.
- Mọi write account/nghỉ việc/xóa nằm dưới khóa exclusive CoffeeManager.AdminBootstrap rồi demand/request/nhân viên/row; khóa tập ADMIN và đọc lại trong transaction. Cả hai ADMIN đồng thời tự khóa/giáng quyền chỉ có một thao tác giảm tập thành công. Bảo vệ trigger sau INSERT/UPDATE/DELETE account và UPDATE/DELETE employment cho bất biến cuối; trigger không biến rehash mật khẩu hợp lệ thành lỗi và không chặn bootstrap đúng singletonPending. App không có DML trực tiếp, migration có preflight riêng; không dùng SESSION_CONTEXT làm cờ bypass.
- Employee.Revision rowversion; account dùng SecurityStamp sẵn có. Update chặn version cũ; tạo account khóa nhân viên và unique; thao tác nghỉ dùng cả employee revision và account stamp đã đọc nếu account tồn tại, không vô tình khóa một account vừa đổi mà chưa xác nhận. Thay lương/chức vụ không UPDATE BANGLUONG hoặc account role.
- Request receipts: EmployeeCreate/Update/Delete/Employment và AccountCreate/Change/Reset; kết quả Code hoặc MaTK chuỗi cùng target/stamp metadata cần thiết. AccountCreate/Reset không persist password/hash vào pending hay DTO. Payload hash receipt SQL có thể băm salted password hash cùng metadata để chặn nội dung khác trong cùng lần gửi; salted hash giữ trong memory cho lần gửi đó, xóa khi kết thúc dialog. Sau restart chỉ reconcile request đã lưu, không tự tái tạo hash rồi resend; nếu chắc chưa ghi mới yêu cầu nhập lại và xác nhận request mới.

## 2. Bước 1 — Migration 022 và public SQL

File mới `database/migrations/022_employees_accounts.sql`, transaction runner và yêu cầu schema 021. Preflight tên/SDT blank/trùng sau chuẩn hóa, username/role/FK, employment-account, tập ADMIN khi setupCompleted; lỗi dừng với PK và mã 53107, không normalize phá dữ liệu hoặc reset mật khẩu. Thêm Revision và check tên/SDT cho dữ liệu mới sau preflight. Với account active của NV đã nghỉ: dừng để xử lý có quyết định, không âm thầm khóa dữ liệu cũ. Không yêu cầu cả hai mức lương loại trừ nhau vì baseline không có quy tắc này.

Thêm singleton AppDatabaseIdentity GUID phục vụ pendingG06, DENY DML, không sửa AppSetupState; procedure đọc identity/schema có kiểm ADMIN. Các procedure đề xuất:

| Procedure | Input chính | Kết quả/bất biến |
|---|---|---|
| sp_EmployeeList/Get | Actor/Stamp, search/filter/skip hoặc Code | Hồ sơ/version/tổng thật; account metadata nullable, không hash |
| sp_EmployeeLookups | Actor/Stamp | Chức vụ hiện có, roleADMIN/CASH/STORE; không tự tạo danh mục |
| sp_EmployeeWrite | Action Create/Update/Delete/Employment, RequestId, Code/ExpectedRevision, trường typed; ExpectedAccountStamp khi đổi employment | Transaction/receipt, cấp mã trongCreate, không thay snapshot cũ |
| sp_AccountList/Get | Actor/Stamp, search/filter/skip hoặc employeeCode | Account metadata/stamp, nhân viên; không trả hash |
| sp_AccountWrite | Action Create/Change/Reset, RequestId, employeeCode, ExpectedStamp, username/hash chỉ theo action, Role/Active | Check ADMIN cuối, trả metadata/version một lần; không trộn reset với đổi role |
| sp_HumanResourcesReceipt | Actor/Stamp, RequestId, operation | Request lock rồi kết quả/NotRecorded/StillProcessing; metadata đủ xác minh self-change, không secret |

Tất cả input text dài nhận kiểu rộng, validate trước cast, SQL kiểm role/numeric/FK/unique/ADMIN cuối. sp_EmployeeWrite chỉ thay trường của hồ sơ, sp_AccountWrite chỉ thay account; quyền thuộc ADMIN và setupCompleted. Receipt AccountChange xác định action role/active rõ; tạo/reset receipt gắn target và kết quả mới. Không ghi username/password vào log lỗi raw parameter. Bổ sung lỗi 53101 input,53102 lịch sử/ADMIN cuối,53103 version,53105 miền tiền,53107 preflight; giữ lỗi phiên/request chung.

DENY DML NHANVIEN/TAIKHOAN cho CafeManagerApp; chỉ grant public procedures. Cơ chế sở hữu procedure cho phép bootstrap/rehash vẫn chạy theo quyền hiện tại. Kiểm trigger admin cuối với rehash/bootstrap trong smoke/đợt test, không sửa bootstrap thành đường quản trị chung.

Kiểm kỹ thuật sau triển khaiSQL: migration clean/demo 021→022; chạy lại đúng checksum AlreadyApplied; invalid clone rollback không ghi version; public runtime procedure và denied DML. Ghi riêng technical-checks, không PASS caT24.

## 3. Bước 2 — Models/DAL/BLL

File mới dự kiến:

- `src/CafeManager.Models/Dtos/HumanResources/EmployeeRow.cs`, `EmployeeAccountRow.cs`, `HumanResourcesLookup.cs`; Requests/HumanResources/EmployeeRequests.cs và AccountRequests.cs. Credential request không dùng record ToString mặc định làm lộ mật khẩu: class có trường password phục vụ gọi, không serialize vào pending/log, không giữ trên list/session.
- `src/CafeManager.DataAccess/Repositories/EmployeeRepository.cs`, `EmployeeAccountRepository.cs`: dùng executor AuthRepository/SqlParameter/DBNull, precision rõ; nullable account/SDT/stamp được map đúng. Không gọi sp_AuthReadAccount để xây grid vì procedure đó trả hash.
- `src/CafeManager.Business/Services/EmployeeService.cs`, `EmployeeAccountService.cs`: chuẩn hóa, DemandAsync EmployeeManage cho đọc/ghi/receipt/lookups; PasswordService và xác nhận password trước hash; dịch DataAccessException, revoke nếu 51505; dùng linked CTS disposed sau await. Không tham chiếu WinForms.

Sửa PermissionService thêm EmployeeManage (ADMIN-only theo switch hiện có), SqlErrorMapper thêm 531 xx; DTO receipt có metadata nhưng không secrets. Sau tự đổi account, không gọi Demand cũ ngay để quyết định thao tác đã thất bại: dùng kết quả đã commit rồi báo/đóng và revoke; nếu timeout không hiển thị success giả.

Kiểm kỹ thuật: build Models/DAL/BLL và typed smoke create/get/update account metadata qua runtime trên DB riêng; đọc lương cũ trước/sau sửa hồ sơ không đổi. Chưa chạy xUnit nghiệm thu tại bước này.

## 4. Bước 3 — Module nhân sự và dialog

Thêm `Modules/HumanResources/HumanResourcesControl.cs`, `EmployeesControl.cs`, `EmployeeAccountsControl.cs`, `EmployeeEditorDialog.cs`, `AccountEditorDialog.cs`, `ResetPasswordDialog.cs` và helper HR nhỏ nếu cần. Hai tab đầu trong module; reuse CatalogService cho chức vụ. Không thêm form cho từng bảng; role chỉ selector ba role, không CRUD VAITRO tự do.

Sửa Program.cs composition, NavigationController.cs enum/mapping, MainForm.cs menu/icon/title để chỉADMIN có Nhân sự. Tái sử dụng OrbitRecordListControl/UseStackedSearch và list profile phù hợp; cột lương/SDT typed, tiền format vi-VN, account không có secret columns. Bộ lọc tự áp dụng với applied keyword và tổng 50/page; khóa action khi loading/unknown/selection cũ.

Editor modal native owner đúng chuỗi; trường lương/hợp đồng/chức vụ rõ, account tab độc lập; reset có password che và xác nhận. Xác nhận nghỉ việc nêu ảnh hưởng account/lịch sử, xác nhận đổi role/khóa nêu mất quyền/phiên. Save/Reset không tự gán Enter nguy hiểm; dùng guard nháp, busy, close/esc và select-all chung. Password text chỉ sống trong editor đến khi gửi/xóa/đóng.

Mở rộng `PendingOperation`/`IPendingOperationStore` theo plans.md bằng API scoped opt-in G06; giữ G05 tương thích. Account pending chứa request/target/action/expected version, không plaintext/hash. Nếu không ghi pending được, dừng trước SQL và giữ input. Sau SQL commit nhưng localclear lỗi, hiển thị kết quả từ receipt rồi cleanup riêng, không báo phải reset lại. Khi tự khóa/nghỉ/giáng quyền: success một lần rồi session revoke; Unknown thì chỉ đường đăng nhập lại/ADMIN khác tra request. Không cho phiên revoked tạo dialog mới sau dispose.

Kiểm kỹ thuật: buildDebug/Release, tạo control thật/dữ liệu tối thiểu kiểm binding/owner/disposal 125%; không chụp nghiệm thu hình thức hoặc chạy checklist tự động ngoài smoke.

## 5. Fixture và nghiệm thu sau duyệt đợt test

Tạo `database/tests/g 06/t 24_*.sql`, xUnit `tests/CafeManager.Tests/HumanResources/EmployeeRulesTests.cs`, bổ sung probe/runnerG06. Runtime actors ADMIN 1/ADMIN 2/CASH/STORE; hồ sơ full-time/part-time, nhiềuSDTNULL, SDTduplicate, username case collision/Unicode/51 char, tên 50/51, tiền giới hạn 18 digits và phần lẻ, nghỉ việc có HOADON/CHAMCONG, hồ sơ chưa tham chiếu để xóa. Không hardcode credential thật, không ghi password/hash vào artifact; kết quả verify chỉ boolean/mãcase.

| ID | Chuẩn bị/thao tác và đối chiếu bổ sung |
|---|---|
| T24-K01 | Tạo/sửa cả hai hợp đồng,0/mức lương hợp lệ, đổi chức vụ và reload SQL/DAL/native UI |
| T24-K02 | Mỗi loại invalid riêng, biên length/precision/SDTNULL/duplicate; so số dòng và receipt không đổi sau lỗi |
| T24-K03 | MộtNV/twoaccounts, username case collision và hai session create; chỉ 1 account/receipt thành công |
| T24-K04 | Password 8/128/ngoài biên, confirm sai; reset/hashverify cũfail/mớipass; inspect DTO/grid/artifact không secrets |
| T24-K05 | Nghỉ việc atomically khóa account; history/counts giữ; phiên cũ chặn thao tác mới; quay lạiđang làm không tự mở account |
| T24-K06 | LastADMIN qua khóa/nghỉ/đổi role/xóa, bootstrapCompleted không mở lại; barrier 2 ADMIN tự giảm quyền, rehash/bootstrap không hỏng |
| T24-K07 | Chức vụkhác/rolekhác không liên động; CASH/STORE direct BLL vàruntime SQL bị chặn |
| T24-K08 | Sửa lương/chức vụ không đổi BANGLUONG seed, và hồi quy snapshot mới sauT26 |
| T24-K09 | Mỗi nguồn FK có history/account/setup/receipt bị chặn; hồ sơ chưa tham chiếu xóa được, không cascade |
| T24-K10 | Read/write/reset/lookups/reconcile direct BLL/SQL theo 3 role, filter/reload/pager/lỗi/asyncmuộn; session cũ và self-change |

Thêm subcase chung pendingI/O, requestmismatch, reconcile đangcommit/saucommit/restart/đổiDB, đóngmodal/logout/nativekeyboard 125% trongID liên quan và UX-K01–K04; không tăng số ID gốc. Testsourcecố định, ghi expected/actual và rollback@@TRANCOUNT/XACT_STATE. Hình thứcSKIPPED, DPI 100/150%NOTRUN. Báo cáo nhóm giữ kết quả thật, bug ghi bug-report.md task này, chờ duyệt cả đợt.

## 6. Đầu ra trước khi sang T25

Migration 022/source employee/account đủ và smoke phụ thuộc có bằng chứng; publicDTO không secret, quyền/menu/authbootstrap không bị phá. T24 chưa nghiệm thuPASS; T25 dùngEmployeeService và khóa nhân viên này, không cần chạy hết checklist trước khi sangtask. Hồ sơ technical-checks/test-results tạo đúng pha.

## 7. Phạm vi và checklist gốc

### T24 — Nhân viên, chức vụ và quản lý tài khoản

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Hoàn thiện hồ sơ nhân sự và quyền quản trị tài khoản. |
| Phạm vi | NHANVIEN/CHUCVU/TAIKHOAN/VAITRO; thay lương, nghỉ việc, tạo/reset/khóa tài khoản; chưa chấm công/lương. |
| Quy mô | M — hồ sơ có vòng đời |
| Phụ thuộc | T23 |
| Đầu ra | Module nhân sự/account; ADMIN quản lý được, giữ lịch sử nguồn. |

**Kiểm thử bắt buộc:**

- [ ] `T24-K01` [I/M] Tạo/sửa full-time/part-time đúng mức lương và chức vụ; tên/SDT/FK hợp lệ.
- [ ] `T24-K02` [I] Hợp đồng sai, lương âm, tên rỗng/quá dài, SDT thực trùng bị chặn; nhiều NULL hợp lệ.
- [ ] `T24-K03` [I] Một nhân viên một tài khoản theo unique; username trùng bị chặn.
- [ ] `T24-K04` [M/U] Reset password ghi hash mới, mật khẩu cũ không login; hash/pwd không lộ trên grid/log.
- [ ] `T24-K05` [I] Nhân viên nghỉ khóa account, không xóa lịch sử hóa đơn/chấm công.
- [ ] `T24-K06` [I/M] Không khóa/xóa admin hoạt động cuối cùng; bootstrap không bị mở lại để reset tùy ý.
- [ ] `T24-K07` [I] Đổi chức vụ không tự đổi vai trò; đổi vai trò chỉ thao tác được cấp quyền.
- [ ] `T24-K08` [I] Đổi mức lương không sửa snapshot BANGLUONG kỳ cũ.
- [ ] `T24-K09` [I] Xóa nhân viên đã tham chiếu bị chặn; employee editor không tạo cascade delete.
- [ ] `T24-K10` [U/M] CASH/STORE gọi service quản lý nhân sự/account bị từ chối; reload hồ sơ đúng.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.





