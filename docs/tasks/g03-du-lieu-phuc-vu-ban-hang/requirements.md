# G03 — Dữ liệu phục vụ bán hàng

**Trạng thái:** PASS nghiệm thu theo quyết định người dùng ngày20/10/2026, với ngoại lệ kiểm thử ghi tại [acceptance-decision.md](acceptance-decision.md). Không xác nhận 47/47 ca kỹ thuật đã chạy PASS.
**Duyệt requirements:** Người dùng “duyệt” ngày 18/10/2026 sau khi nhận requirements G03.
**Ngày:** 18/10/2026.
**Yêu cầu:** Người dùng “tiếp tục làm g03”.
**Nguồn chuẩn:** `docs/KeHoach_QuanLiQuanCafe.md` phiên bản 3.3, mục 6, 10–13; `docs/UX_QuanLiQuanCafe.md` mục 3, 5, 10, 11.1, 15–18; bộ UI Orbit tại `docs/ui/`.

## 1. Mục tiêu và thứ tự

Chuẩn bị dữ liệu thật phục vụ bán hàng: nguyên liệu/tồn mở sổ, sản phẩm/công thức, bàn/khu vực, khách/hạng, khuyến mãi và lịch sử điểm. Một task con tại một thời điểm: T09 → T10 → T11 → T12 → T13, phụ thuộc đầu vào T08.

G02 có nghiệm thu lịch sử 27/27 ID gốc ngày 17/10/2026. Các thay đổi Orbit/config sau đó có giới hạn riêng theo mục 12.1 kế hoạch và report G02; không suy ra chúng đã được nghiệm thu từ PASS cũ. Khi lập plan phải đối chiếu source/schema hiện tại và phần dùng lại, ghi blocker nếu thực sự chặn G03.

## 2. Phạm vi và hành vi chung

- T09: quản trị nguyên liệu/đơn vị, đọc tồn từ SQL, mở sổ ADMIN một lần có nguồn/mốc và transaction. Nguyên liệu mới tồn 0; không cho sửa tồn qua editor. Lượng dùng decimal, tối đa 4 chữ số lẻ; gợi ý 18g = 0,0180 Kg, không tự đổi đơn vị ngầm.
- T10: menu có giá dương, danh mục, trạng thái bán; thay nhiều dòng công thức nguyên tử. Không bật bán món chưa có công thức; bánh thành phẩm định mức 1. Đổi giá/công thức không thay snapshot lịch sử. STORE chỉ đọc công thức, CASH không sửa menu, ADMIN quản trị.
- T11: CRUD bàn/khu vực, sơ đồ theo `vw_TrangThaiBan`; chọn bàn phải đọc lại đơn mở thực tế. Không có thao tác đặt bàn trống bằng UPDATE trạng thái. Quyền xem POS ADMIN/CASH, quản trị ADMIN; STORE không được vào POS.
- T12: tạo/tìm khách theo SDT và chọn theo MaKH; SDT tùy chọn trống thành NULL. Hồ sơ không cho sửa điểm/hạng; khách điểm 0 có hạng ngưỡng 0. Điểm mở sổ có nguồn, khởi tạo một lần; CASH thêm/tìm và xem thông tin cần bán, STORE không truy cập hồ sơ khách. Lịch sử hóa đơn chỉ hiển thị phần dữ liệu/contract hiện có, tra cứu đầy đủ thuộc T17.
- T13: quản trị KM, chính sách hạng và điều chỉnh điểm có lý do; tái phân hạng nguyên tử. SQL cung cấp preview ưu đãi tái sử dụng: KM thay ưu đãi hạng, không cộng dồn, không giảm ship, làm tròn tiền VND theo kế hoạch. UI hiển thị kết quả SQL. ADMIN sửa chính sách/điểm; CASH chỉ dùng kết quả phục vụ bán, STORE không sửa chính sách/điểm.

Quyền kiểm ở BLL, độc lập ẩn nút. Không mở rộng dữ liệu tài chính cho CASH/STORE. Plan sẽ chốt ma trận từng thao tác và trường trả về theo mục 10 kế hoạch/UX trước code.

Danh sách/editor có tìm/lọc, reload sau ghi, xác nhận xóa/ngừng bán và báo FK rõ. Có loading/rỗng/lỗi/xung đột/kết quả chưa biết, giữ input/nháp, chặn gửi lặp, bỏ kết quả async trả muộn. Enter không tự chốt tồn/điểm. Đối chiếu kết quả sau timeout trước retry thao tác ảnh hưởng số dư.

Giao diện theo bộ Orbit hiện hành; khi lập plan UI phải đọc đủ tài liệu theo `docs/ui/README.md`. Chốt mẫu danh sách/editor, cửa sổ thường/thu nhỏ/chia đôi màn hình, 1280×720 và 1366×768, DPI 100/125/150%, bàn phím, tên Việt dài, Designer và trạng thái lỗi. HTML mẫu không là bằng chứng WinForms. Nếu yêu cầu mẫu POS trước trong guide cản các màn G03 trước T16, phải nêu và chốt phạm vi áp dụng trong bộ plan, không tự đổi thứ tự backlog.

## 3. Đầu ra và giới hạn

Đầu ra gồm migration theo task, contract SQL và DTO/request, DAL/BLL có quyền/validation, màn WinForms dùng được, tài liệu và kịch bản dùng thử trên đường chạy development cố định đã bàn giao G02. Baseline/seed không coi là contract tương lai; trước code đọc schema/seed/migrations thật. Không chạy seed lại trên DB đã dùng; không ghi fixture test vào DB development/sử dụng.

SQL → DAL → BLL → UI cho từng task; procedure sở hữu transaction, trigger cập nhật số dư một lần. Không thêm EF/Dapper/API, generic repository hoặc nền cho module chưa đến lượt.

Ngoài phạm vi: POS tạo/sửa đơn và checkout đích T14–T16, in/lịch sử đầy đủ T17, giao hàng T18, nhập/xuất/kiểm kê thông thường T20–T22, quản lý tài khoản T24; không đổi điểm thành tiền/PT010, size/topping, đặt bàn, gộp/tách bill hoặc hoàn tiền.

T09-K09 và T10-K04/K06–K08 kiểm phần G03 bằng baseline/fixture hợp lệ trên SQL Server test riêng; ghi rõ phần chưa tích hợp. G04 bắt buộc regression với checkout đích; không gọi procedure chưa có migration và không đánh dấu toàn ca PASS từ fixture. Không mở rộng checkout ngoài phạm vi để né phụ thuộc.

## 4. Phạm vi và nghiệm thu gốc T09–T13

Phần dưới sao nguyên bản task/đầu ra/checklist từ kế hoạch phiên bản 3.3. Giữ nguyên 47 ID (9 + 10 + 8 + 9 + 11); subcase bổ sung trong plan không thay hoặc bỏ ca gốc.
### T09 — Nguyên liệu, đơn vị và số dư mở sổ

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Quản lý nguyên liệu mà không cho CRUD trở thành đường sửa tồn tùy ý. |
| Phạm vi | NGUYENVATLIEU, tồn hiện tại, mốc mở sổ có nguồn lưu; chưa nhập/xuất thông thường. |
| Quy mô | M — danh mục có số dư |
| Phụ thuộc | T08 |
| Đầu ra | Màn hình nguyên liệu, quy trình tồn đầu kỳ một lần có kiểm soát, hợp đồng đối chiếu. |

**Kiểm thử bắt buộc:**

- [ ] `T09-K01` [I/M] Nguyên liệu mới có tồn 0, tên/đơn vị hợp lệ; số lượng 4 chữ số lẻ không bị mất.
- [ ] `T09-K02` [I] Mở sổ số dư hợp lệ ghi tồn và dấu vết/mốc trong cùng transaction; có lỗi rollback cả hai.
- [ ] `T09-K03` [I] Mở sổ lần hai hoặc sau khi đã phát sinh giao dịch bị chặn.
- [ ] `T09-K04` [I/M] Số âm/quá precision/đơn vị trống bị từ chối, không âm thầm làm tròn sai.
- [ ] `T09-K05` [M] Ví dụ 18g quy đổi nhập 0,018Kg; preview/DB cùng đơn vị.
- [ ] `T09-K06` [I] Sửa tên không đổi tồn; đổi đơn vị sau có công thức/giao dịch bị chặn hoặc qua migration có kiểm soát.
- [ ] `T09-K07` [I/M] User CASH chỉ đọc; ADMIN/STORE không sửa trực tiếp tồn từ editor sau mở sổ.
- [ ] `T09-K08` [I] Xóa nguyên liệu đã được công thức/phiếu/tiêu hao tham chiếu bị chặn.
- [ ] `T09-K09` [M] Danh sách tồn refresh sau giao dịch, không dùng cache làm kết quả chốt.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

### T10 — Sản phẩm và công thức nguyên liệu

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Chuẩn bị menu có thể bán và kiểm soát tiêu hao. |
| Phạm vi | SANPHAM/CONGTHUC, editor công thức nguyên tử; ghi chú size/topping không tính riêng. |
| Quy mô | M — menu và nhiều dòng công thức |
| Phụ thuộc | T09 |
| Đầu ra | Menu, ngừng/bật bán, thay công thức nguyên tử. |

**Kiểm thử bắt buộc:**

- [ ] `T10-K01` [I/M] Tạo sản phẩm giá dương, đúng danh mục; thêm nhiều định mức hợp lệ.
- [ ] `T10-K02` [I] Giá 0/âm, định mức 0/âm, FK sai hoặc cặp SP/NVL trùng bị chặn.
- [ ] `T10-K03` [I] Một dòng công thức lỗi khiến toàn bộ thay công thức rollback, công thức cũ còn nguyên.
- [ ] `T10-K04` [I/M] Không bật bán/checkout món không công thức; bánh thành phẩm định mức 1 hoạt động.
- [ ] `T10-K05` [M] Ngừng bán làm món không chọn mới; đơn mở đang chứa món cần sửa, không tự bỏ món.
- [ ] `T10-K06` [I] Đổi giá menu không đổi GiaBan của dòng hóa đơn cũ.
- [ ] `T10-K07` [I] Đổi công thức không đổi TIEUHAO_HOADON đã lưu.
- [ ] `T10-K08` [I] Lưu công thức đồng thời checkout bị khóa/đọc snapshot nhất quán, không công thức nửa cũ nửa mới.
- [ ] `T10-K09` [U/M] CASH không sửa menu, STORE chỉ đọc công thức; ADMIN quản trị được.
- [ ] `T10-K10` [I/M] Xóa sản phẩm có lịch sử bị chặn; tìm/lọc danh mục và mở lại hiển thị đúng.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

### T11 — Bàn, khu vực và sơ đồ bàn

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Dùng trạng thái dẫn xuất theo đơn thay vì tự đồng bộ cột trạng thái. |
| Phạm vi | BAN/KHUVUC/vw_TrangThaiBan, sơ đồ và tìm đơn mở; tạo đơn nghiệp vụ ở T14. |
| Quy mô | S — danh mục và view |
| Phụ thuộc | T10 |
| Đầu ra | Sơ đồ bàn đúng trạng thái, chọn bàn trả đúng mã đơn mở. |

**Kiểm thử bắt buộc:**

- [ ] `T11-K01` [I/M] CRUD bàn/khu vực hợp lệ và quan hệ khu vực được giữ sau reload.
- [ ] `T11-K02` [I] Tên trống/quá dài/FK khu vực không có bị chặn.
- [ ] `T11-K03` [I/M] Seed B0003 đang phục vụ, các bàn còn lại theo dữ liệu view thực tế.
- [ ] `T11-K04` [I] Đơn đã trả/hủy không làm bàn bận; đơn mở tại quán làm bàn bận.
- [ ] `T11-K05` [M] Không truy vấn/update BAN.TrangThai vì cột không còn.
- [ ] `T11-K06` [I] Xóa bàn/khu vực có hóa đơn/bàn con bị từ chối, không xóa lịch sử.
- [ ] `T11-K07` [M] Chọn bàn refresh và lấy đơn hiện tại, không tin trạng thái màu cũ trên UI.
- [ ] `T11-K08` [U/M] Quyền quản trị danh mục bàn/khu vực và quyền xem POS đúng vai trò.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

### T12 — Khách hàng, hạng và thông tin mua hàng

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Tạo/tìm khách trong POS, bảo đảm hạng khởi tạo đúng số dư. |
| Phạm vi | KHACHHANG/HANGTHANHVIEN, tìm SDT, chỉnh thông tin; điều chỉnh điểm và KM ở T13. |
| Quy mô | M — CRUD có hạng |
| Phụ thuộc | T11 |
| Đầu ra | CustomerService, màn hình khách, SQL khởi tạo điểm/hạng hợp lệ. |

**Kiểm thử bắt buộc:**

- [ ] `T12-K01` [I/M] Thêm khách điểm 0 nhận hạng ngưỡng 0; chỉnh tên/SDT không đổi điểm/hạng.
- [ ] `T12-K02` [I] SDT rỗng thành NULL và nhiều khách NULL hợp lệ; SDT thực trùng bị chặn.
- [ ] `T12-K03` [I/M] Tên trống/quá dài và điểm mở sổ âm bị chặn; không dùng tên khách làm khóa.
- [ ] `T12-K04` [I] Ngưỡng 0, ngay dưới/bằng/trên ngưỡng chọn đúng hạng.
- [ ] `T12-K05` [I] Điểm mở sổ có nguồn và chỉ khởi tạo một lần; không reset khách có lịch sử.
- [ ] `T12-K06` [M] Tìm SDT nhanh, chọn đúng MaKH; khách vãng lai không cần tạo khách giả.
- [ ] `T12-K07` [I/M] Xóa khách đã có hóa đơn/lịch sử điểm bị chặn, không mất lịch sử bán.
- [ ] `T12-K08` [U/M] CASH thêm/tìm khách được nhưng không sửa số dư/hạng tùy ý.
- [ ] `T12-K09` [M] Danh sách/chi tiết khách reload đúng điểm và hạng từ DB, không tự tính từ cache.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

### T13 — SQL ưu đãi, chính sách hạng và điều chỉnh điểm

| Thuộc tính | Nội dung |
|---|---|
| Mô tả | Có một nguồn tính ưu đãi chính thức để checkout sử dụng. |
| Phạm vi | KHUYENMAI/HANGTHANHVIEN/LICHSU_DIEM; hàm hoặc procedure SQL preview tái sử dụng, quản trị chính sách và lịch sử. |
| Quy mô | L — tiền và điểm |
| Phụ thuộc | T12 |
| Đầu ra | Migration ưu đãi/hạng; editor KM; điều chỉnh điểm có lý do; hợp đồng preview. |

**Kiểm thử bắt buộc:**

- [ ] `T13-K01` [I] Không khách/không KM giảm 0; chỉ khách dùng hạng; có KM dùng KM, không cộng dồn.
- [ ] `T13-K02` [I] Ngày bắt đầu/kết thúc đều hợp lệ, chương trình cùng ngày hợp lệ; ngoài ngày bị từ chối.
- [ ] `T13-K03` [I/M] Giảm 0%, 100%, số tiền cần làm tròn 0,5 VND đúng chính sách SQL; UI hiển thị nguyên kết quả preview SQL, không tự tính lại.
- [ ] `T13-K04` [I] Ngưỡng nhỏ nhất khác 0, ngưỡng trùng/âm hoặc % ngoài 0..100 bị chặn.
- [ ] `T13-K05` [I] Sửa ngưỡng áp dụng tái tính khách nguyên tử; một lỗi rollback chính sách và hạng.
- [ ] `T13-K06` [I] Cộng/trừ điểm thủ công ghi lịch sử; trừ vượt số dư rollback, lý do rỗng bị từ chối.
- [ ] `T13-K07` [I] UPDATE/DELETE lịch sử bị chặn; MaHD sai khách/chưa trả và cộng mua hàng trùng bị chặn.
- [ ] `T13-K08` [I] Không UPDATE DiemTichLuy hoặc MaHang trực tiếp qua màn hình chỉnh thông tin.
- [ ] `T13-K09` [I] Chương trình đổi tỷ lệ sau khi chốt không đổi tiền giảm/snapshot đơn cũ.
- [ ] `T13-K10` [U/M] CASH/STORE không sửa chính sách/điểm; quyền ADMIN đúng.
- [ ] `T13-K11` [I] Hai điều chỉnh trừ đồng thời không cùng tiêu số dư; kết quả còn điểm không âm.

**Điều kiện hoàn tất:** Có đủ đầu ra; các ca kiểm thử trong phạm vi đã chạy và PASS, có dữ liệu thử và kết quả đối chiếu; không còn lỗi trọng yếu của task. Lưu commit cùng biên bản kiểm thử. Nếu thay SQL, migration phải chạy được trên phiên bản trước và bảo toàn dữ liệu cũ ngoài thay đổi đã mô tả.

## 5. Giả định và các điểm cần chốt trong bộ plan

- Dùng một kho chung; danh mục nguyên liệu lưu đơn vị chuẩn. ADMIN mở sổ, STORE quản trị nguyên liệu theo quyền nhưng không có đường sửa tồn trực tiếp; CASH chỉ đọc tồn cần bán.
- Dữ liệu cũ có tồn/điểm ban đầu cần cơ chế ghi nhận nguồn và preflight migration bảo toàn số dư; không reset hay tự dựng lịch sử mua hàng giả. Cấu trúc mốc/nguồn, xử lý dữ liệu thiếu hạng ngưỡng 0, lỗi dữ liệu cũ và rerun phải được chốt trước triển khai.
- Plan chốt contract chống ghi đè đồng thời, khóa thay công thức/checkout baseline, khóa điều chỉnh điểm và thay chính sách, truy vấn đối chiếu sau timeout; không tin cache UI.
- Plan xác định dataset sạch/demo/legacy, principal runtime/migration và fixture kiểm lịch sử/rollback/hai session; không thay SQL Server bằng SQLite.
- Chưa có câu hỏi nghiệp vụ bắt buộc ngoài phạm vi chuẩn. Phát sinh thay nghiệp vụ/contract hoặc blocker phụ thuộc sẽ được ghi rõ để duyệt phần phát sinh.

## 6. Tiêu chí hoàn tất và bước duyệt

1. Requirements nhóm được duyệt một lần; tiếp theo viết đủ năm plan task con và `plans.md`, chờ duyệt cả bộ một lần trước code.
2. Triển khai tuần tự T09–T13; kiểm tra kỹ thuật tối thiểu ghi riêng, không dùng làm PASS nghiệm thu. Không commit/push khi chưa được giao.
3. Triển khai xong mới trình đợt test nhóm để duyệt riêng. Test giữ source/migration cố định, lưu `test-results.md` tại từng task, tổng hợp `report.md` kể cả FAIL/BLOCKED/NOT RUN. Bug tại task sở hữu và duyệt cả đợt trước debug; debug đã duyệt bao gồm regression theo ảnh hưởng.
4. Đợt test bao phủ 47 ID và từng subcase bắt buộc: tiền/kho/điểm, snapshot, transaction sau lỗi, hai session và quyền; truy vết UX-K06 (phần bàn T11), UX-K09–K10 (T12/T13 và tích hợp T16), UX-K33 (T13), cùng UX-K39–K42 theo màn. Không nhận thao tác service thay bằng chứng GUI.
5. Bàn giao đường chạy F5/profile/DB/schema và kịch bản ADMIN/CASH/STORE cho chức năng mới; ghi rõ phần chưa triển khai. Manifest tài nguyên test có ownership/marker; cleanup allowlist đúng tài nguyên lượt chạy sau lưu bằng chứng, kể cả FAIL. Báo số lượng trước/tạo/dọn/còn giữ, lý do và cách xử lý tài nguyên còn lại; không dọn DB development hoặc tài nguyên sẵn có.
6. Chỉ ghi Hoàn tất khi đủ đầu ra, các ca bắt buộc PASS và không còn lỗi trọng yếu. Thiếu SDK/SQL/GUI hoặc phần tích hợp chưa chạy phải ghi đúng trạng thái; giữ nguyên 325 ID và lịch sử nhóm trước.



