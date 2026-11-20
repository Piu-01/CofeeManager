# G07 — Đề nghị đợt nghiệm thu sau triển khai

Trạng thái: **Người dùng duyệt đợt test G07 bằng “duyệt” ngày31/10/2026**. Đã chạy các lượt source cố định; kết quả24PASS/0FAIL/9BLOCKED tại [report.md](report.md). Duyệt bộ plan không thay duyệt nghiệm thu. Kiểm kỹ thuật và đo chọn tối ưu trong pha triển khai lưu riêng, không tick33ID từ các lượt đó.

## Phạm vi và thứ tự

1. T27-K01–K09: CRUD/biên/NULL/revision, chọn và mở MaTS hai chiều với thu chi; không tự ghi chi khi lưu tài sản; khấu hao tháng mua/cuối kỳ/tiền lẻ/intmax; role/BLL/SQL, đồng thời sửa-xóa-ghi chi, pending DB/actor/restart/I/O, timeout trước/saucommit, replay/payloadkhác.
2. T28-K01–K13: bảng kỳ vọng tính tay cho ngày trả khác ngày lập,2món×2TT, snapshot/ship/giảm/phí; cutoffnợ; nguồn thu/chi/lương khôngdoublecount; mởsổ→nhập→xuất→bán→KK; coveragelegacy; filter/page/empty/error/latequery/drilldown; role/session/dashboard.
3. T29-K01–K11: auditdataset10.000bill, đọc artifactactualplan/IO/TIME/rawtimings và bản cuối; parity/query/index/writecost; pagingTVP; hai session có barrier cho checkout/checkout,checkout/nhập,checkout/xuất; UIresponsive và số đo theo máy/dataset thực.

Ma trận từng subcase/dữ liệu/expected tại ba plan. Giữ9+13+11=33ID và325ID/T01–T32. UX-K38 thuộcT27, UX-K37 thuộcT28; UX-K39–K42 liên kết keyboard/layout kỹ thuật/input/async của từng màn.

## Môi trường và source cố định

- SQL Server localhost thật, DBtest riêng clean/demo/upgrade phiên bản trướcG07, quyền runtime CafeManagerApp và actorADMIN/CASH/STORE; không dùng SQLite/DBdevelopment làm fixture.
- Ghi SDK, buildDebug/Release phù hợp, sourceSHA256, schema/checksum, dataset, rawSQL/TRX/log trước chạy. Fixture/harness chốt trước lượt; không đổi source/migration/expected khi FAIL.
- Unit xUnit chỉ quy tắc có giá trị trong Assets/Reports; integration đối chiếu tiền/kho/điểm/nợ/lương/trạng thái/liên kết và trạng thái transaction trước/sau lỗi. Service/SQL không thay native handler.
- WinForms thật ở125%: full/cửa sổ/chiađôi;1280×720/1366×768 hoặc ghi kích thước khả dụng. Test binding/pager/validation/selectallclick-Tab-clicklần2,owner/modal/nháp/busy/unknown/Enter/Esc,querymuộn/logout/đóng/dispose. Tool không điều khiển được modal ghiBLOCKED; không suyPASS từ component.
- Người dùng kiểm hình thức, agent ghiSKIPPED; không chụp/so ảnh,hover/spacing/Designer.100/150% chưa chạyNOTRUN, không đổi WindowsScale.

## Kết quả và dọn tài nguyên

Mỗi task lưu test-results.md: mã/subcase,môi trường/schema/hash,input,lệnh/thao tác,expected/actual,PASS/FAIL/BLOCKED/NOTRUN,bằngchứng. Reportnhóm tổng hợp đủ33ID và giới hạn thật. Bug tại task sở hữu, duyệt cả đợt trướcdebug; không tự vá trong lượt. Giữ nguyên lịch sửG04–G06.

Manifest exactDBname/runID/marker/principal/config/path; owner chỉ provision/fixture/đốichiếu. Cleanupfinally theo allowlist+marker, lưu chứng cứ trước dọn kể cảFAIL; khôngDROPwildcard hoặcchạmDBdevelopment. Báo tạo/dọn/còn giữ/lýdo/cáchdọn. Chưa đủcaP​ASS thìG07 vẫnĐanglàm, khôngcommit/push hoặc nângdevelopment từ duyệt đợttest.

