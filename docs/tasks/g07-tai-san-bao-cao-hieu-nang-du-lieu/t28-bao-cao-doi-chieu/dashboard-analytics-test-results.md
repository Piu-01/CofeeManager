# Kiểm thử Tổng quan ngày/tháng —10/11/2026

**Cập nhật sau người dùng duyệt sửa fixture:**xem[kết quả regression hiện tại](dashboard-analytics-regression-results.md):G07-DA01đãđóng,56subcheckPASS/1FAILhiệu năngUI,G07-DA02chờduyệtsửa. Nội dung bên dưới giữ lịch sử lượt093601,khônglàtrạngthái chờsửafixturehiệntại.

**Đang làm:16 kiểm tra PASS;1 lỗi setup fixture bổ sung; chưa đủ bằng chứng nghiệm thu DA01–DA14.** Người dùng đã duyệt đợt test bằng “duyệt”. Không chỉnh source ứng dụng/migration/test đã freeze trong lượt; chưa cập nhật development lên028.

## Môi trường và lệnh

- Windows,SQL Server localhost17.x,.NET SDK10.0.401;desktop DeviceDpi120 (125%). Các phiên SQL/GUI chạy thật với DB test riêng, không SQLite.
- Lệnh chính: `tools/database/Test-DashboardAnalytics.ps1 -RunApprovedTests`.
- Unit: `dotnet test tests/CafeManager.Tests/CafeManager.Tests.csproj -c Debug --no-build --no-restore -p:BaseOutputPath=D:/Workspace/Nam_3/HK1/CoffeeManager/artifacts/dashboard-analytics/bin/ --filter FullyQualifiedName~CafeManager.Tests.Reports. --logger trx`.
- [Manifest chính](../runs/dashboard_20261110_093601_0d29d43b/manifest.json),[source freeze](../runs/dashboard_20261110_093601_0d29d43b/source-before.csv),[typed results](../runs/dashboard_20261110_093601_0d29d43b/typed-results.json),[TRX](../runs/dashboard_20261110_093601_0d29d43b/dashboard-reports.trx).
- [Manifest bổ sung](../runs/dashboard_20261110_093601_0d29d43b/extended/manifest.json),[harness freeze](../runs/dashboard_20261110_093601_0d29d43b/extended/harness-freeze.csv),[log lỗi setup](../runs/dashboard_20261110_093601_0d29d43b/extended/probe.log). Harness này được chuẩn bị riêng cho các ca bổ sung; compile preflight thiếu `using Microsoft.Data.SqlClient` được sửa trước freeze/thực thi, log preflight giữ lại. Sau freeze không sửa harness.

## Các kiểm tra đã chạy

| Ca | Input/expected | Actual | Kết quả |
|---|---|---|---|
|DA01.clean|Baseline clean→001–027→028/rerun;giữ bảng/cột/computed/index và dữ liệu|028 Applied/AlreadyApplied;so trước/sau khớp|PASS|
|DA01.demo|Tương tự trên demo+fixture lịch sử|Schema/index/dữ liệu không đổi|PASS|
|DA02/03.day|15/01/2026 trước12:00:62bill,127.500₫,127ly,chi1.000₫;bill2món×2PTTT không nhân tiền|Đúng;trend=channels=127.500₫,Until bị loại|PASS|
|DA04.month|MTD137.500₫,tháng trước20.000₫;ngày trước10.000₫;%ngày1175|Khớp tất cả|PASS|
|DA04.calendar|31/03 thiếu mốc tháng02;29/02/2024→29/01;hai kỳ0 change0;avg thiếu mẫu null;trước0/sau>0 %null|Khớp bounds/null rules|PASS|
|DA06.top|61SKU;Top5X0002,X0001,X0003,X0004,X0005;giá snapshot1000 khác menu999999;gross toàn kỳ127.000₫|Thứ tự/tiền đúng|PASS|
|DA07.current|4counter bằng truy vấn trực tiếp;giống ở2mode|Orders/delivery/material/debt đều khớp|PASS|
|DA08.paging|62nguồn,50+12,khôngtrùng;channel1,product2,expense1|Đúng số lượng và mã nguồn riêng biệt|PASS|
|DA09.permissions|ADMIN đọc;CASH/STORE bị chặn BLL+SQL;runtime không SELECTHOADON|Hai role bị chặn;direct SELECT permission0|PASS|
|DA14.mapping|Map mới cả2mode;report cũ full15/01 gồm bill12:00=137.500₫;DashboardAsync cũ callable|Mỗi mode4page;report cũ137.500₫|PASS|
|DA10.transaction|Connection kiểm tra @@TRANCOUNT sau call=0|0|PASS|
|DA11.cancel|Token đã hủy không trả aggregate0 giả|OperationCanceledException|PASS|
|DA11/12.native|WinForms thật125%;đổi ngày/tháng/refresh/chartEnd/owner/hide-show;responsive technical|DeviceDpi120;KPI2/2/1,chart2/1/1 tại widths1600/1150/700;tooltip có mốc;ownedmodal đúng;reload lại sau hide|PASS subset|
|Unit.Reports|3test normalization/permission/report rules hiện có|3/3PASS,TRX lưu riêng|PASS (3ca)|
|Extended.setup|Chuẩn bị dữ liệu qua MaterialService/ProductService hợp lệ|FixtureDZ001 bị chặn bằng ArgumentException trước các ca bổ sung;DX001 cũng không đúng pattern qua đọc source|FAIL harness|

Tổng16PASS là2migration+11typed/native+3unit,không phải16nhóm hoặc toàn bộ14nhóm đãPASS. Native subset chưa chứng minh4cột/KPI tại màn đủrộng,1280×720/1366×768,nhãn/tiền18chữ số hoặc mọi phím/nguồn;không ghiPASS các phần này.

## Phần chưa chạy hoặc bị chặn

| Nhóm/phần còn lại | Trạng thái | Lý do |
|---|---|---|
|DA02/04 bổ sung23:59:59,00h/đầu tháng|NOT RUN|Ngoài subset đã chạy;phiên bổ sung dừng tại setup|
|DA05 chi liên kết/nhập trả ngay/trả nợ/chi lương,commit/khấuhao không tạochi|BLOCKED|G07-DA01:fixture chưa tạo được|
|DA07 giao unpaid/cancel/finished vàNVL0 nhiều đơnvị|BLOCKED|Fixture bổ sung chưa chạy|
|DA08 chi tiết Invoice/Finance/Debt,Material action và các khunggiờ chính xác|NOT RUN|Subset mới kiểm source paging/filter cơ bản vàowner dialog đơn mở|
|DA09 đổi role/khóa/logout và source sau revoke|NOT RUN|Subset mới kiểm2role vàendpoint đọc|
|DA10 hai session checkout/chi đồngthời,receipt/tồn/điểm vàXACT_STATE|BLOCKED|Fixture bổ sung dừng trước startgate;không suy từtransaction0 đơnluồng|
|DA11 timeout/connectionerror/schema027/latequery/dispose|BLOCKED/NOT RUN|Cancel/hide-show đã chạy;fault/latequery chưa cóbằngchứng|
|DA12 phần ngoài native subset|NOT RUN|Cần phiên bổ sung;hìnhthứcSKIPPED theo phâncông|
|DA13 10.000bill/IO/TIME/UI timing|BLOCKED|Fixture bổ sung dừng trước generator;không dùng sốđoT29 cũ thay028|
|DA14 toàn report/source cũ vàstartup/loginCASH/STORE|NOT RUN|Mới kiểm API report ngày/dashboard cũ+3unit|
|DPI100/150%|NOT RUN|Theo phạmvi AGENTS;khôngđổiScale|

## Bug, tài nguyên và bước tiếp theo

[G07-DA01](bug-report.md#g07-da01--fixture-kiểm-thử-bổ-sung-dùng-mã-không-hợp-lệ) là lỗi fixture do agent viết;chưa phát hiện lỗi dashboard trong các ca đã chạy. Chờ duyệt sửa **fixture/harness** và regression/các ca bị chặn;không đổi nghiệpvụ/Models/migration để test qua.

Runner chính SourceUnchanged=true,2DB tạo/2DB dọn/0giữ. DB bổ sung `CoffeeManager_Test_G07_20261110_094017_57c0f239_demo` đã dọn sau xác minh marker;harness hash sau khớp freeze. Tổng3DB tạo/3DB dọn/0giữ. Audit cuối source/DB ở cùng thưmục run.

**Giới hạn source hiện tại:** [audit cuối](../runs/dashboard_20261110_093601_0d29d43b/source-final-audit.csv) có325file khớp/1file đổi là `src/CafeManager.WinForms/Forms/MainForm.cs`,hash472978…→C7E765…. Agent không sửa MainForm trong đợt này. Runner chính đã kết thúc vớiSourceUnchanged=true;không áp kết quả UI trước đó mặc định choMainForm mới. Lượt kế tiếp cầnfreeze/check lại UI trênhash hiện tại;không hoànnguyên thay đổi độc lập hoặc tuyênbố toànsourcekhôngđổi sau pha bổ sung.

Development **chưa áp028**,không backup/deploy vì điều kiện testđạt chưa thỏa. G07/T28 giữĐang làm;325ID và lịch sử cũ khôngđổi.
