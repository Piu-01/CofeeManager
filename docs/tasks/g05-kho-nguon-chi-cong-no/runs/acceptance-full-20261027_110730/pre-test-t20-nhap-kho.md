# T20 — Biên bản nghiệm thu chưa chạy

26/10/2026. Source đã triển khai; chờ duyệt đợt test G05. Build/compile/smoke kỹ thuật lưu riêng, không thay actual của các ID dưới. Môi trường nghiệm thu, schema/hash, dữ liệu, lệnh và bằng chứng sẽ điền khi chạy. Hình thức SKIPPED theo phân công; DPI100/150% NOT RUN.

| ID | Expected gốc | Actual / dữ liệu / thao tác | Kết quả | Bằng chứng |
|---|---|---|---|---|
| T20-K01 | [M/I] Soạn nháp/đóng dialog không đổi kho; chốt n dòng cộng đúng tồn và tổng dòng làm tròn. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T20-K02 | [I] NCC/nhân viên sai, lượng 0/âm, giá 0/âm, NVL trùng cùng phiếu bị từ chối. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T20-K03 | [I] Một dòng lỗi/thiếu quyền rollback header, mọi chi tiết, kho, chi và nợ. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T20-K04 | [I] Trả ngay 0 tạo nợ toàn bộ, không chi 0; trả đủ tạo một chi, không nợ 0. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T20-K05 | [I] Trả ngay một phần: chi trả ngay đúng phần, nợ đúng tổng trừ phần đã trả. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T20-K06 | [I] Trả ngay âm/vượt tổng hoặc dòng chi sai loại bị chặn. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T20-K07 | [I] Nhấn/gọi lại cùng yêu cầu không nhập kho/chi lần hai; đối chiếu theo mã nguồn khi timeout. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T20-K08 | [I] Phiếu chốt không sửa/xóa header/NCC/ngày/chi tiết; trigger cũ không cho bypass sửa lịch sử. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T20-K09 | [I] Baseline có seed được backfill cờ chốt đúng, không chạy trigger cộng tồn lần nữa. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T20-K10 | [I] Hai phiên nhập cùng NVL cập nhật tồn đúng, không lost update. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T20-K11 | [U/M] STORE nhập được, CASH không; trả ngay/ghi tài chính chỉ ADMIN hoặc quy trình được cấp quyền rõ. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T20-K12 | [M] Tính tổng preview khớp SQL và sau reload nguồn chi/nợ hiển thị đúng. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
