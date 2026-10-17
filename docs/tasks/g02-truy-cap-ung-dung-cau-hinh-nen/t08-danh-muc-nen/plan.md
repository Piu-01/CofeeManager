# Plan T08 — Danh mục nền, dữ liệu hệ thống và selector

Trạng thái: Bộ plan G02 đã duyệt; đầu ra đã triển khai, nghiệm thu T08 10/10IDgốc PASS17/10/2026. ChờbàngiaoGit riêng. Phụ thuộc T07 shell và T06 quyền/session đã nghiệm thu.
Nguồn: requirements G02, kế hoạch T08, UX09/UX-K39/40, schema/seed/migration001.
Đầu ra: 7 tab quản trị, dữ liệu hệ thống DB sạch, lookup/selector có quyền.

## 1. Bước và file

| Bước | File dự kiến | Đầu ra / kiểm tra kỹ thuật |
|---|---|---|
| 1 | database/migrations/003_reference_catalogs.sql | SystemExpenseCategory, PaymentMethodSupport, revision/validation/procedure; DB sạch/demo không mất lịch sử |
| 2 | Models/Dtos/Catalogs/*; Models/Requests/Catalogs/* | DTO/request cụ thể7bảng, lookup hẹp, decimal/TimeSpan, expected revision/request ID |
| 3 | DataAccess/Repositories/CatalogRepository.cs, PaymentMethodRepository.cs | Typed param/read/procedure, tham số phân trang/tìm kiếm, không tính nghiệp vụ snapshot |
| 4 | Business/Services/CatalogService.cs, PaymentMethodService.cs | Validate/quyền/session/contractselector; không UI/SqlClient |
| 4a | DataAccess/SqlErrorMapper.cs | Thêm51501–51508 và thông báo context BLL, giữ mapping G01/T06 |
| 5 | WinForms/Modules/Catalogs/CatalogsControl.*; Forms/CatalogEditorDialog.*, MainForm navigation | 7tab grouped ADMIN; editor native thích ứng fields theo loại; refresh owner/dirty guard |
| 6 | database/tests/t08/*; tests/CafeManager.Tests/Catalogs/*; Test-G02.ps1 phần T08 | Fixtures/runner/mapping subcase đủ7bảng, DB guard, chưa nghiệm thu |
| 7 | docs/database/catalog-contract.md, migrations.md, README và technical-checks.md | Contract mã/hệ thống/selector và lệnh migration001→002→003 |

Một repository/service theo nhóm7danh mục, không generic repository mọi entity
hoặc UnitOfWork. CatalogKind enum whitelist7loại; không truyền table/SQL từ UI.
DAL dispatch các procedure có tên/cột cụ thể, không SELECT* hoặc SQLidentifier
do client nhập. DTO hẹp lookup không reuse DTO quản trị chứa trường tài chính.

## 2. Ma trận quyền chốt đề xuất

D/T/S/X = Đọc tab quản trị / Thêm / Sửa / Xóa. “0/0/0/0” không được mở tab;
lookup use case ở bảng tiếp theo độc lập. BLL bắt SessionContext + recheck T06
trước tất cả calls. ADMIN CRUD vẫn chịu bảo vệ hệ thống/FK/concurrency.

| Bảng | ADMIN D/T/S/X | CASH D/T/S/X | STORE D/T/S/X |
|---|---|---|---|
| DANHMUC_SP | 1/1/1/1 | 0/0/0/0 | 0/0/0/0 |
| NHACUNGCAP | 1/1/1/1 | 0/0/0/0 | 0/0/0/0 |
| PHUONGTHUCTT | 1/1/1/1 | 0/0/0/0 | 0/0/0/0 |
| DOITACGIAOHANG | 1/1/1/1 | 0/0/0/0 | 0/0/0/0 |
| DANHMUC_THUCHI | 1/1/1/1 | 0/0/0/0 | 0/0/0/0 |
| CHUCVU | 1/1/1/1 | 0/0/0/0 | 0/0/0/0 |
| CALAMVIEC | 1/1/1/1 | 0/0/0/0 | 0/0/0/0 |

| Lookup service / trường trả về | ADMIN | CASH | STORE |
|---|---|---|---|
| ProductCategories: MaDM/TenDM | Có | Có | Có (lọc menu/công thức) |
| Suppliers: MaNCC/TenNCC/SDT | Có | Không | Có (nhập/kho/nợ) |
| CheckoutMethods: MaPT/TenPT, chỉ hỗ trợ | Có | Có | Không |
| InventoryPaymentMethods: MaPT/TenPT, chỉ hỗ trợ | Có | Không | Có (lookup, không quyền ghi chi) |
| DeliveryPartners: MaDT/TenDT | Có | Có | Không |
| ExpenseCategories: MaDMTC/TenDMTC/Loai, loại hệ thống chỉ trả cho workflow được cấp | Có | Không | Không |
| Positions: MaCV/TenCV (không PhuCap/HeSo) | Có | Không | Không |
| Shifts: MaCa/TenCa/GioBatDau/GioKetThuc | Có | Không | Không |

Lookup tồn tại khi có use case cần dùng, không thêm màn giả để CASH/STORE xem.
STORE đọc menu categories/nhà cung cấp phục vụ kho, không đọc ChietKhau của đối
tác hoặc PhuCap/HeSo của chức vụ. ADMIN quản trị đọc đủ fields. Đọc financial
fields từ service khác phải có quyền riêng, không reuse lookup mở rộng.

## 3. Mã, validation, dữ liệu hệ thống

### 3.1. Nhập/cấp mã và dữ liệu

| Danh mục | Mã tạo mới | Fields và validation |
|---|---|---|
| DANHMUC_SP | sp_CapMa DANHMUC_SP (DM001–999) trong procedure tạo | TenDM required≤50, unique theo baseline |
| NHACUNGCAP | ADMIN nhập NCC01–NCC99 (prefixNCC+2số, khớp seed) | TenNCC required≤100; SDT required1–15 ASCII digits, giữ0đầu, trimspace; NULL/rỗng bị chặn; unique |
| PHUONGTHUCTT | ADMIN nhập PT001–PT999, PT010 reserved | TenPT required≤50 unique; tạo mới chưa hỗ trợ |
| DOITACGIAOHANG | ADMIN nhập DT001–DT999 | TenDT required≤50, ChietKhau decimal(5,2)0–100 |
| DANHMUC_THUCHI | ADMIN nhập TC001–TC999, reservedTC005/011/012 | TenDMTC required≤50, Loai Thu/Chi typed enum |
| CHUCVU | ADMIN nhập CV001–CV999; CVADM chỉ setup T06 | TenCV required≤50; PhuCap decimal(18,0)≥0 nguyênVND; HeSo decimal(5,2)≥1 |
| CALAMVIEC | ADMIN nhập CA001–CA999 | TenCa required≤30; GioBatDau/GioKetThuc time phút, không bằng; end<start nghĩa qua ngày kế tiếp |

Không đổi mã PK khi sửa. Mã uppercase ASCII, trimspace, kiểm format/range/trùng;
không MAX+1; không mở rộng sp_CapMa. Giới hạn NCC99 là lựa chọn hữu hạn theo
CHAR5/seed, hết mã báo lỗi rõ, không tự đổi schema. Mã hiện có hợp lệ ngoài
format tạo mới (CVADM) được đọc/sửa trường được phép, không tự remap lịch sử.
Required tên trim theo contract, whitespace-only bị chặn; C# chặn tab/control
đầu vào, SQL CHECK/validation kiểm lại required/length. Preflight dữ liệu cũ
invalid dừng rollback, không tự normalize lịch sử. Tìm tiếng Việt dùng query
param LIKE escaped '%'/'_'/'[' dưới collation hiện tại, giữ dấu; tìm chính xác
tên có dấu đảm bảo, không tự áp đặt accent-insensitive search. Paging50dòng.
Decimal quá precision/scale từ chối trước SQL, không âm thầm round. Ca qua đêm
chỉ lookup/giờ hiển thị, không tính chấm công/lương T25/T26.

### 3.2. Mã hệ thống đề xuất và migration

Migration003 tạo dbo.SystemExpenseCategory(UseCase varchar(20) PK,
MaDMTC char(5) UNIQUE FK DANHMUC_THUCHI), 3mapping tường minh:

| UseCase | Mã | Tên mặc định nếu thiếu | Loại | Trường ADMIN được sửa |
|---|---|---|---|---|
| Payroll | TC005 | Lương nhân viên | Chi | Chỉ TenDMTC |
| SupplierDebt | TC011 | Trả nợ nhà cung cấp | Chi | Chỉ TenDMTC |
| PurchaseImmediate | TC012 (mới đề xuất) | Trả ngay nhập hàng | Chi | Chỉ TenDMTC |

TC005/011 được đối chiếu seed; sp_TraCongNo dùngTC011. TC012 chưa có trongG01;
đề xuất mã riêng thay TC008 chi phát sinh để không đổi ý nghĩa lịch sử.
Preflight TC012 nếu đã tồn tại bất kỳ dữ liệu do người dùng tạo và chưa mapping
PurchaseImmediate: dừng xung đột (không đoán ý nghĩa theo tên). TC005/TC011 có
loạiChi giữ tên hiện tại hợp lệ, không overwrite; loạiThu hoặc tên rỗng thì
dừng để xử lý riêng. Nếu thiếu bản ghi tạo tên mặc định/mapping trong migration
transaction. Tên mặc định va chạm unique (nếu contract sau này có) báo lỗi,
không đổi tên để né. Chạy lại qua version/checksum runner → AlreadyApplied,
không seed lại, không reset cấu hình đã đổi tên hợp lệ.

Procedure và trigger set-based trên DANHMUC_THUCHI chặn thay PK/Loai hoặc DELETE
của3mã hệ thống ngay cả chưa có PHIEUTHUCHI; giữ trigger lịch sử baseline.
Mapping table runtime không DML; ADMIN service cũng không đổi mapping. Trường
tên được sửa không đổi sources/historic amounts; procedure snapshot không đổi.
Mọi danh mục có FK lịch sử xóa bị chặn, không cascade. Phương thức được dùng
không xóa; child Support row chỉ được xóa cùng phương thức chưa dùng trong
procedure, không CASCADE xóa lịch sử. CVADM khi chưa có NV cũng không cho đổi
mã (PK invariant); xóa nếu FK còn thì bị chặn, không là mã chi hệ thống.

### 3.3. CRUD và concurrency contract

Thêm CatalogRevision rowversion vào7bảng để editor gửi ExpectedRevision.
Tạo procedure List/Get/Create/Update/Delete cụ thể từng danh mục, actor MaTK/
ExpectedSecurityStamp từ BLL, checked DB HoatDong/DangLamViec/ADMIN trong cùng
write transaction (không tin MaVT request). Check expected revision khi sửa/xóa,
không overwrite nếu stale. Read lookup không trả financial fields trái quyền.
Role SQL runtime EXECUTE procedure, không DML trực tiếp7bảng/mapping/support;
giữ G01 DENY balance/history và execute procedure financial đã có.

Create mã tự cấp (DM) nhận RequestId GUID; dbo.CatalogCreateReceipt(RequestId
PK,ActorAccountId,Kind,EntityCode,PayloadHash char64,CreatedAt) để ghi receipt
cùng transaction, payload hash chỉ fields danh mục không secret. Cùng actor/
request/payload trả mã đã commit; request reused khác actor/payload báo Conflict.
PayloadHash riêng tạo DM do SQL tính SHA2_256 trên bytes Unicode TenDM đã
chuẩn hóa của request, không nhận hash tùy ý từ UI; cùng RequestId khóa bằng
unique key và UPDLOCK/HOLDLOCK trong transaction trước cấp mã.
Các bảng nhập mã cố định đối chiếu bằng key/payload sau timeout, không tạo lại
bằng key mới. Update/delete đối chiếu key và expected/newrevision; timeout
unknown chỉ unlock khi read xác nhận trạng thái hoặc người dùng reload. Không
outer C#transaction. Write action không giữ transaction lúc người dùng nhập.
Receipt không generic audit mọi module; chỉ giải quyết unknown-create DM.

## 4. Phương thức được hỗ trợ

Tên procedure catalog theo template cụ thể sp_Catalog{Kind}List/Get/Create/
Update/Delete với Kind=ProductCategory/Supplier/PaymentMethod/DeliveryPartner/
ExpenseCategory/Position/Shift. SQL chỉ nhận field của đúng bảng, MaTK và stamp
cho write, ExpectedRevision cho update/delete; không table name hoặc SQLtext
parameter. sp_PaymentMethodSetSupport và sp_PaymentMethodListSupported là
contract riêng; lookup projections dùng procedure đọc theo bảng không lộ
financial fields. Return row/Code/Revision typed; SQL51501 validation,
51502 protected system row, 51503 revision conflict, 51504 unsupported method,
51505 unauthorized/stale actor, 51506 code conflict, 51507 migration data
conflict, 51508 idempotency mismatch. Error unknown giữ RequiresReconciliation.

Thêm dbo.PaymentMethodSupport(MaPT char5 PK/FK PHUONGTHUCTT, IsSupported bit
NOT NULL DEFAULT0), CHECK MaPT<>PT010 OR IsSupported=0. Runtime không DML;
ADMIN qua procedure + recheck permission/ExpectedRevision của phương thức.
SetSupport phải cập nhật revision PHUONGTHUCTT cùng transaction để editor stale
không ghi đè cấu hình hỗ trợ mới.

- DB sạch tạo PT001 Tiền mặt, PT002 Chuyển khoản ngân hàng nếu thiếu, hỗ trợ
  ghi nhận **thủ công đã xác nhận**; code-based defaults, không dựa vào tên.
  Bản ghi có mã đúng giữ tên hiện tại hợp lệ. Tên unique đã dùng bởi mã khác →
  migration báo xung đột, không rewrite. Seed demo PT003–009 giữ dữ liệu nhưng
  mặc định chưa hỗ trợ; PT010 giữ để tương thích lịch sử, support0.
- Phương thức ADMIN tạo mới support0, phải chọn rõ “Cho phép ghi nhận thủ công
  đã xác nhận bên ngoài” để bật. Không tích hợp online/gọi gateway. Tên bất kỳ
  không cấp support; đổi tên không đổi support. PT010 không thể bật.
- Selector CheckoutMethods chỉ ADMIN/CASH, lấy IsSupported1 AND MaPT<>PT010;
  InventoryPaymentMethods chỉ ADMIN/STORE, cùng tiêu chí dữ liệu. ValidateMethod
  kiểm một mã tại service/contract theo operation permission; IDlạ/unsupported/
  PT010 bị từ chối, không chỉ hide UI. Không dùng CheckoutMethods cho quyền ghi
  chi STORE; không implement checkoutT15/nhậpT20/chiT26.
- Các procedure checkout baseline chưa đổi tại T08; cấm PT010 trong contract
  mới service, T15 sẽ phải gọi selector/validation và kiểm SQL khi triển khai
  checkout. Không tuyên bố baseline SQL đã enforce contract selector tương lai.

## 5. UI, fixtures và kiểm tra kỹ thuật

CatalogsControl7tab ADMIN, grid explicit columns/headers theo AppTheme. Filter
search, pagination, Thêm/Sửa/Xóa; editor dialog native fields phù hợpKind,
ErrorProvider sát input, tiền định dạng vi-VN nhưng paramdecimal. Mã read-only
khi sửa; mã hệ thống Loai readonly và nút xóa disabled + service/SQLguard.
Phương thức có checkbox hỗ trợ rõ nhãn thủ công, PT010 không bật. Save async,
khóa gửi lặp, conflict giữinput, timeout dùng contract đối chiếu; Cancel không
ghi. Owner refresh đúngtab/filter, dirty guard T07.

Fixtures SQL thật qua Test-G02.ps1 TaskT08: clean+001+002bootstrap+003,
demo+001+002bootstrap+003; migration rerun; actors3role. Historical fixtures
FK tối thiểu SANPHAM/PHIEUNHAP/CHITIET_THANHTOAN/HOADON/PHIEUTHUCHI/NHANVIEN/
CHAMCONG phục vụ delete/snapshot, chỉDBtest không implement nghiệp vụ future.
Preflight clone có codeTC012 conflict/TC011saiLoai và username valid baseline.
Không seeddemo lên schema đã có migration, không fake PASS bằng sửa expected.
Kiểm tra kỹ thuật: compile003 và một CRUD/read smoke để xác nhận shell/editor
nối được; ghi riêng. Toàn bộ matrix sau chỉ chạy khi duyệt đợt nghiệm thu.

## 6. Ma trận nghiệm thu bắt buộc

Các hàng ký hiệu `{kind}` phải mở thành7subcase riêng ở biên bản:
DM/NCC/PT/DT/TC/CV/CA. Không báo một PASS cho7bảng. Fixture/thao tác/expected
đã chỉ rõ, actual/status/evidence riêng khi chạy; hiện tất cả NOT RUN.

| ID | Fixture / thao tác | Expected |
|---|---|---|
| T08-K01.{kind}.a | Thêm hợp lệ→sửa tên→tìm→đóng/mở app | Dữ liệu persist, grid/DTO đúngfields7bảng |
| T08-K01.{kind}.b | Tìm tên tiếng Việt và input chứa quote/%/_/[ | Kết quả đúng tìm literal, không SQLinjection |
| T08-K02.{kind}.a | Required tên rỗng/space/tab, maxlen/maxlen+1 | Biên hợp lệ lưu, invalid từ chối rõ, không truncation |
| T08-K02.{kind}.b | Mã trùng/malformed, service/SQL direct | Unique/validation đúng, không ghi nửa chừng |
| T08-K02.ncc | SDT NULL/rỗng/trùng, 0đầu/length15/16 | NULL/rỗng/trùng/16 chặn; 0đầu giữ; không dùng optional NVcontract |
| T08-K02.nv | Bootstrap NV SDTNULL haiNV và SDTtrùng | NULL permitted theo filtered index, nonNULL duplicate chặn; truy vết T06-K01.a |
| T08-K02.fk | Fixture SP có MaDMkhông có; actorMaTKlạ/expectedstamp sai | FK sai bị chặn; writecatalog không có actor/quyền hợp lệ không ghi |
| T08-K03.a | DT tỷ lệ-1/100.01; CV phụ cấp-1/hệ số0.99; CA start=end | C# và SQL từ chối, DB không đổi |
| T08-K04.a | DT0/100; CV0phụ cấp/1hệ số | Biên hợp lệ persist không round bất ngờ |
| T08-K04.b | CA22:00→06:00; giờ bằng nhau | Qua đêm hợp lệ, cùng giờ bị chặn |
| T08-K05.{kind}.a | Xóa rowchưa dùng rồi row cóFKfixture | Chưa dùng xóa; có lịch sử từ chối, không cascade |
| T08-K06.a | DB sạch không seed→migrations/bootstrap | Role/TC005/TC011/TC012/PT001/002 đủ; không seeddemo |
| T08-K06.b | Demo migration→đổi tên hợp lệ→runner003lần2 | AlreadyApplied, không trùng/reset cấu hình/history |
| T08-K06.c | TC011LoaiThu/TC012preexistingconflict/tênPTuniqueclash | Migration rollback và báo xung đột, không overwrite |
| T08-K07.a | Chưa history, đổi mã/Loai/xóa3mãhệthống từ UI/service/rawSQLfixture | Đều chặn; sửa TenDMTC allowed, mapping không đổi |
| T08-K07.b | PT đã dùng, đổi hỗ trợ/rename rồi thửdelete | Support/rename hợp lệ giữhistory, delete bị chặn |
| T08-K08.a | Selector 3role với PT001/002,003unsupported,010,IDlạ | Chỉ supported hợpquyền; ValidateMethod chặn010/unsupported/lạ |
| T08-K08.b | ADMIN thêmPTsupport0→bật→rename→tắt | Selector theo support, không theo tên; PT010bật chặn |
| T08-K09.{kind} | 3role gọi read/add/update/delete trực tiếp | Đúng ma trận4quyền, CASH/STORE không quản trị |
| T08-K09.lookup | Lookup3role và probeDTOfields | Đúng bảngquyền/fields, không ChietKhau/PhuCap/HeSo lọt qua |
| T08-K10.a | Đơnpaid có snapshotDT, sửaChietKhau hiện tại | SnapshotPhanTramPhiDoiTac/PhiDoiTac/orderfinance không đổi |
| T08-K01.concurrent | Hai editor revisioncũ, update/update hoặcupdate/delete | Một write thành công, stale conflict giữinput/không overwrite |
| T08-K01.unknown | CommitcreateDM rồi ngắt response/timeout; đối chiếuRequestId | Một row/receipt, không cấpDMmới hoặc báo success trước đối chiếu |

Unit meaningful: validation money/ratio/ca/permission; integration CRUD/
constraint/procedure/state/transaction/selector; UI của7tab áp dụng ma trận T07.
SQL tests không tăng quyền runtime để né DENY; fixtures dựng bằng owner DBtest
riêng, thao tác ứng dụng qua role hạn chế. Count/hash/history trước-sau và
@@TRANCOUNT sau lỗi phải vào evidence. T08-K09.{kind} tách3role×4thao tác,
T08-K09.lookup tách từng lookup×role; K02.ncc/K03/K04 ghi từng biên riêng,
không một PASS chung thay biên chưa chạy. Snapshot/fault fixture chỉ dựng
trên DB test, không cập nhật expected để né lỗi. Chỉ hoàn tất khi10IDgốc và mọi subcase
PASS, đầy đủ đầu ra/biên bản/bàn giao được giao riêng.
