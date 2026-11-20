# Đổi ngôn ngữ áp dụng ngay trong phiên — requirements

Ngày soạn: 10/11/2026. Trạng thái: **Chờ duyệt requirements**.
Task này sửa phương án áp dụng ngôn ngữ của `docs/tasks/localization-vi-en/`:
requirements cũ chốt phương án (B) — trong app phải đăng xuất/mở lại mới áp dụng.
Người dùng chốt ngày 10/11/2026: đổi ngay, chỉ chặn khi đang có nháp/dialog chưa lưu,
và dùng lại đúng cơ chế hỏi-lưu sẵn có khi chuyển tab/màn.

## 1. Mục tiêu

Tại "Thông tin hệ thống", bấm **Áp dụng** ngôn ngữ thì toàn bộ giao diện đổi ngay
trong phiên đang đăng nhập, không cần đăng xuất, không đăng nhập lại, không mở lại app.
Nếu màn hiện tại đang có dữ liệu chưa lưu thì hỏi trước như khi chuyển tab.

## 2. Phạm vi

### Làm

1. **Áp dụng tức thời**: sau khi lưu lựa chọn vào `%LocalAppData%\CoffeeManager\language.txt`
   và cập nhật `LanguageService`, dựng lại shell `MainForm` với **cùng `SessionContext`**
   đang dùng: giữ nguyên nhân viên, role, quyền, menu, token phiên. Shell mới mở đúng
   màn nghiệp vụ đang xem trước khi đổi. Shell cũ được đóng/dispose, không revoke phiên,
   không quay về màn đăng nhập.
2. **Chặn khi có nháp**: gọi lại **nguyên** `NavigationController.ConfirmLeaveAsync`
   đang dùng cho chuyển tab/màn, không thêm cơ chế riêng:
   - Có nháp chưa lưu → hỏi đúng câu hiện dùng khi chuyển tab/màn
     ("Bỏ nháp và rời? …"). Chọn **Không** → không đổi ngôn ngữ, giữ nguyên màn, nháp,
     phiên; chọn **Có** → bỏ nháp rồi đổi ngôn ngữ ngay.
   - Đang ghi hoặc kết quả chưa xác định (`IsWriting`) → hàm này đã chặn thẳng và báo
     hoàn tất bước hiện tại (không cho chọn), y như khi chuyển màn.
   - Màn không khai báo `IUnsavedChanges` thì không hỏi, đổi ngay.
3. **Thứ tự thao tác**: kiểm tra chặn nháp → lưu file preference → `LanguageService.Set`
   → dựng lại shell. Nếu người dùng hủy hoặc lưu file thất bại thì **không có thay đổi
   nào** (không đổi ngôn ngữ, không đổi nội dung file).
4. **Dialog đang mở**: modal Windows đã khóa form nên không bấm được Áp dụng; không
   cần thêm xử lý. Chỉ cần không dựng lại shell khi hộp đang mở nếu vì lý do kỹ thuật
   lệnh đổi vẫn được gọi.
5. **Màn đăng nhập**: giữ nguyên hành vi hiện tại — đổi ngay tại chỗ, không liên quan
   shell.
6. Giữ nguyên toàn bộ nội dung dịch hiện có: chỉ dùng lại `L.T`/`L.F`, không sửa từ điển
   `en.json`, không đổi chuỗi nghiệp vụ, không đổi định dạng tiền/ngày.

### Không làm

- Không viết cơ chế relocalize tại chỗ cho từng control (không sửa ~925 chỗ `L.T`).
- Không giữ nháp POS/editor chưa lưu xuyên qua lần đổi ngôn ngữ.
- Không đổi ngôn ngữ khi đang có dialog mở, đang ghi dữ liệu hoặc kết quả chưa xác định.
- Không đổi SQL/procedure/DB/migration, contract request/response, quyền, nghiệp vụ,
  phím tắt, luồng nháp/unknown.
- Không đổi hình thức Orbit (màu/font/spacing/icon), không thêm ngôn ngữ thứ ba.
- Không đổi luồng đăng xuất thường, không đổi cách đóng app.

## 3. Giả định

- "Tab/màn cần lưu dữ liệu" = màn đang mở khai báo `IUnsavedChanges` (hiện là POS
  `SalesControl`, `CatalogsControl` qua `CatalogEditorDialog`) và các editor modal
  (đã tự hỏi khi đóng). Các tab con trong module (Bàn/Loyalty/…) chỉ mở editor modal,
  không giữ nháp inline nên không cần guard riêng.
- Dựng lại shell sẽ mất trạng thái tạm của màn hiện tại (từ lọc, trang, vị trí grid,
  dữ liệu đã tải) và tự tải lại từ DB; chấp nhận. Ngôn ngữ đổi xong không cần hộp
  thông báo thêm.
- Người dùng đã đăng nhập nên preference mới cũng chính là preference sẽ dùng ở lần
  mở app sau; không có khái niệm ngôn ngữ theo từng tài khoản.
- Màn `SystemInfo` khi dựng lại vẫn đọc lại thông tin schema như lần mở bình thường.

## 4. Câu hỏi mở

Không có câu hỏi chặn. Hai điểm được chốt theo đề xuất, nêu để bạn phản hồi nếu muốn khác:
1. Hủy ở hộp "Bỏ nháp" → **không** lưu lựa chọn ngôn ngữ (tránh trạng thái file đã đổi
   nhưng giao diện chưa đổi).
2. Sau khi đổi ngôn ngữ thành công → **không** hiện thêm thông báo "đã đổi"; giao diện
   đổi ngay là phản hồi.

## 5. Tiêu chí hoàn thành

- Đang ở EN, bấm Áp dụng tại Thông tin hệ thống: menu, nhóm menu, breadcrumb, thanh
  trạng thái và màn đang mở chuyển sang EN ngay; **vẫn đăng nhập**, đúng nhân viên/role,
  đúng màn; không xuất hiện màn đăng nhập.
- Đổi VI → EN → VI liên tiếp trong phiên hoạt động đúng, không nhân đôi cửa sổ, không
  rò dialog mồ côi, không lỗi khi đóng app sau đó.
- Có nháp POS chưa lưu: hộp hỏi bỏ nháp xuất hiện; chọn Không → ở lại, ngôn ngữ và nháp
  giữ nguyên; chọn Có → nháp bị bỏ và giao diện đổi ngay.
- Đang có dialog nghiệp vụ mở: không thể đổi ngôn ngữ (bị chặn), không có nửa vời.
- Đóng/mở lại app: đúng ngôn ngữ vừa chọn; file preference không đổi khi thao tác bị hủy.
- Phiên không bị revoke khi đổi ngôn ngữ (không có sự kiện logout, không quay về login).
- Build Debug/Release pass; unit test localization hiện có vẫn pass; không sửa SQL/migration.
- Kiểm thử chức năng trên WinForms thật ở DPI 125% theo phân công; hình thức do người
  dùng tự kiểm, ghi SKIPPED, không dùng làm blocker.
