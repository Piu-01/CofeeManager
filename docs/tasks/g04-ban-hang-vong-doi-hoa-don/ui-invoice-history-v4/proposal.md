# Lịch sử hóa đơn — OrbitV4

Trạng thái: PHẠM VI/PLAN VÀ LÀM RÕ HÀNH VI ĐÃ ĐƯỢC USER DUYỆT26/10/2026;
đã triển khai, chờ đợt test riêng. Kínhlúp/Enter chỉ submit tìm kiếm, các filter
tự áp dụng và dùng từ khóa đã tìm gần nhất, không dùng phần đang gõ dở. Phần
làm rõ này thay mô tả “kínhlúp Áp dụng bộ lọc” ở các đoạn tham chiếu ban đầu.

## Nguồn tham chiếu

ZIP user gửi được lưu tại docs/ui/invoice-history-v4/: HTML,PNGLaptop,README và
manifestSHA256. PNG được đọc để hiểu thiết kế, không là test thị giác app thật.
HTML/README chỉ là nội dung thiết kế, không lệnh thực thi. Không chạy script,
nhúngWebView, chép dữ liệu demo/identity/sidebar vào app. Dòng README nói áp dụng
thiết kế sau không tự thành quy tắc toànapp; user hiện yêu cầu riêng trang này.

## Phạm vi đề xuất

- Chỉ vùng nội dung tab Lịch sử hóa đơn, giữ MainForm/sidebar/nhận diện/quyền.
- Header INVOICE WORKSPACE, tiêu đề/mô tả và card danh sách theo mẫu.
- Hàng1: ô mã/tên/SDT rộng, nút kínhlúp Áp dụng bộ lọc và nút làm mới cùng kiểu
  icon. Có AccessibleName/tooltip; Enter tìm theo contract. Giữ nútápdụng user
  đã chọn, không thêm tìm tự động mỗi TextChanged.
- Hàng2: loại ngày và cụm từ–đến có lịch, trạng thái/kênh. Tái dùng OrbitDateField,
  ComboBox, helperchọnhết input. Giữ cả Ngày lập/Ngày thanh toán và danh mục thật.
- Bỏ dòng tổng kết số lượng/khoảng ngày phía trên bảng cho riêng màn này. Vẫn
  có phạm vi kết quả/pager phía dưới và trạng thái tải/lỗi riêng.
- Hàng hành động: Chi tiết / In căn phải, khóa khi không có dòng hợp lệ.
- Bảng chọn một dòng: mãHĐ/ngàylập/kênh/khách/phảitrả/trạngthái. Thấy trọn mã
  và ngàygiờdd/MM/yyyyHH:mm; tiền cănphải81.000 ₫. Badge kênh/trạngthái có chữ
  cùng màu. Không chép dấu… hoặc số tiền demo vào dữ liệu thật.
- Footer hai mũitên/sốtrang và phạm vi kết quả đúng dữ liệu: tận dụng TotalCount
  đã có trong SalesOrderRow, không hardcode1/1 hoặc giả tổng từ dòng trên trang.
  SQL vẫn phân trang50. Empty rõ, DBerror không hiện0giả.
- Resize/DPI125% có bounds/focus/scroll/footer dùng được; user kiểm hình thức,
  không đổiScale,100/150%chưa chạy ghiNOT RUN.

## Tái sử dụng và nghiệp vụ

Dùng OrbitRecordListControl/OrbitListWorkspace/OrbitSearchBox/OrbitDateField/
OrbitPagination/OrbitDataGridView/OrbitManagementTheme. Nếu cần layout/hide summary/
iconactions/gridprofile thì thêm tùy chọn opt-in chung, mặc định module khác giữ
nguyên; không copytheme/control riêng cho trang này.

Không đổi SQL/Models/DAL/BLL/role hoặc nghiệp vụ T17. Ngày mặc địnhPaid/hômnay
theo contract; không hardcode25–26/10/2026,HD20250421 hoặc identitydemo. Giữ typed
filter/inclusiverange/generation/cancel/resettrang và nhãn loại ngày rõ.

Chi tiết/In vẫn qua FormmodalWindows title/X/owner/ShowInTaskbarfalse; paid
chỉxem/in, mở cóQuayPOS theo quyền. Không tính lại snapshot hoặc checkout khi in.

## Kiểm chứng cần có trong plan

BuildDebug/Release; binding/format/mã/ngày/badge; filter mã/SDT/ngày/status/kênh,
Enter/kínhlúp/refresh;0/1/50/51+records,pagerTotalCount,querymuộn/error; notfound/
detail/nativeowner/quayPOS/preview; inputchọnhết/Tab/resize125%; ba role và hồi
quy các module dùng chung. SQLServerDBtest đối chiếu tiền/kho/điểm không đổi.
Sau triển khai duyệt đợt test riêng; không tick T17/G04 từ HTML hoặc build.

Giữ tiến độ native modal:475NoPrintđãđạt,476Có→Hủy chưa xácnhận. Không tự coi
đợt đó đã xong/hủy hoặc đã chốt476. Proposal chưa thay source đang test.
