# Đợt test B07 — chuẩn kích thước bảng theo màn Giao hàng, 15/11/2026

**Phạm vi:** ORBIT-UI-B07 — mọi `OrbitDataGridView` dùng chung chiều cao dòng 46
và header 40 logical (metrics của màn Giao hàng); dòng OrderLines có ghi chú 64.
Không đổi cỡ chữ, cột, binding, backend/SQL. Chỉ kiểm kỹ thuật; người dùng tự
nghiệm thu hình thức.

## Môi trường

- Màn hình 1536×864 scale 125% → `DeviceDpi 120` đo trong probe.
- Build Debug và Release: **PASS 0 warning 0 error**.
- Probe: project tạm ngoài repo, tham chiếu DLL Release dựng từ source hiện tại,
  chỉ đọc control thật; không kết nối SQL, không ghi dữ liệu.
- Source giữ nguyên trong lượt test; hash 2 file đã sửa ở `source-after.blob.txt`.

## Kết quả

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| P1 | 16/16 profile: Inventory, Menu, Tables, Areas, Customers, Promotions, Ranks, Points, Catalogs, Payment, OpenOrders, InvoiceHistory, Delivery, OrderLines, Finance, SupplierDebts | PASS: dòng = 58px, header = 50px (tức 46/40 logical ở 125%), đồng nhất với Delivery | probe.json `B07-*` |
| P2 | Cỡ chữ không đổi ngoài phạm vi | PASS: ô 9pt (Payment 10.5pt), header 7.5pt | probe.json |
| P3 | Bảng Thu chi/Công nợ sau thay đổi | PASS: vẫn `Fill`, 0 cột cắt chữ tại 1366/1400; cửa sổ hẹp 1000 cuộn ngang thay vì rút chữ | probe.json `B06-*` |
| P4 | Trang lazy (Tồn kho/Nhập hàng) sau thay đổi | PASS: không ô viền vuông, palette giữ nguyên | probe.json `B05-lazy-*` |
| U1 | Unit test Finance, Inventory, HumanResources, G03 | PASS 52/52 | dotnet test |

Ghi chú: `OrderPanel.CancelNoteEdit` đổi 46/64 để khớp chiều cao dòng mới; probe
chỉ đo grid, chưa đo dialog thanh toán POS (xem mục NOT RUN).

## Không chạy trong lượt này

- App thật + DB test, đăng nhập và thao tác: **NOT RUN** theo phân công (người
  dùng tự kiểm GUI; không đổi backend/SQL).
- POS: giỏ hàng có ghi chú, dialog thanh toán sau khi dòng cao hơn (chiều cao
  dialog tính theo `RowTemplate.Height`): **NOT RUN** — cần xem bằng mắt để chắc
  không tràn ở 720px.
- DPI 100%/150% và Designer: **NOT RUN** theo quy định.

## Tệp bằng chứng

- `probe.json` — metrics từng profile + kiểm cột Thu chi/Công nợ.
- `probe.log` — log lượt chạy.
- `b05-lazy-page.png`, `b06-finance.png`, `b06-debts.png` — ảnh kỹ thuật từ probe.
- `source-after.blob.txt` — hash source 2 file đã sửa tại thời điểm test.
