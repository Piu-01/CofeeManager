using System.Diagnostics;
using System.Drawing.Drawing2D;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Chỉ gọi từ xác nhận checkout hợp lệ; cửa sổ sở hữu không nhận focus/chuột.
internal sealed class StarDustOverlay : Form
{
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private readonly Stopwatch clock = new();
    private readonly List<(double Angle, double Radius, double Life, float Size, Color Color)> particles = [];
    private Point origin;
    private readonly Form owner;
    private const int Count = 40;
    private const double MinRadius = 75, MaxRadius = 215, MinLife = 850, MaxLife = 1250;
    public StarDustOverlay(Form owner)
    {
        this.owner = owner; FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual; BackColor = Color.Magenta; TransparencyKey = Color.Magenta;
        DoubleBuffered = true;
        timer.Tick += (_, _) => { if (clock.ElapsedMilliseconds > MaxLife + 50 || owner.IsDisposed || !owner.Visible) HideParticles(); else Invalidate(); };
        owner.LocationChanged += Reposition; owner.SizeChanged += Reposition; owner.VisibleChanged += Reposition;
    }
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get { var p = base.CreateParams; p.ExStyle |= 0x08000000 | 0x20 | 0x80; return p; }
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x84) { m.Result = new IntPtr(-1); return; }
        base.WndProc(ref m);
    }
    public void Burst(Control button)
    {
        if (IsDisposed || owner.IsDisposed) return;
        try
        {
        Bounds = owner.RectangleToScreen(owner.ClientRectangle); origin = PointToClient(button.PointToScreen(new Point(button.Width / 2, button.Height / 2)));
        particles.Clear();
        Color[] colors = [AppTheme.Primary, AppTheme.Accent, AppTheme.Text];
        for (var i = 0; i < Count; i++) particles.Add((Random.Shared.NextDouble() * Math.Tau, MinRadius + Random.Shared.NextDouble() * (MaxRadius - MinRadius), MinLife + Random.Shared.NextDouble() * (MaxLife - MinLife), i % 3 == 0 ? Random.Shared.Next(10, 20) : Random.Shared.Next(3, 8), colors[i % colors.Length]));
        Show(OrbitDialogHost.FindMainForm(owner)); clock.Restart(); timer.Start(); Invalidate();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // Hiệu ứng phụ không được ngắt luồng chốt hoặc tạo một lần gửi lại.
            HideParticles(); Trace.TraceWarning(L.T("Không hiển thị được hiệu ứng thanh toán: {0}"), ex.Message);
        }
    }
    private void Reposition(object? sender, EventArgs e) { if (!owner.Visible) HideParticles(); else if (Visible) Bounds = owner.RectangleToScreen(owner.ClientRectangle); }
    private void HideParticles() { timer.Stop(); particles.Clear(); Hide(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var scale = DeviceDpi / 96F;
        foreach (var p in particles)
        {
            var progress = Math.Min(1, clock.Elapsed.TotalMilliseconds / p.Life); if (progress >= 1) continue;
            var travel = 1 - Math.Pow(1 - progress, 3);
            var x = origin.X + (float)(Math.Cos(p.Angle) * p.Radius * travel * scale);
            var y = origin.Y + (float)((Math.Sin(p.Angle) * p.Radius * travel + 30 * progress * progress) * scale);
            var size = p.Size * scale * (float)(1 - progress * .5);
            using var brush = new SolidBrush(Color.FromArgb((int)(255 * (1 - progress)), p.Color));
            e.Graphics.FillEllipse(brush, x - size / 2, y - size / 2, size, size);
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { timer.Stop(); timer.Dispose(); owner.LocationChanged -= Reposition; owner.SizeChanged -= Reposition; owner.VisibleChanged -= Reposition; }
        base.Dispose(disposing);
    }
}
