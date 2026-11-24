# Requirements — Mỗi món chỉ một khuyến mãi, chọn tổ hợp giảm nhiều nhất

- Ngày: 23/11/2026
- Người giao: người dùng
- Trạng thái: **requirements chờ người dùng duyệt** (chưa viết plan, chưa code)
- Liên quan: thay đổi cơ chế cộng dồn của `promotion-dieu-kien-tu-ap` (039) và
  `promotion-v2` (042). Không đổi UI quản lý/editor khuyến mãi.

## 1. Mục tiêu

Sửa cơ chế tự áp khuyến mãi giảm giá: một hóa đơn có thể nhận **nhiều** khuyến mãi,
nhưng **mỗi dòng món chỉ được hưởng tối đa một khuyến mãi**. Khi các khuyến mãi chồng
lên cùng món, hệ thống chọn tổ hợp khuyến mãi rời nhau cho **tổng giảm của hóa đơn lớn
nhất**, thay vì cộng dồn phần trăm của mọi khuyến mãi như hiện tại.

Ví dụ người dùng: đơn có món a, b, c; KM1 giảm a; KM2 giảm b; KM3 giảm b và c. Đơn chỉ
được nhận (KM1 + KM2) hoặc (KM1 + KM3), không bao giờ KM2 + KM3 cùng lúc vì b chỉ được
một khuyến mãi; chọn bên có tổng giảm lớn hơn.

## 2. Quy tắc nghiệp vụ

| Chủ đề | Quy tắc |
|---|---|
| Ứng viên | Khuyến mãi loại `Giam` đang `DangChay`, đúng ngày (quy ước 042), đúng đối tượng khách; `PhanTramGiam > 0`. Khuyến mãi `Tang` không tham gia chọn tổ hợp (xem mục quà). |
| Phủ dòng | `PhamVi='ALL'` phủ **mọi dòng món** của hóa đơn; `PhamVi='ITEM'` phủ các dòng có `MaSP` nằm trong `KHUYENMAI_MON`. Nhiều dòng cùng một `MaSP` cùng thuộc phạm vi của khuyến mãi đó. |
| Một món một KM | Tập khuyến mãi được áp phải có các tập dòng phủ **rời nhau**: không dòng nào bị hai khuyến mãi cùng áp. Vì `ALL` phủ mọi dòng nên nếu chọn một KM `ALL` thì không chọn KM nào khác trên hóa đơn đó. |
| Chọn tối ưu | Chọn tập khuyến mãi rời nhau sao cho **tổng giảm toàn hóa đơn lớn nhất**. Vì hạng tính trên phần còn lại và cùng một mức hạng cho mọi phương án, xếp hạng phương án theo `PromotionDiscount` là tương đương (sai lệch tối đa do làm tròn phải được xử lý nhất quán). Không có ưu tiên "KM món đè KM toàn bộ". |
| Giảm mỗi dòng | KM được chọn giảm **trước**: số tiền dòng × KM%/100, làm tròn 0 chữ số thập phân như 039; dòng không có KM không bị giảm ở bước này. Ship không giảm. |
| Hạng thành viên | **Khác 039/038**: % hạng tính trên **hóa đơn sau khi đã trừ khuyến mãi**, không cộng dồn % trên giá gốc và không còn công thức `min(100, Hạng%+KM%)`. `TierDiscount = round((TongTien − PromotionDiscount) × Hạng%)`, làm tròn một lần trên tổng; không giảm ship. Luật cộng dồn additive của 038 bị thay bởi quy tắc này. Hạng không phải ứng viên xung đột. |
| Phân bổ hiển thị | `Discount = TierDiscount + PromotionDiscount` như contract 039; phần khuyến mãi chia cho từng KM theo tỷ lệ phần giảm thô của các dòng nó phủ, tổng chia đúng bằng `PromotionDiscount` (phần lẻ dồn KM cuối theo thứ tự mã). `BaseAmount` của mỗi KM = tổng tiền các dòng nó phủ. `PercentDiscount` hiển thị phải phản ánh mức giảm thực tế sau hai bước (ràng buộc snapshot cũ đang ép công thức cộng—sẽ xử lý ở plan/migration). |
| Hòa tổng | Nếu nhiều tổ hợp cùng tổng giảm lớn nhất: chọn tổ hợp **ít khuyến mãi hơn**, rồi theo thứ tự mã KM tăng dần (so sánh chuỗi) để kết quả tất định. |
| Quà tặng | Không đổi: KM `Tang` áp một lần/hóa đơn, độc lập với tổ hợp giảm, không xung đột, vẫn thêm dòng 0 ₫, trừ kho, nhãn "Tặng". |
| Đơn đã chốt | Không backfill: hóa đơn đã thanh toán giữ nguyên `HoaDonUuDaiSnapshot`, `HoaDonUuDaiApDung`, `GiamGia`, dòng quà và hóa đơn in. Đơn đang mở tính lại theo luật mới khi đọc/preview. |
| Preview token | Token phải đổi khi tập khuyến mãi được chọn hoặc số tiền giảm thay đổi (digest đã có sẵn phải phản ánh danh sách mới); checkout với token cũ vẫn báo 52201. |
| Quyền | Không đổi: ADMIN quản lý KM; CASH nhận KM tự áp. |

## 3. Phạm vi kỹ thuật dự kiến

- **SQL (migration 044, chạy sau 043)** — không đổi cấu trúc bảng, chỉ thay hàm/procedure:
  - Thêm hàm chọn tổ hợp (dự kiến `fn_UuDaiChon` hoặc tương đương) nhận ngày, khách,
    KM legacy và danh sách dòng; trả về các cặp (dòng, MaKM) của tổ hợp tối ưu.
  - Sửa `fn_UuDaiCap` (chỉ trả cặp của tổ hợp được chọn), `fn_UuDaiDong`, `fn_UuDaiTong`,
    `fn_UuDaiChia`, `fn_UuDaiApDung`, `fn_HoaDonGiamGia`, `fn_HoaDonUuDaiApDung`,
    `fn_HoaDonUuDaiDigest` nếu cần; giữ nguyên chữ ký/contract trả về.
  - Rà lại `fn_CheckoutPreview` (token/digest), `sp_SalesReadCore`, `sp_InvoiceDetail`,
    `sp_ThanhToanHoaDon`, `sp_DiscountPreview`, `sp_SalesCheckoutPreview` để dùng kết quả
    mới; snapshot và trigger kiểm tổng vẫn phải khớp.
  - Ràng buộc `CK_DiscountSnapshot_Known` của 039 đang buộc
    `PercentDiscount = min(100, TierPercent+PromotionPercent)`; phải thay bằng công thức
    tuần tự cho dòng mới, vẫn giữ dòng snapshot cũ hợp lệ (không backfill).
- **C#**: không đổi công thức (số liệu do SQL trả); rà BLL/DAL/UI chỉ để đảm bảo danh
  sách KM áp dụng, số tiền, token, nhãn hiển thị vẫn đọc đúng contract. Không thêm màn
  chọn tay khuyến mãi.
- **Kiểm thử**: bộ `database/tests/promo-auto` và `database/tests/promo-v2` hiện mã hóa
  luật cộng dồn 039/042 sẽ FAIL có chủ đích; cập nhật kỳ vọng theo luật mới và thêm ca
  cho ví dụ a/b/c, ALL vs món, nhiều dòng cùng món, hòa tổng, quà, snapshot, token,
  performance. Giữ nguyên lịch sử kết quả cũ, ghi rõ luật thay đổi.
- **Tài liệu**: cập nhật `docs/database/loyalty-contract.md`, `docs/database/migrations.md`,
  mục nghiệp vụ khuyến mãi trong kế hoạch/UX nếu có câu "cộng dồn mọi KM".

## 4. Giả định

1. Tổ hợp được chọn bằng tìm kiếm **chính xác** trên các KM ứng viên; số KM giảm đang
   chạy chồng chéo trên một đơn ở quán nhỏ là ít. Plan sẽ tách theo nhóm xung đột để tính
   nhanh và nêu ngưỡng/chiến lược nếu số KM vượt dự kiến (không được làm chậm POS bất
   thường; nếu vượt ngưỡng sẽ có xử lý rõ ràng, không âm thầm cộng dồn).
2. KM legacy `HOADON.MaKM` của đơn mở được coi như một ứng viên phạm vi `ALL` (tham gia
   chọn tổ hợp), không còn được ép cộng thêm ngoài tổ hợp. Đơn đã chốt không đổi.
3. Làm tròn, ship, điểm tích lũy, phi đối tác giữ nguyên 039/042. Chặn 100% không cần công thức riêng vì giảm tuần tự: KM tối đa 100% thì phần còn lại bằng 0, hạng không vượt quá phần còn lại.
4. Không thêm: chọn KM tay, ưu tiên KM theo thứ tự khai báo, voucher, hoàn tiền, giới hạn
   số KM trên một đơn ngoài ràng buộc "mỗi món một KM".
5. KM `Tang` không tham gia tranh chấp và không làm giảm số KM giảm được chọn.

## 5. Ca nghiệm thu chính (chi tiết ở plan)

- **PP01** Ví dụ a/b/c: KM1(a), KM2(b), KM3(b,c). Kết quả chỉ là một trong hai tổ hợp
  rời nhau có tổng lớn hơn; không bao giờ KM2+KM3; danh sách KM áp và số tiền khớp.
- **PP02** KM `ALL` so với tổ hợp KM món: chọn bên tổng giảm lớn hơn; nếu `ALL` thắng
  thì nó là KM duy nhất; nếu tổ hợp món thắng thì `ALL` không áp dòng nào.
- **PP03** Nhiều dòng cùng `MaSP`: cùng một KM, không tách dòng; KM phủ món đó chỉ áp
  một lần cho mọi dòng của món.
- **PP04** Hạng tính sau khuyến mãi: ví dụ hóa đơn 100.000, KM được chọn giảm
  20.000, hạng 5% → hạng giảm 4.000 (5% của 80.000), tổng giảm 24.000 (không phải
  25.000); `Discount = TierDiscount + PromotionDiscount`, `BaseAmount`/phần chia từng
  KM khớp tổng; ship không giảm.
- **PP05** Hòa tổng: tie-break ít KM hơn rồi mã tăng dần; chạy lặp cho cùng kết quả.
- **PP06** Quà tặng vẫn áp độc lập một lần/hóa đơn, kể cả khi tổ hợp giảm đổi.
- **PP07** Snapshot hóa đơn đã chốt bất biến; hóa đơn mở đổi luật tính lại đúng; token
  đổi khi tổ hợp/tiền đổi và token cũ bị từ chối 52201.
- **PP08** Hiệu năng preview/checkout với số KM thực tế và với bộ KM chồng chéo lớn hơn
  ngưỡng dự kiến; ghi số đo, không chặn bán ngoài dự kiến.

## 6. Tiêu chí hoàn tất

1. Migration 044 áp được trên DB test riêng và dev (sau duyệt), có backup/verify.
2. Mọi điểm tính giảm (preview tạm tính, POS, checkout, chi tiết/in) dùng chung một tổ
   hợp khuyến mãi; không còn cộng dồn hai KM trên cùng dòng.
3. Bộ kiểm thử cũ được cập nhật theo luật mới, có ca PP01–PP07 PASS trên DB test; PP08
   có số đo và kết luận; UI do người dùng tự nghiệm thu theo phân công.
4. Build Debug/Release sạch; tài liệu contract/migration cập nhật; không sửa lịch sử test
   đã chốt của 039/042.
