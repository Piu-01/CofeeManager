# Triển khai Orbit UI trên WinForms .NET 10

17/10/2026 · Hướng dẫn triển khai, chưa phải code đã nghiệm thu trên Windows.

## 1. Hợp đồng thiết kế

Giữ MainForm + UserControl, C# / SQL Server và kiến trúc dự án. HTML là mẫu để nhìn/đo/tương tác, không yêu cầu chuyển ứng dụng sang web hoặc tự nhúng WebView2. Không giả định CSS opacity, transform, blur và shadow có đối tượng tương đương trên mọi WinForms control.

Đầu ra đúng phải có bố cục, màu, chữ và cảm giác gần mẫu, đồng thời vẫn có bàn phím, DPI, focus và nhập liệu ổn định. Sai khác do font renderer cần ghi lại, không lặng lẽ thay thiết kế.

## 2. Thành phần dùng chung

| Thành phần đề xuất | Trách nhiệm |
|---|---|
| AppTheme / ThemeTokens | Parse tokens một lần hoặc ánh xạ hằng C# có nguồn; cấm magic number màu/motion rải rác. |
| OrbitButton | Normal/hover/pressed/focus/disabled/busy; vẽ bóng, sheen, ripple; một đường kích hoạt chung. |
| ProductCardControl | Vẽ ảnh, tên/mô tả/footer giá và dấu cộng cố định tâm; xử lý AddRequested một lần. |
| OrbitTextField | Container có nhãn/viền/focus; TextBox native cho caret, IME, clipboard và tiếng Việt. |
| OrbitBadge | Nhãn trạng thái; màu không thay chữ. |
| OrderPanel | Dòng món, bước tăng giảm, tổng SQL; footer thanh toán tiếp cận được. |
| OrbitDialog | Modal xác nhận/editor; focus vào nội dung phù hợp, trả focus khi đóng. |
| MotionCoordinator | Stopwatch + timer UI chung; quản lý trạng thái hover/press/reveal, không timer mỗi hạt. |
| StarDustService | Opt-in từng button; phát hạt ở overlay cấp form, không gắn toàn bộ nút. |

Giữ DataGridView/ComboBox native ở nghiệp vụ cần nhập liệu; custom paint phần header/selection/border hợp lý. Không custom vẽ lại toàn bộ accessibility của các control phức tạp chỉ để giống web.

## 3. DPI, font và bố cục

- Chốt cấu hình DPI ở đầu ứng dụng theo cấu trúc project .NET 10 đang có; ưu tiên cấu hình thống nhất PerMonitorV2 khi đã kiểm chứng. Không trộn cấu hình .NET Framework/app.config cũ vào project hiện đại.
- Đồng bộ AutoScaleMode giữa các form/container; không đổi tùy tiện giữa Font và Dpi trong cùng cây control.
- JSON dùng logical px tại 96 DPI; device px = logical × DeviceDpi / 96 cho custom painting. Bounds do WinForms autoscale thì không nhân lần nữa.
- Font JSON là CSS px từ reference; pt tương đương = px × 72/96. Ví dụ 14px = 10,5pt; kiểm tra PreferredSize vì renderer WinForms khác browser. Không lấy 14px làm 14pt.
- Dock/Anchor/TableLayoutPanel cho shell; menu sản phẩm co giãn 2–3 cột theo bề rộng dùng được, không chỉ theo độ phân giải vật lý.
- Tên món tối đa vùng hai dòng; mô tả vùng hai dòng; footer giá/cộng cùng baseline. Tên dài không đẩy footer lệch; tooltip/editor cung cấp nội dung đầy đủ.
- Ô dấu cộng 32×32 ở reference, hai nét 12×2px và 2×12px vẽ từ cùng tâm, stroke theo DPI. Không rotate glyph '+' và không nested button bên trong Button.
- Hình có tay cầm phải căn giữa theo toàn silhouette, không chỉ theo thân ly.
- Test 1280×720, 1366×768, DPI100/125/150%; bàn phím, tên tiếng Việt dài và số tiền lớn. DPI cao có thể cần giảm số cột/panel bàn riêng, không giảm font đến khó đọc.

## 4. Vẽ hover/press và chuyển động

1. Paint nền, border, bóng trong/ngoài, ảnh/text rồi focus ring; không vẽ glow sáng đè chữ.
2. Hover product: nâng phần render khoảng 3px, viền/glow tím, spotlight theo con trỏ. Không đổi Bounds của cả grid mỗi MouseMove.
3. Press: cập nhật visual ngay ở MouseDown hoặc kích hoạt bàn phím; 55ms lún, 150ms trả về. MouseUp ngoài nút, capture lost, focus loss phải reset visual, không giữ trạng thái lún.
4. Kích hoạt nghiệp vụ qua duy nhất một đường; MouseDown chỉ vẽ, không vừa gọi nghiệp vụ ở MouseDown vừa gọi ở Click.
5. Timer UI khoảng 16ms chỉ là mục tiêu tick; lấy tiến độ từ Stopwatch, không đếm frame. Không hứa đạt 60fps khi chưa đo Windows thật.
6. Một coordinator invalidate vùng thay đổi; chỉ chạy timer khi có animation. Không Thread.Sleep/DoEvents/vòng lặp repaint chặn UI.
7. Cache ảnh bóng/glow có biên để tránh cắt; custom control có AllPaintingInWmPaint/OptimizedDoubleBuffer/UserPaint phù hợp. Double buffer không tự làm native child HWND trong suốt.

Dọn Pen/Brush/Font/GraphicsPath/Bitmap/Timer do mình sở hữu; không dispose e.Graphics hoặc shared resource chưa hết dùng. DPI đổi phải rebuild raster cache, không scale bitmap nhỏ nhiều lần.

## 5. Bụi sao — opt-in và overlay

Ví dụ cấu hình, không phải code hoàn chỉnh:

```csharp
confirmPaymentButton.EnableStarDust = true;
openPaymentButton.EnableStarDust = false;
addProductButton.EnableStarDust = false;
```

Mặc định false. Không suy ra từ Primary/Danger/Confirm. Nút mới chỉ có nếu người dùng chỉ định.

- 40 hạt, radius75–215 logical px, sao10–19px/hạt3–7px, lifetime850–1250ms. Caramel/lilac/cream theo JSON.
- Tính origin = form.PointToClient(button.PointToScreen(localClick)); keyboard dùng tâm nút. Không dùng screen coordinates trực tiếp để vẽ vào client.
- Overlay phải nằm trên các sibling và vùng cuộn, không chỉ trong clip của Button; pointer/click phải đi xuyên. WinForms child Panel transparent không tự bảo đảm vừa thấy native HWND dưới vừa vẽ phủ mọi thứ. Cần prototype phương án overlay cấp form; nếu cần cửa sổ overlay layered owned form, kiểm tra z-order, không activate, hit-test xuyên, reposition/resize/DPI/minimize và dispose.
- Không tạo 40 Label/Control. Vẽ tất cả hạt trên một surface bằng một timer; ngân sách tối đa280 hạt. Xóa hạt cũ theo lifetime; không để overlay tồn tại khi đóng/chuyển form.
- Cho các action hợp lệ đi qua, không che thông báo lỗi. Disabled/busy/validation fail không phát. Khóa gửi lặp trước khi gọi BLL.
- Bụi sao là phản hồi nhấn, không bằng chứng SQL đã commit. In lỗi sau commit không thu tiền lại. Timeout phải đối chiếu mã đơn, không retry mù.

## 6. Cuộn hai cấp

Reference có cả menu cuộn và trang dài phía dưới. Cuộn menu: reveal opacity0,4→1 + dịch16px/360ms, stagger45ms. Cuộn trang: opacity0,3→1 + dịch22px/520ms, stagger65ms; thanh tiến độ thật và nền dịch nhẹ. Không smooth-scroll hijack bánh xe.

WinForms không có opacity tùy ý cho Panel chứa nhiều child HWND. Reveal text/card nên render trên custom surface hoặc snapshot ngắn; không thay đổi Opacity toàn Form mỗi lần cuộn. Với danh sách lớn, chỉ animate các card mới vào viewport, giới hạn số đang chạy; virtualize hoặc tái sử dụng item nếu số liệu đòi hỏi. Không làm nội dung vô hình nếu animation bị lỗi.

Trang component ở cuối reference phục vụ đối chiếu, không yêu cầu thêm trang marketing vào app. Với nghiệp vụ, áp dụng motion vào vùng nội dung dài thật; giữ footer thanh toán, focus và vị trí cuộn.

## 7. Trình tự làm việc với agent

1. Đọc README, guide, tokens; chạy reference offline, xem ảnh/video.
2. Kiểm tra source/AGENTS của dự án; không phá Designer, quyền hoặc scope task đang duyệt.
3. Theme → button/card → shell/POS → xác nhận thanh toán → overlay bụi sao → cuộn.
4. Chạy Windows, chụp cùng viewport và trạng thái. So sánh card alignment, text contrast, padding/size, focus, glow, press/release, full-page scroll và clip hạt.
5. Sửa visual trong scope được giao, ghi rõ sai khác còn lại. Không áp dụng hàng loạt sang module khác trước khi POS đạt đối chiếu.
6. Không tick nghiệm thu Txx/UX-Kxx chỉ từ screenshot hoặc build thành công. Giữ quy trình duyệt/test/commit của dự án.

## 8. Tiêu chí kiểm tra

| Ca | Kết quả cần đạt |
|---|---|
| Hover/leave nhanh | Không rung layout, không kẹt glow/pressed. |
| Nhấn/giữ/thả ngoài, keyboard | Visual reset, action không chạy hai lần. |
| Double click xác nhận | Một lệnh nghiệp vụ, disabled chặn burst tiếp theo. |
| Các nút thường | Không bụi sao; hover/press vẫn có. |
| 40 hạt sát mép/vùng cuộn | Bay ngoài nút, không clip nội bộ, không nhận chuột. |
| Cuộn dài/filter/đổi màn | Không mất focus, không nhảy về đầu ngoài chủ đích; query muộn không ghi đè. |
| 100 lần đổi module | Không tăng liên tục GDI handles/timer/ảnh; theo dõi số liệu thực. |
| Tên dài/tiền lớn/DPI150% | Không cắt tiếng Việt, giá và CTA; card cùng baseline. |
| SQL chậm/lỗi/timeout/in lỗi | Trạng thái đúng contract, không success giả hoặc checkout lại. |

Nguồn kỹ thuật chính thức (tham khảo ngày17/10/2026):
- https://learn.microsoft.com/en-us/dotnet/desktop/winforms/forms/autoscale
- https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/how-to-reduce-graphics-flicker-with-double-buffering-for-forms-and-controls
- https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.timer
- https://motion.dev/docs/scroll (tham chiếu hành vi web; không là dependency WinForms)
