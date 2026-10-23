# T03 — Bug đợt test G01_20261008_220313

## G01-B01 — Helper setup nhầm biến Seed/seed (chặn T03 và T05)

**Trạng thái:** Đã duyệt bằng “ok” ngày 08/10/2026; đã sửa, regression seed PASS.  
**Task sở hữu:** T03 — helper nền SQL; T05 tham chiếu cùng bug.

### Hiện tượng và bằng chứng

- `Test-ContractMigration.ps1` exit 1, run `20261008_220320_d0edc907`:
  `SetupError: Unexpected seed USE.`; chưa chạy các Case SQL sau setup.
- `Test-DataAccess.ps1` exit 1, run `20261008_220349_2a7e3e26`, cùng lỗi;
  xUnit integration chưa được khởi chạy.
- DB mới đã tạo schema nhưng seed chưa chạy. JSON runner ghi Database null
  vì helper chưa return; metadata SQL xác nhận hai DB còn để tra cứu.
- Bằng chứng: JSON/runs của T03/T05 và
  `../runs/G01_20261008_220313/seed-collision-proof.json`, `sql-metadata.log`.

### Nguyên nhân đã chứng minh

`tools/database/DatabaseTestTools.ps1:26` khai báo `[bool]$Seed`; dòng 41 lại
gán nội dung SQL vào `$seed`. PowerShell không phân biệt hoa/thường tên biến:
đây là cùng biến typed bool, chuỗi không rỗng bị ép thành True. Regex dòng 42
thực tế chạy trên `True`, không phải nội dung seed; số USE matches bằng 0.
Probe độc lập ghi `ActualType=System.Boolean`, `Value=True`, `UseMatches=0`.
Baseline không có lỗi USE; không sửa baseline để né lỗi.

### Hướng sửa đề nghị cho cả đợt

1. Tách tên `$IncludeSeed`/`$seedSql`, giữ guard thay đúng CREATE/USE.
2. Ghi DB test ngay khi tạo, kể cả setup seed lỗi, để runner không mất tên DB.
3. Lưu subcase tiến độ/kết quả trong runner để lỗi/harness timeout không làm
   mất bằng chứng các ca đã chạy. Không coi dữ liệu DB sau timeout là PASS ca.
4. Regression trên **DB mới**: toàn T03-K01–K07 và T05 integration K01–K06,
   K08/K09/K10 liên quan; review K08/K09 T03 theo plan.

### Ảnh hưởng và rủi ro thực tế

Bug thuộc tool setup, chặn integration T03/T05; không chứng minh migration/DAL
sai. Hai DB đã tạo chỉ có schema, không xóa/reset; không ghi vào DB ứng dụng.
Sửa giới hạn helper/runner, giữ nguyên dữ liệu/contract nghiệp vụ. Test mới
dùng tên DB mới và giữ kết quả lỗi lượt đầu.

### Kiểm chứng độc lập đã tiếp tục

Migration 001 áp dụng được trên DB empty mới; repeat trả AlreadyApplied;
DM999 hợp lệ, lần tiếp trả 51302. Các bằng chứng này không thay ca trên seed.
Thử chạy các block độc lập trên empty bằng shell bị harness timeout 120 giây,
không tạo JSON tổng kết; chỉ metadata sau timeout được giữ, không tick PASS
cho ca đồng thời/rollback từ metadata đó. Hiện chưa kết luận nguyên nhân timeout
thuộc sản phẩm; cần tiến độ/log rõ hơn khi regression sau duyệt.

### Regression sau duyệt — G01_Regression_20261008_221528

Đã tách IncludeSeed/seedSql, thêm callback lưu tên DB và progress JSON từng
subcase. Run `20261008_221536_f995bfc1` tạo DB seed/empty mới, không còn lỗi seed;
9/10 subcase SQL PASS. B01 đã được kiểm chứng ở bước setup và các ca độc lập.
Source cố định trong regression; lỗi concurrency mới ghi B03 dưới đây.

## G01-B03 — Thu reader đồng thời bị PowerShell enumerate

**Cập nhật:** Đã duyệt và sửa: List<object>.Add giữ reader nguyên object,
đọc xong dispose/remove trước query tiếp; finally chỉ dispose reader còn mở.
Regression `20261008_222434_ca0e775e` K03 PASS, đủ 100 mã unique đã lưu;
toàn runner 10/10 subcase PASS. Phần điều tra ban đầu phía dưới giữ lịch sử.

**Trạng thái:** Mới phát hiện; chờ duyệt bổ sung, chưa sửa.

- T03-K03-concurrent FAIL: `DataRecordInternal` không có phương thức Dispose.
  Bằng chứng: `runs/20261008_221536_f995bfc1.json` và progress JSON cạnh đó.
- Source dòng 76 `$readers+=$reader` đưa SqlDataReader (IEnumerable) vào phép
  nối mảng; PowerShell enumerate thành các DataRecordInternal. finally dòng 83
  gọi Dispose trên record, che khuất trạng thái assertions trước cleanup.
- Chưa có danh sách 100 mã được lưu; không kết luận SQL sinh mã trùng hay đúng.
- Đề nghị giữ reader như một object (collection typed hoặc thêm unary comma),
  dispose đúng một lần, giữ lỗi gốc và lưu danh sách mã trước cleanup.
- Regression: K03 trên DB mới, hai session chồng lấn, đủ 100 mã duy nhất/đúng
  format/không va seed; cleanup không có exception. Không đổi procedure cấp mã.

## Điều tra bổ sung 18/10/2026 — chưa sửa

Người dùng báo không chạy được luồng đăng nhập từ Visual Studio và tác động môi
trường ngoài ý muốn. Hồ sơ điều tra chỉ đọc, mapping từng finding về task và hướng
khắc phục: [báo cáo toàn đợt](../../../bugs/project-environment-audit-20261018/report.md).

PASS cũ giữ làm lịch sử của bản/fixture đã nghiệm thu; không thay bằng chứng chạy
F5 hiện tại. Đợt này không chạy test hoặc sửa source/migration/configuration.
Các thao tác khắc phục/xóa DB hoặc tài khoản Windows chưa được thực hiện.
