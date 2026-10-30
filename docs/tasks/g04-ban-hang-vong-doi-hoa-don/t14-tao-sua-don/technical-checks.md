# Kiểm tra kỹ thuật T14 — 23/10/2026

- Migration001–010 clean/demo trên SQL Server2025localhost đã apply; smoke tạo
  mang đi+món đầu, thêm dòng, đọc đơn/receipt và tổng55.000 thành công.
  Evidence: ../runs/technical_20261023_151953_662bf8e5/manifest.json.
- Tạo2DB test, dọn2, giữ0; source SQL không đổi trong lượt chạy. Không ghi DBdevelopment.
- Build Debug solution SDK10.0.401 thành công,0warning/0error (lệnh ngoài sandbox).
  Lệnh build trong sandbox trước đó báo FAILED0error; giữ rõ giới hạn môi trường.
- Đã chuẩn bị SalesRulesTests; chưa chạy test. Chưa nghiệm thu T14-K01–K12,
  fault/concurrency/quyền mutation còn NOT RUN trong đợt nghiệm thu G04.

Đầu ra migration010/Models/DAL/BLL dùng cho T15; không báo Hoàn tất riêng task.
