# Plan T05 — DAL, xử lý lỗi và nền kiểm thử

**Trạng thái:** 10/10 ca nguyên bản PASS kế thừa; runner-failure và regression B09/B10 PASS trên bản sau Save Designer. G01 đủ kiểm chứng kỹ thuật, chờ cho phép commit bàn giao.  
**Phụ thuộc:** T04; SQL contract/quyền T03 dùng làm đầu vào, kiểm chứng ở đợt test.  
**Nguồn:** Kế hoạch 3.2/T05; UX mục 15–17; schema/seed và migration T03.

## 1. Phạm vi và file

| File/khu vực | Đầu ra |
|---|---|
| `src/CafeManager.DataAccess/ConnectionFactory.cs`, csproj | Microsoft.Data.SqlClient; tạo connection riêng mỗi thao tác từ cấu hình runtime |
| `src/CafeManager.DataAccess/Repositories/TableStatusRepository.cs` | Mẫu query async DTO từ vw_TrangThaiBan, filter có tham số |
| `src/CafeManager.DataAccess/SqlParameters.cs`, `PaymentTableBuilder.cs` khi cần | Helper nhỏ cho NULL/typed parameter và TVP DanhSachThanhToan |
| `src/CafeManager.DataAccess/SqlErrorMapper.cs` | Lỗi SqlClient sang lỗi dữ liệu có mã, không phụ thuộc UI |
| `src/CafeManager.Business/Services/TableStatusService.cs` | Mẫu điều phối query, dịch lỗi hiển thị an toàn |
| `src/CafeManager.Models/` | DTO/request/error tối thiểu dùng thật, không SqlException/control |
| `src/CafeManager.WinForms/Program.cs`, `Forms/StartupForm.*` | Nối service; mẫu tải async/loading/rỗng/lỗi/hủy khi đóng Form |
| `tests/CafeManager.Tests/Integration/`, `TestDatabaseFixture.cs`, `database/tests/t05/` | SQL test DB guard, fixtures, mapping/TVP/rollback/dispose |
| `tests/CafeManager.Tests/` unit tests có giá trị | Error mapping, TVP validation/NULL khi có logic cần kiểm chứng |
| `tools/database/Test-DataAccess.ps1`, README/cấu hình mẫu không secret | Setup DB T05 mới, schema→seed→migration, chạy test và lưu bằng chứng |

Không làm ORM/generic repository/UnitOfWork, CRUD tương lai hoặc checkout UI.
Procedure test-only phục vụ mapping/TVP/lỗi nằm ở DB test, không thêm vào
migration nghiệp vụ. Chỉ viết helper khi mẫu query/test có nhu cầu thực.

## 2. Thứ tự triển khai

1. **Connection/config:** pin SqlClient tương thích .NET 10; cấu hình app và
   migration tách principal theo T03, local config bị ignore. ConnectionFactory
   không giữ connection sống dài; caller `await using`/dispose đúng hạn, hỗ trợ
   CancellationToken và command timeout rõ ràng.
2. **Typed query:** query view bằng parameter SqlDbType/size; CHAR trim padding
   khi map mã, DBNull thành nullable. Không AddWithValue cho decimal/CHAR/ngày;
   precision/scale tường minh. Không trả reader/transaction lên BLL/Form.
3. **TVP:** dựng DataTable/SqlDataRecord đúng cột MaPT/SoTien/MaThamChieu,
   Structured + TypeName dbo.DanhSachThanhToan; 0/1/nhiều dòng. Request dùng
   decimal/string/null; không nhận chuỗi tiền đã format.
4. **Procedure/rollback mẫu:** dùng procedure baseline có sẵn trong test để
   chứng minh DAL gọi trực tiếp, không outer transaction. Fixture làm lỗi sau
   ghi tiêu hao/phiếu chi, chụp dữ liệu/transaction rồi assertion. Không thêm
   service thanh toán module tương lai chỉ để test plumbing.
5. **Lỗi/log:** ánh xạ các lỗi baseline 510xx/511xx, FK/CHECK/unique/timeout/
   cancellation/connection và unknown. BLL không MessageBox; Form nhận lỗi an
   toàn, không SQL/stacktrace/secret. Log thao tác, mã nguồn/correlation và mã
   lỗi; không password, parameter nhạy cảm hoặc connection string.
6. **Mẫu async WinForms:** Form gọi service để tải danh sách bàn, giữ trạng thái
   loading/rỗng/lỗi riêng. Hủy khi đóng; kết quả cũ không cập nhật control đã
   dispose hoặc ghi đè request mới. Không Task.Run bọc query sync. Mẫu hiển thị
   chỉ đọc, không thực hiện giao dịch tài chính ở Form.
7. **Nền test:** runner chỉ nhận prefix `CoffeeManager_Test_T05_...`, kiểm tra
   DB_NAME/allowlist trước ghi, tạo tên mới và từ chối DB đang có/production.
   Setup SQL bằng principal test/migration; app tests dùng quyền app T03.
   Fixture có prefix mã riêng, dọn đúng dữ liệu test do nó tạo hoặc giữ DB
   riêng để tra cứu; không reset/drop DB có sẵn.

Chỉ viết code/runner/fixture ở pha triển khai. Sau T05 xin duyệt đợt test chung
G01; setup DB/migration, build/test/run GUI kiểm chứng thực hiện trong đợt đó.

## 3. Ma trận kiểm thử T05-K01–K10

| Ca | Fixture/thao tác | Expected / actual cần lưu |
|---|---|---|
| K01 [I] | Helper/test-only procedure lưu/tìm `Cà phê`, `O'Brien`, chuỗi chứa `'; DROP TABLE ...--` có tham số trong DB riêng | Dữ liệu round-trip đúng; bảng sentinel tồn tại, không thực thi input thành SQL |
| K02 [I] | Dòng test có NULL/string rỗng đã chuẩn hóa, CHAR padding, decimal(18,0)/(18,4) và DATETIME2(0); thử ngoài precision/scale | Map typed đúng, trim mã/giữ SDT; input ngoài contract bị từ chối rõ, không silent truncation |
| K03 [I] | Query filter không khớp; sau đó connection sai/unavailable với timeout giới hạn | Query thành công trả rỗng; mất kết nối là lỗi riêng, không trả rỗng giả |
| K04 [I] | TVP 0/1/nhiều dòng vào test-only proc; thử TypeName/cột/type sai | Đúng cột/order/type/null; lỗi có chẩn đoán và không ghi dữ liệu dở |
| K05 [I] | Checkout method FK sai hoặc fault trigger giai đoạn muộn; snapshot toàn bảng trước/sau qua cùng SQL session; thử tiếp query sau dispose | Rollback tiền/kho/điểm/nợ/header/liên kết; DAL dispose reader/command/connection, không outer transaction |
| K06 [I] | Gọi baseline proc trực tiếp thành công và các nhánh lỗi bằng DAL; probe @@TRANCOUNT/XACT_STATE cùng connection trước dispose | Sau lỗi/success không transaction mở; không suy diễn từ connection mới |
| K07 [M] | Test service chậm qua WAITFOR test-only SQL; thao tác resize/di chuyển, đóng Form hoặc gửi request mới trước khi cũ trả | UI không freeze, đóng/hủy không cập nhật control dispose; kết quả muộn không ghi đè mới |
| K08 [M] | Lỗi đã biết/kết nối/unknown với thao tác và mã nguồn; review file log/output | Có context/mã lỗi, không password/secret/connection string hoặc thông tin nhạy cảm |
| K09 [M] | Trigger lỗi nghiệp vụ đã biết; lỗi chưa biết test-only THROW và timeout | Dịch đúng thông báo an toàn; unknown/timeout không báo thành công, timeout ghi cần đối chiếu nguồn |
| K10 [M] | DB guard với tên production/master/tempdb/DB đã có; kiểm tra fixture cleanup và dữ liệu sentinel | Từ chối trước ghi; dọn đúng phần đã tạo; dữ liệu sentinel/seed ngoài fixture giữ nguyên |

K05/K06 cần cùng session thực sự: test hook nhỏ trong lớp thực thi command nếu
cần, không lộ connection/transaction lên Form. Không dùng connection mới để
chứng minh session lỗi không transaction mở. Snapshot gồm dữ liệu hàng/cột,
không chỉ count/checksum; không đòi identity/sequence rewind sau rollback.

K07 dùng app WinForms thật; fixture chậm chỉ trong SQL test, không thêm delay
vào app production. Cấu hình sample/test mode nêu rõ, không mặc định dùng DB
test hoặc account quản trị khi app chạy thường. Kiểm tra DPI/resize bàn phím
cho controls thêm vào Form theo guide; thiếu GUI ghi BLOCKED và yêu cầu manual.

## 4. Lệnh và lưu kết quả sau duyệt test

- `dotnet build CafeManager.slnx -c Debug` và `-c Release` trong đợt T04/T05.
- Runner setup DB test mới rồi `dotnet test tests/CafeManager.Tests --logger trx`
  với cấu hình DB test runtime; tách integration và unit có giá trị theo trait.
- Mỗi case ghi input/SQL/lệnh, expected/actual, source/schema hash, DB/engine,
  SQL errors/transaction, log/TRX hoặc manual evidence. `test-results.md` và
  JSON/runs đặt cạnh plan; kết quả chưa chạy không tick PASS.
- Bug tại `bug-report.md` T05, tổng hợp report G01. Không sửa source trong đợt
  test; chờ duyệt cả đợt bug rồi debug/regression, giữ kết quả FAIL ban đầu.
