# G02 — Bộ plan T06–T08

Trạng thái: Requirements và cả bộ plan đã được người dùng duyệt ngày09/10/2026.
Đã triển khai tuần tự T06→T07→T08 và nghiệm thu **PASS27/27IDgốc17/10/2026**.
CácđợtdebugB01–B07 vàPKCSđượcduyệt,sửa/regressionPASS. HaiWindowsidentity
thật/setup-popup/restart exe đãPASS,cleanupđầyđủ. Xem`acceptance-matrix.md`.
ChờbàngiaoGit riêng,chưa commit/push; khôngcònblockernghiệmthu.
Mốc source G01:
`deabac108cbeddaa18126733ad18803b0626d7d7`.

| Thứ tự | Plan | Đầu vào → đầu ra |
|---|---|---|
| 1 | [T06](t06-bootstrap-dang-nhap-phan-quyen/plan.md) | DAL G01 → setup một lần, auth/session/quyền, LoginForm |
| 2 | [T07](t07-khung-giao-dien-dieu-huong/plan.md) | Auth T06 → MainForm/AppTheme/navigation/lifecycle |
| 3 | [T08](t08-danh-muc-nen/plan.md) | Shell T07 → 7 tab danh mục, dữ liệu hệ thống, selector phương thức |

## Các quyết định trong bộ plan đã duyệt

| Quyết định | Phương án | Plan sở hữu |
|---|---|---|
| Khóa bootstrap | AppSetupState singleton Pending/LegacyReview/Completed; transaction + application lock; Completed không mở lại | T06 mục 2 |
| DB sạch/demo/chuyển phiên bản | Tạo admin tối thiểu; chỉ kích hoạt account demo được chọn; admin hash không phải placeholder cần xác nhận bằng password hiện tại, không reset | T06 mục 2 |
| Setup/runtime | Chế độ --setup dùng Windows identity có role setup riêng; đóng process setup rồi chạy app bằng identity runtime | T06 mục 2 |
| Username/rehash/session | ASCII 1–50, trim dấu cách ngoài, case-insensitive key BIN2 sau lowercase; CAS rehash bằng hash/stamp cũ; recheck mỗi service call | T06 mục 3 |
| Quyền danh mục | ADMIN CRUD; CASH/STORE không mở tab quản trị, chỉ lookup có trường được cấp, không lộ tỷ lệ/phụ cấp | T08 mục 2 |
| Mã hệ thống | TC005 lương, TC011 trả nợ, đề xuất mới TC012 trả ngay; tất cả loại Chi, khóa mã/loại/xóa ngay cả chưa dùng | T08 mục 3 |
| Cấp mã | DM/NV dùng sp_CapMa; 6 danh mục còn lại nhập CHAR(5), NCC+2 số hoặc PT/DT/TC/CV/CA+3 số; mã không đổi khi sửa | T08 mục 3 |
| Phương thức | PaymentMethodSupport riêng; PT001/PT002 mặc định hỗ trợ ghi nhận thủ công, PT010 luôn loại, phương thức mới mặc định chưa hỗ trợ | T08 mục 4 |
| Shell | ADMIN vào danh mục; CASH/STORE vào trang phiên với trạng thái chưa triển khai; không giao dịch giả | T07 mục 2 |

TC012 là đề xuất mới, không nói đã có trong G01. Xung đột username, mã hệ thống,
tên unique hoặc role trên DB cũ làm migration dừng/rollback và báo quyết định;
không tự sửa dữ liệu để thông qua. Đây không phải quyền mở rộng scope khi code.

## Migration, kiểm tra kỹ thuật và nghiệm thu

- Đề xuất 002_auth_and_bootstrap.sql (T06) và 003_reference_catalogs.sql (T08),
  chạy sau001 qua Apply-Migration.ps1; không sửa baseline/seed/001 đã bàn giao.
- T06→T07→T08 tuần tự, SQL→DAL→BLL→UI. Trong triển khai được build/compile SQL
  và smoke tối thiểu trên DB test riêng để xác nhận dependency; ghi
  technical-checks.md cạnh task, không đánh dấu PASS Txx-Kxx.
- Sau triển khai cả nhóm: báo sẵn sàng, chờ duyệt đợt nghiệm thu một lần.
  Không chạy ma trận dưới đây chỉ vì bộ plan được duyệt.
- Mỗi lượt nghiệm thu lấy manifest source/migration/test cố định và schema
  checksum; không sửa giữa lượt. Mỗi subcase có dữ liệu, lệnh/thao tác,
  expected/actual, PASS/FAIL/BLOCKED/NOT RUN và evidence riêng.
- SQL Server thật, DB test riêng có marker G02; không dùng DB ứng dụng hoặc
  SQLite. Source G01 kế thừa; regression theo tác động auth/composition/UI.
- Bug tại task sở hữu, tổng hợp report.md; chờ duyệt đợt bug trước debug,
  duyệt debug bao gồm regression. Commit/push G02 chỉ khi giao riêng.

## Đầu ra hồ sơ

Mỗi task: technical-checks.md, test-results.md và runs/ sau khi chạy tương ứng;
bug-report.md khi có lỗi. Report nhóm giữ tất cả kết quả và lịch sử, không
nghiệm thu riêng task trong pha triển khai. Giữ 27 ID gốc và toàn bộ subcase
bắt buộc trong ba plan; 325 ID toàn dự án không đổi.
