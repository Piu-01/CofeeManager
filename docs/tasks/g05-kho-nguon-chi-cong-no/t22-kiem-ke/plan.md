# T22 — Kiểm kê và khóa điều chỉnh sau chốt

26/10/2026. **Bộ plan đã duyệt26/10; đã triển khai source, chờ đợt test**. Phụ thuộc T21/schema019. Đọc [contract chung](../plans.md). Chưa chạy đợt nghiệm thu.

## 1. Snapshot, số đếm và chốt

Migration020 thêm PHIEUKIEMKE.SnapshotAt datetime2(7) NULL, Revision rowversion và CutoverKind; CHITIET_KK thêm MaterialRevision binary(8) NULL. TonThucTe cho NULL khi chưa đếm, CHECK NULL hoặc ≥0; không mặc định0. Dữ liệu cũ giữ cờ và số đếm, không gán revision hiện tại thành snapshot lịch sử. Phiếu cũ chưa chốt mà thiếu metadata chỉ xem tham khảo, phải tạo snapshot mới nếu muốn kiểm kê; không sửa ngày để vượt điều kiện.

sp_StocktakeCreate(actor/stamp/request, scopeTVP) chỉ ADMIN/STORE, cấp SeqKiemKe/mãKK và ngày hôm nay. SQL lấy snapshot nhất quán bằng khóa Shared/HOLDLOCK từng NVL tăng dần trong transaction ngắn, lưu header/chi tiết và receipt rồi commit trước khi đếm. Scope chọn NVL hoặc toàn bộ; trả thời điểm, NVL/tên/đơn vị, tồn và revision. Snapshot bền vững do SQL tạo, không tin TVP do client đưa TonHeThong. Đây là phiếu kiểm kê, không thêm lưu nháp DB cho nhập/xuất.

sp_StocktakeSaveCounts nhận MaKK/ExpectedRevision/request riêng và TVP(MaNVL, Quantity nullable miền rộng, LyDo). Khóa header chưa chốt, kiểm đúng scope đã lưu; chỉ lưu số đếm/lý do, không đổi snapshot/ngày/nhân viên. Sửa số đếm làm tăng header revision để chống ghi đè. Lý do ≤100, chênh lệch bắt buộc lý do khi chốt. Nút Điền bằng hệ thống chỉ áp dụng cho dòng người dùng chủ động chọn.

ALTER sp_ChotKiemKe nhận actor/stamp/request/ExpectedRevision: request → khóa KK → kiểm hôm nay/chưa chốt/không rỗng/đủ số đếm/lý do → khóa NVL tăng dần → đối chiếu cả lượng tồn và MaterialRevision → UPDATE tồn đúng số thực một lần → DaDieuChinh1 → receipt → commit. Revision phát hiện cả trường hợp tồn thay đổi rồi trở về lượng cũ. Đổi metadata cũng yêu cầu lấy lại để an toàn.

Snapshot lệch trả lỗi rõ và mã NVL nếu có; không ghi đè kho mới. Lấy lại tạo phiếu/request mới sau xác nhận, số đếm cũ chỉ tham khảo; không tự chuyển số đếm sang snapshot mới. Replay Create trả snapshot gốc. Guard khóa header và chi tiết đã chốt, không reset DaDieuChinh. Cấp lại EXEC runtime chỉ sau đổi chữ ký kiểm actor. Public List/Get/Create/SaveCounts/Receipt; không cho DML app hoặc cập nhật tồn lần hai qua trigger. Lỗi529xx theo index.

## 2. Bước/file/kiểm kỹ thuật

| Bước | File dự kiến | Đầu ra |
|---|---|---|
| 1 | database/migrations/020_stocktakes.sql | Preflight019, metadata/count nullable/sequence/procs/guards/rights; không đổi tồn legacy |
| 2 | src/CafeManager.Models/Requests/Inventory/StocktakeRequests.cs; Dtos/Inventory/StocktakeRow.cs, StocktakeDetail.cs | Snapshot, count nullable, revision và phản hồi xung đột typed |
| 3 | src/CafeManager.DataAccess/Repositories/StocktakeRepository.cs, StocktakeTableBuilder.cs, SqlErrorMapper.cs | TVP/query, receipt tách operation Create/Save/Commit |
| 4 | src/CafeManager.Business/Services/StocktakeService.cs | Demand Inventory, normalize/count/lý do, request riêng từng bước, pending |
| 5 | src/CafeManager.WinForms/Modules/Inventory/{StocktakesControl,StocktakeEditorDialog,StocktakeDetailDialog}.cs | Scope → snapshot → đếm/lưu → xem lệch → chốt; chưa đếm khác0 |
| 6 | InventoryControl.cs, Program.cs | Tab Kiểm kê, reload tồn sau chốt, bảo vệ nháp/unknown khi rời |
| 7 | tests/CafeManager.Tests/Inventory/StocktakeRulesTests.cs; database/tests/g05/t22-stocktakes.sql; technical-checks.md | Build Debug/Release và smoke DB riêng; chưa nghiệm thu11ca |

Editor900×600/co640×420 logical theo index: thời điểm snapshot, cột tồn hệ thống/thực đếm/chênh lệch/đơn vị/lý do. Lưu số đếm và Chốt kiểm kê là hai bước rõ; lưu chưa đổi kho. Notice cần tạm dừng phát sinh để chốt; không giữ khóa khi nhập/đếm/modal. Phiếu cũ/đã chốt không có Reset/Chốt lại. Không bụi sao.

## 3. Preflight và kiểm thử sau duyệt

Kiểm FK/header/chi tiết, tồn/count không âm; giữ MaterialOpening, tiêu hao, PN/PX và kiểm kê cũ. Không điều chỉnh phiếu cũ chưa chốt khi migration. Fixture có đủ/thiếu đếm, lệch/không lệch, ngày cũ/tương lai/rỗng, hai NVL và snapshot ABA.

| ID | Bằng chứng cần có |
|---|---|
| T22-K01, T22-K10 | Snapshot/time/revision đúng; khi native đang đếm phiên khác vẫn giao dịch được, notice tạm dừng rõ |
| T22-K02, T22-K03 | NULL khác0; zero/bằng/lệch, thiếu/đủ lý do; âm/precision/trùng/scope thiếu-thừa bị chặn |
| T22-K04, T22-K05 | Chốt hôm nay delta một lần; replay không đổi tồn; second request/revision cũ/reset/sửa header-CT bị chặn |
| T22-K06 | Ngày cũ/tương lai/rỗng/legacy thiếu snapshot không chốt; không sửa ngày để PASS |
| T22-K07, T22-K08 | Một NVL lệch rollback toàn bộ; ABA bị từ chối dù lượng bằng snapshot, transaction0 |
| T22-K09 | Hai session cùng KK và KK/checkout cả hai thứ tự, barrier quan sát, không mất biến động kho |
| T22-K11 | Native ADMIN/STORE, CASH chặn UI/BLL/SQL; lưu/reload đúng; timeout trước/sau chốt đối chiếu từng request |

Guard được kiểm thêm bằng principal negative riêng, không chỉ suy ra từ DENY app. Biên bản/bug tại task theo đợt; chưa đánh Hoàn tất trong triển khai.

## Checklist gốc — giữ nguyên expected

### T22 — Kiểm kê và khóa điều chỉnh sau chốt

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Hoàn thiện cả header và chi tiết để kiểm kê thực sự chốt một lần. |
| Phạm vi | sp_ChotKiemKe hiện có, sửa bảo vệ header, màn hình snapshot/số đếm/lý do. |
| Quy mô | M — điều chỉnh tồn có kiểm soát |
| Phụ thuộc | T21 |
| Đầu ra | Phiếu kiểm kê có thời điểm/snapshot, chốt một lần và lịch sử đọc lại. |

**Kiểm thử bắt buộc:**

- [ ] `T22-K01` [I/M] Tạo snapshot đúng tồn tại thời điểm lấy, chưa chốt chưa đổi tồn.
- [ ] `T22-K02` [I] Số đếm 0/bằng tồn/khác tồn hợp lệ; âm/quá precision/NVL trùng bị chặn.
- [ ] `T22-K03` [I/M] Khác tồn yêu cầu lý do, thiếu lý do bị từ chối.
- [ ] `T22-K04` [I] Hôm nay/chưa chốt/snapshot khớp: điều chỉnh đúng một lần.
- [ ] `T22-K05` [I] Chốt lại, reset DaDieuChinh, đổi ngày/nhân viên, sửa/xóa chi tiết sau chốt bị chặn.
- [ ] `T22-K06` [I] Phiếu ngày cũ/ngày tương lai/rỗng không chốt; không tự sửa ngày để né điều kiện.
- [ ] `T22-K07` [I] Phát sinh kho sau snapshot: từ chối, không ghi đè tồn mới bằng số cũ.
- [ ] `T22-K08` [I] Nhiều NVL một dòng snapshot lệch: toàn bộ rollback.
- [ ] `T22-K09` [I] Hai session chốt cùng phiếu/checkout song song được tuần tự hóa hoặc từ chối hợp lệ; không mất biến động kho.
- [ ] `T22-K10` [M] Không giữ khóa DB trong thời gian người dùng đếm; UI nêu cần tạm dừng phát sinh để chốt.
- [ ] `T22-K11` [U/M] STORE/ADMIN được; CASH không; reload trạng thái và chênh lệch đúng.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.


