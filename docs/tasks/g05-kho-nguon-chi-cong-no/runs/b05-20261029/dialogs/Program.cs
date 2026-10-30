using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Finance;
using CafeManager.WinForms.Modules.Inventory;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;
internal static class Program
{
    private sealed class Pending : IPendingOperationStore
    {
        public PendingOperation? Read(int a,string op)=>null;
        public void Save(PendingOperation op)=>throw new InvalidOperationException("Read-only component probe");
        public void Clear(int a,string op)=>throw new InvalidOperationException("Read-only component probe");
    }
    [STAThread]
    private static void Main(string[] args)
    {
        if(args.Length!=3||!System.Text.RegularExpressions.Regex.IsMatch(args[0],@"^CoffeeManager_Test_G05_\d{8}_\d{6}_[a-f0-9]{8}_demo$"))throw new ArgumentException("Exact DB, marker, output required");
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);Application.ThreadException+=(_,e)=>{File.WriteAllText(args[2]+".thread-error.txt",e.Exception.ToString());Environment.Exit(1);};Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();
        var cs=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=args[0],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true}.ConnectionString;
        var results=new List<object>();using var host=new MainForm{Text="G05 B05 component",ClientSize=new Size(1280,720),ShowInTaskbar=false};
        host.Shown+=async(_,_)=>
        {
            try
            {
                using var c=new SqlConnection(cs);await c.OpenAsync();using var cmd=c.CreateCommand();cmd.CommandText="SELECT RunId FROM dbo.G05TestMarker";if((string?)await cmd.ExecuteScalarAsync()!=args[1])throw new InvalidOperationException("Wrong marker");cmd.CommandText="SELECT MIN(MaTK) FROM dbo.TAIKHOAN WHERE MaVT='ADMIN' AND HoatDong=1";var actor=Convert.ToInt32(await cmd.ExecuteScalarAsync());
                var factory=new ConnectionFactory(cs);var auth=new AuthRepository(factory);var permission=new PermissionService(auth);using var session=new SessionContext((await auth.ReadIdentityAsync(actor,default))!);
                var finance=new FinanceService(new FinanceRepository(factory),permission);var purchase=new PurchaseService(new PurchaseRepository(factory),permission);var issue=new StockIssueService(new StockIssueRepository(factory),permission);var stocktake=new StocktakeService(new StocktakeRepository(factory),permission);var debt=new SupplierDebtService(new SupplierDebtRepository(factory),permission);var pending=new Pending();var links=new DocumentLinks(purchase,finance,debt,session);
                var fr=(await finance.ListAsync(session,new(DateTime.Today.AddDays(-30),DateTime.Today),default))[0];var pn=(await purchase.ListAsync(session,"",0))[0];var px=(await issue.ListAsync(session,"",0))[0];var cn=(await debt.ListAsync(session,"",null,0))[0];
                Func<OrbitDialogForm>[] factories=[()=>new FinanceEntryDialog(finance,session,pending),()=>new FinanceDetailDialog(fr,links),()=>new PurchaseEditorDialog(purchase,session,pending),()=>new PurchaseDetailDialog(pn,links),()=>new StockIssueEditorDialog(issue,purchase,session,pending),()=>new StockIssueDetailDialog(px),()=>new StocktakeEditorDialog(stocktake,purchase,session,pending),()=>new SupplierDebtDetailDialog(cn,links),()=>new DebtPaymentDialog(debt,session,pending,cn.Code)];
                foreach(var create in factories)
                {
                    using var dialog=create();dialog.Shown+=async(_,_)=>
                    {
                        try
                        {
                            var watch=Stopwatch.StartNew();var busy=dialog.GetType().GetField("busy",BindingFlags.Instance|BindingFlags.NonPublic);
                            while(busy?.GetValue(dialog) is true){if(watch.Elapsed.TotalSeconds>30)throw new TimeoutException(dialog.Text);await Task.Delay(20);}
                            await Task.Delay(30);Measure("Loaded");
                            dialog.ClientSize=new Size(640,420);dialog.PerformLayout();Measure("Resized");
                            if(dialog.GetType().GetField("check",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(dialog) is Button check){check.Visible=true;dialog.PerformLayout();Measure("ReconcileVisible");}
                        }
                        catch(Exception e){results.Add(new{Dialog=dialog.GetType().Name,Result="FAIL",Error=e.ToString()});Environment.ExitCode=1;}
                        finally{typeof(OrbitDialogForm).GetMethod("Abort",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(dialog,null);}
                        void Measure(string state)
                        {
                            var layout=dialog.Controls.OfType<OrbitDialogLayout>().Single();var footer=(TableLayoutPanel)layout.GetControlFromPosition(0,3)!;var actions=(FlowLayoutPanel)footer.GetControlFromPosition(1,0)!;
                            var buttons=actions.Controls.Cast<Control>().Where(x=>x.Visible).Select(x=>new{x.Text,x.Enabled,Accessible=Accessible(x),x.Width,x.Height}).ToArray();
                            var ok=buttons.Length>0&&buttons.All(x=>x.Accessible)&&dialog.DeviceDpi==120&&dialog.AcceptButton is null;
                            results.Add(new{Dialog=dialog.GetType().Name,State=state,Result=ok?"PASS":"FAIL",Dpi=dialog.DeviceDpi,Client=dialog.ClientSize,InsideWorkingArea=Screen.FromControl(dialog).WorkingArea.Contains(dialog.Bounds),Buttons=buttons});File.WriteAllText(args[2],JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
                        }
                    };
                    await OrbitDialogHost.ShowAsync(dialog,host);
                }
            }
            catch(Exception e){results.Add(new{Result="FAIL",Error=e.ToString()});Environment.ExitCode=1;}
            finally{File.WriteAllText(args[2],JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));host.Close();}
        };Application.Run(host);
    }
    private static bool Accessible(Control c){var rect=c.RectangleToScreen(c.ClientRectangle);for(var p=c.Parent;p is not null;p=p.Parent)rect=Rectangle.Intersect(rect,p.RectangleToScreen(p.ClientRectangle));return c.Width>0&&c.Height>0&&rect.Size==c.ClientSize;}
}




