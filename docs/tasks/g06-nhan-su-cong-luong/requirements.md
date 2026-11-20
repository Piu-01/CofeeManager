# G06 — Nhân sự, công và lương: requirements

Ngày soạn: 30/10/2026. Trạng thái: **Requirements nhóm đã được người dùng duyệt bằng “duyệt” ngày 30/10/2026**. Bộ plan T24–T26 tại [plans.md](plans.md) đã được duyệt bằng “duyệt” ngày30/10/2026; đã triển khai source, chờ duyệt đợt nghiệm thu G06 riêng.

## 1. Mục tiêu và bối cảnh chuyển nhóm

Hoàn thiện quản lý nhân viên/tài khoản → chấm công hợp lệ → chốt snapshot bảng lương → chi lương một lần, cho một quán cafe bằng WinForms .NET 10 và SQL Server.

Người dùng yêu cầu “triểnkhai g06” ngày 30/10/2026: ghi nhận chỉ đạo bắt đầu quy trình G06. G05 vẫn Đang làm, báo cáo hiện tại đối chiếu 44/55 ID đủ bằng chứng PASS và 11 ID chưa đủ; không coi chuyển nhóm là nghiệm thu G05. T23 đã có đầu ra source/migration021 và regression trả nợ/link chi; khi lập plan cần xác minh nguồn chi MaBL của T19 và đầu ra T23 có đủ cho triển khai. Chỉ báo blocker nếu thiếu đầu ra thực sự chặn G06; không tự đổi kết quả các nhóm trước.

Nguồn: [kế hoạch mục 8, 11–13 và T24–T26](../../KeHoach_QuanLiQuanCafe.md), [UX mục 6, 13, 15–18](../../UX_QuanLiQuanCafe.md), [báo cáo G05](../g05-kho-nguon-chi-cong-no/report.md). Phạm vi gốc và toàn bộ 33 ca nghiệm thu được sao nguyên bản tại mục 6.

## 2. Task con, thứ tự và đầu ra

| Thứ tự | Task | Đầu ra | Phụ thuộc |
|---|---|---|---|
| 1 | T24 — Nhân viên, chức vụ và quản lý tài khoản | Module nhân sự/account cho ADMIN; tạo/sửa hồ sơ, nghỉ việc, cấp/reset/khóa tài khoản, giữ lịch sử | T23 |
| 2 | T25 — Chấm công, ca qua đêm và chống chồng giờ | Module công theo nhân viên/kỳ; lưu/chỉnh qua procedure, dữ liệu giờ hợp lệ và bảo vệ kỳ chốt | T24 |
| 3 | T26 — Chốt bảng lương và chi lương một lần | Preview SQL, snapshot kỳ chốt, chi toàn bộ một lần và liên kết phiếu chi | T25 |

Một task con tại một thời điểm, triển khai T24 → T25 → T26. Không tạo requirements riêng cho task con; duyệt đủ bộ plan một lần trước code.

## 3. Phạm vi nghiệp vụ và UX

### T24 — Hồ sơ và tài khoản

- Quản lý NHANVIEN/CHUCVU/TAIKHOAN/VAITRO trong phạm vi kế hoạch; tái sử dụng danh mục chức vụ/vai trò đã có, không tạo hệ thống quyền khác. Hồ sơ full-time/part-time có mức lương, chức vụ, trạng thái; kiểm tên, độ dài, lương, hợp đồng và FK. SDT trống chuẩn hóa NULL; số thực không trùng.
- Chức vụ độc lập vai trò ADMIN/CASH/STORE. Chỉ ADMIN quản lý hồ sơ/tài khoản và đổi role; BLL kiểm quyền khi gọi trực tiếp, không chỉ ẩn menu.
- Một nhân viên một tài khoản, username không trùng; cấp/reset mật khẩu dùng PasswordHasher hiện có, không đưa mật khẩu/hash vào grid, session hoặc log. Reset làm mật khẩu cũ không còn đăng nhập được.
- Nghỉ việc có xác nhận, khóa tài khoản và giữ mọi lịch sử. Không khóa/xóa/giáng quyền làm mất ADMIN hoạt động cuối cùng; bảo vệ cả cạnh tranh hai session. Không mở lại bootstrap để reset tùy ý.
- Thay đổi tài khoản/quyền được kiểm lại theo cơ chế phiên hiện có: chặn thao tác mới khi phiên không còn hợp lệ, không giả vờ rollback giao dịch SQL đang xử lý. Đổi lương không sửa bảng lương đã snapshot. Xóa hồ sơ có tham chiếu phải bị chặn, không cascade lịch sử.

### T25 — Chấm công

- Chọn kỳ/nhân viên/trạng thái; hiển thị ngày làm, ca, trạng thái, giờ vào/ra đầy đủ ngày. Ca qua đêm hiển thị cả ngày ra, phân kỳ bằng NgayLam, không đếm hai lần cuối tháng.
- Giờ ra yêu cầu có giờ vào, ra > vào; ngày vào khớp ngày làm. Nghỉ phép/Vắng không có giờ. Đang làm chưa ra được lưu và gắn Chưa hoàn tất; không tự điền giờ ca mẫu thành giờ thực hoặc coi thiếu giờ là 0 để chốt lương.
- Procedure khóa theo nhân viên để kiểm overlap cả khi hai session nhập; ca nối tiếp đúng điểm cuối/đầu được phép. Không ghi trùng nhân viên/ca/ngày. Ca không tồn tại hoặc nhân viên đã nghỉ bị chặn thao tác mới; dữ liệu cũ vẫn đọc được.
- Kỳ đã chốt lương khóa chỉnh công theo phạm vi nhân viên/kỳ. Batch phải có phạm vi rollback công bố rõ trong bộ plan, một dòng lỗi không để dữ liệu dở.

### T26 — Chốt và chi lương

- Tải giờ hợp lệ/vấn đề của kỳ; giải quyết ca thiếu/overlap trước chốt. ADMIN duyệt giờ thường, giờ và tiền tăng ca, phụ cấp, thưởng, khấu trừ. Không tự suy chính sách tăng ca từ ca mẫu; giờ tăng ca không đồng thời tính như giờ thường.
- Preview và kết quả chính thức tính ở SQL. Full-time = lương tháng + phụ cấp/tăng ca/thưởng − khấu trừ; part-time = giờ thường × giá giờ + các khoản còn lại. Giữ ThucLinh computed, tiền làm tròn theo contract SQL; chặn kỳ sai, số âm, khấu trừ vượt tổng.
- Chốt lưu snapshot mức lương, hợp đồng, giờ và khoản duyệt; unique nhân viên/kỳ, không UPDATE/DELETE snapshot đã chốt. Đổi hồ sơ không thay kỳ cũ. Preview phải được kiểm lại khi chốt để tránh chốt số liệu đã thay đổi.
- Đã chốt/Chưa chi và Đã chi là hai trạng thái riêng. Chi toàn bộ thực lĩnh qua procedure, tạo đúng một phiếu chi gắn MaBL và liên kết nguồn nguyên tử. Không chi lần hai; lỗi giữa ghi/liên kết rollback; không tự ghi thêm phiếu ở C# hoặc editor thu chi.
- Thực lĩnh 0 hiển thị Không phát sinh chi, không tạo phiếu SoTien=0. Không tạm ứng/chia đợt. Dữ liệu lương và phiếu chi không được cộng hai lần vào dòng tiền.
- Chốt/chi chặn gửi lặp, có mã nguồn/request ổn định và đường đối chiếu sau timeout. Giữ input/mã khi chưa rõ kết quả; chỉ retry sau đối chiếu theo contract, không suy timeout là rollback.

### Hành vi UI dùng chung

- MainForm + UserControl; editor/xác nhận dùng Form modal Windows có owner đúng chuỗi, titlebar/nút X native, ShowInTaskbar=false. Giữ selection/filter/input/focus khi quay lại; dispose đúng và guard nháp/busy/unknown/session.
- Đọc đầy đủ bộ docs/ui/ theo README trước thiết kế/code UI; tái sử dụng AppTheme, control Orbit, cơ chế dialog và OrbitField.SelectAllOnEntry. Mọi ô văn bản/số sửa được chọn hết khi mới vào bằng click/Tab, kể cả editor grid; click tiếp trong cùng focus hoạt động bình thường.
- Query async, bỏ kết quả trả muộn, tách loading/rỗng/lỗi/xung đột/chưa biết/đã chốt. Lỗi DB không thành tổng 0; lỗi input giữ dữ liệu và chỉ rõ trường. Enter không tự chốt lương/chi; modal/busy chặn gửi lặp.
- Tiền căn phải, dạng 81.000 ₫; ngày dd/MM/yyyy, giờ HH:mm; SQL nhận typed values. Chứng từ chốt chỉ đọc và xem được phiếu chi nguồn theo quyền.

Truy vết UX: UX16–UX18, UX-K01–K04, UX-K34–K36 và trạng thái/contract chung tại mục 15–17. Giữ checklist gốc và subcase có expected/actual riêng.

## 4. Giả định và các quyết định cần chốt trong bộ plan

- Giữ kiến trúc Models → DAL/BLL → WinForms theo dự án; SQL sở hữu transaction nhiều bảng, C# không bọc transaction dư. Không EF/Dapper/API/generic repository. Hash mật khẩu bằng Microsoft.Extensions.Identity.Core.
- Migrations source hiện có 001–021; đây chưa là xác minh schema DB thực tế. Trước chốt contract phải đọc DB_Schema.sql, Insert_Data.sql, migrations auth/bootstrap/danh mục/nguồn chi và source phiên hiện hành. Procedure đích không mặc định đã tồn tại.
- Plan phải chốt migration/backfill nhân sự/công/lương cũ, quy tắc bảo toàn lịch sử và xử lý dữ liệu không hợp lệ; không sửa seed hay hợp thức hóa dữ liệu sai để né ràng buộc. Xác minh clean/demo/upgrade và quyền runtime.
- Chốt DTO/TVP/error map, thứ tự khóa nhân viên/account/kỳ/chi, version/xung đột, bảo vệ ADMIN cuối, phạm vi khóa kỳ, batch rollback và quyền trực tiếp tại BLL/SQL theo kiến trúc hiện có.
- Chốt precision giờ, cách đối chiếu tổng giờ/giờ duyệt, khoản tăng ca, làm tròn, preview fingerprint/version và xử lý dữ liệu công chưa hoàn tất. Nếu cần quyết định nghiệp vụ vượt kế hoạch thì trình phần phát sinh trước code.
- Chốt mã danh mục chi lương từ dữ liệu thực, MaBL/FK/unique, cấp mã, request/replay/đối chiếu chốt và chi, đường mở nguồn từ thu chi, đồng thời và timeout trước/sau commit. Không coi fixture MaBL của G05 là tích hợp T26 đã PASS.
- Ngoài phạm vi: tạm ứng, trả lương nhiều đợt, bảo hiểm/thuế/quy trình HR mới, tự suy hệ số tăng ca, tài sản T27, báo cáo tổng hợp T28, tối ưu benchmark T29, vận hành/publish G08. Không mở rộng thành sửa hết ca còn thiếu của G04/G05.
- Hỗ trợ 1280×720, 1366×768 và DPI100/125/150%; agent chỉ kiểm chức năng/layout kỹ thuật125%, không đổi Windows Scale. Hình thức do người dùng tự kiểm, ghi SKIPPED theo phân công; DPI100/150% chưa chạy ghi NOT RUN.

## 5. Kiểm chứng và tiêu chí hoàn tất

1. Duyệt requirements nhóm → viết đủ plan T24–T26 và plans.md → duyệt cả bộ một lần → triển khai tuần tự. Sau triển khai chờ duyệt đợt test riêng. Không tự coi yêu cầu bắt đầu G06 là duyệt tài liệu chưa trình.
2. Khi triển khai SQL → DAL → BLL → UI; chỉ build/compile SQL/smoke kỹ thuật tối thiểu trong phạm vi duyệt, ghi riêng technical-checks. Không đánh dấu ca nghiệm thu từ build hoặc đọc source.
3. Test giao dịch trên SQL Server thật với DB test riêng, không ghi fixture vào DB đang dùng; hai session cho overlap/chốt/chi/admin cuối. Đối chiếu tài khoản, công, snapshot, tiền, phiếu chi, liên kết và transaction sau lỗi; không chỉ xem thông báo.
4. xUnit cho quy tắc có giá trị, WinForms thật cho binding/validation/handler/role/session/keyboard/loading/unknown. Source/migration cố định trong mỗi lượt test. Lưu test-results.md từng task với schema/hash, môi trường, dữ liệu, lệnh/thao tác, expected/actual, PASS/FAIL/BLOCKED/NOT RUN và bằng chứng.
5. Báo cáo chung report.md, giữ đủ 33 ID = 10 + 11 + 12. Bug ở task sở hữu, duyệt cả đợt trước debug/regression. Giữ lịch sử325ID/T01–T32 và các ca chưa đủ của nhóm trước. Regression theo ảnh hưởng auth/session, danh mục, thu chi/nguồn MaBL và tính bất biến dữ liệu.
6. Chỉ ghi Hoàn tất khi đủ đầu ra và ca bắt buộc đã PASS theo phân công; chưa đủ bằng chứng giữ Đang làm, ghi ngoại lệ nếu người dùng quyết định riêng. Không tự commit/push hoặc nâng DB development khi chưa có phạm vi thao tác phù hợp; chuẩn bị hướng dẫn triển khai review được.

## 6. Phạm vi, đầu ra và ca nghiệm thu gốc T24–T26

Sao nguyên bản từ kế hoạch, giữ đủ **33 ID** và điều kiện hoàn tất.
### T24 — Nhân viên, chức vụ và quản lý tài khoản

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Hoàn thiện hồ sơ nhân sự và quyền quản trị tài khoản. |
| Phạm vi | NHANVIEN/CHUCVU/TAIKHOAN/VAITRO; thay lương, nghỉ việc, tạo/reset/khóa tài khoản; chưa chấm công/lương. |
| Quy mô | M — hồ sơ có vòng đời |
| Phụ thuộc | T23 |
| Đầu ra | Module nhân sự/account; ADMIN quản lý được, giữ lịch sử nguồn. |

**Kiểm thử bắt buộc:**

- [ ] `T24-K01` [I/M] Tạo/sửa full-time/part-time đúng mức lương và chức vụ; tên/SDT/FK hợp lệ.
- [ ] `T24-K02` [I] Hợp đồng sai, lương âm, tên rỗng/quá dài, SDT thực trùng bị chặn; nhiều NULL hợp lệ.
- [ ] `T24-K03` [I] Một nhân viên một tài khoản theo unique; username trùng bị chặn.
- [ ] `T24-K04` [M/U] Reset password ghi hash mới, mật khẩu cũ không login; hash/pwd không lộ trên grid/log.
- [ ] `T24-K05` [I] Nhân viên nghỉ khóa account, không xóa lịch sử hóa đơn/chấm công.
- [ ] `T24-K06` [I/M] Không khóa/xóa admin hoạt động cuối cùng; bootstrap không bị mở lại để reset tùy ý.
- [ ] `T24-K07` [I] Đổi chức vụ không tự đổi vai trò; đổi vai trò chỉ thao tác được cấp quyền.
- [ ] `T24-K08` [I] Đổi mức lương không sửa snapshot BANGLUONG kỳ cũ.
- [ ] `T24-K09` [I] Xóa nhân viên đã tham chiếu bị chặn; employee editor không tạo cascade delete.
- [ ] `T24-K10` [U/M] CASH/STORE gọi service quản lý nhân sự/account bị từ chối; reload hồ sơ đúng.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

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



