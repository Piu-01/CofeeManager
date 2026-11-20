using System.Data;
using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Reports;
using CafeManager.Models.Requests.Reports;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Home;
using CafeManager.WinForms.Modules.Reports;
using CafeManager.WinForms.Navigation;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

if(args.Length!=3||!System.Text.RegularExpressions.Regex.IsMatch(args[0],@"^CoffeeManager_Test_G07_\d{8}_\d{6}_[a-f0-9]{8}_demo$"))throw new ArgumentException("Test target required");
var b=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=args[0],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false};
await using var owner=new SqlConnection(b.ConnectionString);await owner.OpenAsync();
async Task<object?> Scalar(string sql){await using var c=owner.CreateCommand();c.CommandText=sql;return await c.ExecuteScalarAsync();}
if((string?)await Scalar("SELECT RunId FROM dbo.G07TestMarker")!=args[1])throw new InvalidOperationException("Marker mismatch");
var factory=new ConnectionFactory(b.ConnectionString);typeof(ConnectionFactory).GetProperty("SessionInitializer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(factory,(Func<SqlConnection,CancellationToken,Task>)(async(c,t)=>{await using var cmd=c.CreateCommand();cmd.CommandText="EXECUTE AS USER='ExtendedRuntime'";await cmd.ExecuteNonQueryAsync(t);}));
var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);using var session=new SessionContext((await auth.ReadIdentityAsync(1))!);var reports=new ReportService(new ReportRepository(factory),permissions);
var results=new List<object>();var failures=0;
void Assert(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
void Save()=>File.WriteAllText(args[2],JsonSerializer.Serialize(new{Results=results,Failures=failures},new JsonSerializerOptions{WriteIndented=true}));
async Task Check(string id,string expected,Func<Task<object?>> action){try{results.Add(new{Id=id,Expected=expected,Status="PASS",Actual=await action()});}catch(Exception e){failures++;results.Add(new{Id=id,Expected=expected,Status="FAIL",Actual=e.ToString()});}Save();}
async Task Denied(SessionContext target){var n=0;try{await reports.ReadDashboardAnalyticsAsync(target,false);}catch(UserOperationException){n++;}try{await reports.DashboardAnalyticsSourcesAsync(target,"orders",null,null);}catch(Exception e) when(e is UserOperationException or OperationCanceledException){n++;}Assert(n==2,"Revoked account read allowed");}
var employees=new EmployeeService(new EmployeeRepository(factory),permissions);var accounts=new EmployeeAccountService(new EmployeeAccountRepository(factory),permissions,new PasswordService());
await Check("DA09/role-lock","role change and account disable revoke old identity; analytics/source both denied",async()=>{
 var employee=await employees.SaveAsync(session,new("Dashboard role fixture",null,"Full-time",1000,0,"CV001"),null,Guid.NewGuid());var password=Guid.NewGuid().ToString("N");await accounts.WriteAsync(session,"Create",new(employee,null,"da."+Guid.NewGuid().ToString("N")[..12],"ADMIN",true),password,password,Guid.NewGuid());
 var accountId=Convert.ToInt32(await Scalar("SELECT MaTK FROM dbo.TAIKHOAN WHERE MaNV='"+employee+"'"));using var old=new SessionContext((await auth.ReadIdentityAsync(accountId))!);await reports.ReadDashboardAnalyticsAsync(old,false);
 await accounts.WriteAsync(session,"Change",new(employee,old.Identity.SecurityStamp,null,"CASH",true),null,null,Guid.NewGuid());await Denied(old);Assert(old.IsRevoked,"Role change did not revoke");
 var cash=(await auth.ReadIdentityAsync(accountId))!;await accounts.WriteAsync(session,"Change",new(employee,cash.SecurityStamp,null,"ADMIN",true),null,null,Guid.NewGuid());using var active=new SessionContext((await auth.ReadIdentityAsync(accountId))!);await reports.ReadDashboardAnalyticsAsync(active,false);
 await accounts.WriteAsync(session,"Change",new(employee,active.Identity.SecurityStamp,null,"ADMIN",false),null,null,Guid.NewGuid());await Denied(active);Assert(active.IsRevoked,"Disable did not revoke");return new{accountId,RoleRevoked=old.IsRevoked,DisabledRevoked=active.IsRevoked};
});
await Check("DA08/typed-bucket-channel-product","source header within[From,Until);source channel/product real codes;totals match at captured cutoff",async()=>{
 var d=await reports.ReadDashboardAnalyticsAsync(session,false);var from=d.Period.From;var until=d.Period.Until;
 var bucket=d.Pages["trend.current"].Rows.OfType<SalesReportRow>().Last(r=>r.Bills>0);var start=DateTime.Parse(bucket.Code,System.Globalization.CultureInfo.InvariantCulture);var end=start.AddHours(1)<until?start.AddHours(1):until;
 var source=await reports.DashboardAnalyticsSourcesAsync(session,"trend",start,end);Assert(source.Rows.All(s=>s.SourceDate>=start&&s.SourceDate<end),"Source outside bucket");
 var product=await reports.DashboardAnalyticsSourcesAsync(session,"product",from,until,product:"SP999");Assert(product.Rows.All(r=>r.SourceType=="Invoice"),"Fake product source");
 var channel=await reports.DashboardAnalyticsSourcesAsync(session,"channel",from,until,channel:"Mang di");Assert(channel.Rows.All(r=>r.Name=="Mang di"),"Wrong channel");return new{Bucket=bucket.Code,BucketTotal=source.TotalCount,ProductCount=product.TotalCount,ChannelCount=channel.TotalCount};
});
await Check("DA14/all-legacy-reports","9 legacy report types and source routes callable after028;no duplicate IDs",async()=>{
 var values=new List<object>();foreach(var kind in Enum.GetValues<ReportKind>()){
  var filter=new ReportFilter(kind,DateOnly.FromDateTime(DateTime.Today),DateOnly.FromDateTime(DateTime.Today));var p=await reports.ReadAsync(session,filter);Assert(p.Metadata.Kind==kind,"Wrong report kind");
  if(p.Rows.Count>0){var sources=await reports.SourcesAsync(session,filter,p.Rows[0].Code);Assert(sources.Rows.Select(r=>(r.SourceType,r.Code)).Distinct().Count()==sources.Rows.Count,"Duplicate legacy source IDs");}
  values.Add(new{Kind=kind.ToString(),Rows=p.Rows.Count,p.Totals.Amount});
 }return values;
});
await Check("DA08/material-list","zero materials in g/ml;counter exact and source rows have0quantity;no unsupported detail action",async()=>{
 var materials=new MaterialService(new MaterialRepository(factory),permissions);foreach(var (code,unit) in new[]{("NVL98","g"),("NVL97","ml")}){
  var saved=await materials.SaveAsync(session,new(code,"Dashboard zero "+unit,unit),null,Guid.NewGuid());await materials.OpenAsync(session,new(code,0,"Isolated zero opening"),saved.Revision!,Guid.NewGuid());
 }
 var d=await reports.ReadDashboardAnalyticsAsync(session,false);var list=await reports.DashboardAnalyticsSourcesAsync(session,"material",null,null);Assert(d.Indicators["material.zero"]==list.TotalCount,"Counter/source mismatch");Assert(list.Rows.Where(r=>r.Code is "NVL97" or "NVL98").Count()==2&&list.Rows.All(r=>r.Quantity==0),"Zero material list invalid");
 Exception? failure=null;var disabled=false;var thread=new Thread(()=>{
  Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);using var main=new MainForm();
  var assets=new AssetService(new AssetRepository(factory),permissions);var payroll=new PayrollService(new PayrollRepository(factory),new EmployeeRepository(factory),permissions);var links=new DocumentLinks(new PurchaseService(new PurchaseRepository(factory),permissions),new FinanceService(new FinanceRepository(factory),permissions),new SupplierDebtService(new SupplierDebtRepository(factory),permissions),session,payroll,assets);
  var nav=new ReportSourceNavigation(session,links,new InvoiceService(new InvoiceRepository(factory),permissions),new StockIssueService(new StockIssueRepository(factory),permissions),new StocktakeService(new StocktakeRepository(factory),permissions),new AttendanceService(new AttendanceRepository(factory),permissions));
  IEnumerable<Control> Desc(Control parent){foreach(Control c in parent.Controls){yield return c;foreach(var child in Desc(c))yield return child;}}
  main.Shown+=async(_,_)=>{try{
   var type=typeof(ReportsControl).Assembly.GetType("CafeManager.WinForms.Modules.Reports.ReportSourceDialog")!;
   using var dialog=(OrbitDialogForm)Activator.CreateInstance(type,session,"Materials","Current",(Func<int,CancellationToken,Task<ReportSourcePage>>)((skip,token)=>reports.DashboardAnalyticsSourcesAsync(session,"material",null,null,skip:skip,token:token)),nav)!;
   using var timer=new System.Windows.Forms.Timer{Interval=50};var n=0;timer.Tick+=(_,_)=>{n++;var grid=Desc(dialog).OfType<DataGridView>().FirstOrDefault();if(grid?.Rows.Count>0){grid.CurrentCell=grid.Rows[0].Cells[0];var button=Desc(dialog).OfType<OrbitButton>().First(x=>x.Text=="Xem chi tiết");disabled=!button.Enabled;timer.Stop();dialog.Close();}else if(n>200){timer.Stop();dialog.Close();}};timer.Start();await OrbitDialogHost.ShowAsync(dialog,main);Assert(disabled,"Material detail action enabled or not loaded");
  }catch(Exception e){failure=e;}finally{main.Close();}};Application.Run(main);
 });thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();if(failure is not null)throw failure;return new{list.TotalCount,DetailDisabled=disabled};
});
await Check("DA14/CASH-STORE-login-startup-logout","real AuthService login;CASH=>SalesControl,STORE=>InventoryControl;logout revokes and no ADMIN dashboard",async()=>{
 var seen=new List<object>();foreach(var role in new[]{"CASH","STORE"}){
  var employee=await employees.SaveAsync(session,new("Startup "+role,null,"Full-time",1000,0,"CV001"),null,Guid.NewGuid());var username="startup."+Guid.NewGuid().ToString("N")[..10];var password=Guid.NewGuid().ToString("N");await accounts.WriteAsync(session,"Create",new(employee,null,username,role,true),password,password,Guid.NewGuid());
  using var roleSession=await new AuthService(auth,new PasswordService()).LoginAsync(new(username,password));Exception? error=null;var opened="";var logout=false;
  var thread=new Thread(()=>{
   Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
   var assets=new AssetService(new AssetRepository(factory),permissions);var payroll=new PayrollService(new PayrollRepository(factory),new EmployeeRepository(factory),permissions);var links=new DocumentLinks(new PurchaseService(new PurchaseRepository(factory),permissions),new FinanceService(new FinanceRepository(factory),permissions),new SupplierDebtService(new SupplierDebtRepository(factory),permissions),roleSession,payroll,assets);
   UserControl Inventory()=>new CafeManager.WinForms.Modules.Inventory.InventoryControl(new MaterialService(new MaterialRepository(factory),permissions),new PurchaseService(new PurchaseRepository(factory),permissions),new StockIssueService(new StockIssueRepository(factory),permissions),new StocktakeService(new StocktakeRepository(factory),permissions),roleSession,new NoPending(),links);
   var modules=new Dictionary<NavigationTarget,Func<UserControl>>{[NavigationTarget.Home]=()=>role=="STORE"?Inventory():new SessionHomeControl(roleSession.Identity),[NavigationTarget.Sales]=()=>new CafeManager.WinForms.Modules.Sales.SalesControl(new SalesService(new SalesRepository(factory),permissions),new TableService(new TableRepository(factory),permissions),new CustomerService(new CustomerRepository(factory),permissions),new LoyaltyService(new LoyaltyRepository(factory),permissions),roleSession,new InvoiceService(new InvoiceRepository(factory),permissions))};
   using var main=new MainForm(roleSession,permissions,t=>auth.ReadStateAsync(t),modules:modules);main.LoggedOut+=()=>logout=true;
   using var watchdog=new System.Windows.Forms.Timer{Interval=20000};watchdog.Tick+=(_,_)=>{error=new TimeoutException("Role startup timeout");main.Close();};
   main.Shown+=async(_,_)=>{watchdog.Start();try{
    var content=(Panel)typeof(MainForm).GetField("content",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(main)!;
    for(var i=0;i<200&&content.Controls.Count==0;i++)await Task.Delay(50);opened=content.Controls.Cast<Control>().FirstOrDefault()?.GetType().Name??"";
    Assert(opened==(role=="CASH"?"SalesControl":"InventoryControl"),"Wrong role startup "+opened);
    await Task.Delay(500);await (Task)typeof(MainForm).GetMethod("RequestLogoutAsync",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(main,null)!;
   }catch(Exception e){error=e;main.Close();}finally{watchdog.Stop();}};Application.Run(main);
  });thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();if(error is not null)throw error;Assert(logout&&roleSession.IsRevoked,"Logout did not revoke");seen.Add(new{role,opened,logout});
 }return seen;
});
Save();Environment.ExitCode=failures==0?0:1;
internal sealed class NoPending:IPendingOperationStore{
 public CafeManager.Models.Dtos.PendingOperation? Read(int actor,string operation)=>null;
 public void Save(CafeManager.Models.Dtos.PendingOperation operation)=>throw new InvalidOperationException("Read-only startup probe must not write pending operation");
 public void Clear(int actor,string operation){}
}
