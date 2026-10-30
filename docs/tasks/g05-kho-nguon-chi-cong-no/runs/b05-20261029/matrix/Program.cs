using System.Text.Json;
using CafeManager.WinForms.UI;
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();var results=new List<object>();
        foreach(var config in new[]{("One",1,false,true),("Three",3,false,true),("Seven",7,false,true),("Leading",7,true,true),("Legacy",3,false,false)})
        {
            using var form=new Form{Text=config.Item1,ClientSize=new Size(900,600),AutoScaleDimensions=new SizeF(96,96),AutoScaleMode=AutoScaleMode.Dpi,ShowInTaskbar=false};
            var actions=new FlowLayoutPanel{AutoSize=true};for(var i=0;i<config.Item2;i++)actions.Controls.Add(new OrbitButton{Text=i==config.Item2-1?"Quay lại":"Thao tác kiểm thử "+i});
            var lead=config.Item3?new Button{Text="Hành động đầu",AutoSize=true}:null;var body=new Panel();var layout=new OrbitDialogLayout(form,body,new Label(),actions,leadingAction:lead);form.Controls.Add(layout);if(config.Item4)layout.EnableResponsiveActions(form,true);
            form.Shown+=(_,_)=>form.BeginInvoke(()=>form.BeginInvoke(()=>{
                foreach(var size in new[]{new Size(640,420),new Size(1280,720),new Size(1366,768)})
                {
                    form.ClientSize=size;layout.PerformLayout();Measure("All",size);
                    if(config.Item2==7){actions.Controls[1].Visible=false;layout.PerformLayout();Measure("Hidden",size);actions.Controls[1].Visible=true;actions.Controls[2].Text="Kiểm tra kết quả sau khi bị timeout";layout.PerformLayout();Measure("ShownLong",size);}
                }
                if(!config.Item4)results.Add(new{Case="LegacyFlags",Result=actions.AutoSize&&!actions.WrapContents?"PASS":"FAIL"});form.Close();
            }));Application.Run(form);
            void Measure(string state,Size size)
            {
                var checks=actions.Controls.Cast<Control>().Where(x=>x.Visible).Select(x=>new{x.Text,Accessible=Accessible(x),x.Left,x.Top,x.Width,x.Height}).ToArray();
                results.Add(new{Case=config.Item1,State=state,Size=size,Dpi=form.DeviceDpi,Body=body.ClientSize,Result=form.DeviceDpi!=120?"BLOCKED":checks.All(x=>x.Accessible)&&body.Height>0&&(lead is null||Accessible(lead))?"PASS":"FAIL",Buttons=checks});
            }
        }
        File.WriteAllText(args[0],JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
    }
    private static bool Accessible(Control c){var rect=c.RectangleToScreen(c.ClientRectangle);for(var p=c.Parent;p is not null;p=p.Parent)rect=Rectangle.Intersect(rect,p.RectangleToScreen(p.ClientRectangle));return c.Width>0&&c.Height>0&&rect.Size==c.ClientSize;}
}
