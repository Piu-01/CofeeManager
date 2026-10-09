# Phát hiện khi kiểm chứng T01

**Ngày:** 08/10/2026.  
**Trạng thái:** Hai hướng xử lý đã được duyệt và áp dụng; kiểm chứng kỹ thuật PASS.  
**Duyệt:** Người dùng xác nhận “duyệt” ngày 08/10/2026.  
**Phạm vi:** Project kiểm chứng môi trường và kết nối SQL local; chưa chạy baseline/seed.

## 1. Cảnh báo CS8669 trong file Designer

### Hiện tượng và bằng chứng

Debug, Release và bản copy source sạch đều build thành công nhưng có 1 warning:

```text
EnvironmentProbeForm.Designer.cs(5,45): warning CS8669:
The annotation for nullable reference types should only be used in code
within a '#nullable' annotations context. Auto-generated code requires
an explicit '#nullable' directive in source.
```

Tại dòng 5:

```csharp
private System.ComponentModel.IContainer? components = null;
```

### Nguyên nhân

File `.Designer.cs` được compiler coi là generated code. Nullable đã bật ở
project, nhưng nullable annotation `?` trong generated code vẫn cần context
`#nullable` tường minh. File hiện chưa có directive đó.

### Cách sửa đề xuất

Thêm **một dòng `#nullable enable`** trước namespace trong
`tools/CafeManager.EnvironmentProbe/EnvironmentProbeForm.Designer.cs`.
Không thay logic/UI, không tắt warning hoặc bỏ nullable của toàn project.

### Ảnh hưởng và verify

Chỉ ảnh hưởng context phân tích nullable của file Designer. Build Debug/Release
và bản source sạch lại; expected 0 warning, 0 error. Designer vẫn cần kiểm chứng
thực tế trong T01-K01.

## 2. Kết nối SQL local bị chặn bởi xác minh chứng chỉ

### Hiện tượng và bằng chứng

```powershell
sqlcmd -S localhost -E -b -l 10 -i docs/tasks/g01-nen-tang-ky-thuat-du-lieu/t01-moi-truong-repository/sql-environment-check.sql -W -s "|"
```

Exit 1 với ODBC Driver 18:

```text
SSL Provider: The certificate chain was issued by an authority that is not trusted.
Client unable to establish connection.
```

Service `MSSQLSERVER` đang Running. Query chưa chạy; chưa biết version engine,
login SQL hoặc quyền tạo DB. Chưa kiểm chứng qua SSMS.

### Nguyên nhân đã có bằng chứng

Client không chấp nhận chuỗi chứng chỉ của kết nối local. Thông báo chứng minh
lỗi trust chain; chưa đủ dữ liệu khẳng định certificate tự ký hay CA nào thiếu.
Lỗi này xảy ra trước query, không do schema/seed hoặc cú pháp script.

### Cách xử lý đề xuất cần duyệt

Cho phép **Trust server certificate chỉ cho kết nối phát triển local này**:

- Chạy lại script chỉ đọc bằng `sqlcmd -S localhost -E -C ...`.
- Trong SSMS, kết nối `localhost` với Windows Authentication và tùy chọn
  **Trust server certificate** để kiểm chứng T01-K04.
- Ghi rõ cấu hình và kết quả vào README/biên bản. Không thay cấu hình SQL Server,
  không ghi tùy chọn này thành mặc định runtime hoặc áp dụng cho máy/LAN khác.

### Ảnh hưởng và verify

Tùy chọn trust bỏ xác minh chuỗi chứng chỉ cho lần kết nối; chỉ áp dụng khi
người dùng xác nhận đúng SQL instance local. Không thay quyền SQL hoặc dữ liệu.
Sau kết nối, script phải trả đúng tên máy/default instance và quyền tạo database.
Nếu phát sinh lỗi xác thực/quyền, ghi nhận lỗi mới trước khi đề xuất sửa tiếp.
Kết quả CLI không thay kết quả thao tác SSMS.

## 3. Kết luận

Đã áp dụng đúng hai hướng được duyệt:

- Thêm `#nullable enable` vào Designer; Debug/Release và bản source copy sạch mới
  đều build với 0 warning, 0 error.
- Chạy query chỉ đọc qua `sqlcmd -S localhost -E -C ...`: exit 0, đúng default
  instance `MSSQLSERVER` trên `LAPTOP-79UNRIRC`, engine 17.0.1000.7 Enterprise
  Developer Edition (64-bit); các cột quyền tạo DB đều là 1.

Không thay cấu hình SQL Server/runtime hoặc chạy baseline/seed. T01 giữ Đang làm
chờ commit bàn giao; hai ảnh người dùng đã xác nhận Designer và SSMS PASS.
Chi tiết trong `test-results.md`.
