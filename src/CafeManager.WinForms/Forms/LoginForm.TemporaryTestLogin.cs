#if DEBUG
using CafeManager.Business.Security;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Forms;

public partial class LoginForm
{
    private OrbitButton? temporaryTestButton;
    private Func<CancellationToken, Task<SessionContext>>? temporaryTestSession;

    internal void ConfigureTemporaryTestLogin(Func<CancellationToken, Task<SessionContext>> createSession)
    {
        if (temporaryTestButton is not null) return;
        temporaryTestSession = createSession;
        temporaryTestButton = new OrbitButton { Text = "Bỏ qua đăng nhập — TEST", AccessibleName = "Bỏ qua đăng nhập — TEST", TabIndex = 4, Dock = DockStyle.Top };
        OrbitManagementTheme.Button(temporaryTestButton, ManagementButtonStyle.Secondary, "");
        temporaryTestButton.AutoSize = true; temporaryTestButton.Margin = new Padding(3, 0, 3, 14);
        layout.SuspendLayout();
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowCount++;
        layout.SetRow(statusLabel, 8); layout.Controls.Add(temporaryTestButton, 0, 7);
        layout.ResumeLayout(true);
        temporaryTestButton.Click += async (_, _) =>
        {
            if (temporaryTestSession is not null) await RunLoginAsync(temporaryTestSession);
        };
    }

    partial void SetTemporaryTestLoginBusy(bool busy)
    {
        if (temporaryTestButton is not null) temporaryTestButton.Enabled = !busy;
    }
}
#endif
