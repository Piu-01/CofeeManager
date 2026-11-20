# Test results — bug combo hiển thị tên enum / nhãn lookup

- Ngày chạy: 16/11/2026 02:35–02:37 (giờ máy)
- Người chạy: agent (kiểm chức năng), người dùng tự kiểm hình thức theo phân công 22/10/2026
- Phạm vi: B01–B04 của `report.md` trong cùng thư mục
- Source: bản đã sửa B01–B04 (chưa commit); build Debug `CafeManager.WinForms.dll` 16/11/2026 02:32
- Môi trường: Windows, .NET SDK 10.0.401, app build `net10.0-windows`, DPI 125% (120)
- DB: `localhost` / `CoffeeManager_Development` (DB phát triển hiện có), đăng nhập `admin` (ADMIN) qua
  quick-login development của chính app. Chỉ đọc/không ghi dữ liệu nghiệp vụ.
- Cách chạy: probe WinForms riêng (ngoài repo, `%TEMP%`), tham chiếu build sẵn của
  `CafeManager.WinForms`, tạo `ReportsControl`, `EmployeesControl` (qua reflection vì internal)
  và các dialog thật, đọc item/text của ComboBox, gọi `CaptureFilter`, bấm `Xem báo cáo`.
  Raw: `runs/20261116/probe-results.json`.

## Kết quả

| Mã | Ca | Expected | Actual | Kết quả |
|---|---|---|---|---|
| P01 | Đăng nhập ADMIN quick-login development | Vào được DB dev | `CoffeeManager_Development`, role ADMIN | PASS |
| B01-1 | Item combo loại báo cáo không phải tên enum | Không dòng nào là `SalesDay…Assets` | 9 dòng đều là nhãn | PASS |
| B01-2 | 9 dòng đúng nhãn VI theo thứ tự | `Doanh thu theo ngày … Tài sản / Khấu hao tham khảo` | Khớp đủ 9 | PASS |
| B01-3 | Chọn dòng thứ i tạo đúng `ReportKind` thứ i | Đủ 9/9 | Index 0–8 → SalesDay…Assets khớp | PASS |
| B01-4 | Xem báo cáo Dòng tiền quản trị | Description bắt đầu `Dòng tiền quản trị`, có dòng dữ liệu | 5 dòng; Thu 138.947.660 ₫, Chi 130.339.510 ₫, Thu − Chi 8.608.150 ₫; có dòng `Sales · Thu bán hàng` | PASS |
| B01-5 | Xem báo cáo Đối chiếu tồn kho | Metadata đúng loại | 20 dòng, Description bắt đầu `Đối chiếu tồn kho` | PASS |
| B01-6 | Combo loại báo cáo ở English | Nhãn EN, không phải enum | `Daily revenue … Assets / Estimated depreciation` | PASS |
| B02-1 | Combo bộ lọc phụ khi chọn Phương thức thanh toán | Hiện `Tất cả`, không lộ ToString | Text `Tất cả`, 11 lựa chọn (thêm 10 phương thức) | PASS |
| B02-2 | Chọn một phương thức cụ thể | Request gửi đúng `Code` | Chọn `Chuyen khoan ngan hang` → `Filter.Method = PT002` | PASS |
| B03-1 | Bộ lọc Chức vụ ở Hồ sơ nhân sự | 1 + N tên chức vụ, không có `HumanResourcesLookup` | 11 item = "Mọi chức vụ" + 10 chức vụ (`Quan ly`, `Pha che`, …) | PASS |
| B03-2 | Chọn chức vụ cụ thể | Lọc chạy, không lỗi | Chọn `Quan ly` (CV001) → trạng thái `Đã cập nhật 02:37:27` | PASS |
| B04-1 | Ghi phiếu thu/chi — combo danh mục | Hiện tên danh mục | 9 danh mục, Text `Tien dien` | PASS |
| B04-2 | Nhập kho — NCC và nguyên liệu | Hiện tên | NCC 10 dòng (`Cty Bao Bi Xanh`…), nguyên liệu 20 dòng | PASS |
| B04-3 | Xuất kho — nguyên liệu | Hiện tên | 20 nguyên liệu, Text `Banh croissant thanh pham` | PASS |
| B04-4 | Trả nợ NCC — phương thức | Hiện tên | 2 phương thức, Text `Tien mat` | PASS |

Tổng: 15/15 PASS.

## Ghi chú / phần chưa chạy

- **Hình thức giao diện**: SKIPPED theo phân công 22/10/2026 — người dùng tự kiểm bằng mắt
  (màu, spacing, chiều rộng combo, dropdown).
- **DPI 100% / 150%**: NOT RUN — chỉ kiểm ở 125% như quy định; không đổi Windows Scale.
- Dữ liệu là DB phát triển hiện có; không tạo fixture ghi. Với B04 chỉ mở dialog, không bấm
  chốt/lưu.
- Probe nằm ngoài repo nên không commit; `probe-results.json` trong `runs/20261116/` là bằng
  chứng đã lưu.
- Đính chính so với dự đoán trong báo cáo: B02/B04 trước sửa không hiện raw record (lookup đã
  override `ToString`), chỉ khác nhãn. Lỗi nặng thật sự là B01 và B03.
