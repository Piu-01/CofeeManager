# Kết quả UI Giao hàng26/10/2026

**Đóng lượt trước sửa UI POS30/10/2026:** các số55/108/53/18 bên dưới thuộc
source/binary giao hàng đã lưu. Manual/hình thức chưa có xác nhận vẫn giữ chờ;
không chuyển PASS sang binary POS gọn hoặc gán kết quả này cho thay đổi G05.

Trạng thái: ĐÃ DUYỆT bằng “đuyệt” và chạy đợt chức năng26/10/2026. Các kiểm
component/SQL đạt; chờ người dùng xác nhận thao tác tay/hình thức. Không tự thay
số ca gốc hoặc chốt toànT18/G04 từ sốcheck bổ sung.

| Phần | Actual | Kết quả |
|---|---|---|
| Build Debug/Release |Mỗi cấu hình0warning/0error |PASS kỹ thuật |
| Smoke bounds/readonly/cột/không co theo số dòng |48/48 tại DeviceDpi120, client1280×720/1366×768, dữ liệu0/1/50 trong bộ nhớ |PASS trong phạm vi layout kỹ thuật |
| Smoke lần đầu |49PASS/1FAIL ngưỡng harness200px không có trong requirement; raw giữ nguyên |Không là ca nghiệm thu, xem giải thích report |
| DG01 |DeviceDpi120, header đúng;48smoke bounds giữ theo binary khớp,0/1/50 không co Body |PASS kỹ thuật; hình thức chờ user |
| DG02 |Mã/SDT0đầu, kính lúp/Enter, clear nháp/submit, không chuyển giao bởi Enter |PASS component/SQL; physical input chờ user |
| DG03 |Filter tiến độ/thanh toán tự tải, draft không apply qua filter/paging/refresh, task cũ hoàn tất muộn bị bỏ |PASS component/SQL; late reply qua query seam |
| DG04 |0/1/49/50/51/100/101 query SQL thật; total/range/Next đúng cả trang cuối đủ50; stale-empty fallback seam |PASS |
| DG05 |Readonly/profile/code frozen/address fill/phone typed/select-all; error/cancel không stale detail/0giả |PASS component; tooltip/visual chờ user, không hover test |
| DG06 |Native owner/title/no taskbar/khóa HWND, xác nhận nested, Cancel không chuyển;2bước thực trên đơn có món; Đã giao vẫn Chưa thanh toán; detail472paid/473canceled/474open và hướng dẫn thủ công đúng |PASS các subcase đã chạy; không tạo paid fixture mới để chuyển tiến độ |
| DG07 |Hai session cùng revision mộtOK/một52103; pending receipt thật được reconcile khi filter rỗng; không thêm receipt, writing khóa action |PASS; unknown state mô phỏng acknowledgement bị mất, không timeout mạng thực |
| DG08 |ADMIN/CASH, STORE chặn;108native/shared regression,53invoice regression,18Sales xUnit |PASS trong phạm vi read/validation/control và write giao riêng; không CRUD/checkout |
| Hình thức |Người dùng tự kiểm, chưa xác nhận |SKIPPED theo phân công agent |
| DPI100/150% |Chưa chạy/không đổi Scale |NOT RUN |

Session/service null chỉ áp dụng smoke triển khai, không bộ chức năng. Body181/229px
giãn hết phần còn lại giữa action/footer; không suy hình thức tốt từ invariant.
Nguồn/hashes và log tại
[manifest](../runs/delivery-ui-implementation-20261026/manifest.json).
Không SQL/migration/datawrites trong pha triển khai; đợt acceptance có fixture
và chuyển bước giao trong DBtest đã duyệt. Không kế thừa8/8T18 từ binary cũ.

## Bằng chứng đợt chức năng

[Run acceptance](../runs/delivery-ui-acceptance-20261026/README.md),
results.json55/55, dialog-results.json108/108 (initial94/94 lưu riêng),
invoice-regression/results.json53/53, unit-results/sales.trx18/18.
source-verification.json xác nhận142file không đổi và DLL trùng triển khai.
Build harness có nullable warning ở fixture, product Debug/Release không warning.

AuthService/runtime SQL thật, DB CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout,
marker20261025_181739_98e5df59, schema016. Fixtures:

- 101header giaoDG26000001–DG26000101 cho paging, không dùng header trống kiểm ghi.
- 2đơn HD96260001/HD96260002 tạo bằng SalesService/SP001×1; UI chuyển2bước có
  xác nhận, service concurrency và receipt reconcile; không checkout/in/refund.
- DGA01/DGC01/DGS01 và fixture regression tự dọn trong finally. Receipts/dòng/
  headers thuộc fixture được dọn đúng allowlist; không rewindidentity/sequence.

before-transitions.json=after-transitions.json: tài chính header/dòng/payment/
ưu đãi/kho/điểm bất biến, chỉDelivery/Revision/receipt giao được đổi.
before.json=after.json sau cleanup: toàn orders/lines/payments/consumption/
discounts/stock/points/receipts hiện hữu khớp. Regression cũng ghi bất biến/dọn
actors. Không chốt hoặc sửa đơn476–478 còn giữ.

App test mới PID6748 đã mở qua đăng nhập thường, HWND/title/Responding kiểm ngoài
sandbox tại gui-visible-check.json. Câu hỏi manual474 chỉ tìm/lọc/xem/đóng và
nhận xét hình thức đã gửi, chưa có trả lời tại thời điểm ghi. Giữ app+DBtest chung
để user kiểm, review27/10/2026; DB thuộc ownership cũ, không tạo DB thứ4. Các phần
physical input/visual vàDPI100/150% còn thiếu không tự chuyển thành PASS.
