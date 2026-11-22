# Bug — promotion-v2 / t05

## B01 — Script `02-discount-gift-preview.sql` lỗi cú pháp, cả batch không chạy

- Phát hiện: đợt test `20261123_050625_e03567f7` (23/11/2026), bước 2 FAIL.
- Loại: lỗi script kiểm thử do t05 viết; **không phải lỗi sản phẩm**, không liên quan
  migration 042, C# hay procedure.

### Hiện tượng

`manifest.json` của lượt test: fixture PASS, `01-meta-date` PASS,
`02-discount-gift-preview` FAIL sau 24 ms với:

```
Incorrect syntax near '2000'. (x3)
```

Bước 3 và 4 không được chạy. DB test đã được drop (`Cleaned=true`), source không đổi
(`SourceUnchanged=true`, `source-before.csv` = `source-after.csv`).

### Nguyên nhân (có bằng chứng)

Ba chỗ gọi `sp_PromotionWrite` truyền hàm trực tiếp làm giá trị tham số:

- dòng 64: `@StartDate=DATEFROMPARTS(2000,1,1),@EndDate=DATEFROMPARTS(2000,12,31)`
- dòng 119–120: như trên trong lệnh `Update` KM92000005
- dòng 130–131: như trên trong lần `Update` hoàn nguyên KM92000005

Cú pháp `EXEC ... @param = <value>` chỉ nhận hằng/variable; gọi hàm tại chỗ là lỗi
cú pháp, parser báo ngay token `2000` — đúng 3 thông báo, khớp 3 vị trí trên.
Vì là lỗi compile của batch, **toàn bộ** assertion trong file 02 (kể cả phần giảm
giá trước dòng 63) không được thực thi; lỗi không nằm ở dữ liệu hay procedure.

`fixture.sql` không lỗi vì đã gán `@sd/@ed` bằng `SET` trước khi gọi procedure.

### Đề xuất sửa (chỉ file test)

Trong `database/tests/promo-v2/02-discount-gift-preview.sql`, khai báo biến ngày một
lần và dùng lại:

```sql
DECLARE @sd date=DATEFROMPARTS(2000,1,1),@ed date=DATEFROMPARTS(2000,12,31);
```

thay 3 cặp `@StartDate=DATEFROMPARTS(...),@EndDate=DATEFROMPARTS(...)` bằng
`@StartDate=@sd,@EndDate=@ed`. Không đổi expected value, không đổi seed/DB/procedure.

### Phạm vi ảnh hưởng và rủi ro

- Chỉ script test 02; sản phẩm, migration 042, C#, các script 01/03/04/fixture giữ nguyên.
- Rủi ro: sau khi sửa phải chạy lại cả suite từ DB mới (vì 02 tạo KM92000017 và sửa KM05,
  03/04 phụ thuộc trạng thái cuối của 02). Không chạy chèn vào DB cũ.

### Regression sau khi sửa

Chạy lại `tools/database/Invoke-PromoV2.ps1` với DB mới; tất cả bước fixture → 04 phải
PASS rồi mới ghi kết quả PM04–PM07.

### Kết quả sau sửa

- Đợt 2 `20261123_050926_6f85560d`: 02 PASS (1969 ms), 03 PASS (2821 ms) — B01 đóng.


## B02 — Fixture KM92000011 phủ tháng 6, làm sai phép so KM seed 039 ở bước 04

- Phát hiện: đợt test `20261123_050926_6f85560d` (23/11/2026, sau khi sửa B01).
  fixture/01/02/03 PASS; 04 FAIL 676 ms với thông báo “KM seed 039 không còn áp đúng sau 042.”
- Loại: lỗi bộ dữ liệu test do t05 đặt, **không phải lỗi sản phẩm**. Lỗi này chỉ lộ ra
  sau khi B01 được sửa (trước đó 02 chưa compile nên 04 chưa từng chạy).

### Hiện tượng

Bước 04 mục 7 dùng `fn_UuDaiTong('2027-06-15','KH001',...)` và kỳ vọng chỉ có hai KM seed
tháng 6 (KM24SAL 10% + KM25SAL 10%) cộng hạng BAC 3% → 23% của 25000 = 5750.

### Nguyên nhân (có bằng chứng)

Fixture tạo `KM92000011` — “Promo v2 het han hom qua” — với `DoiTuongKhach='ALL'`,
`TrangThai='DangChay'`, cửa sổ 01/01–22/11, giảm 50%. Migration 042 lưu thành
`2000-01-01`–`2000-11-22` và `fn_UuDaiCap` chỉ lọc theo `fn_NgayTrongKy` (tháng/ngày),
trong khi `fn_KhachThoaDoiTuong('ALL',...)` trả 1. Vì vậy ngày 15/06/2027 rơi vào cửa sổ
01/01–22/11 → KM92000011 cũng áp 50%.

Đây là hành vi đúng của thiết kế lặp hằng năm: “Hết hạn” chỉ là trạng thái hiển thị của
năm hiện tại; sang năm KM lặp lại. Lỗi nằm ở việc fixture dùng cửa sổ 01/01–22/11 cho ca
“hết hạn hôm qua”, trùng với tháng 6 dùng để kiểm KM seed 039.

Số liệu suy ra tại 15/06/2027 với KH001 (BAC 3%): 3% + 10% + 10% + 50% = 73% của 25000
= 18250, không phải 5750. Các KM fixture còn lại không áp: KM92000001 (01/11–28/02) và
KM92000006–10/12 không phủ 15/06; KM92000002/14 theo món SP920/SP921 (đang đo SP001);
KM92000015 theo món SP923; KM92000013 chỉ hạng KIM; KM92000017 đã xóa ở bước 02.

### Đề xuất sửa (chỉ fixture)

Đổi cửa sổ `KM92000011` trong `database/tests/promo-v2/fixture.sql` từ 01/01–22/11 thành
**01/09–22/11** (vẫn hết hạn khi chạy ngày 23/11 nên 01 vẫn thấy `HetHan`), để không phủ
tháng 6 và không còn chen vào phép so KM seed 039. Không đổi expected 5750, không đổi
procedure/seed 039/migration.

### Phạm vi ảnh hưởng và rủi ro

- Chỉ dòng fixture của `KM92000011`; 01 (assert `HetHan`), 02, 03 không phụ thuộc độ dài
  cửa sổ này vì hôm nay KM đều hết hạn ở cả hai phương án.
- Phải chạy lại toàn bộ suite trên DB mới; không chạy chèn vào DB cũ.

### Kết quả sau sửa

- Đợt 3 `20261123_053328_9e0fbec1` vẫn FAIL vì bản sửa đầu đặt `9` vào **cột ngày**
  (tuple fixture là tháng bắt đầu, ngày bắt đầu, tháng kết thúc, ngày kết thúc), thành
  01/09–22/11 — vẫn phủ tháng 6. Dò trên DB test xác nhận KM92000011 áp thêm 50%.
- Sửa lại thành `9,1,11,22` (09/01–22/11). Đợt 4 `20261123_053514_3a6515e3`: cả 5 bước
  PASS, `Succeeded`, DB drop sạch, source không đổi.


