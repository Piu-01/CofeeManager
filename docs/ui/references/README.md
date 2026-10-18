# Ảnh và video đối chiếu

Nguồn: `../reference.html` trong cùng gói. Capture trên Chromium153, Linux/headless, viewport1366×768 hoặc1280×720, deviceScaleFactor1. Font dùng fallback hệ thống khi Segoe UI không có; ảnh không phải screenshot WinForms/Windows.

## Ảnh

| File trong screenshots/ | Trạng thái |
|---|---|
| pos-1366x768.png | Màn POS ban đầu, sau khi reveal kết thúc. |
| pos-1280x720.png | Viewport nhỏ hơn; dữ liệu đã thêm một món trong lượt capture. |
| page-full-1366.png | Toàn bộ trang reference, gồm khu đối chiếu component/màu/motion. |
| card-hover.png | Hover card, glow và viền nổi. |
| payment-before-confirm.png | Mở xác nhận; chưa có bụi sao. |
| payment-stardust.png | Khoảng220ms sau xác nhận, bụi sao bay ngoài nút. |
| tables.png | Bàn/khu vực demo. |
| menu-scrolled.png | Menu đã cuộn xuống; tiến độ cuộn thật. |
| page-components.png | Cuộn toàn trang xuống các phần đối chiếu bên dưới. |

## Video

- `videos/hover-click-stardust.mp4`: hover/lia chuột, nhấn giữ/thả, thêm món, mở thanh toán, xác nhận và bụi sao. Chỉ xác nhận có bụi sao.
- `videos/scroll-menu-page.mp4`: cuộn menu rồi cuộn trang dài, reveal các nhóm và trở về đầu.

Các clip quay trình duyệt thật bằng Playwright, không phải dựng lại hoặc hoạt hình mô phỏng. MP4 H.264, không âm thanh. Bụi sao là animation phản hồi demo, không chứng minh giao dịch SQL.

`computed-styles.json` ghi CSS thực đo để xử lý trường hợp selector override; ưu tiên kích thước hiệu lực, không lấy một declaration CSS bất kỳ rồi coi là kích thước cuối.

## Giới hạn

- Chưa có source app Windows: chưa kiểm tra WinForms, DPI125/150%, GDI handles, Designer hoặc kết nối SQL.
- Viewport1366×768 khác kích thước vùng client của WinForms vì titlebar/border hệ điều hành; đối chiếu trên vùng client tương đương.
- Card và order tiếp tục theo đúng mẫu đã xem; tại viewport thấp cần cuộn trang để thấy toàn footer. Khi code app thật, bố trí footer thanh toán tiếp cận được theo UX gốc, không copy độ cao trang component vào POS.
- Không thêm toàn bộ phần component ở cuối HTML vào app sản phẩm; chúng chỉ để kiểm tra skin/motion.

## Tái tạo

Dành cho agent có Node.js/Playwright và browser đã cài. Các dependency chỉ nằm trong ui/tools (tooling riêng), không đưa vào runtime WinForms. Cài một lần nếu cần tái tạo: `npm --prefix ui/tools install` và `npx --prefix ui/tools playwright install chromium`. Sau đó chạy:

```sh
node ui/tools/capture-reference.cjs
node ui/tools/record-interactions.cjs
```

Đặt `ORBIT_CHROMIUM` là đường dẫn executable nếu dùng browser có sẵn. Khi dùng Playwright-managed browser, bỏ biến này và cài Chromium theo Playwright. Script capture tạo PNG/SVG; script record tạo WebM. Chuyển từng WebM sang MP4:

```sh
ffmpeg -i input.webm -c:v libx264 -crf 20 -pix_fmt yuv420p -movflags +faststart output.mp4
```

Video gốc không được ghép thêm hiệu ứng; không thay ảnh/video chuẩn bằng ảnh tạo giả khi không chạy được browser.
