using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Thông báo dùng chung; màu luôn đi kèm nội dung chữ, không sở hữu nghiệp vụ.
public sealed class OrbitNotice : Label
{
    private ModuleState kind = ModuleState.Empty;
    [System.ComponentModel.DefaultValue(ModuleState.Empty)]
    public ModuleState Kind { get => kind; set { kind = value; ApplyTone(); } }
    public OrbitNotice()
    {
        AutoSize = true; Dock = DockStyle.Fill; Padding = new Padding(12); Margin = new Padding(0, 12, 0, 0);
        Font = OrbitManagementTheme.Table; UseMnemonic = false; ApplyTone();
    }
    private void ApplyTone()
    {
        ForeColor = kind switch { ModuleState.Error => AppTheme.Error, ModuleState.Conflict or ModuleState.Unknown => AppTheme.Warning, ModuleState.Saved => AppTheme.Success, _ => OrbitManagementTheme.Muted };
        var surface = OrbitManagementTheme.Surface;
        BackColor = Color.FromArgb((surface.R * 7 + ForeColor.R) / 8, (surface.G * 7 + ForeColor.G) / 8, (surface.B * 7 + ForeColor.B) / 8); Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); if (Width < 3 || Height < 3) return;
        using var border = new Pen(Color.FromArgb(100, ForeColor));
        using var path = OrbitManagementTheme.Round(new RectangleF(1, 1, Width - 3, Height - 3), OrbitManagementTheme.Px(this, 6));
        e.Graphics.DrawPath(border, path);
    }
}
