# Kịch bản demo xuyên nguồn

Dùng DB demo/test riêng schema027, package ứng viên tương ứng; operator tạo ADMIN/CASH/STORE với password riêng ở máy. Không thực hiện demo ghi giao dịch vào DB sử dụng. Ghi mã nguồn từng bước và đối chiếu SQL, không chỉ nhìn UI. Tài khoản role là độc lập chức vụ.

1. CASH đăng nhập → bàn trống → SP001 hai dòng Ít đường/Không đá → chọn khách/ưu đãi hợp lệ. Đọc preview SQL; chia tiền mặt/chuyển khoản đúng tổng. Tiền đưa chỉ tính thối, không là doanh thu. Xác nhận thanh toán → in/preview → tra cứu đúng ngày thanh toán. Kiểm bàn trống, snapshot giá/giảm/tiêu hao/điểm và tổngTT. In lỗi chỉ in lại, không checkout lần hai.
2. STORE lập phiếu nhập nháp, bỏ nháp phải không ghi. ADMIN nhập10đơn vị×1000, trả ngay2000 → nợ8000 → trả3000+5000; phiếu chi mỗi nguồn đúng, tồn+10/còn nợ0. STORE chỉ xem nợ, không trả; STORE không được trả ngay khi nhập theo quyền hiện hành.
3. Xuất hủy một nguyên liệu → kiểm kê lấy snapshot → phiên khác xuất thêm → chốt cũ phải từ chối. Lấy snapshot/đếm lại, nhập lý do chênh lệch và chốt một lần; không reset header hoặc tự hoàn kho từ đơn mở bị hủy.
4. ADMIN tạo nhân viên theo giờ35000/h và ca qua đêm22h→06h;8giờ→preview/chốt280000 khi khoản bổ sung0 → chi toàn bộ. Chốt và chi là hai trạng thái/ngày nguồn; không chi trùng. Đổi hồ sơ không sửa snapshot đã chốt.
5. CASH tạo đơn giao có tên/SDT/địa chỉ/ship, thanh toán → Chờ giao→Đang giao→Đã giao. Tiền/kho/điểm không đổi; chỉ lưu trạng thái hiện tại, không timeline chuyển bước. Báo cáo lọc đúng ngày thanh toán; tiền món/giảm/ship/phí riêng. Tài sản/khấu hao là tham khảo, không dòng tiền/giá vốn chính xác.

Seed lịch sử chủ yếu04/2025, không mặc định khuyến mãi seed còn hiệu lực hôm nay. Luồng tạo mới dùng ngày hiện tại từ SQL và chính sách hợp lệ do operator chuẩn bị; không UPDATE ngày trả/giá snapshot để khớp demo. Bản này không có hoàn tiền, đổi điểm thành tiền, size/topping, API hoặc thanh toán online.

Các con số demo là expected trước thao tác. Actual theo biên bản T31/T32; chưa chạy phải ghi NOT RUN. Người dùng tự kiểm hình thức; agent kiểm chức năng125% và không đổi Windows Scale.
