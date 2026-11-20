using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Sales;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Sales;
using CafeManager.WinForms.UI;

internal static class Program
{
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static T F<T>(object o,string n)=>(T)o.GetType().GetField(n,Flags)!.GetValue(o)!;
    [STAThread] static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();Application.SetColorMode(SystemColorMode.Dark);
        using var session=new SessionContext(new AccountIdentity(0,"SMOKE","Constructor only","smoke","CASH",true,true,new byte[8]));session.Revoke();
        using var page=new SalesControl(null!,null!,null!,null!,session);using var main=new MainForm{Text="POS compact technical bounds, no Auth/SQL"};
        F<Panel>(main,"content").Controls.Add(page);
        var results=new List<object>();int failed=0;void Check(string name,bool ok,object? actual=null){if(!ok)failed++;results.Add(new{Name=name,Outcome=ok?"PASS":"FAIL",Actual=actual});}
        bool Inside(Control c){var r=c.RectangleToScreen(c.ClientRectangle);for(var p=c.Parent;p!=null;p=p.Parent)if(!p.RectangleToScreen(p.ClientRectangle).Contains(r))return false;return true;}
        main.Shown+=async(_,_)=>
        {
            try
            {
                Check("DeviceDpi120",page.DeviceDpi==120,page.DeviceDpi);
                var orderPanel=F<Control>(page,"orderPanel");var lines=F<DataGridView>(orderPanel,"Lines");var products=F<FlowLayoutPanel>(page,"products");var types=typeof(SalesControl).Assembly;
                var show=orderPanel.GetType().GetMethod("ShowOrder",Flags)!;
                var header=new SalesOrderHeader("HD99999999",DateTime.Today,"SMOKE",null,null,null,null,"Mang di",null,null,null,null,null,0,0,81000,0,0,81000,"Chua thanh toan",new byte[8],true,"None",0,DateTime.Now);
                foreach(var size in new[]{new Size(1280,720),new Size(1366,768)})
                {
                    main.ClientSize=size;await Task.Yield();
                    foreach(int count in new[]{0,1,50})
                    {
                        foreach(Control c in products.Controls.Cast<Control>().ToArray())c.Dispose();products.Controls.Clear();
                        foreach(var i in Enumerable.Range(0,count))products.Controls.Add((Control)Activator.CreateInstance(types.GetType("CafeManager.WinForms.Modules.Sales.ProductCardControl")!,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,[new SalesMenuItem("SP001","Tên món tiếng Việt dài",81000,"Dang ban",true),new EventHandler((_,_)=>{})],null)!);
                        var rows=Enumerable.Range(1,count).Select(i=>new SalesLine(i,"SP001","Tên món",2,40500,"Ít đá, không đường",81000)).ToList();show.Invoke(orderPanel,[count==0?null:new SalesOrderDto(header,rows,Array.Empty<SalesPayment>())]);
                        page.PerformLayout();orderPanel.PerformLayout();await Task.Yield();
                        foreach(var field in new[]{"topActions","tableHost","menuSurface","orderPanel","menuFooter","products","state"})Check($"{size}/{count} inside {field}",Inside(F<Control>(page,field)),F<Control>(page,field).Bounds.ToString());
                        foreach(var field in new[]{"pay","header","edit","delete","cancel","reconcile"})Check($"{size}/{count} inside button {field}",Inside(F<Control>(page,field)),F<Control>(page,field).Bounds.ToString());
                        Check($"{size}/{count} grid has header+one row",lines.Height>=lines.ColumnHeadersHeight+lines.RowTemplate.Height,new{Lines=lines.Bounds.ToString(),Panel=orderPanel.Size.ToString(),Header=lines.ColumnHeadersHeight,Row=lines.RowTemplate.Height});
                        Check($"{size}/{count} grid before actions",lines.Bottom<=F<Panel>(orderPanel,"Actions").Top,lines.Bounds.ToString());
                        if(count>0){var card=products.Controls[0];Check($"{size}/{count} card height",Math.Abs(card.Height-105*page.DeviceDpi/96F)<=1);foreach(Control child in card.Controls)Check($"{size}/{count} card child {child.GetType().Name}",card.ClientRectangle.Contains(child.Bounds),child.Bounds.ToString());}
                    }
                    results.Add(new{Observation=size.ToString(),Content=page.Size.ToString(),Order=orderPanel.Bounds.ToString(),Grid=lines.Bounds.ToString(),Menu=products.Bounds.ToString(),Cards=products.Controls.Count});
                }
                main.MinimumSize=Size.Empty;main.ClientSize=new Size(960,900);await Task.Yield();page.PerformLayout();
                var tabs=F<TabControl>(page,"compactTabs");Check("Narrow reparent Món/Đơn",tabs.Visible&&orderPanel.Parent==tabs.TabPages[1]);tabs.SelectedIndex=1;await Task.Yield();page.PerformLayout();orderPanel.PerformLayout();
                foreach(var field in new[]{"topActions","tableHost","state","pay","header","edit","delete","cancel","reconcile"})Check("Compact inside "+field,Inside(F<Control>(page,field)),F<Control>(page,field).Bounds.ToString());
                Check("Compact grid header+one row",lines.Height>=lines.ColumnHeadersHeight+lines.RowTemplate.Height,lines.Bounds.ToString());
                var selected=(lines.CurrentRow?.DataBoundItem as SalesLine)?.LineId;
                main.ClientSize=new Size(1366,768);await Task.Yield();page.PerformLayout();Check("Restore parents and selection",!tabs.Visible&&orderPanel.Parent==F<TableLayoutPanel>(page,"workspace")&&(lines.CurrentRow?.DataBoundItem as SalesLine)?.LineId==selected);
                results.Add(new{Observation="Compact branch",Client="960x900; narrower content; no Scale change",Mode="Native tabs Món/Đơn"});
            }
            catch(Exception e){Check("Runner exception",false,(e.InnerException??e).ToString());}
            finally{File.WriteAllText(Path.Combine(args[0],"layout-smoke.json"),JsonSerializer.Serialize(new{Failed=failed,Scope="Technical bounds only; revoked constructor-only session and in-memory DTO, no auth/SQL/business interaction or visual QA",Results=results},new JsonSerializerOptions{WriteIndented=true}));main.Dispose();Application.ExitThread();}
        };
        Application.Run(main);return failed==0?0:1;
    }
}
