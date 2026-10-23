# Plan T11 — Bàn, khu vực và sơ đồ

**Trạng thái:** Bộ plan G03 đã được duyệt ngày 18/10/2026; đã triển khai, đang làm/chờ nghiệm thu nhóm. **Phụ thuộc:** T10. Áp dụng [plans.md](../plans.md).

## 1. Quyết định và contract

- Migration006 thêm Revision BAN/KHUVUC, tên required≤50; mã bàn B0001–B9999 và khu vực KV001–KV999 nhập bởi ADMIN, bất biến khi sửa. TenKV unique theo baseline; không tự áp unique TenBan ngoài yêu cầu.
- Procedures `sp_AreaList/Get/Create/Update/Delete`, `sp_TableList/Get/Create/Update/Delete/SelectCurrent`; receipt/revision/actor theo contract chung. Mutation ADMIN; xem sơ đồ/chọn bàn ADMIN/CASH, STORE bị chặn. CASH chỉ lookup area Code/Name và trạng thái bàn, không editor quản trị.
- Status lấy duy nhất `vw_TrangThaiBan`, DTO hiện có MaBan/TenBan/MaKV/TrangThai/MaHD; thêm ReadAt và revision metadata khi cần. Không thêm BAN.TrangThai hoặc đồng bộ cache màu. SelectCurrent đọc lại view theo MaBan, trả trạng thái và MaHD hiện tại từ cùng statement. Nếu không còn bàn thì báo đã xóa; order đổi thì phản ánh mã mới. Không giữ khóa khi người dùng xem/chọn. T14 tiếp tục kiểm và unique index khi tạo đơn.
- Metadata CRUD khóa row/revision; FK kiểm khi xóa. Xóa khu vực có bàn, bàn có bất kỳ hóa đơn bị từ chối, giữ lịch sử. Không di chuyển hóa đơn hoặc tự tạo/hủy đơn để quản trị bàn.
- Bổ sung SessionContext/PermissionService vào TableStatusService (hiện chưa có), cập nhật call sites/tests; read service cho bảng/view và SQL select procedure đều guard. Sơ đồ G03 chọn bàn chỉ hiển thị trạng thái/mã đơn hiện có và thông báo chức năng mở POS thuộc G04; không tạo đơn giả hay editor hóa đơn ngoài T14.

## 2. Bước/file và kiểm tra kỹ thuật

| Bước | File | Đầu ra/kiểm tra |
|---|---|---|
| 1 | database/migrations/006_tables_areas.sql | Yêu cầu005, rowversion/procedures/quyền; không thay view nghiệp vụ hoặc unique bàn mở; compile và CRUD smoke |
| 2 | Models/{Dtos,Requests}/Tables/*; DataAccess/Repositories/TableRepository.cs, TableStatusRepository.cs | Area/table metadata và typed selection hiện tại, parameterized paging/filter |
| 3 | Business/Services/TableService.cs, TableStatusService.cs; PermissionService.cs; SqlErrorMapper.cs | ADMIN quản trị, CASH đọc/chọn, STORE chặn, session/reconcile, lỗi518xx |
| 4 | WinForms/Modules/Tables/TablesControl.*, TableMapControl.*; Forms/TableEditorDialog.*, AreaEditorDialog.*; composition/navigation | Tab quản trị và sơ đồ trong cùng module; badge có chữ/icon/màu; area filter, refresh rồi chọn |
| 5 | database/tests/t11/*; tests/CafeManager.Tests/Tables/*; runnerG03; docs/database/table-contract.md, migrations.md, write-ownership.md; README | Demo B0003 thật/clean rỗng, technical-checks và đường F5 |

Sơ đồ dùng TableLayout/FlowLayout có cuộn, card bàn hẹp đủ tên2dòng+trạng thái+mã; vùng chi tiết gọn xếp dưới khi client nhỏ. Không thêm hover/animation POS mới hoặc stardust; dùng OrbitButton hiện có. Khi refresh chậm giữ nhãn dữ liệu cũ/chưa cập nhật, không vẽ tất cả bàn trống.

## 3. Dataset và subcase nghiệm thu sau duyệt test

| ID | Fixture/thao tác | Expected và đối chiếu |
|---|---|---|
| T11-K01 | Clean tạo/sửa area+table, reload/restart | Quan hệ và tên đúng DB |
| T11-K02 | Tên rỗng/51ký tự, KV không tồn tại, mã sai/trùng | Reject không cắt chuỗi hoặc orphan |
| T11-K03 | Demo seed một lần trước migrations; đọc view và sơ đồ | B0003 phục vụ, từng bàn khác khớp query view, không hardcode B0003 |
| T11-K04 | Fixture order mở/tất toán/hủy trên các bàn riêng | Chỉ order mở tại quán làm bận; tiền/kho/trạng thái nguồn hợp lệ |
| T11-K05 | Đọc source SQL command và thao tác sơ đồ/refresh | Không truy cập BAN.TrangThai, dữ liệu từ view |
| T11-K06 | Xóa bàn mới/có order mở/đã trả/hủy, KV có bàn | Chưa dùng xóa được; mọi lịch sử giữ nguyên |
| T11-K07 | SessionA xem bàn, sessionB fixture đổi order trạng thái, A chọn | SelectCurrent trả MaHD/trạng thái mới, không tin màu cũ; UX-K06 phần chọn bàn; mở POS G04 |
| T11-K08 | 3role × metadataCRUD/status/select qua service+SQL | ADMIN đầy đủ, CASH đọc/chọn, STORE bị chặn |

Bổ sung conflict2editor, concurrent xóa KV/thêm bàn, receipt/timeout kể cả xóa, migration clean/demo/rerun/preflight invalid, auth revoke/logout/late query và GUI chung. UX-K39–42 cho màn, UX-K06 tích hợp T16 phải lưu riêng NOT RUN trước G04.

## 4. Checklist gốc giữ nguyên

- [ ] `T11-K01` [I/M] CRUD bàn/khu vực hợp lệ và quan hệ khu vực được giữ sau reload.
- [ ] `T11-K02` [I] Tên trống/quá dài/FK khu vực không có bị chặn.
- [ ] `T11-K03` [I/M] Seed B0003 đang phục vụ, các bàn còn lại theo dữ liệu view thực tế.
- [ ] `T11-K04` [I] Đơn đã trả/hủy không làm bàn bận; đơn mở tại quán làm bàn bận.
- [ ] `T11-K05` [M] Không truy vấn/update BAN.TrangThai vì cột không còn.
- [ ] `T11-K06` [I] Xóa bàn/khu vực có hóa đơn/bàn con bị từ chối, không xóa lịch sử.
- [ ] `T11-K07` [M] Chọn bàn refresh và lấy đơn hiện tại, không tin trạng thái màu cũ trên UI.
- [ ] `T11-K08` [U/M] Quyền quản trị danh mục bàn/khu vực và quyền xem POS đúng vai trò.

Mỗi subcase lưu mã, môi trường/schema/source hash, dữ liệu, lệnh/thao tác, expected/actual, PASS/FAIL/BLOCKED/NOT RUN và evidence tại test-results.md cạnh plan. Bug ghi bug-report.md tại task; report nhóm liên kết và chờ duyệt đợt bug. Chưa đánh dấu hoàn tất từ plan/build/smoke.



