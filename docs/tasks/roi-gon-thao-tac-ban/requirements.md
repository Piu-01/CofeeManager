# Requirements — Rơi gọn thao tác Bàn / Khu vực và chọn bàn ở POS

- Ngày ghi: 20/11/2026
- Người yêu cầu: người dùng (dùng thử sau T34, cùng đợt với bug T34-02)
- Trạng thái: **chờ duyệt requirements** (chưa có plan, chưa sửa code)

## Mục tiêu

Giảm thao tác thừa ở hai màn dùng bàn:

1. Màn **Bàn / Sơ đồ** (tab Bàn, tab Khu vực): bỏ các nút thao tác không còn giá trị;
   chuyển sửa/xóa sang thao tác trực tiếp trên dòng/card.
2. Màn **Bán hàng (POS)**: bàn được mở ngay khi chọn trong ô chọn bàn, không cần
   bấm nút "Chọn bàn" nữa.

## Yêu cầu nguyên văn của người dùng (20/11/2026)

> "bỏ 2 nút chọn bàn và trả bàn ở màn bàn khu vực đi, không cần thiết, ở trang bán hàng
> bỏ nút chọn bàn luôn, khi chọn bàn trên listbox thì nó tự động vào không cần nút chọn
> bàn mới áp dụng. màn bàn khu vực, bỏ 2 nút sửa và xóa, thêm icon thùng rác với dòng
> chức năng xóa thay cho nút xóa, khi click vào thì cho phép sửa"

## Phạm vi

### A. Màn Bàn / Khu vực (`Modules/Tables/TablesControl.cs`)

1. **Tab Bàn (`TableList`)**
   - Bỏ action "Chọn bàn" và "Trả bàn" khỏi thanh thao tác.
   - Giữ nguyên: bấm card trong Sơ đồ (và bấm dòng trong Danh sách) **vẫn mở POS**
     với bàn đó như hôm nay — đây là đường mở bàn chính, không thay đổi hành vi.
   - Bỏ action "Sửa" và "Xóa" khỏi thanh thao tác.
   - Double-click một dòng (Danh sách) hoặc card (Sơ đồ) → mở hộp "Sửa bàn" (chỉ ADMIN,
     đúng dialog hiện có). Với nhân viên không phải ADMIN, double-click không mở gì;
     click đơn vẫn mở POS như cũ.
   - Thêm **icon thùng rác theo từng dòng** trong bảng Danh sách (chỉ ADMIN, đúng quyền
     như nút "Xóa" hiện tại). Bấm icon → mở đúng luồng xóa hiện có (hộp xác nhận +
     reconcile idempotent); không có nút "Xóa" dạng chữ trên toolbar nữa.
   - Trạng thái bật/tắt của "Thêm" giữ như hiện tại.

2. **Tab Khu vực (`AreaList`)** — áp dụng đúng bộ thay đổi trên: bỏ "Sửa"/"Xóa" toolbar,
   double-click dòng mở "Sửa khu vực", icon thùng rác theo dòng mở luồng "Xóa khu vực".

3. Ô "Trả bàn" trong POS và hộp "Trả bàn ngay?" sau thanh toán **giữ nguyên**; nhân viên
   vẫn trả bàn từ POS như luồng T34 đã chốt.

### B. POS (`Modules/Sales/SalesControl.cs`)

4. Bỏ nút "Chọn bàn" trên dải chọn bàn; giữ ô chọn bàn (combo), khu vực và nhãn trạng thái.
5. Khi người dùng **chủ động chọn** một bàn trong combo (chuột hoặc phím), POS tự gọi
   đúng luồng mở bàn hiện có (`OpenTableAsync`):
   - Bàn đang có đơn mở → mở đơn đó.
   - Bàn trống/Đã trả · giữ bàn → giữ hộp xác nhận bỏ nháp nếu có nháp chưa lưu
     (`ConfirmInputAsync`), rồi đặt ngữ cảnh Tại quán cho bàn đó.
   - Lỗi nghiệp vụ (bàn bị chiếm 52104, quyền, mất kết nối) → hiển thị như hiện tại,
     không mở đơn sai.
6. Các lần gán bàn **bằng code** (khôi phục lựa chọn sau `LoadTables`, `Đơn mới` tự chọn
   bàn trống, đổi kênh, mở đơn có sẵn, reset sau trả bàn) **không** kích hoạt tự mở bàn,
   để tránh mở trùng đơn/hỏi nháp vô ích.
7. Sửa các câu thông báo còn nhắc "bấm Chọn bàn" (SalesControl + `en.json`) cho khớp
   hành vi mới, ví dụ "Chọn bàn khác trong danh sách để mở đơn hiện có".

## Giả định

- **Icon thùng rác**: người dùng trả lời "ủa bán hàng có bàn/ khu vực đâu mà" → hiểu là
  icon đặt ở **màn Bàn/Khu vực** (mỗi dòng của bảng Danh sách), không liên quan POS.
- **Card Sơ đồ**: giữ hành vi cũ — click mở POS (người dùng chọn "Vẫn mở POS").
- **Cách sửa**: double-click dòng/card (người dùng chọn "Double-click dòng/card").
- **Phạm vi tab**: áp dụng cho cả tab Bàn và tab Khu vực (người dùng chọn "Có, cả hai tab").
- **Icon thùng rác trong Sơ đồ**: requirements này chỉ làm icon theo dòng ở Danh sách;
  xóa từ Sơ đồ thực hiện bằng cách chuyển sang Danh sách. Nếu người dùng muốn icon
  ngay trên card, ghi bổ sung trước khi duyệt plan.
- Double-click khi đang ở Sơ đồ chỉ mở sửa với ADMIN; click đơn vẫn mở POS ngay (không
  chờ phân biệt double-click) — nhất quán với hiện tại và không làm chậm đường mở bàn.
- Icon thùng rác hiển thị theo quyền ADMIN giống nút "Xóa" cũ; BLL vẫn là nơi chặn
  quyền cuối cùng, UI chỉ ẩn/hiện.
- Không đổi contract DAL/BLL/SQL: các service `SaveAsync`/`DeleteAsync`/`SelectAsync`
  và dialog hiện có được tái sử dụng nguyên vẹn.

## Ngoài phạm vi

- Không đổi nghiệp vụ phiên bàn/trả bàn (T34), không đổi `sp_TraBan`, checkout, in.
- Không đổi layout/tổng thể màn Bán hàng ngoài việc bỏ nút "Chọn bàn" và cập nhật chuỗi.
- Không thêm chức năng mới (lọc, sắp xếp, gộp bàn, kéo thả sơ đồ…).
- Không đổi hành vi quyền ngoài việc giữ nguyên chặn ADMIN như cũ.
- Không kiểm hình thức UI (theo phân công, người dùng tự kiểm); agent chỉ kiểm chức năng
  và layout kỹ thuật DPI 125%.

## File dự kiến bị ảnh hưởng

| File | Thay đổi |
|---|---|
| `src/CafeManager.WinForms/Modules/Tables/TablesControl.cs` | Bỏ 4 action (Chọn bàn, Trả bàn, Sửa, Xóa) ở `TableList`; bỏ Sửa/Xóa ở `AreaList`; double-click dòng/card → Edit; icon thùng rác theo dòng → luồng Delete |
| `src/CafeManager.WinForms/UI/OrbitDataGridView.cs` / `OrbitRecordListControl.cs` | Hỗ trợ cột icon thao tác theo dòng + sự kiện double-click dùng chung (không chép handler riêng từng màn) |
| `src/CafeManager.WinForms/Modules/Sales/SalesControl.cs` | Bỏ `selectTable`, thêm tự mở bàn khi người dùng chọn combo (có cờ chặn gán bằng code), cập nhật chuỗi |
| `src/CafeManager.Models/Localization/Translations/en.json` (+ vi nếu có chuỗi mới) | Cập nhật/xóa chuỗi "bấm Chọn bàn", thêm chuỗi tooltip/accessible name cho icon thùng rác |
| `docs/UX_QuanLiQuanCafe.md`, `docs/tasks/g03-.../t11-ban-khu-vuc/plan.md` | Ghi nhận thay đổi hành vi trong phạm vi được duyệt (chỉ mục liên quan) |

## Câu hỏi mở

Không có câu hỏi chặn. Điểm duy nhất có thể điều chỉnh thêm: có muốn icon thùng rác
xuất hiện cả trên card ở Sơ đồ hay không (mặc định: không, dùng Danh sách).

## Tiêu chí hoàn thành

1. Màn Bàn/Khu vực không còn nút Chọn bàn, Trả bàn, Sửa, Xóa trên toolbar; bấm card/dòng
   vẫn mở POS đúng bàn; double-click (ADMIN) mở đúng hộp Sửa bàn/khu vực; icon thùng rác
   trên dòng (ADMIN) mở đúng hộp Xóa với xác nhận và xử lý lỗi như cũ; nhân viên không
   phải ADMIN không thấy icon và không sửa/xóa được qua UI.
2. POS không còn nút "Chọn bàn"; chọn bàn trong combo tự mở đơn/đặt ngữ cảnh đúng một lần,
   không lặp hộp xác nhận, không mở nhầm khi các luồng code tự gán bàn (Đơn mới, đổi kênh,
   tải lại, sau trả bàn, mở đơn có sẵn).
3. Không còn chuỗi hướng dẫn "bấm Chọn bàn" trong UI tiếng Việt/Anh.
4. Build Debug/Release 0 error; kiểm chức năng WinForms thật ở DPI 125%: mở bàn từ combo,
   mở đơn đang có, hủy nháp, double-click sửa, xóa có xác nhận, chặn quyền nhân viên.
5. Không thay đổi SQL/procedure/contract; regression T34 (trả bàn từ POS, "Trả bàn ngay?")
   vẫn chạy đúng.
