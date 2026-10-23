# Bug trong đợt test chức năng cửa sổ con Orbit

22/10/2026 — **B01/B02 đã sửa và PASS regression** sau khi người dùng duyệt cả đợt. Phần điều tra bên dưới được giữ làm lịch sử. Bằng chứng trước sửa trong test-results.md và runs/functional-results.json; sau sửa tại regression-results.md. Đây là lỗi thuộc đợt UI độc lập hiện tại, không tự mở rộng backlog G03.

## B01 — OrbitDialogField lỗi ngay khi khởi tạo (ưu tiên cao)

- **Tái hiện:** newOperationEditorDialog, gọiAddText/AddField; ProductsControl.Recipe cũng tái hiện qua handler thật. Bốn ca test có stacktrace cùng điểm, trước khi gọi ghiDB.
- **Actual:** NullReferenceException tại `src/CafeManager.WinForms/UI/OrbitDialogField.cs:31` (`caption.MaximumSize`); ctor đang ởdòng14gán thuộc tính TableLayoutPanel.
- **Nguyên nhân đã có bằng chứng:** các setterColumnCount/RowCount/Dock/AutoSize trongctor kích hoạtOnLayout trước khi `caption = new Label` ởdòng18. OverrideOnLayout đọc caption chưa khởi tạo, không có guard. Không phải SQL hoặc payload sai.
- **Ảnh hưởng:** tất cả AddText/AddField của OperationEditorDialog: thêm/sửa/mởsổ nguyên liệu/khách, sản phẩm/bàn/khuvực/khuyếnmãi, côngthức/chínhsách/preview/điểm/hóađơn khi được đặt trong editor. CatalogEditorDialog và CustomerPickerDialog độc lập vẫn mở/chạy được. Các dialog xóa chỉAddNote vàkhôngAddField không tái hiện cùngđường này.
- **Hướng sửa đề xuất:** bảo vệ OnLayout trong giai đoạnconstructor (chỉ cập nhậtcaption/note khi sẵn sàng, luôn gọi base); bố tríkhởi tạo/SuspendLayout phù hợp. Không đổi binding, handlers, nghiệp vụ hoặc SQL. Regressionmởfield text/combo/date/grid/controlcon, validation/focus, dirty, đọc/ghi vàhủy các nhóm đangBLOCKED.
- **Rủi ro thực tế:** tác động chung tới mọi hàngfield; cần chắc row/note vẫn nhận maximumwidth sau khởi tạo. Không mở rộng sửa hình thức theo DPI trong đợt này.

## B02 — X ở xác nhận Có/Không trả Cancel thay No (ưu tiên thấp)

- **Tái hiện:** OrbitConfirmationDialog YesNo.ShowDialog; gọiClose tương đương nhánh đóngcửa sổ. ExpectedNo theo mappingđãduyệt; actualCancel. Các nútCó/Không/Escape/Enter vàOKCancel đạt.
- **Nguyên nhân:** Form modal đặt DialogResult=Cancel trướcOnFormClosing khi đóng bằngX. Override hiện chỉ đổi khi `DialogResult == None`, nên không ánh xạCancel sangdismissed=No ởYesNo.
- **Ảnh hưởng:** sai contract DialogResult của YesNo. Các callsite hiện chỉ thực hiện khi `==Yes`, vì vậy X vẫn từ chối và **không có bằng chứng ghi ngoài ý muốn**.
- **Hướng sửa đề xuất:** chuẩn hóa nhánh đóng/Cancel choYesNo vềNo; giữYes nếungười dùngchọnCó vàgiữOKCancel nhưcũ. Regression đủ2tậpnút×5đườngra, chủyếuX/Escape/Enter, giữmặcđịnhtừchối.
- **Rủi ro:** nếu normalizequárộng cóthể làm mấtYes/OK; chỉđổi nhánhtừchối tươngứng.

## Đề nghị duyệt một đợt

Người dùng đã duyệt. Đã sửa B01/B02 trong đúng hai control chung, regression các ca độc lập và tiếp tục các luồng chức năng bị chặn; giữ miễn kiểm hình thức. Không sửa SQL/Business/DataAccess/Models, không xóa lịch sử/receipt hoặc vá dữ liệu. Không phát hiện thêm bug ứng dụng đã xác nhận trong đợt này; các lỗi harness được ghi riêng tại regression-results.md.
