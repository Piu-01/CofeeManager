# Triển khai điều chỉnh native —26/10/2026

Trạng thái: ĐÃ TRIỂN KHAI, CHỜ DUYỆT TEST RIÊNG. Chưa nghiệm thu ND01–ND13.
Ý định user và plan điều chỉnh đã duyệt.

## Kết quả thay đổi

- OrbitDialogHost nay là bộ quản lý modal/owner/return dùng chung, không Control
  chứa layer/card hoặc titlebar giả. MainForm giữ layout trực tiếp như trước.
- Queue ShowDialog trên UI thread hiện có để callers await. Mỗi dialog có
  titlebar/X native, ownerMainForm hoặc hộpcha, không taskbar/minimize độc lập;
  Windows tự khóa owner. Không DoEvents/polling/threadUI khác.
- OrbitDialogForm giữ kết quả OK/Cancel khi nativeFormClosing bị hủy để hỏi bỏ
  nháp; abort queued/active hoàn tất task; F3 validResult không hỏi bỏ nháp sau
  Apply thành công. Chỉ trả kết quả khi modal native đã trở về, owner đã mở khóa.
- FitToContent theoWorkingArea native, printer nhận owner đang hoạt động.
- Giữ nguyên helper chọn hết input (hash bằng bản host), nội dung/theme và toàn
  bộ caller. Không đổi SQL, Models/DAL/BLL, quyền hoặc dữ liệu nghiệp vụ.

Năm file source đổi so với bản host: OrbitDialogHost, OrbitDialogForm, MainForm,
OrbitDialogLayout, OrderHeaderDialog. Source/log/evidence cũ giữ nguyên.

## Kiểm chứng triển khai

- Debug/Release:0warning/0error.
- Smoke18/18 tạiDeviceDpi120: title/X/owner, ShowInTaskbar=false, OSenabled state
  chỉtopdialog, một Formtaskbar, resultOK/Cancel, bỏ nhápNo giữ/Yesđóng, abort
  nested/queued khôngorphan; NativeOwner trả activechild cho printer.
- Đây là smoke kỹ thuật metadata/windowstate và componentclose, không input
  native/thịgiác, không là nghiệm thu nghiệp vụ/SQL hoặc tất cảdialogs.
- Không tạo DB/ghiSQL, không tự đăng nhập hoặc mở thêm bản app để nghiệm thu.
  Processsmoke đã thoát; bản host cũ user đang mở không bị tự đóng/reset.
- Hình thức SKIPPED theo phân công;100/150%NOT RUN. Helper input giữ source
  không đổi nhưng cần ND13 trên modal mới, không tự gán PASS từ bản host.

Bằng chứng: runs/native-implementation-20261026/{native-smoke.json,
build-debug.log,build-release.log,source-final.csv,dll-hashes.json,
changed-product-files.csv,manifest.json}. Test đề xuất tại native-test-plan.md.
