# Biên bản test 16/11/2026 — gợi ý ghi chú dòng món (POS)

Phạm vi: bỏ icon bút chì khi rê chuột; bấm tên **hoặc** dòng ghi chú mở editor;
dropdown gợi ý gồm mục cố định của quán và lịch sử đọc từ SQL; ↑/↓ chọn, Enter nhận
và lưu, Esc đóng danh sách rồi mới hủy. Chạy đợt test người dùng duyệt16/11/2026;
không sửa source trong lượt test; không kiểm hình thức/100%/150% (người dùng tự kiểm).

## Môi trường

| Mục | Giá trị |
|---|---|
| Ngày giờ | 16/11/2026, ~06:40–07:10 |
| Build dùng test | `CafeManager.WinForms.dll` Debug 16/11/2026 (sau thay đổi), có `NoteSuggestionPanel`, `SetNoteHistory`; Release build cùng ngày0 error |
| SQL | SQL Server `localhost`, DB test riêng `CoffeeManager_Test_G04_20261111_draftflush`, schema31 (đã áp `031_sales_note_suggestions.sql`), actor NV001 (ADMIN/CASH) và NV010 (STORE) |
| Harness UI | `AppData/Local/CoffeeManager/build-check/note-ui-test` (ngoài repo), host `OrderPanel` + DTO trong bộ nhớ, không kết nối SQL |
| Harness SQL | `AppData/Local/CoffeeManager/build-check/note-sql-test` (ngoài repo), gọi `SalesService.NoteSuggestionsAsync` qua DAL thật |
| DPI | 120 (125%), `DeviceDpi=120`; chưa chạy100%/150% |
| Kích thước | Host 430×620 (lưới 428×340), 430×420 (lưới 428×140), 430×288 (lưới thấp) |
| Bằng chứng | [note-suggestions-ui-smoke.json](runs/note-suggestions-20261116/note-suggestions-ui-smoke.json), [note-suggestions-sql-smoke.json](runs/note-suggestions-20261116/note-suggestions-sql-smoke.json) |

Harness UI dùng thông điệp chuột/bàn phím thật (`WM_LBUTTONDOWN/UP`, `WM_MOUSEMOVE`,
`WM_KEYDOWN`) trên cửa sổ hiển thị, đọc control qua reflection; không chụp/đối chiếu
hình ảnh. Mỗi ca bấm chuột mở editor được lặp tối đa3 lần nếu cửa sổ mất tiêu điểm;
lượt ghi bằng chứng dưới đây dùng lần1 cho mọi ca (`Attempts-*=1`). Chạy lại5 lượt
sau khi thêm `host.Activate()`: 5/5 PASS. Trước đó, các lượt bị cửa sổ khác cướp tiêu
điểm làm mất lượt bấm đầu và gây FAIL dây chuyền; cùng source, cùng ca — không phải
lỗi ứng dụng, đã ghi ở mục “Nhiễu harness”.

## Kết quả

### Nhóm hồi quy cũ — 41 ca

| Nhóm | Nội dung | Số ca | Kết quả |
|---|---|---|---|
| A | Profile cột OrderLines, chiều cao dòng46/64 theo DPI | 4 | PASS |
| B | Hover đổi chữ/con trỏ tay, không còn icon | 3 | PASS |
| C | Click một lần mở editor, vị trí dưới nội dung, focus, chọn hết | 8 | PASS |
| D | Đổi dòng, hiện ghi chú, chọn hết nội dung | 4 | PASS |
| E | Enter lưu, trim, đóng, trả chiều cao | 4 | PASS |
| F | Esc hủy không lưu | 3 | PASS |
| G | Nút Lưu bằng chuột | 1 | PASS |
| H | Vùng −/+/giữa, thùng rác, đóng editor | 4 | PASS |
| I | Không tương tác thì không mở | 1 | PASS |
| J | Lưới disabled: con trỏ mặc định, không mở | 2 | PASS |
| K | Đổi đơn đóng editor | 1 | PASS |
| L | Panel thấp/vừa/cao, không đủ chỗ, thu nhỏ khi mở | 6 | PASS |
| M | Vẽ panel khi editor/dropdown mở (`DrawToBitmap`) | 2 | PASS |

### Nhóm gợi ý mới — 12 ca

| Mã | Ca | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| N1 | Gõ `kh` | dropdown hiện | Visible | PASS |
| N2 | Lọc theo nội dung | 1–6 mục, mọi mục chứa `kh` | 4 mục: Không đá/Không đường/Không sữa/Không ống hút | PASS |
| N3 | Ưu tiên mục cố định | mục đầu bắt đầu bằng “Không” | Không đá | PASS |
| N4 | Vị trí dropdown | dưới editor, trong panel | top340 ≥ editorBottom338, bottom442 ≤ 620 | PASS |
| N5 | Bề ngang | trong panel | 3…364 (rộng430) | PASS |
| N6 | Lịch sử do UI cấp | có “Ghi chú cũ” | có | PASS |
| N7 | ↓/↑ rồi Enter | nhận mục, lưu, đóng editor+dropdown | lưu “Không đá”, đóng cả hai | PASS |
| N8 | Esc khi dropdown mở | chỉ đóng dropdown | dropdown đóng, editor còn mở | PASS |
| N9 | Esc lần hai | hủy editor, không lưu | đóng, không tăng save | PASS |
| N10 | Click nửa dưới ô tên (dòng ghi chú) | mở editor với ghi chú | Visible, text “Ít đá”; hit-test `Cell` cột0/dòng1 | PASS |
| N11 | Mở editor | không tự bung dropdown | false | PASS |
| N12 | Lưới vừa 428×140, dòng cuối | dropdown trong panel, không đè editor | 3,260–364,362; editor đáy258 | PASS |

### Nhóm SQL/DAL/BLL — 6 ca

| Mã | Ca | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| Q1 | ADMIN đọc lịch sử | 5 ghi chú, đầu tiên “It duong” | đúng | PASS |
| Q2 | Lọc `da` | 3 mục It da/Khong da/Nhieu da | đúng | PASS |
| Q3 | Lọc `%` | ký tự đại diện được escape, 0 dòng | 0 | PASS |
| Q4 | Tìm có khoảng trắng `  It  ` | BLL trim, trả mục chứa “It” | It duong, It da | PASS |
| Q5 | STORE gọi dịch vụ | bị từ chối quyền | `UnauthorizedAccessException`: “Bạn không có quyền thao tác này.” | PASS |
| Q6 | Phiên CASH sau khi bị từ chối ở phiên khác | phiên vẫn hợp lệ | `IsRevoked=false` | PASS |

Tổng: **53/53 ca UI PASS, 6/6 ca SQL/DAL/BLL PASS**, 0 FAIL, tại DPI125%.

## Nhiễu harness (không phải bug ứng dụng)

Trong các lượt giữa, một số lượt báo FAIL ở ca bấm chuột đầu tiên sau khi cửa sổ
probe mất tiêu điểm (ví dụ N10 rồi E/G/H/I/J dây chuyền, hoặc K/L2/L3/L6). Cùng
source, cùng ca, chạy lại thì PASS và ghi nhận `mouse-down/cell-click` đúng. Nguyên
nhân: cửa sổ harness luôn TopMost nhưng bị cửa sổ đang dùng cướp tiêu điểm; lượt
bấm đầu chỉ kích hoạt cửa sổ nên bị nuốt. Đã thêm `host.Activate()` trước mỗi lượt
bấm và chạy lại5 lượt liên tiếp: 5/5 PASS, mọi ca dùng lần bấm đầu. Không sửa source
ứng dụng trong lúc test.

## NOT RUN / giới hạn

- Chưa chạy app đăng nhập thật rồi thao tác POS end-to-end; UI được test trên
  `OrderPanel` thật với DTO trong bộ nhớ, phần SQL được test qua `SalesService`
  thật trên DB test riêng.
- Chưa chạy100%/150%; hình thức dropdown, màu/viền, hover do người dùng tự kiểm
  theo phân công.
- Chưa kiểm IME/tiếng Việt gõ dấu trong ô ghi chú; dropdown lọc theo
  `CurrentCulture` chứ không bỏ dấu.
