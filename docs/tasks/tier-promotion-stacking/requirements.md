# Requirements — Cộng dồn ưu đãi hạng khách hàng + 1 khuyến mãi mỗi hóa đơn

- Ngày: 21/11/2026
- Trạng thái: requirements đã được người dùng duyệt 21/11/2026 (cộng thẳng %, làm tròn một lần); đã triển khai migration 038 và C#;
  đợt test 21/11 có 1 bug tại `sp_DiscountPreview` đã sửa, regression **35/35 PASS** trên DB test
  `CoffeeManager_Test_038_accept8`; migration 038 đã áp lên `CoffeeManager_Development` (có backup)
  ([test-results.md](test-results.md), [bug-report.md](bug-report.md)).
  Còn chờ người dùng nghiệm thu và quyết định áp 038 lên `CoffeeManager_Development` (đang version 37).
- Liên quan: thay quy tắc "KM thay ưu đãi hạng" hiện hành; chính sách hạng 4 hạng/trần 8% đã áp ở migration 037 không đổi.

## 1. Mục tiêu

Hiện mỗi hóa đơn chỉ dùng **một** nguồn giảm giá: nếu chọn khuyến mãi thì KM thay ưu đãi hạng, nếu không chọn KM mới dùng ưu đãi hạng. Người dùng muốn đổi sang **cộng dồn**: khách có hạng được hưởng ưu đãi hạng, và nếu đơn có chọn khuyến mãi thì cộng thêm khuyến mãi đó — vẫn **tối đa một chương trình khuyến mãi cho một hóa đơn** như trước.

Kết quả mong đợi: preview, thanh toán, snapshot và hóa đơn in đều phản ánh đúng phần giảm gồm hạng + KM, có tách riêng từng tỷ lệ để đối soát; dữ liệu hóa đơn lịch sử không đổi.

## 2. Quy tắc nghiệp vụ đề xuất

| Tình huống | Cách tính |
|---|---|
| Không khách, không KM | Giảm 0 |
| Có khách (hạng X%), không KM | Giảm X% trên tiền món |
| Không khách, có KM Y% | Giảm Y% trên tiền món |
| Có khách (X%) + KM (Y%) | Giảm **(X + Y)%**, chặn tối đa 100% |
| Nhiều KM cho một đơn | Không cho phép (giữ nguyên: `HOADON.MaKM` một cột, UI chọn một dòng) |

- Phần trăm cộng thẳng rồi làm tròn **một lần**: `Discount = ROUND(TongTien × min(100, X + Y) / 100)`; `Phải trả = TongTien + PhiShip − Discount`.
- Ưu đãi **không áp vào phí ship** (giữ nguyên).
- Điểm mua vẫn tính trên tiền món sau giảm: `FLOOR((TongTien − Discount) / 1000)` (giữ nguyên).
- KM phải còn hiệu lực tại ngày lập đơn (giữ nguyên). Sửa KM/chính sách hạng sau preview phải làm preview cũ mất hiệu lực (giữ nguyên cơ chế revision/token).

## 3. Phạm vi

### Làm

1. **SQL (migration mới 038)**
   - Đổi `fn_DiscountPreview` trả thêm tỷ lệ hạng, tỷ lệ KM, tổng tỷ lệ; `Source` thêm giá trị `TierAndPromotion`.
   - Đổi `fn_CheckoutPreview` và token để phản ánh cả hai tỷ lệ; `sp_SalesCheckoutPreview` trả thêm trường breakdown.
   - Đổi `sp_ThanhToanHoaDon` (bản mới nhất ở 032) ghi snapshot có breakdown.
   - Mở rộng `HoaDonUuDaiSnapshot`: thêm cột tỷ lệ hạng và tỷ lệ KM; `PercentDiscount` tiếp tục là **tổng** tỷ lệ; nới CHECK cho `Source` mới. Không backfill dữ liệu cũ.
   - Cập nhật các procedure đọc hóa đơn phải trả breakdown (`sp_InvoiceDetail`, `sp_SalesOrderGet`/đọc đơn theo bản mới nhất).
   - Báo cáo doanh thu không đổi (vẫn dùng số tiền thực thu); chỉ kiểm tra không vỡ join snapshot.
2. **C#**
   - DTO `DiscountPreview`, `CheckoutPreview`, `InvoiceDetailDto`, `SalesOrderDto` thêm tỷ lệ hạng/KM (giữ `PercentDiscount` là tổng).
   - DataAccess đọc thêm cột mới.
   - UI hiển thị breakdown: preview ở màn Ưu đãi, dòng nguồn giảm ở dialog thanh toán, chi tiết hóa đơn, mẫu in PDF; nhãn nguồn mới `Hạng + khuyến mãi`.
3. **Tài liệu nghiệp vụ**
   - Cập nhật `docs/KeHoach_QuanLiQuanCafe.md` (dòng quy tắc tỷ lệ giảm), `docs/UX_QuanLiQuanCafe.md` (mục ưu đãi/UX-K10), `docs/database/customer-contract.md` nếu có mô tả rule.
   - Không sửa hồ sơ test lịch sử T13/G03; hành vi mới được ghi ở task mới.
4. **Áp dụng DB phát triển**: sau khi code và test xong mới chạy migration 038 trên `CoffeeManager_Development` (đóng app trước).

### Không làm

- Không cho chọn/nhập nhiều mã KM trên một đơn.
- Không đổi ngưỡng/tỷ lệ hạng, không sửa migration 037 hay seed hạng.
- Không đổi công thức tích điểm, không thêm dùng điểm/đổi quà/voucher, không hoàn tiền.
- Không sửa điểm, lịch sử điểm, hóa đơn/snapshot đã phát sinh.
- Không đổi quyền, không đổi luồng F3/F8 ngoài phần hiển thị breakdown.

## 4. Giả định

- "Cộng dồn" = cộng thẳng phần trăm trên tiền món, làm tròn một lần, chặn 100% (không nhân nối tiếp từng bước, không cộng dồn số tiền sau hai lần làm tròn).
- Hóa đơn đã trả và snapshot cũ giữ nguyên cách hiển thị cũ (`Tier`/`Promotion`/`LegacyUnknown`); chỉ hóa đơn tạo sau migration 038 mới có `TierAndPromotion` và breakdown.
- Một đơn vẫn chỉ có tối đa một KM; "1 cái cho 1 đơn" là ràng buộc giữ nguyên.
- Không cần đổi seed vì dữ liệu mẫu là hóa đơn lịch sử.

## 5. Câu hỏi mở

Không có câu hỏi chặn. Nếu bạn muốn cách cộng khác — ví dụ nhân nối tiếp `(1−X%)×(1−Y%)` thay vì cộng thẳng — hãy nói khi duyệt requirements; mặc định sẽ làm theo mục 4.

## 6. Tiêu chí hoàn thành

1. Preview SQL với khách BAC 3% + KM 10%, món 250.000, ship 0 → tổng 13%, giảm 32.500, phải trả 217.500; breakdown hiển thị 3% + 10%.
2. Không KM → chỉ hạng; không khách → chỉ KM; cả hai đều không có → 0.
3. KM 100% + hạng 8% → chặn 100%, phải trả 0 (ship vẫn cộng nếu có).
4. Mỗi đơn chỉ chọn được một KM; thử chọn/gửi hai mã bị chặn ở UI và SQL.
5. Checkout thật trên DB test: `GiamGia`, `ThanhTien`, snapshot (mã hạng, mã KM, tỷ lệ hạng, tỷ lệ KM, tổng %, revision) và điểm mua khớp preview; token đổi khi sửa KM/chính sách sau preview.
6. Hóa đơn đã trả trước 038 (seed `HD20250401`, các hóa đơn cũ) giữ nguyên tiền giảm/snapshot/in; in PDF hóa đơn cũ vẫn hiển thị đúng như cũ.
7. `dotnet build` sạch; test SQL trên DB test riêng; đợt test chính thức chờ duyệt riêng theo `post-change-test-workflow`.
8. Báo cáo doanh thu/đối soát không đổi số tiền thực thu so với trước thay đổi (cùng dữ liệu).
