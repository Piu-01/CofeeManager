# Kiểm hộp nổi và chọn hết input —26/10/2026

Đợt test đã được user duyệt. Đang chạy, chưa nghiệm thu toàn task.
Source210file cố định; DLLDebug hash khớp cả runner host và runner dialog.
DB riêng CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout, marker
20261025_181739_98e5df59, schema016; quyền InvoiceDetail runtime đã phục hồi=1.
Không tạo DB mới, không thay profile DPAPI. DeviceDpi120, client đã đo1280×720,
1366×768 (host còn đo800×600). Hình thức SKIPPED,100/150%NOT RUN.

## Kết quả đã chạy

- 32/32 host/input component: embedded/non-top-level, khóa nền, nested result,
  trả focus/input cha, từ chối mở từ nền, guard bỏ nháp Có/Không, busy close,
  abort hoàn tất task,20vòng dispose; TextBox/multilineViệt/readonly/masked/
  NumericUpDown internal editor/ComboBox gõ và list-only, reentry/caret/dynamic.
- 87/87 dialog/runtime SQL component: ctor menu ba role qua AuthService thật,
  nhập món, header3kênh/UI06 ba trường, F4load, detailpaid khóaPOS, giao hàng,
  checkoutvalidation80k/abc/overflow/100k, CustomerPicker,7catalogkinds,
  picker lồng OperationEditor và preview. Đo bounds/dialog thật ở125%.
- 18/18 xUnit namespaceCafeManager.Tests.Sales,0fail/0skip.
- Sau87checks, snapshot toàn hóa đơn/payment/tiêu hao/tồn/điểm bằng trước runner.
  Actor đọcNEA01/NEC01/NES01 gỡ xong. Không gọi checkout trong các runner này.
- Fixture NEM01 tạo475–478 qua BLL trong DBtest cho ca native; có baseline
  before-manual.json. Các đơn còn mở lúc gửi hướng dẫn, chưa tự chốt bằng fixture.

Component dùng PerformClick/reflection/call handler trên WinForms thật, không
phải input chuột/bàn phím thật. Không coi119checks hoặc18unit là137IDED PASS.
Compilefixturehost đầu lỗi thiếu Cast<Control>, đã giữ source/log ban đầu rồi
sửa kiểu dữ liệu fixture; không sửa expected hoặc source sản phẩm để né lỗi.

## Theo dõi toàn ca

PARTIAL nghĩa có phần đã PASS nhưng chưa đủ toàn bộ subcase theo plan.

| Mã | Actual hiện có | Trạng thái toàn ca / còn cần |
|---|---|---|
| ED01 | Inventory dialog embedded/bounds,1top-level main ở host | PARTIAL; native các đường mở từ tab |
| ED02 | Background/sidebar disabled,onlytop enabled,focusrestore | PARTIAL; inputnative và phím hệ thống |
| ED03 | ChildOK/Cancel, production picker lồng giữ input | PARTIAL; user đang kiểm F3→Thêm khách→bỏ nháp |
| ED04 | GuardNo/Yes, busyrefusal, cancelcomponent | PARTIAL; X/Esc/MainFormclose thật với dirty/unknown |
| ED05 | Abort2stack hoàn tất task, disposedowner từ chối, busygiữ | PARTIAL; sessionrevoke/lỗi/unknown/reconcile thật |
| ED06 | Catalog7loại/read/editor/picker/detail openclose,0financialchange | PARTIAL; CRUDSave/Error/Refresh từng module |
| ED07 | Header3kênh typed; UI06lỗi ba ô/focus đúng | PARTIAL; nativeF3vàlookupfail/giữinput |
| ED08 | F4tải realSQL, rootopenerrepeat bị từ chối | PARTIAL; nativepaging/Enter/doubleclick/quayPOS |
| ED09 | Checkoutinvalid3case khóaConfirm,100000mở | PARTIAL; native475Enter/chốt/No, rồi concurrent/unknown |
| ED10 | Previewembedded mởđóng,khôngđổiDB | PARTIAL; Có→hủy/PDF/lỗisaucommit/reprint |
| ED11 | MainFormmenus3role actualAuth,20cycles/noorphans | PARTIAL; native/session/callback/resourcehandles |
| ED12 | Host3sizes/dialog2sizes,DeviceDpi120/boundswithinMain | PARTIAL; nhiều dòng/footer/focus/scroll; hìnhthứcuser |
| ED13 | Inputtypes/readonly/dynamic/caret component đạt | PARTIAL; gridreused/paste/IME và click/Tabnative |

## Native đang chờ

Đúngexe implementation-20261026/Debug, PID37712 mở đăng nhập thường vào DBtest.
User được yêu cầu nestedF3draft (Hủygiữ rồi Đồngýbỏ) trên475, sau đó F8tender100000,
Enterkhôngchốt, Confirm→No và POStrống. Chưa ghiPASSnative trước phản hồi.

Bằng chứng: runs/acceptance-20261026/{host-input-results.json,dialog-results.json,
unit-results/sales-rules.trx,source-frozen.csv,dll-hashes.json,progress.json,
before.json,before-manual.json,manual-cases.json,manifest.json}.
GiữDB chung đến27/10 vì testđangactive, cleanupmarker theo manifest; không dọn
trong khi user kiểm. Source/migration và quyềnSQL không đổi trong lượt hiện tại.

## Phản hồi native — cần làm rõ kiểu form con

User xác nhận nền không bấm được, nhưng nói cách hiển thị hiện tại không đúng ý
“form con”. Đây là phản hồi về cơ chế/kiểu hiển thị cần làm rõ, chưa tự coi là
lỗi giao dịch hoặc tự sửa source. Chưa có xác nhận đầy đủ nested cancel/input giữ,
không taskbar và checkout475; không suy PASS từ câu “không bấm được”.

Đã hỏi phân biệt: (1) owned Windows dialog như trước, titlebar/X, đi cùng app,
không taskbar riêng; (2) cửa sổ con kéo/resize trong vùng MainForm. Chờ lựa chọn
trước chỉnh requirements/plan hoặc sửa hiển thị. Source210file vẫn cố định.
Lệnh continue của user là tiếp tục xử lý phản hồi, không phải chấp nhận UI hiện tại.

## Quyết định kiểu hiển thị đã rõ

User chọn Form Windows như trước có titlebar/X, owner và không taskbar riêng.
Cách hiểu card trong client bị thay thế. Đã cập nhật AGENTS/requirements và viết
native-dialog-adjustment-plan.md để duyệt điều chỉnh trước sửa; không thay source
hoặc expected của lượt test host. Chưa xác nhận checkout475.
