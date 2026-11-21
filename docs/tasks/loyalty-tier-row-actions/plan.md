# Plan — Thao tác từng dòng hạng thành viên

- Ngày: 21/11/2026
- Requirements đã duyệt: `docs/tasks/loyalty-tier-row-actions/requirements.md`
- Nguyên tắc: chỉ UI; tái dùng contract chính sách hạng hiện có; không migration,
  không đổi DAL/BLL. Thực hiện tuần tự, mỗi bước build được.

## Bước 1 — Ẩn nút “Tải lại” riêng cho màn Hạng thành viên

- File: `src/CafeManager.WinForms/UI/OrbitRecordListControl.cs`, nhánh
  `if (profile == OrbitGridProfile.Ranks)` trong `Configure` (khoảng dòng 88–92).
- Thay vì đưa `refresh` vào `Workspace.RowActions`, ẩn nút này (`refresh.Visible = false`).
  Giữ đăng ký refresh trong `buttons` để không ảnh hưởng logic chung; chỉ màn Ranks bị ẩn.
- Không sửa nhánh profile khác, không đổi `UseStackedSearch`.
- Verify: build sạch; màn Hạng thành viên không còn nút Tải lại; các màn khác vẫn còn
  (kiểm ở đợt test AR-01/AR-11).

## Bước 2 — Đổi toolbar `PolicyList`

- File: `src/CafeManager.WinForms/Modules/Loyalty/LoyaltyControl.cs`, class `PolicyList`.
- Giữ `Configure("Chính sách hạng", ...)`, `Search.Visible=false`, `Paginated=false`.
- Bỏ `AddAction("Thay chính sách", Edit)` và toàn bộ `Edit()` sửa cả bảng
  (DataTable + grid + preview toàn bộ) — thay bằng:
  - `AddAction("Thêm", AddTier)` (nút chính, breadcrumb `createCaption` đã là “Thêm”).
  - `AddRowDeleteColumn()`.
  - Gán `RowDeleteRequested = item => DeleteTier((TierRow)item)` (async void bọc
    `RunRowAction` để bắt lỗi như action thường; hoặc dùng handler `async` có try/catch
    hiển thị `State`).
  - Gán `RowActivated = item => EditTier((TierRow)item)`.
- Thêm helper nội bộ:
  - `Task<List<TierRow>> ReadTiers()` → `service.PolicyAsync(session)`.
  - `static TierInput ToInput(TierRow)` và `static IReadOnlyList<TierInput> WithChange(
    IEnumerable<TierRow>, ...)` trả danh sách ứng viên **sắp xếp tăng dần theo
    `MinimumPoints`** trước khi preview/save.
  - `Task RunRowAction(Func<Task> action)` bắt `UserOperationException`,
    `UnauthorizedAccessException`, `ArgumentException`, `OperationCanceledException`
    và ghi `State.Text` (cùng kiểu các list khác).
- Verify: build; toolbar chỉ còn “Thêm”; double-click/icon chưa gắn hành vi cho đến bước 3–4.

## Bước 3 — Dialog Thêm / Sửa một hạng

- Vẫn trong `PolicyList`, thêm `AddTier()` và `EditTier(TierRow row)` dùng chung một
  builder `Task ShowTierEditorAsync(TierRow? row)`.
- Nội dung dialog `OperationEditorDialog`:
  - Tiêu đề “Thêm hạng thành viên” / “Sửa hạng thành viên”.
  - `AddText("Mã hạng (A–Z, 0–9, tối đa 5)", row?.Code ?? "", readOnly:false)` — mã
    sửa được khi edit theo yêu cầu.
  - `AddText("Ngưỡng điểm", row?.MinimumPoints.ToString() ?? "")`.
  - `AddText("Giảm %", row?.Discount.ToString() ?? "0")`.
  - Nút `OrbitButton` “Xem lại từ SQL” + `Label` hiển thị số khách đổi hạng và bản
    xem trước các hạng sau thay đổi.
- Đọc/validate cục bộ: parse `int` cho ngưỡng và %; lỗi parse ném `ArgumentException`
  với thông điệp tiếng Việt (“Ngưỡng điểm phải là số nguyên không âm.”, “Giảm % phải
  nguyên 0–100.”). Các kiểm tra trùng mã/ngưỡng, hạng 0 để `NormalizeTiers`/SQL báo.
- Preview:
  - Bấm “Xem lại từ SQL”: khóa nút trong lúc chờ, gọi `service.PolicyPreviewAsync`
    với danh sách ứng viên đã sort; lưu `preview` + `captured`.
  - Mọi thay đổi ở 3 ô nhập làm mất hiệu lực preview (`preview=null; captured=null;
    info.Text="Cần xem lại từ SQL."`).
- `CaptureWrite`:
  - Đọc lại 3 ô, tạo danh sách ứng viên, so sánh `SequenceEqual` với `captured`;
    nếu khác/null → `ArgumentException("Cần xem lại SQL cho nội dung hiện tại.")`.
  - Đặt `Confirmation` theo thao tác: thêm/sửa hạng và tính lại hạng toàn bộ khách
    trong một transaction; nêu số khách preview.
  - Trả delegate gọi `service.PolicySaveAsync(session, payload, snapshot, id)`; bọc
    `try/catch` để khi lỗi (đặc biệt 51603 “Chính sách đã thay đổi.”) xóa
    `preview/captured` trước khi rethrow, buộc người dùng xem lại.
- `OrbitDialogHost.ShowAsync(dialog, this) == OK` → `await Reload()`.
- Verify: build; thêm/sửa thủ công ở đợt test AR-02/AR-03/AR-05/AR-06.

## Bước 4 — Xóa một hạng bằng icon thùng rác

- `DeleteTier(TierRow row)`:
  - Đọc `tiers = await ReadTiers()`.
  - Nếu `tiers.Count == 1 || row.MinimumPoints == 0` → ném
    `ArgumentException("Phải giữ một hạng có ngưỡng 0 điểm.")`; không mở dialog, không gọi SQL.
  - Tạo danh sách ứng viên = các hạng còn lại (sort tăng dần), gọi
    `service.PolicyPreviewAsync` **trước** khi mở dialog để lấy số khách bị đổi hạng.
  - Mở `OperationEditorDialog("Xóa hạng thành viên", reconcile)`; `AddNote` nêu
    `row.Code · ngưỡng · %` và số khách sẽ được xếp lại hạng.
  - `Confirmation = "Xóa hạng {code}? Khách đang ở hạng này sẽ được xếp lại theo
    ngưỡng còn lại."`; `CaptureWrite = () => id => service.PolicySaveAsync(...)` với
    `preview` vừa lấy.
  - OK → `Reload()`; lỗi preview/save → hiện ở `State`, DB không đổi.
- Verify: build; xóa/hủy/chặn ngưỡng 0 ở đợt test AR-07/AR-08.

## Bước 5 — Rà soát hành vi chung và hồi quy phạm vi

- Kiểm tra `RowActivated` không kích hoạt khi double-click cột Delete (base đã chặn).
- Kiểm tra action “Thêm” bị khóa khi `busy`/không phải ADMIN; PolicyList chỉ được tạo
  cho ADMIN như hiện tại.
- Kiểm tra reload sau lưu không mất cột `Delete`; `Grid.Columns["Delete"]` vẫn cuối.
- Không sửa `.Designer.cs`, không thêm dependency, không đổi POS/Ưu đãi/Điểm.
- Verify: build; đọc diff chỉ gồm 2 file UI.

## Bước 6 — Build và kiểm kỹ thuật

- Lệnh: `dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj`
  (nếu `bin` bị app/VS khóa thì dùng `-p:BaseOutputPath=<temp mới>`, **không** đặt
  `BaseIntermediateOutputPath` để tránh CS0579).
- Kỳ vọng: 0 error; warning chỉ là warning có sẵn (WebView2/WindowsBase).
- Không chạy test chức năng/GUI trong bước này.

## Bước 7 — Đề xuất đợt test, chờ duyệt riêng

- Soạn danh sách ca AR-01..AR-12 (theo requirements) kèm môi trường dự kiến:
  DB test copy riêng từ `CoffeeManager_Development`, SQL Server thật, app build tạm,
  `admin/Admin@123` + `thungan/Cash@123`, DPI 125%, UIA.
- **Không chạy** cho đến khi người dùng duyệt đợt test. Khi chạy: source cố định,
  không sửa code, lưu `docs/tasks/loyalty-tier-row-actions/test-results.md` và
  bằng chứng trong `runs/`.
- Ca DB cần đối chiếu: `HANGTHANHVIEN` trước/sau, `KHACHHANG.MaHang` được xếp lại,
  `LoyaltyPolicyState.Revision` tăng, `G03OperationReceipt` có bản ghi `PolicyReplace`,
  không đổi `LICHSU_DIEM`/`HOADON`.

## Rủi ro và cách giảm

| Rủi ro | Giảm thiểu |
|---|---|
| Mỗi thao tác dòng vẫn là replace toàn bộ → ảnh hưởng nhiều khách | Preview hiển thị số khách đổi hạng trước khi lưu; xác nhận rõ; rollback transaction nếu lỗi |
| Người khác đổi chính sách giữa chừng | ExpectedRevision + hash preview của SQL; lỗi 51603 hiển thị, không ghi đè |
| Xóa/đổi mã hạng làm mồ côi `KHACHHANG.MaHang` | SQL xếp lại khách trước khi xóa hạng cũ; test có đếm khách mồ côi = 0 |
| Ẩn refresh ảnh hưởng list khác | Chỉ ẩn trong nhánh profile `Ranks`; test AR-11 trên Khuyến mãi/Điểm |
| Parse số ở UI lệch contract | Dùng `int.TryParse` + để `NormalizeTiers`/SQL là nguồn chặn cuối |

## Tiêu chí hoàn tất

- Code chỉ đụng `OrbitRecordListControl.cs` và `LoyaltyControl.cs` (phần `PolicyList`).
- Build 0 error; không migration/procedure mới.
- Đợt test AR-01..AR-11 PASS trên DB test riêng sau khi được duyệt; AR-12 người dùng
  tự kiểm hình thức.
- Báo cáo bàn giao nêu rõ phần đã chạy, phần chờ duyệt test và không tuyên bố PASS
  trước khi có biên bản.
