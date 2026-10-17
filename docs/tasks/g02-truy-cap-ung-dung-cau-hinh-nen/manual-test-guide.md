# G02 — Chuẩn bị thao tác nghiệm thu native / SQL

Đợt test đã được duyệt và đã chạy nhiều lượt; actual ở test-results.md từng
task, report.md và remaining-cases.md. Hướng dẫn này không là biên bảnPASS.
Giữ source/migration cố định trong từng lượt. Các
subcase bắt buộc trong ba plan là danh sách nghiệm thu, không lấy số test xUnit
thay 27 ID gốc hoặc bỏ các nhánh chưa tự động hóa.

## SQL và runner

`tools/database/Test-G02.ps1 -Task T06|T08|All -AcceptanceApproved` tạo DB có
marker, migrations001–003 và hai user WITHOUT LOGIN riêng cho runtime/setup.
Pooling=false khi impersonate, password sinh trong RAM. Có DB riêng cho race
bootstrap và trigger rollback. TRX/stdout/stderr/exitCode/setupError/manifest
trước-sau nằm trong `runs/<run>/`. DB được giữ để đối chiếu, không tự xóa.

Test tự động hiện chuẩn bị cho: auth/permission/username/password rules,
bootstrap race/rollback/restart-lock, rehash CAS chống ghi đè password mới,
revoke phiên, CRUD/search/revision của từng7bảng, bảo vệ mãChi, hỗ trợ phương
thức và receipt replay/mismatch. Các tên test chỉ gợi ý liên kết case; actual
và evidence sau chạy mới quyết định subcase nào được đáp ứng.

Những nhánh cần fixture/thao tác SQL riêng theo plan: demo activation, nhân viên
chưa có account, legacy confirmation/khóa/nghỉ, collation CS/CI và username
preflight; migration rerun/xung đột; rehash-needed thật và các race còn lại;
3role×4thao tác×7bảng, từnglookup/field, FK/history/snapshot, fault ngắtresponse.
Dùng `New-IsolatedDatabase 'G02' <run> <seed>` trong helper, kiểm marker trước
fixture; schema→seed nếu demo→001→002→003. Hash legacy/old-version tạo bằng
PasswordHasher trong RAM tại máy, không ghi password/hash vào transcript.
Fixture chỉ owner DB test, thao tác app qua principal hạn chế; không thêm quyền
runtime để né DENY. `database/tests/t08/fixture-guard.sql` là guard dùng trước
các fixture lịch sử, không phải fixture nghiệp vụ đầy đủ hoặc chứng cứ PASS.

## Native UI

1. Chọn DB test có marker và setup/admin/CASH/STORE hợp lệ. Provision Windows
   setup/runtime riêng theo contract; nhập password tại app, không trong chat.
2. Chạy app thật. Dùng thao tác trong plan T07/T08 cho3role,7editor, hủy/lưu/
   nháp/unknown,100chuyển màn và10login/logout. Ghi counts từng loại control,
   callback/request và SQL DMV trước/sau; HandleCount chỉ phụ trợ, không thay
   phép đo instance/event/connection. Có thể dùng debugger + weakref test
   harness để kiểm các instance đã dispose sau forcedGC; không thêm telemetry
   sản phẩm hoặc kết luận từ native handlecount đơn thuần.
3. Mỗi DPI100/125/150 chạy cả1280×720,1366×768. Chụp app native, ghi actualbounds,
   scroll/minimize/restore và keyboard tại mỗi combo. Collector có sẵn:
   `tools/ui/Verify-G02.ps1 -ProcessId <PID> -Width 1280 -Height 720
   -DpiPercent 125 -AcceptanceApproved`. Collector không đổi DPI, không đọc
   password/controltext và chỉ chụp MainForm. Capture là evidence, chưa PASS.
4. Designer mở LoginForm/BootstrapForm/MainForm/CatalogEditorDialog/
   CatalogsControl: Save→close→reopen. Constructor không mởSQL. So sánh source
   manifest; serializer thay source thì kết thúc lượt trước, ghi thay đổi,
   build và chụp manifest mới để test bản sauSave. Không vá trong lượt nghiệm thu.
5. Trả displayDPI125% sau lượt. Lưu từng expected/actual/status/evidence trong
   `test-results.md` tại task, báo cáo chungG02; lỗi ghi task sở hữu, chờ duyệt
   cả đợtbug trước debug.
