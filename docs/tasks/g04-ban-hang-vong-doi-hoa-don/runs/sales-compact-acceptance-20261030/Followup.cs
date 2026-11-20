using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests.Sales;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Sales;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

internal static class Program
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool IsWindowEnabled(IntPtr h);
    static readonly List<object> Results=[];static int failed;
    static void Check(string n,bool ok,object? actual=null){Results.Add(new{Name=n,Outcome=ok?"PASS":"FAIL",Actual=actual});if(!ok)failed++;}
    static FieldInfo Info(object o,string n){for(var t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(n,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);if(f!=null)return f;}throw new MissingFieldException(n);}
    static T F<T>(object o,string n)=>(T)Info(o,n).GetValue(o)!;
    static Task Call(object o,string n,params object?[] args){for(var t=o.GetType();t!=null;t=t.BaseType){var m=t.GetMethod(n,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);if(m!=null){var result=m.Invoke(o,args);return result as Task??Task.CompletedTask;}}throw new MissingMethodException(n);}
    static IEnumerable<Control> Children(Control c){foreach(Control x in c.Controls){yield return x;foreach(var y in Children(x))yield return y;}}
    static async Task Wait(Func<bool> p){for(int i=0;i<500;i++){if(p())return;await Task.Delay(25);}throw new TimeoutException("UI readiness deadline");}
    static async Task Idle(object p){await Task.Delay(100);await Wait(()=>!F<bool>(p,"busy"));}
    static Form? Open(string type)=>Application.OpenForms.Cast<Form>().LastOrDefault(f=>f.GetType().Name==type&&f.Visible);
    static async Task<Form> Dialog(string type){await Wait(()=>Open(type)!=null);return Open(type)!;}
    static Button Button(Control c,string text)=>Children(c).OfType<Button>().Single(b=>b.Text==text);
    static async Task Confirm(bool yes=true,string? title=null)
    {
        var d=await Dialog(nameof(OrbitConfirmationDialog));if(title!=null)Check("Confirmation title "+title,d.Text==title,d.Text);
        Button(d,yes?(Children(d).OfType<Button>().Any(b=>b.Text=="Có")?"Có":"Đồng ý"):(Children(d).OfType<Button>().Any(b=>b.Text=="Không")?"Không":"Hủy")).PerformClick(); await Wait(()=>d.IsDisposed||!d.Visible);
    }
    [STAThread]static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();Application.SetColorMode(SystemColorMode.Dark);
        const string db="CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout";
        using var owner=new SqlConnection($"Server=localhost;Initial Catalog={db};Integrated Security=True;TrustServerCertificate=True;Pooling=False");owner.Open();
        object? Scalar(string sql){using var c=owner.CreateCommand();c.CommandText=sql;return c.ExecuteScalar();}
        void Sql(string sql,string? hash=null){using var c=owner.CreateCommand();c.CommandText=sql;if(hash!=null)c.Parameters.AddWithValue("@hash",hash);c.ExecuteNonQuery();}
        Dictionary<string,decimal> Values(string sql){using var c=owner.CreateCommand();c.CommandText=sql;using var r=c.ExecuteReader();var result=new Dictionary<string,decimal>();while(r.Read())result[r.GetString(0).Trim()]=r.GetDecimal(1);return result;}
        if((string?)Scalar("SELECT RunId FROM dbo.G04TestMarker")!="20261025_181739_98e5df59"||Convert.ToInt32(Scalar("SELECT MAX(Version) FROM dbo.SchemaMigrations"))!=16)throw new Exception("DB marker/schema mismatch");
        var cs=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CHECKOUT_SQL_RUNTIME")!);if(cs.DataSource!="localhost"||cs.InitialCatalog!=db||cs.IntegratedSecurity)throw new Exception("Runtime target mismatch");
        var factory=new ConnectionFactory(cs.ConnectionString);var auth=new AuthRepository(factory);var permission=new PermissionService(auth);var sales=new SalesService(new SalesRepository(factory),permission);var tables=new TableService(new TableRepository(factory),permission);var customers=new CustomerService(new CustomerRepository(factory),permission);var loyalty=new LoyaltyService(new LoyaltyRepository(factory),permission);var invoices=new InvoiceService(new InvoiceRepository(factory),permission);
        string Existing()=> (string)Scalar("SELECT (SELECT (SELECT * FROM dbo.HOADON WHERE MaNV<>'SCC30' ORDER BY MaHD FOR JSON PATH) Orders,(SELECT d.* FROM dbo.CHITIET_HD d JOIN dbo.HOADON h ON h.MaHD=d.MaHD WHERE h.MaNV<>'SCC30' ORDER BY d.MaHD,d.MaDong FOR JSON PATH) Lines,(SELECT d.* FROM dbo.CHITIET_THANHTOAN d JOIN dbo.HOADON h ON h.MaHD=d.MaHD WHERE h.MaNV<>'SCC30' ORDER BY d.MaTT FOR JSON PATH) Payments,(SELECT MaKH,DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH<>'SC030' ORDER BY MaKH FOR JSON PATH) Points FOR JSON PATH)")!;
        var existing=Existing();var stockBefore=Values("SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU");File.WriteAllText(Path.Combine(args[0],"followup-before-existing.json"),existing);File.WriteAllText(Path.Combine(args[0],"followup-before-stock.json"),JsonSerializer.Serialize(stockBefore));
        var password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));bool provisioned=false;
        try
        {
            if(Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.NHANVIEN n JOIN dbo.TAIKHOAN t ON t.MaNV=n.MaNV WHERE n.MaNV IN('SCA30','SCC30','SCS30') AND n.TenNV=N'POS compact fixture' AND t.TenDangNhap LIKE 'sc.fixture.%'"))!=3)throw new Exception("Owned fixture recovery mismatch");
            Sql("UPDATE dbo.TAIKHOAN SET MatKhauHash=@hash,HoatDong=1 WHERE MaNV IN('SCA30','SCC30','SCS30') AND TenDangNhap LIKE 'sc.fixture.%';",new PasswordService().Hash(password));provisioned=true;
            using var cash=new AuthService(auth,new PasswordService()).LoginAsync(new("sc.fixture.cash",password)).GetAwaiter().GetResult();using var admin=new AuthService(auth,new PasswordService()).LoginAsync(new("sc.fixture.admin",password)).GetAwaiter().GetResult();using var store=new AuthService(auth,new PasswordService()).LoginAsync(new("sc.fixture.store",password)).GetAwaiter().GetResult();
            using var main=new MainForm{Text="POS compact frozen functional acceptance"};SalesControl page=new(sales,tables,customers,loyalty,cash,invoices);F<Panel>(main,"content").Controls.Add(page);
            using var deadline=new System.Windows.Forms.Timer{Interval=240000};deadline.Tick+=(_,_)=>{Check("Runner deadline",false);main.Dialogs.AbortAll();main.Dispose();Application.ExitThread();};
            main.Shown+=async(_,_)=>
            {
                deadline.Start();
                async Task Header(Action<Form>? edit=null)
                {
                    var task=Call(page,"EditHeader");var d=await Dialog("OrderHeaderDialog");await Wait(()=>F<bool>(d,"ready"));edit?.Invoke(d);Button(d,"Áp dụng").PerformClick();await task.WaitAsync(TimeSpan.FromSeconds(10));await Idle(page);
                }
                async Task Add(string product,int quantity,string note)
                {
                    var item=(await sales.MenuAsync(cash,"",null,0)).Single(x=>x.Code==product);var task=Call(page,"AddProduct",item);var d=await Dialog("OrderLineDialog");F<NumericUpDown>(d,"quantity").Value=quantity;F<TextBox>(d,"note").Text=note;Button(d,"Áp dụng").PerformClick();
                    if(F<ComboBox>(page,"channel").SelectedValue as string=="Giao hang") {var header=await Dialog("OrderHeaderDialog");await Wait(()=>F<bool>(header,"ready"));Button(header,"Áp dụng").PerformClick();}
                    await task.WaitAsync(TimeSpan.FromSeconds(10));await Idle(page);
                }
                async Task CloseCheckout(Form d,Task task)
                {
                    Button(d,"Quay lại").PerformClick();await Wait(()=>task.IsCompleted||Open(nameof(OrbitConfirmationDialog))!=null);if(Open(nameof(OrbitConfirmationDialog))!=null)await Confirm();await task.WaitAsync(TimeSpan.FromSeconds(10));await Idle(page);
                }
                async Task Pay(string kind,bool reject=false,bool printYes=false)
                {
                    var before=F<SalesOrderDto>(page,"order");var task=Call(page,"Checkout");var d=await Dialog("CheckoutDialog");var preview=F<CheckoutPreview>(d,"preview");Check("SC05 F8 breakdown matches DTO "+kind,preview.Total==before.Header.Total&&preview.Ship==before.Header.Ship&&preview.Discount==before.Header.Discount&&preview.Payable==before.Header.Payable,new{preview.Total,preview.Discount,preview.Ship,preview.Payable});
                    if(kind=="mixed")
                    {
                        var rows=F<IList>(d,"rows");rows.Clear();var type=d.GetType().GetNestedType("PaymentDraft",BindingFlags.Public)!;
                        foreach(var allocation in new[]{("PT001",30000m),("PT002",preview.Payable-30000)}){var row=Activator.CreateInstance(type)!;type.GetProperty("Method")!.SetValue(row,allocation.Item1);type.GetProperty("Amount")!.SetValue(row,allocation.Item2.ToString("0"));rows.Add(row);}
                        F<TextBox>(d,"tendered").Text="50000";
                    }
                    else if(preview.Payable>0)F<TextBox>(d,"tendered").Text=(preview.Payable+19000).ToString("0");
                    Check("SC07 confirm enabled "+kind,F<Button>(d,"confirm").Enabled);
                    F<Button>(d,"confirm").PerformClick();await Confirm(true,"Xác nhận thanh toán");
                    if(reject)
                    {
                        await Wait(()=>!F<bool>(d,"busy")&&F<Control>(d,"state").Text.Contains("nguyên liệu"));Check("SC07 insufficient stock rejected atomically",(await sales.GetAsync(cash,before.Header.Code))!.Header.IsOpen&&Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM dbo.CHITIET_THANHTOAN WHERE MaHD='{before.Header.Code}'"))==0&&Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM dbo.TIEUHAO_HOADON WHERE MaHD='{before.Header.Code}'"))==0,F<Control>(d,"state").Text);await CloseCheckout(d,task);return;
                    }
                    var ask=await Dialog(nameof(OrbitConfirmationDialog));Check("SC07 print question "+kind,ask.Text=="In hóa đơn",ask.Text);await Confirm(printYes);await task.WaitAsync(TimeSpan.FromSeconds(10));await Idle(page);
                    var actual=await sales.GetAsync(cash,before.Header.Code);Check("SC07 paid once/reset "+kind,actual?.Header.Status=="Da thanh toan"&&F<SalesOrderDto?>(page,"order")==null&&F<Label>(F<Control>(page,"orderPanel"),"Totals").Text=="—",F<Label>(page,"state").Text);Check("SC07 allocations exact "+kind,actual!.Payments.Sum(p=>p.Amount)==preview.Payable);
                    Check("SC07 one checkout receipt "+kind,Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE EntityCode='{before.Header.Code}' AND Operation='SalesCheckout'"))==1);
                }
                try
                {
                    await Idle(page);
                    var initial=(string)Scalar("SELECT TOP(1) MaHD FROM dbo.HOADON WHERE MaNV='SCC30' AND TrangThai=N'Chua thanh toan' ORDER BY NgayLap,MaHD")!;
                    await page.OpenOrderAsync(initial);await Pay("cash");Check("SC07 cash change19000",F<Label>(page,"state").Text.Contains("19.000"));
                    foreach(var scenario in new[]{("mixed","HD96300001",(string?)null),("zero","HD96300002","KM96300002"),("missing-stock","HD96300003",(string?)null)})
                    {
                        await sales.CreateAsync(cash,new(scenario.Item2,Guid.NewGuid(),new("Mang di",Promotion:scenario.Item3),scenario.Item1=="missing-stock"?"SP963":"SP001",1,"SC "+scenario.Item1));await page.OpenOrderAsync(scenario.Item2);await Pay(scenario.Item1,reject:scenario.Item1=="missing-stock");
                    }
                    await Call(page,"NewOrder");F<ComboBox>(page,"channel").SelectedValue="Tai quan";await page.OpenTableAsync("B9630");await Add("SP001",1,"SC tại quán");Check("SC04 at-table creates first item",F<SalesOrderDto>(page,"order").Header.TableCode=="B9630");var cancel=Call(page,"CancelOrder");await Confirm();await cancel;await Idle(page);Check("SC04 canceled order locks pay",F<SalesOrderDto>(page,"order").Header.Status=="Huy"&&!F<Button>(page,"pay").Enabled);
                    await Call(page,"NewOrder");F<ComboBox>(page,"channel").SelectedValue="Giao hang";await Header(d=>{F<TextBox>(d,"recipient").Text="SC người nhận";F<TextBox>(d,"phone").Text="098763030";F<TextBox>(d,"address").Text="Địa chỉ SC giao hàng";F<TextBox>(d,"ship").Text="19000";});await Add("SP001",1,"SC giao");Check("SC04 delivery info/ship",F<SalesOrderDto>(page,"order").Header is{Channel:"Giao hang",Ship:19000,Recipient:"SC người nhận"});
                    var checkout=Call(page,"Checkout");var cd=await Dialog("CheckoutDialog");Check("SC05 delivery F8 breakdown",F<CheckoutPreview>(cd,"preview").Ship==19000);await CloseCheckout(cd,checkout);
                    // Fault only the optional postcommit invoice reader, not global SQL permissions.
                    await Call(page,"NewOrder");page.Dispose();var bad=new SqlConnectionStringBuilder(cs.ConnectionString){InitialCatalog="SC_ReadFault_NoSuchDatabase",ConnectTimeout=2};var broken=new InvoiceService(new InvoiceRepository(new ConnectionFactory(bad.ConnectionString,2)),permission);page=new SalesControl(sales,tables,customers,loyalty,cash,broken);F<Panel>(main,"content").Controls.Add(page);await Idle(page);
                    await sales.CreateAsync(cash,new("HD96300004",Guid.NewGuid(),new("Mang di"),"SP001",1,"SC read fault"));await page.OpenOrderAsync("HD96300004");await Pay("postcommit-read-fault",printYes:true);Check("SC07 read failure still paid/reset",F<Label>(page,"state").Text.Contains("Đã thanh toán")&&F<Label>(page,"state").Text.Contains("Chưa in được hóa đơn"));
                    Check("SC08 ADMIN reads menu",(await sales.MenuAsync(admin,"",null,0)).Count>0);try{await sales.MenuAsync(store,"",null,0);Check("SC08 STORE sales denied",false);}catch(UnauthorizedAccessException){Check("SC08 STORE sales denied",true);}
                }
                catch(Exception e){Check("Runner exception",false,(e.InnerException??e).ToString());}
                finally{deadline.Stop();main.Dialogs.AbortAll();page.Dispose();main.Dispose();Application.ExitThread();}
            };
            Application.Run(main);
            var stockAfter=Values("SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU");var used=Values("SELECT t.MaNVL,CONVERT(decimal(18,4),SUM(t.SoLuong)) FROM dbo.TIEUHAO_HOADON t JOIN dbo.HOADON h ON h.MaHD=t.MaHD WHERE h.MaNV='SCC30' GROUP BY t.MaNVL");Check("SC07 all stock equals snapshot consumption",stockBefore.All(p=>stockAfter[p.Key]==p.Value-used.GetValueOrDefault(p.Key)),new{Before=stockBefore,After=stockAfter,Consumed=used});Check("SC07 customer points equal point history",Convert.ToInt32(Scalar("SELECT DiemTichLuy FROM dbo.KHACHHANG WHERE MaKH='SC030'"))==Convert.ToInt32(Scalar("SELECT COALESCE(SUM(CASE WHEN Loai=N'Cong' THEN SoDiem ELSE -SoDiem END),0) FROM dbo.LICHSU_DIEM WHERE MaKH='SC030'")));
            var after=Existing();File.WriteAllText(Path.Combine(args[0],"followup-after-existing.json"),after);Check("Existing orders/lines/payments/points unchanged",existing==after);File.WriteAllText(Path.Combine(args[0],"followup-after-stock.json"),JsonSerializer.Serialize(stockAfter));
        }
        finally
        {
            if(provisioned)Sql("UPDATE dbo.TAIKHOAN SET HoatDong=0 WHERE MaNV IN('SCA30','SCC30','SCS30');");
            var owned=(string)Scalar("SELECT (SELECT MaHD,TrangThai,LoaiDon,ThanhTien,MaKH FROM dbo.HOADON WHERE MaNV='SCC30' ORDER BY MaHD FOR JSON PATH)")!;File.WriteAllText(Path.Combine(args[0],"owned-orders.json"),owned);
            File.WriteAllText(Path.Combine(args[0],"followup-results.json"),JsonSerializer.Serialize(new{Failed=failed,Database=db,Scope="Frozen binary; real Auth/runtime SQL and in-process native UI actions. Header money probe DTO-only. No physical keyboard/visual acceptance. Paid fixture history retained, actors disabled.",Results},new JsonSerializerOptions{WriteIndented=true}));
        }
        return failed==0?0:1;
    }
}

