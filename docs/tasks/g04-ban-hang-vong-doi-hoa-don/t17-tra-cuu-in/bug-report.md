# Lỗi bộ kiểm thử T17 — đợt G04 ngày 23/10/2026

## Cập nhật24/10/2026

G04-TEST-04 đã sửa trong v2: đặt ngày lập trước checkout; dùng ngày thanh toán thực,
prefixHD971 riêng, dateSQL động theo SYSDATETIME, fixture lỗi thì ca phụ thuộc BLOCKED.
Không tắt trigger hoặc sửa paid dates. FixturePASS; Created/Paid2/1,filter1,paging50/1
đạt; raw IDs lưu đầy đủ trong remaining-results.json. Detail4set/snapshot/notfound
và filter giao hàng cũng đạt. Bằng chứngv1 FAIL giữ nguyên, không sửa procedure.

## G04-TEST-04 — Fixture thời gian sửa hóa đơn đã khóa; các assertion sau phụ thuộc

Trạng thái: chờ duyệt sửa fixture, chưa sửa. Không xác nhận lỗi lọc sản phẩm.

`remaining.sql` tạo/chốt HD97000002 rồi UPDATE NgayLap/NgayThanhToan. SQL Server chặn
“Hoa don da TT chi duoc cap nhat trang thai giao hang.” tại bước dựng fixture.
Runner vẫn giữ FAIL và tiếp tục phần độc lập; do batch dừng trước vòng tạo51đơn,
ca paging nhận0/0 thay50/1. Ca ngày dùng prefix HD970 rộng nên còn nhận các đơn
độc lập được tạo sau đó:7/3 thay2/1. Đây là kết quả FAIL kế thừa fixture, không là
bằng chứng query bỏ giờ cuối ngày hoặc phân trang sai. Chi tiết mã/row nằm trong
`../runs/acceptance-wave_20261023_195243_f9876e48/remaining-results.json`.

Đề xuất sau duyệt: dựng ngày lập trước checkout khi đơn còn mở; sử dụng ngày thanh
toán thật/fixture import quản trị đã có, tuyệt đối không tắt trigger bảo vệ chỉ để
sửa đơn đã trả. Dùng prefix riêng cho mỗi nhóm và tách chuẩn bị fixture khỏi assertion;
fixture thất bại thì case phụ thuộc BLOCKED, vẫn giữ lỗi gốc. Chạy lại Created/Paid,
23:59:59, filters/paging51đơn, reload/notfound, snapshot và print/in lại/lỗi máy in.

Rủi ro: fixture phải giữ các bất biến lịch sử và không tạo lối import runtime.
Không cần đổi procedure tra cứu từ bằng chứng này. Phiên bản test cũ và FAIL giữ
nguyên; không sửa lại kết quả hoặc expected hàng cũ. Print probe100dòng/13trang đã
chạy exit0 nhưng không thay nghiệm thu UI preview, hủy/lỗi máy in và in lại.

## 24/11/2026 — [UX] Phía hóa đơn bán thiếu điểm vào in rõ như phiếu nhập

Trạng thái: đã điều tra bằng source, chờ duyệt hướng sửa. Chưa đổi code.

**Hiện tượng.** Người dùng lập hóa đơn và không thấy chỗ in tương đương nút máy in
trên màn "Nhập hàng" (phiếu nhập có nút icon in ngay thanh công cụ). Hệ quả là họ
không biết hóa đơn đã in được hay in ở đâu.

**Bằng chứng.**
- Phía phiếu nhập có điểm vào in độc lập: `Modules/Inventory/PurchasesControl.cs:33-37`
  (icon `printer` trong `Workspace.SearchActions`, tooltip "In phiếu đang chọn"),
  bật theo dòng đang chọn (`State()`: `print.Enabled = row is not null`, in được mọi
  trạng thái phiếu); thêm nút "In phiếu" ở panel chi tiết (dòng ~156), trong
  `PurchaseEditorDialog` và `PurchaseReceiveDialog`.
- Phía hóa đơn chỉ có một nút chữ "Chi tiết / In" trong `RowActions`
  (`Modules/Sales/InvoicesControl.cs:37`); phải mở hộp chi tiết rồi bấm
  "Xem trước / In lại", và nút này bị khóa nếu đơn chưa trả
  (`InvoiceDetailDialog.cs:38-39`: `print.Enabled = h.Status == "Da thanh toan"`).
- Trong POS, "In tạm" chỉ xuất hiện khi đang mở một đơn
  (`SalesControl.cs:105,111,210-212`); thanh toán xong đơn bị gỡ khỏi POS, chỉ còn
  hộp hỏi Có/Không (`SalesControl.cs:883`) và câu nhắc "Tra cứu hoặc in lại tại
  Lịch sử hóa đơn." (`:908`).
- Hộp chi tiết giao hàng không có nút in (`DeliveryControl.cs`, `DeliveryDetailDialog`);
  muốn in phải "Xem hóa đơn / POS" rồi vào chi tiết.

**Nguyên nhân.** Không phải lỗi chức năng: đường in đã có và dùng chung
(`InvoicePrinting.PreviewAsync` → `InvoicePdfReport` → `PdfPreviewForm`, quyền in qua
`InvoiceService.DemandPrintAsync`). Đây là lỗi khả dụng: đầu ra in phía hóa đơn bị
ẩn sau hộp chi tiết, nhãn là chữ thay vì icon, và phụ thuộc trạng thái đã thanh toán,
nên người dùng không nhận ra — khác hẳn phiếu nhập có nút in độc lập trên thanh công cụ.

**Đề xuất sửa (chờ duyệt).**
1. `InvoicesControl`: thêm nút icon `printer` vào `Workspace.SearchActions` (cùng hàng
   tìm kiếm/làm mới, giống `PurchasesControl`), tooltip "In hóa đơn đang chọn", chỉ bật
   khi có dòng đang chọn; bấm thì `DetailAsync` + `InvoicePrinting.PreviewAsync(...,
   reprint: true)` trực tiếp, không phải mở hộp chi tiết. Giữ "Chi tiết / In" cho xem.
2. `InvoiceDetailDialog`: đổi nhãn nút thành "In hóa đơn" rõ nghĩa; cân nhắc cho đơn
   Chưa/Chờ thanh toán in "Tạm tính" (reprint:false) thay vì khóa, để ngang phiếu nhập
   — điểm này đổi hành vi T17-K05/K08 nên cần người dùng chốt riêng.
3. Tùy chọn: thêm nút "In hóa đơn" ở hộp chi tiết giao hàng khi đơn đã trả.

**Rủi ro.** Chỉ đổi UI WinForms, không đổi SQL/BLL/tiền; lỗi in không đổi hóa đơn đã
chốt; quyền in vẫn kiểm ở BLL trước khi mở preview. Nếu cho in đơn chưa trả từ lịch sử
thì phải giữ nhãn "Tạm tính"/"In lại" đúng nghĩa và không ghi gì vào DB.

**Regression dự kiến.** T17-K03/K05/K06/K07, lịch sử hóa đơn không chọn dòng/đơn
Chờ/đã trả, bố cục 125%; phần hình thức do người dùng kiểm.

### 24/11/2026 — Cập nhật hộp chi tiết hóa đơn (đã sửa theo phản hồi)

Người dùng xác nhận in có thật nhưng hộp chi tiết "hiển thị nhiều thông tin thừa"
và "nút đặt không hợp lí". Đã sửa `InvoiceDetailDialog.cs` (chỉ UI, không đổi
SQL/BLL/in):
- Dùng `OrbitDialogLayout` + `OrbitSummaryPanel` như các hộp chi tiết khác: thông tin
  khóa/giá trị thay cho khối 4 dòng chữ, bỏ dòng 0 đồng và khách vãng lai, chỉ hiện
  nguồn giảm/điểm/tặng kèm khi có; "Phải trả" là dòng nhấn.
- Bảng món: tiền định dạng `#,##0 ₫` căn phải (trước đây hiện số thô), bỏ cột "Quà"
  khi không có quà, giữ "Tên hiện tại" để không mạo nhận snapshot, đổi "Giá snapshot"
  thành "Đơn giá" cho dễ đọc.
- Hành động dồn phải theo `OrbitDialogLayout`: `Đóng` ngoài cùng, `In hóa đơn` (primary)
  chỉ hiện với đơn đã trả, `Quay lại POS` chỉ hiện với đơn còn mở (trước đây luôn hiện
  nút xám ở góc trái). Đơn chưa trả vẫn in tạm tại POS.
- Thêm khóa dịch mới trong `Translations/en.json`.

Chưa làm: nút icon in độc lập trên thanh công cụ "Lịch sử hóa đơn" (đề xuất 1 ở trên)
và cho in đơn chưa trả từ lịch sử (đề xuất 2) — vẫn chờ duyệt.
