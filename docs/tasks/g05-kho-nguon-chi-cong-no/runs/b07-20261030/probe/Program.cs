using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos;
using CafeManager.WinForms.Modules.Finance;
using CafeManager.WinForms.Modules.Inventory;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

internal static class Program
{
    sealed class Pending : IPendingOperationStore
    {
        public PendingOperation? Read(int actor,string operation)=>null;
        public void Save(PendingOperation operation)=>throw new Exception("Read-only lifecycle test");
        public void Clear(int actor,string operation)=>throw new Exception("Read-only lifecycle test");
    }
    static readonly List<object> results=[];
    static readonly List<string> asyncErrors=[];
    static object? Field(object view,string name)=>view.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(view);
    static Task Refresh(Control view)=>(Task)view.GetType().GetMethod("LoadRows",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(view,null)!;
    static void Check(string id,bool ok,object? actual=null){results.Add(new{Id=id,Result=ok?"PASS":"FAIL",Actual=actual});if(!ok)Environment.ExitCode=1;}
    [STAThread]static void Main(string[] args)
    {
        if(args.Length!=3||!System.Text.RegularExpressions.Regex.IsMatch(args[0],@"^CoffeeManager_Test_G05_\d{8}_\d{6}_[a-f0-9]{8}_demo$"))throw new ArgumentException("Disposable DB, marker, output required");
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException+=(_,e)=>{asyncErrors.Add(e.Exception.ToString());Environment.ExitCode=1;};
        using var host=new Form{Text="B07 lifecycle regression",ClientSize=new Size(1280,720),ShowInTaskbar=false};
        host.Shown+=async(_,_)=>
        {
            try
            {
                var cs=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=args[0],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true}.ConnectionString;
                using var guard=new SqlConnection(cs);await guard.OpenAsync();using(var cmd=guard.CreateCommand()){cmd.CommandText="SELECT RunId FROM dbo.G05TestMarker";if((string?)await cmd.ExecuteScalarAsync()!=args[1])throw new Exception("Wrong marker");}
                var factory=new ConnectionFactory(cs);var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);
                var finance=new FinanceService(new FinanceRepository(factory),permissions);var purchase=new PurchaseService(new PurchaseRepository(factory),permissions);var issue=new StockIssueService(new StockIssueRepository(factory),permissions);var count=new StocktakeService(new StocktakeRepository(factory),permissions);var debt=new SupplierDebtService(new SupplierDebtRepository(factory),permissions);
                Check("DPI125",host.DeviceDpi==120,host.DeviceDpi);
                foreach(var name in new[]{"Finance","Purchases","Issues","Stocktakes","Debts"})
                {
                    Control Create(SessionContext session){var pending=new Pending();var links=new DocumentLinks(purchase,finance,debt,session);return name switch{"Finance"=>new FinanceControl(finance,session,pending,links),"Purchases"=>new PurchasesControl(purchase,session,pending,links),"Issues"=>new StockIssuesControl(issue,purchase,session,pending),"Stocktakes"=>new StocktakesControl(count,purchase,session,pending),_=>new SupplierDebtsControl(debt,session,pending,links)};}
                    using(var session=new SessionContext((await auth.ReadIdentityAsync(9001001,default))!))
                    {
                        using var view=Create(session);host.Controls.Add(view);await Loaded(view);var grid=Descendants(view).OfType<DataGridView>().Single();
                        Check(name+".InitialLoad",grid.DataSource is not null);
                        view.Hide();await Task.Delay(50);Check(name+".HideReleasesRead",Field(view,"loading") is null);
                        view.Show();await Loaded(view);Check(name+".ReloadAfterShow",grid.DataSource is not null&&Field(view,"loading") is CancellationTokenSource);
                        view.Dispose();view.Dispose();await Task.Delay(50);Check(name+".DisposeTwice",view.IsDisposed&&Field(view,"loading") is null);
                        var generation=Field(view,"generation");await Refresh(view);Check(name+".NoReadAfterDispose",Equals(generation,Field(view,"generation"))&&Field(view,"loading") is null);
                    }
                    using(var session=new SessionContext((await auth.ReadIdentityAsync(9001001,default))!))
                    {
                        using var view=Create(session);host.Controls.Add(view);await Loaded(view);var grid=Descendants(view).OfType<DataGridView>().Single();var before=grid.DataSource;
                        using var blocker=new SqlConnection(cs);await blocker.OpenAsync();using var transaction=blocker.BeginTransaction();using(var cmd=blocker.CreateCommand()){cmd.Transaction=transaction;cmd.CommandText="SELECT TOP(1) MaTK FROM dbo.TAIKHOAN WITH(TABLOCKX,HOLDLOCK)";await cmd.ExecuteScalarAsync();}
                        var pending=Refresh(view);await Task.Delay(150);Check(name+".RealReadPending",!pending.IsCompleted);
                        view.Dispose();transaction.Rollback();await pending.WaitAsync(TimeSpan.FromSeconds(15));await Task.Delay(50);
                        Check(name+".DisposePendingRead",view.IsDisposed&&Field(view,"loading") is null&&ReferenceEquals(before,grid.DataSource));
                    }
                    using(var session=new SessionContext((await auth.ReadIdentityAsync(9001001,default))!))
                    {
                        using var view=Create(session);host.Controls.Add(view);await Loaded(view);session.Revoke();var generation=Field(view,"generation");await Refresh(view);
                        Check(name+".RevokedSkipsRefresh",Equals(generation,Field(view,"generation")));
                        view.Dispose();view.Dispose();await Task.Delay(50);Check(name+".RevokedDispose",Field(view,"loading") is null&&view.IsDisposed);
                    }
                }
                Check("NoAsyncLifecycleExceptions",asyncErrors.Count==0,asyncErrors);
            }
            catch(Exception e){Check("Harness",false,e.ToString());}
            finally{File.WriteAllText(args[2],JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));host.Close();}
        };
        Application.Run(host);
    }
    static IEnumerable<Control> Descendants(Control root){foreach(Control child in root.Controls){yield return child;foreach(var nested in Descendants(child))yield return nested;}}
    static async Task Loaded(Control view){var workspace=(OrbitListWorkspace)Field(view,"workspace")!;var watch=Stopwatch.StartNew();do{await Task.Delay(30);if(watch.Elapsed.TotalSeconds>30)throw new TimeoutException(workspace.Status.Text);}while(workspace.Status.Text=="Đang tải…");if(workspace.Status.Text.StartsWith("Chưa cập nhật"))throw new Exception(workspace.Status.Text);}
}
