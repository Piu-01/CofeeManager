using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests;
using CafeManager.Models.Requests.Sales;
using CafeManager.WinForms.Forms;
using Microsoft.Data.SqlClient;

internal static class Program
{
    private static readonly Type UiType=typeof(MainForm).Assembly.GetType("CafeManager.WinForms.Modules.Sales.CheckoutDialog")!;
    private static readonly List<object> Results=[];private static int failed;
    private static async Task<int> Main(string[] args)
    {
        using var manifest=JsonDocument.Parse(File.ReadAllText(args[0]));var m=manifest.RootElement;var db=m.GetProperty("Database").GetString()!;
        if(!System.Text.RegularExpressions.Regex.IsMatch(db,@"^CoffeeManager_Test_G04_\d{8}_\d{6}_[a-f0-9]{8}_checkout$")||m.GetProperty("Cleaned").GetBoolean())throw new InvalidOperationException("Isolated checkout DB required");
        await using var owner=new SqlConnection($"Server=localhost;Initial Catalog={db};Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False");await owner.OpenAsync();
        async Task<object?> Scalar(string sql){await using var c=owner.CreateCommand();c.CommandText=sql;return await c.ExecuteScalarAsync();}
        async Task Execute(string sql,string? hash=null){await using var c=owner.CreateCommand();c.CommandText=sql;if(hash is not null)c.Parameters.AddWithValue("@hash",hash);await c.ExecuteNonQueryAsync();}
        if((string?)await Scalar("SELECT RunId FROM dbo.G04TestMarker")!=m.GetProperty("Marker").GetString())throw new InvalidOperationException("Marker mismatch");
        var b=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CHECKOUT_SQL_RUNTIME")??throw new InvalidOperationException("Runtime connection required"));
        if(b.DataSource!="localhost"||b.InitialCatalog!=db||b.IntegratedSecurity)throw new InvalidOperationException("Actual runtime login required");
        var f=new ConnectionFactory(b.ConnectionString);var repo=new AuthRepository(f);var permission=new PermissionService(repo);var sales=new SalesService(new SalesRepository(f),permission);var invoices=new InvoiceService(new InvoiceRepository(f),permission);
        void Check(bool value,string message="Assertion failed"){if(!value)throw new InvalidOperationException(message);}
        async Task Case(string name,Func<Task<object?>> test){try{Results.Add(new{Name=name,Outcome="PASS",Actual=await test()});}catch(Exception ex){failed++;Results.Add(new{Name=name,Outcome="FAIL",Error=ex.ToString()});}Save();}
        void Save()=>File.WriteAllText(args[1],JsonSerializer.Serialize(new{Database=db,Schema=16,Scope="Exact frozen dialog captures allocations; production SalesService called directly for SQL commit. No physical Confirm click or real timeout delivery claimed.",Results,Failed=failed},new JsonSerializerOptions{WriteIndented=true}));
        try
        {
            var password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            await Execute("""
                INSERT dbo.NHANVIEN(MaNV,TenNV,LoaiHopDong,MaCV) SELECT 'NCU01',N'Checkout test','Full-time',MIN(MaCV) FROM dbo.CHUCVU;
                INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT,HoatDong) VALUES('NCU01','checkout.reg.cash',@hash,'CASH',1);
                INSERT dbo.KHUYENMAI(MaKM,TenKM,NgayBD,NgayKT,PhanTramGiam) VALUES
                ('KM99992500',N'Checkout full fixture',DATEADD(day,-1,CONVERT(date,SYSDATETIME())),DATEADD(day,1,CONVERT(date,SYSDATETIME())),100),
                ('KM99992501',N'Checkout zero percent fixture',DATEADD(day,-1,CONVERT(date,SYSDATETIME())),DATEADD(day,1,CONVERT(date,SYSDATETIME())),0);
                """,new PasswordService().Hash(password));
            var auth=new AuthService(repo,new PasswordService());using var session=await auth.LoginAsync(new LoginRequest("checkout.reg.cash",password));
            var methods=await sales.PaymentMethodsAsync(session);
            async Task<(SalesOrderDto Order,CheckoutPreview Preview)> New(string? customer=null,string? promo=null)
            {
                var code=await sales.AllocateAsync(session);await sales.CreateAsync(session,new(code,Guid.NewGuid(),new("Mang di",Customer:customer,Promotion:promo),"SP001",1,"Checkout UI isolated regression"));
                var order=(await sales.GetAsync(session,code))!;return(order,await sales.PreviewAsync(session,order.Header));
            }
            async Task<string> Snapshot()=>Convert.ToString(await Scalar("SELECT (SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) Stock,(SELECT MaKH,DiemTichLuy FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH) Points,(SELECT * FROM dbo.CHITIET_THANHTOAN ORDER BY MaTT FOR JSON PATH) Payments FOR JSON PATH"))!;
            await Case("C01 UI allocation81000/tender100000 commits once, stock/points/replay",async()=>
            {
                var n=await New("KH001","KM99992501");var pointsBefore=Convert.ToInt32(await Scalar("SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH001'"));var stockBefore=Convert.ToDecimal(await Scalar("SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01'"));
                var capture=Capture(sales,session,n.Order,n.Preview,methods,"100000",[("PT001","81000")]);Check(capture.Enabled&&capture.Change=="19.000 ₫");
                var id=Guid.NewGuid();var request=new CheckoutRequest(n.Order.Header.Code,n.Preview.Revision,id,n.Preview.Token,capture.Allocations);
                await sales.CheckoutAsync(session,request);var actual=(await sales.GetAsync(session,n.Order.Header.Code))!;var detail=(await invoices.DetailAsync(session,n.Order.Header.Code))!;
                Check(actual.Header.Status=="Da thanh toan"&&actual.Payments.Count==1&&actual.Payments.Sum(x=>x.Amount)==81000);
                var pointDelta=Convert.ToInt32(await Scalar("SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='KH001'"))-pointsBefore;var stockDelta=stockBefore-Convert.ToDecimal(await Scalar("SELECT SoLuongTon FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL01'"));Check(pointDelta==detail.Points&&pointDelta==81&&stockDelta==.018m);
                var after=await Snapshot();await sales.CheckoutAsync(session,request);Check(after==await Snapshot(),"Replay changed payment/stock/points");
                return new{Code=actual.Header.Code,Allocated=capture.Allocations.Sum(x=>x.SoTien),Paid=actual.Payments.Sum(x=>x.Amount),pointDelta,stockDelta,ReplaySnapshotEqual=true};
            });
            await Case("C05 mixed cash31000/bank50000 only cash determines change",async()=>{var n=await New();var ui=Capture(sales,session,n.Order,n.Preview,methods,"50000",[("PT001","31000"),("PT002","50000")]);Check(ui.Enabled&&ui.Change=="19.000 ₫");await sales.CheckoutAsync(session,new(n.Order.Header.Code,n.Preview.Revision,Guid.NewGuid(),n.Preview.Token,ui.Allocations));var actual=(await sales.GetAsync(session,n.Order.Header.Code))!;Check(actual.Payments.Count==2&&actual.Payments.Sum(x=>x.Amount)==81000&&actual.Payments.Single(x=>x.Method=="PT001").Amount==31000);return new{Code=actual.Header.Code,Payments=actual.Payments.Select(x=>new{x.Method,x.Amount})};});
            await Case("C05 bank only ignores tender and persists bank81000",async()=>{var n=await New();var ui=Capture(sales,session,n.Order,n.Preview,methods,"abc",[("PT002","81000")]);Check(ui.Enabled&&!ui.TenderEnabled);await sales.CheckoutAsync(session,new(n.Order.Header.Code,n.Preview.Revision,Guid.NewGuid(),n.Preview.Token,ui.Allocations));var actual=(await sales.GetAsync(session,n.Order.Header.Code))!;Check(actual.Payments.Single().Method=="PT002"&&actual.Payments.Single().Amount==81000);return new{Code=actual.Header.Code};});
            await Case("C05 zero payable commits without fake payment",async()=>{var n=await New(promo:"KM99992500");Check(n.Preview.Payable==0);var ui=Capture(sales,session,n.Order,n.Preview,methods,"0",[]);Check(ui.Enabled&&ui.Allocations.Count==0);await sales.CheckoutAsync(session,new(n.Order.Header.Code,n.Preview.Revision,Guid.NewGuid(),n.Preview.Token,ui.Allocations));var actual=(await sales.GetAsync(session,n.Order.Header.Code))!;Check(actual.Header.Status=="Da thanh toan"&&actual.Payments.Count==0&&actual.Header.Payable==0);return new{Code=actual.Header.Code};});
            await Case("C06 competing session checkout then UI preview reads paid",async()=>
            {
                var n=await New();using var other=await auth.LoginAsync(new LoginRequest("checkout.reg.cash",password));await sales.CheckoutAsync(other,new(n.Order.Header.Code,n.Preview.Revision,Guid.NewGuid(),n.Preview.Token,[new("PT001",81000)]));var before=await Snapshot();
                var result=await RunDialog(sales,session,n.Order,n.Preview,methods,async form=>{await (Task)UiType.GetMethod("ReloadPreview",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(form,null)!;return ((SalesOrderDto?)UiType.GetProperty("ResultOrder")!.GetValue(form))?.Header.Status;});
                Check(result=="Da thanh toan"&&before==await Snapshot());return new{Code=n.Order.Header.Code,UiResult=result,SnapshotEqual=true};
            });
            await Case("C07 unknown reconcile uses original request without duplicate payment",async()=>
            {
                var n=await New();var id=Guid.NewGuid();await sales.CheckoutAsync(session,new(n.Order.Header.Code,n.Preview.Revision,id,n.Preview.Token,[new("PT001",81000)]));var before=await Snapshot();
                var result=await RunDialog(sales,session,n.Order,n.Preview,methods,async form=>{UiType.GetField("requestId",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(form,id);UiType.GetField("unknown",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(form,true);await (Task)UiType.GetMethod("Reconcile",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(form,null)!;Check(UiType.GetProperty("TenderedCash")!.GetValue(form) is null&&UiType.GetProperty("Change")!.GetValue(form) is null);return ((SalesOrderDto?)UiType.GetProperty("ResultOrder")!.GetValue(form))?.Header.Status;});
                Check(result=="Da thanh toan"&&before==await Snapshot());return new{Code=n.Order.Header.Code,UiResult=result,SnapshotEqual=true,FaultScope="Unknown state injected; no real lost-response fault in this case"};
            });
            await Case("Transaction state clean",async()=>{var state=Convert.ToString(await Scalar("DECLARE @t int=@@TRANCOUNT,@s int=XACT_STATE();SELECT CONCAT(@t,':',@s)"));Check(state=="0:0");return state;});
        }
        catch(Exception ex){failed++;Results.Add(new{Name="Setup/runner",Outcome="FAIL",Error=ex.ToString()});Save();}
        Console.WriteLine($"SQL/UI bridge {Results.Count-failed}/{Results.Count} PASS");return failed==0?0:1;
    }
    private sealed record UiCapture(bool Enabled,bool TenderEnabled,string Change,IReadOnlyList<PaymentAllocation> Allocations);
    private static UiCapture Capture(SalesService s,SessionContext session,SalesOrderDto o,CheckoutPreview p,List<SalesLookup> methods,string tender,(string Method,string Amount)[] allocations)
    {
        UiCapture? result=null;Exception? error=null;var thread=new Thread(()=>{try{using var form=New(s,session,o,p,methods);var rows=(System.Collections.IList)UiType.GetField("rows",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(form)!;rows.Clear();var t=UiType.GetNestedType("PaymentDraft",BindingFlags.Public)!;foreach(var a in allocations){var row=Activator.CreateInstance(t)!;t.GetProperty("Method")!.SetValue(row,a.Method);t.GetProperty("Amount")!.SetValue(row,a.Amount);rows.Add(row);}var cash=(TextBox)UiType.GetField("tendered",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(form)!;cash.Text=tender;var captured=(IReadOnlyList<PaymentAllocation>)UiType.GetMethod("CaptureAllocations",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(form,null)!;var button=(Button)UiType.GetField("confirm",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(form)!;var change=(Label)UiType.GetField("differenceValue",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(form)!;result=new(button.Enabled,cash.Enabled,change.Text,captured);}catch(Exception ex){error=ex;}});thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();if(error is not null)throw error;return result!;
    }
    private static Form New(SalesService s,SessionContext session,SalesOrderDto o,CheckoutPreview p,List<SalesLookup> methods)=>(Form)Activator.CreateInstance(UiType,s,session,o,p,methods)!;
    private static Task<string?> RunDialog(SalesService s,SessionContext session,SalesOrderDto o,CheckoutPreview p,List<SalesLookup> methods,Func<Form,Task<string?>> action)
    {
        var completion=new TaskCompletionSource<string?>();var thread=new Thread(()=>{try{ApplicationConfiguration.Initialize();using var form=New(s,session,o,p,methods);form.Shown+=async(_,_)=>{try{completion.TrySetResult(await action(form));}catch(Exception ex){completion.TrySetException(ex);}finally{if(!form.IsDisposed){UiType.GetField("saved",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(form,true);form.Close();}}};Application.Run(form);}catch(Exception ex){completion.TrySetException(ex);}});thread.SetApartmentState(ApartmentState.STA);thread.Start();return completion.Task;
    }
}
