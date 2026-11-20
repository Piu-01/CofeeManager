# T25 — Chấm công, ca qua đêm và chống chồng giờ

Ngày 30/10/2026. **Cả bộ plan G06 đã được duyệt ngày30/10/2026**; requirements nhóm đã duyệt. Phụ thuộc T24; áp dụng [plans.md](../plans.md). Đã triển khai source; chưa chạy đợt nghiệm thu. Checklist gốc cuối file là tiêu chí nghiệm thu.

## 1. Contract thời gian và batch

Nguồn: CHAMCONG baseline/seed và CALAMVIEC/CatalogRevision migration003. PK(MaNV,NgayLam,MaCa), GioVao/GioRa datetime2(0), TrangThai SQL giữ N'Di lam'/N'Nghi phep'/N'Vang'; UI hiển thị tiếng Việt. Không thêm trạng thái lưu Đang làm; Chưa hoàn tất là nhãn dẫn xuất khi Đi làm thiếu vào/ra. Không tự điền giờ mẫu thành giờ thực.

- Giờ đầy đủ precision giây, ngày vào = NgayLam, ra > vào khi có ra; Nghỉ/Vắng không có giờ. Dòng Đi làm chưa đủ giờ được lưu nhưng chặn preview/chốt lương. Không giới hạn ca qua đêm vào cùng ngày hoặc tự cắt 24 giờ khi chưa có quy tắc nghiệp vụ đó.
- Khoảng hoàn tất [GioVao,GioRa), ca nối đúng thời điểm được phép. Ca đang mở có vào/không ra tạm xét [GioVao,+∞) để chặn ca khác chồng thời gian; dòng thiếu cả vào/ra không có interval nhưng vẫn chặn chốt kỳ. So với mọi dòng của nhân viên, kể cả NgayLam tháng trước/sau; không chỉ kiểm cùng ngày.
- Khóa kỳ theo nhân viên + tháng/năm của NgayLam, không khóa nhân viên khác. T25 kiểm EXISTS BANGLUONG ngay từ đầu, gồm seed/legacy. Ca qua đêm thuộc kỳ NgayLam, không cộng hai kỳ.
- Editor một dòng và màn Lưu batch dùng cùng procedure/TVP. Batch từ1 đến200 dòng, toàn batch atomic. Cột Action Insert/Update/Delete, key(MaNV,NgayLam,MaCa), ExpectedRevision cho Update/Delete. Editor khóa PK dòng đã lưu; muốn đổi key phải bỏ dòng/thêm dòng có xác nhận trong cùng batch, kiểm cả kỳ cũ/mới.
- PK trùng trong TVP hoặc hai action cùng key bị chặn; không upsert biến insert trùng thành update. Dựng trạng thái cuối của cả batch rồi kiểm overlap giữa các dòng mới và với DB trừ dòng được thay/xóa; không phụ thuộc thứ tự ghi từng dòng.
- Nhân viên đã nghỉ/ca không tồn tại chặn Insert/Update; dữ liệu cũ vẫn đọc. Delete công nhập sai được ADMIN xác nhận khi kỳ chưa chốt, giữ metadata request; không dùng Delete lách khóa kỳ. Đổi giờ mẫu không sửa giờ thực cũ.
- Thêm rowversion trên CHAMCONG; request UUID cố định cho batch. Receipt riêng AttendanceBatchReceipt/AttendanceBatchReceiptLine chứa request/actor/payload hash và key/action/result revision. Không nhét key tổng hợp vào EntityCode10 ký tự hoặc tạo receipt độc lập từng dòng làm mất atomic batch. Tái sử dụng request applock hiện có, không tạo framework chung.

## 2. Bước1 — Migration023 và SQL

File mới database/migrations/023_attendance.sql yêu cầu022 và runner transaction. Preflight CHECK cũ, orphan, trạng thái, ngày/giờ, overlap gồm interval mở; liệt kê key lỗi rồi dừng53207, không tự điền giờ/sửa ca. Incomplete hợp lệ được giữ và gắn nhãn, không chặn nâng cấp chỉ vì thiếu giờ. BANGLUONG cũ giữ nguyên và khóa kỳ ngay, kể cả chưa đủ snapshot nguồn.

Thêm Revision, receipt batch/lines và DENY DML runtime trên CHAMCONG/receipt. Receipt line giữ key lịch sử nhưng không FK đến dòng công, để xóa dòng công nhập sai vẫn giữ bằng chứng thao tác.

| Procedure/type | Input và kết quả |
|---|---|
| dbo.AttendanceBatch | RowNo, Action, MaNV, NgayLam, MaCa, GioVao, GioRa, TrangThai, ExpectedRevision; trường chuỗi đủ rộng để validate trước cast |
| sp_AttendanceList/Get | Actor/Stamp, kỳ/employee/status/search/skip hoặc PK; ngày giờ nullable, completed hours, incomplete, period locked, Revision, TotalRows |
| sp_AttendanceLookups | ADMIN; nhân viên/ca có mã/tên/active, gồm option cũ để xem lịch sử |
| sp_AttendanceSaveBatch | Actor/Stamp, RequestId, TVP; một transaction toàn batch, trả request và line versions |
| sp_AttendanceReceipt | Actor/Stamp, RequestId; lấy request lock rồi trả NotRecorded/StillProcessing/Recorded cùng lines |

Ghi: XACT_ABORT/TRY-CATCH/rollback; demand ADMIN → request applock → employee applock tăng dần theo mã → NHANVIEN/khóa kỳ → dòng công. Check nhân viên/ca/version và kỳ của mọi key; staging final set, kiểm overlap; apply theo tập, persist receipt rồi commit. Lookup nhân viên/ca trong transaction dùng HOLDLOCK để FK/state không đổi giữa chừng. Không giữ transaction khi người dùng nhập.

Trigger bảo vệ INSERT/UPDATE/DELETE khi có BANGLUONG của nhân viên/kỳ liên quan, không chỉ UI readonly. Procedure/trigger và T26 cùng dùng employee applock để chốt không lọt công mới; thứ tự tài nguyên nhất quán theo mã. DML trực tiếp runtime bị DENY. Nếu cần index cho truy cập theo MaNV/GioVao/GioRa thì thêm đúng phục vụ contract; không tuyên bố tối ưu benchmark T29.

Lỗi53201 input,53202 trạng thái/kỳ đã chốt,53203 overlap/version,53207 preflight; thông tin ca xung đột lấy qua query kiểm vấn đề sau lỗi, không parse raw SQL. Kiểm kỹ thuật clean/demo022→023, AlreadyApplied/checksum, invalid clone rollback và typed batch một dòng/đọc lại/quyền; ghi technical-checks riêng.

## 3. Bước2 — Models/DAL/BLL

Thêm Models/Requests/HumanResources/AttendanceRequests.cs, Dtos/HumanResources/AttendanceRow.cs, AttendanceBatchResult.cs và AttendanceConflict.cs; ngày/giờ ánh xạ typed DATE/datetime2, Revision byte[], TotalRows int. Giờ tổng do SQL trả, C# không chốt giờ lương.

Thêm DataAccess/Repositories/AttendanceRepository.cs dùng DataTable TVP một lời gọi, dispose connection, không trả reader/transaction lên UI; DBNull cho giờ nghỉ/vắng. Thêm Business/Services/AttendanceService.cs chuẩn hóa action/biên batch/ngày/precision giây; AppPermission.AttendanceManage ADMIN-only, revoke khi lỗi phiên51505, linked CTS có vòng đời rõ. Sửa PermissionService và SqlErrorMapper532xx; BLL không chứa MessageBox.

List đủ paging/tổng, filter phân biệt Đi làm hoàn tất/Chưa hoàn tất/Nghỉ/Vắng qua query; không xem giờ NULL là0 giờ đã làm. Kiểm kỹ thuật build và typed mapper/batch dưới runtime, chưa chạy nghiệm thu11 ID.

## 4. Bước3 — UI chấm công

Thêm Modules/HumanResources/AttendanceControl.cs, AttendanceEditorDialog.cs, AttendanceBatchDialog.cs; ghép tab vào HumanResourcesControl của T24, Program.cs truyền AttendanceService. Không thêm điểm vào CASH/STORE. List dùng base/stacked search với filter tháng/năm/nhân viên/trạng thái; ngày giờ có cả ngày ra, badge chưa hoàn tất/khóa kỳ bằng chữ.

Editor/batch modal qua OrbitDialogHost. Grid batch chỉ cột input được sửa; cột PK dòng đã lưu khóa. Dòng mới chọn nhân viên/ca; DateTime input nullable có checkbox rõ, không mặc định giờ thực theo ca. Chọn Nghỉ/Vắng khi đã nhập giờ phải xác nhận xóa giờ, không âm thầm đổi input; SQL vẫn chặn request mâu thuẫn.

Lưu batch nêu số dòng và toàn batch rollback khi một dòng lỗi; ErrorProvider/row error chỉ rõ dòng, giữ toàn nháp. Kỳ chốt chỉ đọc có lý do. Mở editor rồi kỳ vừa chốt: SQL từ chối và cho tải lại, không đổi expected để né lỗi.

Pending scoped G06 lưu payload typed/request trước ghi; Unknown khóa body/Lưu, Kiểm tra kết quả dùng pending request thay vì dòng đang chọn. Guard nháp/busy/unknown/session/dispose và callback muộn dùng mẫu đã sửa G05B07: không Cancel CTS đã dispose. Thành công reload giữ filter/selection theo key. OrbitField áp dụng chọn hết input/grid editor động; owner chuẩn, footer responsive125%.

## 5. Fixture và nghiệm thu sau duyệt đợt test

Thêm database/tests/g06/t25_*.sql và tests/CafeManager.Tests/HumanResources/AttendanceRulesTests.cs cho chuẩn hóa/biên có giá trị; không sao chép thuật toán SQL làm oracle. Probe so thời lượng seconds tính tay; UI test trên WinForms thật.

| ID | Dữ liệu/thao tác và đối chiếu |
|---|---|
| T25-K01 | Cùng ngày08–16=8 giờ; qua đêm22–06=8 giờ; có giây/rounding hiển thị; so SQL và DTO |
| T25-K02 | Ra có/vào NULL, ra=vào, ra<vào, ngày vào khác NgayLam, input dưới giây; từng subcase không ghi receipt |
| T25-K03 | Nghỉ/Vắng có giờ bị chặn; Đi làm thiếu cả giờ hoặc chỉ chưa ra được lưu và gắn nhãn; T26 phải chặn chốt |
| T25-K04 | Trùng PK, overlap chứa nhau/một phần/qua nửa đêm/interval mở, batch tự chồng; đều rollback |
| T25-K05 | Ca06–14 và14–22 được phép theo [begin,end); chồng1 giây bị chặn |
| T25-K06 | Barrier hai session ghi cùng nhân viên, request khác; giữ employee applock chứng minh tranh chấp; chỉ một success, transaction0/0 |
| T25-K07 | Nhân viên nghỉ/ca sai chặn Insert/Update; đọc lịch sử và xóa công sai kỳ chưa chốt đúng contract |
| T25-K08 | Kỳ seed/legacy và kỳ T26 mới chốt; Insert/Update/Delete qua app/runtime bị chặn; đổi key không lách khóa |
| T25-K09 | 31/10 22h→01/11 06h thuộc tháng10, tháng11 không cộng lại; năm9999 không overflow |
| T25-K10 | Filter kỳ/NV/status, applied keyword/pager/refresh/error/query muộn; ADMIN/CASH/STORE trực tiếp BLL/SQL; native input/select-all/owner |
| T25-K11 | Batch hai NV valid+invalid, hai NV đều valid, hai dòng cùng NV overlap, hai action cùng key; đối chiếu công/receipt không dữ liệu dở |

Subcase công so với chốt lương/hồ sơ đồng thời, timeout trước/sau commit, request mismatch/restart/pending I/O/DB scope, đóng/logout ghi tại ID liên quan và UX-K34; không đổi11 ID. Hai session SQL thật, DB test riêng; đối chiếu giờ/key/version/receipt/transaction. Hình thức SKIPPED, DPI100/150% NOT RUN; agent chỉ125%. Source cố định trong test, bug ghi task này và duyệt cả đợt trước sửa.

## 6. Đầu ra trước T26

SQL023 và service/UI đủ đọc/ghi công, báo thiếu giờ/overlap, khóa nhân viên/kỳ dùng được cho T26. Smoke kỹ thuật có bằng chứng, chưa PASS nghiệm thu. T26 dùng cùng khóa để đọc công/snapshot, không viết procedure công khác song song.

## 7. Phạm vi và checklist gốc

### T25 — Chấm công, ca qua đêm và chống chồng giờ

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Có nguồn giờ làm hợp lệ trước khi tính lương. |
| Phạm vi | CHAMCONG/CALAMVIEC; procedure lưu/chỉnh có kiểm soát, khóa nhân viên khi kiểm tra overlap. |
| Quy mô | M — quy tắc thời gian |
| Phụ thuộc | T24 |
| Đầu ra | Module chấm công theo nhân viên/kỳ, dữ liệu giờ có thể dùng chốt lương. |

**Kiểm thử bắt buộc:**

- [ ] `T25-K01` [I/U] Giờ vào/ra hợp lệ cùng ngày và qua đêm cho thời lượng đúng.
- [ ] `T25-K02` [I] Giờ ra không có giờ vào, ra≤vào, ngày vào khác NgayLam bị chặn.
- [ ] `T25-K03` [I] Nghỉ phép/Vắng có giờ bị chặn; đang làm chưa ra được lưu nhưng không tự tính thành kỳ hoàn tất.
- [ ] `T25-K04` [I] Cùng nhân viên trùng MaCa/NgayLam bị PK chặn; khác ca chồng khoảng giờ bị procedure chặn.
- [ ] `T25-K05` [I] Ca kết thúc đúng lúc ca sau bắt đầu được phép, không coi là overlap.
- [ ] `T25-K06` [I] Hai session cùng nhập ca chồng giờ không cùng vượt kiểm tra.
- [ ] `T25-K07` [I] Nhân viên đã nghỉ/ca không tồn tại bị chặn thao tác mới; dữ liệu cũ đọc được.
- [ ] `T25-K08` [I] Chỉnh chấm công kỳ đã chốt lương bị chặn theo quy trình.
- [ ] `T25-K09` [U/I] Phân kỳ theo ngày làm đã chọn, qua đêm cuối tháng không bị cộng hai lần.
- [ ] `T25-K10` [M] Bộ lọc tháng/nhân viên/trạng thái và reload đúng; quyền ADMIN, không quyền kế thừa từ chức vụ.
- [ ] `T25-K11` [I] Một batch nhiều nhân viên có dòng lỗi rollback theo phạm vi batch đã công bố.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.




