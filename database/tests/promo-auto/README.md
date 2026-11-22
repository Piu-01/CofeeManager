# PromoAuto — test SQL cho migration039

Bộ test riêng cho ưu đãi tự áp theo điều kiện. Chỉ chạy trên DB test tạo mới
(`CoffeeManager_Test_PromoAuto_<runId>`), không chạm `CoffeeManager_Development`.

## Cách chạy (sau khi người dùng duyệt đợt test)

```powershell
powershell -ExecutionPolicy Bypass -File tools/database/Invoke-PromoAuto.ps1
```

Runner tạo DB từ `DB_Schema.sql` + `Insert_Data.sql`, áp tuần tự migration 001–039
qua `Apply-Migration.ps1`, chạy `fixture.sql` rồi 4 script, thêm ca đua hai session
Pause cùng revision, ghi biên bản vào
`docs/tasks/promotion-dieu-kien-tu-ap/runs/<runId>/manifest.json`, và drop DB sau khi
đối chiếu marker. Source được băm trước/sau để chứng minh không sửa mã khi test.

## Nội dung

| File | Phạm vi |
|---|---|
| `fixture.sql` | 13 ưu đãi (ALL/FIRST/MEMBER/TIER, ITEM/ALL, Nháp/Tạm dừng/Hết hạn/Tương lai/không NgàyKT, chặn 100%), 2 khách test, món giá lẻ, đơn mở legacy; baseline hóa đơn seed + tồn kho |
| `01-preview-multi.sql` | 3 ưu đãi cộng dồn, tách hạng/khuyến mãi, ship không giảm, phân bổ AP, TVP rỗng, danh sách ADMIN/CASH |
| `02-conditions.sql` | FIRST/MEMBER/TIER, phạm vi món, biên ngày, làm tròn theo dòng, chặn 100%, NgàyKT NULL, trạng thái, legacy không nhân đôi |
| `03-checkout-snapshot.sql` | token đổi khi Pause/Resume, token cũ bị 52201 và rollback sạch, snapshot + `HoaDonUuDaiApDung`, điểm, tiêu hao kho, trigger cấm sửa/xóa, đọc lại POS/chi tiết |
| `04-report-unchanged.sql` | hóa đơn seed không đổi số, doanh thu hôm nay đúng 50000/15000/35000, báo cáo SalesDay/Products/Inventory chạy được, tồn kho khớp tiêu hao |
| runner | đua hai session Pause cùng `ExpectedRevision`: đúng 1 thành công, 1 nhận 51603 |

## Kỳ vọng số học chính

- KH902 (VANG 5%, chưa có hóa đơn đã trả), SP001×2 50.000 + SP002 30.000, ship 15.000:
  hạng 4.000; FIRST 10.000; TIER 3.000; ALL 4.000; giảm 21.000; phải trả 74.000.
- SP901 giá 105 với 5% + 7%: gộp 12% làm tròn 13 (không phải 5 + 7 = 12).
- SP003 32.000 với hạng 5% + 60% + 60% + 5%: chặn 100%, giảm 32.000, hạng 1.600,
  phân bổ khuyến mãi 30.400.
- Đơn chốt SP001×2 cho KH902: tổng 50.000, giảm 15.000 (hạng 2.500 + KM 12.500),
  thu 35.000, điểm 35, tiêu hao NVL01 0.036 và NVL03 0.02.

Không tick PASS trước khi chạy. Hình thức UI do người dùng tự kiểm theo phân công.
