# G04-POS-07b — Kết quả test "vào Tại quán theo cơ chế Đơn mới"

Ngày 16/11/2026. Phạm vi: thay đổi nhánh đổi kênh của nháp — vào Tại quán dùng chung
cơ chế chọn bàn của Đơn mới (`TableForNewOrder`), chỉ chặn ở ngoại lệ hết toàn bộ bàn
trống. Không đổi SQL/procedure.

## Môi trường

- Harness WinForms thật (MainForm + SalesControl, hộp native qua `OrbitDialogHost`),
  DPI 125% (120), Windows NT 10.0.26200, .NET 10.0.14.
- DB test riêng `CoffeeManager_Test_G04_20261111_draftflush`, schema 31.
  Fixture: B0001 có đơn mở `HD20250411`; B0007/B0008 trống.
- Bản build test: `CafeManager.WinForms.dll` SHA-256
  `a1997c126d2fe7763c6dc871db4e940d71519a210c21714fc3a93c86a4cba4ae`
  (build Debug sang thư mục riêng `build-check/channel-table`, 1 warning MSB3277
  WebView2 có sẵn, 0 error). `bin\Debug` của repo đang bị Visual Studio/app giữ khóa
  nên không ghi đè; không đóng app của người dùng.
- Harness `channel-table-probe`, `ChannelTableProbe.dll` SHA-256
  `28f563845463dcb385730f1e6322d5731415f0c50e9278fd850ba529d96cd070`.

## Kết quả: 40/40 PASS

Bằng chứng JSON: [channel-table-ui-smoke.json](../runs/channel-table-20261116/channel-table-ui-smoke.json).

| Nhóm | Ca | Nội dung |
|---|---|---|
| E | E00–E02 | đúng DB test schema 31, DPI 120, fixture B0001 có 1 đơn mở |
| G1 | G1.0–G1.8 | rời Tại quán sang Mang đi: không hỏi nháp/bàn, giữ dòng + ghi chú, bỏ bàn, không ghi SQL; flush tạo đơn Mang đi MaBan NULL, ghi chú đúng; B0001 không đổi |
| G2 | G2.0–G2.4 | nháp Giao hàng có thông tin: Không giữ nguyên kênh/dữ liệu, Có xóa thông tin giao, giữ dòng; không ghi SQL |
| G3 | G3.1–G3.6 | Mang đi → Tại quán: chưa chọn bàn thì tự lấy bàn trống (khớp `free` B0007); bàn đang chọn bận bị bỏ qua và vẫn lấy bàn trống; status "Nháp đã chuyển sang Tại quán", panel còn dòng và thao tác được; không hỏi, không ghi SQL |
| G4 | G4.1–G4.6 | mô phỏng toàn bộ bàn bận bằng danh sách bàn trong bộ nhớ (không tạo đơn SQL): đổi kênh bị chặn — hộp "Hết bàn trống"/"Đã hiểu", kênh giữ Mang đi, nháp nguyên, status yêu cầu chọn phương thức khác, không ghi SQL; Đơn mới cũng mở cùng hộp và không ghi SQL; combo bàn xóa được ngoài hộp modal |
| G5 | G5.1–G5.7 | reset/discard không để lưới chết; Đơn mới hỏi Giữ/Không giữ/Ở lại, "Không giữ" tiếp tục đích sạch; Đơn mới trên bàn bận chọn bàn trống đầu tiên, không ghi SQL |
| G6 | G6.1–G6.2 | 52104 thật trên nháp Tại quán vẫn mở hộp 4 lựa chọn; "Bỏ nháp" sạch panel, không ghi SQL |
| G7 | G7.1–G7.2 | đơn đã lưu khóa kênh; tổng số đơn chỉ tăng 1 do flush Mang đi của G1 |

## SQL đối chiếu

- Hai lượt chạy harness tạo `HD26093409`, `HD26093411` (Mang đi, `MaBan` NULL,
  2 dòng, ghi chú "Ít đá" ở lượt đầu) — đúng nhánh flush Mang đi, không liên quan bàn.
- B0001 vẫn đúng 1 đơn mở; không có bàn nào bị chiếm thêm; nhánh chặn hết bàn và
  nhánh chọn bàn chỉ đổi nháp, không ghi SQL.

## Quan sát ngoài phạm vi

- Sau hộp "Hết bàn trống" của **Đơn mới**, combo bàn tự hiển thị lại dòng đầu
  (B0001) dù `NewOrder` đã đặt `SelectedIndex = -1`; xóa chọn thủ công sau đó vẫn
  nhận (`-1`). Hành vi này có từ trước, không do thay đổi đợt này (nhánh đổi kênh
  không xóa chọn bàn). Ghi nhận để xử lý riêng nếu cần; không sửa trong đợt này.

## NOT RUN

- Unit suite: DLL `bin\Debug` bị Visual Studio/app đang mở giữ khóa (không tắt app
  của người dùng).
- DPI 100%/150%, click tay/IME, hình thức giao diện: theo phân công người dùng tự kiểm.

## Kết luận

PASS chức năng cho thay đổi đổi kênh → Tại quán dùng cơ chế Đơn mới và chặn khi hết
bàn; regression Đơn mới/chọn bàn/bỏ nháp/52104/khóa kênh không hồi quy.
