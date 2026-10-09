# T05 - test results

## Chốt regression B10

[b10-test-results.md](b10-test-results.md): B10 và regression B09 **PASS** trên
bản sau Save Designer, Debug/Release0 warning/0 error, DPI100/125/150,
keyboard/scroll/minimize PASS, source cố định Count0. 10/10 mã bắt buộc PASS
kế thừa; G01 đủ kiểm chứng kỹ thuật, chờ cho phép commit bàn giao.

## B09 sau duyệt — kết quả mới nhất

[b09-test-results.md](b09-test-results.md): giới hạn DPI và keyboard trước Save
đạt; Designer Save viết lại source gây **FAIL B10** (mất header/style, layout
grid ở 150%, warning CS0649). Đã kiểm bản sau Save, không coi PASS trước Save
là kết quả bản hiện tại. B10 chờ duyệt bổ sung; G01 vẫn Đang làm.

## Mới nhất — bổ sung UI/runner

[supplement-test-results.md](supplement-test-results.md): B04 capturefailurePASS
(latest raw JSON19/20 do faultfixture có chủ đích). DPI150 **FAIL B09**, không
thu Form được về720/768px; keyboard chỉ có Tab tiến100% đủ bằng chứng.
10 mã nguyên bản PASS trước giữ lịch sử, nhưng planUI bổ sung cònFAIL, chưa
nghiệm thu nhóm. B09 chờ duyệt, không sửa trong test.

## Mới nhất — regression B08 PASS09/10/2026

Đã sửa và regression app Windows account riêng thật; T05-K07 PASS: hủy trước
load đầu/có rows/hủy lặp đều đã hủy, unlock không late update, reload10 bàn,
close khi SQL blocked không còn request. BuildDebug/Release0 warning/0 error.
T05 **10/10 ca nguyên bản PASS**; biên bản [b08-test-results.md](b08-test-results.md).
K01–K06/K08–K10 kế thừa do DAL/fixture/contract không đổi ở B08.
DPI100/150 và keyboard bổ sung NOT RUN; các FAIL/BLOCKED dưới là lịch sử.

## Mới nhất09/10/2026 — T05-K07 manual FAIL

App chạy account Windows riêng thật, tải10 bàn, query chậm UI phản hồi, hủy
dừng SQL và close process được. Nhưng status sau hủy vẫn “Đang tải…”: B08 mới,
chờ duyệt sửa. T05 hiện9 PASS/1 FAIL. Source fixed, không sửa trong test.
Biên bản đầy đủ: [manual-test-results.md](manual-test-results.md).
Các BLOCKED K07 bên dưới là lịch sử trước có account/run manual.

## Cập nhật B07 — UI mới

Đã sửa UI theo duyệt; build/native125% resize/loading/lỗi và demo grid đã kiểm
chứng. Biên bản: [b07-test-results.md](b07-test-results.md). K07 vẫn BLOCKED
chờ app principal riêng và slow-query/hủy/đóng; preview DTO không thay SQL.
Các ca DAL/SQL trước được kế thừa vì source DAL/fixture không đổi. T05 vẫn9 PASS/1 BLOCKED.

## Tổng hợp mới nhất — regression B04–B06 đã duyệt

Run `20261008_222503_29aa4634`, đợt `G01_Regression_20261008_222427`.
SQL localhost 17.0.1000.7, SDK 10.0.401, source chưa commit/fixed manifest nhóm.
Lệnh: Test-DataAccess.ps1. **20/20 xUnit PASS, 0 FAIL, 0 skipped**, exit 0,
SetupError null; fixtures dùng DB mới, quyền T05_App WITHOUT LOGIN.
TRX/stdout/stderr/results.json ở `runs/20261008_222503_29aa4634/`.

| Ca | Trạng thái | Expected / Actual |
|---|---|---|
| T05-K01 | PASS | Unicode/O'Brien/injection cả 3 roundtrip PASS, sentinel nguyên |
| T05-K02 | PASS | NULL/CHAR/decimal/date assertions PASS; validation precision/length PASS |
| T05-K03 | PASS | Empty list khác connection failure |
| T05-K04 | PASS | TVP 0/1/3/type sai và validation PASS |
| T05-K05 | PASS | Hai late errors rollback snapshot toàn bảng và state cùng session đúng |
| T05-K06 | PASS | Checkout trực tiếp không outer transaction, sau gọi 0:0, tiền/trạng thái đúng |
| T05-K07 | BLOCKED | UI query chậm/hủy/đóng và principal app riêng chưa có bằng chứng |
| T05-K08 | PASS | Log assertions thao tác/mã/correlation, không sensitive text/connection/password |
| T05-K09 | PASS | Unknown, write timeout đối chiếu và known 51109 map đúng |
| T05-K10 | PASS | Guard targets/cleanup sentinel và 10 HD seed PASS |

Probe B06 trong runs nhóm: pooling bật tái hiện SQL596, pooling tắt không lỗi.
Lịch sử FAIL giữ tại `runs/G01_Regression_20261008_221528-acceptance.md`.
Chưa chạy nhánh runner nonzero bằng failure cưỡng bức sau sửa B04; không dùng
điều đó làm bằng chứng rollback/SQL FAIL ở bộ tests hiện đã PASS.

## Output runner
Run: 20261008_222503_29aa4634; DB: CoffeeManager_Test_T05_20261008_222503_29aa4634; dotnet exit: 0
| Test | Outcome | Actual |
|---|---|---|
| CafeManager.Tests.ParameterAndErrorTests.TestGuardRejectsNonIsolatedDatabase(database: "QuanLyCafe_ERP_V2") | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.ParametersRoundTripWithoutExecutingInput(text: "Cà phê sữa") | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.ParametersRoundTripWithoutExecutingInput(text: "'; DROP TABLE dbo.T05_Sentinel;--") | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.TvpPreservesColumnsTypesAndNull(count: 0) | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.ParametersRoundTripWithoutExecutingInput(text: "O'Brien") | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.TvpPreservesColumnsTypesAndNull(count: 1) | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.TvpPreservesColumnsTypesAndNull(count: 3) | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.SuccessfulProcedureHasNoOuterTransactionAndAppCanExecute | Passed |  |
| CafeManager.Tests.ParameterAndErrorTests.UnknownWriteAndReadTimeoutHaveDifferentReconciliationSemantics | Passed |  |
| CafeManager.Tests.ParameterAndErrorTests.PrecisionAndLengthAreRejectedInsteadOfTruncated | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.EmptyResultsAreDifferentFromConnectionFailure | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.LateProcedureErrorRollsBackRowsAndSession(invoice: "Z5PAY00003", method: "PT001", expectedNumber: 52906) | Passed |  |
| CafeManager.Tests.ParameterAndErrorTests.PaymentRowsRejectInvalidAmountsAndReferences | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.UnknownAndTimeoutErrorsDoNotLeakSensitiveTextOrReportSuccess | Passed |  |
| CafeManager.Tests.ParameterAndErrorTests.TestGuardRejectsNonIsolatedDatabase(database: "CoffeeManager_Test_T05_wrong") | Passed |  |
| CafeManager.Tests.ParameterAndErrorTests.TestGuardRejectsNonIsolatedDatabase(database: "master") | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.LateProcedureErrorRollsBackRowsAndSession(invoice: "Z5PAY00001", method: "ZZ999", expectedNumber: 547) | Passed |  |
| CafeManager.Tests.ParameterAndErrorTests.TestGuardRejectsNonIsolatedDatabase(database: "tempdb") | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.FixtureCleanupPreservesSentinelAndSeed | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.InvalidTvpTypeProducesDiagnosticError | Passed |  |
Manual UI/Designer/DPI/log/guard evidence: NOT RUN until recorded separately. Automated assertions do not substitute for WinForms manual acceptance.
