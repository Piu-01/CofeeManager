# Plan kỹ thuật — Tổng quan phân tích ngày/tháng

Lập09/11/2026, rà soát10/11/2026. Người dùng đã **duyệt thiết kế và ràng buộc mô hình hiện có**. **Người dùng đã duyệt plan bằng “duyệt”; source đã triển khai, build Debug/Release đạt và chờ duyệt đợt test riêng.** Chưa áp migration028 vào development hoặc chạy test chức năng. Xem [biên bản triển khai](dashboard-analytics-implementation.md). Tham chiếu [thiết kế đã duyệt](dashboard-analytics-proposal.md), T28/mục12–13 kế hoạch, UX về vai trò/phiên/báo cáo, bộ UI tại docs/ui/. Không tạo requirements task con mới, không thay325ID hoặc lịch sử UI02/DEV01.

## Tóm tắt phạm vi để duyệt

- Bổ sung hai procedure chỉ đọc: tổng hợp dashboard và tra nguồn chi tiết. Không thêm bảng/cột thực thể, không sửa Models hoặc tạo DTO/property mới.
- Dùng các DTO báo cáo hiện có; chỉ số tính ra được truyền bằng container .NET tạm, không lưu ngược DB và không mượn field sai nghĩa.
- Màn ADMIN có Hôm nay/Tháng này, bốn KPI, % cùng mốc, biểu đồ đường/donut, Top5 và trạng thái hiện tại. CASH/STORE giữ quyền và điểm vào hiện có.
- Vẽ biểu đồ bằng control WinForms native, không thêm thư viện. Chi tiết giữ đúng kỳ/khung giờ/kênh/món bằng tham số hàm, không thêm thuộc tính filter.
- Triển khai theo SQL → DAL → BLL → UI; build Debug/Release. Đợt test riêng gồm14 nhóm ca bổ sung, giữ nguyên325ID và source cố định trong lượt kiểm.
- Sau test đạt: backup, xác minh và đồng bộ development với procedure mới trước bàn giao app. Không chèn dữ liệu để làm đẹp biểu đồ, không commit/push tự động.

## 1. Phạm vi và bất biến

- ADMIN: Hôm nay/Tháng này;4KPI,so sánh cùng mốc,xu hướng,donut kênh,Top5 và4trạng thái hiện tại như thiết kế đã chốt. CASH/STORE giữ điểm vào/quyền hiện có.
- **Không thêm/sửa bảng,cột,computed column,entity,DTO/model,property hoặc enum trong CafeManager.Models.** Không tạo bảng lưu số thống kê,snapshot,mục tiêu,ngưỡng tồn hoặc hạn trả nợ.
- Chỉ số mới là kết quả đọc/tổng hợp tạm từ trường đã truy vết. Không nhét% hoặc số đếm vào field DTO có ý nghĩa khác để né ràng buộc.
- Các control/helper UI mới phục vụ vẽ biểu đồ không phải entity/model nghiệp vụ. Không thêm framework UI,API server,WebView hoặc package ngoài.
- SQL tính tiền/tổng/đếm/bình quân/%; UI chỉ định dạng và đổi tọa độ/màu/chọn điểm để vẽ. Không đọc toàn bảng về C# để tổng hợp.
- Giữ nguyên các procedure/report cũ và checksum001–027. Không nâng nghiệm thu G07/G08 từ phần mở rộng này.

## 2. Cách truyền dữ liệu khi không tạo DTO/property mới

Tái sử dụng nguyên định nghĩa `ReportPage`, `ReportMetadata`, `ReportTotals`, `SalesReportRow`, `ProductReportRow`, `ReportSourcePage`, `ReportSource`. Không sửa thư mục Models.

Đầu ra repository/service dùng các container .NET có sẵn, kết hợp bằng ValueTuple, không định nghĩa class/record mới:

```csharp
Task<(
    IReadOnlyDictionary<string, ReportPage> Pages,
    IReadOnlyDictionary<string, decimal?> Indicators,
    (DateTime From, DateTime Until,
     DateTime? PreviousFrom, DateTime? PreviousUntil) Period
)> ReadDashboardAnalyticsAsync(SessionContext session, bool month, CancellationToken token);
```

Đây là chữ ký dự kiến BLL; DAL nhận AccountIdentity thay SessionContext. Dictionary chỉ chứa **kết quả aggregate trong bộ nhớ**, không là thuộc tính ghi vào DB/entity/model. Key cố định được khai báo ở phần code sử dụng, không lấy từ input SQL động.

| Container | Nội dung/ánh xạ đúng nghĩa |
|---|---|
|Pages[trend.current],Pages[trend.previous]|ReportPage/SalesReportRow:Code ISO mốc giờ/ngày,Name nhãn,Bills số bill,Gross/Discount/AfterDiscount/Ship/CustomerPaid/PartnerFee đúng nguồn. Previous có thể vắng khi không có mốc so sánh hợp lệ. Totals.Amount là doanh thu;Metadata ghi kỳ/ReadAt/DateBasis/Note.|
|Pages[channels]|SalesReportRow theo3LoaiDon hiện có;Totals.Amount cùng tổng doanh thu.|
|Pages[products]|ProductReportRow theo5món xếp Quantity DESC rồi MaSP;giữ BeforeBillDiscount đúng nghĩa,không dùng làm doanh thu sau giảm.|
|Indicators|Giá trị SQL suy ra:invoice-count.current/previous,average.current/previous,expense.current/previous,change.revenue/invoices/average/expense,difference.revenue/invoices/average/expense,quantity.total,share.channel.<LoaiDon>,orders.open,delivery.pending,material.zero,debt.current. Giá trị không xác định lànull,không giấu trongstringJSON.|
|Period|Mốc datetime2 được map DateTime;nullable previous thể hiện thiếu mốc tương ứng ở tháng ngắn. Metadata.ReadAt của các page cùng một lần đọc.|

Tỷ lệ và các trạng thái “Mới phát sinh/Không thay đổi/Chưa đủ cơ sở” dùng giá trị SQL đã trả và nullability; C# không tính lại số%/số tiền. Trong plan này chưa được phép thêm model nếu chữ ký trên gặp trở ngại; phải báo phần thiếu trước khi đổi hướng.

## 3. SQL — hai procedure đọc, không thay cấu trúc thực thể

File dự kiến:`database/migrations/028_dashboard_analytics_read.sql`. Khi rà soát10/11 version cao nhất vẫn027; kiểm lại version trống trước viết,không ghi đè migration khác. Migration chỉ CREATE procedure/GRANTEXECUTE và ghi version qua runner đã có; không tạo/sửa bảng thường,cột,view,index hoặc dữ liệu nghiệp vụ. Bảng tạm/bucket trong lúc thực thi là trung gian truy vấn,tự giải phóng,không thực thể lưu trữ; không dùng chúng làm bảng thống kê tồn tại lâu dài.

### 3.1. sp_DashboardAnalyticsRead

Tham số dự kiến:`@Actor int,@Stamp binary(8),@Month bit,@At datetime2(7)=NULL`. Actor/Stamp demandADMIN bằng sp_G03Demand; đọc theo cơ chế quyền đã có,không grantSELECT rộng cho runtime. Có thể dùng WITH EXECUTE AS OWNER như report hiện tại,kèm demandADMIN bắt buộc.

- SQL chụp ReadAt một lần. @At mặc định ReadAt; nếu truyền phải không ở tương lai. Đây là mốc đọc nội bộ để tái sử dụng khi mở nguồn,UI không có bộ nhập ngày/giờ tùy ý ở bản đầu; không thêm cờ bypass/test. Tham số cũng cho phép kiểm các mốc cuối tháng bằng cùng procedure thật.
- Tính From/Until và PreviousFrom/Until như đề xuất:ngày hômqua cùng giờ hoặc thángtrước cùng ngày/giờ. Thángtrước không có ngày tương ứng thìpreviousnull;không dùng DATEADD(MONTH) clamp âm thầm rồi ghi “cùng kỳ”.
- Predicate thời gian `[From,Until)`,không CAST cột NgayThanhToan/NgayLap trong WHERE. GROUP theo mốc sau khi lọc. Giữ độ chính xác timestamp và tiền decimal,không ghi computedThanhTien.
- Vật hóa tập header đã trả trong kỳ hiện tại/kỳ trước một lần. KPI,trend,kênh lấy từ cùng tập này. Tổng dòng món và phiếu chi tổng hợp độc lập,không JOIN gây nhân tiền2món×2PTTT.
- Hômnay:bucketgiờ đã qua;thángnày:bucketngày đã qua. Bucket hiện tại cắt đúng Until và kỳ trước cùng thời lượng;bucket tương lai không sinh điểm0. Tạo dãy bucket bằng CTE/VALUES trong truy vấn,không thêm bảng lịch.
- KPI doanh thu=SUMThanhTien;hóa đơn=COUNT header;bình quân=SUM/COUNT nếuCOUNT>0;Thựcchi=SUMPHIEUTHUCHI.SoTien joinDMTC.Loai=Chi theoNgayLap,mỗiMaTC1lần. Không cộng phiếu nhập/chốt lương/khấu hao vào chi thêm lần nữa.
- Tính% bằng decimal,kiểm mẫu số0/null. Trả cả giá trị kỳ trước và% để UI đặt nhãn. Hai kỳ=0 trả mức thay đổi0;trước0/sau>0 trả%null và giá trị gốc để hiện “Mới phát sinh”. Bình quân thiếu mẫu trảnull. Nếu không có độ phủ lịch sử đáng tin cậy,không bịa thời điểm mở quán/coverage từ hóa đơn đầu tiên.
- Donut:Squa từngLoaiDon theo tiền,chia cùng tổng;group0 vẫn có legend. Giá trị hiển thị làm tròn không sửa tổng tiền;ghi chú tổng% có thể lệch nhỏ do làm tròn,không giả mạo số nguồn.
- Top5 theoSUMSoLuong từ hóa đơn đã trả,sắp lượngDESC/MaSPASC. SQL trả tổngly của toàn kỳ riêng,không lấy tổng5dòng để gọi là tổngquán.
- Snapshot hiện tại:đếm đơnChua thanh toan,đơn giao đã trảCho giao/Dang giao,nguyên liệuSoLuongTon=0 vàSUMConNo dương từvw_CongNoNCC. Các số này đọc ởReadAt thật,không giả số dư lịch sử theo @At nếu @At là mốc kiểm quá khứ.
- Đọc nhất quán trong một transaction ngắn SERIALIZABLE,materialize nguồn theo thứ tự khóa đã đối chiếu với các procedure ghi. Kết thúc transaction trước trả dữ liệu/vẽ UI,không giữ khi user xem. KhôngNOLOCK,không bật thay cấu hình snapshot-isolation DB,không tự thêm index. Kiểm đồng thời và đo khóa ở đợt test;không coi chọn isolation là bằng chứng hết deadlock.
- Resultsets gồm bounds,Indicators và các metadata/totals/rows cố định cho những ReportPage ở mục2. DAL map typed/dispose reader trước trả lên BLL.

### 3.2. sp_DashboardAnalyticsSources

Tham số scalar:Actor/Stamp,nhóm nguồn được whitelist,From/Until,ReadAt/channel/productcode nếu áp dụng,Skip. Đọc toàn kỳ/khunggiờ/kênh/món/phiếuchi hoặc trạng thái hiện tại;query tham số,không ghép tên bảng hoặc WHERE từUI. Trả đúng shape ReportSourcePage/ReportSource đang có,pagesize50,sort ngày+mã ổn định.

- Revenue/bill/average/trend/channel/product trả nguồn Invoice bằngMaHD thật;Thựcchi trảFinance/MaTC. Nợ trảDebt/MaCN. Không tạo mã giả cho dòng aggregate.
- Danh sách đơn mở/giao chưa xong,nguyên liệu hết tồn,nợ hiện tại dùng cùng predicate với counter;không ápFrom ngày tạo làm mất đơn mở từ hômtrước.
- Đổi nguồn/thao tác recheckADMIN/phiên,cắt giờ đúng,nhiều phương thức thanh toán không nhân dòng bill. Phiếu chi không bị đếm lại qua nguồn liên kết.
- Material source trảMaNVL/TenNVL/Quantity0 từ trường sẵn có;danh sách đủ để xem nguyên liệu nào hết. Không bật “Xem chi tiết” nếu loại nguồn chưa có handler phù hợp;không gọi ReportSourceNavigation với loại không hỗ trợ rồi phát exception. Các nguồn Invoice/Finance/Debt dùng navigation hiện có.
- Không hứa cố định lịch sử các trạng thái hiện tại khi mở sauReadAt;UI ghi rõ số liệu đã đọc lại lúc nào. Đơn đã chốt bất biến theo contract hiện có.

Kiểm chứng sau khi đợt test được duyệt:clean/demo001–028,rerun/checksum,metadata schema trước/sau,quyền3role,parity và concurrency. Không sửa001–027.

## 4. DAL và BLL

Files:`src/CafeManager.DataAccess/Repositories/ReportRepository.cs`,`src/CafeManager.Business/Services/ReportService.cs`.

- Thêm hai luồng đọc analytics/source trên các class hiện có;giữ ReadAsync/DashboardAsync/SourcesAsync cũ hoạt động. Không tạo generic repository/UnitOfWork hoặc model mới.
- BLL demandFinancialReports,rechecksession như hiện có;chỉ cho đúng chế độHômnay/Thángnày. Bounds/sourcekind/channel/productcode/pager kiểm whitelist và miền giá trị. Nhân viên/account từsession,khôngUI truyềnActor tùy ý.
- Trước đọc analytics,kiểm SchemaVersion hiện có từAuthRepository.ReadStateAsync. Thiếu028 báo rõ cần cập nhật dữ liệu và không giả thành không phát sinh;không lặp lỗi2812 không rõ nguyên nhân từng xảy ra.
- DAL một call đọc từngresultset,map ReportPage/rowđúng nghĩa và các tuple/container chuẩn.NET nêu ởmục2. Disposeconnection/reader,async,cancellation;không trảSqlDataReader/SqlTransaction hoặcSqlClientobject lênform.
- Tiền/tỷ lệ decimal,số đếm kiểm miềnint khi cần hiển thị. Không tính lại tổng/chi/% ởC#. Giá trịnull được giữ đếnUI để phân biệt không đủ cơ sở so sánh.

Verify:API cũ không đổi kết quả;Models namespace/definition hash không đổi;dataset bất thường/mapping overflow có lỗi rõ,cancellation không thành số0.

## 5. UI Tổng quan và biểu đồ native

Files chính:`Modules/Home/AdminDashboardControl.cs`;mới trong cùng thư mục:`DashboardTrendChart.cs`,`DashboardChannelChart.cs`. Hai class mới chỉ là WinFormsControl vẽ/truy cập dữ liệu đã đọc,khôngentity/model/DTO. Top5 dùnglayout/label/thanh ngang từPanel chuẩn,tái sử dụngOrbitSurface/OrbitButton/theme hiện có.

- HeaderTổngquanquán,modeHômnay/Thángnày vàTảilại;labelbounds/so sánh/ReadAt. Bỏ nhómTruycậpnhanh lớn,đưa4KPI,2charts,Top5 vàCầnchúý theo đúng thiết kế.
- Mode dùng state UI local và tham sốbool,không thêm enum hoặc property vàoReportFilter. Đổi mode hủyrequest cũ,tănggeneration,xóa/khóa số liệu cũ và render nguyên bộ khi thành công. Quay về giữ mode nhưng tính ngày mới;login mới mặc địnhHômnay.
- KPI nhận giá trị SQL;%null/0 và mẫu gốc để đặt nhãn đúng. Số0 thật kháclỗi/thiếu dữliệu. Charttrống có thông báo vàlegend đơn vị,không vòng100% giả.
- TrendControl vẽ bằngGDI+/OnPaint như cáccontrol hiện có,2series khác màu+nét,có trụcVND/thờigian vàtooltip giá trị/thờiđiểm thật. Tooltip/chọnpoint qua chuột/phím trái/phải/Tab/Enter,không cần packageChart/DataVisualization/WebView. Độ thưa tick theo chiều rộng,không giảm font để ép nhãn.
- DonutControl vẽ2D,3màukênh cố địnhtheotheme,cólegend tên/tiền/% và chọnbằngphím. Không dựa vào màu để hiểu nhóm;AccessibleName/Description chứa thông tin và giá trị hiện tại. Tổng0 chuyển trạng thái rỗng.
- Top5 xử lý tên dài,quantitytheoDTO,bar chỉ biểu diễn tỷ lệ hình học từ lượng đã trả. Không tính lại tài chính hoặc đưa phần trướcgiảmallbill cạnh tổng đãtrả như cùng khái niệm.
- Cầnchúý có nhãnHiện tại tách với các phần theo kỳ;chọn một dòng mở nguồn đã lọc. Côngnợ là sốdư,không gắn%tháng hoặc gọiquáhạn. Tồnchỉhếttồn,không tự thêmngưỡng.
- Flow/tablelayout responsive:4KPI→2→1,haiChart→1,Top5/Cầnchúý→1 khi hẹp. Hỗtrợ1280×720,1366×768 vàpanehẹp,cócuộn khi cần;không chỉnhWindowsScale hay épDPIcốđịnh.
- Mộtrequestpending;hủyẩn/đóng/logout,latecallbackkhônglộdata. Tảilại lỗi chỉ rõ lỗi/toànkhối snapshotcũ nếu giữ,lần thànhcông mới render đồngbộ.

Verify:controlbinding/keyboard/tooltip/empty/error/bounds kỹthuật tại125%;hìnhthứcngườidùngkiểm,khôngchụpsoảnh hoặcPASSthịgiác.

## 6. Mở chi tiết mà không thêm thuộc tính filter

Files:`Modules/Reports/ReportSourceDialog.cs`,`Program.cs` và điểm mở từAdminDashboardControl. Giữconstructor/lệnhgọi cũ choReportscũ;thêmoverloadUI nhậnloader `Func<int,CancellationToken,Task<ReportSourcePage>>` và tiêu đề/phạmvi hiển thị. Đây làdelegateUI,khôngDTO/businessentity.

- Dashboard gọi source method bằng các tham sốscalar đúngkỳ/khunggiờ/kênh/món đã chọn;loader giữbounds vàkey trongclosure,không thêmFromTime/ToTime vàoReportFilter.
- Nguồn được mở bằngOrbitDialogHost cóownerMainForm,lồngchi tiết theocha,sửdụngReportSourceNavigation/DocumentLinks đã có. Chọnxong/quaylại giữmode,không gửiwrite từdashboard.
- KPI/card có affordanceXemchitiet/keyboard;trendpoint mởnguồn đúngbucket;donut đúngkênh;Top5 đúngmón. Tổngngàyhiện tại cắt theo cùngUntil đã đọc;không âmthầmmởcảhômnay và coi là snapshotbanđầu. Snapshotcurrentalerts được đọc lại vớiReadAt mới và có nhãn.
- Program nối source navigation hiện có vàoDashboard qua tham sốUI/delegate;không đổi MainForm navigation củaCASH/STORE/menuchung chỉđể thêm trang.
- Material list khôngcórowdetailhandler thìdisableactionrow phùhợp,không dựngentity/windowđộc lập. Không sửa cácForm nghiệp vụ ngoài nguồn đã liệt kê.

Verify:đổi ngày/mode/point,kênh,món trước mở/đóng/lồngdialog;nguồn khớpfilter,owner đúng và không truyvấn trùng;reports cũ giữ luồngcũ.

## 7. File/đầu ra và thứ tự triển khai sau duyệt plan

| Thứ tự | File/khu vực | Đầu ra kiểm được |
|---|---|---|
|1|028_dashboard_analytics_read.sql|Hai proceduređọc/GRANT nhưmục3;không DDL bảng/cột/view/index hoặc businesswrite|
|2|ReportRepository.cs|Map resultset vào DTO cũ+container/scalar chuẩn.NET,cancellation/dispose|
|3|ReportService.cs|Phânquyền/schema guard/validation và hai luồngđọc|
|4|AdminDashboardControl.cs,2chartcontrol|Bố cục phân tích thật,mode,render và lifecycle|
|5|ReportSourceDialog.cs,Program.cs|Nguồn chính xác/ownedmodal,constructorcũ tương thích|
|6|Fixture vàharness ởdatabase/tests/g07 vàtools probe cục bộ|Dataset2kỳ,biên thời gian/%/fanout/Top5/quyền/concurrency;khôngsửa expected để né lỗi|
|7|Build Debug/Release vàhồ sơ|report/test-plan,hashschema/Models/source;khôngticknghiệmthu từbuild|

Phạm vi không tự gồm thêm entity/DTO/property,bảng/cột,helperdùngchungngoàidanhsách hoặc refactorreportscũ. Nếu thiếu đầu ra SQL chặnDAL/BLL thì báo đúngblocker,không giảcontract.

## 8. Bộ kiểm thử dự kiến — chờ duyệt đợt test riêng

| Ca bổ sung | Dữ liệu và kết quả cần đạt | Truy vết |
|---|---|---|
|DA01|Clean/demo/rerun/checksum028;schema tables/columns/computed/index vàModelsdefinitions trước/sau không đổi;chỉ2procedure/GRANT/version thay đổi|Ràng buộc người dùng,T28|
|DA02|Bill lập khác ngày trả,open/hủy,bucket23:59:59 vàUntilđúng biên;hômnay/thángnày tính theoNgayThanhToan|K01/K05|
|DA03|2món×2PTTT,bill cógiảm/ship/phí;card=trend=donut,khôngnhântiền|K02–K04|
|DA04|So sánh cùng giờ/phút/dayMTD,kỳ trước0/cảhai0/thiếu mẫuAvg;31/03vớitháng02,nămnhuận,qua00h/sangtháng;khôngđổiclockWindows|K05/K06|
|DA05|CashChi thường/nhập trả ngay/trả nợ/chi lương;khôngcộngchốt/khấuhao/nguồnchi2lần;khácbiệtngàynhậpngàychi|K08/K10/K13|
|DA06|Top5 có>50món hoặc nhiềuSKU hơnpagesize,hòasốly,đổitên/giá sau bán;top5vàtổnglytoànkỳkhớp|K03/K04|
|DA07|Đơn mởtừhômtrước,giaopaid/open/cancel/finished,NVL0/nhiềuđơnvị,nợcòn0/dương;khôngđổisốdưtheomode|K07/K09/K13|
|DA08|Mọi nguồn/drilldown đúngbucket/kênh/món/kỳ,50rowpaging,total/sortổnđịnh;chi tiết Invoice/Finance/Debt vàMateriallist khôngactionlỗi|K05/K11|
|DA09|ADMINđọcđược,CASH/STOREbịchặnBLLvàSQL,demandkhiđổirole/logout/khóa;khônglộaggregate|K12|
|DA10|SQLServer2session:checkout/chiđồngthờireaddashboard,nguồntrùngkhớp vàtransaction0;đo deadlock/timeout thật|K03/K08|
|DA11|Dữliệurỗng,nguồnkhôngđủ,lỗikếtnối/timeout,modeA→Bquerymuộn,cancel/hide/dispose;không0giả/lẫnkỳ|K06/K11|
|DA12|WinFormsthật125%:4/2/1KPI,2/1chart,nhãndài/tiền18chữsố,legend/tooltip/Tab/arrow/Enter/owner/quaylại|K11/K13/UX39–42|
|DA13|Dataset10.000billtest:IO/TIME/timingUI;khôngtải toàn bộbill vềform,khônggiữtransactionkhixem;đánhgiálock SERIALIZABLE|T29 theo ảnh hưởng|
|DA14|Reportscũ,DashboardAsynccũ,login/startupCASH/STORE,đườngnguồncũkhôngbịđổi|Regression ảnh hưởng|

Test trênSQLServerthật/DBtestriêng,khôngtạodatađẹpchocharttrêndevelopment. Source/harness/migration cốđịnh từnglượt;biênbản mã/môi trường/schema/hash/input/lệnh/expected/actual/evidence. HìnhthứcSKIPPED theo phâncông,DPI100/150%NOTRUN;agentkhôngđổiScale hoặcghiPASSthịgiác. Bug mới ghi tạiT28 và duyệt cảđợttrướcsửa,khôngvátronglượttest.

## 9. Bàn giao development có kiểm soát

Không lặp bàn giao code mới với DB thiếu procedure. Sau triển khai/build,xin duyệt đợt test riêng theoquytrình. Chỉ sau khi testDBriêng đạt vàkhôngcònbugchặn,thực hiện bàn giao development trong phạm vi planđãduyệt:

1. Đóngapp bìnhthường,xácminh đúnglocalhost/CoffeeManager_Development/marker/schema027 vàchecksum.
2. BackupCOPY_ONLY/CHECKSUM,VERIFYONLY,giữbackup;ápduy nhấtmigrationđọc028quaApply-Migration. Trước/sau đốichiếuhashnguồn,tables/columns/Models để chứng minhkhôngthêmthựcthể/thuộctính hoặcđổidữliệunghiệpvụ.
3. BuildDebugF5vàReleasekhớp sourceđãkiểm;runtime đọcADMIN,Hômnay/Thángnày,nativecards/chartsđãload,khôngfakeaggregate. Developmentkhôngcógiaodịchkỳnàyphảihiệnrỗngđúng;khôngseedlạichỉđểcóchart.
4. ĐểappởTổngquanmới,ghiReadAt/build/schema vàcácgiớihạnthật. Khôngcommit/push nếu chưađượcgiao.

Đây là **phạm vi bàn giao đề nghị trong plan**,chưa được thực thi từ duyệt thiết kế. Nếu không thể đóngappcònnháp/preflightkhôngkhớp,giữnguyên và hỏi quyếtđịnh;khôngkillprocess/resetdata/chépchecksum choqua.

## 10. Điểm duyệt

Plan kỹ thuật mở rộng T28 đã được người dùng duyệt; không xin lại duyệt plan. Chờ duyệt [đợt test riêng](dashboard-analytics-test-plan.md) sau triển khai. Duyệt plan cho phép triển khai/build và phạm vi bàn giao đã ghi,không tự đồng nghĩa duyệt chạy đợt nghiệmthu/debug. Không xin lại duyệt thiết kế đã chốt hoặc từngfile riêng.
