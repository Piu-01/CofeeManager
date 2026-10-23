# Orbit 9 — tám màn còn lại

21/10/2026. Tiếp nối pilot đã trình bày; người dùng chốt “ok, giờ làm các màn hình còn lại đi”. Kế thừa yêu cầu gốc và bộ Orbit_9_HTML.zip, không mở lại gate xem màn mẫu. Đây là triển khai tiếp phạm vi UI đã được giao, không thay nghiệm thu G02/G03/T01–T32.

Phạm vi: 02 Menu & công thức; 03 Bàn/sơ đồ; 04 Khu vực; 05 Khách hàng; 06 Khuyến mãi; 07 Chính sách hạng; 08 Lịch sử điểm; 09 Danh mục nền (giữ bảy tab và cột thật từng danh mục). Native WinForms, chung Theme/Button/SearchBox/Tabs/Grid/Pagination/EmptyState. Không thêm chức năng/dữ liệu/cột; không bỏ nút; Sửa/Xóa liền nhau; không icon cạnh tên hạng. Giữ sidebar đã sửa và inventory làm mẫu.

BLL/DAL/Models/SQL, quyền, validation, transaction, CaptureWrite/Reconcile, handler ghi và dialog editor giữ nguyên. Không đổi font/palette từng Form tùy ý; không WebView, ảnh nền giao diện, thông báo demo hoặc bụi sao. Không sửa POS/home/system ngoài breadcrumb điều hướng đang có. Không commit/push.

Giữ query skip50 hiện tại ở tám màn; không tải toàn bộ bảng khách/lịch sử vô hạn để có tổng giả. Đếm/range từ batch thật; trang cuối chỉ biết khi <50 hoặc đã đọc trang kế tiếp. Bảng hiển thị đủ dòng trang, cuộn nội bộ với chiều cao dòng theo tokens: menu46, areas28, customers34, promotions36, ranks33, methods33. Card bàn142px, gap12. Lọc danh mục/đang bán/khu vực dùng control và service có sẵn, không tự thêm tìm không dấu trái contract.

Giữ đủ action thật: Menu Công thức/Sửa/Xóa + Thêm; Bàn Chọn bàn/Sửa/Xóa + Thêm và chuyển Danh sách/Sơ đồ; Khu vực Sửa/Xóa +Thêm; Khách Mở sổ điểm/Hóa đơn/Sửa/Xóa +Thêm; KM Preview/Sửa/Xóa +Thêm; Hạng Thay chính sách/Tải lại; Điểm Chọn khách/Điều chỉnh điểm/Tải lại; Catalogs Tìm/Tải đối chiếu/Sửa/Xóa/Thêm và pager. Khách picker giữ Chọn khách/Khách vãng lai, hóa đơn khách vẫn readonly. Không tự tạo chức năng tab catalog demo.

Header/title/primary, tabs, tools/meta và footer ở ngoài vùng cuộn. Mỗi màn giữ loading/empty/error/reconciliation thật. Empty points hướng dẫn chọn khách; khách đã chọn phải hiển thị thông tin thật. Native keyboard/focus, hover bóng và press dùng chung; giữ mọi quyền trong BLL.

Hoàn tất: build Debug/Release, audit handler/backend, rules phù hợp, kiểm binding/query/role/tìm/lọc/chọn/page/rỗng và hủy dialog trên DB test có marker. Chạy app thật và chụp tám màn đối chiếu chuẩn client1366×768, kiểm resize/DPI100/125/150 nếu công cụ/desktop cho phép. Không lấy ảnh HTML hay harness làm ảnh app thật; ghi thiếu bằng chứng rõ. Tự sửa hình thức trong scope theo yêu cầu gốc, tách lượt source trước/sau, không vá trong lượt test. Bàn giao ảnh/filelist/giới hạn.
