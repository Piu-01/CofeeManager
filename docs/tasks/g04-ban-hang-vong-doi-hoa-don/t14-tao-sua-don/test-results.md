# Kết quả kiểm thử chức năng G04-POS-01 — 10/11/2026

Đợt test được người dùng duyệt bằng "duyệt" sau khi chốt hướng sửa: **Đơn mới tự
chuyển sang bàn trống, hết bàn thì thông báo**. Source cố định trong suốt lượt
test; không sửa code, migration hay expected.

## Môi trường và phạm vi

| Mục | Giá trị |
|---|---|
| App | WinForms .NET 10 (`net10.0-windows`), build Debug, source tại commit `8a38f86` + thay đổi chưa commit của G04-POS-01 |
| SQL | SQL Server `localhost`, DB test cô lập `CoffeeManager_Test_G04_20261110_124309_41e6e800_posnew` (schema 16, marker `20261110_124309_41e6e800`) |
| DPI | 120 (125%); 100%/150% NOT RUN theo phân công |
| Hình thức giao diện | SKIPPED theo phân công (người dùng tự kiểm) |
| Thực thi | Harness WinForms thật trong tiến trình: MainForm + SalesControl, AuthService/PasswordService thật, SQL runtime user `G04PosNew`; thao tác click dialog qua control WinForms |
| Source hash trước = sau | `SalesControl.cs` `822A94DF…16CD`; `OrbitConfirmationDialog.cs` `44DFFEC1…EC68` |
| Binary test | `CafeManager.WinForms.dll` SHA256 `20c1B7B4…35844` (khớp DLL vừa build từ source trên) |

Dựng DB từ `database/DB_Schema.sql` + `Insert_Data.sql` + migration 001–016, thêm
bảng marker và user runtime; không đụng `CoffeeManager_Development` hay DB test G04
đã có. Fixture đặt bàn 01 có đơn mở (giống báo lỗi), bàn 03 có đơn mở từ seed.
Tài khoản fixture `posnew.admin`/`posnew.cash` dùng mật khẩu ngẫu nhiên và đã bị
khóa (`HoatDong=0`) sau lượt test.

## Lệnh chạy

```
dotnet build PosNewOrder.csproj -c Debug
powershell -NoProfile -File run.ps1   # trỏ runtime connection DPAPI sang DB test
```

Kết quả máy: `results.json` — **27/27 PASS, 0 FAIL**. Chi tiết tại
`runs/pos-neworder-20261110/results.json`, manifest cùng thư mục.

## Ca kiểm thử

| Mã | Ca (đã duyệt) | Expected | Actual | Kết quả |
|---|---|---|---|---|
| P00 | Trạng thái đầu giống báo lỗi | Combo chọn B0001 đang có đơn mở; nhãn "Ban 01 · Đang phục vụ" | Đúng | PASS |
| P01 | Bàn đang chọn bận + Đơn mới → tự chuyển bàn trống, thêm món tạo đơn tại bàn mới | Chọn bàn trống kế tiếp (B0002); nhãn "Đã chuyển sang Ban 02 (trống)"; món đầu tạo đơn bàn 02 | Đơn `HD20250412` bàn `B0002`, 1 dòng, nhãn đúng | PASS |
| P01 | Không ghi nhầm vào bàn đang bận | B0001 giữ nguyên đơn `HD20250411`; cashier không tạo đơn B0001 | Đúng | PASS |
| P02 | Bàn trống đang chọn + Đơn mới | Giữ nguyên bàn đang chọn, nhãn "Đang chọn Ban 05 (trống)" | Đúng | PASS |
| P03 | Đơn mới lặp lại khi bàn vừa tạo đã bận | Chọn bàn trống kế tiếp, không tạo header | Chọn B0004, không tạo header | PASS |
| P04 | Toàn bộ bàn bận + Đơn mới | Hộp thoại "Hết bàn trống" một nút "Đã hiểu"; bỏ chọn bàn; nhãn giải thích | Đúng; nút duy nhất "Đã hiểu" | PASS |
| P04 | Thêm món khi hết bàn | Bị chặn với thông báo "Chọn bàn trước khi thêm món"; không ghi header/dòng | Đúng; số HOADON không đổi | PASS |
| P05 | Sau khi hết bàn, chuyển Mang đi | Tạo được đơn Mang đi không gắn bàn | `HD20250420`, `MaBan NULL`, `LoaiDon=Mang di` | PASS |
| P06 | Chọn bàn trống qua danh sách (regression) | Giữ đúng bàn, nháp chờ món đầu, tạo đơn tại bàn đã chọn | B0004, đơn `HD20250413` bàn B0004 | PASS |
| P06 | Mở đơn có sẵn rồi thêm món (đường F4, regression) | Nạp đúng bàn/loại đơn và thêm dòng thành công | `HD20250410` bàn B0003; dòng 2 → 3 | PASS |
| P06 | Dialog Có/Không và Đồng ý/Hủy sau khi thêm nhánh OK-only | Vẫn 2 nút đúng nhãn; "Không" trả No, "Hủy" trả Cancel | Đúng | PASS |

## Bằng chứng SQL sau lượt chạy

| Dữ liệu | Giá trị |
|---|---|
| Đơn fixture bàn bận | `HD20250411` B0001 SCA30 mở (không phải cashier) |
| Đơn cashier tạo đúng bàn chuyển | `HD20250412` B0002, `HD20250413` B0004, `HD20250420` Mang đi, tổng tiền 25.000 mỗi đơn |
| Đơn seed thêm dòng | `HD20250410` từ 2 lên 3 dòng |
| Bàn không có đơn mở sau P04 | 0 (toàn bộ bàn bận đúng fixture) |
| Tài khoản fixture | `posnew.admin`, `posnew.cash` đều `HoatDong=0` sau lượt |

## Chưa chạy / rủi ro còn lại

- **NOT RUN**: đua hai phiên giữa lúc tải bàn và lúc tạo (người khác chiếm bàn sau
  khi UI đã tải) — khi đó lỗi 52104 vẫn chỉ hiện ở nhãn trạng thái, ngoài phạm vi
  sửa đã chốt.
- **NOT RUN**: 100%/150% DPI; hình thức/thị giác do người dùng tự kiểm (SKIPPED).
- DB test được giữ lại làm bằng chứng; không xóa đơn/lịch sử để dọn fixture.

## Lượt test 15/11/2026 — G04-POS-04 map lỗi thiếu đối tượng

Người dùng duyệt đợt test bằng "duyệt" sau khi chốt sửa `SqlErrorMapper`
(mục 8 của bug report). Source cố định trong suốt lượt: không sửa code, migration
hay expected.

| Mục | Giá trị |
|---|---|
| App/source | Build Debug, commit `8a38f86` + thay đổi chưa commit của G04-POS-04 (`DataError.cs`, `SqlErrorMapper.cs`, `ParameterAndErrorTests.cs`) |
| SQL | Không truy cập DB trong lượt này (chỉ unit test thuần) |
| Lệnh | `dotnet build CoffeeManager.slnx -c Debug` và `dotnet test tests/CafeManager.Tests/CafeManager.Tests.csproj -c Debug --filter "Category=Unit"` |
| Build | 0 warning, 0 error (14,12 giây) |
| Kết quả | **37/37 PASS, 0 FAIL, 0 skipped** (414 ms) |
| Theory mới | `MissingSchemaObjectsPointToMigrationInsteadOfUnknownResult` — **5/5 PASS** (42 ms) |

### Ca kiểm thử

| Mã | Ca | Expected | Actual | Kết quả |
|---|---|---|---|---|
| MAP-01 | `MapNumber(2812, writing=false)` | Kind `Schema`, không `RequiresReconciliation`, thông báo nhắc migration/triển khai | Đúng | PASS |
| MAP-02 | `MapNumber(2812, writing=true)` | Như trên: lời gọi thiếu procedure hỏng trước khi ghi nên POS không khóa đối chiếu | Đúng | PASS |
| MAP-03 | `MapNumber(2715, writing=true)` (thiếu kiểu dữ liệu/TVP) | Kind `Schema`, không đối chiếu | Đúng | PASS |
| MAP-04 | `MapNumber(208, writing=false)` | Kind `Schema`, không đối chiếu (lệnh đọc) | Đúng | PASS |
| MAP-05 | `MapNumber(208, writing=true)` | Kind `Schema` **và** `RequiresReconciliation = true` (bảng có thể thiếu giữa procedure đã ghi dở) | Đúng | PASS |
| REG-01 | Toàn bộ unit suite: T05-K02, T05-K04, T05-K09, T05-K10, kết nối/chứng chỉ/timeout, localization | Giữ nguyên hành vi cũ | 32/32 PASS | PASS |

### Chưa chạy

- **NOT RUN** luồng POS nháp → Giữ → Thanh toán trên GUI (người dùng tự thao tác
  theo phân công kiểm UI; migration 030 đã áp theo mục 7 bug report).
- **NOT RUN** `Category=Integration` của test project (cần DB test cô lập qua
  `TestDatabaseFixture`); thay đổi lần này không chạm SQL/DAL call.
- **NOT RUN** 100%/150% DPI và hình thức — SKIPPED theo phân công.

## Lượt test 19/11/2026 — Bỏ nháp POS, tự mở đơn mới (G04-POS-02 thay thế)

Người dùng duyệt đợt test bằng "duyệt" sau khi chốt bỏ nháp bộ nhớ: vào màn Bán hàng
tự mở ngữ cảnh đơn mới (ưu tiên bàn trống), chỉ hỏi khi rời màn lúc có input thật chưa
ghi. Source cố định trong suốt lượt: không sửa code, migration hay expected.

### Môi trường và phạm vi

| Mục | Giá trị |
|---|---|
| App | WinForms .NET 10 (`net10.0-windows`), build Debug từ working tree tại commit `5673f51` + thay đổi bỏ nháp chưa commit (`SalesControl.cs`, `OrderPanel.cs`, `SalesService.cs`, `SalesRepository.cs`, `SalesRequests.cs`, xóa `SalesDraft.cs`, `PosDraft*Dialog.cs`, `SalesDraftTableBuilder.cs`) |
| Build | `dotnet build -c Debug -o <thư mục tạm>`: **0 error**, 1 cảnh báo MSB3277 (WebView2/WindowsBase) có sẵn; DLL không bị khóa bởi app người dùng đang mở |
| SQL | SQL Server `localhost`, DB test cô lập `CoffeeManager_Test_G04_20261119_043701_42f42376_ui` (schema 35, COPY_ONLY backup + RESTORE từ `CoffeeManager_Development`), marker `dbo.G04TestMarker`; app chạy bằng biến môi trường `CAFEMANAGER_APP_CONNECTION`, không đụng DB dev |
| Đăng nhập | `thungan` (Pham Thi Dung · CASH) |
| DPI | 125%; 100%/150% NOT RUN theo phân công |
| Hình thức giao diện | SKIPPED theo phân công (người dùng tự kiểm) |
| Thực thi | UIA (ControlViewWalker) + chuột thật trên cửa sổ WinForms thật; thao tác dialog nút bằng UIA InvokePattern; SQL kiểm chứng bằng script chỉ đọc trên DB test |
| Run | `runs/ui_20261119_043701_42f42376/` (`manifest.json` ghi DB, backup, SourceVersion=35) |
| Source hash trước = sau | Không sửa file source nào trong lượt; `git status` chỉ gồm thay đổi đã có trước lượt |

### Ca kiểm thử

| Mã | Ca (đã duyệt) | Expected | Actual | Kết quả |
|---|---|---|---|---|
| V1 | Vào màn Bán hàng khi chưa có đơn | Tự mở ngữ cảnh đơn mới, chọn bàn trống đầu tiên, không ghi SQL | Nhãn "Đã chọn Bàn 02 (trống). Thêm món đầu để tạo đơn."; bảng món rỗng "Chưa tạo đơn"; số HOADON không đổi | PASS |
| V2 | Thêm món đầu tiên | Tạo header + dòng đầu trong một giao dịch, bàn B0002, 25.000 | `HD26093447` bàn `B0002`, 1 dòng `SP001` 25.000, trạng thái `Chua thanh toan`; nhãn "Đã tạo đơn HD26093447 và tải lại từ SQL." | PASS |
| V2b | Thêm món thứ hai (autosave) | Tự lưu dòng, tổng 55.000 | Header 55.000, 2 dòng (`SP001` 25.000 + `SP002` 30.000) | PASS |
| V3a | Rời tab Bán hàng rồi quay lại khi không có input dở | Không hiện hộp hỏi; đơn tải lại từ SQL | Vào "Khách hàng" rồi quay lại: không hộp hỏi; nhãn "Đã tải từ SQL · 04:42"; đơn và bàn giữ nguyên | PASS |
| V3b | Mở ghi chú dòng món rồi rời màn | Có hỏi vì tồn tại input thật chưa ghi; chọn Hủy thì ở lại màn | Hộp "Dữ liệu chưa lưu / Bỏ input chưa ghi để tiếp tục?" (Đồng ý/Hủy); bấm Hủy giữ nguyên màn Bán hàng, ghi chú còn mở | PASS |
| V4 | Thanh toán rồi đóng phiên | Ghi thanh toán SQL, đóng phiên bàn, POS tự mở đơn mới, bàn trống | Thanh toán 55.000 PT001; phiên 1 đóng `04:48:59`; POS tự mở đơn mới, B0002 "Trống", panel "Chưa tạo đơn" | PASS |
| V5 | Xóa dòng cuối của đơn đã lưu | Giữ header mở, nhãn "Đơn đã hết món..."; SQL còn đơn mở 0 dòng | Nhãn đúng; `HD26093448` vẫn `Chua thanh toan`, 0 dòng | PASS |
| V5b | Hủy đơn mở của đơn 0 dòng → Trả bàn | Hộp xác nhận hủy + hộp "Trả bàn ngay?"; hủy xong tự mở đơn mới, phiên đóng | Đồng ý → "Trả bàn ngay?" Có → phiên 2 đóng, `HD26093448` = `Huy`, B0002 "Trống", tự mở đơn mới | PASS |
| V6 | Hai nguồn tranh bàn (52104) | Hộp "Mở đơn hiện có?" khi bàn vừa bị chiếm; không merge dòng; chọn Không thì giữ bàn đang chọn + nhãn hướng dẫn | Dialog "Bàn đã có đơn / Bàn Ban 02 vừa có đơn ở phiên khác. Mở đơn hiện có?" (Có/Không); Không → "Đã giữ nguyên bàn đang chọn..."; SQL chỉ còn đơn fixture 0 dòng, không có header/dòng mới | PASS |
| V6b | Mở đơn hiện có của bàn bị chiếm | "Chọn bàn" tải đúng đơn của phiên khác và thao tác được | Tải `HD26999001` (0 ₫); hủy được qua đúng quy trình; phiên 3 đóng; B0002 "Trống"; tự mở đơn mới; SQL `Huy` | PASS |
| V7 | Hết toàn bộ bàn trống → Đơn mới | Hộp "Hết bàn trống" một nút "Đã hiểu"; nhãn hướng dẫn Mang đi/Giao hàng; không ghi SQL | Hộp "Hết bàn trống" + "Không còn bàn trống để tạo đơn mới. Hãy xử lý đơn đang mở hoặc chọn đơn Mang đi/Giao hàng."; nhãn trạng thái đúng; panel "Chưa tạo đơn"; không ghi SQL | PASS |

### Bằng chứng chính

- V1–V4: `runs/ui_20261119_043701_42f42376/after-login.txt`, `dump-deep.txt`,
  `after-add1.txt`, `after-add2.txt`, `sql-hd-check.txt`, `sql-hd-check2.txt`,
  `after-leave-nav.txt`, `after-return-sales.txt`, `after-open-note.txt`,
  `after-leave-with-note.txt`, `after-cancel-leave.txt`, `after-open-checkout.txt`,
  `after-pay-click.txt`, `after-pay-close-table.txt`, `sql-after-pay.txt`.
- V5: `after-v5-add3.txt`, `after-v5-delete.txt`, `sql-v5-delete.txt`,
  `after-v5-cancel-click.txt`, `after-v5-cancel-confirm4.txt`,
  `after-v5-close-confirm.txt`, `sql-v5-close.txt`, `sql-v5-sessions.txt`.
- V6: `after-v6-conflict.txt`, `after-v6-decline.txt`, `sql-v6-open-b0002.txt`,
  `after-v6-conflict2.txt`, `after-v6-select-table.txt`, `sql-v6-fixture-session.txt`,
  `after-v6-cancel-fixture3.txt`, `sql-v6-cleanup.txt`.
- V7: `sql-v7-occupy.txt` (10/10 bàn `Dang phuc vu`), `after-v7-refresh.txt`,
  `after-v7-neworder.txt`, `after-v7-ack.txt`, `sql-v7-cleanup.txt`,
  `after-v7-recover.txt`.

### Ghi nhận trong lượt (không sửa source)

- **Fixture V6**: lần tranh bàn đầu tiên dùng đơn chèn tay chưa có `PHIEN_BAN`, view
  không trả `OrderCode` nên lần bấm thứ hai rơi nhánh "Bàn Ban 02 đang có đơn..."
  thay vì hộp Có/Không. Đã gắn phiên mở cho fixture rồi thử lại đúng đường "Chọn bàn"
  → mở đơn hiện có. Đơn do `sp_TaoHoaDon` tạo thật luôn có phiên, nên đây là giới hạn
  của fixture, không phải lỗi app.
- **Nhãn bàn V7**: sau khi hết bàn, combo bàn bị bound tự chọn lại dòng đầu nên nhãn
  hiển thị "Ban 01 · Dang dùng" thay vì "Chưa chọn bàn"; context vẫn là Tại quán
  không bàn và nhãn trạng thái hướng dẫn đúng. Ghi nhận để theo dõi, không sửa trong
  lượt test.
- **Thẻ món**: bấm vùng thân thẻ không thêm món; chỉ nút "+" (Thêm <tên món>) thêm.
  Đúng như hiện trạng được duyệt (P-series trước dùng nút), ghi nhận để đối chiếu UX.
- **Dọn fixture**: đã xóa `HD26999002`–`HD26999006` và phiên 4–8. `HD26999001` (đã
  `Huy` qua UI) không xóa được do trigger "Khong sua/xoa hoa don da huy" — giữ lại
  làm bằng chứng, không ảnh hưởng bàn (phiên 3 đã đóng, B0002 trống).

### Chưa chạy / rủi ro còn lại

- **NOT RUN**: 100%/150% DPI và mọi kiểm hình thức/thị giác — SKIPPED theo phân công.
- **NOT RUN**: đua hai phiên đúng nghĩa (hai app độc lập bấm cùng lúc); V6 dùng fixture
  SQL thay cho app thứ hai, chỉ kiểm nhánh xử lý 52104 phía UI.
- **NOT RUN**: in sau thanh toán, tra cứu hóa đơn, giao hàng F3 đầy đủ — ngoài phạm vi
  đợt này (đã có biên bản riêng).
- **NOT RUN**: lối tắt bàn phím F2/F3/F4/F8 trong ngữ cảnh mới; lượt này thao tác chuột.
