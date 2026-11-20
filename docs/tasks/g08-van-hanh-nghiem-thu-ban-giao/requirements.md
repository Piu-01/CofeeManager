# G08 — Vận hành, nghiệm thu và bàn giao: requirements

Ngày soạn: 02/11/2026. Trạng thái: **Requirements nhóm đã được người dùng duyệt bằng “duyệt” ngày02/11/2026**. Đã soạn đủ bộ plan T30–T32 tại [plans.md](plans.md), cả bộ đã được duyệt ngày02/11/2026; đã viết đầu ra triển khai, đợt test G08 đã được duyệt và chạy:16PASS/0FAIL/13BLOCKED; G08-B01 đã sửa/regression PASS, xem report.md.

## 1. Mục tiêu và căn cứ

Hoàn thiện khả năng cài sạch, sao lưu/khôi phục, xác nhận các phân hệ hoạt động xuyên suốt và tạo bản Windows có thể bàn giao ngoài máy phát triển. Giữ kiến trúc C# WinForms .NET 10, SQL Server, một quán và một kho chung.

Căn cứ: [kế hoạch mục 11–13 và T30–T32](../../KeHoach_QuanLiQuanCafe.md), [UX01/UX20 và UX-K44](../../UX_QuanLiQuanCafe.md), [AGENTS.md](../../../AGENTS.md), [báo cáo G07](../g07-tai-san-bao-cao-hieu-nang-du-lieu/report.md), [kết quả gỡ đăng nhập TEST](../temporary-test-login/removal-results.md). Mục 6 giữ nguyên phạm vi, đầu ra, 29 ID nghiệm thu T30–T32 và điều kiện bổ sung chốt G08.

Yêu cầu “triển khai g08” và “continue” được ghi nhận là bắt đầu quy trình nhóm. G07 có source/migrations001–027 và kết quả24PASS/0FAIL/9BLOCKED; chưa Hoàn tất. Chuyển sang chuẩn bị G08 không nghiệm thu thay G07 hoặc thay đổi lịch sử. Kiểm tra đầu ra T29 trước T30; nếu thiếu đầu ra thực sự chặn triển khai thì báo cụ thể để người dùng quyết định. T31 phải tổng hợp đầy đủ ca còn thiếu của các nhóm, gồm9ID native của G07; không suy PASS từ build/probe hoặc SQL regression.

## 2. Task con và trình tự

| Thứ tự | Task | Đầu ra chính | Phụ thuộc |
|---|---|---|---|
| 1 | T30 — Backup, restore và cấu hình cài đặt sạch | Hướng dẫn/script vận hành có kiểm soát, cài sạch baseline+migrations+dữ liệu nền, fixture demo tách biệt | T29 |
| 2 | T31 — Nghiệm thu xuyên suốt và regression cuối | Ma trận325ID kế thừa lịch sử, kịch bản E2E, đối chiếu nguồn, biên bản thực và hồ sơ bug | T30 |
| 3 | T32 — Publish và bàn giao hoàn chỉnh | Package Windows, source/schema/migrations, hướng dẫn/demo, manifest phiên bản và bằng chứng bản cuối hết bypass | T31 |

Một task con tại một thời điểm. Sau duyệt requirements viết đủ ba plan và chỉ mục plans.md, chờ duyệt cả bộ một lần rồi triển khai tuần tự. T31 chuẩn bị harness/fixture/kịch bản trong pha triển khai; việc thực thi nghiệm thu, kể cả kiểm bản publish, đợt test G08 đã được duyệt và chạy:16PASS/0FAIL/13BLOCKED; G08-B01 đã sửa/regression PASS, xem report.md. Không coi kế hoạch test là kết quả test.

## 3. Phạm vi và hành vi

### T30 — Vận hành và cài sạch

- Backup bằng cơ chế SQL Server; restore sang DB test riêng có tên/marker xác định. Không copy MDF/LDF đang mở, không ghi đè DB sử dụng thật. Phân biệt đường dẫn SQL Server với đường dẫn máy client; xử lý quyền SQL service account và hướng dẫn khi thiếu quyền.
- Ghi snapshot đối chiếu trước/sau restore: tiền, kho, điểm, nợ, lương, trạng thái và liên kết nguồn. Kiểm version/checksum migration, chạy nâng cấp đúng thứ tự và bảo toàn dữ liệu; không chạy seed lần hai.
- Hai đường cài đặt tách biệt: cài sạch không seed demo nhưng có dữ liệu hệ thống và bootstrap ADMIN một lần; DB demo dùng fixture công khai tương thích schema cuối. Không tái mở bootstrap hoặc thêm recovery ngoài contract đã duyệt.
- Runtime dùng quyền tối thiểu, tách tài khoản schema/migration; không để credential/local config trong source, evidence hoặc package. Cấu hình thiếu/sai/chứng chỉ sai phải báo dễ hiểu, không fallback DB ngầm. UX20 không trở thành công cụ quản trị SQL tổng quát; restore là quy trình quản trị đóng phiên app, xác nhận đích và kiểm tra lại.

### T31 — Kiểm chứng xuyên suốt

- Lập ma trận đủ325ID T01–T32, truy vết từng biên bản và subcase, giữ kết quả lịch sử. Kế thừa bằng chứng phù hợp phiên bản; ghi rõ ca cần chạy lại theo ảnh hưởng hoặc còn thiếu. Không chạy lại chỉ vì đổi cấu trúc hồ sơ.
- Chuẩn bị5luồng E2E bán hàng, nhập/nợ, xuất/kiểm kê, công/lương và giao hàng; đối chiếu trực tiếp nguồn SQL và liên kết. Thử lỗi/mất kết nối trước/sau commit, kết quả chưa biết, chống gửi lặp và đối chiếu trước retry; không tự retry giao dịch tài chính.
- Kiểm đăng nhập/đăng xuất/thu hồi phiên/quyền ADMIN-CASH-STORE; WinForms thật cho binding, validation, focus/Tab/Enter/Esc, loading, modal owner, query trả muộn, nháp và pending. Không dùng probe thay toàn bộ thao tác native.
- Hình thức do người dùng tự kiểm: SKIPPED theo phân công. Agent chỉ kiểm chức năng/layout kỹ thuật ở125%, không đổi Windows Scale;100/150% chưa chạy ghi NOT RUN. Giữ nguyên checklist gốc.
- Bug ghi tại task sở hữu, liên kết báo cáo G08 và duyệt cả đợt trước sửa; không tự vá trong lượt test hoặc sửa expected để né lỗi. Duyệt debug gồm regression trong phạm vi bug đã duyệt.

### T32 — Bản bàn giao

- Chuẩn bị publish Windows đúng kiến trúc đích, manifest hash/phiên bản/schema và package không chứa bí mật, dữ liệu nhạy cảm hoặc đường dẫn máy tác giả. Giả định ban đầu win-x64 framework-dependent theo môi trường hiện có; bộ plan xác minh và ghi rõ Desktop Runtime cần cài. Nếu chọn self-contained phải kiểm chạy thực tế.
- Cung cấp hướng dẫn baseline→migrations→dữ liệu nền hoặc demo, cấu hình/bootstrap, backup/restore, chạy không cần Visual Studio và kịch bản demo xuyên nguồn. Publish không đóng gói SQL Server.
- Kiểm smoke trên đúng package và thư mục khác, với DB cài sạch/demo/restore phù hợp: đăng nhập, bán/checkout/in, nhập/nợ, công/lương và report. Không coi F5 hoặc build thành công là publish PASS.
- Tiện ích bỏ qua đăng nhập đã gỡ sớm24/10; giữ bằng chứng lịch sử và kiểm lại source/hooks/launcher/config cùng DLL Debug/Release/publish cuối, kể cả cờ test cũ còn trong môi trường. Regression đăng nhập thường/sai mật khẩu/logout/khóa/thu hồi phiên/quyền trên bản cuối. Chưa có bằng chứng kiểm lại thì G08 chưa PASS.

## 4. Giả định và giới hạn

- Bộ plan sẽ đọc baseline, seed, migrations và công cụ hiện có trước chốt file/contract; tái sử dụng tối đa. Không sửa migration cũ đã có checksum hoặc tự thêm migration khi chưa xác định nhu cầu.
- Không tự nâng DB development, restore lên DB thật, bật Mixed Mode, đổi cấu hình SQL/Windows toàn máy, xóa tài nguyên không thuộc lượt chạy hoặc commit/push. Backup nguồn đang dùng chỉ đọc dữ liệu nguồn; đích restore là DB test mới. Nếu thao tác cần quyền hoặc đường dẫn chưa có, ghi blocker và đề xuất cụ thể.
- Tài nguyên test có manifest/marker/allowlist; cleanup xác minh tên chính xác, không DROP wildcard. Backup/evidence không đưa dữ liệu nhạy cảm vào repo/package. Báo tài nguyên tạo/dọn/giữ.
- Không thêm hoàn tiền, đổi điểm, API, offline, nhiều chi nhánh, kế toán giá vốn/lợi nhuận chính xác hoặc công cụ recovery mới. Thay nghiệp vụ/contract/phạm vi cần duyệt bổ sung.
- Không tự sửa9ca BLOCKED G07 thành PASS. Nếu phiên native vẫn không thao tác được, giữ bằng chứng blocker; phần kiểm chứng đó phải có kết quả thật trước chốt nghiệm thu.

## 5. Tiêu chí hoàn tất và lưu bằng chứng

1. Duyệt requirements nhóm → viết đủ plan T30/T31/T32 → duyệt cả bộ → triển khai tuần tự → duyệt đợt test riêng. Mỗi lần kiểm kỹ thuật trong triển khai lưu riêng, không tick ca nghiệm thu.
2. Kiểm giao dịch bằng SQL Server thật trên DB test riêng; hai session cho ca đồng thời. Unit xUnit chỉ cho quy tắc có giá trị; UI chức năng trên WinForms thật. Mỗi lượt test giữ source/migration cố định.
3. test-results.md từng task ghi mã ca/subcase, môi trường, schema/commit và hash workingtree, dữ liệu, lệnh/thao tác, expected/actual, PASS/FAIL/BLOCKED/NOT RUN và evidence; report.md tổng hợp29ID G08 và ma trận bao phủ325ID. Không bịa log hoặc đánh dấu ca chưa chạy.
4. Chỉ Hoàn tất khi đủ đầu ra, các ca bắt buộc PASS theo phân công và không còn lỗi trọng yếu; thiếu bằng chứng giữ Đang làm. T32-K08 công khai trạng thái thật của từng nhóm, không dùng tài liệu/package để che ca chưa kiểm.
5. Điều kiện gỡ bypass là gate bổ sung bắt buộc, giữ nguyên29ID G08/325ID toàn dự án. Lưu phiên bản được kiểm; commit chỉ khi người dùng giao riêng.

## 6. Phạm vi, đầu ra và ca nghiệm thu gốc

Sao nguyên bản phần T30–T32 và điều kiện chốt G08 từ kế hoạch. Toàn bộ29ID dưới đây chưa chạy trong G08.
### T30 — Backup, restore và cấu hình cài đặt sạch

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Bảo đảm dữ liệu và cấu hình có thể khôi phục trước nghiệm thu cuối. |
| Phạm vi | Backup SQL, restore DB test, cài app/DB không seed và bootstrap; quyền tài khoản DB; chưa bàn giao publish cuối. |
| Quy mô | M — vận hành |
| Phụ thuộc | T29 |
| Đầu ra | README backup/restore, baseline+migrations cài mới và fixture demo tách dữ liệu thật. |

**Kiểm thử bắt buộc:**

- [ ] `T30-K01` [M/I] Backup DB đang dùng bằng công cụ SQL, restore sang DB test khác thành công.
- [ ] `T30-K02` [I] Sau restore đối chiếu tiền/kho/điểm/nợ/lương và trạng thái chứng từ với bản backup.
- [ ] `T30-K03` [M] SQL service account ghi được thư mục backup; đường dẫn không nhầm filesystem client/server.
- [ ] `T30-K04` [M/I] Cài database sạch không seed demo nhưng đủ role/danh mục hệ thống; bootstrap admin login được.
- [ ] `T30-K05` [I] Chạy migration trên DB restore không xóa/nhân đôi tồn/điểm/chi; version lưu đúng.
- [ ] `T30-K06` [M] Runtime app dùng quyền tối thiểu; schema/migration dùng tài khoản riêng.
- [ ] `T30-K07` [M] App báo lỗi khi cấu hình thiếu/sai/chứng chỉ không hợp lệ; không fallback ngầm sang DB khác.
- [ ] `T30-K08` [M] Không copy MDF/LDF đang mở làm backup; tài liệu nêu đúng cách người dùng thực hiện.
- [ ] `T30-K09` [M] Cấu hình local/bí mật không xuất hiện trong source/README/package demo công khai.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

### T31 — Nghiệm thu xuyên suốt và regression cuối

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Xác nhận các phân hệ phối hợp đúng khi chạy như một app duy nhất. |
| Phạm vi | Các test còn lại của T01–T30, kịch bản E2E, mất kết nối, role và UX; không thêm tính năng ngoài scope. |
| Quy mô | L — tích hợp toàn phạm vi |
| Phụ thuộc | T30 |
| Đầu ra | TestCases có kết quả thật, lỗi trọng yếu đã sửa và regression liên quan PASS. |

**Kiểm thử bắt buộc:**

- [ ] `T31-K01` [M/I] E2E 1: login → bàn → hai dòng cùng món khác ghi chú → ưu đãi → TT hai phương thức → in → bàn trống, tồn/điểm khớp.
- [ ] `T31-K02` [M/I] E2E 2: nhập trả một phần → tồn tăng → nợ → hai lần trả → còn nợ/phiếu chi/báo cáo khớp.
- [ ] `T31-K03` [M/I] E2E 3: xuất hủy → lấy kiểm kê → phát sinh kho làm snapshot lệch → từ chối → lấy lại/chốt → không điều chỉnh lần hai.
- [ ] `T31-K04` [M/I] E2E 4: ca qua đêm → chốt lương → chi lương → không chốt/chi trùng → báo cáo đúng.
- [ ] `T31-K05` [M/I] E2E 5: đơn giao → TT → chuyển giao → kiểm tra tài chính không đổi và lịch sử xem được.
- [ ] `T31-K06` [M/I] E2E thất bại: dừng kết nối đúng các bước nhiều bảng; đối chiếu toàn bộ nguồn, không dữ liệu dở hoặc retry thu tiền.
- [ ] `T31-K07` [M/I] Session cũ sau logout/nghỉ việc/khóa tài khoản không thao tác trái chính sách; kiểm tra CASH/STORE/ADMIN.
- [ ] `T31-K08` [M] Resize/DPI, tiếng Việt, nhiều món, focus/tab/Enter, loading, dialog hủy và truy vấn trả muộn đều đúng.
- [ ] `T31-K09` [I] Đối chiếu trực tiếp tiền, tồn, điểm, nợ, lương, trạng thái chốt và liên kết nguồn sau E2E; không chỉ xem UI đúng.
- [ ] `T31-K10` [M/I] Mọi checklist task liên quan đã chạy/PASS; ca không áp dụng phải có lý do scope, không tick tùy ý.
- [ ] `T31-K11` [M] Không còn lỗi mất dữ liệu, sai tiền/kho/điểm/nợ/lương, bypass quyền hoặc crash chặn luồng chính.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

### T32 — Publish và bàn giao hoàn chỉnh

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Tạo bản có thể chạy ngoài máy phát triển và đủ tài liệu để demo/bảo trì. |
| Phạm vi | Publish Windows, dependency SQL/Runtime, source sạch, hướng dẫn, demo và kết quả test; không coi publish .exe là đóng gói SQL Server. |
| Quy mô | M — sản phẩm bàn giao |
| Phụ thuộc | T31 |
| Đầu ra | Bản publish, source, schema+migrations, hướng dẫn và kịch bản demo chính thức. |

**Kiểm thử bắt buộc:**

- [ ] `T32-K01` [M] Publish Release cho kiến trúc máy đích; chạy ở thư mục/máy khác không cần Visual Studio.
- [ ] `T32-K02` [M] Framework-dependent yêu cầu đúng Desktop Runtime; self-contained nếu chọn được thử thực tế, không chỉ build.
- [ ] `T32-K03` [M/I] Cấu hình kết nối DB demo sạch và bootstrap theo README, app login/bán/in hoạt động.
- [ ] `T32-K04` [M] Package không có credential thật, DB test có dữ liệu nhạy cảm hoặc đường dẫn tuyệt đối máy tác giả.
- [ ] `T32-K05` [M/I] Thử smoke test bán/checkout/nhập/nợ/chấm công/lương/report trên bản publish, không chỉ bản Debug.
- [ ] `T32-K06` [M] Restore backup và chạy publish cùng dữ liệu khôi phục, không cần sửa source.
- [ ] `T32-K07` [M] README ghi thứ tự baseline→migration→dữ liệu nền hoặc seed demo; tên/version SQL và test đúng bản bàn giao.
- [ ] `T32-K08` [M] Source có task status/test results, các migration đề xuất đã thực hiện được đánh dấu; phần chưa làm công khai rõ.
- [ ] `T32-K09` [M] Kịch bản demo đi trọn các nguồn; báo cáo không quảng cáo hoàn tiền/lợi nhuận/API khi chưa có.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

### Điều kiện chốt G08 — gỡ đăng nhập test ở cuối T32

**Bổ sung theo yêu cầu người dùng ngày 22/10/2026:** thực hiện ở cuối G08, tại bước
dọn bản bàn giao T32 và trước khi chốt PASS G08. Đây là điều kiện bàn giao bổ sung,
không thay thứ tự/phụ thuộc T01–T32 hoặc325ID kiểm thử gốc.

- [ ] Gỡ hoàn toàn nút “Bỏ qua đăng nhập — TEST”, helper tạo phiên không xác thực,
  hook trong Program/ApplicationSessionContext/LoginForm và launcher bật bypass.
  Không chỉ ẩn nút hoặc dựa vào `#if DEBUG`; source bàn giao phải hết đường bypass.
- [ ] Gỡ cờ/cấu hình/hướng dẫn bật bypass khỏi cấu hình chạy và tài liệu vận hành;
  giữ hồ sơ lịch sử kiểm thử. Danh sách gỡ chi tiết:
  `docs/tasks/temporary-test-login/README.md`.
- [ ] Build Debug/Release và kiểm bản publish cuối: không có nút/helper/đường vào
  bỏ qua đăng nhập, kể cả khi còn biến môi trường test từ lần chạy trước.
- [ ] Regression đăng nhập thường, sai mật khẩu, logout, khóa/thu hồi phiên và quyền;
  lưu bằng chứng đã gỡ và kết quả kiểm tra trong hồ sơ T32/báo cáo G08.

**G08 không được PASS nếu chưa gỡ tiện ích hoặc chưa có bằng chứng kiểm lại sau gỡ**,
kể cả các ca nghiệm thu khác đã đạt. Theo yêu cầu người dùng ngày24/10/2026,
đã gỡ sớm tiện ích khỏi source/hooks/profile/launcher, xem
`docs/tasks/temporary-test-login/removal-results.md`. Vẫn giữ checklist chốt bản
bàn giao cuối ở T32; không tự đánh dấu G08 hoàn tất từ đợt gỡ sớm này.







