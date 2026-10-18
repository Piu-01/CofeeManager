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
    public event Action<SessionContext>? LoggedIn;
    public LoginForm()
    {
        InitializeComponent(); Theme.AppTheme.Apply(this); Theme.AppTheme.PrimaryButton(loginButton);
        title.BackColor = caption.BackColor = Color.Transparent;
        caption.ForeColor = Theme.AppTheme.Primary; Theme.AppTheme.Status(statusLabel);
        UI.OrbitField.Wrap(usernameInput); UI.OrbitField.Wrap(passwordInput);
        Controls.Add(new UI.OrbitContentHost(layout, 480, centerVertically: true));
    }
    public LoginForm(AuthService? auth, string? configurationError) : this()
    {
        this.auth = auth;
        loginButton.Enabled = auth is not null;
        statusLabel.Text = configurationError ?? "Nhập tài khoản để đăng nhập.";
    }
    private async void Login_Click(object? sender, EventArgs e)
    {
        if (auth is null || pending is not null) return;
        var version = ++generation;
        var request = new CancellationTokenSource(); pending = request;
        loginButton.Enabled = false; statusLabel.Text = "Đang đăng nhập…";
        try
        {
            var session = await auth.LoginAsync(new LoginRequest(usernameInput.Text, passwordInput.Text), request.Token);
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
            if (!IsDisposed && version == generation) loginButton.Enabled = true;
        }
    }
    private void ShowPassword_Changed(object? sender, EventArgs e) => passwordInput.UseSystemPasswordChar = !showPassword.Checked;
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (!e.Cancel) { ++generation; pending?.Cancel(); passwordInput.Clear(); }
    }
}
