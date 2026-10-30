# Kết quả test Đơn đang mở — 25/10/2026

Người dùng đã duyệt đợt test riêng. Đúng DLL UI
F9667F577E484EB766A4FA8C20460162A2C8F55071BA55B7642A4BA5DDE2B0AF;
208 file sản phẩm giữ nguyên trong toàn lượt. Build sản phẩm Debug/Release sạch
kế thừa pha triển khai. Không sửa sản phẩm, expected hoặc migration trong test.

## Kết quả đã chạy

| Phần | Actual | Kết quả |
|---|---|---|
| Dialog thực + SQL | Form Darkmode, AuthService thật, 51 đơn qua BLL trong DB test: tìm theo khách/mã, trang50/1, rỗng, pagevượtphạmvi, chọn mã, loading/lỗi, responseđảo thứtự, bounds/metrics |59/59 PASS |
| Nội dung/handler bổ sung | Componentẩn: kháchnull→—, tênViệtdài không đổi dữ liệu, tiền123.456.789 ₫ cănphải, ngày25/10/2026 09:07, doubleclickdòngtrảđúngmã/headerkhôngmở, loadingkhôngmởstale, Esckhôngchọn, Dispose2lầnhủytoken |7/7 PASS |
| Hồi quy Checkout | Fixture cũ không đổi expected, chỉ thayDLL: validation/money/method/reference, mixed/bank/0₫, busy/unknown, sharedreadonly/payment, footer/DPI, dispose |32/32 PASS |
| Source/binary |208file khôngđổi;3fixture cùngDLL |PASS audit |
| Tiền/kho/tiêuhao/lịchsửđiểm |FullJSONsnapshot trước tạo51đơn và sau tạo/query giống nhau; không checkout khi test đơn mở |PASS |

## Đo cột mã tại125%

DeviceDpi=120. MãHD20250471 rộng80px theo font đang dùng; cột188px, vùng chữ sau
padding156px. Kết quả không đổi ở form1225×700,1280×720,1366×768,740×620 và sau
rebindtrang2(mãHD20250421). CodeFrozen giữcộttrongviewportkhi cuộnngang.
Mãfooter lấy nguyênCode. Đo bounds các control grid/search/selection/pageInfo/
Trước/Sau/Mở/count qua chuỗi ancestor sau settle; đều trong vùng sửdụng.
Đây là kiểm layout kỹthuật, không là nghiệmthu giốngPNG/hìnhthức.

## Phạm vi fault và thao tác

- Hai lượt đọcSQLthật dùng initializer kết nối test có sẵn: lượtđầu bị trìhoãn
  1500ms, lượt sau kết thúc trước (đãassert overlap). Kếtquảcũ không ghiđè tìmrỗng
  mới. Không sửa procedure hoặc source để tạo độtrễ; không gọi đây là mấtmạng thật.
- Fault đọc được tiêm OperationCanceledException trong initializer trước câuquery;
  form giữ input/50dòngcũ, hiệnChưa cập nhật vàkhóaMởđơn. Sau bỏ fault đọc lại đạt.
  Đây là componentfault qua kết nối thực, không là bằngchứng quyền SQL bịthu hồi.
- 59ca điều khiển form/service/fields trực tiếp, không chuột/phímvật lý. 7caẩn
  dùng DTO/session tổnghợp, không gọiShown/reload/SQL vàkhông là kiểmđăngnhập.
  HandlerEscape/doubleclick được gọi trực tiếp, không suyPASSnativekeyboard.
- Dispose có kiểmtoken/repeateddispose, chưa ép đóngđúnglúcSQLđangchạy trên app.
  Entersearch/Entergrid vật lý và hìnhthức đang chờ người dùng trên bản đãmở.

## Dữ liệu và tài nguyên

Không tạoDBthứ4. DùngDBtestcủađợthỏiin:
CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout, markerđúng,
schema016. TạoactorNOO01 với passwordngẫunhiên/hash vàAuthService thật, khách
fixture riêng “OO kiểm thử 20261025”; 51đơn mởHD20250421–HD20250471, khôngpayment.
Fixtureaccount là data test trong DB riêng; không khôi phục TEST login bypass.
Tấtcả SQL parameters/credentials không ghi log; không ghi development/DB sửdụng.

DB được giữ để người dùng kiểmF4/paging và các ca in cònchờ củađợttrước, hạnxemlại
26/10/2026, lýdo cậpnhật ởmanifestchủsởhữuđợthỏiin. Chưa dọn khi đangkiểmmanual.
Không xóa dữliệuG03/manualcũ; dọn vềsau bằngcleanup exactname/marker củađợtchủ.

Artifacts tại [run](../runs/open-orders-test-20261025/final-manifest.json):
results.json,content-results.json,component-results.json,fixture.json,
source-frozen.csv vàlogs. Harnessbuild có6warningnullable; sản phẩm0warning/0error.
Không có FAIL sản phẩm trong98checks đãchạy; không đổi expected/che log.

## Trạng thái nghiệm thu

Đã kiểm thêm doubleclick native từ trạng thái chỉmộthộp: người dùng xác nhận
hộpchọnđónghết, POSđúngHD20250421. Sự cố hộpcũcònlại đã được táchbiệt với
Close của hộp đơnlẻ; không thay source để xử lý trạngthái test chồng.

Người dùng đã xác nhận chuỗi native trên compactpager mới: tìm51đơn, trangcuối
1dòng, mởđúngHD20250421 trênPOS. Ghi PASS nhánh OO04native tìm/paging/mở; không
đánh đồng với mọi nhánh hủy/đónginflight hoặc test hìnhthức/DPIchưachạy.

OO01/OO02 và dữliệu/paging đạt trong phạmvicomponent/SQL nêu trên. Người dùng
đã xácnhận chuỗi native tìmkhách51đơn→Sau1dòng→MởđúngHD20250421 trênPOS.
OO06đóngđúnglúcqueryđangchạy chưa đủ; không mở rộng thành PASS mọi nhánh từchuỗi này.
Hìnhthức agentSKIPPED;DPI100/150%NOT RUN, không đổiScale.
G04 vẫn34PASS/0FAIL/22BLOCKED; không tự nângT16-K01/K11 hoặc toànG04 từOOsubset.
