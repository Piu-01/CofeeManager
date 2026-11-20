# T24 — Đợt bug G06 đã sửa và regression PASS

## G06-UI-20261109 — Tab Nhân sự còn chrome sáng và khởi tạo nặng

**Đã duyệt/sửa10/11/2026; regression chung100+12 kiểm tra PASS.**
[Kết quả và giới hạn](../../g05-kho-nguon-chi-cong-no/ui-tabs-regression-20261110.md).
Giữ đủ4nhãn; chỉ tạo body được chọn, không theme lại cả cây đã hiện.
Nhân sự ở cặp đo669→233ms, EmployeeLookups3→1; hình thức user kiểm.
Nội dung điều tra dưới giữ làm lịch sử. B01/B02 bên dưới
vẫn giữ trạng thái/hồ sơ lịch sử, không dùng PASS cũ thay hiện tượng mới.

- Ảnh người dùng09/11: Chấm công có nền thanh tab/khung sáng; click chuyển màn
  bị giật theo mô tả user. HumanResourcesControl.cs:22 dùng TabControl mặc định;
  AppTheme.DrawTab chỉ tô nhãn, không gọi đường chrome tối OrbitTabControl.
- HumanResourcesControl.cs:20–28 đọc context rồi tạo tối đa4trang con và áp
  AppTheme cho toàn cây trên UI thread. Đây là công việc eager đã xác nhận từ
  source; mức trễ, heartbeat và query count chưa đo. Base list có ready guard,
  chưa có bằng chứng tất cả trang HR tải lặp hoặc SQL chậm.
- Đề xuất dùng lại OrbitTabControl, đo rồi chuyển khởi tạo page theo nhu cầu
  và gom layout/theming trong scope; giữ đủ tab, filter/input, quyền, query
  cancellation/generation, loading/lỗi và refresh sau ghi. Rủi ro chính là
  thay vòng đời Load/VisibleChanged và độ mới dữ liệu. Không đổi lương/SQL/BLL.
- Duyệt một đợt chung với Kho; xem [báo cáo/phạm vi/regression](../../g05-kho-nguon-chi-cong-no/ui-tabs-investigation-20261109.md).

---

Ghi nhận ban đầu31/10/2026 (trước debug; kết quả sau sửa ở cuối file). Phát hiện trong đợt nghiệm thu đã duyệt. Khi ghi nhận chưa sửa source; xem [báo cáo nhóm](../report.md). Quyền Computer Use đã được cấp và thử lại; hạn chế input native là blocker công cụ riêng, không kết luận là bug sản phẩm.

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

## Đã duyệt, sửa và regression —31/10/2026

Người dùng duyệt cả G06-B01/B02 bằng “duyệt”. Đã sửa đúng3file: EmployeeRepository.Money truyền SqlDecimal giữ Precision38/Scale18; EmployeesControl/PayrollControl dùng format `#,##0' ₫'` và providervi-VN. Không đổi SQLcontract, BLLvalidation, theme hoặc bố cục. [Biên bản regression](../regression-b01-b02.md):78/78subcase và14unit PASS, Release0warning/0error. Hai bug đóng trong phạm vi đã tái hiện. Không coi các nativehandler chưa chạy là đã PASS; G06 vẫn Đang làm.
