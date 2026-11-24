# Tự chấm công bằng tài khoản nhân viên (hướng B) — requirements

Ngày soạn: 24/11/2026. Trạng thái: **bản nháp, chờ người dùng duyệt requirements**. Chưa viết plan, chưa sửa source, chưa nâng schema DB.

Đây là phần mở rộng phạm vi của G06, không thay đổi nội dung, thứ tự hay 33 ID nghiệm thu T24–T26. Hồ sơ liên quan:

- [Requirements G06](../requirements.md)
- [Báo cáo G06](../report.md)
- [Đánh giá nghiệp vụ/người dùng 24/11/2026](../review-nghiep-vu-nguoi-dung-20261124.md), mục B11 và D07
- [Kế hoạch tổng](../../../KeHoach_QuanLiQuanCafe.md), mục 8 và 12
- [UX tổng](../../../UX_QuanLiQuanCafe.md), mục 13

## 1. Mục tiêu

Cho nhân viên tự chấm vào ca/ra ca bằng **tài khoản của chính mình**, mỗi lần chấm đều phải nhập lại mật khẩu tài khoản đó; không cần vào màn quản trị Nhân sự, không cần quyền ADMIN, không tạo thêm phiên đăng nhập ứng dụng.

Kết quả mong đợi:

- Nhân viên chấm công ngay tại máy đặt ở quầy/điểm chấm, kể cả khi máy đang có người đăng nhập bằng tài khoản khác.
- Giờ vào/ra do SQL Server ghi theo giờ máy chủ; nhân viên không sửa được giờ, ngày, trạng thái hay dữ liệu của người khác.
- Quản lý vẫn là người duy nhất sửa/xóa công; dữ liệu tự chấm đi vào đúng CHAMCONG và bảng lương hiện có, không tạo nguồn giờ thứ hai.
- Có dấu vết đối soát: ai chấm, lúc nào, vào/ra, ca nào, mã yêu cầu; không lưu mật khẩu.

## 2. Hiện trạng và lý do

Hiện tại chỉ ADMIN mở được màn Nhân sự → Chấm công và nhập batch cho mọi nhân viên. CASH/STORE không có màn này; POS không ghi giờ. Vì vậy nhân viên không có cách tự chấm, và người dùng đã nêu đúng vấn đề: không thể bắt nhân viên đăng nhập ADMIN để chấm cho mình.

Hướng B được chọn: mỗi nhân viên đã có tài khoản; dùng tài khoản và **nhập mật khẩu mỗi lần chấm**. Tính năng này giải quyết D07 (không có tự chấm) và giảm B11 (không tìm thấy cách chấm); không sửa các bug B01–B10 đang chờ duyệt riêng.

## 3. Phạm vi

### 3.1. Điểm vào và quyền

- Thêm nút **“Chấm công”** ở hai nơi, dùng chung một hộp thoại:
  1. Màn đăng nhập: mở được khi cấu hình kết nối hợp lệ; đóng hộp thoại trở về đăng nhập, không tạo phiên, không vào ứng dụng.
  2. Cửa sổ chính: nút dạng quiet cho **mọi vai trò** (ADMIN/CASH/STORE), mở cùng hộp thoại; phiên hiện tại vẫn giữ nguyên sau khi đóng.
- Hộp thoại là Form modal có owner đúng (LoginForm hoặc MainForm), titlebar/nút X native, `ShowInTaskbar = false`; không nhúng thành panel/card.
- Không thêm quyền `AttendanceManage` cho CASH/STORE và không mở màn Nhân sự. Quyền chấm được xác nhận bằng mật khẩu tài khoản, không bằng role của phiên đang mở.
- Hộp thoại không hiển thị lương, hồ sơ, tài khoản khác, nút quản trị hay liên kết sang màn Nhân sự.

### 3.2. Luồng chấm công

1. Nhân viên mở “Chấm công”.
2. Chọn tên mình trong danh sách tài khoản đang hoạt động và nhân viên đang làm việc; nếu đang mở từ phiên của chính mình thì chọn sẵn.
3. Màn hình hiển thị trạng thái công hiện tại của tài khoản được chọn: chưa chấm hôm nay, đang trong ca nào từ lúc nào, hoặc các dòng công hôm nay. Chỉ hiển thị dữ liệu công của tài khoản đó.
4. Nhập mật khẩu tài khoản. Mỗi lần bấm chấm đều phải nhập lại; không lưu, không gợi ý, không dùng lại mật khẩu của lần trước.
5. Một hành động chính theo trạng thái:
   - Không có ca đang mở: chọn ca rồi **“Vào ca”**.
   - Có đúng một ca đang mở: **“Ra ca”**, không cần chọn ca.
   - Có nhiều ca mở (dữ liệu bất thường): chặn tự chấm, hướng dẫn nhờ quản lý.
6. SQL ghi giờ máy chủ, trả kết quả; hộp thoại hiển thị “Đã vào ca lúc …” hoặc “Đã ra ca lúc …”, ca, ngày, giờ vào/ra. Ô mật khẩu được xóa; lần chấm tiếp theo yêu cầu nhập lại.
7. Nhân viên đóng hộp thoại; phiên đăng nhập (nếu có) không thay đổi.

### 3.3. Quy tắc dữ liệu và thời gian

- Chỉ ghi trạng thái `Di lam`; nhân viên không tự khai `Nghi phep`/`Vang`, không tự sửa/xóa giờ, không tự tạo dòng đã hoàn tất.
- `NgayLam` khi vào ca là ngày máy chủ tại thời điểm vào. Ca qua đêm dùng ngày vào làm ngày công; khi ra sau nửa đêm, ca đang mở của ngày hôm trước được tìm thấy và đóng bình thường.
- Mỗi nhân viên tối đa một ca đang mở. Vào ca khi đang có ca mở bị chặn; ra ca khi không có ca mở bị chặn.
- Không ghi trùng khóa (nhân viên, ngày làm, ca). Không chấp nhận khoảng giờ chồng với ca khác, kể cả ca đã hoàn tất; ca nối tiếp đúng điểm cuối/đầu vẫn được phép.
- Ca được chọn từ CALAMVIEC; gợi ý ca đang trong khung giờ hiện tại nhưng không tự ghi nếu nhân viên chưa chọn/không chắc. Ca không tồn tại bị chặn.
- Ca đang mở quá **16 giờ** không cho tự ra ca; báo nhờ quản lý chỉnh để tránh tạo giờ công sai. Ngưỡng này là bảo vệ dữ liệu, không phải giới hạn ca làm việc thật.
- Kỳ đã chốt lương: chặn tự chấm theo trigger hiện có; thông báo rõ “Kỳ đã chốt lương, nhờ quản lý điều chỉnh”.
- Tài khoản bị khóa, nhân viên đã nghỉ, tài khoản không tồn tại hoặc không có tài khoản: không cho chấm; hướng dẫn liên hệ quản lý.
- Giờ lưu đến giây (`DATETIME2(0)`), không làm tròn phút; hiển thị giờ vào/ra đủ ngày và giây như màn chấm công hiện tại.
- Không tự động tính công, không tự điền giờ ca mẫu, không coi thiếu giờ ra là 0. Dòng đang mở vẫn là “Chưa hoàn tất” và đi vào kiểm tra trước khi chốt lương như hiện tại.

### 3.4. Xác thực và bảo mật

- Mỗi lần chấm, BLL đọc credential hiện có, kiểm mật khẩu bằng `PasswordService`/`PasswordHasher` hiện hành; không tạo mật khẩu/PIN mới, không lưu mật khẩu, không ghi mật khẩu vào log, session, grid hay thông báo lỗi.
- Không tạo `SessionContext` mới cho lần chấm; không cấp quyền ADMIN/CASH/STORE từ thao tác này. Danh tính dùng để ghi công là tài khoản vừa xác thực, không phải tài khoản đang mở ứng dụng.
- SQL kiểm lại tài khoản còn hoạt động, nhân viên còn làm việc và `SecurityStamp` còn khớp trước khi ghi; mật khẩu chỉ được kiểm ở BLL theo kiến trúc hiện có.
- Không dùng mật khẩu đã xác thực cho lần chấm sau; không giữ credential trong bộ nhớ sau khi hoàn tất. Ô mật khẩu dùng ký tự ẩn, có nút hiện/ẩn như đăng nhập.
- Giới hạn mềm ở hộp thoại: sau mỗi lần sai chờ ngắn, sau 5 lần sai liên tiếp tạm chặn 60 giây (chỉ trong phiên UI, không khóa tài khoản, không thêm cột/bảng đếm sai). Đây là giảm dò đoán cơ bản, không thay thế bảo vệ tài khoản.
- Nếu mở từ phiên đang đăng nhập, có thể ghi thêm tài khoản đang trực máy vào bảng đối soát; trường này chỉ để truy vết, không dùng để cấp quyền. Mở từ màn đăng nhập thì để trống.

### 3.5. Ghi nhận, chống gửi lặp và đối soát

- Mỗi lần chấm có `RequestId` ổn định, payload hash và bảng receipt riêng; retry cùng request/nội dung không tạo thêm dòng công, trả lại kết quả cũ. Cùng request nhưng khác nội dung bị từ chối.
- Receipt ghi tối thiểu: request, tài khoản xác thực, nhân viên, vào/ra, ca, ngày làm, giờ máy chủ, thời điểm ghi; không ghi mật khẩu/hash.
- Khóa theo nhân viên khi kiểm tra trạng thái cuối, dùng cơ chế `sp_HRLockEmployee`/khóa hiện có; hai máy chấm cùng lúc chỉ một thao tác thắng, thao tác còn lại nhận thông báo tải lại/đối chiếu.
- Không sửa CHAMCONG trực tiếp từ C#; procedure sở hữu transaction và kiểm tra overlap/khóa kỳ. Không gọi `sp_G03Demand 'ADMIN'` cho tự chấm.
- Quản lý vẫn dùng màn Chấm công hiện có để sửa/xóa dòng tự chấm; revision/concurrency giữ nguyên. Bảng lương, preview và chốt/chi không đổi công thức.
- Nguồn “tự chấm” được lưu ở receipt để đối soát. Bản MVP không thêm cột nguồn vào view/grid quản lý; nếu muốn hiển thị “Tự chấm/Quản lý” trên lưới thì làm ở phần mở rộng sau khi duyệt.

### 3.6. Trạng thái giao diện và lỗi

- Tách rõ: đang tải danh sách/trạng thái, chưa có tài khoản phù hợp, lỗi kết nối/schema, sai mật khẩu, tài khoản khóa/nghỉ, đang có ca mở, không có ca mở, trùng ca, chồng giờ, kỳ đã chốt, ca mở quá lâu, kết quả chưa xác định sau timeout.
- Lỗi giữ nguyên tài khoản/ca đã chọn, chỉ rõ chỗ cần sửa; không hiển thị mã lỗi SQL hay tên bảng/cột. Sai mật khẩu không tiết lộ tài khoản có tồn tại hay không.
- Đang gửi thì khóa nút và ô nhập để chống gửi lặp; không đóng hộp thoại giữa lúc chưa rõ kết quả. Sau timeout phải đối chiếu receipt/trạng thái trước khi cho chấm lại.
- Ô nhập dùng hành vi chọn hết khi vào ô (`OrbitField.SelectAllOnEntry`); Enter không tự gửi khi chưa có mật khẩu; không dùng phím tắt nào thay cho bước nhập mật khẩu.
- Hỗ trợ 1280×720, 1366×768 và DPI 100/125/150%; ưu tiên kiểm kỹ thuật ở 125%. Không ép app chạy một DPI cố định.

### 3.7. Không làm trong phạm vi này

- Không duyệt công nhiều cấp, không thêm trạng thái “Chờ duyệt”, không thêm workflow.
- Không làm kiosk PIN, vân tay/khuôn mặt, thẻ từ, mobile, web, offline sync, GPS/định vị.
- Không tự xếp ca, không chấm hộ không cần mật khẩu, không sửa giờ của người khác.
- Không đổi công thức lương, snapshot, chốt/chi lương, báo cáo doanh thu hay quyền T24–T26.
- Không thêm bảng lương tạm, nhiều chi nhánh, tài khoản dùng chung, hay cơ chế đăng nhập thứ hai.
- Không sửa các bug B01–B10 và không mở rộng sang màn Nhân sự khác.

## 4. Giả định

| Mã | Giả định |
|---|---|
| A1 | Có hai điểm vào (màn đăng nhập và cửa sổ chính) vì máy quầy thường đang có người đăng nhập; nếu chỉ muốn một điểm vào, chốt lại trước khi viết plan. |
| A2 | Chọn nhân viên theo danh sách tài khoản đang hoạt động + nhập mật khẩu; không bắt nhớ/username gõ tay. Danh sách chỉ gồm tài khoản HoatDong=1 và nhân viên DangLamViec=1. |
| A3 | Trạng thái công của tài khoản được chọn được đọc trước khi nhập mật khẩu để biết nên Vào ca hay Ra ca; chỉ đọc dữ liệu công của chính tài khoản đó, không đọc lương/hồ sơ. Nếu không muốn lộ trạng thái, chuyển sang yêu cầu mật khẩu trước khi xem. |
| A4 | Ghi thẳng vào CHAMCONG, không qua bước duyệt; quản lý sửa khi cần. Lý do: khớp T25 hiện có và không làm chậm quầy. |
| A5 | Ca mở quá 16 giờ bị chặn tự ra ca; quản lý chỉnh. Đây là ngưỡng bảo vệ dữ liệu, có thể đổi trước khi duyệt. |
| A6 | Nguồn tự chấm lưu ở bảng receipt; MVP không thêm cột nguồn vào CHAMCONG/view để tránh đổi mapping lưới hiện có. |
| A7 | Không thêm `AppPermission` mới; quyền chấm dựa trên mật khẩu tài khoản. Role của phiên đang mở không quyết định việc chấm. |
| A8 | SQL Server là nguồn giờ chính thức; hiển thị giờ Việt Nam theo cấu hình hiện có, giữ giây. |
| A9 | Migration mới đánh số tiếp theo sau 045 (dự kiến 046), chỉ chạy trên DB test khi được duyệt; không tự nâng DB development. |
| A10 | Agent không nghiệm thu hình thức/DPI bằng mắt; người dùng tự kiểm giao diện. Agent chỉ kiểm chức năng và layout kỹ thuật 125%. |

## 5. Câu hỏi mở cần chốt khi duyệt

1. **Điểm vào:** giữ cả màn đăng nhập và cửa sổ chính (A1) hay chỉ màn đăng nhập?
2. **Xem trạng thái trước khi nhập mật khẩu:** chấp nhận đọc trạng thái công của tài khoản được chọn (A3), hay bắt buộc nhập mật khẩu mới xem?
3. **Ngưỡng ca mở quá lâu:** 16 giờ (A5) có phù hợp ca qua đêm/thực tế quán không?
4. **Hiển thị nguồn tự chấm:** chỉ lưu receipt (A6) hay cần thêm cột “Tự chấm/Quản lý” trên lưới chấm công ngay trong đợt này?

Nếu người dùng không đổi, các giả định trên được dùng để viết plan.

## 6. Ảnh hưởng kỹ thuật dự kiến (mức requirements, chưa phải plan)

- **SQL:** migration mới (dự kiến 046) thêm bảng receipt tự chấm, procedure đọc danh sách/trạng thái và procedure chấm vào/ra; cấp `EXECUTE` cho login ứng dụng, `DENY` DML trực tiếp; dùng `sp_HRLockEmployee` và trigger khóa kỳ hiện có. Không đổi công thức lương.
- **Models:** request/kết quả chấm tự chấm, dòng trạng thái; không đưa mật khẩu vào DTO lưu lâu.
- **DataAccess:** repository mới hoặc mở rộng repository chấm công; gọi procedure có tham số, không SQL trong UI.
- **Business:** service xác thực lại mật khẩu (dùng AuthRepository/PasswordService hiện có), kiểm tra đầu vào, không tạo session; dịch lỗi qua `SqlErrorMapper`.
- **WinForms:** `SelfAttendanceDialog` dùng Orbit/AppTheme; nút ở LoginForm và MainForm; wiring service trong `Program`/`ApplicationSessionContext`; không đổi menu/role hiện có.
- **Tài liệu:** cập nhật liên kết ở report G06 và đánh giá 24/11 sau khi triển khai; không sửa ID T24–T26.

## 7. Tiêu chí hoàn thành và ca nghiệm thu đề xuất

Chỉ ghi Hoàn tất khi source/migration và các ca bắt buộc đã PASS trên SQL Server thật, DB test riêng; hình thức do người dùng kiểm.

### 7.1. Chức năng và dữ liệu

- [ ] `SA01` Mở “Chấm công” từ màn đăng nhập; không tạo phiên, không vào ứng dụng; đóng trở về đăng nhập.
- [ ] `SA02` Mở từ cửa sổ chính bằng ADMIN/CASH/STORE; phiên và màn hiện tại giữ nguyên sau khi đóng; không lộ màn Nhân sự.
- [ ] `SA03` Danh sách chỉ có tài khoản hoạt động + nhân viên đang làm; tài khoản khóa/nghỉ/không có không chọn được và bị chặn nếu gọi trực tiếp.
- [ ] `SA04` Sai mật khẩu/rỗng không ghi công, không tiết lộ tài khoản tồn tại; có chờ/lock mềm theo A; đúng mật khẩu mới tiếp tục.
- [ ] `SA05` Vào ca khi không có ca mở: ghi đúng nhân viên, ca, ngày máy chủ, giờ vào máy chủ, trạng thái `Di lam`; không ghi giờ do client gửi.
- [ ] `SA06` Ra ca khi có ca mở: đóng đúng dòng, giờ ra > giờ vào; ca qua đêm ra sau nửa đêm đóng đúng dòng ngày hôm trước.
- [ ] `SA07` Mỗi lần chấm kế tiếp bắt buộc nhập lại mật khẩu; mật khẩu được xóa sau khi thành công, không lưu session/log/grid.
- [ ] `SA08` Vào trùng ca/ngày bị chặn; vào khi đang có ca mở bị chặn; ra khi không có ca mở bị chặn.
- [ ] `SA09` Khoảng giờ chồng với ca khác bị chặn; ca nối tiếp đúng điểm cuối/đầu được phép.
- [ ] `SA10` Ca mở quá 16 giờ không cho tự ra; báo nhờ quản lý; quản lý sửa được bằng màn hiện có.
- [ ] `SA11` Kỳ đã chốt lương: tự chấm bị chặn ở SQL; thông báo đúng, không ghi một phần.
- [ ] `SA12` Retry cùng RequestId/nội dung không tạo dòng thứ hai; cùng RequestId khác nội dung bị từ chối; timeout có đường đối chiếu.
- [ ] `SA13` Hai session/máy chấm cùng lúc: chỉ một thao tác thắng; bên còn lại nhận thông báo tải lại/đối chiếu, dữ liệu cuối đúng.
- [ ] `SA14` Quản lý sửa/xóa dòng tự chấm theo revision như dòng thường; bảng lương preview dùng đúng giờ; dòng thiếu giờ ra vẫn bị đánh dấu chưa hoàn tất; công thức lương không đổi.
- [ ] `SA15` Schema thiếu migration: báo “CSDL chưa cập nhật”, không crash, không ghi công; sau khi áp migration chạy lại được.

### 7.2. Giao diện và vận hành

- [ ] `SA16` Loading/rỗng/lỗi/xung đột/kết quả chưa xác định tách rõ; giữ input khi lỗi; nút bị khóa khi đang gửi; không đóng giữa lúc chưa rõ kết quả.
- [ ] `SA17` Hộp thoại có owner đúng, titlebar/X native, `ShowInTaskbar=false`; đóng bằng X/Esc không ghi dữ liệu; focus và thứ tự tab hợp lý; ô nhập chọn hết khi vào ô.
- [ ] `SA18` Layout kỹ thuật đúng ở 1280×720, 1366×768 và DPI 125%; DPI 100/150% ghi NOT RUN nếu chưa chạy; hình thức/typography/màu do người dùng kiểm, ghi SKIPPED theo phân công.
- [ ] `SA19` Không có mật khẩu/hash trong UI, log, session, grid, thông báo hay tài liệu; không có đường tắt bỏ qua mật khẩu.

### 7.3. Kiểm thử

- SQL integration trên SQL Server thật, DB test riêng; hai session cho trùng/đồng thời; đối chiếu CHAMCONG, receipt, trạng thái và bảng lương sau thao tác.
- xUnit cho quy tắc tách được ở BLL (chọn ca/trạng thái/request) nếu có giá trị; không test getter hay lặp CRUD.
- WinForms thật cho binding, bàn phím, validation, loading/lỗi, quyền và handler; agent không tự chạy test thị giác/Designer.
- Lưu `test-results.md` cạnh hồ sơ này khi được duyệt đợt test; không tick trước khi chạy.

## 8. Rủi ro và cách giảm

| Rủi ro | Giảm thiểu |
|---|---|
| Nhập mật khẩu mỗi lần gây chậm/mất kiên nhẫn | Một hành động chính, chọn sẵn tài khoản khi mở từ phiên của mình, gợi ý ca, thông báo kết quả ngắn; không thêm bước phụ. |
| Lộ trạng thái công của đồng nghiệp (A3) | Chỉ đọc trạng thái của tài khoản đang chọn, không lương/hồ sơ; nếu không chấp nhận, chuyển sang yêu cầu mật khẩu trước khi xem. |
| Dò mật khẩu tại máy chấm | Chờ/lock mềm ở UI, mật khẩu hash hiện có, không tiết lộ tài khoản tồn tại; không thay đổi cơ chế đăng nhập. |
| Hai máy chấm cùng lúc | Khóa theo nhân viên + kiểm tra trạng thái cuối + RequestId; thông báo đối chiếu thay vì ghi đè. |
| Ca mở quên ra ca tạo giờ sai | Chặn tự ra sau 16 giờ; quản lý chỉnh; payroll vẫn đánh dấu chưa hoàn tất. |
| Tự chấm vượt quyền/kỳ chốt | Procedure tự kiểm tài khoản/nhân viên/khóa kỳ, không dựa vào UI; không dùng `sp_G03Demand 'ADMIN'`. |
| Thay đổi mapping lưới chấm công | MVP chỉ thêm bảng receipt, không đổi view/DTO lưới hiện có. |

## 9. Việc tiếp theo

1. Người dùng duyệt hoặc sửa requirements này (đặc biệt 4 câu hỏi mở).
2. Sau khi duyệt, viết `docs/tasks/g06-nhan-su-cong-luong/self-attendance/plan.md` theo SQL → DAL → BLL → UI, kèm migration/receipt/procedure/test; chờ duyệt plan.
3. Chỉ sau khi duyệt plan mới sửa source/migration; không tự nâng DB development.
4. Triển khai xong chờ duyệt đợt test riêng; không tự chạy test hoặc ghi PASS.
