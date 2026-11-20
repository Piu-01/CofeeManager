# Đợt nghiệm thu G08 — đã được người dùng duyệt

Requirements/bộ plan đã duyệt không đồng nghĩa đã duyệt đợt này. **Người dùng đã duyệt đợt test ngày02/11/2026; đã chạy trên source/package cố định:15PASS/1FAIL/13BLOCKED, xem report.md và test-results từng task.** Thực thi chỉ sau người dùng đồng ý đợt test; test lần lượt T30→T31→T32 với source/harness/migration/package cố định. Nếu sửa harness, kết thúc lượt, giữ evidence và tạo lượt mới; không thay expected né lỗi.

## Phạm vi/lệnh và nguồn expected

1. T30-K01–K09 theo [planT30](t30-backup-restore-cai-sach/plan.md): backup nguồn development đã xác minh/không writer, restore DBtestmới, snapshot parity từng bảng, clean không seed/demo tách biệt, bootstrap một lần, upgrade021/026→027/rerun/checksum, quyền identity thực, config/chứng chỉ và auditsecret. Script bảo vệ đích/marker/manifest; không nângdevelopment hoặc restoređè.
2. `tools/database/Test-G08.ps1 -RunAcceptance`: tạo clean/demo test có manifest, build probe, bootstrap/login BLL; Demo chạy subcase SQL trong E2EProbe. Từng subcase có expected/actual/Status, thất bại không tự sửa và không dừng các case độc lập. Script này không tự bao phủ29ID hoặc325ID; native/fault/backup nguồn development/package cần các bước riêng. Không gọi tất cả runner cũ để tăng số PASS.
3. T31-K01–K11 theo [nativeguide](t31-nghiem-thu-regression/native-test-guide.md), [ma trận325](acceptance-matrix.md) và [UX44](ux-matrix.md):5E2E, errors trước/saucommit7usecase,3role/phiên/logout/khóa, keyboard/modal/pending/latequery/125%, đốichiếuSQL và ca còn thiếu nhóm trước theo ảnh hưởng. Thiếu điều khiển/điểmfault thìBLOCKED, không giảlậpPASS từ code.
4. T32-K01–K09 theo [publishguide](t32-publish-ban-giao/native-publish-guide.md): unzip thưmục mới/hash/runtimethực, clean/demo/restore,5luồng trên đúngexe, inventory/license/secret, tài liệu/version vàdemo. Kiểm source/DLLDebug/Release/publishhếtbypass+nativebiếncũ/login/logout/thu hồiphiên/quyền làgatebổsung.

Expected nghiệpvụ:SP00125000/dòng,côngthứcNVL01=.018,NVL03=.010;nhập10×1000/trảngay2000→nợ8000,trả3000+5000→0;nhânviêngiờ35000×8→280000 với khoản khác0;giao25000+5000→30000 khi không ưuđãi. Nhánh khách/KM/điểm và report phải ghi chính sách/fixture/thờiđiểm thực trước chạy, không đọc actual làm expectedduynhất. Seed04/2025 khác ngày tạo mới.

## Môi trường và bằng chứng

- SQLServer thật localhost, DB riêng;ghi versionengine/schema/checksum/SDK/runtime/OS/sourcehash/packagehash vào manifest. Không SQLite, không ghi fixture vàoDB thật, không reset WindowsScale hoặc SQLservice/mode.
- Ghi inventory tài nguyên trước/tạo/dọn/giữ:exactDBname/runID/marker/principal/file,cleanupfinally;khôngDROPwildcard hoặc xóa backup cũ. Nguồnbackupdevelopmentchỉđọc và backupmới giữriêng cólýdo/hạngiữ/kếhoạchdọn;không commit .bak/secret.
- test-results.md cạnh từngtask ghi từngID/subcase dữliệu/lệnh/thao tác/expected/actual/PASS-FAIL-BLOCKED-NOTRUN và linkraw. reportG08 tổnghợp29IDvàgatenativebypass,linkbugtask sởhữu;khôngxóa lịch sử. Snapshot credential chỉ hash tổng,không export hash mật khẩu/rawaccount.
- HìnhthứcSKIPPEDtheophâncôngngườidùng;DPI100/150NOTRUN,125chức năng/layoutkỹthuật. Máy khác/máyin/chứngchỉ/identity/toolnativekhôngcó→ghi rõ phầnthiếu,khôngsuyPASS.
- Cóbug:ghiFAIL/điều tra,tiếp tục cađộc lập;trìnhđợtbugduyệttrướcsửa. ChỉchốtHoàntấtkhiđủđầu ravàca bắtbuộcPASS,điều kiệnT32bypassđạt;khôngchốtG08từpackage.


