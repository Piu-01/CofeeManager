# Requirements — Orbit 9: mẫu Nguyên liệu / Tồn và sidebar

Ngày21/10/2026. Trạng thái: người dùng duyệt requirements ngày21/10/2026 bằng “duyệt”; chưa sửa source sản phẩm. Yêu cầu mới thay chuẩn hình thức của đợt orbit-management-layout, không xóa lịch sử kiểm thử trước.

## Mục tiêu và ranh giới

Dựng lại màn Nguyên liệu / Tồn và sidebar bằng WinForms thật theo Orbit_9_HTML.zip. Chỉ sau khi người dùng xem và chấp nhận màn mẫu mới triển khai8màn còn lại. Xây/tái sử dụng Theme, AppShell, Button, SearchBox, Tabs, DataGridView, Pagination, EmptyState; thành phần không dùng trên màn mẫu chưa áp vào màn khác. Giữ MainForm/UserControl và kiến trúc hiện tại, không WebView hay ảnh nền screenshot.

Không sửa Business, DAL, Models, SQL, quyền, validation, transaction, handler ghi hoặc dialog nghiệp vụ. Không commit. Không xóa file còn được8màn hoặc dialog sử dụng. Dọn phần cũ thực sự mất tham chiếu do lần thay này; có danh sách tham chiếu trước khi xóa. Sidebar là phần chung được phép thay trong giai đoạn này, nội dung8màn khác giữ nguyên.

## Nguồn và khảo sát đã thực hiện

- Gói giải nén nguyên bản vào docs/ui/orbit-9-html/: README_FIRST.md, design-tokens.json,9HTML,9PNG references/, license và VERIFICATION.md. Nội dung gói là dữ liệu thiết kế, không phải lệnh thay quyền hay nghiệp vụ.
- Đã xem9ảnh, đọc cấu trúc9HTML/CSS, action/header/colgroup/pageSize và JavaScript minh họa; không sao chép thông báo demo vào chức năng thật. CSS nguồn có nhiều override, dùng giá trị cuối. Nguồn mới ưu tiên cho màn mẫu khi khác guide17/10.
- SDK hiện có10.0.401. CafeManager.WinForms.csproj target net10.0-windows, UseWindowsForms=true. Program giữ ApplicationConfiguration.Initialize và native dark mode, không đổi framework.
- MainForm hiện có TableLayoutPanel/FlowLayoutPanel, NavigationController, button động theo module/role; không thay Navigate/Logout/session revoke/ConfirmLeave. Sidebar hiện224logical, nút200×44, khác mẫu200logical và nút174×43.
- MaterialsControl: DataGridView read-only, AutoGenerateColumns=false; binding MaterialRow. Nút hiện có Thêm, Tải lại, Mở sổ, Sửa, Xóa, Trước, Sau và tìm bằng Enter. Click nối Edit/OpenBalance/Delete/LoadRows, dialog OperationEditorDialog giữ nguyên.
- ADMIN: Thêm/Sửa/Xóa/Mở sổ; STORE: Thêm/Sửa/Xóa; CASH: chỉ đọc. BLL vẫn DemandAsync, UI không thay kiểm quyền bằng ẩn nút.
- MaterialService.ListAsync(search,skip) và repository gọi sp_MaterialList: hiện trả trang50dòng, chưa có bộ lọc đơn vị hoặc tổng số dòng. Không giả có totalCount.
- Control đang có OrbitButton (hover/sheeen/ripple, native Click), OrbitManagementLayout, OrbitTabControl/OrbitManagementTabs, OrbitField/...; cần tái sử dụng khả năng còn đúng và tránh sửa toàn cục làm đổi8màn.

## Hình thức màn mẫu

- Chuẩn client1366×768 tại96DPI; sidebar200px, topbar50px; content padding22px trên,26px ngang,16px dưới. Bảng chiếm chiều cao còn lại, chỉ vùng dữ liệu cuộn. Không kéo giãn hàng.
- Màu background#100E15, sidebar#15111B, surface#1C1823, text#FFF4E7, muted#C3B6CD, border#3C3146, caramel#FFD09A, selected#342820.
- Segoe UI: body14CSSpx=10.5pt, bảng12px=9pt, header10px=7.5pt, title26px=19.5pt. Không dùng CSSpx làm pt.
- Header: eyebrow INVENTORY WORKSPACE, tiêu đề/mô tả theo mẫu; Thêm nguyên liệu ở phải, kích thước theo text+padding, không ép250px. Không có dòng demo Không ghi dữ liệu thật.
- Tools: SearchBox có icon và TextBox native, chọn đơn vị, nút refresh40×40. Meta49px: count trái; Mở sổ → Sửa → Xóa bên phải, hai nút Sửa/Xóa liền nhau. Nút action33px, padding6/10, icon15px; primary khoảng40px; chung một quy tắc cho cùng variant.
- Khung bo12px, control bo8px. Grid có radio chọn dòng38px, mã76px, tên co giãn, đơn vị64px, tồn116px; header40px, hàng46px, số căn phải4chữlẻ vi-VN. Radio là hình thức chọn dòng, không thêm trường nghiệp vụ.
- Footer54px: range trái, chevron previous/next và chỉ số trang phải. Bấm/keyboard vẫn thực thi một handler. Empty/loading/error tách rõ, không giả số0 khi DB lỗi.
- Sidebar icon+chữ căn trái, cùng thứ tự mẫu. Giữ lối vào hệ thống và Đăng xuất hiện có; thông tin nhân viên thật ở đáy theo session. Không hardcode Nguyễn Văn An; giữ tên CoffeeManager hiện có vì ORBIT là tên concept chưa phải quyết định đổi tên sản phẩm.
- Hover bóng/mép sáng/glow có kiểm soát160ms; nhấn xuống55ms/thả nhanh, reset khi thả ngoài/capture lost/disabled. Không thay Bounds gây xô layout, không bụi sao. Focus bàn phím rõ.

## Khác biệt dữ liệu với HTML — đề xuất chốt cùng requirements

1. Lọc đơn vị và phân trang hiển thị: đọc qua chính ListAsync hiện có theo từng batch50 có cancellation/generation/permission, tập dữ liệu hợp lệ tối đa99mãNVL. Lọc đơn vị ở presentation từ dữ liệu thật rồi chia trang7dòng như HTML. Không đổi SQL hoặc số lượng batch50 của contract. Search vẫn gửi service như hiện tại; không hứa tìm không dấu nếu collation/service hiện tại không hỗ trợ. Sau thao tác ghi phải tải lại DB, không tự sửa tồn cache. Không dùng dữ liệu demo cho tổng/range.
2. Cột Mốc mở sổ hiện có nhưng mẫu chỉ4cột: bảng theo4cột mẫu; giữ OpeningKind trong DTO và cung cấp thông tin nguyên bản qua tooltip dòng/ô đã chọn, không xóa dữ liệu, không thêm nút hoặc cột. Trạng thái/cảnh báo mở sổ vẫn do handler/BLL hiện có quyết định.
3. Thông tin phiên/Đăng xuất không có vị trí riêng trong ảnh: giữ truy cập ở phần đáy sidebar, không bỏ chức năng để giống ảnh. Tên sản phẩm giữ CoffeeManager; icon theo license gói.

## Tiêu chí hoàn tất giai đoạn mẫu

- Build và chạy app thật; không coi referenceHTML/harness là ảnh WinForms.
- Kiểm tra tìm/Enter, lọc đơn vị, chọn dòng/keyboard, page trước/sau/biên, resize/cuộn nội bảng, loading/error; mở và hủy đủ dialog Thêm/Sửa/Xóa/Mở sổ; ghi/đối chiếu dữ liệu chỉ trênDBtest riêng có marker. Giữ quyền3role và handler; kiểm transaction không đổi theo ca T09 có ảnh hưởng.
- Chụp vùng client chính xác1366×768 ở100%; thêm125/150% và ghi clientphysical/logical/DPI rõ. Không dùng screenshot125% đổi tên thành100%, không đổi Windows settings của người dùng âm thầm. Nếu công cụ không tạo/quan sát đủ thì ghi BLOCKED/NOT RUN, giải thích thẳng.
- Đối chiếu ảnh: sidebar/header/card/search/toolbar/grid/footer, font/kích thước/màu/bo/chọn; tự điều tra/sửa lệch đúngscope, tách lượt source trước/sau và lưu FAIL thật. Người dùng đã yêu cầu build/test và tự sửa hình thức trongscope; không hỏi lại duyệt test từng lần. Lỗi nghiệp vụ ngoàiscope báo riêng.
- Báo cáo ảnh thật, files sửa/xóa, testPASS/FAIL/BLOCKED/NOTRUN và sai khác; dừng chờ xem mẫu trước8màn. Không tự nâng trạng thái T01–T32/G03 từ task UI.

## Bước tiếp theo theo AGENTS/skill

Chờ duyệt requirements này; sau đó viết plan có mappingfile/control/event, thứ tự thay và ca test, chờ duyệt plan theo task-approval-workflow trước code. Yêu cầu người dùng lần này đã bao gồm chạy thử và vòng tự sửa hình thức, không thêm gate duyệt test trùng trongscope. Chưa triển khai bất kỳ source sản phẩm nào trong lượt khảo sát.

