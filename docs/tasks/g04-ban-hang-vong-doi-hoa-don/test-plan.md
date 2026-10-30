Trạng thái24/10/2026: hai vấn đề bổ sung đã xử lý theo duyệt, schema016;
35PASS/0FAIL/21BLOCKED trên56ID; UI05 đã sửa và handler Confirm thật được người dùng/SQL xác nhận; xem report.md.

Trạng thái hiện tại: đã duyệt bằng 'duyệt' và đã chạy; xem report.md và năm test-results.md.
Nội dung đề nghị bên dưới là kế hoạch được duyệt, các chữ chưa chạy ghi thời điểm chuẩn bị.

# Đợt nghiệm thu G04 — đã duyệt ngày23/10/2026

Nguồn chuẩn: requirements.md, plans.md và năm plan T14–T18. Giữ56ID gốc; source,
migration001–015, checksum và build sẽ đóng băng trước đợt chạy. Đã duyệt plan
không đồng nghĩa duyệt test. Hiện56ID **NOT RUN**, không nhận build/smoke làm PASS.

## Phạm vi đề nghị duyệt một lần

| Phần | Cách thực hiện / expected |
|---|---|
| T14 ·12ID | Ba loại đơn, món đầu nguyên tử, giá snapshot/MaDong, xóa dòng cuối, FK/biên/role, hai session cùng bàn/version, fault header/dòng/ưu đãi và đối chiếu timeout |
| T15 ·15ID | Ưu đãi đổi trước xác nhận, phương thức/phân bổ/trùng PT/PT010, bill0₫/ship, tiêu hao/điểm/phí snapshot, tranh tồn, hai checkout, fault tại3vị trí và transaction sạch, quyền DML, seed legacy |
| T16 ·12ID | WinForms thật: tạo/mở lại3kênh, sửa/xóa, khách/ưu đãi,81.000–100.000 và mixed payment, keyboard/Enter/F8/gửi lặp, quyền STORE, timeout/đối chiếu/hủy/refresh và reload app |
| T17 ·9ID | Bộ lọc ngày cuối/ngày lập-ngàyTT/paging, detail snapshot, nhiều trang/ghi chú Việt, in lại/lỗi/hủy máy in không đổi DB, khóa tài chính và notfound |
| T18 ·8ID | Người nhận, tiến độ một bước, khóa tài chính paid, hủy mở đúng contract, hai phiên stale, role/filter/reload và hướng dẫn giao thất bại |
| Regression | T09-K09, T10-K04/K06–K08, bànT11, khách/preview/điểmT12–T13 bằng checkout đích; UX-K05–K23/K39–K43 và phiên/nháp/revokeK03/K04 |

Unit xUnit dùng các lớp `CafeManager.Tests.Sales` cho chuẩn hóa loại/NULL/điện thoại,
biên tiền, tiền thối theo phần cash, phân bổ trùng và chuyển giao/quyền. Không test
getters. Chưa chạy unit trong pha triển khai này.

## Công cụ và fixture

`tools/database/Test-G04.ps1 -ApprovedAcceptance` chỉ dùng sau khi người dùng duyệt
đợt này. Runner tạo clean/demo trên SQL Server thật, baseline/seed demo trước
migrations, apply001–015, lưu source hash/checksum; chạy3batch SQLsubset có expected
assertion tại `database/tests/g04/acceptance-*.sql`, typed dependency smoke và unit.
Exit/manifest phản ánh FAIL, TRX phải có test thật và không thiếu/skip. Kết quả runner
chỉ là **partial coverage**, không tự nâng56IDPASS. Các batch SQL đã chuẩn bị, chưa
chạy/compile nghiệm thu. Test giữ nguyên source; lỗi fixture/test cũng ghi bug, không
sửa giữa lượt chạy để đổi kết quả.

Các ca chưa tự động hóa trong runner phải chạy riêng theo từng hàng/subcase của
plan, ghi dữ liệu/lệnh/thao tác/expected/actual/evidence vào test-results của task:

- Dùng hai SqlConnection thật cùng DBtest và request/version ổn định, điều phối
  đồng thời bằng barrier hoặc fixture trigger có WAITFOR trong DBtest; không
  dùng outer transaction C# quanh entry point đã cấm ambient transaction. Chạy cả
  hai thứ tự policy/công thức/checkout và các nguồn tranh bàn/tồn/khách.
- Fault trigger test-only trên header/chi tiết/tiêu hao/payment/điểm có THROW định
  danh, snapshot trước/sau toàn bộ bảng nguồn và @@TRANCOUNT/XACT_STATE trên session
  gọi. Dọn hook giữa scenario theo fixture đã chuẩn bị, không ALTER production
  procedure hoặc thay expected. Dữ liệu legacy-invalid ở DBtest riêng, không tự
  sửa dữ liệu lỗi để migration đi qua.
- Runtime principal giới hạn và EXECUTE AS user WITHOUT LOGIN ở DBtest cho DENY,
  actor/stamp/role. Operator smoke trong triển khai không chứng nhận các quyền này.
  GUI dùng profile test đúng marker và credential runtime đã cấp trên máy; setup
  phải qua contract chuẩn đếnCompleted, không dùng TEST bypass làm bằng chứng login.
- GUI chức năng thực: binding/handler, keyboard/focus/IME, modal/nháp, lỗi/late query/
  unknown, resize/cuộn/bounds ở125%,1280×720 và1366×768 khi máy hỗ trợ, thường/max/Snap.
 100đổi module/10login-logout ghi handle/timer/connection trước/sau; không suy tốc độ
  T29. Nếu công cụ GUI thiếu thì ghi blocker đúng nhánh, không lấy service thay UI.

Agent không kiểm hình thức, chụp ảnh đối chiếu, hover/spacing hoặc nghiệm thu qua
Designer: **SKIPPED theo phân công cho người dùng**. Source hỗ trợ DPI100/125/150%;
agent chỉ kiểm chức năng/layout kỹ thuật ở125%, không đổi Windows Scale.100/150%
chưa chạy ghi NOT RUN riêng; giữ nguyên chữ checklist gốc.

## Tài nguyên và kết quả

Tối đa3DBtest cùng lúc (clean/demo/legacy-invalid), tên chính xác có runID/marker;
manifest gồm server/principal/config/process/ownership/source/schema/build. Không
tạo Windows user/login mới, không thay SQLmode/service/registry, không fixture vào
development hoặc DB đang sử dụng. Phần tự động mặc định chỉ tạo hai DB clean/demo;
DB thứba cho scenario legacy/fault phải ghi vào manifest riêng trước tạo.

Sau evidence dọn allowlist đúng marker, kể cả FAIL; không DROP wildcard/tài nguyên
sẵn có. Giữ điều tra cần chỉ định, lý do/hạn/cách dọn. Ghi số trước/tạo/dọn/còn giữ.
SQLbackup development không thuộc DBtest cleanup, giữ theo chính sách bàn giao.

Test lần lượt T14→T18, lưu biên bản từng task rồi report nhóm kể cả FAIL/BLOCKED.
Ghi bug đúng task sở hữu, gom đợt chờ duyệt debug. Không sửa source/migration trong
lượt test. Chỉ nghiệm thu Hoàn tất khi đủ đầu ra và ca bắt buộc PASS hoặc quyết
định ngoại lệ cụ thể của người dùng được lưu riêng; không biến ngoại lệ thành PASS.









