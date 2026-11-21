# Bug report — loyalty-tier-row-actions

Trạng thái: **Đã sửa BR-01, đã chạy regression R1–R8 ngày 21/11/2026 (7 PASS, R6 N/A), chờ người dùng nghiệm thu**
(đợt bug đã được duyệt ngày 21/11/2026). Kết quả chi tiết: `test-results.md` mục “Đợt regression BR-01”.

## BR-01 — Lỗi NullReferenceException không bắt được khi Lưu trong OperationEditorDialog (không tái hiện lần 2)

- Mức độ: cao (màn hình lỗi unhandled; người dùng phải Continue/Hủy; nếu bấm Quit sẽ mất phiên).
- Tần suất: 1/2 lần thử cùng thao tác trong đợt test 21/11/2026 (lần 1 lỗi, lần 2 thành công); chưa tái hiện sau khi lặp lại.

### Hiện tượng

Trong ca AR-02 (Thêm hạng SILV / 5000 / 4%): preview SQL thành công (“14 khách sẽ đổi hạng…”), bấm Lưu, hộp xác nhận hiện ra, bấm Đồng ý thì xuất hiện hộp:

```
Unhandled exception has occurred in your application...
Object reference not set to an instance of an object.

************** Exception Text **************
System.NullReferenceException: Object reference not set to an instance of an object.
   at CafeManager.WinForms.Forms.OperationEditorDialog.WriteAsync() ... OperationEditorDialog.cs:line 63
   at CafeManager.WinForms.Forms.OperationEditorDialog.Save_Click(Object sender, EventArgs e) ... line 55
   at System.Threading.Tasks.Task.<>c.<ThrowAsync>b__124_0(Object state)
```

Sau lỗi: dialog Thêm vẫn mở, danh sách phía sau không đổi; DB test KHÔNG có dòng hạng mới và KHÔNG có `G03OperationReceipt` `PolicyReplace`. Nghĩa là lỗi xảy ra trước khi procedure ghi chạy xong/được gọi.

### Bằng chứng

- `runs/20261121-ui/ar02-first-attempt-exception.txt` (nội dung Details đọc từ hộp lỗi).
- `runs/20261121-ui/db-final-state.txt`: receipt `PolicyReplace` chỉ có từ lần lưu thành công sau đó.
- `runs/20261121-ui/ar02-list-after-add.txt`: lần lặp lại sạch lưu thành công.

### Phân tích sơ bộ (có bằng chứng + giả thuyết)

- `OperationEditorDialog.WriteAsync` (dòng 60–73) không sửa trong task này; `PolicyList` chỉ cấu hình `CaptureWrite`/`Confirmation`.
- Vị trí NRE tại dòng 63 (`await pendingWrite!(requestId)` / đọc `result.Code`) với stack chỉ còn `WriteAsync` + `Save_Click`, cho thấy exception phát sinh ngay trong `WriteAsync`, không ở tầng SQL: `pendingWrite` là `null` tại thời điểm gọi, hoặc task/delegate trả về không hợp lệ.
- `Save_Click` chỉ bắt `ArgumentException`; mọi `NullReferenceException` từ `WriteAsync` thoát ra `async void` và hiện hộp lỗi WinForms.
- Giả thuyết cần kiểm chứng khi được duyệt sửa:
  1. Một luồng `Save_Click` trước đó bị hủy xác nhận đã đặt `pendingWrite = null` (dòng 54) xen vào lượt lưu hiện tại (double click/UIA invoke lặp hoặc hai lớp xác nhận). Lần lỗi có dấu hiệu thao tác test lặp trên cùng dialog.
  2. Hoặc `CaptureWrite` bị gọi lại sau khi dialog đã ở trạng thái khác, `pendingWrite` chưa được gán lại.
- Không đủ bằng chứng để khẳng định nguyên nhân gốc; không bịa log. Lần lặp lại sạch (một lần Lưu, một lần Đồng ý) không lỗi.

### Đề xuất sửa (đã áp dụng trong fix bên dưới)

1. Chặn vào `Save_Click`/`Reconcile_Click` ngay khi đang xác nhận (thêm cờ “confirming” hoặc kiểm tra `pendingWrite`/`writing` trước khi mở xác nhận), đảm bảo một lượt lưu duy nhất.
2. `Save_Click` bắt thêm lỗi không mong đợi và hiển thị ở `status` (giữ input), thay vì để `async void` đẩy ra hộp lỗi hệ thống; chỉ dùng ThreadException cho lỗi thật sự không xử lý được.
3. Trong `WriteAsync`, kiểm tra `pendingWrite`/kết quả null trước khi dùng, báo “Chưa cấu hình thao tác.” thay vì NRE.

### Phạm vi ảnh hưởng nếu sửa

- `src/CafeManager.WinForms/Forms/OperationEditorDialog.cs` dùng chung cho Thêm/Sửa/Xóa hạng, điều chỉnh điểm, khuyến mãi, danh mục… nên sửa cần regression các dialog editor khác.
- Không đổi contract SQL/DTO.

### Rủi ro

- Nếu nguyên nhân là thao tác test lặp (không xảy ra với người dùng), sửa chỉ mang tính phòng thủ; vẫn nên làm vì lỗi hiện hộp hệ thống và có thể làm mất phiên.
- Cần tái hiện có kiểm soát (log `pendingWrite`/số lần `Save_Click`) trước khi khẳng định fix.

## Fix đã áp dụng (21/11/2026)

- File: `src/CafeManager.WinForms/Forms/OperationEditorDialog.cs` (dialog dùng chung, không riêng màn hạng).
  1. Thêm cờ `confirming`: `Save_Click` bỏ qua khi đang mở hộp xác nhận/đang ghi, chặn lần bấm lặp làm một lượt khác đặt `pendingWrite = null`.
  2. `WriteAsync` nhận delegate ghi qua tham số thay vì đọc lại field `pendingWrite!` tại thời điểm await; kiểm tra kết quả null và báo “Thao tác không trả về kết quả.” thay vì NRE.
  3. Hủy xác nhận chỉ xóa `pendingWrite` nếu nó vẫn là delegate của chính lượt đó.
  4. `Save_Click`/`Reconcile_Click` bắt thêm lỗi không mong đợi và hiển thị ở `status`, không để `async void` đẩy hộp lỗi hệ thống.
- Không đổi contract SQL/DTO, không đổi luồng preview/revision/receipt.
- Build Debug: **0 error** (1 warning WebView2/WindowsBase có sẵn từ trước), output tạm `C:\Users\Lenovo\.qoder\tmp\cm-build-br01`.
- Đã chạy regression trên DB test ngày 21/11/2026: R1–R5, R7, R8 PASS; R6 N/A vì dialog Điều chỉnh điểm đã gỡ theo task customer-point-history. Chi tiết ở `test-results.md`.

## Regression đề xuất (chờ duyệt đợt test)

Trên DB test riêng, bản build `cm-build-br01`:

| Mã | Nội dung |
|---|---|
| BR01-R1 | Thêm hạng: preview → Lưu → Đồng ý → lưu thành công, receipt `PolicyReplace`, danh sách sắp xếp. |
| BR01-R2 | Bấm Lưu lặp nhanh (2 lần liền) ở dialog Thêm/Sửa: chỉ một lượt xác nhận/ghi, không hộp lỗi hệ thống, không ghi trùng. |
| BR01-R3 | Hủy xác nhận rồi Lưu lại: vẫn ghi được, không NRE, không báo “Chưa cấu hình thao tác.”. |
| BR01-R4 | Xóa hạng có khách: xác nhận → xóa, khách xếp lại hạng. |
| BR01-R5 | Lỗi nghiệp vụ (trùng mã/ngưỡng, mất quyền, xung đột revision) vẫn hiện đúng ở `status`, giữ input. |
| BR01-R6 | Điều chỉnh điểm khách (dialog cùng lớp): preview → Lưu → Đồng ý thành công; hủy xác nhận không ghi. |
| BR01-R7 | Editor khác cùng lớp không hồi quy: thêm/sửa khuyến mãi, một dialog danh mục (VD kho/nhân viên) lưu bình thường. |
| BR01-R8 | Nút Đóng/Hủy khi đang ghi hoặc chưa preview vẫn chặn đúng như trước. |
