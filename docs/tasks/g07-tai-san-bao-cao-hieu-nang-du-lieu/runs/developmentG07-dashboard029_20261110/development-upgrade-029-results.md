# Nâng development 027 → 028 → 029 — 10/11/2026

Phạm vi người dùng duyệt: nâng đúng `localhost/CoffeeManager_Development`, có backup,
không reset dữ liệu, để mở lại màn Tổng quan (bug G07-DEV02). Đây là kiểm chức năng/
kỹ thuật; hình thức do người dùng tự kiểm.

## Môi trường

- SQL Server localhost (integrated security), engine thật; Windows 10.0.26200 x64.
- Source: `main` commit `8a38f86` + thay đổi làm việc hiện có; không sửa source ứng dụng.
- App `CafeManager.WinForms` đã được người dùng đóng trước khi chạy; script không kill process.

## Công cụ và bằng chứng

- Runner ASCII: [upgrade-runner.ps1](upgrade-runner.ps1) (Windows PowerShell 5.1 đọc sai
  UTF-8 của `OperationsTools.ps1`, nên runner tự chứa thay vì dot-source).
- [upgrade-manifest.json](upgrade-manifest.json), `028.json`, `029.json`,
  [schema-before.json](schema-before.json), [schema-after.json](schema-after.json),
  [procedures-after.json](procedures-after.json).
- Lần chạy đầu dừng tại bước tra checksum trong transaction (lỗi runner, chưa áp migration);
  xác nhận DB vẫn 27 rồi mới chạy lại. Không có migration nào được áp ở lần lỗi.

## Kết quả

| Hạng mục | Expected | Actual | Kết quả |
|---|---|---|---|
| Preflight marker/version | Marker khớp, version 27 | Khớp, 27 | PASS |
| Checksum 001–027 | Khớp file | 27/27 khớp | PASS |
| Backup | COPY_ONLY/CHECKSUM, VERIFYONLY PASS, nguồn ổn định | `…before_dashboard029_20261110_122928_5054ef6e.bak`, GUID `1a3b8297-…`, SourceStable=true | PASS |
| Áp 028 | `Applied` | `Applied`, checksum `121C4936…C4C4` | PASS |
| Áp 029 | `Applied` | `Applied`, checksum `05E2508D…6361` | PASS |
| Version sau nâng | 29 | 29 | PASS |
| Procedure tổng quan | 2 procedure | 2 (`sp_DashboardAnalyticsRead`, `sp_DashboardAnalyticsSources`) | PASS |
| GRANT EXECUTE cho `CafeManagerApp` | 2 | 2 | PASS |
| Bảng/cột/index không đổi | Không đổi | `SchemaTablesColumnsUnchanged=true` | PASS |
| Dữ liệu nghiệp vụ không đổi | Không đổi | `DataUnchanged=true` | PASS |
| `sp_AppReadState` | `SchemaVersion=29` | 29, State Completed | PASS |
| Màn Tổng quan trên app | Không còn nhánh 53502, hiện số thật | NOT RUN — cần người dùng mở app đăng nhập ADMIN | NOT RUN |

## Lưu ý còn mở

- **G07-DA02** (cập nhật UI 3–6 giây với 10.000 bill) chưa được sửa; nâng schema không
  giải quyết độ chậm. Cần đợt debug được duyệt riêng.
- Backup giữ nguyên tại đường dẫn trong manifest; không tự xóa.
- Không ghi fixture, không reset mật khẩu, không đổi Models/DTO trong lượt này.
