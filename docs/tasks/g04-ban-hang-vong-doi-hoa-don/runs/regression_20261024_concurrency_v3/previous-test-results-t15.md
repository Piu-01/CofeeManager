# Biên bản T15 — regression24/10/2026

Người dùng đã duyệt sửa5lỗi bộ kiểm thử và chạy bù ngày24/10. V1 giữ nguyên lịch sử;
source sản phẩm/schema015 không đổi. Các nhánh mới dưới đây thay trạng thái hiện
tại theo evidence, không sửa expected/checklist T01–T32.

[Manifest v2](../runs/regression_20261024_testfix_v2/manifest.json),
[SQL extended](../runs/regression_20261024_testfix_v2/extended-results.json),
[audit raw concurrency](../runs/regression_20261024_testfix_v2/concurrency-audit.json),
[SQL còn lại](../runs/regression_20261024_testfix_v2/remaining-results.json),
[SQL chạy bù](../runs/regression_20261024_testfix_v2/supplement-results.json),
[52unit TRX](../runs/regression_20261024_testfix_v2/unit/g04-regression.trx),
[GUI control actions](../runs/regression_20261024_testfix_v2/gui-actions.json).
SQL/script/JSON lưu đầy đủ dữ liệu, lệnh, expected và actual của subcase. Chỉ dựa
raw actual để kết luận, không dựa PASS sai của Pair. Kết quả kế thừa v1 xem
[biên bản trước](../runs/regression_20261024_testfix_v2/previous-test-results-t15.md).

| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| T15-K01 | [I] Bill hợp lệ trả 1/nhiều phương thức và nhiều dòng cùng phương thức chốt đúng một lần. | Một payment và hai dòng cùng PT chốt đúng; replay không tăng kho/điểm/payment. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T15-K02 | [I] SQL tái tính ưu đãi theo chính sách; preview thay đổi thì trả lỗi xác nhận lại và không thu theo số cũ. | Token đổi do KM/policy/khách/đối tác kể cả tiền giữ nguyên:52201; không ghi thanh toán, toàn snapshot rollback. Hai thứ tự policy/recipe đồng thời chưa chạy bổ sung. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T15-K03 | [I] Thiếu/vượt phân bổ, phương thức không có/PT010 bị từ chối và rollback. | Thiếu/dư1₫, unknown/PT010 bị52202; snapshot tiền/kho/điểm/receipt không đổi. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T15-K04 | [I] Giảm 100%, ship 0 và TVP rỗng: chốt 0 VND hợp lệ, tiêu hao đúng, không dòng thanh toán 0. | KM100% ship0 chốt được bằng TVP rỗng; có tiêu hao, không payment. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T15-K05 | [I] Giảm 100% nhưng ship >0: vẫn phải trả đủ phí ship. | KM100% ship15.000: thu15.000, không lịch sử điểm. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T15-K06 | [I] Thiếu một nguyên liệu, món thiếu công thức/ngừng bán: rollback toàn kho/tiền/điểm/trạng thái. | Thiếu NVL/ngừng bán/thiếu recipe đều chặn checkout, snapshot và transaction rollback, đơn mở còn nguyên. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T15-K07 | [I] Nhiều dòng cùng SP/NVL: tiêu hao group đúng, không trừ hai lần; decimal 4 chữ số lẻ giữ đúng. | HaiSP chung2NVL,hai dòng cùngSP: tiêu hao1.2885/4.7035, tồn9998.7115/9995.2965; đổi recipe sau chốt không đổi snapshot. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T15-K08 | [I] Điểm floor sau giảm, không cộng ship/phí đối tác; mỗi MaHD chỉ cộng một lần. | Fixture KM0% thay hạng, net999/1000/1999→0/1/1; ship không tính điểm; overflow52205 rollback; replay không nhân history. G04-TEST-01 đã sửa. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T15-K09 | [I] Phí đối tác 0/100% và số lẻ làm tròn đúng; đổi tỷ lệ đối tác không đổi đơn cũ. | Phí0%/100% và50%×25.001 làm tròn12.501; ship không trừ; đổi tỷ lệ không đổi đơn paid. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T15-K10 | [I] Hai session checkout cùng đơn: một commit; session còn lại không tăng tiền/điểm/trừ kho nữa. | Hai kết nối cùng đơn:0/52102; chỉ một payment/history. Replay cùng RequestId không ghi lặp. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T15-K11 | [I] Hai đơn dùng lượng tồn cuối: chỉ giao dịch đủ tồn commit; không tồn âm. | Raw v2:0/1205 thay0/52203; Pair ghi PASS sai, audit hạ FAIL. Đồ thị deadlock khóa kho↔HOADON đã lưu; invariants một paid/tồn không âm/đơn thua không payment vẫn đạt. Chờ G04-CONC-01. | FAIL | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T15-K12 | [I] Lỗi cưỡng bức sau tiêu hao/sau ghi tiền/trước điểm rollback tất cả và @@TRANCOUNT về trạng thái hợp lệ. | Fault sau tiêu hao/payment/điểm: toàn header/dòng/tiền/kho/điểm/snapshot/receipt như trước, transaction0. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T15-K13 | [I] Gọi trực tiếp sửa TrangThai/GiamGia/tiêu hao thay procedure bằng quyền app bị chặn. | User WITHOUT LOGIN thuộc CafeManagerApp bị229 khi DML6bảng; STORE sales bị51605. Không là bằng chứng login runtime. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T15-K14 | [I] Ngày thanh toán trước lập đơn bị chặn; ngày mua/import dùng đường quản trị riêng. | Ngày tương lai52205 đúng contract; toàn snapshot không đổi,transaction0. Expected51108 cũ giữ FAIL lịch sử; không sửa sản phẩm. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T15-K15 | [I] Chạy lại seed baseline hoặc bản seed migration đã ghi nhận thay đổi: tổng/snapshot/điểm khớp hợp đồng mới. | Clean/demo001–015 apply được, LegacyUnknown không bịa tỷ lệ; rerun/checksum/preflight lỗi chưa chạy nghiệm thu. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |

Chức năng GUI đầy đủ/phiên/login/revoke, hai thứ tự policy/recipe đồng thời, timeout
trước commit, preflight legacy-invalid/rerun/checksum chưa đủ bằng chứng: NOT RUN
ở các subcase tương ứng. Không nâng task Hoàn tất từ PASS vài subcase.
Hình thức UI/bản in SKIPPED theo phân công; DPI100/150%NOT RUN, agent không đổi Scale.
Các vấn đề mới G04-TEST-06/G04-CONC-01 chờ duyệt bổ sung tại additional-bug-batch.md.


