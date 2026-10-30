# Plan T18 — Theo dõi và chuyển trạng thái giao hàng

**Trạng thái:** Bộ plan G04 đã được duyệt ngày 23/10/2026; đã triển khai trong nhóm, đã chạy đợt test được duyệt; còn FAIL/BLOCKED, xem report.md. **Phụ thuộc:** T17. Áp dụng [plans.md](../plans.md), UX9/K22 và contract tài chính chỉ đọc sau chốt. Đã triển khai/chạy một phần nghiệm thu; chưa đủ PASS.

## 1. Quyết định phạm vi và trạng thái

Chỉ lưu TrangThaiGiao hiện tại trên HOADON, không thêm bảng lịch sử chuyển bước hoặc timeline. Màn hình và hướng dẫn ghi rõ giới hạn; T31-K05 chỉ đối chiếu trạng thái hiện tại theo quyết định đã duyệt, không đòi dữ liệu lịch sử không được lưu. Không sửa hồi tố lịch sử G03 hoặc tạo timestamp chuyển giao giả từ NgayLap/NgayThanhToan.

| Trạng thái tài chính | Trạng thái giao hiện tại | Thao tác được phép |
|---|---|---|
| Chưa thanh toán / Đã thanh toán | Cho giao | Xác nhận bắt đầu → Dang giao |
| Chưa thanh toán / Đã thanh toán | Dang giao | Xác nhận hoàn tất → Da giao |
| Chưa thanh toán | Cho giao / Dang giao / Da giao | Hủy đơn qua sp_HuyHoaDon T16 nếu chưa có tiền/tiêu hao → tài chính Huy, giao Huy giao |
| Đã thanh toán | Bất kỳ trạng thái giao hợp lệ | Không hủy tài chính hoặc Huy giao; chỉ bước tiến hợp lệ, hoặc hướng dẫn xử lý thủ công nếu giao thất bại |
| Huy | Huy giao | Chỉ đọc, không mở lại/chuyển tiến độ |
| Chưa thanh toán / Đã thanh toán | Da giao | Chỉ đọc tiến độ; chưa thanh toán vẫn phải checkout qua POS, không suy là đã thu |

Mỗi lần chỉ chuyển một bước; không đi lùi, nhảy bước, đổi loại đơn hay đổi thông tin tài chính. Các đơn legacy đã có trạng thái cuối giữ nguyên, không giả lịch sử bước trung gian. Thông tin người nhận bắt buộc hợp lệ ở tạo/sửa đơn và kiểm lại khi chuyển giao. Đơn giao chưa trả không tự bị chốt khi giao xong; đơn đã giao chưa trả vẫn xuất hiện trong tra cứu/đơn mở cần thu.

Giao thất bại của đơn đã trả: thông báo liên hệ quản lý xử lý thủ công ngoài phạm vi, giữ trạng thái DB hiện tại; không nút hoàn tiền, không ghi nhận thất bại bền vững nếu không có cột lưu. Đây là hướng dẫn thao tác, không là audit log.

## 2. Contract SQL/DAL/BLL

Migration014 thêm `sp_DeliveryList`, `sp_DeliveryGet`, `sp_ChuyenTrangThaiGiaoHang`. Actor/Stamp ADMIN/CASH cho mọi call, STORE bị chặn. List nhận mã/SDT người nhận/trạng thái giao và trạng thái thanh toán tùy chọn, Skip; chỉ LoaiDon=Giao hang, phân trang50, order NgayLap DESC/MaHD, metadata ReadAt/count. Search escape wildcard, SDT là chuỗi giữ số0 đầu. Get dùng lại InvoiceDetail theo MaHD sau kiểm LoaiDon hoặc query projection hẹp, không tạo bản tổng tài chính khác.

Write nhận MaHD, ExpectedRevision, ExpectedDeliveryStatus, TargetStatus, RequestId. Transaction/request fence/receipt → Order lock/header/version → kiểm tài chính không Huy, LoaiDon đúng, người nhận đầy đủ, bước kế tiếp đúng → chỉ UPDATE TrangThaiGiao → receipt/commit và DTO trạng thái/Revision mới. Không cập nhật MaKH/MaDT/NgayThanhToan/TongTien/GiamGia/PhiShip/PhanTramPhiDoiTac/PhiDoiTac, không payment/tiêu hao/điểm. Khóa cùng Order với checkout/hủy để hai thao tác cùng nguồn được tuần tự hóa; stale version/status trả conflict để reload.

Giữ TR_HD_Chot bảo vệ tài chính đã trả và snapshot T15 bất biến; không thêm DML trực tiếp cho runtime. Không nhận ActorRole từ UI làm quyền. Receipt operation SalesDelivery dùng helper và hash như plans.md. Timeout giữ request/status kỳ vọng, đối chiếu receipt+header sau fence; replay cùng payload không chuyển thêm bước. Không tự lấy status mới rồi gửi tiếp TargetStatus khác dưới request cũ.

Dải525xx:52501 không phải đơn giao/không tìm thấy,52502 bước không hợp lệ,52503 người nhận/dữ liệu giao không hợp lệ; version/receipt dùng52103/51608, khóa lỗi cần đối chiếu. Error map an toàn tiếng Việt; không log tên/SDT/địa chỉ đầy đủ nếu không cần.

## 3. Bố cục và các bước thực hiện

Dùng mẫu danh sách Orbit hiện có: filter mã/SDT/trạng thái + grid/paging + badge tài chính và giao riêng. Chọn dòng mở detail người nhận/đối tác, nút duy nhất cho bước tiếp theo hợp lệ, có xác nhận mã/người nhận/bước cũ→mới. Danh sách không cho sửa trực tiếp ô trạng thái. Có Xem hóa đơn/Quay lại POS khi đơn mở; hủy chỉ dùng contract T16, không thêm một đường Huy giao độc lập bypass hủy hóa đơn.

Dialog detail mục tiêu760×560logical, clamp WorkingArea, field/grid cuộn và footer cố định. Filter wrap ở client nhỏ, tên/địa chỉ nhiều dòng,1280×720/1366×768 và125% kiểm chức năng/bounds; hình thức SKIPPED,100/150% NOT RUN khi chưa chạy. Không stardust trên nút giao; dùng OrbitButton hiện có.

| Bước | File/khu vực | Kiểm chứng kỹ thuật sau duyệt plan |
|---|---|---|
| 1. Migration014 | database/migrations/014_delivery.sql; docs/database/delivery-contract.md | Apply sau013, smoke chuyển một bước trên đơn test và đối chiếu tài chính/số dư bất biến |
| 2. Models/DAL/BLL | Models/Requests/Sales/DeliveryRequests.cs; Dtos/Sales/DeliveryDtos.cs; DataAccess/Repositories/DeliveryRepository.cs; Business/Services/DeliveryService.cs; SqlErrorMapper.cs | Typed enum ánh xạ DB đúng, quyền Sales/session, cancellation ghi unknown, không tính tài chính |
| 3. WinForms | Modules/Sales/DeliveryControl.cs + Designer; DeliveryDetailDialog.cs; Program/MainForm/NavigationController | Menu ADMIN/CASH, STORE không vào; load/empty/error/conflict/unknown, giữ filter/selection khi reload |
| 4. Fixture/test | database/tests/t18/{acceptance-contract,concurrency}.sql; tests/CafeManager.Tests/Sales/DeliveryTransitionTests.cs; tools/ui/Test-G04Functions.ps1 | Chuẩn bị ma trận transition/role, hai session và UI thật, không nghiệm thu trước duyệt riêng |
| 5. Hồ sơ/handoff | docs/database/{delivery-contract,migrations,write-ownership}.md; UX_QuanLiQuanCafe.md mục9/17 ghi rõ chỉ hiện trạng; development-guide.md; Update-DevelopmentG04.ps1; technical-checks.md | Ghi schema cuối014 thực tế sau apply, kịch bản dùng thử3role, phạm vi demo; không đánh G04PASS từ smoke |

## 4. Ma trận nghiệm thu sau duyệt đợt test

| ID | Fixture/thao tác | Expected/bằng chứng |
|---|---|---|
| T18-K01 | Đơn3kênh, giao thiếu/trắng người nhận/SDT/địa chỉ bằng fixture quản trị test | Chỉ đơn giao vào module; thiếu thông tin bị chặn theo contract, không nhận dữ liệu kênh khác |
| T18-K02 | Mọi cặp trạng thái gồm giữ nguyên/đi lùi/nhảy bước, mở và paid | Chỉ Cho giao→Dang giao→Da giao; cùng request replay không thành bước mới; sai không ghi receipt |
| T18-K03 | Paid chuyển2bước, sửa policy/partner sau paid | Tiền/khách/đối tác/ngàyTT/snapshot/dòng/payment/tiêu hao/kho/điểm giữ nguyên; chỉ trạng thái giao+Revision/receipt đổi |
| T18-K04 | Hủy giao đơn mở ở các bước, thử hủy paid, cạnh tranh checkout/hủy/giao | Hủy mở qua T16 đúng, không hoàn kho; paid không hủy tài chính; stale reload không ghi đè |
| T18-K05 | Hai session cùng version/status, khác target; một phiên đọc cũ sau bước mới | Một bước commit, stale conflict; không nhảy/lùi hoặc ghi đè kết quả mới |
| T18-K06 | ADMIN/CASH/STORE, actor không tồn tại/inactive/nghỉ/stamp cũ; direct service/SQL | ADMIN/CASH thao tác hợp lệ; STORE/actor sai chặn, revoke không callback UI cũ |
| T18-K07 | Filter mã/SDT0đầu/trạng thái,>50đơn, reload/restart, clear filter | Đúng tập/trang, trạng thái bền vững, pending/error không giả rỗng; query muộn không đổi bộ lọc mới |
| T18-K08 | Giao thất bại đơn paid tại Chờ/Đang giao, mở lại chi tiết/in | Hướng dẫn thủ công rõ, DB giữ trạng thái thật, không hoàn tiền/giảm doanh thu hoặc timeline giả |

Bổ sung timeout trước/sau commit/fence, request hash khác, SQLDMLruntime, đọc lại đơn đã hoàn tất ở lịch sử T17; UI keyboard/busy/125% theo nhóm. RegressionT15-K10 vàT16-K06/K07/K08 với giao đồng thời; đối chiếu toàn bộ tiền/kho/điểm/snapshot, không chỉ message. UX-K22 và giới hạn T31-K05 được ghi vào contract/UX trước code.

## 5. Checklist gốc

**Kiểm thử bắt buộc:**

- [ ] `T18-K01` [I/M] Chỉ đơn Giao hang có màn hình và dữ liệu người nhận hợp lệ.
- [ ] `T18-K02` [I] Cho giao → Dang giao → Da giao được; nhảy lùi/nhảy sai trình tự bị chặn.
- [ ] `T18-K03` [I] Chuyển trạng thái giao của đơn đã trả không đổi tiền/đối tác/khách/ngày thanh toán.
- [ ] `T18-K04` [I/M] Đơn mở chưa trả hủy giao/hủy đơn đúng; không dùng hủy giao để hoàn tiền đơn đã trả.
- [ ] `T18-K05` [I] Hai session cập nhật từ trạng thái cũ: phát hiện xung đột, không ghi đè kết quả mới.
- [ ] `T18-K06` [U/M] CASH/ADMIN được cập nhật; STORE bị chặn.
- [ ] `T18-K07` [M] Bộ lọc tìm theo mã/SDT/trạng thái, refresh sau thao tác và reload còn dữ liệu.
- [ ] `T18-K08` [M] Giao thất bại đơn đã trả có hướng dẫn xử lý thủ công, không hiển thị đã hoàn tiền tự động.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.





