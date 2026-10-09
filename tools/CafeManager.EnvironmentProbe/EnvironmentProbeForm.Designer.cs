#nullable enable
namespace CafeManager.EnvironmentProbe;

partial class EnvironmentProbeForm
{
    private System.ComponentModel.IContainer? components = null;
    private Label environmentLabel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        environmentLabel = new Label();
        SuspendLayout();
        //
        // environmentLabel
        //
        environmentLabel.AccessibleName = "Thông tin kiểm chứng môi trường";
        environmentLabel.Dock = DockStyle.Fill;
        environmentLabel.Location = new Point(16, 16);
        environmentLabel.Name = "environmentLabel";
        environmentLabel.Size = new Size(528, 208);
        environmentLabel.TabIndex = 0;
        environmentLabel.Text = "CoffeeManager — Kiểm chứng môi trường T01\r\n\r\nWinForms .NET 10\r\nĐóng cửa sổ để kết thúc kiểm tra.";
        environmentLabel.TextAlign = ContentAlignment.MiddleCenter;
        //
        // EnvironmentProbeForm
        //
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(560, 240);
        Controls.Add(environmentLabel);
        MinimumSize = new Size(400, 220);
        Name = "EnvironmentProbeForm";
        Padding = new Padding(16);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CoffeeManager — T01 Environment Probe";
        ResumeLayout(false);
    }

    #endregion
}
