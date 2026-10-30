# G05-B06 — Grid xuất có cùng cấu hình ReadOnly toàn bảng

29/10/2026, người dùng đã duyệt B06 kèm regression; đã sửa hai editor. Chủ sở hữu điều tra ở [T20](../t20-nhap-kho/bug-report.md#g05-b06--grid-nhậpxuất-đang-khóa-ô-cần-nhập--đã-sửa2910).

StockIssueEditorDialog dùng OrbitDataGridView mặc định ReadOnly=true, chỉ đặt ReadOnly=false cho cột Quantity mà không mở grid cấp trên. Ảnh hưởng T21 được xác định qua source; hiện tượng không mở editor đã tái hiện native ở T20, chưa chạy native ghi T21 trong lượt này. Không gắn thêm FAIL nghiệm thu T21 từ source-only.

Đề xuất mở chỉnh sửa riêng grid xuất, chỉ Quantity; giữ cột tham chiếu khóa và default OrbitDataGridView nguyên trạng. Sửa/regression cùng đợt B06: nhập số, validation, bỏ nháp, chốt xuất và đối chiếu tồn/receipt/quyền. Không sửa source trong lượt test B05.

## Sau sửa29/10

Mở grid xuất ReadOnly=false/EditOnEnter, giữ mã/tên/đơn vị readonly. Native đã xác nhận lượng0 bị chặn,9999 vượt tồn rollback/giữ input, sửa1 chốt PX20250506 một lần; bỏ nháp sửa3 không ghi. Component ADMIN/STORE binding/selection/khóa unknown và unit/SQL subset PASS. Native Tab chưa đủ bằng chứng; STORE/trả hàng native chưa chạy trong lượt này. [Biên bản](../runs/b06-20261029/test-results.md); không đổi bảng10ID lịch sử.

