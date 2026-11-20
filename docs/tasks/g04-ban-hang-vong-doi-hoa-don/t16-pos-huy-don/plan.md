# Plan T16 — POS, thanh toán và hủy đơn trên WinForms

Bổ sung mới30/10/2026: [Bán hàng gọn](sales-compact-ui-plan.md) đã được user duyệt
và triển khai; [bàn giao](sales-compact-ui-implementation.md),
[test chờ duyệt riêng](sales-compact-ui-test-plan.md).123bounds checks không
thay nghiệm thuT16/SQL/native/hình thức. Giữ lịch sử các bổ sung phía dưới.

Bổ sung Đơn đang mở25/10 đã duyệt/triển khai:
[plan](open-orders-ui-plan.md), [triển khai](open-orders-ui-implementation.md),
[test chờ duyệt](open-orders-ui-test-plan.md). Không thaySQL/role/checklist.

Bổ sung hỏi in/reset POS25/10 đã duyệt và triển khai:
[plan](post-checkout-print-plan.md), [triển khai](post-checkout-print-implementation.md).
Chờ duyệt [đợt test](post-checkout-print-test-plan.md); không nghiệm thu từ build.

Bổ sungUI25/10/2026 theo yêu cầu người dùng:
[CheckoutDialog/theme/control dùng chung](../ui-checkout-proposal/plan.md).
Đã duyệt và triển khai phần bổ sung này;chờ duyệt đợt test mới,không đổiSQL/BLL/role.

**Trạng thái:** Bộ plan G04 đã được duyệt ngày 23/10/2026; đã triển khai trong nhóm, đã chạy đợt test được duyệt; còn FAIL/BLOCKED, xem report.md. **Phụ thuộc:** T15. Áp dụng [plans.md](../plans.md), UX7–8/15–18 và guide Orbit hiện hành. Chưa code/chạy nghiệm thu.

## 1. Quyết định và contract hủy

Migration012 ALTER sp_HuyHoaDon: bắt buộc Actor/Stamp, MaHD, ExpectedRevision, RequestId; giữ tên và nghiệp vụ, bỏ entry point runtime chỉ nhận MaHD. Request fence/receipt → Order lock/header/version → kiểm đơn mở chưa có thanh toán/tiêu hao → UPDATE TrangThai=Huy, TrangThaiGiao=Huy giao khi là đơn giao → receipt/commit → trả DTO. Không cập nhật tồn/điểm. Chỉ xác nhận, không ô lý do. Nếu replay đúng request trả receipt và đơn thật; request mới với đơn đã trả/hủy bị từ chối. Dải523xx dành cho hủy/trạng thái không hợp lệ; lỗi xung đột dùng52103, quyền dùng51505/51605. Giao đã pha có hướng dẫn xuất hủy ở T21; không tạo phiếu giả hoặc hoàn kho.

T14/T15 phải bàn giao đủ read/write/preview/reconcile trước nối UI; không dùng data mẫu khi thiếu backend. SalesService bổ sung CancelAsync và cách đọc kết quả; mọi thao tác Sales đều Demand quyền và session. STORE không có entry/menu và direct service/SQL cũng bị chặn.

## 2. Bố cục và trạng thái UI

Tạo `Modules/Sales/SalesControl` có Designer chỉ bố cục; `ProductCardControl`, `OrderPanel` nhỏ theo guide khi control chung chưa có. Dùng MainForm hiện tại, không thêm cửa sổ POS độc lập. Vùng trái chọn loại/khu vực/bàn và Đơn mở; giữa tìm/danh mục/card món; phải dòng món và tổng SQL/khách/KM/ship/CTA. Panel bàn có thể thu vào bộ chọn; giữ footer tổng và nút Thanh toán truy cập được. Mỗi card có tên/giá/trạng thái, ảnh minh họa Resources khi dùng thì ghi đúng tính chất, không hứa ảnh thật của món. Tên dài tối đa2dòng ở card, nội dung đầy đủ qua editor/detail, không bắt hover để biết món.

Tại client≥1100logical dùng3vùng với vùng bàn gọn và đơn khoảng282–340logical theo không gian thực; 900–1099 thu bàn thành nút mở panel và2vùng món/đơn; 680–899 sidebar gọn, menu ít cột; <680 dùng tab Món/Đơn, tổng+CTA cố định. Không thêm banner marketing chiếm chiều cao luồng bán. Bố cục dùng Dock/Anchor/TableLayoutPanel/AutoScroll, không co font để lách DPI. Kích thước mục tiêu dialog theo plans.md, clamp WorkingArea và vùng nhập/grid cuộn, footer cố định; hỗ trợ thường/maximize/Snap.

Chọn bàn trống/chọn kênh chỉ tạo context local. Khi thêm món đầu: validation thông tin cần thiết → lấy MaHD tự động → lưu RequestId/payload trong ngữ cảnh → gửi tạo+món đầu. Nếu xung đột bàn thì tải đơn hiện có sau thông báo, không âm thầm áp món đã bấm vào đơn người khác. Rời khi chưa ghi hỏi nháp; đơn đã lưu giữ DB. Mở lại app vào Đơn mở để tải, không tự hủy/checkout.

Dialog sửa dòng chỉ lượng/ghi chú theo MaDong; không sửa giá. Chọn khách có tìm tên/SDT, tên+hạng/điểm, MaKH ẩn nội bộ; thêm khách dùng CustomerService G03, chọn ngay sau lưu, bỏ khách→NULL. Chọn KM theo NgayLap đơn (không lấy ngày hiện tại thay ngày lập), nhãn Không chọn chương trình vẫn cho hạng. Không dùng sp_PromotionList CASH mặc định ngày hôm nay nếu mở đơn cũ; lookup POS của T14 phải truyền ngày lập thật. Loại và bàn khóa sau tạo, hướng dẫn hủy/tạo lại khi cần; đơn giao còn Chờ giao sửa được người nhận/đối tác/ship. Đang ghi khóa các thao tác đổi đơn/đổi context.

Đang tải/rỗng/lỗi/xung đột/unknown/đã chốt là state riêng. Bàn/menu cũ khi lỗi có nhãn Chưa cập nhật và Thử lại; không giả bàn trống/tổng0. Sau ghi dùng DTO SQL và reload nguồn cần thiết. Sau checkout/hủy invalidate bàn/đơn mở; checkout còn invalidate khách/điểm/tồn qua cơ chế sự kiện cụ thể hoặc reload khi màn được hiện lại, không tự cộng/trừ cache, không tạo event bus tổng quát.

Mỗi query có cancellation và generation; callback kiểm disposed/session/generation. NavigationController dùng IUnsavedChanges cho nháp/IsWriting, khóa rời khi đang ghi hoặc đối chiếu. Logout/revoke/system-close ngắt hiển thị an toàn, nhưng không tuyên bố SQL rollback; sau đăng nhập/mở lại đọc đơn thật. Giữ quyền kiểm lại ở Business, không chỉ tin menu.

## 3. Dialog checkout và kết quả

CheckoutDialog mở chỉ sau preview T15 thành công đúng Revision. Phần đầu hiển thị mã/bàn/kênh/khách, tiền món, nguồn/tỷ lệ/giảm, ship/phải trả; giữa là DataGridView phân bổ gồm phương thức/số thu/tham chiếu, thêm/xóa dòng; cuối tổng đã phân bổ/thiếu/dư và phần tiền mặt đưa/thối. Dùng supported methods Checkout từ PaymentMethodRepository; default PT001 nếu còn được hỗ trợ, nếu không để chọn rõ, không tự dùng PT010 hoặc phương thức đã tắt.

C# chỉ tính tổng phân bổ/chênh lệch và thối trên phần tiền mặt để trợ giúp nhập, không tính lại tổng bill/giảm/điểm chính thức. Bill81.000 đưa100.000, allocation81.000; hỗn hợp cash30.000+transfer51.000 thì đưa100.000 thối70.000 trên phần cash. Nhiều dòng tiền mặt cộng thành phần cash; đưa thiếu cash hoặc amount không nguyên/overflow báo ErrorProvider/bảng, giữ input. Bill0₫ để TVP rỗng và nút Chốt hóa đơn0₫; ship>0 vẫn phải thu.

Không đặt AcceptButton tự checkout; Enter ở ô tiền chỉ commit edit, không gửi. F8 chỉ mở dialog, F2 tìm món/F3 khách/F4 đơn mở; modal/busy chặn shortcut toàn POS. Esc quay lại khi chưa gửi, hỏi nháp phân bổ khi cần; đang gửi không đóng bằng nút dialog. Một đường handler xác nhận: commit edit→validate→khóa gửi lặp trước await→gửi đúng token/RequestId→state kết quả. SQL từ chối giữ dialog/input và mở thao tác sửa đúng trạng thái. PreviewChanged hiển thị số cũ/mới, giữ phân bổ để tham khảo nhưng vô hiệu xác nhận đến khi người dùng duyệt preview mới và phân bổ đủ.

Timeout/cancel sau gửi→Unknown, khóa gửi/đóng đổi đơn và có Kiểm tra lại theo request fence. Chưa có bằng chứng kết thúc request cũ thì giữ Unknown; receipt vắng không là rollback. Đã trả→trang kết quả thật; còn mở sau phân xử→tải preview và yêu cầu xác nhận lại, không tự gửi. Mất app/restart tra MaHD qua Đơn mở/lịch sử; không tự thanh toán tiếp.

Kết quả thành công có mã/tiền thu/phương thức/điểm đã cộng từ DB, In/Xem chi tiết/Đơn tiếp theo. T16 chưa có T17 thì hiển thị rõ chức năng in/tra cứu sẽ nối ở T17, không nút làm việc giả. Đầu vào in nhận mã/DTO đã commit, không callback checkout. Nếu app đóng ngay sau commit vẫn đọc lại được trạng thái.

Bụi sao chỉ trên nút xác nhận cuối, sau validation hợp lệ và trước gọi BLL theo một đường handler; không đồng nghĩa thành công. Thêm `StarDustOverlay` cấp dialog/owned form nhỏ không activate/hit-test xuyên, một timer theo Stopwatch,40hạt/850–1250ms, budget280; dọn khi dispose/DPI/ẩn. Không tạo timer/control mỗi hạt. Không burst khi disabled/busy/invalid/đối chiếu. AppTheme tập trung token, không bật trên card/nút mở TT/hủy/lưu. Agent chỉ kiểm hành vi hit-test/lifecycle/gửi lặp, hình thức do người dùng kiểm.

## 4. Bước và file

| Bước | File/khu vực | Kiểm chứng kỹ thuật sau duyệt plan |
|---|---|---|
| 1. Hủy SQL | database/migrations/012_sales_cancel.sql; SalesRepository/SalesService; SqlErrorMapper | Apply sau011; smoke hủy1đơn mở và đọc view bàn, không đổi kho/điểm |
| 2. POS và dialog | WinForms/Modules/Sales/{SalesControl.cs,SalesControl.Designer.cs,OrderPanel.cs,ProductCardControl.cs,OrderLineDialog.cs,OrderHeaderDialog.cs,OpenOrdersDialog.cs,CustomerPickerDialog.cs,PromotionPickerDialog.cs,CheckoutDialog.cs,CheckoutResultControl.cs} | Binding DTO, pending/unknown/nháp, native control; không SQL hoặc logic nghiệp vụ trong Designer |
| 3. Theme/effect | WinForms/UI/StarDustOverlay.cs; Theme/AppTheme.cs; Resources/Orbit chỉ asset cần dùng+license | Token từ guide, không đổi theme màn khác; không chặn input, timer được dispose |
| 4. Điều hướng/dependencies | Program.cs, Forms/MainForm.cs, Navigation/NavigationController.cs (enum/map), ApplicationSessionContext khi thật cần | CASH vào Sales; ADMIN/STORE giữ điểm vào; permission Sales và module cache đúng lifecycle |
| 5. Fixture/test/docs | database/tests/t16/cancel-contract.sql; tests/CafeManager.Tests/Sales/CheckoutInputTests.cs; tools/ui/Test-G04Functions.ps1; docs/database/sales-contract.md; development-guide.md | Chuẩn bị ca chức năng, build Debug/Release; smoke native125% ghi riêng, không visual hoặc nghiệm thu trước duyệt |

Màn khách/menu/tồn G03 chỉ thêm điểm tích hợp cần cho refresh/selector; không sửa rộng editor hoặc theme đang có. Ghi technical-checks theo source/schema/build/lệnh. SQL/DAL/BLL/UI thiếu đầu ra phải báo blocker trước T17.

## 5. Ma trận nghiệm thu sau duyệt đợt test

| ID | Thao tác/data | Expected và bằng chứng |
|---|---|---|
| T16-K01 | Tạo/mở lại3kênh, khách/ghi chú; đóng/mở app, đổi khu vực; chọn bàn trống rồi rời | DB bền vững, chưa có món không tạo header; không tự hủy/thu, bàn có đơn tải đúng |
| T16-K02 | Thêm nhiều món, sửa/xóa đúng MaDong,2ghi chú khác; đổi khách/KM | Dòng/tổng/nguồn preview từ DB, không gộp sai hoặc dùng cache tính giảm |
| T16-K03 | Bill81.000/cash đưa100.000; hỗn hợp cash30.000+transfer51.000, đưa100.000 | Thu81.000/thối19.000; hỗn hợp thối70.000; SQL payment đúng allocations |
| T16-K04 | Tiền thiếu/ký tự/19chữ số/lẻ/âm, phương thức trống/tắt, đóng trước gửi | Không commit; giữ input/lỗi đúng trường; DB không ghi dở |
| T16-K05 | Double-click/Enter/F8 lặp, keyboard Space, đang gửi thì đóng/modal | Một request nghiệp vụ; handler/busy reset đúng; invalid không burst |
| T16-K06 | PhiênB checkout khi POSA đang mở hoặc sửa stale | POSA đọc trạng thái thực, khóa sửa/thu, không cộng tiền/kho/điểm lần2 |
| T16-K07 | Delay trước commit/commit rồi mất kết quả, DB mất khi reconcile; kill/reopen app | Unknown đúng, request fence và MaHD xác minh; không automatic retry, không gắn success khi chưa biết |
| T16-K08 | Hủy đơn mở có/rỗng món, hủy paid/stale, hủy giao mở | Mở→Huy, bàn trống, tồn không hoàn; paid/stale reject; lời hướng dẫn xuất hủy đúng phạm vi |
| T16-K09 | Đơn rỗng, thiếu kho/công thức, bill0₫ và còn ship | Rỗng không TT; lỗi rõ/giữ đơn mở;0₫ TVPrỗng;ship phân bổ đủ |
| T16-K10 | STORE/CASH/ADMIN, gọi service trực tiếp; menu ngừng/PT010/tắt support; revoke | Quyền BLL/SQL đúng, không lựa chọn trái phép hoặc bypass qua stale data |
| T16-K11 | Native125%, client nhỏ/thường/max/Snap,1280×720/1366×768, nhiều dòng/bàn, query chậm | Bounds/cuộn/focus/CTA truy cập được, UI responsive; hình thức SKIPPED,100/150%NOT RUN nếu chưa chạy |
| T16-K12 | Checkout xong quay lại bàn/khách/tồn, màn cache bị ẩn rồi hiện | Reload DB đúng, một lần tác động số dư; lỗi reload không giả dữ liệu0 |

UX-K05–K21/K39–K43 gắn từng nhánh trên; thêm logout/nháp/revoke/query muộn (UX-K03/K04), keyboard không chuột, tiếng Việt/IME, text dài,100đổi module/10login-logout theo nhóm. Bằng chứng chức năng lấy từ WinForms thật và DB, không ảnh/Designer hình thức. T17 sẽ nối In/Xem chi tiết và regression kết quả sau in lỗi.

## 6. Checklist gốc

**Kiểm thử bắt buộc:**

- [ ] `T16-K01` [M] Tạo/mở lại đơn tại quán, mang đi, giao hàng; thông tin khách/ghi chú còn sau đóng mở app.
- [ ] `T16-K02` [M] Thêm/sửa/xóa nhiều món; chọn đúng dòng và tổng/ưu đãi tự refresh từ DB.
- [ ] `T16-K03` [M] 81.000 VND, khách đưa 100.000: lưu phân bổ 81.000 và hiển thị thối 19.000.
- [ ] `T16-K04` [M] Nhập tiền không đủ/ký tự/quá lớn, bỏ phương thức, đóng dialog: không chốt hoặc ghi dở.
- [ ] `T16-K05` [M] Nhấn double-click/Enter nhiều lần chỉ gửi một request; thao tác xong/lỗi mở nút đúng.
- [ ] `T16-K06` [M/I] Cửa sổ khác chốt đơn: POS cũ tải lại và không tự sửa/thu thêm.
- [ ] `T16-K07` [M/I] Mất kết nối/timeout sau gửi: đọc lại MaHD, hiển thị đã chốt hoặc chưa rõ; không tự retry trả tiền.
- [ ] `T16-K08` [M] Hủy đơn mở trả bàn trống; hủy đơn đã trả bị từ chối và không hoàn kho.
- [ ] `T16-K09` [M] Đơn rỗng không TT; thiếu kho/công thức hiển thị lý do và giữ đơn mở để sửa.
- [ ] `T16-K10` [M] Không chọn PT010 hoặc menu ngừng bán; quyền STORE không mở thao tác bán trái phép.
- [ ] `T16-K11` [M] DPI/resize, tìm kiếm, nhiều dòng, bàn nhiều: không che nút hoặc khóa UI khi query.
- [ ] `T16-K12` [M/I] Bán xong refresh bàn/khách/tồn từ DB, không tự trừ/cộng lần nữa phía app.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.





