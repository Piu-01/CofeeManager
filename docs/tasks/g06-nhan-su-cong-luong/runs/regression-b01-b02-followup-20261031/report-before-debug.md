# G06 — Nghiệm thu còn lỗi và blocker native

31/10/2026. Đợt test được duyệt30/10/2026 và đã thực hiện với source cố định. **G06 Đang làm, chưa Hoàn tất:**25PASS,2FAIL,6BLOCKED trên33ID gốc. Hai bug chờ duyệt cùng đợt để sửa/regression. Không nâng DBdevelopment, không commit. [Báo cáo triển khai trước nghiệm thu](runs/extended-20261030/report-implementation-before-acceptance.md) giữ nguyên lịch sử.

| Task | Tổng ID | PASS | FAIL | BLOCKED | NOT RUN ID |
|---|---|---|---|---|---|
| [T24](t24-nhan-vien-tai-khoan/test-results.md) |10|5|2|3|0|
| [T25](t25-cham-cong/test-results.md) |11|10|0|1|0|
| [T26](t26-chot-chi-luong/test-results.md) |12|10|0|2|0|
| Tổng |33|25|2|6|0|

NOT RUN ID=0 nghĩa là mỗiID đã được bắt đầu; không có nghĩa mọi nhánh đã chạy. Các nhánh native chưa đủ được ghi BLOCKED tạiID tương ứng. Hình thức SKIPPED theo phân công người dùng; DPI100/150%NOT RUN. Không đổi Scale, không suy PASS từ build/component hoặc từ số subcase.

## Kết quả và bằng chứng

- .NETSDK10.0.401; SQL Server thật localhost. Các lời gọi ứng dụng dùng G06Runtime/CafeManagerApp qua EXECUTE AS USER, connection không pooling. Owner chỉ provision/bootstrap/fixture/đối chiếu. Baseline/seed một lần và migrations001–024, checksum/log nằm trong các thư mục run. Nguồn chưa commit; [SHA256 cuối](runs/extended-20261030/source-final.csv) khớp trước test, [audit](runs/extended-20261030/final-audit.json) SourceUnchanged=true. Không sửa source/migration/test fixture sản phẩm để xử lý FAIL.
- `tools/database/Test-G06.ps1 -RunAcceptance`:14/14 xUnit PASS;3SQLfixture subset qua; clean/demo001–024 thành công. [Manifest](runs/acceptance_20261030_155806_b10d6581/manifest.json). Runner không thay toàn bộ33ID.
- [Extended C#](runs/extended-20261030/results.json):59subcase,52PASS/7FAIL ban đầu. CRUD/unique/role/ADMINcuối, công/ngày/overlap, lương tính tay, snapshot, chi một lần và fault rollback đã thực hiện. Bốn race có barrier thật và2SQLwaiters trước nhả khóa: ADMIN tự hạ quyền, ca chồng, chốt cùngkỳ, chi cùngMaBL; mỗi race đúng1success. [Script/DB manifest](runs/extended-20261030/manifest.json), [mã harness](runs/extended-20261030/Program.cs).
- Bảy FAIL ban đầu của extended là kiểm transaction trong harness giả định `@@TRANCOUNT:XACT_STATE=0:0`, trong khi baseline cùng session thực tế là0:1, OpenTransactions0. Không ghi đè FAIL hoặc chỉnh expected cũ. [Diagnostic](runs/extended-20261030/transaction-diagnostic.json) và [followup độc lập](runs/extended-20261030/transaction-followup-v2.json) kiểm7lời gọi bị từ chối đúng mã53101/229/51605 và trạng thái trước/sau bằng nhau, không có transaction mở. Lượt followup đầu dùng pooling gặp596 được giữ riêng, không quy là lỗi sản phẩm.
- [Supplement](runs/extended-20261030/supplement/results.json):32subcase,31PASS/1FAIL thực ở tiền biên. Bổ sung mật khẩu8/128 và7/129, roleđanghoạtđộng, overlap chứa/mộtphần/quangày/mở và1giây, batchnhiềuNV, insert/update/delete kỳsnapshotmới,36giây làm tròn một lần, trigger bất biến, timeout trước/saucommit và replay. PendingDPAPI roundtrip, scopeDB, gửi lần2, process mới và corrupt đã chạy; file pending/lock/temp synthetic được dọn trong finally. Không coi đây là native unknown/restart nghiệm thu đầy đủ.
- SQL lương full-time10.800.000 và part-time1.600.000 khớp tính tay. SUMphiếu chỉ10.800.000 khi một bảngPaid và mộtUnpaid; lỗi sau ghiexpense trướcreceipt rollback; chi0không tạo phiếu. Snapshot giữ sau đổi hồ sơ; tiền/kho/điểm/hóađơn/nợ ngoàiusecase không đổi theo businesshash.
- Legacy [lượt đầu](runs/extended-20261030/legacy-results.json): tênrỗng,SDTsai,overlap,rerun/checksum PASS; fixture retiredactive FAIL do seedaccountđangkhóa, chưa tạo đúng tiềnđề. Giữ kết quả cũ. [Followup](runs/extended-20261030/legacy-followup-results.json) tạo rõ accountactive và chạy5case PASS: tên,phone,active-NVnghỉ,overlap,chi khác thực lĩnh. Migrations lỗi không có versionrow/cột mới; fixture bị sửa chỉ trong DBtest giữa ca chuẩn bị độc lập. Rerun024 AlreadyApplied, checksumfixture khác bị từ chối; không sửa migration gốc. Clean/demo hợp lệ001–024 đã chạy trong runner.

## Native125% và giới hạn

WinForms thật MainForm/modules đã chạy ởDeviceDpi120. Component probe trong runner bind các tab và kiểm owner/modal/nativeborder/AcceptButton=null, bounds kỹ thuật tại client1280×720/1366×768. Không dùng làm bằng chứng chuột/phím handler editor.

Sau lần đầu tool hết hạn chờ quyền, người dùng xác nhận đã cấp quyền; thử lại mở app thành công, dùng AuthService login thật với tài khoản fixture rồi mở Nhân sự/Thêm nhân viên. Đây không phải thao tác LoginForm native. Tool chỉ liệt kê rootMainForm; các index modal báo không available, input coordinate/key/type trên rootdisabled không đổi ôtên. Cả fallback ComputerUse/cua_repl đã thử, SQL đối chiếu0nhân viên tênNative G06 Test. [Trace accessibility](runs/extended-20261030/native-trace.json), [audit](runs/extended-20261030/final-audit.json). Không kết luận bug sản phẩm từ giới hạn này.

Vì vậy native create/edit/reset, select-allclick/Tab/clicklần2, validation/dirtyclose/Esc/Enter, busy/unknown, filter/pager/reload, sessionchange/latecallback/dispose, linklương↔chi/dialoglồng còn BLOCKED/NOTRUN ở tầng native. CácID chứa yêu cầu manual tương ứng chưa PASS. Không nghiệm thu thị giác, hover/spacing hoặc Designer; screenshot chỉ phục vụ định vị click, không lưu ảnh đối chiếu hay báo PASS hình thức.

## Hai bug trong đợt đề nghị

[G06-B01/B02 chi tiết và hướng sửa](t24-nhan-vien-tai-khoan/bug-report.md), [ảnh hưởngT26](t26-chot-chi-luong/bug-report.md):

1. G06-B01: tiền hợp lệ lớn bị SqlClient overflow trước SQL do helperMoneyScale18/CLRdecimal. Ca validmax FAIL, đối chứng scale0/literalSQL đúng; stack có trong [diagnostic](runs/extended-20261030/diagnostic/results.json). Đề nghị sửa parameter giữ chính xác tiền/giờ, không hạ giới hạn hoặc làm tròn ngầm; regression cả employee và payroll.
2. G06-B02: grid dùng `N0' ₫'` sinh literalN, ví dụ `N12000000 ₫`; expected `12.000.000 ₫`. AXappthật và formatter vi-VN xác nhận. Đề nghị sửa formattext nhân viên/payroll, không thay bố cục/theme.

Bug được ghi tại task sở hữu, chưa vá. HaiIDFAIL gồm ca lưu/hiển thị lươngT24-K01 và ca biên mởrộngT24-K02; không phải haiID tương ứng một-một với hai bug.

## Dọn tài nguyên và bước tiếp theo

Đợt nghiệm thu tạo6DBtest (runner2,extended1,native/supplement1,legacy2), dọn6/còn0; manifest từng lượt giữ exactname/marker. Harness native25908 đã dừng đúng fullpath trước khi dọn DB; [native manifest](runs/extended-20261030/native-manifest.json). Audit không còn DBprefixG06. Lần cleanup đầu bị guard chặn vì so case-sensitive đường dẫnWindows; sau kiểm lại fullpath, dùng so không phân biệt hoa/thường và dọn đúng mục tiêu. Không DROPwildcard, không chạm development/processứngdụng khác.

Chờ người dùng duyệt cả đợt **G06-B01 và G06-B02** theo skillbug-investigation-workflow, rồi sửa lần lượt và regression theo ảnh hưởng; không xin duyệt riêng từngfix trong đợt. Native còn thiếu sẽ tiếp tục khi tool điều khiển được modal hoặc được người dùng phân công manual; không đánh dấu Hoàn tất trước khi các ca bắt buộc có bằng chứng PASS. Các ca planned mởrộng chưa thực hiện hết (nativeI/O lỗiquyềnfile/pendingactor khác, latequery/sessionUI) giữ NOTRUN, không suy từ các subcase đã qua.
