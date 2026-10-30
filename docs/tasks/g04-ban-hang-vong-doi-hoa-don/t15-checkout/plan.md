# Plan T15 — Checkout nguyên tử và snapshot

**Trạng thái:** Bộ plan G04 đã được duyệt ngày 23/10/2026; đã triển khai trong nhóm, đã chạy đợt test được duyệt; còn FAIL/BLOCKED, xem report.md. **Phụ thuộc:** T14. Áp dụng [plans.md](../plans.md). Không chạy đợt nghiệm thu khi mới duyệt plan.

## 1. Contract và migration011

Giữ dbo.DanhSachThanhToan(MaPT char5, SoTien decimal18,0>0, MaThamChieu nvarchar100NULL), không thêm unique làm mất các dòng cùng phương thức. PaymentTableBuilder dùng lại cho TVP rỗng/nhiều dòng; BLL kiểm scale/precision trước tạo TVP. Không lưu tiền đưa/thối vào allocations.

Thêm bảng `HoaDonUuDaiSnapshot`: MaHD PK/FK; Kind Checkout/LegacyUnknown; Source None/Tier/Promotion nullable cho legacy; TierCode/PromotionCode/PercentDiscount nullable; Discount decimal18,0; RecordedAt; PolicyRevision/CustomerRevision/PromotionRevision nullable. Mã hạng/KM là snapshot text, không FK tới chính sách có thể thay; Discount khớp số đã lưu lúc chốt. Không dùng bảng này làm cache preview đơn mở.

Migration ghi một hàng LegacyUnknown cho mỗi đơn đã trả trước011, Discount từ HOADON.GiamGia, nguồn/tỷ lệ/revision NULL. Không suy ngược nguồn/tỷ lệ hoặc tính lại tồn/điểm/phí/tổng cũ. Đơn mở/hủy không backfill snapshot checkout. Trigger khóa UPDATE/DELETE snapshot và chỉ cho ghi snapshot Checkout khi đơn còn mở trong transaction; runtime DENY DML. Legacy cutover ghi trước bật guard; runner có transaction/checksum. Giữ snapshot phí đối tác trên HOADON và tiêu hao/dòng đã có; không dựng lịch sử công thức.

ALTER sp_ThanhToanHoaDon, bỏ đường runtime chữ ký cũ không actor/preview. Request mới bắt buộc Actor/Stamp, MaHD, ExpectedRevision, RequestId, ExpectedPreviewToken binary32 và TVP. NgayTT runtime lấy SYSDATETIME, không nhận ngày tùy ý. Fixture ngày quá khứ dùng harness test riêng dưới migration principal trong DB test; không GRANT đường import/backdate cho CafeManagerApp. Chữ ký cũ thiếu thông tin phải reject rõ, không fallback baseline.

Thêm sp_SalesCheckoutPreview(Actor/Stamp/MaHD/ExpectedRevision), trả DTO số chính thức, nguồn/hạng/KM/tỷ lệ, phí/tỷ lệ đối tác, điểm dự kiến, OrderRevision, Policy/Customer/Promotion/Partner revisions và token HASHBYTES trên dữ liệu typed canonical. Token gồm tổng/giảm/ship/phải trả, nguồn/tỷ lệ, MaKH/MaKM/MaDT, ngày áp dụng và revision liên quan; policy đổi dù số tiền trùng cũng yêu cầu xác nhận lại. Công thức không nằm trong token tài chính nhưng khóa nhất quán lúc chốt. Preview không ghi tài chính/receipt và không giữ khóa lúc người dùng xác nhận. Preview thất bại không có token hợp lệ.

### Trình tự chốt

1. Transaction + demand + request fence + receipt. Replay đúng payload trả kết quả đã chốt từ receipt/snapshot, không chạy trigger lần nữa; actor/hash khác bị chặn. Request mới trên đơn đã trả báo đã chốt để tải, không thành giao dịch thứ hai.
2. Policy Shared → Recipes Shared → Order lock → header UPDLOCK/HOLDLOCK/version → khách rồi dữ liệu KM/đối tác/payment method. Tính tiền món từ dòng snapshot bằng decimal rộng và kiểm fit18chữ số. Ưu đãi dùng fn_DiscountPreview T13 theo ngày lập và hạng trước checkout. KM không hợp lệ phải reject, không âm thầm dùng0.
3. So token với số/revision mới. Khác trả PreviewChanged và không ghi; đọc preview mới ở call riêng sau rollback, buộc xác nhận lại. Kiểm đủ người nhận/menu/công thức/dòng, tổng>0 và không có tiền/tiêu hao cũ.
4. Tổng TVP bằng phải trả; từng PT tồn tại, PaymentMethodSupport.IsSupported=1 và khácPT010; reference đúng độ dài, tiền dương nguyên. Phải trả0 dùng TVP rỗng; dòng tiền0 vẫn bị chặn. Nhiều dòng cùng PT giữ MaTT riêng.
5. Group tiêu hao theo MaNVL từ mọi dòng/công thức, kiểm precision4 và overflow. Khóa các hàng kho theo MaNVL tăng dần, kiểm đủ tồn; INSERT một batch TIEUHAO_HOADON để trigger trừ một lần. Không UPDATE tồn thêm ở procedure/C#.
6. Đặt GiamGia mới hợp lệ; ghi snapshot ưu đãi và thanh toán; phí đối tác ROUND(tiền món sau giảm × tỷ lệ/100,0); chốt header/NgayTT. Bill0₫ vẫn có món, tổng món dương, tiêu hao và SUMallocation0. TR_HD_Chot hiện có đã kiểm SUMcoalesce0; chỉ bổ sung guard liên kết snapshot khi cần, không bỏ bảo vệ hiện có.
7. Điểm FLOOR(tiền món sau giảm/1000), kiểm cộng số dư không vượt int.MaxValue. Có khách và điểm>0 mới INSERT LICHSU_DIEM; trigger cập nhật điểm/hạng. Điểm0 không tạo lịch sử0. Receipt và DTO kết quả chỉ trả sau commit; fault rollback toàn bộ.

HOADON/dòng/thanh toán/tiêu hao/điểm/snapshot được DENY DML runtime và guard lịch sử; chuyển giao T18 không đổi tài chính. Không tạo phiếu Thu. Phí đối tác0/100% là chi phí quán, không trừ phân bổ khách. Dữ liệu cũ có partial payment/tiêu hao ở đơn mở phải báo preflight/blocker, không tự xóa để checkout.

## 2. Bước và file

| Bước | File/khu vực | Kiểm chứng kỹ thuật sau duyệt plan |
|---|---|---|
| 1. Migration011/preflight/guard | database/migrations/011_checkout.sql | Apply test010 clean/demo; LegacyUnknown đúng, số dư cũ/checksum001–010 giữ nguyên |
| 2. Preview và ghi SQL | Cùng migration; docs/database/checkout-contract.md; data-contract.md/write-ownership.md | Smoke preview→chốt một đơn test→đọc receipt/tồn/điểm; không gọi baseline |
| 3. Models/DAL/BLL | Models/Requests/Sales/CheckoutRequest.cs; Dtos/Sales/CheckoutDtos.cs; SalesRepository.cs/SalesService.cs; PaymentTableBuilder.cs khi cần; SqlErrorMapper.cs | Dispose TVP/reader; đọc nhiều result set; chuẩn hóa allocation/token; quyền/cancellation ghi unknown |
| 4. Test/seed compatibility | database/tests/t15/{fixtures,acceptance-contract,concurrency,faults}.sql; database/demo/g04-sales.sql; tests/CafeManager.Tests/Sales/CheckoutRulesTests.cs; Test-G04.ps1 | Viết kịch bản cho đợt test, build Debug/Release, SQL smoke ghi riêng |
| 5. Đầu vào T16 | checkout-contract.md/migrations.md/technical-checks.md | Đủ selector phương thức, token/DTO preview/kết quả/read/reconcile và map lỗi cho UI |

Dải522xx:52201 preview thay đổi,52202 payment sai,52203 thiếu tồn,52204 menu/công thức sai,52205 overflow/ngày sai,52206 khóa,52207 legacy/preflight; giữ511xx/51504/51603/51608. Error DTO có mã món/NVL thiếu và lượng nếu SQL trả đủ bằng chứng; không đoán từ cache. Không log TVP/raw người nhận/secret.

Baseline/Insert_Data giữ nguyên; dựng demo bằng seed một lần TRƯỚC001–011 để không gọi chữ ký cũ sau nâng cấp. Script demo G04 versioned chỉ chạy có marker trong DB demo/test, tạo dữ liệu mới qua API mới; không UPDATE chứng từ chốt. Legacy dùng snapshot LegacyUnknown có giới hạn; fixture mới kiểm snapshot Checkout đúng nguồn. Không chạy seed demoG04 lên development đã dùng.

## 3. Ma trận nghiệm thu

| ID | Fixture/thao tác | Expected và đối chiếu |
|---|---|---|
| T15-K01 | Một/nhiều PT, hai dòng cùng PT khác/cùng reference, replay request | SUMallocation=phải trả; MaTT riêng; một commit/receipt, không tăng tiền/kho/điểm lần hai |
| T15-K02 | Không khách/KM, hạng, KM thay hạng; đổi điểm/KM/policy/đối tác sau preview, cả số tiền không đổi | SQL tính đúng; token khác reject toàn bộ, preview mới cần xác nhận |
| T15-K03 | Thiếu/dư1₫, PT không có/tắt hỗ trợ/PT010, overflow/reference101,0/âm/lẻ VND | Reject không ghi tiền/tiêu hao/điểm/receipt; TVP không làm tròn input lỗi |
| T15-K04 | KM100%, ship0; TVP rỗng và TVP dòng0 | Rỗng chốt0₫ có tiêu hao, không payment; dòng0 bị chặn |
| T15-K05 | KM100%, ship15.000 | Phải trả15.000 và phân bổ đủ ship, điểm0 |
| T15-K06 | Thiếu một NVL trong nhiều NVL; món ngừng/công thức thiếu qua fixture quản trị | Rollback mọi bảng/số dư, transaction0, đơn mở sửa được |
| T15-K07 | Hai dòng cùng SP, hai SP chung NVL; định mức0.0180/1.2345; bánh định mức1 | Một dòng tiêu hao/NVL, decimal4 đúng, trừ một lần |
| T15-K08 | Net999/1000/1999, ship/phí>0, khách sắp overflow, hai request | Điểm0/1/1;0 không history; overflow reject; mỗi MaHD một lịch sử mua |
| T15-K09 | Đối tác0/100%, số cần làm tròn0.5; đổi tỷ lệ sau thanh toán | PhiDoiTac đúng snapshot/ROUND, không trừ phí khỏi tổng khách, đơn chốt không đổi |
| T15-K10 | Hai session cùng MaHD/cùng hoặc khác RequestId, hai thứ tự | Một commit; phiên còn lại replay/đã chốt, không ghi lặp tiền/kho/điểm |
| T15-K11 | Hai đơn tranh lượng tồn cuối, NVL chung với thứ tự dòng khác nhau | Chỉ đơn đủ tồn commit, tồn không âm, không dữ liệu nửa chừng |
| T15-K12 | Fault test-only sau tiêu hao, sau payment, trước lịch sử điểm | Snapshot/header/dòng/tiền/kho/điểm/receipt về trước; @@TRANCOUNT0/XACT_STATE0 trên session gọi |
| T15-K13 | EXECUTE AS runtime DML header/giảm/tiêu hao/điểm/snapshot; SQL actor STORE/stamp cũ | DENY/role check; ADMIN app cũng không sửa lịch sử trực tiếp hoặc bypass preview |
| T15-K14 | NgayLap tương lai; runtime backdate/chữ ký cũ; fixture import quản trị riêng | NgàyTT trước lập bị chặn; runtime không backdate; không GRANT đường fixture cho app |
| T15-K15 | Baseline→seed một lần→001–011; demoG04 mới; rerun/checksum | Tiền/tồn/điểm legacy không đổi; LegacyUnknown minh bạch; đơn mới snapshot Checkout đúng; không reseed DB đã dùng |

Bổ sung bắt buộc: Recipes/policy sửa đồng thời checkout cả hai chiều; PaymentMethodSupport/đối tác đổi cùng lúc; điều chỉnh điểm đồng thời checkout không mất số dư; timeout trước/sau commit/fence/restart; đọc paid không preview theo chính sách mới. Regression T09-K09, T10-K04/K06–K08 và T13-K01–K03/K09/K11 bằng checkout đích; không đổi nhãn PASS G03.
## 4. Checklist gốc

**Kiểm thử bắt buộc:**

- [ ] `T15-K01` [I] Bill hợp lệ trả 1/nhiều phương thức và nhiều dòng cùng phương thức chốt đúng một lần.
- [ ] `T15-K02` [I] SQL tái tính ưu đãi theo chính sách; preview thay đổi thì trả lỗi xác nhận lại và không thu theo số cũ.
- [ ] `T15-K03` [I] Thiếu/vượt phân bổ, phương thức không có/PT010 bị từ chối và rollback.
- [ ] `T15-K04` [I] Giảm 100%, ship 0 và TVP rỗng: chốt 0 VND hợp lệ, tiêu hao đúng, không dòng thanh toán 0.
- [ ] `T15-K05` [I] Giảm 100% nhưng ship >0: vẫn phải trả đủ phí ship.
- [ ] `T15-K06` [I] Thiếu một nguyên liệu, món thiếu công thức/ngừng bán: rollback toàn kho/tiền/điểm/trạng thái.
- [ ] `T15-K07` [I] Nhiều dòng cùng SP/NVL: tiêu hao group đúng, không trừ hai lần; decimal 4 chữ số lẻ giữ đúng.
- [ ] `T15-K08` [I] Điểm floor sau giảm, không cộng ship/phí đối tác; mỗi MaHD chỉ cộng một lần.
- [ ] `T15-K09` [I] Phí đối tác 0/100% và số lẻ làm tròn đúng; đổi tỷ lệ đối tác không đổi đơn cũ.
- [ ] `T15-K10` [I] Hai session checkout cùng đơn: một commit; session còn lại không tăng tiền/điểm/trừ kho nữa.
- [ ] `T15-K11` [I] Hai đơn dùng lượng tồn cuối: chỉ giao dịch đủ tồn commit; không tồn âm.
- [ ] `T15-K12` [I] Lỗi cưỡng bức sau tiêu hao/sau ghi tiền/trước điểm rollback tất cả và @@TRANCOUNT về trạng thái hợp lệ.
- [ ] `T15-K13` [I] Gọi trực tiếp sửa TrangThai/GiamGia/tiêu hao thay procedure bằng quyền app bị chặn.
- [ ] `T15-K14` [I] Ngày thanh toán trước lập đơn bị chặn; ngày mua/import dùng đường quản trị riêng.
- [ ] `T15-K15` [I] Chạy lại seed baseline hoặc bản seed migration đã ghi nhận thay đổi: tổng/snapshot/điểm khớp hợp đồng mới.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.






