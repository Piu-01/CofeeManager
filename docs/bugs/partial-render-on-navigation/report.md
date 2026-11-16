# Bug: Mở màn hình bị dựng/paint từng phần, cảm giác lag và "chập chờn"

- Trạng thái: **Đã đo xong (15/11/2026) — chờ duyệt phạm vi sửa** (chưa sửa logic nghiệp vụ;
  source chỉ có instrumentation đo tạm, bật bằng `CAFEMANAGER_DIAG=1`)
- Ngày ghi nhận: 15/11/2026
- Phạm vi: toàn bộ màn hình sau khi đăng nhập (ADMIN, CASH, STORE)
- Loại: lỗi độc lập, ảnh hưởng nhiều nhóm G02–G06 nên ghi tại `docs/bugs/`

## 1. Hiện tượng

Người dùng báo: "mỗi lần mở trang là nó cần load data, màn hình chập chờn, cảm giác bị lag,
không mượt như app hay web bình thường".

Làm rõ thêm:

| Câu hỏi | Trả lời |
|---|---|
| Màn bị ảnh hưởng | Tất cả các màn |
| Biểu hiện | "Xuất hiện 1 phần của màn hình" — chỉ một phần giao diện hiện ra trước, phần còn lại hiện sau |
| Độ trễ | Không cố định, lúc nhanh lúc chậm |
| Bản chạy | Build từ source mới nhất |
| Môi trường dự kiến | WinForms .NET 10, SQL Server `localhost`, DPI 125% |

Điểm cần nhấn: không phải "quay vòng chờ" có spinner rõ ràng, mà là **nội dung hiện dần
từng mảnh**, nên mắt thấy giật/nháy.

## 2. Bằng chứng trong source

### 2.1. Màn hình được dựng và bind dữ liệu *sau khi* đã nằm trong cây control đang hiện

`src/CafeManager.WinForms/Forms/MainForm.cs:160-165`:

```csharp
var changed = await navigation.NavigateAsync(target, () => modules.TryGetValue(target, out var factory)
    ? factory()
    : target switch { ... }, this);
```

`factory()` là constructor của cả màn hình: dựng hàng trăm control, TableLayoutPanel,
grid, rồi mới trả về. Việc này chạy đồng bộ trên UI thread **ngay trong lúc màn cũ vẫn đang hiển thị**.

`src/CafeManager.WinForms/Navigation/NavigationController.cs:32-36`:

```csharp
if (!modules.TryGetValue(target, out var control)) { control = create(); control.Dock = DockStyle.Fill; modules.Add(target, control); }
foreach (Control page in content.Controls) if (page != control) page.Visible = false;
if (control.Parent != content) content.Controls.Add(control);
control.Visible = true; control.BringToFront(); current = control;
```

Thứ tự hiện tại: tạo control → ẩn màn cũ → add vào `content` (đang visible, handle đã tạo)
→ bật `Visible` → `BringToFront`. Control chỉ được add **sau khi** đã dựng xong và **khi
container đã hiện**, nên WinForms tạo handle + layout + paint ngay trên màn hình thật,
theo từng nhịp message. Đó là cơ chế tạo ra "chỉ hiện một phần màn hình rồi phần còn lại
hiện sau", đặc biệt khi constructor + lần load đầu chiếm UI thread lâu.

### 2.2. Container chứa màn hình không double-buffer

`src/CafeManager.WinForms/Forms/MainForm.Designer.cs:20,40`:

```csharp
content = new Panel();
...
content.Dock = DockStyle.Fill; content.Margin = new Padding(0);
```

`Panel` mặc định **không** bật `DoubleBuffered`. Khi control con được add/ẩn/hiện hoặc
đổi kích thước, nền `content` được vẽ lại trước, rồi từng control con vẽ sau → thấy nền
trống/nháy ở vùng chưa kịp vẽ. Các control Orbit khác đều đã bật double buffer
(`OrbitButton.cs:38`, `OrbitDataGridView.cs:13`, `OrbitSurface.cs:11`…), riêng container
gốc của workspace thì không.

### 2.3. Mỗi lần quay lại màn là tải lại từ DB và xóa trắng danh sách

- `NavigationController` giữ control trong cache và chỉ đổi `Visible` (2.1), nhưng các màn
  tự reload khi được bật lại:
  `FinanceControl.cs:41`, `SupplierDebtsControl.cs:21`, `StockIssuesControl.cs:19`,
  `PurchasesControl.cs:20`, `StocktakesControl.cs:19`, `SalesControl.cs:124`.
- `OrbitRecordListControl.cs:62-68`: khi ẩn thì `ready = false`, khi hiện lại thì `Reload()`.
- `AdminDashboardControl.cs:91-96`: mỗi lần hiện lại gọi `ReadAsync()`, khi ẩn thì
  `++generation; pending?.Cancel(); ClearSnapshot();` → lần sau vào lại không còn số liệu cũ
  để hiển thị, buộc phải chờ đọc báo cáo xong mới có nội dung.
- Trong lúc tải, danh sách còn **xóa dữ liệu đang có**:
  `OrbitRecordListControl.cs:167-177` → `Grid.Enabled = false; ... Grid.DataSource = null;`
  rồi sau `await` mới `Grid.DataSource = rows;`.

Hệ quả: mỗi lần mở màn vừa có một khoảng trống dữ liệu, vừa có một đợt vẽ lại toàn bộ
grid, cộng thêm chi phí một round-trip SQL. Đây là nguồn "mỗi lần mở trang là cần load data"
và là lý do cảm giác lag không cố định (phụ thuộc thời gian query).

### 2.4. Layout/paint bị lặp lại nhiều lần trong một lượt hiển thị

- `src/CafeManager.WinForms/UI/OrbitManagementLayout.cs:108-145`: mỗi lượt `OnLayout` lại
  chạy `PaintSurface(card)` (đệ quy set `BackColor` toàn bộ cây con) và
  `AppTheme.PrimaryButton(button)` cho mọi nút chính, rồi mới `base.OnLayout(e)`.
  Set `BackColor`/`FlatStyle` đều gây `Invalidate` trên control con.
- `src/CafeManager.WinForms/UI/OrbitListWorkspace.cs:42-45`: mỗi `ControlAdded`,
  `SizeChanged`, `VisibleChanged` của control con đều gọi `PerformLayout()` của cả
  workspace, không gộp theo nhịp.
- `src/CafeManager.WinForms/UI/OrbitSidebar.cs` (bản đang sửa trong working tree):
  `RefreshLayout()` được gọi từ setter `Compact`, từ `HomeVisible`, từ `SetGroups`, và
  `MainForm.ArrangeNavigation()` gọi `sidebar.PerformLayout()` mỗi `ClientSizeChanged`.
- `src/CafeManager.WinForms/UI/OrbitPagination.cs`: `SetPage/SetServerPage` gọi
  `PerformLayout()` mỗi lần cập nhật chữ.

Không có `SuspendLayout/ResumeLayout` bao quanh chuỗi thao tác "tạo control → add → bind"
trong `NavigationController.NavigateAsync`.

### 2.5. Đã được đo lại ngày 15/11/2026

Mục này trước đây ghi "chưa chứng minh bằng số đo". Nay đã đo (xem `test-results.md`
mục 2 và mục 4.0): nguyên nhân chính của độ trễ là `Pooling=False` (~334 ms/kết nối),
không phải câu truy vấn SQL. Hành vi DPI 100%/150% vẫn chưa kiểm theo quy định hiện hành
(chỉ kiểm 125%).

## 3. Phân loại: hạn chế nền tảng / quyết định thiết kế / thay đổi gần đây

Không phải mọi phần của hiện tượng đều là "bug phát sinh". Tách theo nguồn gốc:

| Nguồn | Nội dung | Có phải bug phát sinh? |
|---|---|---|
| **Giới hạn WinForms** | Mỗi control là một HWND; Windows không compositing toàn cửa sổ như trình duyệt. Không bật double buffer thì thấy vẽ từng mảnh. Không có sẵn animation/compositing pipeline như web. | Không. Đây là giới hạn nền tảng, không thể kỳ vọng "mượt y hệt web". Nhưng "hiện một phần rồi phần còn lại" là do **cách dùng** (dựng control khi container đang hiện + container không double buffer), phần này khắc phục được. |
| **Quyết định thiết kế có từ G02–G03** | `NavigationController` cache module, chỉ đổi `Visible` (giữ nháp, giữ trạng thái DPI). Kèm theo đó các màn reload khi `VisibleChanged` để dữ liệu tươi. `content` là `Panel` thường (G02, commit `1ab09af`). Danh sách reload khi ẩn/hiện có từ G03 (`bce84b4`). | Không phải lỗi logic — code chạy đúng như thiết kế. Nhưng đây là **defect ở mức thiết kế/UX hiệu năng**: thiết kế đúng kỹ thuật nhưng trải nghiệm không đạt. |
| **Thay đổi gần đây (G04–G05)** | `Grid.DataSource = null` trước khi tải và `VisibleChanged += Reload()` ở Thu chi/Kho/Bán hàng được thêm trong commit `8a38f86` (30/10). Trước đó grid giữ dữ liệu cũ trong lúc tải. | **Có yếu tố phát sinh**: đây là thay đổi làm hiện tượng nặng thêm (nháy trắng + mất dữ liệu tạm thời). |
| **Working tree chưa commit** | `OrbitSidebar` viết lại, `RefreshLayout()` gọi lại nhiều đường; một số file UI đang sửa dở (localization). | Chưa xác định. Chưa đo nên không quy kết; cần tách khỏi phạm vi sửa. |

Kết luận phân loại:

- "Màn hình hiện từng phần / chập chờn" = **hạn chế WinForms + cách dựng control**, không phải bug logic.
- "Mỗi lần mở trang đều phải load lại và xóa trắng" = **hệ quả thiết kế**, có phần mới thêm ở G04–G05 làm tệ hơn.
- Vì vậy nên xử lý như **hạng mục cải thiện hiệu năng/UX có phạm vi duyệt riêng**, trong đó phần G04–G05 (xóa grid, reload khi hiện) có thể tách thành fix nhỏ nếu người dùng muốn giữ quy trình bug.

## 4. Nguyên nhân (kết luận)

0. **Nguyên nhân chính, đã đo (15/11/2026)**: chuỗi kết nối runtime đang đặt
   `Pooling=False`, nên **mỗi** lần gọi DB mở một kết nối vật lý mới tốn **~334 ms**
   (TCP + TLS + đăng nhập SQL). Mỗi màn tốn 2 kết nối (kiểm quyền + dữ liệu) ⇒ `query`
   ~706 ms; dashboard 4 kết nối ⇒ ~1393 ms; mỗi lần đổi màn tốn 1 kết nối ⇒ điều hướng
   ~437 ms. Đo cùng chuỗi với `Pooling=True`: mở kết nối 0,1 ms, tổng 11 ms/lượt.
   Chi tiết: `test-results.md` mục 2. `Pooling=False` là tàn dư của harness kiểm thử G02
   (`EXECUTE AS USER`), không phải yêu cầu của app.
1. **Trực tiếp, đã chứng minh bằng code**: màn hình mới được khởi tạo, add vào container
   đang hiển thị rồi mới layout/bind, trong khi `content` không double-buffer → giao diện
   hiện từng phần và nháy trong lúc UI thread còn bận dựng control.
2. **Trực tiếp, đã chứng minh bằng code**: danh sách bị xóa trắng + reload mỗi lần quay
   lại màn, dashboard xóa snapshot khi ẩn → luôn có khoảng "chưa có dữ liệu" trước khi
   dữ liệu mới về.
3. **Gia tăng cảm giác lag**: layout/paint lặp lại nhiều lượt (2.4) làm mỗi lần hiện màn
   tốn nhiều chu kỳ vẽ hơn cần thiết.
4. **"Ô đen" ở vùng nội dung khi vào trang lần đầu (đã bắt được bằng ảnh,
   16/11/2026)**: đó không phải chữ "Đang tải…" để lại, mà là **các cửa sổ con (HWND)
   đã được tạo nhưng chưa tới lượt `WM_PAINT`**. Ảnh chụp window DC cho thấy khi màn mới
   vừa hiện, toàn bộ 166 cửa sổ con đã tồn tại (số lượng không tăng thêm ở các khung sau),
   nhưng mỗi control tự vẽ ở một nhịp riêng, nên khoảng ~300 ms có những mảng đen đúng bằng
   diện tích các control chưa vẽ. Bằng chứng: chuỗi ảnh chụp trong lúc mở màn
   (mục 4 của `test-results.md`); ảnh chụp sau khi vẽ xong thì hoàn toàn bình thường — nên
   lỗi chỉ thấy ở nhịp chuyển, không phải màu nền sai hay control thiếu `BackColor`.

## 5. Cách sửa đề xuất (theo thứ tự ưu tiên, đúng phạm vi)

### A0. Bật connection pooling cho runtime (ưu tiên 1, đã có số chứng minh)

- Đổi cấu hình kết nối runtime sang `Pooling=True` (dev connection hiện tại đang `False`);
  giữ nguyên `Encrypt=True`, `TrustServerCertificate` chỉ cho `localhost`.
- Chỗ cần sửa: cấu hình dev đã lưu (`%LOCALAPPDATA%\CoffeeManager\development\connection.dpapi`
  do công cụ setup ghi), hướng dẫn/tài liệu cấu hình dev, và chuỗi mẫu trong harness —
  **chỉ harness dùng `EXECUTE AS USER` mới cần `Pooling=False`** (G01-B06, lỗi SQL 596).
- Kèm theo: cache kết quả kiểm quyền trong phiên (`PermissionService.DemandAsync` đang gọi
  `ReadIdentityAsync` mỗi lượt điều hướng) — hoặc chấp nhận 1 round-trip ~2 ms sau khi có pooling.
- Phải kiểm chứng lại: pooling không gây SQL 596 trên luồng app (procedure chạy dưới quyền
  caller, không `EXECUTE AS` mức session) và mọi ca quyền/đăng nhập vẫn đúng.

### A. Chuẩn bị màn hình trước khi cho hiện (sửa chính)

- `NavigationController.NavigateAsync`: bọc `content.SuspendLayout()` / `ResumeLayout(true)`;
  control mới tạo để `Visible = false`, `Dock = Fill`, add vào `content`, gọi
  `control.CreateControl()` + `control.PerformLayout()` **trong lúc còn ẩn**, sau đó mới
  ẩn màn cũ, bật `Visible = true`, `BringToFront()`, `ResumeLayout(true)`.
- Mục tiêu: chỉ có **một** lần paint sau khi cây control đã bố trí xong.

### B. Double-buffer container workspace

- Thay `Panel content` bằng một panel nhỏ kế thừa `Panel` có `DoubleBuffered = true`
  (đặt tại `UI/`, dùng chung; không đổi Designer logic nghiệp vụ).
- Không đổi `Dock/Anchor` hay bố cục hiện có.

### C. Không xóa dữ liệu cũ khi tải lại

- `OrbitRecordListControl.Reload`: bỏ `Grid.DataSource = null`; giữ dữ liệu cũ, chỉ đổi
  trạng thái "Đang tải…", khóa hành động; khi có kết quả mới thì gán một lần.
- Các màn `Load += … / VisibleChanged += …`: giữ dữ liệu đã có khi rời màn; chỉ tải lại
  khi người dùng bấm "Tải lại" hoặc khi dữ liệu được đánh dấu cũ (ví dụ quá N giây), thay
  vì luôn `ready = false` khi ẩn.
- `AdminDashboardControl`: không `ClearSnapshot()` khi chỉ ẩn màn; giữ số liệu cũ và làm
  mới nền.

### D. Gộp nhịp layout/paint (giảm chi phí, cùng phạm vi)

- `OrbitManagementLayout.OnLayout`: bỏ `PaintSurface(card)` và vòng
  `AppTheme.PrimaryButton(...)` khỏi mỗi lượt layout; chuyển sang `HandleCreated`/
  `DpiChanged`/khi thêm nút.
- `OrbitListWorkspace`: gộp các `PerformLayout()` phát sinh trong cùng một nhịp
  (dùng cờ `arranging`/`BeginInvoke` một lần thay vì gọi ngay mỗi sự kiện con).

### E. Khóa vẽ trong lúc thay màn rồi vẽ trọn một lượt (đã áp dụng 16/11/2026 — sửa "ô đen")

Trạng thái đúng (đo bằng ảnh chụp window DC, `test-results.md` mục 4.3): khi đổi màn, **tất cả**
cửa sổ con của màn mới đã tồn tại (166 cửa sổ, không tăng thêm) nhưng UI thread còn **nợ lượt
vẽ**: mỗi control con nhận `WM_PAINT` ở một nhịp khác nhau, nên trong ~300 ms người dùng thấy
các mảng đen đúng bằng diện tích những control chưa tới lượt vẽ. Vì vậy "vẽ lại sau khi hiện"
(`control.Refresh()`) không sửa được gốc, còn ghép ở mức cửa sổ thì phải chờ cả cây xong mới
được đưa ra màn hình.

Cách sửa đang dùng (`UI/NativeRedraw` + `NavigationController.NavigateAsync`):

1. `WM_SETREDRAW = 0` cho `content` trước khi thay màn (khóa vẽ cả cây con; hình cũ vẫn nằm
   nguyên trên màn hình, không nháy, không lộ nền đen).
2. `SuspendLayout` → thêm/ẩn/hiện màn → `ResumeLayout(true)`.
3. `WM_SETREDRAW = 1` rồi `content.Refresh()` — một lượt vẽ **đồng bộ** cho cả cây con, nên
   khung hình kế tiếp đã đầy đủ; không còn khoảng "một nửa đen một nửa vẽ".

Chi phí: `nav / show-module` tăng từ median 100 ms lên 181 ms (chính là lượt vẽ trọn vẹn mà
trước đây vẫn phải trả, chỉ là trả rải rác); `navigate-total` median 229 ms, cùng mức dao động
với các lượt đo trước (126–254 ms). Đổi lại: màn hình chỉ có hai trạng thái — hình cũ trọn vẹn
hoặc hình mới trọn vẹn.

Đã thử và **bỏ**: `WS_EX_COMPOSITED` cho `MainForm` (không sửa được "ô đen" trên máy thật, lại
làm cuộn lưới/hover ở các tab danh sách giật rõ — Intel Iris Xe, DWM bật; người dùng báo
16/11/2026), và `control.Refresh()` đơn lẻ sau khi hiện màn (không xóa được mảng đen vì control
con còn vẽ sau, cộng thêm 41–66 ms).

## 6. Tối ưu (không bắt buộc, tách khỏi fix lỗi)

- Gộp nhịp layout/paint (mục D) để giảm chính cái chi phí mà mục E phải trả một lần.
- Đo thời gian `LoadRows`/`ReadAsync` bằng `Stopwatch` ở môi trường test để biết có cần
  tối ưu query/index (thuộc phạm vi T29, không gộp vào fix này).
- Cache danh mục ít đổi (bàn, khu vực, phương thức thanh toán) trong phiên.

## 7. Rủi ro

| Thay đổi | Rủi ro |
|---|---|
| Đổi thứ tự add/visible trong `NavigationController` | Có thể ảnh hưởng luồng `ConfirmLeaveAsync`, `SetWorkspaceTitle`, focus ban đầu và `VisibleChanged` reload của các màn; cần kiểm lại điều hướng + nháp |
| Panel double-buffer mới | Thấp; chỉ ảnh hưởng vẽ, không đổi nghiệp vụ |
| Bỏ `Grid.DataSource = null` | Người dùng có thể thấy dữ liệu cũ trong lúc tải — phải có trạng thái "Đang tải…" rõ ràng để không hiểu nhầm là dữ liệu mới |
| Bỏ reload khi quay lại màn | Dữ liệu có thể cũ sau thao tác ở màn khác; cần cơ chế đánh dấu cũ thay vì bỏ hẳn |
| Bỏ `PaintSurface` khỏi layout | Màu nền có thể sai ở control thêm sau khi handle đã tạo; phải gọi đúng thời điểm |
| Khóa vẽ (`WM_SETREDRAW`) quanh lượt đổi màn | Nếu quên mở lại (lỗi giữa đường) thì vùng nội dung đứng hình; đã đặt trong `finally`. Không đổi bố cục/nghiệp vụ |

## 8. Ca regression dự kiến

1. Điều hướng qua lại tất cả màn (ADMIN) 3 vòng: không còn hiện từng phần, không nháy nền.
2. Màn danh sách có phân trang: đổi trang, tìm kiếm, tải lại — không xóa trắng grid.
3. Dashboard: rời màn rồi quay lại — không mất số liệu cũ, số liệu cập nhật đúng.
4. POS: mở bàn/đơn, rời màn khi có nháp — hộp xác nhận nháp vẫn đúng.
5. Quyền: CASH/STORE chỉ thấy màn được phép, không lỗi khi màn bị ẩn/hiện.
6. DPI 125%: kích thước và bố cục không đổi so với hiện tại.
7. Từ màn đăng nhập vào app: màn đầu hiện trọn một lượt, không có ô đen ở góc trên-trái
   vùng nội dung; bấm qua từng màn lần đầu cũng không còn ô đen.
8. Cuộn lưới và hover nút ở các tab danh sách: không giật/chậm (đã gỡ `WS_EX_COMPOSITED`).
