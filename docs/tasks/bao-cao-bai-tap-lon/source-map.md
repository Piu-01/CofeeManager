# Truy vết nguồn báo cáo

Ngày biên soạn: 27/10/2026. Mốc đọc source được lưu bằng SHA-256 trong `source-manifest.json`; không dùng Git commit vì người dùng không giao thao tác Git. Các file mã nguồn/SQL chỉ được đọc, không chạy nghiệp vụ hoặc thay đổi dữ liệu khi viết báo cáo.

## Nguồn của nội dung

| Nội dung báo cáo | Nguồn | Giới hạn |
|---|---|---|
| Thông tin bìa | Người dùng xác nhận Nguyễn Minh Phú, CQ.65.CNTT, 6551071065, khóa 64, đề tài Quản lí quán cafe; các mục khác giống mẫu thứ hai. | Giữ khóa 64 đúng lời người dùng, không tự đổi theo lớp/MSSV. Học phần giữ theo yêu cầu ban đầu Phân tích thiết kế hệ thống thông tin. |
| Khung/logo bìa | Hai trang đầu PTTKHDT_6451071038_NgoThanhKhoi.pdf, chỉ dùng đối tượng đồ họa. | Loại toàn bộ text của trang mẫu, đặt text đã xác nhận. Letter → A4 bằng biến đổi đồng dạng. Bìa trong Word là ảnh render sắc nét từ PDF, không phải chữ Word chỉnh trực tiếp; source tạo bìa được giữ trong build_report.py. |
| Chương 1, tên/địa chỉ | Người dùng cung cấp Ther Thẩn Cafe, 49 Đường 449, Tăng Nhơn Phú, Hồ Chí Minh; đối chiếu bài đăng 01/07/2026 trên FindGlocal. | FindGlocal là nguồn tổng hợp. Không dùng địa chỉ tổng trang ở Quận 4 cho cơ sở Đường 449; không chuyển giờ mở cửa/tổ chức/ảnh địa điểm khác sang cơ sở được chọn. Chưa có biên bản khảo sát trực tiếp. |
| Phạm vi, kiến trúc, nhóm chức năng | docs/KeHoach_QuanLiQuanCafe.md, mục 1–10 và 12–13. | Kế hoạch gồm thiết kế đích; chưa đồng nghĩa đã xây dựng. |
| Luồng tương tác, phiên, thanh toán, in | docs/UX_QuanLiQuanCafe.md, mục 1–3, 5–9; source Services và Modules/Sales. | UX là contract. Chỉ khẳng định source có trách nhiệm tương ứng khi đối chiếu source. |
| Dữ liệu và liên kết | database/DB_Schema.sql; Insert_Data.sql; migrations 001–021. | Đọc đủ schema baseline và đối chiếu các khai báo thay đổi; không chạy SQL/khẳng định nghiệm thu engine trong task báo cáo. |
| Tạo/sửa đơn, giá dòng, quyền, timeout | SalesService.cs, SalesService.Checkout.cs; SalesRepository.cs; migrations 010–016. | MaDong/MaTT; phiên bản/request; không giả thanh toán trực tuyến. |
| Nhập/xuất/kiểm kê/công nợ/chi nguồn | migrations 017–021; các service/repository/control tương ứng; report G05. | Source có chức năng nhưng đợt test G05 còn FAIL/BLOCKED/NOT RUN. |
| Giao hàng | migration 014; plan T18. | Chỉ lưu trạng thái hiện tại; không tự tạo timeline/lịch sử chuyển bước. |
| Modal có owner | docs/tasks/embedded-dialogs/native-implementation-report.md, native-test-results.md; docs/ui/WINFORMS_IMPLEMENTATION.md. | Không dùng mô tả host nhúng cũ làm hiện trạng; kết quả cũ giữ theo hash/source của nó. |
| Trạng thái xây dựng/kiểm chứng | Chỉ mục 12.1 của kế hoạch; report G04/G05 và biên bản liên quan. | G05 27/10: 38 PASS, 5 FAIL, 10 BLOCKED, 2 NOT RUN/55 ID. Không cộng các smoke/subcase thành số ID gốc hoặc chứng nhận toàn ứng dụng. |
| Ảnh WinForms | figure-register.md, ba ảnh native được chọn ngày 17/10 và 22/10. | Ảnh lịch sử có nhãn ngày; không chứng nhận bản hiện tại. Không sử dụng screenshot HTML, ảnh đề xuất hoặc ảnh lỗi làm màn thành công. |

## Nguồn công khai đã đọc

Các mã tham khảo tương ứng danh mục cuối Word. Văn bản diễn giải lại có giới hạn; không sao hình hoặc ví dụ nghiệp vụ từ nguồn sang CoffeeManager.

- [3] University of Cape Town, *Elements of data-flow diagrams*, MSc-IT January 2011: https://www.cs.uct.ac.za/mit_notes/software/htmls/ch06s05.html — thành phần và ký pháp DFD.
- [4] University of Cape Town, *Context diagrams*, cùng bộ: https://www.cs.uct.ac.za/mit_notes/software/htmls/ch06s06.html — ranh giới và một tiến trình ngữ cảnh.
- [5] Adrienne Watt/Nelson Eng, *Database Design 2nd Edition*, BCcampus, 2014; chương 8: https://opentextbc.ca/dbdesign01/chapter/chapter-8-entity-relationship-model/ — khái niệm ER. Metadata tác giả/năm đối chiếu tại https://opentextbc.ca/dbdesign01/.
- [7] Chương 11: https://opentextbc.ca/dbdesign01/chapter/chapter-11-functional-dependencies/ — phụ thuộc hàm; các phụ thuộc trong báo cáo được phân tích riêng theo schema dự án.
- [8] Chương 12: https://opentextbc.ca/dbdesign01/chapter/chapter-12-normalization/ — mục tiêu chuẩn hóa và các dạng chuẩn; không sao dữ liệu ví dụ của sách.
- [9] Microsoft Learn, *Windows Forms Overview*: https://learn.microsoft.com/en-us/dotnet/desktop/winforms/overview/ — nền tảng UI; không lấy dòng “latest version” trong trang làm target dự án.
- [10] Microsoft Learn, *Transactions*: https://learn.microsoft.com/en-us/sql/t-sql/language-elements/transactions-transact-sql — commit/rollback.
- [11] Microsoft Learn, *Table-Valued Parameters*: https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql/table-valued-parameters — nguyên lý TVP, không suy dự án dùng .NET Framework từ URL tài liệu.
- [13] FindGlocal, *Ther Thẩn Cafe*: https://www.findglocal.com/VN/Ho-Chi-Minh-City/696962493504088/Ther-Th%E1%BA%A9n-Cafe — bài 01/07/2026 ghi 49 đường 449; không dùng nội dung quảng bá hoặc chi nhánh khác làm dữ kiện khảo sát.

Ngày truy cập các nguồn trên: 27/10/2026. Nguồn không hiển thị năm xuất bản không được gán năm truy cập thành năm xuất bản. Hai PDF mẫu chỉ tham khảo bố cục và đồ họa bìa đã được người dùng cho dùng, không nằm trong nguồn lý thuyết hoặc dữ liệu CoffeeManager.

## Kiểm tra mô hình

- Một từ điển luồng F01–F10 dùng xuyên mức ngữ cảnh, mức đỉnh và phần giải thích dưới đỉnh. F07/F08 là nhập–chi/nợ; F09/F10 là nguồn lương đích. Mức đỉnh tách hai hình để giữ chữ đọc được, cùng sáu tiến trình.
- DFD không vẽ khách/NCC/đối tác như người trực tiếp dùng app; không nối tác nhân thẳng vào kho. Tham chiếu cùng mã trên nhiều hàng là cùng đối tượng, không tạo nhiều kho độc lập.
- Các cụm ER thể hiện liên kết khái niệm, có giải thích bội số và cận dưới nghiệp vụ khác ràng buộc FK. Chỉ mô hình hóa tổ chức cơ sở khi có xác nhận.
- Năm hình quan hệ dùng FK→PK; script kiểm cặp trường theo schema-baseline.json, ngoại lệ khai báo 011/017 được liệt kê rõ trong kiểm tra.
- 35 quan hệ baseline có danh sách thuộc tính và PK/FK; các thay đổi migration trình bày riêng. Không kết luận toàn schema vật lý ở 3NF; dữ liệu snapshot/derived được giải thích riêng.
