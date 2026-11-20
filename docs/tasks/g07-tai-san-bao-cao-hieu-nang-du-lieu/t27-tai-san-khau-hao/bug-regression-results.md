# G07-B01 — Biên bản sửa và regression

31/10/2026 · **PASS trong phạm vi bug đã duyệt.** Người dùng duyệt sửa và regression bằng “duyệt” sau báo cáo G07-B01; không xin duyệt lại từng ca. G07 chưa Hoàn tất vì9ID native còn BLOCKED.

## Source và môi trường

- Migration `027_asset_write_lock.sql` trên schema026, SQL Server localhost17.0.1000.7 Developer, SDK10.0.401, Windows. DBtest clean/demo có marker; runtime G07Runtime/CafeManagerApp và actor ADMIN/CASH/STORE; không chạm development.
- Helper nội bộ lấy applock transaction độc quyền `CafeManager:Asset:<MaTS>`, chuẩn hóa hoa/thường. AssetWrite Create lấy mã rồi khóa trước INSERT; Update/Delete khóa trước hàng/range. FinanceCreate có MaTS lấy cùng khóa trước kiểm tra nguồn/ghi phiếu. Không thêm retry hoặc thay quyền/revision/request contract.
- [migration-diff-audit.json](migration-diff-audit.json):025/026 SHA256 giữ nguyên; hai procedure chỉ thêm3lời gọi khóa, ngoài helper mới; không index/isolation/NOLOCK hay C# sản phẩm thay đổi.
- [source-final.csv](../source-final.csv) là source thực, HEAD nền8a38f8691d37089feec149b71a8f5718d37167f3 + working tree. Không coi HEAD là toàn bộ source chạy. Checksum migration tại demo/clean-migrations.jsonl của lượt.

## Lệnh và bằng chứng

`tools/database/Test-G07.ps1 -RunAcceptance -Regression -ThroughVersion 27`

[regression_20261031_200422_dd2b716e](../runs/regression_20261031_200422_dd2b716e/manifest.json): Succeeded, SourceUnchanged=true; clean/demo001–027+rereun027, số dư tiền/kho/điểm/nợ/lương qua migration không đổi; tạo2/dọn2/còn0.

| Ca | Input/thao tác | Expected | Actual |
|---|---|---|---|
| G07-B01-R01 | Ca xóa/chi gốc10lượt | Chi thắng→xóa53402; xóa thắng→chi52602; không1205 | 10/10 PASS trong139ca cũ |
| R02 | 10delete/expense +10update/delete +10update/update, hai connection, barrierXLOCK hàng; quan sát2waiter rồi nhả | Kết quả nghiệp vụ/revision đúng; không1205; thắng córeceipt/thua khôngreceipt; row/phiếu/FK khớp; trước/sautransaction0:0 | 30/30 PASS, từng outcome/snapshot trongasset-lock-regression.json |
| R03 | Owner chỉ giữ applockA; ghi chi B và khôngMaTS bằng hai connection khác | Hoàn tất trong5s, không bị khóaA, transaction0:0 | PASS |
| R04 | Owner giữ khóa tài sản, FinanceCreate dùng MaTS chữ thường | Cùngkhóa chuẩn hóa; timeout52603~10s,0:0/khôngphiếu/receipt; nhảkhóa và xác minh chưaghi rồi chủ động gửi lại cùngpayload/request1lần | PASS;10.019,8ms timeout, sau đối chiếu gửi lại7,3ms |
| R05 | AssetWrite replay, thaypayload, ADMINkhácactor; FinanceCreate replay | Khôngghi2lần; sai payload/actor51608; transaction0:0 | PASS |
| R06 | 3warm+30samples create/update/expense; xóa tài sản có chi sau mỗi lượt | Ghi thành công, xóa53402, khôngdở transaction | PASS; rawsamples ởwrite-cost-b01.json |
| R07 | 139ca tích hợp cũ,3fixtureSQL,15unit | Không regression quy tắc/quyền/replay/fault rollback/assets/report/kho | 139/139,3/3,15/15 PASS |

[34ca chuyên biệt](../runs/regression_20261031_200422_dd2b716e/asset-lock-regression.json), [139ca cũ](../runs/regression_20261031_200422_dd2b716e/extended-results.json), [TRX](../runs/regression_20261031_200422_dd2b716e/g07-unit.trx).30race kho checkout/checkout/nhập/xuất thuộc139ca cũng PASS. Không lấy receipt từ tên/giá suy ra kết quả; đối chiếu request GUID và dữ liệu thật.

## Số đo ghi và giới hạn

| Đường ghi | Median ms | P95 ms |
|---|---:|---:|
| Create qua service+Get | 29,74 | 36,91 |
| Update rawprocedure | 4,69 | 5,49 |
| Ghi chi rawprocedure | 5,23 | 6,15 |

[write-cost-b01.json](write-cost-b01.json) giữ3warm+30samples/loại trên demoDB riêng. TimerCreate bao gồm service quyền/đọc lại; Update/Expense đo thực thi command, không cộng mởconnection/probe transaction. Đây là số đo sau sửa; không có baseline cùngsnapshot để khẳng định phần trăm overhead, không so ba đường khác nhau hoặc gọi là UItiming. Không chạy lại toàn bộ benchmark10.000bill vì không đổi index/report/checkout; dữ liệu hiệu năng cũ vẫn là lịch sử schema026.

Build probeDebug tronglượt và [Release toànsolution](../build-release-b01.log):0warning/0error. UI sản phẩm không sửa; hình thứcSKIPPED,100/150%NOTRUN.9ID native/timeoutUI/pending/querymuộn vẫn BLOCKED, không nâng thànhPASS từ regressionSQL.

## Lịch sử phép đo và dọn tài nguyên

Lượt [200218](../runs/regression_20261031_200218_8ec08ae7/manifest.json) có139PASS nhưng34FAIL phép đo XACT_STATE trực tiếp, đã giữ nguyên. [Đối chứng connection mới](transaction-measurement.md) tách lỗi harness khỏi procedure; sửa phép đo giữa lượt, expected0:0 và migration027 không đổi. Lượt200422 chụp trạng thái vào biến trước SELECT, trước/sau trên cùngconnection rồi chạy lại toàn bộ; không bỏ check transaction/receipt hoặc chấp nhận1205.

Cleanup exact tên+marker cho cảhai lượt, mỗi lượt2/2DBdọn/0giữ. [resource-audit](../resource-audit.json) kiểm sys.databases cuối; không có DBtest G07 giữ lại. Không nâng development, không commit/push. Lịch sử10deadlockgraph/FAILschema026 vẫn ởbug-report và artifact cũ.
