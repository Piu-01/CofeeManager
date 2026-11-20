using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Home;
using Microsoft.Data.SqlClient;
if(args.Length!=3||!System.Text.RegularExpressions.Regex.IsMatch(args[0],@"^CoffeeManager_Test_G07_\d{8}_\d{6}_[a-f0-9]{8}_demo$"))throw new ArgumentException("Test target required");
var b=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=args[0],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false};await using(var marker=new SqlConnection(b.ConnectionString)){await marker.OpenAsync();await using var c=marker.CreateCommand();c.CommandText="SELECT RunId FROM dbo.G07TestMarker";if((string?)await c.ExecuteScalarAsync()!=args[1])throw new InvalidOperationException("Marker mismatch");}
var factory=new ConnectionFactory(b.ConnectionString);var auth=new AuthRepository(factory);using var session=new SessionContext((await auth.ReadIdentityAsync(1))!);var reports=new ReportService(new ReportRepository(factory),new PermissionService(auth));
var samples=new List<object>();Exception? failure=null;var thread=new Thread(()=>{
 Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);using var main=new MainForm();main.ClientSize=new(1366,768);using var dashboard=new AdminDashboardControl(reports,session);
 var content=(Panel)typeof(MainForm).GetField("content",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(main)!;content.Controls.Add(dashboard);
 FieldInfo Field(string name)=>typeof(AdminDashboardControl).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!;
 IEnumerable<Control> Desc(Control parent){yield return parent;foreach(Control child in parent.Controls)foreach(var item in Desc(child))yield return item;}
 main.Shown+=async(_,_)=>{try{
  for(var i=0;i<2000&&!(bool)Field("ready").GetValue(dashboard)!;i++)await Task.Delay(10);if(!(bool)Field("ready").GetValue(dashboard)!)throw new TimeoutException("Dashboard not ready");
  var saved=new[]{"pages","indicators","period"}.ToDictionary(k=>k,k=>Field(k).GetValue(dashboard));
  foreach(var method in new[]{"Render","ClearSnapshot","Render"}){
   if(method=="Render"){foreach(var item in saved)Field(item.Key).SetValue(dashboard,item.Value);}
   var layouts=0;var counters=new Dictionary<string,int>();var handlers=new List<(Control Control,LayoutEventHandler Handler)>();var controls=Desc(dashboard).ToArray();
   foreach(var control in controls){LayoutEventHandler handler=(_,_)=>{layouts++;var key=control.GetType().Name;counters[key]=counters.GetValueOrDefault(key)+1;};control.Layout+=handler;handlers.Add((control,handler));}
   var watch=Stopwatch.StartNew();typeof(AdminDashboardControl).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(dashboard,null);watch.Stop();
   foreach(var pair in handlers)if(!pair.Control.IsDisposed)pair.Control.Layout-=pair.Handler;
   samples.Add(new{Method=method,ElapsedMs=watch.Elapsed.TotalMilliseconds,Layouts=layouts,ControlsBefore=controls.Length,ByType=counters});
  }
 }catch(Exception e){failure=e;}finally{main.Close();}};Application.Run(main);
});thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();File.WriteAllText(args[2],JsonSerializer.Serialize(new{Samples=samples,Error=failure?.ToString(),Note="Instrumentation only; no layout suspension/fix or production source modifications."},new JsonSerializerOptions{WriteIndented=true}));if(failure is not null)Environment.ExitCode=1;
