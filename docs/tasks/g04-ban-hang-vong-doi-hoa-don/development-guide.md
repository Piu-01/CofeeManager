# Dùng thử G04 trên development

Cập nhật24/10/2026, bổ sung16/11/2026 và20/11/2026. Solution `CafeManager.slnx`, startup
`src/CafeManager.WinForms`, profile `CoffeeManager.Development`, SQL
`localhost/CoffeeManager_Development`, schema đích036 (032 T33, 033 nhãn danh mục,
034 phiên bàn, 035 chi tiết hóa đơn, 036 In tạm nối thêm món). Tài khoản/config DPAPI
giữ nguyên; không ghi mật khẩu trong chat/repo.

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

**Từ031 lên032/033/034 (T33/T34):** đóng app, backup COPY_ONLY + CHECKSUM (Verify)
và chạy `Apply-Migration.ps1 -Version 32`, rồi `33`, rồi `34` tuần tự; không chạy
seed. 032 mở rộng trạng thái/index/view và cổng thu; 033 chỉ đổi nhãn danh mục;
034 tạo PHIEN_BAN, gắn MaPhien, view 3 cột phiên và sp_TraBan. Preflight của 032/034
sẽ chặn nếu đơn mở có tiền/tiêu hao hoặc một bàn nhiều đơn; khi đó dừng, đối chiếu
dữ liệu thay vì sửa tay. Sau áp, kiểm Version=34, đăng nhập thường và mở bàn/POS
một lượt; chưa chạy fixture test trên DB này. Không sửa migration đã áp.

**Từ035 lên036 (In tạm nối thêm món, 20/11/2026):** đóng app, backup
COPY_ONLY/CHECKSUM rồi chạy `Apply-Migration.ps1 -Version 35` (nếu chưa có) và `36`.
036 chỉ thay đổi procedure (`sp_SuaDongHoaDon`, `sp_SuaThongTinHoaDon`), không đổi bảng
hay dữ liệu; đơn đang mở giữ nguyên. Sau áp kiểm Version=36, mở một đơn, In tạm, thêm
món để xác nhận cùng hóa đơn; không chạy fixture test trên DB này.

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
   Bấm **In tạm** để lưu đơn “Chờ thanh toán” và in ngay (không hỏi). Khách gọi thêm
   thì bấm món/tăng số lượng để nối vào cùng hóa đơn — thêm lại món cũ gộp một dòng;
   không xóa/giảm/đổi ghi chú sau khi in. F3 vẫn sửa được khách/ưu đãi/ship; nút đổi
   thành **In lại** để in lại tạm tính. Không còn nút “Mở lại đơn”. Bàn còn bận cho tới
   khi bấm **Trả bàn**, kể cả khi khách đã trả trước; sau khi thu, khách gọi thêm tạo
   hóa đơn mới trong cùng phiên (không gộp).
5. Thanh toán hiển thị preview SQL. Phân bổ đủ số phải trả, thêm nhiều phương thức
   hoặc nhiều dòng cùng phương thức. PT010 không hỗ trợ. Tiền mặt đưa/thối chỉ là
   trợ giúp nhập:81.000 đưa100.000 thì lưu thu81.000/thối19.000. Bill0₫ dùng phân bổ
   rỗng; còn ship vẫn phải thu ship. Xác nhận cuối mới gửi giao dịch.
   Thu xong app hỏi in rồi hỏi **Trả bàn ngay?** (mặc định Có); chọn Không thì bàn
   hiện “Đã trả · giữ bàn”. Lỗi trả bàn không làm mất kết quả thu.
6. Lỗi validation/SQL giữ đơn/input; preview đổi yêu cầu xác nhận lại. Timeout hoặc
   mất kết quả có Kiểm tra kết quả, không tự gửi lại. Đã trả chỉ xem/in; chưa rõ
   thì giữ mã hóa đơn và đối chiếu, không thu thêm bằng cửa sổ khác.
7. Hủy đơn mở có xác nhận mã/tổng; không hoàn kho. Nếu đã pha, xử lý xuất hủy ởT21
   khi phân hệ có chức năng, không ghi tiêu hao bán cùng lượng lần nữa. Hủy đơn cuối
   cũng hỏi trả bàn; chỉ trả bàn khi phiên không còn đơn chưa chốt.
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
