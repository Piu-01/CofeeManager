# G04 — Đã kiểm nốt phần SQL của agent24/10/2026

**Mới nhất26/10 — UI Giao hàng đã triển khai theo plan bổ sung được duyệt:**
toolbar tìm kiếm hết hàng, filter tự áp dụng, profile Delivery/Address fill,
pager TotalCount thật và actionstate theo writing/unknown. Debug/Release sạch,
48/48smoke kỹ thuật125%. Đợt chức năng riêng đã duyệt/chạy:55/55giao hàng,
108/108native/shared regression,53/53invoice regression,18/18Sales xUnit; source
không đổi, fixture dọn, snapshot tiền/kho/điểm/receipt khớp. Còn xác nhận manual/
hình thức và giới hạn subcase, chưa chốt toànT18/G04.
[Báo cáo](t18-giao-hang/ui-delivery-report.md),
[kết quả/giới hạn đợt test](t18-giao-hang/ui-delivery-test-results.md).
Lượt test V4 cũ đã đóng theo hash cũ, manual/hình thức thiếu giữ nguyên chưa xác nhận.

**Mới nhất26/10 — Lịch sử hóa đơn V4 đã triển khai theo plan duyệt:** kính lúp
chỉ tìm kiếm, bộ lọc tự áp dụng với từ khóa đã submit; dùng theme/control chung,
pager TotalCount thật. Đợt test riêng đã được duyệt và chạy:53/53 functional,
94/94 regression,18/18 xUnit Sales; source cố định, fixture dọn, tiền/kho/điểm
bất biến. Còn xác nhận manual/hình thức và các subcase chưa chạy được ghi riêng,
không chốt toàn bộ T17/G04. Xem [báo cáo](ui-invoice-history-v4/report.md).
Các số PASS/native phía dưới thuộc binary và đợt cũ, không chứng nhận UI V4.
**UI hộp nổi mới26/10:** đã chuyển cơ chế editor/detail/F3/F4/F8 sang host trong
MainForm theo yêu cầu mới; build sạch, chờ duyệt test tại
[embedded-dialogs](../embedded-dialogs/report.md). Mốc40PASS/16BLOCKED bên dưới
thuộc binary trước refactor, không chứng nhận UIhost mới; các bằng chứng SQL/BLL
và lịch sử vẫn giữ nguyên. Không tự nghiệm thu lại T18 hoặc chuyển G05 từ build.

**Cập nhật26/10 —40 PASS /0 FAIL /16 BLOCKED trên56ID.** UI06 sửa và regression
40/40dialog +nativeuser đạt; T18-K01 PASS, T18 hiện8/8ID PASS. Theo yêu cầu ưu tiên
phụ thuộc để triển khai G05 trước, phụ thuộc trực tiếp T18→T19 đã được kiểm đạt;
có thể sang requirements/bộ plan G05, code sau duyệt. G04 vẫn Đang làm vì16ca
T16/T17 chưa đủ, không bỏ hoặc suy PASS các ca đó.
[Biên bản UI06/native](runs/recipient-validation-fix-20261026/test-results.md).
Các trạng thái FAIL/chờ native ở phía dưới là lịch sử trước xác nhận này.


**PC04 đã PASS lỗi đọc sau commit và đã khôi phục fixture:** người dùng xác nhận
HD20250420 về POS trống, thông báo Đã thanh toán / Chưa in được hóa đơn.
SQL chỉ một khoản PT001 81.000 ₫; kho NVL01 giảm0,018 và NVL03 giảm0,010,
khớp snapshot tiêu hao. Đã REVOKE direct DENY; quyền EXECUTE runtime kiểm lại=1.
[Biên bản](runs/post-commit-read-fault-20261025/test-results.md).
Đây là lỗi đọc dữ liệu in, chưa chứng nhận lỗi máy in vật lý. Đang kiểm in lại421.

**Mới nhất26/10:39PASS/1FAIL/16BLOCKED.** T18-K01 FAIL G04-UI06: editor F3 luôn gắn lỗi tại tên thay vì SDT/địa chỉ. [Điều tra và hướng sửa](t16-pos-huy-don/bug-report.md). Chờ duyệt sửa/regression, chưa đủ chốt T18/G05;39/0/17 là lịch sử. T18-K06 PASS quyền ba role native+SQL;38/0/18 là lịch sử. T18-K04 đạt hủy mở/khóa tài chính paid qua native và SQL;37/0/19 là lịch sử. T18-K07 đã PASS tìm mã/SDT/lọc trạng thái/refresh qua người dùng và SQL;36/0/20 là lịch sử. T18-K08 đạt qua ca403 người dùng xác nhận và SQL trước/sau bất biến; ưu tiên phụ thuộc G05/T18 tại [biên bản](runs/g05-dependency-check-20261026/test-results.md). Các số35/0/21 bên dưới là lịch sử.  PC03PDFđãđạt trên421: file1trang đúng
tiền81000/khôngnhãnINLẠI, POSnháp/Đãgửiin; SQL1PT00181000, điểm+81, kho/tiêuhao
khớp. T17-K04PASS. File thực ởOneDrive/Máytính/Documents, đã lưu evidence/hash.
[Kết quả](runs/pdf-print-20261025/test-results.md). Raw7/8harness cómộtlabelhardcode
códấukhácNameSQLTienmat; follow-up theo dữliệudanhmụcthậtđạt, giữ nguyên log.
PC04 đã dùng420 và chốt thành công;421 chỉ dùng kiểm in lại, không thu lại.
Các số34/0/22 bên dưới là lịch sử trước caPDF này.

**Hộp Đơn đang mở đã kiểm sạch:** người dùng F4mộtlần→tìm/doubleclick421,
xác nhận hộpđónghết/POSđúngmã. Đóng điều tra trạngthái hộpcũbịchồng từtool,
không sửa source hoặc gán bugClose chưa có. Tiếp tục PC03PDF, G04giữ34/0/22.

**Điều tra hộp chọn còn lại25/10:** ảnhusercho thấyPOSđãmởnhưnghộpchọncòn.
Logtooltrướccó2ownedmodal;hiệntạicòn1hộpcũ/POS21. Đang kiểm lại từ1F4sạch,
chưa khẳngđịnhbugClose hoặc sửa sảnphẩm. [Bằngchứng](runs/open-dialog-overlap-investigation-20261025/report.md).
PC03PDFchờkhôi phục trạngthái chọnđơn;G04giữ34/0/22.

**Native Đơn đang mở đã xácnhận:** người dùng tìm51đơn, trangcuối1dòng vàmởđúng
HD20250421. Người dùngkhôngcómáyingiấy; agentđọc thấyMicrosoftPrinttoPDFđãcài,
lưu snapshot21chưaTT81000/KH011điểm0 và hướng dẫn kiểm PC03bằngPDF, chưachốt
PASSoutput. [LượtPDF](runs/pdf-print-20261025/test-results.md). G04giữ34/0/22;
những đoạn chờnativeĐơnđangmở bên dưới là lịch sử trước phản hồi lần này.

**Tiếp tục sau chỉnhpager25/10:** người dùng đãcho test tiếp;59/59 form/SQL được
chạy lại trên đúngDLLpager mới,208sourcefilecốđịnh, táidùng51đơn, không ghi tiền/
kho/tiêuhao. Nativehelper setsearchđược nhưngReturnnhắmPOSrootkhông xácnhậnđược
query và hiện2ownedmodal; dừnginput, chờngườidùng thao tácOO51/page2/mởđúngmã.
[Biênbản](runs/g04-continue-pager-20261025/test-results.md). Còn kếtquảin418/lỗi
saucommit chưa đủ; G04giữ34/0/22.

**Chỉnh cụm pager theo ảnh riêng25/10:** chỉOpenOrdersDialog đổi sang hai nút
mũitênStylePager nhỏ +sốtrangcăngiữa, giữstate/query. Debug/Release sạch, hồiquy
7/7content/handlerPASS;98checks bên dưới thuộcbinary trước chỉnhnhỏnày, không
tựgánlại. [Biênbản](t16-pos-huy-don/open-orders-pager-adjustment.md). Hìnhthức
ngườidùngxácnhận;G04giữ34/0/22.

**Test Đơn đang mở25/10 đã duyệt/chạy:**59/59 formthật+SQL,7/7content/handlers,
32/32Checkoutregression PASS. TạiDPI120 mã80px có156pxvùngchữ/cột188px, giữ qua
4sizevàrebind/paging;51đơn tìmkhách cho50/1, responsecũ khôngghiđè, faultgiữinput.
208filecốđịnh, money/stock/consumption/pointshistorysnapshotkhôngđổi.
[Biênbản](t16-pos-huy-don/open-orders-ui-test-results.md). Đãmởbảnmới tạiLogin,
chờngườidùng native/hìnhthức; giữ34PASS/0FAIL/22BLOCKED, khôngsuytoànG04PASS.
Các chữchờduyệttestOpenOrders dưới đây là lịch sử trước đợt này.

**Đơn đang mở25/10 đã duyệt/triển khai:** bố cục Orbit theoZIP, profileOpenOrders
riêng bảo vệCodeđầyđủ/khôngco khibinding, ngàygiờ/tiền/kênh vàpagingtheodữliệuthật.
Chỉhai fileUIđổi, Debug/Release0warning/0error. Chờ duyệt
[đợt test mới](t16-pos-huy-don/open-orders-ui-test-plan.md), chưa nghiệmthu từbuild.
G04giữ34PASS/0FAIL/22BLOCKED; các chữchờduyệtplan phía dưới là lịch sử.

**Làm rõ manual hỏi in25/10:** PC01Không đạt trênHD20250419; PC02Có→Hủy đạt
trênHD20250417. Người dùng418chọnCó, chưa xác nhận kết quả in thật. SQL417/418/419
mỗiđơn1PT00181000, tổngHD20khôngtăng, khoNVL01−0.054/NVL03−0.030 khớp tiêuhao.
Source208file vẫn cố định. G04giữ34/0/22 vì còn nhánh khác. Người dùng yêu cầu
[Đơn đang mở theo ZIP](t16-pos-huy-don/open-orders-ui-plan.md), plan đã chuẩn bị,
chờ duyệt; chưa sửa sản phẩm.

**Đợt test hỏi in/reset25/10 đã duyệt, đang chạy:**8/8reset/nội dungin +4guard mới,
32/32checkout component,7/7SQLbridge PASS trên208sourcefilecốđịnh. Chưa suyPASS
Yes/No/PrintDialog từcomponent. DB riêng và4đơnPC01–04 đãchuẩnbị; người dùng đang
được hướng dẫnHD20250417chọnKhông,HD20250418chọnCó→Cancel, agent snapshotSQL.
[Biên bản](t16-pos-huy-don/post-checkout-print-test-results.md). G04giữ34/0/22.
Các dòng“chờduyệttest” dưới là lịch sử trước phản hồi duyệt lần này.

**Bổ sung hỏi in/reset POS25/10:** người dùng đã duyệt bằng “ok đó”; đã triển khai
hỏi in sau commit, bỏ Xem/In khỏiPOS, kết thúc bước in/bỏ qua/hủy/lỗi đều resetnháp.
SharedInvoicePrinting dùng lại cho lịch sử; Debug/Release0warning/0error.
[Triển khai](t16-pos-huy-don/post-checkout-print-implementation.md),
[đợt test chờ duyệt](t16-pos-huy-don/post-checkout-print-test-plan.md).
Chưa chạy chức năng/SQL/in phần mới; G04 giữ34PASS/0FAIL/22BLOCKED, không nâng từbuild.

**Mới nhất25/10:34PASS/0FAIL/22BLOCKED; G04 Đang làm.** Người dùng đã chốt
HD20250411 trên handler Confirm thật: không lỗi, thối19000. SQL xác nhận paid81000,
đúng1paymentPT001, tiêu hao/kho NVL01−0.018 vàNVL03−0.010 khớp. Source207file
không đổi. T16-K03 PASS, UI05 đã xử lý trong phạm vi lỗi; còn22ca GUI chưa đủ.
[Biên bản](t16-pos-huy-don/checkout-submit-test-results.md). Các trạng thái chờ/
33PASS bên dưới là lịch sử trước xác nhận lần này, không thay kết quả mới nhất.

**Sau duyệt sửa UI05 ngày25/10:** đã sửa effect và SelectAll, Debug/Release sạch;
32/32 component,60/60 layout/binding,11/11 effect/input PASS. Source207file cố định,
ba file sản phẩm đổi. Đang chờ kiểm handler Confirm thật trên bản mới và đối chiếu
SQL; giữ33PASS/1FAIL/22BLOCKED đến khi có bằng chứng, không suyPASS từfixture.
[Biên bản UI05](t16-pos-huy-don/checkout-submit-test-results.md).

**Mới nhất25/10:33PASS/1FAIL/22BLOCKED, G04 Đang làm.** Ca bấm Confirm thật
HD20250411 crash trước CheckoutAsync: StarDustOverlay.Enabled=false rồi Show.
Đã tái hiện đúng lỗi; SQL vẫnopen81000,0payment/tiêu hao,kho không đổi. T16-K03
FAIL. Chưa sửa source; gom lỗi này và yêu cầu chọn toàn bộ khi vào ô Khách đưa
trong [đợt UI05](t16-pos-huy-don/checkout-submit-fix-plan.md), chờ duyệt một lần.
Các kết quả33/0/23 bên dưới là lịch sử trước phát hiện lỗi handler này.

**Tiếp tục manual25/10:** người dùng xác nhận căn giữa ổn và yêu cầu test tiếp.
Đã mở F8 trên bản thật bằng phím, đọc modal đúng81000 và SQL vẫnopen/0payment.
Input/click modal gặp lỗi helper, đang chờ người dùng làm ca cash/Enter/Quay lại/
chốt để agent đối chiếuSQL. [Lượt hiện tại](runs/gui-continue-20261025/test-results.md).
Chưa nâng tổng33PASS/0FAIL/23BLOCKED, không sửa source trong lượt test.

**UI04 ngày25/10:** theo yêu cầu căn giữa ô Tiền mặt, đã thêm cột combobox native
dùng chung. Editor38px ở panel58px chuyển Top0→10;32/32 component và60/60
layout/binding Darkmode125% PASS, Debug/Release sạch. Không đổi nghiệp vụ Số thu
chia phương thức. [Biên bản](t16-pos-huy-don/method-center-test-results.md).
G04 vẫn33PASS/0FAIL/23BLOCKED; hình thức do người dùng kiểm.

**Cập nhật sau sửa UI03 ngày25/10:33 PASS /0 FAIL /23 BLOCKED; G04 Đang làm.**
Đã duyệt và sửa bảng giãn, cash/notice bị cắt; bổ sung style editor trong Payment
profile dùng chung và note ưu đãi dưới dòng Ưu đãi. BuildDebug/Release sạch;
60/60 layout/binding Darkmode125%,32/32 component cũ,7/7 SQL bridge PASS.
DB checkout riêng đã dọn có marker; đơn manual giữ nguyên. Hình thức do người dùng
tự kiểm; DPI100/150% và thao tác chuột/phím toàn app chưa chạy, không suyPASS.
[Biên bản UI03](t16-pos-huy-don/display-fix-test-results.md). T16-K11 trở BLOCKED vì
còn nhánhPOS chưa kiểm. Các dòng33/1/22 bên dưới là lịch sử trước sửa UI03.

**Mới nhất25/10:33PASS/1FAIL/22BLOCKED,G04chưađạt.** Người dùngbáobodyCheckout
khônggiốngmẫu/cònclipping. Đúngbảnfooterfix;probeDarkmode/DPI120tái hiệngrid
1408pxtrongformcao750px,ôtiền/noticebịđẩyra ngoàiviewport. G04-UI03đãđiềutra,
chưa sửa,đãgom [hướngsửa](ui-checkout-proposal/display-fix-plan.md) chờduyệt.
Khônggánlỗi chongườidùngdùngnhầmbảnhoặcnghiệmthuhìnhthứctừ32/32footer.

**Sau sửafooter25/10:33PASS/0FAIL/23BLOCKED,toànG04chưa nghiệmthu.**
Người dùng đãduyệt G04-UI02;chỉsửaCheckoutDialog.Layout đểCTAkhôngwraptựphát.
Debug/Release sạch;32/32component và3/3boundsổnđịnh125%PASS,sourcecốđịnh.
[Biênbản](t16-pos-huy-don/footer-fix-test-results.md). T16-K11cònnhánhPOSchưakiểm
nêntrởBLOCKED,khôngnângPASS.Nhữngsố33/1/22bêndưới làlịchsửFAILtrướcsửa.

**Kết quảđợttestCheckoutUI25/10:33PASS/1FAIL/22BLOCKED,toànG04chưađạt.**
Unit13/13,component29/32,SQL/UIbridge7/7;3FAILmetricsfooterđãtáiđosau700ms
vẫnFAIL,T16-K11chuyểnFAIL. [Biênbảnđầyđủ](ui-checkout-proposal/test-results.md).
Bugmới[G04-UI02](t16-pos-huy-don/bug-report.md) đãđiềutracónguyênnhân/chưa sửa.
Nativekeyboard/cancel/timeoutmấtresponsechưađủevidence,khôngsuyPASS từcomponent.
206sourcefileđóngbăngkhôngđổi.DBChekouttestđãdọn,HD20250411DBmanualvẫnopen81000,
0payment. Số33/0/23bêndưới làlịchsửtrướcpháthiệnUI02.

**UI Checkout25/10:đã duyệt và triển khai,chờ duyệt đợt test mới.**
Đã dùng sharedOrbitDialog/Field/Button/GridPayment/Summary/Notice theoZIP;
Debug/Release sạch0warning/0error. [Triểnkhai](ui-checkout-proposal/implementation.md),
[test-plan](ui-checkout-proposal/test-plan.md). Không chạytestchức năng,bấmchốt,
hoặcghiDB trong đợt triểnkhaiUI.G04giữ33PASS/0FAIL/23BLOCKED,chưa nghiệmthu.
Đoạn“chờduyệtplan”bên dưới là lịch sử trước khi người dùng duyệt.

25/10người dùng yêu cầu đổiUICheckoutDialog theoZIP mới,ưu tiên sharedtheme/control.
Đã kết thúc lượtmanual cũ tại bước trước xác nhận validationthanh toán;không
ghiPASS T16-K03/K04 hoặc giả đãchốtHD20250411. Chưa sửa source;đang chờ duyệt
[planbổsungUI](ui-checkout-proposal/plan.md). G04giữ33PASS/0FAIL/23BLOCKED.

**Tiến độmanual25/10/2026:33PASS/0FAIL/23BLOCKED.** T16-K02 đãPASS:người dùng
xác nhận đủthêm/sửa/xóa đúng dòng vàtổng111000→192000→162000→81000 trênHD20250411;
agent đối chiếuSQL cuối chỉSP001×1,ghi chúgiữ,giảm/ship0,chưaTT,0payment.
[Evidence](runs/header-fix-20261025/manual-order-HD20250411-edit.json).
Các tổng32/0/24 và32/1/23 bên dưới là lịch sử;chưa nghiệm thu toànG04.

**Sau sửa25/10/2026:32PASS/0FAIL/24BLOCKED,Đang làm.** Người dùng đã duyệt
G04-UI01;đã sửaOrderHeaderDialog,13/13component regressionPASS vớiSQLtest/125%,
Debug/Release sạch. [Biên bản](t16-pos-huy-don/header-fix-test-results.md).
T16-K01 còn chờ đủ các nhánh tạo/reopen3kênh trênapp nên chuyểnFAIL→BLOCKED,
không nângPASS. Phần32/1/23 dưới đây là lịch sử lúc phát hiện lỗi, vẫn giữ nguyên.

**Cập nhật25/10/2026:32PASS/1FAIL/23BLOCKED, Đang làm.** T16-K01 chuyển từ
BLOCKED sangFAIL khi thao tác thực tế không tạo được món đầu của đơnMang đi:
header dialog giữÁp dụng disabled. Đã điều tra
[G04-UI01](t16-pos-huy-don/bug-report.md), source chưa sửa; chờ duyệt bổ sung
bug mới. Probe binding .NET10 tái hiện lỗi partner không parent/BindingContext
trước bước bật nút. Không thay expected hoặc gán lỗi này thành lỗi quyềnSQL.
Các số32/0/24 bên dưới là lịch sử trước phát hiện này.

**Đang làm, chưa nghiệm thu toàn G04:32PASS,0FAIL,24BLOCKED.** Bốn ca SQL trước đó
được gắn BLOCKED khi agent vẫn kiểm được là phân loại chưa chính xác. Đã kiểm nốt,
không chuyển phần này cho người dùng. Không sửa sản phẩm/source/schema016 hoặc
fixture vào development trong lượt này;không commit/push.

| Ca trước đây thiếu bằng chứng | Kết quả mới |
|---|---|
| T14-K02 |NULL/FK bàn/khách;actor không có/stamp cũ/tài khoản khóa/nhân viên nghỉ đều kiểm;không orphan/receipt,transaction0:0 |
| T14-K09 |Hai session,hai thứ tự;chính phiên thua tải đúng MaHD thắng và1dòng món,không chèn nhầm món |
| T14-K11 |Faultưuđãi/timeout trướccommit rollback;đóngconnection vàtimeout saucommit đọc lại1receipt/header/line;replay không lặp;khác hash/actor51608 |
| T15-K15 |Baseline+seed→001–016 giữ lịch sử/số dư;seed lại51200 không đổi rows;legacy-invalid bị52107/52207,không tự sửa dữ liệu hoặc ghi version |

Đã kiểm thêm4ca SQL policy/recipe đồng thời theo cả hai thứ tự với applock GRANT
làm barrier:policy trước0/52201 yêu cầu preview mới;checkout trướcpolicy0/0 giữ
snapshot cũ;recipe trước/sau0/0 trừ đúng NewQty/OldQty. Cả8session0:0,không mất điểm
hoặc ghi lặp. [Diễn giải và lịch sử instrumentation](runs/sql-completion_20261024_v4/interpretation.md)
giữ nguyên FAIL của assertion/barrier ban đầu và follow-up có quan sát đúng;
không sửa expected để né lỗi và không gán lỗi bộ test thành lỗi sản phẩm.

[Manifest tổng](runs/sql-completion_20261024_v4/final-manifest.json),
[lượt chính](runs/sql-completion_20261024_v4/manifest.json),
[actor quan sát4mode](runs/sql-completion_20261024_v4/actor-followup.json),
[policy/recipe4ca](runs/sql-completion_20261024_v4/ordering2-manifest.json).
Source/test giữ nguyên trong từng lượt. Đã dọn6DBtest,còn0;không đổi dev/backup,
service,login hoặc WindowsScale.

Kếthừa [đợt016](runs/regression_20261024_concurrency_v3/manifest.json):68/68SQL và
3/3clean/rerun/checksumPASS;52unit v2 vẫn giữ,C# không đổi. Development vẫn016 với
backup COPY_ONLY CHECKSUM,số dư/tài khoản không đổi. Hai bug concurrency đã sửa,
không còn yêu cầu duyệt đợt debug mới.

**24ca cònBLOCKED là phần GUI chưa đủ bằng chứng**:T16-K01–K12 (12ca),T17-K03–K09
(7ca),T18-K01/K04/K06/K07/K08 (5ca). Nhiều phầnSQL của chúng đã đạt;phần thiếu là
thao tác POS,keyboard/nháp,unknown/refresh,phiên/quyềnUI,xem/in/in lại và chuyển giao.
Computer Use đọc accessibility/SetValue được nhưng click/focus chưa thao tác được;
không suyPASS handler từSQL hoặc UI đứng yên. Người dùng có thể phối hợp thao tác
GUI theo kịch bản;agent đối chiếuDB và lưu evidence,không cần người dùng tự testSQL.
Hình thức UI/bản inSKIPPED theo phân công;DPI100/150%NOT RUN,không đổiScale.

Ngày24/10 đã chuẩn bị [hướng dẫn phối hợp GUI](manual-test-guide.md) thành6lượt,
và một DB test riêng schema016 theo [manifest](runs/manual-g04-20261024/manifest.json).
SP001 giá81.000 chỉ trong fixture để kiểm tiền đưa100.000/thối19.000.
Build Debug thành công0warning/0error; chuẩn bị/mở app chưa phải bằng chứng PASS.
DB giữ cho phiên manual, xem lại25/10; có script cleanup xác minh đúng tên/marker.
Không đổi source sản phẩm, dữ liệu development, tài khoản hoặc server login.

Sau đó người dùng yêu cầu gỡ hoàn toàn bypass đăng nhập ngày24/10. Đã kết thúc
source cố định của lượt SQL/GUI trước; sửa riêng tiện ích đăng nhập, không đổi
nghiệp vụ G04. Launcher/hướng dẫn manual G04 chuyển sang đăng nhập thường
admin/thungan/kho. Xem [biên bản gỡ](../temporary-test-login/removal-results.md).
Các kết quả trước vẫn là lịch sử;24ca GUI chưa có actual vẫn chưa PASS.

Người dùng đã đăng nhập thungan qua GUI sau gỡ bypass; agent quan sát đúng CASH
và POS, không phải ADMIN. Menu CASH còn lối vào riêng không đúng UX, ghi bug mới
tại [T07-NAV-20261024](../g02-truy-cap-ung-dung-cau-hinh-nen/t07-khung-giao-dien-dieu-huong/bug-report.md).
Chưa sửa navigation; không suy vượt quyền ghi hoặc nghiệm thu toàn ca G04 từ việc
đăng nhập được. Đăng nhập sai/logout qua GUI chưa có actual.

Sau khi người dùng duyệt, đã sửa navigation theo ma trận UX, giữ3roleSQL.
[T07 menu/startup regression18/18PASS](../g02-truy-cap-ung-dung-cau-hinh-nen/t07-khung-giao-dien-dieu-huong/menu-fix-test-results.md),
Debug/Release sạch. CASH vàoPOS, không có menu quản trị bàn/menu/ưuđãi riêng;
STORE không có bán/giao;ADMIN giữ8module vàDanh mục nền. G04 vẫn32/0/24 vì các
nhánh chức năng handler/checkout/in/giao chưa đủ bằng chứng không được suyPASS.

Biên bản đủ56ID:[T14](t14-tao-sua-don/test-results.md),[T15](t15-checkout/test-results.md),
[T16](t16-pos-huy-don/test-results.md),[T17](t17-tra-cuu-in/test-results.md),
[T18](t18-giao-hang/test-results.md). Checklist gốc T01–T32/325ID giữ nguyên;không
nâng cảG04Hoàn tất khi24ca chức năngGUI chưa nghiệm thu. Lịch sử bàn giao trước
lượt này:[report-before-sql-completion](runs/sql-completion_20261024_v4/report-before-sql-completion.md).







