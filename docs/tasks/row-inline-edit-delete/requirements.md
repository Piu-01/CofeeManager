# Requirements — Sửa/xóa trực tiếp trên dòng danh sách

Trạng thái: **requirements và plan đã được duyệt 22/11/2026** — đang triển khai.

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
Khách hàng, Nhân viên, Tài sản, Hạng thành viên.

Không áp dụng cho **Khuyến mãi** (người dùng đang sửa riêng màn này) và
**Chấm công** (bỏ qua theo yêu cầu, giữ nguyên nút Sửa hiện tại).

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
| Lỗi nhập/validate/SQL | Không ghi một phần; giữ giá trị đang nhập ở editor (hoặc trả giá trị hiển thị về dữ liệu DB nếu đã đóng), báo lỗi ở status và giữ dòng. Lỗi xung đột phiên bản thì tải lại dòng/trang. |
| Kết quả ghi chưa xác định (timeout) | **Chỉ khóa đúng dòng đó** (không sửa/xóa, vẫn chọn/xem được), hiện nút **Đối chiếu** căn giữa dòng và status kèm request id. Các dòng khác vẫn dùng được. Bấm Đối chiếu: có biên nhận → tải lại + báo đã ghi; không có → mở khóa, tải lại, báo chưa ghi. Không tự hoàn giá trị đã nhập trước khi đối chiếu. |
| Đang ghi | Chặn sửa/xóa trên lưới cho tới khi xong; chặn gửi lặp; không khóa transaction khi chờ người dùng nhập. |

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
- Không xác nhận: gọi xóa ngay; thành công hiện "Đã xóa …" ở dòng status và tải lại;
  **không** thêm thông báo/toast phụ hay nút undo;
  bị chặn (có lịch sử/ràng buộc/hệ thống) thì giữ dòng, báo lỗi rõ ở status.
- Chỉ ADMIN (hoặc ADMIN/STORE ở Nguyên liệu như hiện tại) thấy và dùng được icon;
  BLL vẫn kiểm quyền như cũ. Dòng đang chờ đối chiếu không hiện/không dùng icon xóa.
- Dòng chờ đối chiếu hiện nút **Đối chiếu** căn giữa dòng (đè nội dung dòng đó), bấm để
  tra biên nhận theo request id; không thêm nút đối chiếu toàn màn cho các màn này.
- Danh mục nền chưa có `@RequestId` trong procedure Update/Delete (chỉ ProductCategory tạo
  mới có biên nhận). Đối chiếu danh mục dùng `GetAsync` + so `Revision`/giá trị (sửa) hoặc
  còn tồn tại (xóa); ghi rõ giới hạn này, không thêm migration ngoài phạm vi.

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
| Tài sản | Tên, Ngày mua, Nguyên giá, Số tháng KH | `AssetEditorDialog` chỉ còn cho **Thêm**, **Yêu cầu chờ** và **Chi tiết**; ghi inline không ghi file chờ, đối chiếu bằng `ReceiptAsync` + tải lại | **Xác nhận** (tài chính) |
| Hạng thành viên | Ngưỡng điểm, Giảm % | Mã hạng là khóa, chỉ đọc khi sửa; preview SQL trước khi ghi giữ nguyên | **Xác nhận** (hạ/đổi hạng khách) |

Ghi chú quyền: giữ nguyên quyền hiện hành từng màn (Menu ADMIN; Khách hàng ADMIN;
Nguyên liệu ADMIN/STORE; Danh mục theo service hiện tại; …). Thiếu quyền thì ô
không sửa được và không hiện icon xóa.

## 5. Giả định

1. “Bỏ bảng sửa” áp dụng cho các trường đơn giản; cấu trúc con nhiều dòng
   (định mức công thức) không thể sửa bằng một ô, nên vẫn dùng hộp thoại chuyên
   dụng qua nút **Công thức**. Khuyến mãi và Chấm công nằm ngoài phạm vi lần này.
2. Tài sản cũng sửa inline như các màn khác. Ghi inline không ghi `IPendingOperationFile`
   (một file chờ dùng chung sẽ bị đè khi nhiều dòng), thay bằng request id + `ReceiptAsync`
   và khóa đúng dòng khi kết quả chưa xác định. Hộp `AssetEditorDialog` giữ cho Thêm,
   Yêu cầu chờ và Chi tiết; sau restart DB là nguồn đúng, revision cũ bị SQL từ chối.
3. Xóa ngay nghĩa là không có câu hỏi xác nhận; chỉ báo kết quả ở dòng status,
   không thêm thông báo phụ, không undo; vẫn bị SQL chặn khi có lịch sử.
4. Mỗi lần ghi inline là một yêu cầu độc lập dùng đúng service/normalize hiện có,
   request id mới, revision hiện tại của dòng; không gộp nhiều ô thành batch.
5. Không đổi vị trí/ý nghĩa các nút hành động khác (Công thức, Mở sổ điểm, …).
6. Toàn bộ chữ mới đi qua `L.T` và bổ sung `en.json`; tiền/ngày hiển thị theo chuẩn
   `81.000 ₫`, `dd/MM/yyyy`; gửi giá trị typed vào SQL, không gửi chuỗi đã format.
7. Người dùng tự kiểm hình thức; agent chỉ kiểm chức năng/layout kỹ thuật ở DPI 125%.

## 6. Câu hỏi đã chốt

- **Tài sản**: sửa inline như các màn khác (người dùng chốt 22/11/2026); lỗi/timeout
  khóa đúng dòng và hiện nút Đối chiếu giữa dòng. Hộp hồ sơ chỉ còn cho Thêm/Yêu cầu
  chờ/Chi tiết.
- Q2 (Khuyến mãi), Q3 (Chấm công), Q4 (thông báo sau xóa) đã chốt: ngoài phạm vi /
  bỏ qua / chỉ báo ở dòng status.
- Quy tắc tương tác này được ghi thành quy tắc UI chung tại
  `docs/ui/HuongDan_UI_Orbit_Cafe.md` mục 11.5 và `docs/ui/WINFORMS_IMPLEMENTATION.md`
  mục 2.2 để các màn danh sách sau áp dụng, không chỉ riêng 8 màn lần này.

## 7. Tiêu chí hoàn thành

1. Không còn nút Sửa/Xóa trên thanh hành động của 8 màn trong mục 2.1; không còn
   khoảng trống bố cục; nút Thêm và các nút khác vẫn đúng chỗ, đúng quyền.
2. Mỗi trường trong cột “Sửa inline” sửa được bằng một lần bấm; giá trị ghi đúng
   qua service/normalize hiện có; lỗi không làm mất dữ liệu cũ và có thông báo.
3. Mỗi dòng có icon thùng rác đúng quyền; xóa đúng dòng; ma trận xác nhận ở mục 4
   được áp đúng; xóa bị chặn thì dòng còn nguyên và báo lỗi. Khi kết quả ghi chưa xác
   định, chỉ dòng đó khóa và có nút Đối chiếu giữa dòng; các dòng khác vẫn thao tác được.
4. Bấm vùng trống/ô chỉ đọc chỉ chọn dòng; các nút phụ thuộc dòng vẫn hoạt động.
5. Hover đúng quy tắc 3.2, kể cả khi dòng đang chọn và khi lưới đang tải/khóa.
6. Build Debug/Release sạch; kiểm chức năng trên WinForms thật với DB test riêng,
   ghi `test-results.md`; các ca cũ bị ảnh hưởng được regression.
7. Tài liệu UX/UI liên quan được cập nhật trong phạm vi được duyệt; không đổi
   nghiệp vụ/SQL/contract.
