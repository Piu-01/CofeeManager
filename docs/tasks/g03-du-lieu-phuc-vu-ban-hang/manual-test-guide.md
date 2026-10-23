# Hướng dẫn đợt nghiệm thu G03 — chưa chạy

Đợt này cần duyệt riêng sau triển khai; duyệt bộ plan không là duyệt test.
47 ID gốc, subcase tại từng `plan.md` và phần chung `plans.md` giữ nguyên.
Từng `test-results.md` là nơi ghi expected/actual/status/evidence, không tick từ
build, smoke hoặc ảnh đọc màn. Source/migration cố định suốt lượt nghiệm thu.

## 1. Chuẩn bị và chạy phần tự động

- Ghi source hash/schema/checksum, server, run ID, DB/principal/config/tài nguyên.
  Chạy `tools/database/Test-G03.ps1 -ApprovedAcceptance` chỉ sau khi có duyệt.
- Runner tạo clean/demo với marker riêng, migration001→009, unit rules G03 và
  SQL contract subsets `database/tests/t09..t13/acceptance-contract.sql`, lưu TRX/
  kết quả từng batch và source trước/sau. Đây là tập tự động có giới hạn, không
  gán cả47ID PASS từ batch thành công.
- Các fixture này kiểm tạo/mởsổ/receipt, validation scale, rollback recipe lỗi,
  FK bàn, NULL SDT/hạng, preview0,5VND/ship và điều chỉnh điểm không lặp. Chưa
  thay fault injection giữa các bước, quyền principal thật, concurrency và GUI.
- Runner dọn DB trong finally sau lưu evidence, kể cả FAIL; cleanup thất bại
  ghi tên/marker/lý do còn giữ. Không tái sử dụng DB development làm fixture.

## 2. Subcase SQL bổ sung bắt buộc

Theo từng bảng ID/thao tác/expected ở plan: clean/demo/legacy preflight không
hợp lệ, rerun/checksum sai, bảo toàn số dư và lịch sử. Dùng principal runtime
WITHOUT LOGIN trên DBtest riêng, EXECUTE AS USER từng connection; đối chiếu DENY
DML và procedure/service theo cả ba role. Không đổi quyền/mode SQL server để né lỗi.

Fault injection test-only có tên/nguồn/run marker trong fixture: lỗi sau khi ghi
mốc nhưng trước set tồn/điểm, lỗi giữa delete/insert recipe, lỗi giữa policy mới
và reclassify. Sau lỗi đọc lại toàn tập nguồn, revision, receipt, tồn/điểm/hạng,
`@@TRANCOUNT/XACT_STATE`; expected rollback toàn bộ, transaction sạch. Gỡ hook
fixture sau ca, không sửa migration/product source trong lượt test.

Hai session độc lập, có barrier xác định và log thứ tự:

- T09-K03: cùng mởsổ một nguyên liệu mới, đúng một thành công/một mốc.
- T10-K08: checkout baseline Shared Recipes và writer Exclusive theo cả hai
  thứ tự; tiêu hao dùng trọn công thức cũ hoặc mới. Lưu old/new recipe, tiêu hao,
  tồn và số tiền; không cho deadlock/timeout được coi là thành công.
- T11-K07: màu bàn cũ, sessionB đổi trạng thái đơn fixture, chọn lại trả đơn mới.
- T12-K05: hai mởsổ cùng khách; không reset khi có hóa đơn/history.
- T13-K05/K11: policy thay cùng create/open/adjust/checkout; điểm100 hai lần
  trừ80 chỉ một lần thành công, dư20, một history. Kiểm khóa và policy/hạng;
  nếu revision đổi, phiên thứ hai phải tải/preview lại, không retry mù.

T09-K09 và T10-K04/K06–K08 tách phần baseline G03 với phần T14–T16/G04; G04 còn
NOT RUN, không đánh dấu toàn ca PASS chỉ từ fixture.

## 3. GUI thật và Designer

Provision phiên GUI trên DBtest của lượt chạy, chọn principal runtime giới hạn;
manifest phải ghi cấu hình local/tài khoản thử và cách dọn. Nếu cần giữ DB cho
thao tác manual, chỉ giữ tài nguyên đã được chỉ định, ghi lý do/hạn giữ/cách dọn.
Không chạy fixture nghiệm thu trên DBdevelopment. Collector technical dùng identity
injection chỉ kiểm read UI, không thay đăng nhập, quyền mutate hoặc fixture GUI.

Thao tác tuần tự T09→T13 theo ma trận plan, đủ các nhánh tạo/sửa/xóa/mởsổ/recipe/
chọnbàn/chọnkhách/preview/policy/adjust; kiểm ErrorProvider, focus, giữ input,
modal, Enter/Esc, double-click, loading/rỗng/lỗi/stale/unknown, đổi màn/đăng xuất,
query trả muộn và timeout trước/sau commit. Ghi mã nguồn/request để đối chiếu.

Kiểm 1280×720 và1366×768 × DPI100/125/150; maximize/cửa sổ thường/Snap nửa màn,
ghi desktop/outer/client/WorkingArea/DeviceDpi. Tên Việt dài, số lớn, scroll và
footer vẫn truy cập được. Designer save-close-reopen cho mỗi form/control dùng
trong nhóm, lưu hash trước/sau và compile Designer round-trip khi đúng scope.
100 lần đổi module,10 login/logout đo instance/event/GDI/timer/SQL connection;
khôi phục DPI ban đầu. Không lấy một ảnh full màn hình thay ma trận.

UX: T11→UX-K06; T12→UX-K09; T13→UX-K10/33; mọi màn→UX-K39–42. T16 bổ sung tích hợp
POS cho các ID tương ứng. Không gọi ảnh HTML hoặc dữ liệu mẫu là WinForms acceptance.

## 4. Kết thúc

Ghi mọi subcase kể cả FAIL/BLOCKED/NOT RUN; gom bug vào task sở hữu, report nhóm
liên kết. Dừng sửa source trong lượt test, chờ duyệt cả đợt bug trước debug và
regression. Chỉ ghi Hoàn tất khi đủ đầu ra và ca bắt buộc PASS. Không commit/push
khi chưa được giao riêng.
