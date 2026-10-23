# Chốt nghiệm thu G03 — 20/10/2026

**Quyết định: PASS — người dùng chấp nhận nghiệm thu với ngoại lệ kiểm thử.** Dừng đợt test G03 hiện tại theo yêu cầu: “vụ test g03 này mình thấy chắc ổn r, pass đi”. Đây là quyết định nghiệm thu của người dùng sau đợt test và manual, không phải kết quả chạy mới hoặc xác nhận 47/47 ca kỹ thuật đã PASS.

## Bằng chứng đã có

- 20/20 unit G03/MaterialRules và 132/132 phép kiểm SQL bổ sung; giữ nguyên ma trận 47 ID gốc và raw evidence ngày18/10.
- Manual19–20/10 đã đối chiếu CRUD, mở sổ và chặn thao tác nguyên liệu; sản phẩm/công thức/bật bán/validation; bàn/khu vực; khách/điểm mở sổ/ràng buộc. Chi tiết và dữ liệu thực tế giữ tại runs/manual-g03-20261019/progress.md và JSON từng bước.
- B01/B02 đã xử lý theo hồ sơ; B03 đã sửa, đã kiểm các luồng nghiệp vụ, phần đánh giá UI còn lại được người dùng tạm gác. B04 đã sửa, build Debug/Release,11 ca logic ngày và2 phép đo bounds125%PASS; người dùng chấp nhận giao diện lịch mới.

## Ngoại lệ được giữ minh bạch khi chốt

Theo yêu cầu chốt đợt test hiện tại, không tiếp tục yêu cầu người dùng chạy các phần còn thiếu. Các trạng thái FAIL lịch sử/BLOCKED/NOT RUN trong biên bản vẫn là kết quả thực tế tại thời điểm đo, không đổi hàng loạt thành PASS.

- B03: phần regression visual chưa đầy đủ được chấp nhận tồn đọng, không tuyên bố đã kiểm hết hoặc sửa hết UI.
- Designer, ma trận resize/DPI100/150%, bàn phím, loading/timeout và các nhánh GUI chưa có bằng chứng vẫn chưa kiểm đầy đủ.
- M09 năm chữ số thập phân thiếu xác nhận thao tác Lưu; không tạo bằng chứng giả từ ảnh lỗi cũ.
- M36: KM20261020 thực tế lưu0%, tên chứa20%; chưa có xác nhận input để coi là lỗi lưu hoặc ca giảm20%PASS. Không sửa DB hay expected.
- Tích hợp POS/checkout đích thuộc G04 vẫn NOT RUN; cần kiểm theo task G04, không kế thừa PASS từ checkout baseline/fixture G03.

## Trạng thái bàn giao

G03 được chốt PASS theo quyết định người dùng, với các ngoại lệ trên; đủ để chuyển bước quy trình sang G04 khi được giao. Không tự triển khai G04 trong lượt chốt. Không tick hồi tố checklist T01–T32, không đổi số liệu cũ, không sửa source/SQL, không commit/push. DB test manual và app hiện tại không bị xóa trong thao tác cập nhật tài liệu này.
