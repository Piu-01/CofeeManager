# T26 — Chốt bảng lương và chi lương một lần

Ngày 30/10/2026. **Cả bộ plan G06 đã được duyệt ngày30/10/2026**; requirements nhóm đã duyệt. Phụ thuộc T25; áp dụng [plans.md](../plans.md). Đã triển khai source; chưa chạy đợt nghiệm thu. Checklist gốc cuối file là tiêu chí nghiệm thu.

## 1. Quyết định tính lương và snapshot

Đã đối chiếu BANGLUONG baseline: MaBL identity int, unique (MaNV,Thang,Nam), SoGioLam decimal(10,2), các khoản tiền decimal(18,0), ThucLinh computed = LuongCoBan + ROUND(SoGioLam × DonGiaGio,0) + TienTangCa + PhuCap + Thuong − KhauTru. Giữ nguyên computed expression và các giá trị kỳ cũ; không UPDATE computed column hoặc tạo bảng lương thứ hai.

- Phân kỳ bằng NgayLam. SQL cộng tổng giây các ca Đi làm hoàn tất rồi ROUND(tổng giây/3600,2) một lần, không làm tròn từng ca. Dòng Nghỉ/Vắng không cộng giờ. Dòng Đi làm thiếu một trong hai giờ hoặc overlap bất kỳ dòng trong kỳ với các ca liên quan ngoài kỳ phải xử lý trước preview/chốt; không tự loại bằng 0. Hiển thị vấn đề và đường quay về chấm công, không thêm thao tác bỏ qua ca âm thầm.
- ADMIN nhập giờ thường và giờ tăng ca decimal(10,2), đều không âm và tổng bằng tổng giờ hợp lệ đã làm tròn. Preview mặc định giờ thường = tổng giờ, giờ tăng ca = 0; ADMIN duyệt thay đổi rõ ràng. Không tự suy tăng ca từ CALAMVIEC hoặc CHUCVU.HeSoTangCa. TienTangCa là khoản tiền ADMIN nhập riêng, không nhân hệ số tự động; lưu giờ và tiền tăng ca tách biệt.
- Full-time: LuongCoBan = LuongCung snapshot, SoGioLam = 0, DonGiaGio = 0 để computed column không cộng giờ lần hai. Giờ thường/giờ tăng ca vẫn lưu trong các cột snapshot mới. Part-time: LuongCoBan = 0, SoGioLam = giờ thường duyệt, DonGiaGio = LuongTheoGio snapshot. Cả hai cộng TienTangCa/PhuCap/Thuong trừ KhauTru. PhuCap khởi tạo từ chức vụ hiện tại cho preview nhưng ADMIN duyệt khoản cuối; thay chức vụ sau chốt không đổi khoản này.
- Tất cả tiền nguyên VND, không âm, không vượt 18 chữ số; phép nhân/tổng dùng decimal rộng trước kiểm cast, khấu trừ không vượt tổng, rounding SQL cùng computed column. Giờ đã làm tròn không vượt miền decimal(10,2). Input C# giữ decimal và không tính kết quả chính thức.
- Không có quy tắc tự chia lương full-time theo ngày công: giữ lương tháng như kế hoạch. Kỳ không có công trả tổng 0 rõ ràng; full-time vẫn preview lương tháng, part-time phần giờ 0, ADMIN phải đọc xác nhận. Không cho chốt tương lai: kỳ kết thúc không sau kỳ hiện tại theo ngày máy SQL; kỳ hiện tại vẫn có thể chốt theo quyết định ADMIN và sẽ khóa nhập công còn lại trong kỳ, thông báo rõ trong xác nhận. Đây là quyết định vận hành được trình duyệt trong bộ plan, không tự prorate hoặc tự chốt định kỳ.
- Cho phép chốt kỳ lịch sử cho nhân viên đã nghỉ nếu hồ sơ còn tồn tại, công hợp lệ và ADMIN xác nhận; mức lương dùng hồ sơ hiện tại tại thời điểm preview, hiển thị rõ vì chưa có lịch sử hiệu lực mức lương. Không suy ra mức lương quá khứ. Nếu cần cơ chế hồi tố theo ngày hiệu lực thì ngoài phạm vi, phải trình riêng.
- Một lần chốt cho một nhân viên/kỳ; không thêm batch lương. Snapshot mới gồm loại hợp đồng, hai mức lương hồ sơ, chức vụ/tên chức vụ, tổng giây/giờ hợp lệ, giờ thường/tăng ca duyệt, các khoản duyệt, người/ngày chốt, phiên bản nguồn và danh sách công đã dùng (khóa PK + giờ/trạng thái/revision). Lưu PayrollAttendanceSnapshot dưới MaBL để giải thích kết quả, không phụ thuộc tên/ca mẫu sau này.
- Preview SQL trả các vấn đề, công thức, khoản duyệt, tổng thực lĩnh và token SHA256 của source/employee/position version + request khoản duyệt + kỳ. Chốt dưới cùng employee applock với T25/T24, đọc lại và tính lại bằng cùng routine SQL; token khác thì trả 53303 và yêu cầu preview/xác nhận lại, không chốt số mới ngầm. Dữ liệu công được khóa đến khi ghi snapshot, không giữ transaction khi người dùng xem preview.

## 2. Legacy và migration 024

File mới `database/migrations/024_payroll.sql`, yêu cầu 023 và runner transaction. Preflight unique/FK/computed không âm, precision và nguồn PHIEUTHUCHI.MaBL/OriginKind/TC 005/số tiền. Có liên kết chi nhưng sai tiền hoặc sai nguồn thì dừng 53307, không sửa phiếu chi bất biến.

Seed có hai bảng lương tháng 04/2025 nhưng không đủ bằng chứng loại hợp đồng/giờ duyệt lịch sử. Thêm SnapshotKind New/LegacyUnknown, các cột bổ sung nullable cho legacy, không lấy hồ sơ hiện tại backfill giả. Giữ toàn bộ cột tiền/giờ cũ và MaBL; khóa công kỳ này ngay như T25. Hiển thị “Dữ liệu cũ — chưa đủ snapshot”. Không cho chi mới các bảng LegacyUnknown để tránh trả trùng với khoản chi cũ chưa được liên kết; nếu muốn chi kỳ cũ phải có quyết định đối soát/migration riêng. Bảng legacy đã có MaBL linked hợp lệ vẫn hiển thị Đã chi từ chứng từ thật, không tạo nguồn khác.

Thêm constraint cho snapshot mới: giờ thường+tăng ca=tổng giờ, loại hợp đồng hợp lệ, full-time/part-time đặt trường tính đúng; metadata đầy đủ. Trigger chống UPDATE/DELETE BANGLUONG và snapshot lines đã tạo, gồm legacy sau cutover. Snapshot lines chỉ thêm cùng transaction chốt; runtime DENY DML; không dùng cờ phiên tùy ý để bypass bảo vệ. Có thể bổ sung check/trigger INSERT nguồn chi Payroll để đối chiếu MaBL tồn tại, ThucLinh dương và số tiền đúng, không sửa logic thu/chi nguồn khác.

Danh mục chi lương dùng mapping Payroll→TC 005 từ 003 và helper sp_FinanceSystemExpense ở 017; không thêm loại chi hoặc phương thức thanh toán khi chưa có trong schema tài chính. MaNV trên PHIEUTHUCHI là nhân viên của actor quản lý thực hiện chi, người nhận truy theo BANGLUONG.MaNV.

## 3. Public SQL và thứ tự transaction

| Procedure | Contract |
|---|---|
| sp_PayrollList/Get | Actor/Stamp, kỳ/nhân viên/trạng thái/search/skip hoặc MaBL; tổng thật, snapshot nullable rõ, thực lĩnh, trạng thái chi và MaTC nguồn |
| sp_PayrollPreview | Actor/Stamp, Employee, Month/Year, giờ/khoản duyệt; issues, source totals, công thức, pay amount và preview token; không ghi bảng/receipt |
| sp_ChotBangLuong | Actor/Stamp, RequestId, Employee/kỳ, preview token và khoản duyệt typed | 
| sp_ChiLuong | Actor/Stamp, RequestId, MaBL, ngày chi và expected amount từ bước xác nhận; số tiền chính thức vẫn lấy DB |
| sp_PayrollReceipt | Actor/Stamp, RequestId, Operation Commit/Pay; khóa request, trả Recorded/NotRecorded/StillProcessing cùng MaBL/MaTC |

Public text/tiền/giờ nhận tham số đủ rộng rồi validate trước cast; kỳ nhận int để chặn 0/13 trước tinyint. Date boundary dùng cutoff không overflow năm 9999. ADMIN-only qua sp_G03 Demand, quyền runtime chỉ EXECUTE public, helper tính lương/chi/receipt không được gọi trực tiếp từ client. Lỗi 53301 input,53302 trạng thái/legacy/đã chi,53303 preview thay đổi,53305 giới hạn tiền/giờ,53307 preflight; giữ 51505/51605/51608 và timeout mapper.

Chốt: BEGIN TRAN → demand → request lock/replay → employee applock → khóa employee/position/kỳ/công và kiểm period unique → routine tính chính thức + đối chiếu token → INSERT BANGLUONG và attendance snapshot → receipt PayrollCommit → COMMIT → trả MaBL/snapshot. Receipt dùng G03 OperationReceipt qua helper FinanceBegin/Finish với EntityCode là MaBL chuỗi, Operation PayrollCommit. Không dùng MAX(MaBL) hoặc ghi đầu ra trước commit.

Chi: BEGIN TRAN → demand → request lock/replay → đọc mã NV của MaBL để lấy cùng employee applock → khóa bảng lương → kiểm snapshot New, trạng thái, amount/date → gọi sp_FinanceSystemExpense Payroll với MaBL, ActorEmployee, ThucLinh, NgayChi → receipt PayrollPay → COMMIT. Date không trước ngày chốt và không sau thời điểm hiện tại của SQL; input tương lai bị chặn, không sửa ngày tự động. Unique UX_FinanceBL là bảo vệ cuối, hai request chi cùng nguồn không tạo hai phiếu. Trigger nguồn bổ sung không làm helper ghi lặp tiền.

Thực lĩnh 0: trạng thái NoExpense/Không phát sinh chi suy từ bảng lương, nút Chi bị khóa. sp_ChiLuong gọi trực tiếp cũng trả từ chối/NoExpense có kiểu rõ, không insert PHIEUTHUCHI, không báo Đã chi. Đã chốt chưa có MaTC và ThucLinh>0 là Chưa chi. Không dùng trạng thái paid lưu riêng dễ lệch nguồn. Report/danh sách tài chính chỉ sum PHIEUTHUCHI cho chi thực, không sum BANGLUONG lần nữa; báo cáo tổng hợp vẫn ởT 28.

Reconcile cùng request lock: receipt có MaBL, operationPay đọc thêm nguồn MaTC; chỉ kết luận chưa ghi sau khi lock thành công. Cùng request/payload replay trả nguồn cũ; request khác cho kỳ đã chốt/đã chi từ chối kèm đường mở nguồn, không ghi lần hai. Sau timeout không đổi expected amount, request hoặc ngày rồi gửi lại. Không auto retry transport/deadlock sau khi UI mất trạng thái.

Kiểm kỹ thuật: clean/demo 023→024, legacy được gắn nhãn nhưng tiền không đổi, invalid nguồn rollback, runtime compile/demand/typed preview→chốt→chi một fixture; technical-checks riêng, chưa PASS T26.

## 4. Models/DAL/BLL và liên kết nguồn

Thêm Models/Requests/HumanResources/PayrollRequests.cs, Dtos/HumanResources/PayrollRow.cs, PayrollPreview.cs, PayrollIssue.cs, PayrollReceipt.cs. MaBL int, MaTC string nullable, timestamp typed, decimal cho giờ/tiền, enum trạng thái DTO đủ Chưa chi/Đã chi/Không phát sinh chi/Legacy cần đối soát. Không hiển thị unknown legacy thành 0 hoặc hợp đồng hiện tại.

Thêm DataAccess/Repositories/PayrollRepository.cs gọi procedure async bằng executor hiện có; mapper nullable cho legacy/JSON rỗng như sửaG05 B02/B03, deserialize snapshot không tự giả list rỗng nếu lỗi dữ liệu. Business/Services/PayrollService.cs quyền PayrollManage ADMIN-only, chuẩn hóa và kiểm input sớm, snapshot/result từSQL; không tính lại thực lĩnh ởC#. Sửa PermissionService/SqlErrorMapper 533 xx. Không giữ password hoặc thay phiên trong payroll.

Mở rộng UI/DocumentLinks.cs với dependency PayrollService được nối tạiProgram, ShowPayrollAsync(owner,int); constructor giữ tương thích caller cũ khi cần. FinanceDetailDialog.cs có nút Bảng lương nguồn khi PayrollId có giá trị; PayrollDetailDialog có nút Phiếu chi. Giữ owner chain ngay cả mở lồng qua lại; tránh mở trùng cùng chứng từ trong stack gây vòng vô hạn bằng kiểm source hiện đang mở hoặc khóa nút nguồn quay lại phù hợp. Không đổi contract tài chínhG05 hoặc cho STORE đọc lương.

## 5. UI bảng lương, preview và chi

Thêm Modules/HumanResources/PayrollControl.cs, PayrollPreviewDialog.cs, PayrollDetailDialog.cs, PayrollPaymentDialog.cs; ghép tab cuốiHumanResourcesControl và dependenciesProgram. List dùng base/stacked search, filterkỳ/employee/status, range/pager dưới grid và tổng thật. Các cột số typed, trạng thái có chữ; không gọi snapshot đã chốt là đã trả.

Preview chọn NV/kỳ → tải công và vấn đề → nhập khoản duyệt → Xem preview SQL → hiện công thức/giờ/tiền → Chốt bảng lương. Thay input làm token cũ hết hiệu lực và khóa Chốt đến khi preview lại. Vấn đề công có đường đóng/quay về chấm công; giữ kỳ/nhân viên đã chọn. Footer có thực lĩnh và hành động nhìn được qua cuộn/body/WorkingArea, không tạo footer cố định cắt nội dung.

Chi lương chỉ đọc nhân viên/kỳ/MaBL/thực lĩnh, nhập ngày chi typed; xác nhận trả toàn bộ và bất biến sau ghi. Enter không tự chốt/chi; hai bước chỉ có một nút hành động chính mỗi bước. Modal native có owner quaOrbitDialogHost, dùngOrbitDialogLayout vàOrbitField; không bật bụi sao.

Pending scopedG06 riêng PayrollCommit/PayrollPay: lưu request/payload/token/MaBL trước gửi, khóa nháp khiUnknown và có Kiểm tra kết quả; crash/restart đọc lại cùngDB/actor. Sau receiptRecorded mở nguồn/reload đúng một lần, pending clear lỗi không làm ghi lại. KhiNotRecorded đã xác minh và preview đã thay đổi, cho quay về nháp/lấy preview mới sau xác nhận, không tái dùng request với payload mới. Đóng/login lại không tự hủy transactionSQL; phiênhết quyền chặn query mới và giữ pending an toàn. Cancellation/generation/disposal theo mẫu vòng đời đã sửaG05 B07.

## 6. Fixture và nghiệm thu sau duyệt đợt test

Thêm database/tests/g 06/t 26_*.sql, tests/CafeManager.Tests/HumanResources/PayrollRulesTests.cs cho validation/state/role có giá trị; SQL là nơi kiểm công thức chính thức, không viếtoracle sao chép implementation. Bộ fixture mẫu:

- Full-time lương 10.000.000, công 40 giờ (giờ thường 36/tăng ca 4), tăng ca 400.000, phụ cấp 500.000, thưởng 100.000, trừ 200.000 →10.800.000; SoGioLam tính tiền 0, snapshot giờ 36/4.
- Part-time tổng 40 giờ, thường 36/tăng ca 4, giá 35.000, tăng ca 210.000, phụ cấp 100.000, thưởng 50.000, trừ 20.000 →1.600.000; không cộng 4 giờ tăng ca vào 36 giờ thường lần nữa.
- Tổng giây tạo biên phần trăm giờ, giá lẻ tạo kết quảphần VND, SQLROUND đối chiếu computed; tiền 0, giới hạn 18 digits/overflow, sai kỳ/năm/giờ/khấu trừ, ca thiếu/overlap/quađêm cuối tháng.
- Legacyseed hai kỳ có tiền cũ, legacy linked hợp lệ và invalidsource clone; snapshotNew đổi hồ sơ/chức vụ/giờ mẫu sau chốt; đangnghỉ nhưng chốt kỳ cũ có xác nhận.

| ID | Thao tác và đối chiếu chính |
|---|---|
| T26-K01 | Hai ví dụ tính tay trên,SQLpreview/DBcomputed/DTO/native bằng nhau;full-time kỳ 0 giờ vàpart-time 0 giờ có nhãn đúng |
| T26-K02 | Tổng giây→round tổng một lần,phân loại thường+tăng ca=total,khôngdoublecount;preview vàcomputed khớp biên 0,5 VND |
| T26-K03 | Mỗi kiểu thiếu giờ/overlaptrongkỳ/crosstháng chặnpreview/chốt;giữ công vàkhông tạoMaBL/receipt |
| T26-K04 | 0/13,năm 1999/10000,âm/precision/overflow/khấutrừvượt;transaction 0/0 vàcácbảngkhông đổi |
| T26-K05 | Replaycùngrequest/requestmismatch vàbarrier 2 sessionchốtcùngNV/kỳ;1 BANGLUONG,1 snapshotset |
| T26-K06 | Đổi lương/hợp đồng/chức vụ sau chốt giữsnapshot; sửa giữa preview/chốt buộc previewlại;race hồ sơ/chốt |
| T26-K07 | UPDATE/DELETEbảng lương/snapshot vàInsert/Update/Deletecông kỳchốtđều bị chặn;racecông/chốt khônglọt dữ liệu |
| T26-K08 | Chi bằngThucLinh,MaNVactor,TC 005/Payroll/MaBLđúng;2 session/2 request chỉ 1 chi;receiptlookup |
| T26-K09 | Faultfixturegâylỗi sauINSERTchi/trướcFinish,rollbackMaTC/receipt/paidstate;timeouttrước/saucommit đối chiếu |
| T26-K10 | Thực lĩnh 0:khôngphiếu 0,khôngMaTC,khôngbadgeĐã chi;publicSQLdirect cũngkhông ghichi |
| T26-K11 | Chốt chưa chi so với chi thật;FinanceList chỉ sum 1 PHIEUTHUCHI,không cộng BANGLUONG;link hai chiều đúng owner |
| T26-K12 | ADMIN/CASH/STOREdirect BLL/SQL/read/preview/commit/pay/reconcile;nativeunknown/restart/DBmismatch/IOfailure/keyboard/session |

Các tên subcase được viết đầy đủ trong test-plan/test-results lúc chuẩn bị đợt test; bảng trên không rút gọn ca gốc cuối file. Ghi expected/actual và dữ liệu thật từng subcase; 2 session cóbarrier và snapshotDB/transaction. Engine thật/DB riêng/runtimeprincipal; không thêm fault hook sản phẩm. Hình thứcSKIPPED theo phân công, agent 125%,DPI 100/150%NOTRUN. Sourcecố định trong lượt test, bug ghi tasknày rồi duyệt cả đợt.

## 7. Đầu ra nhóm và chuyển pha

Đủ source/migration 024, module payroll vàliên kếtnguồn, technical-checks có bằng chứng. Viết development-guide.md và chuẩn bị Update-DevelopmentG06.ps 1 với preflight/backup/đốichiếu review được, chưa chạy development. Cập nhật trạng thái G06 riêng trong kế hoạch, không giữ dòngG06–G 08 đồng nhất khi đã triển khai và không đánh PASS từ build.

Sau cảT24–T26 trình test-plan.md cho duyệt một lần; chưa chạy nghiệm thu/debug, không commit/push. Sau duyệt chạy từng task và báo cáo report.md chung kể cảFAIL/BLOCKED/NOTRUN. T24-K08/T25-K08 kiểm lại với snapshotNew ởT26, không chỉ seed/fixture cũ.

## 8. Phạm vi và checklist gốc

### T26 — Chốt bảng lương và chi lương một lần

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Tách kết quả tính lương khỏi việc thực trả; tận dụng computed column và unique kỳ. |
| Phạm vi | BANGLUONG, procedure chốt/chi, snapshot hợp đồng và khoản duyệt, FK PHIEUTHUCHI.MaBL; không tạm ứng/chi nhiều đợt. |
| Quy mô | L — giờ, mức lương và tiền |
| Phụ thuộc | T25 |
| Đầu ra | PayrollService/module; bảng lương bất biến, nguồn chi chống trả trùng. |

**Kiểm thử bắt buộc:**

- [ ] `T26-K01` [I] SQL tính full-time = lương tháng + phụ cấp/tăng ca/thưởng − khấu trừ; part-time = giờ thường×giá + khoản còn lại; đối chiếu dữ liệu kỳ mẫu bằng kết quả tính tay.
- [ ] `T26-K02` [I] SQL không tính giờ tăng ca đồng thời như giờ thường; preview SQL và ThucLinh DB làm tròn khớp.
- [ ] `T26-K03` [I] Ca thiếu giờ ra/overlap phải xử lý trước chốt; không tự im lặng tính 0 rồi chốt.
- [ ] `T26-K04` [I] Tháng 0/13, năm sai, tiền/giờ âm hoặc khấu trừ vượt tổng bị chặn.
- [ ] `T26-K05` [I] Hai lần/2 session chốt cùng nhân viên/kỳ chỉ có một BANGLUONG.
- [ ] `T26-K06` [I] Snapshot mức lương/loại hợp đồng/giờ/duyệt giữ nguyên sau đổi hồ sơ nhân viên.
- [ ] `T26-K07` [I] UPDATE/DELETE bảng lương chốt và sửa CC kỳ chốt qua app bị chặn.
- [ ] `T26-K08` [I] Chi lương đúng thực lĩnh tạo một phiếu chi gắn MaBL; chi lần hai bị chặn ở DB.
- [ ] `T26-K09` [I] Lỗi giữa ghi chi/liên kết rollback, không phát sinh chi rác hoặc trạng thái đã trả giả.
- [ ] `T26-K10` [I] Thực lĩnh 0 không tạo phiếu SoTien=0; UI hiển thị không phát sinh chi theo quy tắc.
- [ ] `T26-K11` [I] Bảng lương tạo nhưng chưa chi không được gọi là đã trả; báo cáo không cộng lương snapshot và phiếu chi hai lần.
- [ ] `T26-K12` [U/M] Chỉ ADMIN chốt/chi; timeout đối chiếu MaBL/phiếu chi trước thử lại, reload đúng.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.





