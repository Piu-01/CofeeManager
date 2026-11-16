using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Modules.Sales;

internal enum DraftConflictChoice { OpenExisting, ChangeTable, Discard, Stay }

// Xung đột khi flush gặp bàn đã có đơn ở phiên khác; nháp vẫn được giữ tới khi chọn.
internal sealed class PosDraftConflictDialog : OrbitDialogForm
{
    private DraftConflictChoice choice = DraftConflictChoice.Stay;

    private PosDraftConflictDialog(string message, string caption)
    {
        Text = caption; AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
        ClientSize = new Size(620, 260); MinimumSize = new Size(480, 240); StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        var open = new OrbitButton { Text = L.T("Mở đơn hiện có"), IsPrimary = true, DialogResult = DialogResult.Yes };
        open.Click += (_, _) => { choice = DraftConflictChoice.OpenExisting; DialogResult = DialogResult.Yes; Close(); };
        var change = new OrbitButton { Text = L.T("Chọn bàn khác"), DialogResult = DialogResult.Retry };
        change.Click += (_, _) => { choice = DraftConflictChoice.ChangeTable; DialogResult = DialogResult.Retry; Close(); };
        var discard = new OrbitButton { Text = L.T("Bỏ nháp"), DialogResult = DialogResult.No };
        discard.Click += (_, _) => { choice = DraftConflictChoice.Discard; DialogResult = DialogResult.No; Close(); };
        var stay = new OrbitButton { Text = L.T("Ở lại"), DialogResult = DialogResult.Cancel };
        stay.Click += (_, _) => { choice = DraftConflictChoice.Stay; DialogResult = DialogResult.Cancel; Close(); };
        var actions = new FlowLayoutPanel(); actions.Controls.AddRange([open, change, discard, stay]);
        var content = new Label
        {
            Text = message, Font = OrbitManagementTheme.Body, ForeColor = OrbitManagementTheme.Text,
            AutoSize = true, Padding = new Padding(16), UseMnemonic = false
        };
        var layout = new OrbitDialogLayout(this, content, new Label(), actions); Controls.Add(layout);
        layout.EnableResponsiveActions(this);
        OrbitManagementTheme.Button(open, ManagementButtonStyle.Primary, "");
        CancelButton = stay; AcceptButton = null;
        Shown += (_, _) => { layout.FitToContent(this); stay.Select(); };
    }

    internal static async Task<DraftConflictChoice> ShowAsync(Control owner, string message, string caption)
    {
        if (owner.IsDisposed || owner.Disposing) return DraftConflictChoice.Stay;
        using var dialog = new PosDraftConflictDialog(message, caption);
        await OrbitDialogHost.ShowAsync(dialog, owner);
        return dialog.choice;
    }
}
