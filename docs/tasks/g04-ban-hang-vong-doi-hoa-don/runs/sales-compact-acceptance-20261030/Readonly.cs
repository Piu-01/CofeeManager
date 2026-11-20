using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Sales;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Sales;
using CafeManager.WinForms.UI;
using CafeManager.WinForms.Theme;
using Microsoft.Data.SqlClient;

internal static class Program
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool IsWindowEnabled(IntPtr h);
    static readonly List<object> Results=[];static int failed;static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static void Check(string n,bool ok,object? actual=null){Results.Add(new{Name=n,Outcome=ok?"PASS":"FAIL",Actual=actual});if(!ok)failed++;}
    static T F<T>(object o,string n)=>(T)o.GetType().GetField(n,Flags)!.GetValue(o)!;
    static void Set(object o,string n,object? v)=>o.GetType().GetField(n,Flags)!.SetValue(o,v);
    static Task Call(object o,string n,params object?[] args)=>o.GetType().GetMethod(n,Flags)!.Invoke(o,args) as Task??Task.CompletedTask;
    static IEnumerable<Control> Children(Control c){foreach(Control x in c.Controls){yield return x;foreach(var y in Children(x))yield return y;}}
    static async Task Wait(Func<bool> p){for(int i=0;i<500;i++){if(p())return;await Task.Delay(25);}throw new TimeoutException("UI readiness deadline");}
    static async Task Idle(SalesControl p){await Task.Delay(100);await Wait(()=>!F<bool>(p,"busy"));}
    static Form? Open(string type)=>Application.OpenForms.Cast<Form>().LastOrDefault(f=>f.Visible&&f.GetType().Name==type);
    static async Task<Form> Dialog(string type){await Wait(()=>Open(type)!=null);return Open(type)!;}
    static void Key(Control c,Keys key){object[] args=[Message.Create(c.Handle,0,IntPtr.Zero,IntPtr.Zero),key];c.GetType().GetMethod("ProcessCmdKey",Flags)!.Invoke(c,args);}
    [STAThread]static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();Application.SetColorMode(SystemColorMode.Dark);
        const string db="CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout";
        using var owner=new SqlConnection($"Server=localhost;Initial Catalog={db};Integrated Security=True;TrustServerCertificate=True;Pooling=False");owner.Open();
        object? Scalar(string sql){using var c=owner.CreateCommand();c.CommandText=sql;return c.ExecuteScalar();}
        void Sql(string sql,string hash){using var c=owner.CreateCommand();c.CommandText=sql;c.Parameters.AddWithValue("@hash",hash);c.ExecuteNonQuery();}
        if((string?)Scalar("SELECT RunId FROM dbo.G04TestMarker")!="20261025_181739_98e5df59")throw new Exception("Marker mismatch");
        var cs=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CHECKOUT_SQL_RUNTIME")!);if(cs.DataSource!="localhost"||cs.InitialCatalog!=db)throw new Exception("Wrong target");
        var f=new ConnectionFactory(cs.ConnectionString);var auth=new AuthRepository(f);var permission=new PermissionService(auth);var sales=new SalesService(new SalesRepository(f),permission);var customers=new CustomerService(new CustomerRepository(f),permission);var loyalty=new LoyaltyService(new LoyaltyRepository(f),permission);var invoices=new InvoiceService(new InvoiceRepository(f),permission);var tables=new TableService(new TableRepository(f),permission);
        var password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));Sql("UPDATE dbo.TAIKHOAN SET MatKhauHash=@hash,HoatDong=1 WHERE MaNV IN('SCA30','SCC30','SCS30') AND TenDangNhap LIKE 'sc.fixture.%';",new PasswordService().Hash(password));
        const string snapshot="SELECT (SELECT (SELECT * FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH) Orders,(SELECT * FROM dbo.CHITIET_HD ORDER BY MaHD,MaDong FOR JSON PATH) Lines,(SELECT * FROM dbo.CHITIET_THANHTOAN ORDER BY MaTT FOR JSON PATH) Payments,(SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) Stock,(SELECT MaKH,DiemTichLuy FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH) Points,(SELECT * FROM dbo.G03OperationReceipt ORDER BY RequestId FOR JSON PATH) Receipts FOR JSON PATH)";
        var before=(string)Scalar(snapshot)!;File.WriteAllText(Path.Combine(args[0],"readonly-before.json"),before);
        try
        {
            using var cash=new AuthService(auth,new PasswordService()).LoginAsync(new("sc.fixture.cash",password)).GetAwaiter().GetResult();
            using var main=new MainForm{Text="POS compact frozen read/keyboard regression"};using var page=new SalesControl(sales,tables,customers,loyalty,cash,invoices);F<Panel>(main,"content").Controls.Add(page);
            main.Shown+=async(_,_)=>
            {
                try
                {
                    await Idle(page);var panel=F<Control>(page,"orderPanel");var grid=F<DataGridView>(panel,"Lines");var search=(TextBox)page.GetType().GetProperty("search",Flags)!.GetValue(page)!;
                    var open=(string)Scalar("SELECT TOP(1) MaHD FROM dbo.HOADON WHERE MaNV='SCC30' AND LoaiDon=N'Giao hang' AND TrangThai=N'Chua thanh toan' ORDER BY MaHD")!;await page.OpenOrderAsync(open);await Idle(page);
                    var original=F<SalesOrderDto>(page,"order");var selected=(grid.CurrentRow?.DataBoundItem as SalesLine)?.LineId;
                    main.ClientSize=new Size(1280,720);await Task.Yield();main.ClientSize=new Size(1366,768);await Task.Yield();Check("SC01 loaded order/selection retained on resize",F<SalesOrderDto>(page,"order").Header.Code==open&&(grid.CurrentRow?.DataBoundItem as SalesLine)?.LineId==selected);
                    search.Text="Không có món SC";F<ComboBox>(page,"category").Focus();Key(page,Keys.F2);await Task.Delay(80);Check("SC06 F2 focus/selectall",search.Focused&&search.SelectionLength==search.TextLength);
                    typeof(Control).GetMethod("OnKeyDown",Flags)!.Invoke(search,[new KeyEventArgs(Keys.Enter)]);await Task.Delay(150);await Call(page,"LoadMenu");Check("SC02 Enter searches without checkout",F<FlowLayoutPanel>(page,"products").Controls.Count==0&&F<SalesOrderDto>(page,"order").Header.IsOpen);
                    search.Text="";var category=(string)Scalar("SELECT MaDM FROM dbo.SANPHAM WHERE MaSP='SP001'")!;F<ComboBox>(page,"category").SelectedValue=category;await Call(page,"LoadMenu");Check("SC02 category typed filter",F<FlowLayoutPanel>(page,"products").Controls.Count==Math.Min(50,Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM dbo.SANPHAM WHERE MaDM='{category}'"))));
                    foreach(bool canAdd in new[]{false,true})
                    {
                        int clicks=0;using var card=(Control)Activator.CreateInstance(typeof(SalesControl).Assembly.GetType("CafeManager.WinForms.Modules.Sales.ProductCardControl")!,Flags,null,[new SalesMenuItem("SP001","Card fixture",81000,canAdd?"Dang ban":"Ngung ban",true),new EventHandler((_,_)=>clicks++)],null)!;
                        var button=F<Button>(card,"Add");button.PerformClick();Check("SC03 one callback/disabled "+canAdd,clicks==(canAdd?1:0));
                    }
                    Key(page,Keys.F3);var header=await Dialog("OrderHeaderDialog");await Wait(()=>F<bool>(header,"ready"));Check("SC06 F3 native owner",header.Owner==main&&header.Modal&&!header.ShowInTaskbar&&!IsWindowEnabled(main.Handle));Children(header).OfType<Button>().Single(b=>b.Text=="Quay lại").PerformClick();await Wait(()=>!main.Dialogs.IsOpen);
                    Key(page,Keys.F4);var orders=await Dialog("OpenOrdersDialog");await Wait(()=>F<bool>(orders,"ready"));F<TextBox>(orders,"search").Text=open;await Call(orders,"Reload");Check("SC04 F4 exact result",F<DataGridView>(orders,"grid").Rows.Count==1);F<Button>(orders,"open").PerformClick();await Wait(()=>!main.Dialogs.IsOpen);await Idle(page);Check("SC04 F4 returns same saved order",F<SalesOrderDto>(page,"order").Header.Code==open);
                    Key(page,Keys.F8);var checkout=await Dialog("CheckoutDialog");var paidCount=Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan'"));Check("SC06 F8 native owner and no AcceptButton",checkout.Owner==main&&checkout.Modal&&!checkout.ShowInTaskbar&&checkout.AcceptButton==null);
                    typeof(Form).GetMethod("ProcessDialogKey",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(checkout,[Keys.Enter]);await Task.Delay(80);Check("SC06 Enter did not settle",Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan'"))==paidCount);
                    if(checkout.Visible){checkout.Close();await Wait(()=>checkout.IsDisposed||Open(nameof(OrbitConfirmationDialog))!=null);var ask=Open(nameof(OrbitConfirmationDialog));if(ask!=null){Children(ask).OfType<Button>().Single(b=>b.Text=="Đồng ý").PerformClick();await Wait(()=>!ask.Visible);}}await Wait(()=>!main.Dialogs.IsOpen);await Idle(page);
                    Set(page,"order",original with{Header=original.Header with{PreviewValid=false}});await Call(panel,"ShowOrder",F<SalesOrderDto>(page,"order"));await Call(page,"SetBusy",false);Check("SC05 invalid preview locks pay and hides total",!F<Button>(page,"pay").Enabled&&F<Label>(panel,"Totals").Text=="—");Set(page,"order",original);await Call(panel,"ShowOrder",original);
                    Set(page,"unknown",true);await Call(page,"SetBusy",false);Check("SC06 unknown blocks edits/pay but allows reconcile",!F<Button>(page,"pay").Enabled&&!F<Button>(page,"edit").Enabled&&F<Button>(page,"reconcile").Enabled&&F<Label>(panel,"Totals").Text=="—");Set(page,"unknown",false);await Call(page,"SetBusy",false);
                    var huge=999999999999999999m;await Call(panel,"ShowOrder",original with{Header=original.Header with{Total=huge,Payable=huge},Lines=new[]{new SalesLine(999,"SP001","Money probe",1,huge,"",huge)}});var amount=grid.Columns["Amount"]!;var priceText="ĐG "+huge.ToString("#,##0' ₫'",System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));var amountText=huge.ToString("#,##0' ₫'",System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));var measured=Math.Max(TextRenderer.MeasureText(priceText,OrbitManagementTheme.DrawingFont(grid,OrbitManagementTheme.Small),Size.Empty,TextFormatFlags.NoPadding).Width,TextRenderer.MeasureText(amountText,grid.DefaultCellStyle.Font,Size.Empty,TextFormatFlags.NoPadding).Width);Check("SC01 full large line price/amount fits",measured+amount.DefaultCellStyle.Padding.Horizontal<=amount.Width,new{ColumnWidth=amount.Width,Needed=measured+amount.DefaultCellStyle.Padding.Horizontal});await Call(panel,"ShowOrder",original);
                    var paid=(string)Scalar("SELECT TOP(1) MaHD FROM dbo.HOADON WHERE MaNV='SCC30' AND TrangThai=N'Da thanh toan' ORDER BY MaHD")!;await page.OpenOrderAsync(paid);Check("SC06 paid order financial actions locked",new[]{"pay","header","edit","delete","cancel"}.All(n=>!F<Button>(page,n).Enabled));Check("SC05 paid Payable remains actual",F<Label>(panel,"Totals").Text.Contains(F<SalesOrderDto>(page,"order").Header.Payable.ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))));
                }
                catch(Exception e){Check("Readonly runner exception",false,(e.InnerException??e).ToString());}
                finally{main.Dialogs.AbortAll();main.Dispose();Application.ExitThread();}
            };
            Application.Run(main);
        }
        finally
        {
            Sql("UPDATE dbo.TAIKHOAN SET HoatDong=0 WHERE MaNV IN('SCA30','SCC30','SCS30');","");var after=(string)Scalar(snapshot)!;File.WriteAllText(Path.Combine(args[0],"readonly-after.json"),after);Check("Readonly/keyboard tests change no financial data",before==after);
            File.WriteAllText(Path.Combine(args[0],"readonly-results.json"),JsonSerializer.Serialize(new{Failed=failed,Scope="Frozen binary, real SQL/Auth; keyboard handler/native modal component actions; money stress DTO only; no physical input/visual QA/financial writes",Results},new JsonSerializerOptions{WriteIndented=true}));
        }
        return failed==0?0:1;
    }
}
