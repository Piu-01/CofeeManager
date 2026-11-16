# Đo hiệu năng — mở màn hình

- Ngày đo: 15/11/2026
- Máy: LAPTOP-79UNRIRC (Windows, SQL Server `MSSQLSERVER` local)
- DB: `CoffeeManager_Development` (localhost, chỉ đọc)
- .NET SDK: 10.0.401
- Dữ liệu tại thời điểm đo: HOADON 3.445 · CHITIET_HD 7.024 · KHACHHANG 22 · SANPHAM 18 · NGUYENVATLIEU 20 · PHIEUTHUCHI 66 · BAN 10 · PHIEUNGHIEPVU khác < 1.400 dòng

## 1. Thời gian procedure (phía DB)

Cách đo: `SqlClient` trên cùng máy, kết nối pooling như app, mỗi procedure chạy nóng 1 lần rồi đo 10 lần; bỏ qua thời gian mở kết nối. Đơn vị ms.

| Màn hình | Procedure | Min | Median | P90 | Max |
|---|---|---|---|---|---|
| Thu chi | `sp_FinanceList` | 2,2 | 3,5 | 14,0 | 14,0 |
| Nguyên liệu / Tồn | `sp_MaterialList` | 1,0 | 1,7 | 1,9 | 1,9 |
| Khách hàng | `sp_CustomerList` | 1,4 | 1,9 | 3,6 | 3,6 |
| Lịch sử hóa đơn | `sp_InvoiceList` | 23,8 | 27,1 | 39,2 | 66,9 |
| Menu / Công thức | `sp_ProductList` | 1,5 | 1,7 | 4,7 | 4,7 |
| POS – thực đơn | `sp_SalesMenu` | 1,3 | 1,7 | 12,2 | 12,2 |
| POS – danh mục | `sp_SalesCategories` | 1,0 | 1,2 | 1,3 | 1,3 |
| POS – đối tác | `sp_SalesPartners` | 0,9 | 1,2 | 3,4 | 3,4 |
| Bàn / Khu vực | `sp_TableList` | 2,5 | 3,0 | 6,2 | 6,2 |
| Tổng quan (dashboard) | `sp_DashboardAnalyticsRead` | 40,4 | 63,9 | 90,7 | 90,7 |
| Công nợ | `sp_DebtList` | 2,8 | 3,2 | 4,2 | 4,2 |
| Nhập kho | `sp_PurchaseList` | 7,2 | 8,4 | 11,6 | 11,6 |
| Xuất kho | `sp_StockIssueList` | 2,8 | 4,5 | 23,1 | 23,1 |
| Kiểm kê | `sp_StocktakeList` | 2,0 | 2,7 | 6,3 | 6,3 |
| Tài sản | `sp_AssetList` | 3,2 | 5,3 | 11,4 | 11,4 |

Ghi chú:

- `sp_InvoiceList` đo lại 20 lần: min 23,8 · median 27,1 · P90 39,2 · max 66,9 (một lần 255 ms ở lượt đo đầu là ngoại lệ, chưa tái hiện).
- Dashboard là truy vấn nặng nhất: median **63,9 ms**, P90 **90,7 ms**.
- Toàn bộ procedure còn lại median **≤ 8,4 ms**.

**Kết luận phần DB**: với dữ liệu development hiện tại, thời gian truy vấn không đủ để tạo cảm giác lag ở hầu hết màn hình. Riêng dashboard có chi phí đáng kể (~64–91 ms) và hóa đơn ~27 ms; cộng dồn vẫn dưới 100 ms, nhỏ hơn nhiều so với cảm giác "chập chờn" mô tả.

## 2. Thời gian phía UI (dựng màn + bind)

Cách đo: thêm `Diagnostics/UiTiming` (chỉ ghi khi `CAFEMANAGER_DIAG=1`), ghi 4 nhóm:

| Nhóm log | Ý nghĩa |
|---|---|
| `nav / create-module` | Thời gian chạy constructor của màn (dựng control) |
| `nav / show-module` | Thời gian ẩn màn cũ, add + hiện màn mới |
| `nav / navigate-total` | Tổng từ lúc bấm đến khi màn lên |
| `finance|list|dashboard / query` | Thời gian chờ service/DB của màn |
| `finance|list|dashboard / bind|render` | Thời gian gán dữ liệu vào grid / render dashboard |

Trạng thái: **ĐÃ ĐO** — chạy bản Debug riêng (`%TEMP%\cm-build-check`), biến môi trường
`CAFEMANAGER_DIAG=1`, đăng nhập nhanh ADMIN, đi 30 lượt điều hướng (12 màn, 2 vòng).

### 2.1. Kết quả theo nhóm log (ms, n = số lượt)

| Nhóm | n | Min | Median | Max |
|---|---|---|---|---|
| `nav / navigate-total` (bấm → màn lên) | 30 | 329 | **437** | 1119 |
| `nav / create-module` (dựng constructor màn) | 16 | 3 | 19 | 99 |
| `nav / show-module` (ẩn màn cũ + add + hiện) | 30 | 8 | 77 | 655 |
| `list / query` (chờ service + DB) | 14 | 309 | **706** | 789 |
| `finance / query` | 2 | 722 | 779 | 779 |
| `dashboard / query` | 2 | 1283 | **1393** | 1393 |
| `list|finance / bind` (gán grid) | 16 | 2 | 12 | 102 |
| `dashboard / render` (vẽ lại dashboard) | 2 | 58 | 78 | 78 |

Quan sát phụ:

- `show-module` cao bất thường ở lần đầu: Home 655 ms, Delivery 224 ms, Reports 198 ms,
  Invoices/Finance/Materials ~140 ms; các lượt sau chỉ 7–56 ms.
- Products lần đầu chạy `query` **hai lần liên tiếp** (774,8 và 759,6 ms) cho cùng 18 dòng.

### 2.2. Đối chiếu với thời gian procedure ở mục 1

`query` 706 ms trong khi `sp_*` chỉ 1–5 ms ⇒ **~700 ms không nằm ở câu truy vấn**.

Kiểm chứng nguyên nhân: chuỗi kết nối dev trên máy này có `Pooling=False`; đo trực tiếp
cùng chuỗi đó (user `CoffeeManager_Development_Runtime`, `Encrypt=True`, cùng `localhost`):

| Cấu hình | Mở kết nối (median) | Chạy `sp_MaterialList` | Tổng 1 lượt (median) |
|---|---|---|---|
| `Pooling=False` (app hiện tại) | **333,8 ms** | 2,3 ms | **340 ms** |
| `Pooling=True` | 0,1 ms | 3,4 ms | 11,2 ms |

Giải thích khớp số: mỗi lần gọi service đều `PermissionService.DemandAsync` (một connection)
rồi repository (một connection nữa) ⇒ `query ≈ 2 × 334 ms ≈ 706 ms`;
dashboard gọi thêm `EnsureAnalyticsAsync` ⇒ `≈ 4 × 334 ms ≈ 1393 ms`;
mỗi lần đổi màn tốn một connection cho kiểm quyền ⇒ `navigate-total ≈ 334 + show-module`.

`Pooling=False` không phải yêu cầu sản phẩm: nó xuất phát từ harness kiểm thử
`runs/G02_*/Complete-G02Windows.ps1` (runtime impersonation + `EXECUTE AS USER`,
G01-B06), rồi được chép vào cấu hình dev trên máy. Báo cáo G01 ghi rõ
"production DAL/connection pooling không đổi".

## 3. Kết luận và phạm vi sửa đề xuất

1. **Nguyên nhân chính (đã có số)**: `Pooling=False` ⇒ mỗi thao tác mở một kết nối vật lý
   mới (TCP + TLS + đăng nhập SQL) tốn ~334 ms. Đây là toàn bộ phần "lag" 300–1400 ms.
   Sửa: bật pooling cho runtime app (dev connection + hướng dẫn cấu hình), giữ `Pooling=False`
   riêng cho harness dùng `EXECUTE AS` nếu cần.
2. **Nguyên nhân phụ**: kiểm quyền gọi DB lại mỗi lần đổi màn (không cache trong phiên) ⇒
   thêm một kết nối/lượt; và màn tải hai lần khi mới hiện (Products) ⇒ nhân đôi chi phí.
3. **Phần "hiện từng phần/nháy"**: `show-module` 140–655 ms lần đầu + bind/xóa grid; xử lý
   bằng SuspendLayout/double buffer và giữ dữ liệu cũ khi tải lại (mục 5 A–D của report).
   Phần này độc lập với pooling và không phải nguồn lag chính.

Sau khi bật pooling, dự kiến `query` còn ~10–20 ms (dashboard ~100 ms) và
`navigate-total` còn ~80–120 ms trên dữ liệu dev hiện tại; cần đo lại để xác nhận.

## 4. Lượt đo 16/11/2026 — "ô đen" khi vào trang lần đầu

### 4.1. Cách bắt lỗi

- Bản build đo: `%TEMP%\cm-build-check\CafeManager.WinForms.exe`, `CAFEMANAGER_DIAG=1`,
  `CAFEMANAGER_USE_DEVELOPMENT=1`, `CAFEMANAGER_DEV_QUICK_LOGIN=1`; đăng nhập nhanh ADMIN.
- Chụp bằng `GetWindowDC` + `BitBlt` (không dùng `PrintWindow`): `PrintWindow` gửi
  `WM_PRINT` nên **ép vẽ lại toàn bộ** và che mất đúng lỗi "vùng chưa vẽ". Ảnh chụp màn hình
  thật bằng `CopyFromScreen` không dùng được trên máy này (desktop đen, app không lên
  foreground), nên BitBlt từ window DC là nguồn bằng chứng.
- Chuỗi 15 khung liên tiếp trong lúc mở màn, mỗi khung ~100 ms:
  `%TEMP%\open-cm\shots3\*.png` (00 = trước khi bấm, 01–14 = sau khi bấm).

### 4.2. Kết quả trước khi sửa (chỉ có A–D)

| Khung | Hiện tượng |
|---|---|
| 04-t3 | Nền trang Tổng quan: **mảng đen ~196×145** ngay phải sidebar, dưới breadcrumb (đúng vùng người dùng khoanh) — phần còn lại đã vẽ |
| 05-t4 | Chuyển sang Nguyên liệu: gần như toàn vùng nội dung đen, chỉ còn một khối nền |
| 06-t5 | Nguyên liệu đang dựng: các ô đen rời rạc ở chỗ nút làm mới, cụm nút Mở sổ/Sửa/Xóa, và vùng lưới |

Ảnh chụp sau khi vẽ xong (07-t6 trở đi) hoàn toàn bình thường ⇒ lỗi chỉ nằm ở nhịp chuyển,
không phải màu nền sai hay control thiếu `BackColor`.

### 4.3. Kết quả sau khi bật `WS_EX_COMPOSITED` (đã gỡ, xem 4.5)

Cùng kịch bản (đăng nhập nhanh → Tổng quan → Nguyên liệu):

| Khung | Hiện tượng |
|---|---|
| 04-t3 | Tổng quan vẽ **trọn** — không còn mảng đen ở góc |
| 05-t4 | Chuyển màn: ảnh đã là nội dung mới hoàn chỉnh, không có mảng đen |
| 06-t5 | Khung trung gian còn sót chữ cũ (do BitBlt đọc DC chưa ghép); không còn ô đen |
| 07-t6 | Nguyên liệu đầy đủ (lưới rỗng, "Sẵn sàng") |

**Kết luận này sai một phần** — xem 4.5: trên máy thật của người dùng, `WS_EX_COMPOSITED`
không xóa được "ô đen" mà còn làm cuộn lưới ở các tab danh sách giật.

Ghi chú: `control.Refresh()` sau khi hiện màn đã thử và **bỏ** — không xóa được mảng đen
(control con vẫn vẽ sau) mà cộng thêm 41–66 ms/lượt (`nav / paint-module`).

### 4.4. Đo A/B hiệu năng (cùng một binary, bật/tắt bằng biến môi trường rồi gỡ biến)

Kịch bản: đăng nhập nhanh ADMIN → Tổng quan → Nguyên liệu → Thu chi → Công nợ → Khách hàng
→ Bán hàng → Lịch sử hóa đơn → … (mỗi bước cách 1,5 s), `navigate-total`:

| Màn | Bật `WS_EX_COMPOSITED` | Tắt |
|---|---|---|
| Tổng quan (lần đầu) | 1290 ms | 1728 ms |
| Tổng quan (quay lại) | 16 ms | 2,5 ms |
| Nguyên liệu | 204 ms | 288 ms |
| Thu chi (lần đầu) | 698 ms | 581 ms |
| Công nợ | 196 ms | 210 ms |
| Khách hàng | 255 ms | 228 ms |
| Bán hàng | 536 ms | 470 ms |
| Thu chi (quay lại) | — | 121 ms |

Chênh lệch nằm trong dao động của harness và của lượt tạo màn đầu; không có hồi quy hệ
thống. Số `create-module` 39–45 ms (median) ở cả hai bên.

Sau khi bật pooling (đo 15/11/2026, 24 lượt): `navigate-total` median **129 ms** (max 906),
`query` danh sách median **70 ms**, Thu chi 63–118 ms, Tổng quan 393–710 ms.
Đo riêng `sp_DashboardAnalyticsRead` bằng đúng user app: **42 ms** (hôm nay) / **131 ms**
(tháng) ⇒ phần lớn thời gian Tổng quan còn lại là UI thread, không phải SQL.

### 4.5. Lượt đo thứ hai 16/11/2026 — đính chính nguyên nhân, gỡ `WS_EX_COMPOSITED`

**Phản hồi người dùng (16/11/2026)**: sau bản bật `WS_EX_COMPOSITED`, "ô vuông" vẫn còn, và
"giật lag ở mấy tab list" (cuộn danh sách) — tức là hướng sửa đó vừa sai vừa gây hồi quy.
Máy: Intel Iris Xe, DWM composition **bật**, phiên 1 ⇒ không phải do tắt hiệu ứng.

**Nguyên nhân đúng, đo lại bằng ảnh + đếm cửa sổ con** (`%TEMP%\open-cm\shots4\`, script
`probe-black.ps1`): mỗi khung chụp window DC kèm `EnumChildWindows` (class, rect, visible).
Kịch bản: đăng nhập nhanh ADMIN → bấm "Lịch sử hóa đơn" → 16 khung liên tiếp → quay lại
Tổng quan → vào lại.

| Khung | Số cửa sổ con | Hiện tượng |
|---|---|---|
| 01–06 (trước khi thay) | 127 → 166 | Hình cũ (Tổng quan) còn nguyên; control của màn mới đã tạo dần |
| 07–08 | 166 | Màn mới đã hiện nhưng vùng nội dung còn đen, chỉ thấy vài mảng nền |
| 09 | 166 | **"Ô vuông đen" đúng như người dùng báo**: ô ~180×45 ở đáy trái card và ô ~110×27 bên phải, đúng chỗ dòng "Hiển thị 1–2…" và nút "Chi tiết / In" |
| 11 trở đi | 166 | Đầy đủ, không còn mảng đen |

Số cửa sổ con **không tăng** từ khung 06 đến khung cuối ⇒ không phải "control chưa được tạo"
hay "handle tạo lúc kích thước 0" như kết luận trước, mà là **UI thread nợ lượt `WM_PAINT`**:
mỗi cửa sổ con tự vẽ ở nhịp riêng nên khoảng ~300 ms có mảng đen ở những control chưa tới lượt.

**Cách sửa mới** (`UI/NativeRedraw.cs` + `NavigationController.NavigateAsync`):
`WM_SETREDRAW=0` cho `content` → `SuspendLayout`/thay màn/`ResumeLayout(true)` → `WM_SETREDRAW=1`
+ `content.Refresh()` (vẽ đồng bộ trọn cây con) trong `finally`.

Kết quả sau sửa, cùng kịch bản (`shots4` chụp lại):

| Khung | Số cửa sổ con | Hiện tượng |
|---|---|---|
| 01–05 | 127 → 166 | Hình cũ (Tổng quan) nguyên vẹn, không nháy |
| 06 | 166 | Khung chuyển: đã là màn Lịch sử hóa đơn **đầy đủ** (form lọc, tiêu đề cột, footer) |
| 07–17 | 166 | Không còn mảng đen nào; dữ liệu về sau (grid 2 dòng) như bình thường |
| quay lại (r0–r9) | 166 | Không còn mảng đen |

**Hiệu năng sau sửa** (12 lượt, `CAFEMANAGER_DIAG=1`, cùng harness `measure-nav.ps1`):

| Chỉ số | Trước (mục 2) | Sau (native redraw) |
|---|---|---|
| `nav / show-module` median | ~100–185 ms | **181 ms** |
| `nav / navigate-total` median | 126–254 ms | **229 ms** |
| `nav / create-module` median | 37–45 ms | 40 ms |

Phần tăng của `show-module` chính là lượt vẽ trọn vẹn mà trước đây vẫn phải trả nhưng trả rải
rác qua nhiều khung hình; `navigate-total` vẫn trong dải dao động đã đo. `WS_EX_COMPOSITED`
đã gỡ khỏi `MainForm` nên cuộn lưới/hover trở lại như trước khi sửa lỗi này.
