# Bug report — promotion-row-edit-filter

Phát hiện trong đợt test được duyệt (25/11/2026). Source trong lượt test giữ nguyên,
chưa sửa.

- BUG-01: đã được người dùng duyệt sửa, đã sửa và chạy lại mục 2 của
  `test-results.md` (P-11…P-18, P-22 PASS sau sửa; xem "Kết quả sửa" cuối BUG-01).
- BUG-02: được người dùng duyệt sửa cùng lượt; đã sửa bằng migration 048, kiểm SQL
  và UI thật xong (xem "Kết quả sửa" cuối BUG-02).

## BUG-01 — Mở màn Khuyến mãi (ADMIN) ném ArgumentOutOfRangeException, màn không mở được

- Mức độ: **chặn** (blocker của đợt test; toàn bộ ca UI ADMIN không chạy được).
- Môi trường: build Debug `C:\Users\Lenovo\.qoder\tmp\coffee-build-debug`, DB
  `CoffeeManager_Test_Promo_20261125` (schema 47), đăng nhập `admin` · ADMIN.

### Hiện tượng

Bấm mục "Ưu đãi / Điểm" trên thanh điều hướng → hộp thoại Windows
"Unhandled exception has occurred in your application":

```
System.ArgumentOutOfRangeException: value ('0') must be less than '0'.
(Parameter 'value') Actual value was 0.
   at System.Windows.Forms.ComboBox.set_SelectedIndex(Int32 value)
   at CafeManager.WinForms.Modules.Loyalty.PromotionList..ctor(...)
      LoyaltyControl.cs:line 59
   at CafeManager.WinForms.Modules.Loyalty.LoyaltyControl..ctor(...)
      LoyaltyControl.cs:line 17
   at CafeManager.WinForms.Program.<>c__DisplayClass0_1.<Main>b__19() Program.cs:90
   at CafeManager.WinForms.Forms.MainForm.Navigate(NavigationTarget)
```

Ứng dụng ở lại màn Tổng quan; lặp lại mỗi lần bấm mục Ưu đãi / Điểm. Tài khoản CASH
không bị vì CASH không mở màn này (menu CASH không có `Loyalty`).

### Nguyên nhân và bằng chứng

`LoyaltyControl.cs:58-59`:

```csharp
statusBox=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,DataSource=choices.ToList(),
    DisplayMember="Label",ValueMember="Code",AccessibleName=L.T("Lọc theo trạng thái")};
statusBox.SelectedIndex=0;AddFilter(statusBox,170);
```

- `ComboBox.DataSource` chỉ nạp `Items` khi control có `BindingContext`. ComboBox vừa
  tạo chưa có control cha (chưa vào `OrbitFilterBox` trong `AddFilter`), nên
  `Items.Count == 0` tại dòng 59.
- `ComboBox.SelectedIndex` setter gọi `ThrowIfGreaterThanOrEqual(0, Items.Count)` →
  ném đúng thông điệp trên khi `Items.Count == 0`.
- Các màn khác không lỗi vì dựng item bằng `Items.AddRange(...)` trước khi chọn
  (`FinanceControl.cs:28`, `EmployeesControl.Choice`), không dùng `DataSource`.
- Stack trace lấy từ nút "Details" của hộp lỗi (bằng chứng đầy đủ, không suy diễn).

### Kiểm chứng cơ chế (thí nghiệm cô lập)

Script `C:\Users\Lenovo\.qoder\tmp\cb-test2.ps1`, `cb-test3.ps1` (WinForms, .NET 10):

- ComboBox **chưa có cha** → gán `DataSource` → `Items.Count=0` → `SelectedIndex=0`
  ném đúng lỗi trong BUG-01. Ở trong form (có `BindingContext`) → `Items.Count=2` và
  `SelectedIndex=0` chạy bình thường.
- `List<ValueTuple<string,string>>` + `DisplayMember="Label"` **không bind được**:
  dựng combo khi đã có cha thì `DisplayMember` ném "Cannot bind to the new display
  member"; dựng trước khi có cha rồi gắn vào cha thì `Items` vẫn rỗng và `SelectedIndex`
  lỗi y hệt. `ValueTuple` chỉ có field, không có property để binding.

Hệ quả thứ hai (cùng gốc): hộp Sửa khuyến mãi (`LoyaltyControl.cs:99`) cũng dựng combo
Trạng thái từ `ValueTuple` + `DataSource`. Khi màn mở được, combo này nhiều khả năng
rỗng nên không chọn được "Áp dụng"/"Tạm ngưng" và lúc lưu sẽ không lấy được giá trị.
Cần kiểm lại sau khi sửa; các màn khác dùng record có property
(`StatusOption`, `AreaRow`, `NameLookup`, `ReportOption`) nên không bị.

### Đề xuất sửa (chờ duyệt)

1. Thay danh sách `ValueTuple` bằng một record có property, ví dụ
   `sealed record StatusChoice(string Code, string Label);` (đặt private trong
   `PromotionList`), dùng cho **cả** combo lọc (dòng 58) và combo Trạng thái trong
   `Edit` (dòng 99). `DisplayMember=nameof(StatusChoice.Label)`,
   `ValueMember=nameof(StatusChoice.Code)`.
2. Với combo lọc: gọi `AddFilter(statusBox,170)` **trước** `statusBox.SelectedIndex=0;`
   (combo phải có cha/BindingContext thì `Items` mới nạp).
3. Đổi chú thích "(045)" thành "(047)" trong `LoyaltyControl.cs` và
   `LoyaltyService.cs` cho khớp migration thực tế.

### Phạm vi ảnh hưởng

Chỉ file `src/CafeManager.WinForms/Modules/Loyalty/LoyaltyControl.cs` (2 combo) và 2
chú thích. SQL 047, DAL, BLL và luồng CASH đã PASS ở mức dữ liệu.

### Rủi ro

Thấp; sửa trong 1 file UI. Sau khi sửa phải chạy lại toàn bộ ca UI của đợt test
(lọc trạng thái, double-click/F2, đổi trạng thái trong hộp sửa, xóa tại dòng, tìm kiếm
kết hợp, trang 1 khi đổi lọc).

### Kết quả sửa (đã duyệt, 25/11/2026)

- Cách sửa thực tế khác đề xuất ban đầu ở điểm 2: thí nghiệm lại cho thấy `AddFilter`
  **không** tạo `BindingContext` khi control chưa gắn vào form, nên record + `DataSource`
  vẫn rỗng lúc khởi tạo (vẫn ném lỗi y hệt ở dòng 60). Đã bỏ `DataSource`, dùng đúng
  mẫu nhà `Items.AddRange(...)` + mảng mã song song `statusCodes` cho cả hai combo:
  - combo lọc: `statusBox.Items.AddRange(labels)`, `statusBox.SelectedIndex=0;AddFilter(...)`,
    `Status()` đọc `statusCodes[statusBox.SelectedIndex]` (guard chỉ số).
  - combo Trạng thái trong hộp Sửa: `foreach(choice in statuses)status.Items.Add(choice.Item2)`,
    đọc theo `SelectedIndex` (0 = Áp dụng, 1 = Tạm ngưng).
- Chú thích "(045)" đã đổi thành "(047)" ở `LoyaltyControl.cs` và `LoyaltyService.cs`.
- Lỗi phụ phát hiện ngay trong lần chạy lại: `Status()` lấy `statusCodes[SelectedIndex-1]`
  làm lệch một bậc (Áp dụng → tất cả 17 dòng; Tạm ngưng → 3; Chưa tới kỳ → 0; Hết hạn → 2).
  Đã sửa thành `statusCodes[SelectedIndex]`; chạy lại đúng 3/0/2/12/17 và khớp SQL P-02…P-06.
- Kiểm chứng sau sửa: màn mở bình thường, 5 nhãn lọc, mặc định "Tất cả", double-click/F2
  mở đúng hộp sửa, xóa thùng rác có xác nhận, tạo/sửa/xóa một KM mã số hợp lệ tròn vòng
  trên DB test (17 → 18 → 17 dòng), không lỗi nào khác. Chi tiết ở `test-results.md`.

## Ghi nhận phụ (không phải lỗi chức năng)

- Chú thích còn số migration cũ: `LoyaltyControl.cs` ("do SQL tính theo ngày server
  (045)") và `LoyaltyService.cs:60` ("Lọc trạng thái hiệu lực (045)") trong khi
  migration thực tế là 047. Đề xuất sửa chú thích cùng lần fix BUG-01.
- Quy tắc trạng thái năm lặp hằng năm: KM 20/12–01/01 (KM26XMAS) hôm nay 25/11 hiển
  thị "Hết hạn" chứ không phải "Chưa tới kỳ". Đây là hành vi có từ 042
  (`fn_NgaySauKy` coi chu kỳ trong năm đã qua là hết hạn), 047 giữ nguyên. Không đổi
  trong đợt này; nếu muốn "sắp tới kỳ" thì cần quyết định nghiệp vụ riêng.
- Hộp xác nhận xóa dùng mẫu `OperationEditorDialog` như các màn master data khác: nút
  chốt tên "Lưu" (kèm "Hủy"/"Đối chiếu"), sau khi bấm hiện hộp xác nhận lồng
  "Xóa <mã>?" với "Đồng ý"/"Hủy". Giữ nguyên cho đồng bộ; nếu muốn đổi nhãn thành
  "Xóa" thì là quyết định UI chung cho mọi màn, ngoài phạm vi task này.

## BUG-02 — Không lưu được khuyến mãi mã cũ (KM24BF, KM24NEL, KM25xxx…), lỗi chung "Thông tin ưu đãi/chính sách/điểm không hợp lệ"

- Mức độ: **cao** với thao tác Sửa; không chặn mở màn hay các ca khác của task.
- Trạng thái: **đã sửa (migration 048), đã kiểm SQL + UI thật** — xem "Kết quả sửa"
  cuối mục; lỗi có sẵn từ 042, không do thay đổi UI của task này.
- Môi trường: DB test `CoffeeManager_Test_Promo_20261125` (schema 47), `admin` · ADMIN.

### Hiện tượng

Mở "Sửa khuyến mãi · KM24NEL", đổi Trạng thái sang "Tạm ngưng", bấm Lưu → hộp sửa
vẫn mở, hiện dòng lỗi "Thông tin ưu đãi/chính sách/điểm không hợp lệ hoặc chương
trình không hiệu lực."; DB không đổi (`TrangThai` vẫn `DangChay`).

### Nguyên nhân và bằng chứng

- Gọi thẳng `sp_PromotionUpdate` trên DB test với đúng payload UI gửi
  (KM24NEL, Status=TamDung, 20/12–31/12, 15%, ALL, Giam) trả:
  `Msg 52001 ... Procedure dbo.sp_PromotionWrite, Line 14` / "Mã KM và 8 chữ số."
- `database/migrations/042_promotion_v2.sql:285` kiểm mã vô điều kiện:
  `DATALENGTH(@Code)<>10 OR @Code NOT LIKE 'KM[0-9]…[0-9]'` → mọi mã cũ
  (KM24BF, KM24NEL, KM25xxx, KM26HRO…) bị chặn ở `Update`/`Delete`.
- Migration 009 từng cho phép giữ mã cũ khi sửa/xóa (chỉ áp định dạng mới cho
  `Create`): `database/migrations/009_promotion_legacy_codes.sql:13`. Bản 042
  (được viết lại proc) làm mất điều kiện `@Action='Create'` → hồi quy.
- UI chỉ hiện thông điệp chung vì `SqlErrorMapper` ánh xạ 52001 thành câu chung
  (`src/CafeManager.DataAccess/SqlErrorMapper.cs:110`), không lộ chi tiết SQL.
- Đối chứng: KM mã số hợp lệ `KM00000001` tạo qua nút Thêm rồi sửa trạng thái lưu
  bình thường (P-22, `test-results.md`) → phần UI/DAL/BLL của task không lỗi.

### Đề xuất sửa (đã duyệt và áp dụng)

Thêm migration mới (ví dụ 048) `ALTER PROCEDURE dbo.sp_PromotionWrite` chỉ siết
`KM+8 số` khi `@Action='Create'` như 009, giữ nguyên định dạng cũ khi
`Update`/`Delete`/`Pause`/`Resume`; áp lên DB dev bằng `Apply-Migration.ps1`.
Rủi ro thấp (1 điều kiện trong 1 proc), nhưng phải chạy lại nhóm ca UI sửa/xóa và
các ca SQL của đợt để hồi quy. Ngoài ra có thể tách thông điệp 52001 thành câu cụ
thể hơn — đề xuất riêng, không bắt buộc.

### Kết quả sửa (đã duyệt, 25/11/2026)

- `database/migrations/048_promotion_legacy_code_edit.sql`: chép nguyên
  `sp_PromotionWrite` của 042, chỉ đổi điều kiện mã thành
  `DATALENGTH(@Code)>10 OR DATALENGTH(@Code)=0 OR (@Action='Create' AND (định dạng KM+8 số))`;
  giữ GRANT cho Write/Create/Update/Delete. Precondition: `SchemaMigrations` có 47 và
  đủ 4 proc khuyến mãi của 042. Áp dụng bằng
  `tools/database/Apply-Migration.ps1 -Version 48` trên DB test và
  `CoffeeManager_Development` (cùng checksum, `Status=Applied`).
- Kiểm SQL trên DB test (`v48.sql`): V01 sửa KM24NEL → `TamDung` OK (mã cũ);
  V02 trả lại `DangChay` OK; V03 Pause/Resume KM24BF OK; V04 tạo mới mã cũ
  `KM26BF` vẫn bị chặn 52001 "Mã KM và 8 chữ số"; V05 sửa mã không tồn tại bị chặn
  51603; V06 tạo/xóa mã số `KM20260001` tròn vòng, tổng KM về 17.
- Kiểm UI thật (build Debug tách riêng, DB test): mở "Sửa khuyến mãi · KM24NEL",
  chọn "Tạm ngưng" → Lưu thành công, hộp đóng, lưới hiện "Tạm ngưng", DB `TamDung`;
  mở lại chọn "Áp dụng" → Lưu thành công, lưới "Chưa tới kỳ" (hiệu lực 20/12, đúng
  SQL), DB `DangChay`. Lọc trạng thái sau đó vẫn 3/0/2/12/17.
- Không đổi DAL/BLL/UI; không đổi thông điệp 52001 (đề xuất riêng còn mở).
