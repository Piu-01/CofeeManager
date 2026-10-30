# G05 — Kết quả test 27/10/2026, chờ duyệt đợt bug

**G05 Đang làm: 38 PASS / 5 FAIL / 10 BLOCKED / 2 NOT RUN trên55ID gốc.** Requirements, bộ plan, nâng development và đợt test đều đã duyệt. Chưa duyệt debug các bug mới; source/migration giữ nguyên trong lượt test.

| Task | PASS | FAIL | BLOCKED | NOT RUN | Biên bản |
|---|---:|---:|---:|---:|---|
|T19|6|0|3|1|[Thu chi](t19-thu-chi/test-results.md)|
|T20|8|1|2|1|[Nhập kho](t20-nhap-kho/test-results.md)|
|T21|7|1|2|0|[Xuất kho](t21-xuat-kho/test-results.md)|
|T22|7|1|3|0|[Kiểm kê](t22-kiem-ke/test-results.md)|
|T23|10|2|0|0|[Công nợ](t23-cong-no/test-results.md)|

Giữ nguyên55ID/expected và325ID toàn kế hoạch. Nhánh chỉ đạt SQL nhưng thiếu native không tự nâng PASS. G04 vẫn40PASS/0FAIL/16BLOCKED, T18 kế thừa8/8; không suy PASS16ca thiếu.

## Development

Sau khi được phép dừng đúng PID23728 của bản test G04 chạy nền, đã nâng **localhost/CoffeeManager_Development từ016 lên021**. Không seed/reset tài khoản/ghi fixture vào development. [Manifest](runs/development021_20261027_112555_5d4d8de7/manifest.json).

Script có lỗi đo nguồn tài chính bị cắt JSON (G05-B04). Đã kiểm bổ sung bằng bản sao riêng từ backup trước nâng: **8/8 hash đầy đủ khớp** cho kho, điểm/hạng, tài khoản, tiền hóa đơn, thu chi, phiếu nhập, nợ và lịch sử trả. [Đối chiếu đầy đủ](runs/acceptance-full-20261027_110730/development-backup-verification.json). Bản sao đã dọn; backup COPY_ONLY CHECKSUM gốc giữ tại SQL Backup theo manifest. Không kết luận mất dữ liệu từ lỗi đo kiểm, cũng không dựa vào chunk đầu để kết luận bảo toàn.

## Kết quả thực chạy

Windows build26200, SDK10.0.401, SQL Server2025 Developer17.0.1000.7, schema001–021. [Môi trường](runs/acceptance-full-20261027_110730/environment.json). Hash304 file src/database/tests/tools trước/sau **0 thay đổi**: [freeze](runs/acceptance-full-20261027_110730/source-freeze-verification.json). Harness mới nằm trong thư mục run, lưu phiên bản riêng.

- Build Debug/Release trước test:0warning/0error ([Debug](runs/final-build-20261026/debug.log), [Release](runs/final-build-20261026/release.log)). **22/22 xUnit PASS** ([log](runs/acceptance_20261027_110753_fb67dcbb/unit.log)); clean/demo qua001–021 và5 SQL replay subcase PASS. Không thay55ID bằng build/unit.
- **104 SQL subcase mở rộng PASS**: precision/FK/quyền, bất biến, trả0/một phần/đủ, NULL/0, ABA, cutoff, unique và fault rollback. [SQL](runs/acceptance-full-20261027_110730/sql-extended-v2-results.json). Public procedures dùng EXECUTE AS G05Runtime WITHOUT LOGIN/CafeManagerApp; DML âm dùng owner được ghi riêng.
- **11 ca đồng thời PASS** với hai SqlConnection thật và barrier thứ ba; DMV ghi nhận cả hai chờ khóa trước thả. Nhập chung NVL, hai xuất/checkout tranh tồn, nợ cuối, cùng KK và KK/checkout: [9ca](runs/acceptance-full-20261027_110730/concurrency-v2-results.json), [2ca KK/checkout](runs/acceptance-full-20261027_110730/concurrency-kk-results.json).
- **8 ca timeout trước/sau commit PASS**: trước commit receipt0/snapshot giữ nguyên; sau commit receipt1/replay không đổi. DMV xác nhận session sau timeout không còn transaction. [Timeout](runs/acceptance-full-20261027_110730/timeout-v4-results.json). Chưa thay kiểm crash/restart/pending của app.
- SQL bổ sung: tổng không cộng trả nợ hai lần; source nợ, đếm0, checkout không tạo Thu, hủy đơn rồi xuất hủy không có tiêu hao bán: [bổ sung](runs/acceptance-full-20261027_110730/sql-supplement-v2-results.json), [nguồn độc lập/checkout](runs/acceptance-full-20261027_110730/sql-supplement-v4-results.json), [quá hạn/legacy](runs/acceptance-full-20261027_110730/sql-final-results.json).
- Hai clone legacy âm: mapping017 thiếu và PN018 rỗng dừng đúng52607/52707, không ghi version/cột mới, hash nghiệp vụ không đổi; cả hai đã dọn ([017](runs/acceptance-full-20261027_110730/legacy-Mapping17-results.json), [018](runs/acceptance-full-20261027_110730/legacy-Empty18-results.json)).
- Native WinForms đăng nhập thường ADMIN fixture; ghi Chi1234 qua dialog và xác nhận lồng, commit/reload TC20250521, đối chiếu NV991/OriginManual. Mở workspace thật và lưu AX/stack trace lỗi. Không nghiệm thu hình thức bằng screenshot/Designer.

Lỗi harness và phiên bản đo xem [instrumentation](runs/acceptance-full-20261027_110730/instrumentation.md). Hai record FAIL supplementv2 là fixture unique PN chặn trước nhánh tiền; giữ nguyên, kiểm lại bằng PN độc lập và expected không đổi. Không cộng dồn record lỗi/trùng phiên thành số ID PASS.

## Đợt bug đề nghị duyệt một lần

| Bug | Phạm vi đề xuất |
|---|---|
|[G05-B01](t20-nhap-kho/bug-report.md)|Workspace thiếu nút lập phiếu/nhóm thao tác/filter. Debug layout/visibility khi attach tab, sửa cấu hình workspace G05 theo helper; chỉ đổi helper khi có bằng chứng, hồi quy các màn dùng chung.|
|[G05-B02](t22-kiem-ke/bug-report.md)|Kiểm kê crash khi LinesJson NULL của header rỗng ca âm. Mapper đọc tập rỗng an toàn, vẫn cấm chốt rỗng.|
|[G05-B03](t23-cong-no/bug-report.md)|Nợ hợp lệ chưa có lần trả có HistoryJson NULL làm crash. Mapper trả lịch sử rỗng, giữ số dư/quyền.|
|[G05-B04](t19-thu-chi/bug-report.md)|Script nâng chỉ so sánh chunk JSON đầu. Sửa đo toàn bộ dữ liệu và kiểm tập lớn; development đã kiểm bổ sung8/8 khớp.|

Đề nghị duyệt **cả bốn bug và regression theo ảnh hưởng**. Chưa sửa các lỗi này. Không đổi nghiệp vụ/quyền, không sửa migration017–021 đã áp dụng, không refactor ngoài nguyên nhân. Bug mới hoặc đổi contract phải duyệt bổ sung.

## Còn thiếu

Hai ID NOT RUN: nhánh tên quá dài T19-K02 và actor/nhân viên không tồn tại T20-K02, dù các nhánh âm khác đã PASS. Các BLOCKED còn nháp/editor/preview/source links, role native và thời gian đếm do lỗi chức năng hoặc công cụ nhập hộp con. Tiếp tục các nhánh này sau debug; không suy PASS từ source.

UX mở rộng chưa đủ: crash/restart/pending đa process/lỗi I/O; Enter/Esc/busy/session; select-all/caret/grid editor; owner native print/file; hai khoản nợ độc lập không khóa toàn cục; tập legacy-invalid ngày/nguồn. DeviceDpi chưa đo xác nhận: **chưa nghiệm thu125%**,100/150% NOT RUN, không đổi Windows Scale. Hình thức SKIPPED theo phân công, không chặn chức năng. MaBL fixture chưa thay tích hợp lương T26.

## Tài nguyên và lịch sử

Acceptance tạo5 DB riêng, **dọn5/còn0**; GUI PID27396 đã thoát, thư mục credential DPAPI fixture đã xóa, không đụng pending tài khoản thật. [Audit](runs/acceptance-full-20261027_110730/resource-audit.json), [cleanup](runs/acceptance-full-20261027_110730/cleanup.json). Không đổi SQL service/authmode, backup development được giữ.

Báo cáo trước test giữ tại runs/acceptance-full-20261027_110730/pre-test-group-report.md; biên bản NOT RUN cũ lưu cùng run. Technical-checks và17/17 DB kỹ thuật ngày26/10 vẫn là lịch sử riêng. Không commit/push; G05 chưa Hoàn tất.

