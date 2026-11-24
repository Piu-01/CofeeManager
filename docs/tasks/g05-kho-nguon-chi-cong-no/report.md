# G05 — B01–B07 đã sửa; còn nhánh nghiệm thu chưa đủ

**Đợt UI10/11/2026 đã duyệt/sửa:** Kho/Nhân sự dùng tab tối Orbit hiện có,
giữ đủ nhãn, body theo nhu cầu; Tồn kho giãn đầy.100/100+12/12 kiểm module thật
PASS tại125%, DB test schema024 riêng đã dọn. Nhân sự669→233ms trong cặp đo;
không kết luận Kho mở nhanh hơn. Snapshot Debug/Release và workspace Debug
buildPASS; workspace Release còn BLOCKED do sửa ngôn ngữ đồng thời ngoài3file.
[Biên bản đầy đủ](ui-tabs-regression-20261110.md). Không nâng55ID hoặc trạng
thái toàn G05; hình thứcSKIPPED, DPI100/150%NOT RUN, chưa commit/push.

**Bổ sung09/11/2026, chờ duyệt G05/G06-UI:** người dùng báo tab Kho/Nhân sự có
chrome trắng, Tồn kho bị co và giật khi click. Đã điều tra source, chưa sửa/
chạy test mới; [báo cáo và phạm vi cả đợt](ui-tabs-investigation-20261109.md).
Kết quả B01–B07 dưới đây giữ làm lịch sử, không phủ định bug mới.
30/10/2026. **B01–B07 đã sửa và kiểm theo phạm vi. G05 vẫn Đang làm:** STORE nhập/trả hàng, ADMIN trả nợ/link chi và logout/relogin ADMIN/CASH bổ sung PASS; native keyboard và các nhánh nghiệm thu/UX còn thiếu. Không đổi bảng55ID lịch sử.

**Đối chiếu bằng chứng hiện tại:44/55 đủ PASS,11ca chưa đủ để chốt** — [bảng từng task, ID và phần còn thiếu](coverage-current.md). Đây là tổng hợp kế thừa38PASS lịch sử và6ID bổ sung đủ bằng chứng, không phải một lượt chạy mới toàn55ca; không thay bảng27/10.

## Thay đổi và kiểm chứng đợt29/10

| Bug | Sửa | Kết quả và bằng chứng |
|---|---|---|
|[B01 — workspace](t20-nhap-kho/bug-report.md)|Ghép control trực tiếp vào slot tự đo, bỏ Dock.Fill/container làm co rộng0; filter StackedTools; nối pager hiển thị với handler/tổng DTO.|16/16 component kiểm năm màn thật, hai client1280×720/1366×768, DeviceDpi120, dữ liệu >50 dòng PASS. Native Lập phiếu nhập mở được; danh sách/chi tiết nợ và KK tải được.|
|[B02 — kiểm kê NULL](t22-kiem-ke/bug-report.md)|IsDBNull trước đọc LinesJson; danh sách rỗng an toàn.|Get/List legacy rỗng, NULL khác0, chốt rỗng vẫn bị52902 PASS. Native KKTEST0001 mở chỉ đọc, Chốt disabled, không crash.|
|[B03 — lịch sử nợ NULL](t23-cong-no/bug-report.md)|IsDBNull trước đọc HistoryJson, giữ nguyên số dư.|Chưa trả/as-of lịch sử0, trả một phần/lần cuối, chặn STORE/CASH trả PASS. Native danh sách72 nguồn và lịch sử0/link PN mở đúng cây hộp thoại.|
|[B04 — hash nguồn tài chính](t19-thu-chi/bug-report.md)|Hash toàn bộ JSON phía SQL, manifest dùng Before/AfterMoneySourcesHash.|JSON25068 ký tự: hash lặp khớp; thêm dòng cuối làm hash đổi dù chunk2033 ký tự cách cũ không đổi. PASS; không chạy nâng development lần nữa.|

Chỉ sửa8 file: năm control G05, hai mapper DAL và Update-DevelopmentG05.ps1. Không đổi helper workspace chung hay migration017–021, không đổi nghiệp vụ/quyền. Nguyên nhân B01 đã đo trước sửa: slot rộng0; ghép đúng contract cho Primary141/Actions118 tại125%.

- Build Debug/Release:0warning/0error ([Debug](runs/debug-batch-20261029/debug-build.log), [Release](runs/debug-batch-20261029/release-build.log)).
- **22/22 xUnit PASS**, clean/demo SQL001–021 thành công và5 replay subcase PASS: [runner](runs/acceptance_20261029_134725_96d98d09/manifest.json), [unit](runs/acceptance_20261029_134725_96d98d09/unit.log).
- Typed regression **11/11 PASS**, gồm chuẩn bị fixture >50 dòng: [DB](runs/debug-batch-20261029/db-regression.json). Connection owner cho typed mapping/BLL, không thay kiểm SQL runtime principal. B04 ghi fixture qua public procedure với G05Runtime/CafeManagerApp.
- Component WinForms thật **16/16 PASS**: [layout/pager](runs/debug-batch-20261029/component-results.json). Nhánh filter Thu trả tập rỗng theo fixture; chưa nghiệm thu toàn bộ tổ hợp bộ lọc. Component/PerformClick tách riêng với thao tác chuột native.
- **B04 PASS**: [hash](runs/debug-batch-20261029/hash-regression.json). Đọc literal SQL từ AST source, không thực thi toàn script upgrade.
- Native: [nợ chưa trả](runs/debug-batch-20261029/native-debt-list.txt), [lịch sử0](runs/debug-batch-20261029/native-debt-detail.txt), [link nguồn lồng](runs/debug-batch-20261029/native-source-link.txt), [nút nhập](runs/debug-batch-20261029/native-purchase-list.txt), [editor](runs/debug-batch-20261029/native-purchase-editor.txt), [KK rỗng](runs/debug-batch-20261029/native-stocktake-detail.txt).
- Source cố định trong regression: [freeze](runs/debug-batch-20261029/source-freeze-check.json); tài chính/nguồn sau thao tác native chỉ đọc khớp hash trước mở app: [đối chiếu](runs/debug-batch-20261029/native-write-check.json).

## B05 — Đã duyệt, sửa và regression29/10

Chỉ sửa cơ chế opt-in của OrbitDialogLayout: cấp chiều ngang thực, đo wrap và chiều cao footer theo vùng đó trước FitToContent. **28/28 helper case,23/23 checkpoint trên9 dialog G05 PASS ở125%**; gồm resize, nút reconcile động, leading action, nhãn dài và legacy chưa opt-in.23 checkpoint đều trong WorkingArea; AcceptButton=null. Debug/Release0warning/0error,22/22 xUnit,5 SQL replay subcase PASS. Source cố định không đổi trong regression.

Native bỏ nháp nhập: Không giữ dòng, Có đóng editor về đúng cha; kho/PN/nợ không đổi. Native thu chi2345đ: xác nhận có owner → ghi TC20250508 → đóng/reload đúng một phiếu, một receipt; đối chiếu SQL PASS. Luồng preview→chốt nhập BLOCKED bởi B06, không coi đã PASS. [Biên bản B05 và giới hạn harness](runs/b05-20261029/test-results.md), [đối chiếu SQL](runs/b05-20261029/native-write-check.json), [freeze](runs/b05-20261029/source-freeze-check.json).

## B06 — Đã duyệt, sửa và regression29/10

Chỉ đổi hai dòng grid ở PurchaseEditorDialog/StockIssueEditorDialog: ReadOnly=false và EditOnEnter; giữ cột tham chiếu khóa, dùng helper chọn hết hiện có, không đổi default grid/SQL. [Điều tra T20](t20-nhap-kho/bug-report.md), [T21](t21-xuat-kho/bug-report.md).

- **54/54 component assertion PASS**: ADMIN/STORE sửa đúng cột/binding/selection, preview SQL, validation, STORE khóa trả ngay, unknown khóa body, default grid readonly, CASH lookup bị chặn. **22/22unit và5SQL subcase PASS** trên DB test schema001–021. Build Debug/Release sản phẩm0warning/0error; harness riêng1warning nullable được ghi rõ.
- Native ADMIN chốt nhập2×10000/trả4000/nợ16000 → PN20250504; chốt xuất0 bị chặn,9999 vượt tồn rollback giữ input, sửa1 → PX20250506; bỏ nháp sửa3 không ghi. SQL xác nhận đúng1PN/1PX/1chi4000/1nợ16000/2receipt; NVL12 tăng ròng1, các NVL khác giữ nguyên. Pending payload đã xóa sau commit.
- Không phát hiện bug mới. Native Tab chưa đủ bằng chứng do công cụ kích hoạt owner; không suy PASS đầy đủ STORE/trả hàng/native keyboard/crash/I/O/UX từ regression này. Source cố định. [Biên bản/lệnh/bằng chứng](runs/b06-20261029/test-results.md), [SQL](runs/b06-20261029/native-write-check.json), [freeze](runs/b06-20261029/source-freeze-check.json).

## Nghiệm thu nhóm và phần còn thiếu

Lượt55ID gần nhất ngày27/10: **38PASS/5FAIL/10BLOCKED/2NOTRUN**, giữ nguyên lịch sử trong [báo cáo trước debug](runs/debug-batch-20261029/report-before-debug.md) và test-results từng task. Đợt29/10 kiểm bốn bug, chưa chạy lại đủ từng nhánh của55ID để phát hành tổng PASS mới; không suy toàn ID từ component/SQL subset. Các FAIL lịch sử có nguyên nhân B01–B03 được đối chiếu với regression ở trên.

B05 clipping, B06 khóa nhập liệu và B07 vòng đời tải đã khắc phục. Đã bổ sung native STORE nhập/trả hàng, ADMIN trả nợ/link chi và logout/relogin ADMIN/CASH; tên51 ký tự T19-K02 và actor không tồn tại T20-K02 PASS subcase30/10. Chưa đủ mọi nhánh UX/pending/crash/restart/đa process/I/O/bàn phím/owner native print-file nên chưa phát hành tổng55ID mới. Hình thức SKIPPED theo phân công. Đã kiểm kỹ thuật125%;100/150% NOT RUN, Windows Scale giữ nguyên. MaBL fixture chưa thay tích hợp T26.

G04 giữ40PASS/0FAIL/16BLOCKED, T18 kế thừa8/8; không thay325ID/T01–T32. Không chốt hoàn tất riêng từng task.

## DB và tài nguyên

Development đã nâng016→021 ngày27/10 sau duyệt target;8 nhóm hash dữ liệu khớp backup trước nâng. [Bằng chứng27/10](runs/acceptance-full-20261027_110730/development-backup-verification.json). Đợt29/10 chỉ dùng DB test, không ghi fixture/nâng/reset development.

Đợt regression tạo2 DB test, **dọn2/còn0**; GUI đã đóng, credential DPAPI tạm đã xóa, không xóa pending thật. [Audit](runs/debug-batch-20261029/resource-audit.json). Backup development cũ không bị thao tác trong đợt này. Không đổi SQL service/authmode. Không commit/push.


Lượt B05 bổ sung tạo2 DBtest, dọn2/còn0; GUI23988 đã đóng và thư mục DPAPI đúng marker đã xóa: [audit](runs/b05-20261029/resource-audit.json). Không ghi/nâng development, không commit.


Lượt B06 bổ sung tạo2DBtest/dọn2/còn0; GUI17252 đóng bình thường, credential DPAPI đã xóa đúng marker, development không bị thao tác: [audit](runs/b06-20261029/resource-audit.json). Không commit/push.


## Cập nhật30/10/2026 — Native follow-up và B07

STORE nhập21000/trả ngay0, xem nợ chỉ đọc và xuất trả2 đơn vị PASS; ADMIN trả10000+11000, lịch sử và hai link phiếu chi đúng nguồn PASS. Hai subcase runtime T19-K02.Name51/T20-K02.ActorMissing PASS, rollback0:0/dữ liệu không đổi. Đối chiếu tiền/kho/nợ cuối PASS. Khi đăng xuất Công nợ phát sinh B07 CTS đã dispose, chờ duyệt bổ sung trước sửa; CASH native tiếp theo BLOCKED, native keyboard chưa đủ. Chi tiết [biên bản](runs/native-followup-20261029/test-results.md), [bug B07](t23-cong-no/bug-report.md). Source cố định; đã dọn1/1DB và credentials, development không bị ghi fixture. Không đổi55ID lịch sử hay chốt nghiệm thu toàn nhóm.


## B07 — Đã duyệt, sửa và regression30/10/2026

Người dùng duyệt bổ sung B07; đã sửa vòng đời CTS trong đúng năm control G05, không đổi SQL/quyền/ghi nghiệp vụ. Debug/Release0warning/0error,22/22unit,47/47lifecycle và5/5CASH-BLL PASS. Native chuyển năm màn, nháp modal, đóng lịch sử nợ→logout, CASH login/logout, ADMIN login lại/đóng MainForm PASS theo phạm vi. Counts/receipt/tồn không đổi; source cố định;1DB test dọn1/còn0, credentials đã xóa. [Biên bản B07](runs/b07-20261030/test-results.md), [bug](t23-cong-no/bug-report.md). Nội dung “B07 chờ duyệt” ở cập nhật trước là lịch sử trước phê duyệt. CASH native không còn bị chặn bởi B07; native keyboard vẫn chưa đủ do công cụ. Không đổi55ID lịch sử, G05 Đang làm do các nhánh nghiệm thu còn thiếu.

## T20b — Nhận hàng 2 bước, đợt test23/11/2026

Phần phát sinh của T20 đã được duyệt kế hoạch; đợt test được duyệt riêng. Migration 043 chạy trên DB test riêng schema001–043, tồn trước/sau không đổi, DB đã dọn0/còn0. **SQL35/35 PASS**, **unit25/25 PASS**, **probe in2/2 và UI component13/13 PASS**; build Debug/Release0 lỗi. Chưa chạy GUI thật end-to-end, preview WebView2/máy in, hai session song song, fault injection và DPI/hình thức — chuyển người dùng kiểm, không suy PASS. Không phát hiện bug mới; T20b vẫn Đang làm. Chi tiết: [biên bản T20b](nhan-hang/t20b-nhan-hang/test-results.md), [bằng chứng](nhan-hang/runs/acceptance_20261123_220006_d9d1cf50/).
