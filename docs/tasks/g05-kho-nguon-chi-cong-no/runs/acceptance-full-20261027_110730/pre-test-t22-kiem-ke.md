# T22 — Biên bản nghiệm thu chưa chạy

26/10/2026. Source đã triển khai; chờ duyệt đợt test G05. Build/compile/smoke kỹ thuật lưu riêng, không thay actual của các ID dưới. Môi trường nghiệm thu, schema/hash, dữ liệu, lệnh và bằng chứng sẽ điền khi chạy. Hình thức SKIPPED theo phân công; DPI100/150% NOT RUN.

| ID | Expected gốc | Actual / dữ liệu / thao tác | Kết quả | Bằng chứng |
|---|---|---|---|---|
| T22-K01 | [I/M] Tạo snapshot đúng tồn tại thời điểm lấy, chưa chốt chưa đổi tồn. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T22-K02 | [I] Số đếm 0/bằng tồn/khác tồn hợp lệ; âm/quá precision/NVL trùng bị chặn. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T22-K03 | [I/M] Khác tồn yêu cầu lý do, thiếu lý do bị từ chối. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T22-K04 | [I] Hôm nay/chưa chốt/snapshot khớp: điều chỉnh đúng một lần. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T22-K05 | [I] Chốt lại, reset DaDieuChinh, đổi ngày/nhân viên, sửa/xóa chi tiết sau chốt bị chặn. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T22-K06 | [I] Phiếu ngày cũ/ngày tương lai/rỗng không chốt; không tự sửa ngày để né điều kiện. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T22-K07 | [I] Phát sinh kho sau snapshot: từ chối, không ghi đè tồn mới bằng số cũ. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T22-K08 | [I] Nhiều NVL một dòng snapshot lệch: toàn bộ rollback. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T22-K09 | [I] Hai session chốt cùng phiếu/checkout song song được tuần tự hóa hoặc từ chối hợp lệ; không mất biến động kho. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T22-K10 | [M] Không giữ khóa DB trong thời gian người dùng đếm; UI nêu cần tạm dừng phát sinh để chốt. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T22-K11 | [U/M] STORE/ADMIN được; CASH không; reload trạng thái và chênh lệch đúng. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
