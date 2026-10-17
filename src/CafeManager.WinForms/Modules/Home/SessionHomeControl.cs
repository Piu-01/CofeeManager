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
        Controls.Add(new Label { Text = message, Dock = DockStyle.Fill, Padding = new Padding(24), AutoSize = false });
        AppTheme.Apply(this);
    }
}
