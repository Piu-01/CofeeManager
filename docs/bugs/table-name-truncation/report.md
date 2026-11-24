# Bug — Tên bàn vượt 20 ký tự trả lỗi dữ liệu chung

Phát hiện: 23/11/2026 trong đợt test task `row-edit-dialog` (ca D-16/F-01).

## Hiện tượng

Sửa tên bàn thành chuỗi dài hơn 20 ký tự từ hộp sửa Bàn, lưu trả về
“Có lỗi dữ liệu chưa xác định; tra mã hỗ trợ trước khi tiếp tục.”, không nêu
đúng giới hạn và không chỉ ra trường tên.

## Nguyên nhân và bằng chứng

- `BAN.TenBan` là `NVARCHAR(20)` (`database/DB_Schema.sql` dòng 266), trong khi
  `TableService.Normalize` cho phép tên tới 50 ký tự cho cả bàn và khu vực
  (`src/CafeManager.Business/Services/TableService.cs` dòng 15).
- Gọi trực tiếp `sp_TableUpdate` với tên dài trả SQL error **2628**
  (“String or binary data would be truncated… TenBan”). `SqlErrorMapper` không
  ánh xạ 2628 (và 8152) nên rơi vào `Unknown`.
- `Map(SqlException)` chỉ chọn lỗi con khi mã nằm trong danh sách ưu tiên; 2628
  không có trong danh sách nên có thể chỉ thấy mã batch (3621) và tiếp tục ra
  thông báo chung.

## Đề xuất sửa (đã được duyệt)

1. `TableService.Normalize`: giới hạn tên bàn tối đa 20, khu vực tối đa 50
   (giữ `MaterialService.Required` để có thông báo trường + giới hạn).
2. `SqlErrorMapper`: ánh xạ 2628 và 8152 thành lỗi ràng buộc có hướng dẫn
   “Dữ liệu vượt độ dài cột cho phép; kiểm tra tên/mã và thử lại.”, đồng thời
   thêm hai mã vào danh sách lỗi con ưu tiên.

## Phạm vi ảnh hưởng và rủi ro

Chỉ ảnh hưởng validate tên bàn/khu vực trước khi gửi SQL và cách dịch lỗi cắt
chuỗi; không đổi procedure, cột hay dữ liệu. Tên bàn 1–20 ký tự, khu vực 1–50
ký tự giữ nguyên hành vi. Rủi ro thấp; cần hồi quy lưu bàn/khu vực và thông báo
lỗi khi tên quá dài.

## Đã sửa và kiểm chứng (23/11/2026)

- `TableService.Normalize`: tên bàn tối đa 20, khu vực tối đa 50.
- `SqlErrorMapper`: 2628/8152 → `Constraint`, thông báo độ dài; thêm vào danh sách
  lỗi con ưu tiên.
- Unit test `F01-TRUNC` (3 ca) PASS; build Debug/Release 0 lỗi.
- UI: sửa B0001 sang “Inline Table 01 ABCDE” (21 ký tự) báo “Tên: bắt buộc, tối đa
  20 ký tự…” và DB giữ nguyên; lưu tên hợp lệ “Ban 01 Test” rồi khôi phục
  “Inline Table 01” thành công.
