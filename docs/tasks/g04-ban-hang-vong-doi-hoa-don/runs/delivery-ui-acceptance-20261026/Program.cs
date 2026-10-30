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
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool IsWindowEnabled(IntPtr h);
    static readonly List<object> Results=[];static int failed;
    static void Check(string name,bool ok,object? actual=null){Results.Add(new{Name=name,Outcome=ok?"PASS":"FAIL",Actual=actual});if(!ok)failed++;}
    static FieldInfo Info(object o,string name){for(var t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);if(f!=null)return f;}throw new MissingFieldException(name);}
    static T F<T>(object o,string n)=>(T)Info(o,n).GetValue(o)!;
    static Task Call(object o,string name){for(var t=o.GetType();t!=null;t=t.BaseType){var m=t.GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);if(m!=null)return(Task)m.Invoke(o,null)!;}throw new MissingMethodException(name);}
    static IEnumerable<Control> Children(Control c){foreach(Control x in c.Controls){yield return x;foreach(var y in Children(x))yield return y;}}
    static async Task Wait(Func<bool> p){for(int i=0;i<400;i++){if(p())return;await Task.Delay(25);}throw new TimeoutException("Readiness deadline");}
    static async Task Idle(object p){await Task.Delay(80);await Wait(()=>!F<bool>(p,"busy"));}
    [STAThread] static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();Application.SetColorMode(SystemColorMode.Dark);
        const string db="CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout";
        using var owner=new SqlConnection($"Server=localhost;Initial Catalog={db};Integrated Security=True;TrustServerCertificate=True;Pooling=False");owner.Open();
        object? Scalar(string s){using var c=owner.CreateCommand();c.CommandText=s;return c.ExecuteScalar();}
        void Sql(string s,string? hash=null){using var c=owner.CreateCommand();c.CommandText=s;if(hash!=null)c.Parameters.AddWithValue("@hash",hash);c.ExecuteNonQuery();}
        if((string?)Scalar("SELECT RunId FROM dbo.G04TestMarker")!="20261025_181739_98e5df59")throw new Exception("Marker mismatch");
        var cs=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CHECKOUT_SQL_RUNTIME")!);if(cs.DataSource!="localhost"||cs.InitialCatalog!=db||cs.IntegratedSecurity)throw new Exception("Runtime target mismatch");
        var factory=new ConnectionFactory(cs.ConnectionString);var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);var delivery=new DeliveryService(new DeliveryRepository(factory),permissions);var sales=new SalesService(new SalesRepository(factory),permissions);var invoices=new InvoiceService(new InvoiceRepository(factory),permissions);
        const string snapshot="SELECT (SELECT (SELECT * FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH) Orders,(SELECT * FROM dbo.CHITIET_HD ORDER BY MaHD,MaDong FOR JSON PATH) Lines,(SELECT * FROM dbo.CHITIET_THANHTOAN ORDER BY MaTT FOR JSON PATH) Payments,(SELECT * FROM dbo.TIEUHAO_HOADON ORDER BY MaHD,MaNVL FOR JSON PATH) Consumption,(SELECT * FROM dbo.HoaDonUuDaiSnapshot ORDER BY MaHD FOR JSON PATH) Discounts,(SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) Stock,(SELECT MaKH,DiemTichLuy FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH) Points,(SELECT * FROM dbo.G03OperationReceipt ORDER BY RequestId FOR JSON PATH) Receipts FOR JSON PATH)";
        var before=(string)Scalar(snapshot)!;File.WriteAllText(Path.Combine(args[0],"before.json"),before);
        bool provisioned=false;var password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        try
        {
            if(Convert.ToInt32(Scalar("SELECT (SELECT COUNT(*) FROM dbo.HOADON WHERE MaHD LIKE 'DG26%' OR MaHD IN ('HD96260001','HD96260002') OR SDTNguoiNhan LIKE '098762%')+(SELECT COUNT(*) FROM dbo.NHANVIEN WHERE MaNV IN ('DGA01','DGC01','DGS01'))"))!=0)throw new Exception("Fixture collision");
            Sql("BEGIN TRAN;INSERT dbo.NHANVIEN(MaNV,TenNV,LoaiHopDong,MaCV) SELECT v.code,N'Delivery UI fixture','Full-time',(SELECT MIN(MaCV) FROM dbo.CHUCVU) FROM(VALUES('DGA01'),('DGC01'),('DGS01'))v(code);INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT,HoatDong) VALUES('DGA01','dg.fixture.admin',@hash,'ADMIN',1),('DGC01','dg.fixture.cash',@hash,'CASH',1),('DGS01','dg.fixture.store',@hash,'STORE',1);WITH n AS(SELECT 1 n UNION ALL SELECT n+1 FROM n WHERE n<101) INSERT dbo.HOADON(MaHD,NgayLap,MaNV,LoaiDon,TenNguoiNhan,SDTNguoiNhan,DiaChiGiao,TrangThaiGiao) SELECT 'DG26'+RIGHT('000000'+CONVERT(varchar,n),6),DATEADD(second,n,CONVERT(datetime2,'20010101')),'DGC01',N'Giao hang',N'DG kiểm thử người nhận dài',CASE WHEN n=1 THEN '098762000000001' WHEN n<=49 THEN '09876200001'+RIGHT('000'+CONVERT(varchar,n),3) WHEN n=50 THEN '098762000100050' WHEN n=51 THEN '098762001000051' WHEN n<=100 THEN '098762010000'+RIGHT('000'+CONVERT(varchar,n),3) ELSE '09876210000101' END,N'Địa chỉ fixture UI giao hàng, dùng chi tiết để xem toàn bộ nội dung dài',N'Cho giao' FROM n OPTION(MAXRECURSION 101);COMMIT;",new PasswordService().Hash(password));provisioned=true;
            using var cash=new AuthService(auth,new PasswordService()).LoginAsync(new("dg.fixture.cash",password)).GetAwaiter().GetResult();using var admin=new AuthService(auth,new PasswordService()).LoginAsync(new("dg.fixture.admin",password)).GetAwaiter().GetResult();using var store=new AuthService(auth,new PasswordService()).LoginAsync(new("dg.fixture.store",password)).GetAwaiter().GetResult();
            foreach(var code in new[]{"HD96260001","HD96260002"})sales.CreateAsync(cash,new(code,Guid.NewGuid(),new("Giao hang",Recipient:"DG chuyển giao",Phone:"0999626001",Address:"Địa chỉ fixture chuyển giao"),"SP001",1,"DG frozen UI acceptance")).GetAwaiter().GetResult();
            string Finance()=> (string)Scalar("SELECT (SELECT (SELECT MaHD,NgayLap,MaNV,MaKH,MaBan,MaDT,MaKM,LoaiDon,TenNguoiNhan,SDTNguoiNhan,DiaChiGiao,NgayThanhToan,PhanTramPhiDoiTac,PhiDoiTac,TongTien,PhiShip,GiamGia,TrangThai,ThanhTien FROM dbo.HOADON WHERE MaHD IN ('HD96260001','HD96260002') ORDER BY MaHD FOR JSON PATH) Headers,(SELECT * FROM dbo.CHITIET_HD WHERE MaHD IN ('HD96260001','HD96260002') ORDER BY MaHD,MaDong FOR JSON PATH) Lines,(SELECT * FROM dbo.CHITIET_THANHTOAN ORDER BY MaTT FOR JSON PATH) Payments,(SELECT * FROM dbo.TIEUHAO_HOADON ORDER BY MaHD,MaNVL FOR JSON PATH) Consumption,(SELECT * FROM dbo.HoaDonUuDaiSnapshot ORDER BY MaHD FOR JSON PATH) Discounts,(SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) Stock,(SELECT MaKH,DiemTichLuy FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH) Points FOR JSON PATH)")!;
            var beforeTransitions=Finance();File.WriteAllText(Path.Combine(args[0],"before-transitions.json"),beforeTransitions);
            using var main=new MainForm{Text="Delivery UI real SQL component acceptance"};using var page=new DeliveryControl(delivery,sales,invoices,cash){Dock=DockStyle.Fill};F<Panel>(main,"content").Controls.Add(page);
            using var deadline=new System.Windows.Forms.Timer{Interval=180000};deadline.Tick+=(_,_)=>{Check("Runner deadline",false);main.Dispose();Application.ExitThread();};
            main.Shown+=async(_,_)=>
            {
                deadline.Start();
                try
                {
                    await Idle(page);var ws=F<OrbitListWorkspace>(page,"Workspace");var grid=F<OrbitDataGridView>(page,"Grid");var search=Children(ws).OfType<TextBox>().Single(x=>x.AccessibleName=="Tìm đơn giao hàng");var status=F<ComboBox>(page,"status");var payment=F<ComboBox>(page,"payment");var submit=Children(ws).OfType<Button>().Single(x=>x.AccessibleName=="Tìm kiếm");var detail=Children(ws).OfType<Button>().Single(x=>x.Text=="Chi tiết / Bước tiếp");var reconcile=Children(ws).OfType<Button>().Single(x=>x.Text=="Kiểm tra kết quả");
                    Check("DG01 native DPI125 and current-status header",main.DeviceDpi==120&&ws.Title.Text=="Giao hàng"&&ws.Description.Text.Contains("Chỉ lưu trạng thái hiện tại"));Check("DG05 reconcile initially locked",!reconcile.Enabled);
                    foreach(var test in new[]{(0,"098762999"),(1,"09876200000"),(49,"0987620000"),(50,"098762000"),(51,"09876200"),(100,"0987620"),(101,"098762")})
                    {
                        search.Text=test.Item2;submit.PerformClick();await Idle(page);Check($"DG04 SQL total {test.Item1}",F<int?>(page,"totalCount")==test.Item1&&grid.Rows.Count==Math.Min(test.Item1,50),new{Total=F<int?>(page,"totalCount"),Rows=grid.Rows.Count,Range=ws.Pagination.Range.Text});Check($"DG04 first controls {test.Item1}",!ws.Pagination.Previous.Enabled&&ws.Pagination.Next.Enabled==(test.Item1>50));
                        if(test.Item1>50){while(ws.Pagination.Next.Enabled){ws.Pagination.Next.PerformClick();await Idle(page);}Check($"DG04 final controls {test.Item1}",grid.Rows.Count==(test.Item1%50==0?50:test.Item1%50)&&!ws.Pagination.Next.Enabled&&ws.Pagination.Previous.Enabled,ws.Pagination.Range.Text);}
                    }
                    search.Text="DG26";await Call(page,"SubmitSearch");await Idle(page);search.Text="CHUAGUI";payment.SelectedValue="Chua thanh toan";await Idle(page);Check("DG03 filter ignores draft",F<int?>(page,"totalCount")==101&&F<string>(page,"appliedSearch")=="DG26"&&search.Text=="CHUAGUI");
                    ws.Pagination.Next.PerformClick();await Idle(page);Check("DG03 paging ignores draft",F<int>(page,"page")==1&&grid.Rows.Count==50);F<OrbitButton>(page,"refresh").PerformClick();await Idle(page);Check("DG03 refresh ignores draft",F<int>(page,"page")==0&&F<int?>(page,"totalCount")==101);
                    typeof(Control).GetMethod("OnKeyDown",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(search,[new KeyEventArgs(Keys.Enter)]);await Idle(page);Check("DG02 Enter submits without transition",grid.Rows.Count==0&&!detail.Enabled&&F<string>(page,"appliedSearch")=="CHUAGUI");
                    search.Text="DG26";await Call(page,"SubmitSearch");await Idle(page);status.SelectedValue="Dang giao";await Idle(page);Check("DG03 progress auto",grid.Rows.Count==0);status.SelectedIndex=0;await Idle(page);Check("DG03 progress clear auto",F<int?>(page,"totalCount")==101);payment.SelectedValue="Da thanh toan";await Idle(page);Check("DG03 payment auto",grid.Rows.Count==0);payment.SelectedIndex=0;await Idle(page);
                    search.Text="";Check("DG02 clearing draft does not query",F<int?>(page,"totalCount")==101);await Call(page,"SubmitSearch");await Idle(page);Check("DG02 clearing submit removes keyword",F<int?>(page,"totalCount")>101);search.Text="DG26000001";await Call(page,"SubmitSearch");await Idle(page);Check("DG02 exact code, Phone string and readonly",grid.Rows.Count==1&&((DeliveryRow)grid.Rows[0].DataBoundItem!).Phone=="098762000000001"&&grid.ReadOnly&&grid.Profile==OrbitGridProfile.Delivery);
                    var phoneBox=search;F<ComboBox>(page,"status").Focus();phoneBox.Focus();await Task.Delay(100);Check("DG05 select all search on entry",phoneBox.SelectionLength==phoneBox.TextLength);Check("DG05 full code and address fill",grid.Columns["Code"]!.Frozen&&grid.Columns["Address"]!.AutoSizeMode==DataGridViewAutoSizeColumnMode.Fill);
                    var queryField=Info(page,"query");var original=queryField.GetValue(page);var row=(DeliveryRow)grid.Rows[0].DataBoundItem!;var old=new TaskCompletionSource<IList>();var newer=new TaskCompletionSource<IList>();int reads=0;queryField.SetValue(page,(Func<string,int,CancellationToken,Task<IList>>)((q,s,t)=>++reads==1?old.Task:newer.Task));var first=Call(page,"Reload");var second=Call(page,"Reload");newer.SetResult(new List<DeliveryRow>{row with{Code="NEW",TotalCount=1}});await second;old.SetResult(new List<DeliveryRow>{row with{Code="OLD",TotalCount=1}});await first;Check("DG03 late query ignored",((DeliveryRow)grid.Rows[0].DataBoundItem!).Code=="NEW");
                    queryField.SetValue(page,(Func<string,int,CancellationToken,Task<IList>>)((q,s,t)=>throw new ArgumentException("Controlled read fault")));await Call(page,"Reload");Check("DG05 error no stale detail or fake zero",grid.Rows.Count==0&&!detail.Enabled&&!grid.Enabled&&ws.Pagination.Range.Text=="Chưa tải được dữ liệu",ws.Status.Text);
                    queryField.SetValue(page,(Func<string,int,CancellationToken,Task<IList>>)((q,s,t)=>Task.FromCanceled<IList>(new CancellationToken(true))));await Call(page,"Reload");Check("DG05 cancel explicit",ws.Status.Text.Contains("Đã dừng tải")&&!detail.Enabled);
                    var skips=new List<int>();queryField.SetValue(page,(Func<string,int,CancellationToken,Task<IList>>)((q,s,t)=>{skips.Add(s);return Task.FromResult<IList>(s>0?new List<DeliveryRow>():new List<DeliveryRow>{row with{TotalCount=1}});}));Info(page,"page").SetValue(page,2);await Call(page,"Reload");Check("DG04 stale empty fallback",skips.SequenceEqual(new[]{100,0})&&F<int>(page,"page")==0&&!ws.Pagination.Next.Enabled);queryField.SetValue(page,original);
                    // Actual native dialogs and actual write service, no checkout.
                    async Task Advance(bool approve)
                    {
                        search.Text="HD96260001";await Call(page,"SubmitSearch");await Idle(page);var task=Call(page,"Detail");await Wait(()=>Application.OpenForms.Cast<Form>().Any(x=>x.GetType().Name=="DeliveryDetailDialog"));var dialog=Application.OpenForms.Cast<Form>().Single(x=>x.GetType().Name=="DeliveryDetailDialog");
                        Check("DG06 detail owner/title/modal",dialog.Modal&&dialog.Owner==main&&!dialog.ShowInTaskbar&&!IsWindowEnabled(main.Handle));var start=(await delivery.GetAsync(cash,"HD96260001"))!.Order.Header.Delivery;
                        Children(dialog).OfType<Button>().Single(b=>b.Text.StartsWith("Chuyển →")).PerformClick();await Wait(()=>Application.OpenForms.OfType<OrbitConfirmationDialog>().Any(x=>x.Visible));var confirmation=Application.OpenForms.OfType<OrbitConfirmationDialog>().Single(x=>x.Visible);Check("DG06 nested confirmation owner",confirmation.Owner==dialog&&!IsWindowEnabled(dialog.Handle));Children(confirmation).OfType<Button>().Single(b=>b.Text==(approve?"Đồng ý":"Hủy")).PerformClick();
                        if(!approve){await Wait(()=>!Application.OpenForms.OfType<OrbitConfirmationDialog>().Any(x=>x.Visible));Check("DG06 cancel no transition",(await delivery.GetAsync(cash,"HD96260001"))!.Order.Header.Delivery==start);dialog.Close();}
                        await task.WaitAsync(TimeSpan.FromSeconds(10));await Idle(page);Check("DG06 transition "+(approve?start+" next":"cancel"),(await delivery.GetAsync(cash,"HD96260001"))!.Order.Header.Delivery==(approve?DeliveryService.Next(start!):start));
                    }
                    await Advance(false);await Advance(true);await Advance(true);
                    Check("DG06 delivered independent of payment",(await delivery.GetAsync(cash,"HD96260001"))!.Order.Header is{Delivery:"Da giao",Status:"Chua thanh toan"});
                    // Two authenticated calls acquire distinct SQL connections against same revision.
                    var h=(await delivery.GetAsync(cash,"HD96260002"))!.Order.Header;var request=Guid.NewGuid();var other=Guid.NewGuid();async Task<string> Change(SessionContext s,Guid id){try{await delivery.ChangeAsync(s,new(h.Code,h.Revision,id,h.Delivery!,"Dang giao"));return "OK";}catch(UserOperationException e){return e.Error.Number.ToString();}}
                    var outcomes=await Task.WhenAll(Change(cash,request),Change(admin,other));Check("DG07 two sessions one winner/stale conflict",outcomes.Count(x=>x=="OK")==1&&outcomes.Count(x=>x=="52103")==1,outcomes);var winning=outcomes[0]=="OK"?request:other;var winner=outcomes[0]=="OK"?cash:admin;
                    // Simulate lost acknowledgement in component state only; SQL commit/receipt is real.
                    var pendingPage=page;if(winner!=cash){Check("DG07 winner admin receipt readable to admin",await sales.ReconcileAsync(admin,winning,h.Code,"SalesDelivery")!=null);}
                    // Runtime reconcile checks actor, so use a cash-owned new request on next valid step if admin won.
                    if(winner!=cash){var current=(await delivery.GetAsync(cash,h.Code))!.Order.Header;winning=Guid.NewGuid();await delivery.ChangeAsync(cash,new(h.Code,current.Revision,winning,current.Delivery!,"Da giao"));}
                    Info(pendingPage,"requestId").SetValue(pendingPage,winning);Info(pendingPage,"pendingCode").SetValue(pendingPage,h.Code);Info(pendingPage,"unknown").SetValue(pendingPage,true);search.Text="NO-DG-ROWS";await Call(page,"SubmitSearch");await Idle(page);Check("DG07 unknown no row still reconciles",grid.Rows.Count==0&&!detail.Enabled&&reconcile.Enabled&&F<Guid>(page,"requestId")==winning&&F<string>(page,"pendingCode")==h.Code);
                    var receiptCount=Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE Operation='SalesDelivery' AND EntityCode='HD96260002'"));await Call(page,"Reconcile");await Idle(page);Check("DG07 reconcile no repeat and unlock",!F<bool>(page,"unknown")&&!reconcile.Enabled&&Convert.ToInt32(Scalar("SELECT COUNT(*) FROM dbo.G03OperationReceipt WHERE Operation='SalesDelivery' AND EntityCode='HD96260002'"))==receiptCount&&ws.Status.Text.Contains("Đã xác minh"),ws.Status.Text);
                    Info(page,"writing").SetValue(page,true);typeof(OrbitRecordListControl).GetMethod("UpdateActions",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(page,null);Check("DG07 writing locks actions",!detail.Enabled&&!reconcile.Enabled&&!submit.Enabled);Info(page,"writing").SetValue(page,false);typeof(OrbitRecordListControl).GetMethod("UpdateActions",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(page,null);
                    var afterTransitions=Finance();File.WriteAllText(Path.Combine(args[0],"after-transitions.json"),afterTransitions);Check("DG06/07 financial data unchanged during transitions",beforeTransitions==afterTransitions);
                    Check("DG08 ADMIN read",(await delivery.ListAsync(admin,"DG26",null,null,0)).Count==50);try{await delivery.ListAsync(store,"DG26",null,null,0);Check("DG08 STORE denied",false);}catch(UnauthorizedAccessException){Check("DG08 STORE denied",true);}
                }
                catch(Exception e){Check("Runner exception",false,(e.InnerException??e).ToString());}
                finally{deadline.Stop();main.Dialogs.AbortAll();main.Dispose();Application.ExitThread();}
            };
            Application.Run(main);
        }
        finally
        {
            if(provisioned)Sql("BEGIN TRAN;DELETE dbo.G03OperationReceipt WHERE EntityCode IN ('HD96260001','HD96260002') AND ActorAccountId IN(SELECT MaTK FROM dbo.TAIKHOAN WHERE MaNV IN ('DGA01','DGC01','DGS01'));DELETE dbo.CHITIET_HD WHERE MaHD IN ('HD96260001','HD96260002');DELETE dbo.HOADON WHERE (MaHD LIKE 'DG26%' OR MaHD IN ('HD96260001','HD96260002')) AND MaNV='DGC01';DELETE dbo.TAIKHOAN WHERE MaNV IN ('DGA01','DGC01','DGS01');DELETE dbo.NHANVIEN WHERE MaNV IN ('DGA01','DGC01','DGS01');COMMIT;");
            Check("Owned fixtures removed",Convert.ToInt32(Scalar("SELECT (SELECT COUNT(*) FROM dbo.HOADON WHERE MaHD LIKE 'DG26%' OR MaHD IN ('HD96260001','HD96260002'))+(SELECT COUNT(*) FROM dbo.NHANVIEN WHERE MaNV IN ('DGA01','DGC01','DGS01'))"))==0);var after=(string)Scalar(snapshot)!;File.WriteAllText(Path.Combine(args[0],"after.json"),after);Check("Existing orders/money/stock/points/receipts unchanged after cleanup",before==after);
            File.WriteAllText(Path.Combine(args[0],"results.json"),JsonSerializer.Serialize(new{Failed=failed,Database=db,Scope="Real Auth/runtime SQL/WinForms component actions125%; direct header-only paging fixtures, two real SalesService delivery orders. Query-seam faults/late replies and lost-ack state are simulations, no physical input/visual QA/checkout.",Results},new JsonSerializerOptions{WriteIndented=true}));
        }
        return failed==0?0:1;
    }
}
