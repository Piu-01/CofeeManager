using CafeManager.Models.Dtos.Sales;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.Modules.Sales;

internal sealed class ProductCardControl : UserControl
{
    internal readonly Button Add;
    private Image? illustration;
    public ProductCardControl(SalesMenuItem item, EventHandler click)
    {
        Width = 220; Height = 210; Margin = new Padding(6); Padding = new Padding(10); BackColor = AppTheme.Surface;
        var name = new Label { Text = item.Name, Dock = DockStyle.Top, Height = 42, AutoEllipsis = true, AccessibleName = item.Name };
        var image = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, AccessibleName = "Ảnh minh họa schematic, không phải ảnh thật món" };
        using var stream = typeof(ProductCardControl).Assembly.GetManifestResourceStream("CafeManager.WinForms.Resources.Orbit.ceramic-coffee.png");
        if (stream is not null) { using var source = Image.FromStream(stream); illustration = new Bitmap(source); image.Image = illustration; }
        Add = SalesUi.Button(item.CanAdd ? SalesUi.Money(item.Price) + "  +" : "Ngừng / thiếu công thức", click);
        Add.Enabled = item.CanAdd; Add.Dock = DockStyle.Bottom; Add.AccessibleName = "Thêm " + item.Name;
        Controls.Add(image); Controls.Add(name); Controls.Add(Add);
    }
    protected override void Dispose(bool disposing) { if (disposing) illustration?.Dispose(); base.Dispose(disposing); }
}
