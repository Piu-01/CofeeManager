# G07 — Báo cáo hiện tại sau sửa G07-B01

**Regression analytics10/11/2026 — fixture đã sửa theo duyệt:**56subcheckPASS trên cácbuild thực chạy,1FAILthời gianUI. [Biên bản regression](t28-bao-cao-doi-chieu/dashboard-analytics-regression-results.md). SQLmedian~0,33s nhưng WinFormsready4,9–5,4s;gọi riêngClearSnapshot/RenderkhôngSQLvẫn3–6s. **G07-DA02 đã được người dùng yêu cầu sửa và đã sửa phạm vi AdminDashboardControl (bỏ panel lồng thanh Top5, guard WrapLabels); đo lại harness Clear 814→242ms, Render-sau-Clear 628→348ms, hình học trùng bản đầy đủ. [Kết quả sửa](t28-bao-cao-doi-chieu/da02-fix-results.md); regression 10.000bill + kiểm native chưa chạy.** [Biên bản regression](t28-bao-cao-doi-chieu/da02-regression-results.md)** (10/11/2026): UI đạt582,6/645,2/661,5ms (<3s) trên10.000bill,perf12/12,rỗng3/3,ma trận4/2/1KPI–2/1chart tại125%;hìnhthứcvàDPI100%/150%do người dùng kiểm. G07-DA01đãđóng;logfixturelỗicũgiữlại. CácDBtestcủalượtđãdọnđúngmarker. Auditcuối có4fileứngdụngđổiđộclập(InvoicePrintPreviewForm,InvoiceDetailDialog,OrderPanel,Program),khôngsuybuildđãchạylàtoànsourcemớinhất. UIcầnfreeze/regressionlạisaulượtsửahiệunăng.G07/T28vẫnĐang làm;khôngthay325ID.

**10/11/2026 — đợt test analytics đã được duyệt và chạy:**16 kiểm tra PASS (2migration,11typed/native,3unit);phần bổ sung bị chặn ở setup do fixture của agent dùng mãDZ001/DX001 ngoài quy tắc hiện có. [Kết quả chi tiết](t28-bao-cao-doi-chieu/dashboard-analytics-test-results.md),[G07-DA01 và phạm vi sửa harness](t28-bao-cao-doi-chieu/bug-report.md#g07-da01--fixture-kiểm-thử-bổ-sung-dùng-mã-không-hợp-lệ). Chờ duyệt sửa fixture và regression/phần chưa chạy;chưa có bằng chứng lỗi dashboard trong subset đã chạy,không suy toànDA01–DA14PASS.3DBtest tạo/3dọn/0giữ;development chưa áp028. SourceUnchanged=true trong runner chính; audit sau pha bổ sung ghi MainForm.cs đã thay đổi ngoài lượt này,UI cần recheck theohash hiện tại. G07/T28 vẫnĐang làm,giữ325ID/lịch sử.

**Analytics10/11/2026 — đã duyệt plan và triển khai source:**4KPI/ngày-tháng,%cùngmốc,trend/donut,Top5/trạngtháihiệntại và nguồnchitiết. DùngDTOcũ/container.NET/chartnative;028chỉ2proceduređọc/GRANT,khôngthêmbảng/cột/entity/DTO/property cho dashboard. BuildDebug/Releaseđạt;[biên bảntriểnkhai](t28-bao-cao-doi-chieu/dashboard-analytics-implementation.md),[planđãduyệt](t28-bao-cao-doi-chieu/dashboard-analytics-plan.md). **Chờduyệt[đợttestDA01–DA14](t28-bao-cao-doi-chieu/dashboard-analytics-test-plan.md)**,chưaáp028development/chưađổiappđangmở. KhôngthaykếtquảnghiệmthuG07cũ.

**Thiết kế theo yêu cầu người dùng09/11/2026 đã được duyệt:** Tổng quan phân tích Hôm nay/Tháng này, KPI và% cùng kỳ, xu hướng doanh thu, donut kênh, món bán chạy và trạng thái vận hành. [Bản thiết kế](t28-bao-cao-doi-chieu/dashboard-analytics-proposal.md). Trạng thái hiện tại ở đoạn10/11 phía trên; kết quả UI02 bên dưới chỉ thuộc bố cục đã sửa trước yêu cầu phân tích này.

**UI02 đã triển khai sau người dùng duyệt “ok”,09/11/2026:** Tổng quan có3card,nhóm4nút đúngkíchthướcOrbit,vùngngày/trạngthái/cậpnhậtriêng và co3/2/1cột.12/12regressionSQLfixture/WinFormsthậtPASS tại125%,Debug/Release0warning0error;DefaultDebugDLLkhớpbảnđãkiểm,appđãmởtạiHomeproductionmới. [Biên bảnUI02](t28-bao-cao-doi-chieu/ui02-regression-results.md). Hìnhthứcuserkiểm;khôngthay24PASS/9BLOCKED hoặc nghiệmthuG07/G08. Các dòng “chờduyệtUI02” bên dưới là lịch sử trước triển khai.

**G07-UI02 —09/11/2026:** người dùng làm rõ vấn đề ở bố cục Tổng quan, đặc biệt nút bị cắt chữ và phần chỉ tiêu trình bày tối thiểu. Đã đối chiếu ảnh/source, đề nghị sửa chính AdminDashboardControl thành ba card chỉ tiêu và nhóm thao tác có kích thước đúng; [phạm vi/hướng sửa](t28-bao-cao-doi-chieu/bug-report.md) chờ duyệt một lần kèm regression. Nâng DB của G07-DEV01 đã xử lý lỗi dữ liệu; chưa sửa hoặc nghiệm thu UI02.

**Cập nhật09/11/2026 — G07-DEV01 đã xử lý theo duyệt:** development nâng021→024→027; cả hai chặng hash dữ liệu khớp, hai backup VERIFYONLY CHECKSUM PASS.16/16 kiểm đọc runtime và native Home ADMIN khớp SQL: doanh thu hôm nay0₫,1đơn mở,nợ5.500.000₫; lỗi thiếu procedure hết. [Biên bản](t28-bao-cao-doi-chieu/development-upgrade-results-20261109.md). App để mở tại Tổng quan. Không đổi24PASS/9BLOCKED hoặc chốt G07/G08 từ lượt bàn giao này; ghi chú chưa nâng/chờ duyệt bên dưới là lịch sử.

**Điều tra09/11/2026 — G07-DEV01:** người dùng báo Tổng quan lỗi trên F5. Đã xác nhận cấu hình F5 trỏ localhost/CoffeeManager_Development, schema hiện021, thiếu sp_ReportDashboard của026 và SQL2812. Chưa nâng development hoặc sửa source. [Báo cáo và phạm vi đề nghị nâng021→024→027](t28-bao-cao-doi-chieu/bug-report.md) chờ duyệt một lần; không thay lịch sử test trên DB riêng bên dưới.

31/10/2026 · **Đang làm:24PASS /0FAIL /9BLOCKED trên33ID gốc.** Source đã triển khai và G07-B01 đã sửa/regressionPASS; chưa nghiệm thu hoàn tất vì phần native còn thiếu bằng chứng. Requirements, cả bộ plan, đợt test và đợt sửa G07-B01 kèm regression đều đã được người dùng duyệt riêng.

## Kết quả sửa và kiểm chứng

Migration [027_asset_write_lock.sql](../../../database/migrations/027_asset_write_lock.sql) bổ sung khóa transaction chung theo MaTS trước khóa hàng/range trong AssetWrite và FinanceCreate khi gắn tài sản. Mã hoa/thường dùng cùng khóa; Create cấp mã trước khóa. Giữ quyền, rowversion, replay/payload/actor, transaction và xử lý đối chiếu; không thêm retry tự động.025/026 giữ nguyên checksum, không thêm index hoặc sửa C# sản phẩm.

[Biên bản regression G07-B01](t27-tai-san-khau-hao/bug-regression-results.md) và [báo cáo nguyên nhân/lịch sử](t27-tai-san-khau-hao/bug-report.md).

| Kiểm chứng | Kết quả |
|---|---|
| 139ca tích hợp cũ | 139/139 PASS, gồm10race xóa/chi từng gây deadlock và30race kho checkout/checkout/nhập/xuất |
| 34ca regression chuyên biệt | 34/34 PASS:30race cóbarrier xóa/chi-sửa/xóa-sửa/sửa, transaction0:0 và receipt/row/phiếu đúng; khácMaTS/khôngMaTS; timeout/case-normalization; replay/actor/payload; writecost |
| Unit Assets/Reports | 15/15 PASS |
| SQLfixture T27/T28/T29 | 3/3 thành công |
| Migration | Clean/demo001–027 và rerun027; số dư tiền/kho/điểm/nợ/lương qua migration không đổi |
| Build | ProbeDebug và solutionRelease0warning/0error; [Release](build-release-b01.log) |
| Source/tài nguyên | SourceUnchanged=true từnglượt; cả2lượt regression tạo2/dọn2/giữ0DB; auditcuối không cònDBtestG07 |

Lệnh: `tools/database/Test-G07.ps1 -RunAcceptance -Regression -ThroughVersion 27`.

Lượt cuối [regression_20261031_200422_dd2b716e/manifest.json](runs/regression_20261031_200422_dd2b716e/manifest.json), [139ca](runs/regression_20261031_200422_dd2b716e/extended-results.json), [34ca](runs/regression_20261031_200422_dd2b716e/asset-lock-regression.json), [TRX](runs/regression_20261031_200422_dd2b716e/g07-unit.trx), checksum tại clean/demo-migrations.jsonl của lượt. SQL Serverlocalhost17.0.1000.7Developer, SDK10.0.401, Windows. HEAD nền8a38f8691d37089feec149b71a8f5718d37167f3+workingtree; [source-final.csv](source-final.csv) ghi sourceSHA256 thực, không coi HEAD là toàn bộ source đã chạy.

| Task | PASS | FAIL | BLOCKED | Biên bản |
|---|---:|---:|---:|---|
| T27 | 5 | 0 | 4 | [test-results](t27-tai-san-khau-hao/test-results.md) |
| T28 | 9 | 0 | 4 | [test-results](t28-bao-cao-doi-chieu/test-results.md) |
| T29 | 10 | 0 | 1 | [test-results](t29-hieu-nang-sql/test-results.md) |
| G07 | 24 | 0 | 9 | 33ID, không đồng nhất với173subcase |

Các đối chiếu tính tay đã chạy:2dòng×2TT không nhân tiền; đổi giá/công thức/KM giữ snapshot;54.321−5.432+5.000=53.889/phí2.444; cutoffnợ100→70; thu+73/chi+81 khôngdoublecount; tồn200+1−2−1=198; giờ8/chốt280.000/chi280.000 tách ngày; ca quađêm8h và1ca thiếu giờra. Fixturelịch sử qua baseline trước migration:bill25.000 lập01/04/2025, trả02/04 23:59:59 chỉ hiện ngàytrả.

## Đầu ra triển khai và hiệu năng

T27:025/assets CRUD, khấu hao tham khảo, liên kết chi, pending/receipt và quyền; T28:026/chín reporttyped SQL/DAL/BLL/WinForms, chứngtừnguồn/dashboardADMIN thật; T29:dataset10.000bill,33scenario và230actualplans/IO/TIME, thử rồi loại2indexcandidate.027 hiện tại sửa khóa G07-B01, không phải migration index.

[Performance-report](t29-hieu-nang-sql/performance-report.md) giữ số đo lịch sử schema026 trên [dataset10.000bill](runs/performance_20261031_103410_dbbb420d/manifest.json):8.000paid/1.000open/1.000cancel,24.000dòngpaid/14.000TT;3warm+20reads/scenario,2warm+10checkout20dòng/phase. Ngàytrảthực31/10, không phân bố giả90ngày. Medianserviceđọc<0,5s trên máy/dataset đó; không phải UIend-to-end. Candidatepaidindex córead+26,7%; receiptindex cải thiệnnhỏ/write+28,4%; cảhai loại. Parity9report tổng/trangđầu cùngsnapshot khớp, không tuyên bố mọi trang/fullregression hoặc mọi máy.

Sau027 đo3warm+30samples/loại trên demoDB: CreatequaService+Get median29,74ms/p9536,91; Updateraw4,69/5,49; Expense raw5,23/6,15. [Rawsamples](t27-tai-san-khau-hao/write-cost-b01.json). Không có baseline cùngsnapshot để suy phần trăm overhead; không so các timer khác phạmvi. Không chạy lại fullbenchmark10.000bill vì không đổi index/report/checkout; số đo cũ vẫn là lịch sử026.

## Phần còn BLOCKED

9ID giữ nguyên:T27-K01/K04/K07/K09; T28-K06/K11/K12/K13; T29-K10. Binding/handlerXemreport, nativeowner-modal/cha bị khóa/khôngtaskbar/khôngAcceptButton và bounds1280×720,1366×768 ở DeviceDpi120(125%) đã chạy qua probe; không thay thao tác native đầu cuối.

[Transcriptnative](native-transcript.json) ghi công cụ click báo geometryunavailable, saurecovery editor không ởcache. Không xác nhậnfocus/input đúng; không quy lỗi công cụ thành bugsảnphẩm hoặc tickPASS. Cần phiên điều khiển dùngđược hoặc người dùng thực hiện các bước chức năng theo plan:click/Tab/clicklần2,dirty/pending/restart/I/O/timeout,Enter/Esc,querymuộn/đổiphiên/drilldown và timingUIresponsive. RegressionSQL không giải quyết blocker điều khiển này.

Hình thức **SKIPPED theo phân công người dùng**; DPI100/150% **NOTRUN**, không đổi WindowsScale, không chụp/soảnh hoặc dùngDesigner làm nghiệmthu. G07 chưa Hoàn tất.

## Lịch sử và tài nguyên

[report-acceptance-026](report-acceptance-026.md) giữ đợt gốc23PASS/1FAIL/9BLOCKED và129PASS/10FAIL của G07-B01;10deadlockgraph/lượt190439 giữnguyên. Các task có test-results-acceptance-026.md, nguồn tại [source-acceptance-026.csv](source-acceptance-026.csv). Không dùng lượt cũ ítca thay kết quả hiện tại.

Lượt regression200218 có139PASS nhưng34FAIL phép đoXACT_STATE trực tiếp; [đối chứng connection mới](t27-tai-san-khau-hao/transaction-measurement.md) chỉ ra SELECTtrực tiếp0:1, capturebiến trướcSELECT0:0. Sửa riêng harness giữa lượt, expectedvẫn0:0 và027không đổi;200422 chạy lại toànbộ PASS. Giữ logFAIL nguyên bản; không chấp nhận1205 hoặc bỏ kiểmreceipt/transaction.

[resource-summary.csv](resource-summary.csv), [resource-totals.json](resource-totals.json), [resource-audit.json](resource-audit.json):cleanup exactDBname+marker, không DROPwildcard; không có DBtestG07 giữ lại. Trong điều tra cũ, reviewer từ chối xuất toànringbuffer; đã dùng truyvấn SQLlọc chỉ graph mọi process thuộc đúngDBtest, không lưuwhole ringbuffer.

[development-guide](development-guide.md) và runner nângcấp được cậpnhật target027, **chưa chạy nângDBdevelopment**, không commit/push. G06 và lịch sử325ID/T01–T32 không đổi. G07-B01 đã đóng trongphạmvi regression; côngviệc còn lại là9ID native chưa đủbằngchứng, không có đợtbug mới đang chờduyệt.
