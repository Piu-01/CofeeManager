# Hướng dẫn agent — CoffeeManager

## 1. Ngữ cảnh và tài liệu bắt buộc

Trao đổi với người dùng bằng tiếng Việt. Dự án quản lý một quán cafe, một kho chung,
C# WinForms **.NET 10.0 LTS** và SQL Server; một người triển khai tuần tự.

Các đường dẫn dưới đây tính từ gốc CoffeeManager, không phụ thuộc đường dẫn máy tác giả.

| Khi nào | Phải đọc |
|---|---|
| Bắt đầu phiên làm việc, trước lập requirements/plan cho task lớn | `docs/KeHoach_QuanLiQuanCafe.md`: kiến trúc, phạm vi, mục 12 và phụ thuộc T01–T32, mục 13 về trạng thái |
| Trước code bất kỳ module/use case | Đọc lại phần nghiệp vụ và task tương ứng trong kế hoạch; `docs/UX_QuanLiQuanCafe.md` phần luồng, trạng thái, contract và ca UX liên quan |
| Trước thiết kế hoặc sửa Form/UserControl/dialog/theme | `docs/Cafe_UI_Style_Guide/Cafe_UI_Style_Guide/README.md` và `index.html`; xem `preview.html` để tham chiếu bố cục; đọc `ASSET_SOURCES.md` khi dùng asset |
| Trước thay SQL hoặc gọi contract dữ liệu | `database/DB_Schema.sql`, `database/Insert_Data.sql` và migrations đã có của task |

**Lưu ý đường dẫn:** bộ UI guide hiện có hai cấp `Cafe_UI_Style_Guide/` lồng nhau.
Kế hoạch quy định nghiệp vụ/task; UX quy định luồng; style guide quy định hình thức.
Khi mâu thuẫn, nêu rõ và chốt theo nghiệp vụ, cập nhật tài liệu liên quan trong phạm vi
được duyệt. Preview HTML chỉ mô phỏng local, không phải app hay chứng cứ test WinForms.
Không coi procedure/trường ghi “bổ sung/sửa” trong kế hoạch là đã tồn tại.

## 2. Skill cục bộ và cách chọn

Skill được cài tại `.agents/skills/`, có thể được OpenCode và công cụ hỗ trợ thư mục
này phát hiện. Khi cần dùng, nạp bằng skill tool theo ID; nếu công cụ không hỗ trợ,
đọc trực tiếp `SKILL.md` theo đường dẫn trong bảng. Chỉ nạp skill phù hợp công việc.

| Skill / file | Khi áp dụng | Kết quả cần có |
|---|---|---|
| `karpathy-guidelines` — `.agents/skills/karpathy-guidelines/SKILL.md` | Viết, review, refactor code | Giả định rõ, giải pháp tối giản, thay đổi đúng phạm vi, mục tiêu kiểm chứng được |
| `task-approval-workflow` — `.agents/skills/task-approval-workflow/SKILL.md` | Nhóm lớn G01–G08 và bộ plan task con | Duyệt requirements nhóm → duyệt cả bộ plan → triển khai tuần tự → duyệt đợt test |
| `bug-investigation-workflow` — `.agents/skills/bug-investigation-workflow/SKILL.md` | Người dùng báo lỗi hoặc phát hiện test FAIL | Điều tra, ghi bug tại task → duyệt cả đợt bug → debug/regression |
| `post-change-test-workflow` — `.agents/skills/post-change-test-workflow/SKILL.md` | Sau triển khai nhóm hoặc đợt debug | Chờ duyệt đợt test → test từng task → lưu kết quả → báo cáo chung |

Quy trình này bổ sung vào chỉ dẫn hệ thống/công cụ; nếu có xung đột, tuân thủ chỉ dẫn
cấp cao hơn và nói rõ bước cần điều chỉnh. Bản skill kiểm thử đã được chuyển từ Roblox
sang WinForms/SQL Server; không cần Studio, Rojo hay skill Roblox khác.

### Nhóm lớn và task con — quy trình người dùng chốt ngày 08/10/2026

- Nhóm theo bảng G01–G08 trong mục 12 của kế hoạch; giữ nguyên toàn bộ chi tiết,
  phụ thuộc, thứ tự T01–T32 và 325 ca kiểm thử. Một task con đang làm tại một thời điểm.
- **Một requirements cho nhóm lớn**, đặt ở `docs/tasks/gXX-<slug>/requirements.md`:
  mục tiêu, các task con, phạm vi/đầu ra/ca nghiệm thu tham chiếu nguyên bản,
  giả định và tiêu chí hoàn tất. Chờ người dùng duyệt một lần cho nhóm.
- **Viết đủ plan của nhóm** trước triển khai, mỗi plan ở
  `docs/tasks/gXX-<slug>/tXX-<slug>/plan.md`, chỉ mục ở `plans.md` của nhóm.
  Nêu bước/file/migration/fixture/test; chờ duyệt **cả bộ plan một lần**.
  Không tạo requirements task con mới hoặc xin duyệt plan từng task riêng.
- Sau duyệt bộ plan, triển khai từng task theo thứ tự phụ thuộc. Đầu ra task
  trước là đầu vào task sau; chưa nghiệm thu PASS trong pha triển khai. Thiếu
  đầu ra/blocker phụ thuộc thì ghi rõ và hỏi quyết định nếu chặn task sau.
- Triển khai xong nhóm → chờ người dùng **duyệt đợt test một lần** → test từng
  task, lưu biên bản trong task → báo cáo chung ở `report.md`, kể cả còn FAIL.
  Duyệt plan không đồng nghĩa duyệt chạy test hoặc debug.
- Bug ghi tại task sở hữu; liên kết các bug trong báo cáo nhóm. Chờ người dùng
  **duyệt cả đợt bug một lần** rồi debug lần lượt và regression theo ảnh hưởng.
  Bug mới ngoài đợt đã duyệt phải báo để duyệt bổ sung, không tự vá.
- T01 có 7/7 PASS, T02 có 12/12 PASS được kế thừa từ quy trình cũ. Giữ nguyên
  lịch sử test/bug; không làm lại chỉ vì đổi cấu trúc. Requirements task con cũ
  giữ làm tham chiếu, không là gate. Không báo cáo hoàn thành riêng từng task.
- Ngoài phạm vi/đổi nghiệp vụ/contract thì cập nhật và xin duyệt phần phát sinh;
  không tự mở rộng. Việc nhỏ rõ ràng không cần tạo nhiều bước duyệt.

### Bug và kiểm thử

- Tận dụng source, log và dữ liệu có sẵn trước khi hỏi; phân biệt nguyên nhân đã có
  bằng chứng với giả thuyết. Không bịa log, input hay kết quả tái hiện.
- Bug của task: `docs/tasks/gXX-<slug>/tXX-<slug>/bug-report.md`; bug độc lập:
  `docs/bugs/<slug>/report.md`. Ghi hiện tượng, nguyên nhân/bằng chứng, đề xuất sửa,
  phạm vi ảnh hưởng và rủi ro thực tế; tối ưu ngoài nguyên nhân chỉ là đề xuất riêng.
- Lỗi nội bộ/độc lập đều điều tra có bằng chứng trước sửa. Trong nhóm, gom bug
  vào đợt để người dùng duyệt một lần; không xin duyệt từng fix đã thuộc đợt.
  Đổi scope/contract và bug phát sinh mới cần duyệt bổ sung.
- Sau triển khai chờ duyệt đợt test riêng. Duyệt debug bao gồm regression của
  các bug đã duyệt. Mỗi lượt test giữ source cố định; ghi FAIL và lưu bằng chứng,
  không sửa source/migration trong lượt test hoặc chỉnh expected để né lỗi.
- Test giao dịch trên **SQL Server thật, DB test riêng**: đối chiếu tiền, kho, điểm,
  nợ, trạng thái, bảng liên kết và transaction sau lỗi; dùng hai session cho ca đồng
  thời. Không thay engine bằng SQLite, không ghi test vào DB sử dụng thật.
- Unit test dùng xUnit cho quy tắc có giá trị; không test getters hay sao chép mọi
  dòng CRUD. UI cần thao tác app thật: Designer, resize/DPI, bàn phím, loading/lỗi.
- Lưu `test-results.md` cạnh hồ sơ task/bug: mã ca, môi trường và schema/commit,
  dữ liệu, lệnh/thao tác, expected/actual, PASS/FAIL/BLOCKED/NOT RUN và bằng chứng.
  Không tick test chưa chạy; thiếu SDK/SQL/GUI ghi rõ blocker.
- Chỉ ghi Hoàn tất khi đủ đầu ra và các ca bắt buộc đã PASS; lỗi chưa xử lý giữ
  Đang làm. Regression theo ảnh hưởng thực tế của thay đổi.

## 3. Kiến trúc và quy tắc code

- `src/CafeManager.WinForms`: Form/UserControl, binding, điều hướng, loading, in.
- `src/CafeManager.Business`: quyền, xác thực, chuẩn hóa request, điều phối use case,
  dịch lỗi; không tham chiếu WinForms hoặc chứa MessageBox/control.
- `src/CafeManager.DataAccess`: ADO.NET `Microsoft.Data.SqlClient`, query có tham số,
  TVP, gọi procedure, ánh xạ DTO; không tính lại số dư/nghiệp vụ chính thức.
- `src/CafeManager.Models`: entity/request/DTO/trạng thái; không tham chiếu UI/SqlClient.
- `Program.cs` nối dependencies; không tạo generic repository/UnitOfWork, interface
  rỗng hay bộ khung mọi module tương lai. Không tự thêm EF Core/Dapper/API server.
- WinForms target `net10.0-windows`; kiểm chứng SDK/Designer ở T01, không tự đổi
  framework. Hash mật khẩu bằng `Microsoft.Extensions.Identity.Core`.
- Query async, dispose connection đúng hạn; không trả SqlDataReader/SqlTransaction
  lên form. UI không chứa SQL, `.Designer.cs` không chứa logic nghiệp vụ.
- Tiền/định mức C# dùng `decimal`, ly/identity dùng `int`; SQL tiền `decimal(18,0)`,
  lượng `decimal(18,4)`. SDT là chuỗi; input tùy chọn trống thành NULL/DBNull theo contract.
- BLL kiểm tra quyền thao tác, không chỉ ẩn nút. Nhân viên lấy từ session; chức vụ
  và vai trò ADMIN/CASH/STORE độc lập. Không giữ mật khẩu trong session/log.
- Giữ naming/style hiện có, chỉ sửa dòng phục vụ yêu cầu; chỉ dọn phần chính thay
  đổi của mình làm dư thừa. Không refactor khu vực liên quan theo cảm tính.

## 4. SQL và bất biến nghiệp vụ

Hai file `database/` là baseline đính kèm cho `QuanLyCafe_ERP_V2`. Chạy schema trước
seed; seed chỉ chạy một lần trên schema rỗng. Đọc source không đồng nghĩa đã kiểm
chứng bằng engine. Mở rộng bằng migration có version theo task.

- Task có SQL: triển khai theo thứ tự SQL → DAL → BLL → UI; integration kiểm
  chứng trong đợt test được duyệt trước nghiệm thu. Nếu thiếu contract/đầu ra
  chặn lớp sau thì báo blocker. Không viết contract tương lai trước task.
- Nghiệp vụ nhiều bảng/số dư chạy trong procedure sở hữu transaction; C# gọi trực
  tiếp, không bọc transaction dư hoặc ghi chốt tài chính bằng UPDATE rời.
- Dùng TVP cho nhiều dòng; trigger đang cập nhật tồn/điểm/tổng thì procedure/app
  không cập nhật lại. Không giữ transaction khi nhân viên nhập, đếm kho hoặc in.
- Tổng món từ giá snapshot dòng; phải trả = tổng món + ship − giảm. KM thay ưu đãi
  hạng, không cộng dồn; giảm không áp dụng ship. Thiết kế đích tính chính thức ở SQL.
- Trừ kho theo công thức khi thanh toán, snapshot tiêu hao không đổi theo công thức
  mới. Hủy đơn mở không hoàn kho; đồ đã pha bị hủy ghi xuất hủy lượng thực dùng.
- Một kho chung, không xuất chuyển ra quầy. Không sửa trực tiếp tồn/điểm sau mở sổ.
- Đọc `vw_TrangThaiBan` và `vw_CongNoNCC`; không giả định còn `BAN.TrangThai` hay
  `CONGNO_NCC.MaNCC/TrangThai`. Dòng món theo `MaDong`, thanh toán theo `MaTT`.
- Đơn đã trả/hủy khóa tài chính; chỉ chuyển giao đúng contract. Không sửa computed
  columns `ThanhTien`/`ThucLinh`. Không sửa/xóa lịch sử điểm/tiêu hao/trả nợ.
- Trả nợ đã tạo phiếu chi qua procedure, không ghi thêm. Chốt lương khác chi lương.
  Checkout không tạo phiếu Thu thứ hai rồi nhân đôi doanh thu.
- Timeout sau ghi: đối chiếu mã nguồn/trạng thái trước retry. In sau commit; lỗi in
  chỉ in lại. Khóa gửi lặp và xử lý xung đột từ dữ liệu DB, không tin cache UI.
- PT010 đổi điểm nằm ngoài phạm vi. Không tự thêm hoàn tiền, size/topping, đặt bàn,
  gộp/tách bill, online payment, nhiều chi nhánh hoặc offline sync.
- Báo cáo doanh thu theo ngày thanh toán; aggregate món và thanh toán độc lập trước
  JOIN. Không gọi thu/chi là lợi nhuận chính xác khi chưa có giá vốn lịch sử.
- Tối ưu bằng actual plan/IO/TIME và số đo ở T29; không dùng NOLOCK cho tiền/kho.

## 5. UI dùng chung và UX bắt buộc

- Style theo guide Espresso/Kem/Caramel: sidebar `#241814`, nút chính `#4B2E23`,
  hover `#633E2D`, nhấn `#C58B52`, nền `#F5EFE6`, panel `#FFFCF8`, chữ `#291E18`,
  viền `#DDD0C2`, chọn `#EAD8C3`. Tập trung màu/font/spacing vào AppTheme khi T07.
- Segoe UI 10–11pt, tiêu đề theo guide; native controls và helper nhỏ trước custom
  control. Dock/Anchor/TableLayoutPanel; kiểm tra 1280×720, 1366×768 và DPI 100/125/150%.
- MainForm + UserControl; dialog editor, một hành động chính mỗi bước. Menu và màn
  bắt đầu theo vai trò; nháp hỏi khi rời, rời POS không tự hủy đơn đã lưu.
- Tách loading/rỗng/lỗi/xung đột/kết quả chưa biết/đã chốt. Lỗi DB không hiển thị
  tổng 0 giả; giữ input khi lỗi, ErrorProvider gần trường, focus/tab rõ.
- Trạng thái có chữ/icon cùng màu. Tiền căn phải và định dạng `81.000 ₫`, ngày
  `dd/MM/yyyy`, giờ `HH:mm`; truyền giá trị typed, không truyền chuỗi đã format vào SQL.
- Enter không tự chốt tiền/kho/nợ/lương. F2/F3/F4/F8 chỉ theo contract UX, không
  trực tiếp checkout; modal/loading chặn phím gây gửi lặp.
- Tái sử dụng PNG/Resources theo kích thước DPI, giữ license; ảnh demo ghi đúng
  tính chất demo, chỉ copy asset cần dùng. Không nhúng đường dẫn tuyệt đối hoặc
  phụ thuộc tải Internet để app hoạt động. Guide vẫn cần nghiệm thu trên WinForms thật.

## 6. Git và báo cáo bàn giao

Kiểm tra `git rev-parse --show-toplevel` trước thao tác Git. T01 đã khởi tạo repo
riêng tại CoffeeManager trên nhánh `main`; root phải là thư mục CoffeeManager,
không phải repo cha `D:/Workspace`. Không tự init/move repo ngoài scope đã duyệt.
Không `git add .`/commit cả Workspace hoặc sửa các dự án anh em; stage đúng file
CoffeeManager nếu đã được giao thao tác Git. Không commit credentials, bin/obj,
cấu hình local, backup nhạy cảm. Không reset/clean thay đổi của người dùng.

Bàn giao ngắn gọn: phần đã làm, đường dẫn chính, kết quả kiểm chứng đã chạy và phần
BLOCKED/NOT RUN, trạng thái task hoặc bước đang chờ duyệt. Không tự đánh dấu T01–T32
hoàn tất từ việc cài skill/tạo tài liệu. Chỉ commit khi người dùng giao/yêu cầu.
