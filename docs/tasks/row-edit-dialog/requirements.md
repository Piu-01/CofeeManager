# Requirements — Sửa master data bằng hộp thoại, giữ xóa tại dòng

Trạng thái: **requirements và plan đã duyệt**; đã triển khai code, build Debug/Release PASS;
đợt test chức năng 23/11/2026 đã chạy 23/24 ca (xem [test-results.md](test-results.md)):
21 PASS, 2 PARTIAL, 1 SKIPPED hình thức/DPI; không còn ca NOT RUN. Hai phát hiện
có sẵn F-01 (tên bàn >20 ký tự trả lỗi Unknown) và F-02 (màn Ưu đãi/Điểm ném
IndexOutOfRange) đã được duyệt sửa, kiểm chứng lại PASS ngày 23/11/2026.

## 1. Mục tiêu

Đổi cách sửa dữ liệu trên 8 màn master data từ **nhập trực tiếp tại ô** sang
**double-click dòng để mở hộp thoại sửa** như trước, vì thao tác inline hiện tại
chưa hợp với cách dùng thực tế. Giữ nguyên xóa bằng thùng rác từng dòng và giữ
hiệu ứng lia chuột báo ô có thể sửa.

Hướng chốt: **không viết lại cơ chế sửa từ đầu**. Tái dùng hạ tầng hiện có —
`OrbitRowEditor` tiếp tục giữ vòng đời thao tác dòng, các hộp sửa hiện có giữ phần
nhập, và `OrbitCellEditor` được dùng lại làm ô nhập một dòng trong hộp sửa thay vì
bị bỏ đi. Chỉ bỏ cách gắn editor overlay trực tiếp lên lưới.

## 2. Phạm vi

### 2.1 Trong phạm vi

Áp dụng cho 8 màn đang dùng `OrbitRowEditor`:

| Màn | Hộp sửa sẽ dùng |
|---|---|
| Menu sản phẩm | `OperationEditorDialog` sửa sản phẩm hiện có (Tên, Danh mục, Giá, Trạng thái). |
| Danh mục nền | `CatalogEditorDialog` hiện có, đúng loại tab đang chọn. |
| Bàn / Khu vực | Hộp sửa bàn và hộp sửa khu vực hiện có. |
| Nguyên liệu | Bổ sung hộp sửa Tên + Đơn vị; không sửa tồn đầu kỳ/tồn hiện tại. |
| Khách hàng | `OperationEditorDialog` sửa khách hiện có (Tên, SDT). |
| Nhân viên | `HrEditorDialog` sửa nhân viên hiện có (Tên, SDT, Hợp đồng, lương, chức vụ). |
| Tài sản | `AssetEditorDialog` với chế độ sửa hồ sơ hiện có; giữ luồng file chờ/đối chiếu của hộp này. |
| Hạng thành viên | Hộp sửa hạng hiện có, giữ bước xem số khách bị ảnh hưởng trước khi lưu. |

Thay đổi tương tác chung:

- **Click một lần** vào dòng chỉ chọn dòng như lưới bình thường; không mở editor,
  không mở hộp sửa.
- **Double-click** vào dòng dữ liệu (trừ ô thùng rác) mở hộp sửa của dòng đó.
  Không phụ thuộc phải click chọn trước.
- **F2** tại dòng đang chọn mở cùng hộp sửa (giữ đường bàn phím cũ).
- Sau khi lưu thành công: đóng hộp, tải lại danh sách và chọn lại đúng dòng theo mã
  như hành vi sửa hiện có của từng màn. Hủy hộp không ghi dữ liệu.
- **Giữ hiệu ứng lia chuột**: rê chuột vào ô thuộc các cột sửa được vẫn đổi màu chữ
  sang Caramel (badge/checkbox đổi viền Caramel) như hiện tại. Con trỏ đổi thành
  Hand để báo double-click được; bỏ con trỏ IBeam vì không còn nhập tại ô.
- **Giữ xóa tại dòng**: cột thùng rác cuối dòng, ma trận xác nhận/xóa ngay, quyền
  xóa, khóa dòng và nút **Đối chiếu** khi kết quả xóa chưa xác định giữ nguyên.

Tái dùng engine và hộp sửa hiện có:

- `OrbitRowEditor` vẫn là engine duy nhất cho thao tác dòng. Giữ nguyên `RowKey`,
  `CanWrite`, `Delete`, `Reconcile`, `Reload`, sự kiện trạng thái, khóa dòng chưa
  xác định, nút **Đối chiếu**, cơ chế requestId/revision và báo lỗi ở dòng trạng thái.
- Bổ sung đường **kích hoạt sửa** trong engine (double-click/F2) để engine gọi
  handler mở hộp sửa do từng màn cấp, thay vì mỗi màn tự gắn một pipeline sửa riêng.
  Màn cấp handler nối vào phương thức `Edit(row)` hiện có; không nhân bản logic lưu.
- Bỏ khỏi engine phần mở editor overlay và ghi từng ô (click-hai-bước, `Begin`,
  `CommitAsync`, `WriteAsync`, `OpenNeighbour`, `PositionEditor`, Enter/Tab/Esc ghi ô).
  Giữ `ColumnFor`/`EditableCell` để xác định cột hover và quyền sửa.
- Hộp sửa **dùng lại các dialog hiện có** của từng màn và control/hạ tầng Orbit sẵn
  có; không tạo thêm một dialog sửa dòng tổng quát, không nhân bản control nhập.
- `OrbitCellEditor` được giữ lại và dùng lại làm ô nhập một dòng trong hộp sửa khi
  hộp cần kiểu nhập tương ứng (text/tiền/số/ngày/giờ/danh sách/checkbox); không còn
  gắn nó làm overlay trên lưới master data. Hộp nào đã có control nhập riêng thì
  giữ control đó, không viết lại chỉ để đổi sang `OrbitCellEditor`.
- Hộp sửa Nguyên liệu dùng lại bố cục và cách lưu của hộp Thêm hiện có, chỉ ẩn các
  trường tồn đầu kỳ/nguồn tồn; không tạo form nhập mới.
- Cập nhật tài liệu UI/UX đang mô tả click-hai-bước và nhập tại ô sang mô hình
  double-click mở hộp sửa; giữ nguyên mô tả thùng rác, hover, engine dùng chung và
  đối chiếu.
- Quy tắc UI sau đợt này là **quy tắc chung cho mọi màn master data hiện có và màn
  làm sau**: click một lần chọn dòng, double-click/F2 mở hộp sửa, không nhập tại ô;
  tài liệu phải ghi rõ để màn mới không quay lại kiểu inline cũ. Đợt này không code
  thêm màn ngoài 8 màn đã nêu.

### 2.2 Ngoài phạm vi

- Không đổi Khuyến mãi, Chấm công, chứng từ, phiếu nhập/xuất, chi tiêu.
- Không đổi nghiệp vụ, service, SQL, migration, quyền hay nội dung các hộp sửa
  hiện có (trừ việc bổ sung hộp sửa Nguyên liệu và chế độ sửa Tài sản).
- Không đổi ma trận xác nhận xóa, cơ chế requestId/revision/đối chiếu.
- Không thêm menu Sửa/Xóa trở lại toolbar.
- Không thêm toast/undo; kết quả vẫn báo ở dòng trạng thái.
- Không kiểm hình thức/DPI 100%/150%; người dùng tự kiểm giao diện theo phân công.

## 3. Giả định

1. “Mở bảng” là mở hộp thoại sửa hiện có của từng màn, không phải bảng nhập
   nhiều dòng mới.
2. Double-click trên bất kỳ ô dữ liệu nào của dòng đều mở hộp sửa; double-click
   ô thùng rác chỉ thực hiện xóa theo luồng hiện tại, không mở hộp sửa.
3. Hover vẫn chỉ áp cho các cột từng sửa inline; các cột chỉ đọc giữ nguyên.
4. Ở màn Khách hàng dùng làm picker (F3), double-click không mở sửa và không có
   thùng rác; giữ hành vi chọn khách hiện tại.
5. Màn Bàn: double-click dòng trong chế độ Danh sách mở hộp sửa bàn; click card
   trong Sơ đồ vẫn mở POS như hiện tại.
6. Hộp sửa Nguyên liệu dùng lại bố cục trường Tên/Đơn vị của hộp Thêm, không cho
   nhập tồn đầu kỳ; lưu qua `MaterialService.Normalize` + `SaveAsync` với revision.
7. Tài sản sửa qua `AssetEditorDialog` (Action `Update`) để giữ file chờ và đối
   chiếu; nút **Chi tiết** hiện có vẫn mở `AssetDetailDialog` riêng.
8. F2 chỉ mở hộp sửa khi dòng hiện tại hợp lệ và người dùng có quyền; nếu đang
   xóa/đang tải thì bỏ qua.
9. “Tái dùng engine” nghĩa là giữ `OrbitRowEditor` làm engine vòng đời dòng và giữ
   `OrbitCellEditor` làm ô nhập dùng chung; **không** có nghĩa khôi phục nhập tại ô
   trên lưới. Double-click và F2 là hai đường mở hộp sửa duy nhất.
10. Hộp sửa hiện có vẫn tự lo phần nhập và `CaptureWrite`/đối chiếu như luồng Thêm;
    engine dùng chung lo kích hoạt, trạng thái bận, xóa, khóa dòng và tải lại, không
    thay thế cơ chế lưu riêng của từng dialog.

## 4. Câu hỏi mở

Không có câu hỏi chặn triển khai. Nếu người dùng muốn bỏ cả F2 hoặc chỉ cho
double-click ở một số cột, điều chỉnh trước khi duyệt plan.

## 5. Tiêu chí hoàn thành

1. Không còn editor nhập tại ô trên 8 màn; click một lần chỉ chọn dòng.
2. Double-click dòng mở đúng hộp sửa của màn, dữ liệu nạp đúng dòng; lưu reload
   và chọn lại dòng; hủy không đổi dữ liệu.
3. F2 mở hộp sửa cho dòng đang chọn.
4. Hover các cột sửa được vẫn đổi màu chữ/viền Caramel, con trỏ Hand; cột chỉ đọc
   và vùng trống không đổi.
5. Thùng rác, xác nhận theo loại dữ liệu, quyền, khóa dòng và Đối chiếu sau timeout
   xóa vẫn hoạt động như trước.
6. Màn Nguyên liệu sửa được Tên/Đơn vị qua hộp mới; Tài sản sửa được qua
   `AssetEditorDialog` và vẫn đối chiếu được khi kết quả chưa xác định.
7. Không ảnh hưởng POS, F3 picker, Khuyến mãi, Chấm công và các màn chứng từ.
8. Engine được tái dùng: `OrbitRowEditor` vẫn là nơi duy nhất giữ vòng đời dòng,
   `OrbitCellEditor` vẫn được dùng trong hộp sửa; không còn pipeline sửa inline
   song song và không có dialog sửa dòng tổng quát mới.
9. Build Debug/Release sạch; kiểm chức năng trên WinForms thật với DB test riêng
   theo đợt test được duyệt; tài liệu UI/UX được cập nhật đúng mô hình mới.
