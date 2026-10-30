# PC04 — Lỗi đọc dữ liệu in sau commit

Đợt test hỏi in/reset đã duyệt; source sản phẩm giữ nguyên. DB riêng
CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout, schema016,
marker20261025_181739_98e5df59. Đơn HD20250420, phải trả81.000 ₫.

| Ca | Expected / Actual | Kết quả |
|---|---|---|
| Chốt rồi lỗi đọc dữ liệu in | DENY EXECUTE đúng dbo.sp_InvoiceDetail cho G04Gui; người dùng chốt với khách đưa100000 và chọn Có in. Xác nhận POS trống, thông báo Đã thanh toán và Chưa in được hóa đơn | PASS |
| Tiền sau lỗi | Một payment PT00181000, hóa đơn Da thanh toan81000 | PASS |
| Kho sau lỗi | NVL01 giảm0,018; NVL03 giảm0,010, khớp TIEUHAO_HOADON | PASS |
| Khôi phục fixture | fixture.ps1 -Mode Restore gỡ đúng direct DENY; journal Restored; HAS_PERMS_BY_NAME dưới G04Gui=1 | PASS |

Không thay role ADMIN/CASH/STORE, source hoặc migration. Không tác động DB sử dụng
thật. Đơn vãng lai nên không suy diễn kết quả điểm khách hàng. Đây là lỗi đọc dữ
liệu sau commit, chưa phải lỗi máy in/spooler vật lý. Không kiểm hình thức.

Bằng chứng: before.json, after.json, verdict.json, fault-journal.json,
restored-execute-permission.txt và phản hồi người dùng. Quyền đã khôi phục trước
khi chuyển sang kiểm in lại HD20250421.
