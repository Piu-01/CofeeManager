# Biên bản kiểm thử — row-inline-edit-delete (đợt UI 22–23/11/2026)

## 1. Môi trường và phạm vi

| Mục | Giá trị |
|---|---|
| Ứng dụng | `C:/Users/Lenovo/.qoder/tmp/cm-build-inline/CafeManager.WinForms.exe` (Debug, net10.0-windows), PID cuối 18996 |
| CSDL | SQL Server thật, DB test riêng `CoffeeManager_Test_Inline_20261122_inline_ui`, schema 41 |
| Tài khoản | admin / Admin@123; thungan / Cash@123 |
| DPI | 125% (GetDpiForWindow = 120) |
| Công cụ | UIA + Win32 P/Invoke trong `C:/Users/Lenovo/.qoder/tmp/cm-inline-tests/` (InlineUi.ps1, Test-*.ps1) |
| Source | Giữ cố định trong suốt lượt test, không sửa code/migration |

Phạm vi: tương tác dòng mới (inline edit, thùng rác từng dòng, xác nhận theo loại
dữ liệu, khóa dòng + “Đối chiếu” khi timeout/không rõ kết quả) trên 8 màn master
data được giao. Khuyến mãi và Chấm công ngoài phạm vi, không thao tác.

Ghi chú kỹ thuật khi test: IME tiếng Việt (UniKey) chặn gõ ký tự qua SendKeys nên
nội dung văn bản được đặt bằng UIA `ValuePattern.SetValue`; chỉ Enter/Tab/Esc/
Shift+Tab/Ctrl+End gửi bằng phím. Đây là giới hạn công cụ test, không phải lỗi app.

## 2. Kết quả theo màn

### 2.1 Danh mục (Nhà cung cấp, Đối tác giao hàng, Loại chi, Chi, Phương thức TT, ...)

| Mã | Thao tác / dữ liệu | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| RI-C01 | Toolbar danh sách | Không còn nút Sửa/Xóa; có Thêm và cột thùng rác | Đúng; cột xóa 55px | PASS |
| RI-C02 | Click ô Mã (không sửa được), góc header, vùng trống | Chỉ chọn dòng, không mở editor | Không mở editor | PASS |
| RI-C03 | Hover/click ô Tên | Con trỏ IBeam, highlight; mở editor chọn hết nội dung | Đúng, select-all khi mở | PASS |
| RI-C04 | PT001 Phương thức TT, đổi Hỗ trợ 1→0 | Lưu ngay, DB đổi `PaymentMethodSupport` | DB PT001 = 0 | PASS |
| RI-C05 | PT010 đổi tên | Không cho sửa mã hệ thống | Editor không mở | PASS |
| RI-C06 | TC001 (Loại chi đã có phiếu) đổi Chi→Thu | Bị từ chối kèm thông báo, dữ liệu giữ nguyên | Bị từ chối đúng; **phát sinh IN-01** (editor kẹt) | FAIL (IN-01) |
| RI-C07 | TC005 mã hệ thống | Không sửa, không xóa | Đúng | PASS |
| RI-C08 | TC009 tạo mới, đổi tên, xóa bằng thùng rác | Hỏi xác nhận, xóa thành công, xóa ngay sau xác nhận | Đúng; bản ghi không còn | PASS |
| RI-C09 | NCC10 xóa | Hỏi xác nhận; xóa thành công | Hộp “Xóa … Danh mục có lịch sử sẽ bị từ chối.”; DB hết NCC10 | PASS |
| RI-C10 | NCC09 (đã dùng) xóa | Hỏi xác nhận rồi bị chặn | Bị chặn, dữ liệu còn | PASS |
| RI-C11 | DT002 Đối tác giao hàng xóa | Xóa ngay, không xác nhận; bị chặn nếu có lịch sử | Không hộp xác nhận, bị chặn | PASS |
| RI-C12 | DM011 tạo mới rồi xóa | Tạo/xóa ngay, không xác nhận | Đúng | PASS |
| RI-C13 | Tải danh sách bị quá thời gian (tab NCC) | Báo lỗi, không hiện tổng 0 giả | Báo “Chưa cập nhật · Truy vấn quá thời gian; thử tải lại.”, lưới trống; “Tải / đối chiếu” tải lại được | PASS (có ghi chú trạng thái tab cũ) |
| RI-C14 | Tab danh mục khác (Loại chi/Chi/PTTT/Đối tác) mở bình thường | Chuyển tab đúng, dữ liệu riêng | Đúng | PASS |

### 2.2 Sản phẩm

| Mã | Thao tác / dữ liệu | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| RI-P01 | Toolbar | Không Sửa/Xóa, có thùng rác | Đúng | PASS |
| RI-P02 | SP001 sửa Tên, Giá, Nhóm, Trạng thái | Lưu từng ô, DB đổi | Đổi tên/giá/nhóm/trạng thái OK | PASS |
| RI-P03 | Hộp Công thức | Mở/đóng đúng, không sửa dữ liệu ngoài ý | Mở OperationEditorDialog, đóng không lưu | PASS |
| RI-P04 | SP019 tạo mới rồi xóa | Xóa ngay, không xác nhận | Đúng | PASS |
| RI-P05 | Xóa sản phẩm đã bán | Kỳ vọng bị chặn | Chưa có sản phẩm chưa dùng để thử nhánh thành công; nhánh chặn chưa chạy | NOT RUN |
| RI-P06 | Timeout khi sửa sản phẩm | Khóa dòng + Đối chiếu | Không chạy (đã phủ timeout ở 3 màn đại diện) | NOT RUN |

### 2.3 Khách hàng

| Mã | Thao tác / dữ liệu | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| RI-K01 | KH001 sửa Tên, SDT | Lưu inline | DB đổi đúng | PASS |
| RI-K02 | Ngày sinh/email nếu read-only | Không mở editor | Đúng theo cấu hình | PASS |
| RI-K03 | KH023 xóa -> Hủy hộp xác nhận | Không xóa, dòng giữ nguyên | Đúng | PASS |
| RI-K04 | KH023 xóa -> đồng ý | Bị chặn nếu có hóa đơn | Bị chặn, dữ liệu còn | PASS |
| RI-K05 | Xóa khách chưa có hóa đơn | Xóa thành công | Không có dữ liệu phù hợp để thử | NOT RUN |
| RI-K06 | Hộp “Mở sổ điểm” / “Lịch sử điểm” | Mở đúng, chỉ đọc | Mở/đóng đúng | PASS |

### 2.4 Nhân viên

| Mã | Thao tác / dữ liệu | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| RI-E01 | NV001 sửa Tên | Lưu inline | DB đổi | PASS |
| RI-E02 | NV001 sửa SDT 0901234567 | Lưu inline | DB đổi | PASS |
| RI-E03 | Lương tháng 12.500.000; Lương giờ 50.000 | Lưu số đúng | DB đổi | PASS |
| RI-E04 | Hợp đồng Part-time; Chức vụ CV002 | Lưu dropdown | DB đổi | PASS |
| RI-E05 | SDT trùng nhân viên khác | Báo trùng, không ghi | Báo trùng đúng nhưng **IN-01** (kẹt editor) | FAIL (IN-01) |
| RI-E06 | “Nghỉ” mở hộp trạng thái; Hủy | Không đổi dữ liệu ngoài ý | Đúng | PASS |
| RI-E07 | NV010 xóa | Hỏi xác nhận; bị chặn do có lịch sử | Bị chặn, dữ liệu còn | PASS |
| RI-E08 | Xóa nhân viên chưa có lịch sử | Xóa thành công | Không có hồ sơ phù hợp | NOT RUN |

### 2.5 Tài sản (giữ hộp hồ sơ, bỏ Sửa/Xóa, thêm thùng rác)

| Mã | Thao tác / dữ liệu | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| RI-A01 | Toolbar/row | Không Sửa/Xóa; có thùng rác | Đúng; “Chi tiết”, “Yêu cầu chờ” còn | PASS |
| RI-A02 | TS001 sửa Tên, Ngày mua (dd/MM/yyyy), Nguyên giá, Số tháng | Lưu inline đúng kiểu | DB đổi đúng cả 4 ô | PASS |
| RI-A03 | Điều hướng bằng Tab/Shift+Tab giữa ô | Lưu ô trước, mở ô kế | Đúng | PASS |
| RI-A04 | TS001 xóa -> Hủy | Không xóa | Đúng | PASS |
| RI-A05 | TS001 xóa -> đồng ý | Bị chặn do có chứng từ | “Tài sản đã có chứng từ liên quan, không thể xóa.” | PASS |
| RI-A06 | Hộp “Tài sản — TSxxx”, “Hồ sơ tài sản” | Mở/đóng đúng | Đúng | PASS |
| RI-A07 | Tạo tài sản/xóa tài sản chưa dùng | Có nút Thêm? | Màn không có hành động Thêm; không thử được | NOT RUN |

### 2.6 Bàn / Khu vực

| Mã | Thao tác / dữ liệu | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| RI-T01 | Tab Khu vực: KV001 đổi tên | Lưu inline | DB đổi | PASS |
| RI-T02 | Xóa KV010 (rỗng) | Xóa ngay, không xác nhận | DB hết KV010 | PASS |
| RI-T03 | Xóa KV001 (còn bàn) | Bị chặn | Bị chặn, dữ liệu còn | PASS |
| RI-T04 | Danh sách bàn: B0001 đổi tên | Lưu inline | DB đổi | PASS |
| RI-T05 | B0011 tạo mới rồi xóa | Xóa ngay | Đúng | PASS |
| RI-T06 | B0002 đổi Khu vực rồi trả lại | Lưu dropdown, DB đổi | Đúng, đã khôi phục | PASS |
| RI-T07 | Bàn đang dùng xóa | Bị chặn (không xác nhận) | Bị chặn | PASS |
| RI-T08 | Sau khi xóa KV010, mở dropdown Khu vực của bàn | Không còn KV010 | Vẫn còn “Khu khong khoi thuoc” | **FAIL (IN-02)** |
| RI-T09 | Sơ đồ bàn mở/đóng | Không lỗi | Đúng | PASS |

### 2.7 Hạng thành viên

| Mã | Thao tác / dữ liệu | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| RI-L01 | BAC sửa Điểm tối thiểu 2500 -> Enter -> Hủy xác nhận | Không lưu, dòng giữ nguyên | Dữ liệu không đổi nhưng **IN-01** (kẹt editor) | FAIL (IN-01) |
| RI-L02 | BAC sửa Điểm tối thiểu 2500 -> đồng ý | Lưu, DB đổi | DB = 2500 | PASS |
| RI-L03 | BAC xóa -> Hủy | Không xóa | Đúng | PASS |
| RI-L04 | BAC xóa -> đồng ý | Xóa thành công (không còn khách hạng) | DB hết BAC | PASS |
| RI-L05 | Tab Khuyến mãi | Ngoài phạm vi, giữ nguyên | Không thao tác | SKIPPED (ngoài phạm vi) |

### 2.8 Timeout / kết quả không rõ (2 phiên SQL)

Cách tạo: phiên SQL giữ khóa bảng đích > 30s để lệnh ghi quá `CommandTimeoutSeconds`.
Sau mỗi ca: kiểm tra DB, dùng nút “Đối chiếu” trên dòng.

| Mã | Màn / dữ liệu | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| RI-X01 | Nguyên liệu NVL02, requestId `032791c4-9399-42bb-b9bc-0c7d0d8e8515` | Dòng khóa, nút “Đối chiếu” giữa dòng, dòng khác vẫn dùng, DB chưa đổi | Đúng; Đối chiếu báo “chưa ghi”, dòng mở khóa, sửa lại được; DB không đổi | PASS |
| RI-X02 | Tài sản TS002, requestId `0512ea8e-0f51-4f7b-9b73-c7dfc5c07814` | Như trên | Đúng; đối chiếu “chưa ghi”, DB không đổi | PASS |
| RI-X03 | Nhà cung cấp NCC02, requestId `098f2a71-3823-4122-ad63-a5ef9b4db351` | Như trên | Đúng; đối chiếu qua revision “chưa ghi”, DB không đổi | PASS |
| RI-X04 | Hai phiên cùng sửa một dòng | Phiên sau bị từ chối do revision | Không chạy riêng; đã kiểm tra revision qua luồng đối chiếu | NOT RUN |
| RI-X05 | In/thanh toán sau timeout | Ngoài phạm vi màn master data | Không chạy | SKIPPED |

### 2.9 Phân quyền CASH (thungan)

| Mã | Thao tác | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| RI-R01 | Menu sau đăng nhập CASH | Không có nhóm master data | Chỉ Bán hàng, Lịch sử hóa đơn, Giao hàng, Khách hàng, Tồn phục vụ bán, Thông tin hệ thống, Đăng xuất | PASS |
| RI-R02 | Màn Khách hàng: click ô Tên / thùng rác | Chỉ đọc, không editor, không xóa | OV=0, không hộp xác nhận | PASS |
| RI-R03 | Gọi thẳng dịch vụ khi không đủ quyền | BLL chặn | Không kiểm trực tiếp ngoài UI (không có menu) | NOT RUN |

## 3. Tổng hợp

- PASS: 50
- FAIL: 4 (RI-C06, RI-E05, RI-L01 do IN-01; RI-T08 do IN-02)
- NOT RUN: 7 (RI-P05, RI-P06, RI-K05, RI-E08, RI-A07, RI-X04, RI-R03)
- SKIPPED: 2 (RI-L05 khuyến mãi ngoài phạm vi; RI-X05 in/thanh toán ngoài phạm vi); kiểm hình thức/DPI 100%/150% do người dùng tự kiểm theo phân công
- Bug: **IN-01** (cao), **IN-02** (thấp) — chi tiết tại `bug-report.md`.

## 4. Bằng chứng

- Kịch bản và log thao tác: `C:/Users/Lenovo/.qoder/tmp/cm-inline-tests/` (notes.md, InlineUi.ps1, các Test-*.ps1).
- Đăng nhập ban đầu: `runs/20261122-ui/login-admin.txt`.
- Kiểm tra DB trực tiếp bằng `sqlcmd -S localhost -E -C`: PT001 support = 0; không còn NCC10/TC009/DM011/KV010/B0011/SP019/BAC; NV001 SDT/lương/hợp đồng/chức vụ đúng; TS001 trường đã sửa đúng; 3 ca timeout không đổi dữ liệu.
- Không có ảnh chụp đối chiếu hình thức (theo phân công, người dùng tự kiểm giao diện).

## 5. Đợt hồi quy sau sửa bug (23/11/2026)

Môi trường: DB test như trên, build `C:/Users/Lenovo/.qoder/tmp/cm-build-inline3`
(Debug, 0 lỗi), PID 27872, DPI 125%. Chỉ chạy hồi quy IN-01/IN-02 và các luồng
liền kề; không sửa source trong lúc test.

| Mã | Ca kiểm tra | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| RG-01 | NVL02 sửa Tên, Enter | Lưu, DB đổi | “Đã cập nhật…”, DB đổi; đã trả tên cũ | PASS |
| RG-02 | NVL03 đổi Đơn vị Kg→Gam (SQL 51602), Enter rồi Esc | Báo lỗi, editor còn mở, Esc đóng được | Báo lỗi đúng, OV=1; Esc → OV=0; DB vẫn Kg | PASS |
| RG-03 | Lặp lỗi NVL03 rồi click thẳng ô Tên dòng 4 | Bỏ giá trị lỗi, mở ô được click, không kẹt | Mở “Inline Mat Chained”, không lặp lỗi; Esc đóng | PASS |
| RG-04 | NVL05 sửa Tên, Tab | Lưu rồi mở ô Đơn vị | Lưu “Đã cập nhật…”, editor chuyển sang Đơn vị | PASS |
| RG-05 | Hạng VANG đổi ngưỡng 12000, Enter, Hủy xác nhận, Esc | Không lưu; editor còn mở; Esc đóng | “Đã hủy.”, OV=1; Esc → OV=0; DB vẫn 10000 | PASS |
| RG-06 | Lặp RG-05 rồi click ô ngưỡng hạng DONG | Không mở lại hộp xác nhận; mở ô DONG | Không có hộp mới, OV=0 (đúng giá trị DONG); DB không đổi | PASS |
| RG-07 | SP001 nhập Giá “abc”, Enter rồi Esc | Báo “Giá không hợp lệ.”, Esc đóng, DB không đổi | Đúng; DB vẫn 26000 | PASS |
| RG-08 | SP001 sửa Giá 27000 rồi trả 26000 | Lưu bình thường sau lỗi parse | DB 27000 rồi 26000 | PASS |
| RG-09 | Tạo KV099 (SQL test), xóa ở tab Khu vực, quay lại tab Bàn mở dropdown Khu vực | KV099 không còn trong danh sách | Danh sách 9 khu vực hiện có, không có “Regression Area” | PASS |
| RG-10 | Đổi khu vực B0001 sang “San vuon” rồi trả “Inline Area A” | Dropdown làm mới vẫn lưu đúng | DB KV002 rồi KV001 | PASS |

Tổng hồi quy: 10/10 PASS. Dữ liệu test đã khôi phục: NVL02/NVL03/NVL05 đúng
tên/đơn vị cũ; hạng VANG 10000/5, KIM 30000/8; SP001 giá 26000; B0001 thuộc
KV001; KV099 đã xóa.

### Quan sát ngoài đợt (chưa duyệt sửa)

Khi mở màn Ưu đãi / Điểm (tab Khuyến mãi mặc định), ứng dụng hiện hộp
“Unhandled exception”: `System.IndexOutOfRangeException` tại
`LoyaltyRepository.Promotion` dòng 29 (đọc cột từ procedure khuyến mãi), phát
sinh từ `Reload()` của PromotionList khi tab hiện lại. Không liên quan source
đã sửa của IN-01/IN-02; cần điều tra riêng (có thể do procedure/schema DB test).
Đã bấm Continue để tiếp tục các ca hạng; không sửa và không tính vào kết quả
đợt này. Nếu được duyệt sẽ lập bug riêng.

## 6. Đợt kiểm thử tương tác click hai bước (23/11/2026)

Môi trường: DB test như trên, build `C:/Users/Lenovo/.qoder/tmp/cm-build-inline4`
(Debug, 0 lỗi), PID 29604, DPI 125%, tài khoản admin. Source giữ cố định trong
lúc test; bằng chứng thao tác/lệnh tại
`runs/20261123-two-step/summary.txt`.

| Mã | Ca kiểm tra | Kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| RG-11 | Click đầu vào dòng chưa chọn (NVL02) và dòng đang auto-select (NVL01) | Chỉ chọn/đánh dấu dòng, không mở editor | OV=0 ở cả hai lượt | PASS |
| RG-12 | Click thứ hai vào ô Tên NVL02 rồi Enter | Mở đúng ô, lưu và reload | OV=1, đúng “Inline Enter Probe”; DB đổi, đã trả tên cũ | PASS |
| RG-13 | Click hai bước ô Khu vực bàn B0002, chọn “San vuon” rồi Enter | Dropdown mở, lưu đúng khu vực | Danh sách 9 khu vực; DB B0002 = KV002; đã trả KV001 | PASS |
| RG-14 | Click hai bước badge Trạng thái SP001 | Editor đúng kiểu, có 2 lựa chọn, Esc không ghi | Combo “Ngừng bán”; list Đang bán/Ngừng bán; Esc đóng, DB giữ Ngung ban | PASS |
| RG-15 | Đang sửa NVL04, click sang dòng NVL05 | Ghi NVL04, đóng editor, chỉ chọn dòng mới | DB NVL04 đổi; OV=0; click một lần sau đó mở đúng editor NVL05; đã trả NVL04 | PASS |
| RG-16 | Đang sửa NVL05, click ô Đơn vị cùng dòng | Ghi ô Tên rồi mở ô Đơn vị | Tên lưu, editor mở Đơn vị “Kg”; đã trả tên cũ | PASS |
| RG-17 | F2 tại dòng/ô đang chọn, rồi Esc | Mở editor đúng ô, Esc không ghi | F2 mở “Inline Mat Test A”; Esc → OV=0 | PASS |
| RG-18 | NVL03 Đơn vị “Gam” gây SQL 51602, rồi click dòng NVL04 | Giữ editor/báo lỗi; click dòng khác bỏ giá trị lỗi, không gửi lặp | Lỗi đúng, OV=1; click dòng 4 → OV=0, không lặp; DB vẫn Kg | PASS |
| RG-19 | Thùng rác NVL99 (xác nhận) và B0099 (xóa ngay) | Đúng luồng xác nhận/xóa ngay, DB xóa đúng dòng | NVL99 có Hủy/Xóa rồi mất; B0099 không hộp thoại, DB còn 0 dòng | PASS |
| RG-20 | IN-02: tạo KV099, xóa ở tab Khu vực, mở lại dropdown Khu vực của bàn | Danh sách làm mới, không còn KV099 | 9 khu vực hiện có, không có KV099; DB KV099 = 0 | PASS |

Tổng đợt: 10/10 PASS. Hover đổi màu chữ Caramel là hình thức, ghi SKIPPED theo
phân công (người dùng tự kiểm); agent không chụp/đối chiếu giao diện.

Ghi chú dữ liệu: SP99 tạo trực tiếp bằng SQL bị service từ chối với thông báo
“Thông tin sản phẩm/công thức không hợp lệ.” nên không dùng làm ca xóa; đã xóa
SP99 bằng SQL và dùng bàn B0099 cho ca xóa ngay. Dữ liệu cuối đã khôi phục:
NVL02/NVL03/NVL04/NVL05 đúng tên/đơn vị; SP001 26000/Ngung ban; B0002 KV001;
không còn SP99/B0099/KV099.

## 7. Kết luận

Chức năng inline edit/xóa từng dòng, xác nhận theo loại dữ liệu, khóa dòng và
“Đối chiếu” sau timeout hoạt động đúng trên các màn đã chạy. Hai bug phát hiện
trong đợt đầu đã được sửa theo duyệt ngày 23/11/2026: IN-01 (editor kẹt sau commit
thất bại/hủy xác nhận) và IN-02 (dropdown khu vực của bàn còn mã đã xóa); đợt hồi
quy 10/10 PASS. Ngày 23/11/2026 đã đổi tương tác sang click hai bước (click đầu
chọn dòng, click kế tiếp mới sửa ô) và hover đổi màu chữ; đợt kiểm thử tương tác
10/10 PASS, dữ liệu test đã khôi phục. Hover màu chữ do người dùng tự kiểm hình
thức (SKIPPED theo phân công). Còn một quan sát ngoài đợt ở màn Khuyến mãi
(unhandled exception trong `LoyaltyRepository.Promotion`) chưa được duyệt
điều tra/sửa.
