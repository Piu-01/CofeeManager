using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Home;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;
if(args.Length!=3||!System.Text.RegularExpressions.Regex.IsMatch(args[0],@"^CoffeeManager_Test_G07_\d{8}_\d{6}_[a-f0-9]{8}_demo$"))throw new ArgumentException("Test DB required");
var b=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=args[0],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false};
await using var owner=new SqlConnection(b.ConnectionString);await owner.OpenAsync();await using(var c=owner.CreateCommand()){c.CommandText="SELECT RunId FROM dbo.G07TestMarker";if((string?)await c.ExecuteScalarAsync()!=args[1])throw new InvalidOperationException("Marker mismatch");c.CommandText="SELECT COUNT(*) FROM dbo.HOADON";if(Convert.ToInt32(await c.ExecuteScalarAsync())!=10000)throw new InvalidOperationException("Dataset not exactly10000");}
var factory=new ConnectionFactory(b.ConnectionString);typeof(ConnectionFactory).GetProperty("SessionInitializer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(factory,(Func<SqlConnection,CancellationToken,Task>)(async(c,t)=>{await using var cmd=c.CreateCommand();cmd.CommandText="EXECUTE AS USER='ExtendedRuntime'";await cmd.ExecuteNonQueryAsync(t);}));
var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);using var session=new SessionContext((await auth.ReadIdentityAsync(1))!);var evidence=new Dictionary<string,object?>();
var samples=new List<object>();var messages=new List<string>();owner.InfoMessage+=(_,e)=>messages.Add(e.Message);
foreach(var month in new[]{false,true}){
 var watch=Stopwatch.StartNew();await using var c=owner.CreateCommand();c.CommandText="SET STATISTICS IO ON;SET STATISTICS TIME ON;EXEC dbo.sp_DashboardAnalyticsRead @Actor=@a,@Stamp=@s,@Month=@m;SET STATISTICS IO OFF;SET STATISTICS TIME OFF;";c.Parameters.AddWithValue("@a",session.Identity.AccountId);c.Parameters.Add("@s",SqlDbType.Binary,8).Value=session.Identity.SecurityStamp;c.Parameters.AddWithValue("@m",month);
 var rows=0;await using(var reader=await c.ExecuteReaderAsync()){do{while(await reader.ReadAsync())rows++;}while(await reader.NextResultAsync());}samples.Add(new{Month=month,ElapsedMs=watch.Elapsed.TotalMilliseconds,ResultRows=rows});
}
evidence["SQL"]=samples;evidence["IO_TIME"]=messages;evidence["CachePolicy"]="First samples of this process; database already warm from dataset/probe. No DBCC/global cache reset.";
Exception? error=null;var ui=new List<object>();var thread=new Thread(()=>{
 Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);using var main=new MainForm();main.ClientSize=new(1366,768);using var dashboard=new AdminDashboardControl(new ReportService(new ReportRepository(factory),permissions),session);
 var content=(Panel)typeof(MainForm).GetField("content",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(main)!;content.Controls.Add(dashboard);
 bool Ready()=>(bool)typeof(AdminDashboardControl).GetField("ready",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(dashboard)!;
 async Task Wait(){for(var n=0;n<2000&&!Ready();n++)await Task.Delay(10);if(!Ready())throw new TimeoutException("UI did not load");}
 OrbitButton Button(string name)=>(OrbitButton)typeof(AdminDashboardControl).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(dashboard)!;
 var startup=Stopwatch.StartNew();main.Shown+=async(_,_)=>{try{
  await Wait();await Task.Yield();ui.Add(new{Mode="startup-day",ElapsedMs=startup.Elapsed.TotalMilliseconds});
  foreach(var mode in new[]{"monthly","today"}){var watch=Stopwatch.StartNew();Button(mode).PerformClick();await Wait();await Task.Yield();ui.Add(new{Mode=mode,ElapsedMs=watch.Elapsed.TotalMilliseconds});}
  evidence["Dpi"]=main.DeviceDpi;evidence["ClientSize"]=main.ClientSize;
 }catch(Exception e){error=e;}finally{main.Close();}};Application.Run(main);
});thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();if(error is not null)throw error;
evidence["UIReadyTiming"]=ui;evidence["UIBoundary"]="Until ready/binding and one UI yield; no visual acceptance or physical paint latency claim.";evidence["Status"]="Measured";
File.WriteAllText(args[2],JsonSerializer.Serialize(evidence,new JsonSerializerOptions{WriteIndented=true}));
