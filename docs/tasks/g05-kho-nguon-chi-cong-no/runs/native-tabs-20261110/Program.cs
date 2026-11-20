using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.HumanResources;
using CafeManager.WinForms.Modules.Inventory;
using CafeManager.WinForms.Navigation;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

internal static partial class Program
{
    static string output="",mode="",database="",runId="",stage="idle";
    static readonly List<object> checks=[],metrics=[],constructors=[];
    static readonly ConcurrentQueue<(string Stage,string Procedure)> queries=new();
    static int failed,opens,layouts; static double maxGap; static long lastTick;
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr handle,uint msg,IntPtr w,IntPtr l);
    [STAThread] static int Main(string[] args)
    {
        output=Path.GetFullPath(args[0]);mode=args[1];
        using var fixture=JsonDocument.Parse(File.ReadAllText(Path.Combine(output,"fixture.json")));
        database=fixture.RootElement.GetProperty("Database").GetString()!;runId=fixture.RootElement.GetProperty("RunId").GetString()!;
        if(database!="CoffeeManager_Test_UiTabs_"+runId||!System.Text.RegularExpressions.Regex.IsMatch(runId,@"^20261110_[a-f0-9]{8}$"))throw new InvalidOperationException("Refuse non-fixture DB.");
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);Application.SetCompatibleTextRenderingDefault(false);
        using var trace=new Trace();using var host=new Form{Opacity=0,ShowInTaskbar=false};
        using var watchdog=new System.Windows.Forms.Timer{Interval=180000};watchdog.Tick+=(_,_)=>{Check("Watchdog",false);Save();Environment.Exit(2);};
        using var heartbeat=new System.Windows.Forms.Timer{Interval=15};heartbeat.Tick+=(_,_)=>{var now=Stopwatch.GetTimestamp();if(lastTick!=0)maxGap=Math.Max(maxGap,Stopwatch.GetElapsedTime(lastTick,now).TotalMilliseconds);lastTick=now;};
        host.Shown+=async(_,_)=>{watchdog.Start();heartbeat.Start();try{if(mode=="supplement")await Supplement();else await Run();}catch(Exception e){Check("Runner exception",false,e.ToString());}finally{watchdog.Stop();heartbeat.Stop();Save();host.Close();}};
        Application.Run(host);Console.WriteLine($"{mode}: {checks.Count-failed}/{checks.Count} checks; failures={failed}; stages={metrics.Count}");return mode=="before"?0:failed==0?0:1;
    }
    static async Task Run()
    {
        var cs=$"Server=localhost;Initial Catalog={database};Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False;Connect Timeout=5";
        using var owner=new SqlConnection(cs);await owner.OpenAsync();
        using(var marker=owner.CreateCommand()){marker.CommandText="SELECT RunId FROM dbo.UiTabsTestMarker";if((string?)await marker.ExecuteScalarAsync()!=runId)throw new InvalidOperationException("Marker mismatch.");}
        using(var setup=owner.CreateCommand())
        {
            setup.CommandText="IF NOT EXISTS(SELECT 1 FROM dbo.AppSetupState WHERE State='Completed') BEGIN DECLARE @s binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=1); EXEC dbo.sp_SetupBootstrap @Mode='ActivateDemo',@Username=N'admin',@NewHash=@hash,@MaNV='NV001',@ExpectedStamp=@s; UPDATE dbo.TAIKHOAN SET HoatDong=1,MatKhauHash=@hash WHERE MaTK IN(2,3); END;";
            setup.Parameters.AddWithValue("@hash",new PasswordService().Hash(Guid.NewGuid().ToString("N")));await setup.ExecuteNonQueryAsync();
        }
        var hashBefore=await DataHash(owner);
        var factory=new ConnectionFactory(cs);
        typeof(ConnectionFactory).GetProperty("SessionInitializer",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(factory,(Func<SqlConnection,CancellationToken,Task>)(async(connection,token)=>{Interlocked.Increment(ref opens);using var init=connection.CreateCommand();init.CommandText="EXECUTE AS USER='UiTabsRuntime';";await init.ExecuteNonQueryAsync(token);}));
        var auth=new AuthRepository(factory);var permission=new PermissionService(auth);
        foreach(var role in new[]{"ADMIN","STORE","CASH"})
        {
            using var session=new SessionContext(await auth.ReadIdentityAsync(role=="ADMIN"?1:role=="CASH"?2:3)??throw new InvalidOperationException("Missing identity."));
            var materials=new MaterialService(new MaterialRepository(factory),permission);var purchases=new PurchaseService(new PurchaseRepository(factory),permission);
            var issues=new StockIssueService(new StockIssueRepository(factory),permission);var stocktakes=new StocktakeService(new StocktakeRepository(factory),permission);
            var employees=new EmployeeService(new EmployeeRepository(factory),permission);var accounts=new EmployeeAccountService(new EmployeeAccountRepository(factory),permission,new PasswordService());
            var attendance=new AttendanceService(new AttendanceRepository(factory),permission);var payroll=new PayrollService(new PayrollRepository(factory),new EmployeeRepository(factory),permission);
            var links=new DocumentLinks(purchases,new FinanceService(new FinanceRepository(factory),permission),new SupplierDebtService(new SupplierDebtRepository(factory),permission),session,payroll);
            var modules=new Dictionary<NavigationTarget,Func<UserControl>>
            {
                [NavigationTarget.Inventory]=()=>Create("Inventory",()=>new InventoryControl(materials,purchases,issues,stocktakes,session,new MemoryPending(),links)),
                [NavigationTarget.HumanResources]=()=>Create("HumanResources",()=>new HumanResourcesControl(employees,accounts,session,attendance,payroll,links))
            };
            using var form=new MainForm(session,permission,auth.ReadStateAsync,null,modules);
            try
            {
                form.Show();await Wait(()=>Field<Label>(form,"statusLabel").Text=="Phiên đang hoạt động.");Check(role+" DPI125",form.DeviceDpi==120,new{form.DeviceDpi,Screen=Screen.FromControl(form).Bounds});
                if(form.DeviceDpi!=120)throw new InvalidOperationException("Actual DPI not125%; no scale change.");
                if(role=="CASH")
                {
                    await form.OpenWorkspaceAsync("Inventory");Check("CASH no inventory",Current(form) is not InventoryControl);
                    await form.OpenWorkspaceAsync("HumanResources");Check("CASH no HR",Current(form) is not HumanResourcesControl);continue;
                }
                foreach(var target in role=="ADMIN"?new[]{NavigationTarget.Inventory,NavigationTarget.HumanResources}:new[]{NavigationTarget.Inventory})
                {
                    await Measure(role+"/"+target+"/first",async()=>{await form.OpenWorkspaceAsync(target.ToString());await Ready(Current(form)!);});
                    var module=Current(form)!;Track(module);var tabs=module.Controls.OfType<TabControl>().Single();
                    Check(role+" "+target+" tab count",tabs.TabCount==4,tabs.TabPages.Cast<TabPage>().Select(p=>p.Text).ToArray());
                    Check(role+" "+target+" dark tab chrome",tabs is OrbitTabControl{FlatManagementTabs:true});
                    if(mode=="after")Check(role+" "+target+" initial only selected page constructed",tabs.TabPages.Cast<TabPage>().Count(p=>p.Controls.Count>0)==1);
                    metrics.Add(new{Name=role+"/"+target+"/initial tree",Controls=Desc(module).Count(),Lists=Desc(module).Count(c=>c is MaterialsControl or OrbitRecordListControl or PurchasesControl or StockIssuesControl or StocktakesControl),CreatedPages=tabs.TabPages.Cast<TabPage>().Count(p=>p.Controls.Count>0)});
                    var pages=new Dictionary<int,Control>();
                    for(int i=0;i<4;i++)
                    {
                        var index=i;await Measure(role+"/"+target+"/tab"+i,async()=>{tabs.SelectedIndex=index;await Ready(module);});
                        pages[i]=tabs.SelectedTab!.Controls.Cast<Control>().Single();
                        Check(role+" "+target+" ready tab"+i,Desc(pages[i]).OfType<DataGridView>().Any(g=>g.DataSource is not null));
                    }
                    for(int round=0;round<2;round++)for(int i=0;i<4;i++)
                    {
                        var index=i;await Measure(role+"/"+target+"/repeat"+round+"/"+i,async()=>{tabs.SelectedIndex=index;await Ready(module);});
                        Check(role+" "+target+" cached tab"+i+" round"+round,ReferenceEquals(pages[i],tabs.SelectedTab!.Controls[0]));
                    }
                    foreach(var size in new[]{new Size(1280,720),new Size(1366,768),new Size(800,640)})
                    {
                        form.ClientSize=size;await Task.Delay(50);
                        for(int i=0;i<4;i++)
                        {
                            tabs.SelectedIndex=i;await Ready(module);var page=tabs.SelectedTab!;var body=page.Controls[0];var grid=Desc(body).OfType<DataGridView>().First();
                            Check(role+" "+target+" fill "+size+" tab"+i,body.Dock==DockStyle.Fill&&body.Width>=page.ClientSize.Width-page.Padding.Horizontal-1&&body.Height>=page.ClientSize.Height-page.Padding.Vertical-1,new{Page=page.ClientRectangle,Padding=page.Padding,Body=body.Bounds,Dock=body.Dock.ToString(),Grid=grid.Bounds});
                        }
                    }
                    form.ClientSize=new Size(1366,768);tabs.SelectedIndex=0;await Ready(module);form.Activate();tabs.Focus();
                    PostMessage(tabs.Handle,0x100,(IntPtr)Keys.Right,IntPtr.Zero);PostMessage(tabs.Handle,0x101,(IntPtr)Keys.Right,IntPtr.Zero);await Task.Delay(100);
                    Check(role+" "+target+" native arrow selects next",tabs.SelectedIndex==1);await Ready(module);
                    var chosen=tabs.SelectedTab!.Controls[0];var input=Desc(chosen).OfType<TextBox>().FirstOrDefault(t=>t.PlaceholderText.StartsWith("Tìm"));
                    if(input is not null){input.Text="probe-draft";tabs.SelectedIndex=2;await Ready(module);tabs.SelectedIndex=1;await Ready(module);Check(role+" "+target+" retains draft input",input.Text=="probe-draft");input.Text="";}
                    await form.OpenWorkspaceAsync("Home");
                    await Measure(role+"/"+target+"/reenter",async()=>{await form.OpenWorkspaceAsync(target.ToString());await Ready(module);});
                    Check(role+" "+target+" cached module",ReferenceEquals(module,Current(form)));
                    Check(role+" "+target+" all tab headers remain",tabs.TabCount==4);
                }
            }
            finally{typeof(MainForm).GetField("closeApproved",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(form,true);form.Dialogs.AbortAll();form.Close();}
        }
        Check("Read-only UI leaves business data unchanged",hashBefore==await DataHash(owner));
        using var tx=owner.CreateCommand();tx.CommandText="DECLARE @t int=@@TRANCOUNT,@s int=XACT_STATE();SELECT CONCAT(@t,':',@s);";Check("Owner transaction clean",(string?)await tx.ExecuteScalarAsync()=="0:0");
    }
    static UserControl Create(string name,Func<UserControl> factory)
    {
        var watch=Stopwatch.StartNew();var result=factory();watch.Stop();Track(result);constructors.Add(new{Name=name,Milliseconds=watch.Elapsed.TotalMilliseconds,Controls=Desc(result).Count()});return result;
    }
    static readonly HashSet<Control> tracked=[];
    static void Track(Control root)
    {
        if(!tracked.Add(root))return;root.Layout+=(_,_)=>layouts++;root.ControlAdded+=(_,e)=>{if(e.Control is not null)Track(e.Control);};foreach(Control c in root.Controls)Track(c);
    }
    static async Task Measure(string name,Func<Task> work)
    {
        await Task.Delay(50);stage=name;int firstOpen=opens,firstLayouts=layouts;maxGap=0;lastTick=Stopwatch.GetTimestamp();var watch=Stopwatch.StartNew();await work();watch.Stop();await Task.Delay(60);
        var count=queries.Count(q=>q.Stage==name);metrics.Add(new{Name=name,ElapsedMs=watch.Elapsed.TotalMilliseconds,MaxHeartbeatGapMs=maxGap,Connections=opens-firstOpen,ProcedureStarts=count,LayoutEvents=layouts-firstLayouts});stage="idle";
    }
    static async Task Ready(Control module)
    {
        await Wait(()=>module.Controls.OfType<TabControl>().Any());var tabs=module.Controls.OfType<TabControl>().Single();
        await Wait(()=>tabs.SelectedTab?.Controls.Count==1&&Desc(tabs.SelectedTab.Controls[0]).OfType<DataGridView>().Any(g=>g.DataSource is not null));await Task.Delay(20);
    }
    static async Task<string> DataHash(SqlConnection db)
    {
        using var c=db.CreateCommand();c.CommandText="SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) + (SELECT MaHD,TongTien,TrangThai FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH) + (SELECT MaNV,LuongCung,LuongTheoGio,DangLamViec FROM dbo.NHANVIEN ORDER BY MaNV FOR JSON PATH) + (SELECT MaPN,TongTien FROM dbo.PHIEUNHAP ORDER BY MaPN FOR JSON PATH)";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes((string?)await c.ExecuteScalarAsync()??"")));
    }
    static IEnumerable<Control> Desc(Control root){foreach(Control child in root.Controls){yield return child;foreach(var c in Desc(child))yield return c;}}
    static Control? Current(MainForm form)=>Field<Panel>(form,"content").Controls.Cast<Control>().FirstOrDefault(c=>c.Visible);
    static T Field<T>(object root,string name)=>(T)root.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(root)!;
    static async Task Wait(Func<bool> predicate){for(int i=0;i<500&&!predicate();i++)await Task.Delay(20);if(!predicate())throw new TimeoutException("Module grid/tab did not become ready.");}
    static void Check(string name,bool passed,object? actual=null){if(!passed)failed++;checks.Add(new{Name=name,Result=passed?"PASS":"FAIL",Actual=actual});}
    static void Save()=>File.WriteAllText(Path.Combine(output,mode+".json"),JsonSerializer.Serialize(new{Mode=mode,Database=database,Schema=24,Checks=checks,Failed=failed,Constructors=constructors,Metrics=metrics,QueryStarts=queries.Select(q=>new{q.Stage,q.Procedure}).ToArray(),Visual="SKIPPED by assignment; no screenshots",Dpi100And150="NOT RUN"},new JsonSerializerOptions{WriteIndented=true}));
    sealed class Trace:IObserver<DiagnosticListener>,IObserver<KeyValuePair<string,object?>>,IDisposable
    {
        readonly List<IDisposable> subscriptions=[];public Trace()=>subscriptions.Add(DiagnosticListener.AllListeners.Subscribe(this));
        public void OnNext(DiagnosticListener listener){if(listener.Name.Contains("SqlClient"))subscriptions.Add(listener.Subscribe(this));}
        public void OnNext(KeyValuePair<string,object?> item){if(!item.Key.EndsWith("WriteCommandBefore"))return;if(item.Value?.GetType().GetProperty("Command")?.GetValue(item.Value) is SqlCommand command&&command.Connection?.Database==database&&command.CommandType==System.Data.CommandType.StoredProcedure)queries.Enqueue((stage,command.CommandText));}
        public void OnError(Exception e){}public void OnCompleted(){}public void Dispose(){foreach(var subscription in subscriptions)subscription.Dispose();}
    }
    sealed class MemoryPending:IPendingOperationStore
    {
        public PendingOperation? Read(int actor,string operation)=>null;public void Save(PendingOperation value)=>throw new InvalidOperationException("No writes in UI read probe.");public void Clear(int actor,string operation){}
    }
}

