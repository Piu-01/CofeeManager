# Đợt test kỹ thuật B05/B06 — 15/11/2026

**Phạm vi:** ORBIT-UI-B05 (ô tìm kiếm có khung vuông ở trang lazy) và
ORBIT-UI-B06 (bảng Thu chi/Công nợ chừa khoảng trống, cắt nội dung cột).
Chỉ kiểm kỹ thuật/UI không tương tác; người dùng tự nghiệm thu hình thức.

## Môi trường

- Windows, màn hình 1536×864 scale 125% → `DeviceDpi 120` (đo trong probe).
- .NET 10.0 Windows, app tham chiếu bản Release dựng từ source hiện tại.
- Build `src/CafeManager.WinForms` Debug và Release: **PASS 0 warning 0 error**.
- Probe là project tạm ngoài repo, chỉ đọc control thật của app; không kết nối
  SQL Server, không ghi dữ liệu, không sửa source trong lượt test.
- Ghi nhận: lúc 21:14 quá trình build tạm thời lỗi `initialTarget` trong
  `Forms/MainForm.cs` do file này đang được sửa song song ngoài phạm vi
  B05/B06; sau khi file đó hoàn tất (21:15) build Debug/Release đều PASS.
  Không có thay đổi nào của B05/B06 liên quan lỗi này.

## Kết quả

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| L01a | Trang lazy Tồn kho (MaterialsControl trong OrbitTabControl), 125% | PASS: 1 TextBox, `BorderStyle=None`, nền `23,19,29`; số ô có viền vuông = 0 | probe.json `B05-lazy-tab-1`, b05-lazy-page.png |
| L01b | Chuyển sang trang lazy thứ hai (tab Nhập hàng), 125% | PASS: 2 TextBox đều `BorderStyle=None`, nền `23,19,29`; viền vuông = 0 | probe.json `B05-lazy-tab-2` |
| L02 | Palette/grid sau khi bỏ `AppTheme.Apply` trên trang lazy | PASS: header grid `37,31,46`, font Segoe UI như cấu hình Orbit | probe.json |
| F1 | Thu chi, 6 cột, cửa sổ 1400 (grid 1315), 125% | PASS: 6/6 cột `Fill`, tổng 1267 = 1315 − 48 row header, 0 cột cắt chữ | probe.json `B06-finance-1400`, b06-finance.png |
| F2 | Thu chi, cửa sổ 1366 (grid 1281) | PASS: tổng 1233 = 1281 − 48, 0 cột cắt chữ, không thanh cuộn ngang | probe.json `B06-finance-1366` |
| F3 | Thu chi, cửa sổ hẹp 1000 (grid 915) | PASS có ghi chú: 0 cột cắt chữ; tổng min 926 > 867 nên hiện thanh cuộn ngang thay vì rút chữ | probe.json `B06-finance-1000` |
| D1 | Công nợ NCC, 8 cột, cửa sổ 1400 | PASS: 8/8 `Fill`, tổng 1267 = 1315 − 48, 0 cột cắt chữ | probe.json `B06-debts-1400`, b06-debts.png |
| D2 | Công nợ NCC, cửa sổ 1366 | PASS: tổng 1233, 0 cột cắt chữ | probe.json `B06-debts-1366` |
| D3 | Công nợ NCC, cửa sổ hẹp 1000 | PASS có ghi chú: 0 cột cắt chữ; cuộn ngang do tổng min 1124 > 867 | probe.json `B06-debts-1000` |
| U1 | Unit test Finance, Inventory, HumanResources, G03 rules | PASS 52/52 | `dotnet test` filter, 51 ms |

Cột được coi là "cắt chữ" khi chiều rộng cần thiết (header + nội dung đã
format theo font thật, cộng padding) lớn hơn `Width` thực tế — cùng công thức
dùng trong `ApplyLedgerColumn`. Dữ liệu probe là mẫu tổng hợp có chuỗi dài
("Mua đá, trái cây, sữa tươi cho ca sáng", "Công ty Cà phê Trung Nguyên"),
không phải dữ liệu DB thật.

## Không chạy trong lượt này

- App thật + DB test, đăng nhập và thao tác tải/lọc/chuyển trang/mở chi tiết:
  **NOT RUN** theo phân công (người dùng tự kiểm GUI; đợt này không đổi
  backend/SQL nên không có ca ghi dữ liệu).
- DPI 100% và 150%: **NOT RUN** (quy định chỉ tự chạy 125%).
- Designer: **NOT RUN**.
- Hành vi khi cửa sổ hẹp hơn tổng min width: có cuộn ngang, không có ellipsis
  — ghi nhận là hành vi đã chọn, không phải pixel-perfect ở mọi viewport.

## Tệp bằng chứng

- `probe.json` — số đo từng ca (border, màu, width/minwidth/fillweight/required).
- `probe.log` — log chạy probe.
- `b05-lazy-page.png`, `b06-finance.png`, `b06-debts.png` — ảnh kỹ thuật từ
  probe (không phải nghiệm thu hình thức).
