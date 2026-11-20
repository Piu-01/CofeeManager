# T28 — Báo cáo SQL và đối chiếu số liệu

**Bổ sung 16/11/2026:** thêm nút In/PDF (A4 ngang, tối đa 500 dòng) và Xuất CSV
(UTF-8 BOM, `;`, tối đa 1.000 dòng) trên màn báo cáo, dùng FastReport OpenSource +
PDFSimple; xem `docs/tasks/print-output-fastreport/plan.md`. Không đổi procedure,
quyền hay công thức tổng; totals lấy từ metadata SQL, không cộng lại ở client.

31/10/2026. **Đã duyệt cùng bộ plan G07 ngày31/10/2026; Đang làm**, phụ thuộc T27/schema025. Checklist13ID nguyên bản ở requirements.md mục6; tài liệu này chốt nguồn, file, fixture và subcase trước code.

## 1. Contract báo cáo

Migration026_reports.sql tạo public read procedures: sp_ReportSales (group Day/Channel), sp_ReportProducts, sp_ReportPayments, sp_ReportInventory, sp_ReportDebts, sp_ReportCashFlow, sp_ReportLabor, sp_ReportAssets, sp_ReportDashboard và sp_ReportOptions. Không dynamic SQL từ tên cột/filter client. Mỗi endpoint Actor/Stamp demand ADMIN; ReportService demand FinancialReports. STORE dùng module kho/nợ đã có, không cấp thêm quyền financial report. Options chỉ trả mã/tên tối thiểu theo quyền. Phần đọc nội bộ sp_ReportRead và endpoint sp_ReportSourceList dùng WITH EXECUTE AS OWNER kèm demand ADMIN để giữ quyền đọc view/temp nhất quán khi query tái compile trên tải lớn; không grant SELECT view nội bộ cho runtime. Quyết định kỹ thuật được kiểm trong pha triển khai, không đổi role hoặc contract tài chính.

Request ReportFilter typed có Kind/From/To, Channel/Method/Supplier/Employee/Material/Asset tùy loại, Skip int≥0, PageSize cố định50. Filter không hỗ trợ loại đó bị BLL/SQL từ chối, không bỏ qua ngầm. Ngày From≤To; end exclusive có xử lý9999-12-31. Debt dùng AsOf=To và công khai không có From; asset dùng tháng nằm trong kỳ chọn; labor phân biệt kỳ công và thời điểm chốt/chi trong metadata. Không đồng thời lọc Method cho doanh thu hóa đơn rồi coi toàn tiền bill là tiền của method đó.

Response typed ReportPage<T> có metadata Kind/filter/date basis/ReadAt/completeness/cảnh báo nguồn, TotalCount, totals và Rows. DAL đọc nhiều resultset: metadata/totals kể cả rỗng, rows trang hiện tại; không dùng DataTable/string tiền để truyền giữa tầng. Từng procedure materialize nguồn đủ dùng một lần rồi trả tổng/count/page từ cùng tập; không SELECT * hoặc load toàn bộ bill lên UI. Total SQL decimal(38,0) được kiểm miền khi map, không overflow CLR/sum double. Không cache trạng thái tài chính làm nguồn chính thức.

## 2. Nguồn, công thức và giới hạn

| Báo cáo | Nguồn và thời điểm | Tổng hợp và drill-down |
|---|---|---|
| Sales ngày/kênh | HOADON đã trả, NgayThanhToan trong kỳ, filter LoaiDon | SUM TongTien, GiamGia, TongTien−GiamGia, PhiShip, ThanhTien, PhiDoiTac riêng; số bill COUNT theo header. Mở danh sách/chi tiết hóa đơn đúng ngày thanh toán/kênh |
| Products | CHITIET_HD JOIN tập hóa đơn đã lọc; MaDong giữ riêng | SUM quantity và quantity×GiaBan snapshot theo MaSP; nhãn trước giảm toàn bill; không phân bổ ship/giảm giả. Mở danh sách bill chứa món theo server filter, rồi detail |
| Payments | CHITIET_THANHTOAN JOIN tập bill đã trả theo NgayThanhToan; Method chỉ cho báo cáo này | SUM SoTien theo MaPT, mỗi MaTT một lần, không join chi tiết món. Nếu lọc method, chỉ tổng tiền method đó; mở bill liên quan, không sum fullbill cho method |
| Debt tại ngày | CONGNO_NCC→PHIEUNHAP.NgayNhap≤cutoff, trả từ THANHTOAN_CONGNO→PHIEUTHUCHI.NgayLap≤cutoff | Aggregate trả theo MaCN trước join; Original−Paid, filter NCC; không dùng số dư hiện tại để suy quá khứ; mở nợ/phiếu nhập/chi theo mã |
| CashFlow quản trị | Thu bán từ thanh toán của bill đã trả theo NgayThanhToan; thu/chi khác PHIEUTHUCHI.NgayLap + DANHMUC_THUCHI.Loai | UNION nguồn có discriminator rồi aggregate; không cộng THANHTOAN_CONGNO/BANGLUONG/PHIEUNHAP lần hai. Tách OriginKind/LegacyUnknown, kênh và phương thức chỉ ở nguồn có dữ liệu. Không coi số dư là ngân hàng/đối soát |
| Labor | CHAMCONG.NgayLam/GioVao/GioRa; BANGLUONG snapshot/NgayChot/kỳ; phiếu chi MaBL theo NgayLap | Giờ có ra hợp lệ, chưa hoàn tất đếm riêng; giờ thường/tăng ca theo snapshot New, LegacyUnknown không suy từ hồ sơ hiện tại. Lương chốt và thực chi riêng, không cộng thành một chi phí; mở công/kỳ/lương/chi |
| Assets | TAISAN_CODINH + fn_AssetDepreciation của025; chi MaTS aggregate riêng | Nguyên giá/khấu hao kỳ/lũy kế/giá trị còn tham khảo, tổng chi MaTS trong kỳ riêng. Không join chi nhiều dòng trước sum nguyên giá; mở tài sản/phiếu chi |
| Inventory | Mốc MaterialOpening + các nguồn đã ghi kho sau mốc, xem quy tắc dưới | Opening + Purchases − Issues − Consumption + Adjustment = Closing; Current riêng nếu To quá khứ. Mở nguồn theo loại/mã, dòng nguyên liệu, không đọc công thức hiện tại |

Labor trả ba chỉ tiêu độc lập theo nhân viên: giờ làm lọc NgayLam trong kỳ; lương **chốt trong kỳ** lọc NgayChot; lương **thực chi trong kỳ** lọc PHIEUTHUCHI.NgayLap. Dòng chi tiết lương giữ Thang/Nam snapshot để người dùng phân biệt kỳ lương với ngày chốt/chi; không lấy một khoảng NgayLam rồi suy đó là tiền lương đã chốt hoặc trả. Kỳ chốt/chi có thể khác kỳ công, nhãn và metadata phải nêu rõ.

Tổng quan ADMIN: sp_ReportDashboard dùng cùng biểu thức aggregate Sales hôm nay (giờ SQL Server local và ngày trả trong metadata), count đơn mở từ header, SUM nợ hiện tại từ nguồn nợ/đã trả; các số không join với nhau thành tích Descartes. Có ReadAt, refresh và lối vào report/POS/nợ theo quyền. Thất bại không điền0; query thành công rỗng mới hiển thị0. Không widget lợi nhuận.

### Đối chiếu tồn từ mốc có nguồn đầy đủ

MaterialOpening.Quantity/RecordedAt là số dư cutover đã bao gồm giao dịch legacy trước mốc; không cộng lại toàn bộ seed PN/PX/tiêu hao/KK. Báo cáo tồn theo **thời điểm ghi kho**, phân biệt ngày nguồn trên chứng từ. Nguồn event sau mốc: G03OperationReceipt.CompletedAt với Operation PurchaseCreate/StockIssueCreate/SalesCheckout/StocktakeCommit; dùng NgayChot cho PN/PX New và NgayThanhToan cho checkout khi không có receipt tương ứng, đồng thời ghi chất lượng timestamp là fallback. Ưu tiên timestamp receipt nếu tồn tại, chỉ một event cho mỗi operation/source; không cộng replay thành event mới. Kiểm kê lấy TonThucTe−TonHeThong và thời điểm StocktakeCommit, không dùng SnapshotAt làm ngày điều chỉnh.

Migration012 hiện chỉ hủy đơn mở, không sinh xuất kho. Đồ đã pha hủy được ghi phiếu xuất theo luồng kho T21; tính qua CHITIET_PX và không trừ thêm tiêu hao của đơn hủy. Không suy liên kết hóa đơn–phiếu xuất khi schema chưa lưu liên kết đó; không dùng công thức hiện tại hoặc trừ kho khi chỉ SalesCancel đơn mở.

Với mỗi NVL, Opening tại From = mốc + event từ mốc tới trước From; period events trong [From,Until), Closing theo Until; Current là SoLuongTon tại ReadAt và chỉ đối chiếu bằng Closing nếu Until bao phủ hiện tại. NVL mở sổ trong kỳ trả nhãn/mốc mở sổ riêng, không giả đã có số dư đầu kỳ. Dòng chưa mở sổ/thiếu thời điểm hoặc giao dịch legacy phát sinh sau cutover không xác định được ghi Coverage=Incomplete, liệt kê nguồn thiếu, không báo chênh lệch0/PASS giả. Không dựng lịch sử trước RecordedAt; user chọn kỳ trước mốc thấy giới hạn, các nguồn đã biết vẫn đọc được. Khi nghiệm thu K09 dùng DB fixture có đầy đủ nguồn sau mở sổ, kiểm đẳng thức trên dataset đứng yên. Không thêm ledger hay sửa timestamp nghiệp vụ cũ để hợp thức hóa số liệu.

## 3. Bước triển khai và file

| Bước | File/khu vực | Đầu ra và kiểm kỹ thuật |
|---|---|---|
| 1 | database/migrations/026_reports.sql | Preflight025, functions/views nội bộ cần dùng và public procs ở mục1; projection từng nguồn, aggregate độc lập, metadata/total/page; grant endpoint ADMIN, không DML/NOLOCK. Clean/demo/upgrade025/rerun/checksum không đổi số dư |
| 2 | src/CafeManager.Models/Requests/Reports/ReportFilter.cs; Dtos/Reports/ReportRows.cs, ReportPage.cs | DTO riêng từng loại, metadata/cutoff/coverage/source ref; không generic dictionary chứa tiền dạng string; SourceRef allowlist loại + mã |
| 3 | src/CafeManager.DataAccess/Repositories/ReportRepository.cs; SqlErrorMapper.cs | Query typed nhiều resultset qua executor hiện có, map535xx và precision/tổng rộng; token/dispose/timeout, không catch trả0 |
| 4 | src/CafeManager.Business/Services/ReportService.cs | Validate filter/biên/ngày/phạm vi, FinancialReports mỗi endpoint; chỉ request hợp lệ mới gọi DAL; session revoked không trả kết quả vào UI |
| 5 | src/CafeManager.WinForms/Modules/Reports/ReportsControl.cs, ReportSourceDialog.cs; Modules/Home/AdminDashboardControl.cs | Report workspace/tổng/grid/pager/filter/refresh; dashboard thật; source dialog danh sách chứng từ page50 trước mở chi tiết, không tự tải tất cả bill |
| 6 | Program.cs, Forms/MainForm.cs, Navigation/NavigationController.cs, UI/DocumentLinks.cs | NavigationTarget.Reports + menu ADMIN; Home ADMIN mới; nối đúng invoice/purchase/issue/stocktake/debt/payroll/finance/assets service theo source type; không SQL trên UI |
| 7 | database/tests/g07/t28-reports.sql; tests/CafeManager.Tests/Reports/ReportRulesTests.cs; tools/CafeManager.G07Probe/ | Fixture tính tay và read-only probe, unit validate filter/role; build/smoke kỹ thuật SQL→DAL→BLL→UI, không tick13ID |
| 8 | technical-checks.md, ../development-guide.md | Nguồn/ngày/kỳ/coverage và đường dùng thử report→chứng từ→quay lại/dashboard; ghi schema/hash và phần chưa chạy |

Nếu cần filter server bill theo món/phương thức để drill-down, bổ sung endpoint sp_ReportSourceList trong026; không đổi semantics sp_InvoiceList cũ hoặc lọc một trang UI rồi gọi là toàn bộ. Mọi source list kiểm quyền lại; source type không hợp lệ từ client bị từ chối. Source mất/không được xem có thông báo, không mở editor tài chính từ dữ liệu tổng hợp.

## 4. Trạng thái UI

Report request có generation riêng. Đổi kind/filter về trang1 và đánh dấu Cần xem lại; tổng/grid cũ bị đánh dấu chưa cập nhật, không mang nhãn kỳ mới và không cho drill-down. Bấm Xem báo cáo chụp filter immutable, hủy request trước; chỉ generation và session còn hợp lệ được publish metadata+totals+rows cùng lúc. Pager dùng applied filter, đổi filter giữa chừng hủy page query. Retry dùng request/filter đã chọn rõ, không tự bắt lỗi thành rỗng.

Tái sử dụng OrbitDateRangeField/filterbox/grid/pagination/notice, không copy theme. Filter phù hợp từng kind, FlowLayout wrap; summary gọn trên grid, không lấy hết chiều cao. Drill-down modal native owner qua host, giữ filter/trang/selection khi quay lại. Query source có cancellation và guard disposed; callback không chạm control đóng. Chọn hết input mới vào, Enter trong filter không tự ghi nghiệp vụ; popup nguồn chỉ đọc. Kích thước/responsive/DPI theo plans.md; không thêm xuất Excel/PDF/chart library vì chưa thuộc đầu ra bắt buộc.

## 5. Dataset và ma trận nghiệm thu

DB fixture G07 gồm ngày lập khác ngày trả, bill mở/hủy/paid; bill2dòng×2method, giá snapshot khác menu hiện tại; ship/giảm/phí; PN trả ngay+2lần trả nợ trước/sau cutoff; thu tay/chi tay/hệ thống/legacy; NV full/part-time, ca thiếu giờ ra/qua đêm, BL chốt chưa chi/đã chi/0/LegacyUnknown;2tài sản có nhiều chi; mở sổ và chuỗi nhập/xuất/checkout/KK có receipt, một fixture thiếu nguồn để kiểm coverage. Lưu bảng kỳ vọng tính tay, không dùng query cần kiểm làm oracle.

| ID | Thao tác/subcase bắt buộc | Expected/bằng chứng |
|---|---|---|
| T28-K01 | Bill lập ngàyA/trảB; mở/hủy cùng kỳ, paid0đ do giảm100% nếu contract cho phép | Doanh thu chỉ kỳB/paid, count đúng; đối chiếu header/thanh toán |
| T28-K02 | Bill tiền món100.000,giảm10.000,ship15.000,phí đối tác snapshot | Món sau giảm90.000,khách105.000; phí riêng theo snapshot, không coi là đã đối soát |
| T28-K03 | 2món×2TT; thêm nhiều bill trùng số tiền | Bill đếm1, tiền đúng1lần; không dùng SUM DISTINCT để che nhân bản |
| T28-K04 | Đổi giá/công thức/KM sau trả | Báo cáo tiền/tiêu hao cũ không đổi, tên hiện tại không được gọi snapshot nếu chưa lưu |
| T28-K05 | Từ00:00/đến23:59:59/ngày kế; cùng ngày, From>To,9999-12-31 | Biên nhận/loại đúng, lỗi filter giữ input; query không CAST cột ngày ở predicate khi tránh được |
| T28-K06 | Rỗng, trang ngoài range, DB timeout/mất kết nối/permission | Thành công mới total0/list rỗng; lỗi rõ/retry, totals không giả0, count vẫn đúng trangrỗng |
| T28-K07 | Nợ tạo trước/sauX, trả trước/đúngcuốingày/sauX | Nợ tạiX đúng tính tay và khác current view khi có trả sauX; source date đúng |
| T28-K08 | Checkout, thu khác, PNtrảngay,nợ2lần,lương đãchi | Union nguồn đúng một lần, Origin/legacy rõ, không cộng thêm bảng liên kết |
| T28-K09 | Mởsổ→nhập→xuất→bán→KK; phiếu lùi ngày, hủy mở/đồpha; chọn kỳ trước cutover/thiếu nguồn | Đẳng thức đúng trên fixture đầy đủ; Current/Closing quákhứ riêng; incomplete không bị coi khớp/PASS giả |
| T28-K10 | Ca qua đêm, chưa ra; BLchốt chưa chi/đãchi0/legacy, khấu hao và chi tài sản | Giờ/kỳ/chốt/thực chi riêng, không doublecount hoặc gán lại hợp đồng cho legacy; snapshot bất biến |
| T28-K11 | Native đổi filterA→B, queryA chậm; đổi trang/refresh, mở nguồn/quaylại | B không bị A ghi đè; tổng/grid cùng metadata, filter/selection giữ; dashboard dùng data thật |
| T28-K12 | CASH/STORE gọi mọi report/options/dashboard/source service và SQL, đổi role/logout lúc đang tải | Từ chối ở BLL/SQL, không chỉ menu; không lộ aggregate sau revoke, ADMIN hoạt động |
| T28-K13 | Đọc nhãn món/cashflow/phí/khấu hao/legacy/coverage và mở nguồn | Nhãn đúng ý nghĩa, không widget lợi nhuận/số dư ngân hàng giả; thao tác native, hình thức SKIPPED |

UX-K37 → K06/K11; UX-K39 → K11 keyboard/owner; K40 → K11 kỹ thuật125% full/window/chiađôi; K41 → K02/K05/K11 tên Việt dài, tiền lớn; K42 → K06/K11/K12 đổi màn/đóng/logout/dispose. Chạy query chậm có barrier hoặc delay trong harness test, không chèn WAITFOR vào procedure sản phẩm. Dashboard gồm thành công/rỗng/lỗi/refresh/quyền được ghi subcase K06/K11/K12.

Kiểm source money/kho/điểm/nợ/lương trước/sau read không đổi; clean/demo/upgrade và grant/deny thật. Regression links T19/T23/T26/T27, shell role, list invoice/payment/stocktake nguồn theo ảnh hưởng. Chỉ chạy nghiệm thu sau duyệt đợt nhóm; lưu test-results.md, bug-report.md và report chung theo plans.md.


