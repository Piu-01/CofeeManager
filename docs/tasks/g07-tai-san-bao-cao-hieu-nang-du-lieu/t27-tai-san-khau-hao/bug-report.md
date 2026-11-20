# G07-B01 — Deadlock khi xóa tài sản đồng thời ghi chi

31/10/2026 · **Người dùng đã duyệt đợt sửa G07-B01 kèm regression bằng “duyệt” ngày31/10/2026.** **Đã sửa bằng migration027 và regression PASS**; xem [biên bản](bug-regression-results.md). G07 còn9ID nativeBLOCKED. Sở hữu T27-K08; ảnh hưởng đường ghi chi G05 và cần regression T28/T29 tương ứng.

## Hiện tượng và tái hiện

Lượt `acceptance_20261031_190439_7666ffd6`, schema026, SQL Server localhost thật, DB riêng có marker, runtime CafeManagerApp/ADMIN. Harness tạo tài sản chưa có chi; hai connection độc lập gửi AssetDelete và FinanceCreate chi7₫ gắn cùng MaTS. Connection owner chỉ giữ XLOCK hàng tài sản để hai bên cùng chờ; quan sát ít nhất2 lock-waiter rồi rollback barrier trước khi chờ kết quả. Barrier không còn trong chu trình deadlock.

Expected: xóa thắng thì chi bị52602; chi thắng thì xóa bị53402. Không mất phiếu, không orphan/receipt dở. Actual: **10/10 lượt gặp1205**:4 lượt `(Delete=1205, Finance=0)` và6 lượt `(Delete=0, Finance=1205)`. Đây là lỗi cạnh tranh, không được đổi expected sang chấp nhận1205 hoặc tự retry giao dịch tiền.

Bằng chứng: [extended-results.json](../runs/acceptance_20261031_190439_7666ffd6/extended-results.json), [manifest](../runs/acceptance_20261031_190439_7666ffd6/manifest.json); [deadlock-1.xml](deadlock-1.xml) đến deadlock-10.xml lấy từ system_health bằng truy vấn lọc tại server: mọi process trong graph thuộc đúng DB test trên. Không xuất toàn bộ ring buffer. Manifest source không đổi, tạo2/dọn2/còn0. Unit/fixture độc lập tiếp tục chạy sau FAIL.

## Nguyên nhân đã có bằng chứng

Graph1 có đúng hai process sản phẩm:

| Process | Đang giữ | Đang chờ |
|---|---|---|
| sp_AssetWrite tại DELETE TAISAN_CODINH | RangeS-S ở PK_PHIEUTHUCHI sau kiểm tra tham chiếu | Chuyển sang X ở PK_TAISAN_CODINH |
| sp_FinanceCreate tại INSERT PHIEUTHUCHI | RangeS-S ở PK_TAISAN_CODINH sau kiểm tra tồn tại tài sản | RangeI-N ở PK_PHIEUTHUCHI |

`025_assets.sql` lấy UPDLOCK/HOLDLOCK tài sản, sau đó HOLDLOCK kiểm phiếu và mới DELETE. `017_finance_sources.sql` giữ HOLDLOCK tài sản rồi INSERT phiếu. U và S có thể cùng tồn tại trước lúc delete nâng X; giữ khóa range ở bảng phiếu tạo chu trình được graph xác nhận. Chỉ serialize theo request GUID không bảo vệ hai yêu cầu khác nhau cùng MaTS.

Rollback victim là trách nhiệm SQL; graph/1205 không chứng minh dữ liệu mất. Harness hiện ghi lỗi trước xuất snapshot chi tiết nhánh FAIL, vì vậy **không tuyên bố đã đối chiếu đầy đủ receipt/transaction của cả10 victim**; đây là phần bắt buộc bổ sung trong regression.

## Đợt sửa đề nghị — một bug G07-B01

1. Migration version027 sửa hai procedure: dùng chung khóa transaction theo MaTS trước khi lấy khóa hàng/range tài sản và phiếu, thống nhất thứ tự ở AssetWrite và FinanceCreate khi có MaTS. Với Create, xác định mã trước khi lấy khóa. Giữ demand ADMIN, rowversion, request/payload/actor replay và transaction hiện có; không đổi business contract hoặc thêm retry tự động.
2. Giữ migration025/026 đã kiểm và checksum lịch sử;027 là sửa khóa, **không phải index tối ưu**. Không nâng development trong đợt này.
3. Regression DB test riêng: barrier delete/chi, update/delete, update/update; mỗi nhánh kiểm row/phiếu/FK/receipt và @@TRANCOUNT/XACT_STATE sau lỗi, replay, sai actor/payload, quyền CASH/STORE. Test assets/cashflow/source và đường ghi chi không MaTS; đo chi phí ghi và chạy lại tranh tồn theo ảnh hưởng. Expected vẫn từ chối nghiệp vụ đúng, không chấp nhận deadlock như PASS.

Rủi ro thực tế: thao tác cùng tài sản bị tuần tự hóa; cần kiểm timeout khóa và đối chiếu receipt trước gửi lại. Thao tác khác MaTS cần tiếp tục song song. Không thay isolation/NOLOCK, không thêm index hay refactor ngoài nguyên nhân.

Đợt test native còn BLOCKED theo transcript nhóm; phê duyệt bug này không biến các ca đó thành PASS. Sau duyệt bug, debug và regression đúng phạm vi trên được phép thực hiện một đợt, không xin duyệt từng fix.

## Kết quả sau phê duyệt

Người dùng duyệt đợt sửa G07-B01 kèm regression ngày31/10/2026. Migration027 chỉ thêm helper khóa theoMaTS và3lời gọi trong2procedure,025/026 không đổi. Lượt regression_20261031_200422_dd2b716e:139/139ca cũ +34/34ca chuyên biệt +15/15unit PASS, không1205 trong các race đã chạy; transaction0:0/receipt/row/phiếu đối chiếu đúng. Lượt trước sai phép đo giữ nguyên; [đối chứng](transaction-measurement.md), [biên bản đầy đủ](bug-regression-results.md). G07-B01 đóng trong phạm vi này; không sửa hoặc tự PASS các ca native ngoài bug.
