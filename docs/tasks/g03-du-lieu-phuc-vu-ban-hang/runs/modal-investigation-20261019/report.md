# Điều tra input modal — 19/10/2026

Người dùng yêu cầu tìm nguyên nhân thay vì nhập hộ. Không thay source sản phẩm, migration, expected hoặc UI để né lỗi. Không có yêu cầu manual mới. Áp dụng bug-investigation-workflow; chưa đề xuất sửa code app khi không có bằng chứng nguyên nhân ở app.

## Kết luận có bằng chứng

Lỗi được tái hiện ngoài CoffeeManager trên app WinForms net10.0-windows tối thiểu chỉ có Form/Button/TextBox và ShowDialog, không tham chiếu project CoffeeManager hoặc SQL. Vì thế lỗi cache/activation quan sát được không đòi hỏi code editor CoffeeManager để xảy ra; chưa có căn cứ sửa editor hoặc bật ShowInTaskbar trong app để né lỗi helper.

Điểm hỏng đã khoanh vùng ở đường helper định danh/cache và kích hoạt cửa sổ có modal: list_windows/list_apps chỉ trả parent, get_window_state(parent) trả cây có child modal và ô input, nhưng set_value(index từ chính cây đó) không tìm được phần tử; type_text(window parent) báo failed to activate captured window. Chưa có source native helper/trace nội bộ để khẳng định dòng code hoặc cơ chế chính xác bên trong. Đây là kết quả điều tra, không phải tuyên bố đã sửa helper triệt để.

## Các lượt đối chứng đã chạy

| Bước | Expected | Actual | Kết luận |
|---|---|---|---|
| CoffeeManager capture trước activate | Ảnh/tree cùng target | Tree CoffeeManager/modal, ảnh trang Chrome khác | Capture đang lấy foreground không đúng target trong lượt này; không dùng ảnh đó để click |
| Activate CoffeeManager rồi capture | Đúng app/modal | Đúng app/modal; modal chưa active |
| Raise modal qua accessibility | Modal active | Title modal chuyển active | Có khả năng Raise riêng, không chứng minh nhập được |
| set_value edit6 sau Raise | Đặt Kg | Cannot set a value for an element that is not settable, dù cây ghi settable | Cache/action không khớp; không kết luận lỗi TextBox |
| App đối chứng parent TextBox | Set parent-control | Cây và log ParentTextChanged=parent-control | PASS đối chứng API trên form chính |
| Click Open native modal sau capture geometry | ShowDialog mở | Native modal hiện, InputGotFocus/ModalActivated trong log | PASS tạo/focus modal native |
| set_value edit10 trong cây modal | Đặt modal-control | element 10 is not available in cached app state for Probe.exe | Tái hiện cùng lỗi ngoài app |
| Refresh focus rồi type_text native-input-123 | Modal nhận chữ | failed to activate captured window; chưa có ModalTextChanged | Lỗi trước khi text tới control |
| Biến thể ShowInTaskbar=true | Kiểm direct target modal | Parent activation lỗi; recovery capture ra wallpaper, dừng input trước mở modal | BLOCKED, không suy kết luận về biến thể này |

Trong log native có InputKeyDown=LWin và mất focus giữa các lượt do tương tác desktop không do agent gửi; agent không gọi phím Windows. Không gán toàn bộ mất focus cho helper từ log này. Tái hiện set_value lỗi cache trước đó vẫn có bằng chứng độc lập. Một lần click khi capture không screenshot báo coordinate input geometry is unavailable; đã chụp mới trước click thành công, không xem lỗi này là bug sản phẩm.

Source đối chứng: [Probe/Program.cs](Probe/Program.cs), project cạnh file, build0warning/0error. Lệnh build: `dotnet build docs/tasks/g03-du-lieu-phuc-vu-ban-hang/runs/modal-investigation-20261019/Probe/Probe.csproj -v quiet`. App nhận đường dẫn log qua đối số; biến thể thứ hai thêm taskbar. Tất cả input UI dùng sky API, không dùng PowerShell UIAutomation/SendKeys hoặc helper protocol tự chế.

Log thô: [native-modal.jsonl](native-modal.jsonl), [taskbar-modal.jsonl](taskbar-modal.jsonl). API trace tái hiện: list_windows → get_window từ kết quả duy nhất → activate_window → get_window_state(true,true) → set_value(parent edit2,parent-control) → capture → click(Open native modal) → capture → set_value(modal edit10,modal-control) → lỗicache → refreshfocus → type_text(native-input-123) → lỗiactivation. Index chỉ dùng từ observation ngay trước hành động; ID đổi theo lượt.

## Đối chiếu code CoffeeManager và giới hạn

OperationEditorDialog.AddText tạo TextBox mặc định editable; AddField chỉ đặt Dock/AccessibleName/theme. MaterialsControl không gọi SetReadOnly cho create. SetBusy chỉ disable fields trong ghi/unknown; lượt này chưa gửi ghi. AppTheme đặt màu/chữ/border, không có KeyDown chặn input. Đây là kiểm source, không thay bằng chứng native UI; chưa loại trừ mọi bug layout/focus khác của sản phẩm.

DB đúng marker: [db-observation.json](db-observation.json), NVL81=0, receipt=0. Do đó chưa có create hay open-stock thành công; T09-K01 vẫn BLOCKED, không PASS.139file sản phẩm/SQL/test không đổi: [audit](source-resource-audit.json).

## Xử lý và bước tiếp theo

Dừng nhánh input không đáng tin theo recovery của computer-use; không yêu cầu người dùng nhập hộ để gọi là nghiệm thu tự động. Không sửa ShowInTaskbar/focus của app chỉ để chiều helper. Chưa thể sửa nội bộ helper native qua API được cung cấp; cần bản helper hoạt động đúng đường modal/cache/activation hoặc công cụ kiểm UI được hỗ trợ khác rồi tái chạy reproducer trước GUI G03. Không hứa bản app update nào sẽ sửa khi chưa có bằng chứng.

Đã đóng đúng2PID diagnostic theo path; đã dọn DB/PID GUI test có marker bằng cleanup.ps1 của B02 sau lưu evidence. Không còn DB phiên này giữ lại. G03 vẫn Đang làm:20/47IDPASS,27BLOCKED; B02 bootstrap/login vẫnPASS. Không commit/push.
