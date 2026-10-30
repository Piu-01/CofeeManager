Trạng thái24/10/2026: hai vấn đề bổ sung đã xử lý theo duyệt, schema016;
35PASS/0FAIL/21BLOCKED trên56ID; UI05 đã sửa và handler Confirm thật được người dùng/SQL xác nhận; xem report.md.

# Bộ plan G04 — Bán hàng và vòng đời hóa đơn

Bổ sung Đơn đang mở25/10 đã duyệt/triển khai/test component+SQL98checksPASS; native/hìnhthức chờ người dùng:
[giao diện theoZIP và cột mã đầy đủ](t16-pos-huy-don/open-orders-ui-plan.md).

**Bổ sung luồng sau thanh toán25/10, đã duyệt/triển khai; chờ duyệt test:**
[Hỏi in và trở về đơn mới — T16/T17](t16-pos-huy-don/post-checkout-print-plan.md).
Không sửa SQL/role/checklist; test phần mới chờ duyệt đợt riêng sau triển khai.

**Bổ sungUI25/10/2026,đã duyệt/triển khai,chờ duyệt test:**
[CheckoutDialog theoZIP người dùng và sharedOrbitcontrols](ui-checkout-proposal/plan.md).
Đây là phần bổ sungT16,không mở lại cácplankhác hoặc thaySQL/role.

**Ngày:** 23/10/2026. **Requirements:** đã duyệt qua phản hồi “duyệt”.  
**Trạng thái bộ plan:** Đã được người dùng duyệt cả bộ ngày 23/10/2026 qua phản hồi “diuyeetj” (duyệt); đã triển khai tuần tự T14–T18. Đợt nghiệm thu đã duyệt và đã chạy; còn FAIL/BLOCKED, xem report.md.  
Nguồn và phạm vi tại [requirements.md](requirements.md); giữ nguyên 56 ID của G04 và 325 ID toàn dự án. Plan sau dùng đầu ra plan trước; làm một task tại một thời điểm.

## 1. Chỉ mục và thứ tự

| Thứ tự | Plan | Migration dự kiến | Đầu ra |
|---|---|---|---|
| 1 | [T14 — Đơn mở](t14-tao-sua-don/plan.md) | 010_sales_orders.sql | Tạo header+món đầu, sửa dòng/thông tin, đọc đơn, version/receipt |
| 2 | [T15 — Checkout](t15-checkout/plan.md) | 011_checkout.sql | Preview/chốt SQL nguyên tử, snapshot, phân bổ, điểm/kho |
| 3 | [T16 — POS và hủy](t16-pos-huy-don/plan.md) | 012_sales_cancel.sql | POS ba kênh, dialog TT/kết quả/đối chiếu, hủy đúng quyền |
| 4 | [T17 — Tra cứu và in](t17-tra-cuu-in/plan.md) | 013_invoice_queries.sql | Lọc/phân trang, snapshot chi tiết, preview/in lại |
| 5 | [T18 — Giao hàng](t18-giao-hang/plan.md) | 014_delivery.sql | Lọc và chuyển trạng thái có version, giữ tài chính |

Tên file mới bên dưới là đường dẫn dự kiến trong CoffeeManager, không xác nhận đã có implementation. Không sửa checksum001–009; nếu migration mới đã được áp dụng rồi mới cần điều chỉnh thì thêm version tiếp, không sửa lịch sử đã áp dụng.

## 2. Đối chiếu source đầu vào

- HEAD ghi ở requirements không đại diện toàn working tree đang có thay đổi. Trước triển khai/test lưu manifest hash source và checksum SQL; không reset/clean, không commit/push khi chưa được giao.
- Baseline có HOADON, CHITIET_HD.MaDong, CHITIET_THANHTOAN.MaTT, tiêu hao, trigger, TVP3cột và procedure checkout/hủy. 001 có SeqHoaDon/sp_CapMa, DENY tài chính/lịch sử; chưa có SalesRepository/Service, POS/tra cứu/in/giao. 010 sẽ bổ sung Revision header, chưa giả định đã có.
- 003 có PaymentMethodSupport và selector Checkout; chỉ PT001/PT002 được bật mặc định, PT010 luôn bị chặn. Không gọi danh mục ADMIN từ POS CASH. Thêm lookup sản phẩm/danh mục/đối tác hẹp ở T14, không mở quyền quản trị danh mục cho CASH.
- 004 có G03OperationReceipt/sp_G03BeginReceipt/sp_G03Demand; 005 có khóa Recipes; 007 khách/điểm mở sổ; 008 có fn_DiscountPreview và khóa LoyaltyPolicy, checkout vẫn tin GiamGia đã lưu. 009 chỉ sửa mã KM legacy. Dùng lại receipt/demand/discount, không tạo generic framework mới.
- MainForm hiện bắt đầu ADMIN ở Catalogs, vai trò khác ở Home; NavigationTarget nằm trong NavigationController.cs. T16 chỉ thay CASH vào Sales, thêm target/permission tương ứng, giữ điểm vào ADMIN/STORE hiện tại. Dashboard ADMIN thật vẫn T28.
- AuthRepository.ReadAsync hiện chỉ đọc một result set và không chuyển cancellation ghi thành unknown. SalesRepository dùng connection/reader riêng để đọc DTO header+dòng/preview nhất quán và map cancellation khi đã gửi; không đổi helper auth toàn dự án.

## 3. Quyết định chung cần duyệt cùng bộ plan

### 3.1. Actor, kiểu dữ liệu, version và mã

Mọi entry point G04 nhận ActorAccountId và SecurityStamp từ SessionContext; BLL DemandAsync Sales, SQL sp_G03Demand ADMIN/CASH trước đọc/ghi. MaNV tạo đơn lấy từ actor đã xác minh ở DB, không tin MaNV do form tự nhập; actor checkout/giao có thể khác nhân viên tạo, nhưng đều phải đang làm việc. Đơn cũ có nhân viên tạo đã nghỉ không bị tự xóa; nhân viên nghỉ không tạo/thực hiện giao dịch mới được.

C# decimal tiền nguyên VND≤18chữ số, lượng snapshot≤4số lẻ, int ly/MaDong/MaTT/điểm; SQL wide parameter kiểm trước cast, không âm thầm làm tròn input lỗi. Giới hạn tên/người nhận100, SDT15chữ số, địa chỉ250, ghi chú dòng100 theo schema. Trim optional→NULL, FK thật, tổng/số điểm overflow từ chối nguyên tử. Không đổi giá trị trạng thái DB không dấu; Models ánh xạ sang nhãn Việt.

010 thêm HOADON.Revision rowversion. Mọi sửa/xóa dòng, thông tin, checkout, hủy, giao yêu cầu ExpectedRevision; khóa header rồi so sánh, stale trả conflict. MaHD lấy sp_CapMa trước gửi lệnh tạo, giữ ổn định sau timeout; gap sequence hợp lệ. CreateRequestId riêng cho từng lần tạo, cùng mã/request/payload không tạo lần hai. Không nhập mã kỹ thuật bằng tay trong POS.

### 3.2. Receipt, khóa và kết quả chưa rõ

Dùng G03OperationReceipt cho operation SalesCreate/SalesLine/SalesHeader/SalesCheckout/SalesCancel/SalesDelivery; mỗi ghi hash canonical gồm thao tác, MaHD, payload, expected revision, preview token và phân bổ có giữ multiplicity. TVP thanh toán sắp theo MaPT/tham chiếu/số tiền để hash ổn định, không DISTINCT các dòng trùng. Cùng request khác actor/payload bị chặn, receipt commit cùng nghiệp vụ. Receipt chỉ chứng minh kết quả tại thời điểm ghi; đọc đơn hiện tại phải ghi rõ có thể đã thay đổi tiếp.

010 thêm khóa transaction-owned `CafeManager:SalesRequest:<guid>` ngay đầu mỗi lệnh ghi/đối chiếu, rồi receipt, rồi LoyaltyPolicy Shared (nếu cần), Recipes Shared (nếu cần), `CafeManager:SalesOrder:<MaHD>` Exclusive, header UPDLOCK/HOLDLOCK, khách, sản phẩm/đối tác và kho. Với tạo tại quán lấy khóa bàn/row BAN trước tạo header; unique filtered index đơn mở/bàn vẫn là chốt DB. Ghi không cần policy/recipes bỏ qua khóa đó, không lấy ngược lại. 008/005 đang lấy Policy→Recipes→header; giữ thứ tự này, không sửa G03 để lấy khóa Order muộn sau header. Khóa khách trước tồn khi checkout, tiêu hao/kho khóa theo MaNVL tăng dần trong cùng giao dịch; không khóa từng dòng theo roundtrip app. Không NOLOCK. Thời gian applock tối đa10giây, lỗi cần đối chiếu trước thao tác tiếp.

sp_SalesReconcile nhận Actor/Stamp, RequestId, MaHD và loại thao tác, lấy request fence với timeout hữu hạn, đọc receipt đúng actor và trạng thái/version hiện tại. Nếu lấy fence chưa được hoặc DB lỗi thì Unknown/Pending; không coi receipt vắng là rollback. Phải kết thúc/dispose lời gọi cũ và xác nhận phiên lệnh cũ đã chấm dứt, rồi mới dùng kết quả đối chiếu không receipt để đưa về trạng thái có thể xác nhận lại. Checkout không tự retry; nếu đã trả chỉ xem/in, nếu còn mở sau phân xử cho người dùng tải preview và xác nhận lại. Không tạo request mới trong lúc kết quả cũ chưa rõ. Replay cùng request chỉ qua một hành động rõ sau đối chiếu và giữ nguyên payload, không lấy version mới nhét vào request cũ. Bản chốt có thể tra MaHD khi logout/restart; mã/request lưu trong ngữ cảnh gửi đến khi kết thúc, không lưu bí mật.

Không mở transaction C#; SQL XACT_ABORT/TRY/CATCH, rollback transaction do procedure sở hữu. Entry point runtime từ chối ambient transaction để không trả success trước commit ngoài; test lỗi kiểm @@TRANCOUNT/XACT_STATE trên đúng connection. Fault hook chỉ nằm trong fixture test riêng, không thêm cờ bỏ kiểm tra vào procedure sản phẩm.

### 3.3. Quyết định nghiệp vụ/UI

- Tạo cùng món đầu nguyên tử; chọn bàn trống không ghi DB. Nếu bàn đã có đơn trả conflict kèm mã đơn thật để tải, không tự thêm món sang đơn đó.
- Khóa LoaiDon sau tạo (nhánh cho phép tại UX7.3); không chuyển bàn/loại, gộp/tách bill trong G04. Người dùng muốn đổi thì hủy đơn mở và tạo lại, có cảnh báo nếu đã pha. Cho sửa khách/KM và, với đơn giao còn mở/chờ giao, người nhận/SDT/địa chỉ/đối tác/ship. Không sửa người tạo/ngày lập hoặc nhập giảm/giá trực tiếp.
- Hủy T16 chỉ xác nhận mã/bàn/tổng/hậu quả, không ô lý do vì schema chưa lưu. Không bổ sung lịch sử lý do hủy ngoài phạm vi.
- T18 chỉ lưu TrangThaiGiao hiện tại, không thêm bảng timeline. Chờ giao→Đang giao→Đã giao cho đơn giao mở hoặc đã trả; hủy giao chỉ bằng hủy đơn mở. Đơn giao đã hủy không tiến bước. Đơn đã trả giao thất bại giữ trạng thái hiện tại và hiện hướng dẫn thủ công; không thêm trạng thái lỗi/hoàn tiền. UX và hướng dẫn sẽ công khai giới hạn này.
- Tiền đưa/thối chỉ dùng phiên checkout, không thêm cột lưu. Snapshot tài chính mới lưu bền vững ở T15; lịch sử cũ thiếu nguồn/tỷ lệ ghi LegacyUnknown, không suy tỷ lệ từ tiền giảm.

### 3.4. UI Orbit và phân công kiểm

Đã đọc README→guide→reference (CSS/markup và JS nghiệp vụ; bỏ bundle icon minified không liên quan)→tokens→references README→WinForms implementation→assets. AppTheme/OrbitButton và native nhập liệu dùng lại; chỉ thêm ProductCard/OrderPanel/overlay nhỏ cần cho POS. Không nhúng WebView2, không tải Internet. Token màu/font/motion tập trung, nội dung Segoe UI10,5pt; PNG minh họa ghi đúng tính chất, Resources theo DPI/license. HTML không là bằng chứng app.

Quy định AGENTS.md ngày22/10 ưu tiên hơn checklist hình thức trong guide: agent không chạy visual/chụp ảnh đối chiếu/hover/spacing/Designer hình thức; phần này SKIPPED theo phân công cho người dùng. Agent kiểm chức năng/bounds/cuộn/focus/handler ở DPI125% hiện có, không đổi Windows Scale; DPI100/150% chưa chạy NOT RUN. Source vẫn hỗ trợ PerMonitorV2, không ép DPI hay scale bitmap toàn app. T16 là mẫu POS trước mở rộng style POS, T17/T18 dùng danh sách/editor Orbit đã có, không nhân rộng card/animation sang màn khác.

MainForm giữ minimum cơ sở640×480logical và điều chỉnh không vượt WorkingArea; client<900logical thu sidebar và panel bàn; client<680 chuyển sang các vùng tab Món/Đơn, tổng+CTA truy cập cố định. Dialog thanh toán mục tiêu760×620logical, editor520×420, chi tiết900×620; chặn kích thước theo WorkingArea trừ titlebar/border, field/grid cuộn, footer luôn truy cập. Không đặt minimum khiến Snap nửa màn bị cấm. Plan task bổ sung bố cục cụ thể. Giá trị chỉ là thiết kế, khi chạy ghi outer/client/resolution/DeviceDpi thực đo.

## 4. Kiểm tra kỹ thuật, nghiệm thu và fixture chung

Sau duyệt bộ plan mới triển khai tuần tự SQL→DAL→BLL→UI. Trong triển khai được build Debug/Release, apply migration và smoke tối thiểu trên SQL Server test để xác nhận contract cho task sau; ghi `technical-checks.md`, không tick ca nghiệm thu. Thiếu SDK/SQL/GUI là blocker thực tế, không suy PASS. Đợt nghiệm thu56ID chỉ bắt đầu sau duyệt riêng.

Chuẩn bị `tools/database/Test-G04.ps1` và `database/tests/g04/` dùng DatabaseTestTools hiện có; runner gọi unit, SQL và tổng hợp exit code/FAIL đầy đủ (kế thừa bài học G03-B01). DBtest riêng có tên chính xác từ runID, tối đa3DB đang tồn tại mỗi lượt: clean, demo, legacy-invalid; tình huống concurrency dùng2connection thật trong cùng DB. Không ghi fixture vào development. Dataset:

- Clean: baseline không seed →001–014, setup/admin qua contract chuẩn; tạo đủ tài khoản ADMIN/CASH/STORE bằng fixture quản trị test, trạng thái setupCompleted; không dùng bypass làm bằng chứng login.
- Demo: schema rỗng→Insert_Data đúng một lần dưới contract baseline→001–014. Đối chiếu bất biến cũ trước/sau, LegacyUnknown và các hóa đơn mới qua checkout đích. Không chạy Insert_Data sau011 để gọi chữ ký checkout cũ.
- Legacy/preflight: sao dữ liệu thử hợp lệ tạo từ clean/demo test vào DB riêng; có đơn mở, chốt/hủy, snapshot thiếu, dữ liệu không hợp lệ, thiếu schema/checksum; migration dừng và rollback nguyên trạng khi preflight sai.
- Fault: trigger test-only trên bảng chốt/tiền/điểm để THROW tại3vị trí T15, trên header/chi tiết khi T14; cô lập và dọn trước ca kế tiếp. Ghi tên/mã hook trong evidence, không chỉnh production procedure giữa lượt test.

Manifest trước chạy gồm runID/server/DB/marker/schema/checksum/sourcehash/build/runtime principal/process/config/path/ownership; dùng operator hiện có, SQL user WITHOUT LOGIN+EXECUTE AS trong SQL, không tạo Windows user/login mới. GUI qua cấu hình test đúng DB và principal giới hạn được cấp trên máy; nếu không sẵn có thì ghi blocker, không tự mở rộng quyền hoặc rơi về development. Test giữ source/migration cố định, không sửa khi FAIL; biên bản từng subcase expected/actual/status/evidence và report nhóm.

Cleanup finally sau lưu log/evidence, exact allowlist+marker+dependency, không DROP wildcard, không dọn tài nguyên sẵn có. Tài nguyên muốn giữ điều tra phải có chỉ định, lý do/hạn/cách dọn; báo trước/tạo/dọn/còn giữ kể cả FAIL. Hash source trước/sau; không log người nhận đầy đủ không cần thiết hoặc credential.

Bổ sung GUI chức năng chung: keyboard/IME/Tab/Esc/F2/F3/F4/F8; loading/error/empty/late result; nháp/session/logout/revoke;125% tại1280×720 và1366×768 (nếu môi trường hỗ trợ, không đổi Windows Scale), thường/maximize/Snap. Dữ liệu dài, nhiều dòng, tiền18chữ số.100lần đổi module+10login/logout theo dõi số timer/handle/connection còn sống; không đặt kết luận tốc độ thay T29. Hình thức SKIPPED,100/150% NOT RUN riêng, giữ checklist gốc.

## 5. Development và trạng thái bàn giao

T16 bàn giao dùng thử POS, T17 in/tra cứu, T18 giao trên cùng solution/profile/DB development đã có. Thêm `tools/database/Update-DevelopmentG04.ps1` theo mẫu G03: app đóng, đúng localhost/CoffeeManager_Development+marker, backup ngoài Git, preflight schema/checksum, áp010–014 theo task đã xong, đối chiếu tồn/điểm/tiền và account trước/sau. Không tự chạy seed/reset account, không fixture nghiệm thu. Quyền nâng DB development và smoke này được bao gồm khi người dùng duyệt bộ plan; log rõ phiên bản thực, không giả định DB hiện tại đã là009.

Cập nhật docs/database/{sales-contract,checkout-contract,invoice-contract,delivery-contract}.md theo task, migrations.md/write-ownership.md/data-contract.md, README và `development-guide.md` nhóm. Không gỡ tiện ích đăng nhập TEST sớm trong G04; nghĩa vụ gỡ sạch/regression ở T32 giữ nguyên. Không dùng tiện ích đó để nghiệm thu đăng nhập/phiên/quyền.

Sau triển khai toàn nhóm trình duyệt đợt test một lần, test T14→T18 rồi report kể cả FAIL. Bug tại task, gom đợt chờ duyệt debug; không đánh Hoàn tất chỉ vì build/smoke. G03 có ngoại lệ được kế thừa như hồ sơ; G04 vẫn phải regression tích hợp T09-K09/T10-K04/K06–K08/T11/T12–T13 và UX tương ứng, không sửa lịch sử cũ. Hiện chỉ có tài liệu, chưa cấp kết quả PASS nào.


## Ghi nhận triển khai23/10/2026

Bộ plan đã triển khai, chờ duyệt test riêng; xem report.md. Thêm migration015 sau014
để chuẩn hóa optional code thànhNULL ở API ghi và đọc nguồn/tỷ lệ của đơn đã trả từ
snapshot, tránh phụ thuộc chính sách hiện tại. Đây là hoàn thiện contract đã duyệt,
không thêm nghiệp vụ; giữ checksum010–014 đã ghi ở lượt kỹ thuật. Schema đích/development
hiện tại015, thay mốc014 dự kiến ban đầu. SQL acceptance được gom thành ba batch tại
`database/tests/g04/acceptance-*.sql` để dùng cùng fixture; không nhân bản fixture ở từng
task. Chúng chỉ là SQLsubcase, không thay56ID, fault/hai session hoặc GUI đầy đủ.
UI mới bố cục bằng code nhỏ theo mẫu control hiện có; không chứa SQL/logic chốt trong
Designer và không thực hiện nghiệm thu hình thức qua Designer.














## Bổ sung giao diện Lịch sử hóa đơnV4 —26/10/2026

Phạm vi và [plan bổ sung](ui-invoice-history-v4/plan.md) đã được user duyệt và
triển khai, gồm hành vi tìm kiếm tách khỏi bộ lọc tự áp dụng. Giữ contract/checklist
T17, control dùng chung opt-in, không đổi SQL/quyền. Build Debug/Release sạch,
smoke kỹ thuật28/28 tại125%. Đợt test riêng đã duyệt/chạy:53/53 functional,
94/94 regression,18/18 xUnit; chờ manual/hình thức, xem
[kết quả](ui-invoice-history-v4/test-results.md).

## Bổ sung giao diện Giao hàng —26/10/2026

[Plan đồng bộ trang danh sách Giao hàng](t18-giao-hang/ui-delivery-plan.md) đã được
người dùng duyệt và đã triển khai. Tái dùng chuẩn V4/
khung khách hàng, profile Delivery opt-in, TotalCount có sẵn; giữ contractT18,
checklist và trạng thái nghiệm thu lịch sử. Đợt test riêng đã duyệt/chạy:55/55giao,
108/108native/shared,53/53invoice,18/18xUnit; còn xác nhận manual/hình thức và giới
hạn subcase tại [kết quả](t18-giao-hang/ui-delivery-test-results.md).

## Bổ sung Bán hàng gọn —30/10/2026

[Phạm vi/plan UI POS](t16-pos-huy-don/sales-compact-ui-plan.md) theo ZIP
Orbit_BanHang_Gon đã soạn, chờ duyệt bổ sung. Menu/card ngang và cột đơn gọn,
POS chỉ Payable, breakdown tại F8; giữ nghiệp vụ/native/SQL/quyền. Chưa code,
sau triển khai duyệt đợt test riêng; không kế thừa PASS từ mẫu/hồ sơ cũ.
