using CafeManager.Business.Security;
using CafeManager.Models.Dtos;
using CafeManager.WinForms.Modules.Home;
using CafeManager.WinForms.Modules.System;
using CafeManager.WinForms.Navigation;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.Forms;

public partial class MainForm : Form
{
    private readonly SessionContext? session;
    private readonly NavigationController? navigation;
    private readonly Func<CancellationToken, Task<AppSetupStatus>>? readState;
    private readonly Func<UserControl>? catalogs;
    public event Action? LoggedOut;
    private bool loggingOut;
    private bool navigating;
    public MainForm() { InitializeComponent(); AppTheme.Apply(this); sidebar.BackColor = AppTheme.Espresso; }
    public MainForm(SessionContext session, PermissionService permissions,
        Func<CancellationToken, Task<AppSetupStatus>> readState, Func<UserControl>? catalogs = null) : this()
    {
        this.session = session; this.readState = readState; this.catalogs = catalogs;
        session.Revoked += SessionRevoked;
        navigation = new NavigationController(content, session, permissions);
        identityLabel.Text = $"{session.Identity.EmployeeName} · {session.Identity.Role}";
        catalogsButton.Visible = session.Identity.Role == "ADMIN" && catalogs is not null;
        Shown += async (_, _) => await Navigate(session.Identity.Role == "ADMIN" && catalogs is not null ? NavigationTarget.Catalogs : NavigationTarget.Home);
    }
    private async Task Navigate(NavigationTarget target)
    {
        if (navigation is null || session is null || navigating || loggingOut) return;
        navigating = true;
        homeButton.Enabled = catalogsButton.Enabled = systemButton.Enabled = false;
        try
        {
            var changed = await navigation.NavigateAsync(target, () => target switch
            {
                NavigationTarget.Home => new SessionHomeControl(session.Identity),
                NavigationTarget.Catalogs => catalogs!(),
                _ => new SystemInfoControl()
            }, this);
            if (!changed) return;
            if (!IsDisposed && !loggingOut)
            {
                foreach (var button in new[] { homeButton, catalogsButton, systemButton })
                { button.BackColor = AppTheme.Espresso; button.ForeColor = Color.White; }
                var selected = target switch { NavigationTarget.Home => homeButton, NavigationTarget.Catalogs => catalogsButton, _ => systemButton };
                selected.BackColor = AppTheme.Selected; selected.ForeColor = AppTheme.Text;
            }
            if (target == NavigationTarget.SystemInfo && content.Controls.Count == 1 && content.Controls[0] is SystemInfoControl info)
            {
                var state = await readState!(session.Token); session.EnsureCurrent();
                if (!IsDisposed) info.SetSchema(state.SchemaVersion);
            }
            if (!IsDisposed && !loggingOut) statusLabel.Text = "Phiên đang hoạt động.";
        }
        catch (OperationCanceledException) when (session.IsRevoked) { }
        catch (UnauthorizedAccessException ex) { if (!IsDisposed) statusLabel.Text = ex.Message; if (session.IsRevoked && !IsDisposed) Logout(); }
        catch (CafeManager.DataAccess.DataAccessException ex) { if (!IsDisposed) statusLabel.Text = ex.Error.Message; }
        finally
        {
            navigating = false;
            if (!IsDisposed && !loggingOut) homeButton.Enabled = catalogsButton.Enabled = systemButton.Enabled = true;
        }
    }
    private async void Home_Click(object? sender, EventArgs e) => await Navigate(NavigationTarget.Home);
    private async void Catalogs_Click(object? sender, EventArgs e) => await Navigate(NavigationTarget.Catalogs);
    private async void System_Click(object? sender, EventArgs e) => await Navigate(NavigationTarget.SystemInfo);
    private void Logout_Click(object? sender, EventArgs e) { if (navigation?.ConfirmLeave(this) != false) Logout(); }
    private void Logout()
    {
        if (loggingOut) return;
        loggingOut = true; session?.Revoke(); navigation?.Dispose(); LoggedOut?.Invoke(); Close();
    }
    private void SessionRevoked()
    {
        if (loggingOut || IsDisposed || !IsHandleCreated) return;
        // Queue teardown after the protected call unwinds, including a modal editor.
        BeginInvoke((Action)(() => { if (!IsDisposed && !loggingOut) Logout(); }));
    }
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!loggingOut && navigation?.ConfirmLeave(this) == false) e.Cancel = true;
        base.OnFormClosing(e);
        if (!e.Cancel) { if (session is not null) session.Revoked -= SessionRevoked; session?.Revoke(); navigation?.Dispose(); }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { if (session is not null) session.Revoked -= SessionRevoked; navigation?.Dispose(); }
        base.Dispose(disposing);
    }
}
