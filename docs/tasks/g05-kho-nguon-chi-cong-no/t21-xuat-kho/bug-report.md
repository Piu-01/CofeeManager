# G05-B08 — Hộp Lập phiếu xuất vỡ layout ở 125%, thanh thêm dòng bị cắt

24/11/2026, người dùng báo lỗi kèm ảnh chụp; **chưa sửa mã nguồn, chờ duyệt**.
Ảnh: [20261124-stockissue-layout-125.png](evidence/20261124-stockissue-layout-125.png)
(cửa sổ ~1118×742, tương ứng ClientSize 900×600 ở Windows scale 125%).

## Hiện tượng

- Hàng công cụ cuối hộp “Xuất nguyên liệu — Chưa ghi kho” bị cắt: nút **Thêm dòng** chỉ
  còn “Th…”, nút **Bỏ dòng** còn “Đã…”, cao ~22px; card chứa lưới kết thúc ngay trên
  hàng công cụ nên phần dưới bị che.
- Tiêu đề lặp hai lần: titlebar Windows và heading lớn trong hộp cùng chữ
  “Xuất nguyên liệu — Chưa ghi kho”.
- Ô **Lý do** nền trắng, ô **Loại xuất** nền tối, ô **Ngày** là DateTimePicker native có
  nút lịch — không cùng kiểu với các editor Orbit hiện hành (ví dụ hộp Lập phiếu nhập).
- Header dồn trên một hàng (nhãn + combo + ngày + nhãn + textbox) và dòng hướng dẫn dài
  chạy sát lưới; lưới chiếm phần lớn hộp nhưng trống, không có hướng dẫn/empty state.
- Cột **LƯỢNG XUẤT** nằm sát mép phải, cách xa các cột còn lại; không có cột tồn,
  không có số thứ tự/xóa dòng.

## Nguyên nhân

1. **Hai nút công cụ chưa được style nên giữ kích thước mặc định của Button.**
   `StockIssueEditorDialog.cs:27-28` tạo `OrbitButton` “Thêm dòng”/“Bỏ dòng” nhưng không
   gọi `OrbitManagementTheme.Button(...)`. `OrbitButton` kế thừa `Button`; chỉ khi có
   `ManagementStyle` thì `GetPreferredSize` mới tính theo chữ (`OrbitButton.cs:45-63`).
   Không style ⇒ kích thước mặc định 75×23, chữ dài bị ellipsis (“Th…”, “Đã…”).
   Hai nút này cũng không được vẽ theo theme quản trị như các nút khác.
2. **Layout tự dựng không theo pattern editor hiện hành.** Body là `TableLayoutPanel`
   3 hàng (header AutoSize, lưới Percent, tools AutoSize) với `header` và `tools` đều là
   `FlowLayoutPanel{AutoSize=true,Dock=Fill}` (`StockIssueEditorDialog.cs:25,27`).
   Header chứa câu hướng dẫn dài (`:25`) nên xuống dòng ở 125%, trong khi
   `OrbitDialogLayout.EnableResponsiveActions(this,true)` (`:30`) gọi
   `FitToContent(..., hasTable:true)` dùng **chiều cao nội dung cố định P(390)**
   (`OrbitDialogLayout.cs:128-132`) chứ không đo nội dung thật. Tổng chiều cao hàng
   vượt vùng card, hàng công cụ bị cắt ở đáy (đúng ảnh).
3. **Không truyền header riêng cho `OrbitDialogLayout`.** Layout mặc định lấy
   `owner.Text` làm heading (`OrbitDialogLayout.cs:26-29`) nên trùng với titlebar.
4. **Input/grid chưa qua theme dialog.** Không gọi `OrbitManagementTheme.DialogInput`
   cho combo/ngày/lý do và `DialogGrid` cho lưới; dùng `DateTimePicker` native, `TextBox`
   đặt `Width=280` và `ComboBox` `Width=310` cố định (`:16`), nên không reflow theo DPI.
5. **Lưới thiếu empty state và cột thông tin.** Không có overlay “Chưa có nguyên liệu”,
   không có cột tồn/số thứ tự/xóa như hộp Lập phiếu nhập
   (`PurchaseEditorDialog.cs:83-120`), nên hộp trông trống và người dùng không biết
   phải chọn NVL rồi bấm Thêm dòng.

Đây là hệ quả của việc hộp Xuất kho chưa được chuyển sang pattern editor Orbit đã áp
cho Nhập hàng/Kiểm kê, không phải lỗi nghiệp vụ hay SQL. Không cần hỏi thêm thông tin:
ảnh + mã đủ xác định.

## Cách sửa đề xuất

Chỉ sửa `src/CafeManager.WinForms/Modules/Inventory/StockIssueEditorDialog.cs` (UI),
giữ nguyên request/service/procedure, theo đúng pattern `PurchaseEditorDialog`:

1. Header riêng: badge “Chưa ghi kho”, tiêu đề “Lập phiếu xuất”, mô tả ngắn; truyền vào
   `OrbitDialogLayout` để bỏ heading trùng.
2. Hàng thông tin dùng `OrbitDialogField`: Loại xuất (ComboBox themed), Ngày xuất
   (`OrbitDateField`), Lý do (TextBox, tối đa 200) chia cột Percent; bỏ Width cố định.
3. Card nguyên liệu: tiêu đề + số dòng; hàng tìm NVL (`OrbitSearchBox` + nút “Thêm”
   style `ManagementButtonStyle.Secondary,"plus"`); lưới `OrbitDataGridView` có
   #, Mã, Nguyên liệu, ĐVT, Lượng xuất, nút xóa; quantity là ô sửa duy nhất; empty state.
4. Footer card: ghi chú “Trả hàng chưa tự hoàn tiền/cấn nợ…” và số dòng; nút
   **Chốt xuất / Quay lại** giữ nguyên handler, xác nhận, pending store, timeout.
5. Áp `OrbitManagementTheme.DialogInput`/`DialogGrid`; style mọi `OrbitButton`.
6. Sau khi thêm dòng, focus thẳng ô Lượng xuất của dòng mới (đang thiếu).

Không đổi contract, không đổi migration. Nếu muốn, dùng `PurchaseService.MaterialsAsync`
(đã có `sp_PurchaseMaterialLookup`) để hiện **Tồn** ngay trên lưới và cảnh báo vượt tồn
trước khi gửi — đây là cải thiện UX tách riêng, không bắt buộc cho fix layout.

## Tối ưu (optional, không thuộc nguyên nhân lỗi)

- Hiện tồn/cảnh báo vượt tồn theo dòng (trùng F06 báo cáo rà soát 24/11).
- Xem lại chứng từ trước khi chốt, in phiếu xuất, cột người lập/giờ chốt (F04/F05/F07).
- Các mục này nên làm ở đợt riêng; không gộp vào fix layout để giữ phạm vi nhỏ.

## Rủi ro và kiểm chứng

- Chỉ đổi UI một file; không đổi SQL/BLL/DTO/quyền; rủi ro nghiệp vụ thấp.
- Cần chạy lại native: thêm/sửa/xóa dòng, lượng 0/âm/vượt tồn, chốt thành công,
  bỏ nháp, timeout/kiểm tra kết quả; 125% (bắt buộc). 100%/150% và hình thức do người
  dùng tự kiểm theo phân công; agent không ghi PASS hình thức.
- Nếu nhận thêm cột Tồn: kiểm mapping `MaterialsAsync`, quyền ADMIN/STORE, và không
  làm thay đổi dữ liệu gửi lên `sp_LapPhieuXuat`.

## Chờ duyệt

Đồng ý sửa theo hướng redesign hộp theo pattern Lập phiếu nhập (khuyến nghị), chỉ UI,
không thêm cột Tồn; hay muốn gộp luôn cột Tồn + focus ô lượng trong cùng lượt?

---

# G05-B06 — Grid xuất có cùng cấu hình ReadOnly toàn bảng

29/10/2026, người dùng đã duyệt B06 kèm regression; đã sửa hai editor. Chủ sở hữu điều tra ở [T20](../t20-nhap-kho/bug-report.md#g05-b06--grid-nhậpxuất-đang-khóa-ô-cần-nhập--đã-sửa2910).

StockIssueEditorDialog dùng OrbitDataGridView mặc định ReadOnly=true, chỉ đặt ReadOnly=false cho cột Quantity mà không mở grid cấp trên. Ảnh hưởng T21 được xác định qua source; hiện tượng không mở editor đã tái hiện native ở T20, chưa chạy native ghi T21 trong lượt này. Không gắn thêm FAIL nghiệm thu T21 từ source-only.

Đề xuất mở chỉnh sửa riêng grid xuất, chỉ Quantity; giữ cột tham chiếu khóa và default OrbitDataGridView nguyên trạng. Sửa/regression cùng đợt B06: nhập số, validation, bỏ nháp, chốt xuất và đối chiếu tồn/receipt/quyền. Không sửa source trong lượt test B05.

## Sau sửa29/10

Mở grid xuất ReadOnly=false/EditOnEnter, giữ mã/tên/đơn vị readonly. Native đã xác nhận lượng0 bị chặn,9999 vượt tồn rollback/giữ input, sửa1 chốt PX20250506 một lần; bỏ nháp sửa3 không ghi. Component ADMIN/STORE binding/selection/khóa unknown và unit/SQL subset PASS. Native Tab chưa đủ bằng chứng; STORE/trả hàng native chưa chạy trong lượt này. [Biên bản](../runs/b06-20261029/test-results.md); không đổi bảng10ID lịch sử.

