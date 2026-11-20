# T32 — Kết quả kiểm bản publish

02/11/2026 · **6 PASS / 0 FAIL / 3 BLOCKED trên 9 ID**. Bản vẫn là ứng viên; chưa Hoàn tấtT32/G08 vì gatebypass/native và nghiệmthu xuyênluồng chưa đủ.

Package cố định: artifacts/g08/20261102_155435_c6b76b42/candidate.zip, SHA256D1F7A782FB0C083DE02D30D0597DE4EC1AF68F138D17C36BF3143EBF88D8C54A;win-x64/framework-dependent,schema 027. Exe thử trong thư mục unzip khác, không chạyDebug hoặcVisualStudio. [Lượt cha](../runs/acceptance_full_20261102_160135_579468c5/manifest.json), [freeze](../runs/acceptance_full_20261102_160135_579468c5/freeze-audit.json), HEAD8a38f8691d37089feec149b71a8f5718d37167f3+workingtree460 hash,source/package không đổi.

| ID | Expected | Actual | Kết quả |
|---|---|---|---|
| T32-K01 | Release đúng kiến trúc, chạy ngoài thư mục build không cầnVS | AppWinForms x64 từunpack-integrity/package/app mở Login→ADMINMainForm/POS/report,khôngVS; archive94 file khớphash. Máy khác chưa thử, không suy từ cùngmáy | PASS |
| T32-K02 | Framework-dependent chạy vớiDesktopRuntime đúng | Runtime 10.0.13 có trênmáy;processmodulecoreclr/System.Windows.Forms thực 10.0.1326.50504,appchạythật. Self-containedkhôngchọn,N/A cólýdo,khôngtuyênbốđãthử | PASS |
| T32-K03 | Cấuhìnhdemo/clean/bootstrap,login/bán/in | Clean/demo loginADMIN vàPOS/dialogmónPASS;bootstrapSQL identityriêng/Completed đã kiểmT30. Chưa đi hếtcheckout/in hoặc nativeBootstrapWindowsidentityriêng do modalinput; không thaypreviewchoin giấy | BLOCKED |
| T32-K04 | Khôngsecret/backup/dữliệuriêng/pathmáytácgiảtrongpackage |95 file (94payload+manifest);inventorymetadata9 assembly0 issues,licensecòn;PDBCodeViewđãPathMap;khôngconfiglocal/DPAPI/backup,secretSQLtestkhôngvàopackage | PASS |
| T32-K05 | Smoke5nghiệpvụ trênexe package,đốichiếuSQL | Login/POS/dialog/reportempty/error/retry/Logout nativeđã chạy;5luồng ghiquaUIpackage chưa đủ do ownedmodalinput. SQL/BLLprobe không thaynative | BLOCKED |
| T32-K06 | Restorebackup rồi chạy cùng dữ liệu khôi phục không sửa source | Nativepackage dùngDBrestorev26→027,mapSQLruntimeidentitytối thiểu;dashboard đúngPaidToday0/Open1/Nợ5500000,POSloadđúngmenu. ĐốichiếuSQLkhớp,source/packagekhông đổi. Full5flow vẫn ởK05BLOCKED | PASS |
| T32-K07 | README/thứ tự/version đúng bảncài | Chínhscript trongpackage cài2 DB mới:Clean027/0 bill/0 NV/Pending;Demo baseline→seedduynhất→027/10 bill/10 NV/Pending. Migrations/checksum/versionSQL17/runtime10 đúng manifest;2DB đãdọn | PASS |
| T32-K08 | Sourcecóstatus/testresults,migrationthậtvàgiớihạncôngkhai |29ID cókết quả/linksraw;325ID/44UXgiữlịch sử,không tựtickHoàntất;developmentthực021khôngbịnâng;migrations001–027đã chạytrênclean/demo/restore | PASS |
| T32-K09 | Chạytrọnkịchbảndemo,cácnguồnđúng,khôngquảngcáotínhnăngngoàiscope | docs/operations/demo.mdđủ5nguồn/giớihạngiao/lợinhuận/API;SQL5luồngđã chạy,nhưngdemoWinFormsđầucuối/in/reportnguồnchưa đủ | BLOCKED |

## Evidence thực

- [Native](../runs/acceptance_full_20261102_160135_579468c5/native-transcript.json), [runtime module](../runs/acceptance_full_20261102_160135_579468c5/native-loaded-runtime.json), [restore dashboardexpected/actual](../runs/acceptance_full_20261102_160135_579468c5/restored-package-dashboard.json).
- [Inventory audit](../runs/acceptance_full_20261102_160135_579468c5/release-inventory-acceptance.json), sourcehooks/launcher/types vàpositiveLoginForm/AuthService/AuthRepository. [Package installthật](../runs/acceptance_full_20261102_160135_579468c5/packaged-install-results.json), package-Clean-install.json/package-Demo-install.json cùngthưmục.
- Build/publish đãcótrongtechnical-checks củaứngviên, khôngbuild/publishlại hoặcpatchfile giữalượtnghiệmthu. NU1900doauditfeedkhôngtruycậpđược giữcôngkhai,khônggọiauditvulnerabilitysạch.

## Điều kiện bổ sung bỏ Login TEST

Source/launcher/profilemetadataDebug/Release/publish khônghelper/nútbypass;Loginbìnhthườngđúng/sai vàlogoutthậtđã chạy. SQLCASH/STOREkhóaphiên/quyềnđúng. **Chưa PASS gatecuối**:native3 role/thu hồi/callback/trạngtháiunknownchưa đủ;khôngtruyđượcđủtênbiếncũtừhồsơđểchứngnhậncaenvlegacy,khôngbịatênrồiđánhPASS. Mọi evidencegỡ24/10giữlàmhistory,không thaykiểmbảncuối.

G08-B01 ởT30-K07 vẫnchưa sửa. Nativebịblocker côngcụkhôngphảichứngcứsảnphẩmcrash hoặcbypassquyền. HìnhthứcSKIPPED theo phâncông;100/150%DPI NOTRUN,không tựđổiScale;125%layout/phímcònphầnchưa đủ.



## Sau regression G08-B01

T30-K07 đã PASS theo [biên bản B01](../t30-backup-restore-cai-sach/bug-regression-results.md). G08 hiện16PASS/0FAIL/13BLOCKED. Bảng6PASS/3BLOCKED T32 bên trên được kế thừa trong phần không bị ảnh hưởng, không phải đã chạy lại9ID trênpackage mới; gatebypass/native vàK03/K05/K09 vẫnBLOCKED. Package/hash hiện tại xemrelease-candidate.json; packageD1F7…ởtrên làhistory.

