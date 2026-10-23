## Chốt G03 ngày 20/10/2026 — PASS theo quyết định người dùng

Người dùng chấp nhận nghiệm thu dựa trên đợt test hiện tại và yêu cầu chốt PASS. **PASS với ngoại lệ kiểm thử**, không phải47/47cađãchạyPASS. Xem [quyết định và các ngoại lệ](acceptance-decision.md). Không tiếp tục chặn nghiệm thu người dùng vì các ca còn thiếu; giữ nguyên số liệu/bằng chứng lịch sử phía dưới. Phần G04 chưa được triển khai hoặc nghiệm thu trong quyết định này.

---

## Xác nhận của người dùng — 20/10/2026

Người dùng phản hồi sau khi nhận bản sửa: “ok test vậy mình thấy đủ ok”. Ghi nhận chấp nhận giao diện lịch/nút lịch bản mới trong phiên manual hiện tại, kết hợp bằng chứng bounds125% đã PASS. Phần lỗi visual B04 được người dùng chấp nhận đóng. Không suy rộng phản hồi này thành đã chạy bàn phím, Designer hoặc DPI100/150%; các ca đó giữ NOT RUN. Không thay đổi kết quả nghiệm thu toàn nhóm G03 hay các ca nghiệp vụ còn lại. Không sửa source hoặc dữ liệu DB trong lượt cập nhật này.
## 20/10/2026 — B04 sửa bổ sung sau duyệt

Popup đã đồng bộ theo native size; icon lịch thay nút chữ bị cắt. Build Debug/Release đạt;20unit+11logicngày+2bounds125%PASS. Native visual/keyboard editor và DPI100/150% chưa nghiệm thu. B04 chưa đóng. Biên bản: runs/g03-b04-followup/test-results.md (tính từ nhóm). Phiên test mới PID9684, cùng DB test, chờ đăng nhập; không ghi dữ liệu khuyến mãi.

## Cập nhật 20/10/2026 — B04 GUI FAIL

Popup lịch bị cắt và nút lịch bị rút chữ theo ảnh người dùng. B04 chưa đóng; đã ghi điều tra và phạm vi sửa bổ sung tại [bug-report T13](t13-uu-dai-diem/bug-report.md). Chờ duyệt bổ sung, chưa sửa source trong lượt test. Build/unit/in-process PASS trước đó không thay thế kiểm tra native GUI.

# G03 — Kết quả đợt nghiệm thu được duyệt ngày 18/10/2026

**20/10 — G03-B04 đãduyệtsửa:** datefieldOrbitTextBox+nativeMonthCalendarpaletteápdụng3ôT13, dirty/previeweventgiữđúng; buildDebug/Release0warning0error, rulesunit20/20, datecontrolinprocess11/11PASS. [Regression](t13-uu-dai-diem/bug-test-results.md). Nativemàu/popup/keyboard/DPIchờmanual, B04chưađóng. AppmớiđãmởDBtestcũ; KM20261020vẫnngày20–21/10/rate0/receipt1, khôngtựthaydata.

**20/10 — T13manualM36:** KM20261020đãlưuđúngngày nhưng0%kháckịchbản20, chờxácnhậninputtrướckếtluậnlỗilưu. Người dùngbáoDatePickertrắng/lệchtheme; [G03-B04](t13-uu-dai-diem/bug-report.md) điều traôdatehaypopuplịch, chưasửatronglượttest. KhôngđánhM36PASS20%hoặcUIđạt từlưuthànhcông.

**20/10 — tiếp tục nghiệp vụ, tạm gác đánh giá UI theo người dùng:** LưurecipeSP981quaappđãghi2dòng/1receiptReplaceRecipe, tồnkhôngđổi. LượngthựctếNVL01=0.0200/NVL02=0.0180kháckịchbảngốc, chờxácnhậninputtrướckếtluậnmapping. [Evidence](runs/g03-b03-fix/db-after-manual-save.json). B03visual/nativechưađủregression nênchưađóng; khôngsuyPASSUI từviệctạmgác.

**20/10 — G03-B03 đãduyệtsửa:** đãápdụng2fileUI choeditortheme/affordance/unitcommit/xóadòngADMIN, giữcontract. Debug/Release0warning0error, unit20/20PASS; [regression](t10-san-pham-cong-thuc/bug-test-results.md). Apptestmới dùngcùngDBđãmở, chờmanualnative đểđóngbug; chưađánhGUIhếtlỗihoặcnângPASS47ID. M17FAILlàlịch sửtrướcsửa, khôngxóa.

**20/10 — phát hiện FAIL GUI sản phẩm:** M17editorcôngthức chưađạt qua4ảnhngườidùng; điều tra vàgộp [G03-B03](t10-san-pham-cong-thuc/bug-report.md): combo trắng/theme/clip, unitdelay, ôđịnhmức khó nhậnbiết, thiếuthaotácxóadòng. DBSP981chưacórecipe, khôngtickM17PASS. Chờduyệtđợtsửa/regression; sourcegiữ nguyên. Số20PASS/27BLOCKED và0FAIL bên dưới là snapshotđợt18/10; hiệnmanualcóUXsubcaseFAIL mới, không dùng tổngcũ đểgọiG03khôngcólỗi. T10vàG03vẫnĐanglàm.

**Tiến độ manual19/10:** [M01–M10](runs/manual-g03-20261019/progress.md) có chứng cứ tạoNVL81/tồn0, mởsổ0.0180Kg, chặnmởsổlại, renamegiữtồn, khóađơnvị/xóa sau mốc; NVL82chặnsốâm và mởsổchínhxác1.2345Kg. M09nămchữsốlẻ cònchờ xác nhận thao tácLưu, không ticktừảnhthôngbáocũ. Chưa đóngtask hoặc nângPASS toànID cònnhánhchưa chạy; manualđangtiếptục, DBtestgiữtheomanifestđến21/10.

**19/10 — người dùng chọn kiểm GUI thủ công có hướng dẫn:** đã chuẩn bị [phiên manual riêng](runs/manual-g03-20261019/progress.md), setupCompleted/runtime readPASS. App được mở tới DBtest trong manifest, agent sẽ đối chiếu SQL sau từng thao tác. Source cố định; bước đầu tạoNVL81 đang chờ người dùng, chưa nângPASS. Một DB/PIDtest được giữ cho phiên manual tới21/10/2026, có cleanup kiểm tên/marker; các số0tài nguyên của lượt trước không bao gồm phiên manual mới.

**Sau duyệt tiếp tục19/10:** [kiểm lại helper với skill26.1013.40934](runs/modal-investigation-20261019/recheck-40934.md), reset phiên/import mới; parent inputPASS, modal vẫn lỗi cache cùng thông điệp trên reproducer. Đã dọn process, không tạo DB mới. Chưa có cách sửa helper qua API hiện có, không sửa sản phẩm để né;47ID giữ20PASS/27BLOCKED.

**19/10 — đã điều tra input modal bằng reproducer độc lập:** [báo cáo và log](runs/modal-investigation-20261019/report.md). Native TextBox ở form chính nhận input; ShowDialog không dùng code CoffeeManager tái hiện lỗicache phần tử và failed to activate captured window. Khoanh vùng helper modal/cache/activation, chưa biết nguyên nhân code bên trong helper; chưa sửa triệt để helper. Không yêu cầu người dùng nhập hộ, không sửa app để né. Đã dọn2processdiagnostic và DB/PIDGUI test, không còn phiên này giữ lại.139file sản phẩm không đổi;20/47PASS,27BLOCKED giữ nguyên. Các nhãn giữ DB/chờ manual phía dưới là lịch sử đã kết thúc.

**GUI sau login thủ công:** đã xác nhận native sessionADMIN và mở Nguyên liệu/Tồn tải12dòng từ DB test; B02 đã xử lý/PASS. Mở được dialog Thêm nguyên liệu nhưng helper báo element không có trong cached app state; sau refresh/focus, type_text không xuất hiện trên ảnh/tree. Không retry mù hoặc coi là bug app. Đã nhờ người dùng nhập NVL81/Tên Cà phê kiểm thử GUI/Kg và lưu để đối chiếu; chưa mở sổ, chưa tăng số IDPASS. DB/PID test mới vẫn giữ cho phiên này theo manifest.

**G03-B02 đã được duyệt và sửa fixture:** DB/PID Pending cũ đã dọn. DB mới đi qua LegacyReview→sp_SetupConfirmLegacy→Completed; runtime đọc setup PASS, source139file không đổi. [Regression](runs/g03-b02-fixture/regression.json). App test mới đang chờ người dùng login thủ công, nên chưa đóng B02 hoặc nâng PASS ca GUI. Một DB test mới giữ cho phiên này đến19/10/2026; [manifest/cách dọn](runs/g03-b02-fixture/manifest.json). Các nhãn chờ duyệt B02 phía dưới là lịch sử trước lần duyệt.

**Blocker mới đã điều tra:** người dùng thử login phiên GUI và app báo cần setup. DB test thực tế Pending/ActiveAdmins1 do fixture sao tài khoản sau migration002 chưa bootstrap; app chặn đúng contract. [G03-B02](t09-nguyen-lieu-mo-so/bug-report.md) đề nghị dựng lại fixture bằng luồng setup chuẩn, chờ duyệt bổ sung; chưa sửa source/fixture để né lỗi. Kết quả47ID chưa đổi. Không cần người dùng thử đăng nhập lại phiên Pending.

**Tiếp tục GUI theo yêu cầu người dùng:** helper đã capture đúng Designer sau activate/recapture; app trên DB test mới đã mở đúng màn đăng nhập. Đang chờ người dùng đăng nhập admin thủ công theo giới hạn computer-use; chưa nâng trạng thái ca GUI nào. [Tiến độ và tài nguyên đang giữ](runs/gui-continuation-20261018/progress.md), [manifest](runs/gui-continuation-20261018/manifest.json). Một DB/tiến trình test giữ riêng cho phiên manual, hạn19/10/2026; có cleanup.ps1 kiểm tên+marker. Các số0DB giữ lại phía dưới là kết quả những lượt đã kết thúc, không bao gồm phiên đang chờ này.

**Cập nhật sau duyệt debug G03-B01:** runner đã gọi/lưu unit test và tổng hợp FAIL đúng; [regression 3/3 PASS](t09-nguyen-lieu-mo-so/bug-test-results.md) cho đường bình thường, SQL lỗi và unit lỗi chủ động. Đã dọn6/6 DB regression, không còn DB giữ lại. Bug G03-B01 đã xử lý; phần “chờ duyệt bug” dưới đây là lịch sử trước lần duyệt này. Kết quả47ID vẫn20PASS/27BLOCKED; blocker GUI chưa thay đổi.

**Trạng thái hiện tại: Đang làm, chưa nghiệm thu hoàn tất.** Người dùng đã duyệt đợt test riêng. Không sửa source/migration/existing test trong lượt test; không commit/push.

| Phạm vi | Kết quả |
|---|---|
| 47 ID gốc | **20 PASS / 27 BLOCKED / 0 FAIL**; các phần manual chưa có bằng chứng không được suy PASS |
| T09 / T10 / T11 / T12 / T13 | Lần lượt 4/9, 2/10, 3/8, 3/9, 8/11 PASS; phần còn lại BLOCKED |
| Unit G03/MaterialRulesTests | **20/20 PASS**, 0 skip; [TRX](runs/acceptance-unit-20261018/g03-unit.trx) |
| Runner clean/demo | Năm batch SQL PASS; baseline→009 trên hai DB; runner chưa gọi unit, xem G03-B01 |
| Phép kiểm SQL bổ sung | **132/132 PASS** hợp lệ; không phải 132 ID nghiệm thu mới |
| Source cố định | [139 file đối chiếu, 0 thay đổi](runs/acceptance-extended-20261018/source-audit-final.json) |
| Tài nguyên | 4 DB tạo/4 dọn/0 giữ lại; tiến trình app test đã đóng; không tạo login server mới |

Hồ sơ theo task giữ nguyên Expected/ID gốc và ghi từng phép kiểm: [T09](t09-nguyen-lieu-mo-so/test-results.md), [T10](t10-san-pham-cong-thuc/test-results.md), [T11](t11-ban-khu-vuc/test-results.md), [T12](t12-khach-hang/test-results.md), [T13](t13-uu-dai-diem/test-results.md). [Tổng hợp máy đọc](runs/acceptance-extended-20261018/acceptance-summary.json).

Đã kiểm SQL Server thật: quyền runtime, boundary/precision/NULL/FK, mở sổ đúng một lần, lịch sử snapshot, rollback tiêm lỗi, receipt/revision, policy/discount/date/tier. Kiểm hai connection có quan sát chờ khóa cho mở sổ nguyên liệu/khách, trừ điểm, công thức với checkout baseline và policy với create/open/adjust/checkout theo hai chiều. Baseline checkout không thay regression checkout đích G04. Rerun/checksum và preflight dữ liệu legacy được kiểm trên bản sao DB test, không sửa dữ liệu development để né lỗi.

**BLOCKED GUI:** cửa sổ đăng nhập test đã mở nhưng công cụ Windows trả lỗi active request; sau reset, tree đúng target nhưng ảnh là cửa sổ khác. Dừng nhập; chưa nhập mật khẩu/bấm login. Chưa thực hiện CRUD/selector native, Designer, resize/DPI, bàn phím, trạng thái loading/lỗi/timeout, vòng điều hướng/đăng xuất. Cần công cụ Windows hoạt động đúng để tiếp tục phần này. Tích hợp POS/checkout đích T14–T16 vẫn NOT RUN theo phụ thuộc.

**Lịch sử phép đo:** các JSON sql-results.json và concurrency-fault-results/v2 có FAIL do harness: SELECT XACT_STATE trực tiếp, EXECUTE AS trên pooled connection không REVERT, chuyển timestamp không qua binary(8), lấy SPID bằng thuộc tính không tồn tại. Giữ raw log; chỉ sql-results-v2 và concurrency-fault-results-v3 là kết quả hợp lệ. Ca refresh tuần tự trong sql-history không được gọi là hai session; table-refresh-two.json là lượt hai connection riêng. Các thay đổi chỉ ở script đo mới trong evidence, source sản phẩm/test hiện có giữ nguyên.

**Đợt bug chờ duyệt:** [G03-B01 — runner bỏ unit và tổng hợp trạng thái sai](t09-nguyen-lieu-mo-so/bug-report.md). Chưa debug; không có lỗi nghiệp vụ đã chứng minh từ các phép kiểm hợp lệ. Đề nghị duyệt một đợt sửa runner và regression đường PASS/SQL FAIL/unit FAIL, gồm kiểm cleanup.

Evidence chạy chính: [runner](runs/acceptance_20261018_201052_07b32f2a/manifest.json), [supplemental/cleanup](runs/acceptance-extended-20261018/manifest.json), [preflight cleanup](runs/acceptance-extended-20261018/preflight-resource.json). SQL/script/input/expected/actual nằm cạnh JSON từng subcase; DB đã dọn nên không chạy lại script lên development.

---

# Lịch sử trước đợt test — Báo cáo triển khai

Phần dưới giữ nguyên lịch sử trước khi người dùng duyệt test; các nhãn NOT RUN/chờ duyệt ở phần lịch sử đã được kết quả hiện tại phía trên thay thế.

**Ngày:** 18/10/2026. **Trạng thái:** Đang làm; đã triển khai T09–T13 theo bộ plan
được duyệt, chưa nghiệm thu. **47 ID gốc đều NOT RUN trong đợt nghiệm thu G03.**
Không có commit/push G03. Source đang có thay đổi từ các đợt trước, giữ nguyên.

## Đầu ra

| Task | Phần đã triển khai | Hồ sơ |
|---|---|---|
| T09 | Migration004; nguyên liệu/đơn vị, cutover snapshot và mở sổ ADMIN một lần; CASH đọc tồn/STORE quản trị theo quyền; receipt/revision | [Plan](t09-nguyen-lieu-mo-so/plan.md), [technical](t09-nguyen-lieu-mo-so/technical-checks.md), [test chưa chạy](t09-nguyen-lieu-mo-so/test-results.md) |
| T10 | Migration005; menu/ngừng-bật bán, recipeTVP nguyên tử, bảo toàn snapshot, khóa Recipes với checkout baseline | [Plan](t10-san-pham-cong-thuc/plan.md), [technical](t10-san-pham-cong-thuc/technical-checks.md), [test chưa chạy](t10-san-pham-cong-thuc/test-results.md) |
| T11 | Migration006; CRUD bàn/khu vực, sơ đồ/view, chọn lại đơn hiện tại, BLL session/quyền | [Plan](t11-ban-khu-vuc/plan.md), [technical](t11-ban-khu-vuc/technical-checks.md), [test chưa chạy](t11-ban-khu-vuc/test-results.md) |
| T12 | Migration007; khách/NULL SDT/sequence mã khách/hạng0, cutover điểm và mở sổ riêng, selector/đọc đơn hẹp | [Plan](t12-khach-hang/plan.md), [technical](t12-khach-hang/technical-checks.md), [test chưa chạy](t12-khach-hang/test-results.md) |
| T13 | Migrations008/009; preview SQL, KM, policy preview/thay nguyên tử, adjustment/history và khóa Policy; tương thích mãKM cũ | [Plan](t13-uu-dai-diem/plan.md), [technical](t13-uu-dai-diem/technical-checks.md), [test chưa chạy](t13-uu-dai-diem/test-results.md) |

DTO/request, repository/service riêng theo nghiệp vụ. UI dùng RecordListControl
cho layout/loading và OperationEditorDialog cho giữ payload/request/đối chiếu;
không thêm generic repository/UnitOfWork. Editor/recipe/selector dùng control native,
AppTheme/Orbit hiện có, mặc định không bụi sao. Chỉ tạo mới KM yêu cầu KM+8số;
mã seed như KM24BF được giữ khi sửa/xóa. 009 bổ sung cùng T13 vì008 đã áp dụng,
không sửa checksum008 hoặc khóa lịch sử.

## Kiểm chứng kỹ thuật đã chạy

- [Build cuối](runs/build_20261018_200326/manifest.json): Debug và Release đều
  thành công,0warning/0error; log có cạnh manifest, hash exe tại cùng lượt.
- [SQL cuối](runs/technical_20261018_195707_26208544/manifest.json): baseline/
  seed khi cần và001→009 trên clean/demo; dependency smoke nguyên liệu→menu→bàn→
  khách→preview/policy/điểm và mãKM seed. Đối chiếu tồn trước/sau migration không đổi.
  Đây là compile/smoke, không thay 47 ID, rollback/fault/concurrency hoặc quyền mutate.
- [Mẫu native T09](runs/t09-layout-technical/): đã mở/render danh sách/editor và
  state mẫu tải/rỗng/lỗi/có dữ liệu. Dữ liệu giả được ghi đúng tính chất kỹ thuật.
- [Đọc màn development đúng lượt](runs/development-ui-technical-r3/result.json):
  ADMIN/CASH đọc tồn12, menu10, bàn10, khách10; ADMIN đọc11KM, CASH rỗng vì ngày
  hiện tại không có chương trình; STORE đọc tồn12/menu10. Collector dùng identity
  hiện có để nối/read UI, không thay kiểm đăng nhập, mutate, keyboard hoặc Designer.
- [Source manifest](source-implementation.csv) và47dòng checklist đã đối chiếu
  không đổi; script PowerShell mới đã parse cú pháp, chưa chạy runner nghiệm thu.

Các giới hạn kỹ thuật còn công khai: collector đầu bị callback VisibleChanged sau
Dispose; đã thêm lifecycle guard trong pha triển khai. Lượt r2 thu hình lệch màn
vì chưa chờ điều hướng, không dùng làm chứng cứ đúng module. Lượt r3 chờ đúng loại
control và datasource rồi mới ghi ảnh. Các lỗi compile/fixture trước cũng giữ log,
không xóa hoặc chuyển thành PASS. Điều chỉnh minimum cửa sổ theo WorkingArea/Snap
ở build cuối chưa có đủ ma trận DPI/resize để nghiệm thu.

## Development và tài nguyên

[Hướng dẫn F5/dùng thử](development-guide.md): cùng solution/startup/profile củaG02,
`localhost/CoffeeManager_Development`, schema009. Cấu hình DPAPI/account giữ nguyên;
không seed lại, không reset account, không ghi fixture nghiệm thu vào DBdevelopment.

Đã backup trước nâng schema và đối chiếu tồn/điểm+hạng/tài chính hóa đơn không đổi:
[lượt004–008](runs/development_20261018_195015_5dc082ac/manifest.json),
[lượt009](runs/development_20261018_195724_5058a866/manifest.json).
Có2backup COPY_ONLY CHECKSUM giữ đến khi G03 nghiệm thu và người dùng chấp nhận;
đường dẫn server và chính sách giữ có trong từng manifest. Backup không phải DBtest.

[Resource summary](resource-summary.json):11lượt SQL kỹ thuật,18DB tạo/18dọn/0giữ.
Mỗi tên trong allowlist được xác minh chưa có trước tạo và kiểm marker trước DROP;
không động vào DB có sẵn. Lượt không kết nối được SQL tạo0tài nguyên. Không tạo
Windows user/SQLlogin mới trong G03. Runtime development giữ principal cũ;
operator chỉ dùng cho migration/backup/fixture DBtest đã duyệt trong bộ plan.

## Đợt test cần duyệt riêng

[Hướng dẫn test](manual-test-guide.md), [phạm vi từng task](plans.md).
`tools/database/Test-G03.ps1 -ApprovedAcceptance` chỉ chạy sau duyệt riêng:
unit rules và SQL contract subsets trên DBtest riêng, source trước/sau, TRX/log,
finally cleanup. Phần tự động không đủ mọi subcase, cần bổ sung theo bảng plan:

- T09: tồn/mốc/transaction, biên đơn vị/lượng, mởsổ lặp/đồng thời, quyền và refresh.
- T10: menu/recipe/rollback, selector/ngừng bán, snapshot và2session checkout baseline.
- T11: CRUD/FK/view/seed/paid/hủy, chọn bàn stale, role và sơ đồ.
- T12: SDT NULL/trùng, ngưỡng/điểm mởsổ/hạng, profile giữ số dư, selector/history/quyền.
- T13: ưu đãi/ship/làm tròn/ngày, policy rollback/tái phân hạng, history bất biến,
  điều chỉnh trừ đồng thời và quyền/reconcile.
- Mọi màn: GUI thật/Designer, hai độ phân giải×3DPI, Snap, keyboard/loading/lỗi/
  stale/unknown/querymuộn/session, đo100đổi module và10login/logout.

Expected/actual của từng subcase ghi riêng tại task; chưa có kết quả được tick.
T09-K09, T10-K04/K06–K08 và UX-K06/09/10 có phần baseline G03 và phần tích hợp
T14–T16/G04 riêng. G03 chưa cung cấp POS/checkout đích; giữ phần G04 NOT RUN,
không đánh dấu toàn ca PASS từ fixture. Phát hiện FAIL trong đợt test sẽ ghi
bug-report tại task sở hữu, gom đợt và chờ duyệt trước debug/regression.




