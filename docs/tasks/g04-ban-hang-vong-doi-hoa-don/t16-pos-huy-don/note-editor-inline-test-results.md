# Biên bản test 16/11/2026 — ghi chú dòng món trên POS

Phạm vi: thay đổi thao tác ghi chú dòng món (click một lần vào tên món, hiệu ứng rê
chuột, editor nằm dưới nội dung dòng). Không kiểm lại nghiệp vụ SQL của POS; không
đổi checklist/ca nghiệm thu T14–T18 và không ghi PASS hình thức.

## Môi trường

| Mục | Giá trị |
|---|---|
| Ngày giờ | 16/11/2026, ~04:10–04:20 |
| Build dùng test | `CafeManager.WinForms.dll` Debug bản 16/11/2026 03:32/04:03 (trước khi có sửa FastReport song song), có chuỗi mới “Bấm vào tên món…”, `LineContentHeight` |
| Harness | `AppData/Local/CoffeeManager/build-check/note-ui/NoteEditorProbe.exe` (ngoài repo), host `OrderPanel` với DTO trong bộ nhớ, không kết nối SQL |
| DPI | 120 (125%), `DeviceDpi=120`; chưa chạy 100%/150% |
| Kích thước | Host 430×620 px (lưới 428×340) cho ca chuẩn; 430×420 (lưới 428×140) và 430×288 (lưới 428×8) cho ca biên |
| Bằng chứng | [note-editor-smoke.json](runs/note-editor-inline-20261116/note-editor-smoke.json) |

Harness dùng thông điệp chuột thật (`WM_LBUTTONDOWN/UP`, `WM_KEYDOWN`, `SetCursorPos`)
trên cửa sổ đang hiển thị, đọc control qua reflection, không chụp/đối chiếu hình ảnh.

## Kết quả

| Mã | Ca | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| A1 | Profile OrderLines | 4 cột `Name/Quantity/Price/Delete`, không có `Note/Amount` | 4 cột đúng | PASS |
| A2 | Chiều cao dòng không ghi chú | 46 logical → 58 px ở 125% | 58 | PASS |
| A3 | Chiều cao dòng có ghi chú | 64 logical → 80 px | 80 | PASS |
| A4 | Quy đổi DPI | khớp `Px(46)/Px(64)` | 58/80 | PASS |
| B1 | Rê chuột vào tên món | con trỏ tay | `Cursor=Hand` | PASS |
| B2 | Rê chuột vào tên món | ghi nhận đúng cột đang rê (0) | 0 | PASS |
| B3 | Rê chuột vào cột số lượng | con trỏ mặc định | `Default` | PASS |
| C1 | Click một lần tên món | editor mở | Visible | PASS |
| C2 | Dòng đang sửa | cao thêm 30 logical (96 px) | 96 | PASS |
| C3 | Vị trí editor | `grid.Top + row.Top + 58 + 2` | 228 = 228 | PASS |
| C4 | Không đè nội dung | editor.Top ≥ đáy nội dung | 228 ≥ 226 | PASS |
| C5 | Nằm trong lưới | đáy ≤ lưới − 2 | 260 ≤ 456 | PASS |
| C6 | Bề ngang | trong khoảng Name…Price | 3…364 (khung 1…366) | PASS |
| C7 | Focus | ô nhập được focus | true | PASS |
| C8 | Ô trống | chọn hết (rỗng) | rỗng, length 0 | PASS |
| D1 | Chuyển sang dòng khác | dòng cũ trả 58 | 58 | PASS |
| D2 | Dòng có ghi chú đang sửa | 80 + 30 → 118 | 118 | PASS |
| D3 | Nội dung editor | đúng ghi chú “Ít đá” | “Ít đá” | PASS |
| D4 | Chọn hết khi vào | chọn đủ 5 ký tự | 5 | PASS |
| E1 | Enter lưu | phát sự kiện lưu 1 lần | 1 | PASS |
| E2 | Trim trước lưu | “Không đường” | “Không đường” | PASS |
| E3 | Sau lưu | editor đóng | false | PASS |
| E4 | Sau lưu | dòng trở về 80 | 80 | PASS |
| F1 | Esc hủy | không phát sự kiện lưu | 1 (không tăng) | PASS |
| F2 | Esc hủy | editor đóng | false | PASS |
| F3 | Esc hủy | dòng trở về 58 | 58 | PASS |
| G1 | Bấm nút Lưu | lưu “Bấm nút lưu” | đúng | PASS |
| H0 | Mở editor rồi bấm số lượng | editor đang mở | true | PASS |
| H1 | `−` / `+` / giữa ô SL | giảm 1, tăng 1, giữa không đổi | 1/1 | PASS |
| H2 | Thùng rác | phát sự kiện xóa 1 lần | 1 | PASS |
| H3 | Bấm số lượng khi editor mở | editor đóng | false | PASS |
| I1 | `SetInteractive(false)` | click tên không mở | không mở | PASS |
| J1 | Lưới disabled | rê chuột trả con trỏ mặc định | Default | PASS |
| J2 | Lưới disabled | click không mở | không mở | PASS |
| K1 | `ShowOrder` khi editor mở | editor đóng | đóng | PASS |
| L1 | Panel rất thấp (lưới 8 px) | không tràn, ẩn editor | ẩn, không lỗi | PASS |
| L2 | Lưới 140 px, cuộn tới dòng cuối, mở ghi chú | editor hiện trong lưới (sau sửa) | editor hiện, top 226, đáy 258 = đáy lưới | PASS sau sửa |
| L3 | Lưới 340 px, dòng cuối | editor hiện trong lưới | hiện, đáy 398 < 458 | PASS |
| L4 | Lưới 140 px, dòng có ghi chú (cao 80) không đủ chỗ | editor đóng và dòng trả về 80 | đóng, 80 | PASS sau sửa |
| L5 | Ca L4 | không giữ focus trong editor ẩn | false | PASS |
| L6 | Đang mở editor rồi thu nhỏ lưới, bấm lại cùng dòng khi vẫn thiếu chỗ | editor đóng, dòng trả 58 | đóng, 58 | PASS |
| M1 | `DrawToBitmap` khi editor mở | painter không lỗi | OK | PASS |
| M2 | Vẽ lại khi đang hover | painter không lỗi | OK | PASS |

Tổng: 41 PASS, 0 FAIL sau sửa (gồm L6 thu nhỏ lưới khi đang mở). Trong lượt chạy đầu (trước sửa) L2 FAIL và C3 đo 228 do lề 2;
các số đo đó ghi ở mục “FAIL L2” dưới đây. File JSON hiện lưu lượt chạy sau sửa.

## FAIL L2 — chi tiết (lượt đầu, đã khắc phục)

- Tái hiện: host 430×420 px (lưới 428×140), 4 dòng, cuộn tới dòng cuối, bấm tên
  dòng cuối.
- Thực tế trước sửa: `BeginNoteEdit` nở dòng lên 96 px; `PositionNoteEditor` thấy
  editor không đủ chỗ (cần tới 142 px trong lưới 140 px) nên ẩn editor nhưng không
  trả chiều cao dòng → dòng cuối cao thêm 30 logical mà không có ô nhập.
- Ảnh hưởng: cửa sổ thấp hoặc đơn nhiều dòng, bấm tên món trông như không phản hồi.
- Sau duyệt16/11, sửa trong `OrderPanel`: bỏ lề dọc2 ở `PositionNoteEditor` (editor
  sát đáy lưới vẫn hợp lệ) và khi `BeginNoteEdit` không đặt được editor thì gọi
  `CancelNoteEdit` trả chiều cao thay vì để lại khoảng trống. Không đổi painter,
  model hay SQL. Regression: L2/L4/L5/L6 PASS, toàn bộ41 ca PASS.
- Giới hạn còn lại: nếu người dùng đang mở editor rồi thu nhỏ cửa sổ/panel tới mức
  không đủ chỗ, editor vẫn ẩn tạm và dòng giữ chiều cao cho tới khi có chỗ lại;
  không mất nội dung đã gõ. Chưa chạy click thật tay người dùng, IME, DPI100/150%.

