# Kết quả VI/EN — lịch sử kỹ thuật và đợt test đã duyệt

**Cập nhật 10/11/2026:** người dùng đã duyệt test và sau đó duyệt debug ba bug;
kết quả regression mới nhất tại `regression-results.md`: 153/153 unit và 21/21
component PASS, ba bug đã sửa. Native toàn task vẫn chưa đủ. Kết quả test trước
fix ở phần cuối file được giữ làm lịch sử.
Giữ nguyên các kết quả kỹ thuật trước đó làm lịch sử, kể cả ghi chú chưa chạy tại
thời điểm ấy. Ba bug đã sửa theo regression mới; task vẫn Đang làm vì giới hạn native.

Ngày 10/11/2026. Source: HEAD `8a38f86` + working tree trong
`runs/source-manifest.csv`. Schema SQL: **N/A — không gọi SQL**. Môi trường/command
đầy đủ tại `technical-checks.md`. Test xUnit cuối dùng dữ liệu tự tạo, file preference
ở thư mục temp riêng được dọn sau ca; không đọc/ghi language.txt thật của người dùng.

| ID | Ca xUnit mới / dữ liệu và expected | Actual | Kết quả |
|---|---|---|---|
| LOC-U01 | Mặc định VI; key chưa biết giữ nguyên câu và CRLF | Đúng nguyên văn, kể cả chuỗi rỗng | PASS |
| LOC-U02 | VI→EN→VI; cùng template dùng LF/CRLF | EN `Save`, hai newline lookup bằng nhau, quay lại `Lưu` | PASS |
| LOC-U03 | CurrentCulture en-US; tiền 81000, ngày 10/11/2026, trang 3 | `TOTAL DUE: 81.000 ₫`, `As of 10/11/2026`, `Page 3` | PASS |
| LOC-U04 | Toàn bộ resource: unique key, EN không rỗng, placeholder khớp, T trả đúng value | Mọi entry đúng | PASS |
| LOC-U05 | Request giao hàng, category Chi, tên/địa chỉ Việt, option nhãn loại | Mã `Giao hang`/enum Chi và dữ liệu gốc giữ nguyên; nhãn EN riêng | PASS |
| LOC-U06 | MapNumber 53403 ở VI→EN→VI | Chữ đổi đúng; Number/Kind/RequiresReconciliation giữ nguyên | PASS |
| LOC-U07 | Preference thiếu, rỗng, fr, JSON hỏng, EN, en+vi | Trả VI an toàn | PASS |
| LOC-U08 | Save EN/VI, tạo instance mới; ngôn ngữ phiên không đổi | File/roundtrip đúng; không còn temp dư | PASS |
| LOC-U09 | Khóa file không cho chia sẻ; thử lưu đè; enum 99 | Lưu trả false, lựa chọn cũ EN giữ nguyên; không temp dư | PASS |
| LOC-U10 | Đích là thư mục thay cho file | Read trả VI, Save trả false, không crash | PASS |

10 ca mới và các unit có sẵn trong filter: **152 PASS / 0 FAIL / 0 SKIPPED**.
Expected/actual từ assertions và từng test case có trong `runs/unit-final.trx`.
Lượt sandbox cũ ABORTED do IPC testhost, tách riêng, không dùng làm kết quả trên.

| Kiểm tra khác | Actual | Kết quả |
|---|---|---|
| Debug / Release / build unit | Mỗi build 0 warning, 0 error | PASS kỹ thuật |
| Audit resource/source | 1.519 used keys, 1.519 EN keys, 0 violations | PASS kỹ thuật |
| Source trước/sau unit cuối | 0 thay đổi trên 272 file snapshot | Khớp |
| LOC-M01–M18 / LOC-F01–F12 | Chưa chạy đợt test WinForms/SQL | NOT RUN — chờ duyệt |
| Layout native 125%, hai kích thước | Chưa chạy | NOT RUN |
| DPI 100% / 150% | Chưa chạy | NOT RUN |
| Hình thức UI | Người dùng tự kiểm | SKIPPED theo phân công |
| StartupForm runtime | Không được Program khởi tạo | NOT RUN; chỉ compile/source |

Task giữ **Đang làm**, chưa nghiệm thu hoàn tất hoặc nâng trạng thái G08/T01–T32.

## Đợt test đã duyệt — 10/11/2026

Run: `runs/acceptance-20261110/`. Source trước/sau khớp **0 thay đổi**; schema test
028, SQL Server 17.0.1000.7, DPI đo thật 120 (125%). Dữ liệu, môi trường, lệnh và
phân loại chi tiết trong `report.md`. App source, migration và test gốc không sửa.

### Bằng chứng thực thi

| Nhóm | Expected | Actual / evidence | Kết quả |
|---|---|---|---|
| Unit thuần | Mọi assertion trong filter pass | 152/152; `unit-acceptance.trx` | PASS |
| Audit | Không key thiếu/thừa/trống/lệch placeholder hoặc literal thuộc bộ quét bị sót | 1.519 key / 0 vi phạm; `audit.log` | PASS trong giới hạn audit |
| SQL E2E VI/EN | Tiền, kho, nợ, payroll, liên kết, replay, quyền và transaction đúng assertions | 7 subcase ×2 =14/14; `vi-e2e.json`, `en-e2e.json` | PASS |
| Checkout đồng thời VI/EN | Hai session cùng request chỉ ghi một lần; điểm/note giữ đúng | Mỗi đơn 1 payment, stock −0,018, points +25, note Việt giữ nguyên; `supplement/probe-results.json` | 2/2 PASS |
| Tên khách trùng mã | `Trong` giữ nguyên ở cột Name | 2/2 FormattedValue=`Trong`; cùng file supplement | PASS |
| Mục ngôn ngữ theo role | ADMIN/CASH/STORE mở được | 6/6 lượt VI/EN; cùng file supplement | PASS component |
| Lưu + Để sau | File đổi, phiên không đổi, owner đúng | 6/6 lượt; cùng file supplement | PASS component |
| Modal owner/result | Owner MainForm, Modal=true, ShowInTaskbar=false, cancel trả No một lần | Đúng; `modal-owner-result-once` | PASS component/message loop |
| Bootstrap sạch | Load và validation VI/EN không crash, caption đúng ngôn ngữ | 4/4 snapshot; cùng file supplement | PASS phần đã chạy |
| Primary probe | Không còn chữ app chưa dịch | 92 entry:85 PASS/7 bị cờ FAIL; `probe-results.json` | 3 lỗi app xác nhận, 4 cảnh báo oracle layout |
| Scroll reachability | Nút ở vùng cuộn có thể đưa vào WorkingArea | 4/4 ca VI/EN, hai kích thước; cùng file supplement | PASS kỹ thuật theo phạm vi |
| Native input | Click/Tab đổi focus và kích hoạt được theo thao tác | Click lỗi geometry; Tab/Shift+Tab không chuyển focus sau refresh | BLOCKED công cụ |

Bốn entry layout ban đầu không bị xóa/đổi expected. Chúng chỉ xét viewport hiện
tại, nên không đủ kết luận nút không thể tiếp cận. Ca cuộn mới kiểm yêu cầu tiếp
cận và PASS; không ghi bốn entry đầu thành PASS hay tự sửa bố cục app.

### Truy vết ma trận màn hình (giữ toàn bộ phạm vi yêu cầu)

| ID | Phần đã có bằng chứng | Kết luận cho toàn ID / phần chưa đủ |
|---|---|---|
| LOC-M01 | Caption/login giữ input; EN→VI status cấu hình sai; native đọc/set username | **FAIL LOC-B03**; pending/login/DEV/khóa qua native chưa đủ |
| LOC-M02 | Clean Bootstrap load/validation VI/EN; bootstrap BLL và login runtime | NOT RUN đầy đủ: activate candidate, unknown, xác nhận admin qua native |
| LOC-M03 | Shell/Home ADMIN caption/binding; role access ngôn ngữ; cuộn125% | NOT RUN đầy đủ: các màn mở đầu CASH/STORE và mọi trạng thái/native |
| LOC-M04 | Các tab caption/binding; editor Thu/Chi gửi enum Chi đúng | NOT RUN đầy đủ: CRUD/unknown/xung đột qua native |
| LOC-M05 | Menu, bàn, khu vực caption/binding | NOT RUN đầy đủ: editor công thức và các nhánh sửa/xóa/xung đột |
| LOC-M06 | Khách/điểm caption, picker, tên Trong giữ nguyên, checkout cộng điểm | NOT RUN đầy đủ: policy preview, mở sổ/điều chỉnh qua UI |
| LOC-M07 | POS load; grid còn SL ở EN | **FAIL LOC-B01**; toàn bộ F2/F3/F4/F8/nháp qua native còn BLOCKED |
| LOC-M08 | Checkout BLL thật, replay/hai session và tiền/kho/điểm đúng | NOT RUN đầy đủ: phân bổ/0₫/unknown qua dialog native |
| LOC-M09 | Đọc nội dung in; mở preview EN còn In… | **FAIL LOC-B02**; in thật/đa trang/lỗi máy in chưa chạy |
| LOC-M10 | Caption/binding; SQL chuyển giao đúng và hash tài chính giữ nguyên | NOT RUN đầy đủ: chuyển/unknown qua native |
| LOC-M11 | Nguyên liệu/tồn load/binding | NOT RUN đầy đủ: mọi filter/pager/editor/mở sổ native |
| LOC-M12 | Tab kho load; SQL nhập/xuất/kiểm kê/replay/stale đúng | NOT RUN đầy đủ: mọi editor/unknown/timeout native |
| LOC-M13 | Thu chi/nợ load; SQL nguồn chi/trả nợ đúng | NOT RUN đầy đủ: filter/ghi/đối chiếu qua native |
| LOC-M14 | Caption/binding; tạo account, login role thật, revoke phiên | NOT RUN đầy đủ: handler cấp/reset/đổi quyền/yêu cầu chờ native |
| LOC-M15 | Caption/binding; SQL ca qua đêm/chốt/chi/replay đúng | NOT RUN đầy đủ: batch/preview/unknown và link trong native |
| LOC-M16 | Tài sản load/binding | NOT RUN đầy đủ: editor/khấu hao/chi liên quan qua native |
| LOC-M17 | Caption; đọc 9 ReportKind ×2 từ SQL | NOT RUN đầy đủ: từng grid kết quả/metadata/drill-down native |
| LOC-M18 | Ba role có mục ngôn ngữ; lưu/Để sau/file/owner đúng | NOT RUN đầy đủ: Đăng xuất ngay → login lại qua lifecycle/native |

### Truy vết ca chức năng

| ID | Actual | Kết luận toàn ID |
|---|---|---|
| LOC-F01 | Input username/password giữ đúng cả hai chiều; status EN→VI sai | **FAIL LOC-B03**; focus native chưa xác minh |
| LOC-F02 | Chưa điều khiển pending login bằng native | BLOCKED native |
| LOC-F03 | Save/Để sau giữ ngôn ngữ phiên ở cả ba role | PASS phần component; nhánh mở dialog mới sau đó chưa đủ |
| LOC-F04 | Chưa chạy đủ logout ngay → login lại | BLOCKED native / NOT RUN lifecycle đầy đủ |
| LOC-F05 | Unit file thiếu/hỏng/khóa/path lỗi PASS, preference được khôi phục sau UI probe | PASS phần unit; mở lại app ở mọi nhánh lỗi chưa đủ |
| LOC-F06 | SQL stale/replay PASS; nháp/busy/unknown qua UI chưa đủ | NOT RUN đầy đủ |
| LOC-F07 | Enum Chi đúng; channel/type code không bị đổi trong các transaction | PASS phần đã chạy; toàn filter/role/contract/issue qua UI chưa đủ |
| LOC-F08 |14 SQL E2E +2 checkout đồng thời PASS, tiền/kho/điểm/nợ/lương/link đúng phạm vi assertions | PASS phần SQL đã chạy; fault/timeout và hành trình UI chưa đủ |
| LOC-F09 | Unit tiền 81.000₫, dd/MM/yyyy và nội dung in PASS; UI POS có 25.000₫ | PASS phần đã chạy; mọi trường/in thực tế chưa đủ |
| LOC-F10 | Confirmation owner/modal/cancel-result PASS component | BLOCKED Tab/Esc/Enter và dialog lồng native đầy đủ |
| LOC-F11 | Khách tên Trong giữ nguyên ở FormattedValue VI/EN | PASS theo ca dữ liệu đã định |
| LOC-F12 | DPI120 thật; cuộn đưa nút vào WorkingArea ở hai kích thước PASS | PASS phần kỹ thuật cuộn; bounds mọi CTA/dialog và input native chưa đủ |

Hình thức **SKIPPED theo phân công**; DPI100%/150% **NOT RUN**. Không kết luận
toàn bộ ma trận PASS từ caption snapshot, audit hoặc SQL riêng lẻ.

### Bàn giao đợt test

Ba bug đã có hiện tượng/nguyên nhân/hướng sửa tại `bug-report.md`, **chưa vá**.
Ba DB test đã DROP sau xác minh namespace, install manifest và hai marker sở hữu;
`cleanup.json` ghi Removed=true cho từng DB. Lựa chọn ngôn ngữ local được khôi phục;
app test đóng, không còn tiến trình fixture cần theo dõi.

Task tiếp tục **Đang làm**, chờ duyệt debug cả batch LOC-B01–B03. Xem tổng hợp
`report.md`; không commit/push, không sửa checklist hoặc lịch sử G08.
