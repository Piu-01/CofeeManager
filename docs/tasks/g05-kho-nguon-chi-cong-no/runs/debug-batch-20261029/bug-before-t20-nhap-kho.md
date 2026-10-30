# G05-B01 — Không tiếp cận được thao tác trong workspace mới

27/10/2026. **Chờ duyệt đợt bug; chưa sửa.** Chủ sở hữu T20, hồi quy T19/T21/T22/T23.

## Hiện tượng và tái hiện

Trong phiên WinForms Debug thật, đăng nhập thường bằng ADMIN fixture NV991, vào Kho → Nhập hàng. Danh sách 32 phiếu tải được nhưng không có nút **Lập phiếu nhập**, **Tìm / Tải lại**, **Xem chi tiết** trong accessibility tree. Đổi kích thước cửa sổ vẫn không có thao tác. Tab Xuất kho cũng thiếu nút lập phiếu; Thu chi thiếu bộ lọc ngày/loại/nguồn và nhóm thao tác. Đây là lỗi tiếp cận chức năng, không phải đánh giá màu/font/spacing.

Bằng chứng: `../runs/acceptance-full-20261027_110730/gui-inventory.txt`, `gui-issue.txt`, `gui-finance-after.txt`; trạng thái sau chuyển tab và sau resize được ghi trong tool output. Không lấy screenshot làm nghiệm thu hình thức. Cửa sổ được kiểm ở kích thước thường và maximize; DeviceDpi chưa đo được, không suy PASS125%.

## Điều tra và phạm vi sửa

Các control mới thêm nút vào `OrbitListWorkspace.PrimaryActions`, và bọc nhóm thao tác/filter bằng `FlowLayoutPanel Dock=Fill`. `OrbitListWorkspace.OnLayout/ArrangeButtons` tự đo theo `Visible`, Width và vị trí từng control; hợp đồng layout này không giống một host Dock thông thường. T20/T21/T22 thêm workspace vào control trước khi thêm các thao tác, và được tạo dưới tab chưa hiển thị. **Chưa có phép đo runtime bounds để khẳng định nhánh layout nào là nguyên nhân cuối cùng**; không kết luận đây là lỗi quyền vì ADMIN tải được dữ liệu và source không có nhánh ẩn nút lập phiếu theo quyền.

Đề xuất debug layout/visibility lúc attach/chuyển tab; sửa cách cấu hình các workspace G05 theo hợp đồng helper chung. Chỉ sửa helper chung nếu chứng minh cần thiết; không thiết kế lại giao diện. Kiểm cả pager đang hiển thị so với handler phân trang, chưa coi khả năng lỗi pager là lỗi đã tái hiện.

## Ảnh hưởng và regression

T20-K01/K11/K12, T21-K01/K08/K10, T19-K05/K07/K10 và thao tác T22/T23 bị ảnh hưởng hoặc cần kiểm lại. Regression native: ADMIN/STORE tạo nháp → bỏ/chốt/reload, bộ lọc, chi tiết/link nguồn, phân trang trên >50 dòng; CASH bị chặn ở BLL/SQL. Kiểm chức năng tại125% khi đo xác nhận được DPI, giữ 100/150% NOT RUN. Nếu sửa helper chung, kiểm lại các workspace cũ sử dụng helper đó. Không đổi nghiệp vụ/SQL.
