# T23 — Biên bản nghiệm thu chưa chạy

26/10/2026. Source đã triển khai; chờ duyệt đợt test G05. Build/compile/smoke kỹ thuật lưu riêng, không thay actual của các ID dưới. Môi trường nghiệm thu, schema/hash, dữ liệu, lệnh và bằng chứng sẽ điền khi chạy. Hình thức SKIPPED theo phân công; DPI100/150% NOT RUN.

| ID | Expected gốc | Actual / dữ liệu / thao tác | Kết quả | Bằng chứng |
|---|---|---|---|---|
| T23-K01 | [I] NCC của nợ đúng NCC phiếu nhập và không đổi sau chốt; một PN một nợ. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T23-K02 | [I] Nợ vượt tổng nhập hoặc nợ 0 bị chặn; hạn NULL hợp lệ. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T23-K03 | [I] Trả một phần/nhiều lần/cuối cùng: đã trả/còn nợ khớp, trạng thái đúng. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T23-K04 | [I] Trả 0/âm/vượt còn nợ/phương thức sai bị chặn và không phiếu chi rác. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T23-K05 | [I] Ngày trả trước ngày nguồn nhập bị chặn; ngày hợp lệ lưu đúng. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T23-K06 | [I] TC011 thiếu/sai loại phải lỗi rõ hoặc khởi tạo trước, không chọn nhầm loại Thu. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T23-K07 | [I] Hai session trả lượng nợ cuối: không trả vượt; mã phiếu chi không trùng. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T23-K08 | [I] UPDATE/DELETE nợ có trả/lịch sử/phiếu chi liên quan bị chặn; đổi nguồn không làm chuyển NCC. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T23-K09 | [I] View quá hạn theo thời điểm hiện tại; báo cáo ngày X bỏ lần trả sau X. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T23-K10 | [I] Phiếu trả ngay khi nhập không đồng thời tính là trả nợ gốc đã giảm. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T23-K11 | [U/M] ADMIN trả được, STORE chỉ xem, CASH không; timeout đọc lại mã chi không retry mù. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T23-K12 | [M] Reload xem được mọi lần trả, phương thức và link nguồn phiếu chi. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
