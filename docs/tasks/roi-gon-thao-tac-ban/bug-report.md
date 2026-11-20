# Bug report — roi-gon-thao-tac-ban

- Ngày báo: 20/11/2026 (người dùng dùng thử bản Debug 20:47, ảnh chụp tab Bàn và tab Khu vực)
- Phạm vi: chỉ 2 lỗi dưới đây; chưa sửa gì cho tới khi được duyệt.
- Trạng thái: **chờ duyệt đợt bug** (bug-investigation-workflow)

## Bug RG-01 — Không hiển thị icon thùng rác trên từng dòng

**Hiện tượng (ảnh người dùng):** tab Bàn và tab Khu vực chỉ có các cột MÃ / BÀN / KHU VỰC /
TRẠNG THÁI / ĐƠN MỞ; sau cột cuối là khoảng trống, không có icon thùng rác nào dù đang đăng
nhập ADMIN.

**Nguyên nhân (đã có bằng chứng code):** cột `Delete` vẫn được thêm và bind đúng (probe kỹ thuật
báo `DeleteColumnWidth=55` ở DPI 125%), nhưng phần vẽ cell dùng sai nguồn tên cột:

- `src/CafeManager.WinForms/UI/OrbitDataGridView.cs` — nhánh vẽ cell chung lấy
  `var property = Columns[e.ColumnIndex].DataPropertyName;` rồi so `property == "Delete"`.
- Cột được tạo trong `OrbitRecordListControl.AddRowDeleteColumn()` với `DataPropertyName = ""`
  (cố ý, để cột không bind dữ liệu), nên `property` là chuỗi rỗng → nhánh icon không bao giờ chạy;
  cell rơi xuống `TextRenderer.DrawText` với chuỗi rỗng → dòng trống.
- `OrbitDataGridView.ApplyMetrics` đã dùng helper `Property(column)` (fallback `Column.Name`) nên
  cột có kích thước đúng; chỉ riêng nhánh paint là chưa dùng helper.

**Đề xuất sửa (tối thiểu):** trong nhánh paint chung đổi sang `Property(Columns[e.ColumnIndex])`
(helper private có sẵn). Áp dụng cho cả `Delete`; OrderLines đã dùng `Property(...)` nên không đổi.

**Phạm vi ảnh hưởng:** chỉ cột `Delete` ở màn Bàn/Khu vực. Không ảnh hưởng cột khác (các cột thật
đều có `DataPropertyName` trùng `Name`).

**Rủi ro:** rất thấp; đổi một biểu thức lấy tên cột.

## Bug RG-02 — Bấm bàn từ màn Bàn nhưng POS hiển thị đơn/ngữ cảnh khác bàn vừa chọn

**Hiện tượng (người dùng):** bấm card/dòng bàn → chuyển sang trang Bán hàng, nhưng nội dung
không tương ứng bàn/đơn vừa bấm (ví dụ bấm bàn trống B0002 mà POS lại ở ngữ cảnh bàn khác/đơn cũ).

**Nguyên nhân (race, có bằng chứng code):**

1. `Forms/MainForm.cs` (`OpenSalesTableAsync`, dòng 150–154): `await Navigate(Sales)` rồi gọi
   `sales.OpenTableAsync(code)`.
2. Khi SalesControl được đưa lên, `SalesControl.VisibleChanged` (dòng 181) chạy `ReloadAll` dạng
   async; `ReloadAll` đặt `busy = true` ngay từ đầu (dòng 217) và tiếp tục nạp nền.
3. `Navigate` trả về trước khi `ReloadAll` xong → `OpenTableAsync` gặp guard `if (busy || unknown) return;`
   (dòng 506–508) và **thoát im lặng**, không mở bàn vừa bấm.
4. `ReloadAll` chạy tiếp: nếu POS còn đơn cũ thì `LoadOrder(order cũ)`, nếu không thì
   `StartNewOrderAsync(false)` (dòng 229–234) tự chọn bàn trống đầu tiên → POS hiển thị ngữ cảnh
   khác bàn người dùng vừa chọn.

Lỗi này có từ trước task này (không do bỏ nút "Chọn bàn"), nhưng lộ ra khi dùng luồng bấm bàn.
Cùng đường gọi `OpenTableAsync` còn từ hộp chọn bàn ở POS (đã có guard `busy` tương tự nên an toàn).

**Đề xuất sửa (không đổi contract SQL/BLL):**

- `SalesControl`: giữ Task của lần nạp gần nhất, ví dụ `private Task loading = Task.CompletedTask;`
  và cho 2 handler `Load`/`VisibleChanged` gán `loading = Guard(ReloadAll); await loading;`.
- Thêm `public async Task OpenTableAfterLoadAsync(string code)`: nếu `loading` chưa xong thì
  `await` (nuốt lỗi nạp), sau đó nếu còn sống và `!busy && !unknown` thì gọi `OpenTableAsync(code)`.
- `MainForm.OpenSalesTableAsync` gọi method mới thay vì `OpenTableAsync` trực tiếp.
- Giữ nguyên `OpenTableAsync` cho các đường khác (tự mở bàn ở POS, MainForm.OpenSalesOrderAsync
  không đổi).

**Phạm vi ảnh hưởng:** luồng mở bàn từ màn Bàn (card/dòng). Không đổi hành vi tự mở bàn ở POS,
không đổi T34/trả bàn/checkout.

**Rủi ro:** nếu `ReloadAll` lỗi mạng thì vẫn mở bàn sau khi await (đúng ý người dùng), lỗi hiển thị
qua `state` như hiện tại; nếu người dùng bấm liên tiếp nhiều bàn, `Navigate` đang bận sẽ bỏ lượt
bấm thứ hai — giữ nguyên hành vi hiện có.

## Đề nghị

Duyệt cả 2 fix trên như một đợt bug. Sau khi duyệt: sửa → build → chạy lại probe (bổ sung kiểm tra
paint/column name và thứ tự mở bàn khi `loading` chưa xong) → ghi `test-results.md`. Đợt kiểm chức
năng/GUI thật (SQL, phiên bàn, quyền) vẫn cần duyệt riêng theo quy trình.
