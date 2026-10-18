using CafeManager.Models.Dtos;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.Modules.Home;

public sealed class SessionHomeControl : UserControl
{
    public SessionHomeControl() : this(null) { }
    public SessionHomeControl(AccountIdentity? identity)
    {
        Dock = DockStyle.Fill;
        var message = identity?.Role switch
        {
            "CASH" => "Bán hàng chưa triển khai. Phiên thu ngân đã được xác thực.",
            "STORE" => "Kho chưa triển khai. Phiên quản lý kho đã được xác thực.",
            "ADMIN" => "Chọn Danh mục nền để quản trị dữ liệu tham chiếu.",
            _ => "Trang phiên CoffeeManager"
        };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(24) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var heading = new UI.OrbitHeader { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(20), ColumnCount = 1, RowCount = 1 };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); heading.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var title = new Label { Text = "Trang phiên", AutoSize = true, Font = AppTheme.TitleFont, Dock = DockStyle.Fill };
        heading.Controls.Add(title);
        var details = new Label { Text = message, Dock = DockStyle.Fill, Padding = new Padding(20), AutoSize = true, Margin = new Padding(0, 20, 0, 0) };
        layout.Controls.Add(heading, 0, 0); layout.Controls.Add(details, 0, 1); Controls.Add(layout);
        AppTheme.Apply(this);
        Controls.Add(new UI.OrbitContentHost(layout, 880));
        title.BackColor = Color.Transparent; details.BackColor = AppTheme.Surface; details.ForeColor = AppTheme.Muted;
    }
}
