# T19 — Biên bản nghiệm thu chưa chạy

26/10/2026. Source đã triển khai; chờ duyệt đợt test G05. Build/compile/smoke kỹ thuật lưu riêng, không thay actual của các ID dưới. Môi trường nghiệm thu, schema/hash, dữ liệu, lệnh và bằng chứng sẽ điền khi chạy. Hình thức SKIPPED theo phân công; DPI100/150% NOT RUN.

| ID | Expected gốc | Actual / dữ liệu / thao tác | Kết quả | Bằng chứng |
|---|---|---|---|---|
| T19-K01 | [I/M] Phiếu Thu/Chi thông thường số tiền dương, ngày/nhân viên/danh mục hợp lệ lưu đúng. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T19-K02 | [I] Tiền 0/âm/quá precision, tên/ghi chú quá dài và FK nguồn sai bị chặn. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T19-K03 | [I] MaPN/MaBL FK hợp lệ; hai phiếu chi cho cùng nguồn trả ngay/lương bị unique chặn. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T19-K04 | [I] Phiếu nguồn hệ thống phải loại Chi đúng, không thể đổi thành Thu. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T19-K05 | [U/M] UI thủ công không chọn danh mục trả nợ/trả ngay/lương hệ thống; chỉ ADMIN ghi chi. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T19-K06 | [I] Nguồn trùng và lỗi giữa lưu phiếu/liên kết rollback, không phiếu rác. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T19-K07 | [I/M] Chính sách sửa chi thường rõ: bản đã xác nhận không xóa/sửa tiền âm để đảo; lịch sử chi nguồn bất biến. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T19-K08 | [I] Tổng Thu/Chi theo ngày không cộng các dòng lịch sử trả nợ thêm lần nữa. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T19-K09 | [I/M] Bán hàng không đồng thời tạo phiếu thu rồi nhân đôi nguồn doanh thu. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T19-K10 | [M] Tra cứu/source link và reload hiển thị đủ loại tiền/ghi chú/mã nguồn. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
