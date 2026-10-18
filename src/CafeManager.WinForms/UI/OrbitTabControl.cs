using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

public class OrbitTabControl : TabControl
{
    protected override CreateParams CreateParams
    {
        get
        {
            // Opt-in trước base.CreateParams để native HWND nhận dark theme của .NET 10.
            SetStyle(ControlStyles.ApplyThemingImplicitly, true);
            return base.CreateParams;
        }
    }

    protected override void WndProc(ref Message message)
    {
        base.WndProc(ref message);
        const int Paint = 0x000F;
        const int PrintClient = 0x0318;
        if (!IsHandleCreated || message.Msg is not (Paint or PrintClient)) return;
        if (message.Msg == PrintClient && message.WParam == 0) return;

        // Chỉ tô chrome ngoài TabPages; native giữ chọn tab, bàn phím và accessibility.
        using var graphics = message.Msg == PrintClient
            ? Graphics.FromHdc(message.WParam) : Graphics.FromHwnd(Handle);
        using var chrome = new Region(ClientRectangle);
        chrome.Exclude(DisplayRectangle);
        for (var index = 0; index < TabCount; index++) chrome.Exclude(GetTabRect(index));
        using var background = new SolidBrush(AppTheme.Surface);
        graphics.FillRegion(background, chrome);
        using var border = new Pen(AppTheme.Border, Math.Max(1F, DeviceDpi / 96F));
        for (var index = 0; index < TabCount; index++)
        {
            var bounds = GetTabRect(index);
            if (bounds.Width > 1 && bounds.Height > 1)
                graphics.DrawRectangle(border, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);
        }
        var pageBorder = Rectangle.Inflate(DisplayRectangle, 1, 1);
        if (pageBorder.Width > 0 && pageBorder.Height > 0) graphics.DrawRectangle(border, pageBorder);
    }
}
