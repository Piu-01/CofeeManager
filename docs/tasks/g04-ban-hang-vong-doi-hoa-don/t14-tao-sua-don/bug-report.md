# Lỗi bộ kiểm thử T14 — đợt G04 ngày 23/10/2026

## Cập nhật24/10/2026

Người dùng duyệt sửa G04-TEST-05 và regression. V2 giữ đầy đủ exception chain, chọn
SqlException trong chuỗi: timeout thực tế-2. Đối chiếu trả một receipt/một header,
session transaction0; `../runs/regression_20261024_testfix_v2/remaining-results.json`.
Lỗi classifier đã sửa; nhánh trước commit/mất kết nối khác vẫn chưa đủ bằng chứng.
Không sửa history v1 hoặc sản phẩm. Các cặp đồng thời được audit lại bằng raw số
do G04-TEST-06 tại T18, không tin riêng chữ PASS của runner Pair.

## G04-TEST-05 — Ca timeout đánh mất loại/mã exception

Trạng thái: chờ duyệt sửa bộ kiểm thử; chưa sửa. Không xác nhận lỗi sản phẩm.

Lệnh trong `runs/acceptance-wave_20261023_195243_f9876e48/remaining.ps1` gọi tạo đơn
HD99000001 rồi WAITFOR 3 giây, CommandTimeout=1. `remaining-results.json` ghi FAIL,
ActualError=null; đối chiếu cùng RequestId `fe47234c-6355-487a-b925-6a9080e1e1f8`
đọc được một receipt và một header. Bằng chứng xác nhận commit/đối chiếu được, nhưng
không đủ xác nhận exception phía client là -2 vì runner không lưu exception đầy đủ.

Runner đi đến InnerException cuối cùng rồi mới đọc Number. Cách này có thể bỏ qua
SqlException và đến Win32Exception không có Number. Đây là nguyên nhân có bằng
chứng từ code; loại exception thực tế cuối chuỗi vẫn cần log bổ sung để chứng minh.

Đề xuất sau duyệt: giữ nguyên FAIL cũ, lưu toàn chuỗi exception và chọn SqlException
trong chuỗi thay vì exception sâu nhất. Chạy lại timeout trước/sau commit, request
replay/mismatch và đối chiếu receipt trên hai kết nối DB test; kiểm transaction sạch.
Không đổi retry hoặc procedure sản phẩm từ kết quả hiện tại. Rủi ro giới hạn ở phân
loại kết quả test; cần tránh đổi expected để coi mọi lỗi kết nối là timeout hợp lệ.

Các fault header/chi tiết/receipt đều PASS, snapshot tiền/kho/điểm/dòng/receipt giữ
nguyên và @@TRANCOUNT/XACT_STATE=0. Các nhánh mất kết nối khác chưa được chứng nhận.
