using System.Data;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Reports;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Home;
using CafeManager.WinForms.Modules.Reports;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

if(args.Length!=3||!System.Text.RegularExpressions.Regex.IsMatch(args[0],@"^CoffeeManager_Test_G07_\d{8}_\d{6}_[a-f0-9]{8}_demo$"))throw new ArgumentException("Exact test target required");
var b=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=args[0],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false};
await using(var marker=new SqlConnection(b.ConnectionString)){await marker.OpenAsync();await using var c=marker.CreateCommand();c.CommandText="SELECT RunId FROM dbo.G07TestMarker";if((string?)await c.ExecuteScalarAsync()!=args[1])throw new InvalidOperationException("Marker mismatch");}
var factory=new ConnectionFactory(b.ConnectionString);typeof(ConnectionFactory).GetProperty("SessionInitializer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(factory,(Func<SqlConnection,CancellationToken,Task>)(async(c,t)=>{await using var cmd=c.CreateCommand();cmd.CommandText="EXECUTE AS USER='ExtendedRuntime'";await cmd.ExecuteNonQueryAsync(t);}));
var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);using var session=new SessionContext((await auth.ReadIdentityAsync(1))!);
var results=new List<object>();var failures=0;
var thread=new Thread(()=>{
 Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);using var main=new MainForm();main.ClientSize=new(1366,768);
 var reports=new ReportService(new ReportRepository(factory),permissions);var assets=new AssetService(new AssetRepository(factory),permissions);var payroll=new PayrollService(new PayrollRepository(factory),new EmployeeRepository(factory),permissions);
 var links=new DocumentLinks(new PurchaseService(new PurchaseRepository(factory),permissions),new FinanceService(new FinanceRepository(factory),permissions),new SupplierDebtService(new SupplierDebtRepository(factory),permissions),session,payroll,assets);
 var navigation=new ReportSourceNavigation(session,links,new InvoiceService(new InvoiceRepository(factory),permissions),new StockIssueService(new StockIssueRepository(factory),permissions),new StocktakeService(new StocktakeRepository(factory),permissions),new AttendanceService(new AttendanceRepository(factory),permissions));
 using var dashboard=new AdminDashboardControl(reports,session,navigation);var content=(Panel)typeof(MainForm).GetField("content",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(main)!;content.Controls.Add(dashboard);
 T Field<T>(string name)=>(T)typeof(AdminDashboardControl).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(dashboard)!;
 IEnumerable<Control> Desc(Control parent){foreach(Control c in parent.Controls){yield return c;foreach(var child in Desc(c))yield return child;}}
 void Assert(bool condition,string text){if(!condition)throw new InvalidOperationException(text);}
 async Task Wait(Func<bool> condition){for(var n=0;n<240;n++){if(condition())return;await Task.Delay(50);}throw new TimeoutException(Field<Label>("state").Text);}
 void Save()=>File.WriteAllText(args[2],JsonSerializer.Serialize(new{Dpi=main.DeviceDpi,Results=results,Failures=failures,Visual="SKIPPED by user assignment"},new JsonSerializerOptions{WriteIndented=true}));
 async Task Check(string id,string expected,Func<Task<object?>> action){try{var actual=await action();results.Add(new{Id=id,Expected=expected,Status="PASS",Actual=actual});}catch(Exception e){failures++;results.Add(new{Id=id,Expected=expected,Status="FAIL",Actual=e.ToString()});}Save();}
 using var watchdog=new System.Windows.Forms.Timer{Interval=120000};watchdog.Tick+=(_,_)=>{failures++;results.Add(new{Id="watchdog",Status="FAIL",Actual="120s"});Save();main.Close();};
 main.Shown+=async(_,_)=>{
 watchdog.Start();try{
  if(main.DeviceDpi!=120){results.Add(new{Id="native",Status="NOT RUN",Actual="Actual desktop is not125%; no scaling changes"});Save();return;}
  await Wait(()=>Field<bool>("ready"));
  await Check("DA12/sizes-money","1280×720/1366×768/pane;4/2/1KPIs,2/1charts;18digit label bound, accessible full value",async()=>{
   var measured=new List<object>();var seen=new HashSet<int>();foreach(var size in new[]{new Size(1800,850),new Size(1280,720),new Size(1366,768),new Size(700,720)}){
    main.ClientSize=size;await Task.Delay(150);var metrics=Field<TableLayoutPanel>("metrics");seen.Add(metrics.ColumnCount);Assert(metrics.Controls.Count==4,"Missing KPI");Assert(metrics.Controls.Cast<Control>().All(c=>c.Width>0&&c.Height>0),"Card has no bounds");measured.Add(new{main.ClientSize,metrics.ColumnCount,ChartColumns=Field<TableLayoutPanel>("charts").ColumnCount});
   }
   Assert(seen.SetEquals(new[]{1,2,4}),"Not all responsive columns seen: "+string.Join(',',seen));
   typeof(AdminDashboardControl).GetMethod("SetValue",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(dashboard,["revenue",(decimal?)999999999999999999m,true]);
   var label=Field<Dictionary<string,Label>>("values")["revenue"];Assert(label.AccessibleName!.Contains("999"),"Full amount inaccessible");Assert(label.Width>0&&label.Height>0,"Amount bounds empty");
   Field<OrbitButton>("refresh").PerformClick();await Wait(()=>Field<bool>("ready"));return measured;
  });
  await Check("DA11/late-mode","locked requestA cancelled;requestB monthly only after release;never renders old period",async()=>{
   if(Field<bool>("month")){Field<OrbitButton>("today").PerformClick();await Wait(()=>Field<bool>("ready"));}
   await using var locker=new SqlConnection(b.ConnectionString);await locker.OpenAsync();await using var transaction=await locker.BeginTransactionAsync();await using(var c=locker.CreateCommand()){c.Transaction=(SqlTransaction)transaction;c.CommandText="SELECT COUNT(*) FROM dbo.HOADON WITH(TABLOCKX,HOLDLOCK)";await c.ExecuteScalarAsync();}
   Field<OrbitButton>("refresh").PerformClick();await Task.Delay(100);Assert(!Field<bool>("ready"),"Old snapshot ready while pending");Field<OrbitButton>("monthly").PerformClick();await Task.Delay(100);await transaction.RollbackAsync();await Wait(()=>Field<bool>("ready"));Assert(Field<bool>("month"),"Mode rolled back");
   var page=Field<IReadOnlyDictionary<string,ReportPage>>("pages")["trend.current"];Assert(page.Metadata.From.Day==1,"Day result overwrote month");return page.Metadata;
  });
  await Check("DA08/Invoice-Finance-Debt","source modal+child detail both owned;closing preserves dashboard mode",async()=>{
   var checkedKinds=new List<string>();foreach(var kind in new[]{"revenue","expense","debt"}){
    var bounds=Field<(DateTime From,DateTime Until,DateTime? PreviousFrom,DateTime? PreviousUntil)?>("period")!.Value;
    var sources=await reports.DashboardAnalyticsSourcesAsync(session,kind,kind=="debt"?null:bounds.From,kind=="debt"?null:bounds.Until);Assert(sources.TotalCount>0,"No source fixture "+kind);
    var mode=Field<bool>("month");var sourceObserved=false;var childObserved=false;string? failure=null;
    using var closer=new System.Windows.Forms.Timer{Interval=60};var ticks=0;closer.Tick+=(_,_)=>{
     ticks++;var forms=Application.OpenForms.Cast<Form>().Where(f=>f!=main).ToArray();var source=forms.FirstOrDefault(f=>f.Owner==main);var child=forms.FirstOrDefault(f=>source is not null&&f.Owner==source);
     if(child is not null){childObserved=child.Modal&&!child.ShowInTaskbar&&!Native.IsWindowEnabled(source!.Handle);child.Close();source!.Close();closer.Stop();return;}
     if(source is not null&&!sourceObserved){var grid=Desc(source).OfType<DataGridView>().FirstOrDefault();var button=Desc(source).OfType<OrbitButton>().FirstOrDefault(v=>v.Text=="Xem chi tiết");if(grid is not null&&grid.Rows.Count>0&&button?.Enabled==true){sourceObserved=source.Modal&&!source.ShowInTaskbar&&!Native.IsWindowEnabled(main.Handle);button.PerformClick();}}
     if(ticks>150){failure="Modal detail timed out "+kind;foreach(var f in forms.Reverse())f.Close();closer.Stop();}
    };
    closer.Start();var invoke=(Task)typeof(AdminDashboardControl).GetMethod("OpenSourcesAsync",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(dashboard,[kind,kind,kind=="debt",null,null,null,null])!;await invoke;
    Assert(failure is null&&sourceObserved&&childObserved,failure??"Owner/child source failure "+kind);Assert(Field<bool>("month")==mode,"Mode lost after detail");checkedKinds.Add(kind);
   }return checkedKinds;
  });
  await Check("DA11/connection-error","missing testDB shows error and — values, no fake0;dispose cancels",async()=>{
   dashboard.Hide();var invalid=new SqlConnectionStringBuilder(b.ConnectionString){InitialCatalog=args[0]+"_missing",ConnectTimeout=1};var broken=new ReportService(new ReportRepository(new ConnectionFactory(invalid.ConnectionString,1)),permissions);using var errorControl=new AdminDashboardControl(broken,session);content.Controls.Add(errorControl);errorControl.BringToFront();
   for(var n=0;n<200;n++){var pending=typeof(AdminDashboardControl).GetField("pending",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(errorControl);var status=(Label)typeof(AdminDashboardControl).GetField("state",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(errorControl)!;if(pending is null&&status.ForeColor==AppTheme.Error)break;await Task.Delay(50);}
   var values=(Dictionary<string,Label>)typeof(AdminDashboardControl).GetField("values",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(errorControl)!;var state=(Label)typeof(AdminDashboardControl).GetField("state",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(errorControl)!;Assert(values.Values.All(l=>l.Text=="—")&&state.ForeColor==AppTheme.Error,"Failure painted as data");content.Controls.Remove(errorControl);dashboard.Show();await Wait(()=>Field<bool>("ready"));return state.Text;
  });
 }catch(Exception e){failures++;results.Add(new{Id="native-setup",Status="FAIL",Actual=e.ToString()});Save();}finally{watchdog.Stop();main.Close();}
 };
 Application.Run(main);
});thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();Environment.ExitCode=failures==0?0:1;
internal static class Native{[DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]internal static extern bool IsWindowEnabled(IntPtr h);}
