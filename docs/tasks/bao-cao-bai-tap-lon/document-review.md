# Kiểm tra và bàn giao bản thảo báo cáo

Ngày: 27/10/2026. **Đã biên soạn và kiểm tra bản nháp; chưa gọi là bản sẵn sàng nộp.** Requirements và plan đã được người dùng duyệt. Không mở lại điểm duyệt hoặc chờ duyệt từng chương.

## Đầu ra

- Word: `artifacts/bao-cao-bai-tap-lon/BaoCao_CoffeeManager.docx`.
- Nội dung có thể sửa: `noi-dung.md`; script dựng Word và sơ đồ: `build_report.py`.
- Sơ đồ PNG/SVG và đặc tả JSON: `artifacts/bao-cao-bai-tap-lon/so-do/`; gói riêng `SoDo_CoffeeManager.zip` để lưu/chỉnh sửa.
- Nguồn đối chiếu: `source-map.md`, `source-manifest.json`, `schema-baseline.json`.
- Danh sách hình và nội dung thiếu: `figure-register.md`, `missing-information.md`.

Word có **61 trang PDF tương ứng khi xuất bằng Microsoft Word**, gồm 10 trang đầu không hiện số và 51 trang từ Chương 1 đến tài liệu tham khảo. Có 22 caption hình/sơ đồ, 43 caption bảng nội dung và một bảng từ viết tắt không đánh số; danh mục bảng/hình tách nhau. Có 13 mục tài liệu tham khảo, phân biệt hồ sơ nội bộ, nguồn học thuật/chính thức và nguồn đối chiếu địa chỉ quán.

## Các bước kiểm tra đã chạy

1. Dùng Python tích hợp đọc schema nguồn, nhận diện đủ 35 bảng baseline và 173 thuộc tính, gồm cột bổ sung trong chính baseline. Ghi PK/FK và trình bày riêng thay đổi migration 001–021. Không kết nối/chạy hoặc nâng cấp SQL Server trong task báo cáo.
2. Dựng DOCX bằng python-docx; sơ đồ sinh xác định thành PNG/SVG, không dùng ảnh AI hoặc hình từ bài mẫu cho mô hình CoffeeManager.
3. Đã thử `render_docx.py` của skill: không có LibreOffice/soffice.exe; log báo thiếu công cụ. Dùng Microsoft Word đã cài để cập nhật field, TOC, phân trang, lưu DOCX và xuất PDF QA. COM trong sandbox bị từ chối quyền; thực hiện bằng lệnh require_escalated được duyệt tự động, không yêu cầu người dùng xác nhận lại.
4. Word báo 3 TablesOfContents và 270 field ở bản cuối, xuất PDF thành công. Khi đóng COM có cảnh báo RPC do đối tượng Word không còn đáp ứng; file đã được Save/Export trước đó, được mở lại bằng bộ đọc độc lập và render đầy đủ. Kiểm tra cuối không thấy process WINWORD còn chạy; không kill tiến trình của người dùng.
5. Render từng trang PDF bằng pypdfium2 ở scale 1,5 thành `qa/page-XX.png`. Đã xem toàn bộ trang qua các ảnh tổng hợp, xem ảnh trang/nguồn chi tiết để rà bìa, bảng, nhãn và quan hệ, và xem lại các trang thay đổi sau lượt chỉnh cuối. Đã sửa font theme của tiêu đề, header bảng bị tách khỏi dòng đầu, đoạn mô tả hình rơi riêng trang, dòng trang trí kế thừa và khoảng trống giữa các sơ đồ quan hệ.
6. `check_document.py` mở lại bản cuối và ghi **18/18 điều kiện cấu trúc đạt** trong `qa/checks.json`: A4 mọi trang; ba section; lề; font Normal TNR13; spacing1,15/6pt/6pt; ba TOC; 65 SEQ; chỉ footer nội dung có PAGE; bắt đầu từ1; kết luận không đánh số chương; không lỗi field; mục lục có kết luận và tham khảo; đủ35 bảng; các FK được vẽ khớp nguồn; snapshot nguồn không đổi; hai trang bìa giống nhau; không có ký tự PDF nằm ngoài trang.

Kiểm tra cấu trúc tự động không thay thế thẩm định nội dung học thuật hoặc khảo sát thực tế. Ảnh PDF kiểm tra nằm trong hồ sơ QA, không phải bản PDF bàn giao được yêu cầu riêng.

## Nội dung đã đối chiếu

- Phạm vi một quán/một kho; tác nhân phần mềm ADMIN/CASH/STORE, không giả cổng thanh toán/API đối tác.
- Sáu nhóm chức năng khớp BFD, bảng phân tích và các gói DFD F01–F10. Mức đỉnh tách thêm hình trao đổi nguồn giữa nhóm; các nút lặp cùng mã là cùng đối tượng.
- Sơ đồ ER có giải thích bội số và cận dưới do nghiệp vụ khác với điều kiện FK. Các sơ đồ quan hệ kiểm FK theo baseline và ngoại lệ bổ sung 011/017.
- Dòng món MaDong, phân bổ MaTT, giá/tiêu hao/ưu đãi snapshot, không cộng trùng chi trả nợ hoặc doanh thu checkout.
- Không kết luận toàn bộ schema vật lý đạt3NF; trình bày phụ thuộc của quan hệ đại diện và giới hạn dữ liệu dẫn xuất.
- Nêu trạng thái G05 38PASS/5FAIL/10BLOCKED/2NOTRUN theo nguồn 27/10; không coi source/build/ảnh lịch sử là nghiệm thu bản hiện tại.
- Bìa: Nguyễn Minh Phú, 6551071065, CQ.65.CNTT, khóa64 đúng người dùng. Tên đề tài Quản lí quán cafe; học phần theo yêu cầu gốc. Khung/logo giữ từ PDF mẫu, toàn bộ chữ bài mẫu đã loại trước khi điền thông tin. PDF nguồn là Letter; chuyển A4 bằng scale đồng dạng, không làm méo khung/logo. Không tuyên bố bìa mới giống100% mọi pixel sau đổi chữ/kích thước.
- Ther Thẩn Cafe tại49 Đường449, Tăng Nhơn Phú theo người dùng, đối chiếu thêm bài đăng được FindGlocal lưu. Không coi dữ liệu demo là dữ liệu quán hoặc giả quán đã sử dụng CoffeeManager.

## Giới hạn cần giữ trong bản bàn giao

- SD01 cơ cấu tổ chức và ghi nhận khảo sát thực tế chưa có dữ liệu xác nhận. Đã có tên/địa chỉ, chưa có biên bản hoặc thông tin nội bộ để điền các phần còn lại.
- Ba ảnh WinForms được chèn là ảnh lịch sử có nhãn ngày. UI03/UI05/UI06/UI07 để chỗ bổ sung cụ thể; không tự tạo ảnh chức năng chưa có hoặc dùng HTML làm ảnh ứng dụng.
- Hai bìa trong Word là nền ảnh chất lượng cao từ PDF đã sửa chữ, giữ hình thức ổn định. Muốn sửa thông tin bìa tiếp thì dùng phần tạo bìa trong script, không sửa trực tiếp chữ trên ảnh; phần nội dung, bảng, mục lục/danh mục vẫn chỉnh sửa được trong Word.
- Không có kết quả đo AI/đạo văn và không cam kết một tỷ lệ phát hiện.
- Không chạy test WinForms/SQL, không sửa source/migration/cấu hình hoặc dữ liệu ứng dụng, không thay trạng thái T01–T32, không commit/push.

Phần công việc biên soạn đã thực hiện tới mức có thể kiểm chứng từ dữ liệu hiện có. Tiếp tục bổ sung các mục thiếu khi có thông tin thật; không yêu cầu duyệt lại phần đã được duyệt nếu vẫn trong phạm vi plan.
