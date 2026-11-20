# Plan phát sinh — "In tạm" và nối thêm món (20/11/2026)

**Trạng thái:** người dùng duyệt hướng trong trao đổi 20/11/2026 (chọn phương án gộp dòng
khi tăng số lượng). Đã triển khai source và **đã chạy đợt test riêng** (người dùng duyệt
"duyệt" cùng ngày): SQL fixture PP/NA PASS; UI PASS các ca 1–4, 6, 9 phần đã chạy; ca 5
PASS với hạng 10% (promo 30% NOT RUN do seed); ca 7 bước bấm xác nhận thanh toán trên UI
**BLOCKED** bởi guard giao dịch tiền, nhánh tiền/tiêu hao/điểm đã PASS ở fixture NA10.
Chi tiết: `test-results-in-tam-noi-mon.md`. Migration 036 đã áp lên
`CoffeeManager_Development` (Version 36, checksum `67B7C5D2…17E0D`, dữ liệu
giữ nguyên) sau khi người dùng duyệt.

**Bối cảnh:** T33 khóa sửa đơn ở `Cho thanh toan` và chỉ ADMIN có "Mở lại đơn". Người dùng
dùng thử thấy gọi quản lý mỗi lần khách gọi thêm là không thực tế. Chốt lại:
`In tạm` lưu đơn, CASH vẫn nối thêm món vào cùng hóa đơn; bỏ nút mở lại.

Sửa đổi requirements: `requirements-cho-thanh-toan-phien-ban.md` §9; UX §7.2.

## 1. Quy tắc nghiệp vụ

| Thao tác khi `Cho thanh toan` | Kết quả |
|---|---|
| In tạm (từ `Chua`) | Lưu đơn, in preview ngay, không hỏi xác nhận |
| Thêm món mới | Cho phép, dòng mới trong cùng `MaHD` |
| Thêm lại món cũ cùng giá/ghi chú | Update tăng SL → một dòng (SL 1 → 2) |
| Giảm SL, xóa dòng, đổi ghi chú dòng | Từ chối `52110` ở SQL; UI khóa |
| F3 khách/ưu đãi/ship | Cho phép (tạm thời), giảm giá tính lại toàn đơn |
| In lại | Cho phép, không đổi trạng thái |
| Thu tiền / Hủy / Trả bàn | Giữ như T33–T34 |

Ưu đãi tự động theo số món (điều kiện trong `KHUYENMAI`) **chưa làm**, ghi nhận mở rộng sau.

## 2. Thay đổi

| Lớp | File | Nội dung |
|---|---|---|
| SQL | `database/migrations/036_sales_pending_add_items.sql` | `sp_SuaDongHoaDon`: cổng `sp_SalesCheckPayable`; khi `Cho` chặn Delete/giảm SL/đổi ghi chú bằng `52110`. `sp_SuaThongTinHoaDon`: dùng `sp_SalesCheckPayable` để F3 còn sửa khi `Cho`. Không đổi bảng/dữ liệu |
| Models | `SalesOrderDto.cs` | `IsPending`, `CanAddLine`, `CanEditHeader`; giữ `CanEdit` cho sửa dòng |
| UI | `SalesControl.cs` | Nút "In tạm" Primary, in ngay; đổi nhãn "In lại" khi Chờ; bỏ nút/hàm `ReopenOrder`; bật thêm món khi Chờ; F3 bật khi Chờ; inline +/-/xóa/ghi chú vẫn khóa |
| Docs | UX §7.2, requirements §9, development-guide | Luồng mới, schema đích 036 |

## 3. Ca kiểm thử đề xuất (đợt test riêng, DB test schema 036)

1. In tạm đơn mới: không hộp xác nhận, preview mở ngay; SQL `Cho thanh toan`, receipt
   `SalesRequestPay`, revision tăng.
2. Thêm món khác sau In tạm: cùng `MaHD`, dòng mới, `TongTien` tăng, vẫn `Cho`.
3. Thêm lại món cũ: một dòng SL 2; SQL chỉ một `MaDong`.
4. Giảm SL / xóa dòng / đổi ghi chú khi `Cho`: UI khóa và gọi thẳng procedure trả `52110`.
5. F3 sau In tạm: chọn ưu đãi 30% → `GiamGia`/`ThanhTien` tính lại trên toàn đơn.
6. In lại sau khi nối món: preview đủ món/tổng mới, trạng thái không đổi.
7. Thu tiền sau khi nối món: tiền/tiêu hao/điểm khớp; hóa đơn `Da thanh toan`.
8. Xung đột revision hai phiên (`52103`), hủy đơn `Cho`, trả bàn còn đơn bị chặn.
9. Regression: đơn nháp/bỏ nháp, tạo đơn mới, bàn trống, in sau thanh toán.
