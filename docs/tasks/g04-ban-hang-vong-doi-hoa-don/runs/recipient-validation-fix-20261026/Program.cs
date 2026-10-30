using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using CafeManager.Models.Requests.Sales;
using CafeManager.WinForms.Forms;
using Microsoft.Data.SqlClient;

internal static class Program
{
    private static readonly List<object> Results=[];private static int failed;
    private static readonly Type DialogType=typeof(MainForm).Assembly.GetType("CafeManager.WinForms.Modules.Sales.OrderHeaderDialog")!;
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        const string db="CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout";
        using var owner=new SqlConnection($"Server=localhost;Initial Catalog={db};Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False");owner.Open();
        object? Scalar(string sql){using var c=owner.CreateCommand();c.CommandText=sql;return c.ExecuteScalar();}
        void Execute(string sql,string? hash=null){using var c=owner.CreateCommand();c.CommandText=sql;if(hash is not null)c.Parameters.AddWithValue("@hash",hash);c.ExecuteNonQuery();}
        if((string?)Scalar("SELECT RunId FROM dbo.G04TestMarker")!="20261025_181739_98e5df59")throw new InvalidOperationException("Owned fixture required");
        if(Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.NHANVIEN WHERE MaNV='NRV01'"))!=0)throw new InvalidOperationException("Fixture actor exists");
        var b=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("G04_HEADER_RUNTIME")??throw new InvalidOperationException("Runtime required"));
        if(b.DataSource!="localhost"||b.InitialCatalog!=db||b.IntegratedSecurity)throw new InvalidOperationException("Actual runtime login required");
        var f=new ConnectionFactory(b.ConnectionString);var repo=new AuthRepository(f);var permissions=new PermissionService(repo);
        var sales=new SalesService(new SalesRepository(f),permissions);var customers=new CustomerService(new CustomerRepository(f),permissions);var loyalty=new LoyaltyService(new LoyaltyRepository(f),permissions);
        var bad=new SqlConnectionStringBuilder(b.ConnectionString){InitialCatalog=db+"_missing",ConnectTimeout=2};
        var failingCustomers=new CustomerService(new CustomerRepository(new ConnectionFactory(bad.ConnectionString,2)),permissions);
        var password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var ordersBefore=Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.HOADON"));
        var provisioned=false;
        try
        {
            Execute("BEGIN TRAN;INSERT dbo.NHANVIEN(MaNV,TenNV,LoaiHopDong,MaCV) SELECT 'NRV01',N'Header regression','Full-time',MIN(MaCV) FROM dbo.CHUCVU;INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT,HoatDong) VALUES('NRV01','header.reg.cash',@hash,'CASH',1);COMMIT;",new PasswordService().Hash(password));provisioned=true;
            using var session=new AuthService(repo,new PasswordService()).LoginAsync(new LoginRequest("header.reg.cash",password)).GetAwaiter().GetResult();
            var table=Convert.ToString(Scalar("SELECT TOP(1) RTRIM(MaBan) FROM dbo.BAN ORDER BY MaBan"));
            var customer=Convert.ToString(Scalar("SELECT TOP(1) RTRIM(MaKH) FROM dbo.KHACHHANG ORDER BY MaKH"));
            var partner=Convert.ToString(Scalar("SELECT TOP(1) RTRIM(MaDT) FROM dbo.DOITACGIAOHANG ORDER BY MaDT"));
            var promo=Convert.ToString(Scalar("SELECT TOP(1) RTRIM(MaKM) FROM dbo.KHUYENMAI WHERE '20250401' BETWEEN NgayBD AND NgayKT ORDER BY MaKM"));
            Form New(OrderHeaderInput input,bool fail=false)=> (Form)Activator.CreateInstance(DialogType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,[input,new DateTime(2025,4,1),sales,fail?failingCustomers:customers,loyalty,session],null)!;
            void Run(string name,OrderHeaderInput input,string action="apply",bool fail=false)
            {
                using var form=New(input,fail);using var timer=new System.Windows.Forms.Timer{Interval=50};var ticks=0;
                timer.Tick+=(_,_)=>
                {
                    ticks++;var state=Field<Label>(form,"state");var ready=Field<bool>(form,"ready");
                    if(action!="early-close"&&!ready&&!state.Text.StartsWith("Không tải được")&&ticks<200)return;
                    timer.Stop();
                    try
                    {
                        var buttons=Descendants(form).OfType<Button>().ToArray();var save=buttons.Single(x=>x.Text=="Áp dụng");var back=buttons.Single(x=>x.Text=="Quay lại");
                        if(action=="early-close"){Check(name,Result(form) is null&&back.Enabled);return;}
                        if(fail){DialogType.GetMethod("Save",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(form,null);Check(name,!ready&&!save.Enabled&&back.Enabled&&Result(form) is null&&state.Text.StartsWith("Không tải được"),state.Text);return;}
                        if(!ready||!save.Enabled||!back.Enabled)throw new InvalidOperationException("Dialog did not become ready: "+state.Text);
                        if(action=="cancel"){Check(name,Result(form) is null&&form.DialogResult!=DialogResult.OK);return;}
                        if(input.Channel!="Giao hang"&&Field<ComboBox>(form,"partner").DataSource is not null)throw new InvalidOperationException("Non-delivery partner initialized");
                        // Invoke the production Save handler as a component test; no physical click claimed.
                        DialogType.GetMethod("Save",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(form,null);
                        var result=Result(form);
                        if(action=="invalid"){Check(name,result is null&&form.DialogResult!=DialogResult.OK&&state.Text.Contains("Cần nhập tên"),state.Text);return;}
                        Check(name,result==SalesService.Normalize(input)&&form.DialogResult==DialogResult.OK,new{Result=result,Dpi=form.DeviceDpi});
                    }
                    catch(Exception ex){Check(name,false,(ex.InnerException??ex).Message);}
                    finally{form.Close();}
                };
                form.Shown+=(_,_)=>timer.Start();Application.Run(form);
            }
            void Validate(string label, string field, string value, bool all = false)
            {
                using var form=New(new("Giao hang",Recipient:"G05 kiểm thử",Phone:"0900000026",Address:"Địa chỉ fixture DB test"));
                using var timer=new System.Windows.Forms.Timer{Interval=50};var ticks=0;
                timer.Tick+=(_,_)=>
                {
                    if(!Field<bool>(form,"ready") && ++ticks<200)return;
                    timer.Stop();
                    try
                    {
                        if(!Field<bool>(form,"ready"))throw new InvalidOperationException("Lookup not ready");
                        var input=Field<TextBox>(form,field); input.Text=value;
                        if(all){Field<TextBox>(form,"phone").Text="abc";Field<TextBox>(form,"address").Text=" ";}
                        DialogType.GetMethod("Save",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(form,null);
                        var errors=Field<ErrorProvider>(form,"errors");
                        Check(label+" error at field/focus, input retained",errors.GetError(input).Length>0&&input.Focused&&input.Text==value&&Result(form) is null&&form.DialogResult!=DialogResult.OK,new{Field=field,Error=errors.GetError(input),Focused=input.Focused,Dpi=form.DeviceDpi});
                        Check(label+" no misplaced errors",new[]{"recipient","phone","address","ship"}.All(n=>errors.GetError(Field<TextBox>(form,n)).Length>0==(n==field || all&&n is "phone" or "address")));
                        Field<TextBox>(form,"recipient").Text="G05 kiểm thử";Field<TextBox>(form,"phone").Text="0900000026";Field<TextBox>(form,"address").Text="Địa chỉ fixture DB test";Field<TextBox>(form,"ship").Text="0";
                        DialogType.GetMethod("Save",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(form,null);
                        Check(label+" correction clears errors and yields typed header",Result(form) is {Channel:"Giao hang",Recipient:"G05 kiểm thử",Phone:"0900000026",Address:"Địa chỉ fixture DB test"} && new[]{"recipient","phone","address","ship"}.All(n=>errors.GetError(Field<TextBox>(form,n)).Length==0));
                    }
                    catch(Exception ex){Check(label,false,(ex.InnerException??ex).ToString());}
                    finally{DialogType.GetField("dirty",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(form,false);form.Close();}
                };
                form.Shown+=(_,_)=>timer.Start();Application.Run(form);
            }
            Validate("Recipient empty","recipient","");
            Validate("Recipient whitespace","recipient","   ");
            Validate("Phone empty","phone","");
            Validate("Phone alphabetic","phone","abc");
            Validate("Phone whitespace","phone","   ");
            Validate("Address empty","address","");
            Validate("Address whitespace","address","   ");
            Validate("Several invalid fields focus first","recipient","",true);
            Validate("Invalid ship stays at ship","ship","abc");
            Run("Mang di apply anonymous/no promotion",new("Mang di"));
            Run("Tai quan apply preserves table",new("Tai quan",table));
            Run("Giao hang self delivery",new("Giao hang",Recipient:"Test header",Phone:"0900000004",Address:"Test address",Ship:10000));
            Run("Giao hang selected partner",new("Giao hang",Partner:partner,Recipient:"Test header",Phone:"0900000004",Address:"Test address",Ship:10000));
            if(string.IsNullOrEmpty(promo)||string.IsNullOrEmpty(customer))throw new InvalidOperationException("Required customer/promotion fixture absent");
            Run("Mang di preserves customer/promotion",new("Mang di",Customer:customer,Promotion:promo));
            Run("Cancel ready dialog has no result",new("Mang di"),"cancel");
            Run("Lookup failure shows error, save disabled, back available",new("Mang di"),fail:true);
            Run("Reopen after lookup failure",new("Mang di"));
            Run("Close during loading has no result",new("Mang di"),"early-close");
            Run("Delivery missing recipient rejected",new("Giao hang"),"invalid");
            Check("Dialog tests did not create invoices",ordersBefore==Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.HOADON")));
        }
        finally
        {
            if(provisioned)Execute("DELETE dbo.TAIKHOAN WHERE MaNV='NRV01';DELETE dbo.NHANVIEN WHERE MaNV='NRV01';");
            Check("Fixture actor removed",Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.NHANVIEN WHERE MaNV='NRV01'"))==0);
            Check("Transaction clean",Convert.ToString(Scalar("DECLARE @t int=@@TRANCOUNT,@s int=XACT_STATE();SELECT CONCAT(@t,':',@s)"))=="0:0");
            File.WriteAllText(Path.Combine(args[0],"results.json"),JsonSerializer.Serialize(new{Database=db,Schema=16,Scope="Real OrderHeaderDialog/BLL/runtime SQL; Save handler component invocation, no physical mouse/keyboard or full POS commit claimed",Results,Failed=failed},new JsonSerializerOptions{WriteIndented=true}));
        }
        Console.WriteLine($"Header regression: {Results.Count-failed}/{Results.Count} PASS");return failed==0?0:1;
    }
    private static OrderHeaderInput? Result(Form f)=>(OrderHeaderInput?)DialogType.GetProperty("Result")!.GetValue(f);
    private static T Field<T>(Form f,string name)=>(T)DialogType.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(f)!;
    private static IEnumerable<Control> Descendants(Control root){foreach(Control c in root.Controls){yield return c;foreach(var d in Descendants(c))yield return d;}}
    private static void Check(string name,bool ok,object? actual=null){if(!ok)failed++;Results.Add(new{Name=name,Outcome=ok?"PASS":"FAIL",Actual=actual});}
}


