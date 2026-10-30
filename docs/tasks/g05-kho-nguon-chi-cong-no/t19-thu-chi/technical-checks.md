# T19 — kiểm kỹ thuật trong triển khai

26/10/2026. Bộ plan đã duyệt; chưa chạy đợt nghiệm thu T19/G05.

- Migration017 compile/apply trên SQL Server thật, hai DB clean/demo qua runner001–017: thành công tại `../runs/technical_20261026_221648_94ccdc0c/manifest.json`; tồn trước/sau không đổi. Đã tạo2/dọn2/còn0 DB test có marker. Không đổi development.
- Lượt trước giữ nguyên log: sandbox chặn localhost (0DB), lỗi compile batch OriginKind (1DB đã dọn), guard lịch sử chặn backfill nhãn nguồn (2DB đã dọn). Đã sửa trong pha triển khai, không sửa checksum migration đã triển khai vào development.
- Build Debug T19 thành công0warning/0error sau sửa compiler namespace/nullable. Unit test mới chỉ soạn, chưa chạy. Release và smoke typed sẽ ghi tiếp trước kết thúc triển khai.
- Chưa kiểm chức năng GUI, chưa ghi PASS10ID; hình thức SKIPPED theo phân công, DPI100/150% NOT RUN. Modal dùng OrbitDialogHost native owner; input dùng OrbitField chung.
- Nguồn PN/CN sẽ nối sau T20/T23. Không coi BL fixture là tích hợp chi lươngT26.
