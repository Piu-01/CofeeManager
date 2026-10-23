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
    private readonly IReadOnlyDictionary<NavigationTarget, Func<UserControl>> modules = new Dictionary<NavigationTarget, Func<UserControl>>();
    public event Action? LoggedOut;
    private bool loggingOut;
    private bool navigating;

    private bool arrangingNavigation;
    public MainForm()
    {
        InitializeComponent(); AppTheme.Apply(this);




        UI.OrbitManagementLayout.Icon(homeButton, "coffee"); UI.OrbitManagementLayout.Icon(catalogsButton, "layout-grid");
        UI.OrbitManagementLayout.Icon(systemButton, "settings"); UI.OrbitManagementLayout.Icon(logoutButton, "log-out");
        foreach (var item in sidebar.Controls.OfType<UI.OrbitButton>()) { item.AlignTextLeft = true; item.Quiet = true; }
        sidebar.BackColor = OrbitManagementTheme.Sidebar;
        brandLabel.BackColor = brandCaption.BackColor = OrbitManagementTheme.Sidebar;
        brandLabel.Font = OrbitManagementTheme.Brand; brandLabel.ForeColor = OrbitManagementTheme.Text;
        brandCaption.Font = OrbitManagementTheme.Header; brandCaption.ForeColor = OrbitManagementTheme.Muted;
        topbar.BackColor = identityLabel.BackColor = breadcrumbLabel.BackColor = OrbitManagementTheme.Background;
        identityLabel.Font = breadcrumbLabel.Font = OrbitManagementTheme.Small;
        identityLabel.ForeColor = breadcrumbLabel.ForeColor = OrbitManagementTheme.Muted;
        foreach (var button in sidebar.Controls.OfType<Button>()) ConfigureNavigation(button);
        statusLabel.BackColor = OrbitManagementTheme.Sidebar; statusLabel.Font = OrbitManagementTheme.Header;
        statusLabel.Padding = Padding.Empty; statusLabel.AutoSize = false;
        ((UI.OrbitSidebar)sidebar).SessionStatus = statusLabel;
        ((UI.OrbitSidebar)sidebar).Person.ForeColor = OrbitManagementTheme.Text;
        ((UI.OrbitSidebar)sidebar).Role.ForeColor = OrbitManagementTheme.Muted;
        ClientSizeChanged += (_, _) => ArrangeNavigation();
        ArrangeNavigation();
        Shown += (_, _) => { UpdateMinimumSize(); ArrangeNavigation(); };
        DpiChanged += (_, _) => { UpdateMinimumSize(); ArrangeNavigation(); };
    }

    private static void ConfigureNavigation(Button button)
    {
        if (button is not UI.OrbitButton orbit) return;
        var icon = button.Text switch { "Menu / Công thức" => "book", "Ưu đãi / Điểm" => "ticket", "Thông tin hệ thống" => "settings", _ => "" };
        OrbitManagementTheme.Button(orbit, ManagementButtonStyle.Navigation, icon);
    }

    private void UpdateMinimumSize()
    {
        var area = Screen.FromControl(this).WorkingArea;
        var scale = DeviceDpi / 96F;
        // Hạ giới hạn theo vùng Snap thật ở DPI cao; không thu font hoặc cắt hành động.
        MinimumSize = new Size(Math.Max(1, Math.Min((int)(640 * scale), area.Width / 2)),
            Math.Max(1, Math.Min((int)(480 * scale), area.Height - 48)));
    }

    // Shell chỉ đổi bố cục, không tạo lại module hoặc reset nháp khi resize.
    private void ArrangeNavigation()
    {
        if (arrangingNavigation) return;
        arrangingNavigation = true;
        try
        {
            var compact = ClientSize.Width * 96F / DeviceDpi < 760;
            var navigationSidebar = (UI.OrbitSidebar)sidebar;
            navigationSidebar.Compact = compact;
            layout.ColumnStyles[0].Width = (compact ? 62 : 200) * DeviceDpi / 96F;
            layout.RowStyles[0].Height = 50 * DeviceDpi / 96F;
            navigationSidebar.PerformLayout();
        }
        finally { arrangingNavigation = false; }
    }
    public MainForm(SessionContext session, PermissionService permissions,
        Func<CancellationToken, Task<AppSetupStatus>> readState, Func<UserControl>? catalogs = null, IReadOnlyDictionary<NavigationTarget, Func<UserControl>>? modules = null) : this()
    {
        this.session = session; this.readState = readState; this.catalogs = catalogs;
        this.modules = modules ?? new Dictionary<NavigationTarget, Func<UserControl>>();
        foreach (var target in this.modules.Keys.Where(target => target is not (NavigationTarget.Tables or NavigationTarget.Customers or NavigationTarget.Loyalty) || session.Identity.Role is "ADMIN" or "CASH"))
        {
            var button = new UI.OrbitButton { Tag = target, Text = target switch { NavigationTarget.Materials => "Nguyên liệu / Tồn", NavigationTarget.Products => "Menu / Công thức", NavigationTarget.Tables => "Bàn / Khu vực", NavigationTarget.Customers => "Khách hàng", NavigationTarget.Loyalty => "Ưu đãi / Điểm", _ => target.ToString() }, AutoSize = false, Size = new Size(168, 44), Margin = new Padding(0, 0, 0, 10) };
            button.AlignTextLeft = true; button.Quiet = true; AppTheme.Apply(button); UI.OrbitManagementLayout.Icon(button, target switch { NavigationTarget.Materials => "package", NavigationTarget.Products => "receipt", NavigationTarget.Tables => "armchair", NavigationTarget.Customers => "users", NavigationTarget.Loyalty => "wallet", _ => "settings" }); sidebar.Controls.Add(button); sidebar.Controls.SetChildIndex(button, sidebar.Controls.Count - 2);
            ConfigureNavigation(button);
            button.Click += async (_, _) => await Navigate(target);
        }
        var order=new[]{"Trang phiên","Danh mục nền","Khách hàng","Bàn / Khu vực","Menu / Công thức","Nguyên liệu / Tồn","Ưu đãi / Điểm","Thông tin hệ thống","Đăng xuất"};
        for(var i=0;i<order.Length;i++){var item=sidebar.Controls.OfType<Button>().FirstOrDefault(b=>b.Text==order[i]);if(item is not null)sidebar.Controls.SetChildIndex(item,Math.Min(i+2,sidebar.Controls.Count-1));}
        ArrangeNavigation();
        session.Revoked += SessionRevoked;
        navigation = new NavigationController(content, session, permissions);
        identityLabel.Text = $"{session.Identity.EmployeeName} · {session.Identity.Role}";
        ((UI.OrbitSidebar)sidebar).Person.Text = session.Identity.EmployeeName;
        ((UI.OrbitSidebar)sidebar).Role.Text = $"{(session.Identity.Role == "ADMIN" ? "Quản trị viên" : "Nhân viên")} · {session.Identity.Role}";
        catalogsButton.Visible = session.Identity.Role == "ADMIN" && catalogs is not null;
        Shown += async (_, _) => await Navigate(session.Identity.Role == "ADMIN" && catalogs is not null ? NavigationTarget.Catalogs : NavigationTarget.Home);
    }
    internal void SetWorkspaceTitle(string caption) { breadcrumbLabel.Text = caption == "Nguyên liệu / Tồn" ? "Kho nguyên liệu  /  Tồn hiện tại" : "Quản lý / " + caption; }
    private async Task Navigate(NavigationTarget target)
    {
        if (navigation is null || session is null || navigating || loggingOut) return;
        navigating = true;
        homeButton.Enabled = catalogsButton.Enabled = systemButton.Enabled = false;
        try
        {
            var changed = await navigation.NavigateAsync(target, () => modules.TryGetValue(target, out var factory) ? factory() : target switch
            {
                NavigationTarget.Home => new SessionHomeControl(session.Identity),
                NavigationTarget.Catalogs => catalogs!(),
                _ => new SystemInfoControl()
            }, this);
            if (!changed) return;
            if (!IsDisposed && !loggingOut)
            {
                foreach (var button in sidebar.Controls.OfType<Button>().Where(b => b != logoutButton))
                { button.BackColor = AppTheme.Espresso; button.ForeColor = AppTheme.Muted; if (button is UI.OrbitButton item) { item.NavigationSelected = false; item.Invalidate(); } }
                var selected = sidebar.Controls.OfType<Button>().FirstOrDefault(b => b.Tag is NavigationTarget tagged && tagged == target) ?? (target switch { NavigationTarget.Home => homeButton, NavigationTarget.Catalogs => catalogsButton, _ => systemButton });
                selected.BackColor = AppTheme.Selected; selected.ForeColor = AppTheme.Primary; if (selected is UI.OrbitButton selectedItem) { selectedItem.NavigationSelected = true; selectedItem.Invalidate(); } SetWorkspaceTitle(selected.Text);
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









