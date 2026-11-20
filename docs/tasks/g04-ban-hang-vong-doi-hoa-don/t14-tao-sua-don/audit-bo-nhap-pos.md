# Audit POS Bán hàng — bỏ "nháp", tự mở đơn mới và các luồng cấn

Ngày lập: 19/11/2026. Trạng thái: **đã triển khai 19/11/2026 theo hướng đề xuất** —
nháp bộ nhớ, hộp Giữ/Không giữ/Ở lại và TVP flush đã gỡ khỏi app; tự mở đơn mới
(V1–V9 §5.3 chờ đợt test được duyệt). Build `CafeManager.WinForms` 0 error (chỉ warning
WebView2/WindowsBase vốn có).
Phạm vi đọc: toàn bộ `src/CafeManager.WinForms/Modules/Sales/*`,
`SalesService*`/`SalesRepository*`, contract SQL liên quan (010/030/032/034),
`docs/UX_QuanLiQuanCafe.md` §3, §7, §8 và `pos-draft-plan.md`.
Yêu cầu gốc của người dùng (19/11): bỏ hẳn cơ chế nháp; vào tab Bán hàng hoặc thanh
toán xong thì tự mở đơn mới, ưu tiên bàn trống; rà toàn bộ luồng xem chỗ nào cấn và
code nào dư.

## 1. Vì sao vẫn còn "Đơn nháp" — không phải chạy nhầm bản

Nháp trong bộ nhớ là tính năng **đã được duyệt trước đó**, không phải code cũ sót lại:

- `pos-draft-plan.md` — G04-POS-02, duyệt 10/11/2026, phạm vi A (lưu một giao dịch
  bằng `sp_TaoHoaDonTuNhap` + TVP).
- `docs/UX_QuanLiQuanCafe.md` §7.6 (cập nhật 16/11/2026, G04-POS-07) quy định
  "Nháp thật cho đơn mới POS", hộp rời **Giữ / Không giữ / Ở lại**.
- Code khớp tài liệu: `SalesControl.cs:50-53` (`draft`, `draftLines`),
  `:433-439` (`ShowDraft`), `:440-456` (`ConfirmDraftAsync`), `:457-495`
  (`FlushDraftAsync`), `PosDraftDecisionDialog`, `PosDraftConflictDialog`.

Bug 18/11 (`docs/bugs/pos-draft-occupied-table/report.md`) đã kiểm chứng: exe =
HEAD `5673f51`, DB schema 35, và nháp trên bàn trống vẫn Giữ/Thanh toán đúng. Vậy
triệu chứng người dùng thấy là **hành vi đang được thiết kế**, không phải sai phiên
bản. Việc người dùng muốn "bỏ nháp" là **đổi phạm vi so với §7.6/G04-POS-02**, cần
duyệt lại tài liệu trước khi sửa (mục 6).

Đáng chú ý: contract gốc của T14 (`plan.md:13,23-25`) **vốn đã là** "thêm món đầu
gọi `sp_TaoHoaDon` và tạo header + dòng đầu trong một transaction", "xóa dòng cuối
giữ header mở, không tự hủy". Nháp G04-POS-02 chèn thêm lớp bộ nhớ lên trên contract
này. Bỏ nháp nghĩa là quay về đúng contract T14 gốc, không phải thiết kế mới từ đầu.

## 2. Luồng hiện tại (as-is) — điểm vào/ra chính

| Bước | Hành vi hiện tại | Chứng cứ |
|---|---|---|
| Vào tab lần đầu / quay lại tab | `ReloadAll`: nạp khu vực, danh mục, bàn, menu, gợi ý ghi chú. Bàn mặc định chọn **dòng đầu** (seed: B0001 đang bận). Không tự mở đơn mới. | `SalesControl.cs:183-184, 217-236, 254-262` |
| Bấm "Đơn mới" | Hỏi nháp (nếu có), reset panel, rồi nếu kênh Tại quán: giữ bàn đang chọn nếu trống, ngược lại chọn bàn trống đầu; hết bàn thì hộp "Hết bàn trống". Kênh khác: **im lặng**, không có thông báo. | `:387-413` |
| Chọn bàn + "Chọn bàn" | Bàn có đơn → mở đơn. Bàn trống → tạo **context** `draft = new("Tai quan", code)` với 0 dòng. | `:525-543` |
| Thêm món đầu | Không kiểm bàn có đơn; ghi vào `draftLines`, panel "Đơn nháp". | `:561-581` |
| Giữ/Thanh toán nháp | `FlushDraftAsync` → `sp_TaoHoaDonTuNhap`. Lỗi 52104 mở hộp 4 lựa chọn, nháp vẫn giữ. | `:457-519` |
| Thanh toán xong | `ResetDraft()` trong `finally`, hỏi in, hỏi "Trả bàn ngay?", tải lại bàn. **Không** tự mở đơn mới / không tự chọn bàn trống. | `:820-858` |
| Rời tab | `ConfirmLeaveAsync` → `ConfirmDraftAsync`: có dòng nháp thì hộp 3 nút; có input chưa ghi thì hộp OK/Cancel. | `:440-456, 456` |

## 3. Các chỗ "cấn" (đã đối chiếu code, xếp theo mức ảnh hưởng)

**C1 — Vào tab / sau thanh toán không đưa về đơn mới (đúng yêu cầu người dùng).**
Spec §8.1 lại ghi "Sau gửi in, hủy hoặc lỗi in, POS đều trở về nháp đơn mới trống
tương đương Đơn mới", nhưng code chỉ `ResetDraft()`: panel về "Chưa tạo đơn", cột Bàn
vẫn đứng ở bàn vừa thu, không chọn bàn trống. Trải nghiệm sau mỗi lần thu tiền là
"đứng yên tại bàn cũ" thay vì sẵn sàng khách kế tiếp.
Chứng cứ: `SalesControl.cs:844-848` (`finally ResetDraft`), `:217-236` (ReloadAll).

**C2 — Thêm món trên bàn đang bận vẫn tạo nháp (bug `pos-draft-occupied-table`).**
`AddProduct` chỉ kiểm `TableCode is null`, không kiểm bàn có đơn, nên nháp ra đời
trên B0001 rồi "Giữ" thất bại 52104 và hộp xung đột giữ nháp lại — đúng như ảnh
người dùng gửi. Chứng cứ: `:561-581`; chặn cuối ở SQL `032:235,275`, `034:85,135`.

**C3 — Chọn bàn trống tạo "nháp rỗng" gây hộp thoại rời màn vô nghĩa.**
`OpenTableAsync` gán `draft` dù chưa có dòng nào (`:539`); `HasUnsavedChanges` tính
`draft is not null` (`:59`) nên rời tab bị hỏi "Bỏ thông tin nháp để tiếp tục?" dù
người dùng chỉ vừa chọn bàn.

**C4 — Xóa dòng cuối để lại trạng thái nháp nửa vời.**
`DeleteLine` xóa dòng, `ShowOrder(null)` nhưng **không** xóa `draft` (`:643-653`);
panel hiện "Chưa tạo đơn" trong khi bộ nhớ vẫn coi là có nháp → hộp rời màn bất ngờ,
`SetBusy` và nút Thanh toán tính theo `draftLines`. Trạng thái UI và state lệch nhau.

**C5 — "Đơn mới" im lặng khi kênh không phải Tại quán.**
`:389` `if (!await BeginNewOrder() || channel.SelectedValue != "Tai quan") return;`
— người dùng đang Mang đi/Giao hàng bấm "Đơn mới" thấy không có phản hồi nào.

**C6 — ComboBox Bàn chỉ đổi nhãn, không mở đơn như mô tả §7.2.**
§7.2 ghi "Chọn bàn có đơn mở thì mở đơn đó"; code chỉ `UpdateTableStatus()`
(`:116, 279-292`), phải bấm thêm nút "Chọn bàn". Không sai nghiệp vụ nhưng lệch mô tả,
dễ khiến người dùng tưởng app "đứng" ở bàn đang dùng.

**C7 — Xóa dòng cuối của đơn đã lưu để lại hóa đơn 0 món.**
`sp_SuaDongHoaDon` cho phép `Delete` dòng cuối (`010:152`); UI không thông báo gì,
đơn còn mở với 0 món (chặn thanh toán/yêu cầu thanh toán). Hành vi SQL này là chủ
đích theo `plan.md:25` ("xóa dòng cuối giữ header mở, không tự hủy"), nhưng UI chưa
nói cho người dùng biết. Sau khi bỏ nháp, trường hợp này sẽ lộ ra thường xuyên hơn
(trước đây dòng cuối thường nằm trong nháp).

**C8 — Sau khi "Không" trả bàn, phiên cũ vẫn mở nhưng ngữ cảnh dễ mất.**
Sau thanh toán/hủy mà chọn No, bàn ở "Đã trả · giữ bàn". Nếu ngay sau đó POS tự mở
đơn mới ở bàn khác, muốn trả bàn cũ phải chọn lại bàn đó trong combo rồi bấm Trả bàn
(`currentSession` đổi theo combo). Hiện chưa có gì chỉ rõ điều này; khi thêm auto
đơn mới cần thông báo/giữ khả năng trả bàn cũ. Chứng cứ: `:760-787, 279-287`.

**C9 — Nút "Yêu cầu thanh toán" bị chặn bởi ghi chú đang mở nhưng không nói rõ.**
`SetBusy` tắt nút khi `HasUnsavedChanges` (`:204`), chỉ còn thanh trạng thái im lặng;
`RequestPayment` có nhánh báo (`:694`) nhưng khó bấm tới vì nút đã tắt. Mức thấp,
có thể để nguyên khi làm C1–C5.

Ngoài POS: **Đơn mở (F4)** đã đúng — `sp_SalesOpenOrders` được 032 mở rộng lấy cả
`Cho thanh toan` (`032:205-211`), dialog phân trang/tìm/lọc ổn (`OpenOrdersDialog.cs`).
**Giao hàng** (`DeliveryControl`) là màn danh sách thuần, không có nháp, không cấn.

## 4. Code dư / sẽ dư

Hiện chưa có code chết ngoài phạm vi nháp. Nếu bỏ nháp, các phần sau thành dư và nên
gỡ trong cùng thay đổi:

- `SalesControl.cs`: trường `draft`, `draftLines`, `nextDraftKey`; `ShowDraft`,
  `DraftRows`, `AddDraftLine`, `FindDraftLine`, `ConfirmDraftAsync` (thay bằng bản
  xác nhận input chưa ghi), `FlushDraftAsync`, `HandleDraftConflictAsync`; các nhánh
  `order is null` trong `ChangeQuantity/SaveNote/DeleteLine/Checkout/ReloadAll`.
- `OrderPanel.cs`: `ShowDraft` (`:60-69`) và nhánh badge "Nháp" khi `order is null`
  (`:55`) — đổi nhãn trống cho đúng nghĩa ("Chưa có đơn").
- Xóa hẳn file: `SalesDraft.cs`, `PosDraftDecisionDialog.cs`, `PosDraftConflictDialog.cs`.
- `SalesService.cs:53-70` `CreateDraftAsync`; `SalesRepository.cs:70-75`;
  `SalesDraftTableBuilder.cs`; `SalesRequests.cs:8-10`
  (`SalesDraftLineInput`, `CreateDraftOrderRequest`).
- Chuỗi dịch chỉ dùng cho nháp trong `Localization/Translations/en.json` (dọn kèm,
  không bắt buộc).

**SQL: giữ nguyên.** `sp_TaoHoaDonTuNhap` + type `SalesDraftLines` (030, sửa ở
032/034) sẽ không còn nơi gọi từ app nhưng vẫn được
`database/tests/g04/acceptance-table-sessions.sql:41` dùng để kiểm chứng contract.
Không sửa migration đã áp; nếu muốn dọn hẳn thì làm migration **036** riêng (DROP
PROC/TYPE + sửa test SQL) — đề xuất để sau, không gộp vào đợt này.
`SalesService.CreateAsync`/`sp_TaoHoaDon` **không** dư: sau khi bỏ nháp nó trở lại là
đường tạo đơn của món đầu tiên (hiện chỉ probe dùng).

## 5. Đề xuất hướng sửa (sau khi được duyệt)

### 5.1. Hành vi đích

1. **Không còn nháp dòng món.** Bỏ "Đơn nháp"/"Nháp"/"Tạm tính"; panel khi chưa có
   đơn hiển thị "Chưa có đơn" + hướng dẫn "Thêm món đầu tiên để tạo đơn".
2. **Context đơn mới** (kênh/bàn/khách/ưu đãi/người nhận) vẫn giữ trong bộ nhớ để
   nhập trước bằng F3 và để tạo đơn ở lần thêm món đầu — đây là *ngữ cảnh*, không
   phải nháp; rời màn không hỏi về nó.
3. **Tự mở đơn mới**: khi vào tab (đã tải xong), sau thanh toán, sau trả bàn, sau hủy
   đơn và khi bấm "Đơn mới": nếu kênh Tại quán thì chọn bàn trống theo đúng
   `TableForNewOrder()` (giữ bàn đang chọn nếu trống, ngược lại bàn trống đầu), hiện
   "Đã chọn Bàn X (trống). Thêm món đầu để tạo đơn."; hết bàn thì báo như "Đơn mới"
   hiện tại. Kênh Mang đi/Giao hàng: chỉ reset context và báo "Bắt đầu đơn mới".
   **Không tự tạo hóa đơn rỗng** (đúng §7.2: chọn bàn chỉ là ngữ cảnh).
4. **Món đầu tiên = một transaction**: kiểm header (đang có), kiểm bàn trống theo
   `tableRows` → `AllocateAsync` + `CreateAsync` (`sp_TaoHoaDon`, product/qty=1) →
   `LoadOrder` + `LoadTables`. Lỗi 52104 (đua phiên khác): không ghi một phần, tải
   lại bàn, hộp **Mở đơn hiện có / Chọn bàn trống khác** (rút gọn từ hộp 4 nút, không
   có "giữ nháp"). Lỗi 52105: tải lại menu, báo món đổi giá.
5. **Món tiếp theo** đi đúng đường autosave hiện tại (`sp_SuaDongHoaDon`).
6. **Xóa dòng cuối** của đơn đã lưu: giữ hóa đơn mở 0 món, báo "Đơn đã hết món.
   Thêm món hoặc Hủy đơn mở." (không tự hủy tài chính).
7. **Rời màn** chỉ hỏi khi có input thật chưa ghi: `headerDraft` của đơn đã lưu hoặc
   ghi chú đang mở; thông điệp "Bỏ input chưa ghi để tiếp tục?" như nhánh hiện có.
   Bỏ hẳn hộp Giữ/Không giữ/Ở lại.
8. Sau thanh toán/hủy mà chọn **Không** trả bàn: vẫn tự mở đơn mới nhưng ghi rõ bàn
   cũ còn "Đã trả · giữ bàn" và có thể chọn lại bàn cũ để Trả bàn (C8).

### 5.2. File thay đổi

| File | Thay đổi |
|---|---|
| `Modules/Sales/SalesControl.cs` | Gỡ nháp, thêm `context` + `StartNewOrderContextAsync`, viết lại `AddProduct` (kiểm bàn, tạo đơn bằng `CreateAsync`), `NewOrder`, `ReloadAll`, `FinishCheckout`, `CloseTable`, `DeleteLine`, `SetBusy`, `HasUnsavedChanges`, xác nhận rời màn |
| `Modules/Sales/OrderPanel.cs` | Bỏ `ShowDraft`; nhãn trống đúng nghĩa; giữ nguyên ghi chú/gợi ý |
| Xóa `SalesDraft.cs`, `PosDraftDecisionDialog.cs`, `PosDraftConflictDialog.cs` | file chết |
| `Business/Services/SalesService.cs` | bỏ `CreateDraftAsync` |
| `DataAccess/Repositories/SalesRepository.cs` | bỏ `CreateDraftAsync` |
| Xóa `DataAccess/SalesDraftTableBuilder.cs` | file chết |
| `Models/Requests/Sales/SalesRequests.cs` | bỏ 2 record nháp |
| `Localization/Translations/en.json` | dọn chuỗi chỉ dùng cho nháp |
| `docs/UX_QuanLiQuanCafe.md` | §3 (câu "hộp rời ba lựa chọn"), §7.2 ghi chú flow, **§7.6 thay bằng "đã bỏ nháp 19/11"**, §8.1 "trở về đơn mới trống tương đương Đơn mới" (giữ nguyên ý, nay đúng hành vi) |
| `t14-tao-sua-don/pos-draft-plan.md`, `bug-report.md` | đánh dấu G04-POS-02 bị thay thế bởi quyết định 19/11 |
| `docs/bugs/pos-draft-occupied-table/report.md` | đóng theo hướng bỏ nháp thay vì vá riêng `AddProduct` |

Không đổi DB trong đợt này (031–035 giữ nguyên; không sửa migration đã áp).

### 5.3. Kiểm chứng dự kiến (sau khi được duyệt, trên DB test riêng)

- V1 Vào tab: tự chọn bàn trống, **không** sinh dòng `HOADON` nào.
- V2 Thêm món đầu: 1 hóa đơn + 1 dòng, panel hiện mã đơn, nút theo trạng thái đúng;
  thêm món thứ hai đi autosave, không tạo hóa đơn mới.
- V3 Chọn tay bàn đang bận rồi thêm món: chặn, báo rõ, không ghi DB.
- V4 Thanh toán xong: panel sạch, tự chọn bàn trống kế; "Không" trả bàn vẫn thấy bàn cũ
  "Đã trả · giữ bàn" và trả bàn lại được.
- V5 Rời tab khi không nhập gì: không hộp thoại. Đang mở ghi chú/F3 chưa lưu: có hỏi
  đúng 1 lần.
- V6 Xóa dòng cuối: hóa đơn còn mở 0 món, có thông báo, Hủy/Trả bàn hoạt động.
- V7 Bàn "Đã trả · giữ bàn": thêm món tạo hóa đơn mới trong cùng phiên.
- V8 F4 Đơn mở vẫn thấy đơn `Chưa/Chờ thanh toán`; Yêu cầu thanh toán, Mở lại (ADMIN),
  Hủy, Checkout, Trả bàn không hồi quy.
- V9 Hai phiên cùng bàn: phiên sau nhận 52104, có hộp xử lý, không ghi một phần.
- Build Debug + unit test hiện có; hình thức do người dùng tự kiểm (theo AGENTS §2).

## 6. Điểm cần người dùng duyệt

1. **Bỏ hẳn nháp dòng món** (thay §7.6 và G04-POS-02 bằng hành vi mục 5.1) — thay
   đổi phạm vi đã duyệt trước đó, cần duyệt lại.
2. Giữ **context đơn mới** trong bộ nhớ cho F3/nhập trước — không hỏi khi rời màn.
3. Hành vi **xóa dòng cuối**: giữ hóa đơn 0 món + nhắc (không tự hủy).
4. SQL: **không** sửa trong đợt này; dọn `sp_TaoHoaDonTuNhap`/TVP (nếu muốn) bằng
   migration 036 sau.
