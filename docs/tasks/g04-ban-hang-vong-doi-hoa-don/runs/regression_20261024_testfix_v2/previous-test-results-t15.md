# Biên bản nghiệm thu T15 — đợt23/10/2026

Người dùng đã duyệt cả đợt bằng 'duyệt'. SQL Server localhost thật, DB test riêng
schema001–015; SDK10.0.401. Source đóng băng tại [manifest](../runs/acceptance-wave_20261023_195243_f9876e48/manifest.json),
không sửa sản phẩm/migration/test đã chạy. Chưa nghiệm thu hoàn tất.

Evidence: [SQL subset +18unit](../runs/acceptance_20261023_195253_7d540db5/manifest.json),
[SQL mở rộng](../runs/acceptance-wave_20261023_195243_f9876e48/extended-results.json), [phần còn lại/điều tra](../runs/acceptance-wave_20261023_195243_f9876e48/remaining-results.json),
[native binding/print](../runs/acceptance-wave_20261023_195243_f9876e48/native-checkout-print.json), [34unit regression](../runs/acceptance-wave_20261023_195243_f9876e48/regression/regression.trx).
SQL và dữ liệu/expected/actual từng subcase lưu nguyên trong JSON và script cùng thư mục.
Operator Windows identity không thay bằng chứng login/quyền runtime; ca DML dùng user WITHOUT LOGIN riêng.

PASS dưới đây chỉ cho checklist gốc đã đủ bằng chứng; subcase mở rộng trong plan chưa chạy vẫn nêu rõ.
FAIL fixture không tự chuyển thành PASS của sản phẩm. BLOCKED là phần chưa đủ do bộ kiểm thử/luồng GUI.

| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| T15-K01 | [I] Bill hợp lệ trả 1/nhiều phương thức và nhiều dòng cùng phương thức chốt đúng một lần. | Một payment và hai dòng cùng PT chốt đúng; replay không tăng kho/điểm/payment. | PASS | JSON/script liên kết trên; lọc Id tương ứng |
| T15-K02 | [I] SQL tái tính ưu đãi theo chính sách; preview thay đổi thì trả lỗi xác nhận lại và không thu theo số cũ. | KM đổi revision dù giữ tỷ lệ:52201, không payment. Policy/khách/đối tác thay đồng thời chưa đủ. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T15-K03 | [I] Thiếu/vượt phân bổ, phương thức không có/PT010 bị từ chối và rollback. | Thiếu/dư1₫, unknown/PT010 bị52202; snapshot tiền/kho/điểm/receipt không đổi. | PASS | JSON/script liên kết trên; lọc Id tương ứng |
| T15-K04 | [I] Giảm 100%, ship 0 và TVP rỗng: chốt 0 VND hợp lệ, tiêu hao đúng, không dòng thanh toán 0. | KM100% ship0 chốt được bằng TVP rỗng; có tiêu hao, không payment. | PASS | JSON/script liên kết trên; lọc Id tương ứng |
| T15-K05 | [I] Giảm 100% nhưng ship >0: vẫn phải trả đủ phí ship. | KM100% ship15.000: thu15.000, không lịch sử điểm. | PASS | JSON/script liên kết trên; lọc Id tương ứng |
| T15-K06 | [I] Thiếu một nguyên liệu, món thiếu công thức/ngừng bán: rollback toàn kho/tiền/điểm/trạng thái. | Thiếu NVL rollback toàn snapshot; ngừng bán52204. Nhánh thiếu recipe chưa chạy riêng. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T15-K07 | [I] Nhiều dòng cùng SP/NVL: tiêu hao group đúng, không trừ hai lần; decimal 4 chữ số lẻ giữ đúng. | Ba ly ở hai dòng group theo recipe đúng, tồn trừ một lần. Chưa đủ nhiềuSP chungNVL và định mức1.2345. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T15-K08 | [I] Điểm floor sau giảm, không cộng ship/phí đối tác; mỗi MaHD chỉ cộng một lần. | Fixture so điểm theo gross sai với hạng10%; điều tra net900→0 điểm đúng. G04-TEST-01, không sửa FAIL. | FAIL | JSON/script liên kết trên; lọc Id tương ứng |
| T15-K09 | [I] Phí đối tác 0/100% và số lẻ làm tròn đúng; đổi tỷ lệ đối tác không đổi đơn cũ. | Phí0%/100% và50%×25.001 làm tròn12.501; ship không trừ; đổi tỷ lệ không đổi đơn paid. | PASS | JSON/script liên kết trên; lọc Id tương ứng |
| T15-K10 | [I] Hai session checkout cùng đơn: một commit; session còn lại không tăng tiền/điểm/trừ kho nữa. | Hai kết nối cùng đơn:0/52102; chỉ một payment/history. Replay cùng RequestId không ghi lặp. | PASS | JSON/script liên kết trên; lọc Id tương ứng |
| T15-K11 | [I] Hai đơn dùng lượng tồn cuối: chỉ giao dịch đủ tồn commit; không tồn âm. | Hai đơn tranh tồn cuối:0/52203; đúng một paid, tồn không âm, đơn thua không có payment. | PASS | JSON/script liên kết trên; lọc Id tương ứng |
| T15-K12 | [I] Lỗi cưỡng bức sau tiêu hao/sau ghi tiền/trước điểm rollback tất cả và @@TRANCOUNT về trạng thái hợp lệ. | Fault sau tiêu hao/payment/điểm: toàn header/dòng/tiền/kho/điểm/snapshot/receipt như trước, transaction0. | PASS | JSON/script liên kết trên; lọc Id tương ứng |
| T15-K13 | [I] Gọi trực tiếp sửa TrangThai/GiamGia/tiêu hao thay procedure bằng quyền app bị chặn. | User WITHOUT LOGIN thuộc CafeManagerApp bị229 khi DML6bảng; STORE sales bị51605. Không là bằng chứng login runtime. | PASS | JSON/script liên kết trên; lọc Id tương ứng |
| T15-K14 | [I] Ngày thanh toán trước lập đơn bị chặn; ngày mua/import dùng đường quản trị riêng. | Ngày tương lai bị chặn đúng52205 nhưng test đòi51108: G04-TEST-02; nhánh hậu điều kiện chưa phủ đủ. | FAIL | JSON/script liên kết trên; lọc Id tương ứng |
| T15-K15 | [I] Chạy lại seed baseline hoặc bản seed migration đã ghi nhận thay đổi: tổng/snapshot/điểm khớp hợp đồng mới. | Clean/demo001–015 apply được, LegacyUnknown không bịa tỷ lệ; rerun/checksum/preflight lỗi chưa chạy nghiệm thu. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |

Hình thức UI/bản in: **SKIPPED theo phân công**, người dùng tự kiểm. DPI100/150%: **NOT RUN**.
Native probe DeviceDpi120=125% chỉ có phạm vi binding/bounds/preview; không có thao tác UI từ fixture compile lỗi.
Các nhánh UI/UX,100đổi module/10login-logout, late query, quyền/phiên/revocation, fault ưu đãi, policy/recipe
đồng thời hai thứ tự, preflight legacy-invalid/rerun/checksum và mất kết nối trước commit chưa được chứng nhận.
Không tick checklist gốc trong plan. Các phần thiếu phải chạy bù sau duyệt đợt sửa bộ kiểm thử.


