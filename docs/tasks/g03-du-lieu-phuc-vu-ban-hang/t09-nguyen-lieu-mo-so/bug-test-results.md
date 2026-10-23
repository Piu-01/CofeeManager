# Regression G03-B01 — 18/10/2026

Đã được duyệt cùng đợt debug. Môi trường Windows/PowerShell7, SDK10.0.401, SQL Server localhost thật; baseline clean/demo→009. Không cần build WinForms Debug/Release hoặc thao tác GUI cho sửa runner này. Source runner cố định trong lượt test, không vá giữa các ca. [Script/lệnh và cách tạo fixture](../runs/g03-b01-regression/Run-Regression.ps1).

Lệnh: `pwsh -NoProfile -File docs/tasks/g03-du-lieu-phuc-vu-ban-hang/runs/g03-b01-regression/Run-Regression.ps1`. Nhánh PASS gọi runner gốc; hai nhánh lỗi gọi bản sao riêng của cùng runner, tiêm THROW52991 vào cuối fixture T09 hoặc thêm một xUnit Fact luôn fail thuộc namespace G03 trước khi bắt đầu lượt test. Không sửa test sản phẩm. Bản sao fixture/source giữ để tái kiểm, chỉ dọn bin/obj của bản sao sau test; chạy lại cần thư mục evidence mới.

| Ca | Expected | Actual | Kết quả |
|---|---|---|---|
| B01-R01 bình thường | 5 SQL PASS; unit20 PASS, TRX; runner Succeeded/code0; dọn2DB | Đúng; unit20/20; source unchanged | PASS |
| B01-R02 SQL lỗi chủ động | T09 FAIL có thông điệp52991, 4SQL khác tiếp tục; unit vẫn chạy; runner Failed/code1; dọn2DB | Đúng; SqlFailed1, unit20/20, lưu lỗi và TRX | PASS |
| B01-R03 unit lỗi chủ động | 5SQL PASS; unit20PASS/1FAIL, TRX/exitcode khác0; runner Failed/code1; dọn2DB | Đúng; unit exit1, runner exit1 | PASS |

Evidence/manifest chính xác từng ca: [results.json](../runs/g03-b01-regression/results.json). Log runner cấp tiến trình: [pass](../runs/g03-b01-regression/pass.log), [SQL lỗi](../runs/g03-b01-regression/sql-fail.log), [unit lỗi](../runs/g03-b01-regression/unit-fail.log). TRX/log unit và SQL từng task nằm cạnh manifest được chỉ trong results.json.

Tài nguyên: **6 tạo/6 dọn/0 giữ lại**, đã đối chiếu sys.databases [audit](../runs/g03-b01-regression/resource-audit.json). [Runner không đổi trong lượt test](../runs/g03-b01-regression/summary.json); [139 file sản phẩm/SQL/test gốc không đổi](../runs/g03-b01-regression/product-audit.json). Chỉ runner thay đổi trong pha sửa trước regression; không commit/push.

Không thay kết quả 47 ID gốc:20PASS/27BLOCKED. GUI/DPI/Designer và tích hợp G04 đích chưa có kết quả mới. Trạng thái nhóm vẫn Đang làm; không suy nghiệm thu toàn bộ từ runner Succeeded.
