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
    public event Action? ShellReloadRequested;
    internal NavigationTarget CurrentTarget => workspaceTarget;
    private bool loggingOut;
    private bool navigating;
    private bool closingMain, closeApproved;
    private bool changingLanguage, keepSessionOnClose;
    public UI.OrbitDialogHost Dialogs { get; }

    private bool arrangingNavigation;
    private NavigationTarget workspaceTarget;
    public MainForm()
    {
        InitializeComponent(); AppTheme.Apply(this);
        homeButton.Tag = NavigationTarget.Home; catalogsButton.Tag = NavigationTarget.Catalogs; systemButton.Tag = NavigationTarget.SystemInfo;
        Dialogs = new UI.OrbitDialogHost(this);
        UI.OrbitField.ApplyInputBehavior(this);




        UI.OrbitManagementLayout.Icon(homeButton, "coffee"); UI.OrbitManagementLayout.Icon(catalogsButton, "layout-grid");
        UI.OrbitManagementLayout.Icon(systemButton, "settings"); UI.OrbitManagementLayout.Icon(logoutButton, "log-out");
        foreach (var item in new[] { homeButton, catalogsButton, systemButton, logoutButton }.OfType<UI.OrbitButton>()) { item.AlignTextLeft = true; item.Quiet = true; }
        sidebar.BackColor = OrbitManagementTheme.Sidebar;
        brandLabel.BackColor = brandCaption.BackColor = OrbitManagementTheme.Sidebar;
        brandLabel.Font = OrbitManagementTheme.Brand; brandLabel.ForeColor = OrbitManagementTheme.Text;
        brandCaption.Font = OrbitManagementTheme.Header; brandCaption.ForeColor = OrbitManagementTheme.Muted;
        topbar.BackColor = identityLabel.BackColor = breadcrumbLabel.BackColor = OrbitManagementTheme.Background;
        identityLabel.Font = breadcrumbLabel.Font = OrbitManagementTheme.Small;
        identityLabel.ForeColor = breadcrumbLabel.ForeColor = OrbitManagementTheme.Muted;
        foreach (var button in new[] { homeButton, catalogsButton, systemButton, logoutButton }) ConfigureNavigation(button);
        statusLabel.BackColor = OrbitManagementTheme.Sidebar; statusLabel.Font = OrbitManagementTheme.Header;
        statusLabel.Padding = Padding.Empty; statusLabel.AutoSize = false;
        sidebar.InitializeNavigation(brandLabel, brandCaption, homeButton, logoutButton, statusLabel);
        sidebar.SetGroups((L.T("Quản trị"), [catalogsButton, systemButton]));
        ClientSizeChanged += (_, _) => ArrangeNavigation();
        ArrangeNavigation();
        Shown += (_, _) => { UpdateMinimumSize(); ArrangeNavigation(); };
        DpiChanged += (_, _) => { UpdateMinimumSize(); ArrangeNavigation(); };
    }

    private static void ConfigureNavigation(Button button)
    {
        if (button is not UI.OrbitButton orbit) return;
        var icon = button.Tag switch { NavigationTarget.Products => "book", NavigationTarget.Loyalty => "ticket", NavigationTarget.SystemInfo => "settings", _ => "" };
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
            var navigationSidebar = sidebar;
            navigationSidebar.Compact = compact;
            layout.ColumnStyles[0].Width = (compact ? 62 : 200) * DeviceDpi / 96F;
            layout.RowStyles[0].Height = 50 * DeviceDpi / 96F;
            navigationSidebar.PerformLayout();
        }
        finally { arrangingNavigation = false; }
    }
    public MainForm(SessionContext session, PermissionService permissions,
        Func<CancellationToken, Task<AppSetupStatus>> readState, Func<UserControl>? catalogs = null, IReadOnlyDictionary<NavigationTarget, Func<UserControl>>? modules = null, NavigationTarget? initialTarget = null) : this()
    {
        this.session = session; this.readState = readState; this.catalogs = catalogs;
        this.modules = modules ?? new Dictionary<NavigationTarget, Func<UserControl>>();
        this.initialTarget = initialTarget;
        // Menu theo công việc; CASH chọn bàn/món/ưu đãi trong POS, không mở màn quản trị.
        NavigationTarget[] menuTargets = session.Identity.Role switch
        {
            "ADMIN" => [NavigationTarget.Sales, NavigationTarget.Invoices, NavigationTarget.Delivery, NavigationTarget.Customers, NavigationTarget.Tables, NavigationTarget.Products, NavigationTarget.Materials, NavigationTarget.Loyalty, NavigationTarget.Finance, NavigationTarget.Inventory, NavigationTarget.Debt, NavigationTarget.HumanResources, NavigationTarget.Assets, NavigationTarget.Reports],
            "CASH" => [NavigationTarget.Sales, NavigationTarget.Invoices, NavigationTarget.Delivery, NavigationTarget.Customers, NavigationTarget.Materials],
            "STORE" => [NavigationTarget.Inventory, NavigationTarget.Materials, NavigationTarget.Products, NavigationTarget.Debt],
            _ => []
        };
        var moduleButtons = new List<Button>();
        foreach (var target in menuTargets.Where(this.modules.ContainsKey))
        {
            var button = new UI.OrbitButton { Tag = target, Text = target switch { NavigationTarget.Reports => L.T("Báo cáo"), NavigationTarget.Assets => L.T("Tài sản"), NavigationTarget.HumanResources => L.T("Nhân sự"), NavigationTarget.Delivery => L.T("Giao hàng"), NavigationTarget.Invoices => L.T("Lịch sử hóa đơn"), NavigationTarget.Sales => L.T("Bán hàng"), NavigationTarget.Materials => session.Identity.Role == "CASH" ? L.T("Tồn phục vụ bán") : L.T("Nguyên liệu / Tồn"), NavigationTarget.Products => L.T("Menu / Công thức"), NavigationTarget.Tables => L.T("Bàn / Khu vực"), NavigationTarget.Customers => L.T("Khách hàng"), NavigationTarget.Debt => L.T("Công nợ"), NavigationTarget.Inventory => L.T("Kho"), NavigationTarget.Finance => L.T("Thu chi"), NavigationTarget.Loyalty => L.T("Ưu đãi / Điểm"), _ => target.ToString() }, AutoSize = false, Size = new Size(168, 44), Margin = new Padding(0, 0, 0, 10) };
            button.AlignTextLeft = true; button.Quiet = true; AppTheme.Apply(button); UI.OrbitManagementLayout.Icon(button, target switch { NavigationTarget.Delivery => "receipt", NavigationTarget.Invoices => "receipt", NavigationTarget.Sales => "receipt", NavigationTarget.Materials => "package", NavigationTarget.Products => "receipt", NavigationTarget.Tables => "armchair", NavigationTarget.Customers => "users", NavigationTarget.Finance => "wallet", NavigationTarget.Loyalty => "wallet", _ => "settings" }); moduleButtons.Add(button);
            ConfigureNavigation(button);
            button.Click += async (_, _) => await Navigate(target);
        }
        ArrangeNavigation();
        session.Revoked += SessionRevoked;
        navigation = new NavigationController(content, session, permissions);
        identityLabel.Text = $"{session.Identity.EmployeeName} · {session.Identity.Role}";
        sidebar.Person.Text = session.Identity.EmployeeName;
        var roleLabel = session.Identity.Role switch { "ADMIN" => L.T("Quản trị viên"), "CASH" => L.T("Thu ngân"), "STORE" => L.T("Quản lý kho"), _ => L.T("Nhân viên") };
        sidebar.Role.Text = $"{roleLabel} · {session.Identity.Role}";
        brandCaption.Text = session.Identity.Role switch { "CASH" => L.T("KHÔNG GIAN THU NGÂN"), "STORE" => L.T("KHÔNG GIAN KHO"), _ => L.T("KHÔNG GIAN QUẢN LÝ") };
        catalogsButton.Visible = session.Identity.Role == "ADMIN" && catalogs is not null;
        // CASH/STORE vào thẳng màn nghiệp vụ; không có trang phiên trùng màn đích.
        sidebar.HomeVisible = session.Identity.Role == "ADMIN";
        Button[] Items(params NavigationTarget[] targets) => targets.Select(target => moduleButtons.FirstOrDefault(button => button.Tag is NavigationTarget tagged && tagged == target)).OfType<Button>().ToArray();
        sidebar.SetGroups(
            (L.T("Bán hàng"), Items(NavigationTarget.Sales, NavigationTarget.Invoices, NavigationTarget.Delivery, NavigationTarget.Tables)),
            (L.T("Khách hàng & ưu đãi"), Items(NavigationTarget.Customers, NavigationTarget.Loyalty)),
            (L.T("Menu & kho"), Items(NavigationTarget.Products, NavigationTarget.Materials, NavigationTarget.Inventory)),
            (L.T("Tài chính & báo cáo"), Items(NavigationTarget.Finance, NavigationTarget.Debt, NavigationTarget.Assets, NavigationTarget.Reports)),
            (L.T("Quản trị"), Items(NavigationTarget.HumanResources).Concat(session.Identity.Role == "ADMIN" && catalogs is not null ? [catalogsButton, systemButton] : new[] { systemButton }).ToArray()));
        Shown += async (_, _) => await InitialNavigationAsync();
    }

    // Điều hướng màn đầu tiên được dùng chung cho lần Show và cho chủ phiên (ApplicationSessionContext)
    // để app chỉ hiện khi màn đầu đã dựng xong.
    private NavigationTarget? initialTarget;
    private Task? initialNavigation;
    private Task InitialNavigationAsync() => initialNavigation ??= Navigate(initialTarget ?? (session!.Identity.Role switch
    {
        "CASH" when this.modules.ContainsKey(NavigationTarget.Sales) => NavigationTarget.Sales,
        "STORE" when this.modules.ContainsKey(NavigationTarget.Inventory) => NavigationTarget.Inventory,
        _ => NavigationTarget.Home
    }));
    internal Task StartAsync() => InitialNavigationAsync();
    internal async Task OpenSalesOrderAsync(string code)
    {
        await Navigate(NavigationTarget.Sales);
        if (content.Controls.OfType<Modules.Sales.SalesControl>().FirstOrDefault() is { } sales && sales.Visible) await sales.OpenOrderAsync(code);
    }
    internal async Task OpenSalesTableAsync(string code)
    {
        await Navigate(NavigationTarget.Sales);
        if (content.Controls.OfType<Modules.Sales.SalesControl>().FirstOrDefault() is { } sales && sales.Visible) await sales.OpenTableAfterLoadAsync(code);
    }
    internal void SetWorkspaceTitle(string caption)
    {
        var workspace = session?.Identity.Role switch { "CASH" => L.T("Thu ngân"), "STORE" => L.T("Kho"), _ => L.T("Quản lý") };
        breadcrumbLabel.Text = workspaceTarget == NavigationTarget.Materials && session?.Identity.Role != "CASH"
            ? L.T("Kho nguyên liệu  /  Tồn hiện tại") : workspace + " / " + caption;
    }
    private async Task Navigate(NavigationTarget target)
    {
        if (navigation is null || session is null || navigating || loggingOut || Dialogs.IsOpen) return;
        navigating = true;
        homeButton.Enabled = catalogsButton.Enabled = systemButton.Enabled = false;
        // DIAG: đo thời gian từ lúc bấm màn đến khi màn được đưa lên.
        var timing = Diagnostics.UiTiming.Start();
        try
        {
            var changed = await navigation.NavigateAsync(target, () => modules.TryGetValue(target, out var factory) ? factory() : target switch
            {
                NavigationTarget.Home => new SessionHomeControl(session.Identity),
                NavigationTarget.Catalogs => catalogs!(),
                _ => CreateSystemInfo()
            }, this);
            Diagnostics.UiTiming.Log("nav", "navigate-total", timing, target.ToString());
            if (!changed) return;
            if (!IsDisposed && !loggingOut)
            {
                workspaceTarget = target;
                foreach (var button in sidebar.NavigationButtons)
                { button.BackColor = AppTheme.Espresso; button.ForeColor = AppTheme.Muted; if (button is UI.OrbitButton item) { item.NavigationSelected = false; item.Invalidate(); } }
                var selected = sidebar.NavigationButtons.FirstOrDefault(b => b.Tag is NavigationTarget tagged && tagged == target) ?? (target switch { NavigationTarget.Home => homeButton, NavigationTarget.Catalogs => catalogsButton, _ => systemButton });
                selected.BackColor = AppTheme.Selected; selected.ForeColor = AppTheme.Primary; if (selected is UI.OrbitButton selectedItem) { selectedItem.NavigationSelected = true; selectedItem.Invalidate(); } SetWorkspaceTitle(selected.Text);
                sidebar.Reveal(selected);
            }
            if (target == NavigationTarget.SystemInfo && content.Controls.Count == 1 && content.Controls[0] is SystemInfoControl info)
            {
                var state = await readState!(session.Token); session.EnsureCurrent();
                if (!IsDisposed) info.SetSchema(state.SchemaVersion);
            }
            if (!IsDisposed && !loggingOut) statusLabel.Text = L.T("Phiên đang hoạt động.");
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
    public Task OpenWorkspaceAsync(string target) => Enum.TryParse<NavigationTarget>(target,out var selected) ? Navigate(selected) : Task.CompletedTask;
    private async void Home_Click(object? sender, EventArgs e) => await Navigate(NavigationTarget.Home);
    private async void Catalogs_Click(object? sender, EventArgs e) => await Navigate(NavigationTarget.Catalogs);
    private async void System_Click(object? sender, EventArgs e) => await Navigate(NavigationTarget.SystemInfo);
    private async void Logout_Click(object? sender, EventArgs e)
    { await RequestLogoutAsync(); }
    internal async Task RequestLogoutAsync()
    { if (!navigating && !Dialogs.IsOpen && (navigation is null || await navigation.ConfirmLeaveAsync(this))) Logout(); }
    private SystemInfoControl CreateSystemInfo()
    {
        var info = new SystemInfoControl();
        info.SetLanguageHandler(ApplyLanguageAsync);
        return info;
    }

    private async Task ApplyLanguageAsync(AppLanguage language)
    {
        if (changingLanguage || navigating || loggingOut || Dialogs.IsOpen || navigation is null || session is null) return;
        changingLanguage = true;
        try
        {
            // Cùng guard nháp/busy đang dùng khi chuyển màn; hủy thì không đổi gì.
            if (!await navigation.ConfirmLeaveAsync(this)) return;
            if (!new Configuration.LanguagePreference().TrySave(language))
            {
                await OrbitConfirmationDialog.ShowAsync(this, L.T("Không lưu được lựa chọn ngôn ngữ. Kiểm tra quyền thư mục cấu hình local."), L.T("Ngôn ngữ"));
                return;
            }
            if (LanguageService.Current == language) return;
            LanguageService.Set(language);
            ShellReloadRequested?.Invoke();
        }
        finally { changingLanguage = false; }
    }

    internal void CloseForShellSwap()
    {
        keepSessionOnClose = true;
        closeApproved = true;
        Close();
    }
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
        if (!e.Cancel) { if (session is not null) session.Revoked -= SessionRevoked; if (!keepSessionOnClose) session?.Revoke(); navigation?.Dispose(); }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { Dialogs?.Dispose(); if (session is not null) session.Revoked -= SessionRevoked; navigation?.Dispose(); }
        base.Dispose(disposing);
    }
}
