# T27 — Kết quả đợt test G07

31/10/2026. Requirements, bộ plan và **đợt test đã được người dùng duyệt riêng**. G07 vẫn Đang làm; không tick Hoàn tất từ build hoặc số subcase.

SQL Server localhost17.0.1000.7 Developer; .NET SDK10.0.401, Windows, WinForms DeviceDpi120 (125%). HEAD nền8a38f8691d37089feec149b71a8f5718d37167f3 + working tree; source thực bằng SHA256 trong [source-final.csv](../source-final.csv), schema001–027/checksum trong [migration log](../runs/regression_20261031_200422_dd2b716e/demo-migrations.jsonl). DBtest clean/demo có marker, runtimeG07Runtime/CafeManagerApp; ADMIN/CASH/STORE, không ghi DBdevelopment.

Lệnh: tools/database/Test-G07.ps1 -RunAcceptance -Regression -ThroughVersion27. Lượt cuối [regression_20261031_200422_dd2b716e](../runs/regression_20261031_200422_dd2b716e/manifest.json):15/15unitPASS,139/139subcasePASS và34/34ca regressionG07-B01 PASS,3SQLfixturePASS; SourceUnchanged=true, tạo2/dọn2/còn0. [Expected/actual từng subcase](../runs/regression_20261031_200422_dd2b716e/extended-results.json), [TRX](../runs/regression_20261031_200422_dd2b716e/g07-unit.trx), [typed/UI](../runs/regression_20261031_200422_dd2b716e/typed-smoke.json). Dữ liệu đầu vào/fixture/harness trong source snapshot. Source cố định trong từng lượt; lịch sử FAIL vẫn giữ, không sửa sản phẩm/migration trong lúc test.

| ID | Expected theo kế hoạch gốc | Actual / giới hạn | Kết quả |
|---|---|---|---|
| T27-K01 | [I/M] Tạo/sửa tài sản tên/ngày/giá trị/số tháng hợp lệ; reload và tìm theo mã đúng. | SQL create/update/reload, tên100ký tự và metadata trang rỗng PASS; native tìm/sửa/pager chưa xác nhận do công cụ geometry/focus. | BLOCKED |
| T27-K02 | [I] Giá trị 0/âm, tháng 0/âm, tên rỗng/quá dài hoặc phiếu chi MaTS không có bị chặn. | BLL/SQL chặn0, âm, lẻ, vượt18chữ số, tháng0/âm, tên trống/101; max18/intmax giữ chính xác. MaTS không có bị52602, không sinh phiếu/receipt. | PASS |
| T27-K03 | [I] Thêm hồ sơ không tự cộng một chi nếu quy trình không thực trả. | Count PHIEUTHUCHI trước/sau create/update bằng nhau; hồ sơ không sinh chi. | PASS |
| T27-K04 | [I/M] Chi sửa chữa gắn đúng tài sản, tiền trong PHIEUTHUCHI không bị cộng thêm nguyên giá vào chi. | Chi500.000 gắn tài sản85.000.000, tổng chi đúng500.000 và source DAL đúng. Native picker/mở hai chiều chưa xác nhận. | BLOCKED |
| T27-K05 | [U/I] Khấu hao bắt đầu tháng mua; trước mua 0; dừng đúng số tháng; không vượt nguyên giá. | 6ca SQL/calculator parity: trước mua, tháng mua, cuối/sau kỳ, tiền1/3 và max18/intmax đến9999; lũy kế không vượt giá. | PASS |
| T27-K06 | [U] Nguyên giá không chia hết số tháng: tháng cuối bù làm tròn, tổng bằng nguyên giá. | 15unit chung gồm quy tắc100/3 và1/3; SQL tháng cuối100/3=100, bù phần lẻ; không lặp intmax tháng. | PASS |
| T27-K07 | [I/M] Khấu hao là ước tính không tự tạo phiếu chi và không bị gọi là giá vốn bán. | Metadata SQL ghi khấu hao tham khảo, không ghi chi; nhãn và thao tác đổi tháng trên dialog native chưa xác nhận. | BLOCKED |
| T27-K08 | [I] Xóa tài sản đã có chi liên quan bị chặn, không mất phiếu chi. | Migration027:10race cũ và30race chuyên biệt xóa/chi-sửa/xóa-sửa/sửa PASS; receipt/row/phiếu và transaction0:0 khớp. G07-B01 đã sửa, FAIL cũ giữ trong test-results-acceptance-026.md. | PASS |
| T27-K09 | [U/M] Chỉ ADMIN sửa/xem tài chính; nguồn tham khảo ghi nhãn rõ. | CASH/STORE bị chặn BLL/SQL; replay/sai payload/fault rollback PASS. Native pending/restart/I/O, timeout trước/saucommit, focus/select-all, Esc/Enter và đổi phiên chưa xác nhận. | BLOCKED |

ID kết hợp native chỉ ghiBLOCKED nếu chưa đủ bằng chứng, dù phầnSQL/BLL PASS. [Transcript native](../native-transcript.json) ghi lỗi click geometry và editor không có trong cache sau recovery; không quy lỗi công cụ thành bug sản phẩm, không coi type/Enter đã tới đúngfocus. Binding/reporthandler/owner-modal và bounds1280×720,1366×768 ở125% đã chạy trong probe; không thay keyboard/click/dirty/unknown/session/drilldown. Phần native cần phiên điều khiển dùng được hoặc người dùng thực hiện theo plan.

Hình thức **SKIPPED theo phân công người dùng**;100/150% **NOT RUN**, không đổi WindowsScale hoặc suyPASS. Các nhánh native timeout/pending/restart/I/O/querymuộn và layoutchiađôi chưa chạy; không tuyên bốUIresponsive từ service timings. Các ca legacy/actor sai và mọi trang ngoài tập đã ghi trong artifact không được suyPASS.

Lịch sử runner giữ nguyên:185205 chọn nhầm1testDALT05 (15G07PASS);185300 lỗi truyềnargs;185610 fixtureKM sai;190114 fixtureUPDATEđơnđãtrả bị51022 đúng thiết kế. Harness/fixture sửa giữa các lượt, không đổi sản phẩm/expected để né lỗi. Lượt190308 có128PASS;190439 thêmrace phát hiện10FAIL. [Báo cáo chung](../report.md), [bugG07-B01 đã regression](../t27-tai-san-khau-hao/bug-report.md), [hiệu năng](../t29-hieu-nang-sql/performance-report.md). Không dùng lượttriểnkhai trước duyệt thay nghiệm thu.
Regression sau duyệt G07-B01: [34ca chi tiết](../runs/regression_20261031_200422_dd2b716e/asset-lock-regression.json), [biên bản bug](../t27-tai-san-khau-hao/bug-regression-results.md). Toàn nhóm hiện24PASS/0FAIL/9BLOCKED; không suy nativePASS. Lịch sử đợt gốc ở test-results-acceptance-026.md. Không commit/nâng development.
