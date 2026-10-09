# T03 - test results

## Mới nhất09/10/2026 — T03-K08 PASS

Đã provision Windows login/user riêng trên DB test manual; sysadmin0/db_owner0/
app role1. Process Win32.GetOwner PID30036 và SQL session65 cùng Windows account
LAPTOP-79UNRIRC\CafeManagerApp, app tải10 bàn. Config local ignored, Windows auth,
user tự nhập password ở hộp thoại tại máy; không credentials vào source/log.
T03 hiện9/9 PASS; chưa nghiệm thu toàn nhóm vì T05-K07 FAIL B08.
Bằng chứng runs/G01_Manual_20261008_225305_cebd98ad của nhóm; biên bản tại
../t05-dal-nen-kiem-thu/manual-test-results.md. K08 BLOCKED phía dưới là lịch sử.

## Tổng hợp mới nhất — B03 đã duyệt và regression PASS

Run `20261008_222434_ca0e775e`, đợt nhóm `G01_Regression_20261008_222427`.
SQL localhost 17.0.1000.7, SDK 10.0.401, source chưa commit/fixed theo manifest
nhóm (source-stability count=0). Lệnh: Test-ContractMigration.ps1.
**10/10 subcase SQL PASS**. DB seed/empty được ghi ở output dưới đây;
progress/results và danh sách 100 mã concurrency trong `runs/` cạnh file.

| Ca | Trạng thái | Expected / Actual |
|---|---|---|
| T03-K01 | PASS | Seed rows/FK giữ nguyên, empty apply và late-DDL rollback PASS |
| T03-K02 | PASS | Repeat không đổi sequence; checksum mismatch bị từ chối |
| T03-K03 | PASS | Hai session cấp 100 mã unique, đúng HD+8 chữ số, không va seed; cleanup không lỗi |
| T03-K04 | PASS | DM999/NV999 và lỗi hết range/nguồn sai đúng |
| T03-K05 | PASS | Rollback không có invoice/mã sau không tái dùng |
| T03-K06 | PASS | App user đọc view/gọi procedures ownership chain |
| T03-K07 | PASS | Protected DML bị chặn, snapshot không đổi |
| T03-K08 | BLOCKED | Windows app principal/local config thực tế chưa provision |
| T03-K09 | PASS | Kế thừa review contract/schema/Models không đổi |

Lịch sử trước sửa B03: `runs/G01_Regression_20261008_221528-acceptance.md`.
Runner NOT RUN K08/K09 bên dưới không thay review/tổng hợp ở mục này.

## Output runner
Run: 20261008_222434_ca0e775e; database: CoffeeManager_Test_T03_20261008_222434_ca0e775e / CoffeeManager_Test_T03_20261008_222434_ca0e775e_empty
| Case | Status | Expected | Actual |
|---|---|---|---|
| T03-K01-seed | PASS | Migration preserves all baseline rows and trusted FK. | Assertions passed. |
| T03-K01-empty | PASS | Migration applies to empty schema. | Assertions passed. |
| T03-K01-rollback | PASS | Late DDL failure rolls back object and version. | Assertions passed. |
| T03-K02-repeat | PASS | Repeat returns AlreadyApplied and retains sequence current value. | Assertions passed. |
| T03-K02-checksum | PASS | Same version with altered bytes is refused. | Assertions passed. |
| T03-K03-concurrent | PASS | Two overlapping sessions return 100 unique HD codes absent from seed. | Assertions passed. |
| T03-K04-limits | PASS | DM/NV last code accepted, next call fails explicitly; unknown source refused. | Assertions passed. |
| T03-K05-gap | PASS | Rolled back invoice absent; later code differs. | Assertions passed. |
| T03-K06-permissions | PASS | App user reads views and executes existing procedures with ownership chain. | Assertions passed. |
| T03-K07-direct-writes | PASS | All protected table writes fail with permission error and unchanged rows. | Assertions passed. |
| T03-K08 | NOT RUN | Review principals/config/log | Manual review required |
| T03-K09 | NOT RUN | Compare contract/schema/Models | Manual review required |
