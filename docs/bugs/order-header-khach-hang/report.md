# Bug / thiết kế lại — hộp Khách hàng (F3)

- Ngày: 22/11/2026
- Người báo: người dùng (ảnh chụp hộp "Khách hàng")
- Trạng thái: **đã sửa code, build Release 0 lỗi; người dùng duyệt bố cục 22/11/2026,
  hình thức vẫn do người dùng nghiệm thu khi chạy app thật**

## Hiện tượng

- Hộp Khách hàng cao, nội dung bị cắt/cuộn; dòng giải thích "Ưu đãi được SQL tự xác
  định…" và dòng "Tối đa 50 kết quả…" là chữ kỹ thuật, không cần cho người dùng.
- Tìm khách phải bấm nút/Enter; gõ tiếp không lọc lại. Kết quả nằm trong ComboBox
  nên khó quét 50 dòng, chọn khách rồi tìm lại thì mất lựa chọn (ComboBox bị gán lại
  DataSource, quay về Khách vãng lai).

## Nguyên nhân

`OrderHeaderDialog` dùng một ComboBox cho cả danh sách kết quả; mỗi lần `Search()`
gán `DataSource` mới và reset `SelectedValue`. Thông báo trạng thái chứa mô tả SQL và
giới hạn 50 dòng lấy từ procedure. Không có vùng tóm tắt khách đang chọn.

## Thay đổi

- Bảng kết quả (`DataGridView` chỉ đọc, cột Khách hàng/SĐT/Hạng/Điểm) thay ComboBox;
  chọn dòng = chọn khách, giữ lựa chọn khi gõ lại từ khóa.
- Tìm tự động sau 350 ms khi gõ; nút kính lúp/Enter tìm ngay. Lọc theo tên, SĐT hoặc
  mã khách (đúng contract `sp_CustomerList`), từ khóa được trim.
- Dải "Đã chọn …" + nút **Khách vãng lai** để bỏ chọn; thông báo chỉ còn khi không
  tìm thấy hoặc lỗi.
- Bỏ toàn bộ chữ SQL/giới hạn 50 dòng khỏi hộp thoại.
- Đơn Giao hàng: bố cục hai cột (khách bên trái, thông tin giao hàng bên phải) để
  không vượt màn hình 1366×768 tại 125%; trường nhận/SĐT, địa chỉ, đối tác/phí ship.
- Nút Áp dụng là nút chính; giữ chọn hết nội dung khi vào ô, validation và nháp như cũ.

## Đồng bộ bố cục với các editor trong app (lượt 22/11/2026)

- Dùng `OrbitDialogField` có nhãn cho cả ba khối: **Tìm khách** (ô nhập + nút Tìm/Thêm
  khách), **Kết quả tìm khách** (bảng, cao cố định theo DPI), **Khách đang chọn**
  (dải tóm tắt + nút Khách vãng lai) — cùng kiểu nhãn trên/control dưới như
  `HrEditorDialog`, `OperationEditorDialog`.
- Bỏ padding 16 dư ở container `fields`; lề ngoài do margin của từng field quy định
  như các editor khác. Panel giao hàng có lề phải 16 để thẳng hàng với mép hộp.
- `ErrorProvider` dùng `BlinkStyle.NeverBlink` như editor HR; nút **Áp dụng** chính,
  **Quay lại** là `CancelButton`; bảng kết quả có viền 1px và style `DialogInput`.
- Kích thước: 640×640 (đơn thường) và 860×640 (Giao hàng); `FitToContent` sau khi
  hiện, giới hạn WorkingArea.

## File

- `src/CafeManager.WinForms/Modules/Sales/OrderHeaderDialog.cs`
- `src/CafeManager.Models/Localization/Translations/en.json` (bỏ khóa chỉ dùng cho
  chữ SQL/50 dòng; thêm khóa cho placeholder, dải đã chọn, không tìm thấy, bảng kết quả)

## Kiểm chứng

- `dotnet build -c Release`: Build succeeded, 0 lỗi (cảnh báo tham chiếu WebView2 có sẵn).
- Debug đang bị tiến trình app đang mở khóa DLL; cần đóng app để build Debug.
- Không chạy kiểm GUI/hình thức; người dùng đã duyệt bố cục ngày 22/11/2026 và tự
  nghiệm thu khi chạy app thật. Không ghi PASS hình thức thay người dùng.
- Còn lại: đóng app đang mở, build Debug/publish theo nhu cầu; không ảnh hưởng
  migration hay DB.
