using CafeManager.Models.Dtos.Sales;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Modules.Sales;

internal sealed class ProductCardControl : OrbitSurface
{
    internal readonly Button Add;
    private readonly Label name, price;
    private readonly PictureBox image;
    private readonly Image? illustration;
    public ProductCardControl(SalesMenuItem item, EventHandler click)
    {
        Width = 220; Height = 105; Margin = new Padding(6); TabStop = false;
        name = new Label { Text = item.Name, Font = OrbitManagementTheme.Table, ForeColor = OrbitManagementTheme.Text, AutoEllipsis = true, UseMnemonic = false, AccessibleName = item.Name };
        price = new Label { Text = item.CanAdd ? SalesUi.Money(item.Price) : "Ngừng / thiếu công thức", Font = OrbitManagementTheme.Table, ForeColor = item.CanAdd ? OrbitManagementTheme.Caramel : OrbitManagementTheme.Muted, AutoEllipsis = true, UseMnemonic = false };
        image = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom, AccessibleName = "Ảnh minh họa schematic, không phải ảnh thật món", TabStop = false };
        using var stream = typeof(ProductCardControl).Assembly.GetManifestResourceStream("CafeManager.WinForms.Resources.Orbit.ceramic-coffee.png");
        if (stream is not null) { using var source = Image.FromStream(stream); illustration = new Bitmap(source); image.Image = illustration; }
        var add = new OrbitButton { AccessibleName = "Thêm " + item.Name, Enabled = item.CanAdd, MinimumSize = Size.Empty };
        OrbitManagementTheme.Button(add, ManagementButtonStyle.Segment, "plus");
        add.Click += click; Add = add;
        Controls.AddRange([image, name, price, Add]);
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e); if (name is null) return;
        int P(float n) => OrbitManagementTheme.Px(this, n);
        var artWidth = Math.Clamp((int)(Width * .29F), P(48), P(92));
        image.SetBounds(P(8), P(10), artWidth, Math.Max(1, Height - P(20)));
        var left = image.Right + P(8); var textWidth = Math.Max(1, Width - left - P(10));
        name.SetBounds(left, P(17), textWidth, P(36));
        Add.SetBounds(Width - P(40), Height - P(43), P(29), P(29));
        price.SetBounds(left, Height - P(43), Math.Max(1, Add.Left - left - P(5)), P(30));
    }
    protected override void Dispose(bool disposing) { if (disposing) illustration?.Dispose(); base.Dispose(disposing); }
}
