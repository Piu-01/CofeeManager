# Bug report — `sp_DiscountPreview` hoán vị cột tỷ lệ hạng/KM

- Ngày: 21/11/2026
- Task: [requirements.md](requirements.md) / [plan.md](plan.md)
- Phát hiện tại: đợt test 21/11/2026, ca ST-01, ST-02, ST-03, ST-05, ST-07 ([test-results.md](test-results.md))
- Trạng thái: **đã sửa và regression xong ngày 21/11/2026** (người dùng duyệt đợt bug); 35/35 ca PASS trên DB test mới
- Mức độ: hiển thị sai breakdown ở màn Ưu đãi (preview giảm giá); số tiền giảm/phải trả và toàn bộ luồng thanh toán vẫn đúng

## 1. Hiện tượng

Gọi `EXEC dbo.sp_DiscountPreview` với KH001 (hạng BAC 3%) + KM26HRO (10%), tiền món 250.000:

```
Source           Tier Promotion PercentDiscount TierPercent PromotionPercent Discount Payable
TierAndPromotion BAC  KM26HRO   3               10          13                 32500    217500
```

Kỳ vọng: `PercentDiscount=13`, `TierPercent=3`, `PromotionPercent=10`. Thực tế ba cột bị quay vòng:
`PercentDiscount` nhận tỷ lệ hạng, `TierPercent` nhận tỷ lệ KM, `PromotionPercent` nhận tổng.

Vì `Discount`/`Payable` tính bên trong hàm bằng đúng tổng 13% nên tiền vẫn đúng. Dialog preview ở màn Ưu đãi (`LoyaltyControl`) hiển thị câu breakdown sai, ví dụ “3% (hạng 10% + KM 13%)” trong khi thực giảm 13%.

Các luồng khác **không bị ảnh hưởng**:

- `sp_SalesCheckoutPreview` / `fn_CheckoutPreview` đọc theo tên cột → đúng (ST-11, ST-13 PASS).
- `sp_ThanhToanHoaDon`, snapshot, `sp_InvoiceDetail`, PDF, báo cáo → đúng (ST-11c, ST-16, ST-18, ST-20 PASS).
- `sp_SalesRefreshDiscount`, `sp_SalesReadCore` đọc theo tên cột → đúng.

## 2. Nguyên nhân (có bằng chứng)

`database/migrations/038_tier_promotion_stacking.sql`:

- Hàm `fn_DiscountPreview` (dòng 14–32) trả thứ tự cột:
  `Source, Tier, Promotion, TierPercent, PromotionPercent, PercentDiscount, Discount, Payable`.
- `sp_DiscountPreview` (dòng 44–45) khai báo bảng đệm theo thứ tự cũ:
  `Source, Tier, Promotion, PercentDiscount, TierPercent, PromotionPercent, Discount, Payable`
  và nạp bằng `INSERT @result SELECT * FROM dbo.fn_DiscountPreview(...)`.

`SELECT *` ghép theo **vị trí**, nên:
cột 4 của hàm (`TierPercent`) rơi vào `PercentDiscount`, cột 5 (`PromotionPercent`) rơi vào `TierPercent`, cột 6 (`PercentDiscount`) rơi vào `PromotionPercent`. Câu `SELECT` cuối (dòng 47) giữ đúng thứ tự tên cột nên kết quả ra ngoài bị hoán vị giá trị.

Đây là lỗi do thay đổi thứ tự cột của hàm trong 038 mà không cập nhật chỗ `INSERT … SELECT *` duy nhất còn lại (008 là bản cũ, cùng kiểu nhưng hàm cũ có thứ tự khớp). Các chỗ gọi khác đều dùng tên cột nên không lỗi.

## 3. Đề xuất sửa (tối thiểu)

Trong `038_tier_promotion_stacking.sql`, đổi thứ tự khai báo bảng đệm của `sp_DiscountPreview` cho khớp hàm:

```sql
DECLARE @result TABLE(Source varchar(20),Tier char(5),Promotion char(10),
 TierPercent int,PromotionPercent int,PercentDiscount int,
 Discount decimal(18,0),Payable decimal(38,0));
```

Câu `SELECT r.Source, …, r.PercentDiscount, r.TierPercent, r.PromotionPercent, …` cuối giữ nguyên, nên hợp đồng trả về cho C#/DataAccess không đổi.

Phương án thay thế: đổi `INSERT` thành danh sách cột tường minh
(`INSERT @result(Source,Tier,Promotion,PercentDiscount,TierPercent,PromotionPercent,Discount,Payable) SELECT Source,Tier,Promotion,PercentDiscount,TierPercent,PromotionPercent,Discount,Payable FROM …`)
để không phụ thuộc thứ tự cột của hàm. Có thể làm cả hai cho chắc; không đổi chữ ký hàm/thủ tục.

## 4. Phạm vi ảnh hưởng khi sửa

- Chỉ file `database/migrations/038_tier_promotion_stacking.sql` (chưa commit, **chưa áp** lên `CoffeeManager_Development`).
- Checksum của 038 sẽ đổi; các DB test scratch đã áp bản cũ (`..._038_stacking`, `..._038_accept*`) sẽ không dùng lại được với runner (báo lệch checksum). Cần tạo DB test mới từ backup `cm038_accept.bak` để regression.
- Không đổi C# (`DiscountPreview` vẫn đọc 0..9 như hiện tại), không đổi UI, không đổi snapshot/checkout/báo cáo.

## 5. Rủi ro

- Thấp: thay đổi chỉ ở thứ tự cột nội bộ của một thủ tục; toàn bộ caller đọc theo tên hoặc theo thứ tự đầu ra không đổi.
- Cần chạy lại ST-01/02/03/05/07 và regression các ca ST-11…ST-20 sau khi sửa.

## 7. Kết quả sửa và regression (21/11/2026)

- Đã đổi bảng đệm `@result` của `sp_DiscountPreview` sang thứ tự khớp hàm
  (`…,TierPercent,PromotionPercent,PercentDiscount,Discount,Payable`) và đổi
  `INSERT … SELECT *` thành danh sách cột tường minh.
- Không đổi C#, UI, snapshot/checkout/báo cáo.
- Checksum migration 038 mới: `F964DB1655C253B317D22F0E113C9A84445B517CF7AA05A5E5E6084355B1B0C8`
  (checksum cũ `FB421E48…` chỉ tồn tại trên các DB test scratch).
- Regression trên DB mới `CoffeeManager_Test_038_accept8` (copy v37 + áp 038 mới):
  `verify-stacking.ps1` → **35/35 PASS**, log `runs/stack-20261121/output-fix.log`.
  ST-01 trả `13%=3+10`, ST-07 trả `8+100=100`, các ca checkout/snapshot/báo cáo giữ nguyên PASS.
- `dotnet build src/CafeManager.WinForms` sạch (0 lỗi).
