## Cập nhật20/10/2026

Đã được người dùng duyệt và đã triển khai. Chờ duyệt đợt test tại [test-request.md](test-request.md); xem [báo cáo triển khai](report.md). Các nhãn chờ duyệt ở nội dung gốc dưới đây là lịch sử trước triển khai.

# Plan — Bố cục quản lý Orbit theo ảnh người dùng

**Ngày:** 20/10/2026. **Trạng thái:** Chờ duyệt toàn bộ plan một lần. Requirements được xác nhận: giữ cuộn trong dữ liệu và phân trang ở footer, thiết kế đúng bố cục ảnh. Không sửa source trước duyệt plan.

## 1. Contract và nguyên tắc triển khai

Nguồn: requirements.md, 10 ảnh trong hội thoại và bộ docs/ui theo thứ tự README. Giữ backend,50 dòng/trang, role/session, event nghiệp vụ, cancellation/generation/reconcile, confirm và dialog hiện có. Không sửa G03 PASS theo quyết định nghiệm thu; đây là đợt UI riêng, không triển khai G04.

Cấu trúc: sidebar/header shell → tiêu đề/mô tả/hành động chính → tab nếu có → card chứa filter, toolbar dòng, grid hoặc sơ đồ → footer. Không dùng scroll của toàn màn danh sách khi grid đủ khả năng co giãn; header/footer luôn tiếp cận được. Thiếu chiều ngang mới wrap nhóm toolbar, không thu chữ hoặc tự đổi kích thước nút theo từng màn.

## 2. Kích thước và tài nguyên chung

Giá trị logical96DPI, WinForms autoscale một lần: sidebar200px, topbar48px, pagepadding24px, gap16/8px; input40px; tab38px; nút chính200×40px, CRUDphụ88×36px, actionnhãn dài160×36px, icon/pager40×40px. Cùng chức năng cùng biến thể/kích thước ở mọi màn. Với label dài cần tăng token dùng chung trước khi khóa source, không AutoSize từng nút dẫn tới lệch màn. BodySegoeUI10.5pt, tiêu đề theo design tokens; giữ theme espresso/surface/caramel/lilac.

Tận dụng OrbitButton/OrbitHeader/OrbitTabControl hiện có. Chỉ thêm helper bố cục/theme token hẹp trong UI nếu cần; không tạo framework layout tổng quát hoặc clone các class thay thế. PNG Lucide milk/caramel20/24/32 từ docs/ui/assets; chọn đúngDPI, embeddedresource với license. Icon: navigation, search, refresh, plus, edit, trash, recipe, opening, receipts, adjustment, map/list và chevron. Giữ CoffeeManager và các mục/đăng xuất có sẵn; không chép tên/quyền/người dùng từ ảnh.

## 3. Thứ tự triển khai và file

| Bước | File dự kiến | Thay đổi và điều kiện kiểm chứng |
|---|---|---|
| 1 | Forms/MainForm.cs, MainForm.Designer.cs; UI/OrbitButton.cs chỉ nếu cần căn nội dung; Resources/Orbit | Sidebar theo ảnh, icon toàn bộ mục, chữ căn trái, active caramel; header breadcrumb/vai trò thật; giữ handlers/navigation guard/logout. Nếu bổ sung tùy chọn căn chữ OrbitButton, mặc định cũ giữ nguyên để không đổi dialog/nút khác. |
| 2 | UI/RecordListControl.cs; helper UI/OrbitManagementLayout.cs nếu cần | Tách Title/HeaderAction/Filters/RowActions/Pager; nối lại các delegate cũ đúng một lần. GridFill với cột tên/lý do co giãn, mã/số/ngày giới hạn hợp lý. State lỗi/loading độc lập với count/pager. FooterTrangN/khoảng dòng thực, previous/next theo contract50. |
| 3 | Modules/Menu/ProductsControl.cs; Modules/Customers/CustomersControl.cs (xác minh tên file trước edit) | Menu/khách theo ảnh1/5, action chính ở góc phải; category/selling giữ query thật, table/action selection rõ. Không sửa nội dung editor/recipe/use case. |
| 4 | Modules/Inventory/MaterialsControl.cs và .Designer.cs | Áp dụng bố cục chung, bỏ hàng trộn CRUD/pager; giữ trường mốc mở sổ, định dạngN4 và quyềnADMIN/STORE. Không thêm filterđơnvị thiếu backend. |
| 5 | Modules/Tables/TablesControl.cs | TabBàn/Khuvực nhẹ; toggleDanh sách/Sơđồ chiếm chung một vùng, không chia bảng + dải map170px như cũ. Card hiển thị DTO thật; selectioncard cập nhật cùng dòng grid; nút Chọn bàn gọi đúng usecaseSelectAsync hiện có, không tự mở POS. Giữ filterkhuvực/ADMINCRUD. |
| 6 | Modules/Loyalty/LoyaltyControl.cs | Ba tab theo ảnh6–9 với header/hành động chính thay theo tab, grid fullwidth. Policy không pager giả. History chưa chọn khách có empty state riêng; sau chọn giữ paging/query thật, không giả0 kết quả khi chưa chọn. Không đổi preview/chính sách/điểm/dialog. |
| 7 | Modules/Catalogs/CatalogsControl.cs và .Designer.cs | Tab7loại nhẹ dưới header; searchwide + reload, CRUDphụ, fieldcolumns theo loại như hiện có. Không đổi Kind/permission/system-expense guard/editor/unknown-write reconciliation. Giữ cả Tìm và Tải/đốichiếu qua nút/phím/tooltip đúng ý nghĩa, không gộp handler có tác dụng khác nhau. |
| 8 | docs/tasks/orbit-management-layout/report.md và bảng test | Ghi file/token/ảnh áp dụng, giới hạn và test đề nghị. Không commit/push. Chờ duyệt đợt test riêng sau triển khai. |

Chỉ đổi presentation ở WinForms, không đụng Business/DataAccess/Models/database. Các module dùng chung phải chuyển hết trước bàn giao, tránh một màn dùng token khác. Sidebar áp dụng chung nhưng nội dung Trangphiên/Hệthống/đăngnhập giữ nguyên.

## 4. Phân trang và cuộn cụ thể

- Giữ query skip0/50/100 và các filter hiện hữu; Search/tải lại theo contract hiện tại. Không thêm count query hay nạp mọi trang để tạo tổng.
- Footer báo “Trang1 · hiển thị1–14” và số dòng của lô khi có, không bịa tổngtrang. Khi backend không trả tổng, không hiển thị1/2 hoặc “/14” như mockup. Tabpolicy trả toàn bộ: chỉ count và không có pager trang trí.
- Trước disabled ở trangđầu; Sau disabled khi lô thành công<50; khi đúng50 chưa biết trangcuối, giữ Sau để kiểm lôtiếp, empty ở trang sau có đường quay lại rõ. Loading khóa thao tác gửi lặp; lỗi không ghi tổng0 hoặc tự nhảy/trộn trang mới với dữ liệu cũ.
- Grid/card cuộn xem dữ liệu trong lô; scrollbar không tải lô tiếp. Footer luôn ngoài vùng cuộn. Không thêm infinite-scroll hoặc pagesize7 chỉ để giống ảnh.
- Selection và action phản ánh đúng dòng/card hiện tại; chuyển chế độ Danhsách/Sơđồ không tự ghiDB/đổifilter/tạođơn.

## 5. Đợt test đề nghị sau triển khai (chưa chạy)

Source/hash và các layer ngoàiWinForms được đóng băng/đối chiếu trước test. Không sửa code trong lượt test; ghi bug và duyệt cả đợt nếu phát sinh.

| Mã | Cách kiểm | Expected |
|---|---|---|
| L01 | BuildDebug/Release; mở DesignerMainForm/Materials/Catalogs | Compile sạch, Designer không lỗi; không logic nghiệp vụ trongDesigner |
| L02 | So ảnh từng màn ở1366×768,1280×720;DPI100/125/150 | Bố cục tương ứng, cột tên phủ khoảng trống, không clip nút/filter/footer/sidebar |
| L03 | Đo bounds nút cùng chức năng trên từng màn cùngDPI | Width/height/font/padding/icon nhất quán; DPIscale một lần |
| L04 | NativeTab/ShiftTab/Entersearch/Escape; hover/focus/disabled | Điều hướng rõ, không gửi lặp hoặc tự ghi tiền/kho/điểm; dialog hiện có không lệch layout |
| L05 | Lô0/1/49/50/51/>100 trênDBtest qua fixture hiện có | Skip/filter đúng; footer/nextprevious/scroll không làm mất dòng, không tổng giả; lastfullpage50 có thể cần kiểmtrangtiếp |
| L06 | Search/category/selling/area/tab/cancel tải; dữ liệuít/nhiều/loading/error | Đúng handler/filter và generation; empty khácerror/chưachọnkhách, không hiển thị success/tổng0 giả |
| L07 | Togglemap/list, chọncard/dòng, Chọnbàn | DTO/state/chọnbàn đúng DB, không tạođơn/checkout mới |
| L08 | ADMIN/CASH/STORE và revoked session | Menu/actions/quyền giữ nguyên; selection/busy không mở quyền mới |
| L09 | SmokeCRUD + opening/recipe/preview/policy/adjust theo ảnh hưởng trênDBtest | Cùng usecase/confirmation/reconcile, backend source khôngđổi. Không đụng development; không lặp lại toàn bộ SQL nghiệp vụ chỉ vì sửa layout |
| L10 | Rời màn/đóngdialog/đăngxuất,100lần đổimàn nếu có helpernative ổn định | Không kẹtloading, mấtdraftguard hoặc tăngcontrol/event/timer do thaylayout |

Thiếu công cụ hoặcDPI thì ghi NOT RUN/BLOCKED, không tự coi mockup/buildPASS thay native. Ảnh kiểm tra lưu trongruns của đợt này và đối chiếu10ảnh người dùng, không ghi đè ảnh chuẩn docs/ui. Tài nguyênDBtest vàPID phải ghi manifest/cáchcleanup nếu cần.

## 6. Rủi ro và ranh giới

RecordListControl dùng chung nhiều màn nên việc tách action cần regression selection/role/lifecycle. Catalog có compact-mode và cơ chế reconcile riêng: chỉ thay layout, giữ state machine. Sidebar/nút dùng chung có thể ảnh hưởng màn khác; tùy chọn mới phải opt-in. Không đưa dữ liệu giả và không ẩn mất thông tin nghiệp vụ để khớp mockup.

Nếu ảnh có thành phần chưa có contract (unitfilter/tổngtrang/toànbộcount), giữ giới hạn ghi ởrequirements; không thêm backend ngoài phạm vi. UI mới không tự động làm các ca G03 đã miễn thànhPASS đã chạy.

**Gate tiếp theo:** người dùng duyệt plan này một lần → triển khai lần lượt toàn phạm vi → báo cáo và xin duyệt đợt test riêng. Không xin duyệt từng màn.

