# Đợt test UI Checkout sau triển khai — đã duyệt/chạy25/10/2026

Kếtquả và phạmvi thực tế xem [test-results.md](test-results.md);giữexpectedbên dưới.

Không ghiPASS chức năng từ build. Giữ56ID G04/325ID toàn dự án và lịch sử.
Chạy đúngbinary `runs/checkout-ui-20261025/Debug` sau duyệt; source đóng băng,
SQL ServerDBtest riêng có marker, schema016; không dùng development.
HD20250411 là đơn manual còn mở trước đợt đổiUI,không tự thanh toán để xem mẫu.

| Ca bổ sung / tham chiếu | Dữ liệu và expected |
|---|---|
| C01/T16-K03 | Phải trả81.000,tiền mặt81.000,khách đưa100.000;hiệnphân bổ81.000,thối19.000;chỉ gửi81.000 khi chốt vàđối chiếuSQL |
| C02/T16-K04 |80000thiếu1000;abc;30chữsố9;âm/lẻ;methodtrống/PT010;phân bổthiếu/dư;referencequá100:notice rõ,Confirm disabled,giữinput,0ghiSQL |
| C03/T16-K04 | Cancel/Quay lại/Esc hỏi bỏ nháp;cancelkhông ghi,giữđơnmở;đangghi/unknown không đóngtrừthu hồi phiên |
| C04/T16-K05 | Native editor method/amount/reference,nhiều dòng,thêm/xóa,tab/focus;Enterchỉkếtthúcsửa,khôngchốt;gửi lặp chỉ1request;input/buttonmởlạikhi lỗiđãxácđịnh |
| C05/mixed | Cash31000+bank50000=81000,kháchđưa50000→thối19000;bankonly81000bỏ qua/disabletiềnkháchđưa;0₫đúngluồng,khôngtạoallocdươnggiả |
| C06/T16-K06 | Phiênkhácchốtđơn/previewđổi:báođúng,tảiSQL/preview,mớikhôngtựthuhoặcsửafinance;UI giữinputđểxemlại |
| C07/T16-K07 | Timeout/trạng tháichưa rõ:khóanhập/chốt/đóng,Kiểmtralạichỉđốichiếucùngmã/request;khôngretựgửi,gộpthuhoặcstock/pointslặp |
| C08/125% |1280×720/1366×768 khi workingarea cho phép,resizehẹp:2cột→stack,bodycuộn,footer/input/bảngcóthểthao tác;khôngđổiWindowsScale |
| C09/shared | SmokeOrbitDialogLayoutmặcđịnhvàgridMenu/Inventory/Catalogs;profilePaymenteditable,editorcótext/selection;khôngđổidefaultreadonly/profilecũ |
| C10/lifecycle |Đóng/mởnhiềulần,cancel,thu hồi phiên;handler/overlayđượcdispose,khôngdirtyDBhoặctimerbịgiữ |

Tái dùng unitCheckoutRules/SalesRules hiện có khi phù hợp. Testcomponent chỉ
chứng minh handler/binding/layout kỹ thuật,nêu rõ khi không phải thao tác chuột.
Commitcheckout phải dùngDBtest,đối chiếu payment/stock/points/receipt cảtrước/sau;
không đánhPASS bằngpreview trong bộ nhớ. Mỗi lượt lỗi giữFAIL trước debug.

Hìnhthức giao diện người dùng tự kiểm:SKIPPED theo phân công agent;
khôngchụpảnhđối chiếu/hover/spacing.DPI100/150%NOT RUN,khôngépDPI.
Biênbản kết quả lưu cạnhplan này;aggregateG04 vẫn33PASS/0FAIL/23BLOCKED trướcđợt mới.
