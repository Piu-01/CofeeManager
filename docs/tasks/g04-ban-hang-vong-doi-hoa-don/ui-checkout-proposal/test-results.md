# Đợt test Checkout UI —25/10/2026

Người dùng đã duyệt đợt test. **Chưa đạt: có lỗi kỹ thuật footerG04-UI02**.
Không sửa source sản phẩm/migration trong lượt kiểm. G04hiện33PASS/1FAIL/22BLOCKED;
T16-K11 chuyểnBLOCKED→FAIL, các caG04khác không tự nângPASS từ component.

## Môi trường và source

.NET10WinForms,DeviceDpi120=125%;test đúngDLLDebug đã đóng băng:
B6F6E7E9C205C4E61EBE1095DB89322C7489A50856E3D21F20FB4E7F766801E3.
Các harnessbuild riêng rồi copy4DLLWinForms/Business/DAL/Models đóng băng sang
output để không thay bản test. Source206file trước/sau không đổi.

SQLServerlocalhost,DBriêng
CoffeeManager_Test_G04_20261025_141901_db21cda5_checkout,schema016,marker
20261025_141901_db21cda5. Fixture lấybaseline/seed/migration001–016,SP00181000,
actorCASHtestNCU01 đăng nhập quaAuthService/runtime login thật;KM0%/100% riêng.
DBtest này cómãHD20250411 trùngDBmanual nhưng làcatalogkhác;khôngnhầmevidence.
HD20250411trongDBmanual vẫnchưaTT,tổng81000,0payment sau kiểm.

## Kết quả đã chạy

| Plan / ID liên quan | Actual và phạm vi chứng minh | Kết quả |
|---|---|---|
| C01/T16-K03 |Dialogthật sinhallocation81000,kháchđưa100000,thối19000;gọiSalesService trực tiếp ghi1payment81000,81điểm,tiêuhaoNVL01=.018;replaykhôngđổi snapshot | PASS component/API;chưa physicalConfirm |
| C02/T16-K04 |Thiếu80000,abc,overflow30số9,âm/lẻ,methodtrống/PT010/unknown,phân bổthiếu/dư,reference101ký tự:Confirmdisabled/notice,inputgiữ | PASS component;chưa đủfullID |
| C03/T16-K04 |Busy/unknownkhóagrid/Back/Confirm vàbậtCheckđúng;popupbỏnháp/Esc bằngchuột/phím thật chưa thao tác | PARTIAL/BLOCKED |
| C04/T16-K05 |Sửaamountđangeditcậpnhậtsummary;commitamountđúng;comboboxbindPT002;referencequádàibịchặn;APIreplaykhônglặp. NativeTab/Enter/doubleclick chưa chứng nhận | PARTIAL/BLOCKED |
| C05/mixed/0₫ |Cash31000+bank50000,kháchđưa50000→thối19000,SQLđúng2payment;bankonly81000bỏquaabcởkháchđưa;0₫chốtpaid0payment | PASS component/API |
| C06/T16-K06 |Phiênthứ2chốt,UIReloadPreview đọcpaid/ResultOrderđúng,snapshotstock/points/paymentskhôngđổi | PASS handler/SQL;fullclickstalechưa chạy |
| C07/T16-K07 |UIReconcileđúngrequestđãcommit,trảpaid,tiềnđưa/thối=null,snapshotkhôngđổi. Unknownđượcgánđểtesthandler,khôngphảifaultmấtresponse thực | PARTIAL/BLOCKED |
| C08/T16-K11 |1280×720,1366×768,740×700ở125%:Confirmngoàivùngclient;đolạisau700msvẫnFAIL | FAIL G04-UI02 |
| C09/shared |DefaultOrbitDialogLayoutconstructor,Menu/Inventory/Catalogsreadonly vàPaymenteditableđạt;editorinput/code/amountđượcbindđúng | PASS smokecomponent;khôngnghiệmthuCRUD/visual |
| C10/lifecycle |5vòngconstructor/disposeđạt;chưađủweakGC/particleburst/logoutthựctế | PARTIAL/BLOCKED |

UnitCheckoutRules+SalesRules hiện có:**13/13PASS**.
Lượtcomponentchính:**29/32PASS**,3FAILfooter. SQL/UIbridge:**7/7PASS**.
Không cộng các countprobe này thành56caG04;khôngsuyphysicalUIhoặctiền từmock.

## Điều tra footer và lịch sử fixture

[Bounds sau ổn định](../runs/checkout-layout-probe-20261025/bounds.json) ghi cả3size
ConfirmInside=false. Ví dụ1280×720:clientY198,height673(bottom871),ConfirmY841,
height50(bottom891). Chain:actionsFlowLayoutPanelwidth238,height100,preferred378×50;
footerheight50. Hàngcheck/confirmwrap2dòng nhưngfooterchỉ1dòng.
XemG04-UI02 tạiT16/bug-report.md;khôngtựsửatrongđợttest.

Editorprobeđầucó3/5PASS,2rawFAIL. Đãgiữnguyênfile/input/expectedvàtạoobservations
ởcheckout-editor-followup-20261025:1assertvềmàueditor(ngoàiphâncôngagent,không
dùngđểnghiệmthuhìnhthức),1assert“thoáteditmode”. Testchứcnăngđộc lập
checkout-editor-functional-20261025đạt4/5,lạiFAILassertCellEndEdit.
checkout-key-observation-20261025 quan sátConsumed=True,EndEvents=0,
BoundAmount=82000,Editor=82000,noticebáodư1000;chưađủcăn cứgánbughandlerEnter.
Không chuyển nhữngFAILrawnày thànhPASS hoặc sửaexpectedđểné;keyboardthật và
hànhvilúcEndEditgiữBLOCKED,kiểmlạisaukhilayoutđạtkhảnăngthaotác.
KhôngkhẳngđịnhEntertựchốttiền,khôngcóbằngchứngđó.

Hìnhthức/cácmàuchữ,hover/spacing vàsosánhảnh:SKIPPED theo phâncông;
100/150%NOT RUN,khôngđổiWindowsScale hoặcépDPI.

## Artifacts và cleanup

- `../runs/checkout-test-20261025/`:component32checks,unit13TRX,setupmanifest,
  cleanup.ps1,manual-order-preserved.json.
- `../runs/checkout-sql-test-20261025/results.json`:7caSQL/UIbridge vàpayload thực.
- `../runs/checkout-layout-probe-20261025/bounds.json`:metricskỹthuậtfooter.
- `../runs/checkout-editor-*/`, `../runs/checkout-key-observation-20261025/`:
  từngfixture/follow-up,bảnraw vàcăn cứphânloại.

DBCheckouttest đãDROPđúngtên/marker sau lưuevidence,actorfixtuređượcxóacùngDB;
giữDBmanual đểngười dùngtiếp tụcG04. KhôngđổiDBdevelopment/backup/service,
SQLserverlogin,accountngười dùng,WindowsScale hoặccommit/push.
