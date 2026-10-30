# Instrumentation SQL bổ sung — v1/v2

V1 chưa gọi được procedure sản phẩm: CONVERT(varchar,rowversion,1) trả byte ký tự, không trả hex; SQL dựng header gặp syntax102 và thiếu@actor137. V1results và script giữ nguyên.

Đã kiểm độc lập cùngconnection: CONVERT(varchar(18),CONVERT(binary(8),SecurityStamp),1) trả hex hợp lệ; SELECT CONCAT(@@TRANCOUNT,':',XACT_STATE()) trả0:1 của autocommitSELECT, trong khi chụp @@TRANCOUNT/XACT_STATE vào biến trướcSELECT trả0:0 và không có explicittransaction. V2 dùng cách đo đã có ở Expect-TestSqlError của DatabaseTestTools.ps1.

Expected nghiệp vụ/mã lỗi/rollback0:0 không đổi; không sửa source/migration. Kết quả v1 là lỗi instrumentation, không dùng nângPASS hoặc kết luận lỗi sản phẩm. V2 tách file/log riêng.

Concurrencyv1 chưa thực thi: PowerShell coi từ Parallel làkeyword, parser yêu cầu block. V2 đổi tên hàm RunTwoSessions, không đổi lệnh SQL/expected; giữscriptv1.

Timeoutv1–v3: sau CommandTimeout dưới EXECUTE AS, client có thể Closed hoặc còn báo Open nhưng session đã kill-state. Không dùng session đó để SELECT transaction. V4 giữ error timeout-2, dùng connection giám sát/DMV xác nhận không còn transaction, receipt/hash đầy đủ và loại riêng timeout connection khỏi pool của harness. 8/8 PASS. Không sửa driver hay source app, chưa thay kiểm timeout native/pending.

Supplementv1 bị runtime DENY SELECT DANHMUC_THUCHI do harness đọc bảng trực tiếp trong impersonation. V2 lấy mã danh mục bằng fixture connection rồi chỉ gọi procedure bằng runtime. Hai record FAIL K02 của V2 dùng PN đã có nợ nên unique chặn trước nhánh số tiền; không thay expected chấp nhận2627. V3/V4 tạo nguồn độc lập hợp lệ trước thử0/vượt tổng. V3 dừng ở query instrumentation dùng sai cột SoLuong; V4 đối chiếu SoLuongXuat đúng schema. Log và phiên bản cũ giữ nguyên; đây không phải các fix source sản phẩm.

Script nâng development có FOR JSON cấp ngoài/ExecuteScalar cắt chunk ở MoneySources: ghi G05-B04, không tự vá tool trong lượt test. Bản sao backup trước nâng dùng hash đầy đủ phía SQL so với live:8/8 khớp; copy dọn. Hash rollback của các harness SQL mở rộng dùng SELECT (SELECT ... FOR JSON), không phải FOR JSON cấp ngoài nên không có giới hạn chunk này.

CleanupV1 có một tên placeholder không tồn tại trong query audit bổ sung; thao tác DROP chỉ dùng đúng DB demo và marker. Giữ CleanupAcceptanceV1.ps1 làm lịch sử, bản CleanupAcceptance.ps1 bỏ placeholder, chưa chạy lại vì tài nguyên đã dọn. Audit cuối resource-audit.json đọc5 tên thực từ manifest (2 acceptance +3 copy backup), kiểm tất cả còn0; dùng audit này làm kết luận tài nguyên.

Không ghi nhận lỗi “coordinate geometry unavailable”/cached AX/keyboard target của công cụ thành bug sản phẩm. Giá trị money1234 đã đọc lại và đối chiếu SQL; ghi chú không nhập thành công giữNULL, không tuyên bố đã gõ được. Header kiểm kê rỗng là fixture âm dùng owner; khoản nợ chưa trả là fixture hợp lệ qua procedure.

Kiểm hồ sơ cuối:55 expected gốc không đổi;38PASS/5FAIL/10BLOCKED/2NOTRUN;304 file source0 thay đổi (final-audit.json). git diff --check báo blank line EOF ở một số file đang thay đổi từ trước lượt test và cảnh báo LF/CRLF; không dọn source trong lượt test chỉ để làm sạch check. Không báo git diff --check PASS.
