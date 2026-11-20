# T30 — Backup, restore và cấu hình cài đặt sạch

02/11/2026 · Plan thuộc bộ G08, **đã duyệt cả bộ ngày02/11/2026**. Phụ thuộc T29. Requirements nhóm đã duyệt; đã viết đầu ra triển khai và kiểm kỹ thuật riêng, chưa nghiệm thu. Áp dụng mọi gate/tài nguyên tại [plans.md](../plans.md).

## 1. Đầu vào và quyết định contract

Đọc lại kế hoạch T30/mục10–13, UX01/UX20/K44; DB_Schema.sql, Insert_Data.sql, migrations001–027 và database/security. Source đã có role, bootstrap bền vững, hash mật khẩu bằng PasswordService. Migration002 tạo ADMIN/CASH/STORE;003 tạo phương thức tiền mặt/chuyển khoản và danh mục chi hệ thống. Bootstrap clean tạo CVADM/nhân viên quản trị theo contract hiện có. Không seed10nhân viên/khách/tồn/điểm để thay cài sạch.

Clean: baseline→001–027→kiểm dữ liệu hệ thống→provision runtime/setup→bootstrap qua --setup→thu hồi role setup→đăng nhập runtime. Demo: baseline→Insert_Data.sql đúng một lần→001–027→xử lý LegacyReview/bootstrap theo contract002→fixture account công khai riêng. Không chạy seed sau migrations hoặc đổi AppSetupState bằng DML để bỏ bước review.

DevelopmentSetup hiện cố định DB development và có lưu bí mật DPAPI; không nới guard hoặc gọi nó để provision DB tuỳ ý. Probe G08 riêng dùng PasswordService/SetupRepository/AuthService và fixture account trong DB test có marker; không dùng account demo trong cài thật. Windows login runtime/setup do operator chọn đã tồn tại; test có thể dùng DB user không login và EXECUTE AS để kiểm quyền SQL, nhưng phần kết nối app phải kiểm với identity runtime thực có giới hạn. Nếu cần SQL login test, chỉ tạo khi Mixed Mode vốn bật, password ngẫu nhiên trong bộ nhớ, ghi manifest và dọn exact login; không tạo Windows user.

## 2. Bước triển khai và file

| Bước | File/đầu ra | Cách làm và kiểm kỹ thuật |
|---|---|---|
| 1. Kiểm dependency | technical-checks.md trong task | Xác minh001–027 liên tục/hash, T29 performance-report và027 lock regression; ghi giới hạn026 benchmark/027 source. Không coi T29-K10 native PASS |
| 2. Bộ cài DB | tools/database/Install-Database.ps1; docs/operations/install.md | Nhận server/DB tường minh, từ chối systemDB/DB tồn tại, validate identifier/quote, kiểm baseline target chính xác rồi áp trên DB mới. Chế độ clean/demo loại trừ nhau, mặc định clean; migration tăng dần/không thiếu-trùng/checksum; lỗi không tự drop DB mà ghi trạng thái/ownership và hướng dẫn tiếp tục an toàn |
| 3. Quyền/bootstrap | docs/operations/security.md; scripts security hiện có làm nguồn | Chỉ dẫn provision Windows runtime và setup riêng, kiểm effective permissions chứ không chỉ tên role; runtime không DDL/backup/restore/ghi bảng được bảo vệ. --setup không nhận password trên commandline, người cài nhập ở hộp hiện có. Thu hồi quyền setup sau hoàn tất; restart/khóa ADMIN không mở bootstrap. Không thay source auth |
| 4. Backup | tools/database/Backup-Database.ps1; docs/operations/backup-restore.md | Chốt server/DB/đường dẫn server-side tường minh, kiểm nguồn tồn tại, COPY_ONLY/CHECKSUM tới file mới, không overwrite backup cũ; VERIFYONLY và metadata backup set. Quyền thiếu báo cụ thể, không tự cấp ACL toàn máy. Log không có connection string/hash mật khẩu |
| 5. Restore | tools/database/Restore-TestDatabase.ps1 | Chỉ đích test mới thuộc manifest; RESTORE HEADERONLY/FILELISTONLY, map mọi data/log file tới vị trí mới trên server; không dùng đường dẫn nguồn/DB thật. VERIFYONLY không thay thử restore. Không REPLACE/ghi đè target tồn tại; kiểm DB_NAME, trạng thái online/schema và sửa mapping user theo identity được chọn, không nâng quyền |
| 6. Snapshot/fixture | database/tests/g08/t30-restore.sql; tools/CafeManager.G08Probe/{Program.cs,OperationProbe.cs,CafeManager.G08Probe.csproj}; tools/database/Test-G08.ps1 | Dữ liệu clean/demo/restore đứng yên; canonical snapshot theo PK, INCLUDE_NULL_VALUES và hash, so tất cả nguồn tài chính/kho/điểm/nợ/lương/trạng thái/liên kết. Kiểm sequence/mã mới không đụng sau restore. Hash mật khẩu chỉ so trong bộ nhớ, không export bảng credential |
| 7. Hướng dẫn operator | README.md; docs/operations/install.md, backup-restore.md, security.md | Nêu server/SQL version/schema thật, thứ tự migration, config runtime, đóng phiên trước restore, tài khoản tối thiểu và đối chiếu. Mất mọi ADMIN: dừng thao tác quản trị, operator xử lý theo quy trình được phê duyệt/backup; không reopen bootstrap, không kèm script reset quyền/mật khẩu tự động |

Không migration mới/DAL/BLL/UI mới theo plan. Nếu kiểm engine phát hiện thiếu dữ liệu hệ thống bắt buộc hoặc contract không đáp ứng, ghi blocker và trình thay đổi cụ thể trước code/migration bổ sung. Script cài là công cụ operator, không thêm lệnh recovery tùy ý.

## 3. Fixture và kiểm kỹ thuật trong triển khai

Chuẩn bị clean không demo; demo baseline seed duy nhất; bản backup từ DB test có giao dịch đủ nguồn; upgrade từ021 và026 tới027 để kiểm bảo toàn, và backup nguồn development thực ở đợt nghiệm thu. Ghi schema nguồn thực thay vì mặc định development027. Không ghi fixture vào development. Smoke kỹ thuật chỉ xác nhận script parse, clean/demo migration và bootstrap public contract dùng được cho T31; lưu riêng technical-checks, không chạy toàn bộ ma trận dưới đây trước duyệt test.

## 4. Ma trận nghiệm thu sau duyệt đợt test

| ID | Thao tác/dataset | Expected và bằng chứng |
|---|---|---|
| T30-K01 | Backup nguồn development đã xác minh, restore vào DB test mới; lặp với DB fixture đủ nguồn | Backup/restore engine thành công, metadata source/target khác nhau, DB online; backup log/manifest không chứa dữ liệu nhạy cảm |
| T30-K02 | Snapshot nguồn đứng yên tại backup và DB restore | Tiền/kho/điểm/nợ/lương, chứng từ và liên kết khớp; ghi expected/actual từng nhóm, không chỉ rowcount |
| T30-K03 | Đường dẫn phía SQL hợp lệ và đường dẫn không được phép | Backup thật ghi được với service account; lỗi quyền/file được báo, không nhầm filesystem client/server; không tự đổi ACL |
| T30-K04 | Clean baseline+migrations, không Insert_Data; cấp setup riêng, tạo ADMIN, restart và login | Đủ3role/danh mục hệ thống; không dữ liệu demo; bootstrap đúng một lần, hash thật, runtime login được; rollback/concurrency/restart theo T06 khi ảnh hưởng |
| T30-K05 | Restore021/026, nâng027 rồi rerun cùng checksum; thử checksum sai trên bản test riêng | Tiền/kho/điểm/chi/lương và liên kết không đổi, version đúng, rerun AlreadyApplied; checksum sai bị chặn, không thay migration để thử |
| T30-K06 | Runtime thật và setup/migration tách biệt; kiểm EXECUTE AS bổ sung | Quyền public use case đủ; DDL/ghi bảng số dư/lịch sử/setup trái phép bị chặn, không db_owner/sysadmin; grant/revoke có evidence |
| T30-K07 | Chạy exe trong môi trường riêng: thiếu config, syntax sai, DB sai/offline, chứng chỉ không tin | Thông báo dễ hiểu, không lộ secret, không fallback; ghi server thực để không hiểu nhầm chứng chỉ local vốn TrustServerCertificate=true; không thay chứng chỉ/mode SQL |
| T30-K08 | Đối chiếu script/tài liệu và chạy quy trình backup/restore | Chỉ SQL BACKUP/RESTORE, không copy MDF/LDF đang mở; hướng dẫn có đích/file/phiên app/đối chiếu |
| T30-K09 | Quét allowlist source/docs/package candidate và evidence | Không credential thật/config local/backup nhạy cảm; mock công khai được ghi nhãn và chỉ thuộc demo |

UX-K44→K03/K06/K07/K09; UX-K01/K02→K04/K07. Ca native phải thao tác thật. Nếu thiếu runtime identity/đường dẫn backup/chứng chỉ phù hợp thì BLOCKED, không đổi máy để né.

## 5. Kết quả và rủi ro kiểm soát

Lưu test-results.md từng subcase với SQL/SDK/OS/schema/hash, expected/actual và links runs; báo tài nguyên tạo/dọn/giữ. Snapshot phải dùng nguồn không phát sinh đồng thời hoặc ghi rõ không thể suy parity tại cùng thời điểm. Backup development có thể chứa dữ liệu riêng: giữ ngoài source/package ở đường dẫn SQL operator chọn. Không xóa backup có trước; file mới chỉ dọn sau xác minh ownership và trong phạm vi lượt. Chuyển sang T31 khi đầu ra kỹ thuật dùng được, không ghi nghiệm thu PASS trong pha triển khai.


