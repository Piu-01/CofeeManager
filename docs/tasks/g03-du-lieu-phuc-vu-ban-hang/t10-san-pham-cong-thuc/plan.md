# Plan T10 — Sản phẩm và công thức

**Trạng thái:** Bộ plan G03 đã được duyệt ngày 18/10/2026; đã triển khai, đang làm/chờ nghiệm thu nhóm. **Phụ thuộc:** T09. Áp dụng [plans.md](../plans.md).

## 1. Quyết định và contract

- Migration005 thêm Revision SANPHAM, TVP `RecipeLineInput(MaNVL varchar(100), Quantity decimal(38,18))`: kiểm mã dài/FK/trùng/lượng trước cast sang CHAR5/decimal18,4. Mỗi sản phẩm một revision bao trùm metadata+công thức; thay công thức luôn bump revision sản phẩm ngay cả payload cùng giá trị. MaSP nhập SP001–SP999, TenSP≤50, GiaBan nguyênVND>0, MaDM hợp lệ; mã không sửa.
- `sp_ProductList/Get/Create/Update/Delete/ReplaceRecipe`, DTO `ProductRow/ProductDetails/RecipeLine`, requests metadata và danh sách công thức typed. Tạo mới có thể Ngừng bán/chưa công thức; bật Đang bán chỉ nếu công thức ít nhất1dòng hợp lệ. Create kèm recipe và bật bán là một transaction; ReplaceRecipe không được xóa hết recipe món đang bán. Cho món ngừng bán giữ recipe; không xóa dòng đơn mở.
- Thay toàn bộ công thức: validate tất cả đầu vào trước delete, transaction khóa product và range recipe, delete/insert cả tập, receipt/revision nguyên tử. NVL đã có công thức không đổi đơn vị. Không cập nhật tiêu hao hoặc giá hóa đơn cũ.
- Phối hợp checkout baseline: migration005 ALTER `sp_ThanhToanHoaDon` tối thiểu để lấy Shared transaction applock `CafeManager:Recipes` trước khóa header, giữ đến commit. ReplaceRecipe và các thao tác có đổi recipe/trạng thái sản phẩm lấy Exclusive cùng resource trước khóa product. Đây là khóa chung đơn giản cho quy mô một quán; không đổi tính ưu đãi/thanh toán/điểm hoặc signature baseline. T15 kế thừa giao thức; fixture không giả checkout đích. Metadata cập nhật trạng thái/giá cũng dùng Exclusive để baseline không đọc trạng thái dở dang; giá hóa đơn vẫn snapshot.
- Preflight món đang bán nhưng thiếu recipe: dừng migration với danh sách mã để xử lý được duyệt, không tự ngừng bán hoặc bịa công thức. Giữ baseline/seed nguyên vẹn. Xóa SP chưa lịch sử: xóa recipe phụ thuộc trong cùng transaction sau kiểm HOADON detail; có lịch sử thì chặn. Không tự cascade lịch sử.
- ADMIN CRUD/recipe; CASH chỉ đọc menu bán (MaSP/TenSP/MaDM/GiaBan/TrangThai, không recipe nếu không cần); STORE đọc menu và recipe (mã/tên/đơn vị/định mức), không sửa. BLL và SQL kiểm riêng thao tác. Selector chỉ trả món đang bán+có recipe, màn admin xem cả ngừng bán.

## 2. Bước/file và kiểm tra kỹ thuật

| Bước | File | Kiểm tra đầu ra |
|---|---|---|
| 1 | database/migrations/005_products_recipes.sql | Yêu cầu004; TVP/procedures/revision, lock baseline, DENY DML product/recipe + GRANT cụ thể; compile và smoke thay recipe nguyên tử |
| 2 | Models/{Dtos,Requests}/Products/*; DataAccess/Repositories/ProductRepository.cs; RecipeTableBuilder.cs | TVP typed, thứ tự canonical để hash, không loop ghi mỗi dòng, không tính tồn chính thức |
| 3 | Business/Services/ProductService.cs; Security/PermissionService.cs; SqlErrorMapper.cs | Metadata/recipe validation, quyền517xx, session/reconcile, STORE không save |
| 4 | WinForms/Modules/Menu/ProductsControl.*; Forms/ProductEditorDialog.*, RecipeEditorDialog.*; composition/navigation | Grid lọc danh mục/trạng thái; editor và recipe grid NVL/lượng/unit, có nháp và xem lại bật/ngừng bán |
| 5 | database/tests/t10/*; tests/CafeManager.Tests/Products/*; runnerG03; docs/database/product-contract.md; docs/database/migrations.md, write-ownership.md; README | Fixture snapshot/hai session, technical-checks, F5 ADMIN tạo món+recipe/STORE đọc/CASH menu |

Layout dùng mẫu T09 đã kiểm kỹ thuật; recipe dialog 900×620, xuống màn nhỏ dùng vùng cuộn/grid, footer không bị che. Thêm NVL trùng đưa focus dòng hiện có, lỗi ghi rõ dòng; không tự bỏ dòng lỗi. Chưa có ảnh riêng sản phẩm thì không tự gán ảnh demo thành ảnh thật. Không tạo card POS ở task này.

## 3. Dataset và subcase nghiệm thu sau duyệt test

| ID | Fixture/thao tác | Expected và đối chiếu |
|---|---|---|
| T10-K01 | Tạo món ngừng/bán có2NVL, reload GUI/DB | Metadata/recipe đủ và cùng revision sau ghi |
| T10-K02 | Giá0/-1/lẻ, định mức0/âm/5lẻ/vượt max, FK sai/trùng MaNVL/chuỗi dài | Reject cả payload; không silent cast/round |
| T10-K03 | Recipe cũ2dòng; thay có dòng lỗi; inject fault test-only giữa delete và insert | Recipe cũ/revision/receipt giữ nguyên, transaction sạch |
| T10-K04 | Món thiếu recipe bật bán bị chặn; fixture baseline order thiếu recipe checkout; bánh NVL thành phẩm lượng1 | Baseline từ chối thiếu recipe; bánh checkout tiêu hao1; tích hợp selector/checkout T14–16 là subcase G04 |
| T10-K05 | Ngừng món đang nằm trong order fixture; đọc selector và order | Selector loại món, dòng order còn nguyên; G04 kiểm POS cảnh báo/sửa đơn |
| T10-K06 | Order snapshot giá cũ, đổi menu, đọc CHITIET_HD | GiaBan cũ không đổi; G04 lặp bằng luồng tạo đơn T14 |
| T10-K07 | Checkout baseline lưu tiêu hao rồi thay recipe | TIEUHAO cũ và tồn không bị tính lại; G04 lặp checkout T15 |
| T10-K08 | Hai connection pause test-only: checkout giữ Shared lock, writer chờ; chiều ngược lại writer đang thay/checkout chờ; fault rollback writer | Tiêu hao từ trọn recipe cũ hoặc mới, không mix; G04 lặp T15 |
| T10-K09 | 3role gọi read/write/recipe trực tiếp service+SQL | Ma trận đúng, STORE chỉ đọc, CASH không mutate |
| T10-K10 | Xóa SP chưa lịch sử/có history, tìm tên Việt/lọcDM/trạng thái/restart | Xóa chưa dùng nguyên tử; history giữ, filter/reload đúng |

Bổ sung stale2editor, receipt/reusedpayload/timeout/create/delete, migration clean/demo/preflight/rerun, runtime denied DML, logout/querymuộn và toàn bộ GUI chung. Các ca baseline/G04 ghi riêng từng subcase, không gộp PASS. UX-K39–42; T10-K05 chuyển giao cho UX-K07/T16.

## 4. Checklist gốc giữ nguyên

- [ ] `T10-K01` [I/M] Tạo sản phẩm giá dương, đúng danh mục; thêm nhiều định mức hợp lệ.
- [ ] `T10-K02` [I] Giá 0/âm, định mức 0/âm, FK sai hoặc cặp SP/NVL trùng bị chặn.
- [ ] `T10-K03` [I] Một dòng công thức lỗi khiến toàn bộ thay công thức rollback, công thức cũ còn nguyên.
- [ ] `T10-K04` [I/M] Không bật bán/checkout món không công thức; bánh thành phẩm định mức 1 hoạt động.
- [ ] `T10-K05` [M] Ngừng bán làm món không chọn mới; đơn mở đang chứa món cần sửa, không tự bỏ món.
- [ ] `T10-K06` [I] Đổi giá menu không đổi GiaBan của dòng hóa đơn cũ.
- [ ] `T10-K07` [I] Đổi công thức không đổi TIEUHAO_HOADON đã lưu.
- [ ] `T10-K08` [I] Lưu công thức đồng thời checkout bị khóa/đọc snapshot nhất quán, không công thức nửa cũ nửa mới.
- [ ] `T10-K09` [U/M] CASH không sửa menu, STORE chỉ đọc công thức; ADMIN quản trị được.
- [ ] `T10-K10` [I/M] Xóa sản phẩm có lịch sử bị chặn; tìm/lọc danh mục và mở lại hiển thị đúng.

Mỗi subcase lưu mã, môi trường/schema/source hash, dữ liệu, lệnh/thao tác, expected/actual, PASS/FAIL/BLOCKED/NOT RUN và evidence tại test-results.md cạnh plan. Bug ghi bug-report.md tại task; report nhóm liên kết và chờ duyệt đợt bug. Chưa đánh dấu hoàn tất từ plan/build/smoke.



