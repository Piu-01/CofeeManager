# G05-B01 — Không tiếp cận được thao tác trong workspace mới

> Cập nhật29/10/2026: bug trong đợt G05-B01–B04 đã duyệt, đã sửa và PASS regression theo phạm vi tại phần cuối. Nội dung27/10 dưới đây là lịch sử điều tra.

27/10/2026. **Chờ duyệt đợt bug; chưa sửa.** Chủ sở hữu T20, hồi quy T19/T21/T22/T23.

## Hiện tượng và tái hiện

Trong phiên WinForms Debug thật, đăng nhập thường bằng ADMIN fixture NV991, vào Kho → Nhập hàng. Danh sách 32 phiếu tải được nhưng không có nút **Lập phiếu nhập**, **Tìm / Tải lại**, **Xem chi tiết** trong accessibility tree. Đổi kích thước cửa sổ vẫn không có thao tác. Tab Xuất kho cũng thiếu nút lập phiếu; Thu chi thiếu bộ lọc ngày/loại/nguồn và nhóm thao tác. Đây là lỗi tiếp cận chức năng, không phải đánh giá màu/font/spacing.

Bằng chứng: `../runs/acceptance-full-20261027_110730/gui-inventory.txt`, `gui-issue.txt`, `gui-finance-after.txt`; trạng thái sau chuyển tab và sau resize được ghi trong tool output. Không lấy screenshot làm nghiệm thu hình thức. Cửa sổ được kiểm ở kích thước thường và maximize; DeviceDpi chưa đo được, không suy PASS125%.

## Điều tra và phạm vi sửa

Các control mới thêm nút vào `OrbitListWorkspace.PrimaryActions`, và bọc nhóm thao tác/filter bằng `FlowLayoutPanel Dock=Fill`. `OrbitListWorkspace.OnLayout/ArrangeButtons` tự đo theo `Visible`, Width và vị trí từng control; hợp đồng layout này không giống một host Dock thông thường. T20/T21/T22 thêm workspace vào control trước khi thêm các thao tác, và được tạo dưới tab chưa hiển thị. **Chưa có phép đo runtime bounds để khẳng định nhánh layout nào là nguyên nhân cuối cùng**; không kết luận đây là lỗi quyền vì ADMIN tải được dữ liệu và source không có nhánh ẩn nút lập phiếu theo quyền.

Đề xuất debug layout/visibility lúc attach/chuyển tab; sửa cách cấu hình các workspace G05 theo hợp đồng helper chung. Chỉ sửa helper chung nếu chứng minh cần thiết; không thiết kế lại giao diện. Kiểm cả pager đang hiển thị so với handler phân trang, chưa coi khả năng lỗi pager là lỗi đã tái hiện.

## Ảnh hưởng và regression

T20-K01/K11/K12, T21-K01/K08/K10, T19-K05/K07/K10 và thao tác T22/T23 bị ảnh hưởng hoặc cần kiểm lại. Regression native: ADMIN/STORE tạo nháp → bỏ/chốt/reload, bộ lọc, chi tiết/link nguồn, phân trang trên >50 dòng; CASH bị chặn ở BLL/SQL. Kiểm chức năng tại125% khi đo xác nhận được DPI, giữ 100/150% NOT RUN. Nếu sửa helper chung, kiểm lại các workspace cũ sử dụng helper đó. Không đổi nghiệp vụ/SQL.

## B01 — Sửa và regression29/10

Đã duyệt cả đợt B01–B04. Probe trước sửa xác nhận tại DeviceDpi120: slot Primary và RowActions rộng0 khi dùng nút Dock.Fill/container FlowLayoutPanel Dock.Fill; ghép trực tiếp cho Primary rộng141 và RowActions118. Bằng chứng `../runs/debug-batch-20261029/layout-before.json`.

Sửa năm control G05: bỏ Dock.Fill/container bọc trong các slot tự đo, dùng StackedTools để filter wrap; nối handler Trước/Sau vào chính Pagination đang hiển thị và cập nhật tổng thực từ DTO. Không đổi OrbitListWorkspace hoặc quy tắc dữ liệu. 16 kiểm component trên năm control thật, hai kích thước1280×720/1366×768 ở125%, dữ liệu >50 dòng: PASS; native mở được Lập phiếu nhập, nháp chưa ghi kho, lịch sử nợ/link PN và chi tiết KK. Các nhánh chốt editor còn bị B05 bên dưới chặn; chưa PASS toàn bộ T20-K01/K11/K12 từ regression này.

## G05-B05 — Footer dialog cắt nút chốt/Quay lại — ĐÃ SỬA, REGRESSION29/10

> Người dùng đã duyệt B05 kèm regression. Nội dung điều tra bên dưới giữ làm lịch sử trước sửa. Kết quả sau sửa ở cuối mục.

**Hiện tượng:** sau khi nút workspace được sửa, ADMIN vào Kho → Nhập hàng → Lập phiếu nhập. Dialog mở được nhưng footer chỉ tiếp cận nút đầu Tính tổng; Chốt nhập kho và Quay lại nằm ngoài vùng được ancestor hiển thị. Dialog chi tiết nợ cũng chỉ hiện hành động đầu. Đây là lỗi chức năng tiếp cận nút, không phải nghiệm thu hình thức. Đóng bằng X vẫn hoạt động. Chưa sửa lỗi mới này.

**Bằng chứng:** native `../runs/debug-batch-20261029/native-purchase-editor.txt` ghi có ba button trong AX, cùng tool observation xác định vị trí nút không tiếp cận bằng chuột. Probe độc lập dùng đúng OrbitDialogLayout + EnableResponsiveActions, DPI120: Form client1107×671, actions chỉ rộng139 nhưng cao150; Chốt nhập kho ởY50 và Quay lạiY100 đều có VisibleHeight0 do clipping của ancestor. `../runs/debug-batch-20261029/dialog-footer-diagnostic.json` ghi **FAIL**; không sửa expected để né.

**Nguyên nhân:** EnableResponsiveActions bật WrapContents và đặt MaximumSize nhưng footer vẫn dùng cột actions AutoSize. Panel bị cấp chiều ngang tối thiểu rồi wrap thành nhiều hàng; phần footer/chiều cao form được đo theo preferred size không phản ánh các hàng thực tại chiều ngang đó. Kết quả button.Visible=true trong AX không bảo đảm nút nằm trong vùng client tiếp cận được.

**Đề xuất:** sửa riêng cơ chế opt-in EnableResponsiveActions của OrbitDialogLayout: cấp chiều ngang footer thật cho actions, đo wrap/chiều cao theo chính chiều ngang đó và cập nhật hàng footer trước clamp WorkingArea. Giữ titlebar/X/owner/modal, thứ tự hành động, guards nháp/busy/unknown/session; không đổi nghiệp vụ, không làm Enter tự chốt. Kiểm các dialog G05 dùng opt-in: thu chi, nhập, xuất, kiểm kê, trả nợ và detail/link nguồn; dialog cũ chưa opt-in giữ hành vi cũ. Chỉ sửa caller nếu cần theo contract helper đã chứng minh.

**Regression sau duyệt:** nhiều nút/ít nút, nút ẩn hiện theo unknown/role, leading action nếu có; nút chốt/Quay lại nằm trọn vùng client ở125% và hai kích thước, resize/clamp, mở dialog lồng rồi đóng đúng owner; native preview → xác nhận → ghi một lần/reload và bỏ nháp không ghi. Unit/SQL quyền/receipt theo ảnh hưởng. Không đổi Windows Scale,100/150% NOT RUN, hình thức do người dùng kiểm. Rủi ro chính là footer chiếm nhầm vùng Body hoặc đổi dialog khác nếu vô tình bỏ giới hạn opt-in.

### B05 — Kết quả sửa

Chỉ đổi OrbitDialogLayout: cột actions nhận phần chiều ngang còn lại sau leading action, đo chiều cao wrap theo chính chiều ngang đó, cập nhật footer/hàng chứa trước FitToContent; guard tránh Layout lặp. Chỉ tác động khi opt-in. Không đổi guards/handlers/SQL. Regression source cố định:28/28 helper case và23/23 checkpoint trên9 dialog thật PASS ở DPI120;23 checkpoint đều trong WorkingArea, AcceptButton=null. Debug/Release0warning/0error,22/22 unit,5 SQL replay subcase PASS. Native Quay lại→Không giữ nháp, Quay lại→Có bỏ nháp đúng owner; kho/nợ/PN không đổi. Native thu chi→xác nhận→ghi một lần/reload PASS (2345đ,1 receipt). Native chốt nhập còn BLOCKED bởi B06 độc lập. Chi tiết, lệnh và giới hạn harness tại [biên bản B05](../runs/b05-20261029/test-results.md). B05 clipping đã khắc phục; không suy toàn G05 PASS.

## G05-B06 — Grid nhập/xuất đang khóa ô cần nhập — ĐÃ SỬA29/10

**Hiện tượng đã tái hiện ở T20:** ADMIN fixture đăng nhập thường, Kho→Nhập hàng→Lập phiếu, thêm NVL12. Lượng và giá khởi tạo0; click/double click ô lượng không mở editor, giá trị vẫn0. Tính tổng trả “Lượng và giá phải dương; giá nguyên VND, lượng tối đa4 số lẻ.” Không nhập được chứng từ hợp lệ để preview/chốt. Bằng chứng AX [native-purchase-validation.txt](../runs/b05-20261029/native-purchase-validation.txt), tool observation29/10. Không ghi fixture vào editor để giả lập PASS.

**Nguyên nhân có bằng chứng source:** OrbitDataGridView constructor đặt ReadOnly=true. PurchaseEditorDialog chỉ đặt ReadOnly theo cột nhưng không mở ReadOnly của toàn grid; khóa cấp grid vẫn thắng cột. StockIssueEditorDialog có cùng cấu hình nên cần sửa và regression cùng đợt; T21 là ảnh hưởng từ source, chưa ghi native tái hiện PASS/FAIL. StocktakeEditorDialog.State đã đặt grid.ReadOnly=!Editable, không thuộc lỗi cấu hình này. Không sửa default chung vì các danh sách/chi tiết phải chỉ đọc.

**Đề xuất phạm vi cụ thể:** mở chỉnh sửa grid riêng tại PurchaseEditorDialog và StockIssueEditorDialog, cấu hình EditMode phù hợp với nhập trực tiếp; giữ Code/Name/Unit chỉ đọc, chỉ cho sửa Quantity/Price của nhập và Quantity của xuất. Tái sử dụng SelectAllOnEntry hiện có, không thay validation decimal/SQL/BLL/transaction/pending, không thêm tính năng. Không đổi OrbitDataGridView mặc định.

**Regression sau duyệt:** ADMIN/STORE nhập lượng/giá bằng chuột và Tab, select-all editor; cột mã/tên/đơn vị không sửa; số sai/0/âm/giá lẻ bị chặn; preview SQL→xác nhận→chốt một lần/reload đối chiếu kho/chi/nợ/receipt trên DB test riêng; bỏ nháp không ghi; xuất nội bộ/hủy lượng đúng và chặn vượt tồn; quyền CASH/STORE/ADMIN, nhánh unknown/khóa gửi giữ hợp đồng. Kiểm lại footer sau validation/status và stocktake/list/detail vẫn khóa đúng. Unit/SQL subset và native theo ảnh hưởng;125%, hình thức SKIPPED,100/150% NOT RUN. Rủi ro: mở nhầm cột tham chiếu hoặc editor không commit giá trị trước chốt; kiểm cột và EndEdit bằng luồng thực.

**Lịch sử trước sửa:** B06 đã được trình duyệt bổ sung theo skill; người dùng đã trả lời “duyệt” cho B06 kèm regression29/10.

### B06 — Sửa và regression sau duyệt29/10

Chỉ đổi ReadOnly=false/EditOnEnter tại grid hai editor, giữ readonly cột Code/Name/Unit, không đổi helper chung/SQL.54/54 component assertion và22/22unit,5SQL subcase PASS; Debug/Release sản phẩm0warning/0error. Native ADMIN nhập2×10000/trả4000 tạo PN20250504/nợ16000; native xuất0 bị chặn,9999 vượt tồn rollback giữ input, sửa1 tạo PX20250506. Đối chiếu đúng1PN/1PX/1chi/1nợ/2receipt, NVL12 tăng ròng1, các NVL khác không đổi. Bỏ nháp xuất sau sửa3 không ghi DB. Source cố định, DB/GUI/credential tạm đã dọn. [Biên bản chi tiết](../runs/b06-20261029/test-results.md).

Không phát hiện bug mới. Native Tab chưa có bằng chứng đáng tin do công cụ kích hoạt lại owner; chưa suy PASS toàn bộ STORE/trả hàng/native keyboard/UX hoặc toàn55ID. B06 đã khắc phục nguyên nhân khóa grid; G05 vẫn Đang làm theo phần nghiệm thu còn thiếu.
