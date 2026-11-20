# Bán hàng gọn — kết quả kỹ thuật30/10/2026

Trạng thái: ĐÃ TRIỂN KHAI, CHỜ DUYỆT ĐỢT TEST CHỨC NĂNG RIÊNG.

| Phần | Actual | Trạng thái |
|---|---|---|
| Build Debug/Release |Mỗi cấu hình exit0,0error,1warningCS8602 tại AttendanceControl.cs:53 ngoàiPOS |PASS compile; warning được giữ/ghi rõ |
| Bounds kỹ thuật125% |123/123 tại DeviceDpi120,0/1/50card/dòng, hai desktop và nhánh960×900 |PASS trong phạm vi triển khai |
| Smoke đầu |Grid9/57px không đủ header+row; đã chỉnh layout trong pha code, expected giữ nguyên |RawFAIL lưu nguyên; không là lượt nghiệm thu |
| SC01–SC08 chức năng/SQL/native |Chưa chạy đợt mới |NOT RUN |
| Hình thức |User tự kiểm, agent không ảnh/hover/Designer visual |SKIPPED theo phân công |
| DPI100/150% |Chưa chạy/không đổi Scale |NOT RUN |

Session constructor đã revoke, services null, DTO/card bộ nhớ; không query/Auth/
write/checkout. Bounds không chứng nhận tài chính/interaction hoặc mock đúng hình.
Hồ sơ/hash/source và raw logs tại
[run](../runs/sales-compact-implementation-20261030/manifest.json).
Không tạo tài nguyên SQL mới; chưa có appPOS mới chạy dùng thử. Các thay đổi
module khác đồng thời trong workspace được giữ, không coi là source do task này.

## Đợt test được duyệt30/10/2026 — Bán hàng gọn

Trạng thái mới: ĐÃ TEST, còn FAIL tiền dài; chưa nghiệm thu toàn bộ. Giữ phần trên
làm lịch sử pha triển khai. Binary Debug cố định trong runs/sales-compact-acceptance-20261030/frozen;
6file POS không đổi sau test (owned-source-after.csv), không build lại source module khác.
WinForms DeviceDpi120; SQLServer localhost, DB riêng CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout,
marker20261025_181739_98e5df59, schema016. Handler/component thật trong tiến trình;
không chứng nhận thao tác chuột/phím vật lý hay hình thức.

| Phần | Actual | Trạng thái |
|---|---|---|
| SC01 resize/selection | Giữ đơn/dòng; bounds triển khai123/123 | PASS kỹ thuật125% |
| SC01 tiền18chữ số | Tổng cần417px/ô279px; cột tiền cần205px/ô132px; DTO stress không ghi SQL | FAIL |
| SC02 menu/tìm/trang/danh mục/rỗng/refresh | Handler thật; Enter tìm không chốt | PASS các nhánh đã chạy |
| SC03 thêm/sửa/xóa | Một callback, Add khóa đúng; giữ MaDong/giá snapshot/note, xóa đúng dòng | PASS |
| SC04 tạo/mở/bàn/nháp | Ba kênh; bàn không tạo header rỗng; hỏi bỏ nháp; F4 tìm/mở | PASS các nhánh đã chạy |
| SC05 tổng/KM/ship | F3 khách/KM10%; F8 breakdown khớp SQL; invalid/unknown hiển thị — | PASS |
| SC06 phím/dialog/quyền | F2 selectall; F3/F4/F8 native owner/không taskbar; Enter không chốt; paid/unknown khóa đúng; ADMIN đọc/STORE bị từ chối | PASS component/handler |
| SC07 cash/mixed/zero/thiếu kho | No print về đơn trống; thiếu kho từ chối nguyên tử | PASS |
| SC07 lỗi đọc in sau commit | HD96300004 đã trả/POS trống/Đã thanh toán và Chưa in được hóa đơn; fault connection riêng | PASS |
| SC07 đối chiếu | Một receipt/đơn paid; phân bổ=Payable; tồn giảm0.090NVL01/0.050NVL03 đúng tiêu hao; điểm=ledger; dữ liệu cũ không đổi | PASS |
| SC07 stale/hai session/gửi lặp/logout giữa thao tác | Chưa chạy bổ sung trên binary mới | NOT RUN |
| SC07 in native/PDF Có→Hủy | Chưa chạy trên binary mới | NOT RUN |
| SC08 các profile/module ngoài OrderLines | Chưa regression đầy đủ trên binary mới | NOT RUN |
| Unit Sales/Checkout/Delivery rules | unit-results/sales.trx18/18 frozen BLL | PASS |
| Hình thức | User tự kiểm | SKIPPED theo phân công |
| DPI100/150% và phím vật lý | Chưa chạy, không đổi Scale | NOT RUN |

Raw results.json:20PASS/3FAIL; followup-results.json43PASS; readonly-results.json16PASS/1FAIL.
Tổng79PASS/2FAIL sản phẩm.2FAIL raw còn lại do harness bắt hộp xác nhận cũ chưa đóng,
rồi timeout; đơn480 chưa trả. Giữ nguyên raw, chỉ sửa chờ modal trong harness,
chạy tiếp cùng binary/expected, không sửa sản phẩm.18unit tính riêng; không đổi56ID G04/325ID.
Readonly before/after tài chính giữ nguyên. SQL snapshots và thao tác trong Program.cs,
Followup.cs, Readonly.cs; không suy PASS toàn SC01–SC08 từ các nhánh đã chạy.

Fixture SCA30/SCC30/SCS30 vô hiệu sau runner. SC030, KM96300001/2, B9630, SP963 và7đơn
owned-orders.json giữ có chủ đích cùng lịch sử paid/tiêu hao/điểm/receipt, không reset tồn/điểm.
Không tạo DB mới/ghi development hoặc sửa tài nguyên G05/G06. Lỗi ghi trong bug-report.md;
chờ duyệt đợt sửa/regression.

## Regression sửa tiền dài31/10/2026 — đã được duyệt

G04-SC-MONEY-20261030:180/180 kiểm kỹ thuật WinForms PASS tại125%, Debug/Release sạch.
Chỉ2file UI thay đổi; không thay SQL/BLL. Đo đủ ngang/dọc tổng0/81000/18chữ số,
cột giá/tiền dòng, caption/F8/grid/selection/resize và null/invalid/unknown/paid.
Không chạy lại79assertion SQL hoặc18unit của đợt trước; không kế thừa thành PASS
trên binary mới. Raw FAIL9 rồi1 của hai lượt debug được lưu, expected giữ nguyên;
chi tiết bug-report.md và ../runs/sales-money-fix-20261031/regression-final.json.
Không ghi SQL trong regression này; chỉ mở Login bản mới trên DBtest cho user.
Hình thức SKIPPED theo phân công;100/150%, native print/concurrency chưa chạy bổ sung.
Không chốt G04 hoàn tất từ việc hai lỗi hiển thị đã sửa.

## Điều chỉnh nhỏ theo yêu cầu31/10/2026 — cột Đơn giá

User yêu cầu tên cột Đơn giá, chỉ giá snapshot một món; số lượng ở cột riêng,
không hiển thị tiền dòng/tiền tố ĐG. Đã đổi OrderPanel và profile OrderLines,
bỏ painter hai dòng; giữ đo giá dài, Amount vẫn bound nhưng ẩn. Không đổi tổng,
SQL/BLL/checkout. UX và guide cập nhật quyết định mới, thay cách trình bày hai dòng.
Build log: ../runs/sales-unit-price-20261031/build.log. Chưa chạy lại regression chức
năng/SQL cho thay đổi này, không kế thừa180PASS của bản trước thành PASS bản mới.

## Pager31/10 — regression được duyệt

G04-SC-PAGER-20261031:37/37 kiểm kỹ thuật125%PASS; Debug/Release sạch.
Không chạy lại chức năng query trang/SQL; chỉ kiểm bounds/icon/labels/busy và
đối chiếu giữ callback. Hình thức user kiểm,100/150% NOT RUN. Hồ sơ
../runs/sales-pager-fix-20261031/. Không kế thừa PASS của binary cũ cho bản mới.
