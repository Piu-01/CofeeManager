# T25 — Kết quả nghiệm thu G06

Đợt được duyệt30/10/2026, tổng hợp31/10/2026. SDK10.0.401; SQL Server localhost, baseline+seed/migrations001–024; DB test riêng có marker. Source chưa commit, SHA256 trước/sau không đổi (src/database/tests/tools). Không nâng development. Hình thức SKIPPED theo phân công; DPI100/150% NOT RUN; app component thực DeviceDpi120.

Bằng chứng/lệnh/input/expected/actual chi tiết: [runner acceptance](../runs/acceptance_20261030_155806_b10d6581/manifest.json), [extended](../runs/extended-20261030/results.json), [bổ sung](../runs/extended-20261030/supplement/results.json), [SQL followup](../runs/extended-20261030/transaction-followup-v2.json), [native trace](../runs/extended-20261030/native-trace.json), [audit](../runs/extended-20261030/final-audit.json). Các script/project đặt cạnh kết quả để tái hiện. Expected gốc dưới đây giữ nguyên.

| ID | Expected gốc | Actual | Kết quả |
|---|---|---|---|
| T25-K01 | [I/U] Giờ vào/ra hợp lệ cùng ngày và qua đêm cho thời lượng đúng. | Cùng ngày8h, qua đêm8h; typed giờ và tổng thời lượng khớp. | PASS |
| T25-K02 | [I] Giờ ra không có giờ vào, ra≤vào, ngày vào khác NgayLam bị chặn. | Ra thiếu vào, bằng/trước vào, khác NgayLam và subsecond đều bị từ chối, không ghi. | PASS |
| T25-K03 | [I] Nghỉ phép/Vắng có giờ bị chặn; đang làm chưa ra được lưu nhưng không tự tính thành kỳ hoàn tất. | Nghỉ/vắng có giờ bị chặn; nghỉ/vắng không giờ lưu được; thiếu giờra phân biệt Incomplete, chặn chốt. | PASS |
| T25-K04 | [I] Cùng nhân viên trùng MaCa/NgayLam bị PK chặn; khác ca chồng khoảng giờ bị procedure chặn. | Trùngkey và overlap rollback; bổ sung interval chứa/một phần/qua ngày/mở đều bị chặn. | PASS |
| T25-K05 | [I] Ca kết thúc đúng lúc ca sau bắt đầu được phép, không coi là overlap. | Ranh giới liền kề được lưu; chồng1giây bị chặn. | PASS |
| T25-K06 | [I] Hai session cùng nhập ca chồng giờ không cùng vượt kiểm tra. | Hai session có barrier thật, quan sát2waiters: đúng1success,1conflict; chỉ1dòng cuối. | PASS |
| T25-K07 | [I] Nhân viên đã nghỉ/ca không tồn tại bị chặn thao tác mới; dữ liệu cũ đọc được. | NVnghỉ/ca không tồn tại không ghi mới; dữ liệu trước nghỉ vẫn đọc được. | PASS |
| T25-K08 | [I] Chỉnh chấm công kỳ đã chốt lương bị chặn theo quy trình. | Kỳ legacy và snapshot mới đều chặn insert/update/delete; kiểm trực tiếp SQL/quyền runtime và service. | PASS |
| T25-K09 | [U/I] Phân kỳ theo ngày làm đã chọn, qua đêm cuối tháng không bị cộng hai lần. | Ca30/09 qua01/10: tháng9=8h, tháng10=0dòng; không cộng hai lần. | PASS |
| T25-K10 | [M] Bộ lọc tháng/nhân viên/trạng thái và reload đúng; quyền ADMIN, không quyền kế thừa từ chức vụ. | Filter tháng/NV/status và role bằng typedservice đúng; native bộ lọc/reload chưa thao tác được. | BLOCKED |
| T25-K11 | [I] Một batch nhiều nhân viên có dòng lỗi rollback theo phạm vi batch đã công bố. | Batch nhiềuNV hợp lệ commit; FK/overlap/trùngkey một dòng rollback toàn batch và receipt. | PASS |

Lịch sử trước nghiệm thu được giữ tại runs/extended-20261030; không đổi expected để né lỗi. PASS là phạm vi ca gốc đã kiểm ở tầng được nêu; không suy native/hình thức PASS từ service/component. Các nhánh native chưa chạy giữ BLOCKED. Xem [báo cáo nhóm](../report.md) để biết giới hạn và đợt bug đề nghị.

## Cập nhật sau đợt debug đã duyệt31/10/2026

G06-B01/B02 đã sửa, [regression chung](../regression-b01-b02.md):78/78subcase SQL/BLL/component PASS và14/14unit PASS. Lượt đầu75PASS/3FAIL của harness được lưu riêng, không ghi đè. Mỗi lượt nguồn cố định; chỉ3fileG06 được sửa trong đợt. Các ca native còn thiếu vẫn BLOCKED, không suy PASS từ formatter/component. Trạng thái mới G06:26PASS/0FAIL/7BLOCKED; lịch sử trước debug nằm trong thư mục run.
