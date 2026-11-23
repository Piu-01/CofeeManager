using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Forms;

public enum SaveChangesChoice { Save, Discard, Stay }

// Hỏi Lưu / Bỏ / Ở lại khi rời màn hoặc chuyển đơn mà còn input chưa ghi.
// X/Alt+F4/Esc đều là Ở lại; không đặt AcceptButton để Enter không tự chốt.
public sealed class OrbitSaveChangesDialog : CafeManager.WinForms.Forms.OrbitDialogForm
{
    private OrbitSaveChangesDialog(string message, string caption, string? saveText, string discardText, string stayText)
    {
        Text = caption; AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
        ClientSize = new Size(560, 240); MinimumSize = new Size(420, 220); StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        var actions = new FlowLayoutPanel();
        OrbitButton? primary = null;
        if (saveText is not null)
        {
            primary = new OrbitButton { Text = saveText, IsPrimary = true, DialogResult = DialogResult.OK };
            primary.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };
            actions.Controls.Add(primary);
        }
        var discard = new OrbitButton { Text = discardText, DialogResult = DialogResult.No };
        discard.Click += (_, _) => { DialogResult = DialogResult.No; Close(); };
        var stay = new OrbitButton { Text = stayText, DialogResult = DialogResult.Cancel };
        stay.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        actions.Controls.Add(discard); actions.Controls.Add(stay);
        var content = new Label { Text = message, Font = OrbitManagementTheme.Body, ForeColor = OrbitManagementTheme.Text, AutoSize = true, Padding = new Padding(16), UseMnemonic = false };
        var layout = new OrbitDialogLayout(this, content, new Label(), actions); Controls.Add(layout);
        AcceptButton = null;
        // Focus mặc định nút an toàn để Enter không vô tình Lưu.
        Shown += (_, _) => { layout.FitToContent(this); stay.Select(); };
    }

    public static async Task<SaveChangesChoice> ShowAsync(Control owner, string message, string caption,
        string? saveText, string discardText, string stayText)
    {
        using var dialog = new OrbitSaveChangesDialog(message, caption, saveText, discardText, stayText);
        return (await OrbitDialogHost.ShowAsync(dialog, owner)) switch
        {
            DialogResult.OK => SaveChangesChoice.Save,
            DialogResult.No => SaveChangesChoice.Discard,
            _ => SaveChangesChoice.Stay
        };
    }
}
