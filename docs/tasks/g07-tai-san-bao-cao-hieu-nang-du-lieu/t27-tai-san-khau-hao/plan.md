# T27 — Tài sản cố định và khấu hao tham khảo

31/10/2026. **Đã duyệt cùng bộ plan G07 ngày31/10/2026; Đang làm** tại [plans.md](../plans.md). Requirements nhóm đã duyệt; phụ thuộc T26. Không phải biên bản triển khai/kiểm thử.

## 1. Mục tiêu và contract đã chọn

CRUD hồ sơ tài sản, xem chi liên quan và lịch khấu hao tham khảo. Giữ MaTS char(5), TenTS nvarchar(100), NgayMua date, GiaTri decimal(18,0), SoThangKH int; không thêm trạng thái/thanh lý vì schema chưa có contract đó. Thêm Revision rowversion để chống ghi đè; mã TS001–TS999 cấp bằng sequence SQL mới, khởi tạo lớn hơn mã chuẩn đang có dưới khóa migration; mã legacy khác mẫu vẫn đọc được, không tự đổi. Hết miền báo lỗi, không quay vòng hoặc MAX+1 lúc ghi.

Tên trim, không rỗng/whitespace/control characters, tối đa100 ký tự; ngày hợp lệ trong miền date; nguyên giá nguyên VND >0 và ≤999999999999999999; số tháng int >0. Không tự giảm miền SoThangKH vì UI calendar: tính số tháng bằng số thứ tự tháng/bigint, không AddMonths vượt năm9999. Ngày mua tương lai được lưu; trước tháng mua khấu hao0. Không buộc tên tài sản duy nhất.

Khấu hao tham khảo dùng VND nguyên: B = FLOOR(GiaTri / SoThangKH); các tháng1…n−1 bằng B, tháng cuối bằng GiaTri−B×(n−1). Lũy kế sau k tháng = B×min(k,n−1) + phần cuối nếu k≥n; trước mua0, sau đủ n bằng nguyên giá. Cách chọn này tránh tháng cuối âm khi nguyên giá nhỏ hơn số tháng. Ví dụ100/3 →33,33,34; 1/3 →0,0,1. Không làm tròn mỗi ngày hoặc tự tạo phiếu chi.

BLL có AssetDepreciationCalculator thuần cho ước tính/unit test; SQL inline function fn_AssetDepreciation trả cùng công thức để T28 aggregate phía SQL. Đây là ước tính tham khảo, không số chốt tài chính; test parity trên các giá trị biên để chống lệch. Chỉnh nguyên giá/ngày/tháng làm thay ước tính theo hồ sơ hiện tại, không hứa snapshot khấu hao lịch sử hoặc sửa chi cũ.

Public procedures mới: sp_AssetList(@Search,@Skip), sp_AssetGet(@Code), sp_AssetWrite(@Action,@Code,@RequestId,@ExpectedRevision,@Name,@PurchasedOn,@Value,@Months), sp_AssetReceipt(@RequestId), sp_AssetExpenses(@Code,@From,@To,@Skip). Tất cả có Actor/Stamp và ADMIN. List/page50 trả metadata ReadAt/TotalCount riêng và rows; get có Revision, tổng chi liên quan. Expenses chỉ chọn danh mục Loai=Chi và MaTS đúng, tổng cả tập trước paging; không gọi tổng chi là tổng nguyên giá. Phiếu Thu có MaTS cũ không bị xóa/sửa, có thể xem qua nguồn thu chi riêng và không gộp vào chi sửa chữa.

Write transaction: demand → request applock qua sp_FinanceBegin → row tài sản UPDLOCK/HOLDLOCK → validate/expected revision → ghi → sp_FinanceFinish chứa revision → commit; code từ sequence chỉ sau biết không replay. Delete kiểm version và mọi PHIEUTHUCHI.MaTS, FK là bảo vệ cuối, không cascade. Receipt lấy cùng request lock rồi tra actor/operation; trả Recorded(code/revision/action), NotRecorded hoặc còn Unknown khi không lấy được khóa. Replay delete vẫn trả receipt dù row đã mất; update không ghi lại từ revision cũ. Giữ receipt riêng operation AssetCreate/Update/Delete.

## 2. Bước triển khai và file

| Bước | File/khu vực | Thực hiện và kiểm kỹ thuật |
|---|---|---|
| 1 | database/migrations/025_assets.sql | Preflight schema024, tên/giá/tháng cũ; thêm rowversion/sequence/function/procedures, DENY DML tài sản và grant endpoint; không thay tiền/phiếu/lịch sử. Chạy clean/demo/upgrade024 bằng runner, rerun/checksum/rollback preflight trong DB test |
| 2 | Models/Dtos/Assets/AssetRows.cs, Models/Requests/Assets/AssetRequests.cs | AssetRow/Detail/ExpensePage, AssetFilter/WriteRequest, receipt có action/code/revision; kiểu date/decimal/int, không SqlClient/UI |
| 3 | DataAccess/Repositories/AssetRepository.cs; DataAccess/SqlErrorMapper.cs | List/Get/Write/Receipt/Expenses và đọc metadata riêng; SqlDecimal input/output phù hợp; map534xx. Dispose reader/connection, token, không trả transaction lên form |
| 4 | Business/Services/AssetService.cs, AssetDepreciationCalculator.cs | Normalize, demand Finance, liên kết session token, phiên đổi bị chặn; calculator không I/O; validate không lặng làm tròn |
| 5 | WinForms/Modules/Assets/AssetsControl.cs, AssetEditorDialog.cs, AssetDetailDialog.cs, AssetPickerDialog.cs | List chuẩn Orbit; editor create/update, delete xác nhận; detail ước tính đến tháng chọn + chi có paging; picker readonly mã/tên để chọn một lần; loading/error/dirty/version/unknown đúng contract |
| 6 | Program.cs, MainForm.cs, NavigationController.cs, UI/DocumentLinks.cs; Modules/Finance/FinanceEntryDialog.cs, FinanceDetailDialog.cs | NavigationTarget.Assets chỉ ADMIN, nối service; chọn MaTS bằng picker và mở hồ sơ từ phiếu chi, từ tài sản mở phiếu chi. Giữ constructor tương thích caller cũ nếu thêm dependency tùy chọn; không refactor navigation khác |
| 7 | tests/CafeManager.Tests/Assets/AssetRulesTests.cs; database/tests/g07/t27-assets.sql; tools/CafeManager.G07Probe/ | Unit cho normalize/khấu hao/quyền; fixture SQL và probe kỹ thuật typed tối thiểu sau build, không đánh dấu9ID |
| 8 | technical-checks.md; ../development-guide.md | Ghi build/migration/smoke cùng hash/schema và nguồn không thay đổi tiền/kho/điểm/nợ/lương; bổ sung kịch bản F5 tài sản→chi, trạng thái chưa nghiệm thu |

Các đường Models/DataAccess/Business/WinForms trong bảng thuộc src/CafeManager.<tầng tương ứng>/. Dùng common contract/pending/cleanup trong plans.md, không chạy các bước trước khi bộ plan được duyệt.

## 3. UI và phục hồi

Tái sử dụng base list, theme, dialog host, dialog layout và select-all đã nêu ở plans.md. Editor logical620×440, picker760×500, detail900×580; body cuộn và owner WorkingArea clamp, footer truy cập được khi hẹp. Không tự thêm tab trạng thái chưa có dữ liệu.

Lưu pending scoped DB trước ghi; busy khóa gửi/đóng theo guard. Timeout giữ request/input/expected revision, Kiểm tra kết quả lấy receipt có khóa; không đọc row rồi suy kết quả từ tên/giá. Unknown không đổi request hoặc cho xóa tiếp. Recorded tải lại nguồn, chỉ trả result một lần; clear pending lỗi không gọi write lại. Xung đột version giữ input cho xem/tải lại và xác nhận mới, không tự ghi đè. Khi đóng/phiên đổi hủy đọc, bỏ callback cũ; không khôi phục session đã revoke. Thao tác chọn tài sản vào phiếu chi không tự tạo chi; người dùng vẫn phải xác nhận Ghi phiếu theo flow G05.

## 4. Ma trận nghiệm thu sau duyệt đợt test G07

Checklist nguyên bản T27-K01–K09 tại requirements.md mục6 được giữ nguyên. Mỗi nhánh bên dưới ghi subcase riêng, expected/actual và bằng chứng, không tick toàn ID nếu thiếu M/native.

| ID | Fixture/thao tác | Expected và bằng chứng cần thu |
|---|---|---|
| T27-K01 | ADMIN tạo tên Việt100ký tự, sửa/ngày/giá/tháng; reload/tìm mã, trang0/50/51 và trang rỗng | Typed data đúng DB/UI, mã đầy đủ, TotalCount/pager đúng, update revision thay; SQL+native handler |
| T27-K02 | Tên trống/space/101, giá0/âm/lẻ/vượt18/max18, tháng0/âm/intmax; MaTS chi không tồn tại | Biên hợp lệ được lưu, sai bị chặn BLL và SQL; không đổi tài sản/phiếu/receipt sau lỗi; intmax không overflow calculator |
| T27-K03 | Chụp count/sum PHIEUTHUCHI trước/sau create/update tài sản | Không có phiếu chi tự sinh hoặc đổi số tiền; giữ snapshot đối chiếu |
| T27-K04 | Chi sửa chữa500.000 qua FinanceService cho tài sản85.000.000, mở hai chiều | Một chi500.000 đúng MaTS, không cộng85.000.000 vào chi; native picker/owner/source |
| T27-K05 | Ngày mua15/01, trước mua/tháng mua/cuối kỳ/sau kỳ; tương lai và9999-12 | Bắt đầu theo tháng, lũy kế≤nguyên giá/dừng đúng; unit + SQL function parity |
| T27-K06 | 100/3,1/3,85.000.000/60, max18/intmax | Cuối kỳ bù chênh, không âm, tổng đúng nguyên giá; xUnit calculator và SQL parity, không lặp intmax tháng |
| T27-K07 | Đọc lịch/đổi tháng báo cáo, snapshot PHIEUTHUCHI | Nhãn tham khảo, không gọi dòng tiền/giá vốn; không phát sinh chi; SQL+native chức năng, hình thức SKIPPED |
| T27-K08 | Xóa tài sản chưa tham chiếu; tài sản có chi/thu legacy; hai session sửa/xóa/cùngrevision hoặc chi đang tạo | Chưa tham chiếu xóa được; có nguồn bị chặn, không mất phiếu. Version cũ không ghi đè, không orphan; đối chiếu transaction và receipt |
| T27-K09 | ADMIN/CASH/STORE service+SQL; native menu; timeout trước/sau commit; replay/sai actor/payload; pending restart/khácDB/I/O | BLL/SQL chặn role sai; native guard/input/select-allclick/Tab/clicklần2, Esc/Enter, phiên đổi/loading/latecallback; Unknown đối chiếu trước retry, đúng1receipt/row |

UX-K38 ánh xạ K03/K05/K07. UX-K39: K01/K09 keyboard; K40: K01/K04/K09 layout kỹ thuật125% full/cửa sổ/chia đôi; K41: K01/K02 tiếng Việt/tiền lớn; K42: K09 async/đóng/logout.100/150% NOT RUN nếu chưa chạy, hình thức SKIPPED; không dùng screenshot/Designer làm nghiệm thu.

Fixture clean/demo/upgrade có TS001… cũ, asset mới không chi/có chi, tiền lẻ/biên, ADMIN/CASH/STORE, request cố định và fault sau ghi trước receipt để kiểm rollback. Snapshot tài sản/receipt/phiếu và tiền/kho/điểm/nợ/lương ngoài scope trước/sau; hai session có barrier thật. Cleanup exact marker trong finally. Kết quả lưu test-results.md, bug-report.md tại task, report chung của nhóm; chưa có kết quả nào được chạy.

