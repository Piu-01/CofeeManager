# Đa ngôn ngữ VI/EN — requirements

Ngày soạn: 09/11/2026. Requirements đã được duyệt theo ghi nhận trong `plan.md`.
Ngày 10/11/2026 người dùng giao tiếp tục code theo requirements/plan này.
Trạng thái: **Đang làm — LOC-B01–B03 đã sửa/regression PASS; nghiệm thu native còn thiếu**.
Đây là tính năng mới ngoài phạm vi kế hoạch T01–T32/G01–G08.

## 1. Mục tiêu

Bổ sung tiếng Anh bên cạnh tiếng Việt để người dùng chọn được ngôn ngữ giao diện.
Mặc định vẫn là tiếng Việt; dữ liệu nghiệp vụ trong DB giữ nguyên.

## 2. Hiện trạng (số liệu khảo sát trên source hiện tại)

- Khoảng **1.666 dòng chứa chuỗi tiếng Việt** trong **127 file** `.cs` (đếm bằng regex,
  có thể lệch vài dòng do chuỗi nhiều dòng):
  - `CafeManager.WinForms`: 1.359
  - `CafeManager.DataAccess`: 161 (chủ yếu `SqlErrorMapper`)
  - `CafeManager.Business`: 122
  - `CafeManager.Models`: 24 (`SalesStates.Label`, ...)
- Chưa có hạ tầng i18n. Chỉ có 1 `.resx` (`StartupForm` của Designer), không có
  `ResourceManager`, `CurrentUICulture` hay file từ điển.
- Chuỗi nằm rải ở: `.Designer.cs`, code khởi tạo control, tiêu đề cột grid, empty state,
  dialog xác nhận, validation `ErrorProvider`, message lỗi BLL/DAL, màn hình in hóa đơn.
- Định dạng tiền `81.000 ₫`, ngày `dd/MM/yyyy`, `N0` và `vi-VN` đang hardcode ở khoảng
  24 file.
- Shell `MainForm` cache các module `UserControl`; POS giữ nháp đơn hàng trong bộ nhớ.
- Trạng thái/nhãn nghiệp vụ do app map từ mã (`SalesStates.Label`, `G05Display...`),
  phần lớn **không** nằm trong DB, nên dịch được ở tầng app.

## 3. Phạm vi

### Làm

1. Hạ tầng đa ngôn ngữ dùng chung: chọn `vi`/`en`, tra từ điển, fallback về tiếng Việt
   khi thiếu bản dịch (không hiện key kỹ thuật), không phụ thuộc culture của Windows.
2. Dịch toàn bộ chuỗi **do app sở hữu** trên đường hiển thị:
   - Đăng nhập, Startup, Bootstrap, dialog cấu hình DEV;
   - Shell: menu, nhóm menu, tiêu đề workspace, breadcrumb, trạng thái phiên, nút;
   - Toàn bộ module/quản trị: danh mục, menu/công thức, bàn, khách, ưu đãi/điểm, POS,
     hóa đơn, giao hàng, thu chi, công nợ, kho (nhập/xuất/kiểm kê), nhân sự, lương,
     tài sản, báo cáo, thông tin hệ thống;
   - Mọi dialog thêm/sửa/chọn/xem chi tiết/xác nhận, empty state, loading, lỗi,
     validation, thông báo đối chiếu/unknown;
   - Tiêu đề cột grid, nhãn trạng thái, đơn vị, tooltip/accessible name do app đặt;
   - In hóa đơn và preview (kể cả tiêu đề, cột, câu ghi chú in);
   - Thông báo lỗi từ `SqlErrorMapper`/BLL mà người dùng nhìn thấy.
3. Bộ chọn ngôn ngữ ở hai nơi:
   - Màn đăng nhập (trước khi vào app);
   - Mục trong "Thông tin hệ thống" (hoặc vị trí tương đương trong app).
4. Lưu lựa chọn bền theo **Windows user** bằng file cấu hình local (cùng cơ chế thư mục
   `%LocalAppData%\CoffeeManager` đang dùng), không ghi DB, không thêm migration.
5. Mặc định `vi`; dữ liệu DB/tên riêng/mã chứng từ không dịch.
6. Test: unit test hạ tầng dịch (fallback, parity từ điển, định dạng) và audit tự động
   phát hiện chuỗi tiếng Việt còn sót trên đường hiển thị khi ở chế độ EN.

### Không làm

- Không dịch dữ liệu nghiệp vụ trong DB: tên món/danh mục/bàn/khu vực/khách/nhà cung cấp/
  nhân viên, ghi chú người dùng nhập, tên chương trình khuyến mãi.
- Không đổi công thức, contract SQL, procedure, bảng, cột hay thêm cột ngôn ngữ vào DB.
- Không đổi nghiệp vụ, quyền, luồng POS/thanh toán/kho/lương/công nợ.
- Không đổi định dạng tiền `81.000 ₫` và ngày `dd/MM/yyyy` (xem câu hỏi 3).
- Không dịch thương hiệu `CoffeeManager`, mã `PT001/HD...`, log kỹ thuật, tên file.
- Không thêm ngôn ngữ thứ ba, không tự đổi ngôn ngữ theo OS, không dịch tự động online.
- Không đổi hình thức Orbit (màu/font/spacing/icon); chỉ thay chữ.
- Không sửa/xóa lịch sử test, checklist T01–T32 hay hồ sơ G08.

## 4. Cách tiếp cận đề xuất (để duyệt hướng, chi tiết nằm ở plan)

- Một `LanguageService` + từ điển `VI → EN` đặt ở tầng thấp (`CafeManager.Models`),
  dùng được cho cả WinForms/Business/DataAccess; key là chính câu tiếng Việt để giảm
  việc đặt tên ~1.700 key và để fallback an toàn. Hàm `L.T("...")` cho chuỗi thường,
  `L.F("...{0}...", args)` cho chuỗi nội suy.
- Chỉ bọc chuỗi **hiển thị**; không bọc chuỗi dùng làm khóa so sánh/logic (`"Dang ban"`,
  `"ADMIN"`, mã trạng thái, tên cột `DataPropertyName`, `Name` control).
- Audit bằng script đếm chuỗi tiếng Việt chưa bọc trong vùng hiển thị + rà soát thủ công
  theo từng màn; test đối chiếu từ điển không thiếu/thừa.
- Áp dụng thay đổi theo từng lát cắt màn hình để regress được, không gộp một lần mù.

## 5. Giả định

- Tiếng Anh dùng chuẩn Mỹ, thuật ngữ quán cà phê phổ thông (Order, Checkout, Stock,
  Purchase, Stocktake, Payroll, Asset...).
- Mã trạng thái/lý do trong DB (`Dang ban`, `Tai quan`, ...) vẫn giữ nguyên trong DB và
  chỉ đổi nhãn hiển thị.
- Định dạng tiền/ngày giữ nguyên ở cả hai ngôn ngữ vì là quy ước nghiệp vụ VND.
- Đổi ngôn ngữ không làm thay đổi dữ liệu, quyền hay phiên đang đăng nhập.
- Không cần dịch tài liệu `docs/`; chỉ dịch chuỗi trong app.

## 6. Câu hỏi mở cần chốt trước khi viết plan

Đã chốt theo phần mở đầu `plan.md`: chọn (B), dịch cả in/lỗi, giữ định dạng VND/ngày,
lưu theo Windows user, triển khai đủ hai đợt nội dung. Các câu hỏi dưới đây được giữ
làm lịch sử đề xuất, không còn là blocker hoặc gate duyệt lại.

**Cập nhật 10/11/2026:** người dùng duyệt đổi phương án áp dụng sang (A) — đổi ngay
trong phiên, chỉ chặn khi có nháp/busy. Chi tiết tại
`docs/tasks/localization-live-apply/`. Nội dung dịch, lịch sử test và kết quả
LOC-B01–B03 giữ nguyên, không làm lại.

1. **Cơ chế áp dụng ngôn ngữ** — chọn một:
   - (A) Đổi ngay trong phiên: dựng lại shell + module đang mở; phải chặn khi có nháp
     POS/busy, rủi ro cao hơn, công nhiều hơn.
   - (B) **Đề xuất**: đổi ngay ở màn đăng nhập/startup; trong app chọn xong thì yêu cầu
     đăng xuất hoặc khởi động lại để áp dụng. An toàn với nháp POS, dễ kiểm chứng.
2. **Có dịch phần in hóa đơn/preview và thông báo lỗi kỹ thuật không?** Đề xuất: có,
   vì người dùng vẫn nhìn thấy.
3. **Có giữ định dạng `81.000 ₫` và `dd/MM/yyyy` khi ở tiếng Anh không?** Đề xuất: giữ
   để không lệch quy ước VND và test hiện có.
4. **Lưu lựa chọn theo Windows user (đề xuất, không đụng DB) hay theo tài khoản nhân
   viên trong DB (cần migration + quyền)?**
5. **Có cần chia đợt không?** Nếu muốn có bản dùng sớm, đề xuất đợt 1 gồm đăng nhập +
   shell + POS/thanh toán + lỗi dùng chung; đợt 2 các module còn lại + báo cáo/in.

## 7. Tiêu chí hoàn thành

- Chọn EN ở màn đăng nhập → các màn hình trong phạm vi hiển thị tiếng Anh, không còn
  chuỗi tiếng Việt do app sở hữu (trừ dữ liệu DB, thương hiệu, mã).
- Chọn VI → giao diện và hành vi trở lại đúng như hiện tại.
- Lựa chọn được lưu và giữ sau khi đóng/mở lại app.
- Không thay đổi nghiệp vụ: POS, thanh toán, kho, công nợ, lương, báo cáo, quyền,
  phím tắt, validation, luồng nháp/unknown hoạt động như cũ; regression theo module bị chạm.
- Từ điển VI/EN cân bằng (không thiếu key dùng trên đường hiển thị, không key chết),
  thiếu bản dịch thì fallback tiếng Việt chứ không hiện key.
- Build Debug/Release pass; unit test hạ tầng dịch pass.
- Không phát sinh migration/contract SQL, không ghi dữ liệu test vào DB sử dụng thật.

## 8. Ảnh hưởng tới G08 và lịch sử kiểm thử

- Thay đổi chạm hầu hết file UI nên **làm mất hiệu lực bằng chứng "source cố định"** của
  các đợt test G08/T31–T32 trước đó; không được suy PASS từ test cũ.
- Sau khi triển khai cần một **đợt test riêng** (chờ duyệt như quy trình hiện hành), gồm
  regression chức năng theo module bị chạm và kiểm tra VI/EN, lưu biên bản trong
  `docs/tasks/localization-vi-en/`.
- Không thay checklist 325 ID/T01–T32; báo cáo G08 hiện tại giữ nguyên làm lịch sử,
  chỉ ghi chú rằng bằng chứng bàn giao cần chạy lại sau thay đổi này.
- Hình thức giao diện vẫn do người dùng tự kiểm (agent chỉ kiểm chức năng/layout kỹ thuật
  ở DPI 125%).
