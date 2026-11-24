# G06 — Rà soát luồng nghiệp vụ màn Nhân sự dưới góc nhìn người dùng

Ngày: 24/11/2026. Trạng thái: **báo cáo rà soát, chưa sửa source, chưa chạy test/GUI**.
Đợt này không thay đổi kết quả 33 ID của G06 (26 PASS / 0 FAIL / 7 BLOCKED) và không
đánh dấu PASS cho bất kỳ ca nào.

## 1. Phạm vi và cách kiểm

- Đọc toàn bộ source màn Nhân sự: 4 tab (Nhân viên, Tài khoản, Chấm công, Bảng lương),
  editor, dialog chi tiết; service BLL; repository DAL; migration 022–024; view và
  procedure liên quan; quyền/navigation; mapper lỗi.
- Đối chiếu với kế hoạch T24–T26, UX mục 13 và AGENTS mục 5–6 (ngôn ngữ người dùng,
  thiết kế sát vận hành, khác biệt có chủ đích).
- Không chạy test, không thao tác GUI, không ghi DB. Các kết luận bên dưới là
  **phân tích source/SQL**, kịch bản người dùng ở mục 6 là kịch bản cần chạy khi
  người dùng duyệt đợt test.

## 2. Luồng hiện tại (theo source)

| Tab | Người dùng làm được | Ràng buộc chính |
|---|---|---|
| Nhân viên | Tìm/lọc, thêm, sửa (double-click/F2 mở dialog), nghỉ/đi làm lại, xóa hồ sơ chưa có lịch sử, đối chiếu yêu cầu chờ | ADMIN-only ở menu + BLL + SQL; revision chống ghi đè; nghỉ việc khóa tài khoản; chặn ADMIN cuối |
| Tài khoản | Cấp tài khoản, đổi quyền/khóa, reset mật khẩu | Một nhân viên một tài khoản; hash chỉ đi qua tham số; thao tác trên tài khoản đang dùng sẽ kết thúc phiên |
| Chấm công | Lọc kỳ/nhân viên/trạng thái; thêm/sửa qua grid batch một transaction | Giờ vào cùng ngày làm, ra > vào, ca qua đêm; chặn chồng giờ; kỳ đã chốt lương bị khóa |
| Bảng lương | Lọc kỳ/trạng thái; preview SQL, chốt từng nhân viên/kỳ; xem chi tiết; chi toàn bộ một lần; đối chiếu chốt/chi | Snapshot bất biến; unique nhân viên/kỳ; không chi lần hai; Thực lĩnh 0 không tạo phiếu chi |

Luồng tháng điển hình: chấm công trong tháng → mở Bảng lương → chọn nhân viên/kỳ →
nhập giờ thường/tăng ca, phụ cấp/thưởng/khấu trừ → **Xem preview SQL** → Chốt →
Chi lương. Đây là luồng đúng hướng chứng từ: chốt và chi tách hai bước, có mã bảng
lương và liên kết phiếu chi nguồn.

## 3. Điểm đã sát nghiệp vụ (đối chiếu source)

- Phân quyền ba lớp: menu chỉ ADMIN thấy Nhân sự; `HumanResourcesAccess` kiểm
  `EmployeeManage/AttendanceManage/PayrollManage`; mọi procedure gọi `sp_G03Demand 'ADMIN'`.
- Bảo vệ ADMIN hoạt động cuối bằng trigger + applock dùng chung
  (`TR_AccountLastAdmin`, `TR_EmployeeLastAdmin`, `sp_HRProtectAdmin`).
- Chống ghi đè/đồng thời: rowversion hồ sơ/công, SecurityStamp tài khoản, applock
  theo nhân viên, unique `(MaNV, Thang, Nam)`, preview token chống chốt số cũ.
- Chống trả trùng: `sp_ChiLuong` kiểm trạng thái/thực lĩnh/ngày chốt; trigger kiểm
  phiếu chi nguồn Payroll; phiếu chi tạo trong transaction của procedure.
- Chấm công: chặn chồng giờ kể cả ca qua đêm và ca đang mở; kỳ đã chốt lương bị
  trigger khóa; ca thiếu giờ ra không bị tính 0 khi chốt lương.
- Lương: công thức tính ở SQL, `ThucLinh` là computed column; full-time không
  cộng giờ lần hai; part-time = giờ thường × giá giờ; tổng giây làm tròn một lần.
- Snapshot đầy đủ hợp đồng/mức lương/giờ/công nguồn; bất biến sau chốt; chứng từ
  legacy được tách `LegacyUnknown` thay vì suy dữ liệu.
- Giao diện: editor là dialog native có owner; Enter không tự chốt lương; busy/unknown
  chặn gửi lặp; money căn phải, định dạng `81.000 ₫`; ngày `dd/MM/yyyy`.

## 4. Vấn đề phát hiện

Mức độ: **P1** sai nghiệp vụ/không dùng được, **P2** người dùng dễ làm sai hoặc
hiểu sai, **P3** ma sát nhỏ. Tất cả đang là **đề xuất bug chờ duyệt**, trừ mục D
là khác biệt có chủ đích cần ghi nhận, không sửa.

### A. Logic/UX cần sửa

| ID | Mức | Vấn đề | Bằng chứng | Ảnh hưởng người dùng | Đề xuất |
|---|---|---|---|---|---|
| B01 | P2 | Nút chính màn Bảng lương có nhãn **"Thêm"** nhưng mở hộp "Preview và chốt bảng lương" | `PayrollControl.cs:27`, `OrbitRecordListControl.cs:157` (createCaption mặc định "Thêm") | Quản lý tưởng thêm một dòng lương; đây là thao tác chốt tiền nên nhãn sai gây hiểu nhầm | Đổi hành động/nhãn thành "Chốt lương" hoặc "Tính và chốt lương" |
| B02 | P2 | Lộ thuật ngữ kỹ thuật ra UI: "batch nguyên tử", "rollback toàn batch", "preview SQL", "request ID", "metadata", "chọn Delete", "cần schema022" | `AttendanceControl.cs:35–36`; `PayrollControl.cs:37,41,43,73`; `EmployeesControl.cs:72,77`; `HumanResourcesControl.cs:35` | Người làm quán không hiểu; trái nguyên tắc ngôn ngữ người dùng (AGENTS §5) | Viết lại: "Lưu nhiều dòng công", "một dòng lỗi thì không lưu dòng nào", "xem trước", "mã yêu cầu", "chọn Xóa", "phiên bản CSDL" |
| B03 | P2 | Khi nhập giờ tăng ca > 0 mà để trống "Giờ thường", preview luôn lỗi vì SQL buộc `thường + tăng ca = tổng giờ` | `PayrollControl.cs:42,50`; `024_payroll.sql:51` | Quản lý phải tự trừ nhẩm từng nhân viên; dễ lặp lỗi, tưởng app hỏng | Tự điền giờ thường = tổng − tăng ca, hoặc hiển thị "còn lại" và chặn trước khi bấm preview |
| B04 | P2 | Chồng giờ, trùng khóa và dữ liệu đã đổi dùng chung một thông điệp; không nêu ca/dòng xung đột và không có đường quay lại Chấm công | `023_attendance.sql:92`; `SqlErrorMapper.cs` mã 53203; `AttendanceControl.cs:68`; UX mục 13 yêu cầu hiển thị ca xung đột | Người dùng không biết sửa dòng nào; đóng hộp thoại là mất bản nháp | Trả về dòng/ca xung đột và tô lỗi tại dòng; thêm nút "Mở Chấm công đúng kỳ/nhân viên" từ hộp lỗi chốt lương |
| B05 | P3 | Xóa nhân viên đã có lịch sử trả lỗi FK chung **"…không đủ tồn…"** (câu của nghiệp vụ kho) | `SqlErrorMapper.cs:154`; `EmployeesControl.cs:118` | Câu lỗi sai ngữ cảnh, người dùng tưởng lỗi kho | Ánh xạ 547 theo ngữ cảnh hoặc chặn trước bằng thông điệp "Hồ sơ đã có lịch sử, chỉ nghỉ việc" |
| B06 | P3 | Ô ngày/giờ công nhập dạng text, parse theo culture Windows; app không đặt culture toàn cục, chỉ định dạng tiền theo vi-VN | `AttendanceControl.cs:43–45`; `Program.cs` không set `DefaultThreadCurrentCulture` | Trên Windows không phải vi-VN, `24/11/2026` bị hiểu sai/không nhập được | Đặt culture hiển thị/parse vi-VN cố định cho các cột ngày hoặc dùng DateTimePicker/editor có format rõ |
| B07 | P3 | Combobox chọn nhân viên chỉ hiện tên, không kèm mã; trùng tên thì không phân biệt | `PayrollControl.cs:38`; `AttendanceControl.cs:42` | Chọn nhầm người khi quán có hai nhân viên cùng tên | Hiển thị "Mã · Tên" (DisplayMember ghép) |
| B08 | P3 | Cho mở "Thêm" công ở kỳ đã chốt rồi mới báo lỗi; nút "Chi lương" vẫn bấm được trên dòng Đã chi/Không phát sinh chi rồi báo lỗi | `AttendanceControl.cs:29`; `PayrollControl.cs:27,84` | Thao tác thừa, cảm giác app "cho làm rồi chặn" | Vô hiệu hóa/khóa nút theo trạng thái kỳ và trạng thái bảng lương, kèm lý do |
| B09 | P3 | Nhân viên **đã nghỉ** vẫn chọn được khi thêm dòng công mới; chỉ SQL chặn lúc lưu | `sp_AttendanceLookups` trả `Active` nhưng `AttendanceControl.cs:42` không lọc | Chọn xong mới biết không lưu được | Lọc/nhãn "Đã nghỉ" ngay trong combobox; vẫn cho sửa/xóa dữ liệu cũ |
| B10 | P3 | Không thấy tổng quỹ lương tháng, không in/xuất phiếu lương cho nhân viên | `PayrollControl.cs:22–27`; `PayrollDetailDialog.cs` chỉ hiển thị | Quản lý phải tự cộng; nhân viên không có phiếu lương | Đề xuất bổ sung (ngoài T26 hiện tại): dòng tổng theo kỳ và mẫu in phiếu lương |
| B11 | P2 | Không có luồng "chấm công hôm nay" dễ thấy: không có nút Vào ca/Ra ca, không có danh sách nhân viên đang trong ca; muốn chấm phải bấm "Thêm" và gõ tay ngày/giờ vào ô text | `AttendanceControl.cs:29–72` (chỉ có Thêm/Sửa mở batch grid); toàn màn không có thao tác chấm nhanh | Người dùng mở tab Chấm công nhưng không biết bắt đầu từ đâu (phản hồi thực tế 24/11); nhập công hằng ngày rất nặng | Thêm màn "Chấm công hôm nay": danh sách nhân viên đang làm, chọn ca, nút Vào/Ra lấy giờ hệ thống, chỉ quản lý được sửa |

### B. Ma sát vận hành nhỏ khác

- Lọc "Mã NV" ở Chấm công là ô nhập tay trong khi ô tìm kiếm đã tìm theo tên; người
  dùng phải nhớ mã (`AttendanceControl.cs:19,25`). Nên dùng combobox như Bảng lương.
- Combobox tháng/năm chỉ hiện số ("11", "2026"), không có nhãn "Tháng"/"Năm"
  (`AttendanceControl.cs:17–18`; `PayrollControl.cs:14–15`).
- Danh sách nhân viên hiển thị đồng thời "Lương tháng" và "Lương giờ" cho mọi hợp
  đồng; cột không áp dụng luôn là 0, gây nhiễu (`EmployeesControl.cs:30`).
- Màn Bảng lương có hai nút "Đối chiếu chốt"/"Đối chiếu chi" thường trực cho tình
  huống hiếm; nên gom vào menu phụ để giảm nhiễu hành động chính.
- Editor chấm công dùng grid nhập ngày/giờ dạng text thay vì chọn ngày/giờ; nhập
  một tuần công cho 8 người khá nặng so với nhịp quán.

## 5. Khác biệt có chủ đích so với doanh nghiệp lớn (không phải bug)

Các điểm dưới đây đã được plan ghi rõ là ngoài phạm vi; ghi lại vì người dùng hỏi
độ sát thực tế. Không tự mở rộng.

| ID | Khác biệt | Thực tế doanh nghiệp | Tác động tại quán | Tài liệu |
|---|---|---|---|---|
| D01 | Chốt lương **từng nhân viên**, không có batch cả tháng | Chạy bảng lương một lần cho toàn bộ nhân viên | 8 nhân viên = 8 lần preview + 8 lần chốt + 8 lần chi | `t26.../plan.md:15` |
| D02 | Không có **lịch sử hiệu lực mức lương**; kỳ cũ dùng mức lương hiện tại | Lương theo ngày hiệu lực, hồi tố có kiểm soát | Tăng lương tháng 11 rồi chốt lương tháng 10 sẽ trả theo mức mới | `t26.../plan.md:14` |
| D03 | Full-time trả **đủ lương tháng** dù vào/ra giữa tháng; nghỉ phép không trừ lương | Pro-rate theo ngày công, nghỉ không lương bị trừ | Nhân viên làm 5 ngày vẫn nhận đủ lương tháng | `024_payroll.sql:53–55` |
| D04 | Nhân viên chỉ có trạng thái đang làm/đã nghỉ; **không có ngày vào làm/ngày nghỉ việc/lịch sử công tác** | Hồ sơ nhân sự có ngày hiệu lực, lịch sử chức vụ | Không tính được thâm niên, pro-rate, biên bản nghỉ việc | `022_employees_accounts.sql`, T24 |
| D05 | Một ADMIN vừa lập công, duyệt, chốt và chi lương | Tách người lập và người duyệt/chi (maker–checker) | Khó kiểm soát nội bộ khi quán có nhiều quản lý | UX mục 13, kế hoạch T26 |
| D06 | Reset mật khẩu do ADMIN đặt, không buộc đổi lần đăng nhập sau | Có buộc đổi/khóa tạm sau reset | ADMIN biết mật khẩu nhân viên | `EmployeesControl.cs:131–149` |
| D07 | **Không có nhân viên tự chấm công**: chỉ ADMIN mở Nhân sự và nhập công hộ; tài khoản CASH/STORE không vào được Nhân sự, POS không ghi giờ vào/ra | Máy chấm công/kiosk, nhân viên tự Vào ca–Ra ca bằng tài khoản/PIN, hoặc POS ghi nhận giờ làm | Quản lý phải nhớ và nhập công cho cả quán; không có giờ thực tế tại quầy; dễ sót công | Menu CASH/STORE không có Nhân sự (`MainForm.cs:100–104`), quyền `EmployeeManage` ADMIN-only, T25 chỉ thiết kế nhập công theo nhân viên/kỳ |

Khuyến nghị: nếu muốn nâng độ sát thực tế, ưu tiên **D02 + D03** (đúng tiền lương)
trước **D01** (thao tác hàng loạt) và **D05** (kiểm soát nội bộ). Mỗi mục cần
requirements riêng, không sửa trong đợt bug này.

## 6. Kịch bản test như người dùng (chưa chạy)

Persona: quản lý quán 8 nhân viên (6 full-time, 2 part-time). Trạng thái cột cuối
là **CHƯA CHẠY** theo quy trình chờ duyệt đợt test; cột "Theo source" chỉ là dự
đoán từ code, không phải PASS.

| Mã | Kịch bản | Kỳ vọng người dùng | Theo source | Trạng thái |
|---|---|---|---|---|
| U01 | Thêm nhân viên full-time mới + cấp tài khoản CASH | Lưu hồ sơ, mã NV tự cấp, sang tab Tài khoản cấp được | Có; mã tự cấp qua `sp_CapMa`, phải đổi tab | CHƯA CHẠY |
| U02 | Thêm part-time chỉ nhập lương giờ | Lưu được; bảng lương dùng lương giờ | Có; cho phép lương tháng = 0 | CHƯA CHẠY |
| U03 | Đổi chức vụ nhân viên | Chỉ đổi chức vụ, không đổi vai trò đăng nhập | Có; hai trường độc lập | CHƯA CHẠY |
| U04 | Cho nhân viên nghỉ việc | Xác nhận, khóa tài khoản, giữ hóa đơn/công/lương | Có; tự kết thúc phiên nếu là chính mình | CHƯA CHẠY |
| U05 | Xóa nhân viên đã có công/lương | Bị chặn với lời giải thích đúng | Bị chặn nhưng báo lỗi "không đủ tồn" (B05) | CHƯA CHẠY |
| U06 | Nhập ca ngày 08:00–12:00 và ca qua đêm 22:00–06:00 | Lưu đúng, giờ ra hiện đủ ngày; kỳ theo ngày làm | Có; SQL kiểm bằng giây | CHƯA CHẠY |
| U07 | Nhập hai ca chồng giờ trong một batch | Không lưu dòng nào, chỉ rõ ca/dòng trùng | Bị chặn nhưng thông điệp chung (B04) | CHƯA CHẠY |
| U08 | Để một ca "Đi làm" thiếu giờ ra rồi chốt lương | Preview báo vấn đề, không cho chốt; không tính 0 giờ | Có; issue chặn chốt | CHƯA CHẠY |
| U09 | Chốt full-time có 4 giờ tăng ca | Nhập giờ tăng ca, app tự hiểu giờ thường còn lại | Phải tự nhập giờ thường = tổng − 4 (B03) | CHƯA CHẠY |
| U10 | Chốt part-time 36 giờ thường + 4 giờ tăng ca | Thực lĩnh = 36 × giá giờ + tiền tăng ca + phụ cấp + thưởng − khấu trừ | Có; kiểm bằng SQL/computed | CHƯA CHẠY |
| U11 | Chốt cùng nhân viên/kỳ lần hai | Bị chặn, chỉ một bảng lương | Có; unique + kiểm trước | CHƯA CHẠY |
| U12 | Chi lương rồi bấm chi lần hai | Bị chặn; đúng một phiếu chi gắn bảng lương | Có; `sp_ChiLuong` + trigger | CHƯA CHẠY |
| U13 | Tăng lương sau khi đã chốt kỳ trước | Kỳ đã chốt giữ nguyên số cũ | Có; snapshot bất biến | CHƯA CHẠY |
| U14 | Tăng lương tháng 11 rồi chốt lương tháng 10 | Kỳ 10 phải dùng mức lương của tháng 10 | Không; dùng mức hiện tại, có ghi chú (D02) | CHƯA CHẠY |
| U15 | Nhân viên full-time nghỉ việc ngày 10, chốt lương tháng đó | Trả theo ngày làm thực tế | Trả đủ lương tháng (D03) | CHƯA CHẠY |
| U16 | Hai quản lý cùng sửa một hồ sơ/công/chốt lương | Một người thắng, người kia được yêu cầu tải lại | Có; revision/stamp/token | CHƯA CHẠY |

## 7. Kết luận và bước tiếp theo

- Luồng chính **đúng hướng chứng từ và an toàn dữ liệu**: quyền, chống ghi đè,
  chống trả trùng, snapshot, khóa kỳ, audit đều đã có và được bảo vệ ở SQL.
- Vấn đề đáng sửa trước là **cách nói với người dùng và đường sửa lỗi** (B01–B04):
  nhãn "Thêm" ở màn chốt tiền, thuật ngữ kỹ thuật, phép trừ giờ tăng ca thủ công,
  lỗi chồng giờ không chỉ đúng dòng/ca.
- Các khoảng cách lớn so với doanh nghiệp lớn (batch lương, lịch sử lương, pro-rate,
  maker–checker) là **có chủ đích** trong plan; nếu muốn nâng, cần requirements riêng.
- Đề nghị người dùng duyệt **đợt bug B01–B11** (ưu tiên B01–B04, B11) trước khi sửa; sau
  khi sửa sẽ chạy đợt test theo kịch bản mục 6, gồm cả GUI thật và SQL test riêng.
  Riêng D07 (nhân viên tự chấm công/kiosk) là tính năng mới, cần requirements riêng;
  không gộp vào đợt bug.
- Báo cáo này không thay đổi trạng thái G06, không commit, không nâng DB development.
- Đã lập kế hoạch đề xuất cho đợt bug B01–B09 tại
  [bug-batch-20261124.md](bug-batch-20261124.md). B10 (tổng quỹ lương/in phiếu lương)
  và B11/D07 (tự chấm công) là tính năng, tách requirements riêng; D01–D06 giữ là
  khác biệt có chủ đích. Chưa sửa source cho đến khi người dùng duyệt đợt bug.
