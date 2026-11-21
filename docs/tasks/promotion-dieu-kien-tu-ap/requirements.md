# Requirements — Ưu đãi có điều kiện, tự áp dụng, bật/tạm dừng

- Ngày: 21/11/2026
- Trạng thái: requirements + plan đã được người dùng duyệt 21/11/2026 (qua plan mode)
- Liên quan: kế thừa task `tier-promotion-stacking` (migration 038, đã ship, DB development schema 38); thay quy tắc "chọn tay một KM" của 038

## 1. Mục tiêu

Ưu đãi (KHUYENMAI) có thêm điều kiện đối tượng và phạm vi món; hệ thống tự xét và áp dụng khi mở đơn, không cần nhân viên chọn tay. Một hóa đơn có thể áp nhiều ưu đãi cùng lúc miễn thỏa điều kiện. Ưu đãi cộng dồn với % hạng thành viên. Bổ sung cơ chế bật/tạm dừng để quản lý chương trình dài hạn.

## 2. Quy tắc nghiệp vụ

| Chủ đề | Quy tắc |
|---|---|
| Hiệu lực | `NgayBD` bắt buộc, `NgayKT` có thể trống = không giới hạn; so theo ngày lập đơn, hai đầu inclusive |
| Trạng thái | `Nhap` / `DangChay` / `TamDung`; `HetHan` suy ra từ ngày; chỉ `DangChay` mới tự áp |
| Bật/tắt | Thao tác Pause/Resume có RequestId + ExpectedRevision, audit/replay; không xóa/dời ngày |
| Đối tượng khách | `ALL` (kể cả vãng lai), `FIRST` (hồ sơ chưa có hóa đơn đã thanh toán), `MEMBER` (có hồ sơ), `TIER` (thuộc danh sách hạng) |
| Phạm vi món | `ALL` hoặc danh sách món cụ thể |
| Số ưu đãi/hóa đơn | 0..N, mọi ưu đãi thỏa điều kiện đều áp; không chọn "một cái tốt nhất" |
| Giảm theo món | Chỉ trên tiền dòng đủ điều kiện; nhiều ưu đãi cùng dòng cộng % và chặn 100%/dòng; làm tròn một lần/dòng |
| Hạng | `TierDiscount = ROUND(TongTien × %hạng / 100)`; tổng giảm vượt tiền món thì cắt phần KM |
| Ship | Không giảm |
| Điểm mua | `FLOOR((TongTien − GiamGia)/1000)` |
| Đơn cũ | Không backfill; hóa đơn đã trả giữ nguyên tiền/snapshot/in |
| Đơn mở có `MaKM` trước 039 | Tính như ưu đãi legacy "tất cả món", không sửa được trên UI |
| Dữ liệu hiện có | Thành `DangChay / ALL / ALL`, giữ nguyên ngày ⇒ tự áp cho đơn còn hạn |

Xem chi tiết công thức, token và kế hoạch triển khai trong `plan.md`.

## 3. Phạm vi

### Làm

1. Migration `039_promotion_dieu_kien_tu_ap.sql`: bảng điều kiện `KHUYENMAI_HANG`, `KHUYENMAI_MON`; cột `TrangThai/DoiTuongKhach/PhamVi`; `NgayKT` nullable; bảng `HoaDonUuDaiApDung`; snapshot mở rộng; hàm/procedure dùng chung; preview/checkout/hóa đơn trả danh sách ưu đãi.
2. C# Models/DAL/BLL/UI: editor điều kiện, nút Bật/Tạm dừng, POS bỏ chọn ưu đãi, hiển thị danh sách ưu đãi tự áp, sửa layout hộp thoại khách, in/chi tiết hóa đơn.
3. Localization + tài liệu nghiệp vụ/contract/migration.
4. Test mới trên DB test riêng; không sửa test lịch sử.

### Không làm

- Không làm "đếm N ngày từ lúc bật"; không tự động chạy lại chu kỳ.
- Không làm lịch theo thứ/khung giờ (happy hour).
- Không đổi ngưỡng/% hạng, công thức tích điểm, đổi điểm/voucher/hoàn tiền.
- Không sửa hóa đơn/snapshot đã phát sinh.
- Không sửa migration 038 hay test lịch sử T13/T15.

## 4. Giả định

- "Thành viên" = khách có hồ sơ `KHACHHANG`; không có khái niệm hội viên riêng.
- "Khách lần đầu" xét tại thời điểm mở đơn: khách chưa có hóa đơn `Da thanh toan` nào trước đó; vãng lai không đạt.
- Giảm giá vẫn là một con số cấp hóa đơn (`HOADON.GiamGia`); không thêm cột giảm giá vào `CHITIET_HD`.
- Phân bổ tiền từng KM khi nhiều KM cùng dòng theo tỷ trọng %, phần lẻ dồn KM có mã cuối.

## 5. Tiêu chí hoàn thành

1. Tạo ưu đãi theo hạng/món/khách lần đầu; danh sách hiển thị đúng trạng thái và điều kiện.
2. Đơn có nhiều ưu đãi cùng thỏa: tất cả cùng áp; tạm dừng một ưu đãi chỉ loại đúng ưu đãi đó.
3. Ưu đãi theo món chỉ giảm trên dòng đủ điều kiện; trần 100%/dòng; `GiamGia <= TongTien`; ship không giảm.
4. Preview/checkout/snapshot/in hóa đơn trả đủ danh sách ưu đãi; token đổi khi sửa/tạm dừng/tạo/xóa ưu đãi.
5. Hóa đơn cũ (trước 038 và 038) đọc/in không đổi; báo cáo không nhân dòng.
6. Build sạch; test SQL trên DB test riêng; đợt test chờ duyệt riêng.
