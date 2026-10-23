# Contract truy cập G02 / T06

Triển khai tại migration002 và Models/DAL/BLL/WinForms. Chưa nghiệm thu.

- `AppSetupState` singleton: Pending/LegacyReview/Completed, CompletedAt và
  AdminAccountId cùng commit với bootstrap. Completed không mở lại khi khóa
  admin hoặc nghỉ việc. SQL applock transaction bảo vệ first-admin.
- Pending: tạo NV tối thiểu + CVADM nếu thiếu, tạo ADMIN; hoặc chọn NV đang làm
  chưa account; hoặc kích hoạt đúng account ADMIN inactive/placeholder demo.
  Account demo CASH/STORE không tự bật. LegacyReview: verify admin hiện tại rồi
  SQL CAS hash/stamp/role/status và chỉ ghi trạng thái hoàn tất.
- Username ASCII1–50 `[A-Za-z0-9._-]`, trim ngoài chỉ ASCII-space; display giữ
  case, key lowercase CI rồi unique BIN2. Migration dừng khi dữ liệu cũ invalid/
  trùng. Không tự đổi username/mật khẩu hoặc suy role từ chức vụ.
  Boundary SQL bootstrap/auth/legacy-read nhận NVARCHAR(MAX), kiểm input gốc
  dài1–50 ASCII, không cóspace ngoài, rồi mới convertVARCHAR(50). BLL trim
  ASCII-space và lower LoginKey trước khi gọi; SQL yêu cầu giá trị canonical,
  không cắt input51 hoặc best-fit Unicode thành ASCII. Cột/key vẫnVARCHAR50.
- Password mới8–128 ký tự, không trim; IdentityV3 PBKDF2/100000 iterations.
  Request không serialize/log; session chỉ identity/stamp/id/cancellation.
  Rehash CAS hashBIN2 + rowversion; thua CAS đọc/verify lại, không overwrite.
- Protected call recheck active/employed/role/stamp và permission. Thay đổi →
  revoke và quay Login; kết quả muộn không bind vào phiên cũ. Cancellation
  không chứng minh transaction ghi đã rollback.

## Principal và khởi chạy

Migration operator áp dụng001→002→003 bằng Windows Authentication. Provision
Windows identity **riêng** vào CafeManagerSetup qua template
`database/security/provision-setup-user.sql`; runtime theo
`database/security/provision-app-user.sql`/CafeManagerApp. Không dùng runtime
principal để setup, không fallback sang migration credentials.

```powershell
# Chạy process bằng Windows setup identity đã provision, password nhập tại form.
dotnet run --project src/CafeManager.WinForms -- --setup --server localhost --database <DB_da_chon>
```

Setup dùng IntegratedSecurity/Encrypt, chỉ trust local certificate khi server
chính xác localhost. Trước mở form, sp_SetupCheckAccess yêu cầu membership
CafeManagerSetup và không đồng thời CafeManagerApp. Sau thành công đóng setup,
người cài thu hồi membership bằng `ALTER ROLE CafeManagerSetup DROP MEMBER
[Windows identity đã chọn]`; chạy process mới bằng runtime identity.

Runtime đọc CAFEMANAGER_APP_CONNECTION, không chọn DB mặc định. Login chỉ khi
State Completed; thiếu cấu hình hoặc lỗi DB giữ màn Login và báo đúng nguyên
nhân, không mở Main. Setup không đọc chuỗi runtime để kết nối.

## Procedure

| Procedure | Nội dung / principal |
|---|---|
| sp_AppReadState | State/schema metadata, không hash; runtime/setup |
| sp_AuthReadAccount | Credential tạm theo LoginKey; runtime |
| sp_AuthReadIdentity | Identity không hash; runtime |
| sp_AuthRehash | CAS hash/stamp; runtime |
| sp_SetupCheckAccess | Check role riêng trước form; setup |
| sp_SetupListCandidates | EligibleNV/account, không hash; setup |
| sp_SetupReadLegacyAccount | Credential tạm để verify; setup |
| sp_SetupBootstrap | Transaction bootstrap clean/demo/existing; setup |
| sp_SetupConfirmLegacy | CAS admin hiện tại, chỉ ghi Completed; setup |

Không cấp SELECT toàn TAIKHOAN, DML account/state hoặc EXECUTE bootstrap cho
runtime. SQL51401–51407 dịch thông báo thân thiện; không đưa rawSQL/hash ra UI.
Mô hình này không chống người nắm credential DB gọi procedure giả actor; giới
hạn đã chốt trong plan, không có API server. Recovery admin là scope riêng.

## Development cố định (18/10/2026)

Ngoại lệ development được duyệt: `Initialize-Development.ps1` dùng Windows operator
để provision SQL login runtime `CoffeeManager_Development_Runtime` vào đúng DB
`CoffeeManager_Development`, chỉ role `CafeManagerApp`; không tạo Windows account.
SQL login setup riêng tạm thời gọi BootstrapService/contract hiện có rồi bị thu hồi.
Chỉ công cụ provision development kích hoạt hai placeholder CASH/STORE chính xác;
bootstrap của ứng dụng thông thường không tự bật chúng. Không reset account đã active.
Không đổi migration hoặc contract đăng nhập. Cấu hình và bí mật lưu DPAPI CurrentUser;
profile F5 không chứa credential. Xem README và biên bản development-handoff-results.md
trong hồ sơ G02. Quy trình `--setup` thông thường giữ nguyên.

## Ngoại lệ mock data được người dùng duyệt 18/10/2026

Ba mật khẩu ứng dụng demo công khai nằm tại database/demo/accounts.json, hướng dẫn
ở database/demo/README.md; operator đồng bộ hash qua DevelopmentSetup --apply-mock-accounts.
Chỉ DB CoffeeManager_Development có marker, không dùng cho cài đặt thật. Login nhập
username/password bình thường; đã bỏ nút chọn account. SQL credential vẫn ngẫu nhiên
và DPAPI. Baseline/migrations và bootstrap production không thay đổi.
