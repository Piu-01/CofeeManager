# G04 — Hướng dẫn phối hợp kiểm 24 ca GUI còn thiếu

Đây là hướng dẫn kiểm chức năng trên app WinForms thật, không phải biên bản PASS.
Đợt test đã được duyệt. Agent giữ source/schema016 cố định, chuẩn bị fixture,
đối chiếu SQL và ghi kết quả. Người dùng thao tác GUI và mô tả actual.
Không cần người dùng chạy SQL, chụp ảnh hoặc kiểm màu/font/spacing.

## Mở đúng bản test

Agent đã chuẩn bị DB riêng theo [manifest](runs/manual-g04-20261024/manifest.json).
Không dùng F5 profile Development cho hướng dẫn này. Profile bypass Test đã gỡ
theo yêu cầu người dùng ngày24/10/2026; tất cả vai trò phải đăng nhập thường.
Fixture G04 dùng schema016; SP001 có giá **81.000**, có công thức baseline.
Giá này chỉ phục vụ ca T16-K03; không thay giá trong DB development.
Tài khoản admin/thungan/kho kế thừa hash của tài khoản development tương ứng,
không đổi mật khẩu. Đăng nhập vai trò bằng mật khẩu bạn đang dùng, không gửi mật khẩu vào chat.

Tại terminal ở gốc CoffeeManager, mở app nối DB test:

```powershell
& .\docs\tasks\g04-ban-hang-vong-doi-hoa-don\runs\manual-g04-20261024\launch.ps1
```

Trên Login đăng nhập **thungan** cho luồng bán hàng/giao hàng, **admin** cho quyền quản lý,
**kho** cho ca quyền STORE. Dùng mật khẩu tài khoản tương ứng bạn đang có.
Để mở cửa sổ thứ hai, chạy lại cùng lệnh.
Launcher chỉ đổi catalog của process con; thiếu fixture sẽ dừng, không rơi về development.

Không tự đổi Windows Scale. Phạm vi kiểm kỹ thuật ở125%; agent xác minh môi trường.
Hình thức SKIPPED theo phân công;100/150% chưa chạy không suy diễn PASS.

## Cách gửi kết quả

Mỗi lượt ghi lại **mã hóa đơn**, thao tác thực tế, số tiền/trạng thái thấy được và
nội dung lỗi nếu có. Ví dụ: `Lượt1: mã ...; thêm/sửa/xóa đúng; mở lại còn ghi chú ...`.
Không cần ghi “PASS” trước; agent đối chiếu DB rồi cập nhật từng ID.
Nếu khác expected, giữ mã đơn/input, ghi actual và tiếp tục ca độc lập;
không cố thao tác lại nhiều lần để biến lỗi thành PASS.

## Lượt 1 — Đơn và dòng món

Lưu ý nút **Đơn mới** không mở popup và chưa cấp mã hóa đơn: nó chỉ bỏ ngữ cảnh
đơn đang xem, đưa POS về nháp trống. Khi POS vốn đã trống, bấm sẽ gần như không
thấy thay đổi. Mã đơn chỉ xuất hiện sau khi thêm và lưu món đầu tiên thành công.
Đây là luồng hiện có trong NewOrder/AddProduct, không lấy việc bấm nút này làm
bằng chứng đã tạo đơn. Nếu thêm món đầu vẫn không mở dialog/không lưu, ghi actual
để điều tra handler, không tự suy PASS.

1. Vào POS, **Đơn mới → Tại quán**, chọn bàn đang trống → **Chọn bàn**.
   Thêm SP001; khi dialog hỏi ghi chú nhập `G04 ít đá, không đường`.
   Ghi mã đơn xuất hiện. Chỉ chọn bàn chưa tạo đơn; món đầu tiên mới lưu đơn.
2. Thêm một món khác. Chọn đúng dòng SP001 → **Sửa dòng**, đổi số lượng thành2,
   sửa ghi chú. Chọn dòng món thứ hai → **Xóa dòng**, xác nhận.
   Expected: chỉ dòng đã chọn đổi/xóa, tổng và ưu đãi tải lại đúng.
3. **F3 · Khách / Ưu đãi**: chọn khách thử nếu có, lưu và ghi lại mã khách.
   Rời POS rồi quay lại; **F4 · Đơn mở** mở đúng mã. Đóng app, mở lại bằng launcher,
   mở lại mã này. Expected: bàn, khách, số lượng và ghi chú đã lưu còn nguyên.
4. Lặp thao tác tạo đơn cho **Mang đi** và **Giao hàng**. Với giao hàng điền tên
   `Khách test G04`, SDT `0900000004`, địa chỉ `Địa chỉ test G04`, ship10000.
   Thử để trống từng trường bắt buộc trước khi nhập hợp lệ: phải báo gần trường,
   không lưu đơn giao thiếu thông tin. Ghi cả ba mã để mở lại sau restart.

Phủ T16-K01/K02 và phần người nhận của T18-K01.

## Lượt 2 — Thanh toán, validation và hủy

1. Tạo **Mang đi** mới, không khách, không KM, ship0, một SP001 số lượng1.
   **F8 · Thanh toán** chỉ mở dialog, chưa chốt. Expected: phải trả81.000.
2. Dòng phương thức chọn tiền mặt, **Số thu (VND)=81000**.
   **Tiền mặt khách đưa=100000** → expected thối19.000.
   Phân biệt số thu81.000 và tiền đưa100.000; không sửa số thu thành100.000.
3. Trước khi chốt, lần lượt thử: tiền đưa80000; nhập `abc`; số cực lớn
   `999999999999999999999999999999`; phương thức trống; số thu80000.
   Mỗi lần thử xác nhận phải bị chặn/thông báo hợp lệ, không có thanh toán dở.
   Bấm **Quay lại**, xác nhận bỏ phân bổ; mở lại đơn phải vẫn chưa thanh toán.
4. Nhập lại giá trị hợp lệ bước2. Thử Enter khi đang nhập ô: không tự chốt tiền.
   Bấm **Xác nhận thanh toán**, đọc popup và chọn OK. Trong lúc đang gửi,
   thử bấm lặp: không có lần chốt thứ hai. Sau thành công phải hiện thu81.000,
   thối19.000 và trạng thái đã thanh toán. Gửi mã cho agent kiểm payment/stock/points.
5. Mở đơn tại quán chưa trả của lượt1 → **Hủy đơn mở**, xác nhận.
   Expected: đơn đã hủy, bàn về trống sau refresh. Đơn đã trả bước4 không cho
   sửa món/tiền/hủy, không hoàn kho. Tạo nháp chưa thêm món: không thanh toán được.
6. Agent chuẩn bị riêng hai mã thiếu công thức/thiếu kho trong DB test rồi cung cấp.
   Bạn mở từng mã → F8. Expected: nói rõ lý do, giữ đơn mở và input để sửa.
   Không tự sửa tồn hoặc công thức để vượt lỗi.
7. Sau checkout, **Làm mới**, mở màn bàn/khách/tồn có quyền và ghi giá trị thấy được.
   Agent đối chiếu số dư chính thức; refresh lại không được trừ kho/cộng điểm thêm.

Phủ T16-K03/K04/K05/K08/K09/K12 và phần khóa tài chính T17-K08.

## Lượt 3 — Hai cửa sổ và kết quả chưa rõ (phối hợp agent)

1. Agent mở thêm cửa sổ vào cùng DB test. Hai cửa sổ mở cùng một đơn chưa trả.
   Giữ cửa sổ A chưa refresh; cửa sổ B thanh toán. Tại A thử sửa/thanh toán,
   rồi tải lại. Expected: nhận trạng thái mới/xung đột, không thu hoặc sửa thêm.
2. Agent chuẩn bị fault/timeout có kiểm soát cho đúng request test, báo thời điểm.
   Bạn gửi thanh toán đúng một lần, ghi thông báo và mã đơn.
   Khi chưa rõ kết quả, app phải khóa gửi lại và cho **Kiểm tra lại**/
   **Kiểm tra kết quả**; sau khôi phục phải đọc đúng đơn, hiện đã chốt hoặc
   hướng dẫn kiểm tra tiếp. Không tự tạo đơn mới để thu lại.
   Agent kiểm receipt, số lần thu và transaction trước khi kết luận.

Không tự dừng SQL Server, rút mạng, sửa procedure hoặc thay cấu hình toàn máy.
Nếu fault chưa chuẩn bị được, riêng nhánh đó giữ chưa chạy; các lượt khác tiếp tục.
Phủ T16-K06/K07.

## Lượt 4 — Hóa đơn và in lại

1. Mở hóa đơn đã trả81.000 của lượt2 → **Xem / In → Xem trước / In lại**.
   Đọc tiền dòng trước giảm, tổng món, giảm, ship, phải trả và phương thức.
   Expected: phải trả/thu81.000, không lấy100.000 tiền khách đưa làm doanh thu.
2. Đóng/mở lại preview. Có nhãn in lại; nếu DB không lưu tiền đưa/thối thì bản
   in lại không bịa100.000/19.000. Agent kiểm snapshot tương ứng.
3. Agent cung cấp thêm hóa đơn nhiều dòng, ghi chú tiếng Việt, có giảm/ship.
   Đối chiếu số dòng/nội dung đầu-cuối các trang và tổng cuối với danh sách fixture.
   Chỉ kiểm không mất nội dung nghiệp vụ; không nghiệm thu hình thức bản in.
4. Bấm **In… → Cancel**. Mở lại/in lại nhiều lần.
   Nếu có **Microsoft Print to PDF**, có thể dùng để kiểm luồng xuất thay máy in giấy;
   chọn file test mới. Nếu hộp lưu báo ghi đè, chọn tên mới.
   Gửi tên file/kết quả; agent kiểm DB trước/sau không phát sinh thanh toán/đơn mới,
   không đổi kho/điểm. Không coi Cancel là đã kiểm được nhánh lỗi máy in.
5. Nhánh lỗi máy in do agent chuẩn bị riêng; bạn ghi thông báo và khả năng mở lại.
   Expected: báo chưa in được, hóa đơn đã thanh toán giữ nguyên, chỉ in lại.
6. Kiểm hóa đơn đã chốt không có nút sửa tài chính. Nhánh đơn không tồn tại:
   agent chuẩn bị danh sách cũ trỏ tới mã fixture không còn tồn tại, bạn bấm chi tiết;
   phải báo không tồn tại/tải lại, app không crash. Không tự xóa hóa đơn thật.

Phủ T17-K03–K09. In thật/PDF, lỗi máy in và mã không tồn tại cần bằng chứng riêng.

## Lượt 5 — Giao hàng

1. Mở **Giao hàng**, tìm mã giao của lượt1 và SDT0900000004; thử bộ lọc tiến độ,
   trạng thái tiền → **Áp dụng**. Đơn tại quán/mang đi không lọt vào danh sách giao.
2. Chọn dòng → **Chi tiết / Bước tiếp**. Đọc lại người nhận/SDT/địa chỉ/ship.
   Chuyển Chờ giao → Đang giao → Đã giao theo các popup xác nhận;
   sau từng bước tải lại, đóng/mở app rồi lọc lại, dữ liệu phải còn đúng.
3. Tạo thêm đơn giao chưa trả và hủy ở POS. Expected: đơn hủy/tiến độ hủy giao.
   Với đơn giao đã trả, không có thao tác hủy để tự hoàn tiền hoặc hoàn kho.
4. Mở chi tiết đơn giao đã trả, đọc hướng dẫn khi giao thất bại:
   phải yêu cầu liên hệ quản lý xử lý thủ công và nói app không tự hoàn tiền.
   Không cần giả lập giao thất bại bằng cách sửa trạng thái ngoài contract.

Phủ T18-K01/K04/K07/K08. Agent đối chiếu trạng thái và tài chính sau thao tác.

## Lượt 6 — Quyền, phím và cửa sổ

1. Mở launcher, đăng nhập **thungan** bằng mật khẩu hiện có.
   Kiểm CASH vào POS/Giao hàng, thao tác chuyển giao của một đơn fixture do agent
   cung cấp. ADMIN kiểm tương tự bằng đăng nhập **admin**. Đăng xuất rồi vào **kho**:
   STORE không được mở/thao tác bán hoặc chuyển giao trái quyền.
2. Trong thanh toán không chọn được PT010/đổi điểm. Agent đánh dấu một món fixture
   ngừng bán rồi báo mã: tìm lại POS không được thêm món đó như đang bán.
3. F2 vào tìm món, F3 mở khách/ưu đãi, F4 mở đơn mở, F8 chỉ mở thanh toán.
   Khi dialog/loading đang mở, phím không xuyên xuống POS tạo giao dịch khác.
   Thử Tab/Shift+Tab và Enter trong ô nhập; Enter không tự chốt nghiệp vụ.
4. Ở Scale125% hiện tại, thu nhỏ/phóng to cửa sổ, tìm món, cuộn nhiều dòng/bàn
   fixture do agent chuẩn bị. Có thể dùng tab Món/Đơn khi hẹp.
   Expected: vẫn truy cập được thao tác chính, đúng dòng đang chọn, app còn đáp ứng.
   Đây là kiểm khả năng thao tác/layout kỹ thuật, không kiểm đẹp/xấu/hover/spacing.

Phủ T16-K10/K11, phần bàn phím T16-K05 và T18-K06.

## Ghi nhận và dọn fixture

Agent điền actual/mã đơn/thời điểm/query đối chiếu vào test-results.md của T16–T18,
giữ nguyên ID và lịch sử. Một lượt có thể mới phủ một phần ID, không nâng PASS
khi còn nhánh chưa chạy. Hiện trạng trước phối hợp:32PASS/0FAIL/24BLOCKED.

Fixture được giữ cho phiên kiểm này, xem lại ngày25/10/2026; không có lịch tự xóa.
Sau khi kết thúc và đóng các cửa sổ test, agent chạy `runs/manual-g04-20261024/cleanup.ps1`.
Script xác minh đúng tên/marker, không xóa development và không tự dừng process người dùng.
