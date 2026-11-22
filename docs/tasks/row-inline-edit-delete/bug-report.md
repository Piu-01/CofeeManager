# Bug report — row-inline-edit-delete (đợt test UI 22–23/11/2026)

Môi trường: DB test `CoffeeManager_Test_Inline_20261122_inline_ui` (schema 41),
build tạm `C:/Users/Lenovo/.qoder/tmp/cm-build-inline` (Debug, net10.0-windows),
DPI 125% (120 DPI). Không sửa source trong lượt test.

**Trạng thái 23/11/2026:** IN-01 và IN-02 đã sửa theo duyệt; hồi quy 10/10 PASS
(build `C:/Users/Lenovo/.qoder/tmp/cm-build-inline3`, 0 lỗi). Còn 1 quan sát ngoài
đợt ở màn Khuyến mãi, chưa được duyệt điều tra.

## IN-01 — Editor ô inline kẹt sau khi commit thất bại hoặc hủy hộp xác nhận

**Mức độ:** Cao (chặn thao tác trên màn đang dùng, phải tắt app).

**Hiện tượng**

- Sửa ô inline rồi nhấn Enter/Tab/Shift+Tab khi lưu thất bại (lỗi nghiệp vụ,
  lỗi ràng buộc, trùng dữ liệu, hoặc người dùng bấm Hủy ở hộp xác nhận):
  editor vẫn mở nhưng phím Esc không đóng được nữa.
- Click sang ô khác không mở ô mới mà lặp lại việc ghi dữ liệu cũ (có trường hợp
  hiện lại hộp xác nhận), trạng thái có lúc kẹt ở “Đang ghi…”.
- Cách duy nhất thoát là đóng ứng dụng.

**Các lần tái hiện có bằng chứng**

1. Nguyên liệu NVL03, sửa Đơn vị -> SQL 51602 (đổi đơn vị khi đã có tồn/công thức).
   Sau Enter editor kẹt; Esc không đóng; click ô khác lặp lỗi.
   (Ghi chú chạy: `C:/Users/Lenovo/.qoder/tmp/cm-inline-tests/notes.md`, mục MC-08.)
2. Danh mục Thu/chi TC001, đổi Loại Chi -> Thu: lưu bị từ chối vì danh mục đã có
   phiếu; combobox “Thu” vẫn mở, Esc không đóng, các lần click sau lặp lỗi.
3. Nhân viên NV001, đổi SDT sang số đã tồn tại: báo trùng dữ liệu nhưng editor
   kẹt; click ô lương lặp lại lỗi SDT.
4. Hạng thành viên BAC: nhấn Enter -> hộp “Thay chính sách hạng” -> bấm Hủy.
   Editor vẫn mở, Esc không đóng; click ô Giảm % mở lại hộp xác nhận (lặp commit).

**Nguyên nhân (đọc source, chưa sửa)**

`src/CafeManager.WinForms/UI/OrbitCellEditor.cs`, hàm `Confirm`: đặt
`begun = false` **trước khi** biết kết quả commit bất đồng bộ. Khi commit trả về
`false` (lỗi hoặc hủy xác nhận), `OrbitRowEditor` giữ editor và gọi
`FocusInput()`, nhưng `Confirm`/`Cancel` không còn chạy vì `begun` đã false.
Vì vậy Esc vô hiệu và click ô khác đi vào nhánh “đang mở editor” rồi commit lại.

**Đề xuất sửa ban đầu (đã được duyệt)**

- Chỉ đặt `begun = false` sau khi commit thành công, hoặc khôi phục `begun = true`
  khi commit trả `false`/bị hủy để Esc và click chuyển ô hoạt động lại.
- Bổ sung nhánh hủy hộp xác nhận: giữ editor, cho Esc đóng, không tự ghi lại.
- Test hồi quy: Enter/Tab/Shift+Tab lỗi 51602, trùng SDT, hủy xác nhận hạng,
  hủy xác nhận xóa; sau đó Esc đóng và click ô khác mở đúng ô mới.

**Đã sửa (duyệt 23/11/2026, chờ test hồi quy)**

- `OrbitCellEditor`: thêm cờ `committing`, không đặt `begun = false` trước khi
  commit xong; `Confirm`/`RequestCommit` giữ editor mở khi lỗi; `EndCommit()`
  mở khóa sau khi engine xử lý. Esc hoạt động lại sau lỗi.
- `OrbitRowEditor`: đánh dấu `commitFailed` khi parse/ghi lỗi hoặc người dùng hủy
  hộp xác nhận (“Đã hủy.”); click sang ô khác bỏ giá trị lỗi và mở ô mới thay vì
  lặp lệnh ghi; `RunAsync` luôn `EndCommit()` trong `finally`.
- Build Debug tại `C:/Users/Lenovo/.qoder/tmp/cm-build-inline2`: 0 lỗi (build vào
  bin chính bị khóa bởi app/VS đang mở của người dùng, không tắt tiến trình).
- Bổ sung sau hồi quy: cờ `CommitFailed` chặn `Leave` tự ghi lại; sửa nội dung thì
  cho phép ghi lại. Hồi quy 23/11 (build `cm-build-inline3`, PID 27872):
  RG-02..RG-08 PASS — lỗi 51602/parse giữ editor, Esc đóng, click ô khác mở ô mới
  không lặp lỗi; hủy xác nhận hạng báo “Đã hủy.”, click ô khác không mở lại hộp.

## IN-02 — Danh sách khu vực trong ô chọn Khu vực của bàn bị cũ sau khi xóa khu vực

**Mức độ:** Thấp (dữ liệu chọn có thể không còn tồn tại).

**Hiện tượng**

Xóa khu vực KV010 ở tab “Khu vực” (xóa ngay, thành công, DB không còn KV010).
Quay lại tab “Bàn / Sơ đồ” -> “Danh sách”, mở ô Khu vực của một bàn: danh sách
vẫn còn “Khu khong khoi thuoc” (KV010). Chọn mục này sẽ gửi mã khu vực đã xóa.

**Bằng chứng**

- DB: `SELECT COUNT(*) FROM KHUVUC WHERE MaKV='KV010'` = 0 (sau khi xóa qua UI).
- UIA lúc 00:06: danh sách ô Khu vực gồm 10 mục, trong đó có
  “Khu khong khoi thuoc”.

**Nguyên nhân (đọc source, chưa sửa)**

`TablesControl` nạp `areaRows` một lần trong sự kiện `Load`; danh sách này được
dùng cho dropdown Khu vực khi sửa bàn. `AreaList` xóa khu vực nhưng không làm
mới `areaRows` của control bàn.

**Đề xuất sửa ban đầu (đã được duyệt)**

- Nạp lại `areaRows` khi chuyển tab hoặc sau khi xóa/tạo khu vực, trước khi mở
  dropdown Khu vực; hoặc lấy danh sách khu vực tươi mỗi lần mở ô.

**Đã sửa (duyệt 23/11/2026, chờ test hồi quy)**

- `TablesControl`/`TableList`: thêm `RefreshAreasAsync()` gọi khi `Load` và khi
  tab Bàn hiện lại (`VisibleChanged`); cập nhật `areaRows` và nguồn bộ lọc khu vực,
  giữ bộ lọc đang chọn nếu còn tồn tại, bỏ chọn nếu khu vực đã bị xóa. Cờ
  `refreshingAreas` tránh tải lặp do sự kiện đổi nguồn bộ lọc.
- Build Debug 0 lỗi (cùng build nêu trên).
- Hồi quy 23/11 (build `cm-build-inline3`): RG-09 PASS — xóa KV099 ở tab Khu vực,
  quay lại tab Bàn, dropdown chỉ còn 9 khu vực hiện có, không còn KV099; RG-10 PASS
  — đổi khu vực bàn rồi trả lại vẫn lưu đúng.

## Quan sát ngoài đợt (chưa duyệt sửa)

Mở màn Ưu đãi / Điểm (tab Khuyến mãi mặc định) phát sinh hộp “Unhandled exception”:
`System.IndexOutOfRangeException` tại `LoyaltyRepository.Promotion` dòng 29 khi
`PromotionList.Reload()` đọc procedure khuyến mãi. Không liên quan thay đổi
IN-01/IN-02; nghi do procedure/schema của DB test. Đã Continue để chạy tiếp các ca
hạng. Chờ duyệt điều tra thành bug riêng, không sửa trong đợt này.
