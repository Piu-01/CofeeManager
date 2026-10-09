# T03 - test results

## Nghiệm thu sau regression được duyệt

Đợt G01_Regression_20261008_221528, source/schema hashes ở manifest nhóm;
SQL localhost 17.0.1000.7, SDK 10.0.401, chưa commit. Lệnh chạy:
`& .\tools\database\Test-ContractMigration.ps1` (exit 1).
9/10 subcase tự động PASS; T03-K03 FAIL do runner B03, không suy diễn mã trùng.

| Ca | Trạng thái | Actual |
|---|---|---|
| T03-K01 | PASS | Seed/empty/late-DDL rollback cả 3 subcase PASS, rows/FK không đổi |
| T03-K02 | PASS | Repeat không đổi sequence; checksum mismatch bị từ chối |
| T03-K03 | FAIL | DataRecordInternal.Dispose ở cleanup reader; chưa có 100 mã đã lưu |
| T03-K04 | PASS | DM999/NV999 hợp lệ; hết giới hạn/nguồn sai lỗi đúng |
| T03-K05 | PASS | Rollback không có invoice, mã sau khác mã đã tiêu thụ |
| T03-K06 | PASS | App user đọc view/gọi procedure qua ownership chain |
| T03-K07 | PASS | Protected DML bị chặn và snapshot không đổi |
| T03-K08 | BLOCKED | Windows app principal/local config thực tế chưa provision |
| T03-K09 | PASS | Kế thừa review contract/schema/Models ở lượt trước, các file này không đổi |

Lịch sử lượt đầu: `runs/G01_20261008_220313-acceptance.md`; JSON/runs cũ giữ nguyên.
K08/K09 NOT RUN phía dưới là trạng thái manual của runner, không thay tổng hợp.

## Output runner regression
Run: 20261008_221536_f995bfc1; database: CoffeeManager_Test_T03_20261008_221536_f995bfc1 / CoffeeManager_Test_T03_20261008_221536_f995bfc1_empty
| Case | Status | Expected | Actual |
|---|---|---|---|
| T03-K01-seed | PASS | Migration preserves all baseline rows and trusted FK. | Assertions passed. |
| T03-K01-empty | PASS | Migration applies to empty schema. | Assertions passed. |
| T03-K01-rollback | PASS | Late DDL failure rolls back object and version. | Assertions passed. |
| T03-K02-repeat | PASS | Repeat returns AlreadyApplied and retains sequence current value. | Assertions passed. |
| T03-K02-checksum | PASS | Same version with altered bytes is refused. | Assertions passed. |
| T03-K03-concurrent | FAIL | Two overlapping sessions return 100 unique HD codes absent from seed. | Method invocation failed because [System.Data.Common.DataRecordInternal] does not contain a method named 'Dispose'. |
| T03-K04-limits | PASS | DM/NV last code accepted, next call fails explicitly; unknown source refused. | Assertions passed. |
| T03-K05-gap | PASS | Rolled back invoice absent; later code differs. | Assertions passed. |
| T03-K06-permissions | PASS | App user reads views and executes existing procedures with ownership chain. | Assertions passed. |
| T03-K07-direct-writes | PASS | All protected table writes fail with permission error and unchanged rows. | Assertions passed. |
| T03-K08 | NOT RUN | Review principals/config/log | Manual review required |
| T03-K09 | NOT RUN | Compare contract/schema/Models | Manual review required |
