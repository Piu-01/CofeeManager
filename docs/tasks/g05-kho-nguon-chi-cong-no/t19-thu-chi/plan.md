# T19 — Thu chi và liên kết nguồn chi hệ thống

26/10/2026. **Bộ plan đã duyệt26/10; đã triển khai source, chờ đợt test**. Requirements đã duyệt; T18 đạt 8/8. Đọc kèm [contract chung](../plans.md). Chưa chạy đợt nghiệm thu.

## 1. Contract được chọn

Migration017 sau016 bổ sung PHIEUTHUCHI.MaPN char(10) NULL FK PHIEUNHAP và MaBL int NULL FK BANGLUONG. Mỗi nguồn có filtered UNIQUE riêng; CHECK không cùng có MaPN/MaBL. Thêm OriginKind: Manual, PurchaseImmediate, Payroll, SupplierDebt, LegacyUnknown. Client chỉ tạo Manual qua editor. Chi trả nợ đi theo THANHTOAN_CONGNO → CONGNO_NCC → PHIEUNHAP; không đặt MaPN của phiếu trả nợ vào cột dành cho chi trả ngay.

Dùng SystemExpenseCategory hiện có: Payroll/TC005, SupplierDebt/TC011, PurchaseImmediate/TC012. Không đổi TC008 thành chi trả ngay. Chi có liên kết lịch sử trả nợ thật được phân loại theo nguồn; phiếu hệ thống cũ thiếu nguồn giữ LegacyUnknown. Không đoán mã PN/BL từ ghi chú, tiền hoặc ngày.

Phiếu ghi tay xác nhận ngay khi commit, sau đó chỉ đọc: không sửa/xóa/đảo bằng tiền âm. Trigger chặn UPDATE/DELETE phiếu đã ghi; danh mục có lịch sử không được đổi loại làm thay lịch sử, vẫn có thể đổi tên theo contract danh mục. MaTS tùy chọn chỉ liên kết tài sản thật, không xây module tài sản T27.

Public procedure dự kiến: sp_FinanceList/Get/Categories/Create/Receipt. Tất cả yêu cầu ADMIN; Create nhận actor/stamp/request, ngày, danh mục thường, MaTS?, tiền nguyên VND > 0, ghi chú ≤ 200. Nhân viên lấy từ actor. SeqPhieuThuChi cấp TC+8digits trong transaction, receipt cùng commit. List lọc ngày/loại/nguồn/mã, phân trang50 và tổng Thu/Chi trên toàn tập lọc. Detail trả nguồn và dấu legacy rõ.

Helper nội bộ sp_FinanceSystemExpense nhận use case, nguồn PN/BL hoặc CN và tiền; kiểm mapping loại Chi, transaction của procedure gọi. Không GRANT helper cho runtime, không dùng SESSION_CONTEXT làm bằng chứng quyền. T20/T23 gọi helper rồi hoàn tất nguồn trong cùng transaction; T19 không viết sp_ChiLuong. Trigger nguồn/loại kết hợp FK/unique; runtime bị DENY DML. Thu hồi GRANT baseline sp_TraCongNo/sp_ChotKiemKe thiếu actor; chỉ cấp lại tại T22/T23 sau đổi chữ ký an toàn.

## 2. Triển khai theo SQL → DAL → BLL → UI

Các đường dẫn dưới tính từ gốc repo; tên file mới là dự kiến.

| Bước | File/khu vực | Đầu ra và kiểm kỹ thuật |
|---|---|---|
| 1 | database/migrations/017_finance_sources.sql | Preflight, schema, sequence, FK/index, procedure, trigger và quyền; smoke trên DB riêng clean/demo |
| 2 | src/CafeManager.Models/Requests/Finance/FinanceRequests.cs; Dtos/Finance/FinanceRow.cs | Request/filter/source/result typed, nguồn nullable khi chưa rõ |
| 3 | src/CafeManager.DataAccess/Repositories/FinanceRepository.cs; SqlErrorMapper.cs | SqlProcedureExecutor, tham số typed, lỗi526xx, phân biệt timeout sau gửi |
| 4 | src/CafeManager.Business/Services/FinanceService.cs | Demand Finance mọi thao tác, normalize và đối chiếu đúng actor/request |
| 5 | src/CafeManager.WinForms/Modules/Finance/{FinanceControl,FinanceEntryDialog,FinanceDetailDialog}.cs | Danh sách, ghi tay, detail chỉ đọc, validation đúng ô, busy/unknown |
| 6 | src/CafeManager.WinForms/Navigation/NavigationController.cs, Forms/MainForm.cs, Program.cs | NavigationTarget.Finance và factory ADMIN; CASH/STORE bị chặn cả service |
| 7 | Models/Dtos/PendingOperation.cs; Business/Services/PendingOperationStore.cs; WinForms/Configuration/PendingOperationFile.cs dưới src/ | Lưu pending tối thiểu bằng DPAPI; implementation qua adapter thực, Business không phụ thuộc UI; không tạo framework chung |
| 8 | tests/CafeManager.Tests/Finance/FinanceRulesTests.cs; database/tests/g05/t19-finance.sql; tools/database/Invoke-G05Technical.ps1 | Build Debug/Release và smoke tối thiểu; lưu technical-checks.md, chưa tick nghiệm thu |

## 3. Cutover, UI và fixture

Preflight kiểm số tiền, FK, trùng nguồn, loại hệ thống và mapping003; xung đột dừng migration, rollback và không ghi version. Cấp mã tránh mọi TC hiện có cùng pattern; giữ mã TR legacy. Snapshot trước/sau xác nhận tiền, nợ, tài khoản, tồn và điểm không đổi. Không sửa baseline/seed hoặc checksum001–016.

Dùng OrbitListWorkspace và dialog620×440 logical, thu tới480×360 theo bố cục chung. Tổng và tiền lấy SQL; nguồn hiện chữ/badge. Link PN/nợ bật sau T20/T23 theo quyền; BL trước T26 chỉ hiển thị mã nguồn có thật, không mở module chưa có. Không bụi sao.

Fixture có Thu/Chi thường, MaTS hợp lệ, PN/BL giả lập có nguồn, chi trả nợ legacy. Kiểm BL bằng fixture không thay nghiệm thu chi lương T26. Helper không được mở public để phục vụ test; negative dùng principal kiểm thử riêng.

## 4. Truy vết kiểm thử sau duyệt đợt test

| ID | Dữ liệu/thao tác và bằng chứng cần có |
|---|---|
| T19-K01, T19-K02 | Native ADMIN tạo/reload Thu/Chi; zero, âm, precision, độ dài, FK, optional NULL; SQL không có phiếu/receipt rác khi lỗi |
| T19-K03, T19-K04 | FK PN/BL, duplicate cùng nguồn, hai nguồn cùng phiếu, loại Thu sai; tích hợp chi lương T26 ghi NOT RUN |
| T19-K05, T19-K07 | Ba role UI/BLL/SQL; danh mục hệ thống bị chặn; phiếu và loại nguồn bất biến |
| T19-K06 | Fault giữa phiếu/liên kết/receipt; rollback toàn bộ, transaction0; replay và khác payload/actor |
| T19-K08, T19-K09 | Aggregate nhiều nguồn theo ngày, không cộng lịch sử nợ hai lần; checkout không tạo thêm Thu |
| T19-K10 | Filter/cutoff/source/reload; timeout trước/sau commit, khôi phục pending sau restart không tạo hai phiếu |

Đầu ra T19 dùng cho T20. Lưu biên bản/bug tại task, báo cáo chung G05; không nghiệm thu PASS trong pha triển khai.

## Checklist gốc — giữ nguyên expected

### T19 — Thu chi và liên kết nguồn chi hệ thống

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Tạo nền đối soát tiền trước nhập hàng và chi lương. |
| Phạm vi | PHIEUTHUCHI/DANHMUC_THUCHI; migration FK nguồn MaPN/MaBL và unique có điều kiện; editor chi thường. |
| Quy mô | M — tài chính nền |
| Phụ thuộc | T18 |
| Đầu ra | Service thu chi, loại hệ thống bị bảo vệ, FK nguồn sẵn cho T20/T26. |

**Kiểm thử bắt buộc:**

- [ ] `T19-K01` [I/M] Phiếu Thu/Chi thông thường số tiền dương, ngày/nhân viên/danh mục hợp lệ lưu đúng.
- [ ] `T19-K02` [I] Tiền 0/âm/quá precision, tên/ghi chú quá dài và FK nguồn sai bị chặn.
- [ ] `T19-K03` [I] MaPN/MaBL FK hợp lệ; hai phiếu chi cho cùng nguồn trả ngay/lương bị unique chặn.
- [ ] `T19-K04` [I] Phiếu nguồn hệ thống phải loại Chi đúng, không thể đổi thành Thu.
- [ ] `T19-K05` [U/M] UI thủ công không chọn danh mục trả nợ/trả ngay/lương hệ thống; chỉ ADMIN ghi chi.
- [ ] `T19-K06` [I] Nguồn trùng và lỗi giữa lưu phiếu/liên kết rollback, không phiếu rác.
- [ ] `T19-K07` [I/M] Chính sách sửa chi thường rõ: bản đã xác nhận không xóa/sửa tiền âm để đảo; lịch sử chi nguồn bất biến.
- [ ] `T19-K08` [I] Tổng Thu/Chi theo ngày không cộng các dòng lịch sử trả nợ thêm lần nữa.
- [ ] `T19-K09` [I/M] Bán hàng không đồng thời tạo phiếu thu rồi nhân đôi nguồn doanh thu.
- [ ] `T19-K10` [M] Tra cứu/source link và reload hiển thị đủ loại tiền/ghi chú/mã nguồn.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.


