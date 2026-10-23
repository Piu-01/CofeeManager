**Đã được người dùng duyệt ngày20/10/2026. Đang thực hiện; xem test-results.md.**

# Đề nghị duyệt đợt test bố cục quản lý Orbit

**Trạng thái:** Chờ duyệt một lần. Phạm vi L01–L10 ởplan.md; source cố định theo runs/implementation/source-final.csv. Không sửa code/migration trong lượt test.

1. BuildDebug/Release và Designer; kiểm hash backend giữ nguyên.
2. Mở từng màn theo10ảnh: Menu,Nguyênliệu,BànDanh sách/Sơđồ,Khuvực,Khách,Khuyếnmãi,Chínhsáchhạng,Lịchsửđiểm,Danhmụcnền; so layout/sidebar/tab/column/empty/action/footer. Kiểm1280×720,1366×768,DPI100/125/150 nếu có khả năng quan sát native; không đổi setting máy ngoài phạm vi.
3. Đo width/height/font/icon các nút cùng chức năng; bàn phím Tab/ShiftTab/Entersearch/Escape, hover/focus/disabled và resize.
4. Phântrang50dòng với0/1/49/50/51/>100dòng, cuộn nội trang, filter hiện có, tải lại/cancel/loading/error; không có tổng trang giả hoặc lẫn dữ liệu/trạng thái trang.
5. DBtest riêng cho fixture và smoke hành động có ghi; xác minhADMIN/CASH/STORE, selection/map/list, CRUD/opening/recipe/preview/policy/adjust/reconcile theo ảnh hưởng. Không chạy ghi trên development; không chạy lại toàn bộ SQLG03 chỉ vì layout.
6. Kiểm rời màn/đóngdialog/logout, đổi màn nhiều lần khi công cụ native đủ ổn định; lưu evidence và báo cáo chung kể cả cóFAIL. Bug mới tổng hợp xin duyệt trước sửa.

Tận dụng DBtest đã có chỉ sau xác minh manifest/marker/tình trạng và hạn giữ; nếu dựng mới thì tên/marker/manifest/cleanup riêng. Không tự giữ dữ liệu/process không rõ sở hữu. Auth vẫn do người dùng thực hiện nếu cần.

