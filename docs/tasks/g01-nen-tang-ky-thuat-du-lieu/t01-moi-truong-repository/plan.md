# Plan T01 — Môi trường .NET 10 và repository riêng

**Trạng thái:** Đã duyệt — nghiệm thu kỹ thuật 7/7 ca PASS, chờ commit bàn giao.  
**Duyệt plan:** Người dùng xác nhận “duyệt” ngày 08/10/2026.  
**Ngày:** 08/10/2026.  
**Requirements:** [requirements.md](requirements.md), đã duyệt ngày 08/10/2026.  
**Tham chiếu:** Kế hoạch phiên bản 3.0, T01; nghiệm thu T01-K01–K07.

**Lịch sử:** Plan/kết quả T01 được kế thừa trong bộ plan G01 (`../plans.md`).
Hồ sơ đã chuyển vào G01; đường dẫn tham chiếu được cập nhật, không chạy lại test.
Các điểm duyệt cũ dưới đây là lịch sử, quy trình mới áp dụng cho lượt tiếp theo.

## 1. Quyết định triển khai

- Pin SDK **10.0.401** đã phát hiện; target **net10.0-windows**. Dùng
  `rollForward: disable` để bản SDK khác không được chọn ngầm.
- Project kiểm chứng độc lập tại `tools/CafeManager.EnvironmentProbe/`, chỉ có
  form native tối thiểu và điểm khởi chạy chuẩn WinForms. Project giữ lại để tái
  kiểm chứng môi trường, không làm nền nghiệp vụ hoặc tạo trước solution T04.
- Repository Git local tại gốc CoffeeManager, nhánh `main`; giữ nguyên nội dung
  dự án hiện có. Không cấu hình remote hoặc sửa Git toàn cục/repository cha.
- SQL ứng viên là default instance local (`localhost`), Windows Authentication.
  T01 chỉ đọc thông tin instance/quyền, không chạy baseline/seed hoặc tạo DB.
- Designer, chạy/đóng form và kết nối SSMS phải được xác nhận thực tế; nếu không
  có công cụ GUI phù hợp, cung cấp thao tác manual để người dùng thực hiện.

## 2. Các bước thực hiện

### Bước 1 — Xác nhận nền tảng và lập biên bản môi trường

**Khu vực:** `docs/tasks/g01-nen-tang-ky-thuat-du-lieu/t01-moi-truong-repository/environment.md`.

- Kiểm tra lại `dotnet --info`, SDK/runtime, bản cài VS/SSMS qua `vswhere`.
- Kiểm tra VS có workload/thành phần desktop .NET và WinForms template hay chưa;
  phân biệt thành phần đã cài với kết quả Designer đã mở được.
- Ghi phiên bản thực tế, ngày khảo sát và câu lệnh; không ghi thông tin bí mật.
- Thiếu thành phần thì ghi blocker và đề xuất xử lý để người dùng duyệt.

**Verify:** Có dữ liệu thực tế cho toolchain; đủ cơ sở chọn SDK cho T01-K03.

### Bước 2 — Thiết lập Git riêng và quy tắc ignore

**Khu vực:** `.git/`, `.gitignore`, mục thông tin Git trong `environment.md`.

- Kiểm tra Git root, metadata `.git` hiện có và file thuộc CoffeeManager trước
  thay đổi; nếu đã có repository riêng thì dùng lại, không ghi đè metadata.
- Khởi tạo `git init -b main` tại CoffeeManager nếu chưa có repo riêng.
- Tạo ignore cho `bin/`, `obj/`, `.vs/`, file người dùng VS, cấu hình local/bí mật,
  output publish, backup SQL và thư mục dữ liệu thử local. Không ignore tài liệu,
  baseline, skill, `.resx` hoặc file Designer của project.
- Mọi thao tác Git sau đó chỉ nhắm repo CoffeeManager; không stage/commit tự động.

**Verify:** `git rev-parse --show-toplevel` trả CoffeeManager (T01-K05);
`git status --short` không liệt kê dự án anh em và `git check-ignore` khớp các
đường dẫn output/secret đại diện (T01-K06).

### Bước 3 — Pin SDK và tạo project kiểm chứng

**File:** `global.json`, `tools/CafeManager.EnvironmentProbe/CafeManager.EnvironmentProbe.csproj`,
`Program.cs`, `EnvironmentProbeForm.cs`, `EnvironmentProbeForm.Designer.cs`,
`EnvironmentProbeForm.resx` khi template/Designer cần.

- Dùng cấu trúc template WinForms tiêu chuẩn, `UseWindowsForms=true`, output
  `WinExe`, target `net10.0-windows`; không thêm package nghiệp vụ.
- Form chỉ có tiêu đề/thông tin kiểm chứng dễ nhận diện, hỗ trợ Designer;
  không theme/custom control, không truy cập SQL, không chứa use case tương lai.
- Dùng `ApplicationConfiguration.Initialize()` và điểm vào STA chuẩn.

**Verify:** Project không phụ thuộc đường dẫn tuyệt đối; SDK chọn đúng pin
(T01-K03); Designer có thể mở form (phần manual T01-K01).

### Bước 4 — Chuẩn bị kiểm chứng kết nối SQL

**File:** `docs/tasks/g01-nen-tang-ky-thuat-du-lieu/t01-moi-truong-repository/sql-environment-check.sql`.

- Viết script chỉ đọc chạy trong `master`: tên server/instance, phiên bản/edition,
  login hiện tại và quyền `CREATE ANY DATABASE`/vai trò liên quan.
- Kiểm tra service default instance; chuẩn bị hướng dẫn mở SSMS với Windows
  Authentication và chạy script trên đúng server.
- Có thể đọc thông tin qua CLI nếu công cụ sẵn có, nhưng kết quả CLI không thay
  cho yêu cầu kết nối **SSMS** của T01-K04.
- Lỗi kết nối/chứng chỉ/quyền được ghi rõ; không đổi instance hay bỏ qua xác minh
  chứng chỉ ngầm, không thu thập mật khẩu vào file.

**Verify:** SSMS kết nối đúng instance và query xác nhận có quyền tạo DB test
(T01-K04). Chưa có quyền hoặc chưa thao tác SSMS thì ghi BLOCKED/NOT RUN.

### Bước 5 — Viết hướng dẫn tái lập và chuẩn bị nghiệm thu

**File:** `README.md`, `docs/tasks/g01-nen-tang-ky-thuat-du-lieu/t01-moi-truong-repository/test-results.md`,
cập nhật `environment.md`, `requirements.md`, `plan.md`; `AGENTS.md` nếu Git root
đã thay đổi cần cập nhật thông tin hiện trạng.

- README ghi SDK/runtime/VS/SQL, cách mở project thử, restore/build/run, bước
  Designer/SSMS manual và cấu hình local. Dùng đường dẫn tương đối cho lệnh.
- Biên bản kiểm thử có đủ T01-K01–K07, expected/actual và bằng chứng; trước khi
  chạy giữ NOT RUN, không đánh dấu PASS chỉ vì đã tạo file.
- Trình bày danh sách nghiệm thu sau triển khai và xin xác nhận theo skill
  `post-change-test-workflow`; chạy các kiểm tra bắt buộc theo chỉ dẫn công cụ.

**Verify:** Hướng dẫn đủ để thực hiện các ca nghiệm thu, không chứa secret.

### Bước 6 — Kiểm chứng và bàn giao T01

**Khu vực:** Project thử, DB metadata chỉ đọc, bản copy sạch ở thư mục tạm được
phép `C:/Users/Lenovo/AppData/Local/Temp/opencode/`, biên bản task.

- Restore project; build Debug và Release; lưu lệnh/kết quả.
- Mở form bằng Designer trong VS; chạy executable rồi đóng cửa sổ bình thường.
  Khi không điều khiển GUI được, hướng dẫn người dùng và ghi kết quả họ cung cấp.
- Xác nhận SDK/target, SSMS/quyền SQL và Git root/ignore.
- Copy source cần thiết cùng `global.json` sang một thư mục tạm mới, bỏ bin/obj,
  `.git`, cấu hình local và dữ liệu nhạy cảm; restore/build lại từ bản sạch.
- Cập nhật từng ca PASS/FAIL/BLOCKED/NOT RUN. Nếu lỗi, ghi nhận và điều tra theo
  skill bug trước khi sửa, không thay source giữa đợt test để che FAIL.
- Khi đủ bảy ca PASS, báo sẵn sàng nghiệm thu. Commit bàn giao theo yêu cầu/cho
  phép của người dùng; ghi commit và biên bản trước khi chuyển sang T02.

**Verify:** Đạt toàn bộ ma trận bên dưới; blocker chưa giải quyết giữ T01 Đang làm.

## 3. Ma trận nghiệm thu

| Ca | Cách kiểm chứng | Kết quả cần đạt |
|---|---|---|
| T01-K01 | VS mở EnvironmentProbeForm trong Designer | Không lỗi tải Designer |
| T01-K02 | Restore/build Debug + Release, chạy và đóng form | Build thành công; cửa sổ mở/đóng bình thường |
| T01-K03 | `dotnet --info`, `dotnet --version`, đọc project/global.json | SDK 10.0.401 và target net10.0-windows |
| T01-K04 | SSMS kết nối localhost/Windows Authentication, chạy script chỉ đọc | Đúng instance, có quyền tạo database kiểm thử |
| T01-K05 | `git rev-parse --show-toplevel` tại CoffeeManager | Gốc là CoffeeManager |
| T01-K06 | `git status --short`, `git check-ignore` đường dẫn đại diện | Không thu gom dự án khác; output/secret bị ignore |
| T01-K07 | Copy source sạch ra nơi khác rồi restore/build | Thành công, không cần đường dẫn máy tác giả hoặc output cũ |

## 4. Điểm dừng và trạng thái

Plan đã được duyệt; triển khai các bước trên. Nếu cần cài thành phần mới,
đổi SQL instance hoặc mở rộng phạm vi, cập nhật plan và chờ duyệt phần phát sinh.
T02 chỉ bắt đầu sau khi T01 được nghiệm thu; không triển khai song song các task.

Tiến độ thực tế: đã tạo repo, SDK pin, project thử, script chỉ đọc và tài liệu;
build/SDK/Git/source sạch đã kiểm chứng. CS8669 và lỗi trust certificate đã được
xử lý theo duyệt; build sạch và CLI xác nhận có quyền tạo DB test. `test-results.md`
ghi đầy đủ bảy ca PASS, gồm hai ảnh manual Designer/SSMS do người dùng cung cấp.
