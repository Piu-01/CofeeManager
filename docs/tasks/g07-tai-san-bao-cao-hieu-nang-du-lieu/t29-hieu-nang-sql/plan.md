# T29 — Đo hiệu năng và tối ưu SQL có bằng chứng

31/10/2026. **Đã duyệt cùng bộ plan G07 ngày31/10/2026; Đang làm**, phụ thuộc T28/schema026. Mục tiêu: đo trên dataset đủ lớn, chỉ sửa query/index có bằng chứng, giữ tiền/kho/nợ/quyền và công khai kết quả thực tế. Không bảo đảm hiệu năng mọi máy.

## 1. Dataset benchmark và môi trường

Tạo DB test riêng được đánh dấu G07Bench, baseline không seed demo và migrations001–026; setup dữ liệu hệ thống/ADMIN/CASH/STORE theo cơ chế hiện hành, bí mật chỉ trong bộ nhớ hoặc cấu hình local không đưa vào artifact. Tạo fixture nền gồm khoảng200sản phẩm/công thức,50nguyên liệu,50bàn,100khách,10NCC và nợ/lương/tài sản đủ nguồn báo cáo; tuân miền mã thực tế, không tạo vượt số mã hỗ trợ.

Tạo đúng10.000hóa đơn hợp lệ trong kỳ90ngày:8.000đã trả (mỗi bill3dòng, trong đó2dòng cùng món khác ghi chú;6.000bill dùng2phương thức,2.000bill dùng1),1.000mở mang đi và1.000hủy. Phân bố3kênh ở billđãtrả, khách/KM/ship/phí hợp lệ; đơn mở không giữ1.000bàn hoặc vượt bàn mở unique. Ngày/snapshot/tiêu hao/thanh toán khớp; hỗ trợ100%giảm/0đ nếu contract cho phép bằng ca riêng, không thay tỉ lệ dataset chính.

Mở sổ nguyên liệu đủ cho toàn bộ tiêu hao và benchmark writes; nhập/xuất/KK/trả nợ/lương mẫu tạo qua public procedure sở hữu transaction bằng runtime role test. Tạo giao dịch bán/checkout qua contract hiện hành; không disable trigger, tự gán paid, sửa snapshot/cấp mã để tăng tốc. Các ngày lịch sử hợp lệ được truyền qua contract cho phép; nếu endpoint không cho giả lập ngày thì xây dataset trong ngày thật và ghi rõ phân bố hạn chế, không âm thầm sửa procedure. Thay đổi dataset khác đáng kể phải cập nhật báo cáo trước đo.

Lưu dataset manifest: số lượng thực theo trạng thái/kênh/dòng/TT, mã range, nguồn mở sổ, totals tiền/kho/điểm/nợ/lương, schema/checksum, số dòng từng bảng, SQL version/edition/compatibility, CPU/RAM/storage/OS/.NET, cấu hình connection/pooling và isolation. Server/DB exactname không nhầm development. Dataset có hash/seed cố định, dữ liệu giả rõ tính chất.

## 2. Cách đo và quyết định tối ưu

1. Build baseline T28 và lưu binary/source hashes; tạo fixture hoàn tất rồi để dataset đứng yên khi đo read. Ghi rowcount, command, parameter và totals kỳ vọng trước mỗi scenario.
2. Đo SQL direct runtime user và end-to-end service/UI riêng. Scenario: POS menu/tồn/bàn; checkout20dòng có ghi chú/two methods; danh sách hóa đơn trang1/trang100/trangcuối, filter chọn lọc/rộng; toàn bộ loại report T28 kỳ1/30/90ngày và dashboard. Không chỉ đo query có lợi cho index mới.
3. Mỗi read3warm-up +20lượt đo; write2warm-up +10lượt đo, ghi raw elapsed từng lượt, median/p95, logicalreads/CPU/elapsedSQL, rows/roundtrips. P95 dùng nearest-rank, ghi rõ số mẫu. Không gộp thời gian dựng fixture/binding vào SQL latency; số UI end-to-end đo riêng.
4. Thu actual plan bằng STATISTICS XML và InfoMessage STATISTICS IO/TIME trong session diagnostic riêng; không tính overhead bật plan vào latency thường. Lưu .sqlplan/raw log và parameter/value hợp lệ đã loại secret. Không DBCC FREEPROCCACHE/DROPCLEANBUFFERS hoặc bật trace serverwide. Lượt đầu/cold quan sát được ghi riêng, không giả cam kết coldcache.
5. Writes chạy trên bộ fixture tương đương trước/sau: request và đơn mới cho từng lần, không benchmark replay như checkout thật. Chuẩn bị trước ngoài timer; checkpoint business totals trước/sau và trừ biến động hợp lệ do writes. Không rollback vòng ngoài bọc procedure không cho transaction; dùng hai DB cùng dataset hoặc reset bằng tái tạo DB test, không sửa/xóa lịch sử bất biến.
6. Lập candidate matrix cho index NgayThanhToan, MaHD, MaPN, MaCN/lịch sử và FK. Ghi index đã có (ví dụ IX_CTHD_MaHD/IX_CTTT_MaHD), actual scan/sort/lookup, cardinality/rows/read, lợi ích/hại dự kiến; không thêm trùng index hoặc chỉ chép missing-index suggestion.
7. Thử từng candidate trên DB test, đo cùng parameter/dataset/build/máy; chỉ giữ nếu cải thiện lặp lại median latency≥10% hoặc logicalreads≥20% cho điểm nghẽn thực, kết quả nghiệp vụ bằng nhau và không làm write/read scenario liên quan xấu hơn15% median/p95 vượt nhiễu. Đây là tiêu chí chọn trong bộ plan, không thay mục tiêu thao tác<1s/report<3s. Dao động lớn thì thêm tối đa20lượt xác minh, báo nhiễu, không lấy mẫu tốt nhất.
8. Chỉ sửa predicate/join/projection/paging hoặc index phục vụ query có bằng chứng; không đổi business semantics. Nếu mọi scenario đạt mục tiêu và plan/IO không chỉ ra lợi ích đáng giữ, bàn giao báo cáo không thêm index. Không tạo migration027 rỗng để đánh dấu đã tối ưu.

Các phép đo trước/sau thuộc pha triển khai T29 sau duyệt bộ plan, mỗi pha ghi source/dataset riêng và không sửa source trong cùng pha đo. Đợt nghiệm thu sau đó chạy **bản cuối cố định**, so với artifact baseline đã lưu, không đổi source/migration để chữa FAIL trong đợt đó.

## 3. Phạm vi file và thứ tự công việc

| Bước | File/khu vực | Verify kỹ thuật/đầu ra |
|---|---|---|
| 1 | tools/database/Measure-G07Performance.ps1; database/tests/g07/performance-fixture.sql | Manifest/marker/allowlist, setup dataset qua contract; audit10.000bill thực và số dư/liên kết; có guard chỉ DBtest của lượt |
| 2 | tools/CafeManager.G07Probe/PerformanceProbe.cs; tools/CafeManager.G07Probe/*.csproj/Program.cs | Chế độ benchmark typed, Stopwatch, rawJSON/CSV, InfoMessage/plan, command count; runtime tách owner, không log secret. Không tạo framework benchmark chung |
| 3 | runs/performance_<runid>/baseline/, candidates.csv; performance-report.md tại task | Lưu baseline, machine/dataset/schema/hash, actualplans/IO/TIME, matrix quyết định từng candidate kể cả loại |
| 4 | database/migrations/027_measured_performance.sql nếu cần | Chỉ CREATE/ALTER index/query đã chọn, preflight026 và metadata; giữ definition/business/permission, checksum, không sửa025/026 đã áp dụng. Không DML/backfill số dư; có script hoàn tác candidate chỉ trên DBtest |
| 5 | DAL repository query liên quan hoặc UI list phát hiện tải quá nhiều, chỉ nếu số đo chỉ ra | Các vị trí cho phép khảo sát: ReportRepository, InvoiceRepository, SalesRepository, ProductRepository, TableRepository và query/proc tương ứng. ADO.NET/TVP/paging/async giữ nguyên; mỗi sửa có source→plan/IO→kết quả trước/sau |
| 6 | database/tests/g07/t29-equivalence.sql; probe concurrency/performance | Parity tiền/kho/nợ/quyền, paging và hai session; nguyên nhân mới sai nghiệp vụ lập bug riêng, không tự debug ngoài phạm vi tối ưu |
| 7 | technical-checks.md, performance-report.md; ../development-guide.md | Chốt bản tối ưu hoặc không thêm index, số đo trước/sau/raw artifact, giới hạn/quyền/thực nghiệm và hướng dẫn dùng thử; chờ duyệt nghiệm thu nhóm |

Không sửa source rộng trước khi có baseline. Nếu cần tối ưu trigger hoặc thứ tự khóa để giải deadlock: phải tái hiện bằng graph/session evidence, xác định cạnh khóa và delta nhỏ; nếu làm đổi contract/ngoài query/index đã duyệt thì trình bổ sung trước sửa. Không bỏ transaction, bỏ demand, bỏ trigger tồn hoặc thêm NOLOCK để đạt thời gian.

## 4. TVP, trigger, đồng thời và UI

Đo command count riêng action checkout20dòng: một lời gọi ghi use case bằng TVP; permission/preview/reconcile là lời gọi riêng có nhãn, không che chúng trong tổng roundtrip. Không loop20lần ghi dòng để thay TVP. Trigger nghiệp vụ tổng/kho kiểm1/nhiều dòng bằng cùng input; cursor dùng khóa tập mã theo thứ tự ở source hiện có không mặc định là lỗi “cursor mỗi dòng”. Chỉ đề xuất đổi khi có chi phí/thắt nút đo được và bảo toàn khóa/tồn.

Concurrency harness hai connection độc lập, có barrier và query quan sát waiter để chứng minh overlap: checkout/checkout thiếu tồn, checkout/nhập, checkout/xuất; mỗi scenario tối thiểu10lặp với dữ liệu riêng. Đối chiếu tồn không âm, tiền/điểm/receipt/snapshot một lần, rollback sau lỗi và @@TRANCOUNT/XACT_STATE so với baseline session, không hardcode trạng thái khi provider có hành vi khác. Deadlock ghi mã1205 và graph nếu quyền hiện có thu được; không bật server session global chỉ để lấy graph. Không thu được graph ghi giới hạn và lưu waiter/error/session; không tuyên bố không deadlock mọi tải.

UI thật125%: POS/menu/bàn, history và report đổi filter/refresh khi query chậm; stopwatch từ action tới bind xong, ghi số hàng và kích thước client. Quan sát phản hồi bàn phím/điều hướng/hủy/đóng, không Thread.Sleep UI/DoEvents. Probe trì hoãn response trong harness, không sửa production WAITFOR. Không đo hover/animation/spacing/hình thức; người dùng kiểm visual. Nếu tool không điều khiển modal, ghi native BLOCKED, kết quả component/service không thay native.

## 5. Ma trận nghiệm thu sau duyệt đợt test G07

| ID | Dataset/thao tác | Expected/bằng chứng |
|---|---|---|
| T29-K01 | Audit dataset,10.000bill/24.000dòng paid/14.000dòng TT theo thiết kế; mở/hủy hợp lệ | SQL rowcount/constraint/tiền/tiêuhao/receipt khớp manifest, DB riêng; seed nhỏ không làm benchmark |
| T29-K02 | Baseline+final toàn scenario mục2 trên dataset/máy tương đương | Raw20read/10write và median/p95/IO/TIME/roundtrip, timingSQL vs service vs UI phân biệt |
| T29-K03 | Actualplan/IO của từng candidate | Chỉ rõ operator/reads cần sửa, kết quả lặp; candidate giữ/loại có căn cứ, không khẳng định từ estimatedplan |
| T29-K04 | Index ngàyTT/MaHD/MaPN/MaCN/FK | Matrix existing/candidate/decision/size/writecost và kế hoạch thụ hưởng; không tạo tất cả |
| T29-K05 | So sánh cùng filter/parameter, snapshot tiền/kho/điểm/nợ/lương | Rowset/totals/order/parity đúng, không đổi giá snapshot/nguồn/trạng thái hoặc quyền |
| T29-K06 | Writebenchmark trước/sau, số index/kích thước | Delta không vượt tiêu chí mục2 nếu giữ index, hoặc loại candidate; index dư chỉ xử lý khi đo/chứng minh |
| T29-K07 | Page1/100/cuối/empty/lastfull50, report rows lớn | Server trả≤50rows+metadata, tổng SQL cả tập; không tải toàn bộ bill về C# để sum |
| T29-K08 | Checkout20dòngTVP, trigger1/nhiều dòng | Ghi nghiệp vụ một call, tổng commandcount công khai; tiền/kho cập nhật một lần, không cursor mỗi dòng để tính số dư |
| T29-K09 | Hai session10lặp mỗi cạnh tranh ghi nêu mục4 | Overlap thật, tồn/transaction/receipt đúng, deadlock được ghi/đối chiếu trước retry; lỗi mới báo bug, không vá trong lượt |
| T29-K10 | WinForms125% thao tác thật + query chậm | UIresponsive, thời gian thực so mục tiêu<1s/<3s; chưa đạt ghi rõ không tự PASS hoặc cam kết mọi máy |
| T29-K11 | Audit definition SQL cuối và hành vi refresh sau ghi/session đổi | Không NOLOCK/đọc cache chốt làm nguồn; quyền/dữ liệu DB vẫn quyết định, không bỏ kiểm để tăng tốc |

UX-K42 và regression UX-K37 → K07/K10 query muộn/đóng; UX-K39 → K10 keyboard; K40/K41 → K10 kỹ thuật125% tại1280×720/1366×768, tên dài/nhiều dòng/tiền lớn. Visual SKIPPED theo phân công,100/150% NOT RUN nếu chưa chạy.

Fixture và artifact chốt trước lượt nghiệm thu; test-results.md ghi riêng từng subcase/môi trường/schema/source/dataset/expected/actual. performance-report.md giữ nguyên baseline và candidate bị loại, không ghi đè số chậm. Cleanup DB benchmark và candidate sau lưu bằng chứng theo exactname+marker; không chạm DB development/process khác. Bàn giao report nhóm đủ33ID kể cả FAIL/BLOCKED, debug theo đợt được duyệt riêng.

