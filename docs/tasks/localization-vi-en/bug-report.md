# Đợt bug localization VI/EN — đã duyệt, sửa và regression

**Cập nhật 10/11/2026:** người dùng đã duyệt batch LOC-B01–B03; cả ba đã sửa và
regression PASS theo phạm vi. Xem `regression-results.md` và evidence
`runs/debug-b01-b03-20261110/`. Các phần dưới giữ nguyên báo cáo điều tra ban đầu,
không xóa kết quả FAIL cũ. Native và nghiệm thu toàn task vẫn chưa đủ.

Ngày 10/11/2026. Phát hiện trong đợt test đã được người dùng duyệt. **Chưa sửa
source, migration hoặc test cũ**; không thay expected để chuyển lỗi app thành PASS.
Source trước/sau đợt khớp, xem `runs/acceptance-20261110/source-before.csv` và
`source-after.csv`. Báo cáo chung: `report.md`.

## LOC-B01 — Cột số lượng còn `SL` khi dùng EN

- **Hiện tượng / tái hiện:** chọn EN, vào POS. Grid dòng món hiển thị `SL` cạnh các
  cột `ITEMS`, `UNIT PRICE`. Probe trên control WinForms thật ghi
  `module.Sales.text`, Language=`en`, Banned=`["SL"]` trong `probe-results.json`.
- **Nguyên nhân đã có bằng chứng:** literal `"SL"` không bọc localization tại
  `src/CafeManager.WinForms/Modules/Sales/OrderPanel.cs:10`. Cùng literal ở
  `Modules/Sales/InvoiceDetailDialog.cs:16` cũng cần sửa trong cùng nguyên nhân;
  điểm này xác định bằng source, chưa ghi PASS/FAIL chạy dialog chi tiết riêng.
- **Đề xuất sửa:** dùng `L.T("SL")` ở hai vị trí; thêm `"SL": "QTY"` vào resource,
  giữ chữ VI như cũ. Bổ sung `SL` vào danh sách audit các nhãn không dấu và ca kiểm
  EN trên cả POS/chi tiết hóa đơn. Không đổi tên/property của cột Quantity.
- **Ảnh hưởng/rủi ro:** chỉ chữ cột; kiểm độ rộng header và binding ở 125%. Không
  thay quantity, request, giá snapshot hoặc thao tác tăng/giảm.

## LOC-B02 — Nút in trên preview vẫn là `In…` khi dùng EN

- **Hiện tượng / tái hiện:** chọn EN, mở preview hóa đơn đã trả. Caption cửa sổ là
  `Preview / Reprint · …` nhưng nút là `In…`. Bằng chứng chạy:
  `invoice-preview.text`, Language=`en`, Banned=`["In…"]` trong `probe-results.json`.
- **Nguyên nhân đã có bằng chứng:** `Forms/InvoicePrintPreviewForm.cs:18` gán
  literal `"In…"` trực tiếp. Audit hiện dựa nhiều vào dấu tiếng Việt; `In` không dấu
  và dấu ba chấm không làm nó trở thành một chuỗi có dấu, nên bị lọt.
- **Đề xuất sửa:** bọc `L.T("In…")`, thêm EN `Print…`; audit nhận diện literal này
  và regression caption/nút preview VI/EN. Giữ handler, owner và trình tự in.
- **Ảnh hưởng/rủi ro:** chỉ chữ nút. Không tự gửi lệnh in lại hoặc checkout; kiểm
  lại nút không chốt tài chính và lỗi máy in không đổi trạng thái hóa đơn.

## LOC-B03 — Lỗi cấu hình login bị giữ EN khi chuyển về VI

- **Hiện tượng / tái hiện component:** đặt English; tạo login với thông báo cấu
  hình đúng theo đường gọi Program; nhập username/password fixture; chọn VI.
  Title/nhãn đổi về VI nhưng status vẫn là:
  `Connection not configured. Configure the application account before loading data.`
  Expected: `Chưa thiết lập kết nối. Cần cấu hình tài khoản ứng dụng trước khi tải dữ liệu.`
- **Bằng chứng:** `login-relocalize-config-error`,
  Language=`English→Vietnamese` trong `probe-results.json`. Ca giữ input PASS ở
  cả hai chiều. Native app đọc được accessibility ở EN nhưng click/Tab bị giới hạn
  công cụ; không gọi bằng chứng component này là thao tác click native đã PASS.
- **Nguyên nhân đã chứng minh:** `Program.cs:32,41,43` gọi `L.T` trước khi chuyển
  chuỗi vào ApplicationSessionContext/LoginForm. `LoginForm.cs:60` cache chính
  chuỗi đã dịch đó. `Relocalize` ở dòng 50 tra tiếp qua `L.T(configurationErrorText)`;
  L.T dùng key VI nên không thể biến một value EN đã cache về VI.
- **Đề xuất sửa:** Program giữ nguyên ba template VI và chỉ dịch ở LoginForm theo
  ngôn ngữ hiện tại. Không thêm tra ngược từ EN về VI. Thêm regression khởi động
  bằng EN rồi EN→VI→EN cho thiếu kết nối, cấu hình sai và cấu hình DEV không hợp lệ;
  kiểm input và trạng thái nút không đổi.
- **Ảnh hưởng/rủi ro:** đường thông báo lỗi cấu hình trước đăng nhập và sau logout;
  không đổi connection string, xác thực, quyền hoặc dữ liệu preference.

## Phân loại riêng các cảnh báo layout của probe

Bốn entry `layout.home.1280/1366` VI/EN trong probe đầu có Status=`FAIL` vì oracle
ban đầu yêu cầu mọi Button.Visible phải nằm ngay trong WorkingArea. Đây chưa đủ
bằng chứng lỗi app: nút ở dưới vùng cuộn vẫn có Visible=true.

Giữ nguyên cả bốn kết quả đầu. Ca bổ sung `layout.scroll-reachability.1280/1366`
VI/EN dùng AutoScroll thật đưa từng nút vào vùng hiển thị: **4/4 PASS** tại DPI 120.
Vì vậy bốn entry đầu được phân loại **cảnh báo do giới hạn oracle probe**, không
đưa vào batch sửa app và không sửa bố cục để làm chúng PASS. Kiểm kỹ thuật này
không thay nghiệm thu thị giác hoặc toàn bộ thao tác cuộn native bằng chuột.

## Phạm vi xin duyệt một lần

Sửa LOC-B01, LOC-B02, LOC-B03; resource/audit và các ca regression đúng ba nguyên
nhân trên. Không SQL/migration, không đổi nghiệp vụ, không mở rộng refactor UI.
Sau sửa: Debug/Release, unit + audit, component giữ input/chuyển ngôn ngữ, POS và
chi tiết/preview VI/EN. Thử native nếu geometry/input đã khả dụng; phần chưa chạy
vẫn ghi rõ. Chỉ bắt đầu debug sau khi người dùng duyệt cả đợt này.
