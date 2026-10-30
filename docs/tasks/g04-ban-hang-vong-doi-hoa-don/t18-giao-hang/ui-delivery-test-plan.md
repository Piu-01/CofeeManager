# Đợt test bổ sung UI Giao hàng

Trạng thái: USER ĐÃ DUYỆT đợt test bằng “đuyệt” ngày26/10/2026. Ma trận DG01–DG08 và
tham chiếu T18-K01–K08 tại [plan](ui-delivery-plan.md), giữ expected/checklist gốc.

1. Cố định source142file và DebugDLL theo manifest triển khai; không sửa source/
   migration trong test. Harness/tooling lưu riêng, rawFAIL giữ nguyên.
2. DG01/DG05: bounds tại125%, nhiều/ít/rỗng, cột mã/SDT/địa chỉ, readonly, status
   độc lập, loading/error/no selection và select-all. Người dùng kiểm hình thức,
   agent không ảnh đối chiếu;100/150% NOT RUN nếu chưa chạy.
3. DG02/DG03/DG04: runtime Auth+SQL thật, tìm mã/SDT0đầu, kính lúp/Enter, draft
   chưa submit, hai filter tự áp dụng, refresh/paging, phản hồi muộn/cancel/error;
   dữ liệu0/1/49/50/51/100/101, tổng/page/Next/fallback đúng.
4. DG06/DG07: native detail/returnPOS, chuyển hai bước chỉ khi xác nhận; pending
   identity/reconcile không đổi khi lọc và không có dòng; stale/two sessions theo
   contract, không tài chính bị đổi hoặc gửi chuyển giao hai lần.
5. DG08:ADMIN/CASH được phép, STORE chặn; regression History/Customers/Menu/
   Tables/profile grid và action hook mặc định không ảnh hưởng module khác.
6. Dùng DBtest chung có marker hiện hữu, đọc lại ownership/state trước test:
   CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout, marker
   20261025_181739_98e5df59, schema016. Không tạo DBthứ4/fixture development.
   Tạo đơn giao fixture riêng bằng contract/đường chuẩn để kiểm chuyển bước;
   không chốt476–478 hoặc dùng nhầm đơn người dùng đã giữ. Snapshot tiền/kho/
   điểm/dòng/ưu đãi/thanh toán trước-sau đọc và chuyển tiến độ phải bất biến.
7. Lưu hash/môi trường/results và dọn allowlist tài nguyên mới dù FAIL. Không in
   máy thật, checkout hoặc hoàn tiền. Nếu cần thêm fault/fixture, thực hiện trong
   DBtest thuộc ownership, không đổi contract/expected để né lỗi.
8. Tổng hợp report nhóm, bug mới ghi T18/bug-report.md và chờ duyệt đợt bug trước
   sửa/regression. Không coi mọi subcase PASS từ build/smoke hoặc service đọc.
