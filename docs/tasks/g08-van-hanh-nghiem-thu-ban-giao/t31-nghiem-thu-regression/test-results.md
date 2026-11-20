# T31 — Kết quả nghiệm thu xuyên suốt

02/11/2026 · **1 PASS / 0 FAIL / 10 BLOCKED trên 11 ID**. Các subcase chạy được đều lưu kết quả thật; không coi7 subcase SQL hoặc128unit là11ID đãPASS.

Môi trường/source/package theo [lượt cha](../runs/acceptance_full_20261102_160135_579468c5/manifest.json) và [freeze-audit](../runs/acceptance_full_20261102_160135_579468c5/freeze-audit.json). Source460file và archivepackage không đổi. SQLServer17.0.1000.7,SDK 10.0.401,Windows x64,Runtime thực 10.0.13. HEAD8a38f8691d37089feec149b71a8f5718d37167f3+workingtree.

| ID | Expected | Actual / giới hạn | Kết quả |
|---|---|---|---|
| T31-K01 | Native login→bàn→2dòng/ghi chú→ưu đãi→2TT→in→tồn/điểm/bàn | SQL:2MaDong/50000/TT20000+30000,tiêuhao đúng/replay1lần. Native:loginADMIN/POS/dialogmón mở; không nhập được ghi chú cófocus xác minh do cache/geometry công cụ. Chưa nativekhách/ưuđãi/in/checkout | BLOCKED |
| T31-K02 | Native nhập/trả ngay/nợ2lần/report/nguồn | SQLnhập10000/trảngay2000/nợ8000,trả3000+5000,còn0,tồn+10,4nguồnchiliênquanđúng; native nhập/2lần trả/reportnguồn chưa đi trọn do modalinput | BLOCKED |
| T31-K03 | Xuấthủy→snapshotstale→từchối→lấy lại/chốt1lần | SQL2phiếuxuất,stale52903,KKmới/replaykhôngghi2lần đúng; same runtimeconnectionerrorstate0:0/toànbảngbấtbiến. Native scope/đếm/nháp/footer chưa đủ | BLOCKED |
| T31-K04 | Caquađêm→chốt→chi→chặntrùng/report | SQL8giờ×35000=280000,1bảnglương/1phiếuchi,replayđúng; native editor công/lương và report ngày chưa đủ | BLOCKED |
| T31-K05 | Đơn giao trả→chuyển2bước→tài chính giữ | SQL25000+ship5000=30000,Chogiao→Đanggiao→Đãgiao,financialhashkhông đổi. Chỉ hiện trạng theoT18 đãduyệt, khôngtimelinegiả. Nativeflow chưa đi trọn | BLOCKED |
| T31-K06 | Trước/saucommit lỗi từng7usecase, đốichiếu/khôngretrymù | Có staleKK lỗi thực/rollback cùngconnection và replay, nhưng không phải faulttrước/saucommit đủ7usecase. Chưa có điểm fault saucommit được xác minh trên native/harnesscốđịnh; khôngngắtSQLservice hoặc suy từreplay | BLOCKED |
| T31-K07 | ADMIN/CASH/STORE,logout/khóa/nghỉviệc/đổirole/phiêncũ/callback | CASH/STORElogin/BLLFinanceDenied/khóaphiên SQLPASS;nativeADMINđúng/sai,mock clean login,logouttrảLogintrốngPASS. Native3 role/khóa/nghỉviệc/đổirole/logoutđangasync chưa đủ | BLOCKED |
| T31-K08 | Nativephím/modal/nháp/pending/125%/latequery/states | Reportempty/error/retry thậtPASS;dialognative mở vàfieldquantity cóselection; chỉmục note khôngởcache vàclick/Tab khôngxácnhậnfocus sau recovery. Chưa đánhgiá mọi input/IME/latequery/DPI125% kỹthuật | BLOCKED |
| T31-K09 | Đối chiếu trực tiếp đủ nhóm tiền/kho/điểm/nợ/lương/trạng thái/nguồn sau E2E |7kiểmđốichiếu trực tiếp trong lượtR3PASS:bill/line/TT/tiêuhao;kho mọiNVL;điểm;chi/lương;liênkết;bàn;cùngruntimeconnection52903và0:0. Expected tính trước, khônglấyactual làmexpected | PASS |
| T31-K10 |325ID/44UX có evidence phù hợp, không bỏ ca bắt buộc | Đủ325ID duy nhất/44UX; lịch sử giữ, nhưng G04/G05/G06/G07 còn ca native chưa đủ và gatecuốiT32 chưa đạt. Không gọi325PASS | BLOCKED |
| T31-K11 | Không lỗi trọng yếu/luồng chính đủ bằng chứng | Không thấy sai tiền/kho/nợ/lương trong phạm viSQL đã chạy; G08-B01 UXkết nốiFAIL vànative/fault chưa đủ. Chưa chứng minh toàn bộ app hết lỗi trọng yếu | BLOCKED |

## Lệnh, dữ liệu và bằng chứng

- `tools/database/Test-G08.ps1 -RunAcceptance`: [manifest](../runs/acceptance_20261102_160708_43592f44/manifest.json), [7 subcase SQL](../runs/acceptance_20261102_160708_43592f44/Demo-probe.json),2DBtạo/2dọn/0giữ,sourcekhông đổi. Không dùng statusSucceeded của runner để ghi29IDPASS.
- Lượt đối chiếu cuối [reconciliation_r3_20261102_165150](../runs/acceptance_full_20261102_160135_579468c5/reconciliation_r3_20261102_165150/manifest.json): [7kết quả](../runs/acceptance_full_20261102_160135_579468c5/reconciliation_r3_20261102_165150/t31-direct-reconciliation.json), [before](../runs/acceptance_full_20261102_160135_579468c5/reconciliation_r3_20261102_165150/t31-balance-before.json), [after](../runs/acceptance_full_20261102_160135_579468c5/reconciliation_r3_20261102_165150/t31-balance-after.json).
- Expected:2bill/3line/3TT,+80000TT,4tiêuhao;NVL01+10−2xuất−1KK−.054=+6.946,NVL03−.030,NVLkhácgiữ;billkhôngKH nênđiểm/lịch sửkhông đổi;chi+290000=2000+3000+5000+280000,4phiếuchi/khôngThucheckoutthứ2;1lương+280000;1chinhập/2linknợtổng8000/1chilương,bànB0001trống. Saulỗistale52903,cùngconnectionruntime@@TRANCOUNT/XACT_STATE0:0 vàhashmọibảngkhông đổi.
- Unit:128/128PASS,0skip, [TRX](../runs/acceptance_full_20261102_160135_579468c5/g08-unit-filtered.trx). `dotnet test ... --no-build --no-restore --filter FullyQualifiedName!~CafeManager.Tests.Integration.&FullyQualifiedName!~CafeManager.Tests.G02.`; excludes integration suites cần fixture riêng, không excludes AuthRules/CatalogRules cóGroupG02. Lệnh unfiltered trước đó bị chặn testhost trongsandbox/đã ngắt, giữlog/unit-unfiltered-blocker.json, không coi là bug sản phẩm hoặc kết quả integration.
- [Native transcript](../runs/acceptance_full_20261102_160135_579468c5/native-transcript.json): lỗi geometry được recovery bằng snapshot phục vụ thao tác; ownedmodal note báo `element178...not available in cached app state`, focus vẫnquantity sau recovery. Không tiếp tục gõ khi chưa xác minhfocus, không quy lỗi này thành bug sản phẩm.
- Report fault được người dùng cho phép riêng sau auto-review từ chối lần đầu; [fixture](../runs/acceptance_full_20261102_160135_579468c5/report-fault-fixture.json): DENYchỉuserDBtest30.23giây,finallyREVOKE/CanExecute1. NativehiệnLỗi tải/Chưacậpnhật, không0giả; retry saurestorePASS. Không thayquyềndevelopment.

## Lượt lệnh đối chiếu không hoàn tất

Lệnh đầu dùng suffixdecimal C#`m` không hợp lệPowerShell, dừng sau2check vàcleanupDB; R2đã qua5check nhưng querytrảnợ dùng cộtSoTienkhôngcótạiTHANHTOAN_CONGNO. Giữnguyên cảhai script/log/results, không coi chúng là FAILsảnphẩm. Mỗi lần kếtthúc/dọn rồi mở lượtmới; R3dùng đúngSoTien từPHIEUTHUCHI quaMaTC, expected8000vàmọidelta không đổi. Không sửa source/test/migration gốc hoặc âmthầm chỉnhexpected né lỗi.

Hình thức SKIPPED;100/150%DPI NOT RUN, không đổiScale. Không dùng screenshot chức năng để nghiệm thu hình thức. [BugG08-B01](../t30-backup-restore-cai-sach/bug-report.md) chờ duyệt đợt sửa; chưa sửa trong test.


## Sau regression G08-B01

Bug mapper đã sửa/regressionPASS, xem [biên bản B01](../t30-backup-restore-cai-sach/bug-regression-results.md). T31 vẫn1PASS/10BLOCKED; không nâng các ca native/fault hoặc suy đã test lại toàn bộ từ regression mapper.

