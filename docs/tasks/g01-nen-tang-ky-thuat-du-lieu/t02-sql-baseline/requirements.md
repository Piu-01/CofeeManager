# T02 — Chạy và kiểm chứng SQL baseline

**Trạng thái:** Hồ sơ tham chiếu — điểm duyệt chuyển sang requirements G01; chưa triển khai SQL.  
**Ngày:** 08/10/2026.  
**Nguồn:** `docs/KeHoach_QuanLiQuanCafe.md` phiên bản 3.0, mục 12/T02.  
**Phụ thuộc:** T01 đã nghiệm thu kỹ thuật 7/7 PASS; commit bàn giao chờ cho phép.  
**Baseline:** `database/DB_Schema.sql`, `database/Insert_Data.sql`.  
**SQL instance:** localhost / MSSQLSERVER, engine 17.0.1000.7 Enterprise Developer
Edition (64-bit), Windows Authentication. Trust certificate local đã được duyệt.

## 1. Mục tiêu

Kiểm chứng schema và seed đính kèm trên SQL Server thật, xác nhận các bất biến
và rollback trước khi dựa vào baseline để phát triển migration/C#.

## 2. Phạm vi và đầu ra

- Chuẩn bị runner/script kiểm chứng có khả năng xử lý `GO` và ghi kết quả từng
  ca, lỗi SQL, đối chiếu trạng thái trước/sau và `@@TRANCOUNT` trên cùng session.
- Tạo database test mới, riêng cho T02. Tên dùng tiền tố `CoffeeManager_Test_T02_`
  và hậu tố duy nhất cho lần chạy; không dùng DB ứng dụng đang có.
- Hai baseline hardcode `CREATE DATABASE`/`USE QuanLyCafe_ERP_V2`. Runner tạo bản
  chạy tạm chỉ thay tên DB tại những câu lệnh này để nhắm DB test; giữ nguyên
  logic schema/seed nguồn. Kiểm tra đúng DB trước các thao tác ghi.
- Chạy schema trước seed; đối chiếu catalog, số liệu mẫu và bất biến trên engine.
- Chuẩn bị fixture/đối chiếu cho đủ T02-K01–K12, gồm lỗi ràng buộc, trigger nhiều
  dòng, checkout/trả nợ/kiểm kê và transaction sau lỗi.
- Seed chạy lại chỉ được thử trên DB test; kiểm tra không làm đổi dữ liệu cũ.
  Không chạy lại CREATE DATABASE vào DB đã có, không tự xóa/ghi đè DB có sẵn.
- Lưu script/runner, `test-results.md` với expected/actual, tên DB test, phiên bản
  engine, hash baseline và kết quả thật. Lưu `bug-report.md` nếu phát hiện lỗi.
- Lỗi baseline được điều tra và đề xuất fix riêng để duyệt trước khi sửa; chạy
  lại ca lỗi và regression liên quan. Không mở rộng nghiệp vụ của baseline.

T02 chưa triển khai migration cấp mã/quyền mới của T03, solution/BLL/DAL/UI của
T04 trở đi. Không bật tài khoản demo, không đổi chính sách ưu đãi sang thiết kế
đích T15 hoặc dùng seed như chức năng quản trị.

## 3. Giả định và cách kiểm chứng

- Dùng instance local đã nghiệm thu và quyền hiện tại cho setup/test; đây chưa
  phải tài khoản runtime quyền tối thiểu của ứng dụng.
- DB test có tên mới và dữ liệu do runner tạo; không tác động database khác.
  Nếu tên đã có, từ chối hoặc dùng tên mới, không reset database cũ ngầm.
- Chọn công cụ runner cụ thể trong plan; dùng SQL Server thật, không mock hoặc
  SQLite. Cần giữ cùng connection cho ca kiểm tra transaction sau lỗi procedure.
- Các ca fixture phải được dọn trong phạm vi DB test/rollback thích hợp; kết quả
  seed cần được đối chiếu trước các ca làm thay đổi dữ liệu.
- Không đánh dấu procedure/trigger đúng chỉ vì compile; kiểm tra tác động tiền,
  tồn, điểm, liên kết và trạng thái. Mọi ca chưa chạy giữ NOT RUN/BLOCKED.
- Không cần hỏi thêm thông tin để lập plan; T01 đã cung cấp đủ instance/quyền.

## 4. Tiêu chí nghiệm thu

| Mã | Expected / phạm vi kiểm chứng |
|---|---|
| T02-K01 | Có đủ 35 bảng, 2 view, 4 procedure, 1 table type nghiệp vụ; đối tượng compile hợp lệ |
| T02-K02 | Seed COMMIT: 10 hóa đơn, 18 dòng món, 9 đơn đã trả, 1 đơn mở bàn B0003 |
| T02-K03 | HD20250401 phải trả 112.500; HD20250409 phải trả 81.000, phân bổ 30.000 + 51.000 |
| T02-K04 | Tổng hóa đơn/phiếu nhập khớp chi tiết; mọi đơn trả đủ phân bổ |
| T02-K05 | Seed chạy lại bị từ chối; dữ liệu trước/sau không đổi |
| T02-K06 | FK sai, tiền âm, lượng 0, ngày KM ngược bị chặn; KM cùng ngày hợp lệ |
| T02-K07 | Nhiều SDT NULL trong khách/nhân viên hợp lệ; SDT thực trùng bị chặn |
| T02-K08 | Trigger INSERT/UPDATE/DELETE nhiều dòng món/nguyên liệu cập nhật đúng từng nhóm và chênh lệch |
| T02-K09 | Checkout thiếu NVL hoặc phương thức sai rollback tiêu hao, tiền, tồn, điểm và trạng thái |
| T02-K10 | Trả nợ vượt rollback phiếu chi/lịch sử; sửa lịch sử chốt bị chặn |
| T02-K11 | Kiểm kê hợp lệ chốt một lần; lần hai/snapshot lệch/ngày cũ bị chặn, tồn không đổi sai |
| T02-K12 | Các nhánh lỗi của bốn procedure có kiểm chứng; @@TRANCOUNT trở lại hợp lệ trên cùng session, không dữ liệu dở |

Plan sẽ diễn giải fixture và các subcase để bao phủ điều kiện từng ca, đặc biệt
các nhánh lỗi T02-K12. Không giảm checklist vì một lỗi làm ca khác không chạy được.

## 5. Điều kiện hoàn tất và điểm duyệt

T02 chỉ hoàn tất khi schema/seed chạy thật, đủ T02-K01–K12 PASS, lỗi trọng yếu đã
xử lý và biên bản/commit được lưu theo cho phép. Nếu engine báo lỗi, ghi actual
và root cause trước khi đề xuất sửa, không sửa seed để né ràng buộc.

Theo quy trình nhóm ngày 08/10/2026, file này chỉ làm tham chiếu. Phạm vi T02
được duyệt qua `docs/tasks/g01-nen-tang-ky-thuat-du-lieu/requirements.md`; sau đó
quy trình mới tại `../plans.md` áp dụng cho các lượt tiếp theo: duyệt bộ plan,
duyệt đợt test riêng và duyệt đợt bug trước debug.
