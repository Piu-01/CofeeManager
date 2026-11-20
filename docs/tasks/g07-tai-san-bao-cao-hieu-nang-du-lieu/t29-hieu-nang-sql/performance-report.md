# T29 — Báo cáo đo và quyết định tối ưu

31/10/2026. **Đã hoàn thành pha đo phục vụ triển khai; chưa nghiệm thu11ID. Không thêm index và không tạo migration027 rỗng.** Source cuối giữ025/026.

## Môi trường và dataset

- Lượt đầy đủ: [performance_20261031_103410_dbbb420d](../runs/performance_20261031_103410_dbbb420d/manifest.json), kết quả Succeeded, SourceUnchanged=true, tạo1DB/dọn1/còn0. Schema001–026, SQL Server2025RTM17.0.1000.7 Developer; SDK10.0.401/buildDebug, ADO.NETSqlClient, runtime G07Runtime thuộcCafeManagerApp, pooling=false. Thông tin CPU/RAM/ổ trong hardware.json và SQL/OS trong results.json của lượt. Không đổi SQL mode/cache/Windows Scale.
- Không dùng seed demo: bootstrap/admin, danh mục và fixture giao dịch riêng;50NVL mở sổ,200SP/công thức,50bàn,100khách,10NCC,30PN/nợ,2lần trả nợ, xuất hủy,10tài sản, công8giờ và một bảng lương đã chi, accountCASH/STORE ngoàiADMIN. Giao dịch qua public procedure, không tắt trigger hoặc tự UPDATE paid/ngày/snapshot.
- Audit trước đo đúng10.000bill:8.000paid/1.000open/1.000cancelled,24.000dòng món paid và14.000dòng thanh toán, tiền header/dòng/TT khớp. Cả10.000bill thực tế cùng ngày31/10 vì checkout tự lấy ngày server; bộ lọc1/30/90ngày không có phân bố lịch sử90ngày. Công khai giới hạn này, không suy hiệu quả index ngày trên dữ liệu phân tán.
- Writebenchmark thêm12bill20dòng mỗi pha (2warm-up+10samples), tối đa36bill bổ sung. Readpair trước/sau mỗi candidate không có write xen giữa; writes được ghi riêng và dataset lớn dần bằng giao dịch hợp lệ. Không dùng rollback ngoài procedure hoặc đo replay thay checkout.

## Phương pháp và baseline

Read:3warm-up+20samples/scenario, lưu từng sample, median và p95 nearest-rank. SQLdiagnostic actualplan/STATISTICSIO/TIME tách khỏi latency thường. Latency dưới đây là service gồm demand và connection; không phải thời gian UI. Có230file.sqlplan và IO/TIME từng loại/pha; raw timing trong thư mục lượt.

| Scenario | Median ms | P95 ms |
|---|---:|---:|
| ReportSalesDay_90d | 73.1 | 82.0 |
| ReportProducts_90d | 325.3 | 371.1 |
| ReportPayments_90d | 174.3 | 241.0 |
| ReportInventory_90d | 390.6 | 439.5 |
| ReportDebts_90d | 23.1 | 24.3 |
| ReportCashFlow_90d | 58.6 | 63.0 |
| ReportLabor_90d | 24.2 | 25.4 |
| ReportAssets_90d | 23.1 | 26.0 |
| Dashboard | 22.4 | 50.6 |
| POSMenu | 15.9 | 27.9 |
| Tables | 16.2 | 18.6 |
| InvoicePage_100 | 120.2 | 128.5 |

Tất cả median đọc đã đo dưới0,5giây trên máy/dataset này; có outlier p95 trong các pha, giữ nguyên sample. Chưa đo UIend-to-end/native responsiveness để kết luận mục tiêu thao tác<1giây/report<3giây ởUI.

## Candidate và quyết định

| Candidate | Bằng chứng | Quyết định |
|---|---|---|
| IX_G07_HD_Paid(NgayThanhToan,TrangThai) INCLUDE tiền/kênh | Actualplan dùng trong SalesDay/Products/CashFlow; logicalreadsSalesDay359→232(−35,4%). SalesChannel90d median57,0→72,2ms(+26,7%); checkout median28,4→30,8ms(+8,4%) | Loại: có lợiIO nhưng độ trễ không cải thiện ổn định, có scenario vượt ngưỡng15%; không đủ bằng chứng giữ trên dataset ngày hẹp |
| IX_G07_Receipt_OperationSource(Operation,EntityCode) INCLUDE CompletedAt | Actualplan dùng Inventory; logicalreads37545→35913(−4,3%). Inventory90d median400,9→382,9ms(−4,5%); checkout median28,4→36,5ms(+28,4%) so baseline đầu | Loại: không đạt ngưỡng10%latency hoặc20%IO, chi phí ghi cao hơn. Không kết luận mọi chênh lệch là do index khi có nhiễu |

[candidates.csv](candidates.csv) giữ đủ33scenario của mỗi candidate với median/p95 và phần trăm trước/sau. index-plan-uses.json trong lượt chỉ ra fileplan thực sự có operator dùng candidate. Không chỉ nhìn missing-index suggestion hoặc estimatedplan. Mỗi candidate được drop trong DBtest sau đo và đối chiếu, không áp vào development.

Actualplan Inventory trước receiptindex có TableScan#events đọc16.511rows một execution; không suy50fullscan từ sourceOUTERAPPLY. View/temp/aggregate là phần cần khảo sát nếu xuất hiện chậm thực tế, nhưng hiện chưa vượt mục tiêu đọc service và không có thay đổi query thêm theo cảm tính.

| Nhóm index khác | Đánh giá |
|---|---|
| MaHD trên chi tiết món/thanh toán | Đã có IX_CTHD_MaHD/IX_CTTT_MaHD; actualplan/IO Products/Payments được lưu. Không tạo bản trùng; chưa thử coveringindex mới và không tuyên bố đã đo mọi cấu hình |
| MaPN/nguồn nhập, MaCN/lịch sử vàFK | Bảo vệ PK/unique/FK và các index nguồn sẵn có được giữ; dataset30PN/nợ nhỏ, Debts median~23ms. Chưa có điểm nghẽn/IO đủ để thêm index. Không dùng số đo seed nhỏ làm bằng chứng tải nợ lớn |

## Tính đúng và giới hạn

- ReportParity trước/sau drop mỗi candidate khớp: tổng đầy đủ và rowset trang đầu của9loại báo cáo, cùng snapshot sau các write hợp lệ. Đây không phải đối chiếu mọi trang hoặc fullregression tiền/kho/điểm/nợ/lương. Businesshash baseline/final có trongresults, khác do thêm giao dịch writebenchmark có chủ đích; không dùng haihash đó chứng minh DDLno-op.
- Dataset đầu dựng đủ10.000 và audit tiền khớp; lỗi runtimeSQL229 đã được tái hiện rõ objectview ở lượt102036, xử lý contextOWNER trong module đọc có demandADMIN, rồi lượt đầy đủ này qua cả đọc tồn trên tải lớn. Không grantSELECTview cho runtime.
- Lượt101005 giữ baseline một phần rồi dừng229; các lượt setup trước lỗi dữ liệu nền/nestedINSERTEXEC/header/ngày chi lương đều giữ artifact và dọn DB. Không dùng chúng làm benchmark thành công hoặc sửa expected cũ.
- Chưa chạy unit suite, fixture nghiệm thu, hai session tranh tồn/deadlock, native keyboard/loading/timeout hoặc nghiệm thu11IDT29/33IDG07. NativeUI metric chưa có, hình thứcSKIPPED theo phân công,100/150%NOTRUN. Đây là phần chờ duyệt đợt test nhóm.
- Giữ kiến trúcSqlClient/TVP/paging/async, khôngNOLOCK, không cache trạng thái chốt, không đổi engine/isolation/database settings. Không claim tối ưu tuyệt đối hoặc lợi nhuận chính xác.



## Cập nhật sau đợt test31/10

Các mục trên là bản ghi lịch sử pha triển khai. Đợt test riêng đã được duyệt và chạy; xem [test-results.md](test-results.md) và [báo cáo nhóm](../report.md):23PASS/1FAIL/9BLOCKED. G07-B01 chờ duyệt sửa; native còn thiếu bằng chứng. Không dùng trạng thái lịch sử 'chưa chạy/chờ duyệt test' thay kết quả hiện tại.
