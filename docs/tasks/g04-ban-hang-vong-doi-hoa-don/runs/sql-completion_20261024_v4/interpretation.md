# Kết quả SQL còn thiếu —24/10/2026

Người dùng yêu cầu agent kiểm tiếp phần còn tự kiểm được; không có gate duyệt mới.
Ba ca T14-K02/K09/K11 và T15-K15 trước đó gắn BLOCKED do chưa đủ lượt chạy là phân
loại chưa chính xác; không phải môi trường SQL chặn. Đã chạy DBtest thật,source016
cố định,không sửa sản phẩm hoặc dữ liệu development.

## Bằng chứng hiện tại

- T14-K02: manifest chính giữ FAIL assertion ban đầu. actor-followup.json quan sát
  đủ4mode missing actor/stamp cũ/inactive/resigned:51505,không header/receipt,snapshot
  tài chính nguyên,TranCount0/CapturedState0. InlineState1 trong truy vấn tổng hợp
  là lỗi instrumentation của assertion,không phải transaction còn mở. FK bàn/khách
  sai52105,NULLkhách và MaNV lấy từactor đều kiểm. Không thay FAIL cũ bằng PASS giả.
- T14-K09:2session thật,hai thứ tự phát lệnh0/52104. Chính connection thua gọi
  TableSelectCurrent,nhận MaHD thắng,rồi SalesOrderGet đúng header/1line. Không món
  của request thua bị chèn sang đơn thắng; cả hai session0:0.
- T14-K11:fault khi giảm giá vừa cập nhật52992 rollback toàn bộ state;timeout trước
  commit-2 không có header/receipt;client đóng connection sau quan sát commit và
  timeout sau commit đều đọc lại đúng1receipt/1header/1line. Replay cùng request
  không đổi state;khácqty/actor51608 không ghi. Các fault header/line/receipt đã có
  bằng chứng v1 được kếthừa,không cần cố tình chạy lại để xóa lịch sử.
- T15-K15:baseline+seed→001–016 giữ nguyên snapshot tiền,kho,điểm/history;LegacyUnknown
  không bịa nguồn/tỷ lệ. Seed lại51200,so mọi bảng không đổi. Dữ liệu legacy9 có
  người nhận trắng bị01052107;legacy10partial payment bị01152207,không ghi version,
  toàn rows giữ nguyên,không tự sửa invalid để migration qua. Cả hai DBinvalid được
  dọn nguyên trạng. Rerun/checksum016 kếthừa v3.
- Policy/recipe:ordering-manifest giữ4FAIL không quan sát được barrier context_info,
  không có kết quả hai session đủ điều kiện để chứng nhận. ordering2-manifest dùng
  applock thực sựGRANT,có barrierObserved cho4ca và cả8session0:0. Policy trước:
  0/52201,đơn còn mở/không money/consume/receipt rồi caller yêu cầu preview mới mới
  chốt. Checkout trướcpolicy:0/0,snapshot policy cũ giữ nguyên. Recipe trước/sau:
  0/0,tiêu hao/tồn đúng NewQty/OldQty theo thứ tựcommit,không mất điểm/ghi lặp.

Các follow-up là hoàn thiện instrumentation/điều phối của bộ test trong phạm vi
đã duyệt,giữ nguyên lượt cũ,không sửa test đang chạy. Source và các script đóng băng
được kiểm hash trước/sau. Không gọi lịch sử FAIL là lỗi sản phẩm đã được sửa.

Checklist hiện32PASS,0FAIL,24BLOCKED. Chỉ24ca có phần GUI chưa đủ bằng chứng; không
còn4ca SQL này gắn BLOCKED. Không tự nghiệm thu GUI hoặc hình thức. Dev/backup giữ
nguyên,6DBtest của lượt này đều dọn; không xóa backup hoặc tạo login/Windowsuser.
