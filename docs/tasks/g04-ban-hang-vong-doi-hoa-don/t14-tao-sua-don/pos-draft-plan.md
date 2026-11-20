# Plan bổ sung T14/T16 — Nháp thật cho đơn mới POS (G04-POS-02)

> **ĐÃ BỊ THAY THẾ 19/11/2026 — không triển khai tiếp, không chạy ca DK01–DK10.**
> Người dùng yêu cầu bỏ nháp bộ nhớ: POS trở lại hợp đồng gốc T14 (món đầu tạo hóa đơn
> một giao dịch), tự mở đơn mới khi vào tab và sau khi kết thúc thanh toán/trả bàn/hủy,
> ưu tiên bàn trống; rời màn chỉ hỏi khi có header F3/ghi chú đang mở. Code/migration030/
> TVP `SalesDraftLines`/`sp_TaoHoaDonTuNhap` không còn được app gọi (giữ làm lịch sử).
> Hướng thay thế: [audit bỏ nháp](audit-bo-nhap-pos.md), UX §7.6.

Ngày 10/11/2026. Trạng thái: **đã duyệt và đã triển khai (11/11/2026); chờ duyệt đợt test**.
Thuộc bổ sung nghiệp vụ G04/T14 + phần POS của T16; giữ nguyên 56 ID T14–T18,
325 ID toàn dự án và lịch sử test. Phạm vi **A** đã chốt: chỉ đơn mới dùng nháp;
đơn đã lưu giữ autosave hiện tại. Xung đột flush giữ nháp và cho chọn hướng.

Nguồn: [requirements bổ sung](../requirements.md#bổ-sung-10112026--nháp-thật-cho-đơn-mới-pos-g04-pos-02),
[báo cáo lỗi G04-POS-02](bug-report.md#g04-pos-02--rời-đơn-đang-dở-không-hỏi-giữkhông-giữ-mọi-thao-tác-đang-ghi-sql-ngay).

## 1. Hiện trạng và mục tiêu

- `SalesControl.AddProduct/ChangeQuantity/SaveNote/DeleteLine` gọi SQL ngay
  (`SalesControl.cs:322-372`); đơn chưa có thì `AllocateAsync` + `CreateAsync`
  (`SalesControl.cs:338-339`), đơn đã có thì `LineAsync`. `Write` tải lại từ SQL
  (`SalesControl.cs:373-385`).
- `HasUnsavedChanges` chỉ theo dõi `headerDraft`/`draft`/ghi chú dở, không có dòng
  nháp (`SalesControl.cs:50`); `BeginNewOrder`/`OpenOrderAsync` chỉ hỏi OK/Cancel
  bỏ nháp (`SalesControl.cs:248-253`, `289-294`).
- `OrderPanel.UpdateTotal` luôn hiển thị `Payable` từ SQL và nhãn "Phải thanh toán"
  (`OrderPanel.cs:123-129`); `pay.Enabled` yêu cầu `order?.Header.IsOpen` và
  `PreviewValid` (`SalesControl.cs:130`).
- `NavigationController.ConfirmLeaveAsync` là điểm chặn chung cho chuyển màn,
  đăng xuất, đổi ngôn ngữ và đóng app (`NavigationController.cs:14-21`;
  `MainForm.cs:198-199`, `244-265`), hiện chỉ Yes/No "Bỏ nháp và rời".

Mục tiêu: đơn mới (mọi kênh) chỉ sinh header/dòng trong SQL khi người dùng chọn
**Giữ** hoặc **Thanh toán**; rời nháp hỏi **Giữ / Không giữ / Ở lại**; bàn vẫn
"Trống" trong DB cho tới flush; không tạo đơn rỗng/dở ngoài ý muốn.

## 2. Quyết định đã chốt

1. **Phạm vi A**: nháp chỉ cho order chưa có trong SQL. Đơn đã lưu giữ nguyên
   `LineAsync`/`HeaderAsync` từng thao tác; rời đơn đã lưu không hỏi Giữ/Không giữ.
2. **Giữ**: một procedure batch ghi header + toàn bộ dòng trong một transaction,
   dùng lại `RequestId`/`G03OperationReceipt` operation `SalesCreate` và
   `sp_SalesReconcile` hiện có; retry cùng request không nhân dòng.
3. **Giá**: nháp giữ giá snapshot lúc thêm; flush gửi kèm giá này, SQL đối chiếu
   `SANPHAM.GiaBan` hiện hành và **từ chối** (52105) nếu lệch, giữ nguyên nháp.
   Không cho UI tự sửa giá; SQL vẫn là nguồn giá.
4. **Xung đột 52104**: giữ nháp; hộp thoại cho chọn **Mở đơn hiện có / Chọn bàn
   khác / Bỏ nháp / Ở lại**; không gộp đơn, không ghi bán phần.
5. **Thanh toán**: flush trước; flush lỗi/không xác định thì giữ nháp và không mở
   checkout.
6. **Không khôi phục nháp** sau crash/đóng app; chỉ cảnh báo trước khi rời/đóng.

## 3. Cách triển khai

### Bước 1 — Migration 030: TVP dòng nháp + procedure flush

Files: `database/migrations/030_draft_order_flush.sql` (mới),
`docs/database/migrations.md`, `docs/database/sales-contract.md`.

- Precondition `SchemaMigrations Version=29`; `SET NOCOUNT ON; SET XACT_ABORT ON;`
  theo mẫu 010/029. Không sửa checksum 001–029.
- `CREATE TYPE dbo.SalesDraftLines AS TABLE(RowNo int NOT NULL PRIMARY KEY,
  Product varchar(5) NOT NULL, Quantity int NOT NULL, Note nvarchar(100) NULL,
  Price decimal(18,0) NOT NULL);`
- `CREATE PROCEDURE dbo.sp_TaoHoaDonTuNhap` nhận Actor/Stamp/Code/RequestId,
  các tham số header như `sp_TaoHoaDon` (010:88) và `@Lines dbo.SalesDraftLines
  READONLY`; `@Lines` phải có ≥1 dòng.
- Thân procedure theo đúng khuôn `sp_TaoHoaDon` (010:88-121):
  - Chặn ambient transaction; `sp_SalesBegin(...,'SalesCreate',payload)` với
    payload JSON canonical gồm header + dòng sắp theo `RowNo`.
  - `@replay=1`: bỏ ghi, trả receipt như cũ.
  - Khóa `LoyaltyPolicy`/`Recipes` Shared, `SalesOrder:<Code>` Exclusive; khóa
    dòng `BAN` UPDLOCK/HOLDLOCK và `THROW 52104` nếu bàn đang có đơn mở.
  - `sp_SalesValidateHeader` cho header; mỗi dòng kiểm: `Quantity` 1..int,
    `Note` ≤100, `Product` tồn tại/`Dang ban`/có công thức, `Price` bằng
    `SANPHAM.GiaBan` hiện hành (lệch → `THROW 52105` thông điệp riêng "Giá món
    đã thay đổi; tải lại menu và thêm lại dòng."), tổng tiền không vượt
    `999999999999999999`.
  - Chèn `HOADON` (MaNV lấy từ `TAIKHOAN` theo `@Actor`, `TrangThaiGiao` như
    `sp_TaoHoaDon`) rồi chèn toàn bộ `CHITIET_HD` từ TVP theo `RowNo`; gọi
    `sp_SalesRefreshDiscount`; `sp_SalesFinish`; commit; trả
    `EntityCode,ResultRevision` từ receipt.
  - Mọi lỗi rollback sạch, không để header/dòng dở.
- `GRANT EXECUTE ON dbo.sp_TaoHoaDonTuNhap TO CafeManagerApp;` và
  `GRANT EXECUTE,REFERENCES ON TYPE::dbo.SalesDraftLines TO CafeManagerApp;`
  (mẫu 001:64).
- Không đổi `sp_TaoHoaDon`, `sp_SuaDongHoaDon`, `sp_SuaThongTinHoaDon`.

Verify: apply 030 trên DB test schema 029; gọi procedure 0 dòng bị từ chối; 1/N
dòng tạo đúng header+dòng một lần; replay cùng RequestId không nhân dòng; giá
lệch/món ngừng/bàn có đơn trả mã lỗi đúng và không còn bản ghi một phần
(`@@TRANCOUNT=0`, `XACT_STATE=0`).

### Bước 2 — Models, DAL, BLL

Files: `src/CafeManager.Models/Requests/Sales/SalesRequests.cs`,
`src/CafeManager.DataAccess/SalesDraftTableBuilder.cs` (mới),
`src/CafeManager.DataAccess/Repositories/SalesRepository.cs`,
`src/CafeManager.Business/Services/SalesService.cs`.

- Thêm request:
  `SalesDraftLineInput(string Product, int Quantity, string? Note, decimal Price)`;
  `CreateDraftOrderRequest(string Code, Guid RequestId, OrderHeaderInput Header,
  IReadOnlyList<SalesDraftLineInput> Lines)`. Giữ nguyên `CreateOrderRequest`.
- `SalesDraftTableBuilder.Build(lines)` tạo `DataTable` cột `RowNo, Product,
  Quantity, Note, Price`; `Parameter(table)` trả `SqlParameter` structured
  `TypeName="dbo.SalesDraftLines"` theo mẫu `PaymentTableBuilder.cs:30`.
- `SalesRepository.CreateDraftAsync` dùng `Write("sp_TaoHoaDonTuNhap", ...)` như
  `CreateAsync` (SalesRepository.cs:66-69), thêm tham số TVP và header.
- `SalesService.CreateDraftAsync`: chuẩn hóa như `CreateAsync` (BLL:47-52) — mã 10
  ký tự, `Normalize(header)`, RequestId khác rỗng, 1..200 dòng, Quantity dương,
  Price không âm/nguyên VND, Note tối đa 100 ký tự; gọi `Run`.

Verify: build; unit test phần chuẩn hóa request (số dòng, giá, ghi chú, header
giao hàng thiếu trường bị từ chối trước khi gọi SQL).

### Bước 3 — Mô hình nháp trong SalesControl và panel đơn

Files: `src/CafeManager.WinForms/Modules/Sales/SalesDraft.cs` (mới, nội bộ UI),
`src/CafeManager.WinForms/Modules/Sales/SalesControl.cs`,
`src/CafeManager.WinForms/Modules/Sales/OrderPanel.cs`,
`src/CafeManager.Models/Localization/Translations/en.json`.

- `SalesDraftLine` (in-memory): `Key` int cục bộ, `Product`, `Name`, `Price`
  snapshot từ `SalesMenuItem`, `Quantity`, `Note`. `SalesControl` giữ
  `List<SalesDraftLine> draftLines`, `int nextDraftKey`, cùng `draft`/`headerDraft`
  hiện có. Không đưa state này xuống DAL/SQL.
- `AddProduct` khi `order is null`: giữ kiểm tra kênh/bàn và F3 giao hàng như
  hiện tại (`SalesControl.cs:325-337`), nhưng **không** `AllocateAsync`/
  `CreateAsync`; thêm vào `draftLines` theo quy tắc gộp cùng `Product` + `Price`
  + `Note` (null/trim) thì tăng số lượng, khác thì thêm dòng mới.
- `ChangeQuantity/SaveNote/DeleteLine` khi `order is null`: chỉ sửa
  `draftLines`; không gọi `Write`; số lượng tối thiểu 1, xóa dòng cuối để nháp
  0 dòng. `SaveNote` đóng editor như cũ.
- `OrderPanel.ShowDraft(...)`: bind projection `SalesLine` với `LineId = Key`,
  `Amount = Quantity*Price` (key cục bộ, không phải MaDong), caption "Đơn nháp",
  status "Nháp", context kênh/bàn/khách; tổng hiển thị **Tạm tính** =
  Σ dòng. Không gọi preview, không hiện "Phải thanh toán" khi đang nháp.
- `OrderPanel.UpdateTotal` bổ sung nhánh draft; đơn đã lưu giữ nguyên
  `Payable`/`PreviewValid` hiện tại.
- `SetBusy`/`SetInteractive`: cho thao tác dòng khi `draftLines.Count>0`
  (hiện yêu cầu `order?.Header.IsOpen`, SalesControl.cs:129-133);
  `pay.Enabled` thêm nhánh `order is null && draftLines.Count>0`, đồng thời bỏ
  điều kiện `!HasUnsavedChanges` cho nhánh nháp (nháp vốn "chưa lưu" nhưng vẫn
  được thanh toán qua flush); `cancel.Enabled` vẫn chỉ cho đơn đã lưu.
- `HasUnsavedChanges` thêm `order is null && draftLines.Count>0`;
  `DiscardChanges` xóa `draftLines` cùng `draft`/`headerDraft`/ghi chú dở.
- `ReloadAll`/`VisibleChanged`: bảo toàn `draftLines` và header nháp; không tải
  lại đơn theo `order` khi đang nháp; menu/giá tải lại không tự đổi snapshot dòng.

Verify: thêm 3 dòng, sửa SL, ghi chú, xóa dòng → không có lệnh SQL nào; panel
Tạm tính đúng; mất focus khi chuyển kênh/bàn đúng như hiện tại; đơn đã lưu vẫn
autosave từng dòng và hiện "Phải thanh toán".

### Bước 4 — Giữ / Không giữ / Ở lại và xung đột flush

Files: `src/CafeManager.WinForms/Modules/Sales/PosDraftDecisionDialog.cs` (mới),
`src/CafeManager.WinForms/Modules/Sales/PosDraftConflictDialog.cs` (mới),
`src/CafeManager.WinForms/Navigation/NavigationController.cs`,
`src/CafeManager.WinForms/UI/ModuleState.cs`, `SalesControl.cs`, `en.json`.

- Thêm interface `IAsyncLeaveConfirmation { Task<bool> ConfirmLeaveAsync(Control
  owner); }`. `NavigationController.ConfirmLeaveAsync` giữ kiểm tra
  `IsWriting` rồi ưu tiên gọi interface mới nếu control hiện tại hỗ trợ; ngược
  lại dùng luồng Yes/No hiện có. Điều này phủ chuyển màn, đăng xuất, đổi ngôn
  ngữ và đóng app qua `MainForm` mà không sửa từng caller.
- `PosDraftDecisionDialog` (native `OrbitDialogForm`, owner đúng, không
  ShowInTaskbar, Esc/Ở lại mặc định) trả `Save | Discard | Stay`:
  - `Stay`: giữ nguyên.
  - `Discard`: `DiscardChanges()`, không gọi SQL.
  - `Save`: gọi `FlushDraftAsync()`; chỉ thành công mới rời/đổi ngữ cảnh.
- `SalesControl.ConfirmDraftAsync` dùng ở `BeginNewOrder`, `OpenOrderAsync`,
  `OpenTableAsync` và `ConfirmLeaveAsync`: nếu không có dòng nháp và
  không có header/ghi chú dở thì cho đi ngay; có nháp thì mở hộp 3 lựa chọn.
  Đơn đã lưu giữ prompt bỏ header/ghi chú dở như hiện tại.
- **Cập nhật16/11/2026 (G04-POS-07):** đổi kênh khi chỉ có nháp **không** dùng
  `ConfirmDraftAsync`/`FlushDraftAsync`. `channel.SelectedValueChanged` cập nhật
  `draft` tại chỗ: giữ dòng món/khách/ưu đãi, `TableCode=null` và bỏ chọn bàn khi
  rời Tại quán, xóa `Partner/Recipient/Phone/Address/Ship` khi rời Giao hàng (hỏi
  Yes/No nếu đã nhập), vào Tại quán dùng cơ chế chọn bàn của Đơn mới
  (`TableForNewOrder`: giữ bàn đang chọn nếu trống, ngược lại lấy bàn trống đầu
  tiên); chỉ khi hết toàn bộ bàn trống thì không chuyển kênh, giữ nháp và báo
  "Hết bàn trống"; gọi `ShowDraft()` để panel khớp
  ngay. Vì không ghi SQL nên không phát sinh 52104 từ đổi kênh;
  `HandleDraftConflictAsync` chỉ chạy khi kênh nháp là Tại quán. `DiscardChanges`
  khi `order is null` phải `ShowOrder(null)` để không để lại lưới cũ đã khóa thao tác.
- `PosDraftConflictDialog` (native) trả `OpenExisting | ChangeTable | Discard |
  Stay`; mở đơn hiện có thì tải theo mã bàn/đơn đã có (bỏ nháp sau khi người
  dùng chọn), đổi bàn thì tải lại danh sách bàn và giữ nháp, bỏ nháp thì
  `DiscardChanges`, ở lại thì không đổi gì.
- Thêm khóa tiếng Anh cho mọi chuỗi mới vào `en.json`; không hard-code chuỗi
  ngoài `L.T`.

Verify: mỗi đường rời (Đơn mới, F4, chọn bàn, rời màn, đăng xuất,
đóng app) hiện đủ 3 lựa chọn; Ở lại không đổi dữ liệu; Không giữ không phát sinh
SQL; Giữ lỗi thì vẫn ở lại với nháp; hộp thoại có owner/titlebar native, không
nhận phím xuyên xuống nền. Đổi kênh trên nháp không mở hộp, không ghi SQL, không
mất dòng; vào Tại quán tự lấy bàn trống như Đơn mới, hết bàn trống thì giữ kênh cũ
và báo, không chặn bởi xung đột bàn.

### Bước 5 — Flush và thanh toán

Files: `src/CafeManager.WinForms/Modules/Sales/SalesControl.cs`.

- `FlushDraftAsync()`:
  - Chặn `busy/unknown`; nháp 0 dòng trả false; chuẩn hóa header và kiểm kênh
    (Tại quán cần bàn, Giao hàng đủ người nhận/SDT/địa chỉ) trước khi gửi.
  - `pendingId = Guid.NewGuid()`, cấp mã bằng `AllocateAsync`, gọi
    `CreateDraftAsync`; thành công: xóa nháp, `LoadOrder(code)`, `LoadTables()`,
    trạng thái "Đã lưu và tải đơn từ SQL".
  - `UserOperationException` 52104: giữ nháp, tải lại bàn, mở
    `PosDraftConflictDialog`.
  - Lỗi khác: giữ nháp; lỗi cần đối chiếu/timeout đặt `unknown` và
    `pendingOperation='SalesCreate'` để nút "Kiểm tra kết quả" dùng
    `ReconcileAsync` hiện có (SalesControl.cs:386-396); không tự retry khi chưa
    đối chiếu.
  - Reconcile có receipt: xóa nháp, tải đơn; không receipt: giữ nháp để người
    dùng kiểm tra rồi chọn Giữ lại (dùng RequestId mới sau khi đã phân xử).
- `Checkout()`: nếu đang nháp thì gọi `FlushDraftAsync()` trước; thất bại/kết quả
  chưa rõ thì dừng, không mở `CheckoutDialog`; thành công mới chạy luồng hiện tại
  (`SalesControl.cs:409-425`). `FinishCheckout`/in/hủy giữ nguyên.
- Giữ khóa busy/unknown trong toàn bộ flush; không mở hai request đồng thời.

Verify: F8 trên nháp ghi đúng một lần rồi mới preview/checkout; flush lỗi giữ
nguyên nháp và không mở checkout; timeout trước/sau commit đối chiếu đúng,
không nhân dòng; đơn đã lưu checkout không đổi.

### Bước 6 — Tài liệu, contract và chỉ mục

Files: `docs/UX_QuanLiQuanCafe.md` (dòng 62, 158, 283),
`docs/database/sales-contract.md`, `docs/database/migrations.md`,
`docs/tasks/g04-ban-hang-vong-doi-hoa-don/plans.md`, `report.md` (sau triển khai).

- UX: nháp đơn mới hiển thị Giữ/Không giữ/Ở lại; nháp không ghi DB; thêm món đầu
  giữ trong bộ nhớ; Giữ/Thanh toán ghi batch nguyên tử; đơn đã lưu vẫn autosave
  (ghi rõ giới hạn phạm vi A).
- Contract: mô tả TVP `SalesDraftLines`, `sp_TaoHoaDonTuNhap`, operation
  `SalesCreate`, kiểm giá snapshot, mã lỗi 52104/52105 và giới hạn phạm vi.
- plans.md thêm chỉ mục plan này dưới bổ sung 10/11; không đổi 56 ID/checklist.

Verify: tài liệu khớp hành vi; không còn câu "tạo header cùng món đầu" áp dụng
cho đơn mới mà không nói nháp/flush.

## 4. Ma trận test dự kiến (chờ duyệt đợt test riêng)

| ID | Ca và expected |
|---|---|
| DK01 | Nháp 3 món tại quán/mang đi/giao hàng: không có `HOADON`/`CHITIET_HD`, bàn vẫn Trống trong `vw_TrangThaiBan`, panel Tạm tính đúng. |
| DK02 | Không giữ qua Đơn mới/F4/chọn bàn/rời màn/đăng xuất/đóng app: không có SQL nào, nháp xóa, hành động đích tiếp tục. Đổi kênh trên nháp (cập nhật16/11) không hỏi, không xóa dòng, không ghi SQL, bỏ bàn khi rời Tại quán. |
| DK03 | Giữ: một transaction tạo đúng header + N dòng, giá snapshot khớp, `RequestId`/receipt một lần; replay cùng request không nhân dòng; revision/đọc lại đơn đúng. |
| DK04 | F8 trên nháp: flush rồi preview/checkout; tiền/kho/điểm theo T15; flush lỗi/không xác định giữ nháp, không mở checkout. |
| DK05 | Hai phiên cùng bàn: flush sau nhận 52104, nháp còn nguyên, không ghi bán phần; mở đơn hiện có/đổi bàn/bỏ nháp/ở lại hoạt động đúng. |
| DK06 | Sửa SL/ghi chú/xóa dòng trên nháp chỉ đổi bộ nhớ; gộp đúng cùng món+giá+ghi chú; khác ghi chú/giá tách dòng; xóa hết dòng không tạo đơn. |
| DK07 | Giá menu đổi giữa nháp và Giữ: 52105, nháp giữ nguyên, không ghi dòng giá cũ. |
| DK08 | Timeout trước/sau commit: đối chiếu receipt đúng một lần, không nhân dòng, transaction sạch; không tự retry khi chưa phân xử. |
| DK09 | Regression đơn đã lưu: thêm/sửa/xóa vẫn autosave, F3/F4/F8, hủy đơn, in sau thanh toán, G04-POS-01 (Đơn mới chọn bàn trống/hết bàn) không hồi quy. |
| DK10 | UI chức năng 125%: phím F2/F3/F4/F8/Esc/Enter, busy/unknown khóa thao tác, selection/focus, bounds 1280×720 và 1366×768; hình thức SKIPPED, DPI 100/150% NOT RUN. |

## 5. Tiêu chí hoàn tất và rủi ro

Hoàn tất khi: đơn mới không ghi SQL trước Giữ/Thanh toán; Giữ flush nguyên tử
đúng một lần; Không giữ không đổi DB; cảnh báo rời/đóng app đủ 3 lựa chọn; xung
đột bàn không ghi bán phần; build Debug/Release sạch và các ca DK trong phạm vi
đã duyệt PASS. Hình thức do người dùng tự kiểm.

Rủi ro chính: nháp giữ state UI qua `ReloadAll`/DPI/đổi màn dễ mất dòng; flush
lặp khi bấm nhanh; timeout làm mất input nếu không giữ nháp; giá snapshot lệch;
mã đơn bị cấp nhưng flush lỗi tạo khoảng trống (chấp nhận như hiện tại); đóng
app khi flush chưa xong. Giảm thiểu bằng khóa busy/unknown, giữ nháp đến khi
đối chiếu xong, TVP + transaction trong procedure, và kiểm tra rời tập trung qua
`NavigationController`/`MainForm`.
