# G07 — Tài sản, báo cáo và hiệu năng dữ liệu: requirements

Ngày soạn: 31/10/2026. Trạng thái: **Requirements nhóm đã được người dùng duyệt bằng “duyệt” ngày 31/10/2026**. Bộ plan T27–T29 tại [plans.md](plans.md) đã được người dùng duyệt cả bộ ngày31/10/2026; đã triển khai source; đợt test đã được duyệt và chạy,24PASS/0FAIL/9BLOCKED. Xem [report.md](report.md); G07 vẫn Đang làm.

## 1. Mục tiêu và bối cảnh chuyển nhóm

Hoàn thiện hồ sơ tài sản và khấu hao tham khảo, cung cấp báo cáo SQL có thể đối chiếu với chứng từ gốc, rồi đo và tối ưu hiệu năng bằng bằng chứng trên SQL Server thật. Áp dụng cho một quán cafe, một kho chung, WinForms .NET 10 và SQL Server theo kiến trúc hiện có.

Yêu cầu “triển khai g07” ngày 31/10/2026 được ghi nhận là bắt đầu quy trình G07. G06 vẫn Đang làm: 26 PASS/0 FAIL/7 BLOCKED trên 33 ID; G06-B01/B02 đã sửa và regression PASS, native modal còn thiếu bằng chứng. Không coi chuyển nhóm là nghiệm thu G06, không nâng DB development hoặc thay kết quả lịch sử. T26 đã có source/migration024; bộ plan sẽ xác minh đầu ra thực sự cần cho G07, chỉ báo blocker nếu thiếu đầu ra ngăn triển khai.

Nguồn: [kế hoạch mục 8–13 và T27–T29](../../KeHoach_QuanLiQuanCafe.md), [UX mục 2, 14–18](../../UX_QuanLiQuanCafe.md), [báo cáo G06](../g06-nhan-su-cong-luong/report.md), [AGENTS.md](../../../AGENTS.md). Mục 6 sao nguyên bản phạm vi, đầu ra, 33 ca nghiệm thu và điều kiện hoàn tất T27–T29.

## 2. Task con, thứ tự và đầu ra

| Thứ tự | Task | Đầu ra | Phụ thuộc |
|---|---|---|---|
| 1 | T27 — Tài sản cố định và khấu hao tham khảo | Module tài sản, liên kết chi sửa chữa, bảng ước tính khấu hao | T26 |
| 2 | T28 — Báo cáo SQL và đối chiếu số liệu | ReportRepository/Service, query/procedure, filter và DTO tổng hợp; tổng quan ADMIN dùng aggregate thật | T27 |
| 3 | T29 — Đo hiệu năng và tối ưu SQL có bằng chứng | Dataset benchmark, actual plan/IO/TIME, báo cáo trước/sau và thay đổi query/index có căn cứ | T28 |

Một task tại một thời điểm, triển khai T27 → T28 → T29. Sau duyệt requirements sẽ viết đủ ba plan và plans.md, chờ duyệt cả bộ một lần trước code; không tạo requirements task con.

## 3. Phạm vi nghiệp vụ và UX

### T27 — Tài sản

- CRUD TAISAN_CODINH theo schema: tên, nguyên giá, ngày mua, thời gian khấu hao và trạng thái được hỗ trợ. Kiểm dữ liệu rỗng/quá dài, tiền/tháng không hợp lệ; reload và tìm theo mã. Chỉ ADMIN quản lý/xem tài chính, kiểm quyền ở BLL khi gọi trực tiếp.
- Lưu hồ sơ không tự ghi chi tiền mua. Chi sửa chữa gắn MaTS qua luồng thu chi đã có; giữ liên kết và không cộng nguyên giá lần nữa vào chi. Xóa tài sản đã có chi bị chặn, không cascade lịch sử.
- Khấu hao đường thẳng ghi rõ **tham khảo**, bắt đầu tháng mua, trước mua bằng 0, dừng đủ số tháng, không vượt nguyên giá. Tháng cuối bù phần làm tròn để tổng bằng nguyên giá. Không tạo phiếu chi khấu hao hoặc gọi là giá vốn bán.

### T28 — Báo cáo và tổng quan ADMIN

- Nhóm báo cáo: doanh thu; món/phương thức/kênh; tồn/kiểm kê; công nợ tại ngày; thu chi; giờ làm/bảng lương/thực chi; tài sản/khấu hao tham khảo. SQL tổng hợp, filter có kiểu và phân trang; UI không tải toàn bộ chứng từ để tính lại nghiệp vụ.
- Doanh thu chỉ đơn đã trả theo NgayThanhToan, loại đơn mở/hủy; tách tiền món, giảm, ship, khách trả và phí đối tác. Món dùng giá snapshot và ghi nhãn trước giảm toàn bill khi chưa phân bổ. Aggregate món và thanh toán độc lập trước JOIN, không nhân doanh thu nhiều dòng × nhiều phương thức.
- Ngày lọc theo khoảng [từ ngày, ngày sau đến ngày). Nợ tại ngày loại khoản nợ/lần trả sau cutoff, không lọc view số dư hiện tại rồi coi là quá khứ. Thu bán hàng và thu khác tính mỗi nguồn một lần; không cộng thêm lịch sử trả nợ/lương/nhập trả ngay ngoài phiếu chi tương ứng.
- Đối chiếu tồn từ mốc mở sổ và đầy đủ nguồn nhập − xuất − tiêu hao + chênh lệch kiểm kê. Giờ làm, lương đã chốt, lương thực chi và khấu hao tách riêng; không gọi dòng tiền quản trị là số dư ngân hàng hoặc lợi nhuận chính xác khi chưa có giá vốn/đối soát.
- Tích hợp tổng quan ADMIN: doanh thu hôm nay, đơn mở, nợ và lối vào nghiệp vụ dùng aggregate thật theo mục 12.3 của kế hoạch. Rỗng và lỗi có trạng thái riêng, không điền số giả.
- Luồng chọn loại → filter phù hợp ngày/kênh/phương thức/NCC/nhân viên → Xem báo cáo → tổng và grid; mở chứng từ nguồn theo quyền, quay lại giữ filter. Đổi filter đánh dấu cần tải lại, không đặt kỳ mới trên số cũ. Bỏ kết quả query trả muộn; có loading/rỗng/lỗi/Thử lại.
- Chỉ ADMIN xem báo cáo tài chính, BLL chặn CASH/STORE. STORE chỉ tiếp cận thông tin kho/nợ được phép theo quyền hiện hành, không mở rộng quyền tài chính từ menu báo cáo.

### T29 — Hiệu năng có số đo

- Chuẩn bị DB test riêng với khoảng 10.000 hóa đơn hợp lệ, nhiều dòng/phương thức; không dùng seed nhỏ làm benchmark. Đo cùng máy/dataset cho POS/menu/bàn, checkout 20 dòng, lịch sử hóa đơn và báo cáo trước/sau.
- Thu actual plan, STATISTICS IO/TIME, roundtrip, paging và chi phí ghi. Đánh giá candidate index ngày thanh toán, mã hóa đơn/phiếu nhập/công nợ/lịch sử/FK; chỉ triển khai index/query có bằng chứng hưởng lợi, không tạo hàng loạt theo cảm tính.
- Giữ kết quả tiền/kho/nợ và bất biến nghiệp vụ; đối chiếu trước/sau và tác động ghi. Kiểm TVP, trigger set-based, hai session tranh tồn và deadlock khi tái hiện. Không dùng NOLOCK cho tiền/kho, không cache trạng thái chốt làm nguồn tin cậy.
- Mục tiêu ban đầu thao tác thường dưới 1 giây, report dưới 3 giây trên cấu hình/dataset ghi nhận; báo số đo thực tế, không cam kết mọi máy. UI async và responsive khi query chậm.
- Đo phục vụ lựa chọn tối ưu thuộc triển khai T29 sau duyệt bộ plan; lưu riêng bằng chứng kỹ thuật. Kiểm thử nghiệm thu T29-Kxx vẫn thuộc đợt test nhóm được duyệt riêng, không tự ghi PASS từ phép đo triển khai.

### UI dùng chung và truy vết UX

- Đọc docs/ui/README.md và đầy đủ bộ Orbit đúng thứ tự trước thiết kế/code UI. Tái sử dụng AppTheme/control Orbit, dialog manager và OrbitField.SelectAllOnEntry; không tạo cơ chế UI riêng.
- MainForm + UserControl; editor/picker/chi tiết/xác nhận là Form modal Windows có owner đúng chuỗi, titlebar/nút X native, ShowInTaskbar=false. Giữ input/filter/selection/focus khi trở về; dispose và guard nháp/busy/unknown/session.
- Ô văn bản/số sửa được chọn hết khi mới vào bằng click/Tab, gồm editor grid; click tiếp trong cùng focus cho đặt caret bình thường. Tiền căn phải dạng 81.000 ₫; ngày dd/MM/yyyy, giờ HH:mm; gửi typed values xuống SQL.
- Tách loading/rỗng/lỗi/xung đột/chưa rõ kết quả; lỗi giữ input, không thành tổng 0 giả. Enter không tự ghi giao dịch tiền/kho; chặn gửi lặp và callback sau đóng/đổi phiên.
- Truy vết UX-K38 → T27; UX-K37 → T28; UX-K39–K42 áp dụng cho UI của nhóm và hồi quy theo ảnh hưởng. Bộ plan ánh xạ từng UX ID/subcase tới Txx-Kxx và bằng chứng cần thu.
- Hỗ trợ 1280×720, 1366×768, DPI100/125/150%; agent chỉ kiểm chức năng/layout kỹ thuật tại125%, không tự đổi Windows Scale. Người dùng tự kiểm hình thức: ghi SKIPPED theo phân công; DPI100/150% chưa chạy ghi NOT RUN.

## 4. Giả định và quyết định cần chốt trong bộ plan

- Giữ kiến trúc Models/DAL/BLL/WinForms, SqlClient async, parameter/TVP, tiền decimal và lượng theo contract. SQL sở hữu transaction nhiều bảng; không EF/Dapper/API/generic repository hoặc transaction C# dư.
- Source có migrations001–024, chưa đồng nghĩa DB development đã ở024. Đọc baseline, seed và migration liên quan trước chốt SQL; xác minh nguồn MaTS, ngày nguồn, snapshot bán/lương, mở sổ và lịch sử kho/nợ. Procedure thiết kế đích không mặc định đã tồn tại.
- Plan T27 chốt validation, precision/làm tròn khấu hao, quyền, xung đột ghi, FK/khóa xóa và đường liên kết thu chi; bảo toàn dữ liệu cũ, không sửa seed để né lỗi.
- Plan T28 chốt từng nguồn/ngày/filter, metadata kỳ dữ liệu, paging/drill-down, DTO/query/procedure, cutoff nợ, đối chiếu tồn và tổng quan ADMIN. Nếu thiếu nguồn lịch sử thì công khai giới hạn và trình phần đổi contract trước code, không dựng số liệu giả.
- Plan T29 chốt dataset/fixture, cách đo lặp/so sánh, truy vấn được khảo sát và tiêu chí chọn/loại tối ưu. Candidate index chỉ là đối tượng đánh giá; quyết định cuối dựa trên số đo trong phạm vi đã duyệt. Bug mới ngoài phạm vi tối ưu phải lập hồ sơ/duyệt đợt bug, không tự vá.
- Chốt danh sách file, migration kế tiếp không trùng version, clean/demo/upgrade/rerun/checksum và quyền runtime. Không sửa tài chính đã chốt, computed columns, lịch sử điểm/tiêu hao/trả nợ hoặc snapshot lương.
- Không mở rộng sang kế toán tài sản/giá vốn đầy đủ, lợi nhuận chính xác, hoàn tiền, đối soát thanh toán, nhiều kho/chi nhánh, API hoặc vận hành/publish G08. Không gộp sửa các ca còn thiếu của nhóm trước vào G07.
- Tài nguyên test có manifest/marker/allowlist và cleanup thuộc lượt chạy; không dùng DB thật làm fixture, không tự nâng development, commit hoặc push. Chuẩn bị hướng dẫn dùng thử theo đường chạy development hiện có; thao tác nâng DB cần phạm vi được duyệt phù hợp.

## 5. Kiểm chứng và tiêu chí hoàn tất

1. Requirements nhóm được duyệt → viết đủ plan T27–T29 và plans.md → duyệt cả bộ → triển khai tuần tự. Hoàn thành triển khai chờ duyệt đợt test nhóm riêng; duyệt plan không đồng nghĩa duyệt nghiệm thu/debug.
2. Task có SQL triển khai SQL → DAL → BLL → UI; build/compile migration/smoke kỹ thuật tối thiểu ghi riêng technical-checks, không thay PASS ca nghiệm thu.
3. Test SQL trên SQL Server thật, DB test riêng; hai session cho ca đồng thời, đối chiếu bảng nguồn/tiền/kho/nợ/lương/liên kết và transaction sau lỗi. xUnit cho quy tắc có giá trị; WinForms thật cho binding/validation/keyboard/quyền/session/loading/late query.
4. Giữ source/migration cố định trong lượt test; test-results.md từng task ghi mã ca/subcase, môi trường/schema/hash, dữ liệu, lệnh/thao tác, expected/actual, trạng thái và bằng chứng. Không tick ca chưa chạy hoặc suy từ source/build.
5. report.md tổng hợp đủ **33 ID gốc = 9 + 13 + 11**, giữ325ID/T01–T32 và lịch sử nhóm trước. Bug ghi tại task sở hữu, duyệt cả đợt trước debug/regression. Hồi quy theo ảnh hưởng thu chi/MaTS, báo cáo nguồn, shell/tổng quan, paging, query/index và transaction tiền/kho.
6. Chỉ Hoàn tất khi đủ đầu ra và ca bắt buộc PASS theo phân công; thiếu bằng chứng giữ Đang làm và ghi rõ BLOCKED/NOT RUN. Không ghi PASS hình thức thay người dùng. Báo cáo tài nguyên trước/tạo/dọn/còn giữ; bàn giao hướng dẫn dùng thử và giới hạn thực tế.

## 6. Phạm vi, đầu ra và ca nghiệm thu gốc T27–T29

Sao nguyên bản từ kế hoạch; giữ đủ **33 ID** và điều kiện hoàn tất, chưa đánh dấu ca nào đã chạy.
### T27 — Tài sản cố định và khấu hao tham khảo

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Sử dụng TAISAN_CODINH và các phiếu chi bảo trì, không lẫn hồ sơ tài sản với chi tiền. |
| Phạm vi | CRUD tài sản, liên kết chi MaTS, lịch khấu hao đường thẳng được công bố; không xây kế toán tài sản đầy đủ. |
| Quy mô | M — tài sản và báo cáo tham khảo |
| Phụ thuộc | T26 |
| Đầu ra | Module tài sản, chi liên quan, bảng ước tính khấu hao. |

**Kiểm thử bắt buộc:**

- [ ] `T27-K01` [I/M] Tạo/sửa tài sản tên/ngày/giá trị/số tháng hợp lệ; reload và tìm theo mã đúng.
- [ ] `T27-K02` [I] Giá trị 0/âm, tháng 0/âm, tên rỗng/quá dài hoặc phiếu chi MaTS không có bị chặn.
- [ ] `T27-K03` [I] Thêm hồ sơ không tự cộng một chi nếu quy trình không thực trả.
- [ ] `T27-K04` [I/M] Chi sửa chữa gắn đúng tài sản, tiền trong PHIEUTHUCHI không bị cộng thêm nguyên giá vào chi.
- [ ] `T27-K05` [U/I] Khấu hao bắt đầu tháng mua; trước mua 0; dừng đúng số tháng; không vượt nguyên giá.
- [ ] `T27-K06` [U] Nguyên giá không chia hết số tháng: tháng cuối bù làm tròn, tổng bằng nguyên giá.
- [ ] `T27-K07` [I/M] Khấu hao là ước tính không tự tạo phiếu chi và không bị gọi là giá vốn bán.
- [ ] `T27-K08` [I] Xóa tài sản đã có chi liên quan bị chặn, không mất phiếu chi.
- [ ] `T27-K09` [U/M] Chỉ ADMIN sửa/xem tài chính; nguồn tham khảo ghi nhãn rõ.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

### T28 — Báo cáo SQL và đối chiếu số liệu

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Tổng hợp bằng SQL với đúng nguồn và thời điểm, tránh tải toàn bảng để tính trên UI. |
| Phạm vi | Doanh thu, món/phương thức/kênh, tồn/kiểm kê, công nợ theo ngày, thu chi, giờ/lương/tài sản; không lợi nhuận giá vốn. |
| Quy mô | L — nhiều nguồn số liệu |
| Phụ thuộc | T27 |
| Đầu ra | ReportRepository/Service, query/procedure báo cáo, filter và DTO tổng hợp. |

**Kiểm thử bắt buộc:**

- [ ] `T28-K01` [I] Doanh thu chỉ đơn đã trả theo NgayThanhToan, không NgayLap nếu hai ngày khác; loại hủy/mở.
- [ ] `T28-K02` [I] Tiền món sau giảm/ship/phải trả/phí đối tác tách rõ và cộng đúng.
- [ ] `T28-K03` [I] Một bill 2 món×2 TT không nhân doanh thu bốn lần; aggregate độc lập trước JOIN.
- [ ] `T28-K04` [I] Món theo giá snapshot; đổi menu/công thức/ưu đãi không đổi báo cáo đơn cũ.
- [ ] `T28-K05` [I] Filter [từ ngày, ngày sau đến ngày) nhận giờ cuối ngày; không CAST cột ngày để lọc nếu tránh được.
- [ ] `T28-K06` [I] Không dữ liệu trả tổng 0/list rỗng; lỗi DB hiển thị lỗi, không biến thành 0 giả.
- [ ] `T28-K07` [I] Công nợ ngày X bỏ khoản nợ sau X và lần trả sau X, khác view hiện tại đúng kỳ vọng.
- [ ] `T28-K08` [I] Thu tiền bán và Thu khác mỗi nguồn tính một lần; nợ/chi lương/nhập trả ngay không double count.
- [ ] `T28-K09` [I] Đối chiếu tồn mở sổ + nhập − xuất − tiêu hao + chênh lệch kiểm kê = tồn hiện tại trong kỳ có đầy đủ nguồn.
- [ ] `T28-K10` [I] Giờ/chốt lương/thực chi lương tách đúng, khấu hao không cộng như chi tiền.
- [ ] `T28-K11` [M] Filter đổi/refresh/grid không hiển thị kết quả cũ của query chậm trả sau query mới.
- [ ] `T28-K12` [U/M] Role không được xem báo cáo tài chính bị chặn ở BLL, không chỉ ẩn menu.
- [ ] `T28-K13` [M] Nhãn ghi rõ trước giảm toàn bill, dòng tiền quản trị/đối tác chưa đối soát, không lợi nhuận chính xác.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

### T29 — Đo hiệu năng và tối ưu SQL có bằng chứng

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Tối ưu phần chậm quan sát được; không thêm index hoặc cache theo cảm tính. |
| Phạm vi | Dữ liệu test lớn, actual plan, STATISTICS IO/TIME, roundtrip/TVP, khóa/deadlock và paging; không thay công nghệ DAL. |
| Quy mô | M — đo và sửa có mục tiêu |
| Phụ thuộc | T28 |
| Đầu ra | Báo cáo trước/sau, migration index có lý do, query tinh gọn không đổi nghiệp vụ. |

**Kiểm thử bắt buộc:**

- [ ] `T29-K01` [P] Dữ liệu test đề xuất 10.000 hóa đơn và nhiều dòng/TT được tạo hợp lệ trong DB riêng; seed nhỏ không dùng làm benchmark.
- [ ] `T29-K02` [P] Đo load POS/menu/bàn, checkout 20 dòng, danh sách hóa đơn và báo cáo trước tối ưu trên cùng máy/dataset.
- [ ] `T29-K03` [P] Actual plan/IO chỉ ra scan/sort/lookup đáng sửa; thêm index chỉ khi truy vấn hưởng lợi.
- [ ] `T29-K04` [P] Candidate index NgayThanhToan, MaHD, MaPN, MaCN/lịch sử và FK được đánh giá; không tạo mọi index tự động.
- [ ] `T29-K05` [P/I] Truy vấn mới/index mới cho cùng kết quả tiền/kho/nợ như bản trước.
- [ ] `T29-K06` [P/I] Benchmark write sau thêm index không giảm quá mức; index dư/trùng được xử lý khi có bằng chứng.
- [ ] `T29-K07` [P] Danh sách phân trang; report aggregate phía SQL, không tải toàn bộ bill để sum trong C#.
- [ ] `T29-K08` [P] TVP ghi nhiều dòng giảm roundtrip; trigger set-based cho 1/nhiều dòng, không cursor mỗi dòng.
- [ ] `T29-K09` [I/P] Hai session bán/nhập/xuất tranh tồn: không sai dữ liệu; ghi nhận deadlock và điều chỉnh thứ tự khóa nếu tái hiện.
- [ ] `T29-K10` [M/P] UI không đứng khi query chậm; mục tiêu ban đầu thao tác thường dưới 1 giây, report dưới 3 giây trên dataset/máy ghi nhận, không cam kết mọi máy.
- [ ] `T29-K11` [I] Không dùng NOLOCK trên tiền/kho để che chậm; không cache trạng thái chốt làm nguồn tin cậy.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.



