using System.Drawing.Drawing2D;
using System.Globalization;
using CafeManager.Models.Dtos.Reports;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.Modules.Home;

internal sealed class DashboardChannelChart : Control
{
    private IReadOnlyList<SalesReportRow> rows=[];
    private IReadOnlyDictionary<string,decimal?> shares=new Dictionary<string,decimal?>();
    private readonly ToolTip tooltip=new();
    private Rectangle[] legend=[];
    private decimal total;
    private int selected;
    public event Action<string>? ChannelSelected;
    public DashboardChannelChart()
    {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Selectable,true);
        TabStop=true;AccessibleName=L.T("Cơ cấu doanh thu theo kênh");BackColor=OrbitManagementTheme.Surface;ForeColor=OrbitManagementTheme.Text;
        HandleCreated+=(_,_)=>Height=OrbitManagementTheme.Px(this,320);DpiChangedAfterParent+=(_,_)=>Height=OrbitManagementTheme.Px(this,320);
    }
    public void SetData(IReadOnlyList<SalesReportRow> values,decimal amount,IReadOnlyDictionary<string,decimal?> percentages)
    {rows=values;total=amount;shares=percentages;selected=0;legend=new Rectangle[rows.Count];Describe();Invalidate();}
    private static string Money(decimal n)=>n.ToString("#,##0",CultureInfo.GetCultureInfo("vi-VN"))+" ₫";
    private string Share(SalesReportRow row)=>shares.TryGetValue("share.channel."+row.Code,out var value)&&value.HasValue?value.Value.ToString("0.#",CultureInfo.GetCultureInfo("vi-VN"))+"%":"—";
    private void Describe()
    {var text=rows.Count==0?L.T("Chưa có số liệu."):Models.States.SalesStates.Label(rows[selected].Code)+": "+Money(rows[selected].CustomerPaid)+" · "+Share(rows[selected]);AccessibleDescription=text;tooltip.SetToolTip(this,text);}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.Clear(BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;
        var font=OrbitManagementTheme.DrawingFont(this,OrbitManagementTheme.Small);
        var colors=new[]{OrbitManagementTheme.Caramel,OrbitManagementTheme.Lilac,AppTheme.Success};
        var gap=OrbitManagementTheme.Px(this,12);var diameter=Math.Max(1,Math.Min(OrbitManagementTheme.Px(this,140),Width-gap*2));
        var circle=new RectangleF((Width-diameter)/2F,gap,diameter,diameter);var angle=-90F;
        if(total>0)
        {
            for(var i=0;i<rows.Count;i++)
            {
                var sweep=(float)(rows[i].CustomerPaid/total*360);if(sweep<=0)continue;
                using var pen=new Pen(colors[i%colors.Length],OrbitManagementTheme.Px(this,18));g.DrawArc(pen,circle,angle,sweep);angle+=sweep;
            }
            TextRenderer.DrawText(g,L.T("Tổng trong kỳ\n")+Money(total),font,Rectangle.Round(circle),ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);
        }
        else TextRenderer.DrawText(g,L.T("Chưa có hóa đơn\nđã thanh toán trong kỳ."),font,Rectangle.Round(circle),ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);
        var top=(int)circle.Bottom+gap*2;var height=OrbitManagementTheme.Px(this,36);
        for(var i=0;i<rows.Count;i++)
        {
            legend[i]=new Rectangle(gap,top+i*height,Math.Max(1,Width-gap*2),height);
            var rect=legend[i];using var brush=new SolidBrush(colors[i%colors.Length]);g.FillEllipse(brush,rect.Left,rect.Top+4,8,8);
            TextRenderer.DrawText(g,Models.States.SalesStates.Label(rows[i].Code),font,new Rectangle(rect.Left+gap,rect.Top,Math.Max(1,rect.Width-OrbitManagementTheme.Px(this,65)),font.Height+4),ForeColor,TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g,Share(rows[i]),font,new Rectangle(rect.Left,rect.Top,rect.Width,font.Height+4),colors[i%colors.Length],TextFormatFlags.Right|TextFormatFlags.SingleLine);
            TextRenderer.DrawText(g,Money(rows[i].CustomerPaid),font,new Rectangle(rect.Left+gap,rect.Top+font.Height+2,Math.Max(1,rect.Width-gap),font.Height+4),OrbitManagementTheme.Muted,TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);
            if(Focused&&i==selected)ControlPaint.DrawFocusRectangle(g,rect,ForeColor,BackColor);
        }
    }
    protected override bool IsInputKey(Keys keyData)=>keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down||base.IsInputKey(keyData);
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);for(var i=0;i<legend.Length;i++)if(legend[i].Contains(e.Location)){selected=i;Describe();}}
    protected override void OnMouseClick(MouseEventArgs e){base.OnMouseClick(e);Focus();for(var i=0;i<legend.Length;i++)if(legend[i].Contains(e.Location)){selected=i;Describe();ChannelSelected?.Invoke(rows[i].Code);Invalidate();return;}}
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);if(rows.Count==0)return;
        if(e.KeyCode is Keys.Left or Keys.Up)selected=(selected+rows.Count-1)%rows.Count;
        else if(e.KeyCode is Keys.Right or Keys.Down)selected=(selected+1)%rows.Count;
        else if(e.KeyCode is Keys.Enter or Keys.Space)ChannelSelected?.Invoke(rows[selected].Code);else return;
        e.Handled=e.SuppressKeyPress=true;Describe();Invalidate();
    }
    protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Describe();Invalidate();}
    protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
    protected override void Dispose(bool disposing){if(disposing)tooltip.Dispose();base.Dispose(disposing);}
}
