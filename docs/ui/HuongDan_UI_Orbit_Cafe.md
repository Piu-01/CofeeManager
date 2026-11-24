# Hướng dẫn sử dụng phong cách UI và hiệu ứng — Orbit Café

**Cập nhật:** 17/10/2026  
**Dự án:** Quản lý quán cà phê — C# WinForms .NET 10, SQL Server  
**Mục đích:** Ghi lại yêu cầu thiết kế đã trao đổi để người lập trình/agent áp dụng thống nhất.  
**Tên Orbit:** Tên concept minh họa, chưa phải quyết định đổi tên quán.

> **Quy định mới nhất về bụi sao:** Chỉ gắn vào **nút xác nhận thanh toán** và **những nút được người dùng chỉ định rõ**. Mặc định mọi nút khác **không có bụi sao**. Không tự mở rộng sang toàn bộ nút, toàn bộ nút chính hoặc toàn bộ thao tác xác nhận.

## 1. Phong cách tổng thể

Thiết kế hiện đại, hightech, hơi hướng galaxy/vũ trụ nhưng vẫn giữ tông màu và cảm giác cà phê.

- Nền espresso gần đen; vùng nội dung có sắc tím than nhẹ.
- Caramel là màu nhấn ấm cho hành động quan trọng, giá và tổng tiền.
- Tím lilac tạo điểm nhấn công nghệ, ánh sáng và bụi sao.
- Chữ kem sữa sáng, dễ đọc trên nền tối.
- Có thể dùng hành tinh, quỹ đạo và ánh sáng tinh vân tiết chế trong vùng nhận diện; vùng nhập liệu và dữ liệu cần rõ ràng.
- Không quay lại bộ nâu–kem cổ điển hoặc giao diện quản trị mẫu thiếu điểm nhấn.

### Bảng màu tham chiếu của bản đã xem

| Vai trò | Màu |
|---|---|
| Nền espresso | `#100E15` |
| Card/nền nhập liệu | `#211B28` |
| Chữ chính — kem sữa | `#FFF4E7` |
| Chữ phụ | `#D0C2D7` |
| Nhấn caramel | `#FFD09A` |
| Nhấn cosmic lilac | `#D0BEFF` |
| Viền thông thường | `#514357` đến `#594760` |
| Chữ trên nút caramel sáng | `#28180E` |

Màu là cơ sở của concept; khi bổ sung màn hình, điều chỉnh theo vai trò và kiểm tra tương phản, không tùy tiện thêm nhiều màu mới.

## 2. Tương phản chữ và nền

Người dùng đã phản ánh một số chữ bị đen trên nền tối, rất khó đọc. Đây là lỗi phải tránh khi áp dụng.

- Đặt màu chữ rõ ràng cho tiêu đề, nội dung, mô tả món, giá, số lượng, input, placeholder, badge và dialog.
- Không để màu mặc định của control hoặc theme bên ngoài ghi đè thành chữ đen trên nền tối.
- Nền tối dùng chữ kem sáng; chữ phụ vẫn đủ sáng, không mờ đến mức khó đọc.
- Nút caramel sáng dùng chữ espresso đậm để đọc rõ.
- Kiểm tra đủ trạng thái: bình thường, hover, đang nhấn, focus, disabled và đang xử lý.
- Ánh sáng/glow không được làm lóa chữ hoặc làm mất đường nét của icon.

**Mục tiêu kiểm tra:** Chữ thường có tương phản tối thiểu 4,5:1; chữ lớn tối thiểu 3:1. Đây là tiêu chí cần kiểm tra trên các cặp màu thực tế, không coi việc có bảng màu là đã nghiệm thu toàn bộ giao diện.

## 3. Bố cục card sản phẩm

Các thành phần phải cân đối và thẳng hàng giữa các card cùng hàng.

| Thành phần | Cách áp dụng |
|---|---|
| Vùng hình món | Cùng chiều cao và căn giữa theo tổng hình, kể cả phần tay cầm ly. |
| Tên món | Cùng vị trí bắt đầu; dự trù tên dài xuống dòng. |
| Mô tả | Dành vùng có chiều cao nhất quán; không đẩy giá lệch hàng. |
| Giá | Cùng đường căn ở chân card; không nhảy khi tên/mô tả dài. |
| Dấu cộng | Nằm chính giữa ô vuông, hai nét cân nhau; không dùng glyph lệch tâm. |
| Khoảng cách | Padding, gap và kích thước ô dấu cộng thống nhất. |

**Dấu cộng không xoay khi hover hoặc click.** Người dùng đã phản ánh xoay làm dấu cộng trông đảo/lệch. Giữ dấu cộng đứng yên; phản hồi bằng sáng hơn hoặc lún nhẹ.

Card có thể nổi lên nhẹ khi hover. Bản tinh chỉnh đã bỏ nghiêng cả card để giữ nội dung cân và dễ đọc; không tự thêm lại xoay/nghiêng làm lệch thành phần.

### 3.1. Variant card ngang cho POS gọn —30/10/2026

Theo mẫu user Orbit_BanHang_Gon và plan bổ sung T16 đã duyệt, POS hiện dùng card
ngang: ảnh schematic bên trái, tên tối đa2dòng và giá bên phải, ô+ vuông đứng yên
ở góc dưới. Chiều cao105logical là mục tiêu card này; hai cột nếu đủ chỗ, một
cột nếu hẹp. Giá/cộng cùng baseline, chỉ+ kích hoạt Add một lần. CanAdd=false
khóa nút và có chữ Ngừng/thiếu công thức. Dùng Resources/theme chung, không
hardcode giá/tên/ảnh thật hoặc tạo callback riêng ở ảnh/tên gây Add lặp.

Variant này ưu tiên mẫu POS gọn cho màn Bán hàng, không tự đổi mọi card/danh mục
khác. Layout và fonts theo WinForms/DPI thực; chưa nghiệm thu hình thức từ PNG.

## 4. Hiệu ứng hover — lia chuột

Hover phải đủ rõ, bóng và có chiều sâu, không chỉ đổi màu rất nhẹ.

### Card món

- Card nổi lên nhẹ khoảng 3px.
- Viền sáng tím/kem và glow rõ hơn nền.
- Ánh sáng mềm đi theo vị trí con trỏ trên bề mặt card.
- Hình đồ uống nhấc lên nhẹ; không làm layout dịch chuyển.
- Ô dấu cộng sáng và bóng hơn, vẫn giữ đúng tâm, không xoay.

### Nút bấm

- Có lớp bóng bề mặt, mép sáng và bóng đổ tạo chiều sâu.
- Nút chính caramel sáng hơn khi hover; có vệt sáng quét ngắn khi chuột đi vào.
- Nút phụ và nút tăng/giảm có ánh tím nhẹ, viền rõ và highlight bề mặt.
- Khi chuột rời đi, trở về trạng thái thường mượt, không nhấp nháy.
- Nút disabled không được sáng/nhún như nút có thể bấm.

Không chạy vệt sáng lặp vô hạn; chỉ phản hồi khi có tương tác.

## 5. Hiệu ứng click — cảm giác nhấn dứt khoát

Yêu cầu của người dùng: click mạnh, rõ và gọn hơn bản ban đầu.

- Phản hồi bắt đầu ngay khi nhấn xuống, không đợi hoàn tất nghiệp vụ.
- Nút/card lún nhẹ, thu nhỏ vừa phải và giảm bóng nổi; xuất hiện bóng trong để tạo cảm giác có lực.
- Thả ra thì bật về nhanh, không nảy kéo dài hoặc rung liên tục.
- Áp dụng phản hồi tương đương khi kích hoạt bằng bàn phím.
- Có thể dùng ripple ngắn và highlight xác nhận thao tác; **ripple thông thường khác với bụi sao**.
- Một lần kích hoạt chỉ thực hiện một hành động; animation không làm thêm món hoặc gửi thanh toán nhiều lần.

### Thông số tham chiếu để triển khai

| Hiệu ứng | Thông số tham chiếu |
|---|---|
| Lún khi nhấn | Khoảng 55–80ms |
| Trở về khi thả | Khoảng 120–180ms |
| Card khi nhấn | Scale khoảng 0,972 và dịch xuống 1px |
| Nút chính khi nhấn | Scale khoảng 0,975 và dịch xuống 3px |
| Ripple ngắn | Khoảng 300ms |

Đây là thông số từ concept để tinh chỉnh theo control thực tế; phải giữ cảm giác nhanh và không thay kích thước bố cục.

## 6. Bụi sao caramel–tím — chỉ định từng nút

### 6.1. Phạm vi áp dụng bắt buộc

| Nút/thao tác | Có bụi sao? |
|---|---|
| **Xác nhận thanh toán** trong bước xác nhận cuối | **Có** |
| Nút khác được người dùng yêu cầu gắn bụi sao rõ ràng | **Có, đúng nút được chỉ định** |
| Nút mở/xem dialog thanh toán | Không mặc định; chỉ có nếu được chỉ định thêm |
| Card món hoặc dấu cộng thêm món | Không |
| Tăng/giảm số lượng | Không |
| Danh mục món, lọc, tìm kiếm | Không |
| Menu điều hướng, chọn bàn | Không |
| Quay lại, đóng, hủy | Không |
| Lưu, chốt kho, trả nợ, chốt/chi lương và các nút xác nhận khác | Không mặc định; cần chỉ định riêng |

**Không được suy ra rằng nút quan trọng, nút chính hoặc có chữ “Xác nhận” đều được bật bụi sao.** Danh sách cho phép hiện tại chỉ có xác nhận thanh toán; những nút khác bổ sung theo yêu cầu cụ thể sau này.

### 6.2. Hình thức đã được người dùng chấp nhận

Bản đầu quá nhỏ, gần như không nhìn thấy. Bản tăng phạm vi đã được người dùng nhận xét “ok được rồi”. Giữ các thông số tham chiếu sau:

| Thuộc tính | Giá trị |
|---|---|
| Số hạt | Khoảng 40 hạt/lần kích hoạt |
| Phạm vi bắn | Khoảng 75–215px từ điểm bấm, ở kích thước tham chiếu |
| Thời gian tồn tại | Khoảng 0,85–1,25 giây |
| Hạt nhỏ | Khoảng 3–7px |
| Sao nổi bật | Khoảng 10–19px, xen kẽ hạt nhỏ |
| Màu | Caramel sáng, tím lilac và kem sữa |
| Độ sáng | Glow rõ trên nền tối; hạt sáng phần lớn quãng bay rồi tan |
| Chuyển động | Bắn tỏa nhiều hướng, có độ rơi nhẹ; vòng sáng lan ngắn ở tâm bấm |

- Bụi sao cần bay ra ngoài phạm vi nút, không bị cắt bởi viền nút/card hoặc vùng cuộn.
- Vị trí phát sinh là điểm bấm; khi dùng bàn phím, dùng tâm nút.
- Lớp hiệu ứng không nhận chuột và không chặn nút/ô nhập bên dưới.
- Tự dọn sạch sau hiệu ứng; giới hạn tổng số hạt đang tồn tại để tránh tích lũy khi bấm nhanh.
- Có thể giới hạn phạm vi theo màn hình nhỏ nhưng vẫn phải nhìn thấy rõ.

### 6.3. Cách gắn vào nút

Thiết kế mặc định là **tắt**. Bật theo danh sách cho phép hoặc thuộc tính riêng của từng nút, chẳng hạn `EnableStarDust = true` cho nút xác nhận thanh toán.

Không bắt click của mọi button rồi phát bụi sao vô điều kiện. Nếu dùng cơ chế xử lý tập trung, phải kiểm tra nút thuộc danh sách được bật trước khi tạo hạt.

Nút disabled, thao tác không hợp lệ hoặc đang bị khóa gửi lặp không phát bụi sao. Bụi sao chỉ là phản hồi kích hoạt, **không phải bằng chứng thanh toán đã thành công**. Trạng thái thành công vẫn phải chờ kết quả SQL; không phát lại bụi sao khi hệ thống tự kiểm tra kết quả hoặc retry.

## 7. Hiệu ứng khi cuộn màn hình dài

Người dùng yêu cầu có hiệu ứng khi lướt/cuộn và cần vùng xem thử đủ dài để cảm nhận được.

- Card/nhóm nội dung xuất hiện khi vào vùng nhìn: tăng độ rõ và trượt lên nhẹ khoảng 12–16px.
- Các phần tử cùng hàng có độ trễ nhỏ để tạo nhịp, không xuất hiện đồng loạt cứng.
- Nền tinh vân có thể dịch nhẹ theo tiến độ cuộn; chữ, số tiền và form không bị kéo trôi.
- Có thể hiển thị thanh tiến độ cuộn mảnh khi phù hợp; phải phản ánh vị trí cuộn thật.
- Cuộn vẫn theo điều khiển bình thường của chuột, touchpad hoặc bàn phím; không ép người dùng theo một chuyển cảnh dài.
- Không giữ nội dung vô hình nếu animation không chạy; phải có phương án hiện trực tiếp.
- Danh sách cập nhật dữ liệu không chạy lại animation toàn màn hoặc làm mất vị trí cuộn.

**Thông số tham chiếu:** reveal khoảng 360ms; lệch nhịp giữa hai card khoảng 45ms. Bản thử dùng menu 12 món có vùng cuộn riêng; số món và chiều cao vùng này là dữ liệu thử, không là giới hạn cho ứng dụng thật.

## 8. Cách sử dụng tài liệu khi triển khai

1. Áp dụng màu nền/chữ, tương phản và bố cục trước.
2. Căn card, tên, mô tả, giá và dấu cộng đồng đều.
3. Thêm hover bóng và phản hồi click dứt khoát cho nút có tương tác.
4. Thêm hiệu ứng cuộn cho khu vực nội dung dài.
5. **Chỉ bật bụi sao cho xác nhận thanh toán; nút khác giữ tắt cho đến khi có chỉ định.**
6. Kiểm tra trên Windows thật, nhiều kích thước màn hình và DPI; tối ưu nếu bị giật/nháy.

Dự án dùng WinForms, nên hiệu ứng trong bản xem HTML là tài liệu tham chiếu trải nghiệm. Không mặc định CSS/Web Animations có thể sao chép nguyên sang WinForms, không tự đổi stack hoặc nhúng web chỉ để có hiệu ứng. Control nền tảng cần xử lý vẽ, timer, DPI và giải phóng tài nguyên phù hợp.

Giữ nguyên quyền, nghiệp vụ và phạm vi dự án. UI/animation không được tự xác nhận kết quả tiền/kho/điểm, thay công thức SQL hoặc bỏ qua bước xác nhận.

## 9. Checklist đối chiếu

- [ ] Chữ không bị đen trên nền tối; tương phản rõ ở mọi trạng thái.
- [ ] Ảnh, tên, mô tả, giá và dấu cộng thẳng hàng/cân đối trong card.
- [ ] Dấu cộng không xoay và luôn nằm chính giữa.
- [ ] Hover có độ bóng/chiều sâu rõ; không che chữ.
- [ ] Nhấn xuống phản hồi ngay, thả ra nhanh; không kích hoạt hành động hai lần.
- [ ] Cuộn nội dung dài có reveal/nền dịch nhẹ nhưng không làm khó thao tác.
- [ ] Bụi sao chỉ có ở xác nhận thanh toán và các nút đã được chỉ định riêng.
- [ ] Card món, tăng/giảm, bộ lọc, menu, đóng/hủy không phát bụi sao mặc định.
- [ ] Bụi sao đủ xa, lớn, sáng và tồn tại đủ lâu theo bản đã chấp nhận.
- [ ] Bụi sao không bị giới hạn trong nút, không chặn input, không tích lũy tài nguyên.
- [ ] Kiểm tra màn nhỏ/DPI cao, thao tác bàn phím, trạng thái đang ghi và lỗi SQL.

## 10. Ghi chú về phiên bản

Tài liệu này tổng hợp hướng Orbit hightech–cà phê đã trao đổi, thay cho hướng Fresh Workspace trắng–teal trước đó trong phạm vi phong cách thị giác.

**Điều chỉnh ngày 17/10/2026:** Đã bỏ mục yêu cầu Giảm chuyển động theo chỉ định mới nhất của người dùng. Quy tắc bụi sao chọn từng nút ở mục 6 thay thế yêu cầu cũ “áp dụng mọi nút”. Các bản xem thử trước đó còn phát bụi sao trên mọi nút chỉ là phiên bản cũ, không dùng hành vi đó làm yêu cầu triển khai. Gói ui/ này đã đồng bộ reference.html: chỉ Xác nhận thanh toán có bụi sao; đã bỏ Giảm chuyển động; có cuộn menu và cuộn toàn trang. Không dùng ZIP trắng–teal cũ làm chuẩn.

Các checklist trên là tiêu chí đối chiếu, chưa được đánh dấu nghiệm thu. Kiểm tra logic ở các bản thử không thay kiểm thử hiển thị/chuyển động thực tế trên Windows.

## 11. Chuẩn trang danh sách dùng chung — 26/10/2026

Người dùng yêu cầu ghi lại thiết kế sau khi triển khai Lịch sử hóa đơn V4 để
các màn code sau nhất quán, gồm Khách hàng, Giao hàng và các trang quản lý có
tìm kiếm/bộ lọc/bảng. Đây là yêu cầu chung được chốt trực tiếp, không là quy
tắc tự trích từ README demo. POS chia vùng món/đơn và dialog editor có bố cục
nghiệp vụ riêng; không ép các màn đó thành trang danh sách.

### 11.1. Bố cục theo vùng nội dung thực tế

- UserControl của trang giãn hết vùng nội dung MainForm, bên phải sidebar và
  dưới topbar. Giữ padding của workspace; “hết màn hình” ở đây là hết vùng
  nội dung được cấp, không tràn lên sidebar/topbar hoặc ra ngoài cửa sổ.
- Header gồm nhãn workspace, tên màn và mô tả. Card danh sách bên dưới giãn
  cả ngang lẫn dọc theo vùng còn lại; không đặt chiều cao bảng/card bằng số
  dòng đang có, kích thước Designer cố định hoặc một hàng50% của container.
- Trong card, xếp theo thứ tự ở bảng dưới. Bảng ít dòng/rỗng vẫn giữ vùng
  bảng đầy đủ. Màn Giao hàng code sau phải theo cùng nguyên tắc giãn vùng như
  Khách hàng; không coi bảng chỉ chiếm nửa chiều cao dù còn khoảng trống là mẫu.

| Vùng | Quy tắc |
|---|---|
| Hàng1: tìm kiếm |Ô tìm kiếm co giãn chiếm toàn chiều ngang còn lại; bên phải là kính lúp và làm mới cùng nhóm nút icon. Không nhét bộ lọc vào hàng này làm ô tìm chỉ còn một đoạn ngắn. |
| Hàng2: bộ lọc |Nhãn Lọc theo và các trường theo nghiệp vụ: loại ngày, khoảng từ–đến, trạng thái, kênh… Chỉ hiện trường màn đó cần; không chép filter hóa đơn sang mọi màn. Thiếu ngang thì wrap ở hàng dưới, không chồng hoặc cắt control. |
| Hàng hành động |Các hành động trên dòng như Chi tiết/In (chứng từ) hoặc Công thức/Mở sổ điểm/Lịch sử điểm (màn dữ liệu) ở hàng riêng, căn phải. Master data **không** còn nút Sửa/Xóa: double-click mở hộp sửa và xóa bằng thùng rác từng dòng theo mục11.5. Hành động tạo mới dùng vị trí PrimaryActions của workspace; không trộn vào ô tìm hoặc footer phân trang. |
| Vùng bảng |Giãn hết chiều ngang card và chiều cao còn lại sau toolbar/action/footer. Grid cuộn bên trong vùng này; mã/ngày/tiền không bị ép mất nội dung chỉ để vừa chiều ngang. |
| Footer card |Phạm vi hiển thị bên trái; Trước, chỉ số trang, Sau bên phải. Luôn tiếp cận được khi resize và khi bảng cuộn. Không thêm summary đếm/khoảng ngày phía trên bảng theo mẫu V4. |
| Trạng thái tải |Dòng trạng thái bên dưới card; phân biệt loading/rỗng/lỗi/hủy tải. Không dùng tổng0 hoặc dữ liệu bộ lọc cũ để giả tải thành công. |

### 11.2. Tìm kiếm và bộ lọc

- Kính lúp thay nút chữ “Áp dụng bộ lọc” trên mẫu danh sách mới. Kính lúp và
  Enter trong ô tìm chỉ submit từ khóa; gõ chữ chưa tự tìm. Có AccessibleName,
  tooltip và trạng thái focus/disabled phù hợp, không dùng icon không có tên.
- Chọn bộ lọc nào thì tự áp dụng bộ lọc đó, không phải bấm thêm kính lúp hoặc
  Áp dụng. Luôn query bằng **từ khóa đã submit gần nhất**. Nội dung đang sửa
  trong ô tìm là nháp; đổi ngày/trạng thái/kênh không được vô tình submit nó.
- Đổi filter hoặc submit từ khóa mới về trang đầu. Chuyển trang và làm mới
  cũng dùng từ khóa đã submit, giữ filter hiện hành. Xóa ô tìm chỉ xóa nháp;
  submit ô trống mới bỏ điều kiện từ khóa.
- Ví dụ: đã tìm HD20250421, sửa ô thành CHUAGUI nhưng chưa submit. Đổi trạng
  thái vẫn lọc HD20250421; bấm kính lúp/Enter sau đó mới tìm CHUAGUI.
- Hủy truy vấn cũ/generation guard khi lọc nhanh; kết quả đến muộn không đè
  kết quả mới. Lỗi/ngày sai giữ input, báo rõ; không mở Chi tiết từ dòng cũ
  không còn thuộc tập kết quả. Không tự đổi SQL/quyền chỉ để đổi giao diện.

### 11.3. Bảng, phân trang và tái sử dụng

- Giữ font/màu/header/selection/radio/badge từ theme và profile chung. Badge
  có chữ, không chỉ màu. Mã hóa đơn đủ toàn bộ10ký tự; ngày giờ dd/MM/yyyy HH:mm;
  tiền căn phải theo vi-VN, ví dụ81.000 ₫. Bảng tra cứu readonly.
- Cột nội dung dài dùng fill/minwidth phù hợp; nếu không đủ ngang thì grid
  cuộn ngang. Không chia viewport thành hai nửa trống chỉ để giữ width cột cũ.
- Khi contract đã trả TotalCount thì dùng tổng thật cho range/số trang/nút Sau,
  kể cả trang cuối đủ50dòng. Không đoán còn trang chỉ vì rowCount bằng50. Nếu
  contract chưa có tổng, giữ cơ chế server paging tương ứng, không bịa tổng.
- Tái dùng AppTheme/OrbitManagementTheme và control tại mục2.1 của
  [hướng dẫn WinForms](WINFORMS_IMPLEMENTATION.md). Mở rộng tùy chọn dùng chung
  khi cần; không tạo bản sao theme/grid/date/search cho từng trang. Profile
  InvoiceHistory chỉ dành cho cột hóa đơn, không áp vào bảng giao hàng/khách.
- Form chi tiết/thêm/sửa vẫn là modal Windows có owner, titlebar/X native,
  ShowInTaskbar=false. Input có thể sửa dùng helper chọn hết khi mới vào ô.

### 11.4. Điều kiện bàn giao lần code tiếp theo

- Kiểm cây container/Dock/Anchor và bounds thực tại125% trên1280×720,
  1366×768: card/bảng giãn hết phần còn lại, filter wrap không làm mất footer.
- Kiểm cả nhiều dòng, ít dòng, rỗng, loading/lỗi và resize; không suy bố cục
  đúng từ riêng màn Khách hàng rồi bỏ kiểm màn Giao hàng hoặc màn mới.
- Người dùng kiểm hình thức. Agent không chụp ảnh/so hình/nghiệm thu visual thay
  người dùng theo AGENTS.md;100/150% chưa chạy phải ghi NOT RUN, không đổi Scale.
- Mẫu là [V4 HTML](invoice-history-v4/Orbit_LichSuHoaDon_V4.html) và
  [V4 PNG](invoice-history-v4/Orbit_LichSuHoaDon_V4_Laptop.png), chỉ để tham chiếu.
  Không chạy demo JS làm logic app hoặc coi mẫu là bằng chứng WinForms PASS.
- Việc ghi quy tắc này chưa sửa các màn cũ. Khi được giao chỉnh một màn, áp
  dụng chuẩn trong phạm vi đó và ghi kết quả thực tế; không tự báo các màn đã đạt.

## 11.5. Sửa bằng hộp thoại, xóa tại dòng — 23/11/2026

Người dùng chốt thay mô hình "sửa tại ô" bằng **double-click mở hộp sửa, xóa tại
dòng** cho các màn master data: Menu sản phẩm, Danh mục nền, Bàn/Khu vực, Nguyên liệu,
Khách hàng, Nhân viên, Tài sản, Hạng thành viên. Mục này là **chuẩn chung cho mọi màn
danh sách master data hiện có và màn làm sau**, không chỉ 8 màn hiện tại; thay cách
hiểu hàng "Hàng hành động" ở mục11.1 **riêng cho master data**. Chứng từ (hóa đơn,
phiếu nhập/xuất, phiếu chi) và dialog thêm mới giữ nguyên quy tắc cũ. Khuyến mãi và
Chấm công chưa chuyển trong đợt này.

Quy tắc bắt buộc cho màn master data làm sau: dùng `OrbitRowEditor` và hộp sửa hiện
có của màn; click một lần chọn dòng, double-click hoặc F2 mở hộp sửa; **không** dựng
editor nhập tại ô, không tự tạo pipeline sửa inline riêng.

### 11.5.1. Bỏ nút Sửa/Xóa, không chừa khoảng trống

- Bỏ thao tác Sửa/Xóa khỏi màn; không chỉ ẩn nút. Hàng hành động dồn các thao tác
  còn lại sang phải; khi màn không còn thao tác dòng nào thì ẩn cả hàng theo bố cục
  workspace, không để dải trống hoặc khoảng hở giữa tiêu đề và bảng.
- Thêm mới vẫn ở vị trí PrimaryActions. Các thao tác khác (Công thức, Mở sổ điểm,
  Lịch sử điểm, Yêu cầu chờ, Chi tiết) giữ nguyên vị trí và điều kiện hiện có.
- Không đổi quyền: nút/hành động chỉ hiện và chỉ chạy khi người dùng có quyền theo
  service hiện có; engine UI không tự quyết định quyền.

### 11.5.2. Double-click mở hộp sửa

- Ô có thể sửa khi lia chuột: chữ đổi màu Caramel (badge/checkbox đổi viền Caramel) thay
  cho viền inset, con trỏ đổi thành Hand để báo double-click được. Không dùng IBeam vì
  không còn nhập tại ô. Ô chỉ đọc và vùng trống không đổi màu chữ hoặc con trỏ.
- Click một lần vào dòng chỉ chọn dòng, không mở editor và không mở hộp sửa.
- Double-click vào ô dữ liệu của dòng mở hộp sửa hiện có của màn, nạp đúng dòng; không
  cần click chọn trước. Double-click ô thùng rác chỉ xóa theo luồng hiện tại, không mở
  hộp sửa.
- F2 tại dòng đang chọn mở cùng hộp sửa. Không có editor overlay, không nhập trực tiếp
  trên lưới, không dùng Enter/Tab/Esc để ghi ô.
- Hộp sửa dùng lại dialog/control hiện có của màn và `OrbitCellEditor` làm ô nhập một
  dòng khi phù hợp; không tạo dialog sửa dòng tổng quát hoặc nhân bản pipeline lưu.
- Sau khi lưu thành công: đóng hộp, tải lại dữ liệu và chọn lại đúng dòng theo mã.
  Hủy hộp không ghi dữ liệu. Lỗi validate/SQL giữ hộp và giá trị đã nhập, báo ở status;
  không ghi một phần và không tự khôi phục giá trị cũ.
- Ghi qua đúng service/Normalize/revision/requestId hiện có; UI không chứa SQL, không
  tính lại tiền/tồn/điểm.

### 11.5.3. Xóa tại dòng

- Mỗi dòng dữ liệu có ô thùng rác 44px ở cuối (cột `Delete` chung), click xóa đúng
  dòng đó kể cả chưa chọn dòng. Dòng đang khóa không hiện/không bấm được thùng rác.
- Xác nhận trước khi xóa với dữ liệu kho, tài chính, lịch sử hoặc có ràng buộc quan
  trọng: Nguyên liệu, Tài sản, Khách hàng, Nhân viên, Hạng thành viên, Nhà cung cấp và
  loại Thu/Chi. Dữ liệu tạm/thuần bán hàng (sản phẩm, bàn, khu vực) xóa ngay không hỏi.
- Xóa ngay chỉ báo kết quả ở dòng trạng thái ("Đã xóa…"), không toast, không undo
  riêng, không thêm bước xác nhận phụ. Bị chặn (còn dữ liệu tham chiếu, mã hệ thống,
  khu vực còn bàn, lịch sử) thì dòng còn nguyên, lỗi hiện ở trạng thái.
- Hạng thành viên giữ bước xem tác động (bao nhiêu khách đổi hạng) rồi mới xác nhận.

### 11.5.4. Kết quả ghi chưa xác định (timeout)

- Chỉ khi hệ thống trả "kết quả chưa xác định" mới **khóa đúng dòng đó**; các dòng
  khác vẫn sửa/xóa bình thường. Dòng khóa có veil nhạt + vạch trái và nút **Đối chiếu**
  căn giữa dòng (không đặt trong ô, không mở dialog). Dòng khóa vẫn chọn được để đọc.
- Bấm Đối chiếu: nếu xác định đã ghi → tải lại, mở khóa, báo "Đã đối chiếu: đã ghi.";
  nếu chưa ghi → tải lại, mở khóa, báo "Chưa ghi; có thể sửa lại."; không xác định được
  → giữ khóa và báo rõ. Không tự gửi lại thao tác ghi khi đối chiếu.
- Lỗi thường không biến thành dòng khóa; chỉ trạng thái chưa xác định mới dùng cơ chế
  này. Trạng thái đang ghi chặn sửa/xóa trên lưới cho tới khi có kết quả.

### 11.5.5. Đối chiếu với tài liệu khác

- `WINFORMS_IMPLEMENTATION.md` mục2.2 ghi control/API: `OrbitRowEditor` mở hộp sửa,
  `OrbitCellEditor` là ô nhập dùng chung, hook `InlineEditable`/`InlineCursor`/
  `RowBlocked`, cột `Delete`.
- Hồ sơ triển khai và kết quả kiểm thực tế ở `docs/tasks/row-edit-dialog/`.
  Việc ghi quy tắc này không chứng nhận hình thức hay chức năng đã PASS; người dùng
  tự kiểm hình thức, agent kiểm chức năng theo đợt test được duyệt.
