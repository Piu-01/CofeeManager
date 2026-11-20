# T24 — Đợt bug G06 chờ duyệt

31/10/2026. Phát hiện trong đợt nghiệm thu đã duyệt. Chưa sửa source; xem [báo cáo nhóm](../report.md). Quyền Computer Use đã được cấp và thử lại; hạn chế input native là blocker công cụ riêng, không kết luận là bug sản phẩm.

## G06-B01 — SqlClient overflow với tiền hợp lệ

- Hiện tượng: `EmployeeService.SaveAsync` với tên50 ký tự, SDT15 chữ số, Full-time, lương tháng999999999999999999VND bị `System.OverflowException: Conversion overflows.` thay vì lưu đúng. Ca bổ sung gắn T24-K02; ảnh hưởng yêu cầu lưu lương T24-K01. Không chứng minh UI crash vì native input bị chặn.
- Bằng chứng: [supplement/results.json](../runs/extended-20261030/supplement/results.json), [diagnostic/results.json](../runs/extended-20261030/diagnostic/results.json), [mã tái hiện](../runs/extended-20261030/diagnostic/Program.cs). 12.000.000 với scale18 qua được; 99.999.999.999,999.999.999.999 và999.999.999.999.999.999 với scale18 lỗi; cùng số với scale0 và literalSQL nhận đúng. Stack dừng ở `SqlDecimal.ToDecimal → TdsParser.AdjustDecimalScale` khi serialize parameter, trước khi SQL xử lý giá trị đó.
- Nguyên nhân đã chứng minh: `EmployeeRepository.Money` dòng12 đặt Precision38/Scale18 nhưng Value là CLR decimal. Việc tăng scale vượt khả năng biểu diễn CLR decimal với các giá trị lớn dù SQLdecimal(18,0) vẫn chứa được. PayrollRepository tái sử dụng helper cho giờ và tiền; đây là phạm vi ảnh hưởng theo source, chưa tái hiện toàn bộ trường payroll ở biên.
- Hướng sửa đề nghị: sửa helper truyền số để giữ giá trị bằng SqlDecimal, tránh bước tăng scale qua CLR decimal; hoặc tách parameter tiền/giờ theo scale hợp đồng sau validation. Giữ kiểm nguyên VND/giờ2 số lẻ và SQL từ chối đầu vào sai; không làm tròn ngầm, giảm giới hạn nghiệp vụ hoặc đổi SQLcontract để né lỗi. Chỉ sửa DAL và test liên quan cần thiết sau duyệt.
- Regression: tiền0/12triệu/11–18 chữ số/max, quámax, âm/lẻ; giờ0/0,01/precision sai; create/updateemployee, preview/chốt/chi và đối chiếu receipt/không ghi một phần. Giữ snapshot/phiếu chi một lần. Nguy cơ: helper chung có thể làm tròn số hoặc đổi payload nếu chọn scale sai; phải so typed values và SQLactual.

## G06-B02 — Grid hiển thị literal N ở tiền

- Hiện tượng chức năng định dạng: grid nhân viên có `N12000000 ₫`, `N35000 ₫`; expected `12.000.000 ₫`, `35.000 ₫`. Không phải nghiệm thu bố cục/hình thức thay người dùng.
- Bằng chứng: [native-trace.json](../runs/extended-20261030/native-trace.json) là accessibility text của app thật; [diagnostic/results.json](../runs/extended-20261030/diagnostic/results.json) tái hiện formatter bằng culturevi-VN. Không lưu ảnh đối chiếu thị giác.
- Nguyên nhân: EmployeesControl dòng32 và PayrollControl dòng26 dùng `N0' ₫'`. Khi ghép literal vào chuỗi định dạng này, .NET coi là customformat và in chữN; `N0` không còn là standard numeric format. Payroll có cùng chuỗi theo source, chưa native mở gridPayroll để kết luận quan sát riêng.
- Hướng sửa: dùng format/customformatter hợp lệ cho tiền với culturevi-VN, giữ typeddecimal và căn phải; tái sử dụng cách định dạng chung hiện có, không refactor theme/dialog.
- Regression: grid hồ sơ và payroll với0,35.000,12.000.000 và số lớn; so giá trị text, không thay đổi dữ liệu/selection/sort. Không test thị giác/spacing; hình thức vẫn do người dùng.

## Đợt duyệt đề nghị

Duyệt cùng G06-B01 và G06-B02 để sửa lần lượt rồi regression theo ảnh hưởng. Không đổi nghiệp vụ, SQLmigration hoặc nâng development trong đợt này. Bug mới ngoài hai mục phải báo bổ sung.
