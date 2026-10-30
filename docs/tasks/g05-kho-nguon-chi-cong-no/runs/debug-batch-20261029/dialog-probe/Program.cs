using System.Text.Json;
using CafeManager.WinForms.UI;
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();
        using var form=new Form{Text="G05 footer diagnostic",ClientSize=new Size(900,600),AutoScaleDimensions=new SizeF(96,96),AutoScaleMode=AutoScaleMode.Dpi,ShowInTaskbar=false};
        var actions=new FlowLayoutPanel{AutoSize=true};actions.Controls.AddRange([new OrbitButton{Text="Tính tổng"},new OrbitButton{Text="Chốt nhập kho"},new OrbitButton{Text="Quay lại"}]);
        var layout=new OrbitDialogLayout(form,new Panel(),new Label(),actions);form.Controls.Add(layout);layout.EnableResponsiveActions(form,true);
        form.Shown+=(_,_)=>form.BeginInvoke(()=>form.BeginInvoke(()=>{
            var buttons=actions.Controls.Cast<Control>().Select(button=>{var visible=button.RectangleToScreen(button.ClientRectangle);for(var parent=button.Parent;parent is not null;parent=parent.Parent)visible=Rectangle.Intersect(visible,parent.RectangleToScreen(parent.ClientRectangle));return new{button.Text,Bounds=button.Bounds,button.Visible,VisibleWidth=visible.Width,VisibleHeight=visible.Height,FullyAccessible=visible.Size==button.ClientSize};}).ToArray();
            File.WriteAllText(args[0],JsonSerializer.Serialize(new{Dpi=form.DeviceDpi,Form=form.ClientSize,Actions=actions.ClientSize,Buttons=buttons,Result=buttons.All(x=>x.FullyAccessible)?"PASS":"FAIL"},new JsonSerializerOptions{WriteIndented=true}));form.Close();
        }));Application.Run(form);
    }
}
