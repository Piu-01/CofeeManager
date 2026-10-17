using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.Modules.System;

public sealed class SystemInfoControl : UserControl
{
    private readonly Label information;
    public SystemInfoControl()
    {
        Dock = DockStyle.Fill;
        information = new Label { Dock = DockStyle.Fill, Padding = new Padding(24), Text = "CoffeeManager · .NET 10\r\nChưa tải thông tin schema." };
        Controls.Add(information); AppTheme.Apply(this);
    }
    public void SetSchema(int version) => information.Text = $"CoffeeManager · .NET 10\r\nSchema version: {version}\r\nKết nối được xác nhận khi đọc metadata. Không có tích hợp thanh toán online.";
}
