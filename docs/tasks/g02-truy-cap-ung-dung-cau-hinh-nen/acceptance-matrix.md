# G02 — Đối chiếu nghiệm thu cuối ngày 17/10/2026

**27/27 ID gốc PASS: T06 10/10, T07 7/7, T08 10/10.** Không thêm/bỏ/đổi
ID hoặc giảm các subcase bắt buộc trong ba plan. Bảng này là chỉ mục nghiệm
thu; expected/actual từng nhánh, fixture và bằng chứng chi tiết được giữ tại
`test-results.md` của task và JSON/TRX/ảnh trong `runs/`.

Các lượt test giữ source cố định. Bug B01–B07 đều đã được duyệt sửa và có
regression; raw FAIL/BLOCKED lịch sử không xóa hoặc sửa thành PASS. Kết quả
mới thay đúng nhánh đã được kiểm lại. SEC-PKCS-01 cũng đã vá và regression.

## T06 — Bootstrap, auth và quyền

Chi tiết: [test-results](t06-bootstrap-dang-nhap-phan-quyen/test-results.md).

| ID | Nhánh bắt buộc đã đối chiếu / actual | Bằng chứng chính | Kết quả |
|---|---|---|---|
| T06-K01 | Clean đúng1ADMIN/1NV/SDTNULL, atomicCompleted; demo chỉ bật account chọn; existingNV không đổi hồ sơ; legacy verify giữ hash; invalid/inactive/departed không reset | Auth supplement28; runner race/rollback; Windows R7 clean/setup-success-popup | PASS |
| T06-K02 | Hai session một commit; lỗi giữa account/state rollback; lần2/restart/lock/departure không mở lại; runtime229; timeout sauCOMMIT đối chiếu không retry tạo | Runner AuthIntegration; FINISH_AUTH_CONFIRMED; Windows R7 restart/lock/departure/runtime-denied | PASS |
| T06-K03 | Đúng/sai/không có username; placeholder/malformed an toàn; password ngoài space không bị trim | AuthRules, Auth supplement28; native3role và Windows R7 | PASS |
| T06-K04 | Account0/employee0 riêng biệt không login; đổi giữa phiên revoke trước nghiệp vụ | Auth supplement; FINISH_DATA; FINISH_RESOURCE | PASS |
| T06-K05 | VerifyoldV3 cần rehash; DBhash/stamp nâng thật; hai session CAS không ghi đè password mới | Runner AuthIntegration; Auth supplement28 | PASS |
| T06-K06 | Đủ quyền3role; directread/write trái quyền bị chặn; đổirole revoke objectcũ, loginmới quyềnmới | AuthRules; FINISH_DATA; CatalogIntegration quyền7kind×3role×4action | PASS |
| T06-K07 | Login/setup masked; clear sau success; session không credential; rà luồng request/DAL/BLL/logger không emit plaintext/hash | FINISH_AUTH; B07_UI15; Windows R7 setup-masked; contextual audit ở T06 test-results | PASS |
| T06-K08 | Logout revoke/dispose/đóng màn quyền; objectcũ không gọi được; login/read pending không mở lại phiên sau unlock | Native10cycle; FINISH_AUTH_UIA; FINISH_RESOURCE; B01 regression | PASS |
| T06-K09 | DB lỗi không login hoặc báo sai password giả; native input giữ | FINISH_AUTH_CONFIRMED/FINISH_AUTH_UIA; DataAccess regression | PASS |
| T06-K10 | UI/C#/SQL length1/50/51, trim/case/charset; duplicate/key và legacy collision rollback; CI/CS; từngASCII,Unicode và `_`/`-` hợp lệ | Runner484; B07_SQL83; B07_UI15; B03Bootstrap17; Auth supplement28 | PASS |

## T07 — Shell và điều hướng

Chi tiết: [test-results](t07-khung-giao-dien-dieu-huong/test-results.md).

| ID | Nhánh bắt buộc đã đối chiếu / actual | Bằng chứng chính | Kết quả |
|---|---|---|---|
| T07-K01 |3role đúng tên/vai trò/trang đầu; ADMIN mở catalog, CASH/STORE màn chưa triển khai; runtime exe thật vào ADMIN | UI_EXTRA; Windows R7 runtime-authenticated-main | PASS |
| T07-K02 |3role menu/directnavigate;10login-logout mỗi phiên1Main; rolechange/accountlock đóng và revoke | Native10cycle/UI_EXTRA; FINISH_RESOURCE | PASS |
| T07-K03 | ADMIN/STORE100chuyển đúnginstance;1click→1recheck+List;callbacklogout1;weakrefGC;SQLDMV0→0 | Native100transition; FINISH_RESOURCE4 | PASS |
| T07-K04 | Đủ6viewport1280×720/1366×768×100/125/150%;minimize/restore;keyboard3DPI;5surfaceDesignerSave/close/reopen;binary sauSave;21editor | DESIGNER_FINAL_CONFIRMED; FINAL_LAYOUT96/120/144 mỗi12PASS; postSave binaryhash | PASS |
| T07-K05 |10clickreadslow không gửi lặp; error/cancel mở lại nút,retry;logout pending không lateUI | B01_REGRESSION5; FINISH_RESOURCE | PASS |
| T07-K06 |7editor×Hủy/X×Không/Có giữ/bỏ nháp,DBsnapshot đúng; SaveNCC chỉrefreshowner/filter | FINAL_CRUD23; FINISH_UI_CONFIRMED | PASS |
| T07-K07 | Navigate/Logout/Close modal draft đúngỞlại/Bỏnháp; không autosave; đóngapp không hủy HOADON/CHITIET đã lưu | FINISH_LEAVE_CONFIRMED3; FINAL_CRUD23 openorder snapshot | PASS |

## T08 — Bảy danh mục và selector

Chi tiết: [test-results](t08-danh-muc-nen/test-results.md). Các nhánh7kind là
DM/NCC/PT/DT/TC/CV/CA; không lấy một kind thay cả nhóm.

| ID | Nhánh bắt buộc đã đối chiếu / actual | Bằng chứng chính | Kết quả |
|---|---|---|---|
| T08-K01 |7CRUD/search tiếngViệt/literal/reopen process; update-update/update-delete hai SQLsession và staleeditor; timeoutcreateDM1row/receipt đối chiếu | Runner CatalogIntegration; FINAL_CRUD23; FINISH_REOPEN7; FINISH_DATA/CONFLICT/AUTH | PASS |
| T08-K02 |7kind tênrequired/max/max+1;6mã nhập malformed/duplicate và DMserverallocation; NCCNULL/empty/trùng/0đầu/15/16; NVNULL2row/trùng; FK/actor/stamp sai không ghi | BoundaryRegression79; PHONE10; nativeVALIDATION; CODE_SQL12/CODE_SERVICE13; FINISH_DATA; REMAINING_SQL20 | PASS |
| T08-K03 | DT-1/100.01,CVphụ cấp-1/hệ số.99,CAequal bịchặn ởC#/SQL,DBkhôngđổi | CatalogRules; REMAINING_SQL20 | PASS |
| T08-K04 | DT0/100,CVphụ cấp0/hệ số1;CA22→06hợp lệ,equal bịchặn,typed/persist đúng | CatalogRules; REMAINING_SQL20; CRUD | PASS |
| T08-K05 |7kindunused xóa;7history bịchặn,khôngcascade; nativeTC chọnchuột thật | CatalogIntegration; FINISH_VALIDATION_DELETE; FINISH_EXPENSE_MOUSE | PASS |
| T08-K06 | Cleanroles/3mapping/PTdefaults khôngseed; demo003rerunAlreadyApplied giữ fullsnapshot/customsupport/history;3preflightconflict rollback | CLEAN_DEFAULTS9; DEMO_RERUN; EXTRApreflight | PASS |
| T08-K07 |3mãchi hệthống chưa history không đổiPK/Loai/delete từUI/service/SQL;renameallowed;usedPTsupport/rename giữhistory/deleteblocked | PROTECTION3; EXTRArawSQL; FINISH_DATAusedPayment | PASS |
| T08-K08 | Selector3role×2purpose;Validate3role×2purpose×5ID;newPTsupport0→enable→rename→disable;010khôngbật | REMAINING_SQL20; FINISH_DATA30; CatalogIntegration | PASS |
| T08-K09 |7kind×3role×4action directservice đúngquyền;lookup×3role/DTOkhônglộfinancialfields | Runner CatalogIntegration; lookupSQL18/BLL18 | PASS |
| T08-K10 | Sửa ratio hiện tại không đổi snapshotDT hoặc tài chính của đơnpaid | Runner CatalogIntegration snapshotJSON | PASS |

## Chỉ mục evidence cuối / kế thừa

- `runs/20261017_155948_e814a21e/`:484/484xUnitPASS,source99file khôngđổi;
  TRX lưu actual từngtest, không quy484 thànhIDgốc.
- `runs/G02_B07_SQL_SUPPLEMENT_20261017/`:83/83PASS;
  `G02_B07_UI_REGRESSION_20261017/`:15/15PASS.
- `runs/G02_WINDOWS_REAL_R7_20261017/`:8/8recordPASS, haiWindowsidentity
  trùngSQLprincipal,khôngsysadmin;clean nativeOK/exit0,actualexe restart/lock/
  departure vàruntime login. Đã reviewảnh thực tế, không chỉcaption/status.
- `runs/G02_FINAL_ACCEPTANCE_20261017/integrity-and-cleanup.json`:99file
  khôngđổi, deployedDLL SHA256khớpworkingtree;0Windowsaccount/DBuser/SQLlogin
  tạm cònlại. Script/harness/ảnh/manifest giữ; không lưu password/hash.
- B03–B06: `runs/G02_B03_B06_HANDOFF_20261016/`,
  `G02_B03_B06_DESIGNER_FINAL_CONFIRMED_20261016`,
  `G02_B03_B06_FINAL_CRUD_20261016`,
  `G02_B03_B06_FINAL_LAYOUT_{96,120,144}_20261016`.
- Các tên FINISH/CODE/CLEAN/DEMO ở bảng được giải thích đầy đủ trong biên bản
  task; source/hash vàharness ở `G02_FINISH_HANDOFF_20261016`.
- PKCS: `runs/SEC_PKCS_20261016`, `SEC_PKCS_20261017`, và
  `docs/bugs/pkcs-dependency-audit/test-results.md`. Actual WinForms load10.0.13;
  DAL sau runtimepatch20/20PASS. README yêu cầu runtimevá khi triển khai.

## Bàn giao

Đủ điều kiện **chốt kỹ thuật/nghiệm thu G02**; không còn ca bắt buộc
FAIL/BLOCKED/NOT RUN sau đối chiếu. Raw lịch sử không bị xóa. Chỉ bàn giao kỹ
thuật ở đây; sau đó người dùng đã yêu cầu push. Commit G02
`1ab09af368f320a598a577d22542e41563e5e432` đãpush`origin/main`; xem
[git-handoff.md](git-handoff.md). Giữ mốc G01
`deabac108cbeddaa18126733ad18803b0626d7d7` và manifest source G02.
Không tự triển khaiG03 hoặc đổi325ca toàn dự án.
