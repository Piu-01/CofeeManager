# Kiểm tra kỹ thuật T15 — 23/10/2026

Migration001–011 chạy được trên clean/demo SQL Server2025localhost. Smoke tạo
đơn mới, đọc preview/token, chốt tiền mặt và đọc snapshotCheckout thành công.
Evidence: ../runs/technical_20261023_152512_6a05abb3/manifest.json. Tạo2DB test,
dọn2, giữ0; source không đổi trong lượt kỹ thuật, không ghi DBdevelopment.

Đây là dependency smoke, không thay15ID nghiệm thu, rollback/concurrency/PTbiên
hoặc quyền. Unit checkout đã chuẩn bị, chưa chạy. T15 cung cấp đầu vào cho T16;
nghiệm thu G04 còn chờ duyệt riêng.
