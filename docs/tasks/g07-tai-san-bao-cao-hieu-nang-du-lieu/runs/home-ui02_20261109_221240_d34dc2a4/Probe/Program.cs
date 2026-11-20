using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Home;
using CafeManager.WinForms.Navigation;
using Microsoft.Data.SqlClient;

internal static class HomeProbe
{
    const string Database="CoffeeManager_Test_G08_20261109_221240_d34dc2a4_demo";
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly string Root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
    static readonly List<object> Results=[];
    static readonly List<object> Metrics=[];
    static readonly Dictionary<string,object?> Evidence=[];
    static SqlConnection Owner=null!;
    static AuthService Auth=null!;static AuthRepository Repository=null!;static PermissionService Permissions=null!;static ReportService Reports=null!;
    static string Password="",Mode="Normal";static int Opened;
    static Form Controller=null!;
    static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
    static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Hidden)!.GetValue(o)!;
    static Task Read(AdminDashboardControl c)=>(Task)c.GetType().GetMethod("ReadAsync",Hidden)!.Invoke(c,null)!;
    static IEnumerable<Control> Descendants(Control c){foreach(Control x in c.Controls){yield return x;foreach(var y in Descendants(x))yield return y;}}
    static void Flush()=>File.WriteAllText(Path.Combine(Root,"results.json"),JsonSerializer.Serialize(new{Results,Metrics,Evidence},new JsonSerializerOptions{WriteIndented=true}));
    static async Task Case(string id,Func<Task> test)
    {
        try{await test();Results.Add(new{Id=id,Status="PASS"});}
        catch(Exception e){Results.Add(new{Id=id,Status="FAIL",Type=e.GetType().Name,Assertion=e is InvalidOperationException?e.Message:null,Sql=e is SqlException s?s.Number:(int?)null});}
        Flush();
    }
    static async Task Wait(Func<bool> done){for(var i=0;i<300&&!done();i++)await Task.Delay(25);Require(done(),"Timed out waiting for UI");}
    static async Task Idle(AdminDashboardControl c)=>await Wait(()=>Field<CancellationTokenSource?>(c,"pending") is null);
    static async Task ScalarSetup(string command){using var cmd=Owner.CreateCommand();cmd.CommandText=command;await cmd.ExecuteNonQueryAsync();}
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();Controller=new Form{Text="Home UI02 technical probe",ShowInTaskbar=false,ClientSize=new Size(1280,720)};
        Controller.Shown+=async(_,_)=>{try{await Run();}catch(Exception e){Evidence["Fatal"]=new{Type=e.GetType().Name,Sql=e is SqlException s?s.Number:e is DataAccessException d?d.Error.Number:(int?)null};}finally{Owner?.Dispose();Flush();Controller.Close();}};
        Application.Run(Controller);
    }
    static async Task Run()
    {
        Evidence["DeviceDpi"]=Controller.DeviceDpi;Require(Controller.DeviceDpi==120,"Only125% approved");
        var b=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=Database,IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false};
        Owner=new SqlConnection(b.ConnectionString);await Owner.OpenAsync();
        using(var marker=Owner.CreateCommand()){marker.CommandText="SELECT RunId FROM dbo.OperationInstallMarker";Require((string?)await marker.ExecuteScalarAsync()=="20261109_221303_a917c75a","Marker mismatch");}
        var ownerFactory=new ConnectionFactory(b.ConnectionString);Password=Guid.NewGuid().ToString("N")+"!a1";
        using var stamp=Owner.CreateCommand();stamp.CommandText="SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE TenDangNhap='admin'";
        await new BootstrapService(new SetupRepository(ownerFactory),new AuthRepository(ownerFactory),new PasswordService()).CompleteAsync(new BootstrapRequest{Mode="ActivateDemo",Username="admin",Password=Password,Confirmation=Password,EmployeeCode="NV001",ExpectedStamp=(byte[])(await stamp.ExecuteScalarAsync())!});
        await ScalarSetup("CREATE USER HomeUiRuntime WITHOUT LOGIN;ALTER ROLE CafeManagerApp ADD MEMBER HomeUiRuntime;");
        var factory=new ConnectionFactory(b.ConnectionString);
        typeof(ConnectionFactory).GetProperty("SessionInitializer",Hidden)!.SetValue(factory,(Func<SqlConnection,CancellationToken,Task>)(async(c,t)=>
        {
            Interlocked.Increment(ref Opened);using var cmd=c.CreateCommand();cmd.CommandText="EXECUTE AS USER='HomeUiRuntime';"+(Mode=="Error"?"THROW 53502,N'UI02 fault fixture',1;":Mode=="Delay"?"WAITFOR DELAY '00:00:02';":"");await cmd.ExecuteNonQueryAsync(t);
        }));
        Repository=new(factory);Auth=new(Repository,new PasswordService());Permissions=new(Repository);Reports=new(new ReportRepository(factory),Permissions);
        using var session=await Auth.LoginAsync(new("admin",Password));
        using var home=new AdminDashboardControl(Reports,session);Controller.Controls.Add(home);await Idle(home);
        await Case("UI02.Binding",async()=>
        {
            await Idle(home);var expected=await Reports.DashboardAsync(session);
            Require(Field<Label>(home,"orders").Text==expected.OpenOrders.ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("vi-VN")),"Orders mismatch");
            Require(Field<Label>(home,"debt").Text==expected.Debt.ToString("#,##0",System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))+" ₫","Debt mismatch");
            Require(Field<Label>(home,"revenue").Text==expected.Revenue.ToString("#,##0",System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))+" ₫","Revenue mismatch");
            Require(Field<Label>(home,"updated").Text.StartsWith("Cập nhật lúc"),"ReadAt missing");
        });
        foreach(var size in new[]{new Size(1280,720),new Size(1366,768),new Size(920,720),new Size(620,720),new Size(440,720)})
            await Case("UI02.Layout."+size.Width+"x"+size.Height,async()=>
            {
                Controller.ClientSize=size;await Task.Delay(60);home.PerformLayout();
                var grid=Field<TableLayoutPanel>(home,"metrics");var cards=Field<List<CafeManager.WinForms.UI.OrbitSurface>>(home,"cards");
                var buttons=Descendants(home).OfType<Button>().ToArray();Require(cards.Count==3&&buttons.Length==4,"Missing card/button");
                var buttonData=buttons.Select(x=>new{x.Text,Actual=x.Size,Preferred=x.GetPreferredSize(Size.Empty),Parent=x.Parent!.ClientSize,x.Bounds}).ToArray();
                var cardData=cards.Select(x=>new{x.AccessibleName,x.Bounds,ChildrenBottom=x.Controls.Cast<Control>().Max(c=>c.Bottom),x.Padding}).ToArray();
                Metrics.Add(new{Size=size,home.DeviceDpi,Columns=grid.ColumnCount,Grid=grid.Size,Buttons=buttonData,Cards=cardData});
                foreach(var x in buttons){var p=x.GetPreferredSize(Size.Empty);Require(x.Height>=p.Height&&x.Width>=p.Width,"Button smaller than preferred:"+x.Text);Require(x.Right<=x.Parent!.ClientSize.Width&&x.Bottom<=x.Parent.ClientSize.Height,"Button clipped:"+x.Text);}
                foreach(var card in cards){Require(card.Width>0&&card.Height>=160,"Card collapsed");Require(card.Controls.Cast<Control>().All(c=>c.Bottom+card.Padding.Bottom<=card.ClientSize.Height),"Card content clipped");}
                Require(Descendants(home).OfType<Label>().All(x=>x.Height>=x.GetPreferredSize(new Size(x.Width,0)).Height),"Label height clipped");
            });
        await Case("UI02.LargeValueLayout",async()=>
        {
            Controller.ClientSize=new(620,720);Field<Label>(home,"debt").Text="999.999.999.999.999.999 ₫";await Task.Delay(60);
            var label=Field<Label>(home,"debt");Require(label.Height>=label.GetPreferredSize(new Size(label.Width,0)).Height,"Large amount clipped");await Read(home);
        });
        await Case("UI02.ErrorAndRetry",async()=>
        {
            Mode="Error";try{await Read(home);Require(Field<Label>(home,"state").Text.StartsWith("Chưa cập nhật"),"No error state");Require(new[]{"revenue","orders","debt"}.All(x=>Field<Label>(home,x).Text=="—"),"Fake zero/stale values");Require(Field<Button>(home,"refresh").Enabled,"Refresh stuck");}finally{Mode="Normal";}
            await Read(home);Require(Field<Label>(home,"state").Text.StartsWith("Đã cập nhật"),"Retry failed");
        });
        await Case("UI02.PendingDuplicateAndHide",async()=>
        {
            Mode="Delay";var count=Opened;var load=Read(home);await Task.Delay(100);Require(Field<CancellationTokenSource?>(home,"pending") is not null&&!Field<Button>(home,"refresh").Enabled,"Loading not locked");
            await Read(home);Require(Opened==count+1,"Duplicate read sent");home.Hide();await load.WaitAsync(TimeSpan.FromSeconds(8));Require(Field<Label>(home,"revenue").Text=="—","Late data after hide");Mode="Normal";home.Show();await Idle(home);Require(Field<Label>(home,"state").Text.StartsWith("Đã cập nhật"),"Reload after show failed");
        });
        await Case("UI02.DisposeWhilePending",async()=>
        {
            using var c=new AdminDashboardControl(Reports,session);Controller.Controls.Add(c);c.BringToFront();await Idle(c);Mode="Delay";
            try{var load=Read(c);await Task.Delay(80);c.Dispose();c.Dispose();await load.WaitAsync(TimeSpan.FromSeconds(8));Require(c.IsDisposed,"Not disposed");}finally{Mode="Normal";home.BringToFront();}
        });
        home.Hide();
        await Case("UI02.RevokedDuringLoad",async()=>
        {
            using var s=await Auth.LoginAsync(new("admin",Password));using var c=new AdminDashboardControl(Reports,s);Controller.Controls.Add(c);c.BringToFront();await Idle(c);Mode="Delay";
            try{var load=Read(c);await Task.Delay(80);s.Revoke();await load.WaitAsync(TimeSpan.FromSeconds(8));Require(Field<Label>(c,"revenue").Text=="—"&&!Field<Button>(c,"refresh").Enabled,"Data after revoke");}finally{Mode="Normal";}
        });
        await Case("UI02.Navigation",async()=>
        {
            using var s=await Auth.LoginAsync(new("admin",Password));AdminDashboardControl? dashboard=null;var created=new Dictionary<NavigationTarget,int>();
            var modules=new Dictionary<NavigationTarget,Func<UserControl>>{[NavigationTarget.Home]=()=>dashboard=new AdminDashboardControl(Reports,s)};
            foreach(var target in new[]{NavigationTarget.Reports,NavigationTarget.Sales,NavigationTarget.Debt})modules[target]=()=>{created[target]=created.GetValueOrDefault(target)+1;return new UserControl{Name=target.ToString()};};
            using var main=new MainForm(s,Permissions,Repository.ReadStateAsync,modules:modules);main.Show();await Wait(()=>dashboard is not null&&dashboard.Visible);await Idle(dashboard!);
            foreach(var pair in new[]{("Báo cáo",NavigationTarget.Reports),("Bán hàng",NavigationTarget.Sales),("Công nợ",NavigationTarget.Debt)})
            {
                Descendants(dashboard!).OfType<Button>().Single(b=>b.Text==pair.Item1).PerformClick();await Wait(()=>created.GetValueOrDefault(pair.Item2)==1&&!dashboard!.Visible);await main.OpenWorkspaceAsync("Home");await Idle(dashboard!);
            }
            main.Dispose();
        });
        Evidence["SourceDllHash"]=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(AdminDashboardControl).Assembly.Location)));
        Evidence["Native"]="NOT RUN in component probe; no screenshot or visual acceptance";
    }
}
