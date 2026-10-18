using System.ComponentModel;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Adapter giờ/phút: giữ DateTime typed cho request hiện có, không tính nghiệp vụ.
public sealed class OrbitTimeInput : UserControl
{
    private readonly NumericUpDown hour = new ClockPart { Maximum = 23, BorderStyle = BorderStyle.None, TextAlign = HorizontalAlignment.Center, AccessibleName = "Giờ" };
    private readonly NumericUpDown minute = new ClockPart { Maximum = 59, BorderStyle = BorderStyle.None, TextAlign = HorizontalAlignment.Center, AccessibleName = "Phút" };
    private readonly Label separator = new() { Text = ":", TextAlign = ContentAlignment.MiddleCenter, AutoSize = false };
    private DateTime date = DateTime.Today;
    private bool setting;
    public event EventHandler? ValueChanged;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DateTime Value
    {
        get => date.Date.AddHours((int)hour.Value).AddMinutes((int)minute.Value);
        set
        {
            var changed = Value != value.Date.AddHours(value.Hour).AddMinutes(value.Minute);
            setting = true; date = value.Date; hour.Value = value.Hour; minute.Value = value.Minute; setting = false;
            if (changed) ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public OrbitTimeInput()
    {
        TabStop = false; BackColor = AppTheme.Surface;
        hour.TabIndex = 0; minute.TabIndex = 1;
        Controls.AddRange([hour, separator, minute]);
        hour.ValueChanged += Changed; minute.ValueChanged += Changed;
    }
    private void Changed(object? sender, EventArgs e) { if (!setting) ValueChanged?.Invoke(this, EventArgs.Empty); }
    private sealed class ClockPart : NumericUpDown
    {
        protected override void UpdateEditText() => Text = Value.ToString("00");
    }
    public override Size GetPreferredSize(Size proposedSize) => new(160, hour.PreferredSize.Height + (int)(16 * DeviceDpi / 96F));
    protected override void OnLayout(LayoutEventArgs e)
    {
        var gap = (int)(8 * DeviceDpi / 96F); var part = (int)(64 * DeviceDpi / 96F);
        Height = GetPreferredSize(Size.Empty).Height;
        hour.SetBounds(gap, gap, part, hour.PreferredSize.Height);
        separator.SetBounds(gap + part, gap, gap * 2, hour.Height);
        minute.SetBounds(gap * 3 + part, gap, part, minute.PreferredSize.Height);
        base.OnLayout(e);
    }
}
