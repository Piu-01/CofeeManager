# Regression LOC-B01–B03 — 10/11/2026

Người dùng đã duyệt sửa cả batch sau báo cáo test. Đã sửa đúng ba nguyên nhân;
duyệt debug bao gồm lượt regression này. **Cả ba bug PASS theo phạm vi kiểm dưới
đây**; không nâng toàn task thành Hoàn tất vì còn các nhánh native chưa đủ bằng chứng.

## Thay đổi

- LOC-B01: `OrderPanel` và `InvoiceDetailDialog` dùng `L.T("SL")`; VI giữ `SL`, EN
  dùng `QTY`. Tên cột và DataPropertyName Quantity giữ nguyên.
- LOC-B02: nút preview dùng `L.T("In…")`; VI giữ `In…`, EN là `Print…`. Giữ handler,
  owner và đường in sau commit; regression không gửi lệnh in thật.
- LOC-B03: ba nhánh lỗi cấu hình trong Program giữ template VI, LoginForm dịch khi
  hiển thị. Không thêm tra ngược EN→VI, không đổi kết nối/xác thực/phiên.
- Audit thêm SL/In… vào bộ phát hiện literal cần dịch. Thêm một unit test kiểm nhãn
  qua VI→EN→VI. Thay đổi sản phẩm/test/audit giới hạn đúng bảy file, không SQL/migration.

## Môi trường và lệnh

Windows, SDK 10.0.401. Component probe dùng Form/control thật, DPI đo từ MainForm
là 120 (125%). Dữ liệu hóa đơn synthetic: số lượng 3, giá 27.000, phải trả 81.000,
tên và ghi chú tiếng Việt. Không gọi DB, không dùng credential thật. Preference
language.txt được lưu byte gốc và khôi phục trong finally; form test đã dispose.

Evidence tính từ `runs/debug-b01-b03-20261110/`:

| Kiểm | Expected / actual | Kết quả |
|---|---|---|
| Build Debug | `dotnet build CafeManager.slnx --no-restore -m:1 -c Debug -v:q`; 0 warning / 0 error | PASS, `debug.log` |
| Build Release | Cùng lệnh với `-c Release`; 0 warning / 0 error | PASS, `release.log` |
| Build unit | `dotnet build tests/CafeManager.Tests --no-restore -m:1 -o artifacts/localization-bug-regression/unit -v:q`; 0 warning / 0 error | PASS, `unit-build.log` |
| Unit | **153 PASS, 0 FAIL, 0 SKIPPED**; 152 ca cũ +1 ca mới | `unit-regression.trx` |
| Audit | **1.521 used / 1.521 EN keys, 0 violations** | PASS, `audit.log` |
| Audit fixture âm | Chỉ tạo hai literal chưa bọc SL/In… trong root fixture riêng; expected exit1 và báo cả hai | PASS, `audit-negative.json`, `audit-negative.log` |
| Component | **21/21 PASS**, chi tiết bên dưới | `component-results.json` |
| Source cố định | So sánh toàn source/migration/test/tool trước/sau regression: **0 thay đổi** | `source-before.csv`, `source-after.csv` |

Lệnh unit (ngoài sandbox vì testhost IPC từng bị chặn):

```powershell
dotnet vstest artifacts/localization-bug-regression/unit/CafeManager.Tests.dll '--TestCaseFilter:FullyQualifiedName!~CafeManager.Tests.Integration&FullyQualifiedName!~CafeManager.Tests.G02' '--Logger:trx;LogFileName=unit-regression.trx' '--ResultsDirectory:docs/tasks/localization-vi-en/runs/debug-b01-b03-20261110'
```

Lệnh component:

```powershell
dotnet artifacts/localization-bug-regression/component/Regression.dll docs/tasks/localization-vi-en/runs/debug-b01-b03-20261110 D:/Workspace/Nam_3/HK1/CoffeeManager
```

## 21 ca component và expected/actual

- **B03-production-templates (1 ca):** xác minh ba nhánh cấu hình của Program giữ
  raw template, không dịch trước khi truyền login. Actual đúng cả ba nhánh.
- **B03-* (12 ca):** ba template (thiếu kết nối, cấu hình không hợp lệ, DEV không
  hợp lệ) ×hai ngôn ngữ khởi đầu ×đổi sang ngôn ngữ kia rồi trở lại. Actual status
  và title khớp L.T ở mỗi bước; username/password fixture giữ nguyên, nút login
  không bị mở khóa khi thiếu auth. Không in password vào evidence.
- **B01-POS-* (2 ca):** SL/QTY đúng theo VI/EN; cột vẫn bind Quantity và đọc đúng
  giá trị 3 từ SalesLine trên grid WinForms.
- **B01-detail-* (2 ca):** mở InvoiceDetailDialog thật với DTO fixture; SL/QTY và
  giá trị Quantity=3 đúng ở cả hai ngôn ngữ.
- **B02-preview-* (2 ca):** mở preview thật có owner MainForm; caption và nút
  In…/Print… đúng theo VI/EN. Không kích hoạt nút in hoặc thay handler tài chính.
- **B01-02-data-* (2 ca):** sau dựng các màn, phải trả vẫn 81.000, tên `Món Việt`
  và ghi chú `Ghi chú Việt` giữ nguyên.

Assertions và actual riêng từng ca ở component-results.json. Không chỉnh expected
hoặc sửa source trong lượt regression đã chốt.

## Giới hạn giữ nguyên

Không chạy lại các luồng SQL vì fix không chạm SQL/DAL/BLL/contract hay mã dữ liệu;
kết quả SQL của lượt trước được giữ đúng phiên bản lịch sử, không gọi là lượt mới.
Không kiểm in thật, lỗi máy in hoặc đa trang trong lượt này. Hình thức SKIPPED theo
phân công; DPI100%/150% NOT RUN. Full native click/Tab/logout/nháp/unknown vẫn chưa
đủ bằng chứng do giới hạn geometry/input đã ghi trong report; không suy PASS từ
component/message loop. Không commit/push hoặc thay checklist T01–T32.
