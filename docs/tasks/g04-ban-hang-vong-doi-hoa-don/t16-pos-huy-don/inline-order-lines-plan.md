# Plan bổ sung T16 — Đặt món trực tiếp trên POS

Ngày 10/11/2026. Trạng thái: **đề xuất, chờ người dùng duyệt trước khi sửa source**.
Thuộc bổ sung UI/UX của G04/T16; giữ nguyên 56 ID của G04, 325 ID toàn dự án,
requirements/plan T14–T18 và lịch sử test. Không thay SQL, contract, quyền hoặc
nghiệp vụ tính tiền.

## 1. Vấn đề và mục tiêu

Hiện tại thêm một món phải qua hộp `Món · <tên>` (số lượng + ghi chú), sau đó có
thể mở thêm hộp `Khách / Ưu đãi / Giao hàng` khi tạo đơn đầu. Panel đơn còn hàng
nút `Sửa dòng`/`Xóa dòng`; ghi chú chiếm một cột riêng; cột tiền/giá và nội dung
dòng dễ lệch header khi panel hẹp.

Mục tiêu: đặt món ngay trên màn Bán hàng, giảm số cửa sổ con, chỉnh số lượng và
xóa dòng ngay tại dòng, ghi chú hiển thị gọn dưới tên món và các cột khớp header.

## 2. Phạm vi

### 2.1. Làm

- Bỏ hộp `OrderLineDialog` khỏi luồng thêm món. Bấm nút `+` trên card món thêm
  ngay 1 đơn vị vào đơn đang mở.
- Bấm lại cùng một món (cùng giá snapshot, cùng ghi chú) tăng số lượng dòng cũ
  bằng action `Update`, không tạo dòng trùng. Khác giá hoặc khác ghi chú thì tạo
  dòng riêng theo UX hiện hành.
- Trong cột `SL` của từng dòng hiển thị cụm `−  số lượng  +` và xử lý bấm trực
  tiếp: giảm 1, tăng 1. Số lượng tối thiểu là 1; bấm `−` ở mức 1 không gửi lệnh.
- Bỏ hai nút `Sửa dòng`, `Xóa dòng` khỏi panel đơn. Thêm cột cuối dòng chỉ có
  icon thùng rác; bấm icon xóa đúng dòng ngay, không hỏi xác nhận. Hủy cả hóa đơn
  vẫn giữ hộp xác nhận như hiện tại.
- Bỏ cột `Ghi chú`. Nếu dòng có ghi chú, vẽ nội dung ghi chú bằng chữ nhỏ ngay
  dưới tên món trong cùng dòng; nếu trống thì dòng giữ chiều cao gọn, không chừa
  khoảng trống.
- Sửa ghi chú **inline hoàn toàn trong danh sách**: nháy đúp vào dòng mở rộng
  dòng tại chỗ, hiện ô nhập ngay dưới tên món cùng nút lưu/hủy nhỏ; Enter lưu,
  Esc hủy. Không mở cửa sổ con, không còn bước nhập số lượng trong hộp.
- Đơn đầu tiên được tạo trực tiếp từ loại đơn + bàn đang chọn trên toolbar:
  - `Tại quán`: yêu cầu đã chọn bàn; nếu chưa chọn thì báo trạng thái và không
    mở hộp.
  - `Mang đi`: tạo ngay.
  - `Giao hàng`: giữ mở `F3 Khách / Ưu đãi / Giao hàng` một lần vì contract bắt
    buộc tên người nhận, số điện thoại và địa chỉ; sau đó tạo đơn và thêm món.
- `F3 Khách / Ưu đãi` vẫn mở hộp đầy đủ để đổi khách/khuyến mãi/thông tin giao;
  `F8`, `F4`, hủy đơn mở, kiểm tra kết quả và luồng busy/unknown/reconcile giữ
  nguyên.
- Chuyển `Hủy đơn mở` lên hàng header cạnh `F3` để bỏ hàng action riêng; vùng
  grid được tăng chiều cao.
- Chỉnh cột: `Món` co giãn, `SL` đủ rộng cho cụm `−  n  +`, `Đơn giá` đo theo
  giá snapshot thực tế và căn phải, cột thùng rác cố định; header/cell dùng cùng
  padding và căn lề. Tiền dòng vẫn ẩn ở POS theo chốt 31/10; tổng chính thức vẫn
  là `Phải thanh toán` từ SQL.

### 2.2. Không làm

- Không đổi SQL, procedure, trigger, bảng, TVP, quyền, snapshot giá/kho/điểm.
- Không thêm nhập giá tay, giảm giá tay, hoàn tiền, gộp/tách bill, đặt bàn,
  online payment hoặc offline sync.
- Không thay `CheckoutDialog`, `OrderHeaderDialog`, `OpenOrdersDialog`, in hóa
  đơn hay native Windows dialog.
- Không đổi hành vi `Update` hiện có: giảm số lượng/bỏ ghi chú được phép kể cả
  món đã ngừng bán; tăng món ngừng/thiếu công thức vẫn bị procedure chặn và
  POS hiển thị lỗi, không tự sửa dữ liệu.
- Không tự merge dòng khác ghi chú hoặc khác giá snapshot.

## 3. Quyết định đã chốt với người dùng ngày 10/11/2026

1. **Ghi chú**: sửa hoàn toàn inline trong danh sách, không mở cửa sổ con. Nháy
   đúp dòng mở ô nhập ngay dưới tên món; lưu bằng Enter/nút lưu, hủy bằng Esc/nút
   hủy. Chỉ mở ô khi người dùng cần; thêm món không mở ô ghi chú.
2. **Xóa dòng**: bấm thùng rác xóa ngay, không hỏi xác nhận. Hủy cả hóa đơn vẫn
   hỏi xác nhận như hiện tại.
3. **Giao hàng**: vẫn mở `F3` một lần khi tạo đơn giao đầu tiên vì thiếu thông
   tin bắt buộc. Các kênh `Tại quán`/`Mang đi` không mở hộp khi thêm món.
4. **Cùng món khác ghi chú**: chỉ tăng dòng khi cùng món, cùng giá snapshot và
   cùng ghi chú; khác ghi chú giữ dòng riêng theo UX 7.2.

## 4. Cách triển khai

### Bước 1 — Dòng đơn trong grid

Files: `Modules/Sales/OrderPanel.cs`, `UI/OrbitDataGridView.cs`.

- Đổi cột `OrderPanel.Lines` còn `Name`, `Quantity`, `Price` và cột unbound
  `Delete`; bỏ cột `Note`, `Amount` khỏi danh sách hiển thị. Vẫn bind
  `SalesLine` để lấy `Note`/`Amount` từ `DataBoundItem`, không đổi DTO.
- Profile `OrderLines`: đặt `Name` fill; `Quantity` cố định theo số lớn nhất của
  dòng hiện tại (tối thiểu đủ cho `−  1  +`); `Price` đo theo chuỗi tiền vi-VN;
  `Delete` cố định 30 logical. Header `SL`/`ĐƠN GIÁ` cùng padding với cell.
- Vẽ riêng trong `OnCellPainting` cho profile `OrderLines`:
  - `Name`: tên một dòng; nếu `Note` có nội dung, tên ở nửa trên và ghi chú chữ
    nhỏ/màu muted ở nửa dưới. Không có ghi chú thì tên căn giữa như cũ.
  - `Quantity`: nền input bo nhẹ, icon `−` bên trái, số căn giữa, icon `+` bên
    phải; màu giảm theo trạng thái disabled/selected.
  - `Delete`: icon thùng rác 16 logical, màu muted; hover đổi màu cảnh báo.
- Chiều cao dòng: 36 logical khi không có ghi chú, 54 logical khi có ghi chú;
  cập nhật trong `ApplyMetrics` sau binding để không chừa khoảng trống.
- Thêm tooltip: cột `Name` hướng dẫn nháy đúp sửa ghi chú và hiện đầy đủ ghi
  chú; cột `Quantity` hướng dẫn bấm `−`/`+`.

Verify: bind 0/1/nhiều dòng có/không ghi chú; header và dữ liệu thẳng cột; dòng
không ghi chú không cao bất thường; không lỗi paint khi grid rỗng/resize/DPI.

### Bước 2 — Tương tác dòng

Files: `Modules/Sales/SalesControl.cs`, `Modules/Sales/OrderPanel.cs`.

- Gắn handler một lần trong `SalesControl`:
  - bấm vùng trái `Quantity` → `DecreaseLine(line)`;
  - bấm vùng phải `Quantity` → `IncreaseLine(line)`;
  - bấm `Delete` → `DeleteLine(line)`;
  - nháy đúp dòng/cột `Name` → `EditNote(line)`.
- `IncreaseLine`/`DecreaseLine` dùng `service.LineAsync` action `Update` với
  `Quantity ± 1`, giữ nguyên `Note`; gọi qua `Write` hiện có để giữ revision,
  busy/unknown, request id và tải lại DTO.
- `DecreaseLine` không gửi khi `Quantity <= 1`; hiển thị trạng thái ngắn.
- `DeleteLine` gọi action `Delete` ngay, không hỏi xác nhận; xóa dòng cuối vẫn
  để header mở, tổng 0 theo T14. Hủy cả hóa đơn giữ nguyên hộp xác nhận.
- `EditNote` mở ô nhập inline ngay trong dòng đang chọn (không tạo cửa sổ):
  tăng chiều cao dòng, đặt `TextBox` dưới tên món cùng nút lưu/hủy nhỏ; Enter
  hoặc nút lưu gọi `Update` với số lượng hiện tại, Esc hoặc nút hủy đóng không
  ghi DB. Giữ text và mở lại được khi lỗi ghi; đóng ô khi tải lại/đổi dòng/hủy
  đơn để không sửa trên dữ liệu cũ.
- Xóa field/handler `edit`, `delete`, `productDraft`, `lineDraft` không còn dùng;
  `HasUnsavedChanges`/`DiscardChanges` cập nhật theo tập nháp còn lại.
- Trong lúc `busy/unknown`, grid và các nút bị khóa như hiện tại; không xếp
  chồng lệnh sửa dòng và không cập nhật tổng bằng số tạm.

Verify: mỗi thao tác chỉ gửi một lệnh; số lượng/tổng/preview sau reload khớp
SQL; xung đột revision hiện thông báo cũ; không mất chọn dòng sau reload.

### Bước 3 — Thêm món trực tiếp và tạo đơn đầu

Files: `Modules/Sales/SalesControl.cs`.

- `AddProduct(item)`:
  - Kiểm tra `CanAdd`, đơn mở, busy/unknown như cũ.
  - Nếu chưa có đơn: dựng header từ `CurrentHeader()` (loại đơn + bàn đang chọn).
    `Tại quán` thiếu bàn → báo trạng thái, không mở hộp. `Giao hàng` thiếu
    thông tin giao → mở `OrderHeaderDialog` một lần; người dùng hủy thì dừng.
  - Cấp mã và gọi `CreateAsync` với món đầu số lượng 1, ghi chú null; không mở
    `OrderLineDialog`.
  - Nếu đã có đơn: tìm dòng cùng `Product`, cùng `Price` với giá menu hiện tại
    và cùng ghi chú (null/trống). Có thì gọi `Update` tăng 1; không có thì gọi
    `Add` số lượng 1.
- Giữ nguyên `F3`, `F4`, `F8`, chọn bàn, đổi kênh, hỏi bỏ nháp và khóa kênh sau
  tạo. Không thêm query/ghi từ `Layout`/`SizeChanged`.

Verify: bấm nhiều lần cùng món tăng đúng số lượng; món có ghi chú vẫn tách dòng;
tạo đơn tại quán/mang đi không mở hộp; giao hàng mở F3 đúng một lần; hủy F3
không tạo đơn rỗng; lỗi ghi giữ input/trạng thái đúng contract.

### Bước 4 — Bố cục panel đơn

Files: `Modules/Sales/OrderPanel.cs`, `Modules/Sales/SalesControl.cs`.

- Bỏ panel `Actions` và hàng nút sửa/xóa/hủy. Thêm nút `Hủy đơn mở` kiểu danger
  vào `HeaderActions`, căn phải cạnh `F3`; ẩn/khóa theo `IsOpen` như cũ.
- Grid nhận phần cao còn lại từ header đến vùng tổng; giữ khoảng đệm và đường
  kẻ panel. Vùng `Phải thanh toán`, `F8`, `Kiểm tra kết quả` giữ nguyên.
- Kiểm tra lại clamp/tính tiền dài, panel 330–345 logical, nhánh `Món/Đơn` khi
  hẹp; không làm mất nút hủy/F8 hoặc chồng chữ.

Verify: bounds tại 1280×720 và 1366×768 logical, DPI 125%; panel hẹp; tiền dài;
0/1/nhiều dòng; không chồng control, grid cuộn được.

### Bước 5 — Theme, editor ghi chú inline và tài liệu

Files: `Theme/OrbitManagementTheme.cs`, `Modules/Sales/OrderPanel.cs`,
`Modules/Sales/SalesUi.cs`, `docs/UX_QuanLiQuanCafe.md`,
`docs/ui/WINFORMS_IMPLEMENTATION.md`, `plans.md`, `report.md` (sau triển khai/test).

- Thêm icon `minus` vào `DrawIcon`; dùng lại `plus`/`trash`, không thêm font
  glyph hoặc ảnh mới.
- Bỏ `OrderLineDialog`; editor ghi chú là control con của panel đơn (TextBox +
  nút lưu/hủy nhỏ), không tạo cửa sổ nghiệp vụ độc lập, không dùng
  `OrbitDialogHost` cho ghi chú.
- Cập nhật UX 7.2: bỏ nút sửa/xóa, số lượng inline, ghi chú dưới tên và sửa
  inline, thùng rác xóa ngay, quy tắc merge cùng món/giá/ghi chú.
- Cập nhật mapping WinForms: profile `OrderLines`, cột hiển thị, hành vi click,
  chiều cao dòng/editor; ghi rõ POS vẫn chỉ hiện tổng.
- Ghi trạng thái triển khai vào hồ sơ T16; không đổi checklist/ca gốc.

Verify: build sạch; không còn tham chiếu `OrderLineDialog`/nút sửa xóa; tài liệu
khớp hành vi; editor nằm trong panel, không mở cửa sổ.

## 5. Ma trận test dự kiến sau khi được duyệt test riêng

| ID | Ca và expected |
|---|---|
| IL01 | Thêm món đầu tại quán đã chọn bàn / mang đi: tạo đơn + dòng ngay, không hộp Món/khách; giao hàng mở F3 một lần, hủy F3 không tạo đơn. |
| IL02 | Bấm lại cùng món cùng giá/ghi chú: tăng số lượng đúng 1 dòng; khác ghi chú hoặc giá snapshot: tạo dòng riêng; món ngừng/thiếu công thức bị khóa. |
| IL03 | `−`/`+` ở 1, 2, nhiều; `−` tại 1 không gửi; bấm nhanh trong lúc busy không gửi chồng; tổng/preview/tiêu hao sau reload khớp SQL. |
| IL04 | Thùng rác xóa đúng dòng ngay, không hỏi; xóa dòng cuối giữ header mở tổng 0; hủy cả hóa đơn vẫn hỏi xác nhận; đơn đã trả/hủy khóa thao tác. |
| IL05 | Ghi chú trống không chừa khoảng trống; có ghi chú hiện chữ nhỏ dưới tên; ghi chú dài ellipsis + tooltip; nháy đúp mở ô inline, Enter/nút lưu ghi, Esc/nút hủy không ghi; max 100; sửa ghi chú không đổi giá/số lượng; không mở cửa sổ con. |
| IL06 | Header `MÓN/SL/ĐƠN GIÁ` khớp dữ liệu và căn phải tiền; cột co giãn đúng; panel 330–345, grid cuộn; 0/1/nhiều dòng; DPI 125%, 1280×720, 1366×768. Hình thức do người dùng kiểm. |
| IL07 | F2/F3/F4/F8, hủy đơn, kiểm tra kết quả, busy/unknown/reconcile, quyền CASH/ADMIN/STORE và đổi kênh/hỏi nháp không hồi quy. Cập nhật16/11 (G04-POS-07): đổi kênh trên nháp không còn hỏi nháp mà đổi ngữ cảnh tại chỗ theo UX 7.6 mới (bỏ bàn khi rời Tại quán). |
| IL08 | Build Debug/Release; smoke bounds/thao tác tại 125%; không chạy SQL thật trong đợt code nếu chưa duyệt test. |

## 6. Tiêu chí hoàn tất và rủi ro

Hoàn tất khi: thêm món không mở hộp Món; đơn đầu tại quán/mang đi tạo trực
tiếp; số lượng `− n +` và thùng rác hoạt động đúng; ghi chú hiển thị dưới tên
và ẩn khi trống; cột khớp header; build sạch; các ca trong phạm vi đã duyệt
PASS. Hình thức người dùng tự kiểm, không ghi PASS thay.

Rủi ro chính: hit-area `−`/`+` trong cell ở DPI khác nhau; chiều cao dòng/editor
inline thay đổi làm lệch cuộn/chọn; gộp nhầm dòng khác ghi chú; bấm nhanh bị mất
lệnh do busy; đơn giao vẫn cần F3; dòng ghi chú dài bị cắt; editor inline mất
focus khi grid cuộn. Giảm thiểu bằng custom paint tập trung trong
`OrbitDataGridView`, đóng/reposition editor khi cuộn hoặc tải lại, khóa thao tác
khi busy, so khớp Product + Price + Note trước khi Update và test bounds 125%.
