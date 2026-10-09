# T01 — Xác nhận môi trường .NET 10 và repository đúng thư mục

**Trạng thái:** Đã nghiệm thu kỹ thuật — 7/7 ca PASS, chờ cho phép commit bàn giao.  
**Duyệt plan:** Người dùng xác nhận “duyệt” ngày 08/10/2026.  
**Duyệt requirements:** Người dùng xác nhận “duyệt” ngày 08/10/2026.  
**Ngày bắt đầu:** 08/10/2026.  
**Nguồn:** `docs/KeHoach_QuanLiQuanCafe.md` phiên bản 3.0, mục 3, 11, 12/T01 và 13.  
**Phụ thuộc:** Không.  
**Schema:** Hai file baseline trong `database/`; chưa chạy/kiểm chứng SQL Server.  
**Commit dự án:** Đã tạo repository riêng trên nhánh main; chưa có commit. Kết quả tại `test-results.md`.

## 1. Mục tiêu

Xác nhận máy hiện tại có thể phát triển, mở Designer, build và chạy WinForms
`net10.0-windows`; xác nhận SQL instance và quyền tạo database kiểm thử; thiết lập
repository riêng ở CoffeeManager và hướng dẫn môi trường có thể tái lập.

## 2. Phạm vi và đầu ra

- Kiểm tra SDK, Windows Desktop Runtime, Visual Studio và thành phần hỗ trợ
  WinForms/Designer; ghi phiên bản thực tế và phần còn thiếu nếu có.
- Tạo `global.json` pin SDK .NET 10 đang được kiểm chứng.
- Tạo một project/form thử tối thiểu để xác nhận toolchain và Designer. Nơi lưu
  project thử sẽ được chốt trong plan; không dùng project này để triển khai nghiệp vụ.
- Restore, build Debug/Release, chạy/đóng form và xác nhận Designer hoạt động.
- Xác nhận kết nối SSMS đến SQL Server đúng instance và quyền tạo DB test.
- Khởi tạo repository riêng tại `D:/Workspace/Nam_3/HK1/CoffeeManager`, giữ nguyên
  tài liệu, skill và baseline hiện có; không thay repository cha hoặc dự án anh em.
- Tạo `.gitignore` phù hợp .NET/Visual Studio, output build, cấu hình local chứa
  bí mật và backup; chỉ đưa file thuộc CoffeeManager vào repository riêng.
- Tạo `README.md` hướng dẫn môi trường, cách kiểm tra/build/run và phần cấu hình
  local; không ghi mật khẩu hoặc connection string chứa bí mật.
- Lưu `test-results.md` với bằng chứng và kết quả từng ca T01; cập nhật trạng thái
  theo kết quả thực tế. Commit bàn giao được thực hiện khi người dùng cho phép.

T01 chỉ xác nhận nền tảng. Chạy schema/seed và kiểm chứng nghiệp vụ thuộc T02;
migration/cấp mã thuộc T03; solution ba tầng + Models và tests thuộc T04.
Không cài/nâng cấp SDK, Visual Studio hoặc SQL Server nếu chưa phát hiện thiếu
thành phần và chưa thống nhất cách xử lý.

## 3. Khảo sát ban đầu (chỉ đọc, chưa nghiệm thu)

Đã chạy `dotnet --info`, `vswhere`, đọc trạng thái service SQL và
`git rev-parse --show-toplevel` trước khi viết requirements:

| Thành phần | Quan sát ngày 08/10/2026 |
|---|---|
| Hệ điều hành | Windows 10.0.26200, win-x64 |
| .NET SDK | 10.0.401; MSBuild 18.9.11 |
| Windows Desktop Runtime | 10.0.12 (có thêm 8.0.31) |
| Visual Studio | Community 2026, 18.10.2; bản cài complete/launchable |
| SSMS | 22.9.2; bản cài complete/launchable |
| SQL service | Default instance `MSSQLSERVER` đang Running |
| Git root | `D:/Workspace`, chưa đáp ứng T01-K05 |
| Nội dung dự án | `AGENTS.md`, `.agents/`, `database/`, `docs/`; chưa có source/project |
| global.json | Chưa có |

Các quan sát trên **không xác nhận** Designer chạy được, form build/run được,
SSMS kết nối thành công hoặc tài khoản có quyền tạo DB. T01-K01–K07 chưa được
đánh dấu PASS.

## 4. Giả định

- Giữ framework .NET 10 theo kế hoạch; dự kiến pin SDK 10.0.401 sau khi kiểm chứng.
- Dùng SQL Server default instance local đang chạy làm ứng viên phát triển/test,
  thử Windows Authentication; xác nhận danh tính server và quyền bằng truy vấn.
  Không tự chuyển sang một DB/instance khác nếu kết nối thất bại.
- Repository riêng là local trong CoffeeManager; chưa cần remote GitHub hoặc
  thay cấu hình Git toàn cục/repository cha.
- Nếu công cụ hiện tại không thao tác được Designer/SSMS hoặc không xác nhận
  trực quan form, người dùng thực hiện đúng ca manual và cung cấp kết quả.
  Ca đó giữ BLOCKED/NOT RUN đến khi có bằng chứng; không thay bằng suy đoán từ build.
- Sau nghiệm thu T01 mới chuyển sang requirements T02, tiếp tục tuần tự T01–T32.

## 5. Câu hỏi mở

Chưa có câu hỏi bắt buộc để chuẩn bị plan. Nếu bạn dùng SQL instance khác default
instance local, hãy chỉ rõ tên instance khi duyệt requirements; không gửi mật khẩu
vào tài liệu hoặc hội thoại.

## 6. Tiêu chí hoàn thành và nghiệm thu

| Mã | Yêu cầu | Bằng chứng cần ghi |
|---|---|---|
| T01-K01 | Form `net10.0-windows` mở Designer không lỗi | Project/form, phiên bản VS và kết quả mở Designer thực tế |
| T01-K02 | Build Debug/Release thành công; cửa sổ mở/đóng bình thường | Lệnh/output build và thao tác chạy/đóng |
| T01-K03 | SDK thực tế khớp global.json; đúng .NET 10 | `dotnet --info`/version và target framework project |
| T01-K04 | SSMS kết nối đúng SQL instance; có quyền tạo DB test | Instance/version/danh tính và kết quả kiểm tra quyền từ SSMS |
| T01-K05 | Git root là CoffeeManager | Output `git rev-parse --show-toplevel` sau thiết lập |
| T01-K06 | Git chỉ xét dự án này; bin/obj và secret local bị ignore | `git status`, `git check-ignore` với các đường dẫn đại diện |
| T01-K07 | Source copy/clone sạch restore/build ở nơi khác | Vị trí thử, cách lấy source sạch, lệnh và kết quả |

T01 hoàn tất khi đầu ra có thật, đủ bảy ca PASS và không còn lỗi trọng yếu;
biên bản ghi expected/actual, môi trường và dữ liệu/thao tác thực tế. Nếu ca manual
chưa được xác nhận hoặc quyền SQL chưa đủ, giữ T01 Đang làm và ghi rõ blocker.

## 7. Điểm duyệt

Requirements và `plan.md` đã được người dùng duyệt; đang triển khai đúng plan.
Sau triển khai, trình bày danh sách kiểm thử theo `post-change-test-workflow`.
