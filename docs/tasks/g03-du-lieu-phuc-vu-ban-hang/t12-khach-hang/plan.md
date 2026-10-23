# Plan T12 — Khách hàng, hạng và điểm mở sổ

**Trạng thái:** Bộ plan G03 đã được duyệt ngày 18/10/2026; đã triển khai, đang làm/chờ nghiệm thu nhóm. **Phụ thuộc:** T11. Áp dụng [plans.md](../plans.md).

## 1. Quyết định và contract

- Migration007 thêm Revision KHACHHANG; `CustomerOpening` lưu MaKH PK/FK, Points int>=0, RecordedAt, Source≤100, Kind Manual/LegacyCutover, ActorAccountId nullable và RequestId nullable unique. Khách cũ ghi snapshot điểm hiện tại bất biến, không INSERT lịch sử cộng điểm để tránh trigger cộng lần2. Không reset điểm hay sửa lịch sử. Không coi snapshot cutover là số dư trước giao dịch cũ.
- Clean không có hạng: tạo DONG ngưỡng0 giảm0 khi bảng hạng rỗng. Nếu đã có hạng mà thiếu ngưỡng0 hoặc mapping MaHang hiện tại không đúng ngưỡng, preflight dừng và báo mã; không tự chỉnh chính sách hoặc hạng legacy. Giữ unique ngưỡng hiện có. T13 mới là use case sửa chính sách/tái phân hạng.
- Sequence riêng `SeqKhachHang` sinh KH001–KH999, bootstrap từ suffix hợp lệ hiện có dưới khóa migration; hết mã báo rõ, không cycle/truncate. `sp_CustomerCreate` cấp mã trong transaction, create point0 và chọn TOP hạng ngưỡng cao nhất≤0. ADMIN/CASH không phải nhập mã khách; retry cùng RequestId trả mã cũ.
- Khách mới ADMIN có thao tác mở sổ riêng trước bất kỳ hóa đơn/lịch sử điểm và chỉ1lần; zero cũng ghi mốc. Không nhập điểm trong editor. Khóa policy Shared→khách→nguồn tham chiếu, ghi mốc/set điểm/hạng một transaction; có lỗi rollback cả3. Nếu đã có giao dịch, cấm mở sổ; T13 điều chỉnh có lý do. Không tự mở sổ cho khách CASH tạo.
- `sp_CustomerList/Get/Create/Update/Delete/Open/Reconcile`; request profile chỉ tên/SDT và revision. Tên≤50 required, SDT ASCII digits≤15, trim rỗng→NULL, giữ0đầu; không áp độ dài10 khi baseline cho15. SQL nhận chuỗi rộng để reject dài, unique filtered giữ nhiều NULL. Tìm SDT chính xác ưu tiên, fallback prefix/tên escaped, paging50; chọn bằng MaKH, tên trùng hợp lệ.
- ADMIN đọc/thêm/sửa/xóa/mở sổ; CASH đọc/tìm/thêm (tên/SDT/điểm/hạng cần bán), không sửa/xóa/mở sổ; STORE không truy cập. Chặn direct UPDATE điểm/hạng, chỉ procedure đặc quyền sở hữu mới thay. Delete bị chặn nếu có HOADON/LICHSU_DIEM/CustomerOpening; không xóa nguồn mốc.
- Không có chức năng tạo khách vãng lai: selector trả MaKH NULL theo lựa chọn rõ. G03 có tìm/chọn để kiểm contract; tích hợp vào POS T16. T17 sở hữu tra cứu hóa đơn đầy đủ; T12 chỉ liệt kê mã/ngày/trạng thái hóa đơn của đúng khách nếu đã có dữ liệu, không expose tài chính ngoài quyền CASH.
- Dùng Shared applock policy cho Create/Open, cùng thứ tự plans.md; policy T13 Exclusive. Không tạo một bộ công thức hạng ở C#.

## 2. Bước/file và kiểm tra kỹ thuật

| Bước | File | Đầu ra/kiểm tra |
|---|---|---|
| 1 | database/migrations/007_customers.sql | Yêu cầu006; rank0 clean, preflight legacy, sequence/mốc/revision/procedures/GRANT; SQL compile và point0→hạng0 smoke |
| 2 | Models/{Dtos,Requests}/Customers/*; DataAccess/Repositories/CustomerRepository.cs | DTO profile/selector/mốc riêng, typed nullable SDT, revision/request |
| 3 | Business/Services/CustomerService.cs; PermissionService.cs; SqlErrorMapper.cs | Ma trận role, normalize SDT, session/reconcile, lỗi519xx |
| 4 | WinForms/Modules/Customers/CustomersControl.*; Forms/CustomerEditorDialog.*, CustomerPickerDialog.*, CustomerOpeningDialog.*; navigation/composition | Grid và selector SDT, editor chỉ tên/SDT; điểm/hạng read-only; mở sổ ADMIN riêng có xem lại |
| 5 | database/tests/t12/*; tests/CafeManager.Tests/Customers/*; runnerG03; docs/database/customer-contract.md, migrations.md, write-ownership.md; README | Fixture ngưỡng/NULL/legacy, smoke/technical-checks và kịch bản F5 ba role |

Dialog picker focus SDT, tìm/thêm/chọn, không dùng Enter chốt điểm; thêm xong reload và chọn đúng mã. History rỗng/lỗi khác nhau. Mẫu layout theo T09, trường SDT không numeric updown, giữ số0đầu.

## 3. Dataset và subcase nghiệm thu sau duyệt test

| ID | Fixture/thao tác | Expected và đối chiếu |
|---|---|---|
| T12-K01 | Clean create point0, ADMIN sửa tên/SDT, reload | Hạng ngưỡng0, điểm/hạng giữ nguyên khi sửa profile |
| T12-K02 | Tạo2NULL, whitespaceSDT, trùng SDT thật, 0đầu | NULL hợp lệ; trùng reject; giữ0đầu |
| T12-K03 | Tên trống/51ký tự/trùng tên, SDT16/chữ, opening âm/vượt int | Sai reject, trùng tên hợp lệ mã khác |
| T12-K04 | Dataset policy ngưỡng0/300/800; mở sổ299/300/301/799/800/801 | SQL chọn đúng ngưỡng, không tính cache |
| T12-K05 | Open1lần; hai session; khách có history/order; legacycutover; inject fault | Một mốc; không reset khách phát sinh; lỗi rollback điểm/hạng/mốc/receipt |
| T12-K06 | GUI tìm số/tên Việt,2khách trùng tên, chọn/vãng lai | Đúng MaKH hoặc NULL, không thêm khách giả; UX-K09 tích hợpT16 riêng |
| T12-K07 | Xóa khách mới/chưa mốc và từng FK history/order/opening | Chưa dùng xóa được; có nguồn không mất lịch sử |
| T12-K08 | 3role direct service+SQL; payload profile chèn điểm/hạng | Đúng ma trận, điểm/hạng không sửa tùy ý |
| T12-K09 | SessionB fixture cộng điểm hợp lệ, SessionA refresh detail/list | Điểm/hạng DB mới hiện đúng, query lỗi không trả0 |

Bổ sung sequence2session/exhaustion/legacy mã khác định dạng không remap, receipt/create timeout, stale editor, policy lock, migration clean/demo/missingzero/wrongrank/rerun, runtimeDML denied, auth/GUI chung. Mốc legacy không chứng minh lịch sử trước cutover; ghi giới hạn khi bàn giao. UX-K39–42 và UX-K09 phần hồ sơ/selector.

## 4. Checklist gốc giữ nguyên

- [ ] `T12-K01` [I/M] Thêm khách điểm 0 nhận hạng ngưỡng 0; chỉnh tên/SDT không đổi điểm/hạng.
- [ ] `T12-K02` [I] SDT rỗng thành NULL và nhiều khách NULL hợp lệ; SDT thực trùng bị chặn.
- [ ] `T12-K03` [I/M] Tên trống/quá dài và điểm mở sổ âm bị chặn; không dùng tên khách làm khóa.
- [ ] `T12-K04` [I] Ngưỡng 0, ngay dưới/bằng/trên ngưỡng chọn đúng hạng.
- [ ] `T12-K05` [I] Điểm mở sổ có nguồn và chỉ khởi tạo một lần; không reset khách có lịch sử.
- [ ] `T12-K06` [M] Tìm SDT nhanh, chọn đúng MaKH; khách vãng lai không cần tạo khách giả.
- [ ] `T12-K07` [I/M] Xóa khách đã có hóa đơn/lịch sử điểm bị chặn, không mất lịch sử bán.
- [ ] `T12-K08` [U/M] CASH thêm/tìm khách được nhưng không sửa số dư/hạng tùy ý.
- [ ] `T12-K09` [M] Danh sách/chi tiết khách reload đúng điểm và hạng từ DB, không tự tính từ cache.

Mỗi subcase lưu mã, môi trường/schema/source hash, dữ liệu, lệnh/thao tác, expected/actual, PASS/FAIL/BLOCKED/NOT RUN và evidence tại test-results.md cạnh plan. Bug ghi bug-report.md tại task; report nhóm liên kết và chờ duyệt đợt bug. Chưa đánh dấu hoàn tất từ plan/build/smoke.



