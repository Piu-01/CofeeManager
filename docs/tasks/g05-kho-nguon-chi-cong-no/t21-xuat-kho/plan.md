# T21 — Xuất hủy và xuất trả nguyên liệu

26/10/2026. **Bộ plan đã duyệt26/10; đã triển khai source, chờ đợt test**. Phụ thuộc T20/schema018. Đọc [contract chung](../plans.md). Chưa chạy đợt nghiệm thu.

## 1. Contract và khóa tồn

Migration019 thêm PHIEUXUAT.DaChot, Revision rowversion, CutoverKind, NgayChot nullable và LyDo nvarchar(200) nullable. Phiếu mới bắt buộc lý do; phiếu cũ thiếu giữ NULL và ghi rõ chưa lưu, không bịa. Backfill đã chốt chỉ cho dữ liệu hợp lệ, không ghi chi tiết hoặc trừ kho lại.

TVP StockIssueLinesInput nhận MaNVL miền rộng, Quantity decimal(38,18). sp_LapPhieuXuat(actor/stamp/request, ngày, loại, lý do, TVP) chỉ ADMIN/STORE; loại đúng Huy do hong hoặc Tra hang, kiểm rỗng/trùng/FK/precision. Receipt → SeqPhieuXuat → khóa từng NVL theo mã tăng dần → kiểm đủ tất cả → header chưa chốt → chi tiết → TR_CTPX_Kho trừ tồn → DaChot1 → receipt/commit. Thiếu một dòng rollback toàn phiếu, không tự bỏ dòng thiếu.

Giữ thứ tự khóa tương thích checkout; không lấy khóa đơn/khách/recipe sau NVL và không sửa tiêu hao/công thức. Xuất trả không sinh Thu/Chi hoặc cấn nợ. Xuất hủy sau hủy đơn là lượng thực người dùng nhập; hướng dẫn kiểm nguồn, không tự lấy đơn đã checkout để ghi tiêu hao kép. Không tự thêm FK hóa đơn mới.

Guard header/chi tiết đã chốt không sửa/xóa/reset; runtime DENY DML, phiếu chưa chốt chỉ tồn tại trong transaction. Public sp_StockIssueList/Get/Receipt và sp_LapPhieuXuat. Lookup dùng T20. Sequence PX khởi tạo tránh mã cũ, mở rộng cấp mã đúng task. Lỗi528xx theo index.

## 2. Bước/file/kiểm kỹ thuật

| Bước | File dự kiến | Đầu ra |
|---|---|---|
| 1 | database/migrations/019_stock_issues.sql | Preflight018, schema/type/procs/guards/rights; compile và smoke DB riêng |
| 2 | src/CafeManager.Models/Requests/Inventory/StockIssueRequests.cs; Dtos/Inventory/StockIssueRow.cs, StockIssueDetail.cs | Request/line/result typed, lý do legacy nullable |
| 3 | src/CafeManager.DataAccess/Repositories/StockIssueRepository.cs, StockIssueTableBuilder.cs, SqlErrorMapper.cs | TVP/query async, receipt và lỗi nghiệp vụ |
| 4 | src/CafeManager.Business/Services/StockIssueService.cs | Demand Inventory, session/request, normalize, đối chiếu kết quả |
| 5 | src/CafeManager.WinForms/Modules/Inventory/{StockIssuesControl,StockIssueEditorDialog,StockIssueDetailDialog}.cs | Nháp → xem lại → chốt; input giữ khi lỗi, detail chỉ đọc |
| 6 | InventoryControl.cs và Program.cs | Thêm tab Xuất cho ADMIN/STORE, bảo vệ nháp khi rời; không đổi menu bán hàng |
| 7 | tests/CafeManager.Tests/Inventory/StockIssueRulesTests.cs; database/tests/g05/t21-issues.sql; technical-checks.md | Build Debug/Release và smoke tối thiểu, chưa nghiệm thu10ca |

Editor900×600/co640×420 logical theo index, header loại/ngày/lý do, grid NVL/đơn vị/lượng và footer chốt. Trả hàng có notice chưa hoàn tiền/cấn nợ. Timeout giữ request và nút Kiểm tra kết quả. Không bụi sao.

## 3. Cutover và fixture

Preflight header/loại/qty/FK/phiếu rỗng, dữ liệu sai dừng rollback/version không ghi. Không tự đổi loại xuất ra quầy thành hủy. Snapshot header/chi tiết/tồn/tiền trước/sau không đổi.

Fixture hai NVL tồn10.0000/0.5000, đơn checkout tranh tồn cuối, một đơn đã pha nhưng hủy mở chưa ghi tiêu hao, xuất cũ hợp lệ và clone invalid. Principal negative riêng để thử guard trực tiếp, không mở quyền app.

## 4. Kiểm thử sau duyệt

| ID | Bằng chứng cần có |
|---|---|
| T21-K01 | Native soạn/bỏ nháp không đổi kho; chốt hủy/trả nhiều dòng, delta/receipt SQL khớp |
| T21-K02, T21-K03 | Loại ra quầy/lạ, lượng zero/âm/precision, FK/trùng bị chặn không để header rác |
| T21-K04, T21-K05 | Bằng tồn về0; thiếu dòng cuối rollback cả dòng đầu, transaction0 |
| T21-K06 | Xuất/checkout cùng tồn cuối, cả hai thứ tự, barrier quan sát; chỉ giao dịch đủ tồn commit, không âm/hao kép |
| T21-K07 | Double click/replay/timeout trước+sau commit/restart một PX; sửa/reset/xóa header/chi tiết bị chặn |
| T21-K08 | Hủy đơn mở đã pha rồi xuất lượng thực; không TIEUHAO_HOADON cho đơn hủy và chỉ giảm kho theo PX |
| T21-K09, T21-K10 | Notice trả hàng, chi/nợ không đổi; ba role UI/BLL/SQL, reload lịch sử/tồn đúng |

Lưu test/bug tại task theo đợt; hồi quy checkout theo ảnh hưởng, không suy PASS16ca G04. Đủ đầu ra019 mới triển khai T22.

## Checklist gốc — giữ nguyên expected

### T21 — Xuất hủy và xuất trả nguyên liệu

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Ghi giảm tồn có chứng từ, tách khỏi bán hàng và chuyển ra quầy. |
| Phạm vi | Procedure sp_LapPhieuXuat đề xuất, TVP, cờ chốt và khóa chứng từ; không tự hoàn/cấn trừ tiền trả hàng. |
| Quy mô | M — transaction kho |
| Phụ thuộc | T20 |
| Đầu ra | Nháp xuất và chốt nguyên tử; lịch sử bất biến. |

**Kiểm thử bắt buộc:**

- [ ] `T21-K01` [M/I] Nháp không trừ tồn; lưu Huy do hong/Tra hang giảm đúng nhiều nguyên liệu.
- [ ] `T21-K02` [I] LoaiXuat=Xuat ra quay hoặc loại lạ bị chặn theo mô hình một kho.
- [ ] `T21-K03` [I] Lượng 0/âm/quá precision, NVL/FK sai, trùng NVL bị chặn.
- [ ] `T21-K04` [I] Xuất bằng tồn đưa tồn 0 hợp lệ; hơn tồn rollback toàn chứng từ.
- [ ] `T21-K05` [I] Một dòng thiếu kho sau dòng khác hợp lệ không để dòng đầu bị trừ dở.
- [ ] `T21-K06` [I] Hai xuất/checkout tranh lượng tồn cuối: không âm, chỉ các giao dịch đủ tồn commit.
- [ ] `T21-K07` [I] Chốt lại/retry không trừ lần hai; sửa/xóa chứng từ chốt bị chặn.
- [ ] `T21-K08` [M/I] Xuất hủy cho đồ đã pha nhưng hủy đơn không đồng thời ghi tiêu hao bán cùng lượng.
- [ ] `T21-K09` [M] Xuất trả hiển thị rõ chưa tự hoàn tiền/cấn trừ nợ.
- [ ] `T21-K10` [U/M] STORE/ADMIN thao tác được, CASH bị chặn; reload đúng lịch sử/tồn.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.


