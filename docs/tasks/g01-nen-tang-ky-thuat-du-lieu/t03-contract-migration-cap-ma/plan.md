# Plan T03 — Contract dữ liệu, migration và cấp mã SQL

**Trạng thái:** 9/9 ca PASS — runtime principal/config09/10 đã xác nhận; chưa nghiệm thu toàn nhóm.  
**Ngày:** 08/10/2026.  
**Nhóm:** G01 — requirements đã duyệt.  
**Phụ thuộc kỹ thuật:** T02-K01–K12 PASS; commit bàn giao chờ cho phép.  
**Nguồn chuẩn:** Kế hoạch 3.1, mục 5 và T03; UX mục 15–17; schema/seed baseline
đã kiểm chứng ở T02.

## 1. Mục tiêu và lựa chọn

- Migration đầu tiên có version và checksum, cấp mã tập trung bằng SQL sequence,
  contract kiểu dữ liệu/trạng thái và ma trận nơi được ghi dữ liệu.
- Tiếp tục trên **DB test T03 mới** tạo từ schema/seed, không sửa hoặc chạy
  migration vào DB ứng dụng/DB nghiệm thu T02. Giữ DB test và bằng chứng sau test.
- Chỉ tạo cơ chế nền và quyền cho đối tượng hiện có. Các procedure tạo đơn,
  tính ưu đãi chính thức, nhập/xuất/chốt lương được bổ sung tại task gốc.

## 2. File dự kiến

| File | Nội dung |
|---|---|
| `database/migrations/001_contract_and_code_generation.sql` | Lịch sử migration, sequence, procedure cấp mã, role app và grants tối thiểu |
| `tools/database/Apply-Migration.ps1` | Đọc SQL/hash, kiểm tra version/checksum, chạy migration transaction và lưu version |
| `database/security/provision-app-user.sql` | Mẫu ánh xạ Windows login đã tồn tại sang DB user/role; tham số runtime, không credentials |
| `docs/database/data-contract.md` | Kiểu request/result, NULL, tiền, lượng, trạng thái, lỗi/timeout và cấp mã |
| `docs/database/write-ownership.md` | Bảng/cột → procedure/trigger/app, quyền hiện có và task bổ sung |
| `docs/database/migrations.md` | Quy trình cài/upgrade, checksum/retry, tách migration principal và app principal |
| `tools/database/Test-ContractMigration.ps1` | Setup DB mới, chạy migration và integration/đối chiếu |
| `database/tests/t03/` | Fixture, SQL assertion, kiểm tra quyền và sequence |
| `docs/tasks/g01-nen-tang-ky-thuat-du-lieu/t03-contract-migration-cap-ma/test-results.md` và JSON/runs | K01–K09, expected/actual và lịch sử regression |

Tái sử dụng helper thực thi GO/snapshot của T02 ở mức cần thiết; không thay
runner T02 chỉ để refactor. Không tạo Models C# trước T04.

## 3. Migration/version

1. Đọc lại toàn baseline, CHECK/PK/identity/trigger và các tên trạng thái.
2. Tạo `dbo.SchemaMigrations`: version PK, checksum SHA-256, thời điểm áp dụng.
3. Runner dùng một connection với transaction, khóa ứng dụng migration để hai
   lượt không chạy DDL cùng lúc; chạy từng batch GO qua command có transaction
   rõ ràng. Không dùng procedure sở hữu transaction nghiệp vụ trong migration.
4. Checksum tính từ bytes file SQL, truyền có tham số, không nhúng hash của
   chính file vào file. Lưu version sau tất cả DDL thành công cùng transaction.
5. Version/checksum trùng: báo already applied, không chạy DDL lần hai.
   Cùng version khác checksum: từ chối rõ, rollback, yêu cầu version mới.
6. Lỗi DDL hoặc ghi version rollback toàn bộ; không lưu version dở. Migration
   được áp dụng trên baseline có seed và baseline rỗng trong DB test riêng.

**Verify:** snapshot 35 bảng trước/sau không đổi (trừ đối tượng/version mới);
FK vẫn enabled/trusted, procedure/view cũ chạy được; retry không đổi sequence.

## 4. Contract cấp mã

- Khởi đầu triển khai cho ba nguồn kiểm chứng: `HOADON.MaHD` (HD + 8 chữ số,
  CHAR(10)), `DANHMUC_SP.MaDM` (DM + 3 chữ số, CHAR(5)), `NHANVIEN.MaNV`
  (NV + 3 chữ số, CHAR(5)). Mỗi nguồn có sequence riêng, NO CYCLE.
- `dbo.sp_CapMa` nhận loại nguồn allowlist, trả một mã typed cùng loại nguồn.
  Không nhận tên bảng/SQL tùy ý; loại chưa hỗ trợ trả lỗi rõ. Các nguồn khác
  bổ sung mapping/sequence tại task dùng nguồn đó theo cùng chuẩn.
- Khởi tạo sequence trên migration bằng max hậu tố số phù hợp của dữ liệu
  hiện có + 1, hoặc 1 nếu rỗng. Đây là bước bootstrap một lần dưới khóa
  migration; runtime luôn NEXT VALUE FOR, không MAX+1.
- Với seed hiện tại, mã HD dạng ngày vẫn được xem là hậu tố số có sẵn; mã mới
  không cam kết ngày trong chuỗi. Không sửa mã seed hoặc PK/FK lịch sử.
- Giá trị 5 ký tự giới hạn 999, 10 ký tự giới hạn 99.999.999; đầy giới hạn trả
  lỗi rõ, không cắt chuỗi/đổi prefix/cycle. Nếu dữ liệu đã tới giới hạn khi
  bootstrap, migration từ chối rõ thay vì tạo sequence trùng.
- Mã lấy rồi nhưng giao dịch rollback có thể bị bỏ trống; không trả số về
  sequence, không bảo đảm liên tục. PK vẫn là lớp chặn trùng khi INSERT.
- Identity (`MaDong`, `MaTT`, `MaTK`, `MaBL`…) vẫn do SQL cấp; không chuyển
  chúng sang mã chuỗi hoặc cho app đoán giá trị tiếp theo.

## 5. Quyền app và ma trận nơi ghi

- Tạo role `CafeManagerApp`, không cho db_owner/db_ddladmin/db_datawriter.
  App principal tách khỏi Windows principal quản trị đang chạy migration.
- Grants hiện có: SELECT hai view, EXECUTE bốn procedure nghiệp vụ và sp_CapMa,
  EXECUTE/REFERENCES TVP thanh toán theo nhu cầu. Bảng tra cứu/query cần thêm
  quyền được ghi rõ trong ma trận và cấp tường minh tại task sử dụng.
- Không cấp DML chung toàn schema. Chặn ghi trực tiếp tiêu hao, lịch sử điểm,
  số dư tồn/điểm, header chốt và phiếu chi hệ thống bằng quyền cột/bảng cần
  thiết; ownership chaining cùng dbo cho phép procedure/trigger hiện có hoạt
  động. Kiểm thử với DB user WITHOUT LOGIN thuộc role qua EXECUTE AS USER.
- Quyền SQL theo connection app không thay quyền ADMIN/CASH/STORE của tài khoản
  nghiệp vụ; quyền BLL được triển khai T06 và task tương ứng.
- Ma trận liệt kê mọi bảng baseline, cột dẫn xuất/snapshot/số dư, nơi ghi hiện
  có, nơi bị chặn và task sẽ mở contract; không giả rằng CRUD app đã tồn tại.
- Mẫu provisioning không tạo login/password mặc định; runtime điền Windows
  login phát triển chuyên biệt. Document local config tách app/migration,
  không đưa secret vào Git/log; TrustServerCertificate chỉ cho localhost dev.

## 6. Chuẩn request/result trong tài liệu

- C# decimal/SQL decimal(18,0) cho tiền VND; decimal(18,4) lượng;
  int số ly/identity; SDT string. Công bố cách validate precision/range trước
  khi gửi, không silently round tiền/lượng ngoài contract.
- Input optional trim rồi rỗng → NULL/DBNull; field bắt buộc không tự chuyển
  thành 0. Ngày/giờ truyền typed, định dạng hiển thị không đi vào SQL.
- Liệt kê chính xác trạng thái từ CHECK, trường computed `ThanhTien`/
  `ThucLinh` chỉ đọc và snapshot không tính lại lịch sử theo giá/công thức mới.
- Request/result của bốn procedure **hiện có**, sequence và query hai view;
  ghi rõ checkout hiện nhận GiamGia đã lưu, tính chính thức thay ở T15.
- Error SQL → lỗi nghiệp vụ/kỹ thuật; kết quả timeout chưa biết phải tra nguồn
  bằng mã đã cấp trước retry, commit rồi mới in. Không mở transaction ở UI.

## 7. Ma trận kiểm thử bắt buộc

| Ca | Thao tác / fixture | Expected / bằng chứng |
|---|---|---|
| T03-K01 [I] | Migration trên DB baseline seed; chụp mọi bảng trước/sau; thêm baseline rỗng và fault DDL | Dữ liệu cũ giữ nguyên, FK trusted; lỗi rollback cả DDL/version |
| T03-K02 [I] | Áp dụng lại đúng file; thử cùng version khác checksum trong fixture | Already applied hoặc từ chối rõ; không tác động sequence/data lần hai |
| T03-K03 [I] | Hai connection SQL cùng cấp nhiều mã HD, đồng bộ bắt đầu; lưu mọi mã trả | Không trùng, không đụng seed, đủ CHAR(10); dữ liệu log danh sách mã |
| T03-K04 [I] | Cấp DM/NV; sequence fixture sát 999 rồi lấy tới/vượt; thử loại không hỗ trợ | Đúng CHAR(5), giá trị cuối hợp lệ; sau đó lỗi rõ, không wrap/cắt/trùng |
| T03-K05 [I] | Lấy mã/tạo header trong transaction rồi rollback; lấy mã tiếp và INSERT mới | Header cũ không còn, mã mới khác; khoảng trống sequence hợp lệ |
| T03-K06 [I] | EXECUTE AS app user, query view/checkout/hủy/trả nợ/kiểm kê fixture hợp lệ | Đọc/execute được; không db_owner/sa, không DDL; gọi procedure tác động đúng |
| T03-K07 [I] | App user thử INSERT/UPDATE/DELETE lịch sử, tiêu hao, điểm/tồn, chốt header và chi nguồn | Bị permission chặn; snapshot không đổi; procedure hợp lệ vẫn ghi qua ownership chain |
| T03-K08 [M] | Review cấu hình development, mẫu provision, grants và file/log | App/migration tách principal; không credentials trong tracked source/log |
| T03-K09 [M] | Đối chiếu tài liệu với sys.columns/CHECK/procedure/TVP và hướng Models | Computed không có setter ghi DB, trạng thái đúng CHECK, typed tiền/NULL đúng |

K06/K07 thực thi thật trên SQL Server với principal giới hạn, không suy diễn từ
GRANT text. Fixture tạo bằng migration/test principal trước khi impersonate;
REVERT trong finally. So sánh tiền/kho/điểm/nợ/trạng thái/liên kết sau procedure,
không chỉ kiểm tra lệnh không ném lỗi. Hai session K03 độc lập, không chạy nối
tiếp rồi gọi là đồng thời. UX-Kxx thao tác Form không thuộc task nền SQL này.

## 8. Thực hiện sau duyệt

Sau khi duyệt bộ plan G01: triển khai migration, runner, contract/docs của T03,
sau đó chuyển T04 theo đầu ra T03. Chưa chạy migration/test vào SQL Server trong
pha triển khai; migration sẽ được áp dụng và kiểm chứng trong đợt test đã duyệt.
Chờ duyệt đợt test G01 một lần rồi chạy K01–K09 với source cố định. Lưu kết quả
tại task, bug tại `bug-report.md`, báo cáo chung ở nhóm và chờ duyệt đợt bug trước
debug/regression. Blocker đầu ra T03 ảnh hưởng T04/T05 phải nêu rõ để quyết định.
Commit/push tiếp tục chờ chỉ định của người dùng.
