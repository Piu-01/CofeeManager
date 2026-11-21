# Requirements — Thao tác từng dòng hạng thành viên

- Ngày: 21/11/2026
- Trạng thái: **Chờ duyệt requirements** (chưa viết plan, chưa sửa code)
- Màn hình: `Hệ thống ưu đãi → Hạng thành viên` (ADMIN), `PolicyList` trong
  `src/CafeManager.WinForms/Modules/Loyalty/LoyaltyControl.cs`.

## 1. Mục tiêu

Đổi thao tác quản lý hạng thành viên từ “sửa cả bảng chính sách một lần” sang thao tác
theo từng dòng: thêm hạng, sửa hạng bằng double-click, xóa hạng bằng icon thùng rác;
bỏ nút “Tải lại” trên màn này. Danh sách luôn xếp tăng dần theo ngưỡng điểm.

Không đổi công thức tích điểm, không đổi quy tắc ưu đãi của hóa đơn, không đổi dữ liệu
khách/hóa đơn hiện có. Đây là task UI trên nền contract chính sách hạng đã có.

## 2. Hiện trạng liên quan

- `PolicyList` đang dùng `Configure(...)` với profile `Ranks`, `Search.Visible=false`,
  `Paginated=false`, và một action duy nhất “Thay chính sách” mở grid toàn bộ hạng.
- Nút “Tải lại” trên màn này là nút refresh dựng sẵn của `OrbitRecordListControl`;
  với profile `Ranks` nó được chuyển từ vùng filter sang vùng action dòng
  (`OrbitRecordListControl.cs` dòng 88–92).
- Danh sách hạng đọc từ `sp_LoyaltyPolicyGet` đã `ORDER BY DiemToiThieu`; mỗi lần lưu
  xong `PolicyList` gọi lại `Reload()`.
- Ghi chính sách hiện là một transaction thay toàn bộ qua `sp_LoyaltyPolicyReplace`:
  bắt buộc có preview khớp nội dung (hash), khóa theo `LoyaltyPolicyState.Revision`,
  tính lại `KHACHHANG.MaHang` theo ngưỡng mới, thêm hạng mới, xóa hạng không còn,
  ghi `G03OperationReceipt`. `HANGTHANHVIEN` chỉ có `MaHang`, `DiemToiThieu`,
  `PhanTramGiam` (không có cột tên riêng).
- Validate hiện có (`LoyaltyService.NormalizeTiers`): ít nhất 1 hạng; phải có hạng
  ngưỡng 0; mã hạng duy nhất, `^[A-Z0-9]{1,5}$`; ngưỡng duy nhất, ≥ 0; giảm % nguyên
  0–100.

## 3. Phạm vi

### Làm

1. Bỏ nút “Tải lại” ở màn Hạng thành viên (chỉ màn này; các danh sách khác giữ nguyên).
2. Bỏ action “Thay chính sách” và dialog sửa cả bảng.
3. Thêm action chính “Thêm” để tạo một hạng:
   - Nhập: mã hạng, ngưỡng điểm, giảm %.
   - Lưu xong danh sách tự hiển thị lại theo thứ tự ngưỡng điểm tăng dần.
4. Mỗi dòng có icon thùng rác ở cột cuối; bấm icon mở xác nhận xóa đúng hạng của dòng.
5. Double-click một dòng mở dialog sửa hạng đó với dữ liệu hiện tại:
   mã hạng, ngưỡng điểm, giảm %.
6. Giữ nguyên cơ chế ghi an toàn hiện có: preview SQL trước khi lưu, khóa revision,
   tính lại hạng khách trong cùng transaction, ghi receipt, reload sau khi lưu.
7. Thông báo rõ khi thao tác bị chặn (mã/ngưỡng trùng, xóa hạng ngưỡng 0, hạng đã
   bị người khác đổi, mất quyền ADMIN).

### Không làm

- Không thêm cột “tên hạng” vào schema; “tên” trong yêu cầu được hiểu là mã hạng
  hiển thị ở cột “Hạng”.
- Không đổi công thức điểm/ưu đãi, không đổi `Insert_Data.sql`, không thêm migration.
- Không đổi quy tắc “KM thay hạng” hay task cộng dồn ưu đãi (task riêng đang chờ duyệt).
- Không đổi màn Khuyến mãi, Điểm khách hàng, POS, hóa đơn.
- Không thêm tìm kiếm/phân trang cho màn hạng (giữ nguyên như hiện tại).
- Không tự chạy đợt test chức năng/GUI khi chưa được duyệt riêng.

## 4. Giả định và quyết định thiết kế

1. **Tái dùng contract thay toàn bộ chính sách**: mỗi thao tác thêm/sửa/xóa sẽ đọc
   danh sách hạng hiện tại, áp đúng một thay đổi, rồi gọi `PolicyPreviewAsync` +
   `PolicySaveAsync` với **toàn bộ danh sách kết quả**. Không thêm procedure
   `sp_LoyaltyTierInsert/Update/Delete`, không thêm migration.
   - Hệ quả: mỗi thao tác vẫn tính lại hạng toàn bộ khách và tăng revision chính sách
     như hiện nay; đây là contract đã có, không phải thay đổi mới.
2. **Preview vẫn là cổng trước khi ghi** vì SQL bắt buộc hash preview khớp nội dung.
   Trong dialog Thêm/Sửa có nút “Xem lại từ SQL” như dialog hiện tại; người dùng xem
   số khách bị đổi hạng rồi mới Lưu. Nút này nằm trong dialog, **khác** nút “Tải lại”
   bị bỏ ở toolbar.
3. **Xóa**: vì nội dung xóa đã xác định ngay khi bấm icon, hệ thống tự chạy preview
   trước, hiện số khách sẽ được xếp lại hạng, rồi mới mở hộp xác nhận. Khách đang ở
   hạng bị xóa sẽ được xếp lại theo ngưỡng còn lại (không chặn xóa chỉ vì có khách).
4. **Không cho xóa hạng ngưỡng 0** khi còn hạng khác, và không cho xóa hạng cuối cùng;
   chặn ngay ở UI với thông báo rõ, không gửi SQL.
5. **Đổi mã hạng** được phép: transaction thêm mã mới, xếp lại khách theo ngưỡng, rồi
   xóa mã cũ; khách thuộc hạng đó chuyển sang mã mới nếu ngưỡng vẫn bao phủ điểm của họ.
6. Ngưỡng điểm nhập số nguyên ≥ 0; giảm % nhập số nguyên 0–100; mã tự viết hoa, tối đa
   5 ký tự A–Z/0–9. Các kiểm tra trùng mã/trùng ngưỡng/thiếu hạng 0 do
   `NormalizeTiers` và SQL quyết định, UI hiển thị nguyên văn lỗi.
7. Xung đột revision khi người khác vừa đổi chính sách: hiện lỗi “Chính sách đã thay đổi.”,
   không tự ghi đè; người dùng đóng dialog, danh sách tải lại khi mở lại màn/thao tác tiếp.

## 5. Luồng UI chi tiết

### 5.1 Toolbar

- Không còn nút “Tải lại”, không còn “Thay chính sách”.
- Còn nút chính “Thêm” (icon plus) ở vùng action chính.
- Không có nút Sửa/Xóa trên toolbar; hai thao tác này chỉ từ dòng.

### 5.2 Thêm hạng

- Dialog “Thêm hạng thành viên”: Mã hạng, Ngưỡng điểm, Giảm %.
- Nút “Xem lại từ SQL” tính trên danh sách hiện tại + hạng mới; hiện số khách đổi hạng
  và bản xem trước ngắn (ngưỡng/% của toàn bộ hạng sau thay đổi).
- Lưu khi preview khớp nội dung hiện tại; xác nhận “Thêm hạng và tính lại hạng khách
  trong một transaction?”.
- Lưu xong đóng dialog, reload danh sách; hạng mới xuất hiện đúng vị trí theo ngưỡng.

### 5.3 Sửa hạng (double-click)

- Chỉ mở khi đã chọn/double-click dòng; dialog điền sẵn mã, ngưỡng, %.
- Sửa bất kỳ trường nào làm preview cũ mất hiệu lực; phải “Xem lại từ SQL” lại.
- Xác nhận trước khi ghi; lưu xong reload, danh sách sắp xếp lại nếu ngưỡng đổi.

### 5.4 Xóa hạng (icon thùng rác)

- Bấm icon thùng rác ở đúng dòng mở hộp “Xóa hạng …?” kèm số khách sẽ được xếp lại.
- Hủy thì không ghi gì; xác nhận thì ghi một transaction và reload.
- Nếu hạng là ngưỡng 0 hoặc hạng duy nhất: báo “Phải giữ một hạng có ngưỡng 0 điểm.”
  (hoặc thông điệp tương đương) và không mở bước xác nhận ghi.

### 5.5 Trạng thái

- Trong lúc tải/ghi: khóa action, không cho bấm lặp.
- Lỗi validate hiện gần trường/dưới dialog; lỗi DB/quyền hiện ở `State` hoặc status
  của dialog, giữ nguyên input để sửa.
- Sau lưu thành công: dòng/caption cập nhật theo dữ liệu SQL vừa đọc lại, không tự
  suy ra từ cache.

## 6. Thay đổi kỹ thuật dự kiến

| Lớp | File/khu vực | Nội dung |
|---|---|---|
| UI chung | `src/CafeManager.WinForms/UI/OrbitRecordListControl.cs` | Với profile `Ranks`, ẩn nút refresh dựng sẵn (không ảnh hưởng danh sách khác) |
| UI | `src/CafeManager.WinForms/Modules/Loyalty/LoyaltyControl.cs` (`PolicyList`) | Bỏ action “Thay chính sách”; thêm “Thêm”, `AddRowDeleteColumn()`, `RowDeleteRequested`, `RowActivated`; dialog thêm/sửa một hạng; xóa một hạng; tái dùng `PolicyAsync`/`PolicyPreviewAsync`/`PolicySaveAsync` |
| BLL/DAL/SQL | — | Không đổi; tái dùng `NormalizeTiers`, `sp_LoyaltyPolicyGet/Preview/Replace` |

Không sửa `.Designer.cs`; dialog dựng bằng code như các editor hiện có.

## 7. Ca nghiệm thu dự kiến (chạy sau khi được duyệt đợt test riêng)

| Mã | Nội dung |
|---|---|
| AR-01 | Màn Hạng thành viên không còn nút “Tải lại”, không còn “Thay chính sách”; có nút “Thêm” |
| AR-02 | Thêm hạng mới ngưỡng giữa hai hạng: lưu thành công, danh sách xếp tăng dần đúng vị trí, DB có dòng mới |
| AR-03 | Thêm trùng mã hoặc trùng ngưỡng: bị chặn, thông báo rõ, DB không đổi |
| AR-04 | Thêm hạng thiếu ngưỡng 0 (khi xóa/sửa làm mất) bị chặn đúng quy tắc |
| AR-05 | Double-click dòng mở đúng hạng, sửa ngưỡng/giảm %: lưu thành công, khách được xếp lại hạng theo điểm, danh sách sắp xếp lại |
| AR-06 | Sửa mã hạng: khách của hạng cũ chuyển sang mã mới, hạng cũ không còn, không mồ côi FK |
| AR-07 | Icon thùng rác đúng dòng: xác nhận xóa hạng có khách → khách xếp lại hạng còn lại; hủy → không đổi |
| AR-08 | Xóa hạng ngưỡng 0 / hạng cuối: bị chặn ở UI, DB giữ nguyên |
| AR-09 | Người khác đổi chính sách giữa preview và lưu: báo “Chính sách đã thay đổi.”, không ghi đè |
| AR-10 | Tài khoản CASH không thấy màn Hạng thành viên; ADMIN thao tác bình thường |
| AR-11 | Regression: màn Khuyến mãi/Điểm khách hàng vẫn có nút “Tải lại” và hành vi cũ; ưu đãi hóa đơn không đổi |
| AR-12 | Hình thức (màu icon, hover, spacing) | SKIPPED theo phân công — người dùng tự kiểm |

## 8. Tiêu chí hoàn thành

- Đủ 5 thao tác UI: bỏ Tải lại, thêm hạng, sửa hạng bằng double-click, xóa hạng bằng
  icon, danh sách tự sắp xếp tăng dần theo ngưỡng sau mỗi lần ghi.
- Không thêm migration/procedure; mọi ghi vẫn đi qua preview + transaction chính sách.
- Build sạch (0 error) và các ca AR-01..AR-11 PASS trên SQL Server thật/DB test riêng
  trong đợt test được duyệt; AR-12 do người dùng tự kiểm.
- Không đổi dữ liệu ngoài phạm vi hạng thành viên; lịch sử điểm/hóa đơn giữ nguyên.

## 9. Điểm cần người dùng xác nhận khi duyệt

1. “Tên hạng” = mã hạng hiện có (không thêm cột tên riêng) — đúng ý?
2. Giữ bước “Xem lại từ SQL” trong dialog Thêm/Sửa để xem số khách đổi hạng trước khi
   lưu — đúng ý, hay muốn bỏ để lưu ngay?
3. Cho phép xóa hạng đang có khách (khách tự xếp lại hạng theo điểm) — đúng ý?
