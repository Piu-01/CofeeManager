# Bug report — POS: thiếu hỏi "Lưu / Bỏ / Ở lại" khi rời màn hoặc chuyển đơn/bàn

Ngày lập: 23/11/2026. Trạng thái: **đã sửa 23/11/2026; probe kỹ thuật 13/13 PASS sau khi sửa focus**;
các ca GUI/SQL thật (report §8) chờ người dùng kiểm. Chi tiết ở `test-results.md` cùng thư mục.
Người dùng báo ngày 23/11/2026, chọn hướng khôi phục:
"Hỏi Lưu / Bỏ / Ở lại khi có input chưa ghi (gồm cả F3 của đơn mới chưa tạo)".

## 1. Hiện tượng

- Rời màn Bán hàng sang màn khác, hoặc mở đơn/bàn khác khi đơn hiện tại đang mở:
  không thấy hộp hỏi lưu/bỏ gì; chuyển thẳng.
- Người dùng cảm giác "logic hỏi lưu đơn hiện tại hay bỏ" trước đây bị mất.

## 2. Nguyên nhân

### 2.1. Cơ chế hỏi cũ đã bị gỡ có chủ đích ngày 19/11/2026

Theo yêu cầu trước đây của chính người dùng, nháp bộ nhớ POS và hộp
**Giữ / Không giữ / Ở lại** đã bị xóa khỏi app, quay về đúng contract T14
(món đầu tiên tạo hóa đơn trong một giao dịch, mọi thao tác sau autosave SQL):

- `docs/tasks/g04-ban-hang-vong-doi-hoa-don/t14-tao-sua-don/audit-bo-nhap-pos.md:3-12,26-30`
- `docs/UX_QuanLiQuanCafe.md:242-246` (mục 7.6), `:254-259`
- Code cũ (`SalesDraft`, `PosDraftDecisionDialog`, `FlushDraftAsync`) đã bị xóa.

Vì vậy bản thân "hộp hỏi 3 lựa chọn" không còn là regression code, mà là
hành vi đã được thay đổi theo quyết định trước.

### 2.2. Phạm vi input chưa ghi hiện quá hẹp

`src/CafeManager.WinForms/Modules/Sales/SalesControl.cs`:

- `:60` — `HasUnsavedChanges => headerDraft is not null || orderPanel.HasPendingNote;`
  Không tính thông tin F3 của **đơn mới chưa tạo** (nằm trong `context`).
- `:434-442` — `ConfirmInputAsync` chỉ mở hộp **OK/Cancel** "Bỏ input chưa ghi để tiếp tục?",
  không có nút Lưu, không có "Ở lại" rõ ràng, và tự `DiscardChanges()` khi OK.
- `:370-377` — `OpenOrderAsync` (mở đơn khác) gọi `ConfirmInputAsync`.
- `:528-553` — `OpenTableAsync` (chọn bàn khác) gọi `ConfirmInputAsync`.
- `:561-566` — `EditHeader` với đơn mới chỉ lưu vào `context`, không đặt `headerDraft`;
  nên chuyển bàn/đơn lúc này **mất im lặng** thông tin khách/người nhận/SĐT/địa chỉ/ship.
- `:644-651` — `SaveNote` ghi ghi chú dòng; `OrderPanel.HasPendingNote` (`OrderPanel.cs:27`)
  bật khi editor đang mở.

Hệ quả: chỉ có 2 trường hợp từng hiện hộp hỏi (ghi chú đang mở; header của đơn
**đã lưu** khi lần ghi trước lỗi). Các trường hợp người dùng thấy đều không hỏi:

1. Rời màn với đơn **đã lưu** đang mở → không hỏi (không mất dữ liệu vì đã autosave SQL).
2. Mở đơn/bàn khác khi đơn đã lưu đang mở → không hỏi (đơn cũ vẫn còn trong SQL).
3. F3 của **đơn mới chưa tạo** đã nhập thông tin → chuyển bàn/đơn mất thông tin, không hỏi.
4. Ghi chú dòng đang gõ dở → **có** hỏi, nhưng chỉ "Bỏ input chưa ghi?" OK/Cancel.

## 3. Cách sửa đề xuất

### 3.1. Mở rộng định nghĩa "input chưa ghi"

Bổ sung helper trong `SalesControl`:

```csharp
// Đơn mới chưa tạo: F3 đã nhập khách/người nhận/SĐT/địa chỉ/ship là input chưa ghi.
private bool HasMeaningfulNewOrderContext =>
    order is null && context is { } c &&
    (c.Customer is not null || c.Partner is not null || c.Recipient is not null ||
     c.Phone is not null || c.Address is not null || c.Ship != 0);
```

`HasUnsavedChanges => headerDraft is not null || orderPanel.HasPendingNote || HasMeaningfulNewOrderContext;`

Không tính bàn/kênh mặc định đã chọn cho đơn mới (giữ đúng UX §7.6: chọn bàn/kênh
không phải input).

### 3.2. Hộp thoại 3 lựa chọn dùng chung

Thêm `src/CafeManager.WinForms/Forms/OrbitSaveChangesDialog.cs` (kế thừa
`OrbitDialogForm`, theo chuẩn embedded-dialogs, owner modal, `ShowInTaskbar=false`,
clamp DPI, Esc/đóng = **Ở lại**), trả `enum SaveChangesChoice { Save, Discard, Stay }`.
Nút: **Lưu** (primary, không phải AcceptButton để tránh Enter tự chốt), **Bỏ thay đổi**,
**Ở lại**. Chuỗi qua `L.T` với key mới trong `vi.json`/`en.json`.

Thay `ConfirmInputAsync` bằng `ConfirmPendingInputAsync(owner, action)`:

| Trường hợp | Lưu | Bỏ | Ở lại |
|---|---|---|---|
| `headerDraft` (đơn đã lưu) | `await Write(h.Code, "SalesHeader", …)` như `EditHeader`; lỗi → ở lại, giữ input, báo lỗi | bỏ draft | hủy thao tác |
| ghi chú dòng đang mở | đọc note qua helper mới `OrderPanel.TryGetPendingNote(...)`, gọi `SaveNote(line, note)` chờ kết quả; lỗi → ở lại | `CancelNoteEdit()` | hủy thao tác |
| F3 đơn mới (chỉ khi **rời màn**) | giữ `context` (màn đã cache, không xóa) | xóa `context`, reset panel | hủy thao tác |
| F3 đơn mới (khi **chuyển đơn/bàn**) | không có nơi lưu vì chưa có bản ghi SQL → hộp chỉ có **Bỏ / Ở lại** kèm giải thích | xóa `context` | hủy thao tác |

Ghi/SQL đang chạy hoặc kết quả chưa rõ: giữ nguyên hành vi hiện tại
(`ConfirmInputAsync` trả false, `NavigationController` báo "Đang gửi…"), không mở hộp mới.

Khi thao tác Lưu thành công mới thực hiện tiếp việc rời màn/mở đơn; Lưu lỗi thì
dừng tại POS như quy tắc "timeout sau ghi: đối chiếu trước khi đi tiếp".

### 3.3. Cập nhật tài liệu (bắt buộc vì đổi hành vi UX §7.6)

- `docs/UX_QuanLiQuanCafe.md` §7.1 (`:149-153`) và §7.6 (`:254-259`): ghi lại hộp
  **Lưu / Bỏ / Ở lại**, phạm vi input chưa ghi gồm F3 đơn mới, và giới hạn
  "chuyển đơn khi chưa có đơn trong SQL chỉ có Bỏ/Ở lại".
- Ghi chú lịch sử: hộp 3 nút cũ (nháp bộ nhớ) đã bỏ 19/11; hộp mới chỉ áp cho
  input chưa ghi, đơn đã lưu vẫn autosave.

### 3.4. Việc không đề xuất (cần người dùng quyết riêng)

Với đơn **đã lưu** đang mở, rời màn/mở đơn khác **vẫn không hỏi** vì mọi thay đổi
đã ghi SQL, tài liệu hiện quy định "rời đơn lưu không hủy" (`UX:238`). Nếu người
dùng muốn thêm hộp nhắc "Đơn {mã} đang mở, vẫn rời?" mỗi lần rời, đó là quyết định
UX riêng và sẽ gây hỏi liên tục; chưa đưa vào phạm vi lần này.

## 4. Phạm vi ảnh hưởng

- `SalesControl.cs`: `HasUnsavedChanges`, `ConfirmInputAsync`/`ConfirmLeaveAsync`,
  các call ở `OpenOrderAsync`, `OpenTableAsync`, `NewOrder`, Đơn mới/Đổi kênh nếu
  dùng chung; có thể cả `HandleOccupiedTableAsync` (mở đơn hiện có thay cho nháp).
- `OrderPanel.cs`: thêm `TryGetPendingNote` (không đổi hành vi editor).
- File mới `OrbitSaveChangesDialog.cs`; key i18n mới vi/en.
- `NavigationController`/`MainForm` không phải sửa: vẫn gọi `IAsyncLeaveConfirmation`.
- Không đổi SQL, contract, schema.

## 5. Rủi ro

- Enter/AcceptButton: phải để không nút nào là AcceptButton (Enter không tự chốt)
  và focus mặc định vào **Ở lại**.
- Hộp mới xuất hiện khi đổi màn có thể chặn phím/click; phải modal owner và
  không xuyên phím xuống nền theo chuẩn embedded-dialogs.
- Lưu header/note là thao tác ghi thật: lỗi mạng/version phải ở lại POS và hiển thị
  lỗi, không rời nửa chừng.
- Đổi hành vi UX quen thuộc: cần test chức năng (không test hình thức theo phân công)
  cho các nhánh Lưu/Bỏ/Ở lại ở rời màn, mở đơn, chọn bàn, logout, đóng app.

## 6. Tối ưu (optional, không bắt buộc kèm fix)

- `DiscardChanges()` hiện xóa cả `context`; có thể tách rõ "bỏ input" và "reset ngữ
  cảnh" để tránh xóa nhầm bàn đang chọn khi chỉ cần bỏ ghi chú.
- Gộp wording "Bỏ input chưa ghi" hiện tại thành hộp mới, xóa chuỗi thừa.

## 7. Đã sửa (23/11/2026)

Người dùng duyệt báo cáo và hướng sửa. File thay đổi:

- `SalesControl.cs`: thêm `HasMeaningfulNewOrderContext` vào `HasUnsavedChanges`;
  `ConfirmInputAsync` thay bằng `ConfirmPendingInputAsync` (3 lựa chọn);
  `Write` và `SaveNote` trả `bool` để biết ghi thành công mới đi tiếp;
  thêm `SaveHeaderDraftAsync`; 4 điểm gọi (rời màn, mở đơn, chọn bàn, tạo đơn) dùng
  luồng mới. Rời màn cho phép giữ F3 đơn mới; chuyển đơn/bàn chỉ Bỏ/Ở lại.
- `OrderPanel.cs`: thêm `PendingNote()` (chỉ tính khi nội dung khác dòng).
- File mới `Forms/OrbitSaveChangesDialog.cs`: hộp Lưu/Bỏ thay đổi/Ở lại, X/Esc = Ở lại,
  không AcceptButton, focus mặc định nút an toàn.
- `en.json`: thêm chuỗi mới, xóa 2 chuỗi cũ không còn dùng.
- `docs/UX_QuanLiQuanCafe.md` §7.1, §7.6: cập nhật hành vi hỏi mới.

Build `CafeManager.WinForms` cách ly output: **Build succeeded, 0 error**.

## 8. Ca cần test chức năng (chờ duyệt đợt test)

1. Đơn đã lưu + sửa F3 → mở đơn/bàn khác: hộp 3 nút; Lưu ghi header rồi chuyển;
   Bỏ không ghi; Ở lại/X/Esc giữ nguyên màn.
2. Ghi chú dòng đang sửa → chuyển đơn/bàn/rời màn: Lưu ghi ghi chú rồi đi tiếp;
   Bỏ bỏ ghi chú; Ở lại giữ editor.
3. F3 đơn mới chưa tạo (Mang đi/Giao hàng có thông tin) → rời màn: Giữ thông tin /
   Bỏ thông tin / Ở lại; quay lại màn còn thông tin khi chọn Giữ.
4. F3 đơn mới → mở đơn/bàn khác: chỉ Bỏ thông tin / Ở lại (không có nút Lưu).
5. Lưu lỗi (chặn SQL/đổi revision): ở lại POS, giữ input, hiện lỗi; không chuyển màn.
6. Busy/unknown: không mở hộp 3 nút, giữ cảnh báo "Đang gửi…" như cũ.
7. Logout/đóng app khi có input chưa ghi: hỏi Lưu/Bỏ/Ở lại qua luồng điều hướng.
8. Enter không chốt; Esc = Ở lại; đơn đã lưu không có input thì rời/chuyển không hỏi.

Chưa chạy ca nào; chờ người dùng duyệt đợt test. Không test hình thức/DPI ngoài phân công.
