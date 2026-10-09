# T05 - test results

## Nghiệm thu sau regression được duyệt

G01_Regression_20261008_221528, SQL localhost 17.0.1000.7, SDK 10.0.401,
source chưa commit/fixed theo manifest nhóm. Runner tạo DB seed/migration được,
nhưng dừng ở stderr xUnit (B04). Chạy trực tiếp dotnet test --no-build bằng
Start-Process trên DB test **mới**, stdout/stderr riêng và logger direct.trx:
**15/20 PASS, 5 FAIL, 0 skipped**; bao gồm 7/7 unit PASS.
DB chính xác ở `../runs/G01_Regression_20261008_221528/t05-direct-database.txt`.

| Ca | Trạng thái | Expected / Actual |
|---|---|---|
| T05-K01 | FAIL | Unicode/injection PASS; O'Brien Unknown, chưa roundtrip thành công |
| T05-K02 | FAIL | Unit precision/length PASS; roundtrip thiếu một subcase do B06 |
| T05-K03 | PASS | Empty result khác connection failure, xUnit PASS |
| T05-K04 | PASS | TVP 0/1/3 và type sai PASS; unit validation PASS |
| T05-K05 | FAIL | 2 rollback subcase bị DBNull snapshot trước procedure (B05) |
| T05-K06 | FAIL | Checkout success Unknown (B06); chưa xác nhận session 0:0 |
| T05-K07 | BLOCKED | UI tải chậm/hủy/đóng và principal app riêng chưa kiểm chứng |
| T05-K08 | FAIL | Test log combined dừng ở known-error assertion; chưa đủ assertions cuối |
| T05-K09 | FAIL | Unknown/timeout assertions qua được, known error Unknown thay 51109 |
| T05-K10 | PASS | Guard 4 targets/cleanup sentinel và 10 HD seed PASS |

Bằng chứng: direct/direct.trx, test-summary.txt, t05-direct-output/error.txt,
t05-sql-diagnostics.txt ở runs nhóm regression. Không sửa source trong lượt;
B04/B05/B06 chờ duyệt bổ sung. Lịch sử lượt đầu giữ ở
`runs/G01_20261008_220313-acceptance.md` và JSON/runs cũ.

## Output runner (không thu đủ tests vì B04)
Run: 20261008_221556_329fb1d8; DB: CoffeeManager_Test_T05_20261008_221556_329fb1d8; dotnet exit: 
| Test | Outcome | Actual |
|---|---|---|
Manual UI/Designer/DPI/log/guard evidence: NOT RUN until recorded separately. Automated assertions do not substitute for WinForms manual acceptance.
Setup blocker: [xUnit.net 00:00:00.92]     CafeManager.Tests.Integration.DataAccessTests.ParametersRoundTripWithoutExecutingInput(text: "O'Brien") [FAIL]
