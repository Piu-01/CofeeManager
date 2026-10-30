# G05 — Đợt test đã duyệt ngày27/10/2026

Kế hoạch gốc26/10/2026, được duyệt27/10. Đã chạy lượt test source cố định; kết quả38PASS/5FAIL/10BLOCKED/2NOTRUN và các nhánh UX chưa đủ tại [report](report.md). 29/10: đã duyệt sửa B01–B04 kèm regression; bốn bug PASS theo phạm vi, source cố định; B05 đã duyệt/sửa và kiểm theo phạm vi; B06 đã duyệt/sửa và regression chính PASS; còn nhánh nghiệm thu chưa đủ. Bảng55ID27/10 là lịch sử, chưa phát hành tổng nghiệm thu mới. Build/compile/smoke không thay55ID. G04 giữ40PASS/0FAIL/16BLOCKED, không xóa ca thiếu.

## 1. Phạm vi và thứ tự

| Task | ID gốc | Phần cần kiểm |
|---|---|---|
| T19 | K01–K10 (10) | Thu/chi thường, nguồn PN/BL/nợ, bất biến, quyền, tổng theo ngày, không nhân doanh thu checkout |
| T20 | K01–K12 (12) | Nháp, nhập nhiều dòng, trả0/một phần/đủ, nợ/chi nguồn, replay, rollback, đồng thời, backfill |
| T21 | K01–K10 (10) | Hủy/trả, thiếu kho rollback toàn phiếu, tranh tồn checkout, chốt một lần, không tự cấn nợ |
| T22 | K01–K11 (11) | Snapshot, chưa đếmNULL khác0, lý do, header/CT bất biến, ABA, ngày, hai session và checkout |
| T23 | K01–K12 (12) | Nợ nguồn, nhiều lần trả, ngày/phương thức/precision, cutoff, số dư cạnh tranh, timeout, quyền và lịch sử |

Expected gốc giữ nguyên trong từngplan/test-results.md. Đọc fixture và truy vết chi tiết tại plans.md và năm plan. UX-K24–K32, UX-K39–K42 bổ sung, không thay55ID.

## 2. Source/môi trường và dữ liệu

- Trước chạy ghi hash src/database/tests/tools, SDK, Windows, SQL instance/schema001–021 và DLL/exe thực. Giữ source/migration cố định trong từng lượt; build mới khi source thay, không dùng DLL cũ. Lỗi phát hiện ghi bug tại task, không sửa trong lượt test.
- SQL Server thật localhost, DB test tên CoffeeManager_Test_G05_<runId>_<mode>, G05TestMarker đúngrunId. Không ghi fixture vào CoffeeManager_Development, DB sử dụng hay DB manualG04. Runtime principal CafeManagerApp kiểm riêng; smoke trước đây chạy connection có quyền owner nên không chứng minh quyềnSQLruntime.
- Clean: baseline khôngseed+migrations001–021; demo: baseline+seed một lần rồimigrations. LegacyValid/Invalid trong clone riêng: mapping sai, nguồn trùng, ngày nguồn sai, chứng từ rỗng, snapshot cũ. Invalid phải dừng/rollback và không ghi version; không sửa dữ liệu để né lỗi.
- Fixture: PN10000/trả4000/nợ6000; tiền100001 để kiểm chia trả/làm tròn; NVL đơn vị chuẩn với4số lẻ; tồn cuối dùng chung checkout/xuất; countNULL/0/lệch và thay đổi ABA; nợ nhiều ngày/23:59:59/cutoff/ngàymax; MaBL fixture chỉ chứng minhFK/unique, tích hợpT26NOTRUN.
- Tài khoản ADMIN/CASH/STORE chỉ kích hoạt/chuẩn bị mật khẩu riêng trong DBtest khi chạy, không log bí mật. Giữ manifest principal/config/process/file tạo mới; mặc định dọn sau evidence kể cảFAIL. Chỉ giữDB choGUI nếu có mục tiêu/hạn giữ/cách dọn rõ; cleanup kiểm tên vàmarker, không wildcardDROP.

## 3. Thực hiện

1. XUnit normalization/quantity/role/state trong Finance/Inventory và regression liên quan. Build project test đã compile, chưa chạy; không test getter hoặc mirrorCRUD.
2. tools/database/Test-G05.ps1 chạy subset xUnit và subcase SQL fixture.sql/t19–t23. G05AcceptanceLog có hậu tố.SQL; không tự đánhPASS cảID cónative/biên khác. Runner đã soạn/kiểm cú pháp, chưa thực thi.
3. Bổ sung negative/fault/runtime principal đúng case: input0/âm/quáprecision/độ dài/FK, rawDML bịchặn, STORE trả ngaykhác0, CASH vượtquyền, TC011thiếu/sailoại. Fault sauheader/chitiết/chi/nợ/trướcreceipt; đối chiếu header/CT/kho/chi/nợ/receipt/điểm và@@TRANCOUNT/XACT_STATE.
4. Hai session với barrier và log quan sát được: nhập chungNVL; xuất/checkout tồn cuối cả hai thứ tự; haiKK/cùngKK/checkout; hai trả nợ cuối và hai nợ độc lập. Không coi tác vụ chạy gần nhau là đã chứng minh cạnh tranh. Kiểm kết quả đủ tồn/mộtcommit/khôngâm/khôngmất biến động.
5. Native WinForms: nhập/bỏnháp/chốt/reload/source links; click/Tab/grid editor chọn hết nội dung khi mớifocus, click tiếp đặtcaret; ownerMain/dialoglồng/nativeprintfile đúng, X/Esc/busy/unknown/session, Enter không tựchốt và không xuyên nền. Component/reflection ghi riêng, không thay chuột/phím thật.
6. Timeout trước/saucommit và crash/restart: pendingDPAPI đúngactor/request, receiptchỉđúngoperation, replay cùngpayload không ghi hai lần, khácpayload bịchặn. Mộtactor nhiềuprocess không ghiđè pending. Lỗi lưu/đọc/xóa pending phải giữ khả năng đối chiếu, không crash hoặc mất mã.
7. Chức năng/layout kỹ thuật125%: full/cửa sổ/thu nhỏ/chia đôi, filter/grid/footer còn tiếp cận được; ghi desktop/client/DeviceDpi thực. Không đổi WindowsScale;100/150%NOTRUN. Hình thức/hover/spacing/ảnh đối chiếu/Designer visual SKIPPED theo phân công22/10, không blocker chức năng.
8. Hồi quy checkout với kho và tổng tiền; quyền/menu/STOREHome/source links; không thêm phiếuThu cho checkout, không cộng chi trả nợ hai lần. Không mở rộng thành nghiệm thu16caG04 hoặc moduleT26.

## 4. Kết quả và điều kiện dừng

Mỗi task lưu đủID/expected/actual/data/lệnh/source/schema/evidence trong test-results.md. Native/asof/parallel/fault chưa chạy vẫnNOTRUN hoặcBLOCKED với lý do thật; không suy từ subset runner. Báo cáo chung report.md kể cảFAIL. Bug gom cảđợt để duyệt trước debug; bug mới cần duyệt bổ sung.

Gate này đã đạt27/10: người dùng duyệt đợt test và targetlocalhost/CoffeeManager_Development. Development đã nâng021, acceptance dùng DB riêng; B01–B04 đã được duyệt debug29/10, B05 đã duyệt/sửa; B06 đã duyệt/sửa; native keyboard/UX còn thiếu. Các câu “chưa chạy” trong danh sách kế hoạch phía trên là trạng thái lúc soạn26/10; kết quả thực tế theo report và từng test-results.md.



## Cập nhật30/10/2026 — Native follow-up và B07

STORE nhập21000/trả ngay0, xem nợ chỉ đọc và xuất trả2 đơn vị PASS; ADMIN trả10000+11000, lịch sử và hai link phiếu chi đúng nguồn PASS. Hai subcase runtime T19-K02.Name51/T20-K02.ActorMissing PASS, rollback0:0/dữ liệu không đổi. Đối chiếu tiền/kho/nợ cuối PASS. Khi đăng xuất Công nợ phát sinh B07 CTS đã dispose, chờ duyệt bổ sung trước sửa; CASH native tiếp theo BLOCKED, native keyboard chưa đủ. Chi tiết [biên bản](runs/native-followup-20261029/test-results.md), [bug B07](t23-cong-no/bug-report.md). Source cố định; đã dọn1/1DB và credentials, development không bị ghi fixture. Không đổi55ID lịch sử hay chốt nghiệm thu toàn nhóm.

## B07 — Đã duyệt, sửa và regression30/10/2026

Người dùng duyệt bổ sung B07; đã sửa vòng đời CTS trong đúng năm control G05, không đổi SQL/quyền/ghi nghiệp vụ. Debug/Release0warning/0error,22/22unit,47/47lifecycle và5/5CASH-BLL PASS. Native chuyển năm màn, nháp modal, đóng lịch sử nợ→logout, CASH login/logout, ADMIN login lại/đóng MainForm PASS theo phạm vi. Counts/receipt/tồn không đổi; source cố định;1DB test dọn1/còn0, credentials đã xóa. [Biên bản B07](runs/b07-20261030/test-results.md), [bug](t23-cong-no/bug-report.md). Nội dung “B07 chờ duyệt” ở cập nhật trước là lịch sử trước phê duyệt. CASH native không còn bị chặn bởi B07; native keyboard vẫn chưa đủ do công cụ. Không đổi55ID lịch sử, G05 Đang làm do các nhánh nghiệm thu còn thiếu.
