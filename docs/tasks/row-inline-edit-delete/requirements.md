# Requirements — Sửa/xóa trực tiếp trên dòng danh sách

Trạng thái: **chờ người dùng duyệt requirements** (chưa viết plan, chưa code).

## 1. Mục tiêu

Đổi logic sửa/xóa ở toàn bộ màn danh sách (master data) để giảm số bước thao tác:

- Bỏ hai nút **Sửa**, **Xóa** trên thanh hành động của mọi màn có hai nút này.
- **Sửa tại chỗ (inline)**: bấm trực tiếp vào ô dữ liệu sửa được để sửa đúng trường đó,
  giữ nguyên phương thức kiểm tra/ghi của màn (cùng service, cùng chuẩn hóa, cùng quyền).
  Không còn bảng sửa cho các trường đơn giản.
- **Xóa tại dòng**: mỗi dòng có icon thùng rác ở cuối; bấm là xóa dòng đó.
  Chỉ các nhóm dữ liệu kho/tài chính/lịch sử mới mở hộp xác nhận; dữ liệu bán hàng
  và dữ liệu tạm xóa ngay để phục vụ khách nhanh.
- Bấm vào vùng trống/chỗ không sửa được của dòng = **chọn dòng** như hiện tại
  (các nút khác như Công thức, Mở sổ điểm, Lịch sử điểm, Nghỉ/Đi làm lại vẫn dùng dòng đang chọn).
- Bố cục thanh hành động co lại, không để khoảng trống của hai nút đã bỏ.

## 2. Phạm vi

### 2.1 Trong phạm vi

Các màn master data: Menu sản phẩm, Danh mục nền, Bàn/Khu vực, Nguyên liệu,
Khách hàng, Nhân viên, Chấm công, Tài sản, Khuyến mãi, Hạng thành viên.

Hạ tầng dùng chung (OrbitRecordListControl, OrbitDataGridView, OrbitListWorkspace)
để mọi màn có cùng cách chọn, sửa, xóa, hover và bố cục.

### 2.2 Ngoài phạm vi (không đổi)

- Chứng từ đã chốt: Nhập hàng, Xuất kho, Kiểm kê, Công nợ NCC, Hóa đơn,
  Giao hàng, Thu/Chi (sổ) — chỉ xem chi tiết, không sửa/xóa.
- Nút **Thêm** và các bảng nhập hiện có vẫn giữ (tạo mới vẫn qua hộp thoại như cũ).
- Nghiệp vụ, SQL, contract dữ liệu, phân quyền, trạng thái thanh toán/kho/điểm:
  không đổi.
- Không thêm hoàn tiền, gộp/tách bill, undo xóa nhiều cấp, audit log mới.

## 3. Quy tắc tương tác thống nhất

### 3.1 Chọn và sửa

| Thao tác | Kết quả |
|---|---|
| Bấm ô **sửa được** | Chọn dòng + mở editor tại ô đó (text/số = ô nhập; danh sách = dropdown; ngày = ô ngày). Chọn hết nội dung khi vào ô theo quy tắc 26/10/2026. |
| Bấm ô **không sửa được** (Mã, Tồn kho, Điểm, Hạng, Tổng tiền…) hoặc vùng trống/row header | Chỉ chọn dòng; không mở editor. |
| Enter / Tab / bấm ô khác | Ghi giá trị ô vừa sửa (gọi đúng service hiện có), rồi mới chuyển ô. |
| Esc | Hủy thay đổi của ô, trả giá trị cũ, giữ dòng đang chọn. |
| F2 / double-click | Vẫn vào chế độ sửa ô như chuẩn lưới (không mở hộp thoại sửa). |
| Lỗi nhập/validate/SQL | Không ghi một phần; trả giá trị hiển thị về dữ liệu DB, báo lỗi gần ô (ErrorText/status) và giữ dòng. Lỗi xung đột phiên bản thì tải lại dòng/trang. |
| Đang ghi | Khóa sửa/xóa toàn lưới cho tới khi xong; chặn gửi lặp; không khóa transaction khi chờ người dùng nhập. |

### 3.2 Hiệu ứng hover

- Ô sửa được: khi rê chuột đổi nền nhẹ (cùng tông hover dòng hiện có) + con trỏ phù hợp
  (IBeam cho chữ/số, Hand cho dropdown/ngày); rời chuột trả lại bình thường.
- Ô không sửa được: không đổi nền, không đổi con trỏ, không gợi ý bấm.
- Dòng đang chọn vẫn giữ màu chọn; hover không ghi đè màu chọn và không đổi giá trị.
- Icon thùng rác: xám, hover đỏ (đang có), có tooltip và AccessibleName "Xóa …".

### 3.3 Xóa

- Icon thùng rác ở cột cuối mỗi dòng, rộng cố định 44px logical, không tiêu đề.
- Bấm icon = xóa đúng dòng đó (không phụ thuộc dòng đang chọn trước đó).
- Có xác nhận (mục 4): mở hộp xác nhận hiện có (modal có owner), nội dung cảnh báo
  giữ như hiện tại; đồng ý mới gọi xóa.
- Không xác nhận: gọi xóa ngay; thành công hiện "Đã xóa …" ở status và tải lại;
  bị chặn (có lịch sử/ràng buộc/hệ thống) thì giữ dòng, báo lỗi rõ ở status.
- Chỉ ADMIN (hoặc ADMIN/STORE ở Nguyên liệu như hiện tại) thấy và dùng được icon;
  BLL vẫn kiểm quyền như cũ.

### 3.4 Bố cục

- Bỏ nút Sửa/Xóa khỏi `RowActions`; các nút còn lại dồn sang phải liền mạch,
  không còn khoảng trống của hai nút đã bỏ (kiểm ở 1280×720, 1366×768 và DPI 125%).
- Nút Thêm giữ ở vùng hành động chính.
- Màn dùng Designer tự dựng (Nguyên liệu, Danh mục) phải chỉnh lại hàng nút tương ứng.

## 4. Ma trận theo màn hình

Cột “Sửa inline” là các trường bấm-sửa-tại-ô. Trường không liệt kê là chỉ đọc/chỉ chọn.

| Màn | Sửa inline | Phần phức tạp giữ hộp thoại | Xóa |
|---|---|---|---|
| Menu sản phẩm | Tên, Danh mục (dropdown), Giá bán, Trạng thái (dropdown) | Nút **Công thức** giữ nguyên (định mức nguyên liệu) | Xóa ngay, **không** xác nhận (bị chặn nếu có lịch sử hóa đơn) |
| Danh mục nền | Tên; SDT (NCC); Chiết khấu (Đối tác); Phụ cấp, Hệ số (Chức vụ); Bắt đầu, Kết thúc (Ca); Loại (Thu/chi); Hỗ trợ thủ công (Phương thức, trừ PT010) | Không | Xác nhận với **Nhà cung cấp** và **Loại thu/chi**; còn lại xóa ngay. Mã hệ thống vẫn bị chặn |
| Bàn / Khu vực | Tên bàn, Khu vực (dropdown); Tên khu vực | Không (đã bỏ nút Sửa/Xóa trước đây) | Xóa ngay, **không** xác nhận (bị chặn nếu còn đơn/lịch sử) |
| Nguyên liệu | Tên, Đơn vị | Tồn đầu kỳ chỉ nhập khi Thêm; Tồn hiện tại chỉ đọc | **Xác nhận** (kho) |
| Khách hàng | Tên, SDT | Mở sổ điểm / Lịch sử điểm giữ nguyên (nút riêng) | **Xác nhận** (điểm/lịch sử) |
| Nhân viên | Tên, SDT, Hợp đồng (dropdown), Lương tháng, Lương giờ, Chức vụ (dropdown) | Nghỉ/Đi làm lại, tài khoản, đối chiếu chờ giữ nguyên | **Xác nhận** (lương/lịch sử) |
| Chấm công | Giờ vào, Giờ ra, Ca (dropdown), Trạng thái (dropdown) | Kỳ đã chốt = chỉ đọc; Thêm vẫn mở bảng nhập nhiều dòng | Không có xóa dòng (giữ như hiện tại) |
| Tài sản | — (giữ hộp thoại hồ sơ) | Bắt buộc giữ `AssetEditorDialog` vì có yêu cầu chờ bền vững + đối chiếu sau timeout | **Xác nhận** trong hộp hồ sơ tài sản (giữ luồng chờ/đối chiếu) |
| Khuyến mãi | Tên, Từ ngày, Đến ngày, Giảm %, Trạng thái, Đối tượng | Danh sách hạng áp dụng + danh sách món giảm giữ trong một nút hành động riêng của dòng (xem câu hỏi mở Q1). Bật/Tạm dừng, Preview giữ nguyên | **Xác nhận** (tài chính) |
| Hạng thành viên | Ngưỡng điểm, Giảm % | Mã hạng là khóa, chỉ đọc khi sửa; preview SQL trước khi ghi giữ nguyên | **Xác nhận** (hạ/đổi hạng khách) |
| Chấm công (dòng) | như trên, Ngày làm **chỉ đọc** (là một phần khóa của dòng công) | | |

Ghi chú quyền: giữ nguyên quyền hiện hành từng màn (Menu ADMIN; Khách hàng ADMIN;
Nguyên liệu ADMIN/STORE; Danh mục theo service hiện tại; …). Thiếu quyền thì ô
không sửa được và không hiện icon xóa.

## 5. Giả định

1. “Bỏ bảng sửa” áp dụng cho các trường đơn giản; cấu trúc con nhiều dòng
   (định mức công thức, hạng/món khuyến mãi, batch chấm công) không thể sửa bằng
   một ô, nên vẫn dùng hộp thoại chuyên dụng qua nút riêng hoặc nút Thêm.
2. Tài sản là ngoại lệ có chủ đích: luồng ghi bền vững (file chờ) và đối chiếu
   không thể hiển thị đầy đủ trong một ô lưới, nên giữ hộp hồ sơ; chỉ bỏ hai nút
   Sửa/Xóa và chuyển sang mở bằng double-click dòng + thùng rác từng dòng.
3. Xóa ngay nghĩa là không có câu hỏi xác nhận; vẫn có báo kết quả, vẫn bị SQL
   chặn khi có lịch sử. Không thêm undo.
4. Mỗi lần ghi inline là một yêu cầu độc lập dùng đúng service/normalize hiện có,
   request id mới, revision hiện tại của dòng; không gộp nhiều ô thành batch
   (trừ Chấm công dùng batch một dòng theo đúng contract hiện có).
5. Không đổi vị trí/ý nghĩa các nút hành động khác (Công thức, Mở sổ điểm, …).
6. Toàn bộ chữ mới đi qua `L.T` và bổ sung `en.json`; tiền/ngày hiển thị theo chuẩn
   `81.000 ₫`, `dd/MM/yyyy`; gửi giá trị typed vào SQL, không gửi chuỗi đã format.
7. Người dùng tự kiểm hình thức; agent chỉ kiểm chức năng/layout kỹ thuật ở DPI 125%.

## 6. Câu hỏi mở (cần chốt trước khi viết plan)

- **Q1 — Khuyến mãi**: chọn một trong hai cách mở phần “hạng áp dụng + món giảm”:
  (a) giữ một nút hành động của dòng, đổi tên từ “Sửa” thành **“Hạng & món”** (khuyến nghị);
  (b) double-click dòng mở hộp cấu hình (bấm vùng trống vẫn chỉ chọn).
- **Q2 — Tài sản**: chấp nhận ngoại lệ giữ hộp hồ sơ như mục 5.2? Nếu không,
  cần thống nhất cách hiển thị trạng thái “chưa rõ kết quả” trên lưới.
- **Q3 — Ngày làm ở Chấm công**: chấp nhận chỉ đọc (đổi ngày = đổi khóa dòng công)?
- **Q4 — Xóa ngay**: có cần hiện thông báo ngắn kèm nút “Tải lại” sau khi xóa
  (không undo), hay chỉ dòng status như hiện tại?

## 7. Tiêu chí hoàn thành

1. Không còn nút Sửa/Xóa trên thanh hành động của 10 màn trong mục 2.1; không còn
   khoảng trống bố cục; nút Thêm và các nút khác vẫn đúng chỗ, đúng quyền.
2. Mỗi trường trong cột “Sửa inline” sửa được bằng một lần bấm; giá trị ghi đúng
   qua service/normalize hiện có; lỗi không làm mất dữ liệu cũ và có thông báo.
3. Mỗi dòng có icon thùng rác đúng quyền; xóa đúng dòng; ma trận xác nhận ở mục 4
   được áp đúng; xóa bị chặn thì dòng còn nguyên và báo lỗi.
4. Bấm vùng trống/ô chỉ đọc chỉ chọn dòng; các nút phụ thuộc dòng vẫn hoạt động.
5. Hover đúng quy tắc 3.2, kể cả khi dòng đang chọn và khi lưới đang tải/khóa.
6. Build Debug/Release sạch; kiểm chức năng trên WinForms thật với DB test riêng,
   ghi `test-results.md`; các ca cũ bị ảnh hưởng được regression.
7. Tài liệu UX/UI liên quan được cập nhật trong phạm vi được duyệt; không đổi
   nghiệp vụ/SQL/contract.
