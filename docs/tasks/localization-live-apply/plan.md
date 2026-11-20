# Đổi ngôn ngữ áp dụng ngay trong phiên — plan

Ngày soạn: 10/11/2026. Trạng thái: **Chờ duyệt plan**.
Requirements: `requirements.md` (đã duyệt 10/11/2026).
Task này thay phương án áp dụng (B) của `docs/tasks/localization-vi-en/` bằng (A) có
guard nháp. Không sửa nội dung dịch, từ điển, SQL hay nghiệp vụ.

## 0. Nguyên tắc

1. Chỉ dựng lại **shell** (`MainForm`) với cùng `SessionContext`; không relocalize từng
   control, không đụng các chỗ `L.T`/`L.F` hiện có.
2. Dùng nguyên `NavigationController.ConfirmLeaveAsync` cho guard nháp/busy; không viết
   cơ chế chặn mới.
3. Không revoke phiên, không đóng/mở lại `LoginForm`, không đổi luồng logout thường.
4. Không đổi SQL/migration/contract/quyền. Không sửa `en.json`.
5. Build phải pass sau mỗi bước; không chạy đợt test nghiệm thu trong lúc triển khai.

## 1. `SystemInfoControl` — chuyển quyền quyết định cho shell

File: `src/CafeManager.WinForms/Modules/System/SystemInfoControl.cs`

- Bỏ toàn bộ nhánh lưu + hộp "Đã lưu. Ngôn ngữ áp dụng khi đăng xuất…" và
  `LogoutRequested` (hiện chỉ phục vụ luồng này).
- Thêm `internal Func<AppLanguage, Task>? ApplyLanguage { get; set; }`.
- `apply.Click`: chặn tái nhập bằng `apply.Enabled = false` trong `try/finally`, đọc
  `languages.SelectedIndex`, gọi `await ApplyLanguage(language)` nếu có. Không tự đọc/ghi
  file preference, không tự `LanguageService.Set` — shell quyết định sau khi guard xong.
- Giữ nguyên cách nạp danh sách ngôn ngữ và `SelectedIndex` từ `LanguagePreference().Read()`.
- Verify: build WinForms pass; màn Thông tin hệ thống không còn tự lưu file.

## 2. `MainForm` — guard, lưu preference, yêu cầu dựng lại shell

File: `src/CafeManager.WinForms/Forms/MainForm.cs`

- Thêm `public event Action? ShellReloadRequested;` và
  `internal NavigationTarget CurrentTarget => workspaceTarget;`.
- Thêm cờ `changingLanguage`; `internal async Task ApplyLanguageAsync(AppLanguage language)`:
  1. Nếu `navigating || loggingOut || Dialogs.IsOpen || navigation is null || session is null`
     hoặc `changingLanguage` thì return.
  2. `changingLanguage = true`; gọi `await navigation.ConfirmLeaveAsync(this)`; false →
     return (giữ nguyên màn/nháp/ngôn ngữ).
  3. `new Configuration.LanguagePreference().TrySave(language)` false → hiện
     `OrbitConfirmationDialog` báo không lưu được (owner `this`), return.
  4. `LanguageService.Current == language` → return (đã lưu, không cần dựng lại).
  5. `LanguageService.Set(language)`; `ShellReloadRequested?.Invoke()`.
  6. `finally { changingLanguage = false; }`.
- `CreateSystemInfo()`: gán `info.ApplyLanguage = ApplyLanguageAsync` (bỏ wiring
  `LogoutRequested`).
- Thêm `internal void CloseForShellSwap()`: đặt `closeApproved = true`, cờ
  `keepSessionOnClose = true`, gọi `Close()`. Trong `OnFormClosing`, nhánh cuối:
  vẫn hủy đăng ký `SessionRevoked` và `navigation?.Dispose()`, nhưng **không** gọi
  `session.Revoke()` khi `keepSessionOnClose`.
- Ctor đầy đủ: thêm tham số tùy chọn cuối `NavigationTarget? initialTarget = null`;
  handler `Shown` điều hướng `initialTarget ?? (CASH ? Sales : Home)` thay vì luôn
  Sales/Home. Không đổi hành vi khi gọi lần đầu (`initialTarget = null`).
- Verify: build pass; đóng app bình thường vẫn revoke/exit như cũ; không còn tham chiếu
  `LogoutRequested`.

## 3. `ApplicationSessionContext` — dựng lại shell an toàn

File: `src/CafeManager.WinForms/ApplicationSessionContext.cs`

- Tách `private MainForm CreateMain(SessionContext current, NavigationTarget? target)`
  từ thân `LoggedIn`, dùng đúng args sẵn có (`permissions`, `repository.ReadStateAsync`,
  `catalogs`, `modules?.Invoke(current)`).
- Gắn event theo **instance form được tạo** (closure), không dựa biến `main` toàn cục:
  - `form.LoggedOut`: chỉ xử lý nếu `ReferenceEquals(main, form)`; dọn session/login và
    `ShowLogin()` như hiện tại.
  - `form.FormClosed`: chỉ `session?.Dispose(); ExitThread()` nếu `ReferenceEquals(main, form)`.
    Điều này để form cũ đóng lúc swap không làm thoát app/dọn phiên.
  - `form.ShellReloadRequested += ReloadShell`.
- `ReloadShell()` (chạy trên UI thread, cùng một message handler nên không chen ngang
  `session.Revoked`):
  1. Nếu `main`/`session` null hoặc `main.Dialogs.IsOpen` → return.
  2. Lưu `target = main.CurrentTarget`, `Bounds`, `WindowState`, `WindowState` restore.
  3. `var next = CreateMain(session, target)`; gán `main = next`;
     `next.StartPosition = Manual`, `next.Bounds = bounds`, `next.WindowState = state`.
  4. `next.Show()`; `old.Hide()`; `old.CloseForShellSwap()`.
- `LoggedIn` dùng `CreateMain(current, null)`; giữ guard `main is not null`.
- Verify: build pass; không còn handler FormClosed/LoggedOut dùng biến `main` trực tiếp.

## 4. Ghi chú thay thế trong hồ sơ localization cũ

Files: `docs/tasks/localization-vi-en/requirements.md` (mục 6) và `plan.md` (phần đầu).

- Thêm 2–3 dòng: ngày 10/11/2026 người dùng duyệt đổi sang áp dụng ngay trong phiên;
  chi tiết tại `docs/tasks/localization-live-apply/`. Không sửa/xóa lịch sử test,
  LOC-B01–B03, checklist hay kết quả cũ.

## 5. Kiểm tra kỹ thuật trong lúc triển khai

- `dotnet build` solution (Debug, rồi Release) pass, không warning mới.
- Rà `grep LogoutRequested` = 0 kết quả; `grep "áp dụng khi đăng xuất"` = 0 trong source.
- Phát sinh nhỏ khi triển khai: `tools/localization audit` tính key không dùng là vi phạm,
  nên gỡ đúng 3 dòng từ điển chết do bỏ luồng cũ trong `en.json`:
  "Đã lưu. Ngôn ngữ áp dụng khi đăng xuất hoặc mở lại ứng dụng.", "Đăng xuất ngay",
  "Để sau". Không đổi bản dịch nào khác; audit còn đúng 8 vi phạm "Missing" có sẵn từ
  trước (không thuộc task này).
- Không chạy đợt test nghiệm thu; việc test chức năng/regression chờ người dùng duyệt
  đợt riêng theo `post-change-test-workflow`.

## 6. Rủi ro và cách xử lý đã tính

- **Nháp POS/Danh mục**: hỏi bằng `ConfirmLeaveAsync`; hủy → không đổi gì (cả file).
- **Đang ghi/unknown**: `ConfirmLeaveAsync` chặn thẳng, chỉ báo hoàn tất bước hiện tại.
- **Dialog đang mở**: modal khóa form nên không bấm được Áp dụng; `ReloadShell` vẫn
  kiểm `Dialogs.IsOpen` làm lớp bảo vệ.
- **Hai shell cùng đăng ký `session.Revoked`** trong khoảnh khắc swap: form cũ hủy đăng
  ký ngay khi `Close()`, toàn bộ swap nằm trong một handler UI thread nên không có
  interleave; form cũ không revoke session.
- **Mất trạng thái tạm** (từ lọc/trang/vị trí grid) của màn đang mở: chấp nhận theo
  requirements; màn được mở lại và tự tải dữ liệu.
- **Vị trí/kích thước cửa sổ**: sao chép `Bounds`/`WindowState` sang shell mới.
- **Lỗi khi đóng app ngay sau swap**: `FormClosed` chỉ thoát khi form hiện hành đóng;
  shell cũ đã bị bỏ tham chiếu `main`.
- **CASH/STORE**: giữ đúng màn đang xem; `NavigateAsync` vẫn kiểm quyền như thường.
