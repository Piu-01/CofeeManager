# G05 — Kho, nguồn chi và công nợ: requirements

Ngày soạn: 26/10/2026. Trạng thái: **Requirements nhóm đã được người dùng duyệt bằng “duyệt” ngày 26/10/2026**. Bộ plan T19–T23 tại [plans.md](plans.md) đã được duyệt cả bộ; source đã triển khai T19–T23, chờ duyệt đợt test riêng.

## 1. Mục tiêu và quyết định chuyển nhóm

Hoàn thiện luồng thu chi nền → nhập hàng/trả ngay/tạo nợ → xuất hủy/trả hàng → kiểm kê → trả nợ và tra số dư theo ngày cho một quán, một kho chung.

Ngày 26/10/2026, người dùng đồng ý chuyển sang G05 bằng “ok duyệt” sau khi xem cập nhật G04: 40 PASS / 0 FAIL / 16 BLOCKED; T18 đạt 8/8 ID. Đây là quyết định chuyển tiếp dựa trên phụ thuộc trực tiếp T18 → T19 đã đạt. G04 vẫn Đang làm; giữ nguyên 16 ca T16/T17 chưa đủ bằng chứng và toàn bộ lịch sử. Không coi quyết định này là nghiệm thu G04 hoặc duyệt trước requirements/bộ plan G05 chưa được trình.

Nguồn: [kế hoạch](../../KeHoach_QuanLiQuanCafe.md), [UX](../../UX_QuanLiQuanCafe.md), [báo cáo G04](../g04-ban-hang-vong-doi-hoa-don/report.md), [kết quả T18](../g04-ban-hang-vong-doi-hoa-don/t18-giao-hang/test-results.md).

## 2. Task con và đầu ra

| Thứ tự | Task | Đầu ra cần có | Phụ thuộc |
|---|---|---|---|
| 1 | T19 — Thu chi và liên kết nguồn chi hệ thống | Service thu chi, editor ghi tay, bảo vệ nguồn hệ thống, FK MaPN/MaBL và unique có điều kiện cho T20/T26 | T18 |
| 2 | T20 — Nhập kho, trả ngay và tạo nợ nguyên tử | Nháp bộ nhớ; chốt nhập/kho/chi/nợ nguyên tử; chứng từ chốt chỉ đọc | T19 |
| 3 | T21 — Xuất hủy và xuất trả nguyên liệu | Nháp xuất, chốt một lần, kiểm tồn và bảo vệ lịch sử | T20 |
| 4 | T22 — Kiểm kê và khóa điều chỉnh sau chốt | Snapshot có thời điểm, số đếm/lý do, chốt hôm nay một lần, khóa header/chi tiết | T21 |
| 5 | T23 — Công nợ, trả từng lần và số dư theo ngày | Tra nợ hiện tại/tại ngày, trả nợ có chi liên kết, lịch sử bất biến và đối chiếu timeout | T22 |

Triển khai tuần tự, một task tại một thời điểm. Phạm vi/đầu ra/phụ thuộc và 55 ca gốc được sao nguyên bản tại mục 6; bảng này chỉ là chỉ mục.

## 3. Yêu cầu nghiệp vụ và UX

- Thu chi phân biệt Ghi tay / Từ nghiệp vụ. Editor thủ công chỉ dùng danh mục được phép; chi trả ngay/trả nợ/lương do procedure nghiệp vụ tạo, có liên kết nguồn và chỉ đọc. Chỉ ADMIN thao tác tài chính. Chính sách sửa phiếu thường phải chốt trong plan T19; phiếu đã xác nhận không sửa/xóa để đảo tiền. Không thêm phiếu thu cho checkout hoặc cộng lịch sử trả nợ lần hai vào tổng chi.
- Nhập hàng: nháp local có nhãn Chưa ghi kho, hỏi khi bỏ nháp; mỗi NVL một dòng, chọn trùng focus dòng cũ. Lượng theo đơn vị chuẩn; tổng bằng tổng ROUND(lượng × giá theo đơn vị, 0) từng dòng. ADMIN trả ngay từ 0 đến tổng; STORE khóa trả ngay bằng 0 và hiển thị Thanh toán do quản lý xử lý; CASH không nhập. Chốt nguyên tử tạo kho, chi nếu trả ngay > 0, nợ nếu còn > 0; không chi/nợ 0. Sau chốt khóa nguồn NCC/ngày/chi tiết, xem lại chi/nợ theo quyền.
- Xuất hàng: chỉ Hủy do hỏng hoặc Trả hàng; không xuất ra quầy/chuyển kho. Thiếu một dòng rollback toàn phiếu. Xuất trả nêu rõ chưa hoàn tiền/cấn nợ. Đồ đã pha nhưng hủy đơn ghi xuất hủy lượng thực dùng, không đồng thời ghi tiêu hao bán cùng lượng. STORE/ADMIN được thao tác, CASH bị chặn.
- Kiểm kê: lấy thời điểm/số liệu snapshot, nhập đủ số đếm theo phạm vi phiếu; ô chưa đếm để trống, không mặc định 0. Lệch cần lý do; chỉ chốt hôm nay, phiếu chưa chốt và snapshot còn khớp. Kho đổi phải lấy/đếm lại có xác nhận, không ghi đè biến động mới. Không giữ khóa DB khi đếm. Chốt xong khóa header/chi tiết và cờ điều chỉnh. STORE/ADMIN được, CASH bị chặn.
- Nợ NCC lấy nguồn từ phiếu nhập; một PN tối đa một nợ gốc. Trả ngay đã giảm nợ gốc không tính lại như lần trả nợ. ADMIN trả một phần/nhiều lần trong số dư, SQL tạo chi và lịch sử trong cùng transaction; STORE chỉ xem, CASH không truy cập. Hiện tại dùng view; tại ngày dùng lịch sử đến cutoff, không lọc view hiện tại để giả số dư quá khứ.
- Mọi giao dịch ghi có mã nguồn/request ổn định, chặn gửi lặp, truy vấn đối chiếu sau timeout; UI có Kiểm tra kết quả và giữ input/mã khi kết quả chưa biết. Không retry mù hay tự đổi số tiền theo số dư mới. Chỉ hiển thị đã chốt từ DTO/DB.
- Tồn đọc từ DB, không sửa trực tiếp sau mở sổ hoặc cộng/trừ lại ở UI. Tra tồn và chứng từ theo role; CASH chỉ đọc tồn phục vụ bán, không thấy giá nhập/nợ. STORE có điểm vào kho đúng UX, không dùng dashboard quản trị giả; tổng quan ADMIN hoàn chỉnh vẫn thuộc T28.
- Loading/rỗng/lỗi/xung đột/chưa rõ/đã chốt tách biệt; query async, giữ input khi lỗi, không giả tổng 0 khi DB lỗi. Enter không tự chốt tiền/kho/nợ; modal/loading chặn gửi lặp. Quyền kiểm lại tại BLL. Tiền/ngày/giờ theo guide, truyền dữ liệu typed.

Truy vết UX: mục 2–5, 11–12, 15–17; UX-K24–K32 và UX-K39–K42. Giữ nguyên checklist UX và T01–T32; subcase không thay ca gốc.

## 4. Giả định, giới hạn và quyết định cần chốt trong plan

- Giữ WinForms net10.0-windows, Models/BLL/DAL SqlClient; SQL sở hữu transaction nhiều bảng. Tiền decimal(18,0), lượng decimal(18,4), C# decimal; không thêm EF/Dapper/API/generic repository. Tận dụng trigger cập nhật tồn/tổng hiện có, không cập nhật lặp.
- Schema016 là mốc được ghi nhận từ G04, cần xác minh source/migrations và DB thực tế khi lập plan/triển khai. Procedure ghi bổ sung/sửa trong kế hoạch là contract đích, không mặc định đã có. Đọc baseline DB_Schema.sql, Insert_Data.sql và migrations liên quan trước chốt contract. Không chạy seed lại trên DB đã dùng.
- Bộ plan phải chốt mã danh mục hệ thống đúng seed, chính sách phiếu thường, nguồn FK/unique MaPN/MaBL, TVP/DTO/error map, phân quyền runtime, request/replay và tra kết quả sau timeout. T19 chỉ chuẩn bị nguồn lương cần thiết; nghiệp vụ chốt/chi lương vẫn ở T26, không viết module tương lai. Phần nghiệm thu nguồn MaBL phân biệt fixture với tích hợp T26 chưa chạy.
- Plan chốt migration/backfill chứng từ cũ, kiểm tra dữ liệu không hợp lệ và đường nâng cấp bảo toàn lịch sử/tồn/tiền/nợ, không tự hợp thức hóa dữ liệu sai. Chốt thứ tự khóa tương thích checkout schema hiện hành và chiến lược snapshot kiểm kê. Thiếu contract/đầu ra chặn lớp sau phải báo.
- Trước thiết kế/code UI đọc đầy đủ docs/ui/ theo README; tái sử dụng AppTheme/control hiện có. Người dùng tự kiểm hình thức; agent ghi SKIPPED theo phân công. Hỗ trợ DPI100/125/150%, 1280×720 và 1366×768; agent chỉ kiểm chức năng/layout kỹ thuật ở125%, không đổi Windows Scale. DPI100/150% chưa chạy ghi NOT RUN.
- Ngoài phạm vi: nhiều kho/chi nhánh, chuyển quầy, hoàn tiền/cấn nợ tự động do xuất trả, thanh toán online, offline sync, đổi điểm, kế toán giá vốn/lợi nhuận chính xác, module lương T26, tài sản T27, báo cáo tổng hợp T28 và publish cuối T32. G05 vẫn có tổng thu chi và số dư nợ theo ngày trong phạm vi task gốc.
- Không có câu hỏi nghiệp vụ bắt buộc để trình requirements hiện tại. Chi tiết kỹ thuật sẽ được chứng minh từ source/SQL khi lập bộ plan; phát sinh đổi phạm vi/contract ngoài yêu cầu phải trình duyệt bổ sung.

## 5. Kiểm chứng và tiêu chí hoàn tất nhóm

1. Duyệt requirements một lần, sau đó viết đủ plan T19–T23 và plans.md; duyệt cả bộ trước code. Đợt test nhóm cần duyệt riêng sau triển khai. Chưa tạo plan/code hoặc chạy test trong bước requirements.
2. Kiểm trên SQL Server thật, DB test riêng với manifest/marker/principal và cleanup allowlist. Đối chiếu header/chi tiết/tồn/chi/nợ/lịch sử/transaction sau lỗi; dùng hai session cho concurrency. Không ghi fixture vào development hoặc DB sử dụng.
3. Bộ test giữ source/migration cố định từng lượt; unit xUnit cho quy tắc có giá trị, chức năng WinForms thật cho binding/validation/quyền/handler/timeout. Lưu test-results.md từng task với môi trường/schema/hash, input/lệnh, expected/actual và bằng chứng; báo cáo chung report.md kể cả FAIL/BLOCKED/NOT RUN. Bug ghi tại task sở hữu, duyệt cả đợt trước debug/regression.
4. Regression theo ảnh hưởng: checkout tranh tồn với xuất/kiểm kê, tổng thu chi không nhân doanh thu/chi nguồn, bảo toàn số dư sau nâng schema, quyền/menu/điểm vào STORE và trạng thái UI dùng chung. Ghi rõ 16 ca T16/T17 còn thiếu của G04, không tự kế thừa thành PASS hoặc mở rộng thành chạy lại toàn G04.
5. Bổ sung kịch bản dùng thử vào đường chạy development hiện có; xác minh profile/exe/DB/schema, nâng cấp có preflight/backup và bảo toàn tài khoản/dữ liệu. Không commit/push khi chưa được giao; câu lưu commit trong checklist không tự cấp quyền commit.
6. Chỉ ghi Hoàn tất G05 khi đủ đầu ra và 55 ca bắt buộc đạt theo phân công đã chốt, không còn lỗi trọng yếu; ngoại lệ nếu có ghi riêng theo quyết định người dùng, không đổi actual thành PASS. Giữ nguyên325ID/T01–T32 và lịch sử các nhóm.

## 6. Phạm vi, đầu ra và ca nghiệm thu gốc T19–T23

Sao nguyên bản từ kế hoạch: **55 ID = 10 + 12 + 10 + 11 + 12**. Không rút gọn nội dung ca gốc hoặc thay đổi phụ thuộc/điều kiện hoàn tất.

### T19 — Thu chi và liên kết nguồn chi hệ thống

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Tạo nền đối soát tiền trước nhập hàng và chi lương. |
| Phạm vi | PHIEUTHUCHI/DANHMUC_THUCHI; migration FK nguồn MaPN/MaBL và unique có điều kiện; editor chi thường. |
| Quy mô | M — tài chính nền |
| Phụ thuộc | T18 |
| Đầu ra | Service thu chi, loại hệ thống bị bảo vệ, FK nguồn sẵn cho T20/T26. |

**Kiểm thử bắt buộc:**

- [ ] `T19-K01` [I/M] Phiếu Thu/Chi thông thường số tiền dương, ngày/nhân viên/danh mục hợp lệ lưu đúng.
- [ ] `T19-K02` [I] Tiền 0/âm/quá precision, tên/ghi chú quá dài và FK nguồn sai bị chặn.
- [ ] `T19-K03` [I] MaPN/MaBL FK hợp lệ; hai phiếu chi cho cùng nguồn trả ngay/lương bị unique chặn.
- [ ] `T19-K04` [I] Phiếu nguồn hệ thống phải loại Chi đúng, không thể đổi thành Thu.
- [ ] `T19-K05` [U/M] UI thủ công không chọn danh mục trả nợ/trả ngay/lương hệ thống; chỉ ADMIN ghi chi.
- [ ] `T19-K06` [I] Nguồn trùng và lỗi giữa lưu phiếu/liên kết rollback, không phiếu rác.
- [ ] `T19-K07` [I/M] Chính sách sửa chi thường rõ: bản đã xác nhận không xóa/sửa tiền âm để đảo; lịch sử chi nguồn bất biến.
- [ ] `T19-K08` [I] Tổng Thu/Chi theo ngày không cộng các dòng lịch sử trả nợ thêm lần nữa.
- [ ] `T19-K09` [I/M] Bán hàng không đồng thời tạo phiếu thu rồi nhân đôi nguồn doanh thu.
- [ ] `T19-K10` [M] Tra cứu/source link và reload hiển thị đủ loại tiền/ghi chú/mã nguồn.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

### T20 — Nhập kho, trả ngay và tạo nợ nguyên tử

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Lưu một phiếu nhập hoàn chỉnh thay cho ghi rời từng dòng trong app. |
| Phạm vi | Procedure sp_LapPhieuNhap đề xuất, TVP chi tiết, cờ chốt/khóa phiếu; FK chi trả ngay và CONGNO_NCC. |
| Quy mô | L — hàng và tiền nhiều bảng |
| Phụ thuộc | T19 |
| Đầu ra | Module nhập với nháp bộ nhớ; transaction nhập/chi/nợ và bảo vệ chứng từ sau chốt. |

**Kiểm thử bắt buộc:**

- [ ] `T20-K01` [M/I] Soạn nháp/đóng dialog không đổi kho; chốt n dòng cộng đúng tồn và tổng dòng làm tròn.
- [ ] `T20-K02` [I] NCC/nhân viên sai, lượng 0/âm, giá 0/âm, NVL trùng cùng phiếu bị từ chối.
- [ ] `T20-K03` [I] Một dòng lỗi/thiếu quyền rollback header, mọi chi tiết, kho, chi và nợ.
- [ ] `T20-K04` [I] Trả ngay 0 tạo nợ toàn bộ, không chi 0; trả đủ tạo một chi, không nợ 0.
- [ ] `T20-K05` [I] Trả ngay một phần: chi trả ngay đúng phần, nợ đúng tổng trừ phần đã trả.
- [ ] `T20-K06` [I] Trả ngay âm/vượt tổng hoặc dòng chi sai loại bị chặn.
- [ ] `T20-K07` [I] Nhấn/gọi lại cùng yêu cầu không nhập kho/chi lần hai; đối chiếu theo mã nguồn khi timeout.
- [ ] `T20-K08` [I] Phiếu chốt không sửa/xóa header/NCC/ngày/chi tiết; trigger cũ không cho bypass sửa lịch sử.
- [ ] `T20-K09` [I] Baseline có seed được backfill cờ chốt đúng, không chạy trigger cộng tồn lần nữa.
- [ ] `T20-K10` [I] Hai phiên nhập cùng NVL cập nhật tồn đúng, không lost update.
- [ ] `T20-K11` [U/M] STORE nhập được, CASH không; trả ngay/ghi tài chính chỉ ADMIN hoặc quy trình được cấp quyền rõ.
- [ ] `T20-K12` [M] Tính tổng preview khớp SQL và sau reload nguồn chi/nợ hiển thị đúng.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

### T21 — Xuất hủy và xuất trả nguyên liệu

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Ghi giảm tồn có chứng từ, tách khỏi bán hàng và chuyển ra quầy. |
| Phạm vi | Procedure sp_LapPhieuXuat đề xuất, TVP, cờ chốt và khóa chứng từ; không tự hoàn/cấn trừ tiền trả hàng. |
| Quy mô | M — transaction kho |
| Phụ thuộc | T20 |
| Đầu ra | Nháp xuất và chốt nguyên tử; lịch sử bất biến. |

**Kiểm thử bắt buộc:**

- [ ] `T21-K01` [M/I] Nháp không trừ tồn; lưu Huy do hong/Tra hang giảm đúng nhiều nguyên liệu.
- [ ] `T21-K02` [I] LoaiXuat=Xuat ra quay hoặc loại lạ bị chặn theo mô hình một kho.
- [ ] `T21-K03` [I] Lượng 0/âm/quá precision, NVL/FK sai, trùng NVL bị chặn.
- [ ] `T21-K04` [I] Xuất bằng tồn đưa tồn 0 hợp lệ; hơn tồn rollback toàn chứng từ.
- [ ] `T21-K05` [I] Một dòng thiếu kho sau dòng khác hợp lệ không để dòng đầu bị trừ dở.
- [ ] `T21-K06` [I] Hai xuất/checkout tranh lượng tồn cuối: không âm, chỉ các giao dịch đủ tồn commit.
- [ ] `T21-K07` [I] Chốt lại/retry không trừ lần hai; sửa/xóa chứng từ chốt bị chặn.
- [ ] `T21-K08` [M/I] Xuất hủy cho đồ đã pha nhưng hủy đơn không đồng thời ghi tiêu hao bán cùng lượng.
- [ ] `T21-K09` [M] Xuất trả hiển thị rõ chưa tự hoàn tiền/cấn trừ nợ.
- [ ] `T21-K10` [U/M] STORE/ADMIN thao tác được, CASH bị chặn; reload đúng lịch sử/tồn.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

### T22 — Kiểm kê và khóa điều chỉnh sau chốt

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Hoàn thiện cả header và chi tiết để kiểm kê thực sự chốt một lần. |
| Phạm vi | sp_ChotKiemKe hiện có, sửa bảo vệ header, màn hình snapshot/số đếm/lý do. |
| Quy mô | M — điều chỉnh tồn có kiểm soát |
| Phụ thuộc | T21 |
| Đầu ra | Phiếu kiểm kê có thời điểm/snapshot, chốt một lần và lịch sử đọc lại. |

**Kiểm thử bắt buộc:**

- [ ] `T22-K01` [I/M] Tạo snapshot đúng tồn tại thời điểm lấy, chưa chốt chưa đổi tồn.
- [ ] `T22-K02` [I] Số đếm 0/bằng tồn/khác tồn hợp lệ; âm/quá precision/NVL trùng bị chặn.
- [ ] `T22-K03` [I/M] Khác tồn yêu cầu lý do, thiếu lý do bị từ chối.
- [ ] `T22-K04` [I] Hôm nay/chưa chốt/snapshot khớp: điều chỉnh đúng một lần.
- [ ] `T22-K05` [I] Chốt lại, reset DaDieuChinh, đổi ngày/nhân viên, sửa/xóa chi tiết sau chốt bị chặn.
- [ ] `T22-K06` [I] Phiếu ngày cũ/ngày tương lai/rỗng không chốt; không tự sửa ngày để né điều kiện.
- [ ] `T22-K07` [I] Phát sinh kho sau snapshot: từ chối, không ghi đè tồn mới bằng số cũ.
- [ ] `T22-K08` [I] Nhiều NVL một dòng snapshot lệch: toàn bộ rollback.
- [ ] `T22-K09` [I] Hai session chốt cùng phiếu/checkout song song được tuần tự hóa hoặc từ chối hợp lệ; không mất biến động kho.
- [ ] `T22-K10` [M] Không giữ khóa DB trong thời gian người dùng đếm; UI nêu cần tạm dừng phát sinh để chốt.
- [ ] `T22-K11` [U/M] STORE/ADMIN được; CASH không; reload trạng thái và chênh lệch đúng.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

### T23 — Công nợ, trả từng lần và số dư theo ngày

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Sử dụng view/procedure hiện có, kiểm soát ngày nguồn và lịch sử chi. |
| Phạm vi | CONGNO_NCC/THANHTOAN_CONGNO/vw_CongNoNCC, ALTER procedure và báo cáo theo ngày; nhập nợ ở T20. |
| Quy mô | L — tiền nguồn và lịch sử |
| Phụ thuộc | T22 |
| Đầu ra | Module công nợ, procedure trả nợ hợp lệ, nguồn ngày cho báo cáo. |

**Kiểm thử bắt buộc:**

- [ ] `T23-K01` [I] NCC của nợ đúng NCC phiếu nhập và không đổi sau chốt; một PN một nợ.
- [ ] `T23-K02` [I] Nợ vượt tổng nhập hoặc nợ 0 bị chặn; hạn NULL hợp lệ.
- [ ] `T23-K03` [I] Trả một phần/nhiều lần/cuối cùng: đã trả/còn nợ khớp, trạng thái đúng.
- [ ] `T23-K04` [I] Trả 0/âm/vượt còn nợ/phương thức sai bị chặn và không phiếu chi rác.
- [ ] `T23-K05` [I] Ngày trả trước ngày nguồn nhập bị chặn; ngày hợp lệ lưu đúng.
- [ ] `T23-K06` [I] TC011 thiếu/sai loại phải lỗi rõ hoặc khởi tạo trước, không chọn nhầm loại Thu.
- [ ] `T23-K07` [I] Hai session trả lượng nợ cuối: không trả vượt; mã phiếu chi không trùng.
- [ ] `T23-K08` [I] UPDATE/DELETE nợ có trả/lịch sử/phiếu chi liên quan bị chặn; đổi nguồn không làm chuyển NCC.
- [ ] `T23-K09` [I] View quá hạn theo thời điểm hiện tại; báo cáo ngày X bỏ lần trả sau X.
- [ ] `T23-K10` [I] Phiếu trả ngay khi nhập không đồng thời tính là trả nợ gốc đã giảm.
- [ ] `T23-K11` [U/M] ADMIN trả được, STORE chỉ xem, CASH không; timeout đọc lại mã chi không retry mù.
- [ ] `T23-K12` [M] Reload xem được mọi lần trả, phương thức và link nguồn phiếu chi.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.


