# T08 — Kết quả nghiệm thu và bug đã xác nhận

## Kết luận cuối 17/10/2026 — T08 10/10 ID gốc PASS

Đãđốichiếuđủ7kindCRUD/validation/restart/history,concurrency/unknownwrite,
clean/demo/rerun/preflight,protectedexpense/payment/selector/lookup3role và
snapshot tại[ma trậnG02](../acceptance-matrix.md). Khôngcònsubcasebắtbuộc
NOTRUN/BLOCKED hoặcbughiệnhành. B01/B02/B06đãsửa/regressionPASS;B03/B07auth
vàPKCSđãregressiontrênbảncuối,runner484/484gồm177caseT08.

CácgateWindowsidentity/setup-popupliênquanT06đãPASS ởR7;khôngcòndùngchúng
làlýdochưanghiệmthuT08. Source99filekhôngđổi,manifest/handoff tại
`../runs/G02_FINAL_ACCEPTANCE_20261017/`. Cácbảngphíasau là **lịch sử**,
giữrawFAIL/BLOCKEDvàbằngchứnglượtmới. Đã bàn giaoGit,commit
`1ab09af368f320a598a577d22542e41563e5e432` đãpush`origin/main`;xem`../git-handoff.md`.

SQL Server thật/DBtest có marker, principal runtime userWITHOUTLOGIN,
Pooling=false khiEXECUTE AS. Source/schema/checksum tại runner evidence;
không sửasource/migration/expected trong lượt. Chưa chốt10ID gốc PASS.

## Evidence

- `../runs/20261009_214858_45d2ab1f/`: Test-G02 -TaskT08 -AcceptanceApproved,
  **98/98 xUnitPASS**, exit0, SetupError=null, SourceUnchanged=true. DBchính
  `CoffeeManager_Test_G02_20261009_214859_40b6b849`; TRX/stdout/stderr giữ.
- `../runs/G02_SQL_20261009/`: lượtPS đầu; giữraw84FAIL/11PASS, nhưng đo
  XACT_STATE trongSELECT vàliteralstamp có lỗiharness, fixtureTC011seedhistory
  bịbaselinechặn. Không dùng cácFAIL này để sửaapp.
- `../runs/G02_EXTRA_20261009/`: C# xácnhận đo transaction bằngDECLARE trước
  SELECT (0:0), bindBinary typed và fixturepreflightfresh.54PASS/3FAIL raw,
  trongđó2FAIL làtruncate đãconfirmed;1FAILDBKindTimeout/Connection làpredicate
  quáhẹp, khôngchứngminh login/DBerror bug. Actualtừngcase tạiJSON.
- Native evidence vàgiới hạn tại test-resultsT07; B01/B02 trong bug-report.

## Các ca đã đủ chứng cứ độc lập

| Subcase | Expected | Actual | Kết quả |
|---|---|---|---|
| T08-K01.DM.SQL | Create/Get/Update/Searchđúng, stalekhôngoverwrite | xUnitCRUD:literal%_[quote/Vietnamese,revisioncũ51503,renamegiữ,deleteunused | PASS |
| T08-K01.NCC.SQL | CùngcontractchoNCC | xUnit riêngNCC | PASS |
| T08-K01.PT.SQL | CùngcontractchoPT | xUnit riêngPT | PASS |
| T08-K01.DT.SQL | CùngcontractchoDT | xUnit riêngDT | PASS |
| T08-K01.TC.SQL | CùngcontractchoTC | xUnit riêngTC | PASS |
| T08-K01.CV.SQL | CùngcontractchoCV | xUnit riêngCV | PASS |
| T08-K01.CA.SQL | CùngcontractchoCA | xUnit riêngCA,22→06hợp lệ | PASS |
| T08-K02.NCC.a.SQL.maxplus1 | Name101 từchối | SQLpersist100,khôngreject,NCC84 | FAIL |
| T08-K02.ncc.length16.SQL | SDT16 từchối | SQLpersist15,khôngreject,NCC85 | FAIL |
| T08-K02.DM.a.required.SQL | Empty/space/tab từchối | Extraba case51501,0:0 | PASS |
| T08-K02.NCC.a.required.SQL | Empty/space/tab từchối | Extraba case51501,0:0 | PASS |
| T08-K02.PT.a.required.SQL | Empty/space/tab từchối | Extraba case51501,0:0 | PASS |
| T08-K02.DT.a.required.SQL | Empty/space/tab từchối | Extraba case51501,0:0 | PASS |
| T08-K02.TC.a.required.SQL | Empty/space/tab từchối | Extraba case51501,0:0 | PASS |
| T08-K02.CV.a.required.SQL | Empty/space/tab từchối | Extraba case51501,0:0 | PASS |
| T08-K02.CA.a.required.SQL | Empty/space/tab từchối | Extraba case51501,0:0 | PASS |
| T08-K05.DM.SQL | Unusedxóa,historychặn | xUnit unusedCRUDdelete vàFKDM990 | PASS |
| T08-K05.NCC.SQL | Unusedxóa,historychặn | xUnit unusedCRUDdelete vàFKPN/NCC90 | PASS |
| T08-K05.PT.SQL | Unusedxóa,historychặn | xUnit unusedCRUDdelete vàpaymentPT990 | PASS |
| T08-K05.DT.SQL | Unusedxóa,historychặn | xUnit unusedCRUDdelete vàpaidorderDT990 | PASS |
| T08-K05.TC.SQL | Unusedxóa,historychặn | xUnit unusedCRUDdelete vàphiếuchTC990 | PASS |
| T08-K05.CV.SQL | Unusedxóa,historychặn | xUnit unusedCRUDdelete vàNVCV990 | PASS |
| T08-K05.CA.SQL | Unusedxóa,historychặn | xUnit unusedCRUDdelete vàCC/CA990 | PASS |
| T08-K06.b.rerun | Demo003rerun giữcustomconfiguration | RealPSrunnerAlreadyApplied;salarynamecustomgiữ | PASS |
| T08-K06.c.TC011Thu | Preflightreject/rollback | Freshcleanwrongtype,Version3/DDLabsent | PASS |
| T08-K06.c.TC012existing | Khôngđoán/remapmãcũ | FreshpreexistingTC012,reject,Version3/DDLabsent | PASS |
| T08-K06.c.PTnamecollision | Defaultnameuniqueclashrollback | FreshPT003trùngtênPT002default,reject,DDL/versionabsent | PASS |
| T08-K07.a.TC005.SQL | Code/type/deleteprotectedunused | BLLdelete/typeblocked;xUnitDALdeleteblocked;rawExtra3actionsrejected,0:0 | PASS |
| T08-K07.a.TC011.SQL | CùngcontractTC011 | Cùng cácprobe riêngTC011 | PASS |
| T08-K07.a.TC012.SQL | CùngcontractTC012 | Cùng cácprobe riêngTC012 | PASS |
| T08-K08.b.SQL | PTnew0→enable→rename→disable | xUnitPT998:selector/Validateđúngqua4trạngthái;010/lạrejected | PASS |
| T08-K09.DM.BLL |3role×read/create/update/delete | ADMINCRUD4hànhđộngPASS;CASH/STORE8denialsPASS tạiTRX | PASS |
| T08-K09.NCC.BLL |3role×4thao tác | Cùng12subcase riêngNCC tạiTRX | PASS |
| T08-K09.PT.BLL |3role×4thao tác | Cùng12subcase riêngPT tạiTRX | PASS |
| T08-K09.DT.BLL |3role×4thao tác | Cùng12subcase riêngDT tạiTRX | PASS |
| T08-K09.TC.BLL |3role×4thao tác | Cùng12subcase riêngTC tạiTRX | PASS |
| T08-K09.CV.BLL |3role×4thao tác | Cùng12subcase riêngCV tạiTRX | PASS |
| T08-K09.CA.BLL |3role×4thao tác | Cùng12subcase riêngCA tạiTRX | PASS |
| T08-K09.lookup.SQL | Mỗilookup×3role/đúngfields | Extra18case riêng;columnsCode/Name/Phone/ExpenseType/timeđúng;deny51505/0:0 | PASS |
| T08-K10.a.SQL | ĐổiRatio khôngđổi paidSnapshot | xUnitpaidorderJSONfinance trước/sau bằng nhau | PASS |
| T08-K01.receipt.replay | SameRequest chỉ1code;differentpayloadreject | xUnitreceipt replay,samecode,reconcilecode;different51508 | PASS |

Các case chứa nhiều biên ở bảng trên có actual/assertions chi tiết trong từng
test/caseJSON; không lấy mộtPASS củarequired thaymax/max+1chưa đủ.

## Subcase bắt buộc chưa được đáp ứng trọn vẹn

| Subcase gốc | Actual / điều kiện thiếu | Kết quả |
|---|---|---|
| T08-K01.DM.a/NCC.a/PT.a/DT.a/TC.a/CV.a/CA.a | SQLCRUD đãPASS; GUIpersist/restartapp từng7kind chưa đủ | NOT RUN |
| T08-K01.DM.b/NCC.b/PT.b/DT.b/TC.b/CV.b/CA.b | xUnitliteral searchSQL đãchạy; nativefilter từng7kind chưa đủ | NOT RUN |
| T08-K02.DM.a/PT.a/DT.a/TC.a/CV.a/CA.a | RequiredSQL vàunitpartial đãchạy; max/max+1UI/BLL/SQLtừngbiên cần bổsung | NOT RUN |
| T08-K02.NCC.a | TruncateSQLmaxplus1confirmedB02; cácbiên khác khôngbùFAIL | FAIL |
| T08-K02.DM.b/NCC.b/PT.b/DT.b/TC.b/CV.b/CA.b | PSduplicate/malformed córawrejection nhưngsaiđo state; toànmatrix chưa confirmđủ | BLOCKED |
| T08-K02.ncc.NULL/empty/duplicate/leading0/15/16 | 16SQLFAIL; NULL/emptyPSraw đo sai; valid0đầu15xUnit có; cònbiênriêng | FAIL |
| T08-K02.nv.NULL/duplicate | T06cleanNULL có;2NVNULL/duplicate riêng chưa đủ | NOT RUN |
| T08-K02.fk | CórawSQL547/51505 nhưngPSstateharness sai; đốichiếudata/error riêng chưa đủ | BLOCKED |
| T08-K03.a.ratio-1/100.01/CV-1/.99/CAequal | Unitcases riêngPASS; directSQLraw có nhưngstateinstrumentation sai | BLOCKED |
| T08-K04.a.DT0/100/CV0/1 | Unitboundary/SQLCRUDvalid có; record từngC#/SQLbiên đầyđủ chưa đủ | NOT RUN |
| T08-K04.b.CAovernight/equal | Unitovernight/equalPASS,SQLovernightPASS; SQLequalconfirmation đo0:0 chưa đủ | NOT RUN |
| T08-K05.DM.a/NCC.a/PT.a/DT.a/TC.a/CV.a/CA.a.UI | SQLnoCascade đãchạy; nativedelete từng7kind chưa thao tác | NOT RUN |
| T08-K06.a | Cleanrowsbootstrap có, cầnbiênbảnfull role/system/default từngrow | NOT RUN |
| T08-K07.a.UI/name | Rawcode/type/deleteđãchặn; UIprotectedselection/rename/mappingverify riêng chưa đủ | NOT RUN |
| T08-K07.b | Historydeleteblocked; support/rename trên chínhPTused chưa thực hiệnđủ | NOT RUN |
| T08-K08.a | ADMINselector/invalid010/lạ có; SQL3purpose PSliteral lỗi; BLL3roleValidate mỗiID chưa đủ | BLOCKED |
| T08-K08.b.PT010enable | Unitguard010 có; BLL/SQLattempt riêngbật010 cầnrecord | NOT RUN |
| T08-K09.lookup.BLL | RawSQL18case projectionđủ; BLLlookup từngrole/DTOprobe chưa chạyđủ | NOT RUN |
| T08-K01.concurrent | Staleupdate tuần tựPASS; đồngthờiupdate/update vàupdate/delete chưa thực hiện2session | NOT RUN |
| T08-K01.unknown | Receipt replayPASS; chưa injectcommit+mấtresponse trongnativeeditor | NOT RUN |

Giữ rootT08 Đang làm. Hai bugconfirmed B01/B02 chờ duyệt; không sửađểnéFAIL.

## Bổ sung ngày 11/10/2026 — 20 ca SQL PASS

Evidence: `../runs/G02_REMAINING_SQL_CONFIRMED_20261011/results.json` và
`harness.ps1`. Chạy trên DB chính T08 đã có marker; application calls qua
G02_Runtime. Mỗi lỗi chụp toàn bộ bảng trước/sau, kiểm SQL error và đo
`DECLARE @t=@@TRANCOUNT,@s=XACT_STATE()` trước SELECT; dữ liệu không đổi, 0:0.

| Subcase riêng | Expected / actual | Kết quả |
|---|---|---|
| T08-K03.a.ratio.minus1.SQL | -1 bị chặn51501, snapshot giữ | PASS |
| T08-K03.a.ratio.above100.SQL |100.01 bị chặn51501, snapshot giữ | PASS |
| T08-K03.a.allowance.minus1.SQL | Phụ cấp-1 bị chặn51501 | PASS |
| T08-K03.a.factor.below1.SQL | Hệ số0.99 bị chặn51501 | PASS |
| T08-K03.a.shift.equal.SQL |06:00→06:00 bị chặn51501 | PASS |
| T08-K02.ncc.NULL.SQL | NULL bị chặn51501 | PASS |
| T08-K02.ncc.empty.SQL | Chuỗi rỗng bị chặn51501 | PASS |
| T08-K02.fk.actorMissing.write | Actor-1 không ghi được,51505 | PASS |
| T08-K02.fk.stampWrong.write | Stamp0 không ghi được,51505 | PASS |
| T08-K02.fk.invalidCategory | SP có DM000 không tồn tại bị chặn547 | PASS |
| T08-K04.DT0.SQL | Persist0.00 | PASS |
| T08-K04.DT100.SQL | Persist100.00 | PASS |
| T08-K04.CV0factor1.SQL | Persist phụ cấp0/hệ số1.00 | PASS |
| T08-K04.CAovernight.SQL | Persist22:00→06:00 | PASS |
| T08-K08.a.selector.ADMIN.Checkout.SQL | PT001/PT002, khôngPT010/PT003 | PASS |
| T08-K08.a.selector.ADMIN.Inventory.SQL | PT001/PT002, khôngPT010/PT003 | PASS |
| T08-K08.a.selector.CASH.Checkout.SQL | PT001/PT002, khôngPT010/PT003 | PASS |
| T08-K08.a.selector.CASH.Inventory.SQL | Chặn51505 | PASS |
| T08-K08.a.selector.STORE.Checkout.SQL | Chặn51505 | PASS |
| T08-K08.a.selector.STORE.Inventory.SQL | PT001/PT002, khôngPT010/PT003 | PASS |

Bảng này thay trạng thái BLOCKED của các **nhánh SQL tương ứng** ở mục trước;
không thay PASS cho toàn ca UI/BLL chưa chạy. K02.ncc vẫn FAIL ở input16;
K08 còn ValidateMethod BLL theo từng role/ID. Không suy20PASS thành20ID gốc.

Lượt đầu cùng ngày tại `G02_REMAINING_SQL_20261011/` giữ raw18FAIL/2PASS:
harness chuyển rowversion bằng CONVERT(varchar,...,1) không tạo hex như mong
đợi, gây lỗi implicit conversion trước khi procedure chạy. Lượt xác nhận dùng
sys.fn_varbintohexstr; chỉ sửa harness ngoài source, không đổi expected/product.
Raw lỗi này không được tính là bug ứng dụng. Integrity cuối tại
`../runs/acceptance-integrity-20261011.json`:96file kiểm tra, chỉ csproj.user
metadata local của Visual Studio thay đổi; source/migration/tests giữ nguyên.

## Regression B01/B02 sau duyệt debug — 11/10/2026

Source mới có bản sửa và `CatalogBoundaryRegressionTests.cs`, đóngmanifest
trước lượt runner. Lệnh `Test-G02.ps1 -Task All -AcceptanceApproved` chạy trên
3DBtest mới, không sửa checksumDB cũ:
`CoffeeManager_Test_G02_20261011_171040_3f1e9b28`,
`CoffeeManager_Test_G02_20261011_171042_443e6208`,
`CoffeeManager_Test_G02_20261011_171043_426c0216`.

**196/196xUnitPASS**, exit0, SetupError=null, SourceUnchanged=true. Gồm19T06
và177T08 (98case trước +79regression mới). TRX vàactual từngcase:
`../runs/20261011_171039_f2e46bea/`. Không quy196test thành196ID nghiệm thu.

| Bộ subcase riêng | Expected / actual | Kết quả |
|---|---|---|
| B02.InvalidNames7kind×Create/Update×Empty/Spaces/Tab/MaxPlusOne/Long=70 | Inputgốc khôngclamp ởSqlParameter; cả70case reject51501, transaction0:0, listcode/name/revisiongiữ, DMreceipt khôngtạo khiCreateinvalid |70/70PASS |
| B02.Phone16×Create/Update=2 | SDT16 từchối51501; khôngrowCreate, Update giữphone/revision |2/2PASS |
| B02.MaxName7kind=7 | Tênmax của từngkind Create→Update→Get giữchínhxác; NCCSDT15/0đầu giữ |7/7PASS |
| Regression CRUD/history/permission/payment/receipt/snapshot trước | Các testcase đãviết trướcfix vẫnPASS |98/98PASS |

10subcaseSQLphone trực tiếp được tách trong
`../runs/G02_B02_PHONE_CONFIRMED_20261011/results.json`:

| Subcase | Expected / actual | Kết quả |
|---|---|---|
| B02.Phone.Create.NULL | SQL51501, fullDBsnapshotgiữ,transaction0:0 | PASS |
| B02.Phone.Create.empty | SQL51501, fullDBsnapshotgiữ,transaction0:0 | PASS |
| B02.Phone.Create.duplicate | SQL2627, fullDBsnapshotgiữ,transaction0:0 | PASS |
| B02.Phone.Create.length16 | SQL51501, fullDBsnapshotgiữ,transaction0:0 | PASS |
| B02.Phone.Create.valid15.leading0 | Persist15ký tự đúngchuỗi,0đầugiữ | PASS |
| B02.Phone.Update.NULL | SQL51501, fullDBsnapshotgiữ,transaction0:0 | PASS |
| B02.Phone.Update.empty | SQL51501, fullDBsnapshotgiữ,transaction0:0 | PASS |
| B02.Phone.Update.duplicate | SQL2627, fullDBsnapshotgiữ,transaction0:0 | PASS |
| B02.Phone.Update.length16 | SQL51501, fullDBsnapshotgiữ,transaction0:0 | PASS |
| B02.Phone.Update.valid15.leading0 | Persist15ký tự đúngchuỗi,0đầugiữ | PASS |

Lượtphone đầu `G02_B02_PHONE_REGRESSION_20261011` bịharnessvariableshadow,
raw8FAIL/2PASS giữ; không gọi làbugapp vàkhôngsửaexpected. Dọnđúng3NCCfixture
của harness trước lượt xácnhận, không sửa dữ liệu sản phẩm đểné lỗi. Giải thích
chi tiết tại bug-report. B01native5/5PASS ghiởT07. Cả2bugđãsửa/regressionPASS;
không tự hoàn tấtT08 khi UI/concurrency/unknown-response cònthiếu.

Integrity97fileproduct/migration/test/tools khôngđổi từđầurunnerđếnsauSQL/native;
schema/hash/harness tại `../runs/G02_B01_B02_REGRESSION_20261011/`.

## Nghiệm thu bổ sung 11–16/10/2026 — không sửa source

| Bộ subcase | Actual | Kết quả / evidence |
|---|---|---|
| T08-K01.DM/NCC/PT/DT/TC/CV/CA.a.native | Mỗikind cóId riêng:Thêm→Sửa→tìm trênapp,typedgridđúngname/code |7/7PASS, FINISH_UI_CONFIRMED |
| T08-K01.DM/NCC/PT/DT/TC/CV/CA.b.native | Mỗikind tìmtiếngViệt,quote/%/_/[đúngliteral;unmatchedempty |7/7PASS, FINISH_UI_CONFIRMED |
| T08-K01.7kind.restart | Processhostmới mởlại7grid,đọcđúngrowsđãsửa |7/7PASS, FINISH_REOPEN |
| T08-K01.concurrent.update/update.SQL |2runtimeconnections dùngrevisioncũ:1commit/1conflict51503 | PASS, FINISH_DATA |
| T08-K01.concurrent.update/delete.SQL |2runtimeconnections:1commit/1conflict51503 | PASS, FINISH_DATA |
| T08-K01.concurrent.native.update/update | UIstalegiữdraft,khôngoverwritewinner,reloadtườngminh đúngrow | PASS, FINISH_CONFLICT |
| T08-K01.concurrent.native.update/delete | UIstalegiữdraft;reloadrowdeletedẩnSave | PASS, FINISH_CONFLICT |
| T08-K01.unknown.native | SQLCOMMIT→WAITFOR→commandtimeout:editorUnknown/khóasave/giữdraft;receiptGetxácnhận1row,đóngOK | PASS, FINISH_AUTH_CONFIRMED |
| T08-K02.7kind.required/maxplus1.native | Empty/spaces/tab/max+1 mỗi4case×7kind;ErrorProviderđúngfield,khôngwrite,snapshotgiữ |28/28PASS, FINISH_VALIDATION_DELETE |
| T08-K02.6kind.malformed.native | BAD00bịErrorProviderchặn;DMcấp mã không nhập |6/6PASS, FINISH_VALIDATION_DELETE |
| T08-K02.6kind.malformed.SQL | BAD00bị51506,snapshottoànDBgiữ,transaction0:0 |6/6PASS, CODE_SQL |
| T08-K02.6kind.duplicate.SQL | Dùngmãhistoryhiệncó bị2627,snapshotgiữ,transaction0:0 |6/6PASS, CODE_SQL |
| T08-K02.nv.NULL |2NVphoneNULLcùngtồn tại | PASS, FINISH_DATA |
| T08-K02.nv.duplicate | NVthứ2nonNULLtrùngbịchặn,phonecũgiữ | PASS, FINISH_DATA |
| T08-K05.7kind.unused.native |7case riêng:confirmYes,xóaDB/owninggrid |7/7PASS, FINISH_VALIDATION_DELETE |
| T08-K05.6kind.history.native |DM/NCC/PT/DT/CV/CA:confirmdeletebịchặn,row/snapshotgiữ |6/6PASS, FINISH_VALIDATION_DELETE |
| T08-K05.TC.history.nativeMouse | ChuộtnativechọnTC990;Deleteenable;FK/historychặn,row/snapshotgiữ | PASS, FINISH_EXPENSE_MOUSE |
| T08-K06.a |3role/3mappingChi/PT001/002supporttừngrowđúng,khôngseedtransactiondemo |9/9PASS, CLEAN_DEFAULTS |
| T08-K07.a.TC005/TC011/TC012.native | Deletekhóa,CodeReadonly,Typekhóa;renamecommit vàmapping/Chi giữ |3/3PASS, FINISH_PROTECTION |
| T08-K07.b.usedPayment | PT990support/renamehợplệ,deletebịchặn,historyexactJSONgiữ | PASS, FINISH_DATA |
| T08-K08.a.Validate3role×2purpose×5ID | PT001/002đúngquyềnaccept;PT003/010/lạreject,kểcảservicecall |30/30PASS, FINISH_DATA |
| T08-K08.b.PT010 | BLLvàSQLattemptenablebịchặn,support0 | PASS, FINISH_DATA |
| T08-K09.lookup6kind×3role.BLL |18case riêngallow/deny;DTOreflectionfields khônglộtàichính |18/18PASS, FINISH_DATA |

Các folder: `../runs/G02_FINISH_UI_CONFIRMED_20261011/`,
`G02_FINISH_REOPEN_20261011/`, `G02_FINISH_RESOURCE_20261011/`,
`G02_FINISH_DATA_20261011/`(59PASS/1FAILB03T06),
`G02_FINISH_AUTH_CONFIRMED_20261011/`(4PASS,1rawFAILởpopupsetupclean),
`G02_FINISH_CONFLICT_20261016/`, `G02_FINISH_VALIDATION_DELETE_20261016/`,
`G02_FINISH_EXPENSE_MOUSE_20261016/`, `G02_CODE_SQL_20261016/`,
`G02_CLEAN_DEFAULTS_20261016/`, `G02_FINISH_PROTECTION_20261016/`.
JSON lưu từngcase,expected/actual/status. Handoff giữharness,manifest97file.

Faulttimeout chèn đúngWAITFOR sauCOMMIT trên procedureDBtest cómarker rồi
restorebody trongfinally; không thay source/migration đểnéFAIL. Đây là lỗi
responseSQLthật trongapp, không chỉinjectunknownstate như regressionB01.
Không gọi đó là mô phỏng đầyđủ mọi hình thức mấtmạngTCP.

RawFAILTC.history ởlượtđầu:programmaticCurrentCell chưa chuyểnselectedrow/
enabledDelete nên không cóMessageBox. LượtchuộtWM_LBUTTONDOWN/UP chọnTC990
đãxácnhận đúnghànhvi người dùng vàPASS; không thêmbugapp từphépđo cũ.

**B06 mớiFAILDesigner**:2surfaceparseerror dòng47, hồsơ tại bug-report.md;
chờ duyệt bổsung, không sửa trong lượt. Không chốtT08Hoàn tất từ các kết quả
trên: còn đối chiếu đầyđủ acceptance matrix, các biênservice/SQL còn thiếu và
Designer saufix. Mọi rawhistoryFAIL giữ nguyên để truy vết.

Bổ sung cuối16/10: `G02_CODE_SERVICE_20261016/`13/13PASS (6kind×malformed/
duplicate gọiService trực tiếp, DMcodeallocation bỏclientcode); và
`G02_DEMO_RERUN_20261016/`PASS trênDBdemomới với003checksumhiện tại,
AlreadyApplied vàfullDBsnapshottrước/sau giữ nguyên, kểcảtênTC005custom,
supportPT001đãtắt vàtoànbộhistoryseed.

## Debug B06 đã duyệt — regression cuối16/10/2026

- Haiinitializer tách chainedassignment; gridAddRange làarray rõ. Bảngroot
  giữColumnStyleAutoSize tườngminh để bảo toànsemantics trước sửa.
- Lượt1sửaDesigner phát sinhgrid150%co5px ởbảnclone; đãgiữ2FAIL/diagnostic
  vàkếtthúclượt rồi sửa B06. Không chỉnh source trong khi đangghi lượt test.
- Runner cuối `../runs/20261016_211207_a26a8e4f/`:222/222PASS,177caseT08
  (gồm79boundaryB02), SourceUnchangedtrue,SetupErrornull,exit0.
- DesignerfinalEditor/Catalogs mở/save/close/reopen được, ảnh đãxem; clone
  sauSave đượcbuild/chạy, không chỉ xácnhậncaptionDesign.
- `G02_B03_B06_FINAL_CRUD_20261016/`:23/23nativePASS, gồm7kind add/edit,
 7kindliteral/Vietnamesesearch,7kindCancel/X×No/Yes,ownerrefreshNCC vàclose
  giữorderopen; không regresshandler/binding/vịtrífields sauSave.
- `G02_B03_B06_FINAL_LAYOUT_{96,120,144}_20261016/`:36/36nativePASS,
  có6exactviewport/minimize/restore,21editor vàkeyboard3DPI. Grid150%hếtdịhình.
- BinarynativehostSHA256khớpcloneWinFormsDLL sauSave;
  `G02_B03_B06_HANDOFF_20261016/integrity.json`:98fileproduct/migration/test/
  tools khôngđổi tronglượt cuối. Baseline/seed/001 không sửa;002B03chạyDBmới.

B06 đãsửa/regressionPASS, không ghiHoàn tấtG02 khi còncaWindowsidentity/
setup-popup. Freshrestoreclone báoNU1902/NU1903PKCS9.0.4, ghi riêng ở
`docs/bugs/pkcs-dependency-audit/report.md`, không tựđổi dependency trongtest.
