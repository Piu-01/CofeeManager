# Plan đồng bộ cửa sổ con Orbit

22/10/2026. Requirements và plan toàn đợt đã được người dùng duyệt. Đã triển khai bước1–6, build Debug/Release; chờ duyệt đợt test bước7. Một người triển khai tuần tự; chưa chạy kiểm thử nghiệm thu.

## Hợp đồng và mốc nguồn

Giữ .NET10 WinForms, model/binding/session/service hiện có. Không migration, không đổi SQL/DAL/BLL/Models hoặc nghiệp vụ. Tham chiếu requirements.md, kế hoạch T07/T08 và T09–T13, UX dialog/nháp/lỗi, docs/ui/ và gói Orbit_9_HTML đã bàn giao. Không khôi phục nhãn đếm hoặc ghi chú tĩnh đã bỏ.

Trước triển khai ghi baseline hash các lớp backend và các file UI liên quan; lưu trạng thái Git sau khi kiểm tra đúng root CoffeeManager. Không reset/stage/commit thay đổi có sẵn. Mỗi bước ghi file thực sửa và kiểm kỹ thuật riêng, không tự đánh dấu test nghiệm thu PASS.

## 1. Thống nhất phần trình bày dùng chung

**File:** Theme/OrbitManagementTheme.cs; UI/OrbitField.cs, OrbitFieldLayout.cs; thêm UI/OrbitDialogLayout.cs khi cần khung header/body/status/footer mà OrbitContentHost hiện chưa cung cấp.

- Lấy palette, font, DPI, radius và button style từ OrbitManagementTheme; bổ sung token/helper dialog vào theme hiện có, không tạo theme cạnh tranh.
- Tái sử dụng OrbitField cho input native. Bổ sung cách áp dụng palette Orbit và chiều cao/padding theo DPI mà không đổi hình thức Login/Bootstrap ngoài phạm vi. Giữ input instance, AccessibleName, TabIndex, ReadOnly, MaxLength, data source và sự kiện.
- Khung dialog chung: heading theo Text hiện có; body với padding thống nhất; status tự ẩn khi rỗng, wrap theo chiều rộng; footer cố định. Chiều rộng theo loại form nhập/bảng; chiều cao theo nội dung, giới hạn WorkingArea, min-size và DPI. Form dài chỉ body cuộn; bảng tự cuộn.
- Dùng OrbitButton cho Primary/Secondary/Danger và trạng thái hover/press sẵn có; cùng chiều cao theo vai trò, width theo chữ. Không thêm stardust hoặc font/màu riêng từng form.
- Không sửa OrbitContentHost toàn cục nếu Login còn dùng; không áp dụng AppTheme.Apply đệ quy sau style mới khiến input/nút bị ghi đè.

**Kiểm kỹ thuật dự kiến:** build; control dài/ngắn/read-only/nhiều dòng tại96/120/144DPI; preferred size không vượt WorkingArea; footer/status không chồng body; Tab/focus không mất do wrap.

## 2. Áp dụng vào editor chung trước

**File:** Forms/OperationEditorDialog.cs và .Designer.cs; chỉ thay phần tạo/trình bày control trong AddText/AddField/AddNote, InitializeComponent, SetReadOnly khi cần.

- Thay hàng78px và note width520 cố định bằng đo/control dùng chung, giữ thứ tự trường và đối tượng input mà module đang giữ tham chiếu.
- Giữ Save_Click, CaptureWrite, requestId, pendingWrite, Reconcile_Click, ErrorProvider, unknown/writing/dirty/saved và FormClosing về hành vi. Footer Lưu/Đối chiếu/Hủy hoặc Đóng; trạng thái enabled/visible giữ đúng cũ.
- Tách cách thêm bảng/kết quả preview để phần bảng có thể Fill và cuộn trong vùng của nó; không còn dựa chiều cao Parent ngẫu nhiên. Khi wrapper làm thay đổi Parent, chỉnh đúng các dòng sizing của call site ở bước4, không đổi event đọc/ghi.
- Xóa phần style/layout cũ đã thay; không bỏ thông tin nhận diện bản ghi, lỗi hoặc kết quả thật.

**Kiểm kỹ thuật dự kiến:** editor ngắn2trường, dài5–6trường, read-only và thông báo lỗi dài; kiểm rằng handler vẫn nối một lần và không đổi chức năng Đối chiếu. Không tự chạy giao dịch ghi trong pha này.

## 3. Editor Danh mục nền

**File:** Forms/CatalogEditorDialog.cs và .Designer.cs; tái dùng OrbitFieldLayout nếu còn phù hợp.

- Chuyển heading, trường, nhóm checkbox, status và footer sang khung/token chung. Giữ mnemonic, thứ tự tab, label gắn đúng input và ẩn/hiện theo CatalogKind.
- Dòng trường ẩn không chiếm khoảng trống; trường tự sinh mã/read-only vẫn đúng. Giữ lịch giờ OrbitTimeInput và giá trị typed.
- Loại bỏ bọc OrbitContentHost/layout/style cũ tại riêng editor nếu khung mới thay hoàn toàn; chỉ bỏ using/field/handler layout thực dư.
- Không sửa Snapshot/Populate/ShowField/Save/Reload/unknown/nháp hoặc kiểm quyền ngoài thay đổi trình bày cần thiết.

**Kiểm kỹ thuật dự kiến:** bảy CatalogKind, cả create/edit; kiểm MaxLength, ReadOnly, supportedInput PT010, ẩn/hiện field không bị mất sau wrap, footer luôn đủ chỗ.

## 4. Bảng và nội dung phụ trong module

**File:** Modules/Menu/ProductsControl.cs, Modules/Customers/CustomersControl.cs, Modules/Loyalty/LoyaltyControl.cs, Modules/Tables/TablesControl.cs, Modules/Inventory/MaterialsControl.cs; UI/RecordListControl.cs, OrbitManagementLayout.cs chỉ khi bảng Hóa đơn còn cần; UI/OrbitDataGridView.cs hoặc helper visual chung nếu cần tái sử dụng phần vẽ.

- Form Thêm/Sửa/Mở sổ/Xóa các module nhận bố cục từ editor chung. Chỉ chỉnh call site về kích thước hoặc cách thêm nội dung; không sửa payload/handler.
- Công thức: giữ ComboBox column nguyên liệu, nhập định mức, tự điền đơn vị, validation trùng/dương/precision, xóa dòng nháp và readonly theo role. Áp dụng style grid chung có chế độ nhập, không dùng constructor readonly của grid danh sách một cách mù quáng.
- Chính sách hạng: giữ các cột/rows typed và preview bắt buộc, không icon cạnh tên hạng; footer ngoài grid.
- Preview ưu đãi/điều chỉnh điểm: kết quả dài wrap đúng, không mất nội dung hoặc nút preview khi lỗi; không đổi cách invalidation preview.
- Hóa đơn khách: bỏ heading/toolbars trùng trong dialog nếu do lồng khung cũ, giữ search/paging/query/columns thật. Ưu tiên dùng khung/grid chung hiện có. Chỉ loại bỏ RecordListControl/OrbitManagementLayout nếu rg xác nhận hoàn toàn không còn dùng, không làm refactor rộng để buộc xóa.
- Date/Time/Filter control hiện có được reuse. Sửa presentation popup chỉ nếu cần và có chứng cứ, không đổi parse/format/value.

**Kiểm kỹ thuật dự kiến:** cấu trúc cột/binding/events trước-sau, mode nhập/readonly ADMIN/STORE, không đổi batch50; grid tự cuộn, rowheight ổn định; nút xóa dòng có một handler.

## 5. Hộp chọn khách và xác nhận chung

**File:** Forms/CustomerPickerDialog.cs, Modules/Customers/CustomersControl.cs; thêm Forms/OrbitConfirmationDialog.cs (và Designer nếu cần); thay các call site MessageBox thuộc G03 trong OperationEditorDialog, CatalogEditorDialog và CatalogsControl.

- Picker dùng khung modal chung, không thêm title/toolbar trùng; giữ tìm/tải lại/phân trang/chọn/đóng. Points không có vãng lai/quản lý; Preview ưu đãi còn vãng lai. Giữ GetAsync đối chiếu trước trả kết quả, DialogResult và CancelButton.
- Xác nhận Orbit native giữ nguyên message, caption, tập lựa chọn Yes/No/Cancel và nhánh xử lý đang có, kể cả bỏ nháp/unknown. Nút từ chối/hủy là lựa chọn an toàn mặc định; Enter không tự kích hoạt ghi tiền/kho/điểm. Không tự đặt AcceptButton cho hành động ghi.
- Không thay MessageBox ở Login/Bootstrap/Setup hoặc module khác ngoài phạm vi; không gắn hiệu ứng thanh toán.

**Kiểm kỹ thuật dự kiến:** mapping DialogResult, Escape/X/cancel, focus, owner/modal, nhấn nhanh không chạy hai handler; picker không có quản lý ở cả hai context.

## 6. Dọn code và build bàn giao triển khai

**File:** chỉ các file UI đã chạm, project/resources nếu thực sự không còn reference; hồ sơ tại docs/tasks/orbit-dialog-refresh/.

- Rg toàn src, csproj, resource và công cụ trước xóa helper/style. Lập bảng file giữ/sửa/xóa và lý do. Không xóa các probe/hồ sơ cũ làm mất lịch sử test.
- So hash backend baseline; kiểm diff cho thấy thay đổi chỉ UI. Kiểm source không còn gọi ghi chú tĩnh/nhãn đếm đã bỏ.
- Build Debug và Release, ghi log cảnh báo/lỗi thực tế. Gửi danh sách triển khai và đề nghị duyệt **một đợt test**. Không chốt nghiệm thu từ build.

## 7. Đợt test sau duyệt — source cố định

**Hồ sơ:** test-plan.md, test-results.md, report.md; bug mới trong bug-report.md của đợt, liên kết taskG03 tương ứng khi liên quan. Không sửa source giữa lượt; gom bug để duyệt một đợt trước sửa/regression.

| Nhóm ca | Dữ liệu/thao tác | Expected |
|---|---|---|
| D01 Editor ngắn | Khách hàng, khu vực, nguyên liệu thêm/sửa | Nhãn/input/nút đồng bộ, không khoảng trắng thừa, tab và focus đúng |
| D02 Editor dài | Sản phẩm, khuyến mãi, ca làm/chức vụ | Cuộn phần nhập; status/footer luôn thấy; native lịch không bị che |
| D03 Bảng nhập | Công thức, chính sách hạng | Giữ edit/readonly, unit binding, xóa nháp, scroll và preview |
| D04 Bảng đọc/picker | Hóa đơn khách, picker Points/Preview | Cột/paging đúng; không nút quản lý trong picker; đóng không đổi lựa chọn |
| D05 Preview/lỗi | Tiền lớn, tên/lý do dài, lỗi trường/DB | Wrap không cắt/chồng; lỗi giữ input, focus lỗi, không success giả |
| D06 Xác nhận/nháp | Hủy xác nhận, X/Escape, nháp thay đổi, đang ghi/unknown | Giữ contract, không ghi khi từ chối, không gửi lặp/đóng sai trạng thái |
| D07 Resize/DPI | Client1280×720 và1366×768;100/125/150%; khởi tạo và đổiDPI | Header/body/footer đúng vị trí, nút không cắt, font rõ, không autoscale kép |
| D08 Regression màn chính | Chuyển màn, quyền ADMIN/CASH/STORE | Không ảnh hưởng layout trang chính, filter/selection, không thêm quyền |
| D09 Hành vi ghi liên quan | Fixture được duyệt trên DBtest có marker | Một thao tác ghi hợp lệ và lỗi/giữ nháp theo nhóm bị ảnh hưởng; đối chiếu dữ liệu/receipt, không ghiDBthật |

Đối chiếu ca gốc phù hợp T08(danh mục), T09(nguyên liệu), T10(menu/công thức), T11(bàn/khu vực), T12(khách), T13(ưu đãi/điểm), không thay expected/checklist gốc. Chốt dữ liệu/lệnh và ID subcase trong test-plan trước khi xin duyệt test, tránh suy diễn giao dịch đã PASS từ layout fixture.

Đăng nhập admin thủ công, gom kiểm nhiều dialog trong một phiên trước đổiDPI; không mở lại app chỉ vì chỉnh tài liệu. Các role còn lại kiểm fixture/quyền và thao tác người dùng khi cần; không tự đăng nhập hoặc ghi mật khẩu. Giữ WindowsScale theo lựa chọn người dùng, ghi đúng trạng thái cuối.

Ảnh thật tối thiểu: editor ngắn, editor dài, grid công thức, chính sách, preview kết quả, danh mục nhiều trường, picker, xác nhận; thêm lỗi và các DPI. Ghi rõ ảnh fixture/DrawToBitmap khác ảnh app thật, BLOCKED nếu công cụ không thao tác được.

## Gate hiện tại

Plan đã duyệt và triển khai bước1→6. Người dùng đã duyệt test bước7 và miễn kiểm hình thức; đã duyệt sửa cả đợtB01/B02. Hai bug đã sửa/PASS regression44ca chức năng; hồ sơ trước/sau tại test-results.md và regression-results.md. Giữ các phần chưa kiểm đã liệt kê, không đổi nghiệm thu G03/T01–T32. Không commit/push.
