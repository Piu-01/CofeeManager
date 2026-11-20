# T30 — Kết quả nghiệm thu G08

02/11/2026 · **9 PASS / 0 FAIL trên 9 ID gốc**. Đợt test đã được người dùng duyệt. G08-B01 đã sửa/regression PASS; kết quả kế thừa8ca không bị ảnh hưởng và kiểm lạiK07, không gọi cả9ca đã chạy lại.

Môi trường: Windows x64, SQL Server localhost 17.0.1000.7 Developer, SDK 10.0.401; packageWinForms thực tải System.Windows.Forms/coreclr10.0.1326.50504. HEAD8a38f8691d37089feec149b71a8f5718d37167f3+workingtree; source thực được xác định bằng460 SHA256 trước/sau, không coi HEAD là toàn bộ bản đã chạy. Package archiveD1F7A782FB0C083DE02D30D0597DE4EC1AF68F138D17C36BF3143EBF88D8C54A.

Lượt cha: [acceptance_full_20261102_160135_579468c5](../runs/acceptance_full_20261102_160135_579468c5/manifest.json). [Source/package freeze](../runs/acceptance_full_20261102_160135_579468c5/freeze-audit.json). Source/migration/test gốc và package không sửa trong lượt; chỉ tài liệu/bằng chứng và fixture tạm trên DB test thay đổi.

| ID | Expected / thao tác chính | Actual | Kết quả | Bằng chứng |
|---|---|---|---|---|
| T30-K01 | Đúng DB nguồn; SQL backup và restore vào DB khác | Development marker đúng, schema 021; COPY_ONLY/CHECKSUM/VERIFYONLY; restore DB test mới online, nguồn ổn định, không ghi/nâng development | PASS | t30-operations.json; development-backup.json; restoredevelopment.json |
| T30-K02 | Tiền/kho/điểm/nợ/lương/trạng thái/nguồn khớp sau restore | Hash theo PK khớp45 bảng; đối chiếu riêng11 nhóm nguồn, không chỉ rowcount | PASS | t30-operations.json; snapshot trong development-backup.json/restoredevelopment.json |
| T30-K03 | SQLservice ghi được backup, đường dẫn server đúng; lỗi đường dẫn không được che | Backup thật thành công; existingfile và serverdirectory không tồn tại bị chặn; backup gốc vẫn verify, không đổi ACL/service | PASS | T30-K03.server-path-and-reject-overwrite trong t30-operations.json |
| T30-K04 | Clean không demo, đủ dữ liệu nền, bootstrapADMIN và login được |027/3 role/2PTTT/3systemexpense;0 bill/NV/KH/NVL/account trước bootstrap. SetupSQL identity riêng bootstrap1lần, Completed, lần2bị chặn; native package login g08.admin vào MainForm, dashboard0 đúng | PASS | t30-operations.json; t30-identities.json; native-transcript.json: native-clean-login |
| T30-K05 | Restore021/026→027, không đổi số dư/nguồn; rerun/checksum đúng | Hash nghiệp vụ trước/sau bằng nhau trên cả2 DB restore;027rerunAlreadyApplied; bản sao checksum cố tình sai bị chặn, file migration gốc không đổi | PASS | upgrade21.jsonl; upgrade26.jsonl; T30-K05 trong t30-operations.json |
| T30-K06 | Runtime tối thiểu, setup/migration tách identity |2 SQL login test thật riêng; runtimeCafeManagerApp,sysadmin0/db_owner0,publicreadOK;DDL/AccountDML/InventoryDML/setup bị chặn,CanBackup0. RevokeSetup làm setupidentity không còn được setup | PASS | t30-identities.json,identity-resources.json; không tạo Windows user hoặc đổiMixedMode |
| T30-K07 | Missing/syntax/offline/chứng chỉ: đúng nguyên nhân, không fallback/lộ secret | Regression B01:4nhánh native PASS;258 và−2146893019 nayConnection,thôngbáohướngdẫnđúng;SQL/TLSvẫnchặnkếtnốisai | PASS | [bug-regression-results](bug-regression-results.md); lượt trước FAIL giữ ởtest-results-acceptance-20261102.md |
| T30-K08 | SQLbackup/restore thay copyMDF/LDF đang mở; hướng dẫn đúng | EngineBACKUP/RESTORE đã chạy; docs nêu filesystemserver/serviceaccount/VERIFYONLY khác restore; không copy file đang mở | PASS | t30-operations.json và docs/operations/backup-restore.md |
| T30-K09 | Không credential thật/localconfig/backup nhạy cảm trong source/package công khai | Releaseinventory0 issues;gittrackedkhôngDPAPI/backup/localconfig; secretSQLtạm chỉmemory/DPAPI ngoài source và đã xóa; mock công khai được ghi nhãn | PASS | release-inventory-acceptance.json;cleanup-audit-final.json |

Các đường evidence không phải link trong bảng đều nằm trong thư mục lượt cha nêu trên. [Chi tiết7 subcase vận hành](../runs/acceptance_full_20261102_160135_579468c5/t30-operations.json), [6subcase identity](../runs/acceptance_full_20261102_160135_579468c5/t30-identities.json), [native](../runs/acceptance_full_20261102_160135_579468c5/native-transcript.json).

Bootstrap concurrency/rollback được kế thừa theo T06 khi source auth không đổi, không tuyên bố chạy lại toàn bộ G02. Không tạo Windows user/kiểm GUIsetup với một Windowsidentity mới; setup role được kiểm bằng SQL login test tách runtime/migration và login clean được kiểm native. Không dùng DBuserWITHOUTLOGIN thay chứng cứ runtime kết nối thật ởK06.

Hình thức SKIPPED theo phân công;100/150%DPI NOT RUN, không đổi Scale. T30-K07 đã PASS theo regression được duyệt; G08 vẫn Đang làm vì13ID khác còn BLOCKED.


## Regression sau sửa G08-B01

[Biên bản](bug-regression-results.md) và manifestregression_b01_20261102_171805_94f20337 xác nhận142unit/4DAL-BLL/7nativePASS,source/packagegiữcốđịnh,cleanupđủ. Phần môi trường/packageởtrên làlượtacceptancegốc;packageB01mớiDBE592A6…đượckiểmriêng. Lịch sử8PASS/1FAIL giữ tại [test-results-acceptance-20261102.md](test-results-acceptance-20261102.md).

