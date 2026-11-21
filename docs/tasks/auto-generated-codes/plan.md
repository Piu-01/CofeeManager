# Plan — Tự sinh mã cho chức năng thêm, bỏ ô nhập mã

- Ngày: 21/11/2026
- Trạng thái: **Đã duyệt plan, đã triển khai xong 21/11/2026; đã chạy đợt test
  21/11/2026 (chức năng SQL PASS; UI chức năng chờ người dùng kiểm) — xem
  [test-results.md](test-results.md)**
- Requirements: [requirements.md](requirements.md) — đã chốt: không tự sinh mã hạng
  thành viên; không thêm thông báo sau khi lưu.

## Nguyên tắc triển khai

- Tái dùng đúng luồng cấp mã đã có của hóa đơn: gọi `sp_CapMa` để lấy mã kế tiếp,
  rồi mới gọi procedure create hiện có với mã thật. **Không sửa procedure create**.
- Không đổi định dạng mã, không đổi/backfill mã cũ, không đổi cột Mã trong danh sách.
- UI không còn ô nhập mã; mã hiện có của bản ghi khi Sửa hiển thị ở tiêu đề hộp thoại.
- Không đổi mã hạng thành viên (ngoài phạm vi).

## Bước 1 — Migration bổ sung nguồn cấp mã

**File mới:** `database/migrations/040_auto_master_codes.sql`

1. Gate phiên bản: yêu cầu `@@TRANCOUNT>0` và đã có `SchemaMigrations` Version 38.
   **Điều chỉnh khi triển khai:** DB dev đang ở 38; migration 039 của task
   `promotion-dieu-kien-tu-ap` không chạy được trên DB này (batch DDL tham chiếu cột
   vừa thêm trong cùng batch — xem bug-report của task đó), nên 040 chỉ gate theo 38.
   040 không phụ thuộc object của 039; khi 039 được sửa và áp sau vẫn không ảnh hưởng
   `sp_CapMa`.
2. Tạo sequence cho 11 loại, khởi tạo `START WITH = MAX(số hiện có) + 1`:

   | Loại | Sequence | Mã | Max |
   |---|---|---|---|
   | Nhà cung cấp | `SeqNhaCungCap` | `NCC` + 2 số | 99 |
   | Phương thức TT | `SeqPhuongThuc` | `PT` + 3 số | 999 |
   | Đối tác giao hàng | `SeqDoiTac` | `DT` + 3 số | 999 |
   | Loại thu/chi | `SeqLoaiThuChi` | `TC` + 3 số | 999 |
   | Chức vụ | `SeqChucVu` | `CV` + 3 số | 999 |
   | Ca làm | `SeqCaLam` | `CA` + 3 số | 999 |
   | Sản phẩm | `SeqSanPham` | `SP` + 3 số | 999 |
   | Nguyên liệu | `SeqNguyenVatLieu` | `NVL` + 2 số | 99 |
   | Bàn | `SeqBan` | `B` + 4 số | 9999 |
   | Khu vực | `SeqKhuVuc` | `KV` + 3 số | 999 |
   | Khuyến mãi | `SeqKhuyenMai` | `KM` + 8 số | 99999999 |

   Bootstrap đọc `MAX` theo đúng pattern hiện có của từng bảng
   (`NCC[0-9][0-9]`, `SP[0-9][0-9][0-9]`, `KM[0-9]{8}`…), dùng `TRY_CONVERT`,
   bỏ qua mã legacy không khớp pattern (ví dụ `KM24TET`).
3. `ALTER PROCEDURE dbo.sp_CapMa` thêm các nhánh `NHACUNGCAP`, `PHUONGTHUCTT`,
   `DOITACGIAOHANG`, `DANHMUC_THUCHI`, `CHUCVU`, `CALAMVIEC`, `SANPHAM`,
   `NGUYENVATLIEU`, `BAN`, `KHUVUC`, `KHUYENMAI`; giữ nguyên các nhánh cũ.
   - `PHUONGTHUCTT`: bỏ qua số 10 (PT010 dành riêng).
   - `DANHMUC_THUCHI`: bỏ qua 5, 11, 12 (mã hệ thống).
   - Sequence cạn: giữ ánh xạ lỗi 11728 → 51302 như hiện tại.
4. Không thêm grant cho sequence (chỉ dùng trong `sp_CapMa` đã grant EXECUTE);
   không đổi bảng/cột/dữ liệu.

**Verify:** chạy `tools/database/Apply-Migration.ps1` với Version 40 trên DB dev;
`SchemaMigrations` có 40; `sys.sequences` có đủ 11 sequence; migration không lỗi
compile. Chưa tạo dữ liệu thử ở bước này (để đợt test).

## Bước 2 — DataAccess: hàm lấy mã kế tiếp

Thêm `AllocateCodeAsync(AccountIdentity, string sourceType, CancellationToken)` cho:

- `src/CafeManager.DataAccess/Repositories/CatalogRepository.cs`
- `src/CafeManager.DataAccess/Repositories/ProductRepository.cs`
- `src/CafeManager.DataAccess/Repositories/MaterialRepository.cs`
- `src/CafeManager.DataAccess/Repositories/TableRepository.cs`
- `src/CafeManager.DataAccess/Repositories/LoyaltyRepository.cs`

Mỗi hàm gọi `sp_CapMa` với `@Loai`, đọc cột `Ma`, cùng pattern hiện có trong
`SalesRepository.AllocateCodeAsync` (lỗi/không có dòng → `InvalidDataException`).

**Verify:** build DataAccess; không tạo generic repository mới.

## Bước 3 — Business: cấp mã trước khi ghi, cho phép mã rỗng khi tạo

**`CatalogService.cs`**

- `Normalize(request, creating)`: khi `creating` và `kind != ProductCategory`, nếu
  `Code` trống thì chấp nhận (`Code = ""`); nếu có mã vẫn kiểm tra regex như cũ.
- Thêm `AllocateCodeAsync(session, kind)` (permission `CatalogAdmin`): map kind →
  `NHACUNGCAP`/`PHUONGTHUCTT`/`DOITACGIAOHANG`/`DANHMUC_THUCHI`/`CHUCVU`/`CALAMVIEC`;
  không dùng cho `ProductCategory` (SQL tự cấp DM như hiện tại).
- `SaveAsync`: nếu tạo mới, kind khác `ProductCategory` và mã vẫn trống (fallback khi
  caller không cấp trước) thì cấp mã trong `Run` trước khi gọi repository.

**`ProductService.cs`**

- `Normalize(r, bool creating = false)`: bỏ yêu cầu regex mã khi `creating` và mã trống;
  Sửa/Xóa vẫn bắt mã đúng.
- `SaveAsync`: sau khi demand quyền `CatalogAdmin`, nếu `revision is null` thì cấp mã
  `SANPHAM` rồi mới `WriteAsync`; `RecipeSave/Delete` giữ nguyên.

**`MaterialService.cs`**

- `Normalize(request, bool creating = false)`: tương tự cho `NVL`.
- `SaveAsync`: nếu `revision is null` cấp `NGUYENVATLIEU` trong `Run` (quyền
  `MaterialManage`) trước khi ghi; `OpenAsync` giữ nguyên (đã có mã bản ghi).

**`TableService.cs`**

- `Normalize(r, area, bool creating = false)`: tương tự cho `B`/`KV`.
- `SaveAsync`: nếu `revision is null` cấp `BAN` hoặc `KHUVUC` (quyền `CatalogAdmin`)
  trước khi ghi; Delete giữ nguyên.

**`LoyaltyService.cs`**

- `Normalize(r, creating)`: khi tạo và mã trống thì chấp nhận; khi có mã vẫn kiểm tra
  `KM` + 8 chữ số.
- `SavePromotionAsync`: nếu `revision is null` cấp `KHUYENMAI` (quyền `CatalogAdmin`)
  trước khi ghi; không đổi `NormalizeTiers`.

**Verify:** `dotnet build`; rà không còn `Normalize` buộc mã khi tạo ở các luồng trên;
mã rỗng không thể tới `WriteAsync` (phải được thay bằng mã vừa cấp).

## Bước 4 — UI: bỏ ô mã, chỉnh bố cục

**`Forms/CatalogEditorDialog.Designer.cs` + `.cs`**

- Xóa `codeLabel`, `codeInput` khỏi khai báo/khởi tạo/cấu hình; giảm `RowCount` từ 10
  xuống 9; dồn các hàng phone/ratio/allowance/factor/type/start/end/supported lên;
  đánh lại `TabIndex` (Tên = 0).
- `Request(string code)`: dùng `original?.Code ?? code`; `ProductCategory` vẫn để trống
  để SQL cấp DM.
- `Snapshot()` bỏ mã; `Validation` map `"Code"` về `nameInput` (hoặc bỏ nhánh);
  `Populate` không còn gán `codeInput`.
- `Save_Click`: validate request mã rỗng trước; nếu tạo mới và kind khác
  `ProductCategory` thì `await service.AllocateCodeAsync(...)` rồi gán mã vào request
  trước `SaveAsync` (để trạng thái unknown/reload vẫn đối chiếu được theo mã như cũ).
- Tiêu đề Thêm/Sửa giữ như hiện tại (`Thêm danh mục` / `Sửa danh mục · <mã>`).

**`Modules/Menu/ProductsControl.cs`**

- Bỏ `var code=dialog.AddText(...)`; khi Sửa đổi tiêu đề thành
  `Sửa sản phẩm · <mã>`; request dùng `row?.Code ?? ""`; gọi
  `ProductService.Normalize(request, create)`.

**`Modules/Inventory/MaterialsControl.cs`**

- Bỏ ô mã; tiêu đề Sửa `Sửa nguyên liệu · <mã>`; request `row?.Code ?? ""`;
  `MaterialService.Normalize(request, creating)`.

**`Modules/Tables/TablesControl.cs`**

- Bàn: bỏ ô mã; tiêu đề Sửa `Sửa bàn · <mã>`; request `row?.Code ?? ""`;
  `TableService.Normalize(..., area:false, creating)`.
- Khu vực (`AreaList.Edit`): tương tự, tiêu đề `Sửa khu vực · <mã>`,
  `Normalize(..., area:true, creating)`.

**`Modules/Loyalty/LoyaltyControl.cs` (PromotionList)**

- Bỏ ô mã; tiêu đề Sửa `Sửa khuyến mãi · <mã>`; request `row?.Code ?? ""`.
- Không đổi `PolicyList` (mã hạng vẫn nhập tay).

**Verify:** build; grep `Mã NCC|Mã SP|Mã NVL|Mã B|Mã KV|Mã KM|Mã PT|Mã DT|Mã TC|Mã CV|Mã CA`
không còn trong các dialog thêm; không còn điều khiển `codeInput`; trường đầu tiên là
Tên; `FitToContent` không để khoảng trống. Không tự chạy test hình thức.

## Bước 5 — Tài liệu

- Cập nhật `docs/UX_QuanLiQuanCafe.md` các mục mô tả form Thêm/Sửa của Danh mục,
  Sản phẩm, Nguyên liệu, Bàn/Khu vực, Khuyến mãi: bỏ “nhập mã”, ghi “mã tự sinh khi
  lưu”.
- Ghi chú trong `docs/tasks/auto-generated-codes/` khi triển khai xong (chưa phải test).

**Verify:** các mục không còn hướng dẫn người dùng nhập mã; khớp source.

## Bước 6 — Build và chuẩn bị đợt test

- `dotnet build` toàn solution (Debug); sửa mọi lỗi biên dịch do đổi chữ ký
  `Normalize`/`Request`.
- Áp migration 040 lên DB dev/test đã chọn; xác nhận version/sequence.
- Không chạy test chức năng cho tới khi người dùng **duyệt đợt test riêng**.

**Ca đề xuất cho đợt test (chưa chạy):**

1. Tạo lần lượt 6 danh mục (NCC/PT/DT/TC/CV/CA) khi ô mã không còn: nhận mã đúng
   pattern, tăng dần; sửa/xóa dùng đúng mã.
2. Tạo sản phẩm, nguyên liệu, bàn, khu vực, khuyến mãi: mã tự sinh đúng; danh sách và
   lookup theo mã hiển thị đúng.
3. Tạo hai bản ghi đồng thời ở hai session: không trùng mã.
4. Dữ liệu seed/mã cũ (KM24TET, PT010, TC005/011/012, DM cũ) vẫn sửa/xóa theo đúng
   quy tắc cũ; mã dành riêng không bị cấp tự động.
5. Cấp mã rồi lưu thất bại (validation/khóa) không tạo bản ghi rác; lần sau vẫn cấp mã
   mới hợp lệ.
6. Hết dải mã (nếu dựng được fixture NCC/NVL đủ 99): báo lỗi rõ, không sinh mã sai.
7. Khôi phục sau timeout/unknown: dialog danh mục đối chiếu được theo mã đã cấp; các
   màn khác đối chiếu theo request ID receipt như cũ.
8. Build Debug/Release phần liên quan; test UI chức năng (focus/Tab/lưu) — phần hình
   thức do người dùng tự kiểm theo quy định.

## Rủi ro và cách giảm

- **Cấp mã nhưng lưu thất bại** → khoảng trống trong dải mã; chấp nhận, không cấp lại.
- **Sequence khởi tạo sai do mã cũ lạ** → chỉ parse đúng pattern, dùng `TRY_CONVERT`,
  bỏ qua legacy; kiểm tra `MAX+1` trên DB test trước khi nghiệm thu.
- **Quên một luồng** → đối chiếu bảng 2.2 trong requirements và ca test số 1–2.
- **Đổi chữ ký `Normalize` gây vỡ call site** → build toàn solution và grep call site.
- **Không sửa procedure create** nên rủi ro SQL thấp; nếu mã cấp trùng (lỗi sequence)
  procedure hiện có sẽ chặn bằng khóa/UNIQUE, UI hiện lỗi bình thường.

## Nhật ký triển khai 21/11/2026

- Migration `040_auto_master_codes.sql` đã áp lên `CoffeeManager_Development`
  (`SchemaMigrations` = 40; đủ 11 sequence `SeqNhaCungCap`…`SeqKhuyenMai`, START =
  MAX hiện có + 1; `sp_CapMa` có các nhánh mới, giữ nhánh cũ).
- DAL: `CatalogRepository.AllocateAsync` dùng chung; `AllocateCodeAsync` ở Catalog,
  Product, Material, Table, Loyalty.
- BLL: cho phép mã rỗng khi tạo; cấp mã trong phạm vi quyền trước khi ghi.
  `CatalogEditorDialog` cấp mã ở UI trước `SaveAsync` để unknown/reload đối chiếu theo mã.
- UI: bỏ ô Mã ở `CatalogEditorDialog` (RowCount 9, Tab lại), Products, Materials,
  Bàn/Khu vực, Khuyến mãi; tiêu đề Sửa kèm mã. Mã hạng thành viên giữ nguyên.
- Build: WinForms + Tests biên dịch 0 lỗi (app đang mở khóa bin nên build kiểm tra
  bằng output tạm `artifacts/build-check`, đã xóa). Chưa chạy test chức năng.
