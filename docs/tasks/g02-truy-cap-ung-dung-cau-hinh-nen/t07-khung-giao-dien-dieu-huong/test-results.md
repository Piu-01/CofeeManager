# T07 — Kết quả native và phần còn thiếu

## Kết luận cuối 17/10/2026 — T07 7/7 ID gốc PASS

Đãđốichiếuđủsubcasebắtbuộc tại[ma trậnG02](../acceptance-matrix.md), gồm
3role/10lifecycle/100navigation/weakGC/DMV/loading/nháp/7editor/6viewport/
3DPIkeyboard/5Designer vàbinarysauSave. Khôngcònbug/blockernghiệmthuhiệnhành.
WindowsroundR7 bổsungactualruntimeexeWindowsidentity:Login→ADMINMain/catalogs,
ownerđúng,SQLApp1/Setup0/sysadmin0,exit0. Ảnh đãreview;source99filekhôngđổi
vàdeployedDLLhashkhớptại`../runs/G02_FINAL_ACCEPTANCE_20261017/`.

Cácbảngphíadưới là **lịch sử** trước/saufix;khôngxóaFAIL/BLOCKED cũ vàkhông
quycounttestthànhID. Đã bàn giaoGit,commit`1ab09af368f320a598a577d22542e41563e5e432`
đãpush`origin/main`; xem`../git-handoff.md`.

Source cố định của G02; native WinForms .NET10 trong STA testhost, app forms/
ApplicationSessionContext thật, runtimeDB G02_Runtime impersonation với
Pooling=false. DB riêng `CoffeeManager_Test_G02_20261009_214237_4969002a` có
marker. Không giả kết quả native bằng previewHTML. Chưa chốt7ID gốc PASS.

Evidence: `../runs/G02_NATIVE_{100,125,150}_20261009/` (DeviceDpi96/120/144),
`../runs/G02_UI_EXTRA_20261010/`, `../runs/G02_DESIGNER_20261009/`;
harness code/sha tại `../runs/harness-snapshot/`. Password sinhRAM, khôngCLI/
log. DPI desktop đã trả125% (Recommended) sau lượt150.

| Subcase | Expected | Actual / evidence | Kết quả |
|---|---|---|---|
| T07-K01.a.ADMIN | Đúngidentity/startpage,1Main | UI_EXTRA chờShown/recheck:1CatalogsControl,identitymatched,screenshotready | PASS |
| T07-K01.a.CASH | Trang phiên bán hàngchưa triển khai | UI_EXTRA chờready:1SessionHomeControl,identitymatched | PASS |
| T07-K01.a.STORE | Trang phiên khochưa triển khai | UI_EXTRA chờready:1SessionHomeControl,identitymatched | PASS |
| T07-K02.a.ADMIN | Menu và BLL đúng quyền | NativeCatalogs visible,1Main | PASS |
| T07-K02.a.CASH | Catalogẩn/directnavigate khôngđổi màn | UI_EXTRA directdenied,currentcontrolretained | PASS |
| T07-K02.a.STORE | Catalogẩn/directnavigate khôngđổi màn | UI_EXTRA directdenied,currentcontrolretained | PASS |
| T07-K02.b.loginlogout |10vòng1Main,dispose/revoke | Native10cycle:1Main mở,0Main saulogout,oldFormdisposed/Sessionrevoked | PASS |
| T07-K02.b.rolechange/lock | Mainđóng khi role/account đổi | BLLrevoke đãchạy; nativeMain với session2đổi/khóa chưa thao tác | NOT RUN |
| T07-K03.a.ADMIN |100chuyển khôngtănginstance | Cached2→2 đúngcùnginstance; visiblecontent1 | PASS |
| T07-K03.a.STORE |100chuyển khôngtănginstance | Cached2→2 đúngcùnginstance; visiblecontent1 | PASS |
| T07-K03.b |1callback/read;weakGC/SQLDMVkhôngtăng | Chưa đủcallback/RPC/weakrefGC/DMVtrước-sau; openscounter không thaySQLRPC | NOT RUN |
| T07-K04.a.100.1280 | Menu/nút/cột chính khôngcắt | Nativeactual1280×720,Dpi96; ADMINảnh đãxem, toolbar/header/status hiện | PASS |
| T07-K04.a.100.1366 | Menu/nút/cột chính khôngcắt | Nativeactual1366×768,Dpi96; screenshotriêng; phần gridvớitên dài chưa kiểm | NOT RUN |
| T07-K04.a.125.1280 | Menu/nút/cột chính khôngcắt | Nativeactual1280×720,Dpi120,screenshot; còn gridlongtext/keyboard | NOT RUN |
| T07-K04.a.125.1366 | Menu/nút/cột chính khôngcắt | Nativeactual1366×768,Dpi120; ảnhADMINđãxem,toolbarwrapkhôngcắt | PASS |
| T07-K04.b.150.1280 | Khôngheightclamp,resize/minimize/restore | Actual1280×720,Dpi144 vàảnhADMINđãxem; minimize/restore tại150chưa chạy | NOT RUN |
| T07-K04.b.150.1366 | Khôngheightclamp,resize/minimize/restore | Actual1366×768,Dpi144 vàảnhriêng; minimize/restore tại150chưa chạy | NOT RUN |
| T07-K04.restore.125.1280 | Minimize/restore giữviewport | UI_EXTRA actual1280×720 saurestore | PASS |
| T07-K04.restore.125.1366 | Minimize/restore giữviewport | UI_EXTRA actual1366×768 saurestore | PASS |
| T07-K04.c.Login | Designer save/close/reopen | ClonebuildOK; automationkhôngxácnhậndocumentclone,refuseSave | BLOCKED |
| T07-K04.c.Bootstrap | Designer save/close/reopen | Cùngblockerclone/foreground; khônggánảnhG01thànhG02 | BLOCKED |
| T07-K04.c.Main | Designer save/close/reopen | Cùngblocker; chỉIDEcacheSubtypeFormthayđổi,sourcekhôngsửa | BLOCKED |
| T07-K04.c.Editor | Designer save/close/reopen | Cùngblockerclone/foreground | BLOCKED |
| T07-K04.c.Catalogs | Designer save/close/reopen | Cùngblockerclone/foreground | BLOCKED |
| T07-K04.d.100 | Nativekeyboard/focus/loading/modal | Chưa thao tác đủ | NOT RUN |
| T07-K04.d.125 | Tab/ShiftTab/Enterkhôngcommit | UI_EXTRAbịmodalcascade trước casekeyboard; rawFAILkhôngđủgánbug | BLOCKED |
| T07-K04.d.150 | Nativekeyboard/focus/loading/modal | Chưa thao tác đủ | NOT RUN |
| T07-K05.a.repeat | Readslow,10clickkhônggửi lặp | Tìm enabled=True,thêmconnection/recheck,rawFAIL; G02-B01 | FAIL |
| T07-K05.a.error/cancel/retry | Xong/lỗi/hủy mở lại nút đúng | Sauunlocktải lại được; chưa đủ nhánherror/cancel | NOT RUN |
| T07-K05.b | Logout/readpending→unlockkhôngUI cũ | Nativelock TAIKHOAN→logoutoldFormdisposed→unlockkhôngMainmuộn | PASS |
| T07-K06.a.DM | Dirtycancelkhôngghi | LượtYes rowsmetadata khôngđổi; exactsnapshot/No ởUI_EXTRA timeout | BLOCKED |
| T07-K06.a.NCC | Dirtycancelkhôngghi | LượtYes rowsmetadata khôngđổi; exactsnapshot/No ởUI_EXTRA timeout | BLOCKED |
| T07-K06.a.PT | Dirtycancelkhôngghi | Exact-cancelmodalcascade làmharness nhiềuDialog; chưađủproof | BLOCKED |
| T07-K06.a.DT | Dirtycancelkhôngghi | Cùngmodalcascade,giữrawevidence | BLOCKED |
| T07-K06.a.TC | Dirtycancelkhôngghi | sys.partitionscountkhác khônglàproofghi; exacttestblocked | BLOCKED |
| T07-K06.a.CV | Dirtycancelkhôngghi | Cùngmodalcascade,giữrawevidence | BLOCKED |
| T07-K06.a.CA | Dirtycancelkhôngghi | Cùngmodalcascade,giữrawevidence | BLOCKED |
| T07-K06.b | SaveNCC refresh đúngtab/filter | NativeSaveđóngdialog,NCCgrid1row,tab/filtergiữ | PASS |
| T07-K07.a | Ởlại/Bỏnháp khi rời/đóng/logout | CaseNo/Yes chưa xácnhậnđầyđủ vìmodal thao tácblocked | BLOCKED |
| T07-K07.b | Đóngappkhônghủyorderopenfixture | SQLfixtureopenordercó ởrunnerT08; nativeclose+đốichiếu riêng chưa chạy | NOT RUN |

RawFAIL ởMenuCASH/STOREdo chưaready đã được đốichiếu bằngUI_EXTRA và không
đổiexpected. Rawcancel/transactionđo khôngđủchứngcứ vẫn giữ, không tự néFAIL.
Bugappconfirmed duy nhất của các ca T07 hiện là B01 sở hữuT08; xem bug-report.
GUI cần thao tác đángtin cậy trênDesigner vàmodal/keyboard để hoàn tấtca còn
BLOCKED. Khôngghi7/7PASS hoặcHoàn tất từ các kết quả trên.

## Regression B01 đã duyệt — 11/10/2026

Bản source sau debug, DB mới `CoffeeManager_Test_G02_20261011_171040_3f1e9b28`,
migration001→002→003 checksum mới; native product Login/Main/CatalogsControl
trongSTAhost, runtime user impersonation/Pooling=false. Harness đặt SQLcommand
timeout1s để inject read error; không sửa procedure/quyền DB để giả lỗi.

Evidence: `../runs/G02_B01_REGRESSION_20261011/results.json` và2ảnh native;
source/schema manifest tại `../runs/G02_B01_B02_REGRESSION_20261011/`.

| Ca / liên quan | Expected | Actual | Kết quả |
|---|---|---|---|
| B01.slowRead / T07-K05.a | Read khóa SQL,10click không thêmrequest; page/filtergiữ | Tìm/Tải disabled;10clicklặp +40directhandler calls không thêmread;1open khi pending,2open tổng=recheck+List; nút mở lại | PASS |
| B01.cancelRead.retry / T07-K05.a | Cancel mở lại nút, retry được | Cancel linkedtoken khi SQLlocked; nút mở lại; unlock vàTải hoàn tất | PASS |
| B01.readTimeout.retry / T07-K05.a | Error giữinput, nút mở lại, retry được | Commandtimeout1s báoerror; filtertiếngViệt giữ; Tải sauunlock hoàn tất | PASS |
| B01.unknownDelete.reconcile | Không khóa mấtTải đối chiếu | FixtureunknownDelete từrowcóDB; Tìmkhóa/Tải bật; Get/refresh xóaunknown vàmởlạinút | PASS |
| B01.logout.pendingRead / T07-K05.b | KhônglateUI saulogout | Main/control disposed; unlock khôngreopen, chỉLoginvisible | PASS |

5/5ca regressionPASS thayFAIL B01 và bổ sung nhánherror/cancel/retry trong
phạm vi trên. Source sản phẩm cố định97file; host có1warning unusedfield,
không phải warning của build sản phẩm. Chưa thayPASS cho keyboard/modal,
Designer/GC/tài nguyên hay simulation mấtresponse sauwrite chưa chạy.

## Nghiệm thu bổ sung 11–16/10/2026

Các nhánh dưới đã chạy trên source sauB01/B02, không sửa trong lượt.

| Subcase | Actual | Kết quả / evidence |
|---|---|---|
| T07-K02.b.rolechange | Mainđangmở; session2đổiADMIN→CASH; nativeactiontiếp theo revoke/đóngMain | PASS, FINISH_RESOURCE |
| T07-K02.b.accountlock | Session2khóaaccount; nativeactiontiếp theo revoke/đóngMain | PASS, FINISH_RESOURCE |
| T07-K03.b.request/event |100chuyển;1clickTải đúng2connection=recheck+List; logoutcallbackđúng1 | PASS, FINISH_RESOURCE |
| T07-K03.b.SQLDMV | sys.dm_exec_sessions theoPID+AppNameG02FinishRuntime:0→0,Pooling=false | PASS, FINISH_RESOURCE |
| T07-K03.b.weakGC | Main,session vàmọicachedcontrol weakrefdead sauforcedGC | PASS, FINISH_RESOURCE |
| T07-K04.a.100.1280 | Actual1280×720,DPI96; minimize/restore; ảnh7tab | PASS, LAYOUT_CONFIRMED_96 |
| T07-K04.a.100.1366 | Actual1366×768,DPI96; minimize/restore; ảnh7tab | PASS, LAYOUT_CONFIRMED_96 |
| T07-K04.a.125.1280 | Actual1280×720,DPI120; minimize/restore; ảnh7tab | PASS, LAYOUT_CONFIRMED_120 |
| T07-K04.a.125.1366 | Actual1366×768,DPI120; minimize/restore; ảnh7tab | PASS, LAYOUT_CONFIRMED_120 |
| T07-K04.b.150.1280 | Actual1280×720,DPI144,khôngheightclamp; minimize/restore; ảnh7tab | PASS, LAYOUT_CONFIRMED_144 |
| T07-K04.b.150.1366 | Actual1366×768,DPI144,khôngheightclamp; minimize/restore; ảnh7tab | PASS, LAYOUT_CONFIRMED_144 |
| T07-K04.d.100 | NativeTab/ShiftTab/AltTfocus; Enterkhôngsave; modalownerWin32disabled; DBsnapshotgiữ | PASS, LAYOUT_CONFIRMED_96 |
| T07-K04.d.125 | Cùngthao tác tạiDPI120 | PASS, LAYOUT_CONFIRMED_120 |
| T07-K04.d.150 | Cùngthao tác tạiDPI144 | PASS, LAYOUT_CONFIRMED_144 |
| T07-K04.editor.7kind×3DPI |21editor riêng:Savevisible trongclientviewport,ảnh riêng |21/21PASS,3lượtLAYOUT_CONFIRMED |
| T07-K04.c.Main | Đúngsolution/surface/save/close/reopen,sourceMainkhôngđổi | Thao tácPASS, DESIGNER_SOLUTION; chưa chốt toàncaDesigner |
| T07-K04.c.Login | DesignerSave đổiRowStyle gâyclipping; runtimeclone sauSave táihiện | FAILB04 sởhữuT06 |
| T07-K04.c.Bootstrap | Designerparseerror dòng38 | FAILB05 sởhữuT06 |
| T07-K04.c.Editor | Designerparseerror dòng47 | FAILB06 sởhữuT08 |
| T07-K04.c.Catalogs | Designerparseerror dòng47 | FAILB06 sởhữuT08 |
| T07-K06.a.DM | Hủy/X×Không/Có:Khônggiữinput,Cóbỏnháp,exact7tablesnapshotgiữ | PASS, FINISH_UI_CONFIRMED |
| T07-K06.a.NCC | Cùng4nhánh native | PASS, FINISH_UI_CONFIRMED |
| T07-K06.a.PT | Cùng4nhánh native | PASS, FINISH_UI_CONFIRMED |
| T07-K06.a.DT | Cùng4nhánh native | PASS, FINISH_UI_CONFIRMED |
| T07-K06.a.TC | Cùng4nhánh native | PASS, FINISH_UI_CONFIRMED |
| T07-K06.a.CV | Cùng4nhánh native | PASS, FINISH_UI_CONFIRMED |
| T07-K06.a.CA | Cùng4nhánh native | PASS, FINISH_UI_CONFIRMED |
| T07-K06.b | SaveNCC giữtab/filter,partnerJSONkhôngđổi | PASS, FINISH_UI_CONFIRMED |
| T07-K07.b | NativeđóngMain:openHOADON/CHITIET_HD exactJSONgiữ | PASS, FINISH_UI_CONFIRMED |

Evidence tương ứng: `../runs/G02_FINISH_RESOURCE_20261011/`4/4PASS,
`G02_FINISH_UI_CONFIRMED_20261011/`23/23PASS,
`G02_FINISH_LAYOUT_CONFIRMED_{96,120,144}_20261016/`mỗi10/10PASS;
`G02_DESIGNER_SOLUTION_20261011/`và`G02_DESIGNER_FINAL_20261016/`.
Không cộngcount này thành IDgốc. DPI trả125%Recommended sau từngđợt.

### RawFAIL/harness và phần còn thiếu

- Lượtlayoutđầu kiểmmanagedForm.Enabled, không phản ánh modalWin32disable;
  lượtCONFIRMED dùngIsWindowEnabled và bànphím thật,30/30PASS. Giữrawkhôngsửa.
- LượtUIđầu bịharnesssyncwaitSQL trênUIthread ởbướcđóngapp; lượtCONFIRMED dùng
  truyvấnsyncchođốichiếusauclose. Lượt trung gian tạo fixturetrùng; giữraw,
  dọnđúngfixturecủaharness rồi chạylượt mới, không đổi source/expected.
- Designertrước mở nhầmfile ngoàiproject. TypedCOM+Solution.Openđãkhắcphục
  điều khiểnIDE; screenshot sauđó xácnhận lỗiappB04–B06. CaptionDesignvà nhãn
  OBSERVED khônglàPASS:3surfaceerror/Loginclipping đềuFAIL quađọcbằngchứng.
- SQLloginXE tronglượtDesignerfinal không cólogin event; chỉlưu PID/AppName,
  khôngSQLtext/hash/password. Điều này không thay kết quảFAILDesigner.
- T07-K07.a cònthiếu đủ3hướngnavigate/logout/close vớiỞlại/Bỏnháp khiowner
  códraft; không lấy các caseHủy/Xeditor đãPASS thay toàn bộ case này.

Bổ sung sau đó: `../runs/G02_FINISH_LEAVE_CONFIRMED_20261016/` có3/3PASS
choNavigate/Logout/Close của luồngmodal thực tế. Mỗi case xácnhận parentnative
disabledkhidraft, X/Khônggiữinput, X/Cóbỏnháp rồiparentactionthànhcông,
exact7tablesnapshotkhôngđổi. Khônggiảlậpnhápinline/POSngoàiphạmviG02.
Rawlượttrước2PASS/1FAIL doReady()harnesscastHome→Catalog quá sớm; lượt mới
đợinavigationhoàntất, không thay sourcehayexpected đểnéFAIL.

## Sau debug B03–B06 — bản sauSave, lượt cuối16/10/2026

Designer của5Form/control đãmở đúnghierarchyCafeManager.WinForms, Save/close/
reopen; ảnhở `../runs/G02_B03_B06_DESIGNER_FINAL_CONFIRMED_20261016/` đãxem.
Không cònparseerror, Login không cònAbsolute20row. Driver mới chọnHWND từDTE,
guardchỉsolutionTemp; rawCOMbusy/TypeGuidtrung gian khôngđược nângPASS.

Clone sauSave đượcbuild, nativehost chỉreference clone này; SHA256WinFormsDLL
tronghost khớpDLLclone, khôngphảibinaryworkingtree. KhôngcopysourceIDEserialize
ngược vào project. Proof vàharness tại `../runs/G02_B03_B06_HANDOFF_20261016/`.

| Lượt / case riêng | Actual | Kết quả |
|---|---|---|
| FINAL_LAYOUT_96.viewport1280/1366 | Exact2size,DPI96,minimize/restore;7tabscreenshotmỗisize,grid/buttonsusable |2/2PASS |
| FINAL_LAYOUT_120.viewport1280/1366 | Exact2size,DPI120,cùngthao tác |2/2PASS |
| FINAL_LAYOUT_144.viewport1280/1366 | Exact2size,DPI144,cùngthao tác,gridkhôngco5px |2/2PASS |
| Keyboard96/120/144 | Tab/ShiftTab/AltTđúngfocus,Enterkhôngghi,parentWin32modaldisabled |3/3PASS |
|7editor×3DPI | Mỗieditorcócase/ảnhriêng,Savevisiblevànằmtrongclientbounds |21/21PASS |
| Login96/120/144 sauSave | Labels/buttonđủheight,masktrue,8rowổnđịnh |3/3PASS |
| Bootstrap96/120/144 sauSave | Fields/mode/candidate/mask/button và9rowổnđịnh |3/3PASS |

Mỗi `G02_B03_B06_FINAL_LAYOUT_{96,120,144}_20261016` có12/12PASS;
tổng36case, khôngđổi thành36IDnghiệmthu. SauSaveCRUD/cancel7kind23/23PASS ở
`G02_B03_B06_FINAL_CRUD_20261016/`,T08ghi các nhánh.

Lượt1 có2viewport150%FAIL, diagnosticgrid886×5/tabpageY235 đãgiữ. Đãkếtthúc
lượt1,debugB06phầnColumnStylePercentmới thêm, đổi thànhAutoSize tươngđương
defaultcũ; sourcebảncuối đượcđóngmanifest98file mới trướcregression. Không
đổi expected, khôngchỉnhheightclamp hoặcảnh đểné lỗi. DPIđãtrả125%Recommended.

T07-K04.c không cònblockerB04–B06; lịch sửFAIL còn nguyên. Không tựnghiệmthu
toànG02 từcaDesigner/36case hoặc bỏquaWindowsprincipal/setup-popup củaT06.
