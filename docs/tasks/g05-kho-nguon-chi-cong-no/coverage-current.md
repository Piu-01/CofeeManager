# G05 — Đối chiếu nghiệm thu hiện tại30/10/2026

Đây là tổng hợp bằng chứng hiện có sau B07, không phải một lượt chạy mới toàn55ID. Giữ nguyên biên bản27/10 (38PASS/5FAIL/10BLOCKED/2NOTRUN). Kế thừa38PASS không bị đổi contract/SQL; cộng6ID đã bổ sung đủ phần thiếu thành **44/55 đủ bằng chứng PASS,11/55 chưa đủ để chốt**. Không cộng số assertion/unit/subcase thành số ID nghiệm thu. B01–B07 đã sửa/regression theo phạm vi, không còn bug đã ghi nhận đang chờ duyệt sửa; chưa có nghĩa cả G05 hoàn tất.

| Task | PASS hiện tại | Chưa đủ | ID đủ bằng chứng PASS |
|---|---:|---:|---|
|T19 Thu chi|7/10|3|K01,K02,K03,K04,K06,K08,K09|
|T20 Nhập kho|10/12|2|K02,K03,K04,K05,K06,K07,K08,K09,K10,K11|
|T21 Xuất kho|8/10|2|K02,K03,K04,K05,K06,K07,K09,K10|
|T22 Kiểm kê|7/11|4|K02,K04,K05,K06,K07,K08,K09|
|T23 Công nợ|12/12|0|K01–K12|
|Tổng|44/55|11|G05 Đang làm|

## Sáu ID bổ sung đủ phần thiếu so với27/10

| ID | Bằng chứng bổ sung |
|---|---|
|T19-K02|Các nhánh tiền/ghi chú/FK đã PASS27/10; Name51 runtime51501, dữ liệu không đổi/transaction0:0 trong [native follow-up](runs/native-followup-20261029/test-results.md).|
|T20-K02|Các nhánh NCC/qty/giá/NVL đã PASS27/10; actor không tồn tại runtime51505/dữ liệu không đổi/transaction0:0 trong cùng follow-up.|
|T20-K11|[B06](runs/b06-20261029/test-results.md) ADMIN trả4000 trên tổng20000; follow-up STORE nhập21000/trả0/nợ21000; SQL/BLL cấm STORE trả tiền/CASH, native CASH không có menu Kho và5BLL đều51605 trong [B07](runs/b07-20261030/test-results.md).|
|T21-K10|B06 ADMIN chốt xuất1/reload; follow-up STORE xuất trả2/reload, SQL đối chiếu tồn và chứng từ đúng; quyền SQL cũ và CASH menu/BLL B07 bị chặn.|
|T23-K11|Quyền/timeout đối chiếu receipt đã PASS27/10; follow-up ADMIN trả10000+11000, STORE chỉ xem; CASH menu/BLL và logout/relogin B07 PASS. Không suy PASS mọi ca crash/restart/unknown UI từ ID này.|
|T23-K12|Follow-up reload lịch sử hai dòng10000/11000, đúng phương thức và link từng TC20250505/TC20250506 đúng SupplierDebt/CN20250405, chỉ đọc.|

## Mười một ID chưa đủ để chốt

| ID | Phần còn phải kiểm |
|---|---|
|T19-K05|Dropdown ghi tay không cho chọn các danh mục nguồn hệ thống; kiểm vai trò trên UI. SQL/BLL đã có bằng chứng.|
|T19-K07|Mở chi tiết phiếu ghi tay và nguồn hệ thống, kiểm readonly/không sửa xóa theo policy. Link phiếu chi nợ đã kiểm, chưa phủ đủ chi tiết phiếu ghi tay.|
|T19-K10|Tra cứu/bộ lọc với dữ liệu có dòng, đủ loại nguồn, tiền/ghi chú/mã và link nguồn/reload. Filter rỗng/component pager không thay ca này.|
|T20-K01|Native nhiều dòng → preview/chốt → đối chiếu tồn/tổng; nháp đóng không ghi đã PASS, SQL nhiều dòng/làm tròn đã PASS. Plan yêu cầu luồng native nhiều dòng, hiện mới chốt native một dòng.|
|T20-K12|Native nhiều dòng preview khớp SQL, sau reload mở nguồn chi/nợ đầy đủ. Preview một dòng và nợ nguồn đã kiểm, chưa phủ cả nhánh theo plan.|
|T21-K01|Native xuất hủy/xuất trả nhiều nguyên liệu, đối chiếu từng tồn/receipt. Hai loại đã chốt native một dòng, nháp không ghi đã kiểm; chưa đủ nhiều dòng theo plan.|
|T21-K08|Native hủy đơn mở đã pha → xuất hủy lượng thực dùng; đối chiếu không TIEUHAO_HOADON và chỉ trừ theo PX. SQL đã PASS, chưa chạy đầy đủ luồng UI này.|
|T22-K01|Native tạo snapshot, hiển thị đúng tồn và chưa chốt chưa đổi kho. Mới kiểm danh sách/legacy rỗng chỉ đọc.|
|T22-K03|Native sửa số đếm lệch, thiếu lý do bị chặn và giữ input; bổ sung lý do hợp lệ. SQL đã PASS.|
|T22-K10|Trong lúc người dùng đếm, dùng session thứ hai xác nhận app không giữ khóa; kiểm lời nhắc tạm dừng phát sinh trước chốt.|
|T22-K11|ADMIN/STORE đi hết tạo/đếm/lưu/chốt/reload trạng thái/chênh lệch; CASH bị chặn. NULL mapper đã sửa và quyền SQL/BLL đã PASS, chưa nghiệm thu trọn luồng native.|

## Điều kiện ngoài bảng55ID cần đối chiếu trước Done

- Các nhánh UX trong plan chưa đủ: bàn phím/Tab/select-all native (đang vướng kích hoạt owner/cache công cụ), gửi lặp/busy/unknown và phục hồi pending qua crash/restart/đa process/I/O, owner của hộp native in/chọn file nếu luồng có dùng. SQL timeout/replay và component có phần PASS; không gộp thành PASS mọi nhánh native.
- Hình thức SKIPPED theo phân công người dùng tự kiểm, không là blocker chức năng. DPI125% đã kiểm theo các lượt regression;100/150% NOT RUN theo giới hạn được chốt, không tự đổi Windows Scale hoặc suy PASS.
- MaBL fixture T19 không thay tích hợp chi lương T26 tương lai; không đòi triển khai T26 trước khi chốt G05.
- Muốn Done G05: bổ sung bằng chứng11ID và các nhánh UX bắt buộc còn thiếu, xử lý bug mới nếu có, rồi phát hành báo cáo nghiệm thu cuối. Không cần chạy lại38ca nền chỉ vì đổi cách tổng hợp; regression theo ảnh hưởng thực tế.
