# Kết quả chỉnh bố cục thích ứng — 17/10/2026

**Đang làm, chưa nghiệm thu toàn bộ.** Đã triển khai yêu cầu tối ưu bố cục trong
WinForms theo phản hồi mới. Không sửa SQL/DAL/BLL/Models/tests hoặc các task tương lai.

## Theo sát UX và kế hoạch

| Phạm vi | Đối chiếu | Thay đổi presentation |
|---|---|---|
| Login/Bootstrap | T06, UX01, UX mục6 | Card có bề rộng tối đa; giữ trường, submit, masking, loading và lỗi hiện có |
| Main/Home/System | T07, UX02, UX mục2–3 | Navigation lên hàng trên khi hẹp, giữ các nút/role/handler và instance module |
| Danh mục/editor | T08, UX09, UX mục5 | Giữ7nhóm, query,50dòng/trang, xác nhận, draft/unknown/conflict; thay bố cục và presentation |
| Startup kiểm tra bàn | G01 hiện có | Giảm footprint header/toolbar và minimum form, không đổi dữ liệu hay use case |

Editor thông tin chính trước/bổ sung sau; action cố định trong card, body cuộn
khi thấp. Không thêm chức năng module chưa triển khai. Kế hoạch T01–T32 và325ca
không thay đổi. Program vẫn chỉ cấu hình native theme như đợt trước.

## Phần đã làm

- Login căn giữa, giới hạn480logical px; Bootstrap720; editor640; Home/System880.
- Field giữ TextBox native cho tiếng Việt/caret/masking; viền và focus dùng helper
  OrbitField. Khi hẹp, nhãn editor/bootstrap chuyển lên trên input, không tạo lại input.
- Main dưới1000logical px chuyển navigation ngang có wrap; minimum form cho phép
  vùng nửa màn hình. Vẫn chỉ hiển thị menu được role cho phép.
- Danh mục dưới780logical px dùng ô chọn nhóm, cửa sổ rộng dùng tab. Header gọn,
  không có phụ đề ở chế độ hẹp; footer không phình thành khối trạng thái lớn.
- Empty state nằm trong workspace, có hướng dẫn cho dữ liệu rỗng/tìm không thấy/
  trang tiếp theo rỗng. Chỉ hiện sau query thành công; lỗi không biến thành rỗng.
- Ô giờ dùng hai NumericUpDown giờ/phút, adapter Value kiểu DateTime giữ request
  và validation BLL hiện tại. Giờ có hai chữ số, không còn nền trắng DateTimePicker.
- OrbitHeader có ánh sáng cầu, bóng và vòng trước/sau; không thêm timer/asset online.
- Button PreferredSize đo đúng text/icon/phần vẽ, tránh cắt nhãn khi AutoSize.

## Kiểm chứng đã chạy

Chứng cứ: [runs/20261017-responsive](runs/20261017-responsive/). Windows/.NET10,
SQL Server thật với DB fixture có marker
`CoffeeManager_Test_G02_20261017_173711_c86ff044`; không DB sử dụng thật.
DPI120=125%. Launcher chỉ arrangement/pre-auth, mọi input UI dùng sky.

| Ca | Expected/actual | Kết quả |
|---|---|---|
| Cửa sổ720×720 | Đọc hàng DM001, truy cập toolbar/chọn nhóm/footer | PASS phần hiển thị ở R2; `main-narrow-r2.png`, geometry Client702×673 |
| Từ hẹp sang maximize | Navigation/sidebar và selector/tab đổi bố cục, dòng còn hiển thị | PASS phần quan sát; `main-maximized-r2.png` |
| Vùng nửa màn hình | Tất cả nút điều hướng, bảng và footer tiếp cận được | PASS với fixture bounds960×1020, client942×973; `main-half-workarea-r2.png` |
| Login maximize | Input nằm trong card, không kéo hết màn hình | PASS visual; `login-maximized-r2.png`; không nhập/submit xác thực |
| Editor maximize/restore | Card hữu hạn,4trường ca và action cạnh nội dung | PASS visual; `editor-maximized-r2.png`, `editor-restored-r2.png` |
| Editor600×720 | Nhãn xếp dọc, giờ/phút và Lưu/Hủy cùng nhìn thấy | PASS visual; `editor-small-r2.png`, geometry editorShift600×720 |
| Empty Supplier | Thông điệp nằm giữa workspace, không bảng tím rỗng | PASS; `empty-state-r2.png` |
| Giờ keyboard | Up tăng06→07; Down trả06, phút00 và kết thúc14:00 giữ nguyên | PASS ca nhỏ; `hour-keyboard-up-r2.png`, `hour-values-r2.txt`. Không lưu DB |
| Unit AuthRules/CatalogRules | Kết quả quy tắc cũ không đổi | 36/36 PASS; `unit.trx` |
| Build Debug/Release R2 | 0warning/error | PASS; `build-debug-r2.txt`, `build-release-r2.txt` |
| Source cố định R2 | Hash trước/sau giống nhau | PASS; source-freeze.csv/source-after.csv,0khác biệt |
| Whitespace bàn giao | Không lỗi diff | PASS sau dọn dòng trắng EOF; `git diff --check` exit0 |

Lượt R1 đã ghi FAIL vì hàng dữ liệu vẫn bị cắt ở720×720. Giữ ảnh
`main-narrow-r1-fail.png`, manifest source-r1.csv và checkpoint-r1.md. Đã kết thúc
lượt đó, sửa đúng bố cục B07/B02 rồi freeze và chạy R2; không đổi expected để né lỗi.
Preview1–4 là các mốc phát triển, không dùng làm nghiệm thu bản cuối.

Sau khi kết thúc R2, chỉ dọn dòng trắng EOF/BOM của các file đã sửa, không đổi câu
lệnh. Manifest sau formatting: source-final-format.csv; build bàn giao ở
build-delivery.txt. Không ghi source-format giống hệt hash R2.

## Chưa kiểm chứng / giới hạn

- Native Windows Snap trái/phải chưa thao tác: fixture dùng đúng nửa WorkingArea,
  không coi đó là chứng cứ thao tác Snap hoặc per-monitor DPI transition.
- DPI100/150 thật, Designer round-trip, toàn bộ7editor và trạng thái lỗi/conflict/
  unknown sau sửa, xác thực qua UI, full keyboard/draft/resize/scroll/IME matrix:
  NOT RUN/BLOCKED. UIA focus có cache cũ giữa nhiều fixture cùng exe; không nhập tên
  thử khi chưa xác nhận focus, không bịa kết quả giữ nháp.
- Ảnh editor-narrow-r2.png là lần drag không đổi kích thước, không dùng làm bằng
  chứng hẹp; ảnh đúng là editor-small-r2.png. Một số geometry được launcher ghi đè
  khi mở cùng mode, kích thước trong nội dung JSON mới là nguồn đối chiếu.
- R2 phát hiện thứ tự brand/caption đổi khi hiện lại navigation. Sau kết thúc R2,
  debug đúng B07 bằng cách cố định child index; freeze R3 rồi kiểm chuyển720×720→
  maximize: PASS thứ tự brand trước/caption sau, `main-maximized-r3.png`.
  R3 chỉ thêm2dòng sắp thứ tự control, không đổi event/data; build Debug và Release
  cuối đều0warning/error (build-debug-r3.txt/build-delivery-release.txt); manifest riêng
  source-r3-freeze.csv/source-r3-after.csv. Không gọi toàn bộ B04–B09 đã PASS.
- Bộ484G02 là lịch sử đợt trước;36unit là tập con, không cộng hoặc ghi484vừa chạy lại.

Không commit/push. Các vòng test/bug gốc được bảo toàn.
