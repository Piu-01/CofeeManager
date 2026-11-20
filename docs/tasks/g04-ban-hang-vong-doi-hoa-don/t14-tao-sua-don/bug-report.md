# Lỗi bộ kiểm thử T14 — đợt G04 ngày 23/10/2026

## Cập nhật24/10/2026

Người dùng duyệt sửa G04-TEST-05 và regression. V2 giữ đầy đủ exception chain, chọn
SqlException trong chuỗi: timeout thực tế-2. Đối chiếu trả một receipt/một header,
session transaction0; `../runs/regression_20261024_testfix_v2/remaining-results.json`.
Lỗi classifier đã sửa; nhánh trước commit/mất kết nối khác vẫn chưa đủ bằng chứng.
Không sửa history v1 hoặc sản phẩm. Các cặp đồng thời được audit lại bằng raw số
do G04-TEST-06 tại T18, không tin riêng chữ PASS của runner Pair.

## G04-TEST-05 — Ca timeout đánh mất loại/mã exception

Trạng thái: chờ duyệt sửa bộ kiểm thử; chưa sửa. Không xác nhận lỗi sản phẩm.

Lệnh trong `runs/acceptance-wave_20261023_195243_f9876e48/remaining.ps1` gọi tạo đơn
HD99000001 rồi WAITFOR 3 giây, CommandTimeout=1. `remaining-results.json` ghi FAIL,
ActualError=null; đối chiếu cùng RequestId `fe47234c-6355-487a-b925-6a9080e1e1f8`
đọc được một receipt và một header. Bằng chứng xác nhận commit/đối chiếu được, nhưng
không đủ xác nhận exception phía client là -2 vì runner không lưu exception đầy đủ.

Runner đi đến InnerException cuối cùng rồi mới đọc Number. Cách này có thể bỏ qua
SqlException và đến Win32Exception không có Number. Đây là nguyên nhân có bằng
chứng từ code; loại exception thực tế cuối chuỗi vẫn cần log bổ sung để chứng minh.

Đề xuất sau duyệt: giữ nguyên FAIL cũ, lưu toàn chuỗi exception và chọn SqlException
trong chuỗi thay vì exception sâu nhất. Chạy lại timeout trước/sau commit, request
replay/mismatch và đối chiếu receipt trên hai kết nối DB test; kiểm transaction sạch.
Không đổi retry hoặc procedure sản phẩm từ kết quả hiện tại. Rủi ro giới hạn ở phân
loại kết quả test; cần tránh đổi expected để coi mọi lỗi kết nối là timeout hợp lệ.

Các fault header/chi tiết/receipt đều PASS, snapshot tiền/kho/điểm/dòng/receipt giữ
nguyên và @@TRANCOUNT/XACT_STATE=0. Các nhánh mất kết nối khác chưa được chứng nhận.

# Lỗi sản phẩm G04-POS-01 — do người dùng báo ngày 10/11/2026

## G04-POS-01 — "Đơn mới" rồi thêm món đầu thất bại im lặng khi bàn đang chọn có đơn mở

Trạng thái: **đã sửa và đã kiểm thử chức năng 27/27 PASS ngày 10/11/2026 trên DB
test cô lập** (Đơn mới tự chuyển sang bàn trống, hết bàn thì thông báo; biên bản tại
`test-results.md`). Chờ người dùng kiểm hình thức và chốt nghiệm thu.

### 1. Hiện tượng

- Màn Bán hàng, loại đơn "Tại quán", bấm **Đơn mới**: panel đơn trở về "Chưa tạo đơn"
  với nhãn **Nháp**; bấm nút **+** trên một card món: không tạo được đơn, panel vẫn
  trống/nháp, không thấy hộp thoại lỗi.
- Nhưng bấm **F4 · Đơn mở**, chọn một đơn đang mở bất kỳ rồi bấm **+**: thêm món
  thành công. Đây là điểm bất đối xứng người dùng phản ánh.
- Ảnh người dùng gửi 10/11/2026 cho thấy combo bàn đang chọn **"Ban 01 · HD20250411"**,
  nhãn **"Ban 01 · Đang phục vụ"**, panel đơn "Chưa tạo đơn", thanh trạng thái dưới
  cùng **"Đã tải từ SQL · 12:06"**.

### 2. Môi trường và bằng chứng tái hiện

- DB đang dùng cho ảnh: `CoffeeManager_Development` (localhost), đọc lúc 10/11/2026 12:11.
  Kết quả: `HD20250411 | B0001 | Tai quan | Chua thanh toan` và
  `HD20250410 | B0003 | Tai quan | Chua thanh toan`; các bàn còn lại trống.
  Mã đơn/bàn khớp đúng ảnh chụp, nên đây là dữ liệu thật của phiên báo lỗi.
- POS luôn mặc định chọn dòng bàn đầu tiên sau khi tải (`B0001` = Ban 01), nên trạng
  thái mặc định đã trỏ vào bàn đang có đơn mở.
- Các bước: mở POS → để nguyên bàn Ban 01 (đang có đơn) → **Đơn mới** → bấm **+**
  một món → panel vẫn "Chưa tạo đơn". Đổi sang bàn trống rồi thêm món là đường đi
  hợp lệ theo code + SQL, nhưng lượt điều tra này chưa thao tác UI trên bàn trống.

### 3. Nguyên nhân (dẫn chứng)

1. `LoadTables` gán `table.DataSource` và chỉ khôi phục lựa chọn cũ; lần tải đầu chưa
   có lựa chọn nên ComboBox tự chọn dòng 0 (`B0001`). Xem
   `src/CafeManager.WinForms/Modules/Sales/SalesControl.cs:155-164`.
2. `NewOrder`/`ResetDraft` chỉ xóa `order`/`draft`, **không** đổi bàn đang chọn; panel
   hiện "Chưa tạo đơn" nhưng ngữ cảnh bàn vẫn là Ban 01 đang phục vụ
   (`SalesControl.cs:180-185`, `262-271`).
3. Khi `order is null`, `AddProduct` lấy header từ `CurrentHeader()` dùng
   `table.SelectedValue` rồi gọi `service.CreateAsync`
   (`SalesControl.cs:284-285`, `295-313`).
4. `sp_TaoHoaDon` chặn tạo đơn khi bàn đã có đơn chưa thanh toán:
   `THROW 52104,N'Bàn đã có đơn. Tải đơn hiện tại trước khi thêm món.'`
   (`database/migrations/010_sales_orders.sql:107`), ánh xạ thành
   "Bàn đã có đơn. Tải đơn hiện tại trước khi thêm món." (`SqlErrorMapper.cs:43`).
5. `Write` bắt `UserOperationException` và **chỉ ghi vào label trạng thái nhỏ ở đáy
   trái**; không mở hộp thoại, không đặt `unknown`, panel đơn giữ nguyên
   (`SalesControl.cs:346-358`). Lần tải lại kế tiếp lại ghi đè label này bằng
   "Đã tải từ SQL · HH:mm" (đúng như ảnh), nên người dùng không thấy lý do.
6. Mở đơn có sẵn qua F4 đi đường `sp_SuaDongHoaDon` (không gọi `sp_TaoHoaDon`), nên
   thêm món vẫn thành công — giải thích đúng hiện tượng bất đối xứng.

Bối cảnh làm lỗi khó thấy hơn: đợt chỉnh POS gọn đang có trong working tree (10/11)
đã bỏ dialog nhập ghi chú/số lượng khỏi `AddProduct` để thêm trực tiếp từ card, nên
lỗi tạo đơn giờ chỉ còn ở label trạng thái thay vì hiện sau một dialog.

### 4. Phương án được duyệt và đã triển khai

Người dùng chốt ngày 10/11/2026: **bấm "Đơn mới" thì tự chuyển sang bàn trống; nếu
không còn bàn trống thì thông báo hết bàn**. Phương án này thay cho các đề xuất A/B/C
ghi ở lịch sử bên dưới (A không còn cần hộp thoại mở đơn ở `AddProduct`).

Đã sửa trong working tree:

1. `SalesControl.NewOrder` (`SalesControl.cs:262-288`, `BeginNewOrder` `289-294`): sau khi xác nhận/xóa nháp,
   nếu loại đơn là "Tại quán" thì giữ bàn đang chọn khi bàn đó đang trống; nếu bàn
   đang chọn đã có đơn (hoặc chưa chọn bàn) thì tự chọn bàn trống đầu tiên trong danh
   sách bàn đang tải và ghi rõ trên thanh trạng thái. Không tạo `draft`/header rỗng;
   mã đơn vẫn chỉ sinh khi thêm món đầu thành công.
2. Trường hợp không còn bàn trống: bỏ chọn bàn, ghi trạng thái và mở hộp thoại một
   nút "Hết bàn trống" (hướng xử lý: xử lý đơn đang mở hoặc chọn Mang đi/Giao hàng);
   không gọi SQL tạo đơn.
3. Tách `BeginNewOrder` để `OpenTableAsync` tái dùng bước xác nhận/xóa nháp mà không
   bị tự đổi bàn: chọn bàn trống từ danh sách bàn vẫn giữ đúng bàn người dùng chọn.
4. `OrbitConfirmationDialog`: `MessageBoxButtons.OK` giờ chỉ hiện một nút (trước đây
   vẫn luôn vẽ thêm nút Hủy dù ngữ nghĩa chỉ để thông báo). Không đổi hành vi YesNo/
   OKCancel hiện có.

Build: `dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj -c Debug`
PASS, 0 warning/0 error. **Đã chạy test chức năng** ngày 10/11/2026: 27/27 PASS trên
DB `CoffeeManager_Test_G04_20261110_124309_41e6e800_posnew`, source hash không đổi
trong lượt chạy; chi tiết và bằng chứng tại `test-results.md`.

Lịch sử đề xuất trước khi người dùng chốt (giữ để tham chiếu):

- A — Hộp thoại "Mở đơn / Chọn bàn khác" khi thêm món đầu vào bàn đã có đơn.
- B — Tự mở đơn hiện có và thêm món ngay, không hỏi.
- C — Chỉ báo lỗi rõ khi tạo thất bại.

### 5. Phạm vi ảnh hưởng và rủi ro

- Đã đổi `SalesControl.cs` (`NewOrder`, `BeginNewOrder`, `OpenTableAsync`) và
  `OrbitConfirmationDialog.cs` (nhánh nút OK). Không đổi SQL/DAL/BLL/Models, không
  cần migration; vẫn giữ `THROW 52104` trong procedure làm chốt cuối.
- Regression cần chạy: Đơn mới khi bàn đang chọn trống/đang phục vụ/hết bàn; thêm món
  đầu tạo đơn đúng bàn tự chọn; tạo đơn Mang đi và Giao hàng sau Đơn mới; mở bàn từ
  danh sách bàn (không bị đổi sang bàn khác); mở đơn F4 và thêm món; reset sau checkout.
- Rủi ro còn lại (ngoài phạm vi sửa này): nếu bàn vừa bị phiên khác chiếm sau lúc tải
  danh sách, lần thêm món đầu vẫn nhận 52104 và hiện ở thanh trạng thái. Muốn xử lý
  triệt để cần bắt 52104 rồi tải lại bàn/mở đơn thực — đề xuất riêng, chưa làm.
- Phụ (không phải nguyên nhân): mỗi lần tạo thất bại vẫn tiêu một số thứ tự hóa đơn
  qua `sp_CapMa` (`database/migrations/001_contract_and_code_generation.sql:35`), tạo
  khoảng trống mã.

# Lỗi sản phẩm G04-POS-02/03 — người dùng báo ngày 10/11/2026

## G04-POS-02 — Rời đơn đang dở không hỏi Giữ/Không giữ; mọi thao tác đang ghi SQL ngay

Trạng thái: **chờ duyệt plan; chưa sửa code**. Người dùng đã chốt ngày 10/11/2026:
**nháp thật (chưa ghi SQL)** cho **phạm vi A — chỉ đơn mới**; đơn đã lưu giữ autosave
hiện tại. Món nằm trong bộ nhớ, chỉ ghi SQL khi chọn **Giữ** hoặc **Thanh toán**;
**Không giữ** thì không ghi gì. Xung đột flush: giữ nháp và cho chọn mở đơn hiện
có / chọn bàn khác / bỏ nháp / ở lại. Plan tại
[pos-draft-plan.md](pos-draft-plan.md).

### 1. Hiện tượng

- Người dùng đang thêm món cho đơn tại quán của một bàn (ví dụ bàn 4), chưa thanh
  toán; bấm **Đơn mới** hoặc **F4 · Đơn mở** rồi chọn đơn khác. Hệ thống chuyển sang
  đơn mới ngay, **không hỏi "có giữ đơn hiện tại không"**.
- Người dùng hiểu là đơn cũ "tự động được lưu". Thực tế không có bước lưu nào xảy ra
  lúc chuyển: mọi thao tác thêm/sửa món đã ghi SQL ngay từ trước đó (autosave từng
  dòng), nên đơn vẫn còn nguyên trong DB dù người dùng không chủ động bấm lưu.
- Mong muốn: khi rời đơn đang dở, hỏi **Giữ / Không giữ**; Giữ = ghi SQL; Không giữ =
  coi như nháp và **không ghi gì vào SQL**. Đơn tại quán vẫn sửa được đến lúc thanh toán.

### 2. Bằng chứng hiện trạng (code + kiểm thử)

1. `HasUnsavedChanges` chỉ theo dõi `headerDraft`, `draft` (thông tin header mới chưa
   lưu) và ghi chú đang gõ dở; đơn đã có trong SQL **không** được coi là "chưa lưu"
   (`SalesControl.cs:50`).
2. `BeginNewOrder` và `OpenOrderAsync` chỉ hỏi khi `HasUnsavedChanges` đúng, nội dung
   hỏi là "Bỏ thông tin nháp..." — không có lựa chọn Giữ đơn
   (`SalesControl.cs:248-253`, `289-294`).
3. Mỗi lần thêm món ghi SQL ngay: đơn chưa có thì `AllocateAsync` + `CreateAsync`;
   đơn đã có thì `LineAsync` (`SalesControl.cs:322-348`). Đổi số lượng, ghi chú, xóa
   dòng cũng gọi `LineAsync` ngay (`SalesControl.cs:349-372`), sau đó `Write` tải lại
   đơn từ SQL (`SalesControl.cs:373-385`).
4. Vì vậy thao tác "chuyển đơn" chỉ là đổi ngữ cảnh hiển thị; dữ liệu đã nằm trong
   DB từ trước. Không có cơ chế "đơn nháp đã có món" trong bộ nhớ.
5. Bằng chứng kiểm thử G04-POS-01 ngày 10/11/2026: từng ca thêm món đều kiểm tra
   `CHITIET_HD`/`HOADON` ngay sau lần bấm `+` (xem `test-results.md`), xác nhận
   autosave từng dòng là hành vi hiện tại.

### 3. Mâu thuẫn với tài liệu/hợp đồng đã duyệt

- `docs/UX_QuanLiQuanCafe.md:62`: "Nháp không được ghi DB tự động. Đơn mở đã lưu là dữ
  liệu DB; rời POS không hủy đơn." — hướng nháp thật phù hợp câu này, nhưng hiện trạng
  chưa có nháp món.
- `docs/UX_QuanLiQuanCafe.md:158`: chọn bàn chỉ chọn ngữ cảnh; thêm món đầu mới tạo
  header + dòng đầu; yêu cầu chốt ở T14.
- `docs/UX_QuanLiQuanCafe.md:283`: contract G04 đã duyệt "tạo header cùng món đầu
  nguyên tử" — tức mỗi lần thêm món là một giao dịch SQL ngay, **trái** với nháp thật.
- `SalesService.PreviewAsync` và `CheckoutAsync` yêu cầu đơn đã có trong SQL
  (`SalesService.Checkout.cs:26-36`); tổng trên panel lấy `Payable` từ SQL
  (`OrderPanel.cs:127`), nút thanh toán yêu cầu `order?.Header.PreviewValid`
  (`SalesControl.cs:130`).

Kết luận: yêu cầu "nháp thật" là **đổi hợp đồng/luồng G04**, không phải sửa nhỏ trong
`SalesControl`; cần requirements + plan riêng và cập nhật UX/contract T14 trước khi code.

### 4. Thiết kế đề xuất cho hướng "nháp thật"

1. **Mô hình nháp trong bộ nhớ**: header (kênh, bàn, khách/ưu đãi, giao hàng) + danh
   sách dòng (mã món, tên, đơn giá snapshot lúc thêm, số lượng, ghi chú). Không gọi
   `CreateAsync`/`LineAsync` khi thêm/sửa/xóa món.
2. **Tổng tiền**: panel hiển thị **Tạm tính** cộng từ dòng nháp; không hiển thị "Phải
   thanh toán" hay tổng 0 giả. Ưu đãi/khách hàng/preview chính thức chỉ tính sau khi
   đã ghi SQL (F3 vẫn lưu thông tin khách/ưu đãi vào nháp, preview hoãn tới lúc Giữ).
3. **Hành động**:
   - **Giữ**: ghi toàn bộ nháp vào SQL **một lần, nguyên tử** (header + N dòng + header
     update nếu sửa đơn có sẵn), có `RequestId`/receipt để chống gửi lặp và đối chiếu
     khi timeout; thành công thì tải lại đơn từ SQL và tiếp tục.
   - **Không giữ**: xóa nháp trong bộ nhớ, không gọi SQL.
   - **Thanh toán**: Giữ (flush) → `PreviewAsync` → `CheckoutAsync` như hiện tại; flush
     lỗi thì giữ nháp, không mở checkout.
   - **Đơn mới / F4 chọn đơn khác / rời màn POS**: nếu có nháp thì hỏi Giữ / Không giữ
     / Ở lại; "Không giữ" bỏ nháp rồi mới chuyển. Cần gắn cả sự kiện rời UserControl
     theo `docs/UX_QuanLiQuanCafe.md:62`.
4. **SQL**: cần contract mới (ví dụ procedure nhận TVP dòng món và danh sách thao tác
   add/update/delete + revision) chạy trong một transaction; không lặp `sp_TaoHoaDon`
   + nhiều `sp_SuaDongHoaDon` vì không nguyên tử. Khóa bàn + kiểm tra đơn mở vẫn giữ
   như `sp_TaoHoaDon` (52104) để chống hai phiên cùng ghi.
5. **Xung đột khi flush**: trong lúc nháp, bàn vẫn "Trống" trong SQL nên phiên khác có
   thể tạo đơn trước. Khi flush gặp 52104 phải giữ nháp, tải lại bàn và báo rõ; hướng
   xử lý (mở đơn hiện có / đổi bàn / bỏ nháp) cần chốt — đề xuất chưa tự gộp đơn.
6. **Phạm vi sửa đơn đã lưu** (cần chốt khi duyệt):
   - **A — chỉ đơn mới**: đơn đã lưu vẫn autosave từng dòng như hiện tại. Ít rủi ro
     nhưng **không đúng** tình huống người dùng nêu (đang thêm món vào bàn 4 đã có đơn).
   - **B — cả đơn đã lưu**: nạp đơn vào mô hình nháp, Giữ mới ghi phần chênh lệch
     (thêm/sửa/xóa dòng + header) trong một transaction có revision; Không giữ thì DB
     không đổi. Đúng mong muốn nhưng cần contract batch mới, xử lý revision/đồng thời
     và thay đổi nhiều test.
   - Đề xuất chọn **B** nếu người dùng muốn "không lưu gì cho tới khi Giữ/Thanh toán"
     áp dụng cho mọi thao tác POS; nếu chỉ muốn cảnh báo khi rời đơn mới thì chọn A.
7. **Mất nháp**: đóng app/crash sẽ mất nháp — cần cảnh báo khi đóng app và ghi rõ
   trong tài liệu; không tự ghi SQL nền.

### 5. Phạm vi ảnh hưởng, rủi ro, kiểm thử

- Ảnh hưởng lớn: `SalesControl` (mô hình trạng thái, mọi handler món), `OrderPanel`
  (tổng tạm tính), luồng `Checkout`/`Preview`, thêm migration + procedure + DAL/BLL/
  Models/request/receipt, dialog xác nhận Giữ/Không giữ, hook rời màn POS; cập nhật
  `docs/UX_QuanLiQuanCafe.md` và contract T14, điều chỉnh bộ test G04.
- Rủi ro: nháp trùng bàn giữa hai phiên; flush bán phần nếu không dùng transaction
  batch; sai lệch preview/ưu đãi trước khi ghi; mất nháp khi crash; regression toàn
  bộ POS (thêm món, sửa số lượng, ghi chú, xóa dòng, hủy đơn, F4, thanh toán).
- Bộ test đề xuất sau khi duyệt: nháp 3 món rồi Không giữ → DB không có header/dòng,
  bàn vẫn trống; Giữ → một lần ghi nguyên tử, không nhân đôi khi retry; nháp rồi
  thanh toán → tiền/kho/điểm đúng; hai phiên cùng bàn → xung đột không ghi bán phần;
  sửa đơn đã lưu rồi Không giữ → DB/revision không đổi; Giữ có add/update/delete →
  áp dụng đủ, revision tăng đúng; timeout/reconcile không nhân dòng; rời POS/đóng app
  có cảnh báo; regression G04-POS-01 + checkout + hủy đơn + danh sách bàn.
- **Chưa sửa gì ở bước này.** Requirements bổ sung đã chốt phạm vi A và cách xử lý
  xung đột; plan chi tiết tại [pos-draft-plan.md](pos-draft-plan.md) đang chờ người
  dùng duyệt trước khi code.

## G04-POS-03 — Nhãn bàn "Đang phục vụ" đổi thành "Đang dùng"

Trạng thái: **đã sửa ngày 10/11/2026** theo hướng người dùng chốt: chỉ đổi nhãn hiển
thị, giữ nguyên giá trị DB. Build kiểm tra và unit test localization PASS; **chưa chạy
kiểm thử chức năng WinForms** cho nhãn mới (chờ duyệt đợt test).

### 1. Hiện tượng

- Bàn có đơn mở hiển thị **"Đang phục vụ"** cho tới khi thanh toán. Với đơn tại quán,
  khách vẫn đang ngồi, nước đã ra, đơn còn sửa được; người dùng muốn nhãn **"Đang dùng"**
  để đúng nghĩa hơn.

### 2. Bằng chứng

- View trả giá trị `Dang phuc vu` khi bàn có đơn chưa thanh toán:
  `database/DB_Schema.sql:498-502` (`vw_TrangThaiBan`).
- Nhãn hiển thị nằm ở 4 điểm UI:
  - `SalesControl.cs:184` — trạng thái cạnh combo bàn: `" · Đang phục vụ"`.
  - `OrbitTableCard.cs:35` — chữ trên card bàn (`busy ? "Đang phục vụ" : "Trống"`).
  - `OrbitRecordListControl.cs:58` — dịch `"Dang phuc vu"` cho lưới danh sách bàn.
  - `TablesControl.cs:40` — `AccessibleName` của card bàn.
- Bản dịch tiếng Anh: `src/CafeManager.Models/Localization/Translations/en.json:28`
  (`" · Đang phục vụ": " · Occupied"`) và dòng 1386 (`"Đang phục vụ": "Occupied"`).

### 3. Đã sửa (UI-only, rủi ro thấp)

1. Đổi chuỗi hiển thị thành **"Đang dùng"** / **" · Đang dùng"** tại 4 điểm trên; logic
   `busy = Status != "Trong"` và mọi giá trị `Dang phuc vu` trong DB/hợp đồng giữ nguyên.
2. Thay hai khóa dịch cũ trong `en.json` bằng `"Đang dùng": "In use"` và
   `" · Đang dùng": " · In use"`.
3. Không cần migration, không đổi SQL/DAL/BLL/Models/DTO; không đổi quyền hay trạng thái
   nghiệp vụ.
4. File đã sửa: `SalesControl.cs:184`, `OrbitTableCard.cs:35`,
   `OrbitRecordListControl.cs:58`, `TablesControl.cs:40`, `en.json` (2 khóa).
   Không còn chuỗi "Đang phục vụ" trong `src/`.
5. Kiểm chứng kỹ thuật: build sang thư mục tạm PASS 0 warning/0 error (build tại chỗ bị
   khóa bởi app/Visual Studio đang chạy); `dotnet test --filter Localization` PASS
   11/11. Chưa chạy kiểm thử chức năng nhãn trên WinForms thật.
6. Test: ca P00 của đợt G04-POS-01 từng so chuỗi "Đang phục vụ" là bằng chứng lịch sử —
   giữ nguyên file cũ; lượt test mới dùng expected "Đang dùng" (không sửa bằng chứng cũ).
7. Rủi ro: chỉ là chuỗi hiển thị; đã rà `src/` không còn hard-code chuỗi cũ.

### 4. Chờ duyệt

- G04-POS-03 đã sửa; đưa ca kiểm nhãn POS/màn Bàn vào đợt test kế tiếp khi được duyệt.
- G04-POS-02: requirements bổ sung đã chốt phạm vi **A** — chỉ đơn mới và cách xử lý
  xung đột tại
  [requirements.md](../requirements.md#bổ-sung-10112026--nháp-thật-cho-đơn-mới-pos-g04-pos-02);
  plan chi tiết tại [pos-draft-plan.md](pos-draft-plan.md) đã duyệt và **đã triển khai
  11/11/2026** (migration030, TVP nháp, UI nháp/Tạm tính, hộp rời và xung đột). Chưa
  chạy ca DK01–DK10; đang chờ duyệt đợt test riêng.
- Không chạy test chức năng khi chưa được duyệt đợt test theo `post-change-test-workflow`.

## G04-POS-04 — Bấm "F8 · Thanh toán" với đơn nháp làm POS khóa cứng (đơ)

Trạng thái: đã áp migration 030 lên development (mục 7) và đã bổ sung map lỗi thiếu
đối tượng trong `SqlErrorMapper` (mục 8); **chờ duyệt đợt test** để xác nhận lại luồng
nháp → Giữ → Thanh toán.
Báo cáo người dùng 15/11/2026: bấm nút thanh toán thì app "đơ", không có gì xảy ra.

### 1. Hiện tượng

POS đang có một đơn nháp (1 món). Bấm **F8 · Thanh toán** thì không mở hộp thanh
toán, màn hình khóa: mọi nút/ô nhập bị disable, chuyển module khác bị chặn bởi hộp
"Đang gửi hoặc cần đối chiếu kết quả. Hãy hoàn tất bước hiện tại." Người dùng hiểu
là app đơ, phải kill.

### 2. Bằng chứng tại máy đang chạy (15/11/2026)

- Trạng thái cuối màn Bán hàng (UI Automation, PID 31580):
  `"Có lỗi dữ liệu chưa xác định; tra mã hỗ trợ trước khi tiếp tục. · HD26093432 · Kiểm tra kết quả"`.
- Hộp chặn điều hướng đúng chuỗi của `Navigation/NavigationController.cs:17`
  (`if (dirty.IsWriting) …`), tức `SalesControl.IsWriting = busy || unknown` đang true.
- `HOADON` không có mã `HD26093432` → lệnh ghi nháp chưa commit dòng nào.
- SQL `CoffeeManager_Development`: `MAX(dbo.SchemaMigrations.Version) = 29`;
  `sys.table_types` **không** có `SalesDraftLines`; `sys.procedures` **không** có
  `sp_TaoHoaDonTuNhap`.
- Source DAL hiện gọi proc đó: `DataAccess/Repositories/SalesRepository.cs:70-75`
  (`CreateDraftAsync` → `sp_TaoHoaDonTuNhap`).
- `docs/database/migrations.md` mục "G04 — 030 draft order flush" ghi rõ 030 mới chỉ
  áp trên DB test `CoffeeManager_Test_G04_20261111_draftflush`, **chưa áp lên
  `CoffeeManager_Development`**.

### 3. Nguyên nhân (đã chứng minh bằng dữ liệu, không phải suy đoán)

1. Bấm F8 khi đơn còn là nháp → `SalesControl.Checkout` gọi `FlushDraftAsync`
   (`Modules/Sales/SalesControl.cs:347-385`), tức `service.CreateDraftAsync`.
2. Proc `sp_TaoHoaDonTuNhap` chưa tồn tại trong DB đang dùng → SQL Server trả lỗi
   2812 (could not find stored procedure), không nằm trong bảng mã nghiệp vụ.
3. `SqlErrorMapper.MapNumber` rơi vào nhánh mặc định
   (`DataAccess/SqlErrorMapper.cs:158`): `DataErrorKind.Unknown` +
   `RequiresReconciliation = true` vì đây là lệnh ghi.
4. `FlushDraftAsync` bắt `UserOperationException`, thấy `RequiresReconciliation`
   nên đặt `unknown = true`; `SetBusy` (`SalesControl.cs:136-150`) tắt toàn bộ nút
   (trừ `reconcile`) và `IsWriting` chặn điều hướng.
5. Vì vậy "đơ" thực chất là trạng thái *kết quả chưa xác định*: app cố ý khóa thao
   tác cho tới khi đối chiếu, và bấm Thanh toán lần nữa chỉ return im lặng
   (`Checkout()` thoát ở `if (busy || unknown) return;`).

Không có deadlock SQL, không có transaction mở bị treo: `sys.dm_exec_requests` lúc
điều tra trống, session app `open_transaction_count = 0`, request cuối 22:06:25.

### 4. Cách sửa đề xuất (chờ duyệt)

**Bắt buộc (khắc phục gốc):** áp migration `030_draft_order_flush.sql` (Version 30)
lên `CoffeeManager_Development` bằng `Apply-Migration.ps1 Version30` theo đúng
`docs/database/migrations.md`: đóng app trước, backup COPY_ONLY + CHECKSUM, kiểm
checksum 001–029 không đổi, xác nhận type `SalesDraftLines`, proc
`sp_TaoHoaDonTuNhap` và grant tồn tại sau khi áp. Không seed lại, không sửa
procedure 028–029.

**Cách thoát trạng thái đang kẹt ngay bây giờ (không cần sửa gì):** bấm
**"Kiểm tra kết quả"** cạnh nút Thanh toán. Với mã `HD26093432` DB không có bản ghi
`sp_SalesBegin`, `sp_SalesReconcile` sẽ trả receipt rỗng → `unknown = false`, nháp
được giữ để thử lại (sau khi đã áp 030). Nếu nút này bị che/cắt ở cửa sổ nhỏ thì
phóng to cửa sổ trước.

### 5. Tối ưu tách riêng (optional, không thuộc nguyên nhân)

- `SqlErrorMapper` đang gộp mọi mã lạ vào "lỗi dữ liệu chưa xác định" +
  `RequiresReconciliation` kể cả 2812/208 (thiếu proc/type do chưa migrate). Đề xuất
  khi có duyệt: map riêng nhóm "DB chưa được nâng schema/thiếu đối tượng" thành thông
  báo hành động được (ví dụ: "CSDL chưa áp migration cần thiết") và chỉ
  `RequiresReconciliation` khi lệnh có thể đã chạy tới SQL nghiệp vụ. *(Đã thực hiện
  ngày 15/11/2026 — xem mục 8.)*
- Nút Thanh toán khi bị chặn bởi `busy/unknown` hiện return im lặng; có thể hiện lý do
  trong dòng trạng thái thay vì không phản hồi.

### 6. Rủi ro

- Áp 030 là thay đổi schema thật trên DB development dùng chung của người dùng: phải
  backup và không seed lại; nếu bỏ qua bước kiểm checksum có thể che sai lệch 001–029.
- Sau khi áp 030, cần chạy lại đúng luồng nháp → Giữ → Thanh toán (ca DK01–DK10 của
  POS-02 vẫn đang chờ duyệt đợt test) trước khi coi module Bán hàng ổn định.
- Không đổi công thức tiền/kho/điểm; nếu sau 030 vẫn lỗi mã khác thì mở bug mới, không
  vá chồng.

### 7. Cập nhật 15/11/2026 — đã áp 030 lên `CoffeeManager_Development` (có duyệt)

Người dùng xác nhận cho áp `030_draft_order_flush.sql` (Version 30) lên đúng
`CoffeeManager_Development`. Không sửa source, không reseed.

- Trước khi áp: `SchemaMigrations` count/max = 29, không có type `SalesDraftLines`,
  không có proc `sp_TaoHoaDonTuNhap`.
- Backup COPY_ONLY + CHECKSUM, RESTORE VERIFYONLY: **Verified** →
  `C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\Backup\CoffeeManager_Development_before030_20261115.bak`,
  manifest `C:\Users\Lenovo\AppData\Local\CoffeeManager\backups\before030_20261115.json`.
- Áp bằng `Apply-Migration.ps1 -Database CoffeeManager_Development -Version 30`
  (bản sao UTF-8/BOM trong `C:\Users\Lenovo\AppData\Local\CoffeeManager\backups\tools\`
  vì PowerShell 5.1 đọc sai tiếng Việt ở file repo; file repo không bị sửa) →
  `Status = Applied`, checksum `480DA3D5FA901EAD80C1CC552E72579B6E8E3FEDC1682AB2C5DE40497FD38339`.
- Sau khi áp: `SchemaMigrations` count/max = 30; hash SHA-256 của cả 30 file migration
  khớp đúng checksum trong DB (**001–029 không lệch**); type `SalesDraftLines`
  (RowNo/Product/Quantity/Note/Price) và proc `sp_TaoHoaDonTuNhap` tồn tại, tham số
  khớp chữ ký DAL, `GRANT EXECUTE` cho `CafeManagerApp`;
  `sys.dm_sql_referenced_entities` phân giải đủ 41 đối tượng tham chiếu (không còn
  đối tượng thiếu).
- Chưa chạy ca nghiệp vụ DK01–DK10 và chưa kiểm lại luồng nháp → Giữ → Thanh toán;
  hai việc này vẫn thuộc đợt test cần duyệt riêng.

### 8. Sửa map lỗi thiếu procedure/bảng/kiểu dữ liệu (15/11/2026, người dùng yêu cầu)

Phần tối ưu đã nêu ở mục 5 (điểm 1), nay được duyệt để sửa:

- `Models/Errors/DataError.cs`: thêm `DataErrorKind.Schema` (thêm cuối enum, không đổi
  giá trị các mục cũ).
- `DataAccess/SqlErrorMapper.cs`:
  - `Map` chọn sớm mã 208/2812/2715 giống nhóm mã nghiệp vụ, để lỗi batch-aborted
    phía sau không che mất nguyên nhân thật.
  - `MapNumber`: 2812 (không tìm thấy procedure) và 2715 (không tìm thấy kiểu dữ liệu,
    ví dụ TVP) → `Schema`, **không** `RequiresReconciliation`: lời gọi hỏng trước khi
    thực thi nên chưa có kết quả ghi chưa xác định, POS không cần khóa để đối chiếu.
  - 208 (không tìm thấy bảng/đối tượng) → `Schema` nhưng **vẫn**
    `RequiresReconciliation` khi là lệnh ghi, vì SQL Server hoãn phân giải tên bảng
    tới lúc chạy nên lỗi có thể bật ra sau khi procedure đã ghi dở.
  - Thông báo mới chỉ đúng việc cần làm: “CSDL chưa áp migration cần thiết …; liên hệ
    người triển khai rồi thử lại.” thay cho “lỗi dữ liệu chưa xác định; tra mã hỗ trợ”.
- `tests/CafeManager.Tests/ParameterAndErrorTests.cs`: thêm theory `G04-POS-04`
  khóa hành vi trên (2812/2715 không đối chiếu, 208 khi ghi thì đối chiếu).
- Build Debug 0 warning/0 error; unit suite **37/37 PASS** (theory mới 5/5 PASS) —
  biên bản ở `test-results.md`, mục "Lượt test 15/11/2026". Còn **NOT RUN**: luồng
  POS nháp → Giữ → Thanh toán trên GUI và `Category=Integration`.

# Lỗi sản phẩm G04-POS-07 — người dùng báo ngày 16/11/2026

## G04-POS-07 — Đổi kênh khi đang có nháp: "Không giữ" để lại lưới đơn chết; "Giữ" lưu theo kênh cũ và mở hộp "Bàn đã có đơn" không liên quan

Trạng thái: **đã sửa source theo duyệt phương án A ngày 16/11/2026; chờ duyệt đợt
test.** Kết luận dưới đây từ đọc source và đối chiếu UX/plan; các ca GUI/SQL chưa chạy
ở lượt sửa này.

### 1. Hiện tượng (theo người dùng và ảnh gửi 16/11/2026)

- Đã thêm món vào đơn nháp tại bàn (chưa thanh toán), sau đó chuyển tab
  Tại quán → Mang đi/Giao hàng:
  - Chọn **Không giữ**: panel vẫn hiện các dòng món và tổng cũ nhưng không thao tác
    được — không sửa số lượng/ghi chú, không xóa dòng, nút thanh toán khóa. "Nháp"
    trông như còn nhưng đã chết.
  - Chọn **Giữ**: ứng dụng lưu nháp theo kênh **cũ** (Tại quán, bàn B0001) rồi mở hộp
    "Bàn đã có đơn" với câu "Bàn B0001 vừa có đơn ở phiên khác. Nháp vẫn được giữ;
    chọn cách xử lý." (4 nút Mở đơn hiện có / Chọn bàn khác / Bỏ nháp / Ở lại), dù
    người dùng đang muốn chuyển sang Mang đi/Giao hàng.
- Người dùng kỳ vọng: đổi kênh trên nháp là đổi ngữ cảnh tại chỗ; rời Tại quán thì
  tự bỏ bàn; không mở hộp xung đột bàn khi đích là Mang đi/Giao hàng; hộp
  "Bàn đã có đơn" trong ngữ cảnh này là dư thừa.

### 2. Môi trường và bằng chứng

- Source hiện tại: `src/CafeManager.WinForms/Modules/Sales/SalesControl.cs`
  (`channel.SelectedValueChanged` dòng 104–122), `PosDraftConflictDialog.cs`,
  chuỗi hộp tại `SalesControl.cs:388–392`.
- Ảnh người dùng khớp đúng nội dung `HandleDraftConflictAsync` (52104) — xác nhận
  nhánh đã chạy là flush nháp từ đường đổi kênh, không phải thêm/sửa dòng.
- Chưa chạy lại harness/GUI ở lượt này; nguyên nhân dưới đây đọc trực tiếp từ source.

### 3. Nguyên nhân (dẫn chứng)

1. Handler đổi kênh gọi `ConfirmDraftAsync` **trước khi** cập nhật nháp sang kênh
   đích (`SalesControl.cs:111` so với `:119`). `ConfirmDraftAsync` → `FlushDraftAsync`
   lấy `draft ?? CurrentHeader()` (`:353`) nên gửi SQL với `Channel/TableCode` cũ.
   Nếu bàn B0001 đã có đơn mở trong SQL (phiên khác hoặc bản ghi cũ, ca DK05) thì
   SQL trả 52104 → `HandleDraftConflictAsync` (`:376`, `:388–392`) mở hộp
   "Bàn đã có đơn" — đúng ảnh, nhưng xuất hiện từ một thao tác đổi kênh.
2. Nhánh **Không giữ**: `ConfirmDraftAsync` gọi `DiscardChanges()` (`:340`), hàm này
   chỉ xóa `draft`, `draftLines`, đóng editor (`:55`), **không vẽ lại panel**. Handler
   thấy `draftLines.Count == 0` nên bỏ qua `ShowDraft()` (`:120`); `Lines.DataSource`
   và tổng vẫn là dữ liệu cũ. `SetBusy(false)` đặt `SetInteractive(false)` vì
   `order is null && draftLines.Count == 0` (`:148`) → lưới hiện dòng cũ nhưng mọi
   click bị bỏ (`LineClicked` return), F8 bị khóa (`:144–145`). Đây đúng hiện tượng
   "vẫn giữ nháp nhưng không thao tác được" và trái ca DK02
   ("Không giữ ... nháp xóa, hành động đích tiếp tục" — `pos-draft-plan.md:230`).
3. Đổi kênh trên nháp chưa có trong SQL không cần ghi SQL: header chỉ được chuẩn hóa
   lúc flush (`SalesService.Normalize`, `SalesService.cs:30–42`). Việc flush chen vào
   đường đổi kênh tạo ra 52104 và hộp bàn, dù bàn không liên quan tới Mang đi/Giao
   hàng. Hộp xung đột chỉ có nghĩa khi flush thật sự dùng bàn (kênh Tại quán).
4. UX 7.6 hiện quy định "đổi loại đơn" là đường rời có hộp Giữ/Không giữ/Ở lại và
   "Giữ khi đang đổi kênh thì kênh quay về kênh của đơn đã lưu"
   (`docs/UX_QuanLiQuanCafe.md:219–223`) — hành vi người dùng thấy là đúng tài liệu
   hiện tại nhưng không hợp lý với nháp; cần người dùng duyệt đổi UX trước khi sửa.

### 4. Đề xuất sửa (chờ duyệt, chưa áp)

**Phương án A (khuyến nghị) — đổi kênh trên nháp là đổi ngữ cảnh tại chỗ, không ghi SQL:**

- `channel.SelectedValueChanged`, nhánh `order is null`: bỏ `ConfirmDraftAsync`/
  `FlushDraftAsync`; không mở hộp Giữ/Không giữ, do đó không thể phát sinh 52104 từ
  đổi kênh. Đơn đã lưu vẫn khóa kênh như contract 23/10.
- Cập nhật `draft` giữ nguyên dòng món/khách/ưu đãi; `Channel = target`.
- Rời Tại quán (sang Mang đi/Giao hàng): `TableCode = null`, bỏ chọn bàn
  (`table.SelectedIndex = -1`), status "Không dùng bàn · …". Không hủy hay sửa đơn
  SQL nào của bàn đó.
- Rời Giao hàng: xóa `Partner/Recipient/Phone/Address/Ship` (bắt buộc theo
  `Normalize`); nếu đã nhập thì hỏi Yes/No "Đổi sang Mang đi sẽ xóa thông tin giao đã
  nhập. Tiếp tục?" (UX 7.3 "sau xác nhận"); không có dữ liệu thì đổi ngay.
- Vào Tại quán dùng **đúng cơ chế chọn bàn của Đơn mới** (`TableForNewOrder` dùng
  chung): giữ bàn đang chọn nếu trống, ngược lại tự lấy bàn trống đầu tiên và status
  nêu rõ bàn. Chỉ chặn ở ngoại lệ **hết toàn bộ bàn trống**: giữ kênh cũ, không đổi
  nháp, status "Không còn bàn trống…" và hộp "Hết bàn trống" yêu cầu xử lý đơn đang
  mở hoặc chọn Mang đi/Giao hàng (cập nhật16/11/2026 theo phản hồi người dùng, thay
  quy tắc cũ "bỏ chọn bàn, chọn bàn trước khi lưu").
- Gọi `CancelNoteEdit()` trước và `ShowDraft()` sau (kể cả 0 dòng) để caption/context/
  tổng khớp ngay; status nói rõ "Nháp đã chuyển sang …; chưa ghi SQL."
- Phòng thủ gốc: `DiscardChanges` thêm `if (order is null) orderPanel.ShowOrder(null)`
  để mọi đường bỏ nháp (kể cả rời màn rồi quay lại) không để lại lưới chết;
  `HandleDraftConflictAsync` chỉ mở hộp khi kênh nháp là Tại quán.
- Tài liệu phải sửa kèm: UX 7.6 (đổi kênh không còn là đường rời hỏi nháp), 
  `pos-draft-plan.md` bước 4, `test-plan.md`/DK02 ghi chú "nháp được chuyển kênh,
  không xóa"; chuỗi mới qua `L.T` + `en.json`.

**Phương án B (tối thiểu, giữ hộp 3 lựa chọn):** sửa (1) discard phải vẽ lại panel,
(2) Giữ khi đổi kênh flush theo kênh đích (Mang đi bỏ bàn; Giao hàng thiếu người nhận
thì mở F3 trước), (3) hộp "Bàn đã có đơn" chỉ hiện khi kênh đích là Tại quán. Vẫn
còn bước hỏi thừa và nhiều nhánh lỗi hơn; không khuyến nghị.

### 5. Phạm vi ảnh hưởng và rủi ro

- Dự kiến sửa: `SalesControl.cs` (handler đổi kênh, `DiscardChanges`,
  `HandleDraftConflictAsync`), tài liệu UX/plan/test-plan; chuỗi mới vào `en.json`.
- Không đổi: SQL/procedure, giá snapshot, đơn đã lưu (kênh vẫn khóa sau tạo),
  checkout/hủy đơn/in.
- Cần test sau sửa: nháp Tại quán trên bàn đang bận (hai phiên) đổi Mang đi/Giao hàng
  không mở hộp bàn; dòng món/ghi chú/khách giữ nguyên; rời/vào lại Tại quán gắn bàn
  đúng; nháp Giao hàng có địa chỉ khi đổi Mang đi; F8 sau đổi kênh lưu đúng kênh,
  không 52104; bỏ nháp ở mọi đường rồi quay lại màn không còn lưới chết; regression
  DK01–DK09, G04-POS-01/04/05, F3/F4/F8.
- Giới hạn: chuyển Mang đi **không** tự hủy đơn SQL đang mở của bàn; muốn giải phóng
  bàn thật thì dùng "Hủy đơn mở" theo nghiệp vụ hiện có.

### 6. Đã sửa ngày 16/11/2026 theo duyệt phương án A

- `src/CafeManager.WinForms/Modules/Sales/SalesControl.cs`:
  - `channel.SelectedValueChanged`: bỏ `ConfirmDraftAsync`/`FlushDraftAsync` cho nháp;
    cập nhật `draft` tại chỗ (`Channel = target`), giữ dòng món/khách/ưu đãi.
    Rời Tại quán: `TableCode = null`, bỏ chọn bàn, `targetTable` chỉ nhận bàn trống.
    Rời Giao hàng: hỏi Yes/No nếu đã nhập người nhận/SĐT/địa chỉ/phí ship rồi xóa các
    trường này; vào Tại quán cần bàn trống, nếu không có thì status nhắc chọn bàn.
    Gọi `CancelNoteEdit()` trước và `ShowDraft()` sau để caption/context/tổng khớp;
    status nêu rõ "chưa ghi SQL" và bàn đã bỏ.
  - `DiscardChanges`: khi `order is null` gọi `orderPanel.ShowOrder(null)` +
    `UpdateTableStatus()` để bỏ nháp không để lại lưới cũ đã khóa thao tác.
  - `FlushDraftAsync`: hộp "Bàn đã có đơn" chỉ mở khi `draft?.Channel == "Tai quan"`.
- `src/CafeManager.Models/Localization/Translations/en.json`: thêm 5 khóa cho hộp xác
  nhận xóa thông tin giao và các status đổi kênh; JSON hợp lệ (`node JSON.parse`).
- Tài liệu: `docs/UX_QuanLiQuanCafe.md` mục 7.6 (đổi kênh không còn là đường rời hỏi
  nháp), `requirements.md` (cập nhật 16/11), `pos-draft-plan.md` (bước 4, Verify,
  DK02).
- Build WinForms Debug sang thư mục tạm: **0 error** (1 warning MSB3277 WebView2 có
  sẵn). Chưa test GUI/SQL; biên bản và bằng chứng sẽ bổ sung sau đợt test được duyệt.

### 7. Kết quả đợt test 16/11/2026

- Harness WinForms thật (MainForm + SalesControl, hộp native qua `OrbitDialogHost`)
  trên DB test `CoffeeManager_Test_G04_20261111_draftflush`, schema 31, DPI 125%:
  **32/32 PASS** cho E00–E02, G1, G2, G3, G5, G6, G7 — đổi kênh không hỏi/không ghi
  SQL, bỏ bàn khi rời Tại quán, chỉ xóa thông tin giao sau xác nhận, bỏ nháp sạch
  panel, 52104 thật vẫn mở hộp 4 lựa chọn, đơn đã lưu khóa kênh.
- SQL đối chiếu: 3 đơn Mang đi không bàn do G1 tạo trong các lượt chạy
  (HD26093404/05/07), 2 dòng, ghi chú "Ít đá" giữ đúng; B0001 không đổi; các ca còn
  lại không ghi đơn.
- Regression ghi chú/hộp gợi ý trên cùng bộ DLL `channel-fix`: **53/53 PASS**.
- Build Debug/Release: 0 error, 1 warning MSB3277 có sẵn. Unit suite **NOT RUN** do
  app đang mở từ Visual Studio giữ khóa DLL; DPI 100/150%, click tay/IME và hình thức
  do người dùng kiểm.
- Chi tiết và bằng chứng:
  [g04-pos-07-test-results.md](g04-pos-07-test-results.md),
  [runs/channel-switch-20261116](../runs/channel-switch-20261116/channel-switch-ui-smoke.json).

### 8. Điều chỉnh quy tắc vào Tại quán và đợt test 16/11/2026 (bổ sung)

Theo phản hồi người dùng sau lượt test trên: bỏ quy tắc "vào Tại quán cần chọn bàn
trống"; vào Tại quán dùng **đúng cơ chế chọn bàn của Đơn mới** qua `TableForNewOrder`
dùng chung (giữ bàn đang chọn nếu trống, ngược lại lấy bàn trống đầu tiên). Chỉ xử lý
ngoại lệ khi **hết toàn bộ bàn trống**: không cho chuyển kênh, giữ kênh/nháp hiện tại,
status "Không còn bàn trống…" và hộp "Hết bàn trống" yêu cầu xử lý đơn đang mở hoặc
chọn Mang đi/Giao hàng. `NewOrder` cũng dùng `TableForNewOrder` để hai đường không lệch.

- File sửa: `SalesControl.cs` (handler đổi kênh + helper), `en.json` (1 chuỗi hộp mới),
  `docs/UX_QuanLiQuanCafe.md` 7.6, `pos-draft-plan.md` bước 4/Verify, mục 4 tài liệu này.
- Đợt test: **40/40 PASS** trên DB test schema 31, DPI 125%, gồm đổi kênh hai chiều,
  bàn bận/chưa chọn bàn, chặn khi hết bàn (mô phỏng danh sách bàn trong bộ nhớ, không
  tạo đơn SQL cho mọi bàn), và regression Đơn mới/chọn bàn/bỏ nháp/52104/khóa kênh.
  SQL chỉ tăng 2 đơn Mang đi MaBan NULL do flush G1 của hai lượt chạy; B0001 không đổi.
- Quan sát ngoài phạm vi: sau hộp "Hết bàn trống" của Đơn mới combo bàn hiển thị lại
  dòng đầu dù đã đặt `SelectedIndex = -1`; có từ trước, không sửa trong đợt này.
- Chi tiết: [g04-pos-07b-test-results.md](g04-pos-07b-test-results.md),
  [JSON](../runs/channel-table-20261116/channel-table-ui-smoke.json).
- Chưa chạy: unit suite (DLL bị Visual Studio/app giữ khóa), DPI 100/150%, click
  tay/IME, hình thức (người dùng tự kiểm).
