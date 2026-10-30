# G04 — Đã xử lý đợt bổ sung24/10/2026

**Đang làm, chưa nghiệm thu toàn G04.** Người dùng đã duyệt G04-TEST-06/G04-CONC-01;
hai vấn đề đã sửa và regression. Checklist56ID hiện **28PASS,0FAIL,28BLOCKED**.
Không commit/push,không đổi API/C#/UI hoặc checklist T01–T32/325ID.

- **G04-TEST-06:** fixture đọc rowversion binary8 trực tiếp trên SQL; validator so
  count/từng int, có negative102/102 và đảo thứ tự. Không còn dựa chuỗi truthy.
- **G04-CONC-01:** actual plan trước sửa xác nhận Clustered Index Scan9rows trong
  trigger thanh toán. Migration016 đổi sang khóa từng affected MaHD theo PK seek,
  sau đó kiểm trạng thái/phân bổ, giữ51002/51003 và transaction. Plan sauSeek1row.
  Không sửa/checksum lại001–015,khôngNOLOCK hoặc retry tiền tự động.

| Kiểm chứng | Kết quả |
|---|---|
| SQL regression sau sửa |68/68PASS |
| Concurrency trong68ca |18cặp,36session transaction0:0;10vòng tranh tồn hai chiều0/52203;4vòng giao0/52103;checkout cùng request0/0,khác request0/52102;sửa dòng0/52103;tranh bàn0/52104 |
| Bất biến/guard |Một commit/receipt,không nhân tiền/kho/điểm;đơn thua không partial;paid UPDATE/DELETE/INSERT bị chặn;overpayment và mixed batch rollback |
| Clean migration/rerun/checksum |3/3PASS,baselineclean001–016,016AlreadyApplied,checksum khác bị từ chối vàtrigger không đổi |
| Unit |Kế thừa52/52PASS của v2,không phải lượt chạy unit mới;C# không đổi |
| Development |015→016 thành công sau COPY_ONLY CHECKSUM; tồn,điểm/hạng,hóa đơn,tài khoản không đổi |

[Manifest tổng](runs/regression_20261024_concurrency_v3/manifest.json),
[68ca SQL](runs/regression_20261024_concurrency_v3/After/manifest.json),
[plan trước/sau](runs/regression_20261024_concurrency_v3/plan-comparison.json),
[clean/rerun/checksum](runs/regression_20261024_concurrency_v3/Empty/manifest.json),
[development016](runs/development016_20261024_132735_d5f3a71d/manifest.json).
Lượt Before giữ nguyên raw0/1205/actualplans và StatusAborted do instrumentation/
summary của điều tra; interpretation.md ghi rõ. Không lấy status đó làm kết quả
regression sau sửa. Source/test của lượt After giữ cố định; artifacts v1/v2 không sửa.

Biên bản đủ56ID: [T14](t14-tao-sua-don/test-results.md),[T15](t15-checkout/test-results.md),
[T16](t16-pos-huy-don/test-results.md),[T17](t17-tra-cuu-in/test-results.md),
[T18](t18-giao-hang/test-results.md). [Hai bug đã xử lý](additional-bug-batch.md).
Lịch sử v2/FAIL/PASS sai được giữ trong
[report-before-concurrency-fix](runs/regression_20261024_concurrency_v3/report-before-concurrency-fix.md)
và các thư mục v1/v2; không đổi expected để né lỗi.

**28ca BLOCKED** còn cần chức năng GUI đầy đủ: POS tạo/mở/reload,nháp,keyboard,
unknown/refresh,phiên/quyềnUI và in. Computer Use đã có quyền đọc/control SetValue,
nhưng click không có geometry vàTab không chứng minh focus;không nâng keyboard/
handler lênPASS từ trạng thái đứng yên. Các subcase timeout trước commit,policy/
recipe hai thứ tự đồng thời,legacy-invalid chưa đủ bằng chứng. Không có lỗi GUI
sản phẩm được chứng minh để sửa. Hình thức UI/bản in **SKIPPED** theo phân công;
DPI100/150% **NOT RUN**,không đổi WindowsScale. Không dùngTEST bypass làm chứng cứlogin.

Đợt này tạo3DBtest (Before/After/Empty),dọn3,còn0;không fixture vào development.
Backup mới trước016 vàbackup trướcG04 cũ tiếp tục giữ đến nghiệm thu/người dùng chấp
nhận development. Không dừng app người dùng,không đổi service/login/WindowsScale.
[F5/dùng thử](development-guide.md) hiện dùng đúng schema016. Đợt sửa bổ sung đã
hoàn thành,không còn bug đã duyệt đang chờ sửa;G04 vẫn chưaHoàn tất khi28ca cònBLOCKED.
