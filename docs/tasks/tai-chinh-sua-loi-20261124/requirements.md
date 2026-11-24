# Requirements — Sửa lỗi và cải thiện luồng Tài chính

Ngày lập: 24/11/2026  
Nguồn: [báo cáo rà soát](../../reviews/ra-soat-tai-chinh-20261124.md)  
Trạng thái: **chờ duyệt requirements**, chưa viết plan, chưa sửa mã nguồn.

## 1. Mục tiêu

Khắc phục các lỗi làm sai số liệu và các điểm khó dùng đã phát hiện ở màn Thu chi và
Công nợ nhà cung cấp; bổ sung truy vết cơ bản (nguồn, người lập) và tách ngày chứng từ
với ngày hạch toán theo hướng đã chọn. Không mở rộng thành sổ quỹ hay hệ thống duyệt.

Kết quả mong đợi: người dùng nhập tiền theo thói quen Việt Nam mà không bị sai 1.000 lần;
phiếu có ngày chứng từ riêng nhưng sổ sách chỉ tính theo ngày hạch toán; danh sách trả
lời được “ai lập, từ nguồn nào” và lọc được theo danh mục; không còn lộ tiếng Anh/mã kỹ
thuật trên màn hình.

## 2. Phạm vi

### 2.1 Nhập tiền (bắt buộc)

- `Ghi phiếu thu chi` và `Trả nợ nhà cung cấp` dùng cùng một cách hiểu số tiền:
  chấp nhận `15000`, `15.000`, `15 000` (phân cách nghìn) và hiểu là **15.000 ₫**;
  từ chối phần thập phân, số âm, số 0 và chuỗi không hợp lệ.
- Không âm thầm hiểu `15.000` là 15. Không đổi giá trị đã nhập khi chỉ focus/blur.
- Hộp xác nhận hiển thị số tiền đã hiểu theo định dạng `15.000 ₫` để người dùng soát.
- Thông báo lỗi nói rõ ví dụ hợp lệ và giữ nguyên nội dung đã nhập.

### 2.2 Tách ngày chứng từ / ngày hạch toán (bắt buộc, cần chốt mô hình)

- Bổ sung **ngày chứng từ** và **ngày hạch toán** cho phiếu thu chi và phiếu trả nợ.
- **Ngày hạch toán** là ngày quyết định số dư, công nợ, báo cáo và thứ tự sổ.
  **Ngày chứng từ** là ngày trên chứng từ giấy, dùng để tra cứu/đối chiếu/in.
- Mọi phép cộng trừ tiền, công nợ hiện tại, công nợ tại ngày và báo cáo chỉ tính theo
  ngày hạch toán; ngày chứng từ không làm thay đổi số dư.
- Hiển thị cả hai ngày ở danh sách (hoặc danh sách + chi tiết nếu thiếu chỗ) và trong
  hộp xác nhận trước khi ghi.
- Dữ liệu cũ được backfill để không thay đổi số dư hiện có.

**Mô hình đề xuất B1 (khuyến nghị):** giữ `NgayLap` hiện tại làm **ngày hạch toán**
(không được sau hôm nay), thêm trường **ngày chứng từ** được phép ở tương lai và chỉ
để hiển thị. Cách này đúng chuẩn “ngày chứng từ khác ngày ghi sổ”, không phải sửa toàn
bộ báo cáo/công nợ và không tạo khoản chi “hẹn giờ”.

**Mô hình B2:** cho phép ngày hạch toán ở tương lai (chứng từ chờ hạch toán). Khi đó phải
thêm trạng thái/lọc “đã hạch toán” cho mọi số dư, công nợ, dashboard, báo cáo và xử lý
xung đột khi một khoản trả nợ đã hẹn trước; khối lượng lớn hơn nhiều. Nếu chọn B2, cần
requirements riêng cho phần chờ hạch toán.

### 2.3 Danh sách Thu chi (bắt buộc)

- Thêm **nhãn** cho Từ ngày, Đến ngày, Loại, Nguồn, Danh mục; đổi bộ lọc thì tự tải lại
  (debounce nhẹ) hoặc có nút “Lọc” rõ ràng, không bắt người dùng nhớ bấm Tìm/Tải lại.
- Thêm cột **Nguồn** (Ghi tay / Trả ngay nhập hàng / Trả nợ nhà cung cấp / Chi lương /
  Dữ liệu cũ) và **Người lập**.
- Thêm bộ lọc **Danh mục**. Danh sách chọn phải gồm cả danh mục hệ thống (lương, trả nợ,
  trả ngay) để lọc được phiếu hệ thống; danh sách chọn khi **ghi phiếu** vẫn loại trừ các
  danh mục này như hiện tại.
- Tìm kiếm mở rộng sang **tên danh mục** và **tên nhân viên**, vẫn giữ tìm theo mã phiếu
  và ghi chú; phân trang và tổng Thu/Chi giữ nguyên hành vi tính trên toàn tập lọc.
- Nguồn/Người lập/Danh mục lấy từ SQL trong cùng truy vấn danh sách, không gọi bổ sung
  từng dòng.

### 2.4 Nhãn màn Thu chi và liên kết Dòng tiền (bắt buộc)

- Đổi tên hiển thị thành **“Phiếu thu chi”** (menu có thể giữ “Thu chi”).
- Phụ đề/trạng thái nói rõ: “Chỉ gồm phiếu thu/chi nội bộ; doanh thu bán hàng xem
  Báo cáo → Dòng tiền quản trị.”
- Ô tổng đổi thành “Thu phiếu · Chi phiếu” để không bị hiểu là toàn bộ doanh thu.
- Thêm liên kết/nút **“Mở Dòng tiền”** mở màn Báo cáo với loại **Dòng tiền quản trị**
  được chọn sẵn, kỳ mặc định 30 ngày gần nhất; không tự in/xuất.
- Không thêm doanh thu bán hàng vào danh sách phiếu, không tạo phiếu thu thứ hai.

### 2.5 Sau khi ghi / trả nợ (bắt buộc)

- Màn chính hiển thị mã phiếu vừa tạo (ví dụ “Đã ghi TC…, danh sách đã tải lại”) và giữ
  thông báo đến lần tải kế tiếp; không để mã chỉ thoáng qua rồi mất khi hộp thoại đóng.
- Có thể mở chi tiết phiếu vừa tạo từ thông báo hoặc chọn sẵn dòng tương ứng sau khi tải.
- Trả nợ hiển thị mã phiếu chi và số tiền đã trả ở màn Công nợ.

### 2.6 Công nợ nhà cung cấp (bắt buộc)

- Lịch sử trả nợ dùng cột tiếng Việt: Mã phiếu, Ngày, Số tiền, Phương thức; ẩn `Id`;
  phương thức hiển thị tên (Tiền mặt/Chuyển khoản) thay vì `PT001`.
- Ô Phương thức có mục “Chọn phương thức” thay vì tự chọn sẵn Tiền mặt; chặn ghi nếu chưa chọn.
- Thêm nút **“Trả hết”** điền đúng số còn nợ hiện tại; vẫn bắt xác nhận trước khi ghi.
- Danh sách công nợ dùng định dạng tiền có `₫`, căn phải, thống nhất với ô tổng.

### 2.7 Hiển thị và lỗi (bắt buộc)

- Cột tiền ở Thu chi và Công nợ hiển thị `15.000 ₫` (không chỉ `15.000`).
- Ghi chú phiếu hệ thống mới ghi bằng tiếng Việt (`Chi lương`, `Trả nợ nhà cung cấp`,
  `Trả ngay nhập hàng`) hoặc để trống và UI hiển thị theo Nguồn.
- Phiếu cũ đang lưu `Payroll`/`SupplierDebt`/`PurchaseImmediate` được **map khi hiển thị**;
  không UPDATE dữ liệu đã ghi.
- Báo cáo Dòng tiền và file CSV hiển thị tên nhóm tiếng Việt cho mọi nguồn, không chỉ
  “Thu bán hàng”.
- Khi tải danh sách lỗi: làm rõ trạng thái lỗi và **không để lưới số cũ** như thể vừa tải;
  tắt thao tác chi tiết/trả nợ cho đến khi tải lại thành công.

### 2.8 Lỗi nhập mã tài sản (nên làm cùng)

- Mã tài sản không tồn tại phải báo đúng trường Mã tài sản và giữ dữ liệu; không dùng
  thông báo chung “Nguồn hoặc trạng thái thu chi không hợp lệ”.

## 3. Không làm trong task này

- Không xây sổ quỹ có số đầu kỳ/cuối kỳ, đối soát két cuối ca/ngày, chốt kỳ.
- Không thêm chứng từ điều chỉnh/đảo có liên kết chứng từ gốc.
- Không thêm phương thức thanh toán/người nhận/chứng từ kèm cho phiếu ghi tay.
- Không in/xuất riêng từng phiếu thu chi.
- Không thêm duyệt/người duyệt, không đổi quyền ADMIN/CASH/STORE.
- Không đổi dãy mã `TC` chung thành mã PT/PC riêng.
- Không cho thu ngân ghi chi vặt ngoài phạm vi quyền hiện tại.
- Không xây “chứng từ chờ hạch toán” nếu chọn B1; nếu chọn B2 thì tách requirements riêng.
- Không sửa dữ liệu chứng từ cũ, không nới bất biến.
- Agent không tự kiểm hình thức/DPI 100/150%; người dùng kiểm giao diện.

## 4. Giả định

- Chỉ ADMIN dùng màn Thu chi/Trả nợ như hiện tại; STORE chỉ xem Công nợ.
- Định dạng tiền là VND nguyên, tối đa 18 chữ số; dấu `.`/khoảng trắng là phân cách nghìn.
- Ngày chứng từ và ngày hạch toán đều bắt buộc; mặc định là hôm nay.
- Liên kết Dòng tiền chỉ điều hướng và chọn sẵn loại báo cáo, không tự tải/không tự in.
- Bộ lọc danh mục là chọn một danh mục hoặc “Tất cả”, không chọn nhiều.
- Ghi chú hệ thống mới do procedure sinh; phiếu ghi tay vẫn giữ ghi chú người dùng nhập.
- Không đổi hành vi bất biến, chống gửi lặp, receipt hay mã yêu cầu.

## 5. Câu hỏi mở cần chốt

1. **Mô hình ngày:** chọn **B1** (ngày hạch toán không ở tương lai; thêm ngày chứng từ
   để hiển thị) hay **B2** (cho phép hạch toán tương lai, cần trạng thái chờ)?
   Đề xuất B1.
2. **Nút Dòng tiền:** mở Báo cáo và chọn sẵn “Dòng tiền quản trị”, kỳ 30 ngày — đúng ý không?
3. **Phạm vi P2:** có làm luôn “Trả hết”, chặn phương thức mặc định, lỗi mã tài sản và
   xóa lưới cũ khi lỗi không? Đề xuất làm cùng vì nhỏ và cùng màn.
4. **Ngày chứng từ trên lưới:** hiển thị cả hai cột ngày, hay chỉ một cột ngày hạch toán
   và xem ngày chứng từ trong chi tiết? Đề xuất cả hai nếu đủ chỗ, thu gọn ở 1280.

## 6. Tiêu chí hoàn thành

- Nhập `15.000`, `15 000`, `15000` ở cả hai hộp thoại đều ghi **15.000 ₫**; `15,5`, `-1`,
  `0`, chữ rác bị chặn với thông báo rõ; có unit test cho bộ chuỗi này.
- Phiếu có hai ngày; sổ/báo cáo/công nợ chỉ đổi theo ngày hạch toán; dữ liệu cũ giữ nguyên
  số dư sau migration; có test SQL trên DB test riêng.
- Danh sách Thu chi có Nguồn, Người lập, lọc danh mục, tìm theo danh mục/nhân viên; đổi
  bộ lọc tự cập nhật; tổng và phân trang không đổi.
- Nhãn “Phiếu thu chi”, phụ đề và ô tổng mới; nút “Mở Dòng tiền” mở đúng báo cáo đã chọn.
- Sau khi ghi/trả nợ, màn chính hiện mã phiếu và có thể mở chi tiết.
- Lịch sử nợ không còn header tiếng Anh/mã `PTxxx`; phương thức không tự chọn; “Trả hết”
  điền đúng số còn nợ.
- Ghi chú/nhóm Dòng tiền tiếng Việt; phiếu cũ không bị sửa; CSV cũng đúng.
- Cột tiền có `₫`; lỗi tải không để lưới cũ; mã tài sản sai báo đúng ô.
- Build Debug/Release 0 lỗi; test đơn vị và SQL theo kế hoạch; không ghi PASS hình thức.

## 7. Kiểm chứng dự kiến (chi tiết ở plan)

- Unit test: parse tiền, chuẩn hóa ngày, mapping nguồn/nhóm dòng tiền, format tiền.
- SQL test trên DB test riêng: backfill, insert có hai ngày, công nợ hiện tại/tại ngày,
  báo cáo Dòng tiền, lọc danh mục/người lập, dữ liệu cũ không đổi.
- Kiểm chức năng UI có kiểm soát (nếu được duyệt): binding cột mới, lọc, thông báo mã phiếu,
  lỗi tải; không tự nghiệm thu hình thức/DPI.
