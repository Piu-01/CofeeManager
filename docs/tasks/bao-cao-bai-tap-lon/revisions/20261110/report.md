# Chỉnh báo cáo theo yêu cầu ngày 10/11/2026

Đã sửa trực tiếp `artifacts/bao-cao-bai-tap-lon/BaoCao_CoffeeManager.docx`, không dựng lại từ script cũ vì file Word hiện tại có các chỉnh sửa của người dùng. Bản trước sửa được giữ tại `BaoCao_CoffeeManager-before.docx` cạnh hồ sơ này.

## Thay đổi

- Bỏ nền xám trong các bảng và nền tô của đoạn/style; bảng nền trắng, viền đen thông thường. Nội dung chữ của cả 44 bảng trước/sau khớp hoàn toàn, gồm cả những thay đổi đã có trong file người dùng.
- Chuyển chữ/tiêu đề sang đen, bỏ ảnh hưởng màu theme ở định dạng được chỉnh; mục lục cập nhật bằng Word cũng được ép màu đen.
- Thụt đầu dòng 1,27 cm cho 156 đoạn văn, giữ tiêu đề, caption, mục lục, chữ trong bảng và phần chữ căn giữa/ký tên theo chức năng của chúng.
- Bỏ 60 dấu trích dẫn số trong ngoặc vuông, kể cả số đầu các mục tài liệu tham khảo. Giữ tên tác giả, tài liệu, URL và phần nội dung nguồn để người đọc vẫn tra cứu được.
- Bổ sung mục 4.2.4 Trang tổng quan quản lý: ngày/tháng, bốn KPI, so sánh, biểu đồ xu hướng/cơ cấu, Top5, nhóm Cần chú ý và xem nguồn.
- Bổ sung mục 4.2.5 Lựa chọn ngôn ngữ tiếng Việt và tiếng Anh: đổi ngay ở Login; lưu tại Thông tin hệ thống và áp dụng khi đăng xuất/mở lại; fallback VI, giữ nguyên dữ liệu/mã và định dạng VND/ngày giờ.
- Mục phạm vi được chuyển thành 4.2.6; cập nhật các đoạn diễn giải liên quan và kết luận để phân biệt mốc cũ 27/10 với bổ sung 10/11. Không thay nội dung các bảng hoặc số liệu lịch sử bằng kết quả tự tạo.

## Căn cứ bổ sung

Đọc AdminDashboardControl, DashboardTrendChart/DashboardChannelChart và hồ sơ T28 analytics; LanguageService/L, LanguagePreference, LoginForm, SystemInfoControl và hồ sơ localization-vi-en. Snapshot các nguồn chính trong `source-manifest.json`.

Nguồn cho biết migration028 phục vụ dashboard phân tích chưa áp development tại mốc hồ sơ10/11; localization đã triển khai và có kiểm kỹ thuật, chưa nghiệm thu toàn bộ native/SQL. Các giới hạn này được giữ đúng trong văn bản. Chưa có ảnh native mới phù hợp trong bộ nguồn đã kiểm; thêm vị trí UI08/UI09 và kích thước cụ thể, không tạo screenshot giả.

## Kiểm tra

- Word cập nhật ba mục lục/danh mục, lưu và export PDF QA thành công: **64 trang**. Kiểm tra lại DOCX sau Word, nội dung44bảng và toàn bộ media có sẵn vẫn nguyên vẹn; không còn dấu [số] trong văn bản, ô bảng nềnFFFFFF, màu chữ tường minh000000.
- Render toàn bộ64trang bằng pypdfium2; đã xem các trang qua ảnh tổng hợp và xem riêng ba trang nội dung mới49–51 để kiểm văn bản, thụt đầu dòng và bố cục. Không có thay đổi hình thức app hoặc test WinForms/SQL trong công việc này.
- Kiểm chi tiết ở `edit-results.json`, `verification.json`; bản PDF và PNG chỉ dùng QA. Word COM báo lỗi RPC khi Quit sau Save/Export, file đã được mở lại và kiểm độc lập; không dùng cảnh báo thoát này làm kết quả kiểm thử phần mềm.

Các ảnh chương trình còn thiếu và dữ kiện khảo sát cơ cấu quán vẫn là phần cần bổ sung như bản trước; không thay bằng giả định. Không commit/push hoặc thay source/schema ứng dụng. Không chạy lại build_report.py cũ lên bản Word này vì sẽ làm mất các chỉnh sửa trực tiếp đã có.
