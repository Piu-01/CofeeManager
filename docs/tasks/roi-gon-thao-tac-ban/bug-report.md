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

## Trạng thái (20/11/2026)

Đã sửa cả 2 bug theo đợt được duyệt và xác nhận lại bằng build + probe kỹ thuật:

- RG-B02 build 0 error (`runs/row-action-20261120/build-rg.log`).
- RG-P06/P07/P08 PASS: cột `Delete` phân giải đúng property, icon được vẽ ở ô (ảnh
  `runs/row-action-20261120/probe-delete-cell.png`), `OpenTableAfterLoadAsync` chờ nạp xong mới mở bàn.
- Chi tiết trong `test-results.md` mục "Đợt bug RG-01 / RG-02".

Kiểm chức năng/GUI thật (SQL, phiên bàn, quyền) vẫn NOT RUN, chờ duyệt đợt test riêng.

## Bug RG-03 — Ô chọn bàn / nhãn bàn lệch với bàn-đơn đang mở ở POS

- Ngày phát hiện: 21/11/2026, trong đợt test chức năng RG-F01–F06 (đã được duyệt chạy).
- Trạng thái: **đã sửa và regression PASS ngày 21/11/2026** (đợt bug được duyệt; xem mục "Trạng thái RG-03" cuối bài).

**Hiện tượng (có bằng chứng UIA, DB test `CoffeeManager_Test_RG_20261121_a_ui`):**

1. Mở đơn qua bấm thẻ bàn ở màn Bàn (B0008, đơn HD26093450): POS mở đúng đơn (header
   "Tại quán · Bàn B0008", tổng 125.000 ₫) nhưng ô chọn bàn và nhãn trạng thái vẫn hiển thị
   bàn trước đó "Ban 02 · Trống" — `runs/ui_20261121/09-f02-card-ban08.txt`.
2. Đang mở đơn HD26093335 (B0003), bấm một dòng món để mở ô ghi chú (nháp), rồi chọn bàn khác
   (Ban 09 · Trống): hộp "Bỏ input chưa ghi để tiếp tục?" hiện đúng; bấm **Hủy** thì POS giữ
   đơn B0003 và giữ ghi chú nháp, nhưng ô chọn bàn **ở lại "Ban 09 · Trống"** thay vì trả về
   Ban 03 — `runs/ui_20261121/25-f02b-cancel-kept-order.txt`, `25b-f02b-combo-state.txt`
   (header "Tại quán · Bàn B0003" + combo "Ban 09 · Trống" cùng lúc).

**Nguyên nhân (theo code, chưa sửa):**

- `SalesControl.OpenTableAsync` (dòng ~519–533): khi `ConfirmInputAsync("chọn bàn khác")` trả
  `false`, method `return` mà không gọi `SetTableSelection`/`UpdateTableStatus` để trả ô chọn bàn
  về bàn đang mở; giá trị combo đã bị binding đổi sang bàn người dùng vừa bấm.
- Luồng mở đơn từ màn Bàn (`MainForm.OpenSalesTableAsync` → `OpenTableAfterLoadAsync` →
  `OpenTableAsync` → `OpenOrderAsync`/`LoadOrder`) chỉ nạp `order` và gọi `UpdateTableStatus()`,
  nhưng `UpdateTableStatus` đọc lại `table.SelectedValue` cũ nên nhãn bàn dựa trên lựa chọn cũ,
  không dựa trên `order.Header.TableCode`; combo cũng không được đồng bộ theo bàn của đơn.

**Đề xuất sửa (tối thiểu, không đổi SQL/contract):**

- Trong `OpenTableAsync`, lưu `table.SelectedValue` trước khi gọi `ConfirmInputAsync`; nếu người
  dùng Hủy thì `SetTableSelection(previous)` + `UpdateTableStatus()` rồi `return`.
- Khi mở đơn thành công (`OpenOrderAsync`/`LoadOrder` có `order != null`), gọi
  `SetTableSelection(order.Header.TableCode)` và cho nhãn bàn hiển thị theo bàn của đơn
  (ưu tiên `order.Header.TableCode` khi `order` khác null).

**Phạm vi ảnh hưởng:** chỉ phần hiển thị/đồng bộ ô chọn bàn ở POS; không đổi dữ liệu, phiên bàn,
quyền hay luồng tạo đơn. Chỉ sửa sau khi đợt bug được duyệt.

## Trạng thái RG-03 (21/11/2026)

Đã sửa sau khi đợt bug được duyệt (`src/CafeManager.WinForms/Modules/Sales/SalesControl.cs`):

- `LoadOrder`: khi đơn có `Header.Channel == "Tai quan"` và `TableCode` khác null thì gọi
  `SetTableSelection(TableCode)` → combo chọn bàn luôn khớp bàn của đơn vừa mở.
- `OpenTableAsync`: lấy `previous = ActiveTableCode()` trước khi mở/hỏi; nếu `ConfirmInputAsync`
  trả false thì `SetTableSelection(previous)` + `UpdateTableStatus()`; sau khi mở đơn xong cũng
  đồng bộ lại theo bàn thực của ngữ cảnh.
- Thêm helper `ActiveTableCode()`: đọc bàn của đơn đã lưu (ưu tiên) hoặc bàn của đơn mới đang mở,
  không đọc combo (combo có thể đã bị binding đổi).

Kiểm chứng (chi tiết `test-results.md` mục "Đợt regression RG-03"):

- RG-B03 build 0 error (`runs/ui_20261121/29-build-rg3.log`).
- RG-F08 PASS: bấm thẻ Ban 08 → combo/nhãn "Ban 08 · HD26093450"/"Ban 08 · Đang dùng"
  (`31-rg3-a-open-ban08.txt`).
- RG-F09 PASS: Hủy bỏ nháp khi chọn Ban 09 → giữ HD26093335 và combo trả về Ban 03
  (`34b-rg3-b-cancel-restored.txt`).
- RG-F10 PASS: sanity chọn bàn trống/mở đơn/Làm mới/Đơn mới (`35`–`38-rg3-c-*.txt`).

Quan sát ngoài phạm vi (chưa sửa): dòng gợi ý `state` có thể còn câu cũ sau khi mở đơn (hành vi có từ
trước, không lệch đơn/bàn/tài chính); cần duyệt riêng nếu muốn dọn.

## Bug RG-04 — Dòng gợi ý `state` còn câu của ngữ cảnh cũ sau khi mở đơn

- Ngày phát hiện: 21/11/2026, trong đợt regression RG-03 (quan sát ngoài phạm vi, người dùng
  đã duyệt dọn ngày 21/11/2026).
- Trạng thái: **đã sửa và kiểm lại ngày 21/11/2026**.

**Hiện tượng (bằng chứng UIA):** mở đơn từ thẻ bàn hoặc ô chọn bàn, dòng gợi ý dưới POS còn câu
của ngữ cảnh trước, ví dụ đang mở HD26093335 nhưng vẫn "Đã chọn Ban 09. Thêm món đầu để tạo đơn."
(`runs/ui_20261121/36-rg3-c-combo-open-ban03.txt`) hoặc "Đã chọn Ban 01 (trống)…" khi mở B0008
(`31-rg3-a-open-ban08.txt`).

**Nguyên nhân (code):** `SalesControl.OpenOrderAsync` chỉ nạp đơn (`LoadOrder`) rồi `return`;
câu gợi ý do `ReloadAll`/`StartNewOrderAsync` đặt trước đó không được thay. Luồng mở bàn
(`OpenTableAsync`) và hộp F4 đều đi qua `OpenOrderAsync` nên cùng bị.

**Đề xuất/sửa (tối thiểu):** sau khi `LoadOrder` thành công trong `OpenOrderAsync`, đặt
`state.Text = L.F("Đã mở đơn {0}.", code)` (cùng câu đã dùng ở `HandleOccupiedTableAsync`).
Không đổi SQL/contract, không đổi logic mở đơn.

**Phạm vi ảnh hưởng:** chỉ chữ gợi ý ở POS sau khi mở đơn; không đổi đơn, bàn, phiên, tiền.

**Kiểm chứng:** build `cm-build-roi-gon4` 0 error (`runs/ui_20261121/39-build-rg4.log`);
mở thẻ Ban 08 → "Đã mở đơn HD26093450." (`40-rg4-card-ban08.txt`); Hủy bỏ nháp chọn Ban 09 →
combo/nhãn trả về Ban 03, gợi ý "Đã mở đơn HD26093335." (`41-rg4-cancel-restored.txt`); chọn
Ban 09 rồi mở lại "Ban 03 · HD26093335" từ combo → gợi ý "Đã mở đơn HD26093335."
(`42-rg4-combo-open-ban03.txt`); "Làm mới" → "Đã tải từ SQL · 08:02", vẫn giữ đơn
(`43-rg4-reload.txt`).
