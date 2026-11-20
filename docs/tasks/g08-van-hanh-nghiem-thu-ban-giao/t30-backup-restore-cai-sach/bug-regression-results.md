# G08-B01 — Kết quả sửa và regression

02/11/2026 · **Đã sửa, regression PASS và đóng G08-B01 trong phạm vi được duyệt.** Người dùng đã duyệt sửa kèm regression; lần dừng Computer Use bằng Esc được người dùng xác nhận là bấm nhầm và cho tiếp tục. G08 vẫn Đang làm vì còn13ID BLOCKED.

## Thay đổi

- `SqlErrorMapper.cs`: thêm258 vào nhóm lỗi kết nối; thêm thông báo riêng cho lỗi chứng chỉ−2146893019: “Không xác thực được chứng chỉ SQL Server; kiểm tra chứng chỉ và tên máy chủ.”
- `ParameterAndErrorTests.cs`:14ca mới kiểm phân loại, thông báo có hành động và `RequiresReconciliation` cho đọc/ghi; giữ kiểm mã kết nối cũ, timeout, quyền, deadlock và Unknown.
- Chỉ hai file source/test thay đổi so với snapshot acceptance. `SalesRepository.Call` vẫn dùng `writing && sent`; lỗi lúc mở kết nối trước khi gửi không bị biến thành kết quả ghi chưa biết. Mapper nhận cờ ghi sau gửi vẫn giữ yêu cầu đối chiếu. Quyền, cấu hình TLS, SQL/migration và giao diện không đổi.

## Kết quả thật

| Kiểm chứng | Expected | Actual | Kết quả |
|---|---|---|---|
| Unit/rules hiện có và14ca mới | Các quy tắc cũ và cờ đối chiếu không bị thay đổi ngoài hai mã lỗi |142/142PASS,0skip | PASS |
| AuthRepository đọc: offline/TLS |258/−2146893019 được báo Connection, thông báo đúng, chưa gửi lệnh nên không cần đối chiếu |2/2đúng số lỗi/Kind/message/flag | PASS |
| SalesService ghi nhưng lỗi trước gửi | Permission kiểm qua kết nối hợp lệ; OpenAsync của đường ghi thất bại trước `sent=true`; Connection/RequiresReconciliation=false |2/2đúng; DBfixture0bill/0TT/0phiếuthu-chi | PASS |
| Native thiếu cấu hình | Thông báo thiếu kết nối, nút Login bị khóa | Đúng trên package mới | PASS |
| Native cấu hình sai | Thông báo cấu hình local không hợp lệ, Login bị khóa | Đúng, không fallback | PASS |
| Native offline | Thông báo kiểm tra kết nối thay Unknown | “Không kết nối được dữ liệu; kiểm tra kết nối và thử lại.” | PASS |
| Native chứng chỉ không tin cậy | Thông báo chứng chỉ/tên máy chủ; vẫn từ chối TLS | Đúng thông báo mới, không vàoMainForm | PASS |
| Native mật khẩu sai/đúng và logout | Sai bị từ chối riêng; đúng vàoADMIN; logout trảLogin trống |3/3đúng | PASS |
| Build/publish/inventory | Debug/Release/publish thành công; package sạch helperbypass/pathriêng |0error, cóNU1900; inventorySucceeded/0issues;94payloadhashđúng | PASS |
| Cố định source/package trong regression | Hash trước/sau bằng nhau |460filekhôngđổi; archivekhôngđổi | PASS |
| Cleanup | Dọn đúng DB/login/secret của lượt |1DB/1SQLlogin/DPAPI đãdọn;0businesswrite;khôngtruycậpdevelopment | PASS |

Unit chạy với filter loại SQLIntegration/G02 cần fixture riêng, vẫn giữ các unit Auth/Catalog trong namespace tương ứng. Không gọi142unit là đủ325ID hoặc đủ mọi native/faultcase. Build có cảnh báoNU1900 do feed audit không truy cập được; không tuyên bố vulnerability audit sạch.

## Lượt kiểm có expected đặt sai giai đoạn

Giữ nguyên `dal-bll-regression.json`: lệnh đầu đã đặt expected“mọi đường ghi đều RequiresReconciliation=true”, nên có2FAIL ở trường hợp OpenAsync thất bại trước gửi. Đối chứng source/hash từ acceptance xác nhận guard`writing && sent` đã có từ trước và không đổi. Đây là sai expected của lệnh kiểm thủ công, không phải lỗi sản phẩm mới.

Lượt đó được giữ nguyên, không ghi đè FAIL hoặc sửa implementation để thỏa expected sai. Scenario mới ở `before-send-contract/` chạy lại thật theo giai đoạn trước gửi và4/4PASS. Các unit riêng cho mapper nhận `writing=true` tiếp tục xác nhận flag đối chiếu vẫn true. Không suy rằng đã kiểm native timeout saucommit; phần đó còn BLOCKED ởT31-K06.

## Phiên bản và bằng chứng

Lượt [regression_b01_20261102_171805_94f20337](../runs/regression_b01_20261102_171805_94f20337/manifest.json), [142unitTRX](../runs/regression_b01_20261102_171805_94f20337/b01-unit.trx), [native7ca](../runs/regression_b01_20261102_171805_94f20337/native.json), [DAL/BLL trước gửi](../runs/regression_b01_20261102_171805_94f20337/before-send-contract/results.json), [đối chứng expected/guard cũ](../runs/regression_b01_20261102_171805_94f20337/dal-bll-expectation-investigation.json).

[Source/package freeze](../runs/regression_b01_20261102_171805_94f20337/freeze-audit.json), [phạm vi hai file](../runs/regression_b01_20261102_171805_94f20337/change-scope.csv), [cleanup](../runs/regression_b01_20261102_171805_94f20337/cleanup.json). SDK10.0.401, Windows x64; fixture clean schema027 trên SQLServerlocalhost. Hash source thực ởsource-before/source-after.csv; không coi HEAD nền8a38f8691d37089feec149b71a8f5718d37167f3 là toàn bộ workingtree đã chạy.

Package mới: `artifacts/g08/20261102_171805_94f20337/candidate.zip`, SHA256 **DBE592A647904A4FF2F06AA6FB29E14D7292C487F86D722AFE33B0EEDFD3C436**. Win-x64/framework-dependent, vẫn là ứng viên. Package và acceptance cũD1F7A782… được giữ làm lịch sử; không patch bản cũ.

T30-K07/UX-K44 được nângPASS theo regression này; T30 hiện9/9PASS. TổngG08 **16PASS/0FAIL/13BLOCKED** có kế thừa phạm vi không bị ảnh hưởng của lượt cũ, không phải29ID đã chạy lại trọn vẹn trên package mới. Hình thứcSKIPPED theo phân công;100/150%DPI NOTRUN; các blocker native/modal/fault/legacyflags còn nguyên. Chưa commit/push.

## Bản đóng gói sau chốt hồ sơ

artifacts/g08/20261102_215232_0a0b72a5/candidate.zip —SHA256 **61AD7FC48DF29E79398F9CD7706F9283594BB5A4FBA83295B772771201E4AE12**. [Đối chiếu](../runs/regression_b01_20261102_171805_94f20337/handoff-package-parity.json) xác nhận87file app/SQL/công cụ không đổi so với binary đã chạy native;hồsơ bàn giao cập nhật số16PASS/0FAIL/13BLOCKED. Package mới tạo sau khi kết thúc regression,khôngpatcharchiveđangtest.

