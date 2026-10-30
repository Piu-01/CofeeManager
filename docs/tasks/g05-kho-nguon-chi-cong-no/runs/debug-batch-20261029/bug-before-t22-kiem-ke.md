# G05-B02 — Danh sách kiểm kê có LinesJson NULL gây exception

27/10/2026. **Chờ duyệt đợt bug; chưa sửa.**

ADMIN fixture mở Kho → Kiểm kê: hiện hộp Unhandled exception, thông báo `Data is Null`. Stack trace trỏ `StocktakeRepository.Map`, dòng23, `SqlDataReader.GetString`.

Bằng chứng tại `../runs/acceptance-full-20261027_110730/`: `gui-stocktake-exception.txt`, `gui-stocktake-stack.txt`, `stocktake-null-evidence.json`. Phiếu KK99000001–03 là **header rỗng cố ý tạo bằng quyền owner cho ca âm ngày cũ/tương lai/rỗng**, không phải phiếu rỗng tạo được qua procedure runtime. Không suy mọi phiếu bình thường bị lỗi. Migration020 cũng không loại bỏ header legacy rỗng; trường hợp đọc tập dòng rỗng cần được xử lý an toàn.

Nguyên nhân đã đối chiếu: `vw_StocktakeRead.LinesJson` trả SQL NULL cho header không có chi tiết trong môi trường SQL Server này; mapper gọi `r.GetString(6)` trước `JsonSerializer.Deserialize(...) ?? []`. Toán tử `?? []` không xử lý được NULL tại bước GetString. Exception không thuộc nhóm lỗi mà LoadRows đang xử lý nên thoát ra event async của WinForms.

Đề xuất sửa tối thiểu mapper: kiểm `IsDBNull(6)` và ánh xạ tập dòng rỗng thành danh sách rỗng; bảo đảm màn chi tiết vẫn chặn chốt phiếu rỗng theo contract. Không thay số đếm/kho/ngày, không xóa fixture để biến FAIL thành PASS, không nới quy tắc SQL. Nếu quyết định chuẩn hóa JSON tại SQL thì phải dùng migration mới; không sửa020 đã áp dụng.

Regression: đọc legacy/header rỗng, phiếu chưa đếm có Actual NULL, Actual0, phiếu chốt và danh sách trộn; mở/reload native không crash. T22-K06 phải tiếp tục từ chối chốt rỗng; K11 reload không lỗi. Rủi ro chính là che dữ liệu không hợp lệ nếu cho phép chốt; giữ nguyên validation hiện có.
