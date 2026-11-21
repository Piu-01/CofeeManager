# Requirements — Tự sinh mã cho các chức năng thêm, bỏ ô nhập mã khỏi giao diện

- Ngày: 21/11/2026
- Trạng thái: **Đã triển khai xong 21/11/2026; đã chạy đợt test ngày 21/11/2026 —
  25/25 ca SQL/gửi lặp PASS, đồng thời PASS, build + 144 unit test PASS; UI chức năng
  chờ người dùng kiểm** (xem [test-results.md](test-results.md))
- Liên quan chính: `database/migrations/`, `src/CafeManager.Business/Services/`,
  `src/CafeManager.WinForms/Forms/CatalogEditorDialog.*`,
  `src/CafeManager.WinForms/Modules/{Catalogs,Menu,Inventory,Tables,Loyalty}/`.

## 1. Mục tiêu

Hiện nhiều chức năng “Thêm” vẫn bắt người dùng tự gõ mã (NCC01, SP003, B0005, NVL02…).
Người dùng muốn:

1. Mã của bản ghi mới được **hệ thống tự sinh** khi lưu; người dùng không phải nhập.
2. **Bỏ ô nhập mã** khỏi hộp thoại thêm/sửa và chỉnh lại bố cục cho khớp (không để
   khoảng trống, trường tiếp theo lên đầu, thứ tự Tab/focus hợp lý).
3. Giữ nguyên mã của bản ghi đã có, định dạng mã hiện tại và mọi tham chiếu/danh sách
   đang dùng mã.

## 2. Hiện trạng (khảo sát trong source)

### 2.1. Đã tự sinh mã (không cần sửa phía người dùng)

Các luồng sau đã tự lấy mã khi tạo, giao diện không có ô nhập mã:

- Khách hàng (`sp_CustomerWrite`), Nhân viên (`sp_CapMa 'NHANVIEN'`),
  Danh mục sản phẩm (`sp_CapMa 'DANHMUC_SP'` — ô mã chỉ đang hiện read-only/rỗng).
- Tài sản, phiếu nhập, phiếu xuất, phiếu kiểm kê, phiếu thu/chi, hóa đơn
  (`sp_CapMa`/sequence tương ứng; dialog không có ô mã).

### 2.2. Còn nhập mã thủ công (phạm vi thay đổi)

| Màn / chức năng | Mã | Định dạng hiện tại | Nơi nhập |
|---|---|---|---|
| Danh mục → Nhà cung cấp | `NCC` | `NCC01`–`NCC99` | `CatalogEditorDialog` |
| Danh mục → Phương thức thanh toán | `PT` | `PT001`–`PT999`, dành `PT010` | `CatalogEditorDialog` |
| Danh mục → Đối tác giao hàng | `DT` | `DT001`–`DT999` | `CatalogEditorDialog` |
| Danh mục → Loại thu / chi | `TC` | `TC001`–`TC999`, hệ thống `TC005/TC011/TC012` | `CatalogEditorDialog` |
| Danh mục → Chức vụ | `CV` | `CV001`–`CV999` | `CatalogEditorDialog` |
| Danh mục → Ca làm việc | `CA` | `CA001`–`CA999` | `CatalogEditorDialog` |
| Menu → Sản phẩm | `SP` | `SP001`–`SP999` | `ProductsControl.Edit` |
| Kho → Nguyên liệu | `NVL` | `NVL01`–`NVL99` | `MaterialsControl.Edit` |
| Bàn | `B` | `B0001`–`B9999` | `TablesControl.Edit` |
| Khu vực | `KV` | `KV001`–`KV999` | `TablesControl.AreaList.Edit` |
| Khuyến mãi | `KM` | mới: `KM` + 8 chữ số; mã cũ giữ nguyên | `LoyaltyControl.PromotionList.Edit` |

Ghi chú:

- Mã hiện được BLL kiểm tra regex (`CatalogService.Normalize`, `ProductService.Normalize`,
  `MaterialService.Normalize`, `TableService.Normalize`, `LoyaltyService.Normalize`) và
  SQL kiểm tra lại trước khi insert.
- `CatalogEditorDialog` là một dialog dùng chung cho 7 loại danh mục; ô “Mã” nằm ở
  hàng đầu của `TableLayoutPanel` (`CatalogEditorDialog.Designer.cs`).
- `Sp_CapMa` hiện chỉ hỗ trợ `HOADON`, `DANHMUC_SP`, `NHANVIEN`, `PHIEUTHUCHI`,
  `PHIEUNHAP`, `CONGNO_NCC`, `PHIEUXUAT`, `PHIEUKIEMKE`; các mã ở bảng trên chưa có
  nguồn sinh trong DB.

## 3. Phạm vi

### Làm

1. **SQL (migration mới, dự kiến `040_auto_master_codes.sql`)**
   - Bổ sung nguồn cấp mã cho 11 loại ở mục 2.2 vào `sp_CapMa` (sequence riêng,
     khởi tạo từ mã lớn nhất đang có để không trùng dữ liệu cũ).
   - Tái dùng đúng cơ chế cấp mã đã có của hóa đơn (`sp_CapMa` gọi trước khi tạo bản
     ghi, như `SalesRepository.AllocateCodeAsync`); không đổi procedure tạo bản ghi.
   - Bỏ qua mã dành riêng (`PT010`, `TC005/TC011/TC012`) khi cấp tự động; hết dải mã
     thì báo lỗi rõ ràng (dùng mã lỗi hiện có 51302), không tự tràn định dạng.
   - Không đổi mã/không backfill bản ghi cũ; các procedure create giữ nguyên kiểm tra
     mã hợp lệ như hiện tại.

2. **BLL/DAL**
   - Thêm hàm cấp mã cho từng domain (Catalog, Product, Material, Table, Loyalty) gọi
     `sp_CapMa`, kiểm tra quyền ghi tương ứng trước khi cấp.
   - Cho phép mã rỗng khi tạo (`creating = true`) ở các `Normalize` liên quan; BLL/UI
     cấp mã trước khi ghi và gửi mã thật xuống procedure create hiện có.
   - Vẫn kiểm tra mã khi Sửa/Xóa; mã đã cấp là mã thật, không đổi sau khi tạo.
   - Hiển thị lỗi hết dải mã/ lỗi SQL bằng cơ chế lỗi hiện có, không tự đoán mã.

3. **UI / bố cục**
   - `CatalogEditorDialog`: xóa hẳn hàng “Mã” (label + input) khỏi Designer và code-behind;
     dồn các hàng còn lại lên, chỉnh `RowCount`, `TabIndex`, `Snapshot`, map lỗi
     `ErrorProvider`, `Request()` dùng `original?.Code ?? ""`. Tiêu đề khi Sửa vẫn hiển
     thị mã (`Sửa danh mục · <mã>`); khi Thêm không có ô mã.
   - `ProductsControl`, `MaterialsControl`, `TablesControl` (Bàn + Khu vực),
     `PromotionList`: bỏ `AddText` mã; trường Tên thành trường đầu tiên; tiêu đề khi
     Sửa hiển thị kèm mã hiện có (ví dụ `Sửa sản phẩm · SP003`) để người dùng vẫn biết
     đang sửa bản ghi nào.
   - Không để khoảng trống/ô disabled thay cho ô mã; dialog tự co giãn theo nội dung
     (`OrbitDialogLayout.FitToContent` hiện có).
   - Có thể thêm một dòng ghi chú nhỏ “Mã được tạo tự động khi lưu” ở chế độ Thêm, không
     phải ô nhập.
   - Giữ nguyên cột Mã, tìm kiếm theo mã và các nơi hiển thị mã trong danh sách/chi tiết.

4. **Tài liệu**
   - Cập nhật mô tả luồng/UI liên quan trong `docs/UX_QuanLiQuanCafe.md` và task/plan
     tương ứng sau khi được duyệt triển khai.

### Không làm

- Không đổi định dạng/tiền tố mã hiện tại, không đổi độ dài cột, không backfill/đổi mã
  bản ghi cũ.
- Không đổi quyền, không thêm màn hình mới, không đổi nghiệp vụ bán hàng/kho/điểm.
- Không đổi các ô tham chiếu không phải mã mới: “Mã tài sản (tùy chọn)” trong phiếu
  thu/chi, “Request ID”, bộ lọc “Mã NV”, cột “Mã” trong danh sách.
- Không tự chạy đợt test khi chưa được duyệt riêng.

## 4. Giả định và quyết định thiết kế

1. **Cấp mã qua `sp_CapMa` trước khi ghi** — tái dùng đúng luồng hóa đơn đang chạy
   (`SalesRepository.AllocateCodeAsync`), sequence bảo đảm hai người thêm cùng lúc
   không trùng mã; mã đã cấp vẫn được kiểm tra lại ở procedure create hiện có.
2. **Sequence khởi tạo từ `MAX` mã hiện có + 1** cho từng loại. Mã đã xóa không được
   cấp lại (tránh nhầm với receipt/lịch sử); khoảng trống trong dải mã là bình thường
   khi cấp mã rồi lưu thất bại.
3. **Mã vẫn bất biến sau khi tạo**; sửa chỉ đổi các trường nghiệp vụ, không đổi mã.
4. **Vẫn nhận mã tường minh khi gọi procedure** (tương thích seed/test hiện có), nhưng
   UI không còn đường nhập mã. Nếu cần chặn hoàn toàn về sau là thay đổi riêng.
5. **Mã khuyến mãi mới** theo đúng quy tắc hiện hành `KM` + 8 chữ số; các mã cũ dạng
   `KM24TET…` giữ nguyên và vẫn sửa/xóa được.
6. **Hết dải mã** (ví dụ đã đủ 99 nhà cung cấp/nguyên liệu): báo lỗi rõ, không sinh mã
   sai định dạng. Việc mở rộng độ dài/định dạng là thay đổi riêng, không tự làm.
7. **Hộp thoại Sửa** hiển thị mã trong tiêu đề (Catalog đã có; các dialog khác bổ sung)
   thay vì giữ ô nhập read-only, để bố cục gọn theo yêu cầu.
8. Sau khi lưu thành công, danh sách tải lại như hiện tại; mã mới hiển thị ở cột Mã.

## 5. Quyết định đã chốt (21/11/2026)

1. **Mã hạng thành viên không tự sinh** — giữ nhập tay như hiện tại, nằm ngoài phạm vi
   đợt này (vì là mã nghiệp vụ có nghĩa: DONG/BAC/VANG/KIM).
2. **Không thêm thông báo mã vừa sinh** — sau khi lưu chỉ tải lại danh sách như hiện tại;
   mã mới xem ở cột Mã.

## 6. Tiêu chí hoàn thành

- Không còn ô nhập mã trong các hộp thoại Thêm thuộc phạm vi mục 2.2; bố cục không có
  khoảng trống, trường đầu tiên và thứ tự Tab hợp lý.
- Tạo mới mỗi loại trên đều nhận mã tự sinh đúng định dạng, tăng dần, không trùng mã
  cũ; thử hai lần tạo liên tiếp/đồng thời vẫn nhận hai mã khác nhau.
- Sửa/Xóa bản ghi cũ vẫn dùng đúng mã hiện có; mã không bị đổi.
- Danh sách, tìm kiếm theo mã, lookup và tham chiếu (hóa đơn, phiếu, khuyến mãi, công
  thức…) vẫn hoạt động.
- BLL không còn bắt nhập mã khi tạo; ứng dụng cấp mã qua `sp_CapMa` và bản ghi được
  lưu với đúng mã vừa cấp.
- Hết dải mã hiển thị lỗi rõ ràng, không tạo dữ liệu sai.
- Build WinForms/Debug thành công; migration chạy được trên DB test riêng. Các ca kiểm
  thử chức năng chỉ chạy sau khi người dùng duyệt đợt test riêng.
- Không có thay đổi ngoài phạm vi đã nêu.
