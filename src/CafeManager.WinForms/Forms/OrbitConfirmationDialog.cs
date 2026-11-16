using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Forms;

public sealed class OrbitConfirmationDialog : CafeManager.WinForms.Forms.OrbitDialogForm
{
    private readonly DialogResult dismissed;

    private OrbitConfirmationDialog(string message, string caption, MessageBoxButtons buttons, string? approveText, string? cancelText)
    {
        Text = caption; AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
        ClientSize = new Size(560, 240); MinimumSize = new Size(420, 220); StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        dismissed = buttons == MessageBoxButtons.YesNo ? DialogResult.No : DialogResult.Cancel;
        var approve = new OrbitButton { Text = approveText ?? (buttons == MessageBoxButtons.YesNo ? L.T("Có") : L.T("Đồng ý")), DialogResult = buttons == MessageBoxButtons.YesNo ? DialogResult.Yes : DialogResult.OK };
        approve.Click += (_, _) => { DialogResult = approve.DialogResult; Close(); };
        OrbitButton? cancel = null;
        var actions = new FlowLayoutPanel(); actions.Controls.Add(approve);
        if (buttons != MessageBoxButtons.OK)
        {
            cancel = new OrbitButton { Text = cancelText ?? (buttons == MessageBoxButtons.YesNo ? L.T("Không") : L.T("Hủy")), DialogResult = dismissed };
            cancel.Click += (_, _) => { DialogResult = dismissed; Close(); };
            actions.Controls.Add(cancel);
        }
        var content = new Label { Text = message, Font = OrbitManagementTheme.Body, ForeColor = OrbitManagementTheme.Text, AutoSize = true, Padding = new Padding(16), UseMnemonic = false };
        var layout = new OrbitDialogLayout(this, content, new Label(), actions); Controls.Add(layout);
        OrbitManagementTheme.Button(approve, ManagementButtonStyle.Primary, "");
        CancelButton = cancel; AcceptButton = null;
        var focusTarget = cancel is null ? (Control)approve : cancel;
        Shown += (_, _) => { layout.FitToContent(this); focusTarget.Select(); };
    }

    public static async Task<DialogResult> ShowAsync(Control owner, string message, string caption, MessageBoxButtons buttons = MessageBoxButtons.OK, string? approveText = null, string? cancelText = null)
    {
        if (buttons is not (MessageBoxButtons.YesNo or MessageBoxButtons.OKCancel or MessageBoxButtons.OK)) throw new ArgumentOutOfRangeException(nameof(buttons));
        using var dialog = new OrbitConfirmationDialog(message, caption, buttons, approveText, cancelText);
        return await OrbitDialogHost.ShowAsync(dialog, owner);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult is DialogResult.None or DialogResult.Cancel) DialogResult = dismissed;
        base.OnFormClosing(e);
    }
}

