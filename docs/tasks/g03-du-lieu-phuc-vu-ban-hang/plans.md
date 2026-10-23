# Bộ plan G03 — Dữ liệu phục vụ bán hàng

**Ngày:** 18/10/2026. **Trạng thái:** Đã triển khai T09–T13; người dùng chốt nghiệm thu PASS ngày20/10/2026 với ngoại lệ kiểm thử tại [acceptance-decision.md](acceptance-decision.md).

**Duyệt bộ plan:** Người dùng “duyệt” ngày 18/10/2026 sau khi nhận đủ năm plan và chỉ mục.

## 1. Thứ tự và phạm vi duyệt

| Thứ tự | Plan | Migration dự kiến | Đầu ra cho task sau |
|---|---|---|---|
| 1 | [T09 — Nguyên liệu và mở sổ](t09-nguyen-lieu-mo-so/plan.md) | 004_inventory_materials.sql | Nguyên liệu, tồn và nguồn mở sổ |
| 2 | [T10 — Sản phẩm và công thức](t10-san-pham-cong-thuc/plan.md) | 005_products_recipes.sql | Menu/định mức nguyên tử, giao thức khóa công thức |
| 3 | [T11 — Bàn và khu vực](t11-ban-khu-vuc/plan.md) | 006_tables_areas.sql | Sơ đồ theo view và chọn lại đơn hiện tại |
| 4 | [T12 — Khách hàng và hạng](t12-khach-hang/plan.md) | 007_customers.sql | Hồ sơ, điểm mở sổ, hạng khởi tạo |
| 5 | [T13 — Ưu đãi và điểm](t13-uu-dai-diem/plan.md) | 008_promotions_loyalty.sql + 009_promotion_legacy_codes.sql | Preview SQL, chính sách nguyên tử, lịch sử điều chỉnh |

Mỗi task SQL → DAL → BLL → UI; một task đang triển khai tại một thời điểm. Đọc lại nghiệp vụ/UX và schema/migrations trước code. Bộ plan chỉ xin duyệt một lần; kiểm thử nghiệm thu có đợt duyệt riêng sau triển khai. Không commit/push trong phạm vi này.

## 2. Hiện trạng đã đọc và cách dùng lại

- Source hiện có migrations 001–003, `ConnectionFactory`, `SqlProcedureExecutor`, `SqlParameters`, `SqlErrorMapper`, `CatalogService`, `PermissionService`, `SessionContext`, `NavigationController`, `MainForm` và Orbit controls. Chưa có CRUD nghiệp vụ G03. Không suy ra schema DB đang chạy chỉ từ file; khi triển khai ghi version/checksum thực tế.
- `TableStatusRepository` đã đọc `vw_TrangThaiBan`; `TableStatusService` hiện chưa nhận session/quyền. T11 bổ sung kiểm quyền trên service và cập nhật call sites/tests bị ảnh hưởng, không mở quyền POS cho STORE.
- `PermissionService` chưa cho CASH đọc tồn riêng; T09 thêm quyền đọc tồn hẹp, tách quyền sửa nguyên liệu và ADMIN mở sổ. T10–T13 thêm quyền theo bảng task, không dùng `CatalogAdmin` để vô tình khóa lookup CASH/STORE.
- `sp_CapMa` chỉ hỗ trợ hóa đơn/danh mục/nhân viên. G03 giữ contract đó; mã G03 do người quản trị nhập trong editor theo từng plan, không MAX+1. CASH thêm khách bằng sequence riêng `SeqKhachHang` trong T12, đồng thời ADMIN cũng dùng cùng sequence; không yêu cầu CASH nhập mã kỹ thuật.
- Baseline đã có unique SDT khách không NULL, unique ngưỡng hạng, một lần cộng điểm mỗi hóa đơn, FK lịch sử, trigger cập nhật tồn/điểm. Giữ chúng; không cập nhật số dư lần hai ở C# hoặc procedure điều chỉnh điểm.

## 3. Contract chung chốt cho cả bộ

- Mỗi procedure app nhận `@Actor int`, `@Stamp binary(8)` từ session, gọi guard hiện có để kiểm actor active và role DB. Role được đọc từ DB; không tin role do UI gửi. BLL `DemandAsync` mỗi use case và kiểm phiên sau await. Giữ giới hạn shared SQL credential đã công bố trong kế hoạch, không coi actor/stamp là danh tính SQL độc lập.
- Metadata nhập chuỗi dùng tham số đủ rộng (nvarchar(max)/varchar(max)) rồi kiểm độ dài trước cast, tránh cắt chuỗi âm thầm. Tên trim, không rỗng/whitespace/control; tìm LIKE có escape %, _, [, ~; collation DB hiện tại, phân trang 50 dòng. Request định lượng kiểm precision/scale trước tạo SqlParameter; SQL nhận lượng đầu vào decimal(38,18) và kiểm khớp decimal(18,4), tiền kiểm nguyên VND trước cast decimal(18,0). Không coi ép kiểu TVP là validation.
- Bảng entity G03 có `Revision rowversion` trừ bảng đã có rowversion thì dùng lại. Update/delete yêu cầu expected revision; stale trả lỗi xung đột, không ghi đè. Công thức dùng revision của sản phẩm; chính sách hạng dùng revision một hàng policy riêng. Cập nhật tồn/điểm qua trigger làm revision đổi là xung đột hợp lệ, UI tải lại.
- T09 tạo `G03OperationReceipt(RequestId uniqueidentifier PK, ActorAccountId, Operation, EntityCode varchar(10), PayloadHash binary(32), CompletedAt datetime2(7), ResultRevision binary(8) NULL)` và procedure đọc receipt có quyền. Dùng cho mọi ghi G03 kể cả xóa; receipt ghi cùng transaction và khóa RequestId. Hash gồm payload chuẩn hóa, loại thao tác, entity, expected revision; TVP sắp theo khóa trước hash. Cùng request/cùng payload trả receipt gốc; khác actor/payload bị từ chối. Lưu request trước gửi, timeout → Đối chiếu; receipt chưa thấy chỉ là chưa xác định khi request cũ còn chạy, không tự tạo request mới. Gửi lại cùng request sau đối chiếu dùng khóa receipt để phân xử. Kết quả xóa vẫn tra được; đọc entity hiện tại được ghi rõ khác thời điểm receipt.
- Số dư/mốc/lịch sử/receipt DENY DML trực tiếp cho `CafeManagerApp`; procedure cụ thể GRANT EXECUTE, TVP GRANT EXECUTE/REFERENCES. Không GRANT toàn schema, không quyền migration cho runtime. Lỗi mới dùng dải 516xx–520xx tương ứng T09–T13; map rõ validation/FK/trùng/xung đột/không quyền/chưa biết, giữ lỗi G01/G02.
- Transaction dùng XACT_ABORT, TRY/CATCH rollback, không giữ khóa khi người dùng nhập/xác nhận. Khi đổi policy/điểm, dùng applock `CafeManager:LoyaltyPolicy`: Shared cho tạo khách/mở sổ/điều chỉnh/checkout có điểm, Exclusive cho thay policy. Khóa transaction-owned, timeout hữu hạn trả lỗi thử lại có đối chiếu; rồi khóa khách theo MaKH. T12 đặt giao thức này từ đầu, T13 dùng lại. Checkout baseline được T13 bổ sung khóa này trước khóa header để tránh chu trình policy–khách; T15 phải kế thừa. Không thay công thức checkout đích trước T15.

## 4. UI chung và phạm vi áp dụng Orbit

Đã đọc README → guide → reference.html → tokens → references README → WINFORMS_IMPLEMENTATION → assets README. Dùng AppTheme/OrbitButton/OrbitField/OrbitTabControl hiện có; không mở đợt thay theme toàn app. Segoe UI 10,5pt cho nội dung, màu/token từ guide, spacing 8/16/24 logical px, native DataGridView/ComboBox/TextBox; tiền căn phải `81.000 ₫`, lượng tối đa 4 số lẻ, ngày dd/MM/yyyy.

**Điểm chốt khi duyệt bộ plan:** guide yêu cầu POS hoàn chỉnh trước mở rộng phong cách, còn backlog bắt buộc G03 trước POS T16. G03 áp dụng theme/control hiện có vào danh sách/editor theo thứ tự T09–T13, không triển khai POS sớm hay nhân rộng card/animation POS. Trước dùng mẫu cho T10–T13, kiểm tra kỹ thuật một danh sách/editor T09 thật với loading/rỗng/lỗi, ghi giới hạn; đối chiếu POS đầy đủ giữ tại T16. Đây là điều chỉnh thứ tự áp dụng guide trong bộ plan để bạn duyệt, không tuyên bố POS đã đạt chuẩn.

MainForm dùng minimum outer size cơ sở 640×480 logical px, hạ giới hạn theo WorkingArea/Snap thực khi DPI cao để không chặn nửa màn hình; khi client <900 logical px thu sidebar sang menu gọn, filter/hành động wrap, grid là vùng co giãn và cuộn ngang. Dialog không ép minimum lớn hơn WorkingArea; kích thước mục tiêu 620×460, max theo vùng làm việc trừ viền, vùng field cuộn và footer luôn tiếp cận. T10 công thức mục tiêu 900×620, dưới 800 logical px xếp editor trên/grid dưới. Không thu font để lách DPI. Kích thước cụ thể đo outer/client/WorkingArea khi chạy, không suy từ desktop resolution.

Danh sách: tiêu đề/hành động → filter → state gọn → grid → paging/thời điểm refresh. Editor: field chính → lỗi tại trường → footer Lưu/Hủy; mở sổ/điều chỉnh/chính sách có bước xem lại và nút hành động rõ, không đặt AcceptButton chốt. Esc hỏi bỏ nháp; không đóng khi đang ghi/chưa đối chiếu. Query có cancellation và generation ID; đổi màn/đóng/logout không cập nhật control cũ. Không bụi sao trên bất kỳ nút G03 nào. Không thêm ảnh món giả hoặc đường dẫn asset tuyệt đối.

Điều hướng thêm target cụ thể và map permission trong `NavigationController`; nối dependencies qua `Program.cs`/`ApplicationSessionContext.cs`/`MainForm.*`. Giữ màn bắt đầu CASH/STORE đúng phần chưa triển khai; thêm lối vào chức năng G03 đã có quyền, không gọi chúng là POS/kho giao dịch hoàn chỉnh.

## 5. Kiểm chứng, dữ liệu và tài nguyên

Trong triển khai chỉ build Debug/Release và SQL compile/dependency smoke tối thiểu, ghi `technical-checks.md`; không tick Txx-Kxx. Không chạy suite nghiệm thu trước duyệt đợt test.

Chuẩn bị `tools/database/Test-G03.ps1`, dùng `DatabaseTestTools.ps1`, marker riêng G03 và manifest run ID/server/DB/principal/file tạm/source hash/schema checksum. Dataset clean không seed, demo seed một lần trước 001, legacy/preflight lỗi; không dùng DB development làm fixture. Mỗi lượt tối đa ba DB có tên chính xác trong manifest (clean/demo/invalid tái lập tuần tự), hai connection cho concurrency. Dùng user WITHOUT LOGIN giới hạn role runtime và migration connection operator hiện có, không tạo Windows user/login mới hoặc đổi SQL mode/service/registry.

Sau ghi evidence, finally cleanup đúng allowlist, kiểm marker/ownership/dependency; không DROP wildcard hoặc tài nguyên có sẵn. Giữ DB điều tra phải có chỉ định/lý do/hạn giữ/cách dọn; cleanup lỗi báo DB còn lại, không ghi PASS giả. Kỹ thuật smoke cũng tuân thủ manifest/cleanup này.

Nghiệm thu từng T09→T13, đối chiếu sau lỗi mọi bảng liên quan và @@TRANCOUNT/XACT_STATE trên connection gọi; nguồn cố định trước/sau. Unit xUnit chỉ cho normalize/scale/permission có giá trị; integration SQL Server thật, GUI WinForms thật. Mỗi subcase trong plan có expected/actual/status/evidence riêng. Các ca GUI thêm cho mỗi task: 2 desktop resolution × 3 DPI, cửa sổ thường/maximize/Snap nửa màn; bàn phím/modal/loading/late query/tên dài; Designer save-close-reopen. 100 lần đổi module và 10 login/logout đo số instance/event, GDI handles/timer/connection, không chỉ chụp ảnh. Quy trình khôi phục DPI ban đầu ghi trong manifest.

## 6. Bàn giao và phụ thuộc G04

Từng task cập nhật contract riêng trong `docs/database/`, write-ownership, migrations, README và kịch bản dùng thử G03. Công cụ development áp dụng 004→009 trên đúng `CoffeeManager_Development` có marker sau khi lưu backup/đối chiếu số dư; không tái seed/reset account hoặc ghi fixture nghiệm thu. Thao tác này thuộc triển khai bộ plan nếu được duyệt, dùng principal operator hiện có; app không tự chạy migration. Kịch bản ADMIN tạo dữ liệu mới, CASH đọc/tìm/thêm khách, STORE xem tồn/công thức và quản trị nguyên liệu theo quyền trên cùng profile F5; không in bí mật.

T09-K09, T10-K04/K06–K08 có subcase G03 baseline/fixture và subcase G04 bắt buộc với T14–T16. Báo riêng PASS phần baseline và NOT RUN phần tích hợp tương lai, không đánh dấu toàn ID đã nghiệm thu end-to-end. Report nêu rõ giới hạn này; chỉ đóng task khi đủ phần bắt buộc có bằng chứng theo mốc tích hợp. UX-K06 T11; UX-K09 T12/T16; UX-K10 T13/T16; UX-K33 T13; UX-K39–42 mọi màn. Không sửa 325 ID hoặc nghiệm thu lịch sử G01/G02.



## Ghi nhận triển khai T13 — tương thích mã cũ

Migration009 bổ sung vào T13 vì seed thực tế dùng các mã CHAR10 cũ như KM24BF.
Chỉ tạo mới bắt buộc KM+8chữ số; sửa/xóa giữ mã đang có, không remap khóa hoặc
đổi dữ liệu hóa đơn. 008 đã áp dụng nên giữ nguyên checksum, không sửa hồi tố.
UI dùng OperationEditorDialog chung cho editor G03, các service/contract vẫn
riêng theo task. Không thay phạm vi nghiệp vụ, 47 ID hoặc thứ tự triển khai.



