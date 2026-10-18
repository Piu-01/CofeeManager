using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.Modules.System;

public sealed class SystemInfoControl : UserControl
{
    private readonly Label information;
    public SystemInfoControl()
    {
        Dock = DockStyle.Fill;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(24) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var heading = new UI.OrbitHeader { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(20), ColumnCount = 1, RowCount = 1 };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); heading.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var title = new Label { Text = "Thông tin hệ thống", AutoSize = true, Font = AppTheme.TitleFont, Dock = DockStyle.Fill };
        heading.Controls.Add(title);
        information = new Label { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(20), Margin = new Padding(0, 20, 0, 0), Text = "CoffeeManager · .NET 10\r\nChưa tải thông tin schema." };
        layout.Controls.Add(heading, 0, 0); layout.Controls.Add(information, 0, 1); Controls.Add(layout); AppTheme.Apply(this);
        Controls.Add(new UI.OrbitContentHost(layout, 880));
        title.BackColor = Color.Transparent; information.BackColor = AppTheme.Surface; information.ForeColor = AppTheme.Muted;
    }
    public void SetSchema(int version) => information.Text = $"CoffeeManager · .NET 10\r\nSchema version: {version}\r\nKết nối được xác nhận khi đọc metadata. Không có tích hợp thanh toán online.";
}
