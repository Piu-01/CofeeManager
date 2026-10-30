# T23 — Công nợ, trả từng lần và số dư theo ngày

26/10/2026. **Bộ plan đã duyệt26/10; đã triển khai source, chờ đợt test**. Phụ thuộc T22/schema020, nợ từ T20/chi từ T19. Đọc [contract chung](../plans.md). Chưa chạy đợt nghiệm thu.

## 1. Contract và bất biến

Migration021 ALTER sp_TraCongNo: actor/stamp/request, MaCN, MaPT, SoTien miền rộng, NgayTra typed; bỏ MaTC/MaNV do client tự chọn. ADMIN được trả; nhân viên từ actor, mã chi từ SeqPhieuThuChi017. Demand trước replay; request hash gồm nguồn, phương thức, ngày và tiền.

Luồng: request → khóa nợ theo MaCN UPDLOCK/HOLDLOCK → đọc PN bất biến/ngày nhập → kiểm tiền >0 ≤ còn nợ, ngày trả ≥ ngày nhập, phương thức được hỗ trợ và khác PT010 → helper finance ghi chiTC011 → insert THANHTOAN_CONGNO → receipt → commit. DTO trả MaCN/MaTC/MaTT/nợ gốc/đã trả/còn nợ/trạng thái. Không đặt MaPN trên phiếu trả nợ vì filtered unique MaPN dành cho chi trả ngay; nguồn PN lấy qua TTCN/CN. C# không ghi thêm chi.

Giữ UQ_CN_MaPN và UQ_TTCN_PhieuChi. Nợ nguồn đã chốt không sửa/xóa/đổi PN kể cả chưa trả; PN không đổi NCC/ngày. TR_TraNo kiểm loại/nguồn và khóa đúng nợ bị tác động theo mã, không quét khóa mọi nợ khác; lịch sử trả và chi bất biến. TC011 phải đúng mapping SupplierDebt/Chi, không fallback mã khác. Cấp lại EXEC cho runtime sau chữ ký an toàn có actor.

Public sp_DebtList/Get/History/Receipt: ADMIN/STORE đọc, chỉ ADMIN trả. STORE thấy lịch sử số tiền/ngày/phương thức/mã chi trong detail nợ nhưng không mở Finance. Hiện tại dùng vw_CongNoNCC đúng contract. Tại ngàyX query nguồn PN đến cuốiX và aggregate các lần trả đến cuốiX, không lọc view hiện tại để giả số dư quá khứ. Trạng thái quá hạn tại ngày so HanTra với X; hiện tại giữ quy ước của view. Filter nửa mở, phân trang50, tổng trước JOIN tránh nhân dòng. Ngày9999-12-31 có nhánh tránh DATEADD overflow.

Nợ legacy giữ TienNo và lịch sử thật; không thêm khoản chi ban đầu suy từ tổng nhập. Preflight thấy ngày trả trước nguồn hoặc vượt nợ phải dừng, không sửa ngày/số tiền. Lỗi530xx theo index.

## 2. Bước/file/kiểm kỹ thuật

| Bước | File dự kiến | Đầu ra |
|---|---|---|
| 1 | database/migrations/021_supplier_debts.sql | Preflight020, ALTER payment/guards, query AsOf và quyền; giữ view contract |
| 2 | src/CafeManager.Models/Requests/Finance/SupplierDebtRequests.cs; Dtos/Finance/SupplierDebtRow.cs, SupplierDebtDetail.cs | Filter Current/AsOf, lịch sử/result typed |
| 3 | src/CafeManager.DataAccess/Repositories/SupplierDebtRepository.cs; SqlErrorMapper.cs | Tham số typed, query async, receipt và lỗi nợ |
| 4 | src/CafeManager.Business/Services/SupplierDebtService.cs | Demand DebtRead/DebtPay, normalize, reload số dư khi xung đột, pending đúng actor |
| 5 | src/CafeManager.WinForms/Modules/Finance/{SupplierDebtsControl,SupplierDebtDetailDialog,DebtPaymentDialog}.cs | NCC/PN/history, Current/AsOf, xác nhận trả và trạng thái chưa rõ |
| 6 | NavigationController.cs/MainForm.cs/Program.cs, InventoryControl.cs, FinanceDetailDialog.cs | Entry Debt ADMIN/STORE, liên kết PN↔CN↔chi theo quyền; hoàn thiện điểm vào kho STORE |
| 7 | tests/CafeManager.Tests/Finance/SupplierDebtRulesTests.cs; database/tests/g05/t23-debts.sql; tools/database/Test-G05.ps1 | Unit/SQL/native kịch bản; build và smoke chưa thay nghiệm thu |
| 8 | tools/database/Update-DevelopmentG05.ps1; docs/tasks/g05-kho-nguon-chi-cong-no/development-guide.md | Đường nâng schema có backup/preflight/đối chiếu, giữ tài khoản và profile F5, không fixture development |

## 3. UI và fixture

Danh sách ghi rõ Hiện tại / Tại ngày; không trả dựa trên số dư quá khứ. Khi chọn Trả nợ phải đọc số hiện tại. Dialog620×440/co480×360 logical theo index, PN/NCC/còn nợ chỉ đọc; nhập số trả/ngày/phương thức, xác nhận số tiền rõ. Enter không tự chốt; lỗi giữ input, tải số dư mới và yêu cầu sửa, không tự ép số trả. Kết quả có mã chi/số dư, không bụi sao.

Fixture PN100001 trả ngay40000, nợ60001; trả10000/20000/30001 qua nhiều ngày gồm23:59:59, hạnNULL/quá hạn, nguồn tương lai, hai session trả số nợ cuối. Clone thiếu/sai TC011 và lịch sử không hợp lệ để kiểm preflight; không sửa DB sử dụng.

## 4. Kiểm thử sau duyệt

| ID | Bằng chứng cần có |
|---|---|
| T23-K01, T23-K02 | Một PN/một nợ, nguồn NCC bất biến; nợ0/vượt PN bị chặn, hạnNULL hợp lệ |
| T23-K03, T23-K10 | Ba lần trả đúng số dư;40000 trả ngay không thành lịch sử trả nợ hoặc bị đếm hai lần |
| T23-K04, T23-K05 | Zero/âm/precision/vượt/method/PT010/ngày trước nguồn; fault sau chi/trước TTCN/receipt rollback, không rác |
| T23-K06 | TC011 thiếu/sai loại báo rõ không fallback; clean không seed vẫn có mã tối thiểu qua migrations |
| T23-K07 | Hai session trả nợ cuối, hai thứ tự; thêm hai nợ độc lập để phát hiện khóa dư, không trùng MaTC/vượt nợ |
| T23-K08 | Negative DML nợ/lịch sử/chi/PN bị chặn; nguồn và số dư giữ nguyên |
| T23-K09 | Current/AsOf đúng cutoff, loại lần trả và PN sauX, biên cuối ngày/ngàymax; aggregate không nhân dòng |
| T23-K11, T23-K12 | Native ba role; timeout trước/sau commit và restart cùng request một chi; reload đủ lịch sử/method/link nguồn |

Sau đủ đầu ra T19–T23 mới trình đợt test55ca riêng. Biên bản từng task/report chung và bug batch theo index; không nghiệm thu G04 hoặc T26 từ fixture nguồn.

## Checklist gốc — giữ nguyên expected

### T23 — Công nợ, trả từng lần và số dư theo ngày

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Sử dụng view/procedure hiện có, kiểm soát ngày nguồn và lịch sử chi. |
| Phạm vi | CONGNO_NCC/THANHTOAN_CONGNO/vw_CongNoNCC, ALTER procedure và báo cáo theo ngày; nhập nợ ở T20. |
| Quy mô | L — tiền nguồn và lịch sử |
| Phụ thuộc | T22 |
| Đầu ra | Module công nợ, procedure trả nợ hợp lệ, nguồn ngày cho báo cáo. |

**Kiểm thử bắt buộc:**

- [ ] `T23-K01` [I] NCC của nợ đúng NCC phiếu nhập và không đổi sau chốt; một PN một nợ.
- [ ] `T23-K02` [I] Nợ vượt tổng nhập hoặc nợ 0 bị chặn; hạn NULL hợp lệ.
- [ ] `T23-K03` [I] Trả một phần/nhiều lần/cuối cùng: đã trả/còn nợ khớp, trạng thái đúng.
- [ ] `T23-K04` [I] Trả 0/âm/vượt còn nợ/phương thức sai bị chặn và không phiếu chi rác.
- [ ] `T23-K05` [I] Ngày trả trước ngày nguồn nhập bị chặn; ngày hợp lệ lưu đúng.
- [ ] `T23-K06` [I] TC011 thiếu/sai loại phải lỗi rõ hoặc khởi tạo trước, không chọn nhầm loại Thu.
- [ ] `T23-K07` [I] Hai session trả lượng nợ cuối: không trả vượt; mã phiếu chi không trùng.
- [ ] `T23-K08` [I] UPDATE/DELETE nợ có trả/lịch sử/phiếu chi liên quan bị chặn; đổi nguồn không làm chuyển NCC.
- [ ] `T23-K09` [I] View quá hạn theo thời điểm hiện tại; báo cáo ngày X bỏ lần trả sau X.
- [ ] `T23-K10` [I] Phiếu trả ngay khi nhập không đồng thời tính là trả nợ gốc đã giảm.
- [ ] `T23-K11` [U/M] ADMIN trả được, STORE chỉ xem, CASH không; timeout đọc lại mã chi không retry mù.
- [ ] `T23-K12` [M] Reload xem được mọi lần trả, phương thức và link nguồn phiếu chi.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.


