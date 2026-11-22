# Plan t03 — UI danh sách và editor khuyến mãi

Phụ thuộc t02. Theo `mockup.html` đã duyệt; dùng `OperationEditorDialog` (đã có cơ chế
kháng gửi lặp/đối chiếu) + control Orbit, không tạo Form mới ngoài luồng dialog chung.

## Bước 1 — Danh sách (`PromotionList` trong `LoyaltyControl.cs`)

- `Configure` còn 6 cột: `Code` (Mã), `Name` (Chương trình), `Offer` (Ưu đãi),
  `Period` (Thời gian), `Audience` (Đối tượng), `Status` (Trạng thái).
- Cột Ưu đãi dựng từ `Kind`: `Giam` → "Giảm {Discount}%"; `Tang` →
  "Tặng {GiftItemCount} món · {GiftQuantity} phần" (badge `OrbitBadge` nếu grid hỗ
  trợ cell painting hiện có; nếu không dùng text màu caramel).
- Cột Thời gian: `dd/MM – dd/MM`, KT null → `dd/MM – hết năm`, thêm dòng phụ
  "Lặp hằng năm" bằng tooltip `Grid.Columns["Period"].ToolTipText`.
- Cột Trạng thái: `DangChay` = "Áp dụng" (xanh), `TamDung` = "Tạm ngưng" (xám),
  `HetHan` = "Hết hạn" (đỏ nhạt); chữ + màu như quy ước chung.
- Bỏ `AddAction("Bật / Tạm dừng")` và `AddAction("Preview ưu đãi")`; xóa hẳn
  `ToggleState()` và `Preview()` cùng các helper chỉ chúng dùng (CustomerPicker
  import nếu không còn dùng).
- Toolbar giữ Thêm/Sửa/Xóa cho ADMIN; CASH chỉ xem.

## Bước 2 — Editor

Giữ `OperationEditorDialog`, đặt tiêu đề "Thêm/Sửa khuyến mãi", thân một cột có
các nhóm theo mockup; thêm helper cục bộ `AddSection(dialog,string)` (Label tiêu đề
nhóm qua `AddNote`/`OrbitDialogField` rỗng, cỡ chữ Body đậm).

1. **Thông tin chung**
   - Tên chương trình: `AddText`.
   - Trạng thái: ComboBox chỉ `DangChay`("Áp dụng")/`TamDung`("Tạm ngưng");
     giá trị `Nhap` cũ được hiển thị và lưu thành `DangChay`.
2. **Thời gian áp dụng**
   - `MaskedTextBox` mask `00/00` + `OrbitDialogField` "Từ ngày"; bắt buộc, validate
     01–31/01–12 (ngày hợp lệ theo tháng/năm 2000).
   - "Đến ngày" cùng mask; CheckBox "Không đặt ngày kết thúc (áp đến hết năm)".
   - Ghi chú: "Lặp hằng năm theo ngày/tháng; ví dụ 20/12 – 10/01 sẽ áp qua năm mới."
   - Không dùng `OrbitDateField` (tránh lộ năm); giá trị gửi BLL là
     `DateOnly(2000,m,d)` / `DateOnly(2001,m,d)` khi vắt năm.
3. **Nội dung ưu đãi**
   - Segmented 2 lựa chọn Giảm giá / Tặng món: dùng hai `OrbitButton` (toggle màu
     theo trạng thái chọn) hoặc `OrbitSectionTabs`; đổi panel hiển thị.
   - Giảm giá: ô số nguyên 0–100; `PromotionItemPicker` (xem bước 3) với tiêu đề
     "Món được giảm"; tóm tắt "Đang chọn N món · bỏ trống = tất cả món".
   - Tặng món: `PromotionItemPicker` chế độ quà có cột **Số lượng** cho dòng đã
     tick; tóm tắt "N món · M phần"; ghi chú "Tặng một lần mỗi hóa đơn, giá 0 ₫,
     vẫn trừ kho."
4. **Đối tượng khách**
   - 3 `RadioButton` trong `FlowLayoutPanel` (Tất cả khách / Theo hạng / Khách
     vãng lai) kèm mô tả phụ; chỉ chọn Theo hạng mới bật phần hạng.
   - Hạng: `CheckedListBox` các hạng từ `PolicyAsync` + dòng "Tất cả hạng"
     (check = bỏ chọn hết); bỏ trống = mọi hạng.
5. Nút Hủy / Lưu khuyến mãi do dialog cung cấp.

`dialog.CaptureWrite`: đọc form → `PromotionRequest` (Kind, Start/End đã chuẩn năm,
Discount/Gifts/Items/Tiers/Audience/Status) → `LoyaltyService.Normalize` qua
`SavePromotionAsync`; giữ nguyên `current?.Revision` và Code. Lỗi validate báo bằng
`ArgumentException` như hiện tại.

## Bước 3 — `PromotionItemPicker` (control cục bộ trong module Loyalty)

- `UserControl` gồm ô tìm kiếm (`OrbitField.SelectAllOnEntry`), `DataGridView` cột
  Chọn/Mã/Món/Giá (và SL khi chế độ quà), tóm tắt dưới.
- Nạp từ `ProductService.ListAsync(..., true)` một lần khi mở dialog; lọc client-side
  bằng `DefaultView.RowFilter` (escape `'`, `%`, `_`, `[` như 039).
- API: `SetItems(IEnumerable<string>)`, `SetGifts(IEnumerable<PromotionGift>)`,
  `ReadItems()`, `ReadGifts()` (gộp món trùng, SL 1–999), `Changed` event.
- Tick/bỏ tick cập nhật tóm tắt; số lượng chỉ bật khi tick; DPI 125% dùng
  `OrbitManagementTheme.Px`.
- Gắn `SelectAllOnEntry` cho ô tìm và ô số lượng (quy ước 26/10/2026).

## Bước 4 — i18n và dọn dẹp

- Thêm key mới: "Áp dụng", "Tạm ngưng", "Không đặt ngày kết thúc (áp đến hết năm)",
  "Lặp hằng năm…", "Tặng món", "Món được giảm", "Tặng một lần mỗi hóa đơn…",
  "Tất cả hạng", "Khách vãng lai", "Đang chọn {0} món", "{0} món · {1} phần",
  v.v. vào `Translations/en.json` và file ngôn ngữ còn lại nếu có.
- Xóa code Preview/Bật-Tạm dừng không còn dùng; không xóa `SetStateAsync` ở BLL/DAL
  (giữ tương thích server) nhưng không còn call site UI.

## Kiểm chứng

- Build Debug/Release.
- Mở màn Khuyến mãi: tạo mới Giảm với 2 món, sửa sang Tặng 2 món SL 2/1, mở lại
  thấy đúng; đổi Tạm ngưng, danh sách hiển thị đúng; không còn nút Preview/Bật.
- Không chạy test DB đầy đủ ở task này; hình thức do người dùng nghiệm thu.
