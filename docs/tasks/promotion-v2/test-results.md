# Kết quả kiểm thử — promotion-v2 (t05)

- Ngày lập: 23/11/2026
- Trạng thái: **PASS** — đợt test 4 `20261123_053514_3a6515e3` chạy đủ 5 bước trên
  DB test mới, toàn bộ assertion SQL PASS. PM01–PM07 PASS phần DB; PM06-GUI/PM08 vẫn
  chờ người dùng kiểm giao diện.
- Đợt 1 `20261123_050625_e03567f7`: FAIL bước 02 do B01 (cú pháp `EXEC`); PM01–PM03 PASS.
- Đợt 2 `20261123_050926_6f85560d`: FAIL bước 04 do B02 (fixture `KM92000011` phủ tháng 6);
  02/03 PASS.
- Đợt 3 `20261123_053328_9e0fbec1`: FAIL bước 04, vẫn B02 — bản sửa đầu đặt sai vị trí
  tháng/ngày trong tuple fixture (thành 01/09 thay vì 09/01).
- Đợt 4 `20261123_053514_3a6515e3`: **PASS toàn bộ** — DB
  `CoffeeManager_Test_PromoV2_20261123_053514_3a6515e3`, schema 42 (migration 001–042
  `Applied`, 42 dòng), thời gian fixture 599 / 01 283 / 02 2912 / 03 2803 / 04 603 ms;
  DB đã drop (`Cleaned=true`), source không đổi (`source-before.csv` =
  `source-after.csv`, `SourceUnchanged=true`).
- Môi trường: SQL Server `localhost`, DB dùng một lần dựng từ `DB_Schema.sql` +
  `Insert_Data.sql` + migration 001–042; không phải DB sử dụng thật.
- Lệnh chạy: `powershell -ExecutionPolicy Bypass -File tools/database/Invoke-PromoV2.ps1`
- Bằng chứng mỗi lượt: `docs/tasks/promotion-v2/runs/<runId>/manifest.json`,
  `migrations.jsonl`, `source-before.csv`, `source-after.csv`.

## Bộ dữ liệu và script

| Thành phần | Nội dung |
|---|---|
| `database/tests/promo-v2/fixture.sql` | NVL91/NVL92, SP920–SP923, KH921 (DONG), KH922 (VANG), KH923 (DONG mới); 15 KM `KM92000001–15` đủ loại giảm/tặng, hạng, vãng lai, tắt, hết hạn, tương lai, vắt năm, KT trống; baseline 9 số đo. |
| `01-meta-date.sql` | PM01, PM02, PM03 |
| `02-discount-gift-preview.sql` | PM04, PM06 |
| `03-checkout-gift.sql` | PM05, PM06, PM07 |
| `04-regression.sql` | PM07, regression 039/042 |

## Tổng hợp ca

| Mã | Nội dung | Môi trường | Kết quả | Bằng chứng |
|---|---|---|---|---|
| PM01 | Lưu KM chỉ dd/MM; BD năm 2000; KT năm 2001 khi vắt năm; KT trống; lặp hằng năm; hai đầu inclusive; KM seed 039 chuẩn hóa ngày, vẫn áp đúng mùa | SQL Server test, script 01 + 04 | PASS | đợt 4: fixture PASS (quy ước ngày KM92000001/05/13, 15 KM), 01 PASS, 04 PASS (seed 039 tháng 6 = 5750, lặp hằng năm) |
| PM02 | Trạng thái chỉ Áp dụng/Tạm ngưng; `Nhap` bị CHECK chặn; danh sách suy ra Hết hạn; không còn action Bật/Tạm dừng/Preview trên UI | SQL Server test (script 01); UI do người dùng kiểm | PASS (DB) / UI BLOCKED | 01 PASS: `UPDATE ... TrangThai='Nhap'` lỗi 547, KM09/KM10 TamDung, KM11 HetHan trên danh sách |
| PM03 | Đối tượng ALL/TIER/WALKIN; nhiều hạng; 0 hạng = mọi hạng; FIRST/MEMBER bị từ chối khi ghi mới | SQL Server test, script 01 | PASS | 01 PASS: ma trận `fn_KhachThoaDoiTuong`, KM92000015 mọi hạng, FIRST/MEMBER → 52001, không chèn dòng |
| PM04 | Giảm theo một/nhiều món; bỏ trống = mọi món; 20% chỉ áp món chỉ định; chặn 100%/dòng; ship không giảm | SQL Server test, script 02 | PASS | đợt 2: 02 PASS (1969 ms) toàn bộ assertion %/chia/trần/ship |
| PM05 | Quà một/nhiều món, số lượng; mỗi KM một lần/hóa đơn; hai dòng cùng món ghi đủ; quà 0 ₫; trừ kho đúng; không cộng điểm cho quà; thiếu tồn 52203 rollback sạch | SQL Server test, script 03 | PASS | đợt 2: 03 PASS (2821 ms) — checkout A/B, quà 0 ₫, tồn kho, điểm, 52203 rollback |
| PM06 | Nhiều KM giảm + quà cùng áp; quà không cộng vào tiền giảm; token/GiftCount đổi khi sửa hoặc tạm ngưng quà; token cũ bị 52201 | SQL Server test, script 02 + 03 | PASS | đợt 2: 02 PASS (token đổi khi sửa quà), 03 PASS (pause → token/GiftCount đổi, token cũ 52201) |
| PM06-GUI | F3 chọn khách, POS/checkout/in liệt kê KM và quà trên app WinForms thật | App thật | BLOCKED — chuyển người dùng kiểm như tiền lệ PA14 | — |
| PM07 | Snapshot/danh sách ưu đãi bất biến; sửa/xóa dòng quà 51001; sửa/xóa thanh toán 51002; snapshot 52207; danh sách áp dụng 53307; xóa KM đã dùng 52006; hóa đơn seed trước 042 giữ nguyên; báo cáo chạy | SQL Server test, script 03 + 04 | PASS | đợt 4: 03 PASS (bảo vệ dữ liệu đúng mã), 04 PASS (seed paid/điểm/tồn kho không đổi, 2 đơn hôm nay, báo cáo SalesDay/Products/Payments/Inventory chạy, contract 042) |
| PM08 | UI editor theo mockup, tìm món, đếm món; DPI 125% chức năng | App thật | BLOCKED — agent không thao tác GUI; hình thức SKIPPED theo phân công người dùng tự nghiệm thu | — |

## Ghi chú các lượt chạy

- Đợt 1 `20261123_050625_e03567f7`: fixture/01 PASS; 02 FAIL (B01 — cú pháp
  `EXEC ... @StartDate=DATEFROMPARTS(...)`); 03/04 chưa chạy. Sửa B01 sau duyệt.
- Đợt 2 `20261123_050926_6f85560d`: fixture/01/02/03 PASS; 04 FAIL (B02 — fixture
  `KM92000011` 01/01–22/11 giảm 50% phủ 15/06/2027, ra 18250 thay vì 5750).
- Đợt 3 `20261123_053328_9e0fbec1`: vẫn FAIL B02 — bản sửa đặt `9` vào cột ngày thay vì
  cột tháng nên cửa sổ thành 01/09–22/11 và vẫn phủ tháng 6. Đã dò lại trên DB test:
  `fn_UuDaiCap('2027-06-15','KH001',...)` trả thêm KM92000011 50%, tổng 73% = 18250.
- Đợt 4 `20261123_053514_3a6515e3`: fixture 599 ms, 01 283 ms, 02 2912 ms, 03 2803 ms,
  04 603 ms — **tất cả PASS**, `Status=Succeeded`, `Cleaned=true`, `SourceUnchanged=true`.
  Fixture đổi cửa sổ KM92000011 thành 09/01–22/11 (cột tháng = 9), hôm nay vẫn hết hạn
  nên 01 vẫn thấy `HetHan`; phép so seed 039 trở lại đúng 5750.
- Ba lỗi B01/B02 đều thuộc bộ kiểm thử t05 (script/fixture), không có thay đổi sản phẩm
  nào trong các lượt test; DB test mỗi đợt đã drop.
- PM02/PM08 phần giao diện: người dùng tự kiểm hình thức; agent không ghi PASS hình thức.
