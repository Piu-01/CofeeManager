# G04-UI02 — Sửa và regression footer25/10/2026

Người dùng đãduyệt sửa/debug;regressionđúngảnhhưởng được thực hiện cùngđợt.
Chỉ sửaCheckoutDialog.Layout.cs:giữactions.WrapContents=false,đểAutoSizecủafooter
đođúngnhómCTA một hàng. Không sửaOrbitDialogLayoutmặcđịnh,BLL/SQL/role,
validation/Confirm/Reconcile hoặcbodybố cục.

## Kiểm chứng

.NET10WinForms,DeviceDpi120=125%,source chưa commit. BuildDebug/Release
0warning/0error vào `../runs/footer-fix-20261025/`. Đóng băng206file rồi chạy
regression bằngcùngfixture/expected cũ;chỉthay4DLL sangbản mới,khôngsửa testgốc.

| Kiểm tra | Actual | Kết quả |
|---|---|---|
| ComponentCheckout32kiểm tra |Validationtiền/method/reference,mixed/bank/0₫,busy/unknown,footer/back3size,profilegrid/dialogcũ,disposal |32/32PASS |
| Footer1280×720sau700ms |ConfirmInside=true,DPI120;chainactions/footermộthàng | PASS |
| Footer1366×768sau700ms |ConfirmInside=true,DPI120 | PASS |
| Footer740×700sau700ms |ConfirmInside=true,DPI120 | PASS |
| Source |206filekhôngđổi trongregression,sođợtUItrướcsửachỉ1fileLayoutđổi | PASS audit |
| SQL/stock/points |KhôngcólệnhSQL trongđợtsửafooter,nênkhôngtạolạidb/chốtđơnmanual |Khôngchạy lại;7/7SQLbridge trướcđó giữlịch sử |
| Chuột/phím vật lý,Esc/popup,draft/cache/nhiềubàn/món |Khôngthayhandler;chưađủthao tácfullapp |NOT RUN /khôngsuyPASS |
| Hìnhthức |Người dùngtựkiểm |SKIPPED |
| DPI100/150% |KhôngđổiWindowsScale |NOT RUN |

Artifacts: [component-results](../runs/footer-fix-20261025/component-results.json),
[bounds](../runs/footer-fix-20261025/bounds.json),
[runner-status](../runs/footer-fix-20261025/runner-status.json),final-manifest.
CácFAIL29/32vàbounds3FAIL trước sửa giữ nguyên tạicheckout-test/layout-probe25/10.
KhôngthayexpectedhoặctickPASS chỉtừbuild.

G04-UI02đãgiảiquyếttrongphạmviđosửafooter. T16-K11trở vềBLOCKED vì cònsearch,
nhiềubàn/dòng,queryresponsivenessPOSchưađủevidence. G04hiện33PASS/0FAIL/23BLOCKED.
Chưa nghiệmthu toànG04 vàkhôngtựghiPASS cácassertionEnter đangđiềutra.
