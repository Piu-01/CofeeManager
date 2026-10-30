# Lỗi bộ kiểm thử T15 — đợt G04 ngày 23/10/2026

## Trạng thái hiện tại24/10/2026 — G04-CONC-01 đã sửa theo duyệt

Người dùng đã duyệt đợt bổ sung. Actual plan schema015 xác nhận Clustered Index
Scan PK_HOADON trong TR_CTTT_KiemTra, đọc9rows; lượt điều tra tái hiện0/1205.
Migration016 khóa từng affected MaHD theo thứ tự bằng PK seek, rồi kiểm trạng thái
và tổng phân bổ. Không đổi API, không sửa/checksum lại001–015 hoặc tự retry tiền.
Actual plan sau sửa là Clustered Index Seek đọc1row. 10lượt tranh tồn hai chiều
đều0/52203,36session state của18cặp concurrency đều0:0. Cùng request/khác request,
closed/overpayment/mixed batch guard,rollback/kho/điểm/receipt đều đạt trong68ca SQL.

Evidence: `../runs/regression_20261024_concurrency_v3/After/manifest.json`,
`plan-comparison.json`, Before/checkout-trigger-44.sqlplan và After/checkout-trigger-46.sqlplan.
Before giữ nguyên StatusAborted do instrumentation/summarizer của lượt điều tra,
không lấy đó làm kết quả sau sửa; interpretation.md ghi rõ raw/giới hạn. Trong
After source/test đóng băng, không sửa trong lượt test. Development đã lên016
sau backup và số dư/tài khoản không đổi. Không nâng toàn G04PASS vì GUI cònBLOCKED.

Các mục chờ duyệt bên dưới là **lịch sử trước khi người dùng duyệt đợt bổ sung**.

## Cập nhật24/10/2026 — đợt sửa đã được duyệt

G04-TEST-01/02 đã sửa trong `runs/regression_20261024_testfix_v2/`, giữ bản23/10.
Điểm dùng KM0% thay hạng, net999/1.000/1.999→0/1/1 đạt; điểm overflow52205 và
snapshot rollback đạt. Ngày tương lai expected52205 đạt, toàn snapshot/transaction
không đổi. Xem extended-results.json và supplement-results.json. Chưa sửa sản phẩm.

## G04-CONC-01 — Deadlock1205 khi hai đơn tranh lượng tồn cuối

Trạng thái: **chờ duyệt điều tra/sửa bổ sung**; chưa sửa SQL. Phát hiện mới ngoài
năm lỗi bộ kiểm thử đã duyệt. Kết quả ca T15-K11 bị hạ FAIL so expected0/52203;
không lấy RunnerStatusPASS vì G04-TEST-06. Các bất biến sau lỗi vẫn đạt: đúng một
đơn paid, tồn không âm, đơn thua không có payment. Chưa thấy sai số dư hoặc ghi đôi.

Evidence SQL Server thật: `../runs/regression_20261024_testfix_v2/deadlocks-file.xml`,
event system_health chỉ lọc mã HD96000005/06 của DBtest
CoffeeManager_Test_G04_20261024_114856_b5dd5242_demo. VictimHD96000006 đang SELECT
SoLuongTon WITH(UPDLOCK,HOLDLOCK) tại sp_ThanhToanHoaDon. PhiênHD96000005 giữ X trên
PK_NGUYENVATLIEU và chờ RangeS-U trên PK_HOADON trong TR_CTTT_KiemTra; victim giữ
RangeS-U đó và chờ U kho. Vòng chờ chứng minh deadlock là thật, không phải suy từ
mã1205. DAL hiện map1205 thành lỗi xung đột đã rollback, không auto retry.

Nguyên nhân đã chứng minh: đảo phụ thuộc giữa khóa kho và khóa/range hóa đơn của
trigger thanh toán. Giả thuyết cần actual plan xác minh: predicate IN(inserted UNION
deleted) + HOLDLOCK quét/khóa rộng sang đơn khác thay vì khóa đúng affected MaHD.
Không kết luận dữ liệu hỏng; khác expected thiếu kho là lý do cần điều tra theo
contract concurrency, không tự sửa expected để né FAIL.

Đề xuất sau duyệt: lấy actual plan/lock evidence trên fixture lặp được, thu hẹp
khóa trigger về đúng tập MaHD theo PK và thứ tự khóa phù hợp checkout, vẫn kiểm
đơn đóng/tiền vượt trong transaction. Nếu cần thay trigger, dùng migration016 mới,
không sửa baseline/checksum001–015. Không dùng NOLOCK, không bỏ kiểm tiền, không
auto retry chốt tiền. Chốt patch tối thiểu từ bằng chứng, không refactor module khác.
Nếu bằng chứng cho thấy đây là lỗi rollback hợp lệ được contract chấp nhận, ghi
quyết định/ngoại lệ riêng, không gọi ca thực chạy52203 khi thực tế là1205.

Regression: hai đơn tranh một/nhiều NVL với thứ tự món khác nhau, hai checkout
cùng đơn/cùng request, hủy/sửa/giao và bảo vệ DML; kiểm không money/stock/points
partial, transaction0, đơn thua sửa được. Đánh giá cả hai thứ tự và nhiều lượt có
điều phối; giữ source cố định trong mỗi lượt. Rủi ro chính: thay lock scope không
được làm mất khóa tài chính hoặc cho payment vượt payable khi hai session ghi.

## G04-TEST-01 — Fixture điểm dùng gross thay net của khách có hạng

Trạng thái: chờ duyệt sửa test, chưa sửa; chưa phát hiện lỗi tính điểm sản phẩm.
`extended-results.json` giữ FAIL `T15-K08 points floor net excludes ship`.
`scenarios.sql` so điểm với FLOOR(@p/1000), với @p là giá trước giảm, nhưng KH001
có hạng BKIM giảm 10%. Ca điều tra độc lập trong `remaining-results.json` ghi:
HD97000005 tổng 1.000, giảm 100, net 900, điểm thực tế 0; khớp FLOOR(net/1000).
Không sửa expected/fixture cũ để xóa FAIL.

Đề xuất sau duyệt: fixture riêng có net định trước 999/1.000/1.999, hoặc tính expected
từ net đã thiết kế độc lập sau giảm; có hạng/KM thay hạng, ship và overflow. Ghi đủ
header, history, số dư và expected từng ca. Không chỉnh công thức SQL đang cho kết
quả đúng. Rủi ro: nếu dùng chính fn_CheckoutPreview làm oracle sẽ kiểm tra vòng tròn;
phải đặt fixture và số tiền expected cụ thể trước chạy.

## G04-TEST-02 — Ca ngày tương lai đòi sai mã lỗi

Trạng thái: chờ duyệt sửa test, chưa sửa. `scenarios.sql` đòi 51108 nhưng migration011
quy định 52205 tại `IF @date<@created`. `extended-results.json` ghi SQL error52205,
“Ngày thanh toán trước lập đơn.” Runtime đã chặn đúng điều kiện ngày. Không đổi
procedure về mã cũ để khớp test. FAIL vẫn giữ nguyên; transaction/số dư của nhánh
này chưa được chứng nhận đầy đủ do assertion kết thúc sớm.

Đề xuất sau duyệt: expected52205 theo contract hiện hành, snapshot đầy đủ trước/sau,
kiểm đơn còn mở, không payment/tiêu hao/history/receipt và transaction0; thêm gọi
chữ ký cũ/backdate qua runtime. Rủi ro nằm ở coverage test, không thay contract tiền.

Nguồn bằng chứng: `../runs/acceptance-wave_20261023_195243_f9876e48/` gồm script đã
đóng băng, kết quả SQL thật và truy vấn điều tra. Hai lỗi này thuộc cùng đợt duyệt
bộ kiểm thử G04; không đề nghị vá sản phẩm khi chưa có lỗi sản phẩm được chứng minh.
