# T27 — Kiểm kỹ thuật trong triển khai

31/10/2026. Chưa nghiệm thu9ID, chưa chạy xUnit/native. SQL→DAL→BLL→UI đã có source theo plan; đủ đầu vào kỹ thuật để tiếp tục T28. Không ghi Hoàn tất T27.

- SDK10.0.401, build CafeManager.slnx Debug --no-restore:0warning/0error.
- SQL Server localhost thật: runner Test-G07.ps1 -Technical -ThroughVersion25 clean/demo001–025 và rerun025 thành công; tiền/kho/điểm/nợ/lương trước/sau migration không đổi.
- Typed smoke -TypedSmoke: runtime G07Runtime thuộc CafeManagerApp qua EXECUTE AS USER; context DB, tạo tài sản100/3, get/list/count/receipt/chi rỗng, SQL khấu hao3tháng=100. Đây là kiểm contract tối thiểu, không thay checklist T27-Kxx.
- Bằng chứng: ../runs/technical_20261031_095142_b3d8e15b/manifest.json và ../runs/technical_20261031_095442_47832092/typed-smoke.json (đường tính từ nhóm G07). Mỗi lượt tạo2DB/dọn2/còn0, không chạm development. Lượt sandbox095107 bị Windows access denied, tạo0DB; chạy ngoài sandbox thành công.
- Hình thức giao người dùng, agent SKIPPED. Native/DPI chức năng chưa chạy, không suy PASS từ compile hoặc smoke service.


## Cập nhật sau đợt test31/10

Các mục trên là bản ghi lịch sử pha triển khai. Đợt test riêng đã được duyệt và chạy; xem [test-results.md](test-results.md) và [báo cáo nhóm](../report.md):23PASS/1FAIL/9BLOCKED. G07-B01 chờ duyệt sửa; native còn thiếu bằng chứng. Không dùng trạng thái lịch sử 'chưa chạy/chờ duyệt test' thay kết quả hiện tại.
