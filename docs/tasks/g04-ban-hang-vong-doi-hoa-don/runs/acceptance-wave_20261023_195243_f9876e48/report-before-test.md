# G04 — Bàn giao triển khai, chờ duyệt đợt test

**Ngày23/10/2026. Trạng thái: Đang làm, chưa nghiệm thu.** Requirements đã duyệt
bằng “duyệt”, cả bộ plan đã duyệt bằng “diuyeetj”. Đã triển khai tuần tự T14–T18.
**56ID gốc đều NOT RUN trong đợt nghiệm thu G04.** Build/smoke dưới đây là kiểm
tra kỹ thuật để nối đầu ra, không nâng checkbox hoặc kế thừa PASS G03 cho checkout.
Không commit/push; working tree còn thay đổi có sẵn được giữ nguyên.

## Đầu ra đã có

| Task | Phần triển khai | Hồ sơ |
|---|---|---|
| T14 | Migration010; tạo header+món đầu nguyên tử, revision/receipt, sửa MaDong/khách/KM/thông tin giao, lookup POS, SalesRepository/Service | [Plan](t14-tao-sua-don/plan.md), [kỹ thuật](t14-tao-sua-don/technical-checks.md), [test](t14-tao-sua-don/test-results.md) |
| T15 | Migration011; preview/token chính thức, checkout TVP/tiền/kho/điểm/snapshot/phí trong một transaction, LegacyUnknown, quyền/biên và đối chiếu | [Plan](t15-checkout/plan.md), [kỹ thuật](t15-checkout/technical-checks.md), [test](t15-checkout/test-results.md) |
| T16 | Migration012; POS3kênh, bàn/menu/khách/ưu đãi, editor dòng/header, phân bổ/tiền mặt/thối, unknown/reconcile, hủy mở, CASH vào POS, tích hợp sơ đồ bàn | [Plan](t16-pos-huy-don/plan.md), [kỹ thuật](t16-pos-huy-don/technical-checks.md), [test](t16-pos-huy-don/test-results.md) |
| T17 | Migration013; lọc ngày/trạng thái/kênh/khách/mã, paging, detail stored/snapshot, PrintDocument/preview/in lại, lỗi in không checkout lại | [Plan](t17-tra-cuu-in/plan.md), [kỹ thuật](t17-tra-cuu-in/technical-checks.md), [test](t17-tra-cuu-in/test-results.md) |
| T18 | Migration014; danh sách/chi tiết giao, chuyển một bước theo version/status, giữ tài chính, STORE bị chặn, hướng dẫn giao thất bại | [Plan](t18-giao-hang/plan.md), [kỹ thuật](t18-giao-hang/technical-checks.md), [test](t18-giao-hang/test-results.md) |

Migration015 bổ sung chuẩn hóa mã tùy chọn thànhNULL ở tạo/sửa header và đọc ưu đãi
paid từ snapshot, không chạy dữ liệu lịch sử qua chính sách mới. Giữ checksum010–014
đã ghi trong các lượt kỹ thuật. Contract mới tại docs/database/sales-contract.md,
checkout-contract.md, invoice-contract.md, delivery-contract.md; UI/BLL không chứa
SQL tính tiền hoặc tự cập nhật số dư. Có input draft được giữ sau từ chối/xung đột;
khóa checkout khi còn input chưa ghi, không dùng số local thay SQL.

Quyết định giữ theo plan: khóa loại/bàn sau tạo, hủy chỉ xác nhận không lý do giả,
tiền đưa/thối không lưu, T18 chỉ lưu tiến độ hiện tại không timeline. Đơn cũ thiếu
nguồn/tỷ lệ ghi LegacyUnknown. Không hoàn tiền/PT010/gộp-tách/đặt bàn hoặc làm sớmG05.
Tiện ích đăng nhập TEST tạm giữ đúng phạm vi hiện có; bắt buộc gỡ sạch ởT32/G08.

## Kiểm chứng kỹ thuật đã chạy

- [Build cuối](runs/build-final-20261023/manifest.json): solution SDK10.0.401,
  Debug và Release đều exit0, **0warning/0error**; log riêng trong thư mục này.
  Unit được compile nhưng **chưa chạy**.
- [SQL/typed smoke015](runs/technical_20261023_154851_e3c1f807/manifest.json):
  baseline/seed đúng thứ tự,001–015 trên clean/demo. Typed C# DAL/BLL tạo→checkout→
  InvoiceDetail/List và tạo giao→chuyển bước→detail thành công; operatorSQL principal,
  không thay bằng chứng quyền runtime hoặc thao tác GUI.
- [Lượt native binding](runs/technical_20261023_155844_f4c85032/manifest.json):
  cùng smokeSQL/typed và cửa sổ WinForms thật125%, đọc10cardmenu,2hóa đơn,4đơn giao
  từ fixture SQL; [JSON](runs/technical_20261023_155844_f4c85032/native-workspace.json).
  Không đăng nhập/ghi từGUI, không chụp ảnh/đánh hình thức hoặc full matrix.
- [Probe checkout/print](runs/native-technical/final-native.json): DeviceDpi120,
  binding1row/3columns, CTA nằm trong client; preview100dòng/ghi chú thành13trang,
  cursor210entry đến cuối. PreviewPrintController trong bộ nhớ, **không gửi job in**.
  Không thay T17-K03/K06/K07 đầy đủ hoặc kiểm hình thức bản in.
- [Lịch sử probe](runs/native-technical/history.md): lần đầu exit1 do Dispose gọi
  lại CancellationTokenSource; đã có guard dọn một lần trong pha triển khai và
  chạy lại exit0. JSON trước khi lỗi không được coi toàn vòng đời PASS. Các lỗi
  compile namespace/serialization ban đầu cũng không làm biến mất giới hạn test.
- Đã parse ba script PowerShell chuẩn bị; runner nghiệm thu và batch SQLsubset
  **chưa chạy**. Kiểm nguyên văn56checklist và đường dẫn tài liệu, không tick ca.

Các lượt SQL kỹ thuật010/011/012/013/014/015 đều có manifest tại runs, hash source
trước/sau và cleanup; không dùng chúng thay ca fault/hai session/role/timeout.
Source bàn giao ghi ở [source-implementation.csv](source-implementation.csv);
evidence từng lượt giữ source/schema tại thời điểm chạy, không suy toàn GUI mới
đã được nghiệm thu từ probe binding.

## Development và tài nguyên

[Đường chạy F5/dùng thử](development-guide.md): CafeManager.slnx, startup WinForms,
CoffeeManager.Development, localhost/CoffeeManager_Development, **schema015**.
[Manifest nâng development](runs/development_20261023_155341_6ed8a070/manifest.json):
009→015 sau backupCOPY_ONLY CHECKSUM; tồn, điểm/hạng, tiền/trạng thái hóa đơn và
digest tài khoản trước/sau không đổi. Không seed/reset account/fixture nghiệm thu.
Mật khẩu/configDPAPI giữ nguyên, không chép bí mật vào repo/chat.

[Kiểm kê chỉ đọc](runs/resource-audit.json):7lượt SQL kỹ thuật, trước0DBG04,
tạo14DB, dọn14DB, **còn0DBtest/0probeprocess**, không đổi service/SQLmode/WindowsScale,
không tạo Windowsuser/login mới.1backup development giữ có chủ đích tới nghiệm thu
G04 và người dùng chấp nhận development; đường dẫn/ownership trong manifest. Không
tự xóa backup hoặc tài nguyên sẵn có, không commit file backup.

## Phần chưa nghiệm thu và bước tiếp theo

[Đề nghị duyệt đợt test](test-plan.md) bao phủ56ID và UX/regression liên quan,
source cố định, DBtest riêng, unit+SQL+hai session+fault+WinForms thật. Ba SQLbatch
và Test-G04.ps1 chỉ tự động hóa subset; không tự đánh toàn56IDPASS. Cần kiểm mutation
GUI, keyboard/nháp/loading/lỗi/timeout, quyền runtime, transaction sau fault, tranh
bàn/tồn, snapshot lịch sử, in lỗi/in lại và vòng đời phiên/điều hướng.

Hình thức UI/bản in **SKIPPED theo phân công cho người dùng**; DPI100/150% **NOT RUN**,
agent không đổi WindowsScale. Chức năng/layout kỹ thuật125% trong probe có phạm vi
hẹp như ghi trên; ma trận bắt buộc vẫn chờ đợt test riêng. T17 là mốc demo bán hàng,
chưa phải đủ vận hành G05–G08. Không tự ghi Hoàn tất T14–T18/G04 từ triển khai.

Đang chờ người dùng duyệt **một đợt test G04**. Test sẽ lưu kết quả từng task và
báo cáo nhóm kể cả FAIL/BLOCKED; bug gom đợt chờ duyệt debug, không sửa source giữa
lượt test. Chưa có quyền commit/push.

