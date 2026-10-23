using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.Forms;

// Mẫu nhỏ cho editor G03: giữ payload và mã yêu cầu khi kết quả ghi chưa biết.
public partial class OperationEditorDialog : Form
{
    private Func<Guid, Task<OperationResult>>? pendingWrite;
    private readonly Func<Guid, Task<OperationReceipt?>>? reconcile;
    private readonly Guid requestId = Guid.NewGuid();
    private bool writing, unknown, dirty, saved;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Func<Func<Guid, Task<OperationResult>>>? CaptureWrite { get; set; }
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string? Confirmation { get; set; }
    public string? ResultCode { get; private set; }
    public int? AffectedCount { get; private set; }
    public OperationEditorDialog() { InitializeComponent(); }
    public OperationEditorDialog(string title, Func<Guid, Task<OperationReceipt?>> reconcile) : this()
    { Text = title; this.reconcile = reconcile; }
    public void SetReadOnly() { save.Visible = check.Visible = false; cancel.Text = "Đóng"; }
    public void MarkDirty() => dirty = true;
    public TextBox AddText(string label, string value = "", bool readOnly = false)
    {
        var input = new TextBox { Text = value, Dock = DockStyle.Top, ReadOnly = readOnly };
        AddField(label, input); if (!readOnly) input.TextChanged += (_, _) => dirty = true; return input;
    }
    public void AddField(string label, Control input)
    {
        var expands = input is DataGridView or UI.OrbitRecordListControl;
        var row = new UI.OrbitDialogField(label, input, expands);
        fields.RowStyles.Add(new RowStyle(expands ? SizeType.Percent : SizeType.AutoSize, expands ? 100 : 0));
        fields.Controls.Add(row, 0, fields.RowCount++);
        if (input is ComboBox combo) combo.SelectedValueChanged += (_, _) => dirty = true;
        if (input is DateTimePicker date) date.ValueChanged += (_, _) => dirty = true;
        if (input is UI.OrbitDateField dateField) dateField.ValueChanged += (_, _) => dirty = true;
        if (input is DataGridView grid) { grid.CellValueChanged += (_, _) => dirty = true; grid.UserDeletedRow += (_, _) => dirty = true; }
    }
    public void AddNote(string text)
    {
        var note = new Label { Text = text, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(16, 12, 16, 4), ForeColor = OrbitManagementTheme.Muted };
        note.SizeChanged += (_, _) => note.MaximumSize = new Size(Math.Max(1, fields.ClientSize.Width - note.Margin.Horizontal), 0);
        fields.RowStyles.Add(new RowStyle(SizeType.AutoSize)); fields.Controls.Add(note, 0, fields.RowCount++);
    }
    private async void Save_Click(object? sender, EventArgs e)
    {
        if (writing || unknown) return;
        errors.Clear();
        try
        {
            pendingWrite ??= CaptureWrite?.Invoke() ?? throw new InvalidOperationException("Chưa cấu hình thao tác.");
            if (Confirmation is not null && OrbitConfirmationDialog.Show(this, Confirmation, Text, MessageBoxButtons.OKCancel) != DialogResult.OK) { pendingWrite = null; return; }
            await WriteAsync();
        }
        catch (ArgumentException ex) { pendingWrite = null; status.Text = ex.Message; var target = Descendants(fields).FirstOrDefault(c => c.TabStop && c is not Button && (ex.ParamName is null || c.AccessibleName?.Contains(ex.ParamName, StringComparison.OrdinalIgnoreCase) == true)) ?? fields;
            errors.SetError(target, ex.Message); target.Focus(); }
    }
    private async Task WriteAsync()
    {
        SetBusy(true);
        try { var result = await pendingWrite!(requestId); ResultCode = result.Code; AffectedCount = result.AffectedCount; saved = true; DialogResult = DialogResult.OK; Close(); }
        catch (UserOperationException ex)
        {
            unknown = ex.Error.RequiresReconciliation; status.Text = ex.Message + (unknown ? $" Yêu cầu {requestId}." : "");
            if (!unknown) pendingWrite = null;
        }
        catch (OperationCanceledException) { unknown = true; status.Text = $"Chưa xác định kết quả. Yêu cầu {requestId}."; }
        catch (UnauthorizedAccessException ex) { status.Text = ex.Message; }
        finally { SetBusy(false); }
    }
    private async void Reconcile_Click(object? sender, EventArgs e)
    {
        if (writing || !unknown || reconcile is null) return;
        SetBusy(true);
        try
        {
            var receipt = await reconcile(requestId);
            if (receipt is not null) { ResultCode = receipt.Code; saved = true; DialogResult = DialogResult.OK; Close(); }
            else if (OrbitConfirmationDialog.Show(this, "Chưa thấy kết quả; yêu cầu cũ có thể còn chạy. Gửi lại cùng mã và nội dung để SQL đối chiếu?", "Đối chiếu", MessageBoxButtons.OKCancel) == DialogResult.OK)
            { unknown = false; await WriteAsync(); }
            else status.Text = $"Chưa có kết quả xác nhận. Yêu cầu {requestId}.";
        }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or OperationCanceledException) { status.Text = ex.Message; }
        finally { SetBusy(false); }
    }
    private void SetBusy(bool value)
    {
        writing = value; fields.Enabled = !value && !unknown;
        save.Enabled = !value && !unknown; check.Enabled = !value && unknown; cancel.Enabled = !value && !unknown;
    }
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e); layout.FitToContent(this, fields.Controls.OfType<UI.OrbitDialogField>().Any(field => field.Expands));
    }
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!saved && (writing || unknown)) { e.Cancel = true; status.Text = "Hoàn tất ghi hoặc đối chiếu kết quả trước khi đóng."; }
        else if (!saved && dirty && OrbitConfirmationDialog.Show(this, "Bỏ nháp và đóng?", Text, MessageBoxButtons.OKCancel) != DialogResult.OK) e.Cancel = true;
        base.OnFormClosing(e);
    }
    protected override void Dispose(bool disposing) { if (disposing) errors.Dispose(); base.Dispose(disposing); }
    private static IEnumerable<Control> Descendants(Control parent) { foreach (Control child in parent.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; } }
}





