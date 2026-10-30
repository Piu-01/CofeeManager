# Kế hoạch biên soạn báo cáo bài tập lớn CoffeeManager

Ngày lập: 27/10/2026. **Plan được người dùng duyệt bằng lời nhắn “duyệt”; đã triển khai bản thảo theo phạm vi.** Thông tin bìa/cơ sở đã bổ sung từ phản hồi người dùng trong lúc soạn; phần còn thiếu theo missing-information.md. Đây là công việc tài liệu độc lập, không thay đổi backlog hoặc nghiệm thu phần mềm.

## 1. Đầu ra và phạm vi

Biên soạn một báo cáo Word theo `requirements.md`, sử dụng bố cục tham khảo đã ghi trong `reference-review.md`. Tên tạm: **Phân tích và thiết kế hệ thống thông tin quản lý quán cà phê CoffeeManager**.

File dự kiến:

- `artifacts/bao-cao-bai-tap-lon/BaoCao_CoffeeManager.docx`: tài liệu chỉnh sửa được, đầy đủ trường mục lục/danh mục/caption.
- `artifacts/bao-cao-bai-tap-lon/so-do/`: sơ đồ kỹ thuật và nguồn chỉnh sửa được.
- `docs/tasks/bao-cao-bai-tap-lon/source-map.md`: truy vết luận điểm, nghiệp vụ, schema và phiên bản bằng chứng.
- `docs/tasks/bao-cao-bai-tap-lon/figure-register.md`: danh sách hình, vị trí, nguồn, tình trạng sử dụng hoặc cần bổ sung.
- `docs/tasks/bao-cao-bai-tap-lon/missing-information.md`: thông tin chưa được xác nhận.
- `docs/tasks/bao-cao-bai-tap-lon/document-review.md`: kết quả kiểm tra nội dung, định dạng và các giới hạn còn lại.
- Nội dung nguồn và script dựng tài liệu, nếu cần, đặt trong hồ sơ task; ảnh render kiểm tra đặt trong thư mục QA riêng, không coi là đầu ra báo cáo.

PDF hoàn chỉnh chỉ bàn giao thêm khi người dùng cần. `bia-mau-nguyen-ban.pdf` đang có là tài sản tham khảo, chưa phải bìa đề tài mới. Không sửa ứng dụng/SQL, không chạy test phần mềm hoặc ghi dữ liệu DB, không commit/push. Không cần giao việc cho agent khác.

## 2. Các bước thực hiện sau duyệt

### Bước 1 Chốt nguồn và dữ kiện

Đọc lại phần nghiệp vụ liên quan trong kế hoạch, UX, baseline/seed, migrations hiện có và source theo từng use case; đối chiếu báo cáo và biên bản mới nhất. Ghi ngày, phiên bản hoặc hash file cần thiết trong `source-map.md` để phân biệt thiết kế, source đã triển khai và kết quả đã kiểm chứng. Đọc thêm `docs/ui/README.md` và tài liệu liên quan khi cần giải thích thiết kế UI; không thiết kế lại giao diện.

Chỉ mục kế hoạch đã thay đổi trong quá trình chuẩn bị: G05 ngày 27/10 có 38 PASS/5 FAIL/10 BLOCKED/2 NOT RUN, khác báo cáo 26/10. Khi viết phải đọc biên bản mới nhất, không lấy ghi nhận cũ làm kết quả hiện tại hoặc suy nhóm đã hoàn tất.

Lập `missing-information.md` cho bìa và khảo sát. Nếu chưa được cung cấp, tiếp tục các chương có nguồn và để dấu chờ bổ sung tại đúng vị trí; không tự tìm một quán trên Internet rồi coi là nơi tác giả đã khảo sát. Không gán số lượng nhân viên/doanh thu/thời gian phỏng vấn từ seed.

**Kiểm tra:** mỗi kết luận về phạm vi, triển khai hoặc kiểm thử có nguồn phù hợp; không có credential hoặc dữ liệu cá nhân không cần thiết trong nội dung dự kiến. Chưa có dữ kiện thì ghi thiếu, không đổi thành giả định thực tế.

### Bước 2 Lập đề cương chi tiết và viết nội dung nguồn

Tạo bản nội dung có thể sửa theo đúng thứ tự các phần trước chương, bốn chương, Kết luận và kiến nghị, Tài liệu tham khảo. Giữ các mục bắt buộc nguyên thứ tự.

| Phần | Nội dung sẽ viết | Nguồn/cách kiểm tra |
|---|---|---|
| Bìa và phần đầu | Đề tài, thông tin xác nhận; lời mở đầu về lý do và phạm vi; trang nhận xét để trống; các danh mục tự động. | Không mang thông tin tác giả mẫu sang bìa mới. |
| Chương 1 | Giới thiệu và tổ chức cơ sở khảo sát. | Dữ liệu người dùng xác nhận; thiếu thì để bản nháp và ghi vị trí cần bổ sung. |
| Chương 2 | BFD, DFD/cân bằng mức, ER, mô hình quan hệ/chuẩn hóa; nền tảng WinForms/SQL Server và kiến trúc vừa đủ. | Đọc nguồn học thuật/chính thức; ghi tác giả, xuất bản, URL/ngày truy cập. Không dùng bài mẫu làm nguồn lý thuyết. |
| Chương 3 | Bài toán, chức năng, tác nhân/hồ sơ, BFD, bảng phân tích và DFD các mức. | Nghiệp vụ và phạm vi dự án; đồng nhất mã tiến trình/kho/luồng. |
| Chương 4 | Thực thể/thuộc tính, ER, quan hệ, chuẩn hóa và giao diện đã xây dựng. | SQL kết hợp migrations, source DAL/BLL/UI và bằng chứng đúng phiên bản. |
| Kết luận | Kết quả có căn cứ, giới hạn triển khai/kiểm chứng, kiến nghị trong phạm vi đề tài. | Không tuyên bố hệ thống đủ vận hành toàn bộ hoặc hoàn tất T01–T32. |

Lý thuyết dùng diễn giải riêng và trích dẫn [n] thống nhất; không tự gán năm xuất bản hoặc trích sách chưa đọc. Chỉ nêu các công nghệ thực dùng; không biến chương lý thuyết thành tài liệu cài đặt.

**Kiểm tra:** đủ mục yêu cầu, không lặp lý thuyết kéo dài, không trộn dữ liệu hai bài mẫu, không viết đã khảo sát/triển khai khi chưa có căn cứ.

### Bước 3 Xây dựng mô hình chức năng và dữ liệu

Thiết lập bảng tác nhân, tiến trình, kho dữ liệu và luồng làm nguồn chung trước khi vẽ. Dùng công cụ sơ đồ xác định, xuất SVG/PNG đủ nét; lưu nguồn có thể chỉnh sửa. Chốt ký pháp và tên mức trong chương lý thuyết, không sao lại các nhầm lẫn BFD/DFD ở mẫu.

| Mã làm việc | Vị trí | Sơ đồ dự kiến | Điều kiện |
|---|---|---|---|
| SD01 | 1.2 | Tổ chức cơ sở khảo sát | Chỉ vẽ từ thông tin xác nhận; chưa có thì ghi chỗ chèn, không lấy role phần mềm làm cơ cấu thật. |
| SD02 | 3.2 | BFD tổng thể | Một gốc, các nhóm công việc và phân rã thống nhất phạm vi; nhận diện phần chưa triển khai. |
| SD03 | 3.4.1 | DFD ngữ cảnh | Một tiến trình hệ thống, tác nhân và luồng qua ranh giới; không vẽ API bên ngoài không có. |
| SD04 | 3.4.2 | DFD mức đỉnh | Khớp BFD/bảng phân tích; kho dữ liệu không nối trực tiếp tác nhân. |
| SD05 | 3.4.3 | DFD dưới đỉnh bán hàng/thanh toán | Đơn mở, dòng món, ưu đãi, phân bổ, chốt và kết quả; theo nghiệp vụ SQL đã đối chiếu. |
| SD06 | 3.4.3 | DFD dưới đỉnh kho và công nợ | Nhập, xuất, kiểm kê và trả nợ; chia hình khi cần để đọc được trên A4. |
| SD07 | 3.4.3 | DFD dưới đỉnh khách hàng/ưu đãi | Khách, hạng, chính sách và lịch sử điểm theo contract hiện có. |
| SD08 | 3.4.3 | DFD dưới đỉnh các nhóm còn lại của mức đỉnh | Phân rã khi cần giải thích, chỉ theo nghiệp vụ được ghi nhận; nhận diện rõ thiết kế chưa xây dựng. |
| SD09 | 4.1.2 | ER khái niệm | Lực lượng/cận dưới dựa nghiệp vụ, NULL, FK, UNIQUE; không gọi sơ đồ bảng là ER khái niệm. |
| SD10 | 4.1.3 | Mô hình quan hệ theo phân hệ | Đủ lược đồ/PK/FK; phần lớn chia hình có đầu mối liên kết, không ép toàn schema thành ảnh nhỏ. |
| SD11 | 4.2 | Kiến trúc chương trình | UI → BLL → DAL → SQL, Models dùng chung, khớp tham chiếu project thực. |

Mã trên phục vụ quản lý hình; số caption chính thức tạo tự động theo chương khi dựng Word. Số lượng hình có thể tăng để tách sơ đồ lớn mà không thay phạm vi.

Đối chiếu schema sau các migration, không chỉ baseline. Lập từ điển dữ liệu và giải thích cách chuyển ER sang quan hệ. Chọn quan hệ đại diện có ý nghĩa để trình bày phụ thuộc hàm/1NF–3NF; chỉ kết luận mức chuẩn hóa khi đã phân tích, giải thích snapshot giá/tiêu hao/chính sách và computed columns theo nghiệp vụ.

**Kiểm tra:** cân bằng luồng giữa mức cha/con; tên luồng là dữ liệu; mỗi tiến trình có dữ liệu vào/ra có ý nghĩa; FK và lực lượng đúng; dữ liệu đã triển khai và thiết kế đích được nhận diện riêng. Không sửa SQL để phù hợp hình vẽ.

### Bước 4 Chọn ảnh chương trình và dự trù vị trí ảnh thiếu

Kiểm kê ảnh có sẵn trong `docs/tasks/*/runs/`, đọc ngữ cảnh bằng chứng và xem ảnh trước khi chọn. Ưu tiên bản native modal đúng quyết định ngày 26/10, không lấy ảnh host nhúng đã thay thế làm hiện trạng. Ảnh cũ chỉ dùng nếu nội dung còn đúng và có nhãn phiên bản cần thiết; thiếu thì ghi vị trí để bổ sung.

| Vị trí | Nhóm ảnh dự kiến | Nội dung giải thích |
|---|---|---|
| 4.2.1 | Đăng nhập, MainForm và POS | Cửa sổ chính, điều hướng theo vai trò, vùng bán hàng. |
| 4.2.2 | Danh sách/editor sản phẩm hoặc nguyên liệu, khách hàng | Tìm/chọn/cập nhật, validation và ràng buộc; chọn ví dụ đại diện, không dàn hết mọi tab tương tự. |
| 4.2.3 | Thanh toán, chi tiết hóa đơn/preview, giao hàng | Dữ liệu đầu vào, chốt tiền, kết quả và in sau commit. |
| 4.2.3 | Nhập/xuất/kiểm kê/công nợ | Chỉ dùng ảnh thực có, giải thích đúng giới hạn G05; không coi ảnh là chứng nhận nghiệm thu. |

Mỗi mục trong `figure-register.md` có nguồn, ngày/phiên bản, caption, mục đặt, kích thước dự kiến và trạng thái đã chọn/cần bổ sung. Với trang A4/lề đã chốt, chiều rộng nội dung tối đa 15,5 cm; hình toàn chiều rộng ưu tiên khoảng 14,5–15,5 cm, giữ tỷ lệ. Ảnh thiếu có ghi chú cụ thể ngay sau đoạn dẫn, bao gồm màn, trạng thái/dữ liệu demo cần hiển thị và caption dự kiến. Không tạo ảnh chấm công, lương hoặc báo cáo chưa triển khai.

**Kiểm tra:** không có ảnh bài mẫu hoặc HTML mô phỏng bị gắn nhãn WinForms; chữ đọc được; hình và lời văn cùng chức năng/phiên bản; không chứa bí mật. Không chạy chụp ảnh/test UI trong phạm vi plan này.

### Bước 5 Dựng Word và xử lý bìa

Nạp lại skill documents/PDF khi thực hiện, dùng runtime tích hợp và đánh dấu thao tác tạo artifact theo skill. Dựng nội dung A4, TNR 13, dòng 1,15, trước/sau 6 pt, lề trái 3,5 cm và các lề khác 2 cm. Tạo section không số cho phần đầu, section Chương 1 bắt đầu số 1; caption trường SEQ và các danh mục tự động. Dùng footnote thật nếu cần chú thích. Không đánh số chương cho Kết luận và kiến nghị.

Bìa giữ khung/logo/bố cục từ mẫu đã chọn nhưng thay đúng thông tin báo cáo mới. Hai trang nguồn đã được kiểm tra giữ hình ảnh nguyên vẹn khi tách; độ trùng đó không đồng nghĩa bản sau thay chữ sẽ tự đạt 100%. Kiểm kích thước trang nguồn vì ảnh render 918×1188 tại scale 1,5 cho thấy cần kiểm lại MediaBox trước khi dùng cho A4. Ưu tiên dựng bìa chỉnh sửa được trong Word, tái sử dụng logo nguồn khi phù hợp và được xác nhận; đối chiếu render với mẫu. Nếu không thể giữ hình thức bằng Word, dùng nền bìa xử lý từ PDF kết hợp nội dung đúng, ghi rõ phần nào là nền ảnh; không để nguyên tên tác giả/đề tài mẫu hoặc làm mất yêu cầu A4.

**Kiểm tra:** mở được DOCX, kiểu chữ/lề đúng, các trường tự động tồn tại và hiển thị đúng sau cập nhật, bìa ngoài/bìa trong đồng nhất, không có số trang La Mã, không mang outline/danh mục cũ từ PDF mẫu.

### Bước 6 Kiểm tra và bàn giao tài liệu

Kiểm tra nội dung theo requirements, đối chiếu chéo mã/tên trong BFD–bảng phân tích–DFD, PK/FK giữa ER–lược đồ–SQL và lời văn–hình. Kiểm nguồn tham khảo khớp trích dẫn và loại thông tin không có căn cứ.

Render Word bằng workflow của skill, xem từng trang và sửa lỗi bảng/hình/chữ/trang/footnote rồi render lại. Kiểm trang mục lục/danh mục sau cập nhật trường; không chỉ kiểm cấu trúc XML. Nếu môi trường không render/cập nhật trường được, điều tra công cụ sẵn có, ghi rõ giới hạn và không báo bản đã kiểm trang hoàn chỉnh. Đây là kiểm tra chất lượng tài liệu trong bước biên soạn, không mở đợt test WinForms/SQL và không áp dụng kết quả cho T01–T32.

Lưu `document-review.md`, bàn giao link Word và các sơ đồ cần thiết; không đưa ảnh QA vào danh sách đầu ra. Khi thiếu dữ liệu bìa/khảo sát/ảnh bắt buộc, báo bản nháp và liệt kê đúng phần cần bổ sung; không gọi bản sẵn sàng nộp.

**Kiểm tra:** hoàn thành tất cả mục có thể xác minh, thông tin còn thiếu được công khai, không hứa điểm phát hiện AI/đạo văn hoặc đánh dấu phần mềm đã PASS từ việc viết báo cáo.

## 3. Thông tin còn cần từ người dùng

- Bìa: trường/bộ môn, giảng viên, tên/mã sinh viên, lớp/khóa, địa điểm/năm và tên đề tài xác nhận. Có thể xác nhận các mục giống mẫu và chỉ cung cấp mục khác; không tự suy từ bài mẫu.
- Khảo sát: tên quán, địa chỉ, cơ cấu tổ chức và ghi nhận vận hành thực tế. Chưa có thì Chương 1 giữ chỗ bổ sung, không dựng dữ kiện.

Không cần gửi lại hai PDF. Thông tin này có thể bổ sung trong lúc viết các phần có căn cứ sau khi plan được duyệt.

## 4. Điểm duyệt

Theo `.agents/skills/task-approval-workflow/SKILL.md`, Bước 3 yêu cầu: “Dừng lại, chờ người dùng duyệt plan. KHÔNG bắt đầu code ở bước này.” Vì vậy hiện chỉ lập plan; sau duyệt sẽ thực hiện trọn các bước biên soạn/kiểm tra tài liệu trong phạm vi trên, không xin duyệt từng chương.
