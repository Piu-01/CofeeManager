# Contract khách hàng — migration007 / T12

ADMIN CRUD/mởsổ; CASH tìm/đọc/thêm; STORE không truy cập. CustomerRequest chỉ Name/
Phone, điểm/hạng không có trong profile editor. Name≤50, Phone ASCII digits1–15,
trống→NULL, giữ0đầu; unique filtered SDT. Mã mới do SeqKhachHang sinh KH001–999;
receipt giữ mã qua retry, không MAX+1 ở C#. Hết sequence báo lỗi, không cycle.

Clean chưa có hạng tạo DONG ngưỡng0/giảm0; có policy mà thiếu0 hoặc mapping legacy
sai thì preflight dừng. Create point0, SQL chọn hạng ngưỡng cao nhất≤điểm.
Khách legacy CustomerOpening snapshot cutover bất biến bằng số hiện tại, không
INSERT lịch sử cộng lại điểm. Khách mới ADMIN mởsổ trước lịch sử/hóađơn một lần;
set điểm/hạng+mốc+receipt nguyên tử. Không mởlại; T13 điều chỉnh qua lịch sử.

sp_CustomerList/Get/Create/Update/Delete/Open/Reconcile, sp_CustomerOrders trả
chỉ mã/ngày/trạng thái và paging50; tra cứu đủ T17. DTO Code/Name/Phone/Points/
Tier/Discount/Revision/ReadAt. Selector chọn MaKH, vãng lai trảNULL; tích hợpPOS T16.
Tạo/open Shared applock LoyaltyPolicy trước rowkhách; T13 Replace Exclusive.

Technical clean/demo/createpoint0/list: G03/runs/technical_20261018_193613_e28171d4;
tạo2/dọn2/giữ0DB. Build0warning/error. Nghiệm thu nghiệp vụ/role/concurrency/GUI chưa chạy.
