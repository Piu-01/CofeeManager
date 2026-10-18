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
