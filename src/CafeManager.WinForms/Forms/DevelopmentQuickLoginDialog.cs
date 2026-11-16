using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.Models.Requests;
using CafeManager.WinForms.Configuration;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Forms;

internal sealed class DevelopmentQuickLoginDialog : OrbitDialogForm
{
    private readonly ComboBox role = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top };
    private readonly TextBox username = new() { MaxLength = 50, Dock = DockStyle.Top, AccessibleName = L.T("Tài khoản DEV") };
    private readonly TextBox password = new() { MaxLength = 128, UseSystemPasswordChar = true, Dock = DockStyle.Top, AccessibleName = L.T("Mật khẩu DEV") };
    private readonly Button save = new OrbitButton { Text = L.T("Lưu"), IsPrimary = true, AutoSize = true };
    private readonly Button delete = new OrbitButton { Text = L.T("Xóa cấu hình role"), AutoSize = true };
    private readonly Label status = new() { AutoSize = true, Dock = DockStyle.Top };
    private readonly AuthService auth;
    private readonly DevelopmentQuickLogin configuration;
    private CancellationTokenSource? pending;
    internal DevelopmentQuickLoginDialog(AuthService auth, DevelopmentQuickLogin configuration, string selectedRole)
    {
        this.auth = auth; this.configuration = configuration;
        Text = L.T("Cấu hình đăng nhập nhanh — DEV"); ShowInTaskbar = false;
        MinimizeBox = MaximizeBox = false; StartPosition = FormStartPosition.CenterParent;
        AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(480, 430); AcceptButton = null;
        role.Items.AddRange(DevelopmentQuickLogin.Roles); role.SelectedItem = selectedRole;
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(24), ColumnCount = 1, RowCount = 8 };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (Control control in new Control[] { new Label { Text = L.T("Vai trò"), AutoSize = true }, role,
            new Label { Text = L.T("Tên đăng nhập"), AutoSize = true }, username,
            new Label { Text = L.T("Mật khẩu (nhập lại khi thay cấu hình)"), AutoSize = true }, password })
        { body.RowStyles.Add(new RowStyle(SizeType.AutoSize)); body.Controls.Add(control); control.Margin = new Padding(3, 6, 3, 10); }
        var close = new OrbitButton { Text = L.T("Đóng"), AutoSize = true };
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = true };
        actions.Controls.AddRange([save, delete, close]); body.Controls.Add(actions); body.Controls.Add(status);
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize)); body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(body); Theme.AppTheme.Apply(this); Theme.AppTheme.PrimaryButton(save);
        save.Click += Save_Click; delete.Click += (_, _) => Delete(); close.Click += (_, _) => Close();
        role.SelectedIndexChanged += (_, _) => { username.Clear(); password.Clear(); status.Text = ""; };
        Shown += (_, _) => ClampToWorkingArea(); DpiChanged += (_, _) => ClampToWorkingArea();
    }
    private void ClampToWorkingArea()
    {
        var area = Screen.FromControl(Owner ?? this).WorkingArea;
        Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height));
        Location = new Point(Math.Clamp(Left, area.Left, area.Right - Width), Math.Clamp(Top, area.Top, area.Bottom - Height));
    }
    private async void Save_Click(object? sender, EventArgs e)
    {
        if (pending is not null) return;
        var selected = (string)role.SelectedItem!;
        var input = new LoginRequest(username.Text, password.Text);
        var request = new CancellationTokenSource(); pending = request; SetBusy(true);
        status.Text = L.T("Đang xác thực tài khoản…");
        try
        {
            using var session = await auth.LoginAsync(input, request.Token);
            if (IsDisposed || request.IsCancellationRequested) return;
            if (session.Identity.Role != selected) throw new InvalidOperationException(L.T("Tài khoản không thuộc vai trò đã chọn."));
            configuration.Save(selected, input);
            password.Clear(); status.Text = L.T("Đã lưu. Đóng hộp này rồi bấm nút role để đăng nhập.");
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException or InvalidOperationException or DataAccessException)
        { if (!IsDisposed && !request.IsCancellationRequested) status.Text = ex is DataAccessException d ? d.Error.Message : ex.Message; }
        finally { pending = null; request.Dispose(); if (!IsDisposed) SetBusy(false); }
    }
    private void Delete()
    {
        if (pending is not null) return;
        try { configuration.Delete((string)role.SelectedItem!); password.Clear(); status.Text = L.T("Đã xóa cấu hình role đã chọn."); }
        catch (InvalidOperationException ex) { status.Text = ex.Message; }
    }
    private void SetBusy(bool busy) => role.Enabled = username.Enabled = password.Enabled = save.Enabled = delete.Enabled = !busy;
    protected override void OnFormClosing(FormClosingEventArgs e)
    { base.OnFormClosing(e); if (!e.Cancel) { pending?.Cancel(); password.Clear(); } }
}
