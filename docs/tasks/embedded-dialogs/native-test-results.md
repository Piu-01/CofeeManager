# Kiểm bản Form modal Windows —26/10/2026

Đợt test đã được người dùng duyệt. Đang chạy, chưa nghiệm thu toàn task.
Source210file cố định theo runs/native-acceptance-20261026/source-frozen.csv;
runner dialog dùng đúng DLLDebug đã bàn giao. DeviceDpi120; không đổiWindowsScale.
DB riêng CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout, schema016,
marker20261025_181739_98e5df59. Không tạo DB mới, không đổi profiledevelopment.

## Phần đã chạy

-87/87 dialog component/runtimeSQL: Form modal có ownerMainForm/viền native,
  ShowInTaskbarfalse và boundsWorkingArea ở hai client1280×720/1366×768; nhập món,
  F3ba kênh/UI06lỗi đúng ba ô/focus, F4load, detailpaid khóaPOS, delivery,
  checkout80k/abc/overflowkhóaConfirm/100kchoConfirm, CustomerPicker,7catalogkinds,
  picker lồngOperationEditorgiữinput,preview và menuconstructor3role vớiAuththật.
  SnapshotOrders/Payments/Consumption/Stock/Points trước-sau runner bằng nhau.
  Actor NNA01/NNC01/NNS01 đã gỡ sau chạy, không checkout hoặc lưuCRUD.
-18/18 owner/close component follow-up: title/X, ownerchain, WindowsIsWindowEnabled
  chỉtopdialog, FormShowInTaskbar chỉMainForm, printerowneractivechild, OK/Cancel,
  No giữ/Yes bỏ nháp, abortactive/queuedkhôngorphan.
-18/18 xUnit Sales/Checkout/Delivery,0fail/0skip.

Các kết quả trên là componentmetadata/PerformClick/reflection/OSenabled state,
không phải inputchuột thật hoặc nghiệm thu hình thức.105component +18unit không
đồng nghĩa toàn13caND PASS. Hình thức userkiểm;100/150%NOT RUN.

## ND-TEST-01 — đồng bộ fixture đóng lần hai

Lượt đầu rawownerfixture14assertionPASS rồi timeout ở lần đóng sau No. Bản chỉ
thêm log (không đổi thao tác/expected) tái hiện: ClosingGuard=false trướcClose1,
ClosingGuard=true trướcClose2. Fixture chỉ đợi ownerđượcWindowsbật lại và hộp
confirmbịgỡ, chưa đợi continuationguard bỏ nháp hoàn tất nên gửiClose2 quá sớm.

Follow-up chờ guard hoàn tất trướcClose2, giữ nguyên expected/thao tác khác,
18/18đạt và trước cả haiClose ClosingGuard=false. Không sửa source sản phẩm,
không đổi expected cũ hoặc xóaFAIL. Giữ native-smoke-first-failure.json,
NativeObservation.cs/native-observation-results.json và
NativeSynchronized.cs/native-synchronized-results.json. Phân loại lỗi đồng bộ
fixture theo bằng chứng; chưa coi đây là chứng nhận thao tácXnative liên tiếp.

## Tiến độ ca gốc của đợt native

| ID | Actual | Trạng thái toàn ca |
|---|---|---|
| ND01 | Inventory dialog modal/owner/frame/ShowInTaskbar component đạt | PARTIAL, chờ user xác nhận kiểuWindows và các đường gọi native |
| ND02 | OS khóa main/cha, chỉtopenabled | PARTIAL, phím/inputnative chưa đủ |
| ND03 | Native childOK/Cancel, productionpickergiữinput | PARTIAL, user đang kiểm nestedF3 |
| ND04 | DirtyNo/Yes follow-upđạt, rawfixtureracegiữ | PARTIAL, X/Esc/đóngMain/busyunknownnative |
| ND05 | Active/queuedabortkhôngorphan | PARTIAL, sessionrevoke/error/reconcile chưa đủ |
| ND06 | Open/read/close các editor,0financialchange | PARTIAL, CRUDSave/Error/Refresh chưa đủ |
| ND07 | F3typed3kênh/UI06đúng3ô | PARTIAL, nativecancel/lookupfail chưa đủ |
| ND08 | F4loadrealSQL | PARTIAL, nativepaging/doubleclick/returnPOS |
| ND09 | Invalidtender3cases/valid100k component đạt | PARTIAL,475Enter/chốt/No đang chờ, cạnh tranh/unknown chưa đủ |
| ND10 | Previewmodal, printerowneractivechild | PARTIAL, nativeprint/PDF/error/reprint chưa đủ |
| ND11 | Menu3role actualAuth, fixtureactorsremoved | PARTIAL, native/session/lifetime/handles chưa đủ |
| ND12 | BoundsWorkingArea hai clients125% | PARTIAL, kỹthuật nhiều dòng/footer/focus/scroll; hìnhthứcuser |
| ND13 | Helper giữ hash; focus ởF3/checkoutcomponent | PARTIAL, nativeclick/Tab/caret/paste/IME/gridreused |

## Native đang chờ

Bản native-implementation-20261026/Debug mở bằng normalLoginPID27260 vào cùng
DBtest.475–478 đều Chua thanh toan81000 tại preflight; chưa có phản hồi chốt475.
User được hỏi đúng kiểuWindowsmodal rồi nestedF3→Thêmkhách, No/Yes bỏ nháp;
nếukiểuđúng thì475F8tender100000/Enterkhôngchốt/Confirm→Khôngin→POStrống.
Chưa ghiPASSnative từ việc agent mởapp hoặc đọcsource.

Bằng chứng ở runs/native-acceptance-20261026/: before.json,dialog-results.json,
native-synchronized-results.json,unit-results/native-sales-rules.trx,progress.json,
source-frozen.csv,dll-hashes.json. Giữ DBtest đến27/10 vì active native acceptance;
không cleanup khi user đang thao tác, không chốt lại đơn đã trả.

## Native475 — user xác nhận và SQL đối chiếu

User xác nhận đúng kiểu Form Windows titlebar/X/owner/không thêm taskbar; nested
F3→Thêmkhách tênNDnháp, Hủy→ởlạigiữtên, Hủy→Đồngý bỏ quayđúngF3. Đây là xác nhận
hình thức/luồng của người dùng; agent không tự nghiệm thu hình thức.

User xác nhận475F8: clickchọnhết, gõ100000khôngxóa, thối19000; Enterchưachốt;
Confirmđồngý→Khôngin→POStrống. SQL verdict-475.json: paid81000, mộtPT00181000,
NVL01−0,018/NVL03−0,010 khớp snapshot, nguyênliệukháckhôngđổi, điểmkhôngđổi.
Native nhánhKhôngin đạt, không suy mọi subcaseND09/ND10 đãPASS.476–478 còn mở.

## Mẫu mới riêng Lịch sử hóa đơn —26/10/2026

User yêu cầu code trang theo Orbit_LichSuHoaDon_V4.zip. Đã đọc mẫu/source và
lưu nguồn/hash dưới docs/ui/invoice-history-v4. Chưa sửa product; proposal phạm
vi bổ sung tại G04/ui-invoice-history-v4/proposal.md, chờ duyệt trước plan.
Giữ kết quả475NoPrint và câu hỏi476Có→Hủy chưa được trả lời, không suy hoàn tất
476/ND10. Evidence/source của lượt native acceptance không đổi vì đọc mẫu mới.

## Chốt lượt native trước chuyển source HistoryV4

User ưu tiên yêu cầu UIhistoryV4 và đã duyệt plan/làm rõ kínhlúp–filter. Lượt
native theo source210file được giữ ở mốc hiện có:105component+18unit, userxác
nhậnnative/nestedF3/475NoPrint vàSQL475đạt.476Có→Hủy chưa xác nhận nên giữchưađủ,
không coi paid/hủy hoặcPASSND10. Sau mốc đó source chuyển phiên bản để triển khai
V4; không chạy tiếp nghiệm thu native trên source đã đổi hoặc gánPASScũ cho V4.
