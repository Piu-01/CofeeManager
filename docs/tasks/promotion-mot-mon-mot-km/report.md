# Báo cáo — một khuyến mãi mỗi dòng món, hạng tính sau khuyến mãi

Yêu cầu: một hóa đơn có thể áp nhiều khuyến mãi, nhưng **mỗi dòng món tối đa một
khuyến mãi giảm**; chọn tổ hợp rời nhau có tổng giảm lớn nhất; khuyến mãi `ALL` tranh
chừng bình thường (không đè); giảm hạng thành viên tính trên hóa đơn **sau** khuyến mãi.
Requirements: `requirements.md`; plan: `plan.md`; bằng chứng test: `test-results.md`.

## Đã làm

- `database/migrations/044_promotion_one_per_item.sql` (chạy sau 043):
  - `fn_UuDaiChon`: dựng ứng viên (Giam, DangChay, % > 0, đúng ngày/đối tượng; legacy
    `HOADON.MaKM` là ứng viên `ALL` chỉ xét ngày), dựng đồ thị xung đột theo dòng, tách
    nhóm xung đột, chọn tổ hợp tối đa tổng giảm theo bitmask khi ≤ 15 ứng viên/nhóm
    (hòa: ít KM hơn rồi mã tăng dần), tham lam tất định + một lượt thay thế khi > 15.
  - `fn_UuDaiCap`, `fn_UuDaiDong` (chỉ phần KM của dòng, không cộng hạng), `fn_UuDaiTong`
    (KM trước, hạng `ROUND((tổng − KM) × %/100)`, `Discount = TierDiscount + PromotionDiscount`),
    `fn_UuDaiChia` giữ cách phân bổ 039.
  - `fn_CheckoutPreview`/`sp_DiscountPreview`: `PercentDiscount` là mức giảm thực tế
    `ROUND(Discount × 100 / TongTien)`; token vẫn gồm revision + số tiền + % nên đổi tổ hợp
    là token đổi.
  - Nới `CK_DiscountSnapshot_Known` (không còn công thức min 100%, chỉ kiểm tổng hợp lệ
    và `Discount = TierDiscount + PromotionDiscount`); không backfill dữ liệu cũ; GRANT
    EXECUTE cho `CafeManagerApp`.
- Bộ test riêng `database/tests/promo-one-per-item/` (fixture + 01–05) và runner
  `tools/database/Invoke-PromoOnePerItem.ps1`: DB test tạo mới, marker, hash source
  trước/sau, manifest + số đo thời gian, drop DB sau đối chiếu marker.
- Script nâng development `tools/database/Update-DevelopmentPromoOnePerItem.ps1`
  (backup COPY_ONLY/CHECKSUM + verify, hash dữ liệu trước/sau, kiểm object/grant/legacy).
- Tài liệu: `docs/database/migrations.md`, `docs/database/loyalty-contract.md` (mục 044).

## Kết quả

- Test DB schema 44, lượt `20261123_221933_99152c8e`: fixture + 01–05 PASS; PP01–PP08 đạt.
  Preview 3 dòng / 17 ứng viên 68 ms; hàm 20 dòng 29 ms.
- `CoffeeManager_Development` 43 → 44 ngày 24/11/2026, backup kiểm chứng, hash dữ liệu
  trước/sau giống nhau, 5 hóa đơn đã trả mới nhất khớp `fn_HoaDonGiamGia`.
- Build Release 0 lỗi. Build Debug chờ đóng app/Visual Studio (DLL bị khóa, không phải lỗi
  biên dịch).

## Chưa làm / còn chờ

- Người dùng tự nghiệm thu giao diện và DPI 100%/150% (theo phân công; agent chỉ kiểm
  chức năng, không ghi PASS hình thức).
- Bộ test lịch sử `promo-auto` (039) và `promo-v2` (042) giữ nguyên theo version; muốn
  chạy lại trên schema 044 thì phải viết lại kỳ vọng số học — tách việc riêng.
- Chưa commit: người dùng chưa giao thao tác Git.
