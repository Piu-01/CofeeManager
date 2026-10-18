# Bàn giao Git UI Orbit — 18/10/2026

Người dùng yêu cầu push riêng phần sửa UI Orbit mới. Commit gồm theme/control,
resources/license, bố cục responsive của các màn hiện có, guide docs/ui và quy tắc
đọc guide trong AGENTS.md. Program chỉ stage hunk SetColorMode Dark.

Không đưa vào commit: cấu hình development/login fixture, tools provision/cleanup,
DB/migration, source BLL/DAL/Models, cập nhật kế hoạch, G03, thay đổi xóa guide cũ,
cấu hình local/bí mật hoặc build output. Các thay đổi đó được giữ tại working tree.

Kiểm chứng đúng nội dung Git index: checkout-index sang artifacts/orbit-push/staged,
restore app thành công, build WinForms Debug và Release đều0 warning/error.
Không chạy app hoặc tạo DB/account để kiểm commit. Whitespace check PASS khi cho
phép Markdown hard-break có chủ đích trong guide. Remote origin/main đã fetch,
không có divergence trước commit; push thường, không force.

Đây là bàn giao source theo yêu cầu, không là nghiệm thu toàn bộ UI. Kế thừa lịch
sử tại responsive-results.md: còn thiếu DPI100/150, native Snap/Designer và các ca
GUI đầy đủ. Bằng chứng binary/build snapshot và screenshot test cục bộ không được
đưa vào commit; đường dẫn tới chúng trong biên bản lịch sử có thể chỉ có trên máy
kiểm thử. Guide chính thức kèm ảnh/video reference dùng dữ liệu mô phỏng, không là
chứng cứ nghiệm thu WinForms. Không coi các manifest cũ là hash của commit mới.

Kết quả push và commit cuối được báo trực tiếp sau khi đối chiếu remote.
