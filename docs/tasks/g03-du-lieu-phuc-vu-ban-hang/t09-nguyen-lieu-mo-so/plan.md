# Plan T09 — Nguyên liệu, đơn vị và mở sổ

**Trạng thái:** Bộ plan G03 đã được duyệt ngày 18/10/2026; đã triển khai, đang làm/chờ nghiệm thu nhóm. **Phụ thuộc:** T08. Requirements G03 đã duyệt ngày 18/10/2026.
Áp dụng toàn bộ contract/UI/tài nguyên/kiểm chứng tại [plans.md](../plans.md); không chạy nghiệm thu trước duyệt đợt test.

## 1. Quyết định và contract

- Migration 004 thêm Revision cho NGUYENVATLIEU; bảng `MaterialOpening` có MaNVL PK/FK, Quantity decimal(18,4), UnitSnapshot nvarchar(20), RecordedAt datetime2(7), Source nvarchar(100), ActorAccountId nullable, Kind (Manual/LegacyCutover), RequestId nullable unique filtered. Mốc bất biến, không sửa/xóa qua app.
- Mọi nguyên liệu có sẵn được ghi một snapshot LegacyCutover bằng tồn/đơn vị hiện tại trong transaction migration; không cộng tồn, không tạo phiếu nhập giả và không gọi snapshot này là tồn trước các giao dịch cũ. Đối chiếu các phát sinh sau mốc theo thời điểm commit/cutover, không theo ngày chứng từ nhập lùi; mốc tương lai T20–T22 phải giữ truy vết nguồn. Ghi trước/sau tồn từng mã, danh sách lịch sử và source migration004. Nguyên liệu cũ khóa mở sổ lại cả khi tồn 0. Đây là cách bảo toàn số dư legacy đã chốt trong plan.
- Nguyên liệu mới có tồn 0, chưa mốc. ADMIN mở sổ từng nguyên liệu một lần, lượng >=0, nguồn/lý do required≤100; khóa row NVL và kiểm không có mốc hoặc tham chiếu CHITIET_PN/PX/KK/TIEUHAO_HOADON. Khóa range trên các nguồn kiểm tra để chặn giao dịch chen vào; transaction ghi mốc và set tồn một lần. Không có “mở sổ lại”; sau giao dịch phải dùng nghiệp vụ kho tương ứng.
- CRUD mã nhập `NVL01`–`NVL99` (CHAR5 phù hợp seed), bất biến khi sửa; hết/trùng báo rõ. TenNVL 1–50, DonViTinh 1–20, trim, không control; quantity 0..99999999999999,9999, tối đa 4 số lẻ. C# giữ decimal, DB kiểm đầu vào trước cast. Sửa tên không đổi tồn. Đổi đơn vị bị chặn nếu đã mở sổ, có công thức hoặc bất kỳ tham chiếu giao dịch; không tự quy đổi dữ liệu cũ.
- Xóa chỉ nguyên liệu chưa có mốc/công thức/giao dịch; không cascade. Nếu không thể xóa, thông báo nguồn đang tham chiếu, không tự thêm inactive column.
- Procedures `sp_MaterialList/Get/Create/Update/Delete/Open/Reconcile`; Actor/Stamp, request/revision theo contract chung. List trả Code/Name/Unit/Quantity/OpeningKind/Revision/ReadAt; không trả giá nhập/nợ. Cấp quyền: ADMIN CRUD+Open, STORE CRUD trừ Open, CASH chỉ List/Get (đọc tồn cần bán); STORE/CASH không mở sổ qua service hoặc gọi SQL guard.
- Tạo G03OperationReceipt và read receipt như plans.md. Migration giữ DENY UPDATE tồn hiện có, bổ sung DENY DML bảng nguồn/NGUYENVATLIEU cho app; ownership chain cho procedure và trigger kho vẫn hoạt động. Không chặn trigger bằng cờ session do client tùy ý đặt.

## 2. Bước/file và kiểm tra kỹ thuật

| Bước | File | Đầu ra và kiểm tra |
|---|---|---|
| 1 SQL | database/migrations/004_inventory_materials.sql | Preflight tên/đơn vị/số dư hợp lệ, yêu cầu003; snapshot cutover/receipt/revision/procedures/GRANT. Compile trên clean/demo test, đối chiếu tồn không đổi và version/checksum |
| 2 Models/DAL | src/CafeManager.Models/{Dtos,Requests}/Inventory/*; DataAccess/Repositories/MaterialRepository.cs | Typed request/DTO, paging, quantity scale4, actor/stamp/revision/request; không trả reader/transaction |
| 3 BLL | Business/Services/MaterialService.cs; Security/PermissionService.cs; DataAccess/SqlErrorMapper.cs | Quyền MaterialRead/MaterialManage/MaterialOpen, CASH đọc tồn, session/recheck; normalize, lỗi516xx và reconcile |
| 4 UI | WinForms/Modules/Inventory/MaterialsControl.*; Forms/MaterialEditorDialog.*, MaterialOpeningDialog.*; navigation/composition theo plans.md | Grid tồn, dialog field không có tồn sửa; mở sổ riêng ADMIN có xem lại nguồn/lượng/đơn vị; mẫu đầu tiên cho màn G03 |
| 5 Fixture/docs | database/tests/t09/*; tests/CafeManager.Tests/Inventory/*; tools/database/Test-G03.ps1; docs/database/material-contract.md, migrations.md, write-ownership.md; README | Fixture riêng, kiểm kỹ thuật tạo→đọc→mở sổ→đọc trên DB test, log technical-checks; kịch bản F5 có thể lặp với mã mới |

UI danh sách có lọc tên/mã, đơn vị và thời điểm đọc; preview quy đổi chỉ gợi ý 18g=0,0180Kg khi đơn vị Kg, giá trị gửi vẫn là lượng người dùng xác nhận. Không cho nhập 18 rồi tự biến thành 0,018. Dialog mở sổ nút Xem lại→Ghi tồn đầu kỳ, Enter không ghi. Áp dụng layout/resize/DPI và guard nháp chung.

## 3. Dataset và subcase nghiệm thu sau duyệt test

Mỗi mục sau phải có actual/evidence riêng; tham chiếu checklist gốc ở mục 4.

| ID | Fixture/thao tác | Expected và đối chiếu |
|---|---|---|
| T09-K01 | Clean ADMIN/STORE tạo NVL mới; lượng mở sổ 1,2345; reload GUI | Lúc tạo tồn0, sau mở sổ giữ4lẻ, tên/đơn vị đúng DB |
| T09-K02 | Open thành công; fault test-only trigger tại ghi mốc/tồn rồi gọi lại trên fixture khác | Thành công có đủ mốc+tồn+receipt; lỗi không có thay đổi/mốc/receipt và transaction sạch |
| T09-K03 | Open lần2; hai session cùng mở; NVL đã có PN/PX/KK/tiêu hao; legacy sau migration | Chỉ1mốc; mọi trường hợp đã phát sinh/cutover bị chặn, tồn giữ nguyên |
| T09-K04 | -0,0001; max và vượt max; 0,00001; unit trống/21ký tự qua BLL/SQL/UI | Reject sai trước cast; max hợp lệ; không cắt chuỗi/làm tròn |
| T09-K05 | Gõ0,0180Kg theo hướng dẫn18g; xem lại/ghi/đọc DB | Cùng lượng và đơn vị, không quy đổi ngầm |
| T09-K06 | Rename; đổi unit trước phát sinh và sau công thức/mốc/PN/PX/KK/tiêu hao | Rename giữ tồn; unit chỉ đổi khi chưa có nguồn, error rõ |
| T09-K07 | 3role gọi service và SQL; editor stale; double-click; timeout trước/sau commit | Ma trận đúng, tồn không có field sửa; một request một hiệu ứng, reconcile đúng |
| T09-K08 | Xóa NVL không nguồn và từng loại FK/mốc riêng | Chưa dùng xóa được; đã dùng không mất lịch sử |
| T09-K09 | Hai session: fixture PN baseline tăng tồn trong DBtest, bấm refresh GUI | Hiện tồn mới từ DB; G04 thêm checkout T15→refresh (NOT RUN ở G03) |

Bổ sung migration clean/demo/rerun/checksum sai/preflight invalid; metadata và số dư không nhân đôi, runtime DENY DML, auth revoke/logout/late async. GUI đủ ma trận chung và Designer; UX-K39–42 áp dụng.

## 4. Checklist gốc giữ nguyên

- [ ] `T09-K01` [I/M] Nguyên liệu mới có tồn 0, tên/đơn vị hợp lệ; số lượng 4 chữ số lẻ không bị mất.
- [ ] `T09-K02` [I] Mở sổ số dư hợp lệ ghi tồn và dấu vết/mốc trong cùng transaction; có lỗi rollback cả hai.
- [ ] `T09-K03` [I] Mở sổ lần hai hoặc sau khi đã phát sinh giao dịch bị chặn.
- [ ] `T09-K04` [I/M] Số âm/quá precision/đơn vị trống bị từ chối, không âm thầm làm tròn sai.
- [ ] `T09-K05` [M] Ví dụ 18g quy đổi nhập 0,018Kg; preview/DB cùng đơn vị.
- [ ] `T09-K06` [I] Sửa tên không đổi tồn; đổi đơn vị sau có công thức/giao dịch bị chặn hoặc qua migration có kiểm soát.
- [ ] `T09-K07` [I/M] User CASH chỉ đọc; ADMIN/STORE không sửa trực tiếp tồn từ editor sau mở sổ.
- [ ] `T09-K08` [I] Xóa nguyên liệu đã được công thức/phiếu/tiêu hao tham chiếu bị chặn.
- [ ] `T09-K09` [M] Danh sách tồn refresh sau giao dịch, không dùng cache làm kết quả chốt.

Mỗi subcase lưu mã, môi trường/schema/source hash, dữ liệu, lệnh/thao tác, expected/actual, PASS/FAIL/BLOCKED/NOT RUN và evidence tại test-results.md cạnh plan. Bug ghi bug-report.md tại task; report nhóm liên kết và chờ duyệt đợt bug. Chưa đánh dấu hoàn tất từ plan/build/smoke.



