# G06 — Đợt nghiệm thu đề nghị duyệt một lần

30/10/2026. **Đã được người dùng duyệt bằng “duyệt” ngày30/10/2026** theo skill post-change-test-workflow. Duyệt bộ plan trước đó cho triển khai/build/compile/smoke phụ thuộc, không thay duyệt đợt này. Đợt đã thực hiện và tổng hợp31/10/2026: xem report.md cùng test-results từng task; còn lỗi và blocker, chưa Hoàn tất.

## Phạm vi cố định

T24-K01–K10 → T25-K01–K11 → T26-K01–K12, giữ nguyên33ID và subcase trong từng [plan](plans.md). Chạy từng task, cuối đợt báo cáo chung; không gửi nghiệm thu riêng từng task. Source/migration/fixture giữ cố định trong lượt test; SHA256 trước/sau, schema/checksum, SDK/build/exe, server/DB/marker/principal và lệnh nhập vào manifest. Không chỉnh expected hoặc vá source giữa lượt test.

| Task | Nhóm kiểm và đối chiếu |
|---|---|
| T24 | Hai hợp đồng/lương/chức vụ, tên/SDT/precision/FK, NULL và trùng; unique account/username; reset hash và mật khẩu cũ/mới, không lộ secret; nghỉ việc/history; ADMIN cuối và bootstrap; chức vụ độc lập role; giữ lương cũ; xóa có tham chiếu; quyền đọc/ghi trực tiếp BLL/SQL; UI/filter/pager/late query |
| T25 | Giờ cùng ngày/qua đêm/thiếu/không hợp lệ/nghỉ-vắng; overlap chứa nhau/một phần/qua ngày/interval mở và adjacency; hai session; NV nghỉ/FK; khóa kỳ, phân kỳ cuối tháng; filter/reload/quyền; batch nhiều NV valid/invalid/trùng key/all-or-nothing |
| T26 | Full-time/part-time theo ví dụ tính tay; rounding tổng giây và tiền, không tính tăng ca hai lần; công thiếu/overlap chặn; biên kỳ/âm/precision/khấu trừ; hai chốt; snapshot giữ sau đổi hồ sơ; chặn sửa kỳ; chi đúng nguồn/unique; lỗi giữa chi/receipt; thực lĩnh0; chốt khác đã chi và không double count; quyền/timeout/refresh |

Mỗi subcase có input/expected/actual riêng; kết quả ID chỉ PASS khi đủ các nhánh bắt buộc, không suy từ một fixture hoặc build. T24-K08/T25-K08 kiểm lại với snapshot mới củaT26, không chỉ seed cũ.

## Môi trường và thứ tự

- SQL Server thật, DB riêng CoffeeManager_Test_G06_<runId>_clean/demo/legacy, marker đúng run và allowlist chính xác; runtime G06Runtime/CafeManagerApp, owner chỉ chuẩn bị/đối chiếu. Không SQLite, không ghi fixture vào development hoặc DB sử dụng. Không tạo Windows user/đổi auth/service/Scale.
- Freeze nguồn trước khi bắt đầu. Unit có giá trị tại HumanResourcesRulesTests; không test getter. Runner `tools/database/Test-G06.ps1 -RunAcceptance` chạy unit và ba SQL fixture subset trên DB riêng sau provisioning/smoke; tự cleanup và lưu manifest. **Runner này không thay toàn bộ33ID**, đặc biệt concurrency/native/legacy/fault cần kịch bản bổ sung dưới đây và từng plan. Chưa chạy runner.
- Chuẩn bị actor ADMIN1/ADMIN2/CASH/STORE qua đường hợp lệ, full/part, NV chưa/có lịch sử, ca liền nhau/qua đêm/đang mở, kỳ chưa chốt/chốt mới/legacy. Password sinh riêng không ghi log; chỉ lưu kết quả xác minh boolean, không raw credential/connection string.
- Hai session với barrier thực, lệnh/thời điểm giữ và lấy khóa: ADMIN cuối, create account, overlap, công↔chốt, hồ sơ↔chốt, hai chốt và hai chi. Đối chiếu bảng/counter/receipt, @@TRANCOUNT/XACT_STATE và bảo toàn tiền/công/phiếu sau lỗi; không chỉ Task.WhenAll hoặc nhìn UI báo lỗi.
- Fault/timeout trước/sau commit trên DB test: trigger hoặc fixture fault riêng, không thêm hook vào sản phẩm. Reconcile đúng request/mismatch/đang chạy/chưa ghi, crash/restart, file pending I/O/corrupt/DB khác/actor khác, self-change/thu hồi phiên. Nếu bị chặn môi trường phải ghi BLOCKED và nguyên nhân, không giả actual.
- Legacy upgrade: sạch/demo/clone021 hợp lệ và lỗi tên/SDT/active-NVnghỉ/overlap/nguồn lương. Invalid phải rollback migration/version và bảo toàn nguồn; valid không đổi tiền/kho/điểm/nợ/account, không giả snapshot. Chạy lại checksum đúng, checksum khác bị chặn. Hash JSON đầy đủ phía SQL.
- WinForms thật ở125%: binding/filter/pager, input/grid, select-all click/Tab và click lần2, validation, owner lồng, nháp/close/Esc/busy/unknown, Enter không chốt, late callback/dispose, phiên bị đổi/quyền mới, link lương↔chi. Component/reflection ghi riêng với native input. Client1280×720/1366×768 và bounds kỹ thuật; không chụp/so hình thức, hover/spacing hoặc Designer.
- Hình thức: SKIPPED theo phân công người dùng. DPI100/150% chưa chạy: NOT RUN, không suy PASS và không đổi Windows Scale. Các phần hình thức này không làm blocker của chức năng theo AGENTS.

## Dữ liệu chuẩn tính tay

Full-time:10.000.000 +400.000 tăng ca +500.000 phụ cấp +100.000 thưởng −200.000 trừ =10.800.000₫; công40 giờ, thường36/tăng ca4 nhưng SoGioLam tính tiền0. Part-time:36×35.000 +210.000 +100.000 +50.000 −20.000 =1.600.000₫; không cộng4 giờ tăng ca vào36 giờ thường. Thêm biên giây/giờ2 số lẻ, ROUND ở0,5VND, thực lĩnh0, tiền18 chữ số và overflow. Không dùng lại số tính từ implementation làm expected.

## Ghi kết quả, lỗi và cleanup

Ghi test-results.md cạnh từng task; report.md chung đủ33ID, kể cả FAIL/BLOCKED/NOT RUN. File lỗi ở task sở hữu; gom đợt bug để duyệt trước debug/regression, không tự sửa trong test. Giữ lịch sử G04/G05 và325ID.

Manifest exact DB/process/principal/file được tạo/dọn/còn giữ/lý do/cách dọn; kiểm name+marker và absolute path trước thao tác. Cleanup cả khi FAIL, không DROP wildcard, không dọn DB/process của lượt khác. Giữ backup theo quyết định vận hành, không xóa backup development. Chưa commit/push hoặc nâng development trong đợt này.

