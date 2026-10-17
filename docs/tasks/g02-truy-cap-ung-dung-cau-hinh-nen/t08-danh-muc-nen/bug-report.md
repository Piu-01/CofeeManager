# G02 / T08 — Đợt bug phát hiện trong nghiệm thu

**Trạng thái cuối17/10/2026:** B01/B02/B06 đãsửa/regressionPASS; mọi subcaseT08
đãđốichiếuPASS,khôngcònblockerhiệnhành. Lịch sửFAIL dướiđây vẫngiữ;
xem`../acceptance-matrix.md`.

Trạng thái: **B01/B02 và B06 đã sửa, regression cuối PASS ngày16/10/2026**.
Người dùng “xác nhận” duyệt đợt debug gồm regression. Giữ nguyên chứng cứ
FAIL trước sửa ở dưới; source bản sửa được đóng manifest riêng khi kiểm thử.
Ảnh hưởng T08 và các ca UI dùng chung T07. G02 chưa nghiệm thu đủ27ID gốc.

## Kết quả sau sửa

| Bug | Thay đổi đúng nguyên nhân | Regression |
|---|---|---|
| B01 | CatalogsControl khóa Tìm/Tải trong loading; guard trước thay skip và trước tạo request; Tải vẫn dùng được khi unknownDelete cần đối chiếu |5/5native PASS:clicklặp/handlerdirect, cancel/retry, timeout/retry, unknown-state reconciliation, logoutpending |
| B02 | Procedure nhận Name nvarchar(max), Supplier Phone varchar(max), giữ validation và kích thước cột; Create/Update đều dùng template sửa |79test boundary mớiPASS +10ca SQLphone riêngPASS; toàn runner196/196PASS |

Evidence: `../runs/20261011_171039_f2e46bea/`,
`../runs/G02_B01_REGRESSION_20261011/`,
`../runs/G02_B02_PHONE_CONFIRMED_20261011/`;
manifest/schema và snapshot harness tại
`../runs/G02_B01_B02_REGRESSION_20261011/`.
Debug/Release build sản phẩm0warning/0error; source97file không đổi từ đầu
runner đến sau native/SQL. Migration003checksum mới chỉ chạy trên DBtest mới.
Không chỉnh checksum các DB giữ chứng cứFAIL cũ. Chưa commit/push.

Ca native unknownDelete dùng fixture trạng thái của control và Get/refresh
thật; không thay ca ngắt mạng sau commit của nghiệm thu còn NOT RUN.
Lượtphone đầu bị lỗi harness PowerShell biến `$action` của Check che biến
Create/Update:SQL syntax error trước procedure. Đã giữraw8FAIL/2PASS tại
`G02_B02_PHONE_REGRESSION_20261011`; sửa tên biến chỉ trong harness bên ngoài,
dọn3NCCfixture vừa tạo trong DBtest và chạy lượt mới. Product/expected không
đổi giữa các lượt; lượt xác nhận10/10PASS. Không tính lỗi harness là bugapp.

## G02-B01 — Nút Tìm vẫn gửi read mới khi đang loading

- **Hiện tượng:** khóa TAIKHOAN bằng session SQL thứ hai, bấm Tìm rồi bấm lặp.
  Tìm vẫn enabled và có thêm lần mở connection/recheck. Ca T07-K05.a FAIL.
- **Bằng chứng:** `../runs/G02_NATIVE_125_20261009/native-results.json`,
  `T07-K05.a.repeat-read`: enabled=True, opens=2 (expected1). Lượt100/150 giữ
  cùng source cũng lưu kết quả riêng. Các opens này đếm SessionInitializer
  của runtime với Pooling=false; không gọi là số query List nếu chưa đo RPC.
- **Nguyên nhân đã có bằng chứng:** `CatalogsControl.cs:39–46` không cập nhật
  searchButton khi loading; reloadButton chỉ xét writing/editor, cũng chưa
  xét loading. `Search_Click:99` đi thẳng RefreshRows; `RefreshRows:50–53`
  không chặn loading mà cancel request cũ và tạo request mới. Request version
  có thể chặn bind muộn nhưng không ngăn lời gọi read/recheck gửi lặp.
- **Đề xuất:** khóa Tìm/Tải trong read đang chạy và guard handler trước khi
  thay filter/skip; mở lại sau completion/error/cancel. Giữ nút đối chiếu cho
  unknown-write đúng contract; không làm hỏng logout/điều hướng hủy read.
- **Phạm vi:** CatalogsControl, regression loading/10click/filter/retry và
  logout khi SQL lock, T07-K05/T08-K01. Không thay quyền hoặc dữ liệu.
- **Rủi ro thực tế:** không được khóa luôn nút đối chiếu sau timeout ghi hoặc
  để nút đọc bị khóa vĩnh viễn sau error/cancel.

## G02-B02 — Stored procedure NCC cắt input vượt giới hạn

- **Hiện tượng:** gọi procedure với parameter Name101 hoặc Phone16, SQL lưu
  Name100 / Phone15 thay vì từ chối. C# validator chặn trước DAL, nhưng contract
  SQL direct bắt buộc cũng không được âm thầm truncate. T08-K02 FAIL.
- **Bằng chứng xác nhận:** `../runs/G02_EXTRA_20261009/extra-results.json`:
  `T08-K02.Supplier.maxplus1.SQL.confirm`,
  `T08-K02.ncc.length16.SQL.confirm` đều FAIL (expected rejection absent).
  Dòng `T08-K02.ncc.truncation.actual` ghi phép đo độ dài persisted100/15;
  đó là evidence lỗi, **không** là PASS nghiệp vụ. DB riêng
  `CoffeeManager_Test_G02_20261009_214859_40b6b849`, NCC84/NCC85.
- **Nguyên nhân:** migration003 khai báo generated procedure @Name nvarchar100
  và Supplier @Phone varchar15. SQL chuyển/cắt tham số vào procedure trước
  khi chạy LEN/DATALENGTH trong body. Input ban đầu vượt giới hạn đã bị mất
  nên validation trong procedure không còn phát hiện được.
- **Đề xuất:** nhận input đủ rộng tại boundary procedure, chuẩn hóa/kiểm độ
  dài/charset trước khi INSERT/UPDATE vào cột100/15. Giữ nguyên schema cột,
  DTO typed, BLL validation và không đổi giá trị expected của các ca biên.
  Rà soát Create/Update sinh từ cùng template; không tự mở rộng sang task khác.
- **Phạm vi:** migration003 chưa phát hành và procedure danh mục nhận Name;
  Supplier Phone. Chạy bản sửa trên DB test mới, không rewrite checksum ở DB
  đã áp dụng bản cũ. Regression7kind CRUD/revision, required/max/max+1,
  NCC NULL/rỗng/duplicate/15/16/0đầu và transaction sau lỗi.
- **Rủi ro thực tế:** phải reject trước binding/cắt dữ liệu; giữ normalization
  và unique/FK hiện tại. BLL pin precision/scale không được nới lỏng theo fix.

## G02-B06 — Designer của CatalogEditor/CatalogsControl không mở được

Trạng thái: **đã duyệt và sửa B06; regression cuối PASS**. B01/B02 vẫn giữ
trạng thái đã sửa/regressionPASS. Mô tả hiện tượng trước sửa bên dưới giữ nguyên.

- Expected: mở/save/close/reopen editor và control danh mục, giữ layout/handler.
  Actual: cả2surface báo “designer cannot process the code at line47”.
- Evidence: `../runs/G02_DESIGNER_SOLUTION_20261011/` ảnh
  CatalogEditorDialog-designer.png và CatalogsControl-designer.png. Solution
  clone đúng đã mở, không suy từ captionDesign hoặc build thành công.
- Nguyên nhân: CatalogEditorDialog.Designer.cs:47 và
  CatalogsControl.Designer.cs:47 dùng chained assignment nhiều control;
  các dòng cùng initializer dùng cú pháp tương tự. Parser dừng ngay tại đó.
- Đề xuất: thay chained assignment bằng từng assignment tương đương trong
  hai initializer; chỉ xử lý cú pháp cùng nguyên nhân. Giữ fields, handlers,
  rows/columns, typed bindings, visibility, keyboard và AppTheme runtime.
- Regression: Designer round-trip thật + runtime sauSave,7tab/editor CRUD,
  validation/cancel/X, loading/sendlock, conflict/reconciliation và3DPI.
- Rủi ro: thay thứ tự khởi tạo làm mất event/binding hoặc DPI layout; không
  chuyển logic nghiệp vụ vào Designer, không đổi expected để tránh FAIL.

### Các kết quả chưa đủ chứng cứ để gọi là bug ứng dụng

### B06 — Regression lượt1 sau sửa, ngày16/10

Source bản sửa đóngmanifest ở runner `20261016_204843_97e5f439`.
Designer mở được, clone sauSave CRUD23/23PASS vàlayout100/125%12/12PASS;
150% có2viewportFAIL. Diagnostic tại
`../runs/G02_B04_B06_AFTER_SAVE_DIAG_144_20261016/`: grid886×5,
TabPage.Y235 trongTabControl244px dùheaderchỉ1hàng, ảnh thể hiện vùnggrid trắng.
GiữFAIL, không sửa expected. Kết thúc lượt1, quay lại debugB06 đãđượcduyệt.
Rà thay đổi thêmColumnStylePercent ởroot củacontrol: bản trước làAutoSize
ngầm. Bước sửa tiếp theo giữAutoSize tường minh thayPercent để không thay
semantics layout cũ trong lúc làm initializer an toàn choDesigner. Phải
nghiệm thu lại150% vàsauSave, không suyPASS từ bản sửa.

### B06 — Kết quả lượt cuối

Đã giữColumnStyleAutoSize tường minh, tương đươngsemantics mặc định trước
sửa, thay vìPercent gây regression. Đây là sửa phầnlayout phát sinh từ cùng
đợtB06; kết thúc lượt1 trước khi sửa, không vá trong lượt test.

- Runner cuối `20261016_211207_a26a8e4f`:222/222PASS.
- Designerfinal5surface mở đúngproject,save/close/reopen; đãxemảnheditor/
  catalogssurface thật. Các chainedassignment đãtách, AddRange dùngarray rõ.
- `G02_B03_B06_FINAL_CRUD_20261016`:23/23nativePASS từbinaryclone sauSave.
- `G02_B03_B06_FINAL_LAYOUT_{96,120,144}_20261016`:mỗi12/12PASS, gồm
 6viewportđúngsize+minimize/restore,21editorSavevisible vàkeyboard3DPI;
  grid150%khôngcònco5px. RawFAILlượt1/diagnostic giữ nguyên.
- Handofflưusource/binaryhash, runtimebinarySHA256 khớpclone sauSave;
 98filesource/migration/test/toolkhôngđổi tronglượt cuối.

Các lỗiCOMbusy/TypeGuid của driver IDE trunggian được giữ riêng; driver
cuối dùngđúnghierarchyWinForms/startupproject vàcaptureHWND củaDTEđãchọn.
Khônggán chúngthànhbugapp hoặc dùngcaptionOBSERVED đểbáoPASS.

- Lượt PowerShell SQL đầu có nhiều FAIL đo transaction: SELECT gọi trực tiếp
  XACT_STATE cho0:1 trên engine này trong autocommit statement. Phép đo đúng
  DECLARE trước SELECT trong helper G01 và lượt Extra cho0:0. Giữ raw FAIL,
  ghi là lỗi đo; không đề xuất sửa transaction app từ kết quả đó.
- CASH/STORE ở lượt native đầu chưa chờ Shown/recheck, gây content index0 lỗi
  trong harness. Lượt UI_EXTRA chờ content ready đã PASS đúng menu/điều hướng.
- So sánh sys.partitions rowcount ở cancel TC là phép đo metadata chưa đủ
  khẳng định app ghi; lượt exact-cancel bị thao tác modal chặn. Ca này giữ
  BLOCKED chờ native thao tác đáng tin cậy, không ghi bug “Hủy ghi dữ liệu”.
- Kết nối lỗi được dịch thành DataAccessException; probe yêu cầu đúng Kind
  Connection nhưng có thể gặp Timeout. Chưa có chứng cứ login thành công hoặc
  thông báo sai password, nên không gọi là bug mapping.
- Designer automation không xác nhận được đúng document clone, đã từ chối
  Save; không ghi bug Designer từ một cửa sổ G01 bị chụp nhầm.
