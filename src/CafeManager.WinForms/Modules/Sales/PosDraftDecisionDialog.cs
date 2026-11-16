using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Modules.Sales;

internal enum DraftDecision { Save, Discard, Stay }

// Hộp native 3 lựa chọn cho nháp POS; mặc định Ở lại khi Esc/đóng.
internal sealed class PosDraftDecisionDialog : OrbitDialogForm
{
    private DraftDecision decision = DraftDecision.Stay;

    private PosDraftDecisionDialog(string message, string caption)
    {
        Text = caption; AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
        ClientSize = new Size(560, 240); MinimumSize = new Size(420, 220); StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        var save = new OrbitButton { Text = L.T("Giữ"), IsPrimary = true, DialogResult = DialogResult.OK };
        save.Click += (_, _) => { decision = DraftDecision.Save; DialogResult = DialogResult.OK; Close(); };
        var discard = new OrbitButton { Text = L.T("Không giữ"), DialogResult = DialogResult.No };
        discard.Click += (_, _) => { decision = DraftDecision.Discard; DialogResult = DialogResult.No; Close(); };
        var stay = new OrbitButton { Text = L.T("Ở lại"), DialogResult = DialogResult.Cancel };
        stay.Click += (_, _) => { decision = DraftDecision.Stay; DialogResult = DialogResult.Cancel; Close(); };
        var actions = new FlowLayoutPanel(); actions.Controls.AddRange([save, discard, stay]);
        var content = new Label
        {
            Text = message, Font = OrbitManagementTheme.Body, ForeColor = OrbitManagementTheme.Text,
            AutoSize = true, Padding = new Padding(16), UseMnemonic = false
        };
        var layout = new OrbitDialogLayout(this, content, new Label(), actions); Controls.Add(layout);
        layout.EnableResponsiveActions(this);
        OrbitManagementTheme.Button(save, ManagementButtonStyle.Primary, "");
        CancelButton = stay; AcceptButton = null;
        Shown += (_, _) => { layout.FitToContent(this); stay.Select(); };
    }

    internal static async Task<DraftDecision> ShowAsync(Control owner, string message, string caption)
    {
        if (owner.IsDisposed || owner.Disposing) return DraftDecision.Stay;
        using var dialog = new PosDraftDecisionDialog(message, caption);
        await OrbitDialogHost.ShowAsync(dialog, owner);
        return dialog.decision;
    }
}
