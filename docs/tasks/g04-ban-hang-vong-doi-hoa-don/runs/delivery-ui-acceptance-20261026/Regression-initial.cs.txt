using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using CafeManager.Models.Requests.Sales;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Navigation;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

internal static class Program
{
    private static readonly List<object> Results=[];
    private static int failed;
    private static void Check(string name,bool ok,object? actual=null){if(!ok)failed++;Results.Add(new{Name=name,Outcome=ok?"PASS":"FAIL",Actual=actual});}
    private static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o)!;
    private static IEnumerable<Control> Children(Control root){foreach(Control c in root.Controls){yield return c;foreach(var d in Children(c))yield return d;}}
    private static OrbitDialogForm New(string type,params object?[] args)=>(OrbitDialogForm)Activator.CreateInstance(typeof(MainForm).Assembly.GetType("CafeManager.WinForms.Modules.Sales."+type)!,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,args,null)!;
    private static async Task Wait(Func<bool> predicate){for(var i=0;i<200;i++){if(predicate())return;await Task.Delay(50);}throw new TimeoutException("Dialog readiness deadline");}
    private static async Task Close(OrbitDialogForm form,Task<DialogResult> task)
    {
        if(form.Visible) form.Close(); await Wait(()=>task.IsCompleted || Application.OpenForms.OfType<OrbitConfirmationDialog>().Any(f=>f.Visible));
        var confirmation=Application.OpenForms.OfType<OrbitConfirmationDialog>().LastOrDefault();
        if(confirmation is not null) Children(confirmation).OfType<Button>().Single(b=>b.Text is "Đồng ý" or "Có").PerformClick();
        await task.WaitAsync(TimeSpan.FromSeconds(5));
    }
    [STAThread] private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();Application.SetColorMode(SystemColorMode.Dark);
        const string db="CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout";
        using var owner=new SqlConnection($"Server=localhost;Initial Catalog={db};Integrated Security=True;TrustServerCertificate=True;Pooling=False");owner.Open();
        object? Scalar(string sql){using var cmd=owner.CreateCommand();cmd.CommandText=sql;return cmd.ExecuteScalar();}
        void Execute(string sql,string? hash=null){using var cmd=owner.CreateCommand();cmd.CommandText=sql;if(hash is not null)cmd.Parameters.AddWithValue("@hash",hash);cmd.ExecuteNonQuery();}
        if((string?)Scalar("SELECT RunId FROM dbo.G04TestMarker")!="20261025_181739_98e5df59")throw new InvalidOperationException("Marker mismatch");
        var runtime=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CHECKOUT_SQL_RUNTIME")??throw new InvalidOperationException("Runtime required"));
        if(runtime.DataSource!="localhost"||runtime.InitialCatalog!=db||runtime.IntegratedSecurity)throw new InvalidOperationException("Runtime target mismatch");
        var f=new ConnectionFactory(runtime.ConnectionString);var auth=new AuthRepository(f);var permission=new PermissionService(auth);
        var sales=new SalesService(new SalesRepository(f),permission);var customers=new CustomerService(new CustomerRepository(f),permission);var loyalty=new LoyaltyService(new LoyaltyRepository(f),permission);var invoices=new InvoiceService(new InvoiceRepository(f),permission);var catalogs=new CatalogService(new CatalogRepository(f),permission);
        var password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        const string financial="SELECT (SELECT (SELECT * FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH) Orders,(SELECT * FROM dbo.CHITIET_THANHTOAN ORDER BY MaTT FOR JSON PATH) Payments,(SELECT * FROM dbo.TIEUHAO_HOADON ORDER BY MaHD,MaNVL FOR JSON PATH) Consumption,(SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) Stock,(SELECT MaKH,DiemTichLuy FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH) Points FOR JSON PATH)";
        var before=(string)Scalar(financial)!;
        var provisioned=false;
        try
        {
            if(Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.NHANVIEN WHERE MaNV IN ('NNA01','NNC01','NNS01')"))!=0)throw new InvalidOperationException("Fixture actor collision");
            Execute("BEGIN TRAN;INSERT dbo.NHANVIEN(MaNV,TenNV,LoaiHopDong,MaCV) SELECT v.code,N'Embedded read fixture','Full-time',(SELECT MIN(MaCV) FROM dbo.CHUCVU) FROM (VALUES('NNA01'),('NNC01'),('NNS01'))v(code);INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT,HoatDong) VALUES('NNA01','nd.fixture.admin',@hash,'ADMIN',1),('NNC01','nd.fixture.cash',@hash,'CASH',1),('NNS01','nd.fixture.store',@hash,'STORE',1);COMMIT;",new PasswordService().Hash(password));provisioned=true;
            using var admin=new AuthService(auth,new PasswordService()).LoginAsync(new LoginRequest("nd.fixture.admin",password)).GetAwaiter().GetResult();
            using var cash=new AuthService(auth,new PasswordService()).LoginAsync(new LoginRequest("nd.fixture.cash",password)).GetAwaiter().GetResult();
            using var store=new AuthService(auth,new PasswordService()).LoginAsync(new LoginRequest("nd.fixture.store",password)).GetAwaiter().GetResult();
            foreach(var pair in new[]{(admin,new[]{NavigationTarget.Sales,NavigationTarget.Invoices,NavigationTarget.Delivery,NavigationTarget.Customers,NavigationTarget.Tables,NavigationTarget.Products,NavigationTarget.Materials,NavigationTarget.Loyalty}),(cash,new[]{NavigationTarget.Sales,NavigationTarget.Invoices,NavigationTarget.Delivery,NavigationTarget.Customers,NavigationTarget.Materials}),(store,new[]{NavigationTarget.Materials,NavigationTarget.Products})})
            {
                var modules=Enum.GetValues<NavigationTarget>().ToDictionary(x=>x,x=>(Func<UserControl>)(()=>new UserControl()));
                using var roleForm=new MainForm(pair.Item1,permission,_=>throw new InvalidOperationException("Not loading SystemInfo"),modules:modules);
                var actual=Children(roleForm).OfType<Button>().Where(b=>b.Tag is NavigationTarget).Select(b=>(NavigationTarget)b.Tag!).Order().ToArray();
                Check("ND11 actual MainForm menu "+pair.Item1.Identity.Role,actual.SequenceEqual(pair.Item2.Order()),actual);
            }
            using var main=new MainForm{Text="ND real SQL dialogs — component runner"};
            using var deadline=new System.Windows.Forms.Timer{Interval=100000};
            deadline.Tick+=(_,_)=>{Check("Runner deadline",false);main.Dispose();Application.ExitThread();};
            main.Shown+=async(_,_)=>
            {
                deadline.Start();
                async Task Inspect(string label,OrbitDialogForm dialog,Func<OrbitDialogForm,Task>? action=null)
                {
                    using(dialog)
                    {
                        try
                        {
                            var task=OrbitDialogHost.ShowAsync(dialog,main);await Wait(()=>dialog.Visible&&dialog.Modal);
                            Check(label+" native owner/modal",dialog.TopLevel&&dialog.Modal&&!dialog.ShowInTaskbar&&dialog.Owner==main&&dialog.FormBorderStyle!=FormBorderStyle.None);
                            foreach(var size in new[]{new Size(1280,720),new Size(1366,768)})
                            {
                                main.ClientSize=size;await Task.Yield();
                                Check(label+" bounds "+size,Screen.FromControl(dialog).WorkingArea.Contains(dialog.Bounds),new{Dpi=dialog.DeviceDpi,Size=dialog.Size.ToString()});
                            }
                            if(action is not null)await action(dialog);
                            if(!task.IsCompleted)await Close(dialog,task);
                            Check(label+" closes to idle",!main.Dialogs.IsOpen);
                        }
                        catch(Exception ex){Check(label,false,(ex.InnerException??ex).ToString());main.Dialogs.AbortAll();}
                    }
                }
                try
                {
                    await Inspect("OrderLine",New("OrderLineDialog","Test món",1,"Ghi chú tiếng Việt"));
                    var open=await sales.GetAsync(cash,"HD20250476")??throw new InvalidOperationException("Missing open fixture");
                    var paid=await invoices.DetailAsync(cash,"HD20250421")??throw new InvalidOperationException("Missing paid fixture");
                    var delivery=await invoices.DetailAsync(cash,"HD20250474")??throw new InvalidOperationException("Missing delivery fixture");
                    foreach(var channel in new[]{"Mang di","Tai quan","Giao hang"})
                    {
                        var input=new OrderHeaderInput(channel,TableCode:channel=="Tai quan"?"B0001":null,Recipient:channel=="Giao hang"?"G05 kiểm thử":null,Phone:channel=="Giao hang"?"0900000026":null,Address:channel=="Giao hang"?"Địa chỉ fixture DB test":null);
                        await Inspect("Header "+channel,New("OrderHeaderDialog",input,DateTime.Today,sales,customers,loyalty,cash),async d=>
                        {
                            await Wait(()=>Field<bool>(d,"ready"));
                            if(channel=="Giao hang")
                            {
                                foreach(var field in new[]{"recipient","phone","address"})
                                {
                                    var box=Field<TextBox>(d,field);var saved=box.Text;box.Text="";
                                    d.GetType().GetMethod("Save",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(d,null);
                                    Check("ND07 UI06 "+field,Field<ErrorProvider>(d,"errors").GetError(box).Length>0&&box.Focused&&d.DialogResult!=DialogResult.OK);
                                    box.Text=saved;
                                }
                            }
                            d.GetType().GetMethod("Save",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(d,null);
                            await Task.Yield();
                            Check("Header typed result "+channel,(OrderHeaderInput?)d.GetType().GetProperty("Result")!.GetValue(d)==input);
                        });
                    }
                    await Inspect("OpenOrders",New("OpenOrdersDialog",sales,cash),async d=>{await Wait(()=>Field<bool>(d,"ready"));Check("ND08 open orders loaded",Field<DataGridView>(d,"grid").Rows.Count>0);});
                    await Inspect("Invoice paid",New("InvoiceDetailDialog",paid,invoices,cash),d=>{Check("ND06 paid POS disabled",!Children(d).OfType<Button>().Single(b=>b.Text=="Quay lại POS").Enabled);return Task.CompletedTask;});
                    await Inspect("Delivery",New("DeliveryDetailDialog",delivery));
                    var preview=await sales.PreviewAsync(cash,open.Header);
                    await Inspect("Checkout",New("CheckoutDialog",sales,cash,open,preview,await sales.PaymentMethodsAsync(cash)),async d=>
                    {
                        var tendered=Field<TextBox>(d,"tendered");var confirm=Field<Button>(d,"confirm");
                        foreach(var value in new[]{"80000","abc","999999999999999999999999999999"}){tendered.Text=value;await Task.Yield();Check("ND09 tendered invalid "+value,!confirm.Enabled&&tendered.Text==value);}
                        tendered.Text="100000";await Task.Yield();Check("ND09 valid tendered enabled",confirm.Enabled);
                    });
                    await Inspect("CustomerPicker",new CustomerPickerDialog(customers,cash,true));
                    foreach(var kind in Enum.GetValues<CatalogKind>()) await Inspect("Catalog "+kind,new CatalogEditorDialog(catalogs,admin,kind,null));
                    using(var editor=new OperationEditorDialog("Nested picker",_=>Task.FromResult<CafeManager.Models.Dtos.Inventory.OperationReceipt?>(null)))
                    {
                        var value=editor.AddText("Nháp","Giữ nguyên");var task=OrbitDialogHost.ShowAsync(editor,main);await Wait(()=>editor.Visible&&editor.Modal);
                        using var picker=new CustomerPickerDialog(customers,cash,true);var nested=OrbitDialogHost.ShowAsync(picker,editor);await Wait(()=>picker.Visible&&picker.Modal);
                        picker.Close();await nested.WaitAsync(TimeSpan.FromSeconds(5));
                        Check("ND03 production picker preserves parent input",value.Text=="Giữ nguyên"&&editor.Enabled);
                        await Close(editor,task);
                    }
                    await Inspect("Invoice preview",new InvoicePrintPreviewForm(paid,invoices,cash));
                    var sharedRoots=new UserControl[]{
                        new CafeManager.WinForms.Modules.Customers.CustomersControl(customers,admin),
                        new CafeManager.WinForms.Modules.Menu.ProductsControl(new ProductService(new ProductRepository(f),permission),admin),
                        new CafeManager.WinForms.Modules.Tables.TablesControl(new TableService(new TableRepository(f),permission),admin),
                        new CafeManager.WinForms.Modules.Loyalty.LoyaltyControl(loyalty,customers,admin)};
                    foreach(var root in sharedRoots)
                    {
                        using(root)
                        {
                            var content=Field<Panel>(main,"content");root.Dock=DockStyle.Fill;content.Controls.Add(root);root.BringToFront();await Task.Delay(150);
                            var pages=Children(root).OfType<OrbitRecordListControl>().Concat(root is OrbitRecordListControl record?new[]{record}:Array.Empty<OrbitRecordListControl>()).ToArray();
                            foreach(var list in pages)
                            {
                                await (Task)typeof(OrbitRecordListControl).GetMethod("ResetAndReload",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(list,null)!;
                                await Wait(()=>!(bool)typeof(OrbitRecordListControl).GetField("busy",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(list)!);
                                var workspace=(OrbitListWorkspace)typeof(OrbitRecordListControl).GetField("Workspace",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(list)!;
                                var listGrid=(OrbitDataGridView)typeof(OrbitRecordListControl).GetField("Grid",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(list)!;
                                Check("IH10 shared list "+list.GetType().Name,workspace.Status.Text.StartsWith("Đã cập nhật")&&!workspace.StackedTools&&workspace.SearchActions.Controls.Count==0&&listGrid.Profile!=OrbitGridProfile.InvoiceHistory,new{State=workspace.Status.Text,Rows=listGrid.Rows.Count,Profile=listGrid.Profile.ToString()});
                            }
                            content.Controls.Remove(root);
                        }
                    }
                    Check("ND11 no orphan forms after inventory",Application.OpenForms.Cast<Form>().All(x=>x==main));
                }
                catch(Exception ex){Check("Runner exception",false,(ex.InnerException??ex).ToString());}
                finally{deadline.Stop();main.Dispose();Application.ExitThread();}
            };
            Application.Run(main);
            Check("ND06 read/bounds/validation did not change financial data",before==(string?)Scalar(financial));
        }
        finally
        {
            if(provisioned)Execute("DELETE dbo.TAIKHOAN WHERE MaNV IN ('NNA01','NNC01','NNS01');DELETE dbo.NHANVIEN WHERE MaNV IN ('NNA01','NNC01','NNS01');");
            Check("Fixture actors removed",Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.NHANVIEN WHERE MaNV IN ('NNA01','NNC01','NNS01')"))==0);
            File.WriteAllText(Path.Combine(args[0],"dialog-results.json"),JsonSerializer.Serialize(new{Failed=failed,Database=db,Scope="Frozen product DLL; real AuthService/runtime SQL; component calls, no physical input/visual QA/checkout commit",Results},new JsonSerializerOptions{WriteIndented=true}));
        }
        return failed==0?0:1;
    }
}




