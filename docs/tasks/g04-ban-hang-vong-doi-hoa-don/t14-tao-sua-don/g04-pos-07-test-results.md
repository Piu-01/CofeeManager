# G04-POS-07 — Biên bản đợt test đổi kênh / bỏ nháp (16/11/2026)

Phạm vi: đợt test được duyệt cho phương án A của G04-POS-07 — đổi kênh trên nháp
tại chỗ, bỏ nháp làm sạch panel, giữ dòng món/ghi chú, SQL chỉ ghi khi Giữ/Thanh toán.
Source app không bị sửa trong lượt test; chỉ harness ngoài repo được chỉnh.

## 1. Môi trường

| Mục | Giá trị |
|---|---|
| DB | `CoffeeManager_Test_G04_20261111_draftflush` (DB test riêng, không dùng DB thật) |
| Schema | 31 (`MAX(Version)` của `SchemaMigrations`) |
| DPI | 120 (125%), `DeviceDpi` của SalesControl |
| OS / runtime | Windows NT 10.0.26200.0, .NET 10.0.14 |
| Build app | Debug `src/CafeManager.WinForms` → `C:\Users\Lenovo\AppData\Local\CoffeeManager\build-check\channel-fix` (SHA-256 `CafeManager.WinForms.dll` `0d6422cb…c89bd1`) |
| Tài khoản | MaTK2 · NV004 · thungan · CASH, stamp `0x…0863` (phiên dựng thủ công) |
| Fixture | B0001 có 1 đơn mở `HD20250411`; bàn trống B0007/B0008; SP001 25.000, SP002 30.000 |
| Harness | `...\build-check\channel-probe` (ChannelProbe), `...\build-check\note-ui-test` (NoteEditorProbe) |
| Bằng chứng | `docs/tasks/g04-ban-hang-vong-doi-hoa-don/runs/channel-switch-20261116/` |

## 2. Cách chạy

- Đổi kênh/bỏ nháp: `dotnet ChannelProbe.dll <thư mục bằng chứng>` — MainForm + SalesControl
  thật, mở hộp native qua `OrbitDialogHost`, timer trong vòng lặp modal ghi lại tiêu đề/nút
  rồi bấm nút theo kịch bản; kiểm SQL trực tiếp trên DB test.
- Ghi chú/hộp gợi ý: chạy `NoteEditorProbe.exe <file json>` (apphost). Chạy bằng
  `dotnet NoteEditorProbe.dll` trong phiên này không nhận click tổng hợp và FAIL 26 ca,
  nhưng **cùng hiện tượng xảy ra với build cũ `note-ui`** → lỗi môi trường/host, không
  phải hồi quy của G04-POS-07; apphost cho 53/53 PASS nên dùng kết quả apphost.

## 3. Kết quả đổi kênh / bỏ nháp / SQL — 32/32 PASS

| Mã | Nội dung | Kết quả |
|---|---|---|
| E00 | Đúng DB test, schema ≥ 31 | PASS |
| E01 | DPI 125% (120) | PASS |
| E02 | B0001 có đúng 1 đơn mở như bug report | PASS |
| G1.0 | Nháp Tại quán trên B0001: 2 dòng + ghi chú "Ít đá" | PASS |
| G1.1 | Chuyển Mang đi không mở hộp "Giữ đơn nháp?"/"Bàn đã có đơn" | PASS |
| G1.2 | Giữ 2 dòng + ghi chú, kênh Mang đi, bàn đã bỏ | PASS |
| G1.3 | Combo bàn trống, status "Không dùng bàn · Mang đi" | PASS |
| G1.4 | Lưới vẽ lại 2 dòng, panel còn thao tác được | PASS |
| G1.5 | Status nêu "chưa ghi SQL" | PASS |
| G1.6 | Đổi kênh không ghi SQL (số đơn không đổi) | PASS |
| G1.7 | F8 lưu đơn Mang đi, `MaBan` NULL, 2 dòng | PASS |
| G1.8 | SQL: `LoaiDon='Mang di'`, `MaBan NULL`, 2 dòng, ghi chú giữ đúng | PASS |
| G1.9 | Đơn mở của B0001 không bị đụng | PASS |
| G2.0 | Nháp Giao hàng có người nhận/SĐT/địa chỉ/ship | PASS |
| G2.1 | Đổi Mang đi mở hộp "Đổi loại đơn" với Có/Không | PASS |
| G2.2 | Chọn Không: giữ Giao hàng + thông tin giao, giữ dòng | PASS |
| G2.3 | Chọn Có: sang Mang đi, xóa người nhận/SĐT/địa chỉ/ship, giữ dòng | PASS |
| G2.4 | Đổi kênh không ghi SQL | PASS |
| G3.1 | Mang đi → Tại quán gắn bàn trống đang chọn, giữ dòng | PASS |
| G3.2 | Bàn bận bị từ chối: bỏ chọn bàn, status nhắc "chọn bàn" | PASS |
| G3.3 | Lưới vẫn còn dòng, panel còn thao tác | PASS |
| G3.4 | Giữ khi chưa có bàn bị chặn (Normalize), nháp còn nguyên | PASS |
| G5.1 | ResetDraft: lưới 0 dòng, "Chưa tạo đơn", tổng "—", khóa thao tác | PASS |
| G5.2 | `DiscardChanges` + tải lại: không còn lưới cũ, không còn nháp | PASS |
| G5.3 | "Đơn mới" hỏi Giữ/Không giữ/Ở lại | PASS |
| G5.4 | Chọn Không giữ: panel sạch, tiếp tục chọn bàn trống | PASS |
| G5.5 | Bỏ nháp không ghi SQL | PASS |
| G5.6 | Thêm món lại sau bỏ nháp hoạt động bình thường | PASS |
| G6.1 | 52104 thật trên nháp Tại quán vẫn mở hộp 4 lựa chọn | PASS |
| G6.2 | Chọn "Bỏ nháp": sạch panel, không ghi SQL | PASS |
| G7.1 | Mở đơn đã lưu `HD20250411`: kênh bị khóa | PASS |
| G7.2 | Tổng số đơn cuối = đầu + 1 (chỉ do F8 ở G1) | PASS |

Máy đọc: `channel-switch-ui-smoke.json` (Total 32, Failed 0, 07:11:02–07:11:10).

### Dữ liệu sinh ra trên DB test

Mỗi lượt chạy G1 tạo 1 đơn Mang đi (không bàn). Ba lượt chạy trong đợt để lại:

| MaHD | Thời điểm | LoaiDon | MaBan | Dòng | Ghi chú |
|---|---|---|---|---|---|
| HD26093404 | 16/11 07:05 | Mang di | NULL | SP001×1, SP002×1 | "Ít đá" ở SP001 |
| HD26093405 | 16/11 07:10 | Mang di | NULL | SP001×1, SP002×1 | "Ít đá" ở SP001 |
| HD26093407 | 16/11 07:11 | Mang di | NULL | SP001×1, SP002×1 | "Ít đá" ở SP001 |

Ghi chú hiển thị lệch dấu trong `sqlcmd` do codepage console; harness so sánh chuỗi
"Ít đá" và PASS. Các ca G2/G3/G5/G6 không tạo đơn; tổng đơn cuối chỉ tăng 1 sau G1.

## 4. Regression ghi chú / hộp gợi ý — 53/53 PASS

`NoteEditorProbe.exe` trên cùng bộ DLL `channel-fix`: 53/53 check PASS, 0 failure,
DPI 120. Bao gồm A1–A4, B1–B3, C1–C8, D1–D4, E1–E3, F1–F3, G1–G2, H0–H3, K1–K2,
L2–L6, N1–N10. Bằng chứng: `note-editor-regression.json`.

## 5. Build và phần chưa chạy

- Build `CafeManager.WinForms` Debug (dùng cho harness) và Release sang thư mục tạm:
  **0 error**, còn 1 warning MSB3277 (WindowsBase/WebView2) có sẵn từ trước.
- **NOT RUN** — unit suite `tests/CafeManager.Tests`: app `CafeManager.WinForms` đang
  được mở từ Visual Studio giữ khóa DLL trong `bin\Debug`, build test không copy được
  file. Thay đổi của G04-POS-07 không đụng Business/Models logic (chỉ handler UI và
  chuỗi hiển thị), nên để lượt test riêng khi đóng app.
- **NOT RUN** — DPI 100%/150%, click tay/IME trên GUI thật, hình thức giao diện
  (người dùng tự kiểm theo phân công).
- Giới hạn đã biết: harness chỉ kiểm nháp → Giữ → SQL; chưa chạy thanh toán đầy đủ,
  F3 sửa thông tin giao hàng, hai phiên đồng thời ngoài 52104 ở G6.

## 6. Kết luận

Đúng phạm vi đã duyệt: đổi kênh trên nháp không hỏi và không ghi SQL, bàn được bỏ
khi rời Tại quán, thông tin giao hàng chỉ xóa sau xác nhận, bỏ nháp không để lại lưới
chết, hộp "Bàn đã có đơn" không còn xuất hiện sai. Ghi chú dòng món và lưới đơn giữ
nguyên hành vi (53/53). Các phần NOT RUN nêu trên cần lượt riêng.
