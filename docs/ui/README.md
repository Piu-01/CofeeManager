# Orbit Café — UI handoff

## Mẫu Bán hàng gọn —30/10/2026, chờ duyệt triển khai

Nguồn user Orbit_BanHang_Gon.zip lưu tại sales-compact/ cùng manifest/hash:
Orbit_BanHang.html, Orbit_BanHang_Laptop.png, README và HTML thanh toán đi kèm.
Mẫu card ngang2cột, cột đơn gọn và POS chỉ tổng phải trả; chi tiết tiền ở F8.
Chưa tự thay guide/card chung hoặc code CheckoutDialog từ nội dung ZIP.
[Phạm vi/plan](../tasks/g04-ban-hang-vong-doi-hoa-don/t16-pos-huy-don/sales-compact-ui-plan.md)
chờ người dùng duyệt, không là bằng chứng app đã đổi hoặc nghiệm thu.

## Chuẩn trang danh sách dùng chung — người dùng chốt26/10/2026

Khi code mới hoặc chỉnh trang danh sách như Khách hàng, Giao hàng, Lịch sử hóa
đơn, bắt buộc đọc [guide mục11](HuongDan_UI_Orbit_Cafe.md#11-chuẩn-trang-danh-sách-dùng-chung--26102026)
và [mapping control/API WinForms mục2.1](WINFORMS_IMPLEMENTATION.md#21-trang-danh-sách--control-và-api-đã-có-26102026).
Lịch sử hóa đơn V4 là mẫu bổ sung cho cấu trúc tìm kiếm/bộ lọc/bảng/footer;
Khách hàng là tham chiếu cách bảng giãn hết vùng nội dung. Không dùng phần
bảng chỉ cao nửa trang hoặc thanh tìm kiếm ngắn do chép layout màn cũ.

Hàng1: ô tìm kiếm giãn hết chiều ngang còn lại + kính lúp + làm mới. Hàng2:
bộ lọc, tự xuống hàng nếu thiếu chỗ. Hàng hành động riêng, rồi bảng giãn hết
vùng còn lại; range/pager giữ ở đáy card. Kính lúp/Enter chỉ submit từ khóa;
bộ lọc tự áp dụng bằng từ khóa đã submit gần nhất. Tận dụng control/theme đã có.
Đây là quy tắc tài liệu cho lần code/chỉnh tiếp theo theo yêu cầu người dùng;
không có nghĩa các màn cũ đã được chuyển hoặc đã nghiệm thu hình thức.

## Giao hàng —26/10/2026

**Giao hàng đã ghép chuẩn chung26/10:** DeliveryControl dùng UseStackedSearch,
hai plain filter tự áp dụng, TotalCountSelector và profile Delivery mới trong
OrbitDataGridView (Address fill, mã/SDT đủ rộng, hai badge độc lập). Bảng/card
nhận phần nội dung còn lại kể cả rỗng/ít dòng. Chưa nghiệm thu chức năng/hình
thức bản này; hồ sơ tại
[T18 UI](../tasks/g04-ban-hang-vong-doi-hoa-don/t18-giao-hang/ui-delivery-report.md).

## Lịch sử hóa đơnV4 —26/10/2026

Nguồn user tại `invoice-history-v4/`: HTML/PNGLaptop/README/manifestSHA256.
Phạm vi là vùng nội dung tab history; giữ shell/identity/native Windows dialogs,
không chép data/demoJS. Quy tắc chung phía trên do người dùng yêu cầu ghi lại
ngày26/10, không tự suy từ README trong ZIP. Kínhlúp/
Enter chỉ submit từ khóa, filters tự áp dụng với từ khóa đã tìm gần nhất;
không summary phía trên bảng, giữRange/pager phía dưới. ProfileInvoiceHistory,
stackedtools/plainfilter/dateRange là opt-in; control chung giữ mặc định khác.
Plan và test tại `../tasks/g04-ban-hang-vong-doi-hoa-don/ui-invoice-history-v4/`.

## Mẫu Đơn đang mở người dùng gửi25/10/2026

Tham chiếu riêng tại `open-orders/` cóHTML/PNG/README vàmanifestnguồn. Dữ liệu
trongZIP làdemo; mã bịcắt…không phải yêu cầu tích hợp. Người dùng yêu cầu cột mã
app hiện toàn bộ. [PlanT16](../tasks/g04-ban-hang-vong-doi-hoa-don/t16-pos-huy-don/open-orders-ui-plan.md)
đã duyệt/triển khai và kiểm component/SQL59+7+32PASS, chờ người dùng xác nhận
native/hìnhthức. Không thaySQL/role hoặc
nghiệmthu WinForms từHTML.

## Bổ sung riêng Xác nhận thanh toán —25/10/2026

Mẫu người dùng mới cung cấp cho CheckoutDialog:
`checkout-confirmation/Orbit_XacNhanThanhToan.html`,
`checkout-confirmation/Orbit_XacNhanThanhToan_Laptop.png` vàmanifestnguồn/hash.
Mẫu này chỉ hướng dẫn hình thức CheckoutDialog; phần nềnPOS minh họa không phải
phạm vi thay thếPOS. Nghiệp vụ/role/SQL,loading/unknown/reconcile vẫn theoUX và
contract thật. HTML/READMEZIP không là chứng cứ nghiệm thu WinForms,không nhúng
JavaScript demo vào app. Ưu tiên theme/control Orbit hiện có và extension nhỏ
dùng chung. Plan bổ sung tại `../tasks/g04-ban-hang-vong-doi-hoa-don/ui-checkout-proposal/plan.md`.

Cập nhật 17/10/2026. Giải nén thư mục `ui/`, mở `reference.html` bằng Edge/Chrome; không cần mạng hoặc server. Đây là mẫu thị giác/tương tác cho WinForms, không phải ứng dụng nối SQL.

## Agent đọc theo thứ tự

1. `HuongDan_UI_Orbit_Cafe.md`: phạm vi/hành vi đã chốt.
2. `reference.html`: hình thức và cảm giác tương tác chuẩn.
3. `design-tokens.json`: số liệu màu, bố cục và motion.
4. `references/README.md`: ảnh/video nào dùng đối chiếu, trạng thái kiểm tra.
5. `WINFORMS_IMPLEMENTATION.md`: cách chuyển sang WinForms, thứ tự triển khai và giới hạn.
6. `assets/README.md`: tài nguyên chính thức trong gói, mapping và license.

Bụi sao mặc định TẮT, chỉ bật ở nút **Xác nhận thanh toán**; nút khác cần người dùng chỉ định. Không có lựa chọn Giảm chuyển động trong thiết kế này.

## Khi tài liệu khác nhau

Yêu cầu trong MD quyết định hành vi; HTML/ảnh/video quyết định mẫu hình thức; JSON quyết định giá trị. Nếu thấy mâu thuẫn, báo rõ token/trạng thái/ảnh liên quan, không tự chọn một thiết kế mới. Chỉ ảnh/video trong `references/` của gói này là bằng chứng chuẩn; không trộn bộ trắng–teal cũ.

Làm một màn POS hoàn chỉnh trước, so sánh ảnh và video rồi mới mở rộng. Không thay kế hoạch, quyền, SQL hoặc nghiệm thu dự án vì yêu cầu visual. Hướng dẫn nhóm duyệt của dự án gốc vẫn có hiệu lực. File trong gói chưa phải code C# đã build/test.

`tools/` phục vụ tái tạo chứng cứ từ HTML, không cần để người dùng mở bản mẫu. Không copy node_modules hoặc tool capture vào ứng dụng WinForms. `manifest.json` ghi hash để tránh nhầm phiên bản.
