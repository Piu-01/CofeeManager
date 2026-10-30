Trạng thái24/10/2026: hai vấn đề bổ sung đã xử lý theo duyệt, schema016;
35PASS/0FAIL/21BLOCKED trên56ID; UI05 đã sửa và handler Confirm thật được người dùng/SQL xác nhận; xem report.md.

# G04 — Bán hàng và vòng đời hóa đơn

## Bổ sung đề xuất30/10/2026 — Bán hàng gọn

User gửi Orbit_BanHang_Gon.zip yêu cầu chỉnh trang Bán hàng. Phạm vi/plan tại
[T16 POS gọn](t16-pos-huy-don/sales-compact-ui-plan.md): mở rộng menu/card ngang,
cột đơn gọn, tách actions/header và giữ tổng Phải thanh toán, breakdown ở F8.
Tái dùng theme/control, giữ native dialog và nghiệp vụ tạo/sửa/chốt/in/reset.
ZIP checkout đi kèm là tham chiếu, không tự mở rộng redesign hộp thanh toán.
Chờ duyệt bổ sung phạm vi/plan trước code; không thay checklist/SQL/role.

## Bổ sung đã duyệt26/10/2026 — đồng bộ UI Giao hàng

Theo yêu cầu người dùng lập kế hoạch chỉnh màn Giao hàng theo chuẩn danh sách
chung mới: search hết hàng + kính lúp/làm mới, filter ở hàng dưới tự áp dụng,
bảng giãn phần nội dung còn lại, profile giao hàng và pager tổng thật từ DTO.
Giữ chuyển bước/reconcile/quyền/SQL, dialog chi tiết native; không thêm ngày lọc,
timeline hoặc hoàn tiền. [Plan đầy đủ](t18-giao-hang/ui-delivery-plan.md) đã được
user duyệt bằng “duyệt”, đã triển khai. Đợt test riêng được duyệt/chạy:55/55giao,
108/108native/shared,53/53invoice,18/18xUnit đạt; source cố định, fixtures dọn,
tiền/kho/điểm bất biến. Còn manual/hình thức và giới hạn tại
[kết quả](t18-giao-hang/ui-delivery-test-results.md), không tự chốtT18/G04.

## Bổ sung đã duyệt26/10/2026 — giao diện Lịch sử hóa đơn OrbitV4

User gửi Orbit_LichSuHoaDon_V4.zip yêu cầu code lại trang này. Phạm vi đề xuất
chi tiết tại [proposal](ui-invoice-history-v4/proposal.md): toolbar tìm/áp dụng
bằng kínhlúp/làm mới, cụm ngày/trạng thái/kênh, Chi tiết/In cănphải, bảngbadge/
mã và ngày đầy đủ, bỏ summary phía trên, giữpager/phạmvi phía dưới. Tái dùng
theme/control chung, thay opt-in cho riêng màn này; giữ contractT17/role/SQL và
native Windows dialog cùng helperchọnhết input đã chốt. Người dùng đã duyệt phạm
vi và [plan](ui-invoice-history-v4/plan.md), gồm kính lúp/Enter chỉ submit từ khóa;
bộ lọc tự áp dụng bằng từ khóa đã tìm gần nhất. Đã triển khai, build Debug/Release
sạch; đợt test riêng đã duyệt và chạy53/53 functional,94/94 regression,18/18
xUnit đạt. Còn xác nhận manual/hình thức và giới hạn subcase tại
[kết quả](ui-invoice-history-v4/test-results.md), chưa chốt toàn bộ T17/G04.

Bổ sung Đơn đang mở25/10 đã duyệt/triển khai/test component+SQL98checksPASS; native/hìnhthức chờ người dùng: bám ZIP mới, mã hóa đơn hiện đầy đủ,
sharedtheme/control; [phạm vi/plan](t16-pos-huy-don/open-orders-ui-plan.md).

**Bổ sung luồng sau thanh toán25/10, đã duyệt/triển khai; chờ duyệt test:** hỏi Có/Không in, kết thúc bước in
hoặc bỏ qua đều về nháp đơn mới; gỡ Xem/In khỏi POS, giữ in lại tại Lịch sử.
Phạm vi/tiêu chí/plan/test tại [bổ sung T16/T17](t16-pos-huy-don/post-checkout-print-plan.md).

**Bổ sung yêu cầuUI25/10/2026:** người dùng cung cấpOrbit_XacNhanThanhToan.zip,
yêu cầu chỉnhCheckoutDialog,ưu tiên theme/control hiện có và dùng chung. Giữ
nghiệp vụ/role/SQLvà56ID. Phạm vi,bố cục,mapping vàplanbổsung tại
[UI checkout](ui-checkout-proposal/plan.md);đã được duyệt25/10,đã triển khai;
đã duyệt và regression UI03; còn ca POS chưa kiểm, không suy PASS từ code/build.

**Ngày bắt đầu:** 23/10/2026.  
**Trạng thái:** Đang làm — requirements đã được người dùng duyệt ngày 23/10/2026 bằng phản hồi “duyệt”; bộ plan đã duyệt qua “diuyeetj” và đã triển khai T14–T18; đã chạy đợt test được duyệt; còn FAIL/BLOCKED bộ kiểm thử, xem report.md.  
**Yêu cầu:** “BẮT ĐẦU G04”.  
**Nguồn chuẩn:** [Kế hoạch](../../KeHoach_QuanLiQuanCafe.md), phiên bản 3.3, mục 6, 10–13 và T14–T18; [UX](../../UX_QuanLiQuanCafe.md), mục 2–5, 7–9, 15–18; AGENTS.md hiện hành.  
**Mốc source:** HEAD `3b87f9915cef2608eb1f6f8d9250b9c10db8fb55`; working tree có nhiều thay đổi có sẵn, không coi HEAD là toàn bộ source hiện tại. Repo root đã xác nhận là CoffeeManager.  
**Mốc schema:** thư mục migrations hiện có 001–009; hồ sơ development G03 ghi schema009. Chưa truy vấn DB thực tế trong lượt khởi động requirements này.

## 1. Mục tiêu, phụ thuộc và thứ tự

Hoàn thiện luồng bán hàng cho một quán, một kho chung: tạo và mở lại đơn tại quán/mang đi/giao hàng, sửa món, áp dụng khách/ưu đãi, thanh toán nguyên tử, hủy đơn mở, xem/in lại và chuyển tiến độ giao. Cuối T17 có mốc demo bán hàng; cuối G04 bổ sung giao hàng, chưa phải bản vận hành đủ mọi phân hệ.

Một task con tại một thời điểm: **T14 → T15 → T16 → T17 → T18**, đầu vào T13. G03 đã PASS theo quyết định người dùng ngày 20/10/2026 với ngoại lệ kiểm thử tại [acceptance-decision.md](../g03-du-lieu-phuc-vu-ban-hang/acceptance-decision.md). Giữ kết quả lịch sử; không suy 47/47 ca đã chạy PASS. Tích hợp POS/checkout đích chưa chạy và thuộc trách nhiệm G04.

## 2. Phạm vi và hành vi cần đạt

| Task | Phần thực hiện | Đầu ra chính |
|---|---|---|
| T14 | Tạo đơn, header/dòng món, khách/KM và preview; kiểm loại đơn, snapshot giá, đồng thời và đối chiếu sau timeout | Migration thao tác đơn, SalesRepository/Service, request/DTO typed |
| T15 | Hoàn thiện checkout SQL, TVP phân bổ, snapshot ưu đãi/tiêu hao/phí, tiền/kho/điểm và bảo vệ giao dịch | Migration checkout, contract preview/kết quả/lỗi, seed tương thích |
| T16 | Nối bàn/menu/khách/ưu đãi thành POS ba kênh; checkout, hủy đơn mở, phục hồi kết quả chưa rõ | POS và dialog thanh toán/kết quả; CASH vào POS sau login |
| T17 | Tra cứu theo bộ lọc, chi tiết snapshot, PrintDocument/PrintPreview và in lại | Lịch sử/chi tiết chỉ đọc sau chốt, preview/in nhiều trang |
| T18 | Danh sách đơn giao, người nhận/đối tác, chuyển bước đúng trình tự và phát hiện xung đột | Module giao hàng và procedure chuyển trạng thái |

### Đơn mở và POS

- Chọn bàn trống chỉ chọn ngữ cảnh; thêm món đầu mới tạo đơn. T14 phải chốt contract tạo header và dòng đầu nguyên tử hoặc cơ chế tương đương không để bàn bị chiếm giả khi lỗi/rời. Chọn bàn có đơn phải đọc lại và mở đúng đơn thực tế, không tin màu/cache.
- Tại quán bắt buộc bàn, không đối tác; mang đi không bàn/đối tác; giao hàng không bàn, đủ người nhận/SDT/địa chỉ/trạng thái, đối tác NULL khi tự giao. Nhân viên từ session, khách vãng lai là MaKH NULL.
- Thêm món dùng giá DB hợp lệ; sửa số lượng/ghi chú theo MaDong và đúng MaHD, giữ giá snapshot. Dòng khác ghi chú/giá giữ riêng; không cho thu ngân sửa giá tùy ý. Món ngừng bán/thiếu công thức không được thêm.
- Lựa chọn khách/KM phải được lưu đúng contract trước khi UI cho áp dụng. Không chọn KM vẫn áp dụng hạng nếu có khách; KM thay hạng, không cộng dồn. Tổng/nguồn giảm/tỷ lệ lấy SQL; preview đang tải/lỗi thì chặn thanh toán.
- Rời POS hoặc đóng/mở app không tự hủy đơn đã lưu. Nháp chưa gửi phải có xử lý khi rời; xung đột giữ input tham khảo, yêu cầu tải lại, không ghi đè tự động. Xóa hết món giữ đơn rỗng mở hợp lệ, có thể hủy và không checkout.

### Checkout, hủy và bất biến

- SQL là nguồn tính chính thức: tiền món theo snapshot dòng; giảm ROUND(tiền món × tỷ lệ / 100, 0), không giảm ship; phải trả = tiền món + ship − giảm. Phí đối tác theo tiền món sau giảm và tỷ lệ lúc chốt; điểm FLOOR(tiền món sau giảm / 1000), không cộng ship/phí.
- Preview thay đổi khi xác nhận phải yêu cầu xác nhận lại số mới. Checkout khóa đơn, kiểm preview/phân bổ/menu/công thức/tồn, snapshot tiêu hao/phí/ưu đãi, ghi thanh toán/chốt/điểm trong một transaction SQL; trigger tác động số dư đúng một lần. Không tạo phiếu Thu thứ hai cho checkout.
- Hỗ trợ nhiều phương thức và nhiều dòng cùng phương thức; loại PT010. Bill 0 ₫ và ship0 dùng TVP rỗng, vẫn tiêu hao/chốt, không tạo dòng tiền0. Bill còn ship phải phân bổ đủ.
- Bill81.000, tiền mặt đưa100.000 lưu phân bổ81.000 và hiển thị thối19.000; thanh toán hỗn hợp tính đưa/thối trên phần tiền mặt. Chưa lưu đưa/thối thì không dựng lại số trên bản in lại.
- Chặn gửi lặp; hai session chỉ một commit, hai đơn tranh tồn không gây âm. Lỗi rollback toàn tiền/kho/điểm/trạng thái; timeout sau gửi đối chiếu MaHD/trạng thái, không tự retry thu tiền. UI không coi hủy task chờ là rollback SQL.
- Hủy qua procedure chỉ đơn mở chưa tiền/tiêu hao, trả bàn trống, không hoàn kho. Đồ đã pha rồi hủy chỉ hướng dẫn nghiệp vụ xuất hủy lượng thực dùng ở T21/G05; không triển khai xuất hủy sớm hoặc tự ghi tiêu hao bán. Đơn đã trả/hủy khóa tài chính.

### Tra cứu, in và giao

- Lịch sử ghi rõ lọc ngày lập hay thanh toán, hỗ trợ trạng thái/kênh/khách/mã và phân trang; không bỏ cuối ngày. Chi tiết/in dùng snapshot đã lưu, không suy lại tỷ lệ ưu đãi lịch sử từ tiền giảm hoặc giá menu hiện tại.
- In sau commit, có preview, nhiều trang và nhãn In lại; lỗi/hủy in không đổi giao dịch. Đọc đơn không tồn tại thông báo đúng. Đơn mở có thể quay lại POS; đơn chốt chỉ xem/in/chuyển tiến độ hợp lệ.
- Giao hàng có bộ lọc mã/SDT/trạng thái, chỉ hiển thị đơn giao; thanh toán và giao là hai trạng thái riêng. Chuyển Chờ giao → Đang giao → Đã giao đúng contract SQL, phát hiện phiên cũ; không thay tài chính đơn đã trả.
- Giao thất bại đơn đã trả có hướng dẫn xử lý thủ công; không hoàn tiền tự động. Khả năng lưu lịch sử chuyển giao hay chỉ trạng thái hiện tại phải được chốt trong plan T18 trước code, công khai giới hạn, không dựng timeline giả.

### Quyền và tương tác chung

ADMIN/CASH được POS/thanh toán/hủy hợp lệ/in/giao; STORE bị chặn tại BLL, không chỉ ẩn menu. Kiểm phiên/nhân viên/quyền trước thao tác. CASH được tìm/thêm khách theo contract G03, không sửa giá/công thức/chính sách/điểm. ADMIN giữ màn bắt đầu đúng vai trò; T16 thay màn tạm CASH bằng POS.

Giữ input khi lỗi; phân biệt loading/rỗng/lỗi/xung đột/kết quả chưa biết/đã chốt. Query async, bỏ kết quả trả muộn khi đổi màn/đóng; không giả tổng0 khi mất DB. F2 tìm món, F3 chọn khách, F4 đơn mở, F8 mở dialog; modal/loading chặn phím gửi lặp. Enter không tự chốt tiền. Tiền/ngày/giờ định dạng theo guide, DAL nhận giá trị typed.

## 3. Giới hạn và các quyết định trong bộ plan

- Giữ kiến trúc WinForms .NET10/Models/BLL/DAL SqlClient, procedure sở hữu transaction; không thêm EF/Dapper/API, generic repository hoặc module tương lai. Mỗi task có SQL đi theo SQL → DAL → BLL → UI; chỉ gọi contract đã có migration.
- Trước lập chi tiết UI/thiết kế/code phải đọc đủ bộ `docs/ui/` theo README: guide Orbit, reference.html, tokens, references, WinForms implementation, assets. Kế thừa AppTheme/native control/helper hiện có, không refactor ngoài phạm vi. HTML không là bằng chứng WinForms.
- Theo phân công ngày22/10 và AGENTS.md hiện hành, người dùng tự kiểm hình thức UI. Agent không test thị giác/chụp ảnh đối chiếu/hover/spacing hoặc nghiệm thu hình thức qua Designer; ghi SKIPPED theo phân công. Vẫn thiết kế hỗ trợ DPI100/125/150%, 1280×720 và1366×768; agent chỉ kiểm chức năng/layout kỹ thuật ở125%, không đổi Windows Scale. DPI100/150% chưa chạy ghi NOT RUN, không suy PASS. Phân công này ưu tiên hơn yêu cầu hình thức/DPI cũ; không sửa checklist gốc bên dưới.
- Bộ plan phải chốt tạo đơn với món đầu, sửa thông tin đơn/khách/KM, có hỗ trợ chuyển loại hay khóa loại sau tạo theo nhánh UX7.3; version/xung đột, mã ổn định/đối chiếu timeout, thứ tự khóa dùng chung với Recipes/Policy G03, DTO và error map. Không để đến lúc code mới quyết định các contract này.
- Plan chốt snapshot bổ sung và preflight/cutover dữ liệu cũ: bảo toàn tiền/tồn/điểm/lịch sử; không bịa nguồn/tỷ lệ cũ. Seed/migration demo tương thích được ghi nhận; không chạy lại seed trên DB đã dùng.
- Plan T16 chốt hủy có lưu lý do hay chỉ xác nhận theo contract; không hỏi lý do rồi làm mất. Plan T18 chốt có lưu lịch sử giao hay chỉ hiện trạng, và hành vi hủy giao/chuyển bước/giao thất bại đúng trạng thái tài chính.
- Ngoài phạm vi: hoàn tiền, đổi điểm/PT010, size/topping, đặt bàn, gộp/tách bill, thanh toán online/đối soát giao, nhiều kho/chi nhánh/offline; xuất hủy T21, tài khoản đầy đủ T24, dashboard tài chính T28, publish cuối T32.
- Không có câu hỏi nghiệp vụ bắt buộc để duyệt requirements hiện tại. Khi viết plan sẽ đọc baseline/seed/migrations/source thực tế, tách phần đã có với contract đích. Blocker phụ thuộc hoặc thay nghiệp vụ ngoài phạm vi phải báo để duyệt phần phát sinh.

## 4. Phạm vi và nghiệm thu gốc T14–T18

Sao nguyên bản từ kế hoạch3.3 dưới đây, giữ nguyên **56 ID (12 + 15 + 12 + 9 + 8)** cùng phạm vi/đầu ra/phụ thuộc và điều kiện hoàn tất. Subcase bổ sung không thay hoặc bỏ ca gốc. Câu yêu cầu lưu commit không tự cấp quyền commit; chỉ commit/push khi người dùng giao riêng.
### T14 — SQL tạo đơn và sửa dòng món nguyên tử

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Đóng gói thao tác đơn mở, tạo cơ sở chắc cho POS. |
| Phạm vi | Procedure đề xuất sp_TaoHoaDon/sp_SuaDongHoaDon, rowversion nếu dùng, header/chi tiết/ưu đãi; chưa thu tiền. |
| Quy mô | L — ghi nhiều bảng và đồng thời |
| Phụ thuộc | T13 |
| Đầu ra | Migration thao tác đơn, SalesRepository/Service và các request typed. |

**Kiểm thử bắt buộc:**

- [ ] `T14-K01` [I] Tạo đúng 3 loại đơn; tại quán bắt buộc bàn, mang đi không bàn/đối tác, giao có đủ người nhận.
- [ ] `T14-K02` [I] Khách vãng lai NULL hợp lệ; mã bàn/khách/nhân viên sai hoặc nhân viên nghỉ bị chặn.
- [ ] `T14-K03` [I] Header khởi tạo tổng/giảm 0, thêm món trigger cập nhật đúng.
- [ ] `T14-K04` [I] Hai dòng cùng món khác ghi chú giữ hai MaDong; thao tác theo MaDong không sửa nhầm dòng/đơn.
- [ ] `T14-K05` [I] Tạo dòng lấy giá DB; sửa số lượng giữ giá snapshot; số lượng 0/âm/quá int bị chặn.
- [ ] `T14-K06` [I] Bớt/xóa món đưa giảm cũ về hợp lệ và tính lại trong một transaction, không vướng CHECK trung gian.
- [ ] `T14-K07` [I] Sản phẩm ngừng bán/không có công thức không được thêm theo hợp đồng.
- [ ] `T14-K08` [I] Sửa đơn đã trả/hủy bị chặn; trạng thái form cũ không bypass kiểm tra DB.
- [ ] `T14-K09` [I] Hai session tạo cùng bàn: một đơn mở duy nhất, session còn lại tải đơn có thật.
- [ ] `T14-K10` [I] Hai session sửa cùng đơn: version cũ bị báo xung đột hoặc được tuần tự hóa theo chính sách, không ghi đè im lặng.
- [ ] `T14-K11` [I] Lỗi giữa header/chi tiết/ưu đãi rollback mọi thay đổi; mất kết nối xác định lại theo mã cấp.
- [ ] `T14-K12` [I] Xóa hết dòng thành đơn rỗng mở, có thể hủy nhưng không checkout.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

### T15 — Hoàn thiện procedure checkout và bảo vệ giao dịch

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Tận dụng procedure/trigger hiện có, đưa tính ưu đãi cuối cùng vào cùng transaction SQL. |
| Phạm vi | ALTER sp_ThanhToanHoaDon, snapshot ưu đãi cần thiết, khóa và output DTO; không viết engine thanh toán mới phía C#. |
| Quy mô | L — giao dịch trọng yếu |
| Phụ thuộc | T14 |
| Đầu ra | Checkout SQL trọn vẹn, contract TVP/preview, error map; seed tương thích được kiểm thử lại. |

**Kiểm thử bắt buộc:**

- [ ] `T15-K01` [I] Bill hợp lệ trả 1/nhiều phương thức và nhiều dòng cùng phương thức chốt đúng một lần.
- [ ] `T15-K02` [I] SQL tái tính ưu đãi theo chính sách; preview thay đổi thì trả lỗi xác nhận lại và không thu theo số cũ.
- [ ] `T15-K03` [I] Thiếu/vượt phân bổ, phương thức không có/PT010 bị từ chối và rollback.
- [ ] `T15-K04` [I] Giảm 100%, ship 0 và TVP rỗng: chốt 0 VND hợp lệ, tiêu hao đúng, không dòng thanh toán 0.
- [ ] `T15-K05` [I] Giảm 100% nhưng ship >0: vẫn phải trả đủ phí ship.
- [ ] `T15-K06` [I] Thiếu một nguyên liệu, món thiếu công thức/ngừng bán: rollback toàn kho/tiền/điểm/trạng thái.
- [ ] `T15-K07` [I] Nhiều dòng cùng SP/NVL: tiêu hao group đúng, không trừ hai lần; decimal 4 chữ số lẻ giữ đúng.
- [ ] `T15-K08` [I] Điểm floor sau giảm, không cộng ship/phí đối tác; mỗi MaHD chỉ cộng một lần.
- [ ] `T15-K09` [I] Phí đối tác 0/100% và số lẻ làm tròn đúng; đổi tỷ lệ đối tác không đổi đơn cũ.
- [ ] `T15-K10` [I] Hai session checkout cùng đơn: một commit; session còn lại không tăng tiền/điểm/trừ kho nữa.
- [ ] `T15-K11` [I] Hai đơn dùng lượng tồn cuối: chỉ giao dịch đủ tồn commit; không tồn âm.
- [ ] `T15-K12` [I] Lỗi cưỡng bức sau tiêu hao/sau ghi tiền/trước điểm rollback tất cả và @@TRANCOUNT về trạng thái hợp lệ.
- [ ] `T15-K13` [I] Gọi trực tiếp sửa TrangThai/GiamGia/tiêu hao thay procedure bằng quyền app bị chặn.
- [ ] `T15-K14` [I] Ngày thanh toán trước lập đơn bị chặn; ngày mua/import dùng đường quản trị riêng.
- [ ] `T15-K15` [I] Chạy lại seed baseline hoặc bản seed migration đã ghi nhận thay đổi: tổng/snapshot/điểm khớp hợp đồng mới.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

### T16 — Hoàn thiện POS, checkout và hủy đơn trên UI

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Nối các service đã kiểm chứng thành luồng bán hàng sử dụng được. |
| Phạm vi | POS/menu/bàn/khách/dòng món, dialog TT, input tiền đưa/thối, hủy đơn; không logic SQL trong form. |
| Quy mô | L — giao diện luồng chính |
| Phụ thuộc | T15 |
| Đầu ra | POS bán đủ 3 kênh, checkout trả kết quả thật, xử lý trạng thái không rõ. |

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

### T17 — Tra cứu, chi tiết và in hóa đơn

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Có bằng chứng hóa đơn đã lưu và in lại an toàn. |
| Phạm vi | Danh sách/filter/detail/PrintDocument/PrintPreview, tài chính read-only sau chốt. |
| Quy mô | M — hậu giao dịch |
| Phụ thuộc | T16 |
| Đầu ra | Tra cứu, xem preview và in lại; hoàn thành mốc demo bán hàng theo mục 12.2, chưa là bản vận hành đầy đủ. |

**Kiểm thử bắt buộc:**

- [ ] `T17-K01` [I/M] Lọc theo ngày/trạng thái/kênh/khách trả đúng đơn, không bỏ giờ cuối ngày.
- [ ] `T17-K02` [I] Tiền và số lượng lấy snapshot đã lưu, không menu hoặc tỷ lệ KM hiện tại.
- [ ] `T17-K03` [M] Preview/in đủ đơn nhiều dòng, ghi chú tiếng Việt, phần giảm/ship/tổng/phương thức; phân trang không mất dòng.
- [ ] `T17-K04` [M] Ghi đúng tiền dòng trước giảm và số phải trả, không nhầm tiền khách đưa.
- [ ] `T17-K05` [M] In lại không bịa tiền đưa/thối khi DB chưa lưu; nhãn in lại rõ.
- [ ] `T17-K06` [M/I] Lỗi máy in/hủy print sau commit không thay tiền/kho/điểm.
- [ ] `T17-K07` [M/I] In lại nhiều lần không gọi checkout; không tạo thanh toán/hóa đơn mới.
- [ ] `T17-K08` [M/I] Không có nút sửa tài chính đơn chốt; gọi service sửa vẫn bị chặn.
- [ ] `T17-K09` [M] Xem đơn không tồn tại/xóa chưa chốt thông báo đúng, không crash.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

### T18 — Theo dõi và chuyển trạng thái giao hàng

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Dùng cột giao hàng sẵn có và kiểm soát trình tự chuyển trạng thái. |
| Phạm vi | Danh sách đơn giao, procedure cập nhật TrangThaiGiao; không tích hợp nền tảng hoặc hoàn tiền. |
| Quy mô | M — trạng thái phụ của hóa đơn |
| Phụ thuộc | T17 |
| Đầu ra | Module giao hàng và SQL kiểm tra chuyển trạng thái. |

**Kiểm thử bắt buộc:**

- [ ] `T18-K01` [I/M] Chỉ đơn Giao hang có màn hình và dữ liệu người nhận hợp lệ.
- [ ] `T18-K02` [I] Cho giao → Dang giao → Da giao được; nhảy lùi/nhảy sai trình tự bị chặn.
- [ ] `T18-K03` [I] Chuyển trạng thái giao của đơn đã trả không đổi tiền/đối tác/khách/ngày thanh toán.
- [ ] `T18-K04` [I/M] Đơn mở chưa trả hủy giao/hủy đơn đúng; không dùng hủy giao để hoàn tiền đơn đã trả.
- [ ] `T18-K05` [I] Hai session cập nhật từ trạng thái cũ: phát hiện xung đột, không ghi đè kết quả mới.
- [ ] `T18-K06` [U/M] CASH/ADMIN được cập nhật; STORE bị chặn.
- [ ] `T18-K07` [M] Bộ lọc tìm theo mã/SDT/trạng thái, refresh sau thao tác và reload còn dữ liệu.
- [ ] `T18-K08` [M] Giao thất bại đơn đã trả có hướng dẫn xử lý thủ công, không hiển thị đã hoàn tiền tự động.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

## 5. Kiểm chứng, bàn giao và tiêu chí hoàn tất

1. Duyệt requirements nhóm một lần → viết đủ năm plan T14–T18 và chỉ mục `plans.md` → chờ duyệt cả bộ một lần, rồi triển khai tuần tự. Yêu cầu bắt đầu G04 chưa là duyệt plan hoặc đợt test.
2. Trong triển khai, build/compile migration/smoke kỹ thuật tối thiểu theo plan được duyệt, ghi riêng lệnh/môi trường/kết quả; không tick Txx-Kxx hoặc gọi nghiệm thu PASS. Không commit/push khi chưa được giao.
3. Triển khai xong trình một đợt test riêng; sau duyệt giữ source/migration cố định, test từng task, lưu `test-results.md`, tổng hợp `report.md` kể cả FAIL/BLOCKED/NOT RUN. Bug ghi tại task sở hữu, duyệt cả đợt trước debug; duyệt debug bao gồm regression theo ảnh hưởng, bug mới cần duyệt bổ sung.
4. Test tiền/kho/điểm và transaction trên SQL Server thật, DB test riêng; đối chiếu toàn bảng liên quan và hai session cho concurrency. Có fixture clean/demo/legacy, principal runtime/migration và fault injection rõ trong plan. Unit xUnit cho quy tắc có giá trị; thao tác service không thay bằng chứng chức năng WinForms.
5. Truy vết UX-K05–K23, UX-K39–K43 và kiểm phiên/quyền liên quan UX-K03/K04. Phân rã phần hình thức SKIPPED theo phân công và DPI100/150% NOT RUN riêng với phần chức năng; không đánh PASS toàn ca khi thiếu bằng chứng bắt buộc. Regression T09-K09, T10-K04/K06–K08 với checkout đích, tích hợp bàn T11 và khách/preview/điểm T12–T13; không kế thừa PASS từ checkout baseline/fixture.
6. Bổ sung kịch bản dùng thử ADMIN/CASH/STORE trên đường chạy development cố định hiện có: `CafeManager.slnx`, startup `src/CafeManager.WinForms`, profile `CoffeeManager.Development`, DB `localhost/CoffeeManager_Development`; xác minh và ghi schema/exe/build thực tế khi triển khai. Nâng schema có backup/preflight/đối chiếu, không reset tài khoản hoặc ghi fixture test vào DB development. Không ghi bí mật vào repo/chat/log.
7. Tài nguyên test có manifest runID/server/DB/marker/principal/config/process/thư mục và ownership. Sau lưu bằng chứng dọn allowlist đúng tài nguyên lượt chạy kể cả FAIL; báo trước/tạo/dọn/còn giữ và lý do/hạn/cách dọn. Không xóa DB development/tài nguyên sẵn có; không tự đổi mode SQL/registry/service.
8. Chỉ ghi Hoàn tất khi đủ đầu ra, các ca bắt buộc PASS và không còn lỗi trọng yếu, hoặc có quyết định ngoại lệ cụ thể của người dùng được ghi riêng; không biến ngoại lệ thành kết quả chạy PASS. Giữ nguyên325ID và lịch sử T01–T32. Requirements đã được duyệt ngày 23/10/2026; bộ plan đã được duyệt và triển khai; bước hiện tại là **duyệt đợt test G04** tại [test-plan.md](test-plan.md).
















