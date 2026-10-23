# Dùng thử G03 trên đường chạy development

Ngày 18/10/2026. Solution `CafeManager.slnx`, startup project
`src/CafeManager.WinForms`, profile `CoffeeManager.Development`. F5 hoặc:

```powershell
dotnet run --project src/CafeManager.WinForms --launch-profile CoffeeManager.Development
```

DB cố định `localhost/CoffeeManager_Development`, schema009 sau cập nhật G03.
Cấu hình DPAPI và ba account ADMIN/CASH/STORE giữ từ G02. Xem mật khẩu riêng tại
máy theo README; không chép vào chat/log. Không chạy lại seed hoặc reset account.
Cập nhật schema khi app đã đóng bằng `tools/database/Update-DevelopmentG03.ps1`;
script kiểm marker, backup trước thay đổi, đối chiếu tồn/điểm/hạng/hóa đơn.

## ADMIN

1. Vào Nguyên liệu/Tồn, tạo mã chưa dùng NVL01–99 (ví dụ NVL90 nếu còn trống),
   tên và đơn vị Kg. Nguyên liệu mới tồn0. Mở sổ riêng, nhập `0,0180`, nguồn,
   xem lại rồi ghi. Bản ghi cũ có mốc chuyển đổi không được mở lại.
2. Vào Menu/Công thức, thêm mã SP chưa dùng, chọn danh mục, giá nguyênVND,
   lưu Ngừng bán. Chọn Công thức, chọn nguyên liệu/lượng theo đơn vị, lưu toàn
   công thức; sau đó Sửa sản phẩm và bật bán. Giá cũ trong hóa đơn giữ snapshot.
3. Vào Bàn/Khu vực: tab Khu vực tạo mã KV mới, tab Bàn tạo mã B mới và chọn KV.
   Sơ đồ lấy view; chọn B0003 trong demo sẽ đọc mã đơn hiện tại. G03 chưa mở POS.
4. Vào Khách hàng, thêm tên/SDT hoặc để SDT trống; mã KH được cấp tự động,
   điểm0/hạng theo SQL. Mở sổ điểm riêng trước khi khách phát sinh; sửa hồ sơ
   chỉ đổi tên/SDT. Hóa đơn hiện chỉ có mã/ngày/trạng thái; tra cứu đủ ở T17.
5. Vào Ưu đãi/Điểm, thêm KM bằng mã KM và8chữ số chưa dùng, ngày có thể cùng
   ngày, giảm0–100. Mã seed cũ như KM24BF được giữ khi sửa; không đổi khóa cũ.
   Preview ưu đãi chọn tiền món/ship/ngày/khách/KM, đọc kết quả SQL.
6. Tab Hạng thành viên: thay toàn chính sách, giữ ngưỡng0/mã/ngưỡng duy nhất,
   Xem lại từ SQL rồi Lưu/xác nhận. Tab Điểm: chọn khách, loại cộng/trừ, điểm,
   lý do, Xem lại số dư từ SQL rồi xác nhận. Thay input phải preview lại.

## CASH và STORE

CASH có đọc tồn/menu, sơ đồ/chọn bàn, tìm/thêm khách và preview KM; không sửa menu,
định mức, tồn, hạng/điểm/chính sách. STORE quản trị nguyên liệu nhưng không mở sổ,
đọc menu/công thức; không vào bàn, hồ sơ khách hoặc ưu đãi/điểm. Menu và service
cùng kiểm quyền. CASH/STORE chưa có POS/kho giao dịch hoàn chỉnh, mốc đó ở G04/G05.

## Khi lỗi

Giữ input khi validation/SQL từ chối. Xung đột cần tải lại. Timeout ghi giữ mã
request và payload, khóa đóng/gửi mới, dùng Đối chiếu; chưa thấy receipt không có
nghĩa rollback. Chỉ gửi lại chính request cũ qua luồng đối chiếu để SQL phân xử.
Mở sổ/điểm/chính sách luôn có bước xác nhận; Enter không tự chốt.

Đây là kịch bản dùng thử development, không ghi fixture nghiệm thu vào DB này.
Ảnh đọc màn kỹ thuật không thay nghiệm thu chỉnh sửa, timeout, DPI, Designer hoặc
thử với người dùng thật. Test chạy trên DBtest riêng sau khi duyệt đợt nhóm.
