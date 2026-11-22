# Requirements — Khuyến mãi v2: lặp hằng năm, giảm hoặc tặng

- Ngày: 22/11/2026
- Người giao: người dùng (sau khi duyệt mockup `mockup.html` cùng ngày)
- Trạng thái: **requirements chờ người dùng duyệt** (chưa viết plan, chưa code)
- Liên quan: kế thừa `promotion-dieu-kien-tu-ap` (migration 039, đã áp dev 22/11/2026);
  thay đổi nghiệp vụ/thời gian/trạng thái/loại ưu đãi của 039.

## 1. Mục tiêu

Làm chương trình khuyến mãi dễ khai báo và tái sử dụng:

1. Thời gian chỉ chọn **ngày/tháng**, tự lặp hằng năm, không phải tạo lại mỗi năm.
2. Trạng thái chỉ còn **Áp dụng / Tạm ngưng**; bỏ Nháp, bỏ thao tác Bật/Tạm dừng và
   bỏ nút Preview khỏi màn quản lý.
3. Mỗi chương trình chọn **Giảm giá** hoặc **Tặng món**; chọn một/nhiều món, chọn
   một/nhiều hạng, hoặc khách vãng lai.
4. UI quản lý và editor bố cục lại theo `mockup.html` đã duyệt: một cột nội dung,
   nhóm rõ ràng, món chọn dạng danh sách có tìm kiếm, không nhồi vào form dài.

## 2. Quy tắc nghiệp vụ

| Chủ đề | Quy tắc |
|---|---|
| Thời gian | Chỉ ngày/tháng (`dd/MM`), lặp mọi năm. `NgayBD` bắt buộc; `NgayKT` có thể trống = từ ngày bắt đầu đến hết năm. Khoảng được vắt qua năm mới (vd 20/12 – 10/01). So theo ngày lập đơn, hai đầu inclusive. |
| Lưu trữ ngày | Giữ kiểu `date` của 039 nhưng quy ước năm tham chiếu cố định (BD năm 2000, KT năm 2001 khi vắt năm); nghiệp vụ chỉ đọc tháng/ngày. Migration chuẩn hóa dữ liệu cũ về quy ước này. |
| Trạng thái | Chỉ `DangChay` (Áp dụng) và `TamDung` (Tạm ngưng). `HetHan` vẫn suy ra từ thời gian. Dòng `Nhap` cũ chuyển thành `DangChay`. Đổi trạng thái thực hiện trong hộp sửa, không còn action riêng. |
| Đối tượng | `ALL` (mọi khách, kể cả vãng lai), `TIER` (theo hạng), `WALKIN` (chỉ đơn không có hồ sơ khách). Bỏ `FIRST`/`MEMBER`; dữ liệu cũ chuyển về `ALL` để không mất hiệu lực. |
| Hạng | Khi chọn Theo hạng: chọn một hoặc nhiều hạng. Không chọn hạng nào = áp mọi hạng. |
| Phạm vi món | `ALL` hoặc danh sách món; không chọn món = mọi món. Chọn nhiều món bằng danh sách tick + tìm kiếm. |
| Loại ưu đãi | Mỗi chương trình một loại: `Giam` (giảm %) hoặc `Tang` (tặng món). Không cấu hình đồng thời cả hai trong một chương trình, nhưng nhiều chương trình khác loại có thể cùng áp lên một hóa đơn. |
| Giảm giá | Giảm % (0–100) trên tiền dòng đủ điều kiện; cộng dồn với % hạng và các KM giảm khác, chặn 100%/dòng như 039; không giảm ship; làm tròn một lần/dòng. |
| Tặng món | Chọn một/nhiều món tặng, mỗi món có số lượng ≥ 1. Khi hóa đơn mở thỏa đối tượng, mỗi chương trình tặng áp **một lần/hóa đơn**: thêm các dòng quà giá 0 ₫, không cộng điểm, vẫn trừ kho theo công thức. Quà xuất hiện trong preview/thanh toán/hóa đơn/in với nhãn “Tặng”. |
| Điều kiện tặng | Theo mockup đã duyệt: chỉ xét đối tượng khách (và thời gian/trạng thái); **không** yêu cầu món mua tối thiểu. Xem mục 6 — nếu cần điều kiện “mua món X mới tặng”, phải bổ sung trước khi duyệt. |
| Nhiều ưu đãi | Mọi chương trình thỏa đều áp; KM giảm xếp theo thứ tự hiện có (039), quà tặng độc lập với tiền giảm. Token preview đổi khi KM/quà/thành phần đổi. |
| Đơn đã chốt | Không backfill; hóa đơn cũ giữ nguyên tiền/snapshot/in. Quà đã ghi không sửa/xóa như dòng dịch vụ khác. |
| Legacy | Đơn mở còn `MaKM` cũ vẫn đọc như 039; không chuyển thành KM v2. |
| Quyền | Chỉ ADMIN thêm/sửa/xóa khuyến mãi như hiện tại; CASH vẫn xem danh sách và nhận ưu đãi tự áp. |

## 3. Phạm vi UI

### 3.1 Danh sách (tab Khuyến mãi)

- Cột: Mã, Chương trình, Ưu đãi (badge Giảm x% / Tặng n món), Thời gian
  (`dd/MM – dd/MM`, chú thích “Lặp hằng năm”), Đối tượng, Trạng thái.
- Thanh hành động chỉ còn **Thêm**, **Sửa**, **Xóa** (bỏ Bật/Tạm dừng, bỏ Preview).
- Trạng thái hiển thị Áp dụng/Tạm ngưng/Hết hạn với chữ + màu như quy ước.

### 3.2 Editor (theo mockup đã duyệt)

1. **Thông tin chung**: Tên chương trình; Trạng thái (Áp dụng / Tạm ngưng).
2. **Thời gian áp dụng**: Từ ngày, Đến ngày dạng `dd/MM`; checkbox “Không đặt ngày
   kết thúc”; ghi chú lặp hằng năm và khoảng vắt năm.
3. **Nội dung ưu đãi**: segmented Giảm giá / Tặng món.
   - Giảm giá: ô %, danh sách món được giảm (tick + tìm kiếm), tóm tắt số món đang
     chọn và quy ước bỏ trống = mọi món.
   - Tặng món: danh sách món tặng (tick + tìm kiếm + số lượng), tóm tắt số món/phần.
4. **Đối tượng khách**: 3 thẻ Tất cả khách / Theo hạng / Khách vãng lai; chọn Theo
   hạng mới hiện chip hạng + “Tất cả hạng”.
5. Nút **Hủy**, **Lưu khuyến mãi**; giữ cơ chế chống gửi lặp/đối chiếu kết quả chưa
   biết của `OperationEditorDialog`.

## 4. Dữ liệu và contract dự kiến (migration 042)

- `KHUYENMAI`: thêm `LoaiUuDai` (`Giam/Tang`); nới CHECK trạng thái còn
  `DangChay/TamDung`; nới CHECK đối tượng còn `ALL/TIER/WALKIN`; chuẩn hóa
  `NgayBD/NgayKT` theo năm tham chiếu; dữ liệu `Nhap→DangChay`, `FIRST/MEMBER→ALL`.
- Bảng mới `KHUYENMAI_MON_TANG(MaKM, MaSP, SoLuong)` cho quà tặng.
- Bảng/ cột snapshot quà trên hóa đơn (dự kiến `HoaDonQuaTangApDung` hoặc cột đánh
  dấu trên dòng hóa đơn) để hóa đơn đã chốt đọc/in lại ổn định; chi tiết chốt ở plan.
- Cập nhật hàm/procedure: `fn_UuDaiCap`, `fn_HoaDonUuDaiApDung`, `fn_CheckoutPreview`,
  `sp_CheckoutPreview`, `sp_ThanhToanHoaDon`, `sp_PromotionList/Get/Write/Create/
  Update/Delete`, `sp_SalesReadCore`, `sp_InvoiceDetail`, `sp_InvoiceList` (nếu cần
  hiển thị quà).
- C#: Models (enum/row/request mới), DAL, BLL (normalize/validate), WinForms
  (`LoyaltyControl` editor/list), hiển thị quà ở POS/checkout/chi tiết/in.
- Không sửa schema 001–041 ngoài migration 042; 039 giữ nguyên lịch sử.

## 5. Ca nghiệm thu chính (dự kiến, chi tiết ở plan)

- PM01 Lưu KM chỉ ngày/tháng; lặp đúng mùa năm sau; khoảng vắt năm; KT trống.
- PM02 Trạng thái chỉ Áp dụng/Tạm ngưng; KM Nháp cũ được chuyển; không còn action
  Bật/Tạm dừng/Preview trên UI.
- PM03 Đối tượng ALL/TIER/WALKIN; chọn nhiều hạng; không chọn hạng = mọi hạng;
  FIRST/MEMBER cũ chuyển ALL và vẫn áp.
- PM04 Giảm % theo một/nhiều món; bỏ trống = mọi món; trần 100%; ship không giảm.
- PM05 Tặng một/nhiều món, số lượng; mỗi hóa đơn một lần; quà 0 ₫; trừ kho đúng;
  không cộng điểm cho quà.
- PM06 Nhiều KM giảm + quà cùng áp; token preview đổi khi sửa/tạm ngưng quà.
- PM07 Snapshot quà và giảm bất biến; in/chi tiết hiển thị nhãn Tặng; hóa đơn cũ
  không đổi.
- PM08 UI: editor theo mockup, tìm món, đếm món, DPI 125% (hình thức người dùng tự
  nghiệm thu).

## 6. Giả định cần xác nhận cùng requirements

1. **Quà không có điều kiện món mua**: đúng mockup đã duyệt — chỉ cần đúng đối tượng,
   thời gian, trạng thái là hóa đơn được tặng. Nếu muốn “mua món X mới tặng”, cần
   thêm danh sách món điều kiện cho loại Tặng (bổ sung UI + DB).
2. Quà áp **một lần/hóa đơn** cho mỗi chương trình; nhiều chương trình tặng cùng lúc
   thì cộng các quà.
3. Không giới hạn số lượng quà toàn cục, không có tồn kho riêng cho quà; hết nguyên
   liệu thì checkout báo lỗi như món bán bình thường.
4. Bỏ `FIRST/MEMBER` là thay đổi nghiệp vụ có chủ đích; KM cũ hai loại này chuyển
   `ALL`, ghi rõ trong migration/tài liệu.
5. Không làm khung giờ happy hour, lịch theo thứ, voucher, đổi điểm, hoàn tiền.

## 7. Tiêu chí hoàn tất

1. Migration 042 áp được trên DB test riêng và dev (sau duyệt), có backup/verify.
2. Tạo/sửa KM v2 đúng UI đã duyệt; danh sách hiển thị đủ loại/thời gian/đối tượng.
3. Tính tiền giảm giữ đúng bất biến 039; quà 0 ₫, trừ kho, in/snapshot đầy đủ.
4. Ca PM01–PM07 PASS trên DB test; PM08 do người dùng nghiệm thu hình thức.
5. Build Debug/Release sạch; hồ sơ task, contract, migration cập nhật; không sửa
   lịch sử test 039.
