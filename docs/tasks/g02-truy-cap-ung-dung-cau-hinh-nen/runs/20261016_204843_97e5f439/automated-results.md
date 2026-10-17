# G02 â€” Automated run evidence
Run: 20261016_204843_97e5f439; task: All; exit: 0; source unchanged: True

| Test | Outcome | Actual |
|---|---|---|
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: DeliveryPartner, action: "Update") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: ProductCategory, action: "Create") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K05_EachCatalogWithHistoryCannotDelete(kind: DeliveryPartner, code: "DT990") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Shift, action: "Update", boundary: "Tab") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ProductCategory, action: "Create", boundary: "Tab") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ExpenseCategory, action: "Create", boundary: "Empty") | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: False, value: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"···) | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: ProductCategory, action: "Create") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: Supplier, action: "Create") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: ProductCategory, action: "Read") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K08_ManualSupportIsExplicitAndNameIndependent | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: True, value: "a b") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_MaxNameAndValidFieldsStillPersist(kind: Shift) | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: True, value: "a/b") | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.SupplierCodeRespectsFiniteAsciiContract(code: "NCC1") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: DeliveryPartner, action: "Update") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: Supplier, action: "Create") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: PaymentMethod, action: "Update") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ExpenseCategory, action: "Update", boundary: "Tab") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: Shift, action: "Delete") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: PaymentMethod, action: "Update", boundary: "Spaces") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K01_EachCatalogPersistsSearchesAndRejectsStaleEditor(request: ShiftRequest { Code = CA999, Name = Tiếng Việt %_[ ' CA, StartTime = 22:00:00, EndTime = 06:00:00 }) | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: True, value: "") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ProductCategory, action: "Update", boundary: "Tab") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K07_UnusedSystemExpenseCannotDeleteOrChangeType(code: "TC005") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_MaxNameAndValidFieldsStillPersist(kind: ProductCategory) | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: PaymentMethod, action: "Delete") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Shift, action: "Create", boundary: "MaxPlusOne") | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.InvalidUsernamesAreRejected(input: "a\tb") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K01_EachCatalogPersistsSearchesAndRejectsStaleEditor(request: DeliveryPartnerRequest { Code = DT999, Name = Tiếng Việt %_[ ' DT, Ratio = 100 }) | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: Shift, action: "Create") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: Shift, action: "Update") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: DeliveryPartner, action: "Read") | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.SupplierCodeRespectsFiniteAsciiContract(code: "NCC100") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: Position, action: "Create") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: Shift, action: "Delete") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: PaymentMethod, action: "Create", boundary: "Spaces") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: ExpenseCategory, action: "Create") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K05_EachCatalogWithHistoryCannotDelete(kind: ExpenseCategory, code: "TC990") | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: False, value: "a b") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: PaymentMethod, action: "Create", boundary: "Long") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Supplier, action: "Update", boundary: "MaxPlusOne") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: ProductCategory, action: "Delete") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Position, action: "Update", boundary: "MaxPlusOne") | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.PositionDoesNotRoundInvalidFinancialFields(allowance: 0, factor: 0.98999999999999999) | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.PositionDoesNotRoundInvalidFinancialFields(allowance: 0, factor: 1.0009999999999999) | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: DeliveryPartner, action: "Update", boundary: "Long") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: DeliveryPartner, action: "Update", boundary: "MaxPlusOne") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Supplier, action: "Create", boundary: "Long") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K01_EachCatalogPersistsSearchesAndRejectsStaleEditor(request: PositionRequest { Code = CV999, Name = Tiếng Việt %_[ ' CV, Allowance = 0, Factor = 1 }) | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_MaxNameAndValidFieldsStillPersist(kind: Supplier) | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Shift, action: "Create", boundary: "Spaces") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ProductCategory, action: "Update", boundary: "Spaces") | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.SupplierCodeRespectsFiniteAsciiContract(code: "NCC０1") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: DeliveryPartner, action: "Create", boundary: "Empty") | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.NewMethodCannotEnableSupportAndPointsNeverEnable | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: False, value: " a") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K07_UnusedSystemExpenseCannotDeleteOrChangeType(code: "TC011") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ExpenseCategory, action: "Create", boundary: "MaxPlusOne") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K05_EachCatalogWithHistoryCannotDelete(kind: ProductCategory, code: "DM990") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: PaymentMethod, action: "Delete") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: PaymentMethod, action: "Read") | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.RatioIncludesBothBoundaries(input: 0) | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: Position, action: "Delete") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Supplier, action: "Update", boundary: "Long") | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.SystemExpenseCanRenameButNotCreateOrChangeType(code: "TC012") | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.PositionDoesNotRoundInvalidFinancialFields(allowance: 1.5, factor: 1) | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: False, value: "a/b") | Passed |  |
| CafeManager.Tests.G02.AuthIntegrationTests.T06_K01_RollbackKeepsStateAndEmployeeAccountRowsRetryable | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsPhone16WithoutTruncating(action: "Update") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_MaxNameAndValidFieldsStillPersist(kind: Position) | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: ExpenseCategory, action: "Update") | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.PermissionMatrixIsIndependentOfMenu(role: "CASH", permission: Finance, expected: False) | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ExpenseCategory, action: "Create", boundary: "Tab") | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.UsernameNormalizationPreservesDisplayCase(input: " a.B_1- ", expected: "a.B_1-") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: PaymentMethod, action: "Create") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: Shift, action: "Read") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Position, action: "Create", boundary: "Long") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Position, action: "Create", boundary: "Spaces") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Position, action: "Create", boundary: "Empty") | Passed |  |
| CafeManager.Tests.G02.AuthIntegrationTests.T06_K01_ConcurrentBootstrapAllowsExactlyOneCommit | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ExpenseCategory, action: "Create", boundary: "Long") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: PaymentMethod, action: "Update", boundary: "Empty") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_MaxNameAndValidFieldsStillPersist(kind: PaymentMethod) | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ExpenseCategory, action: "Create", boundary: "Spaces") | Passed |  |
| CafeManager.Tests.G02.AuthIntegrationTests.T06_K02_RestartAndAdminLockDoNotReopenBootstrap | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.PermissionMatrixIsIndependentOfMenu(role: "STORE", permission: Sales, expected: False) | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: False, value: "a\ud83d\ude42") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: DeliveryPartner, action: "Delete") | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: True, value: "a ") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ProductCategory, action: "Create", boundary: "Long") | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.PermissionMatrixIsIndependentOfMenu(role: "STORE", permission: DebtRead, expected: True) | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K01_CreateReceiptReplaysOnlyTheSameRequestPayload | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.RatioIncludesBothBoundaries(input: 100) | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.SupplierCodeRespectsFiniteAsciiContract(code: "NCC00") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Shift, action: "Create", boundary: "Tab") | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: True, value: "a\ud83d\ude42") | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.PermissionMatrixIsIndependentOfMenu(role: "CASH", permission: Customers, expected: True) | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Shift, action: "Create", boundary: "Long") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: ExpenseCategory, action: "Delete") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: DeliveryPartner, action: "Create") | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: True, value: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"···) | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ExpenseCategory, action: "Update", boundary: "Long") | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.InvalidNamesAreRejected(name: "\t") | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.UsernameNormalizationPreservesDisplayCase(input: "x", expected: "x") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: PaymentMethod, action: "Read") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: ProductCategory, action: "Read") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Position, action: "Update", boundary: "Spaces") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: ExpenseCategory, action: "Update") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Supplier, action: "Update", boundary: "Spaces") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Supplier, action: "Update", boundary: "Empty") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: DeliveryPartner, action: "Read") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: Position, action: "Delete") | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.InvalidUsernamesAreRejected(input: " ") | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: True, value: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"···) | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.RatioDoesNotRoundOrAcceptOutOfRange(input: -1) | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: False, value: "tên") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ProductCategory, action: "Create", boundary: "MaxPlusOne") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: PaymentMethod, action: "Create", boundary: "Empty") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Position, action: "Create", boundary: "MaxPlusOne") | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: True, value: "tên") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Supplier, action: "Create", boundary: "Empty") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Supplier, action: "Create", boundary: "MaxPlusOne") | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.InvalidNamesAreRejected(name: "Tên\nkhác") | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.InvalidUsernamesAreRejected(input: "a b") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Shift, action: "Update", boundary: "Empty") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: ExpenseCategory, action: "Delete") | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: True, value: "a\t") | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.SystemExpenseCanRenameButNotCreateOrChangeType(code: "TC011") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K01_EachCatalogPersistsSearchesAndRejectsStaleEditor(request: PaymentMethodRequest { Code = PT999, Name = Tiếng Việt %_[ ' PT, Supported = False }) | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: Supplier, action: "Read") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: ExpenseCategory, action: "Read") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Shift, action: "Update", boundary: "Long") | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: False, value: "a ") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K05_EachCatalogWithHistoryCannotDelete(kind: Position, code: "CV990") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Position, action: "Update", boundary: "Long") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Position, action: "Update", boundary: "Tab") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K10_PartnerEditLeavesPaidOrderSnapshotUnchanged | Passed |  |
| CafeManager.Tests.G02.AuthIntegrationTests.T06_K05_CasCannotOverwritePasswordChangedAfterRead | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: True, value: "Ａ") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: DeliveryPartner, action: "Create", boundary: "Tab") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_MaxNameAndValidFieldsStillPersist(kind: ExpenseCategory) | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: Supplier, action: "Delete") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: PaymentMethod, action: "Update") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ExpenseCategory, action: "Update", boundary: "Empty") | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.HashVerificationPreservesPasswordSpacesAndRejectsPlaceholder | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Shift, action: "Update", boundary: "MaxPlusOne") | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: True, value: " ") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsPhone16WithoutTruncating(action: "Create") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: DeliveryPartner, action: "Create", boundary: "Long") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K05_EachCatalogWithHistoryCannotDelete(kind: Shift, code: "CA990") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: DeliveryPartner, action: "Update", boundary: "Spaces") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: PaymentMethod, action: "Update", boundary: "MaxPlusOne") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K05_EachCatalogWithHistoryCannotDelete(kind: Supplier, code: "NCC90") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ProductCategory, action: "Update", boundary: "Long") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: Shift, action: "Read") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ExpenseCategory, action: "Update", boundary: "Spaces") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: PaymentMethod, action: "Create") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K05_EachCatalogWithHistoryCannotDelete(kind: PaymentMethod, code: "PT990") | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.RatioDoesNotRoundOrAcceptOutOfRange(input: 0.001) | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.InvalidNamesAreRejected(name: " ") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: DeliveryPartner, action: "Update", boundary: "Tab") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Supplier, action: "Update", boundary: "Tab") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: PaymentMethod, action: "Create", boundary: "Tab") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: ProductCategory, action: "Delete") | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: False, value: "Ａ") | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.SystemExpenseCanRenameButNotCreateOrChangeType(code: "TC005") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: Supplier, action: "Read") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ProductCategory, action: "Create", boundary: "Empty") | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: False, value: "a\t") | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.PermissionMatrixIsIndependentOfMenu(role: "ADMIN", permission: CatalogAdmin, expected: True) | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K01_EachCatalogPersistsSearchesAndRejectsStaleEditor(request: SupplierRequest { Code = NCC99, Name = Tiếng Việt %_[ ' NCC, Phone = 012345678901234 }) | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: Supplier, action: "Update") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: Supplier, action: "Update") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Supplier, action: "Create", boundary: "Tab") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: DeliveryPartner, action: "Update", boundary: "Empty") | Passed |  |
| CafeManager.Tests.G02.AuthIntegrationTests.T06_K08_OldSessionCannotOperateAfterAccountRevocation | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.PositionDoesNotRoundInvalidFinancialFields(allowance: -1, factor: 1) | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Position, action: "Create", boundary: "Tab") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Shift, action: "Create", boundary: "Empty") | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: False, value: "") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: DeliveryPartner, action: "Create") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: DeliveryPartner, action: "Create", boundary: "MaxPlusOne") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: ProductCategory, action: "Update") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: DeliveryPartner, action: "Delete") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: PaymentMethod, action: "Update", boundary: "Long") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Position, action: "Update", boundary: "Empty") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Shift, action: "Update", boundary: "Spaces") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: Supplier, action: "Delete") | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_ValidCanonicalKeyStillReadsTheSameAccount(legacy: True) | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: PaymentMethod, action: "Create", boundary: "MaxPlusOne") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: ExpenseCategory, action: "Create") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_MaxNameAndValidFieldsStillPersist(kind: DeliveryPartner) | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.InvalidUsernamesAreRejected(input: "nguyễn") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: Shift, action: "Update") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ProductCategory, action: "Create", boundary: "Spaces") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: ProductCategory, action: "Update") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K01_EachCatalogPersistsSearchesAndRejectsStaleEditor(request: ProductCategoryRequest { Code = , Name = Tiếng Việt %_[ ' DM, RequestId = f4b59565-0256-4cc3-999f-a537dc6383df }) | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.OvernightShiftIsAllowedButEqualOrSubMinuteTimesAreNot | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: False, value: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"···) | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: Shift, action: "Create") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ProductCategory, action: "Update", boundary: "Empty") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: PaymentMethod, action: "Update", boundary: "Tab") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: Position, action: "Read") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: DeliveryPartner, action: "Create", boundary: "Spaces") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: Position, action: "Update") | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.PermissionMatrixIsIndependentOfMenu(role: "unknown", permission: SystemInfo, expected: False) | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K07_UnusedSystemExpenseCannotDeleteOrChangeType(code: "TC012") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: Position, action: "Update") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: Supplier, action: "Create", boundary: "Spaces") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: Position, action: "Read") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "STORE", kind: ExpenseCategory, action: "Read") | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_ValidCanonicalKeyStillReadsTheSameAccount(legacy: False) | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K09_EachCatalogBlocksUnauthorizedServiceCalls(role: "CASH", kind: Position, action: "Create") | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.RatioDoesNotRoundOrAcceptOutOfRange(input: 100.01000000000001) | Passed |  |
| CafeManager.Tests.Auth.AuthRulesTests.InvalidUsernamesAreRejected(input: "") | Passed |  |
| CafeManager.Tests.G02.CatalogIntegrationTests.T08_K01_EachCatalogPersistsSearchesAndRejectsStaleEditor(request: ExpenseCategoryRequest { Code = TC999, Name = Tiếng Việt %_[ ' TC, Type = Thu }) | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ExpenseCategory, action: "Update", boundary: "MaxPlusOne") | Passed |  |
| CafeManager.Tests.Catalogs.CatalogRulesTests.InvalidNamesAreRejected(name: "") | Passed |  |
| CafeManager.Tests.G02.CatalogBoundaryRegressionTests.B02_SqlRejectsInvalidNameBeforeCreateOrUpdate(kind: ProductCategory, action: "Update", boundary: "MaxPlusOne") | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: True, value: " a") | Passed |  |
| CafeManager.Tests.G02.AuthBoundaryRegressionTests.B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(legacy: False, value: " ") | Passed |  |
Automated evidence only; no aggregate T06/T07/T08 PASS. Record every remaining mandatory manual/SQL subcase separately at its owning task. Test DBs retained for inspection; no automatic source edits or cleanup.