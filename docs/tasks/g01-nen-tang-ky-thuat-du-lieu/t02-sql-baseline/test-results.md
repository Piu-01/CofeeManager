# T02 - SQL baseline test results

Run: `20261008_205557_93421dd4`. Engine: LAPTOP-79UNRIRC | 17.0.1000.7 | Enterprise Developer Edition (64-bit). Commit: uncommitted.
Schema SHA256: `53351D5CA521464558DE6FC4AA1F839CC45CBBD091FDDB4EC005643F2D6EE151`. Seed SHA256: `FBC1CAB2233D8B3B588F6C3DE61DB9A8ADCD40CFE19997531EB91354BADDF706`.
Started: 2026-10-08T20:55:57.4926310+07:00. Completed: 2026-10-08T20:56:11.5208876+07:00.
Test databases are retained for inspection; existing databases are never reset/dropped.
- baseline: `CoffeeManager_Test_T02_baseline_20261008_205557_93421dd4`
- fixture: `CoffeeManager_Test_T02_fixture_20261008_205557_93421dd4`

| Case | Status | Subcases |
|---|---|---|
| T02-K01 | PASS | 2 |
| T02-K02 | PASS | 1 |
| T02-K03 | PASS | 1 |
| T02-K04 | PASS | 1 |
| T02-K05 | PASS | 1 |
| T02-K06 | PASS | 9 |
| T02-K07 | PASS | 5 |
| T02-K08 | PASS | 12 |
| T02-K09 | PASS | 3 |
| T02-K10 | PASS | 7 |
| T02-K11 | PASS | 6 |
| T02-K12 | PASS | 45 |

| Subcase | Description / expected | Status | Actual |
|---|---|---|---|
| T02-K01-catalog | Exact baseline object counts and enabled trusted constraints; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K01-modules | SQL modules resolve dependencies and compile on engine; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K02-seed | Seed commits 10 bills 18 lines 9 paid and one open table B0003; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K03-money | Sample bills and split payments match explicit amounts; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K04-invariants | Every invoice receipt allocation stock and customer tier is consistent; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K05-repeat | Seed refuses repeat and preserves all rows; expected error 51200 | PASS | SQL errors: 51200; transaction 0:0; changed tables: 0 |
| T02-K06-product-fk | Product category FK rejects missing code; expected error 547 | PASS | SQL errors: 547; transaction 0:0; changed tables: 0 |
| T02-K06-order-fk | Invoice employee FK rejects missing code; expected error 547 | PASS | SQL errors: 547; transaction 0:0; changed tables: 0 |
| T02-K06-negative-money | Negative product price violates CHECK; expected error 547 | PASS | SQL errors: 547; transaction 0:0; changed tables: 0 |
| T02-K06-negative-stock | Negative stock violates CHECK; expected error 547 | PASS | SQL errors: 547; transaction 0:0; changed tables: 0 |
| T02-K06-zero-line | Zero invoice quantity violates CHECK; expected error 547 | PASS | SQL errors: 547; transaction 0:0; changed tables: 0 |
| T02-K06-zero-receipt | Zero receipt quantity violates CHECK; expected error 547 | PASS | SQL errors: 547; transaction 0:0; changed tables: 0 |
| T02-K06-zero-export | Zero export quantity violates CHECK; expected error 547 | PASS | SQL errors: 547; transaction 0:0; changed tables: 0 |
| T02-K06-reverse-promotion | Reversed promotion dates violate CHECK; expected error 547 | PASS | SQL errors: 547; transaction 0:0; changed tables: 0 |
| T02-K06-same-day | Same-day promotion is valid; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K07-null-phones | Two NULL phones each for customers and employees are accepted; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K07-customer-duplicate | Customer non-NULL phone duplicates are rejected; expected error 2601 | PASS | SQL errors: 2601; transaction 0:0; changed tables: 0 |
| T02-K07-employee-duplicate | Employee non-NULL phone duplicates are rejected; expected error 2601 | PASS | SQL errors: 2601; transaction 0:0; changed tables: 0 |
| T02-K07-customer-update | Updating a phone to an existing customer phone is rejected; expected error 2601 | PASS | SQL errors: 2601; transaction 0:0; changed tables: 0 |
| T02-K07-employee-update | Updating a phone to an existing employee phone is rejected; expected error 2601 | PASS | SQL errors: 2601; transaction 0:0; changed tables: 0 |
| T02-K08-lines-insert | Batch insert groups independent invoice totals; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K08-lines-update | Batch quantity update preserves per-invoice groups; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K08-lines-delete | Batch delete recomputes all affected invoices; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K08-lines-atomic | One invalid quantity rolls back the entire batch; expected error 547 | PASS | SQL errors: 547; transaction 0:0; changed tables: 0 |
| T02-K08-receipts-insert | Receipt insert accumulates a shared ingredient across two headers; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K08-receipts-update | Receipt update uses inserted minus deleted delta; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K08-receipts-delete | Receipt delete reverses every grouped stock delta; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K08-exports-insert | Export batch groups stock shared across two headers; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K08-exports-update | Export update subtracts only changed quantities; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K08-exports-delete | Export delete restores grouped amounts; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K08-exports-atomic | Insufficient stock in one row rolls back all export rows; expected error 547 | PASS | SQL errors: 547,3621; transaction 0:0; changed tables: 0 |
| T02-K08-receipts-atomic | Invalid ingredient FK rolls back all receipt rows; expected error 547 | PASS | SQL errors: 547; transaction 0:0; changed tables: 0 |
| T02-K09-success | Checkout records grouped consumption split payments and exact points once; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K09-stock | Checkout with one insufficient ingredient rolls back all 35 tables; expected error 547 | PASS | SQL errors: 547; transaction 0:0; changed tables: 0 |
| T02-K09-method | Invalid method after consumption insertion rolls back stock and all rows; expected error 547 | PASS | SQL errors: 547; transaction 0:0; changed tables: 0 |
| T02-K10-overpay | Debt overpayment creates neither expense nor repayment history; expected error 51111 | PASS | SQL errors: 51111; transaction 0:0; changed tables: 0 |
| T02-K10-partial | Partial debt payment creates exactly one linked expense and correct balance; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K10-debt-lock | Debt with payment history cannot be edited; expected error 51013 | PASS | SQL errors: 51013; transaction 0:0; changed tables: 0 |
| T02-K10-history-update | Repayment history cannot be updated; expected error 51017 | PASS | SQL errors: 51017; transaction 0:0; changed tables: 0 |
| T02-K10-history-delete | Repayment history cannot be deleted; expected error 51017 | PASS | SQL errors: 51017; transaction 0:0; changed tables: 0 |
| T02-K10-expense-update | Linked expense cannot be edited; expected error 51020 | PASS | SQL errors: 51020; transaction 0:0; changed tables: 0 |
| T02-K10-expense-delete | Linked expense delete is blocked by FK; expected error 547 | PASS | SQL errors: 547; transaction 0:0; changed tables: 0 |
| T02-K11-success | Count adjusts two ingredients once and marks header finalized; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K11-repeat | Already finalized count cannot be applied again; expected error 51112 | PASS | SQL errors: 51112; transaction 0:0; changed tables: 0 |
| T02-K11-old | Past-day count is rejected; expected error 51112 | PASS | SQL errors: 51112; transaction 0:0; changed tables: 0 |
| T02-K11-stale | One stale snapshot in multi-row count rolls back all ingredients; expected error 51114 | PASS | SQL errors: 51114; transaction 0:0; changed tables: 0 |
| T02-K11-detail-update | Finalized count detail cannot be edited; expected error 51115 | PASS | SQL errors: 51115; transaction 0:0; changed tables: 0 |
| T02-K11-detail-delete | Finalized count detail cannot be deleted; expected error 51115 | PASS | SQL errors: 51115; transaction 0:0; changed tables: 0 |
| T02-K12-pay-missing | Checkout missing invoice hits 51101; expected error 51101 | PASS | SQL errors: 51101; transaction 0:0; changed tables: 0 |
| T02-K12-pay-paid | Checkout paid invoice is not charged again; expected error 51101 | PASS | SQL errors: 51101; transaction 0:0; changed tables: 0 |
| T02-K12-pay-canceled | Checkout canceled invoice is refused; expected error 51101 | PASS | SQL errors: 51101; transaction 0:0; changed tables: 0 |
| T02-K12-pay-empty | Empty invoice hits 51102; expected error 51102 | PASS | SQL errors: 51102; transaction 0:0; changed tables: 0 |
| T02-K12-pay-existing | Invoice with an earlier allocation hits 51103; expected error 51103 | PASS | SQL errors: 51103; transaction 0:0; changed tables: 0 |
| T02-K12-pay-short | Insufficient TVP allocation hits 51104; expected error 51104 | PASS | SQL errors: 51104; transaction 0:0; changed tables: 0 |
| T02-K12-pay-over | Excess TVP allocation hits 51104; expected error 51104 | PASS | SQL errors: 51104; transaction 0:0; changed tables: 0 |
| T02-K12-pay-promotion | Expired promotion hits 51105; expected error 51105 | PASS | SQL errors: 51105; transaction 0:0; changed tables: 0 |
| T02-K12-pay-stopped | Stopped product hits 51106; expected error 51106 | PASS | SQL errors: 51106; transaction 0:0; changed tables: 0 |
| T02-K12-pay-recipe | Product without recipe hits 51107; expected error 51107 | PASS | SQL errors: 51107; transaction 0:0; changed tables: 0 |
| T02-K12-pay-date | Payment before invoice date hits 51108; expected error 51108 | PASS | SQL errors: 51108; transaction 0:0; changed tables: 0 |
| T02-K12-pay-late-fault | Failure at loyalty stage reverses financial close stock payments and points; expected error 52901 | PASS | SQL errors: 52901; transaction 0:0; changed tables: 0 |
| T02-K12-cancel-missing | Cancel missing invoice hits 51109; expected error 51109 | PASS | SQL errors: 51109; transaction 0:0; changed tables: 0 |
| T02-K12-cancel-paid | Paid invoice cannot be canceled; expected error 51109 | PASS | SQL errors: 51109; transaction 0:0; changed tables: 0 |
| T02-K12-cancel-canceled | Already canceled invoice cannot be canceled twice; expected error 51109 | PASS | SQL errors: 51109; transaction 0:0; changed tables: 0 |
| T02-K12-cancel-allocation | Cancel with payment allocation hits financial trigger; expected error 51006 | PASS | SQL errors: 51006; transaction 0:0; changed tables: 0 |
| T02-K12-cancel-consumption | Cancel with earlier consumption is refused; expected error 51006 | PASS | SQL errors: 51006; transaction 0:0; changed tables: 0 |
| T02-K12-cancel-fault | Cancel header failure preserves original status; expected error 52904 | PASS | SQL errors: 52904; transaction 0:0; changed tables: 0 |
| T02-K12-cancel-success | Valid cancellation preserves stock and releases table; expected error 0 | PASS | SQL errors: ; transaction 0:0; changed tables: 0 |
| T02-K12-debt-missing | Missing debt hits 51110; expected error 51110 | PASS | SQL errors: 51110; transaction 0:0; changed tables: 0 |
| T02-K12-debt-zero | Zero repayment hits 51111; expected error 51111 | PASS | SQL errors: 51111; transaction 0:0; changed tables: 0 |
| T02-K12-debt-negative | Negative repayment hits 51111; expected error 51111 | PASS | SQL errors: 51111; transaction 0:0; changed tables: 0 |
| T02-K12-debt-employee | Invalid employee reverses expense creation; expected error 547 | PASS | SQL errors: 547; transaction 0:0; changed tables: 0 |
| T02-K12-debt-method | Invalid method after expense creation reverses both writes; expected error 547 | PASS | SQL errors: 547; transaction 0:0; changed tables: 0 |
| T02-K12-debt-duplicate | Duplicate expense code rolls back repayment; expected error 2627 | PASS | SQL errors: 2627; transaction 0:0; changed tables: 0 |
| T02-K12-debt-missing-category | Missing TC011 prevents expense and history creation; expected error 547 | PASS | SQL errors: 547; transaction 0:0; changed tables: 0 |
| T02-K12-debt-wrong-category | Wrong TC011 type is rejected by repayment trigger; expected error 51018 | PASS | SQL errors: 51018; transaction 0:0; changed tables: 0 |
| T02-K12-debt-fault | Failure on repayment link reverses newly inserted expense; expected error 52902 | PASS | SQL errors: 52902; transaction 0:0; changed tables: 0 |
| T02-K12-count-missing | Missing count hits 51112; expected error 51112 | PASS | SQL errors: 51112; transaction 0:0; changed tables: 0 |
| T02-K12-count-future | Future-day count hits 51112; expected error 51112 | PASS | SQL errors: 51112; transaction 0:0; changed tables: 0 |
| T02-K12-count-empty | Empty current count hits 51113; expected error 51113 | PASS | SQL errors: 51113; transaction 0:0; changed tables: 0 |
| T02-K12-count-fault | Failure marking count finalized reverses prior stock updates; expected error 52903 | PASS | SQL errors: 52903; transaction 0:0; changed tables: 0 |

Setup/act/assert, errors, snapshot hashes and changed rows: test-results.json; prior runs: runs/. Snapshots compare all rows/columns of 35 tables ordered by PK, excluding identity next value. Transaction state is read on the same connection before cleanup.
