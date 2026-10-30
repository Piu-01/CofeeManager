# Lỗi bộ kiểm thử T16 — đợt G04 ngày 23/10/2026

## Điều tra hộp Đơn đang mở còn lại sau doubleclick25/10

Kết quả: người dùng kiểm từ một F4 sạch và xác nhận hộp đóng hết/POS đúng421.
Đã phục hồi trạng thái test bị chồng từ công cụ; chưa có bugClose cần sửa.
Không sửa sản phẩm, giữ lịch sử/bằng chứng bên dưới.

Người dùng báoPOSđãmởđơnnhưngcòn hộpchọn. Trước đó toolnhậpEnternhầmPOSroot
đãđểlại2ownedmodal; capturehiệntại chỉcònhộp124cũ, POSHD20250421. Chưa xácđịnh
bugClose trên trạngthái1hộpsạch; nguồnSelectOrdercóDialogResult.OK/Close.
Đã nhờ đóng hộp còn lại→F4mộtlần→doubleclick để kiểm riêng, không tựsửasource
hoặc xin duyệtfix khi chưa chứngminhnguyênnhân mới. [Điều tra](../runs/open-dialog-overlap-investigation-20261025/report.md).

## G04-UI05 — Confirm crash trước gửi SQL vì StarDustOverlay disabled

Đã kiểm đường Confirm thật: người dùng xác nhận không lỗi/thối19000, SQL thu
đúng81000 một lần và tiêu hao/kho khớp. UI05 đã xử lý, T16-K03 PASS; xem biên bản
checkout-submit-test-results.md. Phần “đang chờ” bên dưới giữ lịch sử.

Sau phản hồi “duyệt”: đã sửa đúng ba file, kiểm riêng effect và input11/11,
component32/32/layout60/60 PASS. Đang chờ xác nhận handler Confirm thật trên bản
mới, không tự nâng PASS từ component. Phần điều tra/chưa sửa dưới giữ lịch sử.
[Biên bản sửa/regression](checkout-submit-test-results.md).

Người dùng báo sau bước bấm xác nhận thanh toán HD20250411. Đã tái hiện đúng
InvalidOperationException tại StarDustOverlay.Burst→Form.Show: overlay.Enabled=false.
Checkout gọi Burst sau SetBusy(true), ngoài try/finally và trước CheckoutAsync,
nên lỗi thoát handler và khóa controls. SQL sau lỗi vẫn chưa thanh toán81000,
0payment/tiêu hao; xem audit kho trước/sau. Không tự retry hoặc chốt bằngSQL.

Chưa sửa sản phẩm; [báo cáo/hướng sửa cả đợt](checkout-submit-fix-plan.md)
gom UI05 và yêu cầu tự chọn toàn bộ ô Khách đưa khi vào ô. T16-K03 FAIL;
32/60/7 kết quả trước chỉ áp dụng phạm vi đã ghi, không chứng nhận handler Confirm.

## G04-UI04 — Editor phương thức bám mép trên dòng

25/10 người dùng gửi ảnh và chỉ định rõ căn giữa ô phương thức. Số đo đã có từ
UI03: editing panel cao58px, combobox38px, editor.Top=0 nên ô bám mép trên;
không phải lỗi tính tiền. Đợt này chỉ làm yêu cầu căn giữa nhỏ, không đổi nghiệp vụ.

Thêm OrbitPaymentMethodColumn dùng lại DataGridViewComboBoxColumn native;
cell override PositionEditingControl gọi base rồi căn Top theo chiều cao panel.
Checkout dùng cột chung mới; binding code/name, editor/theme và profile khác giữ
nguyên. Không sửa BLL/SQL, không khóa Số thu vì UX cho phép chia nhiều phương thức.
Rủi ro thực tế: editor bị cắt khi resize/scroll hoặc mapping không còn đúng;
regression kiểm bounds, binding và validation bằng WinForms thật ở125%.

Trước sửa:Top0/Height38/Parent58,Centered=false; sau sửa:Top10/Height38/Parent58,
Centered=true. [Biên bản](method-center-test-results.md). Hình thức người dùng tự
kiểm; không suy PASS hình thức từ số đo. Giữ nguyên kết quả G04 và lịch sử UI03.

## G04-UI03 — Body giãn vượt viewport và giao diện Checkout lệch mẫu

Đã duyệt/sửa và regression25/10:60/60 layout/binding Darkmode125%,32/32 component,
7/7 SQL bridge PASS. Bảng108px cho một dòng; cash/notice không còn clipping ở
size chuẩn, phần hẹp/nhiều dòng cuộn được. Hình thức chờ người dùng xác nhận;
không suy toànT16-K11 PASS. [Biên bản](display-fix-test-results.md).
Đoạn “chưa sửa/chờ duyệt” dưới đây là lịch sử trước duyệt; giữ chứng cứ FAIL cũ.

Ngày25/10người dùng báo bản mới khônggiốngZIP,gửiảnh. Đãxácnhận process đang
chạyđúngfooter-fix,khôngphảibảncũ. Source chưa sửa,chờduyệt đợt bổ sung.

Nguyênnhânlayoutcóbằngchứng:inputTableAutoSize=true,rowgridPercent100,DockFill
trongbodyAutoSize;gridgiãn1408pxởform1150×750,DPI120,đẩyTenderY1805/NoticeY1900.
Cáccontrols bịclippingởviewport/form,cảtrướcvàsaufocus/chọnmethodblank.
ProbeđúngDarkmodenhưProgram,khôngtruycậpDBhoặcchụpảnhQA.
Ảnhngười dùng cho thấycombo native trắng,stylephânnhóm khácmẫu;managedcolor
editor=#1C1823 khôngđủchứngminhmàupaint thực,nênchưachốt riêngcơchếrenderer.

Bằngchứng:[observations](../runs/checkout-display-investigation-20261025/observations.json)
vàảnhngười dùng;phạmviT16-K11FAIL,khônglấy32/32footer làmnghiệmthu toànbody.
[Hướng sửa/rủiro/regressionđãgom](../ui-checkout-proposal/display-fix-plan.md):
boundgridtheosốdòng/scrollnội bộ,giữcash/notice hiệnđúngvùng,bámheader/summaryPNG,
stylingchungPaymentcell/editor;khôngthaySQL/role/logic tài chính.

Thừa nhận thiếu kiểm chứng trước:bộregressionđofootervàgiátrị/chứcnăng,
khôngđoboundscash/notice. LịchsửPASS chỉ áp dụngphạmvi đãghi;đợtnày bổ sung
criteriahiểnthịcontrols,khôngđổi expectedcũ để néFAIL.

## G04-UI02 — Footer xác nhận thanh toán bị cắt khi layout hàng nút wrap

Trạng thái:người dùng đã duyệt sửa25/10/2026;đã sửa và32/32component cùng3/3
đofooterổnđịnhPASS. [Biênbản](footer-fix-test-results.md). Phầnđiềutradướiđây
giữlịchsửtrướcsửa;khôngsuynghiệmthu toànT16/G04.
Sở hữuCheckoutDialog.Layout.cs và cách bố tríactions củaOrbitDialogLayout;
liênquanT16-K11/C08. KhôngthaySQL/role/BLL đểsửalỗiUI.

Hiện tượng:componentthật tạiDPI120,1280×720/1366×768/740×700 đềuConfirmoutside
client. Đo lại sau700ms giữcùngFAIL,khôngphảichỉchưalayoutxong.
1280×720:clientbottom871,Confirmbottom891;actionsheight100 trongfooterheight50.
[Bounds/parentchain](../runs/checkout-layout-probe-20261025/bounds.json).

Nguyên nhân:CheckoutDialog.Layout đặt `actions.WrapContents=true`;khungdialog
dùngcộtAutoSize. Flowđượcwidth238trongkhipreferredwidth378;check/confirmxuống2dòng,
nhưngfooterchỉchừa1dòng50px. Dialog.DockFillkhôngtựnớifooterđểchứadòngthứ2,
nútchốtbịcắt hoặcra ngoài vùngthao tác. Đây làlayoutkỹthuật,khôngphảiQAđẹp/xấu.

Hướngsửađềxuất:sắpQuaylạivàcheck/confirmbằngfootercókíchthướctheoPreferredSize,
giữnhómCTAkhôngwraptựphátkhiđủchỗ;khihẹp,nếucầnwrapthìdòng/chiềucaophảicậpnhật
đồngbộvớiparent. Giữfootertruycậpđược khi bodycuộn,khôngcốđịnhDPI/cofont.
Ưutiênsửaoptioncheckout,khôngđổidefaultdialogkhácnếukhôngcần.

Phạmvisửa:CheckoutDialog.Layout.cs;OrbitDialogLayout.cs chỉkhinếuđocầnthiết.
GiữhandlersConfirm/Reconcile/Cancel,validation,SQLtransactions,role và56ID.
Rủiro:cộtCTAchiếmhếtnútQuaylạiởwidthnhỏ hoặcđổiheightgâychebody;regression
3size125%,núttrongclient/ancestor,cuộn/chuyển2cột→stack,tabfocus/Enter/Esc,
sharedDialogdefault,validation/unknown. GiữcácFAILvàsourceđóngbăngđợttrước.

Chờ người dùng duyệt đợt bug bổ sung trước sửa. AssertionEnter trongprobe vẫn
giữđiềutra/NOTCONCLUSIVE,khôngkhẳngđịnhlỗichốt tựđộnghoặc tựvákeyboard.

## G04-UI01 — OrderHeaderDialog không bật Áp dụng cho đơn không giao hàng

Ngày25/10/2026, người dùng thao tác Mang đi → thêmSP001 → lưu dòng → hộp
Khách/Ưuđãi/Giao hàng rồi báo không bấm được. Đây là bug sản phẩm mới,
đã được người dùng duyệt bổ sung và sửa25/10. Regression13/13componentPASS,
xem [biên bản](header-fix-test-results.md); chưa suyPASS mouse/keyboard hoặc
toàn ca tạo/reopen3kênh. Phần điều tra/hướng sửa bên dưới giữ làm lịch sử trước sửa.

Accessibility WinForms thật xác nhận fields và Áp dụng disabled, Quay lại enabled,
state còn “Tối đa50 kết quả…”. Không khẳng định Quay lại hoạt động/không hoạt động
từ enabled; chưa có bằng chứng handler đóng. SQL chỉ đọc:11khuyến mãi, không có
request đang chờ khóa ở thời điểm kiểm, session còn lại sleeping,0transaction.

Nguyên nhân: OrderHeaderDialog chỉ gắn partner vào fields khi Channel=Giao hang,
nhưng sau await vẫn bind partner.DataSource rồi đặt SelectedIndex=0 cho mọi kênh.
Combo không có parent/BindingContext có Items.Count=0, SelectedIndex=-1;
gán0 ném ArgumentOutOfRangeException trước save.Enabled/fields.Enabled=true.
Catch hiện tại không xử lý lỗi này. Tái hiện .NET10 WinForms độc lập giữ cùng
DisplayMember/ValueMember/DataSource: không parent→0item/exception; có parent→1item,
index0/không exception. Đây là bằng chứng kỹ thuật binding, không giả làm chạy
lại toàn bộ dialog có SQL.

Bằng chứng: [binding-results](../runs/header-investigation-20261025/binding-results.json),
[dialog-readonly](../runs/header-investigation-20261025/dialog-readonly.txt).
Không sửa product/source/schema/expected hoặc fixture DB để né lỗi.

Đề xuất sửa sau duyệt: chỉ tải/bind/chọn partner ở đơn Giao hang; bảo đảm binding
các combo hiển thị đã sẵn sàng trước chọn index; quản lý trạng thái tải/thất bại rõ,
cho Quay lại/đóng khi lỗi; không bật Áp dụng khi dữ liệu chưa tải hợp lệ. Giữ input
món đang nhập, request/transaction và contract tạo món đầu nguyên trạng.
Không đổi SQL/role, không thêm fallback bỏ qua lỗi hoặc tự chốt thiếu dữ liệu.

Regression: T16-K01 đủ3kênh, khách vãng lai/khôngKM và khách/KM hợp lệ; nhận
Áp dụng/Quay lại bằng chuột/bàn phím, lỗi lookup/cancel/reopen; Giao hang chọn
đối tác/Tự giao và validation người nhận; T14-K01/UX-K21 theo ảnh hưởng. Đối chiếu
DB test món/ghi chú/số tiền/không orphan hoặc ghi lặp;125%,hình thứcSKIPPED.
Rủi ro: chỉ xử lý control ẩn sai cách có thể bỏ kiểm dữ liệu Giao hang hoặc mất
nháp khi quay lại; kiểm cả hai nhánh. Không ghi PASS từ probe binding đơn lẻ.

## Cập nhật24/10/2026

G04-TEST-03 đã sửa đúng hai lỗi compile trong fixture v2; build0warning/0error.
Native CheckoutDialog thật DeviceDpi120 đã mở/đóng; không sửa sản phẩm. Có kiểm
SetValue control thật:100.000→thối19.000,80.000→báo thiếu,abc→báo nhập nguyên VND.
Evidence `../runs/regression_20261024_testfix_v2/gui-actions.json` và gui-results.json.
Không có commit DB từ fixture in-memory; không coi là bằng chứng login hoặc SQL.

Computer Use lần đầu hết hạn cấp quyền; sau người dùng chọn cấp quyền, accessibility
và SetValue hoạt động. Click button vẫn trả `coordinate input geometry is unavailable`
kể cả sau get_window/Raise/retry; Tab không cho thấy focus đổi. Dừng input click,
không coi Enter/doubleclick/handler confirm là PASS từ trạng thái đứng yên. GUI đầy
đủ vẫn BLOCKED bởi khả năng thao tác công cụ, không còn do lỗi compile fixture.

Print thử trong bộ nhớ, không gửi job:100dòng/210entry,9trang lần đầu; controller dùng
lại trả18previewpage tích lũy ở lần hai, cursor210. Chưa chứng nhận18là số trang mới
của lần in thứ hai hoặc kiểm hình thức/nội dung theo trang. Không nâng T17 ca in lại
thành PASS từ số này. 52unit G04/regression đạt, nhưng không thay test UI.

## G04-TEST-03 — Fixture GUI chưa compile, chưa thao tác UI

Trạng thái: chờ duyệt sửa fixture, chưa sửa; không phải lỗi build solution sản phẩm.

`../runs/acceptance-wave_20261023_195243_f9876e48/gui-build.log` ghi hai lỗi trong
`gui/Program.cs`: CS0246 AccountIdentity thiếu namespace `CafeManager.Models.Dtos`;
CS1674 PreviewPrintController không triển khai IDisposable nhưng được khai báo using.
Build này chỉ thuộc fixture mới nằm trong hồ sơ đợt test. Không chạy binary cũ để
thay kết quả; chưa có thao tác bàn phím/validation/checkout nào qua fixture này.

Đề xuất sau duyệt: thêm đúng using, bỏ using trên PreviewPrintController; vẫn dispose
PrintDocument và từng Image trả về. Build fixture, đóng băng lại bản mới và chạy
chức năng CheckoutDialog/POS tại125% bằng Computer Use, không chụp hay đánh hình thức.
Fixture in-memory chỉ chứng nhận validation/keyboard, không chứng nhận login, quyền
SQL runtime hoặc commit. Muốn nghiệm thu luồng ghi phải host dependencies thật với
marker DB test/session hợp lệ; không dùng TEST bypass làm bằng chứng đăng nhập.

Chạy bù: các nhánh T16-K01–K12 và UX về nháp, Enter/F8, gửi lặp, loading/lỗi/unknown,
refresh/revoke; lưu thao tác thực và đối chiếu DB. Các phần đăng nhập bình thường
cần phiên test được thiết lập qua contract chuẩn, không tự động hóa dialog xác thực
bằng Computer Use (giới hạn của skill). Hoàn thành phần độc lập trước khi xin hỗ trợ.
Không thay Windows Scale; hình thức do người dùng tự kiểm.

Rủi ro: chỉ sửa bộ kiểm thử. Không sửa service/UI sản phẩm hay dùng fixture giả để
đánh PASS luồng tiền. Probe hiện có đã chạy lại exit0: native120DPI,1row/3columns,
CTA trong client; đó chỉ là bằng chứng binding kỹ thuật hẹp, không thay T16 nghiệm thu.

## G04-UI06 — Lỗi người nhận luôn gắn dấu lỗi ở tên (26/10/2026)

Trạng thái: ĐÃ ĐIỀU TRA, CHỜ DUYỆT SỬA. Phát hiện ở T18-K01 trong đợt ưu tiên
phụ thuộc G05, owner editor T16. Không sửa source trong lượt test.

Hiện tượng: trên F3 của HD20250474, user thử tên trống, SDT trống/abc và địa chỉ
trống. Tất cả lỗi đều đặt dấu X tại Tên người nhận, không đúng trường sai.
User chưa xác nhận nhánh tìm421 trong Giao hàng; không suy PASS nhánh đó.

Nguyên nhân có bằng chứng: OrderHeaderDialog.Save(), catch ArgumentException
(dòng119) luôn chọn recipient nếu original.Channel=Giao hang rồi SetError/Focus
vào đó. SalesService.Normalize gom tên/SDT/địa chỉ sai vào một ArgumentException
chung. Validation vẫn chặn lưu nhưng UI không ánh xạ đúng trường.

Đề xuất: kiểm từng input người nhận ngay trong Save, gắn ErrorProvider và thông
báo cụ thể tại tên/SDT/địa chỉ tương ứng; focus trường sai đầu tiên. Xóa lỗi cũ
mỗi lần Áp dụng. Tiếp tục gọi Normalize của BLL, không thay contract/SQL/quyền,
không đổi business validation hoặc tự lưu dữ liệu. Tái dùng ErrorProvider sẵn có,
không tạo theme/control mới cho việc này.

Regression trong đợt debug này: tên trống/khoảng trắng; SDT trống/abc; địa chỉ
trống/khoảng trắng; nhiều trường sai focus đầu tiên; sửa lại hợp lệ hết lỗi và
trả header typed; phí ship sai vẫn gắn đúng ô; hai kênh không giao không bị đòi
người nhận. Kiểm cả dữ liệu giữ nguyên khi validation fail/Quay lại.

Rủi ro: ảnh hưởng editor F3 của đơn giao và nháp; chỉ thay trình bày lỗi, không
thay nghiệp vụ tài chính. Phạm vi source dự kiến duy nhất OrderHeaderDialog.cs.

### G04-UI06 — đã sửa, regression kỹ thuật đạt

Sau duyệt26/10, OrderHeaderDialog.Save gắn ErrorProvider đúng từng input tên/SDT/
địa chỉ và focus lỗi đầu; giữ Normalize và ErrorProvider cũ. Debug/Release sạch;
40/40 real-dialog/runtime SQL regression PASS, DeviceDpi120,208source không đổi
trong lượt test. Bản Debug mới đã mở đăng nhập thường cùng DBtest. Còn chờ user
kiểm native F3; chưa đóng bug/toàn T18-K01. Evidence: ../runs/recipient-validation-fix-20261026/.

### G04-UI06 — đóng sau xác nhận native

User26/10 xác nhận toàn bộ dấu lỗi/focus đúng ô và tìm đơn Mang đi421 trong màn
Giao hàng không có dòng. SQL474 giữ dữ liệu hợp lệ,0payment/0tiêu hao. Bug đã
được sửa và regression đạt; T18-K01 PASS. Giữ nguyên hiện tượngFAIL ban đầu.
