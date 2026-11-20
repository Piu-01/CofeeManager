# Bổ sung G04 — Đơn "Chờ thanh toán" và phiên bàn (đề xuất 16/11/2026)

**Trạng thái:** requirements **đã duyệt ngày 16/11/2026**. Bộ plan
[T33](t33-cho-thanh-toan/plan.md), [T34](t34-phien-ban/plan.md),
[T35](t35-tuong-thich-tai-lieu/plan.md) đã viết, **chờ duyệt một lần**; chưa code,
chưa tạo migration.

**Nguồn:** trao đổi với người dùng ngày 16/11/2026. Người dùng yêu cầu chỉnh luồng đặt
đơn tại quán theo hướng: (1) có trạng thái "chưa thanh toán nhưng không cho sửa đơn" sau
khi đã ra món; (2) bàn chỉ trống khi khách về/thanh toán; (3) khách thanh toán trước rồi
mới ngồi thì bàn không được hiển thị trống. Người dùng đã đồng ý hướng vận hành phổ biến
của ngành F&B: **khóa đơn ở mốc "in tạm tính/yêu cầu thanh toán", không khóa ở mốc "đã ra
món"**, và **bàn gắn với phiên bàn, trả bàn là hành động của nhân viên**.

Khảo sát ngày 16/11/2026: repo **không có** tài liệu/đề xuất nào trước đây cho các nội dung
này; hiện trạng đã chốt ngược lại (đơn sửa được tới lúc thanh toán; bàn dẫn xuất từ đơn
`Chua thanh toan`; checkout làm bàn trống ngay). Vì vậy đây là **phần phát sinh ngoài
phạm vi G04 gốc**, giữ nguyên 56 ID T14–T18 và 325 ID toàn dự án; các task mới đánh số
tiếp T33 trở đi.

## 1. Hiện trạng liên quan

- `HOADON.TrangThai` chỉ có `Chua thanh toan` / `Da thanh toan` / `Huy` (`database/DB_Schema.sql:309,333-334`).
- `vw_TrangThaiBan` trả `Trong` khi bàn không có đơn `Chua thanh toan`, còn lại `Dang phuc vu`
  (`database/DB_Schema.sql:498-503`); `BAN` không có cột trạng thái.
- `UX_HD_BanMo` là unique filtered index một đơn mở/bàn (`database/DB_Schema.sql:425-426`).
- `sp_SalesCheckOpen` (`database/migrations/010_sales_orders.sql:35-41`) là cổng khóa dùng chung
  cho sửa dòng/thông tin, checkout và hủy; `sp_ThanhToanHoaDon` chốt `Da thanh toan`
  (`011_checkout.sql:105`), bàn tự trống vì view chỉ join đơn `Chua thanh toan`.
- POS khóa nút theo `SalesOrderDto.IsOpen` (`src/CafeManager.Models/Dtos/Sales/SalesOrderDto.cs:9`,
  `src/CafeManager.WinForms/Modules/Sales/SalesControl.cs:179-193`).

## 2. Mục tiêu

1. Thêm mốc nghiệp vụ **"Chờ thanh toán"** cho đơn: khách đã nhận món, nhân viên đã đưa
   tạm tính; đơn **khóa sửa món/thông tin**, chỉ còn thu tiền hoặc hủy; quản lý có quyền
   mở lại khi cần.
2. Bàn **bận theo phiên bàn**, không theo trạng thái một hóa đơn. Bàn chỉ trống khi nhân
   viên **trả bàn** (đóng phiên), không tự trống ngay sau khi thu tiền.
3. Hỗ trợ khách **trả trước rồi vẫn ngồi**: thu tiền xong bàn vẫn `Đang dùng` cho tới khi
   trả bàn; khách gọi thêm tạo hóa đơn mới trong cùng phiên (không gộp).
4. Không đổi bất biến tiền/kho/điểm, không đổi luồng checkout/hủy/in ngoài phần trạng thái
   và phiên bàn nêu ở đây.

## 3. Phạm vi A — Trạng thái đơn `Cho thanh toan`

### 3.1. Giá trị và chuyển trạng thái

Thêm giá trị thứ tư vào `HOADON.TrangThai`: `Cho thanh toan` (nhãn UI "Chờ thanh toán").

| Từ | Sang | Hành động | Điều kiện |
|---|---|---|---|
| `Chua thanh toan` | `Cho thanh toan` | "Yêu cầu thanh toán" (in tạm tính) | Đơn đang mở, có ≥1 dòng món, revision khớp, quyền `Sales` |
| `Cho thanh toan` | `Chua thanh toan` | "Mở lại đơn" | Quyền **ADMIN**, có receipt/đối chiếu, ghi actor + thời điểm |
| `Cho thanh toan` | `Da thanh toan` | Thu tiền | Như checkout hiện tại |
| `Chua thanh toan` | `Da thanh toan` | Thu tiền trực tiếp | Giữ nguyên hành vi hiện tại (không bắt buộc qua "Chờ thanh toán") |
| `Chua thanh toan` / `Cho thanh toan` | `Huy` | Hủy đơn | Như hiện tại: chưa thu tiền, chưa trừ nguyên liệu |
| `Da thanh toan` | — | Chỉ đổi `TrangThaiGiao` | Giữ nguyên |

> **Sửa đổi 20/11/2026 (xem §9):** bỏ chuyển `Cho → Chua` và bỏ nút "Mở lại đơn"; đơn
> `Cho thanh toan` vẫn nối thêm món. Bảng trên giữ nguyên làm lịch sử.

### 3.2. Khóa sửa

Khi `Cho thanh toan`:

- Chặn thêm/đổi số lượng/ghi chú/xóa dòng món; chặn sửa khách/ưu đãi/kênh/thông tin giao;
  chặn đổi bàn/loại đơn (vốn đã khóa sau khi tạo).
- Vẫn cho: xem, in lại, thu tiền, hủy đơn (có xác nhận như hiện tại).
- Khóa ở **tầng SQL** (procedure + trigger), không chỉ ẩn nút; UI phản ánh trạng thái và
  thông điệp rõ "Đơn đã yêu cầu thanh toán, chỉ có thể thu tiền hoặc hủy".
- Mọi chỗ đang dựa `TrangThai='Chua thanh toan'` để quyết định "còn sửa được" phải tách
  khỏi "còn thu được": tối thiểu `sp_SalesCheckOpen`, `sp_SuaDongHoaDon`,
  `sp_SuaThongTinHoaDon`, `sp_SalesCheckoutPreview`, `sp_ThanhToanHoaDon`, `sp_HuyHoaDon`,
  `sp_SalesReadCore`, trigger tính tiền/chốt đơn/thanh toán/tiêu hao/snapshot ưu đãi.

### 3.3. Hiển thị

- Nhãn đơn "Chờ thanh toán" trên POS, danh sách đơn đang mở, chi tiết hóa đơn.
- Bàn có đơn `Cho thanh toan` trả trạng thái riêng **"Chờ thanh toán"** (vẫn bận, không
  cho tạo đơn mới trên bàn đó).
- Không hiển thị "đã ra món" theo từng món: dự án chưa có luồng gửi bếp/đợt món, và ngành
  không khóa đơn theo mốc ra món (khách vẫn gọi thêm).

## 4. Phạm vi B — Phiên bàn

### 4.1. Dữ liệu

- Bảng mới `PHIEN_BAN`: `MaPhien` (khóa chính), `MaBan` (FK `BAN`), `GioVao`,
  `GioRa` (NULL khi còn mở), nhân viên mở/trả bàn để truy vết.
- Unique filtered index: **mỗi bàn chỉ có tối đa một phiên mở** (`GioRa IS NULL`).
- `HOADON.MaPhien` (NULL, FK) để gắn hóa đơn vào phiên. Đơn cũ không backfill —
  không tự bịa `GioVao` lịch sử.

### 4.2. Quy tắc

- **Mở phiên**: tự động trong transaction tạo đơn (đơn mới/đơn nháp flush) nếu bàn chưa có
  phiên mở; đơn được gắn `MaPhien`. Nháp chưa ghi SQL vẫn không chiếm bàn như hiện tại.
- **Trả bàn**: hành động rõ ràng của nhân viên (POS và màn Bàn), đóng phiên bằng `GioRa`.
  Chỉ cho trả bàn khi phiên không còn đơn `Chua thanh toan`/`Cho thanh toan`; nếu còn thì
  yêu cầu thu hoặc hủy trước. Khóa phiên khi cập nhật, chống bấm lặp.
- **Sau thu tiền**: hộp kết quả hỏi "Trả bàn ngay?" mặc định **có** (khách về luôn).
  Nếu chọn giữ bàn (khách trả trước còn ngồi), phiên vẫn mở, bàn vẫn `Đang dùng`.
  Lỗi trả bàn không làm rollback tiền: checkout đã commit thì vẫn thành công, hiển thị
  nhắc trả bàn và cho bấm lại.
- **Sau hủy đơn**: cùng cơ chế hỏi "Trả bàn ngay?", mặc định có.
- **Gọi thêm sau trả trước**: cho tạo hóa đơn mới trong cùng phiên khi bàn không còn đơn
  mở; hóa đơn mới có receipt riêng, **không gộp** với hóa đơn đã trả (giữ phạm vi không
  gộp/tách bill). UI hiển thị rõ khách sẽ nhận thêm một hóa đơn.
- **Không tự đóng phiên**: không có job hẹn giờ; bàn còn phiên mở thì còn bận (giống POS
  thực tế). Nhân viên quên trả bàn là lỗi vận hành, không tự đoán.

### 4.3. View trạng thái bàn

`vw_TrangThaiBan` tính theo phiên mở, có fallback tương thích dữ liệu cũ:

- Bàn không có phiên mở **và** không có đơn `Chua thanh toan`/`Cho thanh toan` chưa gắn
  phiên → `Trong`.
- Có đơn `Chua thanh toan` → `Dang phuc vu`.
- Có đơn `Cho thanh toan` (và không còn đơn `Chua thanh toan`) → `Cho thanh toan`.
- Phiên mở nhưng mọi hóa đơn đã trả/hủy → vẫn `Dang phuc vu` kèm cờ "đã trả · giữ bàn"
  để UI hiển thị khác nhãn thường.
- Đơn cũ không có `MaPhien` giữ hành vi hiện tại: bàn bận khi còn đơn chưa thanh toán.

### 4.4. Ràng buộc giữ nguyên

- Một đơn **chưa chốt**/bàn vẫn là bất biến (`UX_HD_BanMo` mở rộng cho cả `Cho thanh toan`).
- Không chuyển bàn, không gộp/tách hóa đơn, không đặt bàn/đặt cọc, không hoàn tiền,
  không phiếu bếp, không timeline/thống kê thời gian bàn trong đợt này.

## 5. Không làm

- Không khóa đơn theo từng món/"đã ra món"; không thêm trạng thái bếp/phiếu chế biến.
- Không gộp/tách bill, chuyển bàn, đặt bàn, cọc/thanh toán một phần, hoàn tiền.
- Không đổi cách tính tiền/kho/điểm, không đổi in hóa đơn ngoài bổ sung nhãn trạng thái.
- Không backfill phiên bàn cho dữ liệu lịch sử; không tự đóng phiên theo giờ.
- Không đổi 56 ID T14–T18, 325 ID toàn dự án, checklist/role hiện có.

## 6. Task con (đánh số tiếp, một task tại một thời điểm)

| Task | Nội dung | Migration dự kiến | Đầu ra chính |
|---|---|---|---|
| T33 | Đơn "Chờ thanh toán" | 032 | CHECK + view nhãn bàn; tách cổng sửa/thu; procedure yêu cầu TT, mở lại (ADMIN, receipt); trigger khóa; Models predicates; POS nút/nhãn; test |
| T34 | Phiên bàn và trả bàn | 033 | Bảng `PHIEN_BAN`, `HOADON.MaPhien`, index phiên mở, mở phiên khi tạo đơn, `sp_TraBan`, view theo phiên + fallback cũ, hộp "Trả bàn ngay?" sau thu/hủy, màn Bàn/POS, test |
| T35 | Tương thích và tài liệu | (không bắt buộc) | Rà toàn bộ chỗ đọc `TrangThai`/đếm đơn mở/báo cáo; cập nhật `docs/UX_QuanLiQuanCafe.md`, contract bán hàng/bàn/dữ liệu; regression G04 |

Số migration có thể dời nếu migration cấp quyền cho 031 được tạo trước; khi đó dùng số kế
tiếp còn trống, không sửa migration đã áp.

## 7. Giả định và giới hạn đã biết

- Đơn giữ trạng thái `Cho thanh toan` không tự hết hạn; mở lại chỉ qua ADMIN.
- Phiên bàn không lưu lý do/ghi chú; truy vết qua mã phiên, nhân viên, giờ mở/trả và receipt.
- Trả trước mà khách gọi thêm sẽ có nhiều hóa đơn trong một phiên; chấp nhận nhiều receipt,
  không gộp — nêu rõ trong UX để nhân viên biết.
- Bàn đang ở `Cho thanh toán` không nhận đơn mới cho tới khi thu/hủy.
- DPI/hình thức do người dùng tự kiểm theo phân công; agent chỉ kiểm chức năng.

## 8. Tiêu chí hoàn thành

- Requirements này được duyệt; sau đó bộ plan T33–T35 được duyệt một lần trước khi code.
- Migration áp được trên schema hiện tại, bảo toàn dữ liệu cũ và đơn đang mở.
- SQL Server thật/DB test riêng: khóa sửa đúng ở `Cho thanh toan`; thu/hủy vẫn đúng tiền/
  kho/điểm; ADMIN mở lại có receipt, CASH bị chặn; bàn bận theo phiên; trả bàn khi còn đơn
  mở bị chặn; trả trước giữ bàn rồi gọi thêm không nhân đôi tiền; hai phiên thao tác đồng
  thời không tạo hai phiên mở/không nhân bản đơn.
- Regression checkout/hủy/in/tra cứu/dashboard đếm đơn mở; không đổi 56 ID gốc.
- Cập nhật tài liệu UX/contract tương ứng trong phạm vi được duyệt.

## 9. Sửa đổi 20/11/2026 — "In tạm" và nối thêm món

**Nguồn:** người dùng chốt 20/11/2026 sau khi dùng thử T33: gọi quản lý mỗi lần khách gọi
thêm là không thực tế. Chốt lại luồng:

- Nút **"Yêu cầu thanh toán"** đổi tên **"In tạm"**, nổi bật ngang nút F8, bấm là lưu đơn
  sang `Cho thanh toan` và **in ngay, không hỏi xác nhận** (bỏ hộp "In tạm tính?").
- **Bỏ nút "Mở lại đơn"** khỏi POS (cả ADMIN và CASH). Giữ procedure `sp_MoLaiHoaDon` trong
  DB làm hợp đồng cũ, không gọi từ UI.
- Đơn `Cho thanh toan` **vẫn nối thêm món** vào cùng hóa đơn/bill, cùng `MaHD`, cùng phiên:

| Thao tác khi `Cho thanh toan` | Cho phép | Ghi chú |
|---|---|---|
| Thêm món chưa có | Có | Dòng mới, tính lại ưu đãi toàn đơn |
| Tăng số lượng món đã có cùng giá/ghi chú | Có | Gộp về một dòng (SL 1 → 2), quầy pha thêm |
| Giảm số lượng, xóa dòng, đổi ghi chú dòng | Không | SQL từ chối mã `52110`; UI khóa |
| F3 khách/ưu đãi/ship | Có (tạm thời) | Ưu đãi chốt ở bước thu; giảm giá tính lại toàn đơn |
| In lại | Có | Nút đổi thành "In lại"; chỉ đọc, không đổi trạng thái |
| Thu tiền / Hủy đơn / Trả bàn | Như T33–T34 | Không đổi |

- Migration `036_sales_pending_add_items.sql`: thay cổng `sp_SalesCheckOpen` bằng
  `sp_SalesCheckPayable` trong `sp_SuaDongHoaDon`, `sp_SuaThongTinHoaDon`; thêm luật chặn
  xóa/giảm/đổi ghi chú khi `Cho` (`52110`); giữ nguyên checkout/hủy/in.
- Mở rộng sau này (chưa làm): ưu đãi tự kích hoạt theo điều kiện số món trong `KHUYENMAI`
  và `fn_DiscountPreview`; hiện ưu đãi phải được chọn trên đơn.
- Test riêng trên DB test schema 036: in tạm không hỏi, thêm/gộp món, chặn xóa/giảm, F3
  sau in, in lại, thu tiền sau khi nối món, xung đột revision, hủy/trả bàn.
