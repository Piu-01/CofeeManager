using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Catalogs;
using Microsoft.Data.SqlClient;

static class Probe
{
 static string Db="",Output="",Password="";static ApplicationContext Context=null!;static int Opens,Timeout=30;
 static readonly List<object> Results=[];
 static object? Value(object o,string n)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o);
 static T Field<T>(object o,string n)=>(T)Value(o,n)!;
 static void Set(object o,string n,object? v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(o,v);
 static MainForm? MainForm=>Value(Context,"main") as MainForm;
 static CatalogsControl Catalog=>(CatalogsControl)Field<Panel>(MainForm!,"content").Controls[0];
 static string Cs=>$"Server=localhost;Initial Catalog={Db};Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False";
 static void Need(bool b,string message){if(!b)throw new Exception(message);}
 static async Task Wait(Func<bool> predicate){var clock=Stopwatch.StartNew();while(!predicate()){if(clock.ElapsedMilliseconds>12000)throw new Exception("Wait timeout");await Task.Delay(40);}}
 static async Task Check(string id,string expected,Func<Task<string>> action){try{Results.Add(new{Id=id,Expected=expected,Actual=await action(),Status="PASS"});}catch(Exception e){Results.Add(new{Id=id,Expected=expected,Actual=e.Message,Status="FAIL"});}}
 static ConnectionFactory Factory()=>CreateFactory();
 static ConnectionFactory CreateFactory(){var factory=new ConnectionFactory(Cs,1);Func<SqlConnection,CancellationToken,Task> init=async(c,ct)=>{Interlocked.Increment(ref Opens);await using var cmd=c.CreateCommand();cmd.CommandText="EXECUTE AS USER=N'G02_Runtime'";await cmd.ExecuteNonQueryAsync(ct);};typeof(ConnectionFactory).GetProperty("SessionInitializer",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(factory,init);return factory;}
 static async Task<object?> Sql(string sql,params SqlParameter[] ps){await using var c=new SqlConnection(Cs);await c.OpenAsync();await using var cmd=c.CreateCommand();cmd.CommandText=sql;cmd.Parameters.AddRange(ps);return await cmd.ExecuteScalarAsync();}
 static async Task<SqlConnection> Lock(){var c=new SqlConnection(Cs);await c.OpenAsync();await using var cmd=c.CreateCommand();cmd.CommandText="BEGIN TRANSACTION;SELECT COUNT(*) FROM dbo.TAIKHOAN WITH(TABLOCKX,HOLDLOCK)";await cmd.ExecuteScalarAsync();return c;}
 static async Task Unlock(SqlConnection c){await using var cmd=c.CreateCommand();cmd.CommandText="IF @@TRANCOUNT>0 ROLLBACK";await cmd.ExecuteNonQueryAsync();}
 static void Handler(string name)=>typeof(CatalogsControl).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(Catalog,[Catalog,EventArgs.Empty]);
 [DllImport("user32.dll")]static extern bool PrintWindow(IntPtr h,IntPtr dc,uint flags);
 static void Capture(string name){var form=MainForm!;using var b=new Bitmap(form.Width,form.Height);using var g=Graphics.FromImage(b);var dc=g.GetHdc();try{Need(PrintWindow(form.Handle,dc,2),"Capture failed");}finally{g.ReleaseHdc(dc);}b.Save(Path.Combine(Output,name+".png"));}
 [STAThread]static int Main(string[] args)
 {
  Db=args[0];Output=args[1];Directory.CreateDirectory(Output);
  Need(System.Text.RegularExpressions.Regex.IsMatch(Db,@"^CoffeeManager_Test_G02_\d{8}_\d{6}_[a-f0-9]{8}$"),"Isolated name required");
  Need(Convert.ToInt32(Sql("SELECT COUNT(*) FROM dbo.G02TestMarker").GetAwaiter().GetResult())==1,"Marker required");
  Password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
  Sql("UPDATE dbo.TAIKHOAN SET MatKhauHash=@hash WHERE TenDangNhap='g02.admin'",new SqlParameter("@hash",new PasswordService().Hash(Password))).GetAwaiter().GetResult();
  Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  var auth=new AuthRepository(Factory());var permission=new PermissionService(auth);var service=new CatalogService(new CatalogRepository(Factory()),permission);
  var type=typeof(MainForm).Assembly.GetType("CafeManager.WinForms.ApplicationSessionContext")!;
  Context=(ApplicationContext)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,[new AuthService(auth,new PasswordService()),permission,auth,null,(Func<SessionContext,UserControl>)(s=>new CatalogsControl(service,s))],null)!;
  type.GetMethod("Start")!.Invoke(Context,null);
  var timer=new System.Windows.Forms.Timer{Interval=100};timer.Tick+=async(_,_)=>{timer.Stop();try{await Cases();}catch(Exception e){Results.Add(new{Id="HARNESS",Status="BLOCKED",Actual=e.Message});}finally{File.WriteAllText(Path.Combine(Output,"results.json"),JsonSerializer.Serialize(new{Database=Db,Process=Environment.ProcessId,Principal="G02_Runtime impersonation; Pooling=false; native product forms",Results},new JsonSerializerOptions{WriteIndented=true}));Context.ExitThread();}};
  timer.Start();Application.Run(Context);return Results.Any(x=>JsonSerializer.Serialize(x).Contains("\"FAIL\""))?1:0;
 }
 static async Task Cases()
 {
  var login=Field<LoginForm>(Context,"login");Field<TextBox>(login,"usernameInput").Text="g02.admin";Field<TextBox>(login,"passwordInput").Text=Password;Field<Button>(login,"loginButton").PerformClick();
  await Wait(()=>MainForm is {Visible:true}&&!Field<bool>(MainForm,"navigating")&&Field<Panel>(MainForm,"content").Controls.Count==1);
  await Wait(()=>Value(Catalog,"loading")==null);
  await Check("B01.slowRead.10clickAndDirectHandlers","One pending read; Tìm/Tải disabled; handlers preserve page/filter",async()=>{
   await using var block=await Lock();var before=Opens;var skip=Field<int>(Catalog,"skip");var filter=Field<TextBox>(Catalog,"searchInput").Text;
   try{
    Field<Button>(Catalog,"searchButton").PerformClick();await Wait(()=>Value(Catalog,"loading")!=null);await Task.Delay(150);
    Need(!Field<Button>(Catalog,"searchButton").Enabled&&!Field<Button>(Catalog,"reloadButton").Enabled,"Read buttons enabled while loading");
    for(int i=0;i<10;i++){Field<Button>(Catalog,"searchButton").PerformClick();Field<Button>(Catalog,"reloadButton").PerformClick();Handler("Search_Click");Handler("Previous_Click");Handler("Next_Click");Handler("Reload_Click");}
    await Task.Delay(100);Need(Opens-before==1,$"Repeated read opens={Opens-before}, expected1 pending recheck");
    Need(Field<int>(Catalog,"skip")==skip&&Field<TextBox>(Catalog,"searchInput").Text==filter,"Repeated handler changed skip/filter");Capture("loading-buttons-disabled");
   }finally{await Unlock(block);}
   await Wait(()=>Value(Catalog,"loading")==null);Need(Field<Button>(Catalog,"searchButton").Enabled&&Field<Button>(Catalog,"reloadButton").Enabled,"Buttons not restored");
   Need(Opens-before==2,$"Completed normal read opens={Opens-before}, expected recheck+list2");return "10 user repeats +40 direct handler calls suppressed; pending recheck1, completed recheck/list2; input/page retained, buttons restored";
  });
  await Check("B01.cancelRead.retry","Cancel unlocks actions; reload succeeds",async()=>{
   await using var block=await Lock();try{Field<Button>(Catalog,"searchButton").PerformClick();await Wait(()=>Value(Catalog,"loading")!=null);Field<CancellationTokenSource>(Catalog,"loading").Cancel();await Wait(()=>Value(Catalog,"loading")==null);Need(Field<Button>(Catalog,"searchButton").Enabled&&Field<Button>(Catalog,"reloadButton").Enabled,"Cancel left disabled");}finally{await Unlock(block);}
   Field<Button>(Catalog,"reloadButton").PerformClick();await Wait(()=>Value(Catalog,"loading")==null);return "Canceled linked read while SQL locked, buttons enabled, retry completed";
  });
  await Check("B01.readTimeout.retry","SQL read error restores actions, preserves input, reload succeeds",async()=>{
   var input=Field<TextBox>(Catalog,"searchInput");input.Text="Giữ input khi lỗi";await using var block=await Lock();
   try{Field<Button>(Catalog,"searchButton").PerformClick();await Wait(()=>Value(Catalog,"loading")==null);Need(input.Text=="Giữ input khi lỗi","Input lost");Need(Field<Button>(Catalog,"searchButton").Enabled&&Field<Button>(Catalog,"reloadButton").Enabled,"Error left disabled");Need(Field<Label>(Catalog,"statusLabel").Text.Contains("lỗi",StringComparison.OrdinalIgnoreCase)||Field<Label>(Catalog,"statusLabel").Text.Contains("thời gian",StringComparison.OrdinalIgnoreCase),"No error status");Capture("read-error-restored");}finally{await Unlock(block);}
   Field<Button>(Catalog,"reloadButton").PerformClick();await Wait(()=>Value(Catalog,"loading")==null);Need(input.Text=="Giữ input khi lỗi","Retry lost filter");return "SQL timeout with command timeout1s; error status, input retained, buttons enabled, retry completed";
  });
  await Check("B01.unknownDelete.reconcile","Tải remains enabled for unknown-write state; reconciliation refreshes",async()=>{
   Field<TextBox>(Catalog,"searchInput").Clear();Field<Button>(Catalog,"reloadButton").PerformClick();await Wait(()=>Value(Catalog,"loading")==null);
   var session=Field<SessionContext>(Context,"session");var service=new CatalogService(new CatalogRepository(Factory()),new PermissionService(new AuthRepository(Factory())));
   var row=(await service.ListAsync(session,CatalogKind.ProductCategory,"",0)).First();Set(Catalog,"unknownDelete",row);
   typeof(CatalogsControl).GetMethod("UpdateActions",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(Catalog,null);
   Need(!Field<Button>(Catalog,"searchButton").Enabled&&Field<Button>(Catalog,"reloadButton").Enabled,"Reconciliation unavailable or search allowed");
   Field<Button>(Catalog,"reloadButton").PerformClick();await Wait(()=>Value(Catalog,"unknownDelete")==null&&Value(Catalog,"loading")==null&&!Field<bool>(Catalog,"writing"));
   Need(Field<Button>(Catalog,"searchButton").Enabled&&Field<Button>(Catalog,"reloadButton").Enabled,"Reconcile left disabled");return "Harness injected unknownDelete from existing DB row; native Tải performed Get/refresh, cleared unknown state; not a network-loss simulation";
  });
  await Check("B01.logout.pendingRead","Logout disposes old view; late SQL unlock cannot reopen it",async()=>{
   await using var block=await Lock();var old=MainForm!;var catalog=Catalog;
   try{Field<Button>(catalog,"searchButton").PerformClick();await Wait(()=>Value(catalog,"loading")!=null);Field<Button>(old,"logoutButton").PerformClick();await Wait(()=>MainForm==null);Need(old.IsDisposed&&catalog.IsDisposed,"Old UI survived");}finally{await Unlock(block);}
   await Task.Delay(300);Need(MainForm==null&&Field<LoginForm>(Context,"login").Visible,"Late result reopened old UI");return "Logout canceled pending read and disposed Main/control; unlock left only Login visible";
  });
 }
}
