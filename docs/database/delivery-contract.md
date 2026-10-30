# Giao hàng — migration014 / T18

sp_DeliveryList/Get chỉ đơn Giao hang, ADMIN/CASH, lọc mã/SDT/tiến độ/tài chính,
Skip50. Trạng thái giao và thanh toán độc lập; Đã giao không đồng nghĩa đã trả.

sp_ChuyenTrangThaiGiaoHang nhận actor/stamp/code/ExpectedRevision/ExpectedStatus/
TargetStatus/RequestId. Chỉ Cho giao→Dang giao→Da giao; Order lock/header giữ version,
SQL chỉ UPDATE TrangThaiGiao và receipt. Không đổi tiền/khách/đối tác/NgàyTT/kho/điểm.

Hủy giao chỉ qua hủy đơn mởT16; paid không Huy giao/hoàn tiền. Giao thất bại paid
giữ trạng thái thật và hướng dẫn quản lý xử lý thủ công, không log sự kiện giả.
Chỉ lưu trạng thái hiện tại, không có timeline; T31-K05 áp dụng giới hạn này.

Timeout đối chiếu request fence/receipt/header trước thao tác mới. Không replay
request cũ với status/version mới. Runtime không được DML trực tiếp.
