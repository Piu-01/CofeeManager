# Regression Tổng quan —10/11/2026

**56 kiểm tra PASS;1 FAIL về thời gian UI.** Lỗi fixture G07-DA01 đã được người dùng duyệt sửa/regression và đã đóng. G07-DA02 là lỗi mới,đang chờ duyệt debug/fix;không sửa source ứng dụng trong lượt. Chưa backup/áp028development hoặc mở bản mới để bàn giao.

## Phiên bản và dữ liệu

- Run chính bổ sung: `dashboard-regression_20261110_095018_6af29f27`;fixture mớiNVL99/SP999 có kiểm chưa tồn tại. Log/hash/lượt lỗi trước giữ nguyên.
- Windows/.NETSDK10.0.401/SQL Server17.0.1000.7 Enterprise Developer;localhost,DB test riêng,desktopDPI120/125%.
- Source freeze326file tại [source-before](../runs/dashboard-regression_20261110_095018_6af29f27/source-before.csv). Models/baseline/migration001–028 không sửa để chạy regression. Bản MainForm hiện tại đã được build/kiểm lại,không dùng mặc định kết quả MainForm trước khi nó được cập nhật độc lập.
- Fixture/harness mới được compile trước thực thi từng phiên;Native/API/Guard/Timing/Layoutcófilefreeze. Lệnh ghi freeze củaExtendedbị bộduyệttựđộng chặn vìusagelimit trước thực thi;người dùngduyệtlại vàlệnhchạysauđóthànhcông. Khôngtạobằngchứngfreezequákhứ:đãđốichiếuchecksumdocumenttrongPortablePDBvớisourceharnesshiệntại,khớpSHA25629A9C8…,[audit](../runs/dashboard-regression_20261110_095018_6af29f27/compiled-harness-source-audit.json). Không sửa test đã chạy để biếnFAIL thànhPASS.

## Tổng hợp ca đã chạy

| Bộ | PASS | FAIL | Bằng chứng |
|---|---:|---:|---|
|Migration clean/demo028/rerun|2|0|[Manifest](../runs/dashboard_20261110_095320_5ba96198/manifest.json),schema/index/dữliệukhôngđổi|
|TypedSQL/BLL/DAL vànative cơ bản|11|0|[Typed results](../runs/dashboard_20261110_095320_5ba96198/typed-results.json)|
|Bổ sung fixture đãsửa|9|0|[Results](../runs/dashboard-regression_20261110_095018_6af29f27/results.json)|
|Native bổ sung|4|0|[Native results](../runs/dashboard-regression_20261110_095018_6af29f27/native-results.json)|
|Quyền/nguồn/reportcũ/startup|5|0|[API results](../runs/dashboard-regression_20261110_095018_6af29f27/api-results.json)|
|Schema027/điểm/23:59:59|3|0|[Guard](../runs/dashboard-regression_20261110_095018_6af29f27/Guard/guard-results.json),[Edge](../runs/dashboard-regression_20261110_095018_6af29f27/Guard/edge-results.json)|
|xUnitReports/AuthRules/DeliveryTransition|22|0|[TRX](../runs/dashboard-regression_20261110_095018_6af29f27/dashboard-regression.trx)|
|TimingUI so mục tiêuT29-K10<3s|0|1|[Timing](../runs/dashboard-regression_20261110_095018_6af29f27/timing-results.json),[Layout diagnostic](../runs/dashboard-regression_20261110_095018_6af29f27/layout-results.json)|

Đây là sốsubcheck thực chạy,không là56IDgốc hoặc kết luận325ID/T01–T32PASS. DA13 phần dataset/SQLparity đo đạt;phầnUI FAIL nên không đánh dấu toànDA13PASS. Native làchức năng/bounds,khôngnghiệmthu hìnhthức.

## Kết quả có giá trị đối chiếu

- Tiền/ngày-tháng/%/Top5/paging/quyền bộgốc vẫnPASS.62bill/127.500₫/127ly,MTD137.500₫,kỳtrước20.000₫;khôngnhân2món×2PTTT,giásnapshotgiữnguyên.
- DA05:thựcchi tăng1.373₫=manual73+nhậptrảngay100+trảnợ200+chilương1.000;chốtlương/lưuhồsơtàisảnkhôngtạochi thêm. Nguồn khôngtrùng.
- DA10:20reads đồngthờicheckout;doanhthu+1.000₫/1bill,kho−0,0001,1payment/receipt.20reads đồngthờichi:chỉ+73₫/1receipt. Khôngdeadlock/timeoutngoàica cưỡngbứcđãghi. Connectionmới@@TRANCOUNT/XACT_STATE=0:0;điểmkháchhashkhớpDBfixtuređốichứng.
- DA07:giao chưa trả/hủy không tínhpending;paidwaiting+1;đãgiao vềbaseline. HaiNVL0đơnvịg/ml cótrongsource,buttonchi tiết bịkhóa.
- DA08:nativeInvoice/Finance/Debt mởđượcdialogconđúngowner/MainForm;đónggiữmode;sourcebucket/kênh/mónđúngphạmviđãđọc.
- DA09:đổirole/khóatàikhoản/revokethìanalyticsvànguồnđềubịchặn;CASH/STOREbịchặnBLL+SQL,khônglộSELECTbảnggốc.
- DA11:lỗikếtnốihiệnlỗi/—;schema027báorõcần028,error53502;SQLtimeoutthật−2rồireadthànhcông;queryAcũbịgiữ/đổiB/chỉBmonthlyđượcrender. Hide/show/cancel PASS.
- DA12:1366×768,1280×720,pane700và1800đođược4/2/1KPI,2/1chart;AccessibleNamegiữtiền18chữsố. Charttooltipthờigianđúng,nativeownedmodal. KhôngđổiWindowsScale/chụpsoảnh.
- DA14:9legacyreport/sourcecallable;AuthServiceđăngnhậpCASHvàoSalesControl,STOREvàoInventoryControl;logoutrevokeđúng.
- Edge23:59:59bill25.000₫đượctínhngàytrả,vàloạikhiđúngUntil.

## Hiệu năng —FAIL UI

10.000bill thật qua contract:7.998paid/1.001open/1.001hủy. Đây là dataset mới,không dùng sốđoT29 cũ;phân bố1productbenchmark+baseline,billkhôngđượcbịa ngày trả.2warm+10samples/mode quaBLL:

| Mode | Medianservice | P90service |
|---|---:|---:|
|Hômnay|343,1ms|365,7ms|
|Thángnày|330,2ms|366,4ms|

SQLIO/TIME mẫu riêng332,1ms/277,3ms,53/51rowkếtquả;khôngtải10.000billvềform. DBđãwarmtừgenerator/probe;**khôngcóphépđocoldDBcache**,khôngDBCCtoànserver. Kếtquảkhôngcamkếtmáykhác.

UIready/binding+mộtUIyield:startup4.939ms,monthly5.389ms,today5.370msở1366×768/125%,vượt<3s. Khôngđohoặcnghiệmthuphysicalpaintlatency. Gọi riêngRender/ClearSnapshotkhôngSQLvẫn3.155/4.098/6.002msvà161–234layoutevents;77control. [G07-DA02](bug-report.md#g07-da02--cập-nhật-tổng-quan-làm-ui-chậm-36-giây) đãghirootareaUIvàphạmviđềnghịdebug/batchlayout,chưasửa.

## Trạng thái và giới hạn

- Hìnhthức**SKIPPED theo phâncôngngườidùng**;DPI100/150%**NOT RUN**. Không suy PASSappearance từbounds/HTML/Designer.
- Chưađo coldDBcache hoặc physicalpaintlatency;không đổicache/DPI đểlàmđẹp sốđo.
- Developmentgiữ027;handoffscript/backupchỉchuẩnbị,**chưa chạy** vìFAILUI. Khôngresetcredential/seeddevelopment/commit.
- Testphầnchức nănghoànthànhtheocáccađãghitrên;T28/G07vẫnĐang làm,chờduyệtfixG07-DA02vàregressionđúngảnhhưởng.
- **Auditcuốisource:**322/326filekhớp;4fileđượcchỉnhngoàilượtthựchiệnnày:InvoicePrintPreviewForm.cs,InvoiceDetailDialog.cs,OrderPanel.cs,Program.cs. Agentkhôngsửachúngtrongđợtfixture/regression.[source-after](../runs/dashboard-regression_20261110_095018_6af29f27/source-after.csv)lưuhash;khôngbáoSourceUnchangedtoànworkspacehoặcPASSmặcđịnhchoLatestWorkingTree. SourceAdminDashboard/Models/BLL/DAL/migration028khôngđổi;UIcầnfreeze/regressionlạitrênbảnsourcethiệntạisausửahiệunăng.
- **Tàinguyên:**runnerchính095320tạo2/dọn2;regressionbổsungtạoDB095040vàGuard100556,đãdọncảhaisauđốichiếumarker,[cleanup](../runs/dashboard-regression_20261110_095018_6af29f27/cleanup.json). Tổng4DBtesttạo/4dọn/0giữtrongđợtnày. Handoffupgrade.ps1chỉchuẩnbị,chưathựcthi;khôngcóbackupnâng028đãtạo.
