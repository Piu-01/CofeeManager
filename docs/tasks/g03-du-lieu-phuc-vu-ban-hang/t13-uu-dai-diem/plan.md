# Plan T13 — Ưu đãi, chính sách hạng và điều chỉnh điểm

**Trạng thái:** Bộ plan G03 đã được duyệt ngày 18/10/2026; đã triển khai, đang làm/chờ nghiệm thu nhóm. **Phụ thuộc:** T12. Áp dụng [plans.md](../plans.md).

## 1. Quyết định và contract

- Migration008 thêm Revision KHUYENMAI và `LoyaltyPolicyState(Id=1, Revision rowversion)`. Hạng giữ CHAR5, mã ADMIN nhập ASCII chữ/số1–5ký tự, uppercase; không remap DONG/BAC/BKIM. KM nhập KM+8chữsố (CHAR10) theo schema; ngày Date typed, cùng ngày hợp lệ. TenKM≤100, tỷ lệ int0..100, end>=start. Delete KM chỉ nếu chưa HOADON tham chiếu; không sửa snapshot khi đổi tỷ lệ/ngày/tên.
- `sp_PromotionList/Get/Create/Update/Delete`, `sp_LoyaltyPolicyGet/Preview/Replace`, `sp_LoyaltyAdjustPreview/Adjust/History`, `sp_DiscountPreview`; actor/stamp/request/reconcile chung. ADMIN mutate tất cả; CASH chỉ lookup KM hợp lệ/preview ưu đãi và đọc hạng/điểm cần bán, không lịch sử điều chỉnh toàn bộ hoặc preview thay chính sách/điểm; STORE không truy cập chính sách/KM/điểm. ADMIN xem history của khách; CASH hồ sơ chỉ điểm/hạng và hóa đơn được phép từ T12.
- TVP `LoyaltyTierInput(Code varchar(100), MinimumPoints bigint, DiscountPercent bigint)` kiểm trước cast; replace toàn policy. Tối thiểu1hạng, ngưỡng nhỏ nhất0, không âm/trùng mã/ngưỡng, điểm<=intmax và %0..100. Exclusive applock policy→policy revision→khách theo tập, recheck expected policy revision. Update/insert hạng trước tính lại; hạng xóa chỉ bỏ sau mọi khách được gán sang hạng còn tồn tại. Validate toàn tập trước mutation; nếu lỗi rollback policy+hạng khách+revision+receipt. Không thay tổng điểm.
- Điều chỉnh dùng số điểm int dương + Loai Cong/Tru và lý do1–100, MaHD NULL cho điều chỉnh tay. Shared policy lock→khóa customer UPDLOCK/HOLDLOCK, kiểm int overflow/thiếu điểm trước INSERT LICHSU_DIEM. Trigger hiện có cập nhật điểm/hạng một lần, procedure không UPDATE điểm lần nữa. Receipt chống lặp; không sửa/xóa lịch sử.
- Trigger TR_LichSuDiem giữ bất biến cũ, bổ sung reject lý do rỗng/whitespace và MaHD khác NULL nhưng không Loai Cong; FK/unique UX_LD_HD và đúng khách/đã trả giữ nguyên. Ghi điểm mua vẫn do checkout, không mở procedure app tùy ý ghi MaHD. T13 bổ sung Shared applock policy đầu checkout baseline trước Recipes/header theo thứ tự thống nhất Policy→Recipes→Header→Customer. T12 Create/Open/Adjust cũng khóa Policy trước customer, Policy Replace không khóa Recipes/header; test hai chiều tránh chu trình. Không dùng SESSION_CONTEXT do client kiểm soát để mở đường sửa lịch sử/số dư.
- Inline function `fn_DiscountPreview` dùng một biểu thức cho tỷ lệ/giảm/phải trả: KM hợp lệ tại ngày lập nếu có; nếu khôngKM dùng hạng hiện tại trước checkout; không khách/khôngKM tỷ lệ0. ROUND(TienMon*TyLe/100,0) ép decimal đủ rộng trước nhân, giảm không gồm ship, phải trả=tiền món+ship−giảm, không vượt decimal18,0. Procedure wrapper validate FK/hiệu lực/số tiền/ngày, trả nguồn/mãhạng/mãKM/tỷlệ/giảm/phải trả/ReadAt/PolicyRevision. Không hợp lệ trả lỗi, không im lặng fallback.
- Request preview chứa TienMon/Ship/ngày/MaKH/MaKM, không là chứng từ đã chốt. T14/T15 gọi lại cùng function bằng tổng dòng snapshot và ngày nguồn thật trong transaction; G03 chưa thay công thức checkout đích hay tạo thanh toán từ preview. UI không nhân/chia/tự làm tròn lại. Preview điều chỉnh điểm cũng từ SQL, không dùng số dư cache làm cơ sở xác nhận; ghi phải kiểm lại concurrency.

## 2. Bước/file và kiểm tra kỹ thuật

| Bước | File | Đầu ra/kiểm tra |
|---|---|---|
| 1 | database/migrations/008_promotions_loyalty.sql | Yêu cầu007, policy state/TVP/function/procedures/trigger và lock baseline; GRANT/DENY; compile và preview/adjust/policy smoke trên DB test |
| 2 | Models/{Dtos,Requests}/Loyalty/*; DataAccess/Repositories/LoyaltyRepository.cs; LoyaltyTierTableBuilder.cs | Typed DateOnly/decimal/int, canonical TVP/receipt, history paging, preview DTO chính thức |
| 3 | Business/Services/LoyaltyService.cs; PermissionService.cs; SqlErrorMapper.cs | Quyền từng thao tác, normalize/validate, lỗi520xx, session/reconcile; không duplicate công thức giảm/hạng |
| 4 | WinForms/Modules/Loyalty/LoyaltyControl.*; Forms/PromotionEditorDialog.*, LoyaltyPolicyDialog.*, PointAdjustmentDialog.*, DiscountPreviewDialog.*; CustomersControl history link; navigation/composition | ADMIN tab KM/hạng/điểm, CASH preview được phép; xem lại chính sách/điểm và xác nhận rõ, không Enter chốt |
| 5 | database/tests/t13/*; tests/CafeManager.Tests/Loyalty/*; runnerG03; docs/database/loyalty-contract.md, migrations.md, write-ownership.md; README | Fixture rollback/concurrency/boundary, technical-checks; hướng dẫn F5 và handoff G04 dùng function/locks |

UI policy grid ngưỡng/% và thông báo tái phân hạng, preview SQL trước xác nhận, diff policy và số khách bị ảnh hưởng do SQL trả. Preview policy trả policy revision và hash payload đề xuất; Replace kiểm lại cả hai, tính lại tập khách dưới khóa Exclusive (số khách ảnh hưởng trong preview chỉ là tại thời điểm đọc, kết quả ghi trả số thực tế). Điều chỉnh hiển thị khách/điểm hiện tại/cộngtrừ/lýdo/kết quả SQL; preview trả customer revision và policy revision, Adjust yêu cầu khớp cả hai, đổi thì tải/xác nhận lại. Không giữ khóa qua bước xác nhận. Tab lịch sử read-only. Không nút Dùng điểm hoặc PT010. Layout policy/recipe dùng mẫu responsive chung, footer cố định/field cuộn, không có stardust.

## 3. Dataset và subcase nghiệm thu sau duyệt test

| ID | Fixture/thao tác | Expected và đối chiếu |
|---|---|---|
| T13-K01 | Tiền món100001/ship15000, khôngKH/KM, chỉhạng, chỉKM, cảhai | Nguồn/tỷlệ đúng, KM thay hạng, ship không giảm |
| T13-K02 | Ngày trước/bắtđầu/giữa/kếtthúc/sau; KM cùngngày; FK sai | Hai đầu/cùngngày hợp lệ, ngoài ngày/FK sai reject |
| T13-K03 | %0/100, tiền món1 và giảm50%, ship0/>0, biên decimal, GUI đổi preview/lỗi | SQL0,5 làm tròn1, giảm100% vẫn trảship; UI giữ nguyên DTO, không dùng preview cũ khi lỗi |
| T13-K04 | Policy min>0/ngưỡng âm/trùng, % -1/101, mãtrùng/dài, dữ liệu vượt int | Reject toàn batch, policy/customer/receipt không đổi |
| T13-K05 | Thay ngưỡng hợp lệ, khách nhiều ngưỡng, xóa hạng đang gán; fault test-only khi reclassify | Hạng mọi khách đúng; điểm giữ nguyên; fault rollback toàn bộ và transaction sạch |
| T13-K06 | Cong/Tru hợp lệ, lýdo rỗng/101ký tự, trừ vượt dư, cộng overflow, retryRequest | Một history/receipt, số dư/hạng đúng, invalid rollback |
| T13-K07 | DML history update/delete; fixture MaHD saiKH/chưa trả/không tồn tại, duplicate mua | FK/trigger/unique chặn, không thay điểm/hạng/receipt |
| T13-K08 | Customer profile sửa tên/SDT; direct UPDATE điểm/hạng runtime | Profile không đổi số dư/hạng, runtime bị deny |
| T13-K09 | Đơn paid fixture snapshot, sửaKM tỷlệ/ngày/tên, reload | HOADON giảm/ship/phải trả/snapshot không đổi; G04 xác nhận checkout đích dùng function |
| T13-K10 | 3role × lookup/preview/policy/adjust/history và auth revoke | Chỉ ADMIN mutate, CASH lookup/preview, STORE chặn; BLL/SQL nhất quán |
| T13-K11 | Dư100,2session cùng trừ80; policyreplace đồng thời adjust/create/open/checkout baseline cả2chiều | Chỉ1trừ thành công dư20, history đúng; policy/hạng nhất quán, không lộ dữ liệu dở dang/deadlock chu trình |

Bổ sung receipt hashkhác/actor khác/timeout trước-sau commit và reconcile khi policy đã thay tiếp; revision policy2editor; migration clean/demo/rerun/preflight, DENY DML, GUI chung. UX-K10 preview và UX-K33 policy/adjust/history; UX-K09/10 tích hợpPOS G04 riêng. Preview không phải bằng chứng checkout chốt đúng; không ghi PASS tích hợp khi chưa chạy T15/T16.

## 4. Checklist gốc giữ nguyên

- [ ] `T13-K01` [I] Không khách/không KM giảm 0; chỉ khách dùng hạng; có KM dùng KM, không cộng dồn.
- [ ] `T13-K02` [I] Ngày bắt đầu/kết thúc đều hợp lệ, chương trình cùng ngày hợp lệ; ngoài ngày bị từ chối.
- [ ] `T13-K03` [I/M] Giảm 0%, 100%, số tiền cần làm tròn 0,5 VND đúng chính sách SQL; UI hiển thị nguyên kết quả preview SQL, không tự tính lại.
- [ ] `T13-K04` [I] Ngưỡng nhỏ nhất khác 0, ngưỡng trùng/âm hoặc % ngoài 0..100 bị chặn.
- [ ] `T13-K05` [I] Sửa ngưỡng áp dụng tái tính khách nguyên tử; một lỗi rollback chính sách và hạng.
- [ ] `T13-K06` [I] Cộng/trừ điểm thủ công ghi lịch sử; trừ vượt số dư rollback, lý do rỗng bị từ chối.
- [ ] `T13-K07` [I] UPDATE/DELETE lịch sử bị chặn; MaHD sai khách/chưa trả và cộng mua hàng trùng bị chặn.
- [ ] `T13-K08` [I] Không UPDATE DiemTichLuy hoặc MaHang trực tiếp qua màn hình chỉnh thông tin.
- [ ] `T13-K09` [I] Chương trình đổi tỷ lệ sau khi chốt không đổi tiền giảm/snapshot đơn cũ.
- [ ] `T13-K10` [U/M] CASH/STORE không sửa chính sách/điểm; quyền ADMIN đúng.
- [ ] `T13-K11` [I] Hai điều chỉnh trừ đồng thời không cùng tiêu số dư; kết quả còn điểm không âm.

Mỗi subcase lưu mã, môi trường/schema/source hash, dữ liệu, lệnh/thao tác, expected/actual, PASS/FAIL/BLOCKED/NOT RUN và evidence tại test-results.md cạnh plan. Bug ghi bug-report.md tại task; report nhóm liên kết và chờ duyệt đợt bug. Chưa đánh dấu hoàn tất từ plan/build/smoke.



Triển khai bổ sung009 cho mãKM seed cũ khi sửa/xóa; không đổi mã hoặc checksum008. Chỉ tạo mới yêu cầu KM+8chữ số.

