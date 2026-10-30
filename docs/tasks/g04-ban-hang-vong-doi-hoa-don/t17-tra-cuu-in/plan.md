# Plan T17 — Tra cứu, chi tiết và in hóa đơn

Bổ sung25/10 đã duyệt/triển khai: đường in dùng chung cho hỏi in sau chốt và
preview/in lại lịch sử; [kế hoạch/tiêu chí](../t16-pos-huy-don/post-checkout-print-plan.md).
Chờ duyệt đợt test riêng, không thay checklist hoặc kết quả cũ.

**Trạng thái:** Bộ plan G04 đã được duyệt ngày 23/10/2026; đã triển khai trong nhóm, đã chạy đợt test được duyệt; còn FAIL/BLOCKED, xem report.md. **Phụ thuộc:** T16. Áp dụng [plans.md](../plans.md), UX9/K19/K23 và trạng thái chung. Không chạy nghiệm thu khi mới duyệt bộ plan.

## 1. Contract SQL và dữ liệu lịch sử

Migration013 thêm sp_InvoiceList/sp_InvoiceDetail, Actor/Stamp ADMIN/CASH. Không mở báo cáo tài chính cho CASH/STORE. Danh sách nhận DateBasis Created/Paid rõ ràng, FromDate/ToDate inclusive ở UI, Status?/Channel?/Customer?/CodeSearch?, Skip; SQL lọc >=đầu ngày và <ngày kế tiếp sau ToDate, kiểm khoảng/ngày và giới hạn DateTime.MaxValue an toàn. Khi Paid không lấy đơn chưa có NgayThanhToan; Created gồm mở/trả/hủy theo filter. Mặc định ngày thanh toán hôm nay, người dùng đổi được, nhãn luôn rõ. Phân trang50, sort ngày được chọn DESC rồi MaHD, có TotalCount/ReadAt; không NOLOCK hoặc JOIN món×payment rồi SUM. Filter mới về trang đầu, query cũ bị loại bằng generation.

Detail trả header, dòng theo MaDong (tên sản phẩm hiện có được ghi là nhãn hiển thị, giá/SL/ghi chú/tiền dùng snapshot), payment theo MaTT, snapshot ưu đãi/phí và điểm đã cộng thuộc MaHD. Không giả tên món là snapshot lịch sử vì schema chưa lưu tên; không thêm backfill tên giả. Khách/đối tác/bàn tên hiện có dùng để định danh, số tiền lấy HOADON/dòng/payment đã lưu. Mã và phần dữ liệu đã lưu luôn xem được nếu nhãn danh mục đổi. Đơn chốt không gọi fn_DiscountPreview hiện tại. SnapshotLegacyUnknown hiện thông báo nguồn/tỷ lệ lịch sử chưa lưu, vẫn hiển thị tiền giảm đúng. Không lộ phí đối tác trên bản in cho khách.

Đọc detail bằng transaction ngắn nhất quán header/dòng/phân bổ, không giữ connection trong khi preview/in. NotFound trả trạng thái rõ, không crash hoặc lấy dòng trước thay thế. Màn chỉ đọc; đơn mở có nút Quay lại POS theo MaHD, paid/canceled không có sửa tài chính. Service cũ sửa đơn vẫn bị SQL chặn, không dựa vào nút ẩn.

## 2. In và phân trang

Tạo `Printing/InvoicePrintDocument.cs` và model dữ liệu in bất biến từ InvoiceDetailDto; `InvoicePrintPreviewForm` có PrintPreviewControl và nút In/Đóng, thông báo lỗi máy in dễ hiểu. Header/footer và dòng dùng PrintDocument/GDI; PageSettings theo máy in/khổ giấy được chọn, mặc định A4 portrait để có preview ổn định, không giả định khổ80mm/máy in nhiệt hiện diện. Không in tự động trước preview hoặc xuất hóa đơnVAT.

In có tên quán từ cấu hình/nhãn đang có, mã/ngày/bàn/kênh, dòng tên/mã, SL/giá snapshot/thành tiền trước giảm, ghi chú tiếng Việt xuống dòng, tổng món/giảm/ship/phải trả/phương thức. Nguồn/tỷ lệ chỉ hiện khi snapshot có thật. In lại nhãn rõ; không dựng tiền đưa/thối từ allocation. Nếu in ngay trong phiên checkout có đưa/thối có thể hiện từ DTO phiên và ghi đúng nguồn, nhưng phiên sau chỉ dữ liệu DB. Không in chi phí đối tác hoặc bí mật/người nhận dư thừa.

Đo chiều cao text từng dòng theo width thực, giữ cursor/index qua PrintPage, lặp heading bảng trên trang tiếp theo, không cắt ghi chú. Phần tổng/payment theo sau dòng cuối, sang trang mới khi không đủ. BeginPrint reset toàn state cho preview/in lại nhiều lần không mất hoặc nhân đôi dòng; EndPrint/dispose giải phóng font/brush/document do mình sở hữu. Không dispose Graphics của event. Đơn nhiều dòng/ghi chú dài và payment nhiều dòng đều được kiểm pagination bằng danh sách chỉ số/dữ liệu trang và thao tác native; đánh giá hình thức bản in do người dùng, agent không chụp ảnh/so thị giác.

In/Xem chỉ nhận MaHD/DTO đã chốt, không phụ thuộc callback SalesService.Checkout. In lỗi/hủy chỉ báo Đã thanh toán, chưa in được và cho xem/in lại; DB không đổi. Không có máy in thì vẫn mở chi tiết, preview nếu Windows hỗ trợ; nếu renderer cần driver mà máy không có thì ghi BLOCKED đúng nhánh, không cài driver hoặc gửi job vào máy in thật chưa được chỉ định. Test in dùng printer test sẵn có hoặc hủy hộp in; không thay default printer máy người dùng.

## 3. Bước và file

| Bước | File/khu vực | Kiểm chứng kỹ thuật sau duyệt plan |
|---|---|---|
| 1. SQL truy vấn | database/migrations/013_invoice_queries.sql; docs/database/invoice-contract.md | Apply sau012; smoke ngày/filter/list/detail không làm đổi số dư |
| 2. Models/DAL/BLL | Models/Requests/Sales/InvoiceFilter.cs; Dtos/Sales/InvoiceDtos.cs; DataAccess/Repositories/InvoiceRepository.cs; Business/Services/InvoiceService.cs | Typed ngày/null, multi-result reader/disposal, Demand Sales cho đọc/in |
| 3. UI lịch sử/chi tiết | WinForms/Modules/Sales/InvoicesControl.cs + Designer; InvoiceDetailDialog.cs; UI/OrbitRecordListControl/OrbitPagination dùng lại | Filter/paging/state; detail read-only, Quay lại POS khi mở; clamp900×620logical |
| 4. Printing và nối POS | Printing/InvoicePrintDocument.cs; Forms/InvoicePrintPreviewForm.cs; CheckoutResultControl/SalesControl; Program/MainForm/NavigationController | In/Xem từ kết quảT16, targetInvoices quyềnSales; lỗiin không quay vềcheckout |
| 5. Fixture/test/docs | database/tests/t17/query-contract.sql; tests/CafeManager.Tests/Sales/InvoicePaginationTests.cs; tools/ui/Test-G04Functions.ps1; development-guide.md; technical-checks.md | Chuẩn bị nhiều trang/ngày cuối; unit logic layout có giá trị, build và preview smoke kỹ thuật khi có môi trường |

Không tạo migration cho thay đổi in đơn thuần;013 chỉ query. Không thêm export/email/VAT hoặc số hóa đơn thuế. Mốc demo T17 gồm POS→checkout→tra cứu→preview/in lại với dữ liệu thật; các phân hệ chưa triển khai ghi rõ.

## 4. Ma trận nghiệm thu

| ID | Dữ liệu/thao tác | Expected/bằng chứng |
|---|---|---|
| T17-K01 | Created/Paid,00:00/23:59:59/cuối tháng, status/kênh/khách/mã,51+đơn và trang rỗng | Đúng tập/ngày, count/paging ổn định, không nhân dòng; query muộn không ghi đè filter mới |
| T17-K02 | Paid mới/legacy; đổi giá/tên món/KM/tỷ lệ đối tác sau chốt | Giá/SL/tiền/giảm/ship/phí/payment snapshot giữ nguyên; nguồnlegacy chưa có ghi rõ, tên hiện tại không mạo nhận snapshot |
| T17-K03 | Đơn≥100dòng, ghi chú100ký tự Việt, nhiều payment; preview/in nhiều trang | Mỗi MaDong xuất hiện một lần đầy đủ nội dung, tổng/payment không mất; chức năng phân trang/native, hình thức SKIPPED theo phân công |
| T17-K04 | Bill81.000, đưa100.000, tiền dòng trước giảm | In số phải trả/thu81.000, không gọi100.000 là doanh thu; tổng SQL đúng |
| T17-K05 | Mở lại app và in lại cùng đơn | Có nhãn In lại; không bịa đưa/thối; không lấy giá/KM hiện tại tính tiền |
| T17-K06 | Đã commit rồi printer error, hủy hộp in/preview, máy không có printer | Paid giữ nguyên, báo lỗi đúng nhánh; đối chiếu tiền/kho/điểm/receipt không đổi |
| T17-K07 | Preview/in lại≥3lần, đổi PageSettings hợp lệ | BeginPrint reset; không gọi checkout và không payment/HD mới; đối chiếu DB trước/sau |
| T17-K08 | UI chi tiết paid/canceled, direct service sửa tài chính | Không hành động sửa; SQL/BLL reject, chỉ giao đúng contractT18 được phép |
| T17-K09 | MaHD không tồn tại, đơn mở bị loại trước khi mởdetail bằng fixturetest quản trị | Empty/notfound rõ, không crash hoặc hiện dữ liệu cũ |

Bổ sung rights3role/revoke/SQLactor, keyboard/late query/125% bounds theo nhóm; trạng thái Không có kết quả khác Lỗi DB. RegressionT16-K03/K06/K07 khi nối In/Xem; UX-K19/K23/K39–K42. Không suy PASS hình thức từ unit pagination.

## 5. Checklist gốc

**Kiểm thử bắt buộc:**

- [ ] `T17-K01` [I/M] Lọc theo ngày/trạng thái/kênh/khách trả đúng đơn, không bỏ giờ cuối ngày.
- [ ] `T17-K02` [I] Tiền và số lượng lấy snapshot đã lưu, không menu hoặc tỷ lệ KM hiện tại.
- [ ] `T17-K03` [M] Preview/in đủ đơn nhiều dòng, ghi chú tiếng Việt, phần giảm/ship/tổng/phương thức; phân trang không mất dòng.
- [ ] `T17-K04` [M] Ghi đúng tiền dòng trước giảm và số phải trả, không nhầm tiền khách đưa.
- [ ] `T17-K05` [M] In lại không bịa tiền đưa/thối khi DB chưa lưu; nhãn in lại rõ.
- [ ] `T17-K06` [M/I] Lỗi máy in/hủy print sau commit không thay tiền/kho/điểm.
- [ ] `T17-K07` [M/I] In lại nhiều lần không gọi checkout; không tạo thanh toán/hóa đơn mới.
- [ ] `T17-K08` [M/I] Không có nút sửa tài chính đơn chốt; gọi service sửa vẫn bị chặn.
- [ ] `T17-K09` [M] Xem đơn không tồn tại/xóa chưa chốt thông báo đúng, không crash.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.





