# Contract nguyên liệu — migration004 / T09

Bảng nguồn `NGUYENVATLIEU`, mốc `MaterialOpening`, receipt `G03OperationReceipt`.
Create luôn tồn0; ADMIN mở sổ trước giao dịch một lần, kể cả lượng0. Legacy được
snapshot cutover không cộng lại tồn, không giả lập tồn trước lịch sử cũ.

List/Get: Actor int, Stamp binary8; Code/Name/Unit/Quantity/OpeningKind/Revision/ReadAt.
List Search escaped≤100, Skip≥0, 50dòng. ADMIN/STORE quản trị; CASH đọc tồn hẹp.
Create/Update/Delete/Open: RequestId GUID, Code varchar rộng, ExpectedRevision binary8
trừ Create; Name≤50/Unit≤20; Open Quantity decimal38,18 được kiểm max14+4số,
Source≤100. Ghi trả Code/Revision. Tất cả actor được recheck ở BLL/SQL.

Receipt cùng transaction; SHA256 payload typed, actor/action/request ID; retry cùng
payload trả kết quả cũ. sp_MaterialReconcile/sp_G03Receipt trả receipt của đúng actor;
chưa có receipt không chứng minh rollback. UI giữ payload/request, khóa đóng/gửi mới,
đối chiếu rồi có thể gửi lại chính request cũ để khóa SQL phân xử.

Rename giữ tồn; đổi unit sau mốc/công thức/giao dịch bị chặn; xóa có nguồn bị chặn.
Runtime DENY DML, ownership chain procedure/trigger; không cần quyền migration.

Kiểm kỹ thuật: runs/technical_20261018_192302_dd088a99 (SQL localhost, clean/demo,
compile004, smoke create→open0,0180→get, dọn2/2DB). Không là nghiệm thu T09-Kxx.
