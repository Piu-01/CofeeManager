using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess.Repositories;
using CafeManager.WinForms.Forms;

namespace CafeManager.WinForms;

internal sealed class ApplicationSessionContext(AuthService? auth, PermissionService? permissions,
    AuthRepository? repository, string? configurationError, Func<SessionContext, UserControl>? catalogs = null, Func<SessionContext, IReadOnlyDictionary<Navigation.NavigationTarget, Func<UserControl>>>? modules = null) : ApplicationContext
{
    private LoginForm? login;
    private MainForm? main;
    private SessionContext? session;
#if DEBUG
    internal Func<CancellationToken, Task<SessionContext>>? TemporaryTestLogin { get; set; }
#endif
    public void Start() => ShowLogin();
    private void ShowLogin()
    {
        login = new LoginForm(auth, configurationError);
#if DEBUG
        if (TemporaryTestLogin is not null) login.ConfigureTemporaryTestLogin(TemporaryTestLogin);
#endif
        login.FormClosed += (_, _) => { if (main is null) ExitThread(); };
        login.LoggedIn += LoggedIn;
        login.Show();
    }
    private void LoggedIn(SessionContext current)
    {
        if (main is not null) { current.Dispose(); return; }
        session = current;
        main = new MainForm(current, permissions!, repository!.ReadStateAsync, catalogs is null ? null : () => catalogs(current), modules?.Invoke(current));
        main.LoggedOut += () =>
        {
            main = null;
            session?.Dispose(); session = null;
            login?.Dispose(); login = null;
            ShowLogin();
        };
        main.FormClosed += (_, _) => { if (main is not null) { session?.Dispose(); ExitThread(); } };
        login!.Hide(); main.Show();
    }
    protected override void ExitThreadCore()
    {
        session?.Dispose(); main?.Dispose(); login?.Dispose(); base.ExitThreadCore();
    }
}
