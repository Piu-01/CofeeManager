# T28 — Kết quả đợt test G07

**Bổ sungG07-UI02,09/11/2026:** sửa giao diện Tổng quan đã được duyệt và regression12/12PASS trên SQLtest/WinForms125%;nativeproduction đã xác nhận ba vùng chỉ tiêu/nhóm thao tác và tải thành công. [Ca/expected/actual/evidence](ui02-regression-results.md). Hình thức SKIPPED,DPI100/150%NOT RUN;giữ cácIDgốc và cácgiớihạnnative khác bên dưới.

## Bổ sung G07-DEV01 — development09/11/2026

Người dùng duyệt backup/nâng development021→024→027 và kiểm đọc. Biên bản đầy đủ: [development-upgrade-results-20261109.md](development-upgrade-results-20261109.md). Đây là bổ sung kiểm lỗi môi trường, không thay kết quả các ID gốc31/10.

| Ca bổ sung | Expected | Actual | Kết quả |
|---|---|---|---|
|DEV01|Đúng marker/checksum và schema từng chặng|Hai script Succeeded,27 migration,hash dữ liệu mỗi chặng khớp|PASS|
|DEV02|Backup trước từng chặng đọc được bằng VERIFYONLY CHECKSUM|2backup PASS và giữ nguyên file|PASS|
|DEV03|Runtime thật khôngsysadmin/db_owner; đọc Tổng quan đúng SQL|AppRole1/sysadmin0/db_owner0; ngày09/11 doanh thu0,đơn mở1,nợ5.500.000 khớp SELECT độc lập|PASS|
|DEV04|Các contract đọc được sau nâng|Options52;9loạireport,EmployeeContext/List,AssetContext/List thành công; tổng16recordPASS gồm runtime/dashboard|PASS|
|DEV05|NativeADMIN mở Home và hiện số liệu không lỗi2812|Đăng nhập nút ADMIN bằng tọa độ screenshot mới; đọc AX trên MainForm production có đúng số liệu/thời điểm đọc,không lỗi cũ|PASS theo phạm vi Home|
|DEV06|Không ghi fixture/resetaccount/đổi dữ liệu từ lượt kiểm|Hash dữ liệu trước/sau kiểm và source khớp;khôngresetaccount/fixture;app để mở|PASS|
|DEV07|Hình thức/DPI khác|Không nghiệm thu hình thức;100/150% chưa chạy|SKIPPED/NOT RUN theo phân công|

Evidence ở `../runs/development-handoff_20261109_214910_f23efd01/`:manifest.json,read-check-results.json,native-dashboard.json,backup-verification.json,read-data-audit.json,source-audit.json. Chưa kiểm native các màn ngoài Home hoặc nghiệm thu toàn nhóm từ lượt này.

## Lịch sử đợt nghiệm thu31/10/2026

31/10/2026. Requirements, bộ plan và **đợt test đã được người dùng duyệt riêng**. G07 vẫn Đang làm; không tick Hoàn tất từ build hoặc số subcase.

SQL Server localhost17.0.1000.7 Developer; .NET SDK10.0.401, Windows, WinForms DeviceDpi120 (125%). HEAD nền8a38f8691d37089feec149b71a8f5718d37167f3 + working tree; source thực bằng SHA256 trong [source-final.csv](../source-final.csv), schema001–027/checksum trong [migration log](../runs/regression_20261031_200422_dd2b716e/demo-migrations.jsonl). DBtest clean/demo có marker, runtimeG07Runtime/CafeManagerApp; ADMIN/CASH/STORE, không ghi DBdevelopment.

Lệnh: tools/database/Test-G07.ps1 -RunAcceptance -Regression -ThroughVersion27. Lượt cuối [regression_20261031_200422_dd2b716e](../runs/regression_20261031_200422_dd2b716e/manifest.json):15/15unitPASS,139/139subcasePASS và34/34ca regressionG07-B01 PASS,3SQLfixturePASS; SourceUnchanged=true, tạo2/dọn2/còn0. [Expected/actual từng subcase](../runs/regression_20261031_200422_dd2b716e/extended-results.json), [TRX](../runs/regression_20261031_200422_dd2b716e/g07-unit.trx), [typed/UI](../runs/regression_20261031_200422_dd2b716e/typed-smoke.json). Dữ liệu đầu vào/fixture/harness trong source snapshot. Source cố định trong từng lượt; lịch sử FAIL vẫn giữ, không sửa sản phẩm/migration trong lúc test.

| ID | Expected theo kế hoạch gốc | Actual / giới hạn | Kết quả |
|---|---|---|---|
| T28-K01 | [I] Doanh thu chỉ đơn đã trả theo NgayThanhToan, không NgayLap nếu hai ngày khác; loại hủy/mở. | Fixture lịch sử hợp lệ trước migration: HD20259999 lập01/04/2025, trả02/04 23:59:59,25.000 chỉ có trong nguồn ngày trả; mở/hủy không đổi doanh thu. | PASS |
| T28-K02 | [I] Tiền món sau giảm/ship/phải trả/phí đối tác tách rõ và cộng đúng. | Món54.321−giảm5.432+ship5.000=53.889; phí đối tác2.444 tách riêng. Report delta đúng từng tổng. | PASS |
| T28-K03 | [I] Một bill 2 món×2 TT không nhân doanh thu bốn lần; aggregate độc lập trước JOIN. | 2dòng SP900×2TT: số lượng2, tiền món24.690, tổngTT24.690; không nhân dòng. | PASS |
| T28-K04 | [I] Món theo giá snapshot; đổi menu/công thức/ưu đãi không đổi báo cáo đơn cũ. | Đổi giá/công thức giữ tiền24.690 và snapshot tiêuhao2; đổi KM10→20% sau trả giữ tổng/giảm/ship/phí đã chốt. | PASS |
| T28-K05 | [I] Filter [từ ngày, ngày sau đến ngày) nhận giờ cuối ngày; không CAST cột ngày để lọc nếu tránh được. | Ngày cuối kỳ23:59:59 được nhận, ngày lập/ngày kế loại đúng;9loại nhận0001→9999 và trang ngoài range giữ metadata. Unit chặn From>To. | PASS |
| T28-K06 | [I] Không dữ liệu trả tổng 0/list rỗng; lỗi DB hiển thị lỗi, không biến thành 0 giả. | Typed rỗng trả0; trangrỗng giữcount; DENY EXECUTE gây229, không trở thànhreport0. Native lỗi/giữinput/retry chưa kiểm được. | BLOCKED |
| T28-K07 | [I] Công nợ ngày X bỏ khoản nợ sau X và lần trả sau X, khác view hiện tại đúng kỳ vọng. | NCC99: nợ100 ngày hôm trước, trả30hôm nay; report cutoff100→70, độc lập số dư hiện tại. | PASS |
| T28-K08 | [I] Thu tiền bán và Thu khác mỗi nguồn tính một lần; nợ/chi lương/nhập trả ngay không double count. | Thu khác+73; chi khác31+PNtrả30+trảnợ20 tăngchi81. Chi lương tăng280.000 một lần; thu bán từ TT không nhân dòng. | PASS |
| T28-K09 | [I] Đối chiếu tồn mở sổ + nhập − xuất − tiêu hao + chênh lệch kiểm kê = tồn hiện tại trong kỳ có đầy đủ nguồn. | NVL99 mở200+nhập1−bán2−KK1=198 khớpCurrent198 vàComplete. Kỳ trước cutover trảIncomplete, ClosingNULL;30race kho kiểm tồn thực. | PASS |
| T28-K10 | [I] Giờ/chốt lương/thực chi lương tách đúng, khấu hao không cộng như chi tiền. | 8giờ hôm qua; chốt280.000 hôm nay chưa chi0, sau chi280.000; chi tiền tăng1lần. Worker riêng ca22→06=8h, ca chưa ra có1incomplete, không tự chốt/chi. | PASS |
| T28-K11 | [M] Filter đổi/refresh/grid không hiển thị kết quả cũ của query chậm trả sau query mới. | Binding WinForms thật125% qua handler Xem báo cáo; chưa điều khiển ổn định đổifilter/queryA chậm→B, page/refresh/drilldown và logout. | BLOCKED |
| T28-K12 | [U/M] Role không được xem báo cáo tài chính bị chặn ở BLL, không chỉ ẩn menu. | 18BLLreport+22SQLendpoint, BLLoptions/dashboard/source chặnCASH/STORE51605; revoke51505. Native menu và revoke lúc đang tải chưa xác nhận. | BLOCKED |
| T28-K13 | [M] Nhãn ghi rõ trước giảm toàn bill, dòng tiền quản trị/đối tác chưa đối soát, không lợi nhuận chính xác. | Đọc được nhãn SQL/native accessibility: trướcgiảm toànbill, dòngtiềnquảntrị/khấuhaothamkhảo. Chưa kiểm đủ thao tác mở nguồn/nội dung các dialog. | BLOCKED |

ID kết hợp native chỉ ghiBLOCKED nếu chưa đủ bằng chứng, dù phầnSQL/BLL PASS. [Transcript native](../native-transcript.json) ghi lỗi click geometry và editor không có trong cache sau recovery; không quy lỗi công cụ thành bug sản phẩm, không coi type/Enter đã tới đúngfocus. Binding/reporthandler/owner-modal và bounds1280×720,1366×768 ở125% đã chạy trong probe; không thay keyboard/click/dirty/unknown/session/drilldown. Phần native cần phiên điều khiển dùng được hoặc người dùng thực hiện theo plan.

Hình thức **SKIPPED theo phân công người dùng**;100/150% **NOT RUN**, không đổi WindowsScale hoặc suyPASS. Các nhánh native timeout/pending/restart/I/O/querymuộn và layoutchiađôi chưa chạy; không tuyên bốUIresponsive từ service timings. Các ca legacy/actor sai và mọi trang ngoài tập đã ghi trong artifact không được suyPASS.

Lịch sử runner giữ nguyên:185205 chọn nhầm1testDALT05 (15G07PASS);185300 lỗi truyềnargs;185610 fixtureKM sai;190114 fixtureUPDATEđơnđãtrả bị51022 đúng thiết kế. Harness/fixture sửa giữa các lượt, không đổi sản phẩm/expected để né lỗi. Lượt190308 có128PASS;190439 thêmrace phát hiện10FAIL. [Báo cáo chung](../report.md), [bugG07-B01 đã regression](../t27-tai-san-khau-hao/bug-report.md), [hiệu năng](../t29-hieu-nang-sql/performance-report.md). Không dùng lượttriểnkhai trước duyệt thay nghiệm thu.
Regression sau duyệt G07-B01: [34ca chi tiết](../runs/regression_20261031_200422_dd2b716e/asset-lock-regression.json), [biên bản bug](../t27-tai-san-khau-hao/bug-regression-results.md). Toàn nhóm hiện24PASS/0FAIL/9BLOCKED; không suy nativePASS. Lịch sử đợt gốc ở test-results-acceptance-026.md. Không commit/nâng development.

## Bằng chứng native bổ sung trong G08 —02/11/2026

Xem [SalesDay rỗng/lỗi DB/thử lại](g08-native-supplement-20261102.md). Giữ nguyên biên bản và tổng trạng thái G07 bên trên; bổ sung này không tự nghiệm thu toàn bộ report/9ID native còn thiếu.

