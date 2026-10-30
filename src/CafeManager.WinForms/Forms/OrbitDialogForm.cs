using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Forms;

// Guard đóng giữ kết quả của modal native khi phải hủy close để hỏi bỏ nháp.
public class OrbitDialogForm : Form
{
    private bool closing, approved;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal OrbitDialogHost? DialogHost { get; set; }
    protected virtual Task<bool> CanCloseAsync() => Task.FromResult(true);

    internal Task<bool> RequestCloseAsync() => CloseWithResultAsync(DialogResult);
    private async Task<bool> CloseWithResultAsync(DialogResult requested)
    {
        if (IsDisposed) return true;
        if (closing) return false;
        closing = true;
        try
        {
            if (!await CanCloseAsync() || IsDisposed) { if (!IsDisposed) DialogResult = DialogResult.None; return IsDisposed; }
            approved = true;
            DialogResult = requested == DialogResult.None ? DialogResult.Cancel : requested;
            Close(); return true;
        }
        finally { closing = false; }
    }
    internal void Abort()
    {
        approved = true; DialogResult = DialogResult.Cancel; Close();
    }
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogHost is not null && !approved)
        {
            var requested = DialogResult;
            e.Cancel = true;
            if (!closing) BeginInvoke((Action)(async () => await CloseWithResultAsync(requested)));
        }
        base.OnFormClosing(e);
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { _ = RequestCloseAsync(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}
