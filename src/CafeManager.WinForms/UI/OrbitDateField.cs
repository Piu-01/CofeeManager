using System.Globalization;
using System.Runtime.InteropServices;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Giữ TextBox và MonthCalendar native cho nhập tiếng Việt, bàn phím và accessibility.
public sealed class OrbitDateField : UserControl
{
    private readonly TextBox input = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "dd/MM/yyyy", AccessibleName = "Ngày, định dạng dd/MM/yyyy" };
    private readonly CalendarButton open = new() { Dock = DockStyle.Right, AccessibleName = "Mở lịch chọn ngày" };
    private readonly OrbitCalendar calendar = new();
    private readonly ToolStripDropDown popup;
    private readonly ToolStripControlHost calendarHost;
    private readonly ToolTip tooltip = new();
    public event EventHandler? ValueChanged;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public DateTime Value
    {
        get
        {
            if (!DateTime.TryParseExact(input.Text.Trim(), "dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var date)
                || date < calendar.MinDate || date > calendar.MaxDate)
                throw new ArgumentException("Ngày không hợp lệ. Nhập dd/MM/yyyy trong khoảng 01/01/1753–31/12/9998.");
            return date.Date;
        }
        set
        {
            var date = value.Date;
            if (date < calendar.MinDate || date > calendar.MaxDate) throw new ArgumentOutOfRangeException(nameof(value));
            input.Text = date.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));
            calendar.SetDate(date);
        }
    }

    public OrbitDateField()
    {
        AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
        Height = 34; MinimumSize = new Size(180, 34); TabStop = false;
        Controls.Add(input); Controls.Add(open);
        calendarHost = new ToolStripControlHost(calendar) { Margin = Padding.Empty, Padding = Padding.Empty, AutoSize = false };
        popup = new ToolStripDropDown { Padding = new Padding(1), BackColor = AppTheme.Border, AutoClose = true, AutoSize = false };
        popup.Items.Add(calendarHost);
        tooltip.SetToolTip(open, "Chọn ngày (Alt + ↓)");
        popup.Opened += (_, _) => SizeAndPositionCalendar();
        popup.Closed += (_, _) => { if (!IsDisposed && input.CanFocus) input.Focus(); };
        open.Click += (_, _) => OpenCalendar();
        input.KeyDown += (_, e) =>
        {
            if (e.Alt && e.KeyCode == Keys.Down) { e.SuppressKeyPress = true; OpenCalendar(); }
            else if (e.KeyCode == Keys.Escape && popup.Visible) { e.SuppressKeyPress = true; popup.Close(); }
        };
        // Cả nội dung chưa hợp lệ cũng phải vô hiệu preview cũ, không giữ kết quả ngày trước.
        input.TextChanged += (_, _) => ValueChanged?.Invoke(this, EventArgs.Empty);
        calendar.DateSelected += (_, e) => { Value = e.Start; popup.Close(); };
        AppTheme.Apply(this);
        Value = DateTime.Today;
    }

    internal void ApplyManagementTheme()
    {
        Height = OrbitManagementTheme.Px(this, 40);
        popup.BackColor = OrbitManagementTheme.Border;
        calendar.BackColor = OrbitManagementTheme.Surface; calendar.ForeColor = OrbitManagementTheme.Text;
        calendar.TitleBackColor = OrbitManagementTheme.Selected; calendar.TitleForeColor = OrbitManagementTheme.Text;
        calendar.TrailingForeColor = OrbitManagementTheme.Muted;
    }

    private void OpenCalendar()
    {
        if (!Enabled) return;
        try { calendar.SetDate(Value); }
        catch (ArgumentException) { /* Giữ nguyên ngày gõ sai; chọn lịch mới sẽ thay bằng ngày hợp lệ. */ }
        // Tạo HWND trước khi đo: bỏ visual style làm kích thước native khác kích thước constructor.
        _ = popup.Handle;
        _ = calendar.Handle;
        SizeAndPositionCalendar();
        popup.Show(popup.Location);
        calendar.Focus();
    }

    private void SizeAndPositionCalendar()
    {
        calendar.Size = calendar.SingleMonthSize;
        calendarHost.Size = calendar.Size;
        popup.ClientSize = new Size(calendar.Width + popup.Padding.Horizontal, calendar.Height + popup.Padding.Vertical);
        popup.PerformLayout();
        var area = Screen.FromControl(this).WorkingArea;
        var below = PointToScreen(new Point(0, Height));
        var above = PointToScreen(Point.Empty).Y - popup.Height;
        popup.Location = new Point(
            Math.Clamp(below.X, area.Left, Math.Max(area.Left, area.Right - popup.Width)),
            Math.Clamp(below.Y + popup.Height <= area.Bottom ? below.Y : above,
                area.Top, Math.Max(area.Top, area.Bottom - popup.Height)));
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        // Nút icon vuông theo chiều cao thực đã được WinForms scale, không nhân DPI lần hai.
        open.Width = Height;
        base.OnLayout(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (!Enabled && popup is not null) popup.Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { popup.Dispose(); tooltip.Dispose(); }
        base.Dispose(disposing);
    }

    private sealed class OrbitCalendar : MonthCalendar
    {
        public OrbitCalendar()
        {
            MaxSelectionCount = 1; ShowWeekNumbers = false;
            BackColor = AppTheme.Surface; ForeColor = AppTheme.Text;
            TitleBackColor = AppTheme.Selected; TitleForeColor = AppTheme.Text;
            TrailingForeColor = AppTheme.Muted; Font = AppTheme.BodyFont;
            AccessibleName = "Lịch chọn một ngày";
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Chỉ bỏ visual style của HWND lịch này để palette có hiệu lực; không đổi theme toàn app/Windows.
            SetWindowTheme(Handle, "", "");
        }

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr window, string subAppName, string subIdList);
    }

    private sealed class CalendarButton : OrbitButton
    {
        private readonly Dictionary<int, Bitmap> icons = new();

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var size = (int)Math.Round(20 * DeviceDpi / 96F);
            var sourceSize = size <= 20 ? 20 : size <= 24 ? 24 : 32;
            if (!icons.TryGetValue(sourceSize, out var icon))
            {
                using var stream = typeof(OrbitDateField).Assembly.GetManifestResourceStream($"CafeManager.WinForms.Resources.Orbit.calendar-{sourceSize}.png")!;
                using var source = new Bitmap(stream);
                icons[sourceSize] = icon = new Bitmap(source);
            }
            var bounds = new Rectangle((Width - size) / 2, (Height - size) / 2, size, size);
            if (Enabled) e.Graphics.DrawImage(icon, bounds);
            else ControlPaint.DrawImageDisabled(e.Graphics, icon, bounds.X, bounds.Y, BackColor);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) foreach (var icon in icons.Values) icon.Dispose();
            base.Dispose(disposing);
        }
    }
}
