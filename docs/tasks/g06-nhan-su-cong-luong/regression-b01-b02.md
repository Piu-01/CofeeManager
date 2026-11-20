# G06 — Regression G06-B01/B02

31/10/2026. Người dùng duyệt sửa cả đợt và regression bằng “duyệt”. Áp dụng karpathy-guidelines, bug-investigation-workflow và post-change-test-workflow. Không xin duyệt lại test nằm trong đợt debug. Đã sửa xong hai bug; G06 chưa Hoàn tất vì native còn thiếu.

## Thay đổi đúng phạm vi

- `src/CafeManager.DataAccess/Repositories/EmployeeRepository.cs`: parameter giữ SQLdecimal38/18 nhưng Value là SqlDecimal thay cho CLRdecimal, tránh overflow khi SqlClient tăng scale. NULL vẫn DBNull; không làm tròn và không đổi SQLcontract. Payroll tiếp tục dùng helper chung.
- `src/CafeManager.WinForms/Modules/HumanResources/EmployeesControl.cs`, `PayrollControl.cs`: dùng customformat `#,##0' ₫'` và CultureInfovi-VN như các grid hiện có. Không sửa giá trị typed/binding/sort hoặc bố cục.

[Delta hash giữa trước nghiệm thu và regression](runs/regression-b01-b02-followup-20261031/approved-source-delta.csv) có5file:3file trên do đợt này sửa; OrderPanel.cs và OrbitDataGridView.cs thay đổi ngoài đợt sửa G06, được giữ nguyên. Không nhận thay đổi POS/grid chung là đầu ra của fixG06, không reset thay đổi khác. Nguồn trong mỗi lượt SQL regression cố định theo source-before/after và manifest.

## Môi trường và lệnh

SDK10.0.401, SQL Server thật localhost; DBdemo mới, baseline+seed một lần/migrations001–024. RuntimeG06Runtime/CafeManagerApp qua EXECUTE AS USER và Pooling=false; owner chỉ fixture/bootstrap/đối chiếu. SQL/code/test sản phẩm giữ cố định khi chạy.

- Build Debug qua `dotnet build .../regression-b01-b02-followup-20261031/Acceptance.csproj`;0error,10nullablewarning thuộc harness kế thừa (không phải source sản phẩm). [Log](runs/regression-b01-b02-followup-20261031/build.log).
- `dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj -c Release --no-restore`:0warning/0error. [Log](runs/regression-b01-b02-20261031/release-build.log).
- `dotnet test tests/CafeManager.Tests -c Debug --no-restore --filter FullyQualifiedName~HumanResources`:14/14PASS. [TRX](runs/regression-b01-b02-20261031/g06-regression.trx), [log](runs/regression-b01-b02-20261031/unit.log).
- SQL/BLL/component: chạy `runs/regression-b01-b02-20261031/run.ps1`, rồi lượt bổ sung độc lập `runs/regression-b01-b02-followup-20261031/run.ps1`. Harness/project/script/lệnh và input/expected/actual được giữ cạnh kết quả; không chỉnh source sản phẩm giữa hai lượt.

## Kết quả thực chạy

[Lượt đầu](runs/regression-b01-b02-20261031/results.json):75PASS/3FAIL. Hai parametercase FAIL khi harness gọi SqlDecimal.Value để ép số scale18 về CLRdecimal; create/updateemployee max đã PASS. Case chốt/chi max báo53302; lượt đầu không lưu thời điểm chi/chốt nên không khẳng định nguyên nhân, nghi fixture lấy DateTime.Now rồi truncate có thể trước NgayChotSQL làm tròn. Không vá sản phẩm hoặc ghi đè FAIL để né kết quả.

[Lượt bổ sung](runs/regression-b01-b02-followup-20261031/results.json):**78/78PASS**,0FAIL. Harness mới so SqlDecimal với SqlDecimal, không ép sang CLR khi đọc. Ngày chi được chuẩn bị sau thời điểm chốt và kiểm typed trước gửi; có ca chi ngày trước chốt bị từ chối. Expected nghiệp vụ giữ nguyên. [Manifest](runs/regression-b01-b02-followup-20261031/manifest.json), [mã bổ sung](runs/regression-b01-b02-followup-20261031/BugRegression.cs).

| Phần | Input / expected / actual |
|---|---|
| Employee create/update/replay/receipt |0,12.000.000,99.999.999.999,999.999.999.999,999.999.999.999.999.999VND; cả MonthlyRate/HourlyRate được lưu nguyên vẹn, tên50/SDT15/max chạy lại thành công; receipt đúng và replay không tạo thêmNV |
| Parameter nguyên vẹn |NULL,0,0,01,0,001,0,5,-1,99.999.999,99,max18chữsố vàquámax được truyền đúng vào SQL; helper không tự làm tròn. Giá trị sai chỉ dùng kiểm transport/validation, không cho lưu nghiệp vụ |
| SQL validation |Gọi DAL với lương0,5 bỏ qua BLL bị SQL53101, không đổi hồ sơ/khôngreceipt. Các ca âm/lẻ/quámax và precisiongiờ trong suite cũ vẫn PASS |
| Payroll max |Preview/chốt/chi999.999.999.999.999.999VND đúng; snapshot/receipt khớp; một PHIEUTHUCHI gắnMaBL đúng số tiền; cùngrequestreplay không nhân đôi. Ngàychi trướcchốt bị chặn; ngày hợp lệ qua |
| Giờ có phần lẻ |72giây=0,02h chia0,01thường/0,01tăngca, SQL tiền1VND; không đổi giờ thành nguyên. Suite cũ kiểm36giây làm tròn và tiền0,5→1 |
| Grid thật |EmployeesControl.MonthlyRate/HourlyRate và PayrollControl.Amount:0/35.000/12.000.000/max hiển thị đúng `0 ₫`, `35.000 ₫`, `12.000.000 ₫`, `999.999.999.999.999.999 ₫`. CurrentCultureen-US vẫn đúngvi-VN;12FormattedValue assertions, rawdecimal không đổi; DeviceDpi120. Đây là componentWinForms thật/STA, không phải thao tác native hoặc nghiệm thu thị giác |
| Regression nghiệp vụ |59subcase gốc chạy lại gồm quyền,ADMINcuối,4race2session có barrier/2waiters, công/overlap, batchrollback, snapshot, chi mộtlần, faultrollback, zeroexpense, financekhôngđếmhailần, businesshash ngoàiusecase; tất cảPASS |

RawError trong harness mới kiểm @@TRANCOUNT và sys.dm_exec_sessions.open_transaction_count=0; không lặp giả định0:0 của harness nghiệm thu đầu. Lịch sử test cũ giữ nguyên. Không có bug sản phẩm mới được chứng minh trong đợt này.

## Tài nguyên và giới hạn

HaiDB `CoffeeManager_Test_G06_20261031_024435_baf21892_demo` và `CoffeeManager_Test_G06_20261031_024630_9379d6cf_demo`: tạo2/dọn2/còn0, xác minh exactname+marker trong finally. Mỗi manifest SourceUnchanged=true. Không nâng development, không commit/push/publish. Không có pendingfile/processnative tạo trong regression này.

B01/B02 đã PASS trong phạm vi sửa. T24-K02 chuyểnPASS; T24-K01 chuyểnBLOCKED vì phầnMnative editor chưa đủ, lỗi tiền/text đã hết. TổngG06 hiện **26PASS/0FAIL/7BLOCKED**. Các thao tác selectall/dirty/Esc/Enter/busy/unknown/reload/linklồng vẫn thiếu bằng chứng native như báo cáo trước. Hình thức SKIPPED do người dùng tự kiểm; DPI100/150%NOTRUN, không đổi WindowsScale. Không cần duyệt lại hai fix đã thuộc đợt; G06 vẫn Đang làm đến khi các ca bắt buộc đủ bằng chứng.
