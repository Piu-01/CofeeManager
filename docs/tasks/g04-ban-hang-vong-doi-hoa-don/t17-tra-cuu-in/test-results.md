# Biên bản T17 — hoàn tất SQL24/10/2026, schema016

[Manifest v4](../runs/sql-completion_20261024_v4/final-manifest.json),
[lượt SQL chính](../runs/sql-completion_20261024_v4/manifest.json),
[quan sát actor](../runs/sql-completion_20261024_v4/actor-followup.json),
[policy/recipe cả hai thứ tự](../runs/sql-completion_20261024_v4/ordering2-manifest.json).
Ca kếthừa xem [biên bản v3](../runs/sql-completion_20261024_v4/previous-test-results-t17.md).
Source/schema giữ cố định;không sửa sản phẩm. Giữ lịch sử FAIL instrumentation và kết quả follow-up,không xóa.

| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| T17-K01 | [I/M] Lọc theo ngày/trạng thái/kênh/khách trả đúng đơn, không bỏ giờ cuối ngày. | Fixture ngày trước chốt, actual ngàyTT; Created/Paid2/1,cuối ngày23:59:59 có mặt,filter kết hợp1,paging51đơn50/1, raw IDs không trùng. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T17-K02 | [I] Tiền và số lượng lấy snapshot đã lưu, không menu hoặc tỷ lệ KM hiện tại. | Menu đổi giá không đổi tiền cũ/giá dòng25.000; detail4resultset trả dữ liệu snapshot. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T17-K03 | [M] Preview/in đủ đơn nhiều dòng, ghi chú tiếng Việt, phần giảm/ship/tổng/phương thức; phân trang không mất dòng. | Preview trong bộ nhớ100dòng/210entry;9trang đầu, không kiểm hình thức. Chưa đủ UI preview/in thật và đối chiếu nội dung theo trang. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |
| T17-K04 | [M] Ghi đúng tiền dòng trước giảm và số phải trả, không nhầm tiền khách đưa. | Người dùng chốt421/kháchđưa100000 rồi in MicrosoftPrinttoPDF; output1trang có1×81000=81000, tổng/phải trả/thu81000, không in100000 thành doanh thu. SQL1PT00181000, điểm+81/kho khớp; nguồn phương thức Tien mat đúng danh mục. | PASS | ../runs/pdf-print-20261025/final-manifest.json; ../runs/pdf-print-20261025/G04-HD20250421.pdf |
| T17-K05 | [M] In lại không bịa tiền đưa/thối khi DB chưa lưu; nhãn in lại rõ. | Chưa kiểm UI/print in lại với tiền đưa/thối chưa lưu. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |
| T17-K06 | [M/I] Lỗi máy in/hủy print sau commit không thay tiền/kho/điểm. | Không gửi job in; nhánh hủy/lỗi máy in sau commit chưa chạy. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |
| T17-K07 | [M/I] In lại nhiều lần không gọi checkout; không tạo thanh toán/hóa đơn mới. | Hai Print() trong bộ nhớ hoàn tất, cursor210; controller tái dùng trả9rồi18previewpages tích lũy. Chưa thao tác UI in lại và đối chiếuDB, không claim18là trang mới. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |
| T17-K08 | [M/I] Không có nút sửa tài chính đơn chốt; gọi service sửa vẫn bị chặn. | SQL sửa closed bị chặn; nút/dialog/service qua UI chưa kiểm. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |
| T17-K09 | [M] Xem đơn không tồn tại/xóa chưa chốt thông báo đúng, không crash. | SQL detail mã thiếu trả4set rỗng, không lỗi; thông báo UI chưa chạy. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |

Bốn ca SQL trướcđó ghiBLOCKED đã kiểm vàPASS;ghiBLOCKED khi agentvẫn kiểmđược làkhôngchínhxác.
Policy/recipe đồngthời haichiều vàtimeout trướccommit,legacy-invalid đã chạy v4,khôngcòn ghiNOT RUN cácnhánh này.
GUI chức năng vẫnchưađủ bằngchứng vì côngcụ click/focus;hìnhthứcSKIPPED;DPI100/150%NOT RUN.
Không suyPASS GUI từSQL hoặcfixture in-memory;khôngtick checklist gốc T01–T32.


## 25/11/2026 — Kiểm chức năng hộp chi tiết hóa đơn dựng lại (phương án B)

Môi trường: Windows 10.0.26200, .NET SDK 10.0.401, build tạm cách ly
(`OutDir` riêng vì bin chính bị app PID 25272 và VS khóa), probe reflection
`T17Probe` mở `InvoiceDetailDialog` với DTO dựng sẵn (không gọi DB/không in thật).
DPI thực tế của màn hình khi chạy: **120 (125%)**. Log:
[ui-detail-probe-20261125.log](runs/ui-detail-probe-20261125.log). Lệnh:
`dotnet build T17Probe.csproj -p:OutDir=...\out` rồi chạy `T17Probe.exe`.

| ID | Nội dung | Expected | Actual | Trạng thái |
|---|---|---|---|---|
| T17-UI-01 | Đơn đã trả có giảm/ưu đãi/điểm/tặng kèm | Có "In hóa đơn" (primary, icon printer, ngoài cùng phải), "Đóng", không "Quay lại POS"; món bind `Product`; cột Ghi chú/Quà hiện; tổng 65.000 ₫ cỡ 27pt; "Đã thu đủ · giờ ngày"; 4 ghi chú | Đúng toàn bộ; hộp 1057×724 px tại DPI120, nằm trong WorkingArea | PASS |
| T17-UI-02 | Đơn chưa trả mang đi | Có "Quay lại POS", "Đóng"; **không** nút in (giữ T17-K05); ẩn cột Ghi chú/Quà; footer "Đơn chưa thanh toán…"; không "Đã thu đủ" | Đúng toàn bộ | PASS |
| T17-UI-03 | Đơn đã hủy, không món | Chỉ nút "Đóng"; nhãn "Đã hủy"; tiêu đề "0 món" | Đúng | PASS |
| T17-UI-04 | Bố cục | Không nhãn bị cắt, không control tràn cha, nút nằm trong hộp, cao ≥36px, hộp trong màn hình | Tất cả đạt (3 ca) | PASS |

Lượt chạy đầu FAIL "nút in có icon printer" do `OrbitDialogLayout` style lại nút khi
nhận vào footer (xóa `ManagementIcon`); đã gắn lại icon sau khi tạo layout và chạy lại
đạt — không đổi nghiệp vụ.

NOT RUN (ngoài phạm vi quyền tự kiểm): bấm "In hóa đơn" mở preview PDF/WebView2 và in
thật; mở hộp từ lịch sử hóa đơn với DB thật; lỗi máy in/hủy in; hình thức trực quan
(người dùng tự kiểm); DPI 100%/150% (chỉ chạy 125%). Không ghi PASS hình thức.

### 25/11/2026 — Lịch sử hóa đơn: bỏ nút "Chi tiết / In", mở bằng bấm đúp/Enter

Cùng môi trường probe, bổ sung ca dựng `InvoicesControl` (không gọi DB, không mở
hộp). Log: [invoice-history-activation-20261125.log](runs/invoice-history-activation-20261125.log).

| ID | Nội dung | Expected | Actual | Trạng thái |
|---|---|---|---|---|
| T17-UI-05 | Hàng thao tác lịch sử hóa đơn | Không còn nút "Chi tiết / In", hàng thao tác trống, bố cục không lỗi | 0 nút, không lỗi khởi tạo | PASS |
| T17-UI-06 | Mở chi tiết | `RowActivated` gắn cho bấm đúp dòng; Enter khi chọn dòng mở cùng hộp | Đã gắn handler; nhánh Enter dùng `ShowDetail` như bấm đúp (kiểm bằng source, chưa bấm thật) | PASS (một phần) |
| T17-UI-07 | Hướng dẫn | Dòng mô tả workspace nhắc "Bấm đúp vào hóa đơn để xem chi tiết và in lại." | Đúng | PASS |

NOT RUN: bấm đúp/Enter thật trên DB thật để mở hộp, in lại; chọn dòng rỗng rồi Enter
(không có hóa đơn) — để người dùng kiểm cùng lúc xem giao diện.

### 25/11/2026 — Đổi sang mở chi tiết bằng MỘT lần bấm (theo yêu cầu)

Thay `RowActivated` (bấm đúp) bằng `Grid.CellClick`: bấm một lần vào dòng mở thẳng
`InvoiceDetailDialog`; chờ cú bấm xử lý xong rồi mới mở (BeginInvoke), có cờ
`openingDetail` chống mở lặp, bỏ qua khi danh sách đang bận. Enter khi đang chọn dòng
dùng chung `OpenDetailAsync` — hàm này bắt `UserOperationException`/
`UnauthorizedAccessException`/`ArgumentException` và ghi vào dòng trạng thái như nút
hành động cũ, tránh lỗi async void. Dòng mô tả đổi thành "Bấm vào hóa đơn để xem chi
tiết và in lại." (khóa dịch cũ đổi nội dung).

Log: [invoice-history-single-click-20261125.log](runs/invoice-history-single-click-20261125.log).
Probe T17-UI-05..07 chạy lại PASS (0 nút hành tác, RowActivated null, mô tả đúng);
T17-UI-01..04 của hộp chi tiết vẫn PASS. NOT RUN: bấm chuột thật trên lưới có dữ liệu
DB (mở hộp, đóng hộp, in lại), bấm khi lưới đang tải và hình thức — để người dùng kiểm.
