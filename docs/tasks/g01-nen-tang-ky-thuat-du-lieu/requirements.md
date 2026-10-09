# G01 — Nền tảng kỹ thuật và dữ liệu

**Trạng thái:** Requirements/bộ plan/đợt test và debug B01–B06 đã duyệt — 42 PASS/3 BLOCKED manual, G01 chưa nghiệm thu. Commit bàn giao chờ cho phép.  
**Duyệt:** Người dùng xác nhận “ok tiếp tục dự á đi” ngày 08/10/2026.  
**Ngày:** 08/10/2026.  
**Nguồn chuẩn:** `docs/KeHoach_QuanLiQuanCafe.md` phiên bản 3.2, T01–T05.  
**Phụ thuộc trước nhóm:** Không.  
**Thứ tự task con:** T01 → T02 → T03 → T04 → T05, giữ nguyên phụ thuộc từng task.

## 1. Mục tiêu

Hoàn thiện nền tảng để các phân hệ được triển khai trên WinForms .NET 10 và SQL
Server: môi trường/repo, baseline được kiểm chứng, contract/migrations/cấp mã,
solution đúng tầng và DAL/nền kiểm thử. Đây là nhóm quản lý duyệt, không thay đổi
phạm vi, đầu ra hoặc tiêu chí của năm task nguyên bản.

## 2. Task con, phạm vi và đầu ra giữ nguyên

| Task | Phạm vi nguyên bản | Đầu ra nguyên bản |
|---|---|---|
| T01 — Môi trường/repository | SDK, Visual Studio, WinForms Designer, SQL instance, Git và cấu hình máy; chưa viết nghiệp vụ | Phiên bản thực tế, global.json, README môi trường, repo gốc solution |
| T02 — SQL baseline | DB test, schema/seed, FK/CHECK/trigger/procedure/view; không migration mở rộng | Biên bản PASS/FAIL baseline; xử lý lỗi thực thi theo quy định của T02 |
| T03 — Contract, migration, cấp mã | Migration version, sequence/cấp mã, tiền/NULL/trạng thái, quyền ghi nguồn tự động; chưa toàn bộ procedure tương lai | Migration đầu tiên, lịch sử version, contract request/kết quả và ma trận nơi ghi |
| T04 — Solution ba tầng + Models | Bốn project src, project tests, namespace, composition root, docs/database; chưa lớp rỗng mọi bảng | Solution build được, references đúng, mẫu DTO/service/repository khi cần |
| T05 — DAL và nền kiểm thử | ConnectionFactory, query/command có tham số, TVP, dispose, log/dịch lỗi; không generic ORM | DAL nền, DB test riêng, mẫu query DTO và rollback có xác nhận |

Mô tả, quy mô, phụ thuộc và mọi dòng checklist chi tiết **giữ nguyên trong kế
hoạch**; bảng này là chỉ mục tham chiếu, không thay nội dung từng Txx. Không
triển khai login T06, MainForm T07 hay module nghiệp vụ tương lai trong G01.

## 3. Nghiệm thu — giữ đủ 45 ca nguyên bản

| Task | Ca bắt buộc | Nhóm nội dung kiểm chứng (không thay checklist chi tiết) |
|---|---|---|
| T01 | T01-K01–K07 (7 ca) | Designer, build/run, SDK pin, SQL/SSMS/quyền test, Git root/ignore và source sạch |
| T02 | T02-K01–K12 (12 ca) | Catalog, seed, tiền/phân bổ, tổng, seed chạy lại, FK/CHECK/NULL/unique, trigger batch, rollback checkout/nợ/kiểm kê và transaction |
| T03 | T03-K01–K09 (9 ca) | Migration bảo toàn/idempotency, mã đồng thời/giới hạn/rollback, quyền app/migration và typed contract |
| T04 | T04-K01–K07 (7 ca) | Build, tầng/references, UI không SQL, composition root, Designer, restore sạch và không abstraction dư |
| T05 | T05-K01–K10 (10 ca) | Param/input, mapping, rỗng/lỗi, TVP, rollback/dispose, transaction, async UI, log, error mapping và test DB guard/cleanup |

Áp dụng nguyên văn expected và điều kiện hoàn tất của T01–T05 trong kế hoạch;
không bỏ/giảm ca hoặc đánh dấu PASS từ việc đọc source. Test SQL trên SQL Server
thật và DB test riêng, đối chiếu toàn bộ tác động/số dư; manual có bằng chứng.

## 4. Hiện trạng và giả định đã biết

- T01 đã duyệt requirements/plan, có 7/7 ca PASS và bằng chứng Designer/SSMS từ
  người dùng. Kế thừa hồ sơ `t01-moi-truong-repository/` trong G01, không làm lại.
- Git root CoffeeManager, nhánh main; SDK pin 10.0.401, WinForms net10.0-windows.
- SQL local default instance MSSQLSERVER, engine 17.0.1000.7 Enterprise Developer;
  Windows Authentication. Trust certificate local đã duyệt; chưa là cấu hình LAN.
- Baseline/seed ở `database/`; T02 đã chạy đạt 12/12 ca, 80/80 subcase PASS,
  kế thừa kết quả tại `t02-sql-baseline/test-results.md`. Hai baseline nguồn
  không đổi. T03–T05 đã viết đầu ra, chưa chạy/test; DB test sẽ tách khỏi DB ứng dụng.
- Requirements T02 đã soạn chỉ còn là tham chiếu kỹ thuật; không xin duyệt riêng.
- Không còn câu hỏi bắt buộc để duyệt nhóm; chi tiết runner/migration/contract
  và file triển khai được thể hiện trong plan từng task con.
- Commit bàn giao vẫn theo cho phép của người dùng; không tự commit/push từ việc
  đổi quy trình nhóm. Tình trạng commit hiện có được ghi trong hồ sơ nhóm.

## 5. Quy trình duyệt và thực hiện nhóm

1. Người dùng duyệt **requirements G01 một lần**.
2. Viết đủ plan T01–T05 trong thư mục con của G01, chỉ mục tại `plans.md`.
   T01/T02 là lịch sử kế thừa; duyệt cả bộ một lần cho phần T03–T05 còn lại.
3. Sau duyệt, triển khai từng plan theo thứ tự; sau T05 chờ duyệt đợt test G01
   một lần. Chưa chạy test/ghi PASS từ việc triển khai.
4. Sau duyệt test, chạy từng task theo ma trận và lưu biên bản tại task; báo
   cáo chung ở `report.md`, gồm cả FAIL/BLOCKED/NOT RUN nếu có.
5. Bug ghi tại task sở hữu, tổng hợp trong report; người dùng duyệt cả đợt bug
   một lần trước debug/regression. Giữ source cố định trong lượt test; không
   sửa giữa lượt hoặc đổi expected để né lỗi. Bug mới cần duyệt bổ sung.
6. Chỉ nghiệm thu nhóm khi đủ đầu ra, 45 ca PASS và điều kiện bàn giao nguyên bản.

Blocker cần dữ liệu/quyền/manual thì hỏi để tiếp tục; gom thao tác liên quan
để tránh hỏi nhiều lần. Phát sinh ngoài scope/đổi nghiệp vụ/contract phải xin
duyệt phần thay đổi; nhóm lớn không cho phép mở rộng task con tự động.

## 6. Điều kiện hoàn tất nhóm

Đạt đầy đủ điều kiện hoàn tất nguyên bản của từng T01–T05, đủ 45 ca PASS, không
lỗi trọng yếu và có biên bản/commit bàn giao theo quy định. Không coi nhóm xong
chỉ vì build solution hoặc một phần test PASS. Nhóm sau G02 phụ thuộc T05.
