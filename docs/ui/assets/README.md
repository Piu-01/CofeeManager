# Tài nguyên Orbit Café

## Nguồn chuẩn

- `icons/svg/`: Lucide, geometry kế thừa bộ người dùng gửi và ba icon dùng ở rail được xuất từ chính reference. Dùng currentColor khi renderer hỗ trợ.
- `icons/png/milk/20|24|32/`, `icons/png/caramel/20|24|32/`: PNG trong suốt cho Resources WinForms, không cần thêm thư viện SVG. Chọn theo tone và DPI/kích thước đích; không upscale 20px thành ảnh lớn.
- `products/iced-coffee.png`: ly cà phê/sữa đá.
- `products/ceramic-coffee.png`: ly nóng có tay cầm.
- `products/iced-matcha.png`: ly matcha/sữa.
- `branding/orbit-logo.svg`, `branding/orbit-mark.svg`: nhận diện minh họa Orbit, nền trong suốt; không phải tên quán đã đăng ký.
- `licenses/`: Lucide ISC/Feather MIT và nguồn artwork gốc. Font hệ thống không đóng gói.

## Mapping 12 món trong reference

| ID | Tên | File ảnh |
|---|---|---|
| 1 | Cà phê sữa đá | iced-coffee.png |
| 2 | Caramel latte | ceramic-coffee.png |
| 3 | Americano | ceramic-coffee.png |
| 4 | Matcha latte | iced-matcha.png |
| 5 | Cappuccino | ceramic-coffee.png |
| 6 | Cà phê đen đá | iced-coffee.png |
| 7 | Bạc xỉu | iced-coffee.png |
| 8 | Mocha | ceramic-coffee.png |
| 9 | Trà sữa matcha | iced-matcha.png |
| 10 | Latte nóng | ceramic-coffee.png |
| 11 | Trà xanh sữa | iced-matcha.png |
| 12 | Flat white | ceramic-coffee.png |

Ảnh món là minh họa schematic xuất từ CSS của reference, chưa phải ảnh thật. Màu/nền được giữ như bản mẫu; reference tiếp tục vẽ CSS tự chứa để hover ly còn hoạt động. PNG là tài nguyên tương ứng để agent dựng WinForms, không tự thay bằng ảnh stock/emoji khác.

Giữ tỷ lệ ảnh, không stretch; `Zoom`/contain, căn giữa theo toàn silhouette. Glow nền trong ảnh là một phần bản xuất; không thêm lại hai lần làm ảnh quá sáng. Nếu cần ảnh món thật về sau phải có bộ ảnh thống nhất và được duyệt, không tự đổi trong lần thực hiện theo gói này.

## License

Giữ `licenses/LUCIDE-LICENSE.txt` trong source/distribution theo license; không nhận là icon tự vẽ. Logo và hình CSS là artwork gốc cho concept này, được phép tái sử dụng/chỉnh sửa cho dự án. Không đưa lại bộ logo espresso cũ hoặc ảnh món cũ chưa khớp Orbit.
