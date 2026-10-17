# G02 â€” Automated run evidence
Run: 20261009_213818_cf2043cf; task: T06; exit: 0; source unchanged: True

| Test | Outcome | Actual |
|---|---|---|
| CafeManager.Tests.Auth.AuthRulesTests.InvalidUsernamesAreRejected(input: "a b") | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.UsernameNormalizationPreservesDisplayCase(input: "x", expected: "x") | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.PermissionMatrixIsIndependentOfMenu(role: "CASH", permission: Finance, expected: False) | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.HashVerificationPreservesPasswordSpacesAndRejectsPlaceholder | Passed |  |
| CafeManager.Tests.G02.AuthIntegrationTests.T06_K01_RollbackKeepsStateAndEmployeeAccountRowsRetryable | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.PermissionMatrixIsIndependentOfMenu(role: "STORE", permission: DebtRead, expected: True) | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.PermissionMatrixIsIndependentOfMenu(role: "STORE", permission: Sales, expected: False) | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.PermissionMatrixIsIndependentOfMenu(role: "CASH", permission: Customers, expected: True) | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.InvalidUsernamesAreRejected(input: "nguyễn") | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.InvalidUsernamesAreRejected(input: "a\tb") | Passed |  |
| CafeManager.Tests.G02.AuthIntegrationTests.T06_K08_OldSessionCannotOperateAfterAccountRevocation | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.UsernameNormalizationPreservesDisplayCase(input: " a.B_1- ", expected: "a.B_1-") | Passed |  |
| CafeManager.Tests.G02.AuthIntegrationTests.T06_K02_RestartAndAdminLockDoNotReopenBootstrap | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.PermissionMatrixIsIndependentOfMenu(role: "unknown", permission: SystemInfo, expected: False) | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.InvalidUsernamesAreRejected(input: " ") | Passed |  |
| CafeManager.Tests.G02.AuthIntegrationTests.T06_K01_ConcurrentBootstrapAllowsExactlyOneCommit | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.PermissionMatrixIsIndependentOfMenu(role: "ADMIN", permission: CatalogAdmin, expected: True) | Passed |  |
| CafeManager.Tests.G02.AuthIntegrationTests.T06_K05_CasCannotOverwritePasswordChangedAfterRead | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.InvalidUsernamesAreRejected(input: "") | Passed |  |
Automated evidence only; no aggregate T06/T07/T08 PASS. Record every remaining mandatory manual/SQL subcase separately at its owning task. Test DBs retained for inspection; no automatic source edits or cleanup.