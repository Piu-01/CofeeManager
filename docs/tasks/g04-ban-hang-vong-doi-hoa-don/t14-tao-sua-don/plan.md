# Plan T14 — Tạo đơn và sửa dòng món nguyên tử

**Trạng thái:** Bộ plan G04 đã được duyệt ngày 23/10/2026; đã triển khai trong nhóm, đã chạy đợt test được duyệt; còn FAIL/BLOCKED, xem report.md. **Phụ thuộc:** T13. Áp dụng toàn bộ quyết định, fixture, quyền và quy trình tại [plans.md](../plans.md). Đã triển khai/chạy một phần nghiệm thu; chưa đủ PASS.

## 1. Contract chốt

Migration010 sau009: thêm HOADON.Revision rowversion; DENY DML CHITIET_HD cho CafeManagerApp; giữ DENY HOADON/tiền/tiêu hao và filtered unique bàn mở hiện có. Preflight kiểm tổng với dòng, giảm≤tổng, các FK/loại/người nhận và một đơn mở/bàn; ghi rõ mã lỗi dữ liệu cũ, không tự sửa/xóa. Không backfill hoặc tính lại tiền/điểm/kho của đơn lịch sử.

Các entry point mới nhận Actor/Stamp; tham số mã/tên SQL đủ rộng để validate trước cast, tiền không âm/nguyên VND, số ly SQL bigint kiểm1..int.MaxValue trước cast int. Chữ ký chi tiết:

| Procedure | Input nghiệp vụ | Output |
|---|---|---|
| sp_TaoHoaDon | MaHD cấp trước, RequestId, LoaiDon, MaBan?, MaKH?, MaKM?, MaDT?, người nhận/SDT/địa chỉ?, PhiShip, FirstProduct, FirstQuantity, FirstNote? | MaHD/Revision/trạng thái và DTO đơn sau tạo; lỗi bàn đã có đơn kèm đường đọc mã đơn thật |
| sp_SuaDongHoaDon | MaHD, ExpectedRevision, RequestId, Action Add/Update/Delete, MaDong?, MaSP? cho Add, Quantity?, Note? | DTO đơn mới; thao tác Update/Delete kiểm MaDong thuộc MaHD |
| sp_SuaThongTinHoaDon | MaHD, ExpectedRevision, RequestId, MaKH?, MaKM?, bộ thông tin giao hiện tại nếu LoaiDon=Giao hang | DTO đơn và preview mới; loại/bàn/người tạo/ngày không thay |
| sp_SalesOrderGet | MaHD | Header+dòng theo snapshot, preview SQL hợp lệ hoặc lý do không thể preview, ReadAt |
| sp_SalesOpenOrders | Search mã/khách, Channel?, Skip | Trang50đơn mở, sort NgayLap DESC/MaHD, ReadAt; không tải toàn lịch sử |
| sp_SalesMenu / sp_SalesCategories / sp_SalesPartners | Search/Category/Skip khi phù hợp | Lookup hẹp cho CASH/ADMIN: tên/giá/trạng thái/có công thức, danh mục, đối tác+tỷ lệ; không lộ dữ liệu khác |
| sp_SalesReconcile | RequestId, MaHD, Operation | Receipt đúng actor/operation và trạng thái/version hiện tại sau request fence |

SalesOrderGet đọc trong transaction ngắn với khóa phù hợp để header/dòng nhất quán; một SQL command, nhiều result set, reader dispose trước trả. Không giữ transaction khi UI nhập. Preview lấy fn_DiscountPreview theo NgayLap của đơn và khách/KM hiện tại; KM hết hiệu lực/sai bị báo chưa có preview, vẫn đọc được đơn để sửa hoặc bỏ KM, không biến thành giảm0 hợp lệ. Chỉ đơn còn mở tính preview mới; bản đã chốt không gọi function hiện tại để hiển thị tiền lịch sử.

Tạo: lấy request fence/receipt → Policy Shared → Recipes Shared → Order lock → khóa bàn nếu tại quán → kiểm actor và FK → INSERT header tổng/giảm0 → INSERT dòng đầu lấy SANPHAM.GiaBan → trigger cập nhật tổng → tính preview/hạ giảm hợp lệ → lưu receipt → commit. Không header rỗng tồn tại sau lỗi. Nếu bàn đã có đơn mở từ phiên khác, trả conflict và đọc lại sp_TableSelectCurrent; không thêm món của request thất bại vào đơn đã có.

Sửa dòng: version đúng/đơn mở/không tiền/tiêu hao → hạ GiamGia0 trước thay dòng để tránh CHECK trung gian → Add lấy giá DB, Update giữ GiaBan và MaSP snapshot, Delete đúng MaDong → trigger tổng → tính lại preview → receipt/commit. Mỗi Add tạo dòng riêng, kể cả cùng món/cùng ghi chú; muốn tăng số lượng chọn sửa đúng dòng, không tự merge. Update chỉ lượng/ghi chú có thể thực hiện để giảm/bỏ món đã ngừng bán, nhưng không cho tăng lượng món ngừng/thiếu recipe; checkout vẫn chặn món không hợp lệ. Xóa dòng cuối giữ header mở tổng/giảm0; không tự hủy.

Sửa thông tin: khóa/version như sửa dòng; đơn tại quán/mang đi chỉ khách/KM, đơn giao còn Chờ giao cho đổi người nhận/SDT/địa chỉ/đối tác/ship; không chuyển loại/bàn. SQL tính preview mới; không cho form gửi TongTien/GiamGia/GiaBan. Mọi cập nhật bump Revision kể cả chỉ ghi chú; lỗi bất kỳ không receipt và rollback header/dòng/giảm.

MaNV từ TAIKHOAN đã qua sp_G03Demand và NHANVIEN.DangLamViec, MaKH NULL là vãng lai. Thời gian SYSDATETIME tại SQL, không runtime input NgayLap. Số lượng/tổng overflow reject trước ghi; không saturation hoặc silent rounding.

## 2. Bước, file và kiểm chứng kỹ thuật

| Bước | File/khu vực | Kết quả kiểm chứng trong triển khai sau duyệt plan |
|---|---|---|
| 1. Preflight và migration010 | database/migrations/010_sales_orders.sql; docs/database/sales-contract.md, migrations.md, write-ownership.md | Apply trên DB test clean/demo009; header/dòng cũ giữ nguyên ngoài Revision, runtime chỉ execute được API có quyền |
| 2. Models và DAL | Models/Requests/Sales/SalesRequests.cs; Dtos/Sales/SalesOrderDto.cs; States/SalesStates.cs; DataAccess/Repositories/SalesRepository.cs | Typed nullable/binary8; đọc nhiều result set, trim CHAR; token/cancellation ghi map RequiresReconciliation; không trả reader/connection |
| 3. BLL | Business/Services/SalesService.cs; DataAccess/SqlErrorMapper.cs | PermissionService.Sales+session trước call và kiểm session sau await; chuẩn hóa biên độ/tên/NULL, không tính nghiệp vụ giảm/kho |
| 4. Kịch bản kỹ thuật/fixture | database/tests/t14/{fixtures,acceptance-contract,concurrency,faults}.sql; tests/CafeManager.Tests/Sales/SalesRulesTests.cs; tools/database/Test-G04.ps1 | Chuẩn bị test nhưng chưa chạy nghiệm thu; smoke tạo1đơn/sửa1dòng/read/reconcile trên DB riêng, Debug/Release build |
| 5. Bàn giao đầu vào T15 | docs/database/sales-contract.md; technical-checks.md tại task | Ghi schema/hash/lệnh/kết quả; đủ MaHD/revision/header/dòng/preview cho checkout. Thiếu contract chặn T15 phải báo |

SQL lỗi mới dải521xx:52101input/loại,52102notfound/closed,52103revision,52104bàn đang có đơn,52105món/FK không hợp lệ,52106khóa,52107preflight,52108request mismatch (giữ51608khi helper phát). Map an toàn tiếng Việt và phân biệt xung đột với dữ liệu sai; không hiển thị stack/SQL thô. Khóa/timeout sau ghi luôn đối chiếu.

## 3. Ma trận nghiệm thu sau duyệt đợt test

Dùng fixture chung của nhóm: ba role, khách vãng lai/khách có hạng/KM, menu giá25.000/30.000, hai ghi chú, bàn trống/có đơn, đơn đã trả/hủy. Mỗi subcase có expected/actual riêng; không truncate dữ liệu lỗi trước gửi.

| ID | Dữ liệu/thao tác | Expected và bằng chứng |
|---|---|---|
| T14-K01 | Tạo ba loại; sai tổ hợp bàn/đối tác/người nhận; tên trắng/101ký tự, SDT16ký tự hoặc chữ, địa chỉ251ký tự | Ba đơn đúng; invalid không có header/dòng/receipt; MaNV từ actor, ship chỉ giao |
| T14-K02 | NULLkhách; FK bàn/khách/actor/nhân viên sai; inactive/stamp cũ/nghỉ việc | NULL hợp lệ; sai bị chặn SQL/BLL, không orphan |
| T14-K03 | Tạo món đầu, thêm hai dòng, đọc trước/sau trigger | Header ban đầu0 trong transaction; tổng bằng SUMsnapshot; không header rỗng lộ ra ngoài |
| T14-K04 | Cùng sản phẩm hai ghi chú, MaDong thuộc đơn khác, xóa/sửa một dòng | Giữ hai MaDong; không sửa nhầm; foreign MaDong bị từ chối |
| T14-K05 | Đổi giá menu sau tạo dòng; lượng0/-1/intMax+1; tổng overflow | Dòng mới lấy giá DB; sửa giữ giá snapshot; invalid rollback và transaction sạch |
| T14-K06 | Giảm gần100%, bớt/xóa khiến tổng thấp hơn giảm cũ; fault khi tính preview | Không lỗi CHECK trung gian; tổng/giảm mới đúng; fault giữ toàn trạng thái cũ |
| T14-K07 | Thêm món ngừng/thiếu công thức; bỏ món đã ngừng; tăng lượng món ngừng | Add/tăng bị chặn; giảm/bỏ hợp lệ để sửa đơn; checkout vẫn chặn nếu còn món lỗi |
| T14-K08 | Sửa đơn đã trả/hủy và stale form ở mỗi operation | DB/BLL chặn; tiền/kho/điểm/dòng giữ nguyên |
| T14-K09 | Hai session cùng bàn, cả hai thứ tự chạy | Một đơn mở; phiên còn lại lấy mã thật, không thêm món sang đơn đó |
| T14-K10 | Hai session cùng revision sửa dòng/header rồi tải lại | Một commit, stale báo conflict; không ghi đè im lặng; receipt/revision đúng |
| T14-K11 | THROW sau header/sau chi tiết/trước receipt; timeout trước/sau commit; request cùng/khác hash và actor | Rollback đầy đủ khi lỗi, @@TRANCOUNT0/XACT_STATE0; commit có receipt; unknown không retry mù; giữ MaHD |
| T14-K12 | Xóa hết dòng, đọc/checkout/hủy; tích hợp T15/T16 | Đơn mở0; checkout từ chối, hủy hợp lệ; chỉ kết luận tích hợp khi task sau có đầu ra |

Bổ sung bắt buộc: chọn bàn trống/rời không gọi create (UX-K05 tại T16); đổi khách/KM/ship làm preview đúng; đọc đơn nhất quán khi ghi đồng thời; role/stamp/revocation; SQL DML trực tiếp; migration rerun/checksum/preflight; khóa loại sau tạo (UX7.3); chặn sửa thông tin giao sau bắt đầu giao. T14 không tạo UI trước T16.
## 4. Checklist gốc

**Kiểm thử bắt buộc:**

- [ ] `T14-K01` [I] Tạo đúng 3 loại đơn; tại quán bắt buộc bàn, mang đi không bàn/đối tác, giao có đủ người nhận.
- [ ] `T14-K02` [I] Khách vãng lai NULL hợp lệ; mã bàn/khách/nhân viên sai hoặc nhân viên nghỉ bị chặn.
- [ ] `T14-K03` [I] Header khởi tạo tổng/giảm 0, thêm món trigger cập nhật đúng.
- [ ] `T14-K04` [I] Hai dòng cùng món khác ghi chú giữ hai MaDong; thao tác theo MaDong không sửa nhầm dòng/đơn.
- [ ] `T14-K05` [I] Tạo dòng lấy giá DB; sửa số lượng giữ giá snapshot; số lượng 0/âm/quá int bị chặn.
- [ ] `T14-K06` [I] Bớt/xóa món đưa giảm cũ về hợp lệ và tính lại trong một transaction, không vướng CHECK trung gian.
- [ ] `T14-K07` [I] Sản phẩm ngừng bán/không có công thức không được thêm theo hợp đồng.
- [ ] `T14-K08` [I] Sửa đơn đã trả/hủy bị chặn; trạng thái form cũ không bypass kiểm tra DB.
- [ ] `T14-K09` [I] Hai session tạo cùng bàn: một đơn mở duy nhất, session còn lại tải đơn có thật.
- [ ] `T14-K10` [I] Hai session sửa cùng đơn: version cũ bị báo xung đột hoặc được tuần tự hóa theo chính sách, không ghi đè im lặng.
- [ ] `T14-K11` [I] Lỗi giữa header/chi tiết/ưu đãi rollback mọi thay đổi; mất kết nối xác định lại theo mã cấp.
- [ ] `T14-K12` [I] Xóa hết dòng thành đơn rỗng mở, có thể hủy nhưng không checkout.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.






