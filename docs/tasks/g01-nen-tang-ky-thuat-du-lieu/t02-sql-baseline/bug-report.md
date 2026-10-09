# T02 — Lỗi nội bộ và regression

## Runner PowerShell 5.1 đọc encoding

- Lượt đầu: `& .\tools\database\Test-Baseline.ps1` bị ParserError trước khi
  kết nối SQL; các ca DB chưa chạy.
- Nguyên nhân: Windows PowerShell 5.1 đọc script UTF-8 không BOM bằng ANSI;
  chữ tiếng Việt trong chuỗi báo cáo bị giải mã sai, có ký tự thành dấu quote.
- Sửa trong scope: dùng chuỗi ASCII trong runner; SQL/JSON/Markdown vẫn đọc/ghi
  UTF-8 rõ ràng. Không thay baseline/nghiệp vụ.
- Regression: chạy lại toàn runner, lưu kết quả thực tế bên test-results.

## SqlConnectionStringBuilder trong PowerShell

- Lượt `20261008` lưu trong runs/: setup lỗi `Keyword not supported: 'DataSource'`,
  chưa tạo DB. PowerShell coi property assignment của builder (IDictionary) là
  khóa chuỗi kết nối, không ánh xạ tên property C#.
- Sửa: đặt khóa chuẩn `Data Source`, `Initial Catalog`, `Integrated Security`,
  `Connect Timeout`, `Application Name` qua indexer. Chạy lại full suite.

## Query phiên bản engine

- Setup tiếp theo bị SQL từ chối implicit conversion sql_variant khi CONCAT
  SERVERPROPERTY. Chưa tạo DB. Sửa query metadata bằng CONVERT NVARCHAR rõ ràng.

## Lượt 205456 — helper và thu bằng chứng

- Catalog/seed SQL assertions chạy được nhưng state query `SELECT XACT_STATE()`
  trả 1 khi statement đang thực thi ở engine 17 dù @@TRANCOUNT=0. Đo state vào
  biến trước SELECT để không đo transaction autocommit của chính query đọc.
- ConvertFrom-Json của PowerShell 5.1 trả array như một object pipeline; bọc @()
  tạo array lồng, ghép 35 tên bảng thành một identifier. Bỏ lớp bọc đó.
- Helper #T02_Pay được compile trong tempdb nên không tìm thấy TVP của fixture.
  Ánh xạ prefix helper sang dbo.T02_ chỉ trong DB fixture khi đọc SQL test.
  Baseline giữ nguyên. Lượt lỗi đã lưu tại runs/20261008_205456_aa3e3cdd.json.

## Kết quả regression

Lượt `20261008_205557_93421dd4`: đủ T02-K01–K12 PASS, mọi subcase PASS.
Lệnh `& .\tools\database\Test-Baseline.ps1` exit 0. Baseline không cần sửa;
các lỗi nêu trên thuộc runner/fixture. Bằng chứng expected/actual và SQL đầy đủ
ở `test-results.json`; lịch sử lỗi được giữ trong `runs/`.
