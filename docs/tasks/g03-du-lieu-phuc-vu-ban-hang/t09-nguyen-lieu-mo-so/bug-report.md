# Đợt bug G03 — G03-B01 đã sửa và regression PASS

## G03-B02 — Fixture GUI chưa bootstrap — chờ duyệt bổ sung

**Kết quả cuối B02: Đã xử lý — PASS.** Người dùng xác nhận đã đăng nhập; quan sát native window4526140 titleCoffeeManager, identity Nguyen Van An·ADMIN, đủ menu G03; đã mở Nguyên liệu/Tồn và tải12dòng seed. Như vậy preflightCompleted/runtime read và login thực tế đều PASS. Không còn yêu cầu duyệt B02. Helper nhập modal bị lỗi cache là blocker công cụ khác, chưa phải bug sản phẩm đã chứng minh. Các trạng thái “chờ” phía dưới giữ lịch sử.

**Cập nhật sau duyệt18/10/2026:** đã sửa fixture ở [setup mới](../runs/g03-b02-fixture/setup.ps1), giữ lịch sử script lỗi trước. Đã dọn DB/PID cũ; DB mới chuẩn bị credential trước002, xác nhận LegacyReview bằng sp_SetupConfirmLegacy, áp dụng đến009 và runtime principal giới hạn. [State thực tế](../runs/g03-b02-fixture/setup-state.json): Completed/AdminAccountId1. Runtime EXECUTE AS USER G03Gui gọi sp_AppReadState thành công. [Regression tạm](../runs/g03-b02-fixture/regression.json):139file sản phẩm không đổi; kiểm setup/runtime PASS, login thủ công đang chờ người dùng. Chưa đóng B02 cho đến khi xác nhận login thực tế. [Manifest phiên đang giữ](../runs/g03-b02-fixture/manifest.json), có cleanup.ps1 kiểm đúng tên/marker, expiry19/10/2026. Phần mô tả dưới là điều tra trước sửa.

Ngày18/10/2026, người dùng gửi ảnh login báo cần hoàn tất setup. P2, chặn toàn bộ kiểm GUI, chưa sửa fixture/source.

**Bằng chứng:** DB test CoffeeManager_Test_G03_20261018_212608_4a54aa19_demo đã kiểm marker; [trạng thái đọc thực tế](../runs/gui-continuation-20261018/setup-state-before.json) là Pending, ActiveAdmins1. Truy vấn đầu dùng nhầm tên cột VaiTro báo lỗi đọc; lượt đúng MaVT đã lưu kết quả, không có ghi DB trong điều tra. Không lưu lại ảnh chứa mật khẩu vào evidence.

**Nguyên nhân:** setup.ps1 chạy seed với mật khẩu disabled rồi migration002, nên AppSetupState=Pending. Sau001→009 mới sao hash và kích hoạt tài khoản từ development, nhưng chưa đi qua bootstrap. AuthService.LoginAsync kiểm State=Completed trước khi xác thực nên chặn đúng contract. Đây là lỗi chuẩn bị fixture, không phải bằng chứng lỗi login sản phẩm.

**Đề nghị sửa bổ sung:** thay fixture GUI bằng DB test mới có marker, chuẩn bị credential ADMIN hợp lệ trước migration002 để nhận LegacyReview, rồi gọi sp_SetupConfirmLegacy bằng account/hash/stamp hiện hành trong SQL memory. Tiếp tục migrations→009 và tạo runtime user giới hạn như cũ. Không UPDATE AppSetupState để vượt guard, không sửa app/migration/test expected, không đổi mật khẩu hoặc dữ liệu development. Dọn đúng DB/PID cũ trước đổi phiên test; manifest ghi cả cleanup và phiên mới. Không xuất hash/connection/password.

**Regression:** đối chiếu Completed/AdminAccountId, gọi sp_AppReadState bằng runtime principal; xác nhận login thủ công vào app test, rồi tiếp tục GUI T09→T13 đã duyệt. Bootstrap fixture không được tính là nghiệm thu chức năng setup G02. Kiểm source139file không đổi, marker và tài nguyên cleanup.

**Ảnh hưởng/rủi ro:** chỉ script/evidence và dữ liệu test tạm. Tạo nhầm tài khoản sau migration002 có thể lặp lại Pending; vì vậy có preflight Completed trước launch. Chưa chứng minh lỗi sản phẩm mới. DB cũ giữ tạm chờ quyết định, expiry19/10/2026, cleanup.ps1 kiểm tên/marker; không cần người dùng thử mật khẩu thêm trong phiên này.

## Cập nhật sau duyệt — 18/10/2026

Người dùng đã duyệt đợt sửa và regression. Đã sửa `tools/database/Invoke-G03Technical.ps1`: acceptance gọi xUnit theo bộ lọc G03/MaterialRulesTests, lưu log/TRX/exit code; kiểm TRX có test và tất cả PASS; cộng SQL FAIL vào kết quả cuối; trả lỗi sau finally nếu SQL/unit thất bại, source đổi hoặc còn DB chưa dọn. Không đổi nghiệp vụ, migrations hay expected/test sản phẩm.

**G03-B01: Đã xử lý — regression 3/3 PASS.** [Biên bản](bug-test-results.md), [kết quả máy đọc](../runs/g03-b01-regression/results.json). Hai lỗi có kiểm soát đã xác nhận thực nghiệm đường trạng thái sai trước đây được ngăn chặn. Các kết quả FAIL tiêm chủ động vẫn giữ trong log; đó là expected của regression, không phải lỗi sản phẩm mới.

Phần dưới giữ lịch sử điều tra trước duyệt.

Ngày 18/10/2026. Source giữ nguyên trong đợt nghiệm thu được duyệt. Có một bug tooling; chưa chứng minh bug nghiệp vụ sản phẩm từ các phép kiểm hợp lệ.

## G03-B01 — Runner nghiệm thu thiếu unit test và tổng hợp trạng thái sai

**Mức độ:** P2. **Trạng thái lúc điều tra:** Đã điều tra, chưa sửa, chờ duyệt cả đợt bug; đã được cập nhật kết quả phía trên.

**Hiện tượng đã quan sát:** chạy `tools/database/Test-G03.ps1 -ApprovedAcceptance` tạo hai DB và chạy năm batch SQL, nhưng không chạy xUnit/không có TRX. Phải chạy riêng `dotnet test` với bộ lọc G03/MaterialRulesTests mới có 20/20 PASS. Evidence: [manifest runner](../runs/acceptance_20261018_201052_07b32f2a/manifest.json), [TRX riêng](../runs/acceptance-unit-20261018/g03-unit.trx).

**Nguyên nhân có bằng chứng source:** `tools/database/Invoke-G03Technical.ps1` vòng lặp task chỉ gọi SQL (khoảng dòng47); catch lưu FAIL (khoảng dòng51), sau đó đặt manifest.Status='Succeeded' vô điều kiện (khoảng dòng59). Không có bước gọi unit test. Vì thế đường xử lý khi SQL case lỗi có thể vẫn báo Succeeded: đây là kết luận từ source, chưa chạy tiêm lỗi vào runner trong lượt này; cả năm batch thực tế đều PASS. Không dùng nhãn Succeeded của runner làm bằng chứng toàn bộ nghiệm thu PASS.

**Đề xuất sửa trong một đợt:** sửa runner G03 để gọi unit test phù hợp, lưu exit code/TRX; tổng hợp kết quả SQL/unit và trả trạng thái/exit code thất bại nếu có ca FAIL; luôn cleanup trong finally, giữ manifest khi lỗi. Không đổi expected, source nghiệp vụ hoặc migrations; không tự chạy GUI thay người dùng.

**Regression thuộc đợt đề nghị:** đường PASS với SQL/unit đầy đủ; lỗi SQL có kiểm soát; lỗi unit có kiểm soát; kiểm trạng thái/exit code/evidence và cleanup DB trong cả ba đường. Fixture tiêm lỗi dùng bản sao tạm ở DB test riêng, không sửa test sản phẩm để tạo false green.

**Ảnh hưởng/rủi ro:** phạm vi script kiểm thử và hồ sơ kết quả. Lỗi hiện tại có thể che FAIL hoặc bỏ sót unit trong các lượt sau. Khi sửa cần tránh bỏ finally cleanup hoặc chạy unit trùng không cần thiết. Không đề xuất refactor/tối ưu ngoài nguyên nhân.

**Ngoài đợt bug:** công cụ điều khiển Windows trả active request/ảnh sai target là blocker môi trường, không phải bug app đã chứng minh. Các lỗi harness SQL đầu tiên được lưu nguyên ở evidence và phân tích trong test-results; không gộp thành bug source sản phẩm.
