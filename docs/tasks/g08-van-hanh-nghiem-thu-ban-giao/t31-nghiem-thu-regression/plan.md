# T31 — Nghiệm thu xuyên suốt và regression cuối

02/11/2026 · **Đã duyệt cả bộ G08 ngày02/11/2026**, requirements đã duyệt. Phụ thuộc T30; tuân thủ [plans.md](../plans.md). Task này chuẩn bị nghiệm thu trong pha triển khai; chạy nghiệm thu chờ duyệt đợt test nhóm riêng.

## 1. Phạm vi và kế thừa

Đọc lại kế hoạch T31, UX từng luồng, contracts/migrations010–027 và biên bản nhóm trước. Tập hợp đúng325ID T01–T32 cùng44UX-ID; giữ toàn bộ lịch sử/subcase. Ma trận có ID, task sở hữu, bằng chứng/source/schema cũ, thay đổi ảnh hưởng, kết quả kế thừa, phần cần chạy, nơi lưu kết quả mới. Không tự chuyển ngoại lệ đã được người dùng chấp nhận ở G03 thành47/47đã chạy PASS; không coi nhóm có bug đã regression là toàn bộ checklist PASS.

Ưu tiên ca còn thiếu G04/T16–T17, G05, G06 và9ID G07 theo report gốc; thống kê từ từng test-results/report chứ không cộng số tổng cũ để đoán. G07: T27-K01/K04/K07/K09, T28-K06/K11/K12/K13, T29-K10. Phải kiểm phiên native dùng được trước; nếu tool không điều khiển được, ghi transcript và hướng dẫn manual có input/expected rõ, không tick PASS bằng component probe.

T18 đã duyệt chỉ lưu trạng thái giao hiện tại. T31-K05 đối chiếu đơn đã trả, chuỗi chuyển hợp lệ, truy cập lại và tài chính bất biến; công khai không có timeline bền vững, không dựng ngày chuyển từ NgayLap/NgayThanhToan.

## 2. Bước/file triển khai

| Bước | File | Nội dung/verify kỹ thuật |
|---|---|---|
| 1 | ../acceptance-matrix.md; ../ux-matrix.md | Trích ID từ kế hoạch/UX, kiểm unique325/44, không thiếu/trùng; liên kết lịch sử và ghi pending ở ca chưa chạy. T30/T32 kết quả để NOT RUN cho tới đợt test |
| 2 | database/tests/g08/e2e-fixture.sql; tools/CafeManager.G08Probe/E2EProbe.cs | Fixture tạo qua public procedure/service:3role, NV/ca qua đêm, NVL có mở sổ/công thức/SP, khách/ưu đãi, NCC, bàn, phương thức. Không sửa paid/date/snapshot/receipt trực tiếp; dữ liệu cũ kiểm biên dùng fixture hợp lệ theo contract |
| 3 | tools/CafeManager.G08Probe/StateSnapshot.cs, SessionProbe.cs | Đối chiếu snapshot theo PK cho header/dòng/TT/tiêu hao/điểm, nhập/xuất/kiểm kê, nợ/trả/chi và công/lương; session thật/bảo vệ BLL. SQL state sau lỗi capture @@TRANCOUNT/XACT_STATE vào biến trước SELECT như bài học G07 |
| 4 | tools/database/Test-G08.ps1; tools/CafeManager.G08Probe/FailureProbe.cs | Điều phối T30→T31→T32 khi RunAcceptance; manifest/hash/cleanup, không gọi toàn bộ runner cũ làm mặc định. Chỉ reuse test phù hợp theo ảnh hưởng. Timeout/fault injection thuộc harness/kết nối/session DB test; không thêm hook bypass/fault vào sản phẩm |
| 5 | native-test-guide.md trong task; ../test-plan.md | Checklist thao tác app thật theo role, dữ liệu/mã/chứng từ, input/expected, cách lấy bằng chứng không chụp/so hình thức; native owner, pending/IME/select-all, query muộn. Ghi thao tác thực/giới hạn công cụ |
| 6 | technical-checks.md | Build probe và parse fixture, xác nhận đối chiếu/guard target có thể dùng; không chạy full E2E hoặc viết actual giả |

Không sửa sản phẩm/migration theo plan này. Lỗi mới ghi task sở hữu/bug-report, link report G08, chờ duyệt cả đợt. Nếu harness phải sửa giữa lượt, kết thúc lượt hiện tại, giữ kết quả và tạo lượt mới với hash mới; không thay expected để che lỗi.

## 3. Ma trận E2E và expected chính

| ID | Dataset/thao tác bắt buộc | Expected/đối chiếu |
|---|---|---|
| T31-K01 | Login CASH, chọn bàn, cùng món2dòng khác ghi chú, chọn ưu đãi; snapshot trước trả; chia2TT tổng bằng preview; checkout/in | Hai MaDong riêng/giá snapshot; tổng món+ship−giảm khớp TT; tồn giảm đúng tiêu hao một lần, điểm đúng, bàn trống; lỗi in không checkout lại; ghi mã nguồn và query đối chiếu |
| T31-K02 | STORE nhập nhiều dòng trả một phần; ADMIN trả nợ2lần qua dialog, xem nguồn/report | Tồn tăng một lần; nợ=nhập−trả ngay−2lần trả; mỗi lần đúng phiếu chi/liên kết, không doublecount; runtime STORE không được trả nợ |
| T31-K03 | Xuất hủy; lấy snapshot kiểm kê; session2 phát sinh kho; chốt bị từ chối; lấy lại và chốt; gửi lần2 | Xuất đúng tồn thực dùng, stale snapshot không ghi điều chỉnh, chốt mới một lần; header/dòng/phiếu liên kết đúng và transaction0:0 |
| T31-K04 | Ca qua đêm8giờ; preview/chốt/chi toàn bộ; thử chốt/chi lại, thay hồ sơ sau chốt | Giờ/ngày thuộc kỳ đúng; snapshot lương bất biến; một phiếu chi, không cộng chốt như thực chi; report ngày chi đúng |
| T31-K05 | Tạo giao hàng/TT, chuyển từng bước đúng contract; reload và xem lại | Không đổi tài chính/TT/kho/điểm; chặn bước lùi/nhảy và phiên trái quyền; lưu trạng thái hiện tại bền vững, công khai không lưu timeline |
| T31-K06 | Với checkout/nhập/xuất/kiểm kê/trả nợ/chốt-chi lương: gây timeout/cancel trước ghi bằng khóa từ session2; mất response sau commit bằng harness đóng kết nối đúng điểm có bằng chứng | Trước ghi không dữ liệu dở; sau commit chỉ đối chiếu đúng MaHD/RequestId/mã nguồn, không retry ghi mù; kiểm receipts/actor/payload, bảng nguồn/số dư và transaction. Không chứng minh được thời điểm thì BLOCKED; không gọi lỗi trước gửi là timeout sau commit |
| T31-K07 | Login/logout/relogin3role; session cũ sau khóa tài khoản/nghỉ việc/đổi role/stamp/password | BLL và handler thật chặn phiên cũ; callback muộn không mở màn/ghi kết quả vào phiên mới, password không ở session/log; role theo policy hiện hành |
| T31-K08 | POS nhiều món/tên Việt; click/Tab lần đầu select-all, click lần2 caret, editor grid/IME; Enter/Esc/F2/F3/F4/F8, dirty/busy/unknown; chậm query và đổi filter/phiên; resize | Chỉ modal trên cùng nhận input, owner native/ShowInTaskbar=false, không Enter chốt tiền; giữ input/focus khi lỗi; query muộn bị bỏ; UI responsive. Kiểm125%,1280×720/1366×768/client thực;100/150 NOT RUN, hình thức SKIPPED |
| T31-K09 | Sau từng E2E/nhánh lỗi, đọc nguồn SQL bằng operator và đối chiếu kỳ vọng tính tay | Tất cả tiền/tồn/điểm/nợ/lương/trạng thái/nguồn đúng, không chỉ UI; lưu từng nhóm expected/actual, không export bí mật |
| T31-K10 | Audit325ID và44UX-ID, hash/phạm vi ảnh hưởng | Mọi ca liên quan có evidence phù hợp hoặc lý do scope/quyết định ngoại lệ rõ; phần thiếu vẫn BLOCKED/NOT RUN. Không chốt PASS nếu còn ca bắt buộc chưa đủ |
| T31-K11 | Tổng hợp luồng chính, failures, bug/known limitations | Không còn lỗi trọng yếu sai tiền/kho/điểm/nợ/lương, mất dữ liệu, bypass quyền/crash; FAIL chưa sửa không chốt. Kết luận dựa trên evidence, không chỉ source audit |

Chuẩn bị số tiền mẫu bằng fixture có giá/định mức cố định; tính expected trước thao tác từ contract, lưu rõ giá trị trong guide/test-results khi tạo fixture. Không lấy actual SQL làm expected duy nhất. Hai session dùng barrier/lock có timeout và cleanup trong finally, không dừng SQL service hoặc ngắt mạng toàn máy.

## 4. Truy vết UX và bằng chứng

UX-K01–04→K07/K08;K05–18/K20–21→K01/K06/K08;K19/K23→K01/K08;K22→K05;K24–26→K02;K27–30→K03;K31–32→K02/K04;K33→K01;K34–36→K04;K37–38→K09/K10 và ca còn thiếu G07;K39–43→K06–K08;K44→T30/T32. ux-matrix ghi đủ từng ID riêng và link task sở hữu, không thay nội dung UX gốc.

Mỗi subcase có runID/source/schema/hash, actor/role, dữ liệu/mã, thao tác/lệnh/thời gian, expected/actual, kết quả, bằng chứng. Không log password/connection string/credential rows. test-results.md tổng hợp11ID, report nhóm tổng hợp29ID và backlog còn thiếu. Hình thức người dùng tự nghiệm thu, không dùng ảnh/Designer làm PASS.

Khi source cần thay do debug đã duyệt, tạo manifest phiên bản mới, xác định regression theo tác động và chạy lại ca liên quan; package T32 tạo lại tương ứng. Không thay lịch sử bằng kết quả lượt mới ít phạm vi hơn.

