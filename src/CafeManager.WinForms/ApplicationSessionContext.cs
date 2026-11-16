using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess.Repositories;
using CafeManager.WinForms.Forms;

namespace CafeManager.WinForms;

internal sealed class ApplicationSessionContext(AuthService? auth, PermissionService? permissions,
    AuthRepository? repository, string? configurationError, Func<SessionContext, UserControl>? catalogs = null, Func<SessionContext, IReadOnlyDictionary<Navigation.NavigationTarget, Func<UserControl>>>? modules = null,
    Configuration.DevelopmentQuickLogin? quickLogin = null) : ApplicationContext
{
    private LoginForm? login;
    private MainForm? main;
    private SessionContext? session;
    public void Start() => ShowLogin();
    private void ShowLogin()
    {
        LanguageService.Set(new Configuration.LanguagePreference().Read());
        login = new LoginForm(auth, configurationError, quickLogin);
        login.FormClosed += (_, _) => { if (main is null) ExitThread(); };
        login.LoggedIn += LoggedIn;
        login.Show();
    }
    private async void LoggedIn(SessionContext current)
    {
        if (main is not null) { current.Dispose(); return; }
        session = current;
        var shell = CreateMain(current, null);
        main = shell;
        AttachMain(shell);
        login!.Hide();
        // Hiện shell ở độ mờ 0 để màn đầu (dashboard) dựng xong rồi mới cho thấy, tránh cảnh
        // khung màn hình hiện trước rồi nội dung mới lấp vào.
        shell.Opacity = 0;
        shell.Show();
        try { await shell.StartAsync(); }
        finally { if (!shell.IsDisposed) shell.Opacity = 1; }
    }

    private MainForm CreateMain(SessionContext current, Navigation.NavigationTarget? target) =>
        new(current, permissions!, repository!.ReadStateAsync, catalogs is null ? null : () => catalogs(current), modules?.Invoke(current), target);

    // Event gắn theo instance form để shell cũ đóng lúc đổi ngôn ngữ không làm thoát app.
    private void AttachMain(MainForm form)
    {
        form.LoggedOut += () =>
        {
            if (!ReferenceEquals(main, form)) return;
            main = null;
            session?.Dispose(); session = null;
            login?.Dispose(); login = null;
            ShowLogin();
        };
        form.FormClosed += (_, _) =>
        {
            if (!ReferenceEquals(main, form)) return;
            session?.Dispose();
            ExitThread();
        };
        form.ShellReloadRequested += () => ReloadShell(form);
    }

    private async void ReloadShell(MainForm old)
    {
        if (!ReferenceEquals(main, old) || session is null || old.Dialogs.IsOpen) return;
        var target = old.CurrentTarget;
        var bounds = old.WindowState == FormWindowState.Normal ? old.Bounds : old.RestoreBounds;
        var state = old.WindowState;
        var next = CreateMain(session, target);
        main = next;
        AttachMain(next);
        next.StartPosition = FormStartPosition.Manual;
        next.Bounds = bounds;
        next.WindowState = state;
        next.Opacity = 0;
        next.Show();
        // Giữ shell cũ trên màn hình trong lúc shell mới dựng màn đầu, rồi mới đổi.
        try { await next.StartAsync(); } finally { if (!next.IsDisposed) next.Opacity = 1; }
        old.Hide();
        old.CloseForShellSwap();
    }
    protected override void ExitThreadCore()
    {
        session?.Dispose(); main?.Dispose(); login?.Dispose(); base.ExitThreadCore();
    }
}
