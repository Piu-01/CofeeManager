# Bug — Mở Ưu đãi/Điểm ném IndexOutOfRange khi CSDL thiếu migration khuyến mãi v2

Phát hiện: 23/11/2026 trong đợt test task `row-edit-dialog` (ca D-23/F-02); từng
quan sát ngày 22/11 trên cùng DB test.

## Hiện tượng

Chuyển sang màn **Ưu đãi / Điểm** hiện hộp `Unhandled exception ... Index was
outside the bounds of the array.`; bấm Continue thì màn trống, không tải danh sách
chương trình.

## Nguyên nhân và bằng chứng

- DB test dùng trong đợt test có `MAX(Version)=41` trong `SchemaMigrations`;
  `sp_PromotionList` trong DB là bản trước v2, trả ít cột hơn.
- `LoyaltyRepository.Promotion` đọc cố định các cột 0–12 theo hợp đồng của
  migration `042_promotion_v2.sql`; khi kết quả ít cột hơn, `GetString/GetInt32`
  ném `IndexOutOfRangeException`, không phải lỗi SQL nên không được
  `SqlErrorMapper` dịch và thoát ra `Application.ThreadException`.
- Đối chiếu: DB phát triển `CoffeeManager_Development` có schema 43, đủ hợp đồng
  v2; lỗi chỉ xuất hiện trên DB chưa áp migration cần thiết.

## Đề xuất sửa (đã được duyệt)

1. Thêm kiểm tra số cột tối thiểu trong `LoyaltyRepository.Promotion`; nếu thiếu
   thì ném `DataAccessException` loại `Schema` với thông báo “CSDL chưa áp
   migration cần thiết…” để màn hiện lỗi có hướng dẫn thay vì crash.
2. Áp migration 041→042 cho DB test và chạy lại màn Ưu đãi/Điểm để xác nhận
   đường bình thường.

## Phạm vi ảnh hưởng và rủi ro

Chỉ thêm guard đọc kết quả và cập nhật DB test; không đổi procedure, DTO hay
nghiệp vụ ưu đãi. DB đã đủ migration giữ nguyên hành vi. Rủi ro thấp; cần hồi quy
mở danh sách Khuyến mãi, xem/sửa ưu đãi trên DB schema chuẩn.

## Đã sửa và kiểm chứng (23/11/2026)

- `LoyaltyRepository.Promotion`: nếu kết quả <13 cột thì ném
  `DataAccessException` loại `Schema` thay vì để `IndexOutOfRange` thoát ra.
- Trên DB test schema 41: mở Ưu đãi/Điểm không còn hộp unhandled exception; màn
  báo “Chưa cập nhật · CSDL chưa áp migration cần thiết…”.
- Áp migration 042 vào DB test (version 42): mở lại Ưu đãi/Điểm hiển thị
  “1–17 / 17 chương trình”, không lỗi. Build Debug/Release 0 lỗi.
