# Đợt nghiệm thu VI/EN — đã duyệt và chạy một phần

Ngày 10/11/2026. Người dùng đã duyệt đợt test; kết quả thực tế có FAIL và phần
native còn BLOCKED/NOT RUN, xem `test-results.md`, `report.md`, `bug-report.md`.
Giữ nguyên phạm vi dưới đây và 325 ID T01–T32; không coi phần chưa chạy là PASS.
Unit test/build/audit trong bước 11 của plan là kiểm tra kỹ thuật triển khai, ghi riêng
trong `technical-checks.md` và `test-results.md`.

## Môi trường và dữ liệu

- Chốt source theo manifest SHA-256 trong `runs/source-manifest.csv`; kiểm tra lại
  manifest trước đợt nghiệm thu. Nếu source có cập nhật song song, tạo manifest mới
  và chạy lại kiểm tra kỹ thuật liên quan trước khi ghi kết quả.
- WinForms thật, Windows đang ở DPI 125%; 1280×720 và 1366×768. Không tự đổi Scale.
- SQL Server thật, DB test riêng từ baseline + migration tương ứng source tại lúc
  test. Không dùng DB development/DB đang sử dụng làm nơi ghi fixture kiểm thử.
- Tạo tài khoản ADMIN/CASH/STORE; dữ liệu tiếng Việt cố ý có tên `Khách Việt`,
  món/nguyên liệu/nhà cung cấp có dấu, ghi chú riêng và SDT có số 0 đầu.
- Ghi rõ schema/commit/hash, dữ liệu, thao tác, expected/actual và bằng chứng từng ca.
  Tạm giữ source cố định trong đợt; không sửa code/migration khi gặp FAIL.

## Ma trận màn hình

Mỗi hàng chạy cả VI và EN. Với màn dữ liệu, đi qua loading, có dữ liệu, rỗng,
lỗi/validation và xác nhận/unknown nếu màn hỗ trợ. Expected: tất cả chữ do app sở hữu
đúng ngôn ngữ; tên/mã/ghi chú DB giữ nguyên; binding và quyền hoạt động như trước.

| ID | Màn / nhóm | Trạng thái và trọng tâm |
|---|---|---|
| LOC-M01 | Login, cấu hình DEV | Trống, sai mật khẩu, khóa tài khoản, đang login, lỗi cấu hình |
| LOC-M02 | Bootstrap | Cài mới, chọn ứng viên, validation, unknown, xác nhận admin |
| LOC-M03 | Shell, Home ADMIN/CASH/STORE | Menu đúng quyền, breadcrumb, tải/rỗng/lỗi, dashboard ngày/tháng, tooltip/nguồn |
| LOC-M04 | Danh mục nền | Mỗi tab, thêm/sửa/xóa, Thu/Chi, mã hệ thống bảo vệ |
| LOC-M05 | Menu / công thức, bàn / khu vực | Binding editor công thức, đơn vị, trạng thái, chọn/xóa, xung đột |
| LOC-M06 | Khách / ưu đãi / điểm | Tìm, picker, vãng lai, lịch sử, policy preview, mở sổ/điều chỉnh |
| LOC-M07 | POS / đơn mở | F2/F3/F4/F8, thêm/xóa/sửa lượng/ghi chú, input nháp, xung đột |
| LOC-M08 | Checkout | Phân bổ nhiều phương thức, tiền mặt/thối, 0 ₫, validation, timeout/đối chiếu |
| LOC-M09 | Hóa đơn / preview / in lại | Tiêu đề/cột/ghi chú/số trang EN; nhiều trang, lỗi máy in, dữ liệu snapshot |
| LOC-M10 | Giao hàng | Hai trạng thái độc lập, bộ lọc, chuyển đúng bước, unknown |
| LOC-M11 | Nguyên liệu / tồn | Đơn vị DB, bộ lọc tất cả, phân trang, editor, mở sổ |
| LOC-M12 | Nhập / xuất / kiểm kê | Nháp, lượng/giá, công nợ/trả ngay, lý do, snapshot, chốt/lỗi/unknown |
| LOC-M13 | Thu chi / công nợ | Thu/Chi chỉ dịch nhãn; filter đúng code; loại xuất/nguồn, trả nợ, lịch sử |
| LOC-M14 | Nhân viên / tài khoản | Role, hợp đồng, việc làm/khóa, cấp/reset, yêu cầu chờ |
| LOC-M15 | Chấm công / lương | Batch, giờ, trạng thái, preview/chốt/chi, lịch sử và source links |
| LOC-M16 | Tài sản | Thêm/sửa/xóa, chọn tài sản, khấu hao, chi liên quan |
| LOC-M17 | Báo cáo / drill-down | Mọi ReportKind, DateBasis/Note/Coverage từ SQL, kênh bán, nguồn thu bán |
| LOC-M18 | Thông tin hệ thống | Đọc schema, lưu ngôn ngữ, Đăng xuất ngay / Để sau |

`StartupForm` không nằm trên luồng khởi động hiện tại: chỉ kiểm compile/resource trong
đợt này, không ghi PASS màn chạy thực tế từ việc dịch source.

## Ca chuyển ngôn ngữ và bất biến

| ID | Thao tác | Expected |
|---|---|---|
| LOC-F01 | Login VI→EN→VI, đang gõ username/password | Đổi chữ tại chỗ, giữ input/check Hiện mật khẩu/focus; không login tự động |
| LOC-F02 | Đang gửi login, bấm chọn ngôn ngữ | Bị khóa, không đổi ngôn ngữ giữa request, không gửi lặp |
| LOC-F03 | Lưu EN trong app, chọn Để sau rồi mở dialog mới | Phiên hiện tại vẫn VI nhất quán; file là en |
| LOC-F04 | Lưu EN, chọn Đăng xuất ngay rồi login lại | Qua luồng logout/session/quyền hiện tại, login và phiên mới EN |
| LOC-F05 | Đóng/mở app; file thiếu/hỏng/không đọc hoặc ghi được | Duy trì lựa chọn hợp lệ; mặc định VI khi đọc lỗi; báo lỗi ghi, giữ ngôn ngữ/input |
| LOC-F06 | POS có nháp/busy/unknown rồi rời màn hoặc logout | Giữ guard hiện tại, không tự bỏ nháp/hủy đơn hoặc retry ghi |
| LOC-F07 | Filter Thu/Chi, hợp đồng/role, loại xuất, kênh, trạng thái ở EN | Request/procedure nhận mã cũ; tên/ghi chú tiếng Việt trong DB không dịch |
| LOC-F08 | Thanh toán/nhập/xuất/trả nợ/chốt và chi lương ở VI và EN | Đối chiếu DB test: tiền/kho/điểm/nợ/trạng thái/receipt/link/rollback không đổi; ca đồng thời dùng hai session |
| LOC-F09 | UI và hóa đơn hiển thị 81000 ₫ và ngày 10/11/2026 | `81.000 ₫`, `dd/MM/yyyy`, giờ theo contract; không đổi decimal/request SQL |
| LOC-F10 | Dialog lồng, Esc, Tab/Enter, chọn hết khi mới focus | Owner native đúng, phím không xuyên nền, Enter không tự chốt, input behavior giữ nguyên |
| LOC-F11 | Dữ liệu DB cố ý trùng tên mã trạng thái, ví dụ tên khách `Trong` | Tên/ghi chú giữ nguyên, chỉ cột trạng thái/loại được dịch nhãn |
| LOC-F12 | EN tại 1280×720 và 1366×768, DPI 125% | Kiểm bounds/focus/tab/scroll kỹ thuật: CTA tiếp cận được, không tràn WorkingArea, không chồng/cắt nội dung bắt buộc |

## Phân công và điểm dừng

- Hình thức: **SKIPPED theo phân công**, người dùng tự kiểm; agent không chụp ảnh
  đối chiếu/hover/spacing hoặc ghi PASS thị giác.
- DPI 100%/150%: **NOT RUN**, không suy PASS từ 125%, không đổi Windows Scale.
- Sau khi người dùng duyệt đợt test một lần: chạy hết ma trận, lưu từng kết quả thực tế
  và tổng hợp kể cả FAIL/BLOCKED. Bug ghi tại task này và gom đợt xin duyệt debug;
  không tự vá trong lượt test.
- Không kế thừa PASS bàn giao G08/T31–T32 từ source trước localization. Cần kiểm lại
  các luồng bị ảnh hưởng trên bản cuối trước khi chốt bàn giao.
