# Biên bản test — Sửa master data bằng hộp thoại, giữ xóa tại dòng

Ngày chạy: 23/11/2026. Phạm vi: đợt test chức năng được người dùng duyệt sau khi
triển khai code và build Debug/Release PASS. Trong lượt test source được giữ cố
định, không sửa code, không đổi expected.

## 1. Môi trường

| Mục | Giá trị |
|---|---|
| Ứng dụng | `CafeManager.WinForms` build Debug `net10.0-windows`, chạy từ output tạm `C:/Users/Lenovo/.qoder/tmp/coffee-build-debug/Debug/net10.0-windows/`, PID 23572 |
| CSDL | SQL Server thật, DB test riêng `CoffeeManager_Test_Inline_20261122_inline_ui`, schema 41 |
| Kết nối | `CAFEMANAGER_APP_CONNECTION` trỏ DB test; không ghi vào DB sử dụng thật |
| Tài khoản | `admin` (ADMIN) và `thungan` (CASH) |
| DPI | 125% (120 DPI); không chạy 100%/150%, không kiểm hình thức |
| Công cụ | UIA (`System.Windows.Automation`) + Win32 P/Invoke; script trong `C:/Users/Lenovo/.qoder/tmp/cm-dialog-tests/`, nạp `tools/database/G04Ui.ps1`; đối chiếu DB bằng `sqlcmd` |

Quy ước: PASS = đã chạy và khớp expected; PARTIAL = chạy được phần chính nhưng
không quan sát được hết bằng chứng qua UIA; NOT RUN = chưa chạy, ghi rõ lý do;
SKIPPED = ngoài phân công (hình thức/DPI do người dùng tự kiểm).

## 2. Kết quả theo ca

| Mã | Ca | Kết quả | Bằng chứng |
|---|---|---|---|
| D-01 | Menu sản phẩm: click một lần chỉ chọn dòng, không mở hộp | PASS | Click ô Tên SP001, `DIALOGS=0` |
| D-02 | Menu sản phẩm: double-click mở `OperationEditorDialog` đúng SP001; Hủy không ghi | PASS | `DIALOGS=1: Sửa sản phẩm — SP001`; Hủy, DB giữ giá 26000 |
| D-03 | Menu sản phẩm: F2 mở hộp sửa; lưu tên/giá/nhóm/trạng thái, reload chọn lại dòng | PASS | Lưu đổi giá 27000 rồi trả 26000, tên `Inline Product A` khôi phục; reload chọn SP001 |
| D-04 | Danh mục nền: double-click từng tab mở đúng `CatalogEditorDialog` của loại tab | PASS | Chạy tab Sản phẩm/NCC/PTTT/Đối tác/Loại thu-chi/Chức vụ/Ca làm; tiêu đề hộp đúng loại |
| D-05 | Danh mục nền: sửa trường đại diện và lưu (SĐT NCC, tỷ lệ đối tác, phụ cấp/hệ số chức vụ, giờ ca, loại thu-chi, hỗ trợ PTTT); Hủy không ghi | PASS | Giá trị vào DB đúng sau lưu, khôi phục lại; PT010 và loại Chi hệ thống vẫn không cho sửa |
| D-06 | Nguyên liệu: double-click mở hộp sửa chỉ có Tên/Đơn vị, không lộ tồn đầu kỳ/nguồn tồn | PASS | Hộp `Sửa nguyên liệu — NVL01` có đúng 2 trường; lưu tên `Inline Mat Test A` |
| D-07 | Nguyên liệu: lưu hồ sơ không đổi tồn kho | PASS | Trước/sau lưu NVL01 `SoLuongTon=68.7280`, không đổi |
| D-08 | Khách hàng (quản trị): double-click/F2 mở hộp sửa Tên/SĐT; lưu reload | PASS | KH001 `Inline Customer A`/`0999000111` khôi phục sau sửa |
| D-09 | Nhân viên: double-click mở `HrEditorDialog` đúng NV001; lưu trường hồ sơ | PASS | Hộp sửa NV001, lưu tên/SĐT, DB khôi phục `Inline Employee A`/`0901234567` |
| D-10 | Tài sản: double-click mở `AssetEditorDialog` chế độ Update đúng TS001; lưu | PASS | Hộp sửa TS001; DB `Inline Asset A` |
| D-11 | Tài sản: nút **Chi tiết** vẫn mở `AssetDetailDialog` riêng, chỉ đọc | PASS | `DIALOGS=1: Tài sản — TS001`, nút Đóng/Mở phiếu chi/Xem kỳ; đóng không ghi |
| D-12 | Bàn (chế độ Danh sách): double-click mở hộp sửa bàn, đổi tên lưu/khôi phục | PASS | B0001 `Inline Table 01`/KV001 khôi phục; lưu tên ngắn OK |
| D-13 | Khu vực: double-click mở hộp sửa khu vực, lưu/khôi phục | PASS | KV001 tên khôi phục sau lưu |
| D-14 | Hạng thành viên: double-click mở hộp sửa, có bước preview ảnh hưởng trước lưu | PARTIAL | Hộp sửa mở đúng hạng, xác nhận preview và lưu vào DB (VANG 10000/5, KIM 30000/8); nội dung chữ preview không đọc hết được qua UIA |
| D-15 | Hover: cột sửa được đổi Caramel + con trỏ Hand; cột Mã/thùng rác/vùng trống Arrow; không còn IBeam | PASS | `GetCursorKind` = Hand trên ô Tên/giá của 8 màn; Arrow trên Mã, thùng rác và vùng trống |
| D-16 | Không còn editor overlay: click ô dữ liệu không sinh control nhập, không commit khi Enter/Tab | PASS | Không quan sát control con mới sau click; Enter/Tab không ghi dữ liệu |
| D-17 | Xóa: thùng rác cuối dòng, xác nhận theo loại dữ liệu, xóa ngay không xác nhận ở danh mục | PASS | Chèn `DM099 Temp Dlg Test`, bấm thùng rác: không hộp xác nhận, DB còn 0 dòng |
| D-18 | F3 picker Khách hàng: double-click không mở sửa, không có thùng rác; chọn khách và Áp dụng vẫn chạy | PASS | `DIALOGS=0` khi double-click, không có cột thùng rác; chọn khách đưa về POS thành công |
| D-19 | Quyền CASH: không mở được hộp sửa master data, không hover Hand | PASS | Đăng nhập `thungan`: menu hạn chế, Khách hàng chỉ đọc, không mở hộp sửa/xóa |
| D-20 | Bàn: click card Sơ đồ vẫn mở POS như trước | PASS | Mở POS từ card, không mở hộp sửa bàn |
| D-21 | Xóa bị chặn khóa ngoại: NCC06 có 5 phiếu nhập, xác nhận **Xóa** | PASS | Hộp xác nhận đúng câu, sau xác nhận báo `Dữ liệu không thỏa ràng buộc hoặc không đủ tồn; kiểm tra nguồn liên quan.`, `NHACUNGCAP` vẫn còn NCC06, danh sách giữ 1 dòng |
| D-22 | Xóa timeout: giữ khóa dòng DM098 bằng phiên SQL thứ hai >30s, rồi bấm thùng rác | PASS | Sau 30s dòng bị khóa, hiện nút **Đối chiếu** và thông báo `Chưa xác định kết quả ghi; đối chiếu mã nguồn trước khi gửi lại. · Mã yêu cầu 04cb46b4-…`. Thả khóa rồi bấm Đối chiếu: `Đối chiếu: chưa ghi; có thể sửa lại.`, nút mất, tải lại dòng; DB vẫn còn DM098 |
| D-23 | Regression POS/Khuyến mãi/Chấm công/chứng từ ngoài điều hướng | PARTIAL | POS mở và dùng được (D-20, D-18); Chấm công tải `86 dòng công`; Kho tải Nhập 30 / Xuất 12 / Kiểm kê 2 phiếu; Lịch sử hóa đơn mở, lọc mặc định 0 hóa đơn. **Ưu đãi/Điểm lỗi có sẵn** — xem F-02 |
| D-24 | Hình thức, DPI 100%/150%, khoảng cách/màu | SKIPPED | Theo phân công, người dùng tự kiểm |

## 3. Phát hiện trong đợt test (không sửa khi test)

### F-01 — Tên bàn vượt 20 ký tự báo lỗi chung "Có lỗi dữ liệu chưa xác định"

Hiện tượng: sửa tên bàn thành chuỗi dài hơn giới hạn cột `BAN.TenBan nvarchar(20)`,
lưu từ hộp sửa Bàn trả về thông báo chung, không nêu giới hạn.

Nguyên nhân đã có bằng chứng: gọi trực tiếp `sp_TableUpdate` với tên dài trả SQL
error **2628** ("String or binary data would be truncated... TenBan").
`SqlErrorMapper.MapNumber` không ánh xạ 2628 nên rơi vào nhánh `Unknown`
("Có lỗi dữ liệu chưa xác định; tra mã hỗ trợ trước khi tiếp tục"). Gọi lại với tên
ngắn lưu thành công, xác nhận không phải lỗi của hộp sửa mới.

Đánh giá: lỗi có sẵn từ trước đợt này (validation độ dài ở `TableService`/BLL và
mapping 2628), không do thay đổi double-click. **Đã sửa và kiểm chứng ngày
23/11/2026** — xem mục 6 và `docs/bugs/table-name-truncation/`.

### F-02 — Mở màn Ưu đãi/Điểm ném `IndexOutOfRangeException` chưa bắt

Hiện tượng: chuyển sang **Ưu đãi / Điểm** hiện hộp `Unhandled exception ... Index
was outside the bounds of the array.`; bấm Continue thì màn trống, không tải danh
sách chương trình.

Nguyên nhân/bằng chứng: lỗi phát sinh khi `LoyaltyRepository.Promotion` đọc kết quả
`sp_PromotionList` (ánh xạ cố định các cột 0–12). Đã quan sát cùng lỗi ngày 22/11
trước khi chạy đợt test này (ghi trong ghi chú test inline). Không liên quan thay
đổi double-click/hover; nguyên nhân là DB test schema 41 thiếu migration 042.
**Đã sửa và kiểm chứng ngày 23/11/2026** — xem mục 6 và
`docs/bugs/promotion-list-schema/`.

## 4. Dữ liệu sau test

Đã đối chiếu DB test, dữ liệu trôi về giá trị ban đầu:

| Bảng | Bản ghi | Giá trị |
|---|---|---|
| `BAN` | B0001 | `Inline Table 01`, KV001 |
| `SANPHAM` | SP001 | `Inline Product A`, 26000 |
| `NHANVIEN` | NV001 | `Inline Employee A`, 0901234567 |
| `TAISAN_CODINH` | TS001 | `Inline Asset A` |
| `HANGTHANHVIEN` | DONG/KIM/VANG | 0-0 / 30000-8 / 10000-5 |
| `NGUYENVATLIEU` | NVL01, NVL02 | `Inline Mat Test A` (Kg, 68.7280), `Inline Enter Probe` (Lit, 205.8900) |
| `DANHMUC_SP` | DM099, DM098 | Đã xóa (0 dòng); DM098 dùng cho ca timeout, đã xóa sau test |

Bản ghi tạm đã xóa, tên/giá trị đã khôi phục; XEvent `cm_err2` đã STOP/DROP.
Lưu ý: B0001 từng bị gọi procedure trực tiếp khi điều tra 2628 nên `Revision` có
thể đã tăng, tên và dữ liệu nghiệp vụ không đổi.

## 5. Tổng kết

- 23/24 ca chạy; trong đó 21 PASS, 2 PARTIAL (D-14 nội dung preview không đọc được
  qua UIA; D-23 có màn Ưu đãi/Điểm lỗi có sẵn F-02), 1 SKIPPED (hình thức/DPI).
  Không còn ca NOT RUN trong phạm vi chức năng.
- 2 phát hiện ngoài phạm vi thay đổi đã được duyệt sửa và kiểm chứng lại ngày
  23/11/2026: F-01 và F-02 (mục 6).
- Đợt test chức năng của task đã đủ; không còn ca NOT RUN trong phạm vi chức năng.

## 6. Hồi quy sau sửa F-01/F-02 — 23/11/2026

Build Debug/Release 0 lỗi (output tạm `C:/Users/Lenovo/.qoder/tmp/coffee-build-fix*`).

| Ca | Kết quả | Bằng chứng |
|---|---|---|
| F01-U1 | PASS | Unit test `F01-TRUNC` (2628 đọc/ghi, 8152): 3/3 PASS, `Constraint`, không cần đối chiếu |
| F01-UI1 | PASS | Sửa B0001 thành “Inline Table 01 ABCDE” (21 ký tự): hộp giữ mở, báo “Tên: bắt buộc, tối đa 20 ký tự…”, DB vẫn `Inline Table 01` |
| F01-UI2 | PASS | Lưu tên hợp lệ “Ban 01 Test” vào DB, sau đó khôi phục `Inline Table 01` |
| F02-1 | PASS | DB test schema 41: mở Ưu đãi/Điểm không còn unhandled exception; báo “CSDL chưa áp migration cần thiết…” |
| F02-2 | PASS | Áp migration 042 (version 42) vào DB test: mở lại Ưu đãi/Điểm hiển thị `1–17 / 17 chương trình`, không lỗi |

Cập nhật môi trường: DB test `CoffeeManager_Test_Inline_20261122_inline_ui` nay ở
schema 42. Ứng dụng test là build Debug sau sửa, PID 16408; đã đóng sau test.
