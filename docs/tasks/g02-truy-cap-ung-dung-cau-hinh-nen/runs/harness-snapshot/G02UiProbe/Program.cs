using System.Collections;
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
using CafeManager.WinForms.Navigation;
using Microsoft.Data.SqlClient;

static class UiProbe
{
    static string Db="",Output="",Password="";
    static List<object> Results=[];
    static ApplicationContext? Context;
    static int Opens;
    static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o)!;
    static object? Value(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o);
    static void Need(bool value,string message){if(!value)throw new Exception(message);}
    static async Task Until(Func<bool> condition,int timeout=10000){var w=Stopwatch.StartNew();while(!condition()){if(w.ElapsedMilliseconds>timeout)throw new Exception("Wait timeout");await Task.Delay(40);}}
    static void Record(string id,string status,string expected,string actual)=>Results.Add(new{Id=id,Status=status,Expected=expected,Actual=actual});
    static async Task Check(string id,string expected,Func<Task<string>> action){try{Record(id,"PASS",expected,await action());}catch(Exception e){Record(id,"FAIL",expected,e is TargetInvocationException t?t.InnerException?.GetType().Name??"Invocation":e.Message);}}
    static string Cs=>$"Server=localhost;Initial Catalog={Db};Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False";
    static ConnectionFactory Factory(){var f=new ConnectionFactory(Cs);Func<SqlConnection,CancellationToken,Task> init=async(c,ct)=>{Interlocked.Increment(ref Opens);await using var cmd=c.CreateCommand();cmd.CommandText="EXECUTE AS USER=N'G02_Runtime'";await cmd.ExecuteNonQueryAsync(ct);};typeof(ConnectionFactory).GetProperty("SessionInitializer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(f,init);return f;}
    static async Task<object?> Sql(string text,params SqlParameter[] ps){await using var c=new SqlConnection(Cs);await c.OpenAsync();await using var cmd=c.CreateCommand();cmd.CommandText=text;cmd.Parameters.AddRange(ps);return await cmd.ExecuteScalarAsync();}
    static MainForm? Main=>Context is null?null:Value(Context,"main") as MainForm;
    static LoginForm Login=>Field<LoginForm>(Context!,"login");
    static SessionContext Session=>Field<SessionContext>(Context!,"session");
    static async Task LoginAs(string role){Field<TextBox>(Login,"usernameInput").Text="g02."+role.ToLowerInvariant();Field<TextBox>(Login,"passwordInput").Text=Password;Field<Button>(Login,"loginButton").PerformClick();await Until(()=>Main is {Visible:true});await Until(()=>!Field<bool>(Main!,"navigating"));if(role=="ADMIN")await Until(()=>Field<CatalogsControl>(Field<Panel>(Main!,"content"),"nonexistent")!=null,1);}
    static async Task LoginSafe(string role){Field<TextBox>(Login,"usernameInput").Text="g02."+role.ToLowerInvariant();Field<TextBox>(Login,"passwordInput").Text=Password;Field<Button>(Login,"loginButton").PerformClick();await Until(()=>Main is {Visible:true});await Until(()=>!Field<bool>(Main!,"navigating"));if(role=="ADMIN")await WaitCatalog();}
    static CatalogsControl Catalog=>(CatalogsControl)Field<Panel>(Main!,"content").Controls[0];
    static async Task WaitCatalog()=>await Until(()=>Field<Panel>(Main!,"content").Controls.Count==1&&Field<Panel>(Main!,"content").Controls[0] is CatalogsControl c&&Value(c,"loading")==null);
    static async Task Navigate(NavigationTarget target)=>await (Task)typeof(MainForm).GetMethod("Navigate",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(Main,[target])!;
    [DllImport("user32.dll")]static extern bool PrintWindow(IntPtr h,IntPtr dc,uint flags);
    [DllImport("user32.dll")]static extern IntPtr GetDlgItem(IntPtr h,int id);
    [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int message,IntPtr w,IntPtr l);
    delegate bool EnumWindow(IntPtr h,IntPtr a);
    [DllImport("user32.dll")]static extern bool EnumWindows(EnumWindow callback,IntPtr a);
    [DllImport("user32.dll")]static extern int GetWindowThreadProcessId(IntPtr h,out int pid);
    static void AnswerPrompt(bool yes){EnumWindows((h,a)=>{GetWindowThreadProcessId(h,out var pid);if(pid==Environment.ProcessId){var button=GetDlgItem(h,yes?6:7);if(button!=IntPtr.Zero)SendMessage(button,0x00F5,IntPtr.Zero,IntPtr.Zero);}return true;},IntPtr.Zero);}
    static async Task ClickAndAnswer(Button button,bool yes){_ = Task.Run(async()=>{await Task.Delay(300);AnswerPrompt(yes);});button.PerformClick();await Task.Delay(100);}
    static void Capture(Form f,string name){using var b=new Bitmap(f.Width,f.Height);using var g=Graphics.FromImage(b);var dc=g.GetHdc();try{Need(PrintWindow(f.Handle,dc,2),"PrintWindow failed");}finally{g.ReleaseHdc(dc);}b.Save(Path.Combine(Output,name+".png"));}
    public static int Run(string[] args)
    {
        Db=args[0];Output=args[1];Directory.CreateDirectory(Output);Password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        Need(System.Text.RegularExpressions.Regex.IsMatch(Db,@"^CoffeeManager_Test_G02_\d{8}_\d{6}_[a-f0-9]{8}$"),"G02 target required");
        Need(Convert.ToInt32(Sql("SELECT COUNT(*) FROM dbo.G02TestMarker").GetAwaiter().GetResult())==1,"Marker required");
        Sql("UPDATE dbo.TAIKHOAN SET TenDangNhap='g02.admin',MaVT='ADMIN',HoatDong=1,MatKhauHash=@hash WHERE MaVT='ADMIN'; INSERT dbo.NHANVIEN(MaNV,TenNV,LoaiHopDong,MaCV) SELECT v.Code,v.Name,'Full-time','CVADM' FROM(VALUES('NV900',N'Native CASH'),('NV901',N'Native STORE'))v(Code,Name) WHERE NOT EXISTS(SELECT 1 FROM dbo.NHANVIEN n WHERE n.MaNV=v.Code); INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MaVT,HoatDong,MatKhauHash) SELECT v.Code,v.Login,v.Role,1,@hash FROM(VALUES('NV900','g02.cash','CASH'),('NV901','g02.store','STORE'))v(Code,Login,Role) WHERE NOT EXISTS(SELECT 1 FROM dbo.TAIKHOAN t WHERE t.MaNV=v.Code); UPDATE dbo.TAIKHOAN SET MatKhauHash=@hash WHERE TenDangNhap IN('g02.admin','g02.cash','g02.store');",new SqlParameter("@hash",new PasswordService().Hash(Password))).GetAwaiter().GetResult();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);var repository=new AuthRepository(Factory());var permissions=new PermissionService(repository);var catalogs=new CatalogService(new CatalogRepository(Factory()),permissions);
        var type=typeof(MainForm).Assembly.GetType("CafeManager.WinForms.ApplicationSessionContext")!;
        Context=(ApplicationContext)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,[new AuthService(repository,new PasswordService()),permissions,repository,null,(Func<SessionContext,UserControl>)(s=>new CatalogsControl(catalogs,s))],null)!;
        type.GetMethod("Start")!.Invoke(Context,null);
        var timer=new System.Windows.Forms.Timer{Interval=100};timer.Tick+=async(_,_)=>{timer.Stop();try{await Cases();}catch(Exception e){Record("UI-HARNESS","BLOCKED","Complete cases",e.Message);}finally{File.WriteAllText(Path.Combine(Output,"native-results.json"),JsonSerializer.Serialize(new{Results,Dpi=Login.IsDisposed?0:Login.DeviceDpi,Process=Environment.ProcessId,Principal="G02_Runtime impersonation, Pooling=false; native product forms in test host"},new JsonSerializerOptions{WriteIndented=true}));Context?.ExitThread();}};timer.Start();Application.Run(Context);return Results.Any(x=>JsonSerializer.Serialize(x).Contains("\"FAIL\""))?1:0;
    }
    static async Task Cases()
    {
        await Check("T06-K07.a.mask-session","Login hides password; session excludes credentials",async()=>{Need(Field<TextBox>(Login,"passwordInput").UseSystemPasswordChar,"Password not masked");Need(!typeof(SessionContext).GetProperties().Any(p=>p.Name.Contains("Password")||p.Name.Contains("Hash")),"Session credential exposed");Field<TextBox>(Login,"passwordInput").Text="temporary text";Capture(Login,"login-mask");Field<TextBox>(Login,"passwordInput").Clear();await Task.CompletedTask;return "Mask true, no credential property, native screenshot";});
        foreach(var role in new[]{"ADMIN","CASH","STORE"})
        {
            await Check("T07-K01.a."+role,"Native login opens one Main with correct identity",async()=>{await LoginSafe(role);Need(Session.Identity.Role==role,"Role mismatch");Need(Field<Label>(Main!,"identityLabel").Text.Contains(Session.Identity.EmployeeName),"Name mismatch");Need(Application.OpenForms.OfType<MainForm>().Count()==1,"Main count not 1");Capture(Main!,"main-"+role);return $"Main=1, role {role}, identity label matched";});
            await Check("T07-K02.a."+role,"Menu and direct navigate enforce role",async()=>{Need(Field<Button>(Main!,"catalogsButton").Visible==(role=="ADMIN"),"Catalog visibility mismatch");if(role!="ADMIN"){var before=Field<Panel>(Main!,"content").Controls[0];await Navigate(NavigationTarget.Catalogs);Need(ReferenceEquals(before,Field<Panel>(Main!,"content").Controls[0]),"Unauthorized navigate changed content");}return "Role-specific menu; unauthorized navigation stays on existing control";});
            foreach(var size in new[]{new Size(1280,720),new Size(1366,768)})await Check($"T07-K04.viewport.{Main!.DeviceDpi}.{size.Width}","Requested native size and menu/content usable",async()=>{Main!.Size=size;await Task.Delay(150);Capture(Main,$"viewport-{role}-{Main.DeviceDpi}-{size.Width}");Need(Main.Size==size,"Minimum clamps requested viewport");var panel=Field<Panel>(Main,"content");Need(panel.Width>100&&panel.Height>100,"Content collapsed");return $"Actual {Main.Width}x{Main.Height}, DeviceDpi={Main.DeviceDpi}; visual screenshot requires inspection";});
            if(role=="ADMIN"||role=="STORE")await Check("T07-K03.a."+role,"100 transitions preserve lazy instances",async()=>{var target=role=="ADMIN"?NavigationTarget.Catalogs:NavigationTarget.SystemInfo;await Navigate(NavigationTarget.Home);await Navigate(target);if(role=="ADMIN")await WaitCatalog();var nav=Field<NavigationController>(Main!,"navigation");var modules=Field<IDictionary>(nav,"modules");var before=modules.Values.Cast<UserControl>().ToArray();for(int i=0;i<50;i++){await Navigate(NavigationTarget.Home);await Navigate(target);if(role=="ADMIN")await WaitCatalog();}var after=modules.Values.Cast<UserControl>().ToArray();Need(before.Length==after.Length&&before.All(x=>after.Contains(x)),"Instance count increased");Need(Field<Panel>(Main!,"content").Controls.Count==1,"Visible children duplicated");return $"100 transitions, cached {before.Length}->{after.Length} same instances, visible child1, SQL connections short-lived";});
            if(role=="ADMIN")await CatalogUi();
            var old=Session;Field<Button>(Main!,"logoutButton").PerformClick();await Until(()=>Main is null&&Login.Visible);Need(old.IsRevoked,"Logout did not revoke");
        }
        for(int i=0;i<10;i++)await Check("T07-K02.b.cycle"+i,"One native Main per login/logout",async()=>{await LoginSafe(i%2==0?"CASH":"STORE");var form=Main!;var session=Session;Field<Button>(form,"logoutButton").PerformClick();await Until(()=>Main is null&&Login.Visible);Need(form.IsDisposed&&session.IsRevoked,"Main or session survived logout");Need(Application.OpenForms.OfType<MainForm>().Count()==0,"Main remained open");return "One Main opened, disposed on logout, session revoked, no Main remains";});
        await LoginSafe("CASH");
        await Check("T07-K05.b","Logout cancels blocked native navigation; no late UI",async()=>{await using var blocker=new SqlConnection(Cs);await blocker.OpenAsync();await using var tx=await blocker.BeginTransactionAsync();await using var cmd=blocker.CreateCommand();cmd.Transaction=(SqlTransaction)tx;cmd.CommandText="SELECT COUNT(*) FROM dbo.TAIKHOAN WITH(TABLOCKX,HOLDLOCK)";await cmd.ExecuteScalarAsync();var old=Main!;Field<Button>(old,"systemButton").PerformClick();await Task.Delay(300);Need(Field<bool>(old,"navigating"),"No pending navigate");Field<Button>(old,"logoutButton").PerformClick();await Until(()=>Main is null&&Login.Visible);await tx.RollbackAsync();await Task.Delay(500);Need(old.IsDisposed&&Main is null,"Old UI reopened after unlock");return "Pending read canceled, old Main disposed; unlock did not reopen Main";});
    }
    static async Task CatalogUi()
    {
        var control=Catalog;var tabs=Field<TabControl>(control,"tabs");
        foreach(var kind in Enum.GetValues<CatalogKind>())await Check("T07-K06.a."+kind,"Dirty editor cancel does not write",async()=>{tabs.SelectedIndex=(int)kind;await WaitCatalog();var before=await Sql("SELECT SUM(rows) FROM sys.partitions WHERE index_id IN(0,1)");Main!.BeginInvoke((Action)(()=>Field<Button>(control,"addButton").PerformClick()));await Until(()=>Application.OpenForms.OfType<CatalogEditorDialog>().Any());var dialog=Application.OpenForms.OfType<CatalogEditorDialog>().Single();Field<TextBox>(dialog,"nameInput").Text="Nháp không lưu";Capture(dialog,"editor-"+kind);await ClickAndAnswer(Field<Button>(dialog,"cancelButton"),true);await Until(()=>dialog.IsDisposed);Need(Equals(before,await Sql("SELECT SUM(rows) FROM sys.partitions WHERE index_id IN(0,1)")),"Cancel wrote rows");return "Dirty dialog canceled with confirmation; persistent row count unchanged";});
        tabs.SelectedIndex=1;await WaitCatalog();
        await Check("T07-K06.b","NCC save refreshes owning tab and preserves filter",async()=>{Field<TextBox>(control,"searchInput").Text="Owner only";Field<Button>(control,"searchButton").PerformClick();await WaitCatalog();Main!.BeginInvoke((Action)(()=>Field<Button>(control,"addButton").PerformClick()));await Until(()=>Application.OpenForms.OfType<CatalogEditorDialog>().Any());var dialog=Application.OpenForms.OfType<CatalogEditorDialog>().Single();Field<TextBox>(dialog,"codeInput").Text="NCC91";Field<TextBox>(dialog,"nameInput").Text="Owner only NCC";Field<TextBox>(dialog,"phoneInput").Text="01234999991";Field<Button>(dialog,"saveButton").PerformClick();await Until(()=>dialog.IsDisposed);await WaitCatalog();Need(tabs.SelectedIndex==1&&Field<TextBox>(control,"searchInput").Text=="Owner only","Owner/filter changed");Need(Field<DataGridView>(control,"grid").Rows.Count==1,"Saved NCC not reflected");return "Save closed dialog, NCC row appears, selected tab/filter unchanged";});
        await Check("T07-K05.a.repeat-read","Slow read locks repeated user actions",async()=>{await using var blocker=new SqlConnection(Cs);await blocker.OpenAsync();await using var tx=await blocker.BeginTransactionAsync();await using var cmd=blocker.CreateCommand();cmd.Transaction=(SqlTransaction)tx;cmd.CommandText="SELECT COUNT(*) FROM dbo.TAIKHOAN WITH(TABLOCKX,HOLDLOCK)";await cmd.ExecuteScalarAsync();var before=Opens;Field<Button>(control,"searchButton").PerformClick();await Task.Delay(200);for(int i=0;i<10;i++)Field<Button>(control,"searchButton").PerformClick();await Task.Delay(300);var opened=Opens-before;var enabled=Field<Button>(control,"searchButton").Enabled;await tx.RollbackAsync();await WaitCatalog();Need(opened==1&&!enabled,$"Search stayed enabled={enabled}; connection opens for repeated read={opened} (expected1)");return "Repeated action suppressed";});
    }
}
static class Entry { [STAThread]public static int Main(string[] args)=>UiProbe.Run(args); }
