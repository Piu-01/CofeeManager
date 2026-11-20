# Nhật ký thao tác (VHT-104) — requirements

Ngày soạn: 12/11/2026. Trạng thái: **đang chờ người dùng duyệt requirements**.
Chưa viết plan, chưa sửa source/migration. Theo `task-approval-workflow`: duyệt requirements
→ viết plan → duyệt plan → mới triển khai; test là đợt duyệt riêng.

## 1. Mục tiêu

Cung cấp màn hình **tra cứu nhật ký thao tác** read-only cho quán cafe: lọc theo thời gian,
người thao tác và loại sự kiện; tách **nhật ký tài khoản/hệ thống** khỏi **nhật ký nghiệp vụ**;
phân quyền ở BLL (không chỉ ẩn menu) và không lưu/hiển thị dữ liệu nhạy cảm.

Thẻ VHT-104 yêu cầu "nguồn log" từ NEW-03, nhưng repo CoffeeManager **chưa có hạ tầng ghi nhận
nhật ký** (không có bảng log, không có writer, không log đăng nhập). Vì vậy task này gồm cả
**hạ tầng ghi nhận tối thiểu** đủ để màn hình đọc dữ liệu thật, thay cho NEW-03 trong phạm vi
được duyệt dưới đây.

## 2. Bối cảnh và chênh lệch giữa thẻ task và repo

| Thẻ VHT-104 | Thực tế CoffeeManager |
|---|---|
| Đợt 3D, phân hệ p1 | Repo triển khai theo nhóm G01–G08/T01–T32; "p1/3D" không có trong repo |
| Nguồn tham chiếu `docs/task/cong_viec_theo_tuan.md`, `timeline_song_song.md`, `thao_tac_va_quyen.xlsx` | Không tồn tại trong repo (chỉ có `docs/tasks/` và `docs/`) |
| Vai trò "Chủ trọ", "Quản lý", "Khách" | Chỉ có ADMIN / CASH / STORE, không có vai trò chủ sở hữu riêng |
| NEW-03 (hạ tầng ghi nhận) | Chưa có; phải bổ sung tối thiểu trong task này (mục 3.1) |
| D16 (điều kiện huỷ task) | Không có hồ sơ D16 trong repo; xem giả định G1 và câu hỏi Q3 |
| "Admin không xem nhật ký nghiệp vụ" | Chưa biểu diễn được bằng 3 vai trò hiện có; xem Q1 |

Các chênh lệch trên được xử lý bằng giả định/bản đồ hoá ở mục 4–5, **không tự mở rộng nghiệp vụ**.

## 3. Phạm vi

### 3.1 Hạ tầng ghi nhận tối thiểu (thay NEW-03)

- Migration mới (version kế tiếp, hiện source tới 030; không tái sử dụng version) tạo bảng nhật ký
  append-only, ví dụ `dbo.NHATKY_THAOTAC`, gồm tối thiểu: mã tự tăng, thời điểm UTC, mã tài khoản +
  tên đăng nhập snapshot (nullable cho sự kiện hệ thống), nhóm sự kiện (tài khoản/hệ thống/nghiệp vụ),
  mã sự kiện, loại đối tượng + mã đối tượng (nullable), mô tả ngắn theo allowlist, mã tương quan
  (nullable). Không lưu payload thô, SQL, exception, connection string.
- Quyền DB: ứng dụng chỉ được ghi/đọc qua procedure; không cấp UPDATE/DELETE cho role app
  (nhật ký bất biến ở mức quyền). Có kiểm tra version migration như các migration hiện hành.
- Writer đặt ở DataAccess, được BLL gọi **sau khi** use case thành công; lỗi ghi log không làm đổi
  kết quả giao dịch và không ném lỗi lên UI. Không giữ transaction khi nhân viên đang nhập/đếm/in.
- Nội dung chỉ sinh từ **allowlist mã sự kiện**; cấm mật khẩu/hash/security stamp, token, giấy tờ
  tuỳ thân, số điện thoại/địa chỉ/ghi chú free text, nội dung nháp đơn, ảnh/chứng từ.
- Danh mục sự kiện của đợt này (đề xuất, chốt chi tiết trong plan; chỉ log thao tác ghi, không log đọc):
  - Tài khoản/hệ thống: đăng nhập thành công, đăng xuất/kết thúc phiên, tạo tài khoản, khoá/mở tài khoản,
    đặt lại mật khẩu, đổi vai trò, hoàn tất thiết lập ban đầu.
  - Nghiệp vụ: thanh toán, hủy hóa đơn, nhập kho, xuất/hủy kho, kiểm kê, lập phiếu thu/chi, trả nợ NCC,
    chốt lương/chi lương, giờ làm, thêm/sửa/xóa tài sản — chỉ với dịch vụ đã có trong source.
- Không backfill nhật ký cho dữ liệu lịch sử; nhật ký chỉ có từ sau khi nâng schema.

### 3.2 Màn hình nhật ký

- UserControl trong MainForm theo shell hiện có, menu "Quản trị"; chỉ hiện với vai trò có quyền.
- Danh sách read-only, mới nhất trước, phân trang phía SQL; bộ lọc: khoảng thời gian `[từ ngày, đến ngày)`,
  người thao tác (chọn từ tài khoản hoặc tìm theo tên đăng nhập), nhóm/loại sự kiện.
- Cột: thời điểm, người thao tác, nhóm, sự kiện (nhãn tiếng Việt), đối tượng/mã chứng từ, mô tả ngắn.
- Trạng thái loading/rỗng/lỗi/Thử lại tách biệt; lỗi DB không hiển thị danh sách rỗng giả.
- Không sửa/xóa/xuất/in, không realtime, không xem payload chi tiết ngoài trường allowlist.
- Hình thức theo bộ Orbit `docs/ui/` (đọc đủ theo thứ tự trước khi code UI); tiền/ngày/giờ định dạng
  theo AGENTS.md; agent chỉ kiểm chức năng/layout kỹ thuật tại DPI 125%, không tự đổi Windows Scale.

### 3.3 Phân quyền

- Hai quyền mới trong `PermissionService`/`AppPermission`: `AuditAccount` (nhật ký tài khoản/hệ thống)
  và `AuditBusiness` (nhật ký nghiệp vụ); kiểm quyền ở BLL khi đọc dữ liệu, không chỉ ẩn menu.
- Bản đồ mặc định đề xuất (cần chốt tại Q1): ADMIN nhận `AuditAccount`; `AuditBusiness` cấp cho
  ADMIN với nghĩa "chủ quán", đồng thời tách quyền để tài khoản chỉ có `AuditAccount` không đọc được
  nghiệp vụ. CASH/STORE không có quyền nào; người chưa đăng nhập/khách không truy cập.
- Không thêm vai trò mới trừ khi người dùng chốt Q1 theo hướng đó.

### 3.4 Kiểm thử thuộc đợt test sau này (không chạy ở bước requirements)

- Quyền: CASH/STORE/khách không truy cập cả hai phạm vi; tài khoản thiếu `AuditBusiness` không đọc
  nhật ký nghiệp vụ; gọi thẳng BLL/DAL vẫn bị chặn.
- Không nhạy cảm: đưa mật khẩu/token/giấy tờ/SĐT/ghi chú vào input của use case được log, kiểm bảng
  log và màn hình không chứa các giá trị này; kiểm bằng truy vấn SQL trên dữ liệu test.
- Lọc/paging/timezone: khoảng ngày biên, người không có sự kiện, loại rỗng; hiển thị giờ theo giờ máy,
  lưu UTC; không lệch do format.
- Lỗi ghi log không đổi kết quả nghiệp vụ (ví dụ checkout vẫn commit), không làm UI báo lỗi sai.
- SQL Server thật, DB test riêng; xUnit cho allowlist/sanitizer và `PermissionService`; WinForms thật
  cho binding, lọc, loading/lỗi và quyền.

### 3.5 Không làm

Ngoài phạm vi: sửa/xóa nhật ký, retention/purge tự động, xuất Excel/PDF/in/email, realtime,
log toàn bộ bảng bằng trigger, log thao tác đọc, log nội dung nháp đơn, backfill lịch sử, đổi nghiệp vụ
hay quyền hiện hữu khác, thêm vai trò mới (trừ Q1), đổi framework/kiến trúc, mở rộng G08/publish.

## 4. Giả định

- **G1** — Thẻ VHT-104 được gửi lại sau khi trao đổi = người dùng xác nhận D16 đã được chốt. Nếu D16
  chưa chốt, theo đúng thẻ phải **hủy task**; không tự triển khai.
- **G2** — NEW-03 chưa tồn tại trong repo nên hạ tầng ghi nhận tối thiểu thuộc phạm vi task này
  (mục 3.1), giới hạn ở danh mục sự kiện đã nêu, không dựng hạ tầng log tổng quát cho mọi module.
- **G3** — "Chủ trọ" trong thẻ được hiểu là người quản lý cơ sở (ở repo là ADMIN), "Quản lý/Khách"
  không truy cập được hiểu ở mức **quyền**, không phải tên vai trò. Chi tiết cần chốt tại Q1.
- **G4** — Nhật ký append-only, không sửa/xóa; không có yêu cầu lưu trữ tối thiểu/retention trong đợt này.
- **G5** — Thời điểm lưu UTC, hiển thị giờ máy dạng `dd/MM/yyyy HH:mm`; lọc ngày theo giờ địa phương
  của máy chạy app, chốt quy tắc quy đổi trong plan.
- **G6** — Người thao tác lấy từ session/BLL, không lấy từ tham số UI; sự kiện hệ thống không có tài khoản
  được phép null và hiển thị "Hệ thống".
- **G7** — Mọi dữ liệu nhật ký do chính app ghi sau nâng schema; không giả lập dòng log lịch sử để đẹp demo.

## 5. Câu hỏi mở

- **Q1 (chặn plan)** — Bản đồ vai trò: repo chỉ có ADMIN/CASH/STORE nên tiêu chí "Admin không xem nhật ký
  nghiệp vụ" chưa biểu diễn được. Chọn: (A) ADMIN nhận cả hai quyền, tách quyền để chứng minh bằng
  tài khoản thiếu `AuditBusiness`; (B) ADMIN chỉ `AuditAccount`, `AuditBusiness` tạm chưa cấp cho vai trò
  nào (màn hình nghiệp vụ chưa dùng được); (C) thêm vai trò chủ sở hữu mới (mở rộng nghiệp vụ, cần duyệt riêng).
- **Q2** — Danh mục sự kiện nghiệp vụ ở mục 3.1 có đúng phạm vi mong muốn, hay cần bổ sung/bớt module?
- **Q3** — Xác nhận D16 đã chốt và chấp thuận việc tự làm hạ tầng ghi nhận thay NEW-03 (G1, G2).
- **Q4** — Có cần retention/thời hạn lưu nhật ký ngay trong đợt này không? Đề xuất: không.
- **Q5** — Có cần xuất Excel/PDF hoặc in danh sách nhật ký không? Đề xuất: không (ngoài thẻ).

## 6. Tiêu chí hoàn thành

1. Requirements được duyệt → viết plan chi tiết (file/migration/procedure/DTO/service/UI/test) → duyệt
   plan → triển khai theo thứ tự SQL → DAL → BLL → UI.
2. CASH/STORE/chưa đăng nhập không truy cập được màn hình và không đọc được dữ liệu khi gọi thẳng BLL/DAL;
   có biên bản test quyền.
3. Với bộ dữ liệu test có mật khẩu/token/giấy tờ/SĐT/ghi chú, không dòng nhật ký và không nội dung hiển thị
   nào chứa dữ liệu nhạy cảm; kiểm bằng truy vấn và ảnh chụp/ghi chú test chức năng do agent chạy.
4. Tài khoản không có `AuditBusiness` không xem được nhật ký nghiệp vụ (theo phương án Q1 được chốt).
5. Lọc thời gian/người/loại, phân trang và trạng thái loading/rỗng/lỗi hoạt động đúng trên WinForms thật;
   timezone/định dạng ngày giờ đúng quy định.
6. Build Debug/Release pass; technical-checks ghi riêng; đợt test phải được người dùng duyệt trước khi chạy,
   `test-results.md` chỉ ghi PASS cho ca đã chạy có bằng chứng; thiếu D16/DB test/SQL ghi BLOCKED, không suy PASS.
7. Không nằm ngoài phạm vi mục 3.5; phát sinh mới phải cập nhật và xin duyệt bổ sung.
