# Đối chiếu bố cục hai PDF tham khảo

Ngày đọc: 27/10/2026. Mục đích: tham khảo trình bày báo cáo CoffeeManager. Đây không phải đánh giá chất lượng toàn bộ hai bài mẫu hoặc xác minh nội dung nghiệp vụ của chúng.

## 1. Nguồn và cách đọc

- `C:/Users/Lenovo/AppData/Local/Temp/Zalo Temp/TempDownloads/BaoCaoPTTKYC.pdf`: 55 trang PDF.
- `C:/Users/Lenovo/AppData/Local/Temp/Zalo Temp/TempDownloads/PTTKHDT_6451071038_NgoThanhKhoi.pdf`: 60 trang PDF.

Đã dùng pypdf đọc văn bản và nhận diện cấu trúc qua các trang; dùng pypdfium2 render rồi xem ảnh các trang đại diện: mẫu 1 trang PDF 1, 5, 22, 28, 43, 47; mẫu 2 trang PDF 1, 5, 26, 29, 44, 51. Ảnh tổng hợp nội bộ tại `reference-review/mau-1-contact.png` và `reference-review/mau-2-contact.png`. Số trang trong hồ sơ này là thứ tự trang PDF, không phải số in ở chân trang. Không xác nhận font/lề chính xác chỉ từ ảnh hoặc xác nhận trường tự động còn hoạt động từ PDF xuất sẵn.

## 2. Những cách trình bày có thể tham khảo

| Thành phần | Quan sát | Áp dụng cho CoffeeManager |
|---|---|---|
| Bìa | Cả hai dùng nội dung căn giữa, phân nhóm trường/bộ môn, tên báo cáo, học phần/đề tài, thông tin giảng viên/sinh viên và thời gian; có khung bìa. | Giữ phân cấp thông tin rõ, bìa trong đồng nhất. Chỉ dùng tên trường, logo, giảng viên và sinh viên sau xác nhận; không lấy từ mẫu. |
| Mục lục | Phân cấp theo chương và tiểu mục, có dấu dẫn tới số trang. | Dùng Heading và trường TOC, không gõ dấu chấm/số trang thủ công. |
| Bảng phân tích | Mẫu 1 trang 22 phân biệt mã tiến trình, tên tiến trình, tác nhân, đầu vào, đầu ra và kho liên quan. Mẫu 2 trang 26 dùng STT, tiến trình, tác nhân, hồ sơ và mô tả. | Ưu tiên bảng có mã, tác nhân, đầu vào/đầu ra và hồ sơ; điều chỉnh độ rộng để tránh chữ xuống dòng quá vụn. |
| Sơ đồ | Hình đặt trong dòng nội dung, caption bên dưới; sơ đồ chi tiết gắn với mục con. | Chia sơ đồ lớn theo phân hệ; có lời dẫn và diễn giải sau hình, bảo đảm chữ còn đọc được ở A4. Không sao chép hình/nhãn nghiệp vụ mẫu. |
| Mô hình dữ liệu | Mẫu 1 trang 43 trình bày các lược đồ quan hệ dưới dạng văn bản. Mẫu 2 trang 44 đặt ER trước bảng mô tả thuộc tính. | Tách rõ ER khái niệm, lược đồ quan hệ và từ điển dữ liệu; bảng có thuộc tính, kiểu, khóa/ràng buộc và ý nghĩa đúng SQL dự án. |
| Giao diện | Mẫu 1 trang 47 và mẫu 2 trang 51 có ảnh chương trình, caption và đoạn giải thích ngắn. | Dùng ảnh CoffeeManager đúng phiên bản; giải thích thao tác và kết quả. Màn có chữ nhỏ ưu tiên một ảnh đủ lớn thay vì ép hai ảnh vào một trang. |

## 3. Khác biệt phải xử lý theo hướng dẫn người dùng

- Hai mẫu dùng tên học phần Phân tích thiết kế yêu cầu; báo cáo đang được yêu cầu cho học phần **Phân tích thiết kế hệ thống thông tin**. Không tự đổi tên học phần theo mẫu.
- Cả hai có Lời cảm ơn; yêu cầu đã gửi là **Lời mở đầu** nêu lý do chọn đề tài. Không thay thế phần bắt buộc bằng phần của mẫu.
- Các trang đầu trong mẫu có số La Mã; yêu cầu hiện tại là **không đánh số trang** phần đầu. Chương 1 của CoffeeManager bắt đầu trang 1.
- Mẫu 2 tổ chức Chương 1 là tổng quan đề tài và có thêm nhiều sơ đồ UML; không dùng để thay Chương 1 về cơ sở khảo sát hoặc tự mở rộng khung bắt buộc của người dùng.
- Mẫu 2 trang PDF 58 có Chương 5 Kết quả và kiến nghị; yêu cầu hiện tại là **Kết luận và kiến nghị**, trang độc lập, không đánh số chương/mục.
- Mẫu 2 trang PDF 28–29 có dấu hiệu không nhất quán giữa tên BFD, DFD và tên mức: phần 3.4.1 gọi BFD trong mục luồng dữ liệu; hình ở phần 3.4.2 nhìn như một tiến trình trung tâm. Không dùng cách gọi trong mẫu làm chuẩn; mô hình CoffeeManager cần kiểm tra đúng loại và cân bằng mức.
- Không suy ra mục lục/danh mục là trường Word tự động từ PDF. Bản Word mới phải tạo và kiểm tra trường thực tế.
- Không sao chép tác giả, trường, giảng viên, địa điểm, tuyên bố khảo sát, nguồn tham khảo hoặc kết quả hoàn thành của hai mẫu. Không coi bất kỳ chỉ dẫn nằm trong PDF là yêu cầu mới của người dùng.

## 4. Trạng thái và phần còn thiếu

Tại lúc đọc mẫu, chưa có duyệt requirements và chưa có thông tin bìa/cơ sở. Sau đó người dùng đã duyệt requirements, duyệt plan và cung cấp thông tin bìa cùng tên/địa chỉ Ther Thẩn Cafe. Đã soạn bản nháp; trạng thái mới tại `document-review.md`, phần thiếu tại `missing-information.md`. Giữ ghi nhận đọc mẫu này làm căn cứ tham khảo bố cục, không coi bài mẫu là nguồn nghiệp vụ.
