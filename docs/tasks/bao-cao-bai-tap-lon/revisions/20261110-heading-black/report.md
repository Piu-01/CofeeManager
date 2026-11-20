# Mọi cấp tiêu đề dùng màu đen

Sửa theo ảnh và yêu cầu trực tiếp của người dùng ngày 10/11/2026. Bản Word đầu vào có metadata/style của WPS; Heading2/3 đoạn đã có w:color000000 nhưng các style ký tự liên kết Heading2Char/Heading3Char vẫn có4F81BD, themeaccent1 và w14:textFill accent1. Kiểm tra trước chỉ xét w:color trong document nên chưa phát hiện lớp màu này.

Đã sửa20style heading/liên kết và71đoạn gồm tiêu đề cùng bản sao số mục trong TOC. Mọi cấp Heading1–9, các style ký tự liên kết, màu paragraph mark, màu trực tiếp của từng run tiêu đề và màu số thứ tự trong numbering được đặt000000. Loại màu theme/hiệu ứng chữ có thể kéo màu xanh trở lại. Không thay chữ, font, kích thước hoặc nội dung bảng/ảnh.

Word mở/lưu, cập nhật3danh mục và export64trang thành công. Kiểm lại sau Save: không có màu heading sai, toàn bộ44bảng và media có sẵn khớp bản trước. Đã render và xem trang12có2.1/2.2 và trang14có3.1/3.1.1/3.1.2, đều màu đen. Chi tiết: verification.json; bản trước: before.docx.

File chính đã cập nhật tại artifacts/bao-cao-bai-tap-lon/BaoCao_CoffeeManager.docx; đồng thời lưu bản có tên rõ BaoCao_CoffeeManager_TieuDeDen.docx để người dùng mở đúng bản mới, tránh xem lại tab/bản cũ đã mở trong trình soạn thảo. Hai file giống nhau sau kiểm tra. Không thay dữ liệu/source ứng dụng, không chạy test phần mềm hoặc thao tác Git.
