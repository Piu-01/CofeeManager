# T04 — Kết quả đợt G01_20261008_220313

## Mới nhất — Designer B07 đã regression PASS

Đã dùng DTE của Visual Studio18 đang mở đúng CafeManager.slnx: mở Designer,
lưu StartupForm.cs, đóng document, mở lại bằng vsViewKindDesigner; UIAutomation
xác nhận DesignerView và các control mới. Ảnh native không có màn lỗi Designer.
SaveResult=1, source sau save/reopen không đổi (count=0).
**T04-K05 PASS; T04 hiện7/7 PASS**. Các dòng BLOCKED bên dưới là lịch sử.
Bằng chứng: `../runs/G01_B07_20261008_232240/designer-reopen.json`,
`designer-reopened.png`, `designer-source-stability.json`.

## Sau sửa B07 — source UI mới

Designer/layout đã thay đổi theo duyệt B07. Build Debug/Release0 warning/0 error,
app thật mở/đóng và resize125% được; bằng chứng runs/G01_B07_20261008_232240
ở nhóm. K04 vẫn PASS. K05 regression save/close/reopen trên source mới
**BLOCKED**, chờ thao tác người dùng; xác nhận trước chỉ thuộc source trước B07.
T04 hiện6 PASS/1 BLOCKED. Chi tiết hình thức: ../t05-dal-nen-kiem-thu/b07-test-results.md.

## Bằng chứng người dùng — lượt manual 08/10/2026

**Cập nhật:** Người dùng xác nhận đã Ctrl+S, đóng tab và mở lại Designer thành
công. Kết hợp ảnh Designer trước đó: **T04-K05 PASS**, T04 đạt **7/7 PASS**.
Ảnh mới báo Class Library cannot be started là thao tác chạy startup project,
không phải lỗi Designer. WinForms.csproj đã có OutputType=WinExe; cần chọn
đúng CafeManager.WinForms làm startup project, chưa có bằng chứng lỗi source.
Các mô tả chờ xác nhận bên dưới là lịch sử trước xác nhận này.

Ảnh người dùng gửi trong hội thoại lúc khoảng 23:04 cho thấy StartupForm.cs
[Design] hiển thị Form/control bình thường trong Visual Studio, không có màn
lỗi Designer; VS báo màn hình chính DPI 125%. Output có build 3 succeeded/0 failed
(không thay kết quả build toàn solution đã chạy). Chưa có xác nhận Ctrl+S và
đóng/mở lại Designer nên chưa tick PASS đầy đủ T04-K05.

Đối chiếu manifest manual: 0 file source đã có thay đổi; chỉ có file local mới
`src/CafeManager.WinForms/CafeManager.WinForms.csproj.user`, giữ nguyên.
Bằng chứng kiểm tra: `../runs/G01_Manual_20261008_225305_cebd98ad/designer-source-check.json`.
Ảnh là bằng chứng hội thoại, không có file screenshot local. Tổng vẫn 6 PASS/1 BLOCKED.

## Regression G01_Regression_20261008_221528 sau duyệt B02

Debug/Release `dotnet build CafeManager.slnx -c <configuration> --no-restore`
và copy source sạch build Release đều PASS, **0 warning/0 error**. Logs ở
`../runs/G01_Regression_20261008_221528/build-debug.txt`, `build-release.txt`,
`clean-build.txt`. Copy sạch tại Temp/opencode/CoffeeManager_RegressionClean_20261008_221710.
Các ca kiến trúc/khởi động kế thừa; Designer vẫn BLOCKED. 6 PASS/1 BLOCKED.
Regression UI tải dữ liệu của B02 chưa chạy, ghi ở T05-K07, không coi build là UI PASS.

## Lịch sử lượt đầu (trước sửa B02)

**Môi trường:** Windows, .NET SDK 10.0.401 (dotnet-info.log), source chưa commit.
Manifest/hash và logs ở `../runs/G01_20261008_220313/`. Source không đổi trong lượt.

| Ca | Trạng thái | Lệnh/thao tác | Expected / Actual |
|---|---|---|---|
| T04-K01 | PASS | restore solution, build Debug/Release --no-restore | Build cả 5 project, 0 errors, không circular; 1 CS8602 mỗi cấu hình ghi G01-B02, chưa sửa |
| T04-K02 | PASS | Review 5 csproj/reference + build | Models không UI/SqlClient; BLL không WinForms; dependency graph đúng tầng |
| T04-K03 | PASS | Review Program/Form/Designer, grep SQL/SqlConnection | SQL/connection chỉ DAL; Program nối dependencies; Form chỉ service/control |
| T04-K04 | PASS | Chạy exe Debug, đọc UIAutomation tree và đóng cửa sổ | StartupForm thật mở với title CoffeeManager · G01; thiếu config báo lý do và nút tải disabled; CloseMainWindow làm process thoát |
| T04-K05 | BLOCKED | Cần Visual Studio Designer thật | Chưa có thao tác save/reopen Designer/bằng chứng của Form mới; T01 Designer không thay nghiệm thu Form T04 |
| T04-K06 | PASS | Copy source sạch không bin/obj/local config vào Temp/opencode rồi build Release | Restore/build 5 project thành công, 0 errors; không cần đường dẫn tuyệt đối; cùng warning G01-B02 |
| T04-K07 | PASS | Review file src/tests và graph | Chỉ DTO/error/request/repository/service nền đang dùng; không generic ORM/UnitOfWork/lớp mọi bảng |

## Bằng chứng

- `restore.log`, `build-debug.log`, `build-release.log`, `clean-build.log`.
- Copy sạch: `C:\Users\Lenovo\AppData\Local\Temp\opencode\CoffeeManager_Clean_20261008_220742`.
- `startup-ui.json`: text cấu hình thiếu, buttons disabled, headers grid, window handle.
- Lần đọc window handle ngay sau WaitForInputIdle trả 0 (harness lấy quá sớm);
  sau đó lấy đúng cửa sổ process 2928, đọc tree và đóng thành công. Không sửa
  app hoặc báo lỗi runtime chỉ vì lần lấy handle sớm thất bại.
- Resize/DPI 100/125/150%, Tab/focus/Designer chưa có bằng chứng đầy đủ; BLOCKED
  manual, cần phối hợp người dùng sau khi xử lý bug và có principal app riêng.
- G01-B02 thuộc code async T05, tham chiếu `../t05-dal-nen-kiem-thu/bug-report.md`.

Trạng thái task vẫn Đang làm vì K05 chưa PASS; build PASS không phải nghiệm thu nhóm.
