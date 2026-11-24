# PromoOnePerItem — test SQL cho migration044

Bộ test riêng cho luật "mỗi món tối đa một KM giảm, chọn tổ hợp tổng giảm lớn nhất,
hạng tính sau KM". Chỉ chạy trên DB test tạo mới
(`CoffeeManager_Test_PromoOnePerItem_<runId>`), không chạm `CoffeeManager_Development`.

## Cách chạy (sau khi người dùng duyệt đợt test)

```powershell
powershell -ExecutionPolicy Bypass -File tools/database/Invoke-PromoOnePerItem.ps1
```

Runner tạo DB từ `DB_Schema.sql` + `Insert_Data.sql`, áp tuần tự migration 001–044
qua `Apply-Migration.ps1`, chạy `fixture.sql` rồi 5 script, ghi biên bản vào
`docs/tasks/promotion-mot-mon-mot-km/runs/<runId>/manifest.json`, và drop DB sau khi
đối chiếu marker. Source được băm trước/sau để chứng minh không sửa mã khi test.

## Nội dung

| File | Phạm vi |
|---|---|
| `fixture.sql` | 30 KM93 toàn năm (A 10%, B 30%, B+C 40%, ALL 5%, quà SP944, ALL 50% tạm dừng cho legacy, D 1%, hai KM F 10%, G 100%, A2/B2/B2+C2, H 20%, 16 KM 1–16% chồng chéo trên SP952), 2 khách DONG/VÀNG, món riêng, baseline seed + tồn kho + điểm |
| `01-selection.sql` | PP01 chọn KM1+KM3 thay vì KM2+KM3; PP01b KM11+KM12 thay KM13; PP02 so ALL với KM món; PP03 hai dòng cùng món; PP05 hòa chọn mã nhỏ; chặn 100% |
| `02-tier-after.sql` | PP04 hạng 5% trên phần sau KM (ví dụ 100k/20k/5% = 24 000; 14 500/57 500), % hiển thị là mức thực tế, ship không giảm, KM 100% chặn hạng về 0 |
| `03-checkout.sql` | PP06 preview → Pause KM3 → token cũ bị 52201 và rollback sạch → preview lại 36 600 → thanh toán/snapshot/điểm; PP07 quà độc lập; PP07b legacy `HOADON.MaKM`; PP06e hóa đơn đã trả khóa theo snapshot |
| `04-regression.sql` | PP08 hóa đơn seed, doanh thu hôm nay 300 000/86 600/213 400, snapshot bằng tổng KM đã áp, tồn kho và điểm |
| `05-performance.sql` | PP08 đo ms preview và hàm với nhóm 17 ứng viên (>15, nhánh tham lam), ghi `PromoOnePerItemTiming`; không thanh toán |

## Kỳ vọng số học chính

- A 100 000 + B 60 000 + C 40 000: KM1 (A 10% = 10 000) + KM3 (B+C 40% = 40 000) =
  50 000; không áp KM2 lên B.
- A2 100 000 + B2 60 000 + C2 40 000: KM11 10 000 + KM12 30 000 = 40 000;
  không áp KM13 lên B2.
- D 20 000: ALL 5% (1 000) thắng D 1% (200); E 50 000: ALL 2 500.
- Hai dòng A 100 000 + 50 000: KM1 Base 150 000, giảm 15 000.
- KH932 (VÀNG 5%) với A: KM 10 000, hạng 4 500 (5% của 90 000), tổng 14 500, % thực tế 15.
- KH932 A+B+C: KM 50 000, hạng 7 500, tổng 57 500, phải trả 157 500 khi ship 15 000, % 29.
- Sau khi tạm dừng KM3: KM1+KM2 = 28 000, hạng 8 600, tổng 36 600, thu 163 400, 163 điểm.
- Legacy ALL 50% trên A 100 000: giảm 50 000, thu 50 000, 50 điểm.

Không tick PASS trước khi chạy. Hình thức UI do người dùng tự kiểm theo phân công.
