# G07 — Báo cáo triển khai và đợt test

31/10/2026 · **Đang làm: 23 PASS / 1 FAIL / 9 BLOCKED trên33ID gốc.** Chưa nghiệm thu hoàn tất G07. Requirements, cả bộ plan và đợt test đã được duyệt bằng các lần “duyệt” riêng; chưa có phê duyệt debug G07-B01.

## Đã triển khai

- T27: migration025; CRUD hồ sơ tài sản, revision, receipt/replay theo request/actor, khấu hao tham khảo; chi liên quan có phân trang, chọn/mở tài sản từ thu chi và mở phiếu từ tài sản. Lưu tài sản không tự sinh chi.
- T28: migration026; chín báo cáo typed SQL/DAL/BLL/WinForms, nguồn chứng từ và dashboard ADMIN dữ liệu thật. Tiền theo ngày thanh toán/snapshot; tồn có mốc mở sổ và coverage; nợ cutoff; công/chốt/chi lương tách thời điểm. Quyền kiểm BLL/SQL.
- T29: harness tạo10.000bill hợp lệ,33scenario, actualplan/IO/TIME và đo write20dòng. Hai index candidate đã thử và loại; **không có027 tối ưu**. Phiên bản027 trong đề xuất bug là sửa khóa, chưa tạo/chạy.
- Build Debug/Release thành công0warning/0error: [Debug](build-debug.log), [Release](build-release.log). Kết nối phát triển/F5 và runner nâng cấp có backup được mô tả ở [development-guide](development-guide.md), **chưa chạy nâng DB development**. Không commit/push.

## Bằng chứng và kết quả

SQL Server localhost17.0.1000.7 Developer, SDK10.0.401, Windows; WinForms thực ở DeviceDpi120 (125%). HEAD nền8a38f8691d37089feec149b71a8f5718d37167f3 có working tree; [source-acceptance-026.csv](source-acceptance-026.csv) là SHA256 thực của source/database/tests/tools, không coi HEAD là toàn bộ source đã chạy.

Lượt cuối: [acceptance_20261031_190821_544bc625/manifest.json](runs/acceptance_20261031_190821_544bc625/manifest.json), lệnh `tools/database/Test-G07.ps1 -RunAcceptance -ThroughVersion 26`. Mỗi lượt cố định source/fixture/harness; không sửa sản phẩm trong pha nghiệm thu.

| Phần | Actual |
|---|---|
| Unit Assets/Reports | 15/15 PASS; [TRX](runs/acceptance_20261031_190821_544bc625/g07-unit.trx) |
| Ca tích hợp mở rộng | 129 PASS /10 FAIL trong139subcase;10FAIL là cùng một bug cạnh tranh tài sản; [expected/actual](runs/acceptance_20261031_190821_544bc625/extended-results.json) |
| Fixture SQL T27/T28/T29 | 3/3 thành công; không thay toàn bộ33ID |
| Migration | Clean/demo001–026 và rerun026; snapshot tiền/kho/điểm/nợ/lương không đổi. Schema/checksum trong clean/demo-migrations.jsonl của lượt |
| Source | SourceUnchanged=true; không đổi source/migration/expected trong mỗi lượt |
| Hai session tranh tồn | 10checkout/checkout +10checkout/nhập +10checkout/xuất PASS, có barrier quan sát lock-waiter |
| Tài sản/chi đồng thời | 10/10 lượt cuối có SQL1205; cùng lỗi được tái hiện ở lượt190439, đã lấy10deadlockgraph của đúng DB đó |
| WinForms thực125% | Asset/report binding, handler Xem báo cáo, owner modal khóa cha/không taskbar/không AcceptButton, bounds kỹ thuật1280×720 và1366×768 đã chạy |
| Native input | BLOCKED: Computer Use đọc được cây control nhưng click báo geometry unavailable; recovery/set_value báo editor không trong cache. Không xác nhận được focus/input/CRUD/pending/querymuộn. [Transcript](native-transcript.json) |
| Hình thức và DPI khác | Hình thức SKIPPED theo phân công;100/150% NOT RUN. Không chụp/so hình, không đổi Windows Scale |

Ma trận gốc được giữ nguyên, kết quả chi tiết tại:

| Task | PASS | FAIL | BLOCKED | Biên bản |
|---|---:|---:|---:|---|
| T27 | 4 | 1 | 4 | [test-results](t27-tai-san-khau-hao/test-results.md) |
| T28 | 9 | 0 | 4 | [test-results](t28-bao-cao-doi-chieu/test-results.md) |
| T29 | 10 | 0 | 1 | [test-results](t29-hieu-nang-sql/test-results.md) |
| G07 | 23 | 1 | 9 | 33ID; không đồng nhất với139subcase |

Các kiểm tra tính tay gồm2dòng×2TT không nhân tiền; đổi menu/công thức/KM giữ snapshot;54.321−5.432+5.000=53.889, phí2.444 tách riêng; nợ cutoff100→70; thu+73/chi+81 không doublecount; tồn200+1−2−1=198; giờ8/chốt280.000/chi280.000 tách ngày; ca qua đêm8h và1ca thiếu giờ ra. Fixture lịch sử tạo bằng baseline trước migration: bill25.000 lập01/04/2025, trả02/04/2025 23:59:59 chỉ xuất hiện ngày trả.

## Hiệu năng

[performance-report](t29-hieu-nang-sql/performance-report.md) và [artifact](runs/performance_20261031_103410_dbbb420d/manifest.json):10.000bill đầu,8.000paid/1.000open/1.000cancel,24.000dòngpaid/14.000TT;230actualplans; mỗi scenario3warm+20sample. Write mỗi phase2warm+10checkout20dòng; write tạo thêm giao dịch hợp lệ có ghi nhận. Dataset ngày trả thực31/10, **không phân bố giả90ngày**.

Median service đọc dưới0,5s trên máy/dataset ghi nhận; đây không là số đo UIend-to-end. Paidindex giảmIO nhưng một scenario tănglatency26,7%; receiptindex cải thiện nhỏ và write tăng28,4%; cảhai loại. Parity9report tổng/trangđầu cùngsnapshot trước/sau drop candidate khớp; không tuyên bố mọi trang/fullregression hoặc tối ưu mọi máy.

## Đợt bug cần duyệt

**Một bug G07-B01:** [báo cáo nguyên nhân/hướng sửa/regression](t27-tai-san-khau-hao/bug-report.md). Deadlock graph xác nhận AssetWrite giữ range PHIEUTHUCHI, chờ X tài sản; FinanceCreate giữ range tài sản, chờ insert range PHIEUTHUCHI. Barrier đã rollback, graph chỉ còn hai session sản phẩm.

Đề nghị migration027 thống nhất khóa transaction theo MaTS trước khóa hàng/range ở hai đường ghi; giữ quyền, revision, replay/actor và transaction, không retry tiền tự động. Regression lại các race/receipt/transaction, đường ghi chi liên quan và report/writecost theo ảnh hưởng. Chưa sửa hoặc áp migration; cần duyệt **cả đợt G07-B01 một lần**, bao gồm regression. Ca native còn thiếu vẫn phải kiểm riêng, không được coi phê duyệt bug là PASS UI.

## Lịch sử, tài nguyên và giới hạn bàn giao

- Lượt185205 chọn nhầm test DAL cầnfixtureT05 ngoài15G07unit;185300 sai truyền đối số runner;185610 fixtureKM sai định dạng.190114 thử dựng lịch sử bằng UPDATE đơn đã trả bị51022 đúng thiết kế. Giữ nguyên log/kết quả; chỉ sửa harness/fixture giữa lượt, chuyển lịch sử về baseline contract, không tắt trigger hoặc vá sản phẩm.
- 190308:128PASS;190439 thêmrace phát hiện10FAIL;190821 thêm ca qua đêm:129PASS/10FAIL. Không lấy lượt cũ ít ca thay kết quả cuối có FAIL. Các lỗi kỹ thuật trong pha triển khai cũng giữ artifact tại runs/ và technical-checks của task.
- [resource-summary.csv](resource-summary.csv) liệt kê từng DB tạo/dọn/còn giữ:28lượt,46DBtạo/46dọn/0giữ; [resource-totals.json](resource-totals.json) và [resource-audit.json](resource-audit.json) đối chiếu sys.databases cuối. Cleanup exact name+marker, không DROP wildcard; không còn DBtest G07 giữ lại.
- Reviewer tự động từ chối lệnh xuất toàn bộ ringbuffer vì có thể kèm metadata DB khác. Phương án truy vấn lọc tại SQL chỉ trả graph có mọi process thuộc đúng DBtest G07 đã được chấp thuận; lưu10graph tương ứng, không lưu whole ringbuffer.
- Native click/Tab/clicklần2, pending/restart/timeout/I/O, Enter/Esc, đổi phiên/querymuộn và một số thao tác nguồn chưa có bằng chứng; xem ma trận BLOCKED và plan. Không mở rộng kết quả sang các nhánh/mọi trang chưa chạy. Hình thức do người dùng kiểm,100/150% chưa chạy. G06 và lịch sử T01–T32/325ID giữ nguyên.

**Trạng thái bàn giao:** source triển khai G07 đã có, build qua; nghiệm thu còn G07-B01 và9ID BLOCKED. G07 chưa Hoàn tất; chờ duyệt đợt bug và phiên kiểm native có thể thao tác ổn định.
