# T05 - test results
Run: 20261017_155740_13e97169; DB: CoffeeManager_Test_T05_20261017_155740_13e97169; dotnet exit: 0
| Test | Outcome | Actual |
|---|---|---|
| CafeManager.Tests.Integration.DataAccessTests.ParametersRoundTripWithoutExecutingInput(text: "'; DROP TABLE dbo.T05_Sentinel;--") | Passed |  |
| CafeManager.Tests.ParameterAndErrorTests.UnknownWriteAndReadTimeoutHaveDifferentReconciliationSemantics | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.TvpPreservesColumnsTypesAndNull(count: 0) | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.UnknownAndTimeoutErrorsDoNotLeakSensitiveTextOrReportSuccess | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.InvalidTvpTypeProducesDiagnosticError | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.EmptyResultsAreDifferentFromConnectionFailure | Passed |  |
| CafeManager.Tests.ParameterAndErrorTests.TestGuardRejectsNonIsolatedDatabase(database: "CoffeeManager_Test_T05_wrong") | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.ParametersRoundTripWithoutExecutingInput(text: "Cà phê sữa") | Passed |  |
| CafeManager.Tests.ParameterAndErrorTests.TestGuardRejectsNonIsolatedDatabase(database: "tempdb") | Passed |  |
| CafeManager.Tests.ParameterAndErrorTests.PaymentRowsRejectInvalidAmountsAndReferences | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.SuccessfulProcedureHasNoOuterTransactionAndAppCanExecute | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.FixtureCleanupPreservesSentinelAndSeed | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.ParametersRoundTripWithoutExecutingInput(text: "O'Brien") | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.TvpPreservesColumnsTypesAndNull(count: 3) | Passed |  |
| CafeManager.Tests.ParameterAndErrorTests.TestGuardRejectsNonIsolatedDatabase(database: "QuanLyCafe_ERP_V2") | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.LateProcedureErrorRollsBackRowsAndSession(invoice: "Z5PAY00003", method: "PT001", expectedNumber: 52906) | Passed |  |
| CafeManager.Tests.ParameterAndErrorTests.PrecisionAndLengthAreRejectedInsteadOfTruncated | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.TvpPreservesColumnsTypesAndNull(count: 1) | Passed |  |
| CafeManager.Tests.ParameterAndErrorTests.TestGuardRejectsNonIsolatedDatabase(database: "master") | Passed |  |
| CafeManager.Tests.Integration.DataAccessTests.LateProcedureErrorRollsBackRowsAndSession(invoice: "Z5PAY00001", method: "ZZ999", expectedNumber: 547) | Passed |  |
Manual UI/Designer/DPI/log/guard evidence: NOT RUN until recorded separately. Automated assertions do not substitute for WinForms manual acceptance.