# Điều chỉnh — Form modal Windows, không taskbar riêng

Trạng thái: PLAN ĐIỀU CHỈNH ĐÃ ĐƯỢC USER DUYỆT26/10/2026, đã triển khai;
chờ duyệt đợt test riêng tại native-test-plan.md. Ý định đã rõ sau dùng thử:
Form Windows như trước, titlebar/X thật, có owner, không có taskbar riêng.
Lượt test host đã khép lại trước sửa source native; giữ nguyên evidence cũ.

## 1. Kết quả và phạm vi giữ lại

Giữ helper chọn hết input (TextBox/numeric/ghi chú/grid/combo gõ, dynamic,
readonly exceptions), theme/control và nội dung các màn. Giữ typed result,
validationUI06, quyền, nháp, busy/unknown/reconcile, thanh toán→hỏi in→POStrống.
Không sửa SQL, Models/DAL/BLL hoặc dữ liệu để đổi hình thức.

119component và18unit đã chạy được giữ trong hồ sơ; đó không phải chấp nhận kiểu
card tùy biến. User xác nhận nền bị khóa nhưng từ chối cách hiển thị. Chưa có
xác nhận đầy đủ checkout475, không suy đã thanh toán từ phản hồi về giao diện.

## 2. Các bước sửa

1. **Cơ chế chung:** điều chỉnh OrbitDialogHost từ container/card thành bộ quản
   lý dialog modal có owner. Tận dụng các caller await hiện đã chuyển để không
   chép ShowDialog/owner/ShowInTaskbar riêng từng màn. Bỏ layer/card/title/X giả,
   TopLevel=false, DockFill và thao tác tự disable panel nền. Restore Form native
   border/title/X, CenterParent, ShowInTaskbar=false, không minimize độc lập.
   Form lớn vẫn resize theo thiết kế và giới hạn WorkingArea/DPI.
2. **Owner và kết quả:** dialog đầu thuộc MainForm, dialog lồng thuộc dialog
   đang hoạt động; lookup MainForm đi qua cả owner chain và control parent.
   Để modal Windows khóa owner, chỉ top dialog nhận input. Giữ từ chối opener
   lặp từ nền, focus về cha và kết quả đúng một lần. Native printer thuộc hộp
   preview/checkout đang hoạt động, hoặc MainForm khi không có hộp khác.
3. **Guard đóng:** sửa OrbitDialogForm/manager để close/result tương thích
   DialogResult tự đóng của modal WinForms. Giữ dấu hiệu OK/Cancel khi native
   hủy close để hỏi bỏ nháp; không hỏi lại sau Save thành công, không bị treo
   await khi cancel/close/revoke hoặc khi ShowDialog lỗi. Không dùng DoEvents,
   polling hoặc tạo thêm UI thread để chạy dialog.
4. **MainForm và layout:** bỏ container bọc nền, đưa layout chính về MainForm;
   giữ quản lý trạng thái dialog cho navigation/shortcut/logout/session. Áp dụng
   lại FitToContent/SalesUi.Fit theo Form/WorkingArea native; effectStarDust có
   owner đúng, không nhận input/taskbar. Rà các Close/Cancel/DialogResult/Owner
   và printer path; không khôi phục đường mở nhiều cửa sổ độc lập.
5. **Quy tắc/tài liệu:** cập nhật AGENTS.md mục5, UX/UIguide và hồ sơ task theo
   quyết định mới. Đánh dấu plan cũ là lịch sử. Giữ source/log/fixture/test kết
   quả host trước đó, không sửa expected cũ để nhận native như thể host đãPASS.

## 3. Kiểm chứng và bàn giao

- Trong triển khai: build Debug/Release; smoke tối thiểu titlebar/top-level
  native, owner chain, ShowInTaskbar=false và OK/Cancel/đóng có guard. Không suy
  smoke thành nghiệm thu toàn luồng.
- Sau triển khai trình duyệt đợt test riêng. Ma trận cập nhật kiểm NativeDialog:
  một taskbar app, modal khóa cha, nested đúngowner, title/X thật và resize;
  nháp No/Yes, Esc/Enter, đóng cửa sổ, session revoke; CRUD/caller, F3/F4/F8,
  hỏi in/resetPOS, nativeprinter; chọn hết input/caret/Tab/IME/gridreused.
- Kiểm chức năng125%; user kiểm hình thức.100/150%NOT RUN, không tự đổiScale.
  Tiền/kho/điểm/receipt đối chiếu SQL Server DBtest riêng có marker.
- Đơn475–478 chỉ dùng theo trạng thái DB thực tế, không tự chốt lại; không
  xóa/đổi testdata để né lỗi. Không đánh G04/G05 hoặc tasknàyPASS từ build.
