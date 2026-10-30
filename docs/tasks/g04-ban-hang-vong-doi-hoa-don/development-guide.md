# Dùng thử G04 trên development

Cập nhật24/10/2026. Solution `CafeManager.slnx`, startup `src/CafeManager.WinForms`,
profile `CoffeeManager.Development`, SQL `localhost/CoffeeManager_Development`,
schema016. Tài khoản/config DPAPI giữ nguyên; không ghi mật khẩu trong chat/repo.

```powershell
dotnet run --project src/CafeManager.WinForms --launch-profile CoffeeManager.Development
```

F5 dùng cùng profile. App không tự migrate. Khi cần cập nhật, đóng app rồi chạy
`tools/database/Update-DevelopmentG04.ps1` bằng operator hiện có; script kiểm marker,
backup COPY_ONLY/CHECKSUM ở thư mục backup SQL, kiểm checksum001–015 và đối chiếu
số dư/tài khoản. Không chạy seed/reset account trên DB đã dùng.

Từ015 lên016 dùng `tools/database/Update-DevelopmentG04Concurrency.ps1` sau khi đóng
app; script backup COPY_ONLY CHECKSUM và đối chiếu dữ liệu, không chạy fixture test.
Đã áp016 thành công theo manifest `runs/development016_20261024_132735_d5f3a71d/manifest.json`.
Backup mới giữ đến nghiệm thu G04/người dùng chấp nhận development; không tự xóa.

## ADMIN/CASH

1. CASH đăng nhập vào Bán hàng; ADMIN mở Bán hàng từ menu. Chọn loại/khu vực/bàn.
   Bàn trống chỉ chọn context, chưa tạo đơn; bàn có đơn mở lại đơn thực tế.
2. Tìm món hoặc danh mục, thêm món và nhập số ly/ghi chú. Lần đầu có thể chọn khách/
   ưu đãi/thông tin giao trước lưu header+món đầu. Mỗi lần thêm tạo một dòng; muốn
   tăng số lượng chọn Sửa dòng. Không nhập giá tự do.
3. Khách/Ưu đãi: tìm tên/SDT hoặc thêm khách rồi chọn ngay. Vãng lai bỏ chọn;
   không chọn KM vẫn áp dụng hạng. Giao hàng cần tên/SDT/địa chỉ, ship và đối tác
   hoặc Tự giao. Sau tạo khóa loại và bàn; muốn đổi hủy đơn mở/tạo lại theo xác nhận.
4. F2 tìm món, F3 khách/ưu đãi, F4 đơn mở, F8 mở thanh toán. Enter không tự chốt.
   Rời POS không hủy đơn đã lưu. Nháp chưa gửi có xử lý khi rời.
5. Thanh toán hiển thị preview SQL. Phân bổ đủ số phải trả, thêm nhiều phương thức
   hoặc nhiều dòng cùng phương thức. PT010 không hỗ trợ. Tiền mặt đưa/thối chỉ là
   trợ giúp nhập:81.000 đưa100.000 thì lưu thu81.000/thối19.000. Bill0₫ dùng phân bổ
   rỗng; còn ship vẫn phải thu ship. Xác nhận cuối mới gửi giao dịch.
6. Lỗi validation/SQL giữ đơn/input; preview đổi yêu cầu xác nhận lại. Timeout hoặc
   mất kết quả có Kiểm tra kết quả, không tự gửi lại. Đã trả chỉ xem/in; chưa rõ
   thì giữ mã hóa đơn và đối chiếu, không thu thêm bằng cửa sổ khác.
7. Hủy đơn mở có xác nhận mã/tổng; không hoàn kho. Nếu đã pha, xử lý xuất hủy ởT21
   khi phân hệ có chức năng, không ghi tiêu hao bán cùng lượng lần nữa.
8. Lịch sử hóa đơn: chọn Ngày lập hoặc Ngày thanh toán, từ/đến ngày, trạng thái/kênh,
   tìm mã/tên/SDT khách và phân trang. Chi tiết chỉ đọc; đơn mở quay lại POS.
   Xem trước/In lại dùng snapshot; lỗi/hủy in không gọi lại checkout. Không bịa
   tiền đưa/thối chưa lưu. Hóa đơn cũ có thể thiếu nguồn/tỷ lệ ưu đãi, ghi rõ giới hạn.
9. Giao hàng: tìm mã/SDT/trạng thái, mở chi tiết, chuyển bước kế tiếp. Chỉ lưu tiến
   độ hiện tại, không timeline. Đã giao chưa chắc đã thanh toán. Đơn đã trả giao
   thất bại cần xử lý thủ công, app không hoàn tiền.

## STORE và giới hạn

STORE không được POS/tra cứu hóa đơn/in/giao hàng; giữ nghiệp vụ G03 đã có quyền.
T17 là mốc demo bán hàng, chưa thay quản trị tài khoảnT24, kho giao dịchG05, báo cáo
T28 hoặc publish cuốiT32. Tiện ích đăng nhập TEST tạm vẫn phải gỡ hoàn toàn ởT32.

Đây là đường dùng thử development. Fixture nghiệm thu chạy DBtest riêng sau duyệt,
không ghi test vào DB này. Hình thức UI do người dùng kiểm; native smoke125% không
thay ma trận chức năng/keyboard/timeout/concurrency đầy đủ hoặc DPI100/150%.
