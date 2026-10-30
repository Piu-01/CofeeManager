# PC03 — Chuẩn bị in lần đầu qua Microsoft Print to PDF

## Kết quả cuối

Người dùng báo xong, agent tìm thấy file thực ở
C:/Users/Lenovo/OneDrive/Máy tính/Documents/G04-HD20250421.pdf (không phảiDownloads).
Đã copy nguyên file/hash vào run. PDF1trang, headerCOFFEE MANAGER·HD20250421,
khôngnhãnINLẠI; SP001,1×81000=81000, ghi chúOO pagination fixture, giảm0,
ship0,phảitrả81000, Tien mat81000. Không dựng100000/19000 chưa lưu lênchứngtừ.
UIaccessibilityxácnhận POSnháp vàĐãgửiyêucầuin, thu81000/thối19000/điểm81.
SQLpaid,1PT00181000, KH0110→81và1lịchsử+81, khoNVL01−0.018/NVL03−0.010khớp.
208sourcefilekhôngđổi. PC03PDF vàT17-K04PASS; G0435PASS/0FAIL/21BLOCKED.

Scriptđầu giữ7/8: check hardcode“Tiền mặt” códấu khôngkhớpName danh mụcSQL
“Tien mat”. Đốichiếuđộc lập với PHUONGTHUCTT/PT001xácnhận tên/sốtiềnPDFđúng
dữliệuthật; không sửa expected/filekếtquả7/8 hoặc dữliệuSQL để néfail.
catalog-label-verdict.json giữfollow-up vàpayment-label-source.json lànguồn.
Một querychẩnđoánnhầmtênbảngPHUONGTHUC_TTđãbáoInvalidobject, đọc lại schema
PHUONGTHUCTT rồi chỉSELECT đúng; không là lỗi sản phẩm/không cóDML.
Text extraction/sốtrang không là kiểmthịgiác; hìnhthứcdo người dùngxácnhận.

Artifacts: pdf-results.json,catalog-label-verdict.json,sql-verdict.json,
after-user-done-ui.json,pdf-provenance.json,final-manifest.json vàbảnPDFnguyên.
Các đoạnchờfile dưới đây là lịch sử trước khi tìmđược đườngdẫntừKnownFolder.

25/10/2026. Đợt test hỏiin/reset đã duyệt và người dùng yêu cầu kiểm tiếp.
Người dùng chưa có máy in giấy nên chưa xác nhận output củaHD20250418; không
đánh PASS hoặc lỗi sản phẩm từ thiếu máy in.

Agent đọc inventory Windows (read-only): đã càiMicrosoft Print to PDF vàOneNote.
Lệnh Get-Printer trong sandbox bị từ chối CIM; gọi read-only có quyền đã thành
công, không cài hoặc đổi cấu hình máy in, không tự gửi job. Evidenceinstalled-printers.json.

Chọn đơn còn mởHD20250421 trongDBtestcómarker đúng. Snapshot before-checkout.json
đầy đủ bằng SELECT(scalarFORJSON): Chua thanh toan,81000,KH011điểm0, chưapayment.
Lưu toàn bộ kho/tiêuhao/payment/lịchsửđiểm vàsource208file trước thao tác.

Đã hướng dẫn người dùng F8→100000→chốt→Cóin→MicrosoftPrinttoPDF→SaveDownloads/
G04-HD20250421.pdf. Chờ file vàxácnhậnPOSnháp. Không chốt lại418đãpaid.
PC03vẫnPENDING, chưaclaimfile/máyin/tiềnkhođiểmđãđạt.

Skill PDF dùng read-only để kiểm text/nội dung/sốtrang sau khi filexuấthiện;
không tạo/sửaPDF hoặc renderảnhQA/hìnhthức. pypdf từbundledruntimeđãsẵn;
inspect_pdf.py đã chuẩn bị, chưa chạy với chứng từ vì file chưa có.
Không suy chất lượng giấy/máy in thật từ MicrosoftPrinttoPDF.
