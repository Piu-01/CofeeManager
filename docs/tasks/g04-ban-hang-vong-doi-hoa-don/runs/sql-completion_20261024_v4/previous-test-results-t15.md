# Biên bản T15 — bổ sung24/10/2026, schema016

Người dùng đã duyệt sửa G04-TEST-06/G04-CONC-01; đã xử lý và regression.
[68ca SQL](../runs/regression_20261024_concurrency_v3/After/manifest.json), [plan trước/sau](../runs/regression_20261024_concurrency_v3/plan-comparison.json),
[clean/rerun/checksum](../runs/regression_20261024_concurrency_v3/Empty/manifest.json).
Các ca kế thừa xem [biên bản v2](../runs/regression_20261024_concurrency_v3/previous-test-results-t15.md).
Giữ expected/checklist gốc; không nâng task Hoàn tất khi cònBLOCKED.

| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| T15-K01 | [I] Bill hợp lệ trả 1/nhiều phương thức và nhiều dòng cùng phương thức chốt đúng một lần. | Một payment và hai dòng cùng PT chốt đúng; replay không tăng kho/điểm/payment. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T15-K02 | [I] SQL tái tính ưu đãi theo chính sách; preview thay đổi thì trả lỗi xác nhận lại và không thu theo số cũ. | Token đổi do KM/policy/khách/đối tác kể cả tiền giữ nguyên:52201; không ghi thanh toán, toàn snapshot rollback. Hai thứ tự policy/recipe đồng thời chưa chạy bổ sung. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T15-K03 | [I] Thiếu/vượt phân bổ, phương thức không có/PT010 bị từ chối và rollback. | Thiếu/dư1₫, unknown/PT010 bị52202; snapshot tiền/kho/điểm/receipt không đổi. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T15-K04 | [I] Giảm 100%, ship 0 và TVP rỗng: chốt 0 VND hợp lệ, tiêu hao đúng, không dòng thanh toán 0. | KM100% ship0 chốt được bằng TVP rỗng; có tiêu hao, không payment. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T15-K05 | [I] Giảm 100% nhưng ship >0: vẫn phải trả đủ phí ship. | KM100% ship15.000: thu15.000, không lịch sử điểm. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T15-K06 | [I] Thiếu một nguyên liệu, món thiếu công thức/ngừng bán: rollback toàn kho/tiền/điểm/trạng thái. | Thiếu NVL/ngừng bán/thiếu recipe đều chặn checkout, snapshot và transaction rollback, đơn mở còn nguyên. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T15-K07 | [I] Nhiều dòng cùng SP/NVL: tiêu hao group đúng, không trừ hai lần; decimal 4 chữ số lẻ giữ đúng. | HaiSP chung2NVL,hai dòng cùngSP: tiêu hao1.2885/4.7035, tồn9998.7115/9995.2965; đổi recipe sau chốt không đổi snapshot. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T15-K08 | [I] Điểm floor sau giảm, không cộng ship/phí đối tác; mỗi MaHD chỉ cộng một lần. | Fixture KM0% thay hạng, net999/1000/1999→0/1/1; ship không tính điểm; overflow52205 rollback; replay không nhân history. G04-TEST-01 đã sửa. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T15-K09 | [I] Phí đối tác 0/100% và số lẻ làm tròn đúng; đổi tỷ lệ đối tác không đổi đơn cũ. | Phí0%/100% và50%×25.001 làm tròn12.501; ship không trừ; đổi tỷ lệ không đổi đơn paid. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T15-K10 | [I] Hai session checkout cùng đơn: một commit; session còn lại không tăng tiền/điểm/trừ kho nữa. | Hai kết nối cùng đơn:0/52102; chỉ một payment/history. Replay cùng RequestId không ghi lặp. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T15-K11 | [I] Hai đơn dùng lượng tồn cuối: chỉ giao dịch đủ tồn commit; không tồn âm. | Migration016:10lượt tranh tồn hai chiều đều0/52203; một paid/receipt, đơn thua không money/consumption/snapshot, tồn không âm vàsession0:0. Actual planScan9→Seek1. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T15-K12 | [I] Lỗi cưỡng bức sau tiêu hao/sau ghi tiền/trước điểm rollback tất cả và @@TRANCOUNT về trạng thái hợp lệ. | Fault sau tiêu hao/payment/điểm: toàn header/dòng/tiền/kho/điểm/snapshot/receipt như trước, transaction0. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T15-K13 | [I] Gọi trực tiếp sửa TrangThai/GiamGia/tiêu hao thay procedure bằng quyền app bị chặn. | User WITHOUT LOGIN thuộc CafeManagerApp bị229 khi DML6bảng; STORE sales bị51605. Không là bằng chứng login runtime. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T15-K14 | [I] Ngày thanh toán trước lập đơn bị chặn; ngày mua/import dùng đường quản trị riêng. | Ngày tương lai52205 đúng contract; toàn snapshot không đổi,transaction0. Expected51108 cũ giữ FAIL lịch sử; không sửa sản phẩm. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T15-K15 | [I] Chạy lại seed baseline hoặc bản seed migration đã ghi nhận thay đổi: tổng/snapshot/điểm khớp hợp đồng mới. | Clean001–016 và demo nâng được;016rerun AlreadyApplied/checksum khác bị chặn, legacy snapshot giữ. Preflight legacy-invalid và nhánh seed cũ chưa đủ nghiệm thu. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |

GUI còn giới hạn click/focus như hồ sơ v2; chưa kiểm đủ POS/keyboard/nháp/unknown/refresh/phiên/in.
Hình thức UI/bản inSKIPPED theo phân công;100/150%NOT RUN, không đổiWindowsScale.
Timeout trước commit,policy/recipe đồng thời hai chiều,legacy-invalid vẫn chưa đủ bằng chứng.
016clean/rerun/checksum và18cặp concurrency đã chạy; không suy thành PASS các nhánh chưa chạy.
Nguồn/expected/actual/phiên/cleanup trong manifest; test/source giữ cố định trong lượt After.


