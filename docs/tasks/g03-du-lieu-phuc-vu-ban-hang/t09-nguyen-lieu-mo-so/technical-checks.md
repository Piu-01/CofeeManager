# Kiểm tra kỹ thuật T09 — 18/10/2026

Migration004 compile trên SQL Server localhost clean/demo, smoke tạo nguyên liệu
→ mở sổ0,0180Kg → đọc đúng tồn. Evidence đầu thành công:
`../runs/technical_20261018_192302_dd088a99/manifest.json`. Lượt sandbox trước không
kết nối được SQL, chưa tạo tài nguyên; đã dùng phạm vi cấp quyền localhost để chạy.

Mẫu WinForms thật danh sách/editor và state tải/rỗng/lỗi/dữ liệu mẫu tại
`../runs/t09-layout-technical/`. Kiểm kỹ thuật phát hiện rowversion bị grid tự coi
là ảnh; đã chuyển cột tường minh trong pha triển khai. Ảnh mẫu không là test DB.
Lượt đọc development cuối đúng màn: `../runs/development-ui-technical-r3/result.json`,
ADMIN/CASH/STORE đều đọc12NVL. Dữ liệu chỉ đọc, không thay nghiệm thu ghi/mở sổ.

Source build cuối: `../runs/build_20261018_200326/`, Debug/Release0warning/error.
Kiểm kỹ thuật lifecycle control đã thêm guard dispose; chi tiết các lượt collector
và giới hạn ở report nhóm. Nghiệm thu T09-K01–K09/GUI/DPI/Designer: NOT RUN.
