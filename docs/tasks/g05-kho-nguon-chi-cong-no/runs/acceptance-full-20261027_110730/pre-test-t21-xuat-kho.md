# T21 — Biên bản nghiệm thu chưa chạy

26/10/2026. Source đã triển khai; chờ duyệt đợt test G05. Build/compile/smoke kỹ thuật lưu riêng, không thay actual của các ID dưới. Môi trường nghiệm thu, schema/hash, dữ liệu, lệnh và bằng chứng sẽ điền khi chạy. Hình thức SKIPPED theo phân công; DPI100/150% NOT RUN.

| ID | Expected gốc | Actual / dữ liệu / thao tác | Kết quả | Bằng chứng |
|---|---|---|---|---|
| T21-K01 | [M/I] Nháp không trừ tồn; lưu Huy do hong/Tra hang giảm đúng nhiều nguyên liệu. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T21-K02 | [I] LoaiXuat=Xuat ra quay hoặc loại lạ bị chặn theo mô hình một kho. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T21-K03 | [I] Lượng 0/âm/quá precision, NVL/FK sai, trùng NVL bị chặn. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T21-K04 | [I] Xuất bằng tồn đưa tồn 0 hợp lệ; hơn tồn rollback toàn chứng từ. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T21-K05 | [I] Một dòng thiếu kho sau dòng khác hợp lệ không để dòng đầu bị trừ dở. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T21-K06 | [I] Hai xuất/checkout tranh lượng tồn cuối: không âm, chỉ các giao dịch đủ tồn commit. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T21-K07 | [I] Chốt lại/retry không trừ lần hai; sửa/xóa chứng từ chốt bị chặn. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T21-K08 | [M/I] Xuất hủy cho đồ đã pha nhưng hủy đơn không đồng thời ghi tiêu hao bán cùng lượng. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T21-K09 | [M] Xuất trả hiển thị rõ chưa tự hoàn tiền/cấn trừ nợ. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
| T21-K10 | [U/M] STORE/ADMIN thao tác được, CASH bị chặn; reload đúng lịch sử/tồn. | Chưa chạy đợt nghiệm thu | NOT RUN | Chưa có |
