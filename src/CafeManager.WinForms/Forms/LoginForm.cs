using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.Models.Requests;

namespace CafeManager.WinForms.Forms;

public partial class LoginForm : Form
{
    private readonly AuthService? auth;
    private CancellationTokenSource? pending;
    private int generation;
    private readonly Configuration.DevelopmentQuickLogin? quickLogin;
    private readonly List<Button> devButtons = [];
    private readonly Configuration.LanguagePreference languagePreference = new();
    private readonly List<Button> languageButtons = [];
    private string? configurationErrorText;
    private Label? quickLoginLabel;
    public event Action<SessionContext>? LoggedIn;
    public LoginForm()
    {
        InitializeComponent(); Theme.AppTheme.Apply(this); Theme.AppTheme.PrimaryButton(loginButton);
        title.BackColor = caption.BackColor = Color.Transparent;
        caption.ForeColor = Theme.AppTheme.Primary; Theme.AppTheme.Status(statusLabel);
        UI.OrbitField.Wrap(usernameInput); UI.OrbitField.Wrap(passwordInput);
        Controls.Add(new UI.OrbitContentHost(layout, 480, centerVertically: true));
        var languages = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = false };
        foreach (var language in new[] { AppLanguage.Vietnamese, AppLanguage.English })
        {
            var button = new UI.OrbitButton { Text = language == AppLanguage.Vietnamese ? "VI" : "EN", AutoSize = true, TabIndex = 4 + languageButtons.Count };
            button.Click += (_, _) => ChangeLanguage(language);
            languages.Controls.Add(button); languageButtons.Add(button);
        }
        heading.RowCount = 3; heading.RowStyles.Add(new RowStyle(SizeType.AutoSize)); heading.Controls.Add(languages, 0, 2);
        Theme.AppTheme.Apply(languages); Relocalize();
    }
    private void ChangeLanguage(AppLanguage language)
    {
        if (pending is not null) return;
        if (!languagePreference.TrySave(language)) { statusLabel.Text = L.T("Không lưu được lựa chọn ngôn ngữ. Kiểm tra quyền thư mục cấu hình local."); return; }
        LanguageService.Set(language); Relocalize();
    }
    private void Relocalize()
    {
        Text = title.Text = L.T("CoffeeManager · Đăng nhập");
        caption.Text = L.T("KHÔNG GIAN QUẢN LÝ QUÁN CÀ PHÊ");
        usernameLabel.Text = L.T("&Tên đăng nhập"); passwordLabel.Text = L.T("&Mật khẩu");
        usernameInput.AccessibleName = L.T("Tên đăng nhập"); passwordInput.AccessibleName = L.T("Mật khẩu");
        showPassword.Text = L.T("&Hiện mật khẩu"); loginButton.Text = L.T("Đăng &nhập");
        statusLabel.Text = configurationErrorText is null ? L.T("Nhập tài khoản để đăng nhập.") : L.T(configurationErrorText);
        foreach (var button in languageButtons) button.Enabled = pending is null;
        foreach (var button in devButtons) { button.Text = button.Tag is "QuickLoginConfiguration" ? L.T("Cấu hình đăng nhập nhanh") : (string)button.Tag!; button.AccessibleName = button.Text; }
        if (quickLoginLabel is not null) quickLoginLabel.Text = L.T("Đăng nhập nhanh — DEV");
    }
    public LoginForm(AuthService? auth, string? configurationError) : this(auth, configurationError, null) { }
    internal LoginForm(AuthService? auth, string? configurationError, Configuration.DevelopmentQuickLogin? quickLogin) : this()
    {
        this.auth = auth;
        this.quickLogin = quickLogin;
        configurationErrorText = configurationError;
        loginButton.Enabled = auth is not null;
        statusLabel.Text = configurationError is null ? L.T("Nhập tài khoản để đăng nhập.") : L.T(configurationError);
        if (auth is not null && quickLogin is not null) AddQuickLogin();
    }
    private void AddQuickLogin()
    {
        var tools = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Margin = new Padding(3, 12, 3, 3) };
        foreach (var role in Configuration.DevelopmentQuickLogin.Roles)
        {
            var button = new UI.OrbitButton { Text = role, Tag = role, AutoSize = true, AccessibleName = L.T("Đăng nhập nhanh ") + role };
            button.Click += async (_, _) => await QuickLoginAsync(role);
            tools.Controls.Add(button); devButtons.Add(button);
        }
        var configure = new UI.OrbitButton { Text = L.T("Cấu hình đăng nhập nhanh"), Tag = "QuickLoginConfiguration", AutoSize = true };
        configure.Click += (_, _) => ConfigureQuickLogin("ADMIN"); tools.Controls.Add(configure); devButtons.Add(configure);
        var label = new Label { Text = L.T("Đăng nhập nhanh — DEV"), AutoSize = true, Dock = DockStyle.Top };
        quickLoginLabel = label;
        layout.RowCount += 2;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(label, 0, 8); layout.Controls.Add(tools, 0, 9);
        Theme.AppTheme.Apply(tools); label.ForeColor = Theme.AppTheme.Accent;
    }
    private void ConfigureQuickLogin(string role)
    {
        if (pending is not null || quickLogin is null || auth is null) return;
        using var dialog = new DevelopmentQuickLoginDialog(auth, quickLogin, role);
        UI.OrbitDialogHost.ShowLoginDialog(dialog, this);
    }
    private async Task QuickLoginAsync(string role)
    {
        if (pending is not null || quickLogin is null || auth is null) return;
        try
        {
            var input = quickLogin.Get(role);
            if (input is null) { ConfigureQuickLogin(role); return; }
            await RunLoginAsync(async token =>
            {
                var session = await auth.LoginAsync(input, token);
                if (session.Identity.Role == role) return session;
                session.Dispose();
                throw new InvalidOperationException(L.T("Tài khoản đã lưu không thuộc role đã chọn. Hãy cập nhật cấu hình DEV."));
            });
        }
        catch (InvalidOperationException ex) { if (!IsDisposed) statusLabel.Text = ex.Message; }
    }
    private async void Login_Click(object? sender, EventArgs e)
    {
        if (auth is null) return;
        var input = new LoginRequest(usernameInput.Text, passwordInput.Text);
        await RunLoginAsync(token => auth.LoginAsync(input, token));
    }
    private async Task RunLoginAsync(Func<CancellationToken, Task<SessionContext>> createSession)
    {
        if (pending is not null) return;
        var version = ++generation;
        var request = new CancellationTokenSource(); pending = request;
        loginButton.Enabled = false; statusLabel.Text = L.T("Đang đăng nhập…");
        foreach (var button in languageButtons) button.Enabled = false;
        foreach (var button in devButtons) button.Enabled = false;
        try
        {
            var session = await createSession(request.Token);
            if (IsDisposed || version != generation || request.IsCancellationRequested) { session.Dispose(); return; }
            passwordInput.Clear(); LoggedIn?.Invoke(session);
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException or InvalidOperationException or DataAccessException)
        { if (!IsDisposed && version == generation) statusLabel.Text = ex is DataAccessException d ? d.Error.Message : ex.Message; }
        finally
        {
            if (ReferenceEquals(pending, request)) pending = null;
            request.Dispose();
            if (!IsDisposed && version == generation)
            { loginButton.Enabled = auth is not null; foreach (var button in devButtons) button.Enabled = auth is not null; foreach (var button in languageButtons) button.Enabled = true; }
        }
    }
    private void ShowPassword_Changed(object? sender, EventArgs e) => passwordInput.UseSystemPasswordChar = !showPassword.Checked;
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (!e.Cancel) { ++generation; pending?.Cancel(); passwordInput.Clear(); }
    }
}
