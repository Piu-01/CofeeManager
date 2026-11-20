# T24 — Kết quả nghiệm thu

30/10/2026. Source đã triển khai; **chờ người dùng duyệt đợt test G06**. Không lấy smoke kỹ thuật làm PASS ca gốc. SDK10.0.401; migrations022–024; nguồn chưa commit. Môi trường/source hash/lệnh/dữ liệu/bằng chứng của đợt nghiệm thu sẽ ghi khi thực sự chạy. Hình thức do người dùng kiểm (SKIPPED); DPI100/150%NOT RUN, không đổi Windows Scale.

| ID | Expected gốc | Actual | Kết quả |
|---|---|---|---|
| T24-K01 | [I/M] Tạo/sửa full-time/part-time đúng mức lương và chức vụ; tên/SDT/FK hợp lệ. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T24-K02 | [I] Hợp đồng sai, lương âm, tên rỗng/quá dài, SDT thực trùng bị chặn; nhiều NULL hợp lệ. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T24-K03 | [I] Một nhân viên một tài khoản theo unique; username trùng bị chặn. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T24-K04 | [M/U] Reset password ghi hash mới, mật khẩu cũ không login; hash/pwd không lộ trên grid/log. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T24-K05 | [I] Nhân viên nghỉ khóa account, không xóa lịch sử hóa đơn/chấm công. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T24-K06 | [I/M] Không khóa/xóa admin hoạt động cuối cùng; bootstrap không bị mở lại để reset tùy ý. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T24-K07 | [I] Đổi chức vụ không tự đổi vai trò; đổi vai trò chỉ thao tác được cấp quyền. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T24-K08 | [I] Đổi mức lương không sửa snapshot BANGLUONG kỳ cũ. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T24-K09 | [I] Xóa nhân viên đã tham chiếu bị chặn; employee editor không tạo cascade delete. | Chưa chạy đợt nghiệm thu | NOT RUN |
| T24-K10 | [U/M] CASH/STORE gọi service quản lý nhân sự/account bị từ chối; reload hồ sơ đúng. | Chưa chạy đợt nghiệm thu | NOT RUN |
