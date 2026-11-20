# GUI thực tế sau login — 18/10/2026

Người dùng xác nhận login thủ công. Native window4526140/CoffeeManager có identityNguyen Van An·ADMIN, menuG03 đầy đủ. Click Nguyên liệu/Tồn: tải12nguyên liệu seed, tồn NVL01=96,7400Kg, mốcLegacyCutover; timestampDB23:13:49 trên app. Thời điểm hiển thị là giờ app, không dùng timestamp làm bằng chứng ngày máy chủ.

Mở Thêm nguyên liệu qua nútThêm. Modal có3ôMã/Tên/Đơn vị, thông báo18g=0,0180Kg và không tự quy đổi, nútLưu/Đối chiếu(disabled)/Hủy; không có ôsửa tồn. Không lưu thay đổi qua helper.

BLOCKED input helper: set_value element210 báo `element 210 is not available in cached app state for CafeManager.WinForms.exe`. Refresh tree cho focused204Đơn vị; type_textKg không xuất hiện trên capture tiếp theo. Recovery click ôĐơn vị dựa screenshot mới, xác nhận focus rồi typeKg vẫn không hiện. Dừng nhập; không quy kết appvalidation hoặc ghi SQLFAIL. Các capture runtime hiển thị trực tiếp trong tool, không decode/sao ảnh payload. Người dùng được nhờ nhập NVL81/Cà phê kiểm thử GUI/Kg và bấmLưu thủ công; chờ phản hồi và đối chiếu SQL trước khi kết luậnT09-K01.

G03-B02 regression setup/login PASS; các ca GUI nghiệp vụ chưa hoàn tất. Không tự nâng47ID:20PASS/27BLOCKED. SQL-before lưu gui-before-manual.json; chỉ marker DBtest hợp lệ mới được truy vấn. Source sản phẩm/migrations chưa sửa.
