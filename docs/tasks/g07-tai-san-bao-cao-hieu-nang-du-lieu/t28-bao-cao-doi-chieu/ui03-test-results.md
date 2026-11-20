# Biên bản G07-UI03 — Tổng quan: dialog nguồn, Cần chú ý, câu chú thích Bình quân

10/11/2026. Phạm vi đã duyệt trong [bug-report.md](bug-report.md#g07-ui03). Chỉ sửa
giao diện WinForms; không đổi SQL/schema/migration/quyền. Hình thức do người dùng tự kiểm.

## Môi trường

- Source: `main` commit `8a38f86` + thay đổi đang làm việc; DB development đã ở schema 29.
- Build: .NET SDK theo `net10.0-windows`, Debug và Release.
- App đóng trước khi build; không có giao dịch test.

## Thay đổi

| File | Nội dung |
|---|---|
| `src/CafeManager.WinForms/Modules/Reports/ReportSourceDialog.cs` | Đặt `Dock=Fill` cho control danh sách ở cả hai constructor |
| `src/CafeManager.WinForms/Modules/Home/AdminDashboardControl.cs` | Mục “Cần chú ý” chuyển sang `AlertRow` nhãn–giá trị, bấm/Enter/Space điều hướng; bỏ nút full-width |
| `.../AdminDashboardControl.cs` | Chú thích Bình quân khi kỳ chưa có hóa đơn đã trả: “Chưa có hóa đơn đã thanh toán trong kỳ.” |

Ánh xạ điều hướng: `orders.open→Invoices`, `delivery.pending→Delivery`,
`material.zero→Materials`, `debt.current→Debt` (theo lựa chọn người dùng 10/11/2026).

## Kết quả

| Mã | Nội dung | Expected | Actual | Kết quả |
|---|---|---|---|---|
| UI03-B01 | Build Debug | 0 error/0 warning | 0/0 | PASS |
| UI03-B02 | Build Release | 0 error/0 warning | 0/0 | PASS |
| UI03-S01 | `ReportSourceDialog` gắn `Dock=Fill` | Cả hai constructor | Đã gắn | PASS (đọc source) |
| UI03-S02 | Mục Cần chú ý không còn `OrbitButton` full-width | Chỉ còn `AlertRow` | Đúng | PASS (đọc source) |
| UI03-S03 | Đích điều hướng tồn tại | Invoices/Delivery/Materials/Debt trong `NavigationTarget` và `Program.cs` | Đủ 4 | PASS (đọc source) |
| UI03-S04 | Mở rộng thủ công khác ngoài phạm vi | Không | Không | PASS |
| UI03-R01 | Dialog nguồn hiển thị đầy đủ danh sách, không còn card 150×150 | Grid giãn trong 900×580 | Chưa chạy native | NOT RUN |
| UI03-R02 | Bấm/Enter hàng “Đơn đang mở” mở Lịch sử hóa đơn | Điều hướng đúng một lần | Chưa chạy native | NOT RUN |
| UI03-R03 | Bấm hàng Giao hàng/Nguyên liệu/Công nợ mở đúng workspace | Đúng tab | Chưa chạy native | NOT RUN |
| UI03-R04 | Hover/focus/DPI 125% của `AlertRow` | Không cắt chữ, focus thấy được | Chưa chạy native | NOT RUN |
| UI03-R05 | Card Bình quân hiện “Chưa có hóa đơn…” khi kỳ rỗng | Đúng câu chữ | Chưa chạy native | NOT RUN |

Các ca R01–R05 cần đăng nhập ADMIN trên app thật (không tự reset mật khẩu development).
Hình thức và DPI 100/150% theo phân công người dùng tự kiểm; không ghi PASS thay.

## Ghi chú

- `Average` NULL khi kỳ không có hóa đơn đã trả là hành vi SQL đúng (`028` dòng 53);
  không sửa dữ liệu hoặc công thức để làm card có số.
- G07-DA02 (UI chậm 3–6 giây) vẫn mở, không thuộc đợt sửa này.
