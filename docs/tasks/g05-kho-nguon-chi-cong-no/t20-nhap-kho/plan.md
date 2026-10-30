# T20 — Nhập kho, trả ngay và tạo nợ nguyên tử

26/10/2026. **Bộ plan đã duyệt26/10; đã triển khai source, chờ đợt test**. Phụ thuộc T19/schema017. Đọc [contract chung](../plans.md). Chưa chạy đợt nghiệm thu.

## 1. Contract được chọn

Migration018 thêm PHIEUNHAP.DaChot, Revision rowversion, CutoverKind, NgayChot nullable. Phiếu cũ hợp lệ được đánh dấu đã chốt, NgayChot NULL vì chưa có bằng chứng thời điểm. Phiếu mới tạo và chốt một transaction, không lưu nháp DB. Guard khóa header/chi tiết sau chốt, không reset cờ hoặc đổi NCC/ngày/tổng.

Nợ cũ giữ đúng TienNo. Không tạo chi bằng TongTien−TienNo hoặc suy ra phần đã trả từ ghi chú. Detail legacy báo phần thanh toán ban đầu chưa đủ nguồn, không hiển thị 0 giả cho dữ liệu chưa rõ. Backfill header không chạm chi tiết hoặc chạy lại cộng kho.

TVP PurchaseLinesInput có MaNVL miền rộng, Quantity/UnitPrice decimal(38,18); kiểm NVL trùng, độ dài, lượng4số lẻ và giá nguyên VND trước convert. sp_PurchasePreview dùng chung biểu thức SQL với chốt: tổng SUM(ROUND(lượng×giá,0)) theo đơn vị chuẩn. Không tự đổi g/kg. sp_LapPhieuNhap nhận actor/stamp/request, NCC, ngày, hạn nợ nullable, trả ngay, expected tổng và TVP; SQL kiểm lại tổng trước ghi.

Luồng chốt: demand ADMIN/STORE → receipt → cấp MaPN → khóa NVL tăng dần → header tổng0 → insert chi tiết → TR_CTPN_Kho cộng tồn/tính tổng → chi TC012 gắn MaPN nếu trả ngay > 0 → nợ/MaCN nếu còn > 0 → DaChot1 → receipt → commit. C# không ghi chi hoặc cộng tồn lần nữa. STORE chỉ được trả ngay0; SQL/BLL đều chặn giá trị khác0. Trả ngay không sinh THANHTOAN_CONGNO.

Thêm SeqPhieuNhap/SeqCongNo và case cấp mã đúng task. Public List/Get/Preview/Receipt/Lookups của Purchase cho ADMIN/STORE; lookup NCC không mở quyền quản trị danh mục cho STORE. DTO trả PN/tổng/trả ngay/MaTC?/MaCN?/nợ/revision. Khóa nguồn nợ của PN đã chốt kể cả chưa trả; T23 kế thừa. Lỗi527xx và unknown theo bộ plan.

## 2. Bước/file và kiểm kỹ thuật

| Bước | File dự kiến | Kết quả |
|---|---|---|
| 1 | database/migrations/018_purchase_receipts.sql | Preflight017, schema/type/procs/guards; giữ trigger là nguồn cộng tồn và tổng duy nhất |
| 2 | src/CafeManager.Models/Requests/Inventory/PurchaseRequests.cs; Dtos/Inventory/PurchaseRow.cs, PurchaseDetail.cs | Nháp, preview và kết quả typed, legacy nullable |
| 3 | src/CafeManager.DataAccess/Repositories/PurchaseRepository.cs, PurchaseTableBuilder.cs, SqlErrorMapper.cs | TVP/parameter precision rõ, async và receipt |
| 4 | src/CafeManager.Business/Services/PurchaseService.cs | Demand Inventory, Finance khi trả ngay, normalize và pending theo T19 |
| 5 | src/CafeManager.WinForms/Modules/Inventory/{PurchasesControl,PurchaseEditorDialog,PurchaseDetailDialog}.cs | Nháp local, dòng trùng focus dòng cũ, preview SQL, chốt và nguồn chỉ đọc |
| 6 | InventoryControl.cs cùng thư mục; NavigationController.cs, MainForm.cs, Program.cs | Entry Kho với tab Tồn/Nhập, kế thừa MaterialsControl; chưa có tab chức năng giả |
| 7 | tests/CafeManager.Tests/Inventory/PurchaseRulesTests.cs; database/tests/g05/t20-purchases.sql; technical-checks.md | Build Debug/Release, compile/smoke DB riêng; chưa tick12ca |

Editor900×600 logical, co640×420 theo index: NCC/ngày, grid NVL/đơn vị/lượng/giá/tổng, summary trả ngay/nợ và footer chốt. STORE thấy trả ngay0 khóa và giải thích quản lý xử lý. Chốt thành công dùng DTO; lỗi giữ nháp; rời hỏi bỏ. Link chi chỉ ADMIN, STORE chỉ xem nợ. CASH giữ đường đọc tồn hiện có.

## 3. Preflight và fixture

Kiểm tổng PN khớp SUM ROUND, nợ≤tổng, lượng/giá/FK, nguồn chi trùng/sai. Phiếu cũ rỗng hoặc sai không thể backfill đúng phải dừng có mã nguồn, không tự thêm dòng. Đối chiếu tồn, PN/CTPN, chi/nợ, điểm và tài khoản trước/sau. Rerun cùng checksum không cộng kho.

Fixture có tổng100001, hai NVL đơn vị Kg/lít, lượng0.0001 và dòng cho làm tròn0.5 VND, các mức trả0/40000/đủ, PN legacy và bản invalid riêng. Không ghi vào development.

## 4. Kiểm thử sau duyệt

| ID | Bằng chứng cần có |
|---|---|
| T20-K01, T20-K12 | Native bỏ nháp không ghi DB; nhiều dòng preview/chốt/reload nguồn khớp SQL |
| T20-K02, T20-K06 | NCC/NVL/actor sai, trùng dòng, zero/âm/precision, trả âm/vượt, danh mục sai; không truncate để chấp nhận input |
| T20-K03 | Fault sau header/chi tiết/chi/trước nợ/receipt; rollback PN/CTPN/kho/chi/nợ, transaction0 |
| T20-K04, T20-K05 | Trả0 không chi, trả đủ không nợ, trả40000 nợ60001; không ghi thêm lần trả nợ |
| T20-K07 | Double click, replay hai session, timeout trước/sau commit và restart: một PN/chi/nợ; khác payload bị chặn |
| T20-K08, T20-K09 | Sửa/reset/xóa chứng từ chốt bị chặn; backfill demo không đổi lịch sử và tồn |
| T20-K10 | Hai session cùng NVL với TVP đảo thứ tự, barrier có quan sát; tổng tồn tăng đúng, không lost update |
| T20-K11 | ADMIN trả ngay, STORE0 hợp lệ/giá trị khác bị từ chối, CASH bị chặn UI/BLL/SQL |

T20 cấp đầu ra cho T21/T23; biên bản theo từng pha và báo cáo chung G05.

## Checklist gốc — giữ nguyên expected

### T20 — Nhập kho, trả ngay và tạo nợ nguyên tử

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Lưu một phiếu nhập hoàn chỉnh thay cho ghi rời từng dòng trong app. |
| Phạm vi | Procedure sp_LapPhieuNhap đề xuất, TVP chi tiết, cờ chốt/khóa phiếu; FK chi trả ngay và CONGNO_NCC. |
| Quy mô | L — hàng và tiền nhiều bảng |
| Phụ thuộc | T19 |
| Đầu ra | Module nhập với nháp bộ nhớ; transaction nhập/chi/nợ và bảo vệ chứng từ sau chốt. |

**Kiểm thử bắt buộc:**

- [ ] `T20-K01` [M/I] Soạn nháp/đóng dialog không đổi kho; chốt n dòng cộng đúng tồn và tổng dòng làm tròn.
- [ ] `T20-K02` [I] NCC/nhân viên sai, lượng 0/âm, giá 0/âm, NVL trùng cùng phiếu bị từ chối.
- [ ] `T20-K03` [I] Một dòng lỗi/thiếu quyền rollback header, mọi chi tiết, kho, chi và nợ.
- [ ] `T20-K04` [I] Trả ngay 0 tạo nợ toàn bộ, không chi 0; trả đủ tạo một chi, không nợ 0.
- [ ] `T20-K05` [I] Trả ngay một phần: chi trả ngay đúng phần, nợ đúng tổng trừ phần đã trả.
- [ ] `T20-K06` [I] Trả ngay âm/vượt tổng hoặc dòng chi sai loại bị chặn.
- [ ] `T20-K07` [I] Nhấn/gọi lại cùng yêu cầu không nhập kho/chi lần hai; đối chiếu theo mã nguồn khi timeout.
- [ ] `T20-K08` [I] Phiếu chốt không sửa/xóa header/NCC/ngày/chi tiết; trigger cũ không cho bypass sửa lịch sử.
- [ ] `T20-K09` [I] Baseline có seed được backfill cờ chốt đúng, không chạy trigger cộng tồn lần nữa.
- [ ] `T20-K10` [I] Hai phiên nhập cùng NVL cập nhật tồn đúng, không lost update.
- [ ] `T20-K11` [U/M] STORE nhập được, CASH không; trả ngay/ghi tài chính chỉ ADMIN hoặc quy trình được cấp quyền rõ.
- [ ] `T20-K12` [M] Tính tổng preview khớp SQL và sau reload nguồn chi/nợ hiển thị đúng.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.


