# T35 — Bảng rà soát tương thích (16/11/2026)

Phạm vi: sau T33 (trạng thái `Cho thanh toan`) và T34 (phiên bàn) rà mọi nơi còn
hiểu “đơn mở = `Chua`” hoặc “bàn trống = không có đơn `Chua`”. Không đổi 56 ID
T14–T18, 325 ID toàn dự án, không sửa migration đã áp, không thêm migration.

Kết luận: **giữ** nếu hành vi đã đúng theo 4 trạng thái/phiên; **sửa** nếu thiếu
nhánh `Cho` hoặc thiếu `Retained`. Chưa có lỗ hổng SQL cần migration bổ sung.

## 1. C# — trạng thái đơn

| Điểm | Hiện tại | Tác động | Hành động |
|---|---|---|---|
| `SalesOrderDto.cs:10` `CanEdit` | Chỉ `Chua thanh toan` | Đúng: khóa sửa khi `Cho` | Giữ |
| `SalesOrderDto.cs:12` `IsPayable` / `:13` `IsOpen` | `Chua` hoặc `Cho` | `IsOpen` dùng cho preview (`OrderPanel.cs:238`), không phải “còn sửa” | Giữ, ghi rõ nghĩa |
| `SalesStates.cs:8` | Nhãn `Cho thanh toan` → “Chờ thanh toán” | Đủ 4 trạng thái | Giữ |
| `InvoicesControl.cs:19` | Lọc đủ 4 trạng thái | Đúng | Giữ |
| `InvoiceDetailDialog.cs:32-33` | In chỉ `Da`; “Quay lại POS” theo `IsPayable` | Đúng (`Chua`/`Cho` quay lại, `Da`/`Huy` tắt) | Giữ |
| `OrbitRecordListControl.cs:58` | Dịch `Trong`/`Dang phuc vu`/4 trạng thái tiền | Đủ | Giữ |
| `OrbitDataGridView.cs:433-435` | `InvoiceHistory`: `Chua`/`Cho` nhóm chờ; `Da` xanh, `Huy` đỏ | Đúng | Giữ |
| `OrbitDataGridView.cs:434` (Delivery) | Nhóm chờ chỉ có `Chua thanh toan`; `Cho thanh toan` rơi màu xám | Thiếu nhánh `Cho` sau T33 | **Sửa**: thêm `Cho thanh toan` vào pending |
| `DeliveryControl.cs:25` | Lọc thanh toán chỉ Chua/Da/Huy | Đơn giao có thể `Cho` nhưng không lọc được | **Sửa**: thêm “Chờ thanh toán” |
| `DeliveryControl.cs:41,90-94` | Dịch/lọc qua `SalesStates.Label`; chuyển bước không đụng tiền | Đúng sau T33 | Giữ |
| `SalesControl.cs:203-213` | Nút theo `CanEdit`/`IsPayable`; Trả bàn riêng, không bật khi còn đơn/nháp | Đúng T33/T34 | Giữ |
| `SalesControl.cs:208,742` | Mở lại chỉ `Cho` + ADMIN | Đúng | Giữ |
| `OrderPanel.cs:55-56` | Badge `Cho` màu caramel, `Da` lilac, `Huy` đỏ | Đúng | Giữ |
| `OrbitTableCard.cs:27-28` | `busy = Status != "Trong"`; nhãn `Retained` → “Đã trả · giữ bàn” | Đúng T34 | Giữ |
| `TablesControl.cs:40` | Card + action “Trả bàn” theo `SessionCode` | Đúng T34 | Giữ |
| `TableRow`/`SalesOrderHeader` | Có `SessionCode`/`SessionRevision`/`Retained` | Đúng contract 034 | Giữ |
| `InvoiceDetailDialog`/in | Không đọc `PHIEN_BAN` | Đúng: in theo MaHD, không phụ thuộc phiên | Giữ |

Grep còn lại trong `src/` cho `Chua thanh toan`/`Cho thanh toan`/`Trong`/`Status ==`
đã kết luận ở bảng trên; không còn chỗ nào hiểu `Cho` là sửa được.

## 2. SQL — đọc/đếm/lọc

| Điểm | Kết luận |
|---|---|
| `sp_SalesCheckOpen` (015, còn hiệu lực) | Chỉ `Chua` — đúng cổng sửa; 032 tách `sp_SalesCheckPayable` cho thu/hủy |
| `sp_SalesReadCore`/`sp_SalesOpenOrders`/`sp_TaoHoaDon`/`sp_TaoHoaDonTuNhap` | Bản cuối ở 032/034 đã gồm `Chua`+`Cho` và gắn phiên; bản cũ 010/015/030 chỉ là lịch sử |
| `sp_ReportDashboard`, `sp_DashboardAnalyticsRead/Sources` | Bản 032 đếm `Chua`+`Cho`; doanh thu chỉ `Da` — kiểm bằng TS12/PP09 |
| `vw_TrangThaiBan`, `UX_HD_BanMo`, `CHK_HD_TrangThai` | Bản cuối 032/034; fallback đơn cũ không phiên |
| `sp_InvoiceList`/`sp_InvoiceDetail` (013) | Lọc trạng thái theo tham số, không hard-code. **Đính chính 17/11/2026:** sp_InvoiceDetail còn trả 25 cột header trong khi DAL T34 đọc 27 cột → bug T34-01, đã bổ sung migration035 |
| `sp_DeliveryList` (014) | Nhận mọi giá trị `@Status`; chỉ UI thiếu mục `Cho` (đã sửa ở mục 1) |
| `HOADON.MaPhien`/`PHIEN_BAN` | Chỉ ghi qua procedure; app đọc phiên qua `sp_TableList`/`SelectCurrent`/`sp_SalesReadCore` |
| `TR_CTHD_TongTien`, `TR_CTTT_KiemTra`, `TR_DiscountSnapshot`, `TR_TieuHao_Kho` | 032/015 đã chấp nhận `Cho` trong transaction thu; không đụng lại |

Rà soát 16/11/2026 kết luận không cần migration; đợt test 17/11/2026 phát hiện
sp_InvoiceDetail thiếu `MaPhien`/`SessionRevision` ⇒ bổ sung migration035 (T34-01),
không phát hiện predicate SQL nào khác còn sai sau 032/034.

## 3. Tài liệu đã cập nhật

- `docs/UX_QuanLiQuanCafe.md`: nhãn bàn 4 trạng thái, luồng phiên bàn, “Trả bàn
  ngay?” sau thu/hủy, gọi thêm không gộp, không tự đóng/backfill.
- `docs/database/sales-contract.md`, `table-contract.md`, `checkout-contract.md`,
  `invoice-contract.md`, `data-contract.md`, `write-ownership.md`, `migrations.md`:
  4 trạng thái, hai cổng, `MaPhien`/`PHIEN_BAN`, view + `Retained`, `sp_TraBan`/
  `TableClose`, 52108/52109/52110/52111, quyền ghi, thứ tự 032–034.
- `docs/tasks/g04-.../development-guide.md`: cách áp 032→033→034 có backup/preflight,
  dùng thử Yêu cầu thanh toán/Trả bàn, giới hạn không chạy seed/fixture trên dev.

## 4. Chưa chạy

Regression CP01–CP08 và TS/PP thuộc đợt test phải được duyệt riêng; hình thức
SKIPPED, DPI 100/150% NOT RUN theo phân công; hai connection (TS04) ở đợt
concurrency riêng. T35 không tự đánh dấu hoàn tất G04/G08.
