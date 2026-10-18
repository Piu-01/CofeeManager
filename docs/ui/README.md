# Orbit Café — UI handoff

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
