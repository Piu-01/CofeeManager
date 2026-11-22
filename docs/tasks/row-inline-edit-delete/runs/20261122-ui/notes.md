# Ghi chú chạy test 22–23/11/2026 (DB CoffeeManager_Test_Inline_20261122_inline_ui, schema 41, DPI 120=125%)

- Build tạm: `C:/Users/Lenovo/.qoder/tmp/cm-build-inline` (Debug, net10.0-windows). PID cuối 18996.
- Biên bản chính thức: `docs/tasks/row-inline-edit-delete/test-results.md`; bug: `bug-report.md`.

## Danh mục
- **MC-01** toolbar: không còn Sửa/Xóa; có Thêm, cột thùng rác 55px. PASS.
- **MC-02** click ô Tên mở overlay đúng giá trị, select-all. Gõ thật qua SendKeys bị IME (UniKey) chặn -> dùng ValuePattern. PASS.
- **MC-03** Enter ghi NVL01 "Inline Mat Test A". PASS.
- **MC-04** Esc không ghi NVL03. PASS.
- **MC-05** sửa NVL04 rồi click ô NVL05: NVL04 lưu, editor mở NVL05. PASS.
- **MC-06** click ô Mã/góc header: không mở editor. PASS.
- **MC-07** Tab từ Tên NVL06 -> lưu, mở ô Đơn vị. PASS.
- **MC-08** Shift+Tab trên Đơn vị NVL03 -> SQL 51602 (đúng nghiệp vụ) nhưng editor kẹt: Esc không đóng, click ô khác lặp lỗi -> **IN-01**.
- **MC-10** cursor: Tên/Đơn vị IBeam; Mã/thùng rác Arrow. PASS.

## Các màn khác (PASS trừ khi ghi khác)
- PTTT: PT001 Hỗ trợ 1->0 lưu DB; PT010 không sửa được. Expense: TC001 bị chặn vì có phiếu -> gây IN-01; TC005 hệ thống không sửa/xóa; TC009 đổi rồi xóa (xác nhận) OK; TC011/TC012 hệ thống chặn xóa.
- NCC10 xóa thành công sau xác nhận; NCC09 đã dùng bị chặn; DT002 xóa ngay không xác nhận (bị chặn); DM011 tạo + xóa ngay OK.
- Danh sách NCC có lần tải >30s: status "Chưa cập nhật · Truy vấn quá thời gian; thử tải lại.", lưới trống; "Tải / đối chiếu" khôi phục. Ghi chú trạng thái tab cũ, chưa kết luận bug.
- Sản phẩm SP001: tên/giá/nhóm/trạng thái lưu; hộp Công thức mở/đóng; SP019 tạo + xóa ngay OK.
- Khách hàng KH001 tên/SDT OK; KH023 xóa: Hủy -> không xóa; đồng ý -> bị chặn (có hóa đơn); hộp sổ điểm/lịch sử điểm OK.
- Nhân viên NV001: tên, SDT 0901234567, 12.500.000, 50.000, Part-time, CV002 lưu OK; SDT trùng -> báo lỗi nhưng IN-01; NV010 xóa bị chặn; hộp Nghỉ mở/đóng OK.
- Tài sản TS001: tên/ngày/nguyên giá/số tháng, Shift+Tab OK; xóa Hủy OK; xóa đồng ý bị chặn ("đã có chứng từ liên quan"); hộp Chi tiết/Hồ sơ tài sản OK.
- Bàn/Khu vực: KV001 đổi tên; KV010 xóa ngay; KV001 còn bàn bị chặn; B0001 đổi tên; B0011 tạo+xóa; B0002 đổi khu vực rồi khôi phục; bàn dùng bị chặn xóa. **IN-02**: dropdown khu vực còn KV010 sau khi xóa (cache areaRows).
- Hạng: BAC 2500 -> Hủy xác nhận: dữ liệu không đổi nhưng IN-01; đồng ý -> DB 2500; xóa BAC sau xác nhận OK.

## Timeout (2 phiên SQL, khóa >30s)
- NVL02 request `032791c4-9399-42bb-b9bc-0c7d0d8e8515`: dòng khóa, "Đối chiếu" giữa dòng, dòng khác dùng được, DB không đổi, đối chiếu "chưa ghi", mở khóa. PASS.
- TS002 request `0512ea8e-0f51-4f7b-9b73-c7dfc5c07814`: như trên. PASS.
- NCC02 request `098f2a71-3823-4122-ad63-a5ef9b4db351`: như trên (so revision qua GetAsync). PASS.

## CASH
- thungan: menu chỉ Bán hàng/Lịch sử HĐ/Giao hàng/Khách hàng/Tồn phục vụ bán/Thông tin hệ thống/Đăng xuất. Khách hàng read-only: click Tên OV=0, thùng rác không hộp xác nhận. PASS.

## Chưa chạy
- Xóa thành công khách/nhân viên/sản phẩm chưa có lịch sử; timeout sản phẩm/khách/hạng; hai phiên cùng sửa; kiểm BLL ngoài UI; hình thức/DPI 100-150 (người dùng tự kiểm).

## Bug
- **IN-01**: `OrbitCellEditor.Confirm` đặt `begun=false` trước khi biết kết quả commit; commit lỗi/hủy giữ editor nhưng Esc và click chuyển ô vô hiệu, click lặp commit. Tái hiện: NVL03 51602, TC001, NV001 trùng SDT, BAC hủy xác nhận.
- **IN-02**: `TablesControl.areaRows` nạp 1 lần lúc Load; xóa KV010 ở tab Khu vực không làm mới dropdown Khu vực của bàn.

## Hồi quy sau sửa (23/11/2026, build cm-build-inline3, PID 27872)
- Sửa: editor giữ `committing`/`CommitFailed`; engine FailCommit, chặn Leave tự ghi lại, click ô khác bỏ giá trị lỗi; TablesControl RefreshAreasAsync khi Load/VisibleChanged.
- RG-01 NVL02 tên Enter lưu PASS; RG-02 NVL03 đơn vị lỗi 51602, Esc đóng PASS; RG-03 lỗi rồi click ô Tên dòng 4 mở đúng, không kẹt PASS; RG-04 NVL05 Tab lưu + mở Đơn vị PASS.
- RG-05 hạng VANG 12000 Enter -> Hủy -> “Đã hủy.”, Esc đóng, DB 10000 PASS; RG-06 hủy rồi click ngưỡng DONG không mở lại hộp, mở ô DONG PASS.
- RG-07 SP001 giá “abc” -> “Giá không hợp lệ.”, Esc đóng, DB 26000 PASS; RG-08 sửa 27000 rồi trả 26000 PASS.
- RG-09 tạo KV099 (SQL), xóa ở tab Khu vực, về tab Bàn: dropdown 9 khu vực, không còn Regression Area PASS; RG-10 đổi B0001 sang San vuon rồi trả Inline Area A, DB KV002/KV001 PASS.
- Dữ liệu đã khôi phục; NVL03 vẫn Kg, hạng VANG 10000/5, KIM 30000/8, SP001 26000, B0001 KV001, KV099 đã xóa.
- Quan sát ngoài đợt: mở Ưu đãi/Điểm (tab Khuyến mãi) hiện Unhandled IndexOutOfRange tại LoyaltyRepository.Promotion dòng 29; đã Continue, chưa sửa, cần bug riêng nếu duyệt.
