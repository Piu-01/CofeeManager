# Plan T02 — Chạy và kiểm chứng SQL baseline

**Trạng thái:** Đã triển khai; T02-K01–K12 PASS trên SQL Server thật; commit bàn giao chờ cho phép.  
**Duyệt plan:** Người dùng xác nhận “duyệt” ngày 08/10/2026.  
**Ngày:** 08/10/2026.  
**Nhóm:** G01 — requirements đã được duyệt.  
**Phụ thuộc:** T01 có đủ 7/7 ca PASS; commit bàn giao còn chờ cho phép.  
**Nguồn chuẩn:** Kế hoạch phiên bản 3.1, T02-K01–K12; requirements G01.

**Lịch sử:** Các bước test/fix ngay dưới đây đã thực hiện theo quy trình cũ.
Từ lần chốt lại quy trình, việc chạy mới tuân theo `../plans.md`: duyệt bộ plan,
triển khai tuần tự, duyệt đợt test riêng, báo cáo rồi duyệt đợt bug trước debug.
Kết quả cũ được giữ nguyên; phần quy trình cũ không là quyền tự chạy test/fix mới.

## 1. Lựa chọn triển khai

- Runner PowerShell tại `tools/database/Test-Baseline.ps1`, dùng
  `System.Data.SqlClient` có sẵn trong Windows PowerShell để thực thi SQL và
  giữ connection. Đây là công cụ kiểm chứng T02, không phải DAL của app; DAL
  chính thức vẫn dùng Microsoft.Data.SqlClient ở các task sau.
- SQL test tại `database/tests/t02/`: helper/fixture và script từng nhóm ca.
  Không thêm procedure/view/type nghiệp vụ tương lai vào baseline.
- Windows Authentication đến `localhost`; TrustServerCertificate cho local đã
  duyệt ở T01. Không log chuỗi kết nối chứa secret; cấu hình runner tách runtime.
- Hai DB test mới cho mỗi lần chạy: một DB **baseline** giữ seed để đối chiếu,
  một DB **fixture** để thử tình huống lỗi và thay đổi nhiều dòng.
  Tên đều có dạng `CoffeeManager_Test_T02_<role>_<timestamp>_<suffix>`.
- Kết thúc giữ DB test có tên ghi trong biên bản để tra cứu; runner không có
  thao tác tự DROP/reset DB cũ. Chạy lại dùng tên mới.

## 2. File/khu vực dự kiến

| Đường dẫn | Nội dung |
|---|---|
| `tools/database/Test-Baseline.ps1` | Setup DB/batch GO, runner/assertion, bắt lỗi, kiểm tra transaction và thu kết quả |
| `database/tests/t02/fixtures.sql` | Fixture có mã riêng, tạo dữ liệu hợp lệ cho từng subcase |
| `database/tests/t02/catalog-seed.sql` | K01–K05: catalog, seed, tổng/phân bổ, seed chạy lại |
| `database/tests/t02/constraints.sql` | K06–K07: FK/CHECK/UNIQUE/NULL |
| `database/tests/t02/batch-triggers.sql` | K08: INSERT/UPDATE/DELETE nhiều dòng, header/group và delta kho |
| `database/tests/t02/procedure-cases.sql` | K09–K12: procedure, snapshot trước/sau, lỗi và rollback |
| `docs/tasks/g01-nen-tang-ky-thuat-du-lieu/t02-sql-baseline/test-results.md` | Expected/actual, mã subcase, PASS/FAIL/BLOCKED/NOT RUN và bằng chứng |
| `docs/tasks/g01-nen-tang-ky-thuat-du-lieu/t02-sql-baseline/test-results.json` | Kết quả máy đọc được, DB/engine/hash baseline, lỗi SQL và dữ liệu đối chiếu |
| `docs/tasks/g01-nen-tang-ky-thuat-du-lieu/t02-sql-baseline/bug-report.md` | Chỉ tạo khi phát hiện lỗi, ghi nguyên nhân/fix/regression |
| `docs/KeHoach_QuanLiQuanCafe.md` | Trạng thái và tick ca thực sự PASS của T02 |

Các helper/assertion nằm trong runner hoặc batch test; không tạo framework test
chung lớn hoặc test C# nghiệp vụ trước T04/T05. File SQL có thể tách thêm theo
procedure nếu dài; phạm vi và số ca vẫn như ma trận ở dưới.

## 3. Thứ tự thực hiện và verify

### Bước 1 — Chuẩn bị runner và guard DB test

- Đọc toàn bộ hai baseline, lấy SHA-256 và phiên bản engine.
- Kiểm tra instance local và quyền setup đã nghiệm thu; tạo tên DB test mới,
  chỉ chấp nhận tên tiền tố T02 đã quy định, từ chối nếu tên tồn tại.
- Tạo bản thực thi trong bộ nhớ chỉ thay đúng câu `CREATE DATABASE` và `USE`
  hardcode tên QuanLyCafe_ERP_V2 sang DB test. Xác nhận số lần thay mong đợi;
  không thay chữ trong dữ liệu, câu lệnh nghiệp vụ hoặc file nguồn.
- Tách batch tại dòng GO của baseline, thực thi tuần tự cùng connection; lỗi
  batch được ghi tên file/số batch/lỗi SQL, dừng setup DB đó ngay.
- Kiểm tra `DB_NAME()` đúng DB mục tiêu trước các thao tác test; không chấp nhận
  system DB hoặc DB ứng dụng. Query quyền/version dùng connection master riêng.

**Verify:** Script parse được; setup không dùng DB hiện có, xử lý GO đúng thứ tự;
không có tên DB ứng dụng trong lệnh thực thi sau đổi CREATE/USE.

### Bước 2 — Chạy baseline/seed và kiểm chứng dữ liệu gốc

- Chạy schema rồi seed một lần trên mỗi DB mới; không bọc thêm transaction ngoài
  seed/procedure vì chúng đã sở hữu transaction.
- Kiểm tra catalog đủ bảng/view/procedure/type; đối chiếu dependency/compile.
- Đọc tổng, hóa đơn mẫu, phân bổ, trạng thái bàn, tồn/điểm/nợ và bất biến seed.
- Chạy thử seed lần hai trên DB baseline, bắt lỗi 51200; chụp dữ liệu toàn bộ
  bảng trước/sau và đối chiếu không đổi, kiểm tra transaction sau lỗi.

**Verify:** K01–K05 đúng expected; lỗi seed lần hai là lỗi được mong đợi,
không bị báo FAIL nếu mã lỗi/dữ liệu/transaction đều khớp.

### Bước 3 — Test ràng buộc và trigger nhiều dòng

- Dùng fixture riêng trong DB fixture, kiểm thử FK/CHECK/UNIQUE/NULL.
- Batch món nhắm ít nhất hai hóa đơn; batch nhập/xuất nhắm ít nhất hai header
  và nhiều NVL, có NVL chung để kiểm tra SUM theo nhóm và inserted−deleted.
- Đối chiếu lượng/tổng bằng giá trị expected trước/sau từng INSERT/UPDATE/DELETE.
  Khi thao tác giảm tổng món, giảm GiamGia trước theo contract baseline.
- Thử batch có một dòng vi phạm để xác nhận không còn thay đổi dở.

**Verify:** K06–K08 đúng từng subcase. Không dùng chỉ số dòng affected như bằng
chứng số dư đúng; đọc lại tồn/tổng thực tế.

### Bước 4 — Test procedure thành công, lỗi và rollback

- Tạo fixture hợp lệ, commit setup trước khi gọi procedure; không giữ outer
  transaction vì CATCH baseline có thể ROLLBACK toàn session.
- Snapshot bảng liên quan trước lời gọi, sau lỗi đọc lại bằng **cùng connection**:
  `@@TRANCOUNT`, `XACT_STATE()` và dữ liệu. Assertion diễn ra trước cleanup.
- Snapshot so sánh giá trị hàng/cột có thứ tự xác định, không chỉ row count hoặc
  CHECKSUM_AGG. Identity next value có thể tăng dù rollback, không đòi rewind;
  dữ liệu đã lưu/tồn/điểm/trạng thái mới không được tồn tại sau lỗi.
- Với lỗi mong đợi, đối chiếu đúng error number/ràng buộc; test FAIL nếu không
  có lỗi hoặc bị chặn bởi một nguyên nhân khác chưa tới nhánh cần kiểm tra.
- Kiểm thử failure ở giai đoạn muộn bằng trigger gây THROW chỉ trong DB fixture:
  điểm checkout, lịch sử trả nợ, header kiểm kê đã điều chỉnh và header hủy đơn.
  Trigger test có tên riêng, bỏ sau ca trong finally; không đưa vào baseline.
- Tiếp tục các ca độc lập sau FAIL; ca bị lỗi setup chặn phải ghi BLOCKED, không
  tick PASS. Khi xử lý lỗi nội bộ, kết thúc lượt test rồi sửa/regression lượt mới.

**Verify:** K09–K12 có dữ liệu đối chiếu đủ, mọi nhánh lỗi giữ transaction hợp lệ
và không tác động dở. Lượt test lưu cả lỗi ban đầu và kết quả sau xử lý.

### Bước 5 — Chạy ngay, lưu kết quả và chuẩn bị plan T03

- Chạy runner trên SQL Server thật ngay sau triển khai, không xin duyệt test riêng.
- Ghi JSON/Markdown: thời điểm, DB test, engine, hash source, mã ca/subcase,
  input/lệnh, expected/actual, lỗi, snapshot và trạng thái transaction.
- Fix nội bộ để đạt contract hiện có được điều tra/sửa/regression trong scope.
  Thay đổi nghiệp vụ hoặc mở rộng contract thì xin duyệt phần phát sinh.
- Nếu cần sửa baseline khi còn DB test mới, ghi diff/hash trước/sau và lý do;
  chạy lại setup/seed/test trên DB mới. Không sửa seed để né ràng buộc.
- Khi đủ K01–K12 PASS, cập nhật trạng thái task, chuẩn bị plan T03. Báo cáo kết
  quả tổng hợp khi G01 hoàn tất, không báo cáo nghiệm thu riêng T02.
- Commit/push vẫn cần cho phép hiện hành; không tự suy diễn duyệt plan là duyệt
  thao tác Git. Hồ sơ ghi trạng thái commit thực tế.

**Verify:** Có đủ biên bản từng ca; test chưa chạy/FAIL vẫn hiện đúng trạng thái;
không đánh dấu G01 hoàn tất khi mới xong T02.

## 4. Ma trận T02-K01–K12 và subcase

| Ca | Fixture/thao tác | Expected |
|---|---|---|
| K01 | Catalog sau schema; kiểm tra module/dependency SQL | 35 bảng, 2 view, 4 procedure, 1 TVP; đối tượng hợp lệ |
| K02 | Seed sau COMMIT trước test biến đổi | 10 HD, 18 dòng món, 9 đã trả, 1 mở B0003; bàn dẫn xuất đúng |
| K03 | Đọc HD20250401/HD20250409 và các dòng TT | 112.500 và 81.000; HD09 phân bổ 30.000 + 51.000 |
| K04 | Đối chiếu mọi HD/PN/TT; tồn không âm và hạng/điểm seed | Tổng chi tiết khớp, đơn đã trả đủ tiền, không nhân số tiền khi JOIN |
| K05 | Snapshot toàn DB baseline; seed lần hai | Lỗi 51200, dữ liệu toàn bộ bảng không đổi, transaction không mở |
| K06 | FK không tồn tại; tiền âm; lượng 0; KM ngày ngược/cùng ngày | FK/CHECK đúng bị chặn; chương trình cùng ngày lưu được |
| K07 | Hai khách và hai NV với SDT NULL; SDT thật trùng | NULL lưu được; SDT trùng bị unique chặn, không mất hàng có sẵn |
| K08 | Batch món/nhập/xuất qua nhiều header/NVL: insert, update, delete | Tổng/delta từng nhóm đúng; batch lỗi rollback toàn statement |
| K09 | Đơn có KH, nhiều NVL: thiếu một NVL; phương thức FK sai | Kho/tiêu hao/tiền/điểm/hạng/header trước/sau không đổi; không TT rác |
| K10 | Trả nợ vượt; thành công một phần; sửa lịch sử/chi liên kết | Vượt rollback chi/lịch sử, số dư giữ; lần hợp lệ đúng; lịch sử không sửa/xóa |
| K11 | KK hôm nay snapshot khớp, số thực lệch; chốt lại/ngày cũ/snapshot lỗi thời | Tồn điều chỉnh một lần; các lần từ chối không đổi tồn/header/chi tiết |
| K12 | Nhánh lỗi của cả bốn procedure và lỗi SQL/trigger gây rollback | Mỗi subcase xác nhận đúng lỗi, @@TRANCOUNT=0/XACT_STATE()=0 và không dữ liệu dở |

### Bao phủ nhánh lỗi K12

- **sp_ThanhToanHoaDon:** không tồn tại/đã chốt/đã hủy (51101); rỗng (51102);
  có TT trước (51103); tổng TVP thiếu/vượt (51104); KM hết hiệu lực (51105);
  món ngừng bán (51106); thiếu công thức (51107); ngày TT trước lập (51108).
  Thêm lỗi kho CHECK, phương thức FK và lỗi cưỡng bức khi ghi điểm sau chốt.
- **sp_HuyHoaDon:** không tồn tại/đã chốt/đã hủy (51109); đơn mở đã có tiền
  hoặc tiêu hao bị trigger từ chối (51006); lỗi cưỡng bức khi update header.
- **sp_TraCongNo:** nợ không tồn tại (51110); trả 0/âm/vượt còn nợ (51111);
  lỗi FK nhân viên/phương thức, trùng MaTC, thiếu/sai loại TC011 trong fixture;
  lỗi cưỡng bức sau tạo phiếu chi khi ghi liên kết trả nợ.
- **sp_ChotKiemKe:** phiếu không tồn tại/đã chốt/ngày cũ/ngày tương lai (51112);
  phiếu rỗng (51113); snapshot lệch một NVL trong phiếu nhiều dòng (51114);
  lỗi cưỡng bức khi chốt header sau cập nhật tồn.

Setup mỗi nhánh thỏa các kiểm tra đứng trước, để lỗi observed thực sự kiểm
chứng nhánh mục tiêu. Ghi subcase liên kết K09/K10/K11 vào K12, tái sử dụng
bằng chứng cùng lời gọi thay vì thực thi lặp vô ích.

## 5. Điểm duyệt

Lượt nghiệm thu: `20261008_205557_93421dd4`, kết quả chi tiết tại
`test-results.md`, `test-results.json` và `runs/`. Hai baseline nguồn giữ nguyên
hash. Helper được đặt trong schema dbo của DB fixture (prefix T02_) thay vì
temporary procedure vì TVP không resolve từ tempdb; chi tiết lỗi/regression ở
`bug-report.md`. Lệnh chạy: `& .\tools\database\Test-Baseline.ps1`.

Duyệt plan này một lần cho triển khai và toàn bộ kiểm thử T02 trên DB test mới.
Chưa chạy baseline ở bước chuẩn bị plan. Task T03 chỉ triển khai sau T02 đạt đủ
điều kiện nghiệm thu và plan T03 được duyệt; không gộp scope T03 vào T02.
