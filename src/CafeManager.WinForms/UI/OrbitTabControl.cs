using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

public class OrbitTabControl : TabControl
{
    [System.ComponentModel.DefaultValue(false)]
    public bool FlatManagementTabs { get; set; }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        base.OnDrawItem(e);
        if (!FlatManagementTabs || e.Index < 0) return;
        using var brush = new SolidBrush(AppTheme.Background);
        e.Graphics.FillRectangle(brush,e.Bounds);
        var selected=e.Index==SelectedIndex;
        TextRenderer.DrawText(e.Graphics,TabPages[e.Index].Text,Font,e.Bounds,selected?AppTheme.Primary:AppTheme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        if(selected){using var line=new Pen(AppTheme.Primary,2*DeviceDpi/96F);e.Graphics.DrawLine(line,e.Bounds.Left+4,e.Bounds.Bottom-2,e.Bounds.Right-4,e.Bounds.Bottom-2);}
        if(Focused&&selected) ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(e.Bounds,-4,-4));
    }
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
        if(!FlatManagementTabs)for (var index = 0; index < TabCount; index++) chrome.Exclude(GetTabRect(index));
        using var background = new SolidBrush(FlatManagementTabs ? AppTheme.Background : AppTheme.Surface);
        graphics.FillRegion(background, chrome);
        if(FlatManagementTabs)
        {
            // Phủ cả chrome native trong tab rect rồi vẽ lại; không chừa viền hệ thống sáng.
            for(var index=0;index<TabCount;index++)
            {
                var bounds=GetTabRect(index);var selected=index==SelectedIndex;
                graphics.FillRectangle(background,bounds);
                TextRenderer.DrawText(graphics,TabPages[index].Text,Font,bounds,selected?AppTheme.Primary:AppTheme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
                if(selected){using var line=new Pen(AppTheme.Primary,2*DeviceDpi/96F);graphics.DrawLine(line,bounds.Left+4,bounds.Bottom-2,bounds.Right-4,bounds.Bottom-2);}
                if(Focused&&selected)ControlPaint.DrawFocusRectangle(graphics,Rectangle.Inflate(bounds,-4,-4));
            }
        }
        using var border = new Pen(AppTheme.Border, Math.Max(1F, DeviceDpi / 96F));
        for (var index = 0; !FlatManagementTabs && index < TabCount; index++)
        {
            var bounds = GetTabRect(index);
            if (bounds.Width > 1 && bounds.Height > 1)
                graphics.DrawRectangle(border, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);
        }
        var pageBorder = Rectangle.Inflate(DisplayRectangle, 1, 1);
        if (pageBorder.Width > 0 && pageBorder.Height > 0)
        {
            using var pagePen = new Pen(FlatManagementTabs ? AppTheme.Background : AppTheme.Border,2);
            graphics.DrawRectangle(pagePen, pageBorder);
        }
    }
}
