# Plan bộ task — Khuyến mãi v2 (migration 042)

Căn cứ: `requirements.md` đã duyệt 22/11/2026 và `mockup.html` đã duyệt.
Thứ tự triển khai: **t01 → t02 → t03 → t04 → t05**. Một task tại một thời điểm;
chưa nghiệm thu PASS trong pha triển khai (test chờ duyệt riêng sau).

| Task | Slug | Nội dung | Phụ thuộc |
|---|---|---|---|
| t01 | `t01-migration-042` | Migration 042: schema, chuẩn hóa ngày/đối tượng/trạng thái, hàm ưu đãi theo ngày/tháng, quà tặng, preview/checkout/in | 039 đã áp |
| t02 | `t02-models-data-business` | Models request/row/detail, DAL `LoyaltyRepository`, `LoyaltyService.Normalize`, checkout/invoice DTO | t01 |
| t03 | `t03-ui-promotion` | Danh sách + editor khuyến mãi theo mockup; bỏ Nháp/Bật-Tạm dừng/Preview | t02 |
| t04 | `t04-sales-gift-display` | Hiển thị quà ở checkout, chi tiết, in tạm, in hóa đơn | t02 (t01 cho dữ liệu) |
| t05 | `t05-tests` | Fixture + 4 script SQL + runner + biên bản PM01–PM08 | t01–t04 |

Quyết định kỹ thuật đã chốt trong plan (không mở lại khi code):

1. **Ngày/tháng**: lưu `date` với năm tham chiếu 2000 cho `NgayBD`, 2001 chỉ khi
   `dd/MM` kết thúc nhỏ hơn bắt đầu (vắt năm); `NgayKT NULL` = từ ngày bắt đầu đến
   hết năm. Nghiệp vụ so bằng `MONTH*100+DAY` qua hàm `fn_NgayTrongKy`.
2. **Quà tặng** ghi thành dòng `CHITIET_HD` với `GiaBan=0`, cột mới
   `MaKMQuaTang`, `GhiChu='Tặng kèm'`; nới `CHK_CTHD_GiaBan` thành `>=0`.
   Nhờ vậy tổng tiền, tồn kho, in ấn dùng đúng luồng hiện có; dòng quà chỉ sinh
   trong `sp_ThanhToanHoaDon`.
3. **Không snapshot riêng cho quà**: dòng quà đã bất biến sau khi trả tiền; tên KM
   đọc join `KHUYENMAI` giống cách 039 đang đọc tên ưu đãi đã áp.
4. **Token preview** phải đổi khi nội dung quà đổi: thêm digest quà vào
   `fn_CheckoutPreview` qua `fn_HoaDonQuaTangDigest`.
5. **In tạm** hiển thị danh sách quà dự kiến (kết quả thứ 6 của `sp_InvoiceDetail`,
   chỉ có với đơn mở); hóa đơn đã trả hiển thị dòng quà thật trong danh sách món.
6. Giữ tham số/action `Pause/Resume` phía SQL để tương thích, nhưng UI không còn
   nút; trạng thái đổi qua hộp sửa.

## Điều kiện chung

- Không sửa migration 001–041; chỉ thêm `042_promotion_v2.sql`.
- SQL triển khai trước, DAL/BLL/UI sau; build Debug/Release sau mỗi task.
- Không chạy test trên DB sử dụng thật; DB dev chỉ áp sau khi người dùng duyệt.
- Hình thức UI do người dùng nghiệm thu; agent chỉ kiểm chức năng và DPI 125%.
