# B10 — Regression sau Save Designer, 09/10/2026

**Duyệt:** Người dùng “duyệt” đợt B10, bao gồm sửa và regression.
**Run:** `G01_B10_20261009_183023` trong thư mục runs của nhóm.
**Môi trường:** Windows, Visual Studio 18, .NET SDK 10.0.401.

## Sửa trong phạm vi

- Khai báo 5 DataGridViewTextBoxColumn riêng với DataPropertyName và HeaderText
  tiếng Việt. Style header/dòng gán thuộc tính tường minh để Designer serialize.
- Tắt AutoGenerateColumns trong constructor, giữ font Segoe UI 10pt và tiêu đề
  Form tại runtime. Cấu hình root này từng bị serializer bỏ khi Save.
- Tăng cột filter từ 275 lên 310 tại DPI cơ sở Designer 120, tránh nhãn wrap
  làm toolbar chiếm thêm chiều cao ở 150%.
- ErrorProvider trở thành component trong Designer, khởi tạo cùng container và
  dispose qua container; không còn container khai báo mà không được gán.
- Giữ layout/scroll nhãn rỗng của B09; không đổi contract SQL/DAL/BLL.

## Designer round-trip và source cố định

Visual Studio cũ đã đóng; mở solution bằng VS hiện tại. Save All rồi Save,
đóng tab Designer, chọn StartupForm.cs và Shift+F7 mở lại. Lần đầu harness
gặp trạng thái chưa lưu/focus chưa ổn định; chưa dùng lần đó để báo PASS.
Sau Save All, round-trip thành công; cấu hình cột/style và ErrorProvider còn.

Lấy manifest **sau serialization**, build và chạy UI trên source đó. Round-trip
cuối cùng giữ source: `source-stability.json` **Count=0**. Có ảnh
`designer-reopened.png`, `designer-evidence.json`, source trước/sau Save và
`designer-harness.txt`. Không dùng bản trước Save để nghiệm thu bản sau Save.

## Kết quả

| Ca | Expected | Actual / bằng chứng | Status |
|---|---|---|---|
| T04-K01 regression Debug/Release | Không warning/error | build-debug.txt, build-release.txt: 0 warning, 0 error | PASS |
| T04-K05 Save/close/reopen Designer | Mở được, giữ cột/style/component | Ảnh native, title không dấu *, round-trip cuối không đổi source | PASS |
| UX-K40 bounds, 100/125/150%, 1280×720 và 1366×768 | Đúng viewport, controls truy cập được | GetDpiForWindow 96/120/144; SizeMatched=true ở 6 tổ hợp | PASS |
| Grid sau Save | Header tiếng Việt, màu theo guide | 5 cột đúng; header/selection kem caramel; PNG native | PASS |
| Grid 150%1280×720 | Có dòng dữ liệu và cuộn tới dòng cuối | Ảnh có dòng đầu, Down×3 tới Row 4 cùng PID app | PASS |
| UX-K39 Tab/Shift+Tab ở ba DPI | Input → tải → grid → tải → input | SendInput, 5 quan sát mỗi DPI cùng PID; Row 4 bằng Down | PASS |
| Minimize/restore ba DPI | Iconic rồi phục hồi viewport | IsIconic=true, Restored=true, 1280×720 | PASS |
| Thiếu cấu hình, trạng thái trước tải, loading, lỗi kết nối ba DPI | Phân biệt trạng thái, nút đúng, nội dung truy cập được | states100/125/150/ui-evidence.json và PNG exe thật | PASS |
| Nhãn dài ở 150%1280×720 | Đọc được câu cuối bằng cuộn | ScrollInfo valid, range max131/page107, pos0→25, ảnh sau cuộn | PASS |
| Source cố định trong regression cuối | Không đổi source/test/SQL | source-stability.json Count0 | PASS |
| Khôi phục display | Trả125% | restored-scale.json:125% (Recommended) | PASS |

Grid dùng host tạm mở đúng StartupForm compiled với 4 DTO **minh họa**, không
là bằng chứng query SQL/principal. Các trạng thái kết nối dùng exe thật, endpoint
127.0.0.1,1 không sẵn sàng để gây lỗi, không ghi dữ liệu vào DB. Ca SQL, restricted
principal, cancellation B08 kế thừa các lượt PASS trước vì B10 không đổi chúng.

Lượt bàn phím đầu bị focus ngoài app/phím đang giữ: giữ
`dpi-keyboard-attempt1.json` và `attempt2.json`, không kết luận là lỗi sản phẩm.
Người dùng đồng ý ngừng dùng chuột/phím cho lượt cuối; `dpi-keyboard.json`
chứng minh đủ thứ tự và PID ở cả ba DPI. Capture đầu 100% bị đen không dùng;
ảnh chụp lại sau resize hợp lệ. Không đổi expected để né lỗi.

## Kết luận

**B10 PASS; B09 regression bổ sung đã đạt trên bản sau Save.** Không còn blocker
kỹ thuật được ghi nhận của G01. 45/45 mã bắt buộc và các kiểm chứng bổ sung đã
đủ bằng chứng; giữ toàn bộ lịch sử FAIL trước. G01 đủ kiểm chứng kỹ thuật,
**chờ cho phép commit bàn giao** theo điều kiện hoàn tất của kế hoạch. Chưa
commit và chưa chuyển sang G02.
