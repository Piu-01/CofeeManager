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
    private bool closingMain, closeApproved;
    public UI.OrbitDialogHost Dialogs { get; }

    private bool arrangingNavigation;
    public MainForm()
    {
        InitializeComponent(); AppTheme.Apply(this);
        Dialogs = new UI.OrbitDialogHost(this);
        UI.OrbitField.ApplyInputBehavior(this);




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
        // Menu theo công việc; CASH chọn bàn/món/ưu đãi trong POS, không mở màn quản trị.
        NavigationTarget[] menuTargets = session.Identity.Role switch
        {
            "ADMIN" => [NavigationTarget.Sales, NavigationTarget.Invoices, NavigationTarget.Delivery, NavigationTarget.Customers, NavigationTarget.Tables, NavigationTarget.Products, NavigationTarget.Materials, NavigationTarget.Loyalty, NavigationTarget.Finance, NavigationTarget.Inventory, NavigationTarget.Debt],
            "CASH" => [NavigationTarget.Sales, NavigationTarget.Invoices, NavigationTarget.Delivery, NavigationTarget.Customers, NavigationTarget.Materials],
            "STORE" => [NavigationTarget.Inventory, NavigationTarget.Materials, NavigationTarget.Products, NavigationTarget.Debt],
            _ => []
        };
        foreach (var target in menuTargets.Where(this.modules.ContainsKey))
        {
            var button = new UI.OrbitButton { Tag = target, Text = target switch { NavigationTarget.Delivery => "Giao hàng", NavigationTarget.Invoices => "Lịch sử hóa đơn", NavigationTarget.Sales => "Bán hàng", NavigationTarget.Materials => session.Identity.Role == "CASH" ? "Tồn phục vụ bán" : "Nguyên liệu / Tồn", NavigationTarget.Products => "Menu / Công thức", NavigationTarget.Tables => "Bàn / Khu vực", NavigationTarget.Customers => "Khách hàng", NavigationTarget.Debt => "Công nợ", NavigationTarget.Inventory => "Kho", NavigationTarget.Finance => "Thu chi", NavigationTarget.Loyalty => "Ưu đãi / Điểm", _ => target.ToString() }, AutoSize = false, Size = new Size(168, 44), Margin = new Padding(0, 0, 0, 10) };
            button.AlignTextLeft = true; button.Quiet = true; AppTheme.Apply(button); UI.OrbitManagementLayout.Icon(button, target switch { NavigationTarget.Delivery => "receipt", NavigationTarget.Invoices => "receipt", NavigationTarget.Sales => "receipt", NavigationTarget.Materials => "package", NavigationTarget.Products => "receipt", NavigationTarget.Tables => "armchair", NavigationTarget.Customers => "users", NavigationTarget.Finance => "wallet", NavigationTarget.Loyalty => "wallet", _ => "settings" }); sidebar.Controls.Add(button); sidebar.Controls.SetChildIndex(button, sidebar.Controls.GetChildIndex(systemButton));
            ConfigureNavigation(button);
            button.Click += async (_, _) => await Navigate(target);
        }
        ArrangeNavigation();
        session.Revoked += SessionRevoked;
        navigation = new NavigationController(content, session, permissions);
        identityLabel.Text = $"{session.Identity.EmployeeName} · {session.Identity.Role}";
        ((UI.OrbitSidebar)sidebar).Person.Text = session.Identity.EmployeeName;
        var roleLabel = session.Identity.Role switch { "ADMIN" => "Quản trị viên", "CASH" => "Thu ngân", "STORE" => "Quản lý kho", _ => "Nhân viên" };
        ((UI.OrbitSidebar)sidebar).Role.Text = $"{roleLabel} · {session.Identity.Role}";
        brandCaption.Text = session.Identity.Role switch { "CASH" => "KHÔNG GIAN THU NGÂN", "STORE" => "KHÔNG GIAN KHO", _ => "KHÔNG GIAN QUẢN LÝ" };
        catalogsButton.Visible = session.Identity.Role == "ADMIN" && catalogs is not null;
        Shown += async (_, _) => await Navigate(session.Identity.Role == "CASH" && this.modules.ContainsKey(NavigationTarget.Sales) ? NavigationTarget.Sales : NavigationTarget.Home);
    }
    internal async Task OpenSalesOrderAsync(string code)
    {
        await Navigate(NavigationTarget.Sales);
        if (content.Controls.OfType<Modules.Sales.SalesControl>().FirstOrDefault() is { } sales && sales.Visible) await sales.OpenOrderAsync(code);
    }
    internal async Task OpenSalesTableAsync(string code)
    {
        await Navigate(NavigationTarget.Sales);
        if (content.Controls.OfType<Modules.Sales.SalesControl>().FirstOrDefault() is { } sales && sales.Visible) await sales.OpenTableAsync(code);
    }
    internal void SetWorkspaceTitle(string caption)
    {
        var workspace = session?.Identity.Role switch { "CASH" => "Thu ngân", "STORE" => "Kho", _ => "Quản lý" };
        breadcrumbLabel.Text = caption == "Nguyên liệu / Tồn" ? "Kho nguyên liệu  /  Tồn hiện tại" : workspace + " / " + caption;
    }
    private async Task Navigate(NavigationTarget target)
    {
        if (navigation is null || session is null || navigating || loggingOut || Dialogs.IsOpen) return;
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
    private async void Logout_Click(object? sender, EventArgs e)
    { if (!Dialogs.IsOpen && (navigation is null || await navigation.ConfirmLeaveAsync(this))) Logout(); }
    private void Logout()
    {
        if (loggingOut) return;
        loggingOut = true; Dialogs.AbortAll(); session?.Revoke(); navigation?.Dispose(); LoggedOut?.Invoke(); Close();
    }
    private void SessionRevoked()
    {
        if (loggingOut || IsDisposed || !IsHandleCreated) return;
        // Queue teardown after the protected call unwinds, including a modal editor.
        BeginInvoke((Action)(() => { if (!IsDisposed && !loggingOut) Logout(); }));
    }
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!loggingOut && !closeApproved)
        {
            e.Cancel = true;
            if (!closingMain)
            {
                closingMain = true;
                BeginInvoke((Action)(async () =>
                {
                    try
                    {
                        if (!await Dialogs.CloseAllAsync() || navigation is not null && !await navigation.ConfirmLeaveAsync(this)) return;
                        closeApproved = true; Close();
                    }
                    finally { closingMain = false; }
                }));
            }
        }
        base.OnFormClosing(e);
        if (!e.Cancel) { if (session is not null) session.Revoked -= SessionRevoked; session?.Revoke(); navigation?.Dispose(); }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { Dialogs?.Dispose(); if (session is not null) session.Revoked -= SessionRevoked; navigation?.Dispose(); }
        base.Dispose(disposing);
    }
}

















