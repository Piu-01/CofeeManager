using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.Models.Dtos;
using CafeManager.Models.Requests;

namespace CafeManager.WinForms.Forms;

public partial class BootstrapForm : Form
{
    private readonly BootstrapService? service;
    private List<SetupCandidate> candidates = [];
    private readonly CancellationTokenSource lifetime = new();
    private bool busy;
    private string state = "Pending";
    public BootstrapForm()
    {
        InitializeComponent(); Theme.AppTheme.Apply(this); Theme.AppTheme.PrimaryButton(completeButton);
        titleLabel.BackColor = subtitleLabel.BackColor = Color.Transparent;
        subtitleLabel.ForeColor = Theme.AppTheme.Muted; Theme.AppTheme.Status(statusLabel);
        modeInput.SelectedIndexChanged += (_, _) => UpdateMode();
        candidateInput.SelectedIndexChanged += (_, _) => UpdateMode();
        foreach (var input in new[] { nameInput, phoneInput, usernameInput, passwordInput, confirmationInput }) UI.OrbitField.Wrap(input);
        UI.OrbitFieldLayout.Attach(layout);
        Controls.Add(new UI.OrbitContentHost(shell, 720));
        UpdateMode();
    }
    private void UpdateMode()
    {
        var existing = modeInput.SelectedIndex == 1;
        candidateInput.Enabled = existing && state == "Pending";
        nameInput.Enabled = phoneInput.Enabled = !existing && state == "Pending";
        modeInput.Enabled = state == "Pending";
        confirmationInput.Enabled = state == "Pending";
        var candidate = existing ? candidateInput.SelectedItem as SetupCandidate : null;
        usernameInput.ReadOnly = state == "Pending" && candidate?.AccountId is not null;
        if (usernameInput.ReadOnly) usernameInput.Text = candidate!.Username;
    }
    public BootstrapForm(BootstrapService service) : this() { this.service = service; Shown += LoadSetup; }
    private async void LoadSetup(object? sender, EventArgs e)
    {
        busy = true; completeButton.Enabled = false;
        try
        {
            var status = await service!.ReadStateAsync(lifetime.Token);
            state = status.State;
            foreach (var input in new[] { nameInput, phoneInput, usernameInput, passwordInput, confirmationInput }) UI.OrbitField.Wrap(input);
        UI.OrbitFieldLayout.Attach(layout);
        Controls.Add(new UI.OrbitContentHost(shell, 720));
        UpdateMode();
            if (state == "Completed") { statusLabel.Text = "Thiết lập đã hoàn tất. Đóng setup và chạy app bằng tài khoản runtime."; return; }
            candidates = await service.CandidatesAsync(lifetime.Token);
            if (IsDisposed) return;
            candidateInput.DataSource = candidates.Where(c => c.Eligible).ToList();
            candidateInput.DisplayMember = nameof(SetupCandidate.EmployeeName);
            statusLabel.Text = state == "LegacyReview" ? "Xác nhận admin hiện tại bằng username/password; không reset mật khẩu." : "Chọn tạo admin mới hoặc kích hoạt đúng một ứng viên.";
            completeButton.Enabled = true;
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (DataAccessException ex) { if (!IsDisposed) statusLabel.Text = ex.Error.Message; }
        finally { busy = false; }
    }
    private async void Complete_Click(object? sender, EventArgs e)
    {
        if (busy || service is null) return;
        busy = true; completeButton.Enabled = false;
        try
        {
            if (state == "LegacyReview") await service.ConfirmLegacyAsync(new LoginRequest(usernameInput.Text, passwordInput.Text), lifetime.Token);
            else
            {
                var candidate = modeInput.SelectedIndex == 1 ? candidateInput.SelectedItem as SetupCandidate : null;
                if (modeInput.SelectedIndex == 1 && candidate is null) throw new ArgumentException("Cần chọn nhân viên hợp lệ.");
                await service.CompleteAsync(new BootstrapRequest
                {
                    Mode = candidate is null ? "NewEmployee" : candidate.AccountId is null ? "ExistingEmployee" : "ActivateDemo",
                    Username = candidate?.Username ?? usernameInput.Text.Trim(' '), Password = passwordInput.Text, Confirmation = confirmationInput.Text,
                    EmployeeCode = candidate?.EmployeeCode, ExpectedStamp = candidate?.SecurityStamp,
                    EmployeeName = candidate is null ? nameInput.Text.Trim() : null,
                    Phone = candidate is null && !string.IsNullOrWhiteSpace(phoneInput.Text) ? phoneInput.Text.Trim(' ') : null
                }, lifetime.Token);
            }
            passwordInput.Clear(); confirmationInput.Clear();
            MessageBox.Show(this, "Thiết lập đã hoàn tất. Đóng process setup; chạy ứng dụng bằng tài khoản runtime. Người cài thu hồi quyền setup.", "CoffeeManager");
            busy = false; Close();
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException or DataAccessException)
        {
            if (!IsDisposed) statusLabel.Text = ex is DataAccessException d ? d.Error.Message : ex.Message;
            // Do not blindly retry: a timeout can follow a successful commit.
            if (ex is DataAccessException { Error.RequiresReconciliation: true })
            {
                try { if ((await service.ReadStateAsync(lifetime.Token)).State == "Completed") state = "Completed"; }
                catch (DataAccessException) { state = "Unknown"; }
            }
        }
        finally { busy = false; if (!IsDisposed) completeButton.Enabled = state is "Pending" or "LegacyReview"; }
    }
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (busy) { e.Cancel = true; statusLabel.Text = "Đang chờ kết quả thiết lập; cần đối chiếu trước khi đóng."; }
        base.OnFormClosing(e);
        if (!e.Cancel) { lifetime.Cancel(); passwordInput.Clear(); confirmationInput.Clear(); }
    }
    protected override void Dispose(bool disposing) { if (disposing) lifetime.Dispose(); base.Dispose(disposing); }
}
