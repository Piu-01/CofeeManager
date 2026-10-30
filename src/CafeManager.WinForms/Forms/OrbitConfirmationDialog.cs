using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Forms;

public sealed class OrbitConfirmationDialog : CafeManager.WinForms.Forms.OrbitDialogForm
{
    private readonly DialogResult dismissed;

    private OrbitConfirmationDialog(string message, string caption, MessageBoxButtons buttons)
    {
        Text = caption; AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
        ClientSize = new Size(560, 240); MinimumSize = new Size(420, 220); StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        dismissed = buttons == MessageBoxButtons.YesNo ? DialogResult.No : DialogResult.Cancel;
        var approve = new OrbitButton { Text = buttons == MessageBoxButtons.YesNo ? "Có" : "Đồng ý", DialogResult = buttons == MessageBoxButtons.YesNo ? DialogResult.Yes : DialogResult.OK };
        var cancel = new OrbitButton { Text = buttons == MessageBoxButtons.YesNo ? "Không" : "Hủy", DialogResult = dismissed };
        approve.Click += (_, _) => { DialogResult = approve.DialogResult; Close(); };
        cancel.Click += (_, _) => { DialogResult = dismissed; Close(); };
        var actions = new FlowLayoutPanel(); actions.Controls.AddRange([approve, cancel]);
        var content = new Label { Text = message, Font = OrbitManagementTheme.Body, ForeColor = OrbitManagementTheme.Text, AutoSize = true, Padding = new Padding(16), UseMnemonic = false };
        var layout = new OrbitDialogLayout(this, content, new Label(), actions); Controls.Add(layout);
        OrbitManagementTheme.Button(approve, ManagementButtonStyle.Primary, "");
        CancelButton = cancel; AcceptButton = null;
        Shown += (_, _) => { layout.FitToContent(this); cancel.Select(); };
    }

    public static async Task<DialogResult> ShowAsync(Control owner, string message, string caption, MessageBoxButtons buttons = MessageBoxButtons.OK)
    {
        if (buttons is not (MessageBoxButtons.YesNo or MessageBoxButtons.OKCancel or MessageBoxButtons.OK)) throw new ArgumentOutOfRangeException(nameof(buttons));
        using var dialog = new OrbitConfirmationDialog(message, caption, buttons);
        return await OrbitDialogHost.ShowAsync(dialog, owner);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult is DialogResult.None or DialogResult.Cancel) DialogResult = dismissed;
        base.OnFormClosing(e);
    }
}

