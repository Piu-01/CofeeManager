using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.HumanResources;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
namespace CafeManager.WinForms.Modules.HumanResources;

// Editor HR giữ request trước khi gửi; sau khởi động lại chỉ đối chiếu, không gửi lại mật khẩu.
internal sealed class HrEditorDialog : OrbitDialogForm
{
    private readonly TableLayoutPanel fields = new() { Dock = DockStyle.Fill, AutoScroll = true, ColumnCount = 1 };
    private readonly Label status = new() { AutoSize = true };
    private readonly OrbitButton save = new() { Text = L.T("Lưu"), IsPrimary = true }, check = new() { Text = L.T("Kiểm tra kết quả") }, close = new() { Text = L.T("Đóng") };
    private readonly ErrorProvider errors;
    private readonly SessionContext session;
    private readonly IPendingOperationStore store;
    private readonly string operation;
    private readonly Func<Guid, Task<HumanResourcesReceipt?>> reconcile;
    private Guid requestId = Guid.NewGuid();
    private bool writing, unknown, saved, dirty, canWrite = true;
    private PendingOperation? pending;
    [global::System.ComponentModel.DesignerSerializationVisibility(global::System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal Func<(string SafePayload, Func<Guid, Task<string>> Write)>? CaptureCommand { get; set; }
    [global::System.ComponentModel.DesignerSerializationVisibility(global::System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal string Confirmation { get; set; } = L.T("Lưu thay đổi này?");
    public string? ResultCode { get; private set; }
    public bool Changed => saved;
    internal void SetWriteAvailable(bool value) { canWrite = value; UpdateState(); }
    public HrEditorDialog(string title, string operation, SessionContext session, IPendingOperationStore store, Func<Guid, Task<HumanResourcesReceipt?>> reconcile)
    {
        Text = title; this.operation = operation; this.session = session; this.store = store; this.reconcile = reconcile;
        AutoScaleDimensions = new(96, 96); AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new(680, 500); ShowInTaskbar = false;
        errors = new() { ContainerControl = this, BlinkStyle = ErrorBlinkStyle.NeverBlink };
        var actions = new FlowLayoutPanel(); actions.Controls.AddRange([save, check, close]);
        var layout = new OrbitDialogLayout(this, fields, status, actions); Controls.Add(layout); layout.EnableResponsiveActions(this);
        save.Click += async (_, _) => await Save(); check.Click += async (_, _) => await Check(); close.Click += (_, _) => Close();
        Shown += (_, _) =>
        {
            try { pending = store.Read(session.Identity.AccountId, operation); if (pending is not null) { requestId = pending.RequestId; unknown = true; status.Text = L.F("Yêu cầu đang chờ {0}. Kiểm tra trước khi lập yêu cầu mới.", requestId); } }
            catch (Exception e) when (e is IOException or global::System.Security.Cryptography.CryptographicException or JsonException or UnauthorizedAccessException) { unknown = true; status.Text = L.T("Không đọc được yêu cầu chờ: ") + e.Message; }
            UpdateState(); layout.FitToContent(this, fields.Controls.OfType<OrbitDialogField>().Any(f => f.Expands));
        };
        OrbitField.ApplyInputBehavior(this);
    }
    public TextBox TextField(string label, string value = "", bool secret = false)
    {
        var box = new TextBox { Text = value, Dock = DockStyle.Top, UseSystemPasswordChar = secret };
        AddField(label, box); box.TextChanged += (_, _) => dirty = true; return box;
    }
    public void AddField(string label, Control input)
    {
        var expands = input is DataGridView;
        fields.RowStyles.Add(new RowStyle(expands ? SizeType.Percent : SizeType.AutoSize, expands ? 100 : 0));
        fields.Controls.Add(new OrbitDialogField(label, input, expands), 0, fields.RowCount++);
        if (input is ComboBox c) c.SelectedValueChanged += (_, _) => dirty = true;
        if (input is DateTimePicker d) d.ValueChanged += (_, _) => dirty = true;
        if (input is DataGridView g) { g.CellValueChanged += (_, _) => dirty = true; g.UserDeletedRow += (_, _) => dirty = true; }
    }
    public void Note(string text) => AddField(L.T("Thông tin"), new TextBox { ReadOnly = true, Multiline = true, Text = text, Height = 76, Dock = DockStyle.Top, ScrollBars = ScrollBars.Vertical });
    private void UpdateState()
    {
        fields.Enabled = !writing && !unknown && !saved; save.Enabled = !writing && !unknown && !saved && canWrite;
        check.Enabled = !writing && unknown && pending is not null; close.Enabled = !writing && (!unknown || pending is null || saved);
    }
    private async Task Save()
    {
        if (writing || unknown || saved || !canWrite) return;
        errors.Clear(); bool sent = false;
        try
        {
            var command = CaptureCommand?.Invoke() ?? throw new ArgumentException(L.T("Chưa có nội dung lưu."));
            if (await OrbitConfirmationDialog.ShowAsync(this, Confirmation, Text, MessageBoxButtons.OKCancel) != DialogResult.OK) return;
            writing = true; UpdateState();
            pending = new(session.Identity.AccountId, requestId, operation, command.SafePayload, DateTime.UtcNow);
            store.Save(pending); sent = true;
            var code = await command.Write(requestId);
            if (IsDisposed) return;
            Complete(code);
        }
        catch (ArgumentException e) { status.Text = e.Message; errors.SetError(fields, e.Message); }
        catch (UserOperationException e)
        {
            if (IsDisposed) return;
            unknown = sent && e.Error.RequiresReconciliation; status.Text = e.Message;
            if (sent && !unknown) TryClear();
        }
        catch (OperationCanceledException) { if (!IsDisposed) { unknown = sent; status.Text = L.T("Chưa xác định kết quả; kiểm tra yêu cầu trước khi gửi lại."); } }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or global::System.Security.Cryptography.CryptographicException)
        { if (!IsDisposed) { unknown = sent; status.Text = e.Message; } }
        finally { writing = false; if (!IsDisposed) UpdateState(); }
    }
    private void Complete(string code)
    {
        ResultCode = code; saved = true; unknown = false; dirty = false;
        status.Text = L.T("Đã ghi ") + code + ".";
        if (TryClear()) { DialogResult = DialogResult.OK; Close(); }
        else { status.Text += L.T(" Đã ghi ở DB nhưng chưa dọn được file chờ. Đóng và đối chiếu lại; không gửi lần hai."); }
    }
    private bool TryClear()
    {
        try { store.Clear(session.Identity.AccountId, operation); pending = null; return true; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { status.Text = L.T("Chưa dọn được yêu cầu chờ: ") + e.Message; unknown = true; return false; }
    }
    private async Task Check()
    {
        if (writing || pending is null) return;
        writing = true; UpdateState();
        try
        {
            var receipt = await reconcile(requestId);
            if (IsDisposed) return;
            if (receipt is not null) Complete(receipt.Code);
            else if (await OrbitConfirmationDialog.ShowAsync(this, L.T("SQL đã xác minh yêu cầu chưa ghi. Bỏ yêu cầu chờ để lập lại với nội dung mới?"), L.T("Đối chiếu"), MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                if (TryClear()) { unknown = false; requestId = Guid.NewGuid(); status.Text = L.T("Yêu cầu chưa ghi đã được bỏ. Nhập lại và xác nhận khi sẵn sàng."); }
            }
        }
        catch (Exception e) when (e is UserOperationException or OperationCanceledException or UnauthorizedAccessException) { if (!IsDisposed) status.Text = e.Message; }
        finally { writing = false; if (!IsDisposed) UpdateState(); }
    }
    protected override async Task<bool> CanCloseAsync()
    {
        if (writing || unknown && pending is not null && !saved) { status.Text = L.T("Đối chiếu yêu cầu trước khi đóng; khi phiên hết hạn file chờ vẫn được giữ."); return false; }
        return saved || !dirty || await OrbitConfirmationDialog.ShowAsync(this, L.T("Bỏ nháp và đóng?"), Text, MessageBoxButtons.OKCancel) == DialogResult.OK;
    }
    protected override void Dispose(bool disposing) { if (disposing) errors.Dispose(); base.Dispose(disposing); }
}

