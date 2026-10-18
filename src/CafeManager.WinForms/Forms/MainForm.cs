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
    private bool compactNavigation;
    private bool arrangingNavigation;
    public MainForm()
    {
        InitializeComponent(); AppTheme.Apply(this);
        sidebar.BackColor = brandLabel.BackColor = brandCaption.BackColor = AppTheme.Espresso;
        brandLabel.ForeColor = AppTheme.Primary; brandCaption.ForeColor = AppTheme.Muted;
        identityLabel.BackColor = AppTheme.Espresso; identityLabel.ForeColor = AppTheme.Muted;
        AppTheme.Status(statusLabel);
        AppTheme.SetIcon(homeButton, "coffee"); AppTheme.SetIcon(catalogsButton, "layout-grid");
        AppTheme.SetIcon(systemButton, "settings"); AppTheme.SetIcon(logoutButton, "log-out");
        ClientSizeChanged += (_, _) => ArrangeNavigation();
        ArrangeNavigation();
    }

    // Chỉ đổi vị trí control hiện có; không điều hướng lại hoặc reset nháp khi resize.
    private void ArrangeNavigation()
    {
        if (arrangingNavigation) return;
        var compact = ClientSize.Width * 96F / DeviceDpi < 1000;
        if (compact == compactNavigation) return;
        arrangingNavigation = true; compactNavigation = compact;
        layout.SuspendLayout(); sidebar.SuspendLayout();
        try
        {
            layout.SetRowSpan(sidebar, 1);
            layout.ColumnStyles.Clear(); layout.RowStyles.Clear();
            layout.ColumnCount = compact ? 1 : 2; layout.RowCount = compact ? 4 : 3;
            if (!compact) layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 232 * DeviceDpi / 96F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            if (compact) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.SetCellPosition(sidebar, new TableLayoutPanelCellPosition(0, 0));
            layout.SetCellPosition(identityLabel, new TableLayoutPanelCellPosition(compact ? 0 : 1, compact ? 1 : 0));
            layout.SetCellPosition(content, new TableLayoutPanelCellPosition(compact ? 0 : 1, compact ? 2 : 1));
            layout.SetCellPosition(statusLabel, new TableLayoutPanelCellPosition(compact ? 0 : 1, compact ? 3 : 2));
            layout.SetRowSpan(sidebar, compact ? 1 : 3);
            brandLabel.Visible = brandCaption.Visible = !compact;
            sidebar.Controls.SetChildIndex(brandLabel, 0);
            sidebar.Controls.SetChildIndex(brandCaption, 1);
            sidebar.AutoSize = compact; sidebar.WrapContents = compact;
            sidebar.FlowDirection = compact ? FlowDirection.LeftToRight : FlowDirection.TopDown;
            sidebar.Padding = compact ? new Padding(8) : new Padding(16, 24, 16, 16);
            identityLabel.Padding = compact ? new Padding(12, 4, 12, 4) : new Padding(16, 10, 16, 10);
            foreach (var button in new[] { homeButton, catalogsButton, systemButton, logoutButton })
            {
                button.AutoSize = compact;
                button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                button.Margin = compact ? new Padding(3) : new Padding(0, 0, 0, 10);
                button.Padding = compact ? new Padding(8, 4, 8, 4) : Padding.Empty;
                if (!compact) button.Size = new Size((int)(194 * DeviceDpi / 96F), (int)(52 * DeviceDpi / 96F));
            }
        }
        finally { sidebar.ResumeLayout(true); layout.ResumeLayout(true); arrangingNavigation = false; }
    }
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
                { button.BackColor = AppTheme.Espresso; button.ForeColor = AppTheme.Muted; }
                var selected = target switch { NavigationTarget.Home => homeButton, NavigationTarget.Catalogs => catalogsButton, _ => systemButton };
                selected.BackColor = AppTheme.Selected; selected.ForeColor = AppTheme.Primary;
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
