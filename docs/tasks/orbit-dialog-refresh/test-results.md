# Kết quả test chức năng cửa sổ con Orbit

**Cập nhật sau duyệt sửa:** B01/B02 đã sửa và44ca chức năng regressionPASS; xem [regression-results.md](regression-results.md). Các kết quảFAIL/BLOCKED bên dưới là lịch sử lượt trước sửa, không bị xóa hoặc sửa thànhPASS.

22/10/2026 — **chưa đạt**, còn B01/B02. Người dùng duyệt “test đi, giao điện thì mình tự nhìn không cần test”; hình thức/ảnh/DPI/resize/Designer được **SKIPPED theo yêu cầu**. Không sửa source ứng dụng hoặc migration trong đợt test. Đối chiếu hash với `runs/source-implementation.csv`: **0 file thay đổi** sau test.

## Môi trường, phương pháp và bằng chứng

- Windows, SDK .NET10.0.401, runtime10.0.13; build Debug/Release trước test sạch. Harness `runs/FunctionalProbe` tham chiếu project WinForms, chạy Release, STA + Application.Run, form/control thật và handler qua PerformClick/reflection. Không thay thế control bằng mock HTML/ảnh; không thao tác đăng nhập. Fixture SessionContext lấy identity tài khoản admin của DB test để gọi BLL/DAL, **không phải kiểm thử xác thực hay quyền các role khác**.
- SQL Server thật localhost, database `CoffeeManager_Test_G03_20261019_215457_29e552a9_demo`. Trước mỗi lượt truy vấn kiểm `dbo.G03TestMarker.RunId = 20261019_215457_29e552a9`. Không dùng DB development/sử dụng thật, không seed/migration lại.
- Snapshot source: `runs/source-implementation.csv`; kiểm lại `runs/source-after-tests.csv` và `runs/source-integrity.json`. Không đổi backend. Không thay WindowsScale trong lượt này.
- Quy tắc: `dotnet test tests/CafeManager.Tests/CafeManager.Tests.csproj --no-restore --filter 'FullyQualifiedName~MaterialRulesTests|FullyQualifiedName~CatalogRulesTests|FullyQualifiedName~G03.RulesTests' --logger 'trx;LogFileName=rules.trx' --results-directory docs/tasks/orbit-dialog-refresh/runs/rules -v minimal`. **42/42 PASS**, log TRX `runs/rules/rules.trx`.
- Harness: `dotnet run --project docs/tasks/orbit-dialog-refresh/runs/FunctionalProbe/FunctionalProbe.csproj -c Release --no-restore -- docs/tasks/orbit-dialog-refresh/runs/functional-results.json`; lượt bổ sung thêm `--followup` và output `functional-followup.json`. Build/run cần ngoài sandbox cho WinForms/SQL thật.

## Kết quả theo phạm vi đã duyệt

| Ca | Thao tác/input và expected | Actual / trạng thái |
|---|---|---|
| D01 / D05 editor chung | AddText “Tên khách”, nháp tiếng Việt; bấm Lưu với ArgumentException phải giữ nháp, ErrorProvider/focus đúng input | **FAIL B01**: AddText/AddField đã ném NullReferenceException trước khi kiểm validation; chưa gọi write |
| D01b | Mở sản phẩm/bàn/khuyến mãi, giữ binding typed | **BLOCKED B01**: cùng AddField; không gán PASS từ đọc source |
| D02a | Mở Sửa đủ7 CatalogKind bằng row thật; mã readonly, tên/SDT/giờ/checkbox giữ giá trị, trường ẩn theo kind, không dirty sau Populate | **PASS** 7 ca. PT010 disabled support kiểm bổ sung PASS |
| D02b / D05 ngày | Gán22/10/2026; nhập31/02/2026; đọc Value phải từ chối và giữ nguyên chuỗi sai | **PASS** control OrbitDateField. Không kiểm popup hình thức/che/resize; lịch tương tác chuột chưa chạy |
| D03a | Mở Công thức từ ProductsControl thật, rồi chọn nguyên liệu/đơn vị/xóa nháp/Hủy | **FAIL B01** tại AddField trước modal. Chưa tới thao tác unit/xóa; không thay DB công thức |
| D03b | STORE readonly recipe, ADMIN policy preview/đổi nháp/Hủy | **BLOCKED B01** cho dialog bảng; chưa chạy role STORE/policy thật |
| D04a | CustomerOrderList mới, KH001 và KH999; đối chiếu số dòng qua OrdersAsync,3cột,Readonly | **PASS** binding/query. Chưa chạy page tiếp theo khi >50dòng; <=50 và không đổi cột không chứng minh đủ phân trang nhiều trang |
| D04b | Picker Points tìmSDT0911111111 bằng TextBox+ResetAndReload, chọn | **PASS** đúng1row KH001, GetAsync đối chiếu rồi trả KH001. Không Thêm/Sửa/Xóa/Mở sổ/Hóa đơn/vãng lai |
| D04c | Preview picker có Khách vãng lai; chọn rồi mở picker khác, Đóng sau tải xong | **PASS** guest=>OK+null; Đóng khôngOK, null. Không kiểm wrapper Preview cuối vì B01 |
| D05 catalog | Thêm DeliveryPartner: codeDT987, tên nháp, ratio“abc”; Lưu | **PASS** giữ“abc”, ErrorProvider và focus ngay ratio; không writing/không gọi ghi hợp lệ |
| D05b | Preview tiền/điểm/chính sách, thay input vô hiệu preview cũ | **BLOCKED B01**; không thay expected bằng unit rules |
| D06a | YesNo/OKCancel: positive,negative,Escape,X,Enter qua ProcessDialogKey trong modal fixture | **9/10 PASS,1FAIL B02**: YesNo X trảCancel thayNo. Enter không đi nhánh đồng ý. Không gọi đây là test phím vật lý/native automation |
| D06b busy | Delegate đang chờTCS; nhấnLưu2lần vàClose | **PASS** chỉ1write, khóafields/nút/Close; hoàn tất trảCode/AffectedCount |
| D06b unknown | Delegate OperationCanceledException; thửClose,Đối chiếu receipt | **PASS** khóaLưu/Hủy/Close; bậtĐối chiếu, cùng requestId, trảCode |
| D06b retry | Reconcile không thấyreceipt; đồng ý gửi lại | **PASS** CaptureWrite chỉ1lần,2lần write cùngGuid/payload đã capture |
| D06b từ chối | CaptureWrite hợp lệ, xác nhậnHủy | **PASS**0write, không ResultCode, dialog còn mở |
| D06b readonly | SetReadOnly trên dialog | **PASS** ẩn Lưu/Đối chiếu, cònĐóng. Không thay thế kiểm quyền BLL |
| D06b dirty | AddText rồi đổi vàClose/Hủy/Bỏ nháp | **FAIL B01** trước khi đổi nháp; chưa kiểm được prompt dirty của editor chung |
| D07 / phần hình thức D01–D08 | Screenshot, màu/font/spacing/hover,resize,DPI,Designer | **SKIPPED theo yêu cầu người dùng**, không có ảnh mới và không khẳng định giống mẫu |
| D08 | Regression đầy đủ các màn chính/quyền ADMIN/CASH/STORE | **NOT RUN**. Chỉ query/binding cần cho picker/catalog/recipe đã chạy; không cấpPASS toàn module |
| D09a catalog | Thêm danh mụcSP qua CatalogEditorDialog thật, Save_Click=>BLL=>DAL=>SQL; đọc lại và xóa fixture | **PASS** tạoDM011 tênDlg1022204150; đọc đúng tên; DeleteAsync vàGetAsync=>null. Receipt/audit do contract tạo được giữ, không xóa lịch sử |
| D09a customer | Thêm qua OperationEditorDialog, đọc lại/xóa fixture | **FAIL B01** tạiAddText trướcCaptureWrite; không tạo khách fixture |
| D09a nhóm khác | Ghi nguyên liệu/sản phẩm/bàn/khuvực/khuyếnmãi bằng editor | **BLOCKED B01**; không chuyển sang gọi service ghi trực tiếp để thay thế editor bị lỗi |
| D09b | Hủy mởsổ/điểm/chínhsách và đối chiếu DB | **BLOCKED B01** ở editor thực; chỉ fixture xác nhận0write đãPASS, chưa gọi là PASS tiền/kho/điểm |

## Sai thao tác harness và việc kiểm lại minh bạch

Giữ nguyên file JSON lượt đầu, không sửa expected để né lỗi. Lượt đầu28record:21PASS/7FAIL (có1record marker, không phải ca nghiệp vụ). Trong7FAIL có4ca cùng B01,1ca B02 và2lỗi thao tác harness:

- Picker Đóng bị gọi ngay lúc đang tải nên nút disabled; assertion gộp báo “Close selected a customer”, nhưng chưa hề chọn khách. Lượt bổ sung đợi busy=false rồiĐóng:PASS; kiểm guest riêngPASS.
- Catalog fixture tìm text“Lưu”, trong khi button có mnemonic“&Lưu”, nên Single không tìm thấy. Chưa gọi handler/SQL. Lượt bổ sung gọi đúng fieldsaveButton:PASS ghi/đọc/xóa thật. Không thay button ứng dụng.

Lượt bổ sung10record đềuPASS, trong đó1record chỉ ghi mã fixture và9ca kiểm chức năng. Không cộng record marker/fixture thành ca nghiệm thu hoặc dùng các PASS độc lập để xóaFAIL B01/B02.

## Giới hạn và kết luận

Chưa test đầy đủ chức năng doB01 chặn editor dùng chung; chưa chạy thao tác người dùng trong phiên app chính, quyềnCASH/STORE, nhiều trang>50, gridcommit bằng bàn phím, preview/opening nghiệp vụ và dirty chung. Không tạo thay đổi kho/điểm/chính sách. Task giữ **Đang làm**, không sửa G03/T01–T32; chuyển duyệt cả đợtB01/B02 trước debug/regression. Xem bug-report.md.
