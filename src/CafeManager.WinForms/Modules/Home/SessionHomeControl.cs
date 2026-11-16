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
            "CASH" => L.T("Mở Bán hàng để tạo đơn và thanh toán; dùng Lịch sử hóa đơn để tra cứu/in lại, Giao hàng để chuyển tiến độ."),
            "STORE" => L.T("Mở Nguyên liệu / Tồn để làm việc với kho; dùng Menu / Công thức để tra định mức. Các nghiệp vụ kho tiếp theo sẽ được bổ sung theo kế hoạch."),
            "ADMIN" => L.T("Chọn phân hệ cần quản lý; mở Bán hàng khi cần thao tác POS hoặc Danh mục nền để quản trị dữ liệu tham chiếu."),
            _ => L.T("Trang phiên CoffeeManager")
        };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(24) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var heading = new UI.OrbitHeader { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(20), ColumnCount = 1, RowCount = 1 };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); heading.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var title = new Label { Text = L.T("Trang phiên"), AutoSize = true, Font = AppTheme.TitleFont, Dock = DockStyle.Fill };
        heading.Controls.Add(title);
        var details = new Label { Text = message, Dock = DockStyle.Fill, Padding = new Padding(20), AutoSize = true, Margin = new Padding(0, 20, 0, 0) };
        layout.Controls.Add(heading, 0, 0); layout.Controls.Add(details, 0, 1); Controls.Add(layout);
        AppTheme.Apply(this);
        Controls.Add(new UI.OrbitContentHost(layout, 880));
        title.BackColor = Color.Transparent; details.BackColor = AppTheme.Surface; details.ForeColor = AppTheme.Muted;
    }
}

