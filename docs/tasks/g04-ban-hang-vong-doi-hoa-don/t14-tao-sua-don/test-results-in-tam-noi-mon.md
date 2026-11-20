# Kết quả đợt test "In tạm" và nối thêm món — 20/11/2026

Đợt test được người dùng duyệt bằng "duyệt" sau khi chốt plan
`plan-in-tam-noi-mon.md`. Source cố định trong suốt lượt test; không sửa code,
migration hay expected. Migration 036 áp cho DB test mới; sau lượt test đã áp
`CoffeeManager_Development` (Version 36, checksum `67B7C5D2…17E0D`, app không giữ
kết nối tới DB, số `HOADON` giữ 3451).

## Môi trường

| Mục | Giá trị |
|---|---|
| Source | commit `5673f51` + thay đổi chưa commit của đợt "In tạm" (migration 036, `SalesOrderDto`, `SalesControl`, fixtures) |
| SQL | SQL Server `localhost`, DB test cô lập dựng mới từ `DB_Schema.sql` + `Insert_Data.sql` + migration 001–036: `CoffeeManager_Test_G04_20261120_111724_d63f72cb_demo` (schema 36) |
| Fixture SQL | `database/tests/g04/acceptance-pending-payment.sql`, `acceptance-pending-add-items.sql` — cả hai **PASS**; manifest `runs/test_20261120_111724_d63f72cb/manifest.json` (Status=Succeeded) |
| App UI | WinForms Debug build tại `%TEMP%\CoffeeManager_build_check`, chạy DB test trên, đăng nhập CASH `thungan` |
| DPI | 125% (kỹ thuật); 100%/150% và mọi kiểm hình thức — SKIPPED theo phân công |
| Tài khoản DB test | Chỉ dùng cho DB test dùng một lần; không ghi mật khẩu vào repo |

Chuẩn bị riêng của DB demo (không phải thay đổi source): seed đặt `AppSetupState`
về `Completed` và tài khoản lấy hash/HoatDong từ môi trường dev để đăng nhập được;
đây là dữ liệu fixture, không phải hành vi app.

## Ca kiểm thử theo plan mục 3

| Mã | Ca (đã duyệt) | Expected | Actual | Kết quả |
|---|---|---|---|---|
| PP-IT01 | In tạm đơn mới | Không hộp xác nhận, preview mở ngay; SQL `Cho thanh toan`, receipt `SalesRequestPay`, revision tăng | Bấm "In tạm" (Primary, bật) → không có hộp xác nhận, mở cửa sổ "Xem trước — Hóa đơn HD20250419"; SQL `HD20250419 Cho thanh toan 25000`; receipt `SalesCreate` rev 164 → `SalesRequestPay` rev 165 | PASS |
| PP-IT02 | Thêm món khác sau In tạm | Cùng `MaHD`, dòng mới, `TongTien` tăng, vẫn `Cho` | Bấm "Thêm Ca phe sua" → dòng mới `MaDong 32` SL1 30.000; tổng `80.000`; vẫn `Cho`; receipt `SalesLine` rev 168 | PASS |
| PP-IT03 | Thêm lại món cũ | Một dòng SL 2; SQL chỉ một `MaDong` | Bấm "Thêm Ca phe den" lần hai → `CHITIET_HD` chỉ còn 2 dòng: `31 Ca phe den SL2`, `32 Ca phe sua SL1`; không sinh dòng trùng; receipt `SalesLine` rev 171 | PASS |
| PP-IT04 | Giảm SL / xóa / đổi ghi chú khi `Cho` | UI khóa; procedure và DML trực tiếp trả `52110` | Lưới dòng đơn không còn control sửa (dump chỉ có DataItem, không Edit); fixture NA06 (procedure) và NA07 (UPDATE giảm / đổi ghi chú / DELETE trực tiếp) đều trả `52110`, không rò thay đổi | PASS (SQL + UI khóa) |
| PP-IT05 | F3 sau In tạm | Chọn ưu đãi/hạng → `GiamGia`/`ThanhTien` tính lại toàn đơn | Mở "F3 · Khách / Ưu đãi" (bật), chọn khách `KH001` (BKIM 10%), Áp dụng → `MaKH=KH001`, `GiamGia=8.000` trên tổng 80.000, `ThanhTien=72.000`, vẫn `Cho`; receipt `SalesHeader` rev 178 | PASS với hạng 10%; **NOT RUN** nhánh promo 30% — seed demo không có `KHUYENMAI` hiệu lực ngày 20/11 (fixture NA08 cũng chuyển sang hạng) |
| PP-IT06 | In lại sau khi nối món | Preview đủ món/tổng mới, trạng thái không đổi | Bấm lại nút in (nhãn hiển thị "In lại") → mở lại preview `HD20250419`; SQL vẫn `Cho`, tổng `80.000`, `Revision` không đổi (171), không sinh receipt mới | PASS |
| PP-IT07 | Thu tiền sau khi nối món (UI) | Tiền/tiêu hao/điểm khớp; hóa đơn `Da thanh toan` | Mở F8: dialog hiển thị Tiền món 80.000, Ưu đãi −8.000, Phải thanh toán 72.000, "Đủ tiền thanh toán · Tiền thối 0 ₫". **BLOCKED** bước bấm "Xác nhận thanh toán" — guard công cụ chặn giao dịch tiền thật; đã đóng bằng "Quay lại", không phát sinh thanh toán. Nhánh tiền/tiêu hao/điểm chứng minh bằng fixture NA10 (PASS) | BLOCKED (UI commit); SQL PASS |
| PP-IT08 | Xung đột revision, hủy đơn `Cho`, trả bàn | `52103`; hủy `Cho` không hoàn kho/tiền; trả bàn bị chặn khi còn đơn | Fixture NA05 (revision cũ → `52103`), NA12 (hủy `Cho` có dòng nối → `Huy`, không tiền/tiêu hao) PASS; UI: "Trả bàn" disabled khi có đơn mở; không chạy 2 phiên UI độc lập | PASS ở SQL; hai phiên UI NOT RUN |
| PP-IT09 | Regression tạo đơn/in lại | Tạo đơn mới, nối món, in lại không lỗi | Tạo `HD20250419` từ bàn trống, thêm/nối món, in tạm, in lại — không lỗi; in sau thanh toán NOT RUN (PP-IT07 blocked) | PASS phần đã chạy |

## Bằng chứng SQL cuối lượt

| Dữ liệu | Giá trị |
|---|---|
| `HOADON HD20250419` | `Cho thanh toan`, TongTien 80.000, GiamGia 8.000, PhiShip 0, ThanhTien 72.000, MaKH KH001, Revision 178 |
| `CHITIET_HD` | `31 Ca phe den SL2 25.000`; `32 Ca phe sua SL1 30.000` |
| `G03OperationReceipt` | SalesCreate 164 → SalesRequestPay 165 → SalesLine 168 → SalesLine 171 → SalesHeader 176 → SalesHeader 178; không có receipt cho lần in lại |
| Thanh toán/tiêu hao | `CHITIET_THANHTOAN` 0 dòng, `TIEUHAO_HOADON` 0 dòng (chưa checkout) |
| `KHACHHANG KH001` | Điểm 3.550 (không đổi — chưa thu tiền) |

## Ghi nhận trong lượt (không sửa source)

- **Nhãn trợ năng của nút in**: `SalesUi.Button` đặt `AccessibleName` một lần bằng
  caption lúc tạo, nên UIA vẫn đọc "In tạm" sau khi `Text` đổi thành "In lại"
  (`SalesControl.cs` dòng 199). Chức năng đúng (bấm lần hai là in lại, không đổi
  trạng thái); ghi nhận để cân nhắc cập nhật `AccessibleName` khi đổi nhãn.
- **F3 lưu header không đổi dữ liệu** vẫn tăng revision (lần Áp dụng trống 176);
  đúng cơ chế revision hiện có, không phải lỗi.
- Fixture lịch sử `technical-v16.sql` **không chạy** trong lượt này (kỳ vọng cũ
  `52102` khi thêm món ở `Cho` đã bị thay bởi yêu cầu mới); giữ nguyên làm lịch sử.

## Chưa chạy / rủi ro còn lại

- **BLOCKED**: bấm "Xác nhận thanh toán" trên UI (guard giao dịch tiền thật);
  checkout đầy đủ đã được kiểm bằng fixture NA10 trên DB test.
- **NOT RUN**: promo 30% qua UI (seed demo không có KM hiệu lực 20/11); nhánh
  promo được kiểm bằng SQL trong các đợt trước, NA08 kiểm hạng.
- **NOT RUN**: đua hai phiên UI độc lập; `52103` kiểm ở SQL (NA05).
- **NOT RUN**: 100%/150% DPI, kiểm hình thức/thị giác — SKIPPED theo phân công.
- Migration 036 đã áp lên `CoffeeManager_Development` ngày 20/11/2026 (Version 36,
  checksum `67B7C5D2…17E0D`); chỉ thay procedure/trigger, dữ liệu giữ nguyên.
