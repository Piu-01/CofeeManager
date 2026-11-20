# T31 — Thao tác native sau duyệt đợt test

Đây là kịch bản tham chiếu. Kết quả đã chạy, phần FAIL/BLOCKED/NOT RUN nằm tại [test-results.md](test-results.md), không coi toàn bộ hướng dẫn là transcript thực hiện. Dùng WinForms thật trên DB test G08 có manifest/marker, không DB development/sử dụng. Ghi resolution/client bounds/DeviceDpi thực125%; không đổi Scale. Hình thức SKIPPED,100/150% NOT RUN. Không chụp/so ảnh hình thức/hover/spacing hoặc dùng Designer nghiệm thu.

## Chuẩn bị và ghi kết quả

1. Ghi source/package hash, SQL/SDK/runtime, DB/schema/checksum, runID, role và ngày server. Xác minh phiên điều khiển native click/type/Tab đọc được giá trị. Nếu tool báo geometry/cache/input không xác định, lưu đúng lỗi tool và BLOCKED phần liên quan, không quy thành bug sản phẩm hoặc suy từ probe.
2. Cài Clean/Demo riêng theo T30. Operator tạo account3role bằng UI với mật khẩu riêng; không lưu password/hash trong transcript. Fixture public procedure/SP00125000/công thứcNVL01=.018,NVL03=.010, mở sổ đủ kho, khách có hạng0hoặc ưu đãi đang hiệu lực. Nếu thay ngày/chính sách, ghi rõ input và expected trước thao tác.
3. Mỗi checkpoint: ID/subcase, dữ liệu/mã, thao tác cụ thể/phím/cửa sổ owner, expected, actual quan sát được, SQL đối chiếu, trạng thái. Chưa đủ subcase không nâng cảID. Dữ liệu snapshot dùng StateSnapshot/OperationsTools, chỉ export hash/tổng không thông tin riêng.

## N01 — T31-K01/K09; UX-K05–19/K23

CASH login → bàn trống (ghi mã) → thêm SP001ghi chúÍt đường, thêm cùng món ghi chúKhông đá. Expected2MaDong, giá25000/dòng. Chọn khách/hạng và KM hợp lệ: đọc chính sách trước, tính expected50000−giảm+ship, không cộng cả hạng vàKM. Ghi điểm trước, công thức trước checkout.

F8 mở modal thanh toán có owner MainForm, native titlebar/X và không taskbar riêng. Preview đang tải/lỗi không cho chốt số cũ. Chia2TT tổng đúng; thử thiếu/dư1đồng phải lỗi gần phân bổ. Thử tiền mặt đưa100000/phần tiền mặt81000 theo fixture81000thì thối19000, không ghi tiền đưa vào TT. Enter/F8/doubleclick không tự gửi lần2. Bấm xác nhận đúng một lần; in/preview sau commit. Lỗi máy in chỉIn lại, không phát sinh TT/tiêu hao/điểm mới. Agent không tự gửi tới máy in thật khi không có thiết bị đích được xác định; preview không chứng nhận in giấy.

Đọc HOADON/CHITIET_HD/CHITIET_THANHTOAN/TIEUHAO_HOADON/LICHSU_DIEM và tồn/bàn, expected tiền/tiêuhao theo snapshot/điểm chính sách. Tra cứu ngày thanh toán hiện tại (khác seed04/2025), mở chi tiết/in lại rồi quay lại giữ filter/selection. Tạo ca thiếu1NVL: checkout rollback tất cả và đơn còn mở; bổ sung kho bằng nhập hợp lệ, không UPDATE tồn để sửa test.

## N02 — T31-K02/K09; UX-K24–26/K31–32

STORE mở nhập: thêm nguyên liệu trùng phải focus dòng cũ, đơn vị rõ. Đổi qty/ghi chú, Esc/Quay lại có hỏi nháp; bỏ không đổi tồn/nợ/chi. STORE nhập không trả ngay. ADMIN tạo nhậpNVL01:10×1000, trả ngay2000; tồn+10,nợ8000,một chi2000gắn nguồn. Trả3000tiền mặt rồi5000chuyển khoản: còn5000rồi0; một phiếu chi/một history mỗi lần. STORE không có quyền trả ở BLL dù mở được nợ chỉ đọc. Mở phiếu chi nguồn hệ thống chỉ đọc, không sửa nguồn/tạo thêm.

Kiểm receipt/request đúng actor/payload, timeout đối chiếu theoN06. Phiếu nợ/nhập/chi liên kết đúng mã; report thu/chi không doublecount nhập trả ngay/trả nợ. Không ép STORE làm phần trả ngay vì ADMIN sở hữu quyền tài chính.

## N03 — T31-K03/K09; UX-K27–30

Xuất hủyNVL01lượng1, lý do bắt buộc. Thử nhiều dòng có một NVL thiếu phải rollback toàn bộ. Kiểm kê chọn scope, ô đếm trống/khác hệ thống thiếu lý do phải chặn. Lấy snapshotA; phiên2xuất1 qua procedure; chốtA phải lỗi52903 và không có điều chỉnh. Lấy snapshotB/đếm lại, lý do hợp lệ, chốt1lần. Reload/nhấn lại không xuất điều chỉnh lần2, header không có đường reset. Đối chiếu kho=đầu+nhập−xuất−tiêuhao+KK, liên kết phiếu và transaction0:0.

## N04 — T31-K04/K09; UX-K34–36

ADMIN tạo nhân viên Part-time35000/h, phụ cấp/bonus/deduction0. Ngày làm22h→ngày sau06h=8giờ, hiển thị cả ngày ra. Thử overlap/thiếu giờra phải xử lý trước chốt. Preview280000, chốt rồi đổi hồ sơ không đổi snapshot; trạng tháiĐã chốt/Chưa chi khácĐã chi. Chi toàn bộ một lần, phiếu chi đúngNguồn; lặp không thêm chi. Chốt không bị report cộng như thựcchi. Thêm ca thực lĩnh0: Không phát sinh chi, không phiếu0.

## N05 — T31-K05/K09; UX-K21–22

Tạo giao thiếu tên/SDT/địa chỉ phải lỗi trường, giữ input. Hợp lệ SP00125000+ship5000, không ưuđãi→30000. TT rồi Chờ giao→Đang giao→Đã giao; thử lùi/nhảy bị chặn. Reload/trở lại history đọc hiện trạng. Snapshot header tiền/TT/tiêuhao/kho/điểm trước/sau chuyển bằng nhau. Chỉ lưu hiện trạng theo T18; ghi rõ không timeline bềnvững và không giả ngày chuyển.

## N06 — T31-K06/K09; UX-K18/K31/K42

Áp cho checkout/nhập/xuất/kiểm kê/trả nợ/chốt-chi lương. Trước gửi chụp snapshot hash và giữ RequestId/mã nguồn trong pending của chính thao tác. Session2SQL trên đúng DBtest mở transaction giữ khóa hàng nguồn liên quan trong thời gian giới hạn; app gửi rồi timeout/hủy, session2rollback. Đối chiếu cả nguồn/receipt và transaction, không dữ liệu dở; không coi lỗi trước gửi là timeout saucommit.

Ca saucommit phải có bằng chứng receipt/nguồn đã commit và app chưa nhận response: điều khiển kết nối test của chính lượt/fault harness được xác định, không tắt SQLservice/ngắt mạng toàn máy, không thêm fault hook vào sản phẩm. Nếu không xác định được điểm response, **BLOCKED**; không suy từ replay thường. Khi unknown, UI chỉ đốichiếu, chưa cho ghi lại mù; sau đốichiếuRecorded chỉ tải chứng từ/in lại. NotRecorded chỉ sau cùngrequestlock và khôngreceipt. Payload/actor khác không được nhậnreceipt. Ghi kết quả từng usecase/trước/sau riêng, không mộtca đại diện tất cả.

## N07 — T31-K07/K08; UX-K01–04/K39–43

Ba role login thật; CASH POS,STORE tồn/nợ chỉđọc,ADMIN tài chính/report. Sai password giữ username và không session; logoutđang mởdialog/async không còn callback ghi vào phiên sau. Operator khóa account/nghỉviệc/đổirole/đổipassword trên fixture theo từng ca, phiên cũ bị chặn và trở về Login theo policy; giao dịch SQL đang xửlý phải đốichiếu, không giả rollback từ logout.

Mọi input sửađược: click/Tab mới vào chọnhết; clicktiếp cùngfocus đặtcaret; rời/vào lại chọnhết; editorgrid táisửdụng vàIME không đổi giátrị/ghiDB từfocus. Dialogmodal Windows đúng owner/lồng, chỉ hộp trên nhậninput,Esc/X qua guarddirty/busy/unknown,Enter không chốt tiền/kho/nợ/lương. Cha giữfilter/selection/focus vàdisposedđúng.

Delayquery report/lookup rồi đổifilter/đóngdialog/đổiphiên: UI responsive, callbackcũkhôngghiđè/controlđãdispose; rỗng khác lỗi, lỗiDBkhôngtổng0giả. Chạy1280×720/1366×768/clientkhảdụng tại125%, action/footer còntruycậpđược, cócuộn khi hẹp. Đây là layoutkỹthuật, không nghiệmthuhìnhthức. Đóng/mởappcóđơnmở phải loadDB, khôngautohủy/thu.

## N08 — Ca còn thiếu và chốt bao phủ

Theo acceptance-matrix.md và ux-matrix.md, đọc lại subcase của nhóm sở hữu. G07còn9ID:T27-K01/K04/K07/K09,T28-K06/K11/K12/K13,T29-K10; G04/G05/G06 đọc report/test-results hiệnhành, không lấy sốtổngcũ làmkếtquảmới. Native tài sản→chi→nguồn,report lỗi/đổifilter/querymuộn/quyền/nhãnthamkhảo,timingUI cần thao tácthật. Lưu bằngchứng ởtask sởhữu vàlinkG08. T31-K10/K11 chưaPASS nếu còncabắtbuộcchưađủ hoặc lỗi trọngyếu; ngoại lệ cũ giữnguyên và côngkhai.

