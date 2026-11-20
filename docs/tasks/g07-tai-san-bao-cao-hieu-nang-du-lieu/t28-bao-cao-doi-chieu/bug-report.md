# G07-DEV01 — Tổng quan chạy với DB development thiếu migration

## G07-UI03 — Card Bình quân “—”, danh sách Cần chú ý khó nhìn, dialog nguồn vỡ bố cục

10/11/2026. **Đã sửa theo phạm vi người dùng duyệt (cả 4 mục, “Đơn đang mở” →
Lịch sử hóa đơn). Build Debug/Release 0 lỗi; ca native R01–R05 chờ người dùng chạy
app kiểm.** Biên bản: [ui03-test-results.md](ui03-test-results.md).

### Hiện tượng

1. Card “Bình quân mỗi hóa đơn” hiện “—”, dòng dưới “Chưa đủ cơ sở so sánh.”.
2. Mục “Cần chú ý · Hiện tại” là bốn thanh nút phụ full-width xếp chồng, nhìn nặng
   và không giống danh sách số liệu.
3. Bấm “Công nợ nhà cung cấp” mở một cửa sổ con; nội dung chỉ là card
   “REFERENCE WORKSPACE · Công…” bé xíu, cắt cụt, phần còn lại trống. Người dùng
   muốn chuyển thẳng tới tab nghiệp vụ tương ứng thay vì mở dialog.

### Nguyên nhân và bằng chứng

**1. Bình quân “—”: không phải bug, do dữ liệu kỳ rỗng.**
- `database/migrations/028_dashboard_analytics_read.sql:53` tính
  `@average=@revenue/NULLIF(@bills,0)`; không có hóa đơn đã trả trong kỳ thì
  `@bills=0` nên `average.current` là NULL.
- Truy vấn development 10/11/2026: hóa đơn “Da thanh toan” chỉ có tới
  `2025-05-01`; **không có hóa đơn nào trong hôm nay (2026-11-10) và hôm qua**.
  Vì vậy bình quân là NULL và mốc so sánh cũng NULL → dòng chú thích hiện
  “Chưa đủ cơ sở so sánh.” theo nhánh `!current || !previous` ở
  `AdminDashboardControl.cs:152`.
- Kết luận: số liệu đúng với dữ liệu; chỉ có câu chú thích chưa nói rõ “kỳ này
  chưa có hóa đơn đã thanh toán”, dễ hiểu thành lỗi tải.

**2. Dialog nguồn vỡ bố cục: lỗi bố cục thật.**
- `Modules/Reports/ReportSourceDialog.cs:13` và `:18` thêm
  `new Sources(...)` vào form bằng `Controls.Add` mà **không đặt `Dock=Fill`**.
- `OrbitRecordListControl`/`OrbitListWorkspace` chỉ tự `Dock=Fill` bên trong;
  bản thân UserControl `Sources` giữ kích thước mặc định (150×150) nên bị cắt ở
  góc trên-trái, đúng như ảnh (card “REFERENCE WORKSPACE” bé, phần lớn dialog trống).
- Lỗi này ảnh hưởng mọi dialog nguồn mở từ dashboard (4 nút “Xem chi tiết”,
  điểm xu hướng, kênh, Top 5), không riêng Công nợ.

**3. “Cần chú ý” khó nhìn + mở dialog: đúng hiện trạng thiết kế.**
- `AdminDashboardControl.cs:63–69` tạo mỗi dòng là một `OrbitButton` phụ
  `Dock=Top`, icon “list”, rồi gắn `Click → OpenSourcesAsync(...)` mở
  `ReportSourceDialog`. Vì là nút full-width xếp chồng nên trông như bốn thanh
  bar; không phải dạng “nhãn — giá trị” của danh sách số liệu.
- `ReportSourceDialog.cs:32` (dialog) và `:271` (dashboard) là đường mở modal.
- `MainForm.cs:182` đã có `OpenWorkspaceAsync(string target)` và mapping
  `NavigationTarget` (`NavigationController.cs:6`); module đích đã đăng ký ở
  `Program.cs:74–93`: `Invoices`, `Delivery`, `Materials`, `Debt` đều có sẵn.
  Vì vậy chuyển tab không cần contract/SQL mới.

### Phạm vi sửa đề xuất (chờ duyệt một lần)

1. **Sửa dialog nguồn**: đặt `Dock=DockStyle.Fill` cho `Sources` ở cả hai
   constructor `ReportSourceDialog`. Không đổi dữ liệu/loader/quyền.
2. **Đổi hành vi “Cần chú ý”** sang điều hướng module thay vì dialog:
   - `orders.open` → `Invoices` (Lịch sử hóa đơn, có sẵn bộ lọc “Chưa thanh toán”).
   - `delivery.pending` → `Delivery`; `material.zero` → `Materials`;
     `debt.current` → `Debt` (workspace “Công nợ nhà cung cấp”).
   - Dùng `MainForm.OpenWorkspaceAsync` qua `FindForm()`; giữ dialog nguồn cho
     các KPI/chart/Top 5 (vốn cần danh sách chứng từ theo kỳ).
3. **Chỉnh hình thức mục “Cần chú ý”** theo Orbit hiện có, không tạo framework mới:
   - Mỗi dòng là một hàng ~52 logical: icon + nhãn bên trái (Body/Text), giá trị
     căn phải (money `#.##0 ₫`, số đếm theo vi-VN) + mũi tên “›”; phân cách mảnh.
   - Hover/focus đổi nền Surface sáng hơn; cả hàng bấm được bằng chuột và
     `Enter/Space`; cursor bàn tay; `AccessibleName` đọc đủ “nhãn: giá trị · mở …”.
   - Giữ section header/description hiện có; không thêm số liệu/contract mới.
4. **Làm rõ trạng thái rỗng của Bình quân** (không đổi SQL): khi
   `average.current` NULL do kỳ không có hóa đơn, chú thích hiện
   “Chưa có hóa đơn đã thanh toán trong kỳ.” thay cho “Chưa đủ cơ sở so sánh.”;
   các KPI khác giữ logic hiện tại.
5. Kiểm chức năng/kỹ thuật tại 125% (hàng dài, focus bàn phím, điều hướng đúng
   một lần, quay lại dashboard không mất kỳ/chế độ); hình thức do người dùng
   tự kiểm. Không sửa schema/SQL/migration; giữ nguyên lịch sử G07-DA02/DA03.

### Rủi ro

- Điều hướng rời dashboard làm mất trạng thái đang xem (kỳ, cuộn) — chấp nhận
  theo yêu cầu “chuyển tab”; khi quay lại dashboard sẽ đọc lại dữ liệu hiện tại.
- Hàng tự vẽ cần xử lý DPI/focus đúng; phải kiểm 125% và bàn phím, tránh click
  hai lần gây mở hai lần.
- G07-DA02 (UI chậm 3–6 giây) vẫn tách riêng, không gộp vào đợt này.

## G07-DEV02 — Tổng quan báo “Chưa cập nhật … schema028” trên development

10/11/2026. **Đã được duyệt nâng và đã áp 028→029 vào development, PASS đối chiếu
schema/dữ liệu; chờ người dùng mở app kiểm.** Biên bản:
[development-upgrade-029-results.md](../runs/developmentG07-dashboard029_20261110/development-upgrade-029-results.md).
Đây không phải lỗi code: chốt chặn schema trong source đang chạy đúng, DB development
vẫn ở schema 027 và thiếu hai procedure tổng quan mới.

### Hiện tượng

Người dùng mở app, đăng nhập ADMIN, vào màn Tổng quan (ảnh đính kèm). Màn hiện
“Kỳ thống kê: —”, bốn card KPI để “—”, và dòng chú thích
“Chưa cập nhật · Cần cập nhật dữ liệu lên schema028 để mở Tổng quan ngày/tháng.
Liên hệ người triển khai.” Không có số 0 giả; đây là trạng thái lỗi tải.

### Nguyên nhân và bằng chứng

1. `src/CafeManager.DataAccess/Repositories/ReportRepository.cs:73–77`: mọi yêu cầu
   nguồn tổng quan (kể cả đọc dashboard) gọi `EnsureAnalyticsAsync`, và nếu
   `AuthRepository.ReadStateAsync().SchemaVersion < 28` thì ném
   `BusinessRule 53502` với đúng câu chữ đang thấy trên màn.
2. `src/CafeManager.DataAccess/Repositories/AuthRepository.cs:17–22` đọc schema qua
   `sp_AppReadState`; procedure `sp_AppReadState` (migration 002) trả
   `MAX(Version) FROM dbo.SchemaMigrations`.
3. Truy vấn SQL Server thật `localhost/CoffeeManager_Development` (integrated security,
   10/11/2026): `MAX(Version)=27`; `OBJECT_ID('dbo.sp_DashboardAnalyticsRead')=NULL`,
   `OBJECT_ID('dbo.sp_DashboardAnalyticsSources')=NULL`. Marker development hợp lệ
   (`DevelopmentMarker` khớp 1 dòng).
4. Hai procedure trên chỉ được tạo bởi `database/migrations/028_dashboard_analytics_read.sql`
   (028) và `029_dashboard_source_lock_order.sql` (029); 028 yêu cầu schema027,
   029 yêu cầu schema028. Toàn bộ checksum 001–027 đang khớp file, nên có thể áp tiếp
   028→029 (checksum file 029 = `05E2508D…6361`, trùng biên bản DA03).
5. Hồ sơ đã ghi đúng tình trạng này: [G07-DA03](bug-report.md#g07-da03) dòng đầu,
   [biên bản DA03](test-results-g07-da03.md) mục Môi trường, và
   [dashboard-analytics-regression-results.md](dashboard-analytics-regression-results.md)
   dòng 57: “Development giữ 027; handoff script/backup chỉ chuẩn bị, **chưa chạy** vì
   FAIL UI”. Tức DB development chưa được nâng, không phải nâng rồi bị lỗi.

### Đề xuất xử lý (chờ duyệt)

Nâng development `027 → 028 → 029`, không tạo migration mới, không reset DB:

1. Đóng app development bình thường (script từ chối chạy nếu process còn mở).
2. Backup `COPY_ONLY/CHECKSUM` DB `CoffeeManager_Development` trước khi áp; giữ backup.
3. Preflight: marker, `MAX(Version)=27`, checksum 001–027 (đã kiểm khớp ở trên),
   ghi snapshot dữ liệu + schema trước nâng.
4. Áp `Apply-Migration.ps1 -Version 28` rồi `-Version 29`; đối chiếu sau nâng:
   version 29, hai procedure tồn tại, GRANT EXECUTE cho `CafeManagerApp`, snapshot
   dữ liệu nghiệp vụ và bảng/cột không đổi.
5. Đọc thử dashboard qua DAL/BLL bằng account ADMIN hiện có; mở lại app kiểm màn
   Tổng quan không còn nhánh 53502. Không ghi giao dịch test vào development.

### Rủi ro và lưu ý

- Sau nâng, màn Tổng quan sẽ đọc số thật. **G07-DA02 đã được sửa ngày 10/11/2026
  trong `AdminDashboardControl`** ([kết quả sửa](da02-fix-results.md)); regression
  10.000 bill đã chạy PASS ([biên bản](da02-regression-results.md)). Nâng schema
  không liên quan tốc độ UI.
- Migration chỉ thêm procedure đọc/GRANT, không DDL bảng/cột nên rủi ro dữ liệu thấp;
  vẫn phải kiểm parity sau áp và giữ backup để hoàn nguyên nếu lệch.
- Không tự chạy `-Apply` khi chưa được giao; không kill Visual Studio/process người dùng.

## G07-DA03 — Nguồn theo món deadlock với tạo hóa đơn

10/11/2026. **Đã được duyệt sửa SQL và regression; migration 029 đã thêm, parity 19/19 PASS, hai phiên 229→0 deadlock, kết quả 11/11 kind giống nhau trên 10.000 bill.** 029 đã lên development trong đợt nâng G07-DEV02. Biên bản: [test-results-g07-da03.md](test-results-g07-da03.md). G07-DA02 (UI chậm) đã được sửa phạm vi UI và regression 10.000 bill PASS ([biên bản](da02-regression-results.md)).

### Bằng chứng đã xác nhận

- Ca `DA08/typed-bucket-channel-product` trong [API results](../runs/dashboard-ui02fix_20261110_102411_24721b73/api-results.json) FAIL tại `DashboardAnalyticsSourcesAsync(kind="product",product="SP999")`,đangchạycùnggenerator tạođơn qua contract. Các ca độc lập vẫn tiếp tục.
- [Deadlock graph lọc đúng DBtest](../runs/dashboard-ui02fix_20261110_102411_24721b73/deadlock-1.xml),UTC03:32:54;chỉhai process của DB `CoffeeManager_Test_G07_20261110_102810_0b67b365_demo`,khôngxuấtringbuffercácDBkhác.
- Process58: `sp_DashboardAnalyticsSources`,SERIALIZABLE,đanggiữRangeS-S trênPK_CHITIET_HD,chờRangeS-S trênPK_HOADON.
- Process60: `sp_TaoHoaDon`,đanggiữX trênPK_HOADON,chờRangeI-N trênPK_CHITIET_HD ởlệnhINSERTdòngmón.
- Vòngkhóa rõ:readerđọcCHITIET_HD→HOADON,cònwriterghiHOADON→CHITIET_HD. Endpointnguồncủa028dùngmộtSELECTHOADONcóEXISTSCHITIET_HD;optimizerđượcchọnthứtựngược. Đây là lỗi phối hợp khóa có bằng chứng,không phải lỗi fixture mã hoặc kết nối.

### Phạm vi đề nghị duyệt một lần

1. Thêm migration `029_dashboard_source_lock_order.sql`,chỉALTERproceduređọc `sp_DashboardAnalyticsSources`;giữchecksum001–028,khôngthêmbảng/cột/view/index/entity/DTO/property. Bảngtạm truyvấn khônglà thực thểlưu trữ.
2. Vật hóa tậpheader đã lọc từHOADON vào bảngtạm trongstatementđầu,chưa đọcCHITIET_HD. Statementtiếp theo mới lọctheomón từheadersnapshotvàCHITIET_HD,đểthứtựheader→linekhôngphụthuộctốiưuJOIN. GiữSERIALIZABLEtransactionngắn,ADMIN/stamp,predicate[From,Until),nguồn/code/page50/sort vàresultsetcũ. KhôngNOLOCK,khôngđổiisolationDB,khôngsửaprocghi hoặc thêmretry.
3. Regression nguồn ngày/giờ/kênh/món,50+12/trùngmón/quyền/transaction0,clean/demo028→029/rerun/parity;2session tạođơn/thêmline/checkout trongkhiđọcnguồntheomón,lặp cóstartgate vàghi mọi1205/timeout/receipt/tiền/kho/điểm. ĐoI/O/timingtrướcsau,kiểmmànnguồnownedmodal.
4. Tiếp tục regression/đoUI củaG07-DA02;chỉsaukhiđạt mớibackup/áp028→029development theo phạmvi bà giao đãduyệt,mỗi version/backup/hash ghi rõ. Khôngseed/resetcredentialvàkhôngđổiModels.

Rủiro:materializeheadercóthểtăngtempdb/readsoverheadvới kỳlớn;phảiđo10.000billvàgiữkếtquảtiền/nguồn. Cáchsửanhắmđúngvòngkhóađãchứngminh;khônghứahếtmọideadlockngoàiphạmvimàchưacótest. Duyệtđợtnàybao gồmsửavàregressionnêu trên;khôngxinduyệttừngfile/ca.

### Kết quả sau sửa (10/11/2026)

Ứng dụng source/Models/dữ liệu nghiệp vụ giữ nguyên; chỉ thêm file migration 029.

- Parity trên DB test 028→029: 19/19 kịch bản nguồn giống nhau (bỏ `ReadAt`),
  schema và dữ liệu không đổi, GRANT giữ 1, 7/7 lỗi hợp đồng trước và 2/2 sau đúng mã;
  migration áp lần hai trả `AlreadyApplied`, checksum `05E2508D…6361` ổn định.
  Statement đầu của procedure chỉ đọc `HOADON`, lọc món nằm ở statement sau.
- Hai phiên đồng thời (start gate, 60 giây/phase): control-028 có 229 deadlock 1205
  (toàn bộ ở reader), 0 timeout; fix-029 có 0/0, 0 lỗi khác. Integrity 6/6 bằng 0;
  reader 238→760 vòng, writer 611→1.870 vòng.
- Đo trên 10.036 hóa đơn / 30.720 dòng món: nguồn theo món giảm 78.575→659 logical
  reads/lần, SQL 533,2→120,4 ms, wall 273→71 ms. Các kind theo hóa đơn tăng 48–65 reads
  do materialize `#headers` nhưng nhanh hơn (revenue 223,2→133,6 ms SQL; orders
  101,6→44,8 ms). `material`/`debt`/`expense` gần như không đổi. Kết quả 11/11 kind
  giống nhau giữa hai phiên bản, source không bị sửa trong lúc đo.
- Development đã ở schema 29 (đợt nâng G07-DEV02, backup + hash ghi trong
  [biên bản nâng](../runs/developmentG07-dashboard029_20261110/development-upgrade-029-results.md)).
  Màn Tổng quan trên app vẫn chờ người dùng mở kiểm.
- G07-DA02 (UI chậm) đã sửa trong `AdminDashboardControl` và regression 10.000 bill
  PASS ([biên bản](da02-regression-results.md)); nghiệm thu hình thức và DPI
  100%/150% do người dùng thực hiện.

---

## G07-DA02 — Cập nhật Tổng quan làm UI chậm 3–6 giây

10/11/2026. **Người dùng đã yêu cầu sửa. Đã sửa phạm vi `AdminDashboardControl`
(bỏ panel lồng cho thanh Top 5, guard WrapLabels, giữ treo layout một lượt); đo
lại trên harness: Clear 814→242 ms, Render-sau-Clear 628→348 ms, hình học trùng
khít bản render đầy đủ.** [Kết quả sửa](da02-fix-results.md). **Regression 10.000
hóa đơn đã chạy PASS:** timing UI 582,6 / 645,2 / 661,5 ms (<3 s), perf 12/12,
rỗng 3/3, ma trận 4/2/1 KPI–2/1 chart tại 125%; hình thức và DPI 100%/150% vẫn
do người dùng kiểm. [Biên bản regression](da02-regression-results.md). Nội dung
điều tra dưới đây giữ lịch sử; development đã áp028/029 ở đợt G07-DEV02.

### Hiện tượng và bằng chứng

- Dataset SQL Server thật đúng10.000bill hợp lệ:7.998paid/1.001open/1.001hủy,được tạo qua contract trong DB test riêng. Query service median ngày343ms/tháng330ms;SQL có IO/TIME ghi riêng,không xóa cache toànserver.
- WinForms thật1366×768/DeviceDpi120:startup-day4.939ms,monthly5.389ms,today5.370ms đến ready/binding+mộtUIyield. Vượt mục tiêu report<3s tạiT29-K10;không coi số đo SQL nhanh là UI đã PASS.
- [Timing/IO/TIME](../runs/dashboard-regression_20261110_095018_6af29f27/timing-results.json),[dataset/timing service](../runs/dashboard-regression_20261110_095018_6af29f27/results.json).
- [Điều tra không sửa code](../runs/dashboard-regression_20261110_095018_6af29f27/layout-results.json):gọi private methods trên UI thread bằng dữ liệu đã đọc,không gọi SQL trong phép đo:

| Method | Thời gian | Layout events | TableLayoutPanel events |
|---|---:|---:|---:|
|Render|3.155ms|220|132|
|ClearSnapshot|4.098ms|161|92|
|Render sau khôi phục snapshot|6.002ms|234|136|

77control trước đo,không phải form tải10.000bill về để sum. Các phase này chạy đồng bộ trên UI thread;đã cô lập phần cập nhật control/layout là nơi gây chậm,không quy cho SQL/transaction hoặc thiếu index. Chưa có profiler CPU chi tiết cho từngcontrol;không tuyên bố từngLayout event riêng là nguyên nhân duy nhất.

### Source liên quan và hướng sửa đề nghị

Phạm vi chính `src/CafeManager.WinForms/Modules/Home/AdminDashboardControl.cs`: `ClearSnapshot` khoảng165, `ReadAsync`184–190, `Render`205–235;page/card dùngTableLayoutPanelAutoSize lồng nhau,WrapLabels theoSizeChanged. Hai method cập nhật từngText/AccessibleName,nút,xóa/tạo lạiTop5 vàAddRow trong cây đanglayout,không gom thành một lượt cập nhật.

Đề nghị duyệt một lần:

1. Debug/profiling tập trung hai phase UI trên bằng dataset hiện có/tái dựng;chốt điểm gây lặp đo/layout trước chỉnh. Không thay schema/model/DTO/query tài chính hoặc thêm cache thống kê.
2. Gom cập nhật snapshot thành một đợt bằng cơ chếlayoutWinForms cósẵn;chặn layout/wrap lặp do cập nhật trung gian,updateTop5 theo lô rồi layout một lần. Helper nhỏ nằm trongAdminDashboardControl,không refactorcontrol/theme chung khi chưa có bằng chứng/cấp quyền thêm.
3. Giữ nguyên loading/—khi lỗi,cancellation/generation,đổi mode,sourcebounds,keyboard/ownedmodal vàresponsive/DPI. Không dùngDoEvents/Task.Run để sửacontrolngoàiUIthread,không giảmfont/ẩnwidget hoặc đổi sốliệu để nhanh hơn.
4. BuildDebug/Release;freeze source rồi regression đúng phạmvi:ngày/tháng/rỗng/lỗi/hide-dispose/querymuộn/nguồn,4/2/1KPI/2/1chart,1280×720/1366×768/125%,mẫuSQLđốichiếu và lại10.000bill/timingUI. Mục tiêu report<3s cùngmáy/dataset;giữsốtrước/sau/IO/TIMEvàkhôngticknếuchưađạt. Hìnhthứcuserkiểm;DPI100/150%NOTRUN.

Phạm vi này là sửa lỗi hiệu năng của màn đã triển khai,không mở rộng tínhnăng. Duyệtdebug gồmregression trên;không hỏi lại từngfile/ca. Rủi ro:gomlayout sai cóthể giữbounds cũ/cắtcontrol hoặc khôngresume khi lỗi;regression kỹthuật vàfinally phải kiểm các trường hợp này. Chỉ backup/áp028development sau testđạt/không còn lỗi chặn nhưplan đã duyệt.

---

## G07-DA01 — Fixture kiểm thử bổ sung dùng mã không hợp lệ

10/11/2026. **Đã được duyệt sửa fixture và regression, đã sửa trong phiên bản mới NVL99/SP999 có guard chưa tồn tại.**9/9 kiểm bổ sung đã chạyPASS,giữ nguyên expected;harness/log thấtbại cũ giữlại. Không sửa source ứng dụng/migration để vượt quy tắc mã. [Kết quả regression](dashboard-analytics-regression-results.md). Lỗi fixture đóng;G07-DA02 là lỗiUI mới phát hiện độc lập,chưa đượcduyệt sửa.

### Bằng chứng và nguyên nhân

- [Log thực tế](../runs/dashboard_20261110_093601_0d29d43b/extended/probe.log): `System.ArgumentException: Mã nguyên liệu từ NVL01 đến NVL99`, tại `MaterialService.Normalize`, trước khi chạy các `Check` bổ sung. Exit code `-532462766`; chưa có `results.json` vì dừng ở setup.
- Harness bổ sung tại `extended/Probe/Program.cs:33` gọi MaterialService với `DZ001`. Quy tắc hiện có ở `MaterialService.cs:21` là `^NVL(0[1-9]|[1-9][0-9])$`.
- Cùng fixture dùng mã món `DX001`; source ProductService hiện yêu cầu `^SP[0-9]{3}$`, loại `SP000`. Đây là lỗi thứ hai thấy qua đọc source, chưa chạy đến lệnh đó; không ghi đã tái hiện.
- Agent viết sai fixture bổ sung. Giữ nguyên quy tắc sản phẩm; không mở rộng miền mã của BLL/SQL để cho test đi qua.

### Phạm vi sửa đề nghị một lần

1. Giữ nguyên harness/log/hash lượt lỗi. Tạo phiên bản harness bổ sung mới, đổi mã fixture sang `NVL99` và `SP999`, kiểm chưa tồn tại trong DB test mới trước khi tạo.
2. Chỉ sửa fixture/harness; giữ source ứng dụng, Models, baseline, migration001–028 và các expected tiền/kho/điểm/receipt đã chốt. Không bỏ assertion hoặc giảm số ca để né lỗi.
3. Chốt hash phiên bản mới trước chạy. Chạy lại DA05, DA07 biến thể, DA10 hai session, DA11 timeout/các ca lỗi, DA13 10.000 hóa đơn và các ca độc lập chưa có bằng chứng. Các ca native chi tiết/quyền/nguồn còn lại ghi riêng, không suy PASS từ subset.
4. Regression lại bộ chính theo ảnh hưởng nếu fixture thay đổi; kiểm resource/source và cập nhật báo cáo. MainForm có cập nhật độc lập sau runner chính (audit cuối đã lưu),nên freeze/recheck native trên source hiện tại,không tự sửa hoặc hoànnguyên MainForm. Duyệt sửa bao gồm regression trong phạm vi này.

Không cần đổi nghiệp vụ/contract hay entity/thuộc tính. Rủi ro thực tế là fixture vẫn thiếu điều kiện của procedure nghiệp vụ khác; nếu gặp lỗi mới thì lưu nguyên bằng chứng, không vá tiếp trong lượt. DB test bổ sung đã được dọn sau đối chiếu exact name+marker; development giữ nguyên, chưa áp028.

---

## G07-UI02 — Bố cục Tổng quan chưa hoàn thiện, nút bị cắt chữ

09/11/2026. Người dùng đã làm rõ phạm vi và duyệt bằng “ok”. **Đã sửa đúng AdminDashboardControl,12/12 regression chức năng/layout kỹ thuật PASS;đã mở bản mới tại Tổng quan production.** [Biên bản](ui02-regression-results.md). Hình thức người dùng tự kiểm;không còn chờ duyệt sửa UI02. Nội dung điều tra/đề nghị dưới đây giữ lịch sử. G07-DEV01 xử lý lỗi dữ liệu riêng.

### Hiện tượng và nguyên nhân

- Ảnh mới hiển thị số liệu thật và thời điểm đọc; thông báo lỗi DB cũ đã hết. Bố cục vẫn là một khối chữ ở góc trên, vùng còn lại trống; bốn nút “Tải lại/Báo cáo/Bán hàng/Công nợ” bị cắt chiều cao, không đọc trọn chữ.
- `Modules/Home/AdminDashboardControl.cs` hiện chỉ tạo title, một Label chứa cả ba chỉ tiêu, một FlowLayoutPanel chứa bốn nút, một Label trạng thái. Source không có các vùng chỉ tiêu/card hoặc bố cục dashboard tương ứng phong cách chung.
- Các nút được tạo bằng `new OrbitButton { Text=... }`, không bật AutoSize hoặc áp style/kích thước management button. TableLayoutPanel không khai báo RowStyles/ColumnStyles để cấp kích thước rõ ràng cho các vùng. AppTheme.SecondaryButton chỉ đặt màu/border; không sửa kích thước. OrbitButton có GetPreferredSize nhưng caller chưa dùng cách bố trí bảo đảm kích thước đó. Đây là thiếu cấu hình layout trong chính màn; mức phân bổ kích thước cụ thể cần đo khi regression kỹ thuật, chưa bịa số đo.
- UI contract của ADMIN gồm doanh thu hôm nay, đơn mở, công nợ, lối vào nghiệp vụ (UX mục2 và T28 plan). Source có dữ liệu/handler nhưng phần trình bày còn tối thiểu. Lượt nâng DB trước không thay dòng code UI nào; báo cáo PASS trước đó chỉ kiểm đọc số liệu, không nghiệm thu hình thức. Agent đã hiểu thiếu ý và chưa chỉ rõ phần UI chưa hoàn thiện khi bàn giao.

### Phạm vi đề nghị duyệt một lần

Sửa chính `src/CafeManager.WinForms/Modules/Home/AdminDashboardControl.cs` theo theme Orbit hiện có:

1. Header rõ ràng “Tổng quan quản lý”, ngày nghiệp vụ và thời điểm cập nhật.
2. Ba card chỉ tiêu: **Doanh thu hôm nay / Đơn đang mở / Công nợ nhà cung cấp**, dùng đúng ba trường DashboardRow. Giá trị tiền và số đếm dễ đọc, không ghép vào một đoạn văn. Tái sử dụng OrbitSurface/theme/tokens đã có, không tạo bộ control/framework chung mới.
3. Nhóm thao tác **Tải lại / Báo cáo / Bán hàng / Công nợ** có kích thước theo font/DPI và style management. RowStyles/ColumnStyles rõ, nội dung xuống hàng khi hẹp; nút không bị ép thấp và luôn tiếp cận được.
4. Trạng thái loading/rỗng/lỗi và ghi chú nghiệp vụ có vùng riêng. Không hiển thị0 giả khi lỗi; giữ đúng quyền, nguồn dữ liệu và điều hướng hiện có. Không thêm biểu đồ/số liệu/contract SQL mới hoặc sửa toàn shell.
5. Hỗ trợ co giãn/DPI theo AGENTS. Agent đo layout kỹ thuật/chức năng tại125%, kiểm dữ liệu/binding, refresh/loading/lỗi, các nút điều hướng và hủy khi rời màn; không chạy nghiệm thu thị giác/hover/spacing hoặc đổi Windows Scale. Người dùng tự kiểm hình thức.
6. Build Debug/Release, regression theo ảnh hưởng trên source cố định; không ghi giao dịch test vào development. Các ca giả lập lỗi/query chậm dùng harness/DB test riêng. Lưu kết quả tại T28, giữ lịch sử G07-DEV01/325ID; không chốt toàn G07 từ việc sửa màn này.

Duyệt sửa UI02 bao gồm regression đúng phạm vi trên; không cần vòng requirements/plan mới vì đây là sửa phần trình bày của màn đã có. Chỉ đổi helper dùng chung nếu điều tra/triển khai chứng minh bắt buộc và được duyệt bổ sung. Rủi ro cần kiểm là layout card lúc hẹp/DPI125%, cập nhật label khi tải và kích hoạt điều hướng đúng một lần; không có thay đổi schema/tài chính.

---

## Lịch sử G07-DEV01 — Đã xử lý lỗi dữ liệu

09/11/2026. **Người dùng đã duyệt; development đã nâng021→024→027, hash dữ liệu khớp, hai backup VERIFYONLY CHECKSUM PASS.**16/16 kiểm đọc runtime PASS; native Tổng quan production hiển thị đúng và hết lỗi. [Biên bản](development-upgrade-results-20261109.md). G07-DEV01 đóng theo phạm vi này; nội dung điều tra/đề nghị bên dưới giữ lịch sử, không còn chờ duyệt nâng.

## Hiện tượng

Người dùng mở app từ Visual Studio, đăng nhập ADMIN. Tổng quan hiện “Chưa cập nhật tổng quan.” và “Có lỗi dữ liệu chưa xác định; tra mã hỗ trợ trước khi tiếp tục.” Ảnh người dùng đính kèm là bằng chứng hiện tượng chức năng, không phải biên bản nghiệm thu hình thức.

## Nguyên nhân và bằng chứng

1. Source `AdminDashboardControl` gọi `ReportService.DashboardAsync` → `ReportRepository.DashboardAsync`, procedure `sp_ReportDashboard`.
2. Procedure và GRANT cho CafeManagerApp được tạo tại `database/migrations/026_reports.sql:131–147`.
3. Truy vấn SQL Server thật xác nhận localhost/CoffeeManager_Development chỉ có SchemaMigrations001–021, procedure count0/OBJECT_IDNULL. Lệnh gọi procedure bị thiếu trả **SQL2812:Could not find stored procedure 'dbo.sp_ReportDashboard'.** Không có procedure được thực thi, không ghi dữ liệu.
4. DPAPI connection F5 hiện tại xác nhận DataSource=localhost,InitialCatalog=CoffeeManager_Development, chỉ xuất hai trường này. Process22872 chạy exe từ bin/Debug của workspace. Tại thời điểm truy vấn không có session SQL khớp PID; vì vậy không gọi đây là trace đầy đủ của request trên ảnh, nhưng thiếu contract ở target F5 đã được xác nhận trực tiếp.
5. `SqlErrorMapper` chưa có thông báo riêng cho2812 nên trả Unknown như ảnh; BLL dịch thành UserOperationException và Dashboard hiện lỗi. Đây là hậu quả của thiếu schema, không phải bằng chứng thao tác login nhanh đổi role/phiên sai.
6. Hồ sơ G06/G07 và mục12.1 kế hoạch đã ghi chưa nâng development024/027. Kiểm thử schema027 trước đây dùng DB test riêng; không chứng nhận DB development đã có procedure.

[Bằng chứng đối chiếu](development-schema-investigation-20261109.json). Đã đọc script nâng G06/G07; chưa chạy -Apply. Bàn giao đăng nhập nhanh đã thiếu nhắc điều kiện schema của màn sau login khi hướng dẫn F5; kết quả57subcase/142unit chỉ thuộc phạm vi đã ghi, không phải toàn ứng dụng.

## Hướng xử lý đề nghị duyệt một lần

Nâng đúng **localhost/CoffeeManager_Development:021→024→027** bằng hai script đã có, không tạo migration mới hoặc reset DB:

1. Đóng app development bình thường để không giữ phiên/schema cũ. Không tự kill Visual Studio hoặc process người dùng khi còn nháp/giao dịch.
2. Kiểm DevelopmentMarker, version và checksum các migration đã áp dụng; ghi môi trường/source/schema trước nâng. Chưa khớp thì dừng và báo, không sửa checksum cho qua.
3. Chạy `tools/database/Update-DevelopmentG06.ps1 -Apply`: backup COPY_ONLY/CHECKSUM, áp022–024, đối chiếu hash dữ liệu gốc theo script và lưu manifest. Không tạo fixture/reset mật khẩu.
4. Chỉ khi bước trước thành công, chạy `tools/database/Update-DevelopmentG07.ps1 -Apply`: backup tương ứng, áp025–027, đối chiếu hash và manifest. Giữ backup; không tự xóa backup vận hành.
5. Đối chiếu schema027/procedure/quyền EXECUTE đã có; kiểm read-only Dashboard qua DAL/BLL với identity runtime và account ADMIN hiện có. Mở lại bản Debug/F5 để kiểm Tổng quan đọc số liệu thực, không gán0 khi lỗi. Đối chiếu doanh thu theo ngày trả/đơn mở/nợ bằng truy vấn đọc; kiểm report/options và đường vào nhân sự/tài sản theo ảnh hưởng trong phạm vi đọc. Không ghi giao dịch test vào development.
6. Giữ source/schema cố định sau nâng trong lượt kiểm. Ca chuột/phím không thao tác được ghi BLOCKED; hình thức do người dùng tự kiểm, DPI125% chức năng theo AGENTS. Nâng development thành công không chốt các ID G06/G07/G08 chưa đủ bằng chứng.

Phạm vi duyệt gồm backup, nâng hai chặng, đối chiếu bảo toàn dữ liệu và kiểm đọc sau nâng. Chưa đề nghị sửa code/theme hoặc thêm tính năng. Nếu preflight phát hiện dữ liệu cũ không hợp lệ, báo bằng chứng để duyệt xử lý riêng, không tự sửa dữ liệu cho migration chạy qua.

## Ảnh hưởng và rủi ro thực tế

Schema021 cũng thiếu contract của Nhân sự/Lương/Tài sản/Báo cáo nên ảnh hưởng rộng hơn riêng Home. Migration thay schema DB development đang dùng; cần backup, đóng app, bảo toàn tài khoản/mật khẩu và dữ liệu nghiệp vụ. Các script có guard marker/checksum/hash nhưng chưa được chạy trên DB development này ở đợt điều tra. Nếu đối chiếu thất bại, giữ backup và dừng dùng bản đã nâng để điều tra, không báo thành công hoặc tự restore đè.

Việc cải thiện thông báo khi schema không tương thích và layout các nút là đề xuất riêng, không gộp vào nguyên nhân thiếu procedure hoặc tự sửa trong đợt này.
