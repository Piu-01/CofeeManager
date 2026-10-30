# G05-B04 — Đối chiếu nguồn tài chính trong script nâng DB bị cắt JSON

27/10/2026. **Chờ duyệt đợt bug; chưa sửa tool.** T19 quản lý helper nguồn chi; lỗi này ở công cụ nâng development, không phải procedure ghi tiền.

`tools/database/Update-DevelopmentG05.ps1` dùng Read-TestScalar/ExecuteScalar cho `$moneySql` có `FOR JSON PATH` ở cấp ngoài cùng. SQL Server có thể chia JSON thành nhiều hàng; ExecuteScalar chỉ đọc ô đầu. `BeforeMoneySources`/`AfterMoneySources` trong manifest thành công kết thúc giữa giá trị chuỗi, nên phép so sánh này chưa đủ bao phủ toàn bộ PN/nợ/lịch sử chi. Không được suy bảo toàn đầy đủ chỉ từ dòng Status=Succeeded của script.

Đã khắc phục **khoảng trống bằng chứng**, chưa sửa source tool: khôi phục backup COPY_ONLY trước nâng thành DB test riêng, so sánh SHA256 hoàn chỉnh phía SQL của Inventory, Points, Accounts, Invoices, Finance, Purchases, Debts, Payments. **8/8 khớp**; bản sao đã dọn. Bằng chứng `../runs/acceptance-full-20261027_110730/development-backup-verification.json` và `VerifyDevelopmentBackup.ps1`. Development chỉ đọc trong bước này; backup gốc được giữ nguyên. Không phát hiện mất tiền/kho/điểm/tài khoản.

Đề xuất sửa script bằng hash đầy đủ phía SQL hoặc đọc và ghép mọi hàng JSON; ưu tiên server-side hash và số dòng, không ghi payload tài khoản vào log. Không thay migration017–021 và không cần nâng development lần nữa để sửa lỗi đo kiểm này.

Regression trên DB test: nguồn dữ liệu đủ lớn vượt một chunk JSON; thay dòng ở cuối tập phải làm phép so sánh khác nhau; tập không đổi phải khớp. Kiểm kho/điểm/hóa đơn/tài khoản và chính sách giữ backup. Rủi ro chính của lỗi hiện tại là false-negative khi dữ liệu ngoài chunk đầu đổi.

Lỗi UI của T19 dùng chung [G05-B01](../t20-nhap-kho/bug-report.md); không tạo bug trùng.
