# Khuyến mãi — sửa bằng hộp sửa, xóa tại dòng và lọc theo trạng thái

Yêu cầu người dùng 24/11/2026: "chỉnh lại ui sửa xóa của khuyến mãi, thêm chức năng lọc
theo trạng thái (listbox)".

## Mục tiêu

- Đưa màn Khuyến mãi về đúng chuẩn master data đã chốt ngày 23/11/2026 (guide 11.5):
  bỏ nút **Sửa/Xóa** trên thanh hành động, double-click ô dữ liệu hoặc F2 mở hộp sửa
  hiện có, thùng rác cuối dòng để xóa có xác nhận.
- Thêm bộ lọc **Trạng thái** cho danh sách khuyến mãi: Tất cả, Áp dụng, Tạm ngưng,
  Chưa tới kỳ, Hết hạn.

## Phạm vi

- `PromotionList` trong `LoyaltyControl.cs`: bỏ `AddAction("Sửa"/"Xóa")`, dùng
  `AddRowDeleteColumn` + `CreateInlineEditor` (`Edit`, `Delete`, `Reconcile`, `Reload`,
  `RowKey`, `CanWrite` theo ADMIN), thêm combobox lọc trạng thái vào hàng bộ lọc.
- Lọc chạy ở SQL để đúng với phân trang: `sp_PromotionList` nhận thêm `@Status`
  (migration `045`) và tự tính trạng thái hiệu lực theo ngày hôm nay của server.
- Cột Trạng thái hiển thị theo đúng mã SQL trả về (`DangChay`/`TamDung`/`ChuaToiKy`/
  `HetHan`) để hiển thị và lọc luôn khớp nhau; giữ màu chữ như hiện tại.
- DAL/BLL truyền và kiểm tra giá trị lọc; CASH (chỉ thấy KM đang áp dụng hôm nay)
  không hiện bộ lọc.
- Cập nhật guide 11.5/UX: Khuyến mãi đã chuyển; chỉ còn Chấm công chưa chuyển.

## Không làm

- Không đổi nội dung hộp Thêm/Sửa khuyến mãi, không đổi `sp_PromotionWrite`,
  cách preview/áp dụng khuyến mãi, quyền hay luồng hạng thành viên.
- Không thêm trạng thái mới, không đổi cột/thứ tự cột hiện có.

## Giả định

- "listbox" = combobox lọc trong hàng bộ lọc (OrbitFilterBox) như các màn khác; chọn
  một giá trị tại một thời điểm.
- Trạng thái lọc là trạng thái hiệu lực hôm nay (theo ngày server), khớp nhãn đang
  hiển thị trên lưới.
- Ngày kết thúc trống = áp đến hết năm; kỳ vắt năm (20/12–10/01) giữ quy tắc hiện có.

## Tiêu chí hoàn thành

1. Trên màn Khuyến mãi không còn nút Sửa/Xóa; double-click ô dữ liệu hoặc F2 mở hộp
   sửa đúng dòng; thùng rác xóa đúng dòng sau xác nhận; kết quả chưa xác định vẫn
   khóa dòng + nút Đối chiếu.
2. Chọn từng trạng thái lọc trả đúng danh sách, quay về trang 1, không lẫn dòng khác
   trạng thái; "Tất cả" trả toàn bộ; tìm kiếm kết hợp được với bộ lọc.
3. CASH không thấy bộ lọc và vẫn chỉ thấy KM đang áp dụng hôm nay.
4. Build Debug/Release 0 lỗi; kiểm chức năng theo đợt test được duyệt.
