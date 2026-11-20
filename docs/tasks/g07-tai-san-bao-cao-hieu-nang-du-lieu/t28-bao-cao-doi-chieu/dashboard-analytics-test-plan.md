# Đợt kiểm thử Tổng quan ngày/tháng — đã duyệt

Trạng thái hiện tại:[regression](dashboard-analytics-regression-results.md) đãchạy56subcheckPASS/1FAILUI;fixtureG07-DA01đãđóng,G07-DA02chờduyệtsửa. Phạmviđượcdướiđâygiữlàmtruyvết,khôngđánhđồngvới325ID hoặcPASSLatestWorkingTree.

10/11/2026. Plan triển khai đã được người dùng duyệt. Tài liệu này là phạm vi đợt test riêng, không ghi PASS từ build hoặc đọc source. Giữ nguyên325ID/T01–T32 và lịch sử T28.

## Phần đã chuẩn bị

- `database/tests/g07/dashboard-analytics-fixture.sql`: fixture baseline demo trước migration,61SKU/62bill trong kỳ,2dòng×2PTTT,giảm/ship/phí,ngày lập khác ngày trả,mốc Until,2kỳ đối chiếu,menu đổi giá sau bán. Expected ngày15/01/2026 trước12:00:127.500₫/62bill/127ly,chi1.000₫;MTD137.500₫,kỳ trước20.000₫. Bill đúng12:00 chỉ có trong report cả ngày,không nằm trong snapshot trước12:00.
- `tools/database/Test-DashboardAnalytics.ps1 -RunApprovedTests`: tạo clean/demo riêng có exactname+marker,baseline→001–027→028/rerun,so schema/index/dữ liệu trước/sau028,freeze source,probe và cleanup đúngDB/marker. Runner chỉ tự ghi kết quả **subset**,không tuyên bố đủ14nhóm.
- `tools/CafeManager.DashboardProbe`: typedDAL/BLL/SQL,so mốc lịch,Top5,paging,quyền và API cũ;WinForms thật để kiểm binding/mode/refresh/chartkeyboard/bounds/ownedsource/hide-show tại125%. Không chụp ảnh hoặc nghiệm thu hình thức.
- Ca đồng thời,chi liên kết,lỗi cưỡng bức và10.000bill cần phiên kiểm bổ sung trong cùng đợt đã duyệt;không coi việc có runner là đã bao phủ các ca này.

## Ma trận chạy

Đợt test đã được duyệt và đã chạy một phần; kết quả tại [biên bản](dashboard-analytics-test-results.md), G07-DA01 chặn phần bổ sung. Bảng dưới giữ phạm vi gốc, không phải kết quả PASS. Phạm vi nghiệm thu/expected chi tiết giữ nguyên DA01–DA14 trong [plan](dashboard-analytics-plan.md#8-bộ-kiểm-thử-dự-kiến--chờ-duyệt-đợt-test-riêng).

| Nhóm | Cách thực hiện và chứng cứ |
|---|---|
|DA01|Runner clean/demo/rerun;schema tables/columns/computed/index vàhashmigration001–027;Models so danh sáchfile/định nghĩa trước-sau;chỉ2procedure mới/GRANT/version. Không suy toànModels hash không đổi nếu có thay đổi độc lập trong workspace.|
|DA02–04|Probe lịch sử qua@At,tính tay,fanout,biênUntil,MTD,cùng giờ,31/03,29/02,không phát sinh/thiếuavg. Bổ sung00h vàđầu tháng với@At;không đổi clockWindows.|
|DA05|Fixture chi1.000/2.000/500;tạo riêng trongDBtest các nguồnnhập trả ngay/trả nợ/chi lương qua procedure/service thật;đối chiếu SUMMaTC một lần. Chốt lương/khấuhao không tạochi bổ sung. Ghi mãphiếu vànguồn liên kết.|
|DA06|Probe61SKU,Top5X0002/X0001/X0003/X0004/X0005,127lytoànkỳ,giásnapshot1000khácmenu999999;thêm tên dài/quantityties trong fixture test riêng khi cần.|
|DA07|Probe đối chiếu4predicate trực tiếp ở2mode. Kiểm thêm biến thể giaoopen/cancel/finished vàNVL0 khácđơnvị qua contract;không sửa tồn trực tiếp sau mởsổ.|
|DA08|Probe62nguồn50+12 khôngtrùng;channel/product/expense;nativeInvoice/Finance/Debt ownedmodal;Materiallist không bậtXemchi tiết. Snapshotbounds giữ nguyên khi mở nguồn.|
|DA09|ADMIN/CASH/STORE ởBLL+SQL;runtime khôngSELECTHOADON. Thêm đổi role/khóa/logout rồi gọi lại,cảanalytics/source;không chấp nhận ròaggregate.|
|DA10|SQLServer2session cùngDBtest:chuẩn bịđơnmở/phiếuchi qua service;startgate để checkout hoặc createchi chạy đồng thời20lần đọc;capture timing/error1205/timeout vàđối chiếu từngsnapshot card=trend=channels. Saucommit đọc lại phải khớp nguồn;đối chiếu tiền/tồn/điểm,receipt vàXACT_STATE/@@TRANCOUNT từ connectionmới. Không retry để chedeadlock.|
|DA11|Probe cancel/hide-show. Bổ sung khóaHOADON bằngsessiontest riêng để giữquery A,đổi modeB,rồi nhảkhóa:chỉ kỳcuốiđược hiểnthị. Connectionkhôngtồn tại/timeoutvàschema027phải hiệnlỗi/—;dispose/logouthủyrequest,khôngsố0giả.|
|DA12|ProbeWinForms125%+thao tácchức năng:Tab/arrow/Enter,tooltipfulldate,mở/đóng/lồngsource,4/2/1KPI và2/1chart,1280×720/1366×768/panehẹp;nhãn dài/tiền18chữ số kiểm bounds kỹthuật. HìnhthứcSKIPPED,user tựkiểm;DPI100/150NOTRUN,khôngđổiScale.|
|DA13|DựngDBbenchmark riêng cómarker,10.000bill qua contract đã cóởPerformanceProbe;áp028 vàcaptureIO/TIME/timingSQL+UI cho2mode,cold/warm vàđọcđồngthờighi. Ghi actualtiming/lock,không lấy sốđoT29cũ thay cho028 hoặc thêmindex tự động.|
|DA14|ProbeReportSalescũ full15/01=137.500₫,DashboardAsynccũ;chạy regression report/nguồncũ vàstartupCASH/STORE/login/logout theo ảnhhưởng.|

## Thực hiện và lưu kết quả

1. Sau duyệt test:chốtSHA256source/harness/migrations/Models;cấm sửa trong lượt. Khi workspace có cậpnhật độc lập đang diễnra,ghi thờiđiểm/schema/build chính xác rồi mớifreeze.
2. Chạy runner subset và các phiên bổ sung trong ma trận. SQLServerlocalhost thật,DBtestriêng;không chèn/sửa dữliệu trêndevelopment đểtest hoặc làmđẹpchart. Cần giữDBchoGUI/concurrency thìdùng phiên riêng cómanifest+marker,có cleanupđúngđích.
3. Lưu `dashboard-analytics-test-results.md`:mãca,môitrường/hash/input/lệnh/expected/actual/evidence/PASS/FAIL/BLOCKED/NOTRUN. Bugghi tạiT28/tổnghợpnhóm,cần duyệtđợtbug trướcfix;khôngvátronglượt.
4. Chỉ sau khi testđạt/khôngcònbugchặn,mớibackup/VERIFY/áp028development vàbuildF5 nhưmục9plan. Hìnhthứcdo usertựkiểm;khôngcoi trạngtháiSKIPPEDhìnhthứclàblockerchức năng.
