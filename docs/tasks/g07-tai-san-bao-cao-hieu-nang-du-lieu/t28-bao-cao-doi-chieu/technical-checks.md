# T28 — Kiểm kỹ thuật trong triển khai

31/10/2026. Chưa nghiệm thu13ID. Migration026 và source SQL→DAL→BLL→UI đã triển khai; đủ contract kỹ thuật tiếp tục T29.

- SQL Server thật localhost, clean/demo001–026+rereun026 qua runner Test-G07.ps1; không đổi số dư tiền/kho/điểm/nợ/lương, mỗi lượt dọn hết DBtest.
- Typed smoke tại ../runs/technical_20261031_100226_13088085/:chín loại báo cáo, metadata/totals/page và nguồn dòng đầu, options/dashboard qua runtime G07Runtime thành công. Xem typed-smoke.json và manifest.json. Build probe0warning/0error.
- Các lượt100028/100126 giữ lỗiSQL8124 của aggregate tương quan tồn; lượt100202 giữ lỗi alias phát sinh khi chỉnh. Đã chỉnh trong pha triển khai, chưa là bug nghiệm thu. Lượt100226 xác minh contract đã qua; không ghi đè artifact cũ.
- Không chạy unit suite hoặc nghiệm thu/native/hình thức. Agent SKIPPED hình thức; native/DPI chức năng NOT RUN, chưa có PASS T28-Kxx.

## Bổ sung kiểm kỹ thuật source hiện tại

- Lượt benchmark101005 và102036 dựng đủ10.000bill rồi lỗiSQL229 ở view nội bộ `vw_ReportStockEvents`. Lượt102036 có RawInventoryError trong results.json ghi đúng object/schema/DB; fixture nhỏ có nhập và kỳ ngày hữu hạn trước đó qua. Không suy nguyên nhân engine sâu hơn khi chưa có bằng chứng.
- Đặt context OWNER trong module đọc có demand ADMIN (Read/SourceList); runtime chỉ được EXECUTE endpoint, không grant SELECT view. Kiểm lại source026 tại ../runs/technical_20261031_103131_cd0ce2a8/: clean/demo+rereun026, bảo toàn số dư, mapping chín loại báo cáo/kỳ hữu hạn và9999, options/dashboard/source đều qua; SourceUnchanged=true, tạo2/dọn2/còn0.
- UI technical probe trên WinForms thật ghi DeviceDpi120: asset binding11dòng, report binding rỗng đúng kỳ chọn; bounds grid dương tại1280×720 và1366×768; native owner bị khóa khi modal tài sản mở, titlebar/ShowInTaskbar/AcceptButton đúng. Không thao tác click/Tab/Enter nghiệp vụ, không ghi PASS UX hoặc hình thức từ probe; NativeInput NOT RUN, Visual SKIPPED.
- Lượt102919 có typed-smoke Succeeded nhưng manifest Failed do runner ghi đè biến snapshot nguồn bằng chuỗi snapshot số dư. CSV trước/sau không khác; đã sửa tên biến trong runner và kiểm lại103131 qua. Giữ nguyên artifact cũ, không sửa kết quả lịch sử.
- Metadata pager của list tài sản/picker/source được gắn vào từng collection kết quả bằng OrbitPageRows để query cũ không thay tổng của query mới qua biến closure chung. Đây là thay đổi trong phần triển khai G07, chưa nghiệm thu các ca async/native.


## Cập nhật sau đợt test31/10

Các mục trên là bản ghi lịch sử pha triển khai. Đợt test riêng đã được duyệt và chạy; xem [test-results.md](test-results.md) và [báo cáo nhóm](../report.md):23PASS/1FAIL/9BLOCKED. G07-B01 chờ duyệt sửa; native còn thiếu bằng chứng. Không dùng trạng thái lịch sử 'chưa chạy/chờ duyệt test' thay kết quả hiện tại.
