# G01 — Báo cáo đợt test G01_20261008_220313

## Chốt kiểm chứng kỹ thuật — B10 PASS ngày 09/10/2026

**Duyệt bàn giao Git 09/10/2026:** Người dùng yêu cầu commit vào
`https://github.com/Piu-01/CofeeManager.git`. Bản bàn giao gồm source G01,
SQL/tooling, hướng dẫn và toàn bộ lịch sử kiểm chứng; cấu hình local, bin/obj,
credentials không đưa vào Git. Commit và remote được đối chiếu trong bàn giao
cuối phiên; hash tra bằng `git log`. Các dòng chờ cho phép commit dưới đây là
lịch sử trước yêu cầu này. G02 chưa triển khai.

**G01 đủ kiểm chứng kỹ thuật, chờ cho phép commit bàn giao.** 45/45 mã nghiệm
thu nguyên bản PASS kế thừa; runner failure B04 PASS; bổ sung DPI, bàn phím,
scroll/minimize và Save/close/reopen Designer đã PASS trên bản sau Save B10.
Debug/Release 0 warning/0 error; source cố định Count0, máy trả125%.
[Biên bản B10](t05-dal-nen-kiem-thu/b10-test-results.md).
B01–B10 đã xử lý theo duyệt; không còn blocker kỹ thuật được ghi nhận.
Chưa commit và chưa triển khai G02. Chưa ghi Hoàn tất bàn giao vì kế hoạch
yêu cầu lưu commit; các trạng thái FAIL/chờ duyệt phía dưới là lịch sử.

## Mới nhất — B09 regression và B10 phát sinh

B09 đã triển khai theo duyệt: bounds đúng tại DPI 100/125/150%, hai kích thước;
Tab/Shift+Tab và cuộn nhãn trước Save có chứng cứ PASS. Nhưng **Save Designer
viết lại source làm mất cấu hình grid**, có warning CS0649 và ảnh 150% không còn
dòng grid tại 1280×720. Đã test bản sau Save trên source cố định, ghi **B10**
tại [bug-report.md](t05-dal-nen-kiem-thu/bug-report.md), chờ duyệt bổ sung.
[Biên bản B09](t05-dal-nen-kiem-thu/b09-test-results.md).
G01 vẫn **Đang làm**; 45/45 mã nguyên bản PASS giữ lịch sử, chưa chốt nghiệm thu.
Máy đã trả 125%; không commit. Các trạng thái chờ B09/keyboard phía dưới là lịch sử.

## Hiện trạng — regression B03–B06 đã duyệt ngày 08/10/2026

**Mới nhất kiểm chứng bổ sung09/10:** Runner failure B04 **PASS**: fixture lỗi
có chủ đích trên DBmới, thu đủ20 results(19PASS/1FAIL), runner/dotnetExit1,
SetupErrornull và TRX đầy đủ. Không thay acceptance45 mãPASS bằng faulttest.
DPIWindows thật100/150 đã thử, máy trả125%; **150%FAIL B09**: Form bị clamp
height812 thay720/768 bởi MinimumSize. Tab tiến100% đúng, ShiftTab/150%focus
ngoài app nên chưa kết luận/không tickPASS keyboard đầy đủ. Source không đổi.
Biên bản [supplement](t05-dal-nen-kiem-thu/supplement-test-results.md);
B09 mới tại T05bug-report, chờ duyệt sửa/regression. G01 vẫn Đang làm.
Nhận xét bên dưới “DPI150/runner-failure NOT RUN” là lịch sử trước lượt này.

**Mới nhất09/10 — B08 regression PASS:** Windows app account thật, hủy trước
load đầu/có10 bàn/hủy lặp hiển thị đúng Đã hủy tải; unlock không late update,
reload được, close khiblocked process thoát/requests0. Debug/Release0 warning/
0 error, source không đổi trong lượt. [B08 biên bản](t05-dal-nen-kiem-thu/b08-test-results.md).

| Ca nguyên bản hiện tại | PASS | FAIL | BLOCKED |
|---|---:|---:|---:|
| T01 kế thừa | 7 | 0 | 0 |
| T02 kế thừa | 12 | 0 | 0 |
| T03 | 9 | 0 | 0 |
| T04 | 7 | 0 | 0 |
| T05 | 10 | 0 | 0 |
| **45 mã** | **45** | **0** | **0** |

B01–B08 đã sửa/kiểm chứng theo các lượt duyệt; B02/B07 layout runtime125% và
Designer có chứng cứ. **G01 chưa chốt Hoàn tất:** kiểm chứng bổ sung trong plan/
guide về DPI100/150 và keyboard Tab/focus còn NOT RUN; nhánh nonzero/stderr
của runner sửa B04 chưa cưỡng bức test lại. Commit bàn giao chưa được giao.
Không biến45 mã PASS thành tuyên bố đã chạy mọi subcase bổ sung. Bước tiếp:
kiểm các phầnUI/runner còn thiếu, sau đó bàn giao nhóm theo quy trình.
Source/JSON/PNG của B08 ở runs/G01_B08_20261009_111336; giữ lịch sử FAIL trước.
App đã đóng có chủ đích trong ca close, không phải crash.
Các trạng thái B08 FAIL/chờ duyệt phía dưới là lịch sử.

**Tiến độ B08 sau duyệt09/10:** Đã sửa nhánh cancellation/hiển thị trạng thái
ở StartupForm, không đổi Designer/SQL/DAL. Debug/Release0 warning/0 error,
manifest/logs ở runs/G01_B08_20261009_111336. Mở hộp thoại credential Windows
để chạy bản mới bằng app account; runtime regression còn chờ người dùng nhập
tại máy. K07 chưa được đổi từ FAIL sang PASS chỉ vì build/source đã sửa.

**Mới nhất09/10/2026:** Đã chạy app PID30036 dưới Windows account CafeManagerApp,
SQL session xác nhận identity riêng, tải10 bàn. T03-K08 **PASS**. Query bị khóa
không freeze (resize83ms), hủy dừng request SQL, reload được; đóng khi chờ process
thoát, SQL requests0. Tuy nhiên sau hủy text vẫn “Đang tải…”: **T05-K07 FAIL**,
bug mới **B08** chờ duyệt debug/regression. Tổng **44 PASS/1 FAIL/0 BLOCKED**,
G01 vẫn Đang làm. DPI100/150 và Tab/focus bổ sung vẫn NOT RUN, không tick ngầm.
Biên bản: [manual runtime](t05-dal-nen-kiem-thu/manual-test-results.md);
bug: [T05 bug-report](t05-dal-nen-kiem-thu/bug-report.md).
Source không sửa suốt lượt manual; chưa debug B08. Harness raw PASS chỉ gồm
assertions kỹ thuật, actual text từ JSON/PNG dẫn đến nghiệm thu ca FAIL.
Các tổng43/2 và nhận xét chờ account phía dưới là lịch sử.

**Tiến độ principal:** Người dùng đã tạo CafeManagerApp; SQL login/user/role đã
provision trên DB test manual. Probe quyền: sysadmin0/db_owner0/app role1, đọc10
bàn, procedure trả51109 đúng, direct UPDATE bị229. Đã mở RunAs để chạy app
dưới Windows identity riêng; chờ nhập mật khẩu tại máy và kiểm process/UI.
T03-K08/T05-K07 vẫn BLOCKED, chưa báo PASS từ SQL impersonation thay app process.
Bằng chứng runs/G01_Manual_20261008_225305_cebd98ad/app-principal-check.txt.
Các nhận xét “chưa có account” phía dưới là lịch sử trước lượt provision này.

**Mới nhất sau B07:** Designer bản mới đã được DTE lưu/đóng/mở lại và xác nhận
bằng UIAutomation/ảnh native, source không đổi. T04-K05 PASS, tổng hiện tại
**43 PASS/0 FAIL/2 BLOCKED** (T03-K08, T05-K07). Máy vẫn chưa có Windows
account CafeManagerApp và SQL login app riêng; chờ tạo account trước test runtime.
Bằng chứng Designer ở runs/G01_B07_20261008_232240/designer-*.
Các tổng42/3 phía dưới là lịch sử trước regression Designer thành công.

**Cập nhật B07:** Người dùng đã duyệt sửa; đã chỉnh StartupForm theo guide và
kiểm chứng native125% tại1280×720/1366×768, thiếu config/ready/loading/lỗi,
grid với DTO minh họa. Debug/Release0 warning/0 error. Bằng chứng ở
[B07 regression](t05-dal-nen-kiem-thu/b07-test-results.md) và
`runs/G01_B07_20261008_232240/`. Source không đổi trong lượt regression.
Sau sửa Designer, T04-K05 cần kiểm save/reopen lại trên source mới nên trạng thái
hiện tại **42 PASS/0 FAIL/3 BLOCKED** (T03-K08, T04-K05, T05-K07).
Xác nhận Designer trước B07 vẫn giữ bằng chứng cho source trước sửa.
Ảnh grid minh họa không thay nghiệm thu query SQL dưới account app riêng;
DPI100/150 và Tab/focus chưa chạy. Các số bên dưới là lịch sử.

**Phát sinh từ ảnh runtime:** G01-B07 tại T05/bug-report.md: StartupForm mới
áp dụng một phần palette/font, grid/bố cục/disabled/state chưa đạt hình thức
guide; chờ duyệt sửa riêng. Các PASS kỹ thuật không phải nghiệm thu hình thức.
Nhóm vẫn Đang làm; tiếp tục hai ca manual phụ thuộc principal khi có account.

**Cập nhật mới nhất:** Người dùng xác nhận lưu/đóng/mở lại Designer thành công:
T04-K05 PASS, T04 **7/7 PASS**. Tổng G01 **43 PASS/0 FAIL/2 BLOCKED** (T03-K08,
T05-K07). Ảnh Class Library khi Start chưa chứng minh lỗi source; WinForms
OutputType=WinExe, cần chọn đúng startup project. Ảnh SSMS Personalization
Account là tài khoản công cụ, chưa phải Windows app principal riêng.
Các số 42 PASS/3 BLOCKED phía dưới giữ lịch sử trước xác nhận.

**Cập nhật manual:** Người dùng đã duyệt tiếp tục và chọn tự tạo tài khoản
Windows riêng, tự kiểm chứng Designer. Đã chuẩn bị DB test mới với 10 bàn,
run `G01_Manual_20261008_225305_cebd98ad`; chờ tên account và bằng chứng Designer.
Hướng dẫn/expected/actual: [manual-checklist.md](manual-checklist.md).
Ba ca vẫn BLOCKED đến khi có bằng chứng; tổng 42 PASS/3 BLOCKED chưa đổi.

Đã nhận ảnh StartupForm Designer hoạt động ở DPI 125%; chờ xác nhận save/reopen
để chốt T04-K05. Ảnh SSMS vẫn là login Lenovo sysadmin, chưa có app principal.
Kiểm manifest source: 0 file hiện có đổi, chỉ thêm csproj.user local; không sửa/xóa.

Người dùng duyệt cả đợt bằng “duyệt”. Đã sửa B03/B04/B05 và xác định, sửa
nguyên nhân B06 trong fixture. Đợt `G01_Regression_20261008_222427` giữ source
cố định (source-stability.json count=0), baseline/migration không đổi.

| Task | PASS | FAIL | BLOCKED |
|---|---:|---:|---:|
| T01 kế thừa | 7 | 0 | 0 |
| T02 kế thừa | 12 | 0 | 0 |
| T03 | 8 | 0 | 1 |
| T04 | 6 | 0 | 1 |
| T05 | 9 | 0 | 1 |
| **45 ca** | **42** | **0** | **3** |

- T03 run `20261008_222434_ca0e775e`: **10/10 subcase SQL PASS**, bao gồm
  hai session cấp đủ 100 mã duy nhất/đúng format/không va seed, danh sách đã lưu.
- T05 run `20261008_222503_29aa4634`: **20/20 xUnit PASS**, không skip;
  stdout/stderr, exit 0, TRX và results.json được runner lưu đầy đủ.
- Debug/Release: **0 warning/0 error**. Production DAL/connection pooling không đổi.
- B06: probe SqlClient mở/impersonate/dispose lặp lại trên DB test; pooling bật
  lỗi SQL 596 class 21 tại query kiểm target các lượt 1/3/5. Pooling tắt có 6/6
  lượt impersonation thành công. Fixture AppFactory dùng Pooling=false riêng
  cho session EXECUTE AS USER; không đổi mã mapping để né lỗi.
- B04: đã kiểm chứng runner thu kết quả thành công; nhánh nonzero/stderr của
  runner sửa chưa được chạy lại bằng failure cưỡng bức trong lượt cuối này.
  Bằng chứng failure/TRX trước đó được giữ, không đổi expected để gây/né lỗi.

### Ba ca còn BLOCKED — cần phối hợp nghiệm thu

1. **T03-K08:** thiết lập/đối chiếu Windows principal app riêng và cấu hình local
   tách quyền migration, không credentials vào Git/log.
2. **T04-K05:** mở/save/reopen StartupForm trong Visual Studio Designer thật.
3. **T05-K07:** thao tác WinForms thật với query chậm, hủy tải, đóng Form;
   kèm resize/DPI/Tab/focus theo plan.

G01 vẫn **Đang làm**, chưa đủ điều kiện nghiệm thu. Không cần duyệt đợt bug
mới; bước tiếp là phối hợp ba ca manual trong đợt test đã duyệt. Commit chưa được giao.
Hồ sơ mới: logs/manifest/probe trong `runs/G01_Regression_20261008_222427/`,
JSON/progress/codes T03 ở task T03/runs, TRX/stdout/stderr T05 ở task T05/runs.
Các phần bên dưới là lịch sử các lượt trước, không thay kết quả mới nhất này.

## Cập nhật sau duyệt debug “ok” — G01_Regression_20261008_221528

Đã sửa B01 (Seed/seed + lưu DB/progress) và B02 (guard/bind rows).
Build Debug/Release/copy sạch: **0 warning/0 error**. Source giữ cố định trong
regression, `runs/G01_Regression_20261008_221528/source-stability.json` count=0.
Baseline/migration không sửa. Kết quả lượt đầu phía dưới giữ làm lịch sử.

| Task | PASS | FAIL | BLOCKED |
|---|---:|---:|---:|
| T01 kế thừa | 7 | 0 | 0 |
| T02 kế thừa | 12 | 0 | 0 |
| T03 | 7 | 1 | 1 |
| T04 | 6 | 0 | 1 |
| T05 | 3 | 6 | 1 |
| **45 ca** | **35** | **7** | **3** |

- T03 runner 9/10 SQL subcase PASS; concurrency FAIL do thu reader B03.
- T05 direct xUnit **15/20 PASS, 5 FAIL**; 7 unit PASS. Số xUnit không đồng
  nhất số ca nghiệm thu vì một test bao nhiều ca và một ca có nhiều subcase.
- B01 regression seed PASS; B02 compile warning đã hết, UI dữ liệu chưa kiểm
  chứng đầy đủ. G01 vẫn Đang làm, chưa chuyển nhóm tiếp theo.

### Đợt phát sinh chờ duyệt bổ sung

| Bug | Task | Hướng đề nghị |
|---|---|---|
| B03 | T03 | Giữ reader nguyên object, cleanup đúng, lưu 100 mã concurrent |
| B04 | T05 | Thu stdout/stderr/exit code/TRX đầy đủ, không gọi test FAIL là setup error |
| B05 | T05 | Snapshot chuẩn hóa DBNull thành [] cho bảng rỗng |
| B06 | T05 | Điều tra mã SQL gốc của Unknown trong DAL/fixture, kiểm session/pooling, sửa theo bằng chứng |

Chi tiết bằng chứng/phạm vi/regression tại bug-report của T03/T05 (liên kết
phía dưới). B06 chưa có nguyên nhân chắc chắn; SQL trực tiếp cùng app user
không tái hiện lỗi SaveValues/checkout và trả 51109 đúng ở hủy mã thiếu.
Chưa sửa B03–B06. Đề nghị duyệt **cả đợt bổ sung**, gồm điều tra B06 và regression.
Manual còn thiếu: Designer StartupForm, resize/DPI, principal app và async UI.

## Báo cáo lịch sử trước debug

**Trạng thái:** Đã chạy đợt test được duyệt; nhóm chưa nghiệm thu, chờ duyệt debug.  
**Source:** Chưa commit; source/schema/fixture/migration giữ cố định toàn lượt,
`runs/G01_20261008_220313/source-stability.json`: 0 file đổi.  
**Môi trường:** Windows, SDK 10.0.401, SQL localhost 17.0.1000.7; Windows auth,
trust certificate local đã duyệt. Không chạy ghi vào DB ứng dụng.

## 1. Kết quả theo ca bắt buộc

| Task | PASS | FAIL ca nghiệp vụ | BLOCKED | Kết quả/bằng chứng |
|---|---:|---:|---:|---|
| T01 | 7 | 0 | 0 | Kế thừa biên bản trước |
| T02 | 12 | 0 | 0 | Kế thừa 12 ca/80 subcase; baseline nguồn giữ hash |
| T03 | 1 | 0 | 8 | Runner setup FAIL; các ca phụ thuộc BLOCKED; K09 review PASS |
| T04 | 6 | 0 | 1 | Restore/build/app/copy sạch PASS; Designer BLOCKED |
| T05 | 0 | 0 | 10 | Runner setup FAIL; 7 unit PASS chỉ là bằng chứng bộ phận, chưa đủ ca SQL/manual |
| **Tổng** | **26** | **0** | **19** | Đủ 45 ca được ghi trạng thái, không tick ca chưa chạy |

**Hai lượt runner setup đã FAIL**, không phải toàn đợt không có lỗi. Các ca
nghiệp vụ chưa được thực thi do setup được ghi BLOCKED thay vì bịa FAIL/PASS.
Trong 26 ca T03–T05 chạy mới: 7 PASS, 19 BLOCKED. Không đủ điều kiện Hoàn tất.

- Debug/Release/copy sạch: build thành công, **1 warning CS8602, 0 errors** mỗi lượt.
- App WinForms thật mở/đóng được với thông báo thiếu config và nút tải disabled.
- Unit: **7/7 PASS**, TRX được lưu.
- Empty SQL test độc lập: migration Applied, repeat AlreadyApplied, DM999 và
  lỗi hết giới hạn 51302 đúng; không thay kiểm chứng trên seed.
- Thử supplemental blocks trên empty bị harness timeout 120s, thiếu JSON kết
  quả: không suy diễn PASS từ sequence đã tăng/metadata DB. Giữ ghi nhận timeout.

## 2. Đợt bug đề nghị duyệt một lần

| Mã | Task sở hữu | Nguyên nhân / sửa đề nghị | Regression |
|---|---|---|---|
| G01-B01 | [T03 bug-report](t03-contract-migration-cap-ma/bug-report.md) | `[bool]$Seed` bị `$seed` SQL ghi đè do PowerShell không phân biệt hoa/thường; tách tên, ghi DB setup/progress trước return | Full T03/T05 SQL trên DB mới, checksum/rollback/concurrency/quyền/TVP/transaction/cleanup |
| G01-B02 | [T05 bug-report](t05-dal-nen-kiem-thu/bug-report.md) | Rows nullable bị dereference StartupForm:53; bind biến rows sau guard, không fake empty | Debug/Release/copy sạch và UI success/empty/error/cancel/close |

Chưa sửa bug trong lượt này. Duyệt đợt debug cho phép sửa đúng hai hướng nêu
trên và regression ảnh hưởng; bug mới ngoài đợt sẽ ghi lại để duyệt bổ sung.

## 3. Manual/blocker cần phối hợp

- Visual Studio Designer của **StartupForm mới**: mở/save/reopen, không lỗi.
- WinForms resize 1280×720/1366×768, DPI 100/125/150%, Tab/focus và tải chậm/
  hủy/đóng: cần bằng chứng thực tế, T01/HTML guide không thay nghiệm thu.
- Windows app principal chuyên biệt và local config chưa provision; không
  fallback sang admin để báo PASS quyền/runtime. SQL integration có user
  WITHOUT LOGIN để kiểm ownership chain sau khi bug setup được sửa.

## 4. Hồ sơ

- [T03 kết quả](t03-contract-migration-cap-ma/test-results.md), JSON/runs cạnh file.
- [T04 kết quả](t04-solution-models/test-results.md).
- [T05 kết quả](t05-dal-nen-kiem-thu/test-results.md), JSON/runs cạnh file.
- `runs/G01_20261008_220313/`: manifest, stability JSON, SDK/Git/SQL metadata,
  restore/build/clean logs, unit TRX, UIAutomation JSON và SQL supplement evidence.
- DB test T03/T05 setup lỗi được giữ; DB empty bổ sung ghi tên trong
  `runs/G01_20261008_220313/empty-database.txt`. Không reset/drop DB cũ.
- Commit/push vẫn chờ cho phép. Không chuyển G02 hoặc đánh dấu G01 hoàn tất.
