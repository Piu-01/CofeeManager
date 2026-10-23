## Xác nhận của người dùng — 20/10/2026

Người dùng phản hồi sau khi nhận bản sửa: “ok test vậy mình thấy đủ ok”. Ghi nhận chấp nhận giao diện lịch/nút lịch bản mới trong phiên manual hiện tại, kết hợp bằng chứng bounds125% đã PASS. Phần lỗi visual B04 được người dùng chấp nhận đóng. Không suy rộng phản hồi này thành đã chạy bàn phím, Designer hoặc DPI100/150%; các ca đó giữ NOT RUN. Không thay đổi kết quả nghiệm thu toàn nhóm G03 hay các ca nghiệp vụ còn lại. Không sửa source hoặc dữ liệu DB trong lượt cập nhật này.
## B04 bổ sung đã duyệt và sửa — 20/10/2026

Đã đo xác nhận nguyên nhân: lịch native cần281×248 nhưng calendar/host262×207 tại125%DPI. Đã đồng bộ kích thước sau tạo handle/theme, bố trí theo working area và thay nút chữ bằng icon lịch Orbit theo DPI. [Biên bản cùng đợt](../runs/g03-b04-followup/test-results.md): build Debug/Release đạt, unit20/20, logic ngày11/11, bounds giữa/sát đáy2/2. Visual/keyboard editor và DPI100/150% chưa đủ bằng chứng; helper capture harness timeout quyền app. Đã mở lại app test PID9684 chờ đăng nhập. B04 chưa đóng. Lịch sử FAIL bên dưới giữ nguyên.

## B04 bổ sung — regression GUI FAIL ngày 20/10/2026

Ảnh người dùng `codex-clipboard-887f5ef7-10d2-4543-bf42-a49933e0779b.png` cho thấy popup lịch cắt nội dung và nút hiển thị “Lịc…”, hình thức chưa hợp Orbit. Không suy luận tháng January 2028 là lỗi lưu ngày vì có thể là thao tác điều hướng.

Bằng chứng source: OrbitDateField đặt nút Width=76, trong khi OrbitButton vẽ chữ với EndEllipsis và có GetPreferredSize riêng; chiều rộng cố định không bảo đảm đủ chữ/padding theo DPI. Popup dùng ToolStripControlHost AutoSize từ constructor; OpenCalendar chỉ Show, không đồng bộ lại kích thước sau khi HWND lịch áp SetWindowTheme. Chưa đo native bounds nên nguyên nhân cắt popup còn là giả thuyết về sizing/theme/DPI, chưa khẳng định lỗi z-order.

Đề xuất bổ sung trong B04: đo bounds calendar/host/popup sau tạo handle và áp theme; sửa đúng khâu sizing được chứng minh, bố trí popup đủ cả header/footer và nằm trong working area, mở lên trên nếu phía dưới thiếu chỗ. Thay nút chữ bị cắt bằng nút biểu tượng lịch nhỏ theo Orbit, có AccessibleName/tooltip và kích thước theo DPI. Chỉ sửa helper ngày T13, giữ typed DateTime/validation/ValueChanged; không đổi BLL/SQL hay dữ liệu khuyến mãi.

Regression: giữ các ca logic ngày; bổ sung native popup đủ header/ngày/footer, mở ở biên màn hình, 100/125/150% DPI, Tab/Alt+Down/Escape, chọn ngày rồi mở lại. 11 ca in-process trước không chứng minh GUI đạt. Rủi ro tập trung vào layout/focus popup; phải kiểm chứng trên app thật. Chờ duyệt bổ sung trước sửa; source giữ nguyên trong lượt test này.
# G03-B04 — Chọn ngày lệch theme tối — 20/10/2026

## Sau duyệt — đã áp dụng sửa, chờ GUI

Người dùngduyệtđợtsửa/regression. ThêmUI/OrbitDateField.cs(TextBoxnative/dd/MM/yyyy/nútLịch/popupMonthCalendarnative), SetWindowThemechỉHWNDlịchriêngđểpalettecóhiệu lực. Dùng3ôdateT13(Editstart/end,Previewdate), thêmMarkDirtyquaValueChangedvàoOperationEditorDialog. KhôngđổiBLL/SQL/migrations/Windowssetting, khôngtựchỉnhrate0%.

Debug/Release0warning0error; unitG03/MaterialRules20/20PASS; harnessinprocessSTAcontrolngày11/11PASS. [Biênbản](bug-test-results.md). Nativecalendarvisual/focus/keyboard/DPIchưatựPASS từcode. AppmớiđãmởlạitrênDBtestcũ/manifestmanualcậpnhậtPID; KMngày20–21/10/rate0/receipt1khôngđổi. B04**đãsửachưađóng**, chờmanualô/popup. Lịch sửtrướcsửabêndướigiữnguyên.

## Điều tra bổ sung — người dùng xác nhận cả ô ngày và popup

Người dùng xác nhậncảhai trắng. MicrosoftgiảithíchBackColorcủaDateTimePickerkhôngđổiappearance, vàCalendarMonthBackgroundcóthểkhôngcóhiệuứngkhibậtvisualstyles. VìvậycodeAppThemehiệnchỉsetmàukhôngđủchobộnativeđangdùng; khôngnêntăngthêmcùngpropertyrồicoiđãsửa. Nguồnprimary: [CalendarMonthBackground](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datetimepicker.calendarmonthbackground?view=windowsdesktop-10.0), [DateTimePicker](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.datetimepicker?view=windowsdesktop-10.0), [SetWindowTheme](https://learn.microsoft.com/en-us/windows/win32/api/uxtheme/nf-uxtheme-setwindowtheme). Khôngnâng.NET11đểdùngAPIVisualStylesModemới.

**Hướng sửa cụ thể trình duyệt G03-B04:** tạo helperdatefieldhẹp choT13: TextBoxnativeđịnhdạngdd/MM/yyyytrongcontainerOrbitvànútmởlịch, popupMonthCalendarnativecópaletteOrbit/giớihạnbounds; nếuvisualstylesghiđèmàu thìdùngSetWindowThemechochínhHWNDcalendartrongappđểápdụngpalette, khôngtắtthemeWindows/toànapp. DùngAPIWin32đượctàiliệuhóatrongcontrolapp, khônglàautomationngoàiquyđịnh. Đổi2ôdatePromotionEditvàôdatePreviewT13sanghelpernày, giữValueDateTime/ValueChanged; kiểmngàygõthậtkhônghợplệthìbáolỗi/giữinput, khôngfallbackhômnay. MarkDirtyđúngkhingàyđổi, bỏnháphỏiđúng; Escapeđóngpopuptrướcdialog, Tab/focus/accessibilitynativetiếptục. Khôngvẽlạitoànbộcalendar, khôngthêmthưviện, khôngđổiBLL/SQL/migrations/khuyếnmãiđãlưu.

**Regression cùng đợt xin duyệt:** buildDebug/Release, unitparse/roundtrip/ngàynhuận/ngàykhônghợplệ/giữValue; GUIôđóng/popuplịch/hoverselected/disabled/focus, nhậpbànphímvàchọnlịch/đóngmởpopup/mànbiên/DPI, mởKM20261020khôngsửangàyphảigiữnguyên20–21/10/2026. SQLtrướcsaukiểmngàykhônglệchvàkhôngghiđếnLưu. NativevisualkhôngtựPASStừbuild.

**Rủirothựctế:** textboxdatephảivalidateexact, focuspopup/modalvàscalingcóthểkhó; cầnđốichứngnativechứkhôngcamkếtđạttrướckhiđo. PhạmvihelperT13khôngđụngcácmànkhác; cóthểtáidùngsauchỉkhicóscopeđượcduyệt. Sourcechưasửa/chờduyệtđợtG03-B04. Vấnđềrate0%vẫnchờxácnhậninputriêng, khôngđiềuchỉnhDBđểné.

**Trạng thái:** Đang điều tra; chưa sửa trong lượt test. Người dùng yêu cầu chỉnh phần chọn ngày trắng/lạc lõng khi lưu khuyến mãi. Chưa có ảnh phần datepicker ở thông báo này; cần phân biệt ô ngày đóng hay popup lịch, không tự giả định cả hai đều lỗi.

**Source đã đối chiếu:** PromotionList.Edit tạo hai DateTimePicker native, dd/MM/yyyy, đặt trongOperationEditorDialog rồiAppTheme.Apply. AppTheme áp BackColor/ForeColor chung và CalendarMonthBackground/CalendarForeColor/CalendarTitleBackColor/CalendarTitleForeColor; chưa có xử lý hình thức ô ngày native riêng. Màu thực tế còn phụ thuộc renderer/native theme; đây là giả thuyết cần xác nhận qua UI, không phải root cause engine đã chứng minh. Không dùng SQL hoặc đổi format/ngày để sửa visual.

**Phạm vi dự kiến sau đủ bằng chứng:** ô ngày editor khuyến mãi T13 (và calendar liên quan nếu được xác nhận), bámOrbit; giữ nativekeyboard/datevalidation/accessibility/typedDateTime, không sửaWindows theme/security/registry, không đổiAppTheme toàn bộ màn khác theo cảm tính. Đọc đủguideUI trước sửa. Regression ngày bắtđầu/kếtthúc, popup/chọnngày/keyboard/focus, resize/DPI và đốichiếuDBkhôngđổingày. Chưa duyệt đóng bug hoặc tựcoiUIđạt.

**Phát hiện độc lập cần xác nhận input:** [M36-db.json](../runs/manual-g03-20261019/M36-db.json) KM20261020đúngtên/ngày20–21/10/2026 nhưngPhanTramGiam0, khácfixture20%. Editor source defaultdiscount0; chưa biết người dùng chưađổi hayappđãlưu sai. KhôngtựchỉnhDB/expected, chưa quy thànhbuglưutỷlệ khi chưa có input thực tế. Hỏi xácnhận trước preview sốtiền.



