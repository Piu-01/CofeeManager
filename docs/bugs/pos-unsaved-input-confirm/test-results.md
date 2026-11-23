# Test results — POS hỏi Lưu / Bỏ / Ở lại khi có input chưa ghi

- Ngày: 23/11/2026
- Môi trường: Windows, .NET SDK 10.0.401, DPI 125% (probe DeviceDpi theo hệ thống), app đang mở trong Visual Studio nên build này dùng output cách ly `D:/temp/cm-probe`, `D:/temp/cm-verify2`.
- Source: working tree sau triển khai `docs/bugs/pos-unsaved-input-confirm/report.md` (chưa commit).
- Phạm vi đợt này: **compile + probe kỹ thuật** cho logic input chưa ghi và hộp 3 lựa chọn.
  Các luồng GUI thật cần DB/phiên bàn ghi BLOCKED (agent không điều khiển GUI native WinForms).

## 1. Compile

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| SC-B01 | `dotnet build CafeManager.WinForms` (output cách ly vì `bin` bị app/VS khóa) | PASS — 0 error, 1 warning có sẵn | `runs/20261123/build.log` |

## 2. Probe kỹ thuật

Chạy `SaveChangesProbe.exe` (project ngoài repo `C:/Users/Lenovo/.qoder/tmp/cm-savechanges-probe/`,
ProjectReference tới `CafeManager.WinForms`). Probe không gọi SQL, không chụp hình, không kiểm hình thức.
Bằng chứng: `runs/20261123/probe.json`.

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| SC-P01 | Đơn mới chưa nhập gì (context null) | PASS — `EmptyContext=false` | probe.json |
| SC-P02 | Chỉ chọn bàn/kênh cho đơn mới không tính input chưa ghi | PASS — `ContextTableOnly=false` | probe.json |
| SC-P03 | F3 đơn mới có khách (`Customer`) tính là input chưa ghi | PASS — `ContextCustomer=true` | probe.json |
| SC-P04 | F3 đơn mới có người nhận/SĐT/địa chỉ/ship tính là input chưa ghi | PASS — `ContextDelivery=true` | probe.json |
| SC-P05 | Đơn đã lưu sạch (không sửa F3/ghi chú) không tính input chưa ghi | PASS — `SavedOrderClean=false` | probe.json |
| SC-P06 | headerDraft của đơn đã lưu tính là input chưa ghi | PASS — `SavedOrderHeaderDraft=true` | probe.json |
| SC-P07 | Ghi chú đang mở có nội dung khác dòng: `PendingNote` không null | PASS — `NoteChangedPending=true` | probe.json |
| SC-P08 | Ghi chú mở nhưng nội dung trùng dòng: `PendingNote` null (không có gì để lưu) | PASS — `NoteUnchangedPending=false` | probe.json |
| SC-P09 | Hộp 3 nút đúng thứ tự Lưu / Bỏ thay đổi / Ở lại, mapping OK / No / Cancel, Lưu là primary, `AcceptButton=null` | PASS | probe.json |
| SC-P10 | Click Bỏ thay đổi → `DialogResult=No`, hộp đóng | PASS — `DiscardResult=No`, `DiscardClosed=true` | probe.json |
| SC-P11 | Biến thể "Giữ thông tin / Bỏ thay đổi / Ở lại" cho F3 đơn mới khi rời màn; click Giữ → `OK` | PASS — `KeepText`, `KeepResult=OK` | probe.json |
| SC-P12 | Biến thể không có nút Lưu (chuyển đơn/bàn): chỉ Bỏ thay đổi / Ở lại; Esc → `Cancel` và đóng | PASS — `NoSaveButtons`, `EscapeClosed=true`, `EscapeResult=Cancel` | probe.json |
| SC-P13 | Focus mặc định là nút an toàn **Ở lại** (theo báo cáo §3.2/§5) | FAIL lượt 1 → **PASS sau fix 23/11** — `DefaultFocusText="Ở lại"`, `DefaultFocusStay=true` | probe.json |

### Ghi chú SC-P13 (đã sửa sau duyệt)

Lượt test đầu: `OrbitSaveChangesDialog` đặt `focusTarget = primary ?? stay` nên focus rơi
vào **Lưu**, Enter sẽ lưu ngay. Người dùng duyệt sửa, code đổi thành luôn focus `stay`;
probe chạy lại: `TechnicalSuccess=true`, toàn bộ SC-P01–SC-P13 PASS.

### Ghi chú SC-P07/SC-P08

`HasUnsavedChanges` vẫn dùng `orderPanel.HasPendingNote` (editor đang mở) nên trả `true`
cả khi nội dung ghi chú trùng dòng (xem `NoteUnchanged=true`). Đây là định nghĩa cũ,
không gây hộp thoại thừa vì `ConfirmPendingInputAsync` trả `true` ngay khi
`PendingNote` null và không có F3 đơn mới; chỉ giữ nút Thanh toán bị khóa trong lúc
editor mở như trước. Không sửa trong đợt này (ngoài phạm vi, ghi nhận optional).

## 3. Ca chức năng chưa chạy (BLOCKED)

Cần app WinForms thật + SQL Server test + phiên bàn; agent không điều khiển GUI native:

| Mã | Ca (từ report §8) | Trạng thái | Lý do |
|---|---|---|---|
| SC-F01 | Đơn đã lưu + F3 sửa → mở đơn/bàn khác: Lưu ghi rồi chuyển; Bỏ không ghi; Ở lại giữ | NOT RUN | Cần DB test + thao tác GUI |
| SC-F02 | Ghi chú dở → chuyển/rời: Lưu ghi chú rồi đi tiếp | NOT RUN | Cần DB test + GUI |
| SC-F03 | F3 đơn mới → rời màn: Giữ/Bỏ/Ở lại, quay lại còn thông tin khi Giữ | NOT RUN | Cần GUI thật |
| SC-F04 | F3 đơn mới → mở đơn/bàn khác: chỉ Bỏ/Ở lại | NOT RUN | Cần GUI thật |
| SC-F05 | Lưu lỗi (chặn SQL/đổi revision): ở lại POS, giữ input, báo lỗi | NOT RUN | Cần DB test |
| SC-F06 | Busy/unknown: không mở hộp 3 nút, giữ cảnh báo | NOT RUN | Cần GUI + thao tác ghi |
| SC-F07 | Logout/đóng app khi có input chưa ghi: hỏi Lưu/Bỏ/Ở lại | NOT RUN | Cần GUI thật |
| SC-F08 | Enter không chốt, Esc = Ở lại, đơn lưu sạch không hỏi | NOT RUN | Cần GUI thật |

Không test hình thức/DPI 100%/150% theo phân công; chỉ ghi kỹ thuật DPI hiện tại của probe.
