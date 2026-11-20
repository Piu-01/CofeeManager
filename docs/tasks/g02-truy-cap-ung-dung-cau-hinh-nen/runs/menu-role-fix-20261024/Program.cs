using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Sales;
using CafeManager.WinForms.Navigation;
using Microsoft.Data.SqlClient;

internal static class Program
{
    private const string Database="CoffeeManager_Test_G04_20261024_152759_12204643_manual";
    private static readonly List<object> Results=[];
    private static int failed;
    [STAThread]
    private static int Main(string[] args)
    {
        if(args.Length!=1)throw new ArgumentException("Evidence directory required.");
        ApplicationConfiguration.Initialize();
        using var owner=new SqlConnection($"Server=localhost;Initial Catalog={Database};Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False"); owner.Open();
        object? Scalar(string sql){using var c=owner.CreateCommand();c.CommandText=sql;return c.ExecuteScalar();}
        void Execute(string sql,string? hash=null){using var c=owner.CreateCommand();c.CommandText=sql;if(hash is not null)c.Parameters.AddWithValue("@hash",hash);c.ExecuteNonQuery();}
        if((string?)Scalar("SELECT RunId FROM dbo.G04TestMarker")!="20261024_152759_12204643"||Convert.ToInt32(Scalar("SELECT MAX(Version) FROM dbo.SchemaMigrations"))!=16)throw new InvalidOperationException("Owned schema016 fixture required.");
        if(Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.NHANVIEN WHERE MaNV IN('NVM01','NVM02','NVM03')"))!=0)throw new InvalidOperationException("Fixture codes exist.");
        var runtime=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("G04_MENU_RUNTIME")??throw new InvalidOperationException("Runtime required."));
        if(runtime.DataSource!="localhost"||runtime.InitialCatalog!=Database||runtime.IntegratedSecurity)throw new InvalidOperationException("Actual runtime login required.");
        var factory=new ConnectionFactory(runtime.ConnectionString);var repository=new AuthRepository(factory);var permissions=new PermissionService(repository);
        var password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var sessions=new List<SessionContext>();var provisioned=false;
        var originalAccounts=Convert.ToString(Scalar("SELECT TenDangNhap,MaVT,HoatDong,CONVERT(varchar(16),SecurityStamp,2) Stamp FROM dbo.TAIKHOAN WHERE TenDangNhap IN('admin','thungan','kho') ORDER BY TenDangNhap FOR JSON PATH"));
        try
        {
            Execute("""
                BEGIN TRANSACTION;
                INSERT dbo.NHANVIEN(MaNV,TenNV,LoaiHopDong,MaCV) SELECT x.Code,N'Menu regression','Full-time',(SELECT TOP(1) MaCV FROM dbo.CHUCVU ORDER BY MaCV) FROM (VALUES('NVM01'),('NVM02'),('NVM03'))x(Code);
                INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT,HoatDong) VALUES
                ('NVM01','menu.reg.admin',@hash,'ADMIN',1),('NVM02','menu.reg.cash',@hash,'CASH',1),('NVM03','menu.reg.store',@hash,'STORE',1);
                COMMIT;
                """,new PasswordService().Hash(password));provisioned=true;
            var auth=new AuthService(repository,new PasswordService());
            // Authenticate fixture identities through production BLL/SQL before constructing components.
            foreach(var role in new[]{"admin","cash","store"})sessions.Add(auth.LoginAsync(new LoginRequest("menu.reg."+role,password)).GetAwaiter().GetResult());
            foreach(var session in sessions)
            {
                var role=session.Identity.Role;
                var expected=role switch
                {
                    "ADMIN"=>new[]{NavigationTarget.Sales,NavigationTarget.Invoices,NavigationTarget.Delivery,NavigationTarget.Customers,NavigationTarget.Tables,NavigationTarget.Products,NavigationTarget.Materials,NavigationTarget.Loyalty},
                    "CASH"=>new[]{NavigationTarget.Sales,NavigationTarget.Invoices,NavigationTarget.Delivery,NavigationTarget.Customers,NavigationTarget.Materials},
                    _=>new[]{NavigationTarget.Materials,NavigationTarget.Products}
                };
                var moduleFactories=Enum.GetValues<NavigationTarget>().Where(x=>x is not (NavigationTarget.Home or NavigationTarget.Catalogs or NavigationTarget.SystemInfo)).ToDictionary(x=>x,x=>(Func<UserControl>)(()=>new UserControl()));
                moduleFactories[NavigationTarget.Sales]=()=>new SalesControl(new SalesService(new SalesRepository(factory),permissions),new TableService(new TableRepository(factory),permissions),new CustomerService(new CustomerRepository(factory),permissions),new LoyaltyService(new LoyaltyRepository(factory),permissions),session,new InvoiceService(new InvoiceRepository(factory),permissions));
                using var form=new MainForm(session,permissions,repository.ReadStateAsync,()=>new UserControl(),moduleFactories);
                using var timer=new System.Windows.Forms.Timer{Interval=100};var ticks=0;
                timer.Tick+=(_,_)=>
                {
                    ticks++;var status=Field<Label>(form,"statusLabel");
                    if(status.Text!="Phiên đang hoạt động."&&ticks<150)return;
                    if(role=="CASH"&&!Descendants(form).OfType<Label>().Any(x=>x.Text.StartsWith("Đã tải từ SQL"))&&ticks<150)return;
                    timer.Stop();
                    try
                    {
                        var sidebar=Field<FlowLayoutPanel>(form,"sidebar");
                        var buttons=sidebar.Controls.OfType<Button>().Where(b=>b.Visible).ToArray();
                        var targets=buttons.Where(b=>b.Tag is NavigationTarget).Select(b=>(NavigationTarget)b.Tag!).ToArray();
                        Check(role+" exact ordered menu",targets.SequenceEqual(expected),new{Targets=targets.Select(x=>x.ToString()).ToArray(),Buttons=buttons.Select(b=>b.Text).ToArray()});
                        Check(role+" catalog visibility",Field<Button>(form,"catalogsButton").Visible==(role=="ADMIN"));
                        var expectedCaption=role switch{"CASH"=>"KHÔNG GIAN THU NGÂN","STORE"=>"KHÔNG GIAN KHO",_=>"KHÔNG GIAN QUẢN LÝ"};
                        Check(role+" workspace role label",Field<Label>(form,"brandCaption").Text==expectedCaption);
                        var content=Field<Panel>(form,"content");var current=content.Controls.Cast<Control>().FirstOrDefault(c=>c.Visible);
                        Check(role+" initial screen",role=="CASH"?current is SalesControl:current?.GetType().Name=="SessionHomeControl",new{Actual=current?.GetType().Name,Status=status.Text,DeviceDpi=form.DeviceDpi,Breadcrumb=Field<Label>(form,"breadcrumbLabel").Text});
                        if(role=="CASH")
                        {
                            Check("CASH POS table/customer/promotion entry points remain",new[]{"Chọn bàn","F3 · Khách / Ưu đãi","F4 · Đơn mở"}.All(label=>Descendants(form).OfType<Button>().Any(b=>b.Text==label&&b.Visible&&b.Enabled)));
                            Check("CASH real POS read succeeded",Descendants(form).OfType<Label>().Any(l=>l.Text.StartsWith("Đã tải từ SQL")));
                            Check("CASH workspace title",Field<Label>(form,"breadcrumbLabel").Text=="Thu ngân / Bán hàng");
                        }
                    }
                    catch(Exception ex){Check(role+" observation",false,ex.Message);}
                    finally{form.Close();}
                };
                form.Shown+=(_,_)=>timer.Start();Application.Run(form);
            }
        }
        finally
        {
            foreach(var session in sessions)session.Dispose();
            if(provisioned)Execute("DELETE dbo.TAIKHOAN WHERE MaNV IN('NVM01','NVM02','NVM03');DELETE dbo.NHANVIEN WHERE MaNV IN('NVM01','NVM02','NVM03');");
            var currentAccounts=Convert.ToString(Scalar("SELECT TenDangNhap,MaVT,HoatDong,CONVERT(varchar(16),SecurityStamp,2) Stamp FROM dbo.TAIKHOAN WHERE TenDangNhap IN('admin','thungan','kho') ORDER BY TenDangNhap FOR JSON PATH"));
            Check("Original account roles/stamps unchanged",originalAccounts==currentAccounts);
            Check("Fixture employees removed",Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.NHANVIEN WHERE MaNV IN('NVM01','NVM02','NVM03')"))==0);
            Check("Owner transaction clean",Convert.ToString(Scalar("DECLARE @t int=@@TRANCOUNT,@s int=XACT_STATE();SELECT CONCAT(@t,':',@s)"))=="0:0");
            File.WriteAllText(Path.Combine(args[0],"menu-results.json"),JsonSerializer.Serialize(new{Database,Schema=16,ComponentTest="Real MainForm; CASH real SalesControl/BLL/runtime SQL reads; other module bodies are stubs, no handler/CRUD coverage claimed",Results,Failed=failed},new JsonSerializerOptions{WriteIndented=true}));
        }
        Console.WriteLine($"Menu component regression: {Results.Count-failed}/{Results.Count} PASS.");return failed==0?0:1;
    }
    private static T Field<T>(object owner,string name)=>(T)owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(owner)!;
    private static IEnumerable<Control> Descendants(Control root){foreach(Control child in root.Controls){yield return child;foreach(var descendant in Descendants(child))yield return descendant;}}
    private static void Check(string name,bool ok,object? actual=null){if(!ok)failed++;Results.Add(new{Name=name,Outcome=ok?"PASS":"FAIL",Actual=actual});}
}
