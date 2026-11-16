using System.Drawing.Drawing2D;
using System.Globalization;
using CafeManager.Models.Dtos.Reports;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.Modules.Home;

// Chỉ vẽ dữ liệu đã tổng hợp; mọi phép chia dưới đây phục vụ tọa độ biểu đồ.
internal sealed class DashboardTrendChart : Control
{
    private IReadOnlyList<SalesReportRow> current=[],previous=[];
    private readonly ToolTip tooltip=new();
    private RectangleF plot;
    private int selected;
    public event Action<int>? PointSelected;
    public DashboardTrendChart()
    {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Selectable,true);
        TabStop=true;AccessibleName=L.T("Xu hướng doanh thu");BackColor=OrbitManagementTheme.Surface;ForeColor=OrbitManagementTheme.Text;
        HandleCreated+=(_,_)=>Height=OrbitManagementTheme.Px(this,280);
        DpiChangedAfterParent+=(_,_)=>Height=OrbitManagementTheme.Px(this,280);
    }
    public void SetData(IReadOnlyList<SalesReportRow> values,IReadOnlyList<SalesReportRow> comparison)
    {current=values;previous=comparison;selected=0;Describe();Invalidate();}
    private static string Money(decimal value)=>value.ToString("#,##0",CultureInfo.GetCultureInfo("vi-VN"))+" ₫";
    private static string Time(SalesReportRow row)=>DateTime.Parse(row.Code,CultureInfo.InvariantCulture).ToString("dd/MM/yyyy HH:mm",CultureInfo.GetCultureInfo("vi-VN"));
    private string Detail()=>current.Count==0?L.T("Chưa có số liệu."):Time(current[selected])+": "+Money(current[selected].CustomerPaid)+
        (selected<previous.Count?L.T(" · Kỳ trước ")+Time(previous[selected])+": "+Money(previous[selected].CustomerPaid):L.T(" · Không có mốc so sánh tương ứng."));
    private void Describe(){AccessibleDescription=Detail();tooltip.SetToolTip(this,Detail());}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.Clear(BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;
        var font=OrbitManagementTheme.DrawingFont(this,OrbitManagementTheme.Small);var gap=OrbitManagementTheme.Px(this,12);
        var maximum=current.Concat(previous).Select(r=>r.CustomerPaid).DefaultIfEmpty(0).Max();
        var axisMaximum=maximum>0?maximum:1;
        var longest=TextRenderer.MeasureText(Money(axisMaximum),font).Width;
        plot=new RectangleF(Math.Max(OrbitManagementTheme.Px(this,52),longest+gap),gap,
            Width-Math.Max(OrbitManagementTheme.Px(this,52),longest+gap)-gap,Height-OrbitManagementTheme.Px(this,64));
        if(plot.Width<OrbitManagementTheme.Px(this,90)||plot.Height<40)
        {TextRenderer.DrawText(g,L.T("Mở rộng vùng biểu đồ để xem số liệu."),font,ClientRectangle,ForeColor,TextFormatFlags.WordBreak);return;}
        using var grid=new Pen(OrbitManagementTheme.Border);
        for(var i=0;i<=2;i++)
        {
            var value=axisMaximum*i/2;var y=plot.Bottom-(float)(value/axisMaximum)*plot.Height;
            g.DrawLine(grid,plot.Left,y,plot.Right,y);
            TextRenderer.DrawText(g,Money(value),font,new Rectangle(0,(int)y-font.Height/2,(int)plot.Left-gap,font.Height+4),OrbitManagementTheme.Muted,TextFormatFlags.Right|TextFormatFlags.SingleLine);
        }
        var count=Math.Max(current.Count,previous.Count);
        PointF Point(int index,decimal value)=>new(count<=1?plot.Left+plot.Width/2:plot.Left+index*plot.Width/(count-1),plot.Bottom-(float)(value/axisMaximum)*plot.Height);
        void Series(IReadOnlyList<SalesReportRow> rows,Color color,bool dashed)
        {
            using var pen=new Pen(color,OrbitManagementTheme.Px(this,2)){DashStyle=dashed?DashStyle.Dash:DashStyle.Solid};
            var points=rows.Select((row,index)=>Point(index,row.CustomerPaid)).ToArray();
            if(points.Length>1)g.DrawLines(pen,points);
            if(points.Length==1){using var brush=new SolidBrush(color);g.FillEllipse(brush,points[0].X-3,points[0].Y-3,6,6);}
        }
        Series(previous,OrbitManagementTheme.Lilac,true);Series(current,OrbitManagementTheme.Caramel,false);
        if(current.Count>0)
        {
            var every=Math.Max(1,(int)Math.Ceiling(current.Count/Math.Max(1,plot.Width/OrbitManagementTheme.Px(this,80))));
            for(var i=0;i<current.Count;i++)if(i%every==0||i==current.Count-1)
            {
                if(i==current.Count-1&&i%every!=0&&i%every<every/2)continue;
                var x=Point(i,0).X;var text=current[i].Name;
                if(text.Length>5)text=text[..5];
                var width=TextRenderer.MeasureText(text,font).Width;
                TextRenderer.DrawText(g,text,font,new Point((int)Math.Clamp(x-width/2,plot.Left,plot.Right-width),(int)plot.Bottom+gap/2),OrbitManagementTheme.Muted);
            }
            if(Focused){var point=Point(selected,current[selected].CustomerPaid);using var brush=new SolidBrush(ForeColor);g.FillEllipse(brush,point.X-4,point.Y-4,8,8);}
        }
        if(current.All(r=>r.Bills==0)&&previous.All(r=>r.Bills==0))
            TextRenderer.DrawText(g,L.T("Chưa có hóa đơn đã thanh toán trong kỳ."),font,Rectangle.Round(plot),ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);
        var legendY=Height-OrbitManagementTheme.Px(this,20);
        TextRenderer.DrawText(g,L.T("━ Kỳ này"),font,new Point((int)plot.Left,legendY),OrbitManagementTheme.Caramel);
        TextRenderer.DrawText(g,L.T("┄ Kỳ trước"),font,new Point((int)plot.Left+OrbitManagementTheme.Px(this,100),legendY),OrbitManagementTheme.Lilac);
        if(Focused)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(ClientRectangle,-2,-2),ForeColor,BackColor);
    }
    protected override bool IsInputKey(Keys keyData)=>keyData is Keys.Left or Keys.Right or Keys.Home or Keys.End||base.IsInputKey(keyData);
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);if(current.Count==0||plot.Width<=0)return;
        selected=Math.Clamp((int)Math.Round((e.X-plot.Left)/plot.Width*Math.Max(0,Math.Max(current.Count,previous.Count)-1)),0,current.Count-1);Describe();Invalidate();
    }
    protected override void OnMouseClick(MouseEventArgs e){base.OnMouseClick(e);Focus();if(current.Count>0&&plot.Contains(e.Location))PointSelected?.Invoke(selected);}
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);if(current.Count==0)return;
        if(e.KeyCode==Keys.Left)selected=Math.Max(0,selected-1);else if(e.KeyCode==Keys.Right)selected=Math.Min(current.Count-1,selected+1);
        else if(e.KeyCode==Keys.Home)selected=0;else if(e.KeyCode==Keys.End)selected=current.Count-1;
        else if(e.KeyCode is Keys.Enter or Keys.Space)PointSelected?.Invoke(selected);else return;
        e.Handled=e.SuppressKeyPress=true;Describe();Invalidate();
    }
    protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Describe();Invalidate();}
    protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
    protected override void Dispose(bool disposing){if(disposing)tooltip.Dispose();base.Dispose(disposing);}
}
