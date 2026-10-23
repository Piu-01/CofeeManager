# Triển khai cửa sổ con Orbit

22/10/2026 — requirements, plan, test và sửa B01/B02 đã duyệt. **Đã sửa hai bug và PASS44ca regression chức năng**, xem [regression-results.md](regression-results.md). Người dùng tự kiểm hình thức; không chụp ảnh/DPI hoặc khẳng định hình thức đã đạt mẫu. Phần kiểm ban đầu bên dưới giữ lịch sử.

## Đã thay đổi

- Khung `OrbitDialogLayout` chung: heading theo tên cửa sổ, body, status tự ẩn/wrap, footer nút cố định bên phải; tự đo form nhập và giới hạn WorkingArea. Form bảng có diện tích nội dung riêng.
- `OrbitDialogField` tái dùng `OrbitField` cho TextBox/ComboBox, giữ instance/binding/nhãn/focus; bảng nháp nhận style chung mà không đổi ReadOnly/quyền/cột.
- OperationEditorDialog và CatalogEditorDialog chuyển sang khung/token Orbit. Bỏ layout cũ, chiều cao hàng78/note520 cố định và lớp bọc Catalog dư. Giữ CaptureWrite/requestId/nháp/unknown/reconcile và handler ghi.
- Công thức/Chính sách/Preview bỏ sizing qua Parent.Height; xóa dòng nháp dùng nút Orbit và hàng riêng. Ngày/giờ tái dùng control sẵn có; chỉ áp dụng palette ở context dialog.
- Hóa đơn khách chuyển từ RecordListControl cũ sang OrbitRecordListControl chế độ gọn. Picker cũng dùng chế độ gọn, tránh tiêu đề lặp và giữ action chọn/đóng theo context.
- Xác nhận Orbit native thay MessageBox trong editor/G03, giữ message, lựa chọn và DialogResult; tập trung nút từ chối, không đặt AcceptButton cho ghi.

## File và dọn code

13file sửa,3file thêm,1file xóa so baseline của đợt; danh sách đầy đủ [source-changes.csv](runs/source-changes.csv), hash [source-implementation.csv](runs/source-implementation.csv).

- Thêm: `UI/OrbitDialogLayout.cs`, `UI/OrbitDialogField.cs`, `Forms/OrbitConfirmationDialog.cs`.
- Sửa: hai editor và Designer, CustomerPickerDialog, module Catalogs/Customers/Menu/Loyalty, OrbitManagementTheme, OrbitField, OrbitDateField, OrbitListWorkspace.
- Xóa `UI/RecordListControl.cs` sau khi chuyển consumer cuối và rg xác nhận không còn tham chiếu trong src/tools/tests. Giữ OrbitManagementLayout vì MainForm còn gọi helperIcon; giữ OrbitContentHost/AppTheme/OrbitFieldLayout vì Login/Bootstrap hoặc nơi khác còn dùng.
- Không đổi Business/DataAccess/Models so baseline; không SQL/migration/credential hoặc thao tác DB. Không commit/push, không thay thay đổi có sẵn của người dùng.

## Kiểm kỹ thuật đã chạy

- Build Debug:0warning/0error, [log](runs/build-debug.log).
- Build Release:0warning/0error, [log](runs/build-release.log).
- Output riêng `runs/build/Debug` và `runs/build/Release`, tránh khóa DLL từ app cũ đang mở. App cũ không phải chứng cứ của source mới.
- Lần compile tích hợp đầu báo2lỗi do đặt AutoScaleMode/AutoScaleDimensions lên TableLayoutPanel. Đã bỏ hai gán đó; Form vẫn sở hữu autoscaleDpi. Build cuối sạch. Đây là sửa compile trong pha triển khai, không phải bỏ expected test.
- Rà source không còn consumer RecordListControl, MessageBox ở editor/G03 hoặc sizing grid/preview bằng Parent.Height tại module Menu/Loyalty.

## Chưa kiểm chứng

Xem [test-results.md](test-results.md):42/42rulesPASS;7loại danh mục, validation catalog, picker tìm/chọn/guest/đóng, chống lưu lặp, unknown/reconcile/retry, ngày typed và ghi/đọc/xóa1fixtureCatalog trên SQLtest đãPASS. Chưa nghiệm thu toànbộD01–D09.

- B01: OrbitDialogField.OnLayout đọccaption trước khởi tạo, làm AddText/AddField némNullReferenceException; chặn editor nhập vàcác bảngcon. Chưa ghi fixturekhách/nguyênliệu/sảnphẩm/bàn/KM, chưa kiểm preview/mởsổ/chínhsách qua editor.
- B02: YesNo đóngX trảCancel thayNo; vẫn từchối, không bằngchứng ghi ngoàiýmuốn.
- Source sohash sau test:0file thayđổi. JSONlượtđầu giữ nguyên;2lỗi thao tác harness (nútđangbusy, text cómnemonic&) đã phân biệt vàkiểm lại đúnghandler ở lượt bổ sung. Không sửa ứngdụng để né lỗi.
- Hìnhthức/ảnh/DPI/Designer:SKIPPED theo yêu cầu. Appđăngnhập đầyđủ, quyềnCASH/STORE, nhiều trang>50 và các luồngbịB01chặn chưa được nghiệmthu.

Kết quả trên là lượt trước sửa. Người dùng đã duyệt cả đợt B01/B02; hiện hai bug đã sửa/PASS, các luồng bị chặn đã được kiểm tiếp theo regression-results.md. Build Debug/Release cuối sạch; output mới ở `runs/build-fixed/Release/CafeManager.WinForms.exe`. Source ứng dụng chỉ thêm hai sửa nhỏ trong OrbitDialogField và OrbitConfirmationDialog, không đổi backend. Không thay nghiệm thu G03/T01–T32 hoặc commit/push.

Lượt regression có lỗi timer harness đã điều tra và cô lập theo từng modal; fixture KH014 trên DBtest có300điểm mở sổ được **giữ nguyên lịch sử/receipt**, không vá dữ liệu. Lượt kiểm lại trên KH015 xác nhận Hủy mở sổ/điều chỉnh không ghi và đã dọn fixture mới. Các giới hạn còn lại và lịch sử FAIL được nêu đầy đủ trong regression-results.md.
