using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using CafeManager.Models.Requests.Sales;
using CafeManager.Models.Dtos.Sales;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Sales;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

internal static class Program
{
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool IsWindowEnabled(IntPtr window); static readonly List<object> results=[]; static int failures;
    static void Check(string name,bool ok,object? actual=null){results.Add(new{Name=name,Outcome=ok?"PASS":"FAIL",Actual=actual});if(!ok)failures++;}
    static FieldInfo Info(object o,string name){for(var t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);if(f!=null)return f;}throw new MissingFieldException(name);}
    static T F<T>(object o,string n)=>(T)Info(o,n).GetValue(o)!;
    static Task Call(object o,string name)=>(Task)typeof(OrbitRecordListControl).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(o,null)!;
    static IEnumerable<Control> Children(Control c){foreach(Control x in c.Controls){yield return x;foreach(var y in Children(x))yield return y;}}
    static async Task Wait(Func<bool> p){for(int i=0;i<300;i++){if(p())return;await Task.Delay(30);}throw new TimeoutException("Readiness deadline");}
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
        var factory=new ConnectionFactory(cs.ConnectionString);var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);var service=new InvoiceService(new InvoiceRepository(factory),permissions);
        const string snapshot="SELECT (SELECT (SELECT * FROM dbo.HOADON ORDER BY MaHD FOR JSON PATH) Orders,(SELECT * FROM dbo.CHITIET_THANHTOAN ORDER BY MaTT FOR JSON PATH) Payments,(SELECT * FROM dbo.TIEUHAO_HOADON ORDER BY MaHD,MaNVL FOR JSON PATH) Consumption,(SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) Stock,(SELECT MaKH,DiemTichLuy FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH) Points FOR JSON PATH)";
        var before=(string)Scalar(snapshot)!;File.WriteAllText(Path.Combine(args[0],"before.json"),before);
        bool provisioned=false;var password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        try
        {
            if(Convert.ToInt32(Scalar("SELECT (SELECT COUNT(*) FROM dbo.HOADON WHERE MaHD LIKE 'IH26%')+(SELECT COUNT(*) FROM dbo.KHACHHANG WHERE MaKH='IV426')+(SELECT COUNT(*) FROM dbo.NHANVIEN WHERE MaNV IN ('IHA01','IHC01','IHS01'))"))!=0)throw new Exception("Fixture collision");
            Sql("BEGIN TRAN;INSERT dbo.KHACHHANG(MaKH,TenKH,SDT,MaHang) SELECT 'IV426',N'IH V4 kiểm thử','0900012626',MIN(MaHang) FROM dbo.HANGTHANHVIEN;INSERT dbo.NHANVIEN(MaNV,TenNV,LoaiHopDong,MaCV) SELECT v.code,N'Invoice V4 fixture','Full-time',(SELECT MIN(MaCV) FROM dbo.CHUCVU) FROM(VALUES('IHA01'),('IHC01'),('IHS01'))v(code);INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT,HoatDong) VALUES('IHA01','ih.fixture.admin',@hash,'ADMIN',1),('IHC01','ih.fixture.cash',@hash,'CASH',1),('IHS01','ih.fixture.store',@hash,'STORE',1);WITH n AS(SELECT 1 n UNION ALL SELECT n+1 FROM n WHERE n<101) INSERT dbo.HOADON(MaHD,NgayLap,MaNV,MaKH,LoaiDon) SELECT 'IH26'+RIGHT('000000'+CONVERT(varchar,n),6),DATEADD(day,n-1,CONVERT(datetime2,'20010101')),'IHC01','IV426',N'Mang di' FROM n OPTION(MAXRECURSION 101);COMMIT;",new PasswordService().Hash(password));provisioned=true;
            using var cash=new AuthService(auth,new PasswordService()).LoginAsync(new("ih.fixture.cash",password)).GetAwaiter().GetResult();
            using var admin=new AuthService(auth,new PasswordService()).LoginAsync(new("ih.fixture.admin",password)).GetAwaiter().GetResult();
            using var store=new AuthService(auth,new PasswordService()).LoginAsync(new("ih.fixture.store",password)).GetAwaiter().GetResult();
            using var main=new MainForm{Text="Invoice V4 functional component acceptance"};
            using var page=new InvoicesControl(service,cash){Dock=DockStyle.Fill};
            F<Panel>(main,"content").Controls.Add(page);
            using var deadline=new System.Windows.Forms.Timer{Interval=180000};deadline.Tick+=(_,_)=>{Check("deadline",false);main.Dispose();Application.ExitThread();};
            main.Shown+=async(_,_)=>
            {
                deadline.Start();
                try
                {
                    await Idle(page);var ws=F<OrbitListWorkspace>(page,"Workspace");var grid=F<OrbitDataGridView>(page,"Grid");
                    var search=Children(ws).OfType<TextBox>().Single(x=>x.AccessibleName=="Tìm hóa đơn");
                    var basis=F<ComboBox>(page,"basis");var status=F<ComboBox>(page,"status");var channel=F<ComboBox>(page,"channel");var from=F<OrbitDateField>(page,"from");var to=F<OrbitDateField>(page,"to");
                    var detail=Children(ws).OfType<Button>().Single(x=>x.Text=="Chi tiết / In");
                    var submit=Children(ws).OfType<Button>().Single(x=>x.AccessibleName=="Tìm kiếm");
                    Check("IH02 default Paid today",(string?)basis.SelectedValue=="Paid"&&from.Value==DateTime.Today&&to.Value==DateTime.Today);
                    basis.SelectedValue="Created";from.Value=new DateTime(2001,1,1);to.Value=new DateTime(2001,4,11);await Idle(page);
                    search.Text="IH26";submit.PerformClick();await Idle(page);
                    foreach(int count in new[]{0,1,49,50,51,100,101})
                    {
                        to.Value=count==0?new DateTime(2001,1,1):new DateTime(2001,1,1).AddDays(count-1);await Idle(page);
                        search.Text=count==0?"IH26-MISSING":"IH26";await Call(page,"SubmitSearch");await Idle(page);
                        Check($"IH04 SQL total {count}",F<int?>(page,"totalCount")==count&&grid.Rows.Count==Math.Min(count,50),new{Total=F<int?>(page,"totalCount"),Rows=grid.Rows.Count,Range=ws.Pagination.Range.Text});
                        Check($"IH04 first next {count}",ws.Pagination.Next.Enabled==(count>50)&&!ws.Pagination.Previous.Enabled);
                        if(count>50){while(ws.Pagination.Next.Enabled){ws.Pagination.Next.PerformClick();await Idle(page);}Check($"IH04 last {count}",grid.Rows.Count==(count%50==0?50:count%50)&&!ws.Pagination.Next.Enabled&&ws.Pagination.Previous.Enabled,ws.Pagination.Range.Text);}
                    }
                    search.Text="IH26";await Call(page,"SubmitSearch");await Idle(page);
                    search.Text="not submitted";channel.SelectedValue="Mang di";await Idle(page);
                    Check("IH03 draft ignored filter",F<int?>(page,"totalCount")==101&&F<string>(page,"appliedSearch")=="IH26"&&search.Text=="not submitted");
                    ws.Pagination.Next.PerformClick();await Idle(page);Check("IH03 draft ignored paging",grid.Rows.Count==50&&F<int>(page,"page")==1);
                    F<OrbitButton>(page,"refresh").PerformClick();await Idle(page);Check("IH03 draft ignored refresh",F<int?>(page,"totalCount")==101&&F<int>(page,"page")==0);
                    typeof(Control).GetMethod("OnKeyDown",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(search,[new KeyEventArgs(Keys.Enter)]);await Idle(page);Check("IH03 Enter commits draft",grid.Rows.Count==0&&F<string>(page,"appliedSearch")=="not submitted"&&!detail.Enabled);
                    search.Text="IH26000001";submit.PerformClick();await Idle(page);Check("IH03 code exact",grid.Rows.Count==1&&((SalesOrderRow)grid.Rows[0].DataBoundItem!).Code=="IH26000001");
                    var customer=(string)Scalar("SELECT TenKH FROM dbo.KHACHHANG WHERE MaKH='IV426'")!;var phone=(string)Scalar("SELECT SDT FROM dbo.KHACHHANG WHERE MaKH='IV426'")!;
                    foreach(var term in new[]{customer,phone}){search.Text=term;await Call(page,"SubmitSearch");await Idle(page);Check("IH03 customer search "+(term==phone?"phone":"name"),F<int?>(page,"totalCount")==101);}
                    status.SelectedValue="Da thanh toan";await Idle(page);Check("IH03 status auto",grid.Rows.Count==0);status.SelectedIndex=0;await Idle(page);Check("IH03 status clear auto",F<int?>(page,"totalCount")==101);
                    channel.SelectedValue="Giao hang";await Idle(page);Check("IH03 channel auto",grid.Rows.Count==0);channel.SelectedIndex=0;await Idle(page);
                    basis.SelectedValue="Paid";await Idle(page);Check("IH02 Paid excludes open",grid.Rows.Count==0);basis.SelectedValue="Created";await Idle(page);
                    F<TextBox>(from,"input").Text="31/02/2001";await Idle(page);Check("IH02 invalid date error",!grid.Enabled&&!detail.Enabled&&ws.Status.Text.Contains("Ngày không hợp lệ")&&!ws.Pagination.Next.Enabled,ws.Status.Text);
                    from.Value=new DateTime(2001,4,12);await Idle(page);Check("IH02 reversed range error",ws.Status.Text.Contains("Khoảng ngày")&&!grid.Enabled,ws.Status.Text);from.Value=new DateTime(2001,1,1);await Idle(page);
                    // Date editing uses the actual shared entry helper, no synthetic value replacement.
                    search.Focus();await Task.Delay(100);F<TextBox>(from,"input").Focus();await Task.Delay(100);Check("IH08 date select all on focus",F<TextBox>(from,"input").SelectionLength==F<TextBox>(from,"input").TextLength);search.Focus();await Task.Delay(100);Check("IH08 search select all on focus",search.SelectionLength==search.TextLength);
                    F<Button>(from,"open").PerformClick();await Task.Delay(100);Check("IH08 calendar popup",F<ToolStripDropDown>(from,"popup").Visible);F<ToolStripDropDown>(from,"popup").Close();
                    Check("IH06 readonly code and date",grid.ReadOnly&&grid.Columns["Code"].Frozen&&grid.Columns["Created"].DefaultCellStyle.Format=="dd/MM/yyyy HH:mm");
                    Check("IH06 currency formatted",grid.Rows[0].Cells["Payable"].FormattedValue?.ToString()=="0 ₫",grid.Rows[0].Cells["Payable"].FormattedValue);
                    // Keep product source fixed; inject controlled read completions only at existing query delegate seam.
                    var queryField=Info(page,"query");var original=queryField.GetValue(page);var row=(SalesOrderRow)grid.Rows[0].DataBoundItem!;
                    var old=new TaskCompletionSource<IList>();var newer=new TaskCompletionSource<IList>();int calls=0;
                    queryField.SetValue(page,(Func<string,int,CancellationToken,Task<IList>>)((q,s,t)=>++calls==1?old.Task:newer.Task));
                    var first=Call(page,"Reload");var second=Call(page,"Reload");newer.SetResult(new List<SalesOrderRow>{row with{Code="NEW",TotalCount=1}});await second;old.SetResult(new List<SalesOrderRow>{row with{Code="OLD",TotalCount=1}});await first;
                    Check("IH05 late completion ignored",((SalesOrderRow)grid.Rows[0].DataBoundItem!).Code=="NEW");
                    queryField.SetValue(page,(Func<string,int,CancellationToken,Task<IList>>)((q,s,t)=>throw new ArgumentException("Controlled read fault")));await Call(page,"Reload");Check("IH05 error no stale rows/actions",grid.Rows.Count==0&&!grid.Enabled&&!detail.Enabled&&ws.Pagination.Range.Text=="Chưa tải được dữ liệu",ws.Status.Text);
                    queryField.SetValue(page,(Func<string,int,CancellationToken,Task<IList>>)((q,s,t)=>Task.FromCanceled<IList>(new CancellationToken(true))));await Call(page,"Reload");Check("IH05 cancel explicit",ws.Status.Text.Contains("Đã dừng tải")&&!detail.Enabled);
                    var skips=new List<int>();queryField.SetValue(page,(Func<string,int,CancellationToken,Task<IList>>)((q,s,t)=>{skips.Add(s);return Task.FromResult<IList>(s>0?new List<SalesOrderRow>():new List<SalesOrderRow>{row with{TotalCount=1}});}));Info(page,"page").SetValue(page,2);await Call(page,"Reload");Check("IH04 empty stale page fallback",skips.SequenceEqual(new[]{100,0})&&F<int>(page,"page")==0&&grid.Rows.Count==1&&!ws.Pagination.Next.Enabled,skips);
                    queryField.SetValue(page,original);await Call(page,"ResetAndReload");await Idle(page);
                    // Real detail from SQL and native owner manager; do not print or settle an order.
                    foreach(var code in new[]{"HD20250421","IH26000001","HD20250473"})
                    {
                        var task=page.ShowDetail(code);await Wait(()=>Application.OpenForms.Cast<Form>().Any(f=>f.GetType().Name=="InvoiceDetailDialog"));var dialog=Application.OpenForms.Cast<Form>().Single(f=>f.GetType().Name=="InvoiceDetailDialog");
                        var back=Children(dialog).OfType<Button>().Single(b=>b.Text=="Quay lại POS");
                        Check("IH07 detail native "+code,dialog.Modal&&dialog.Owner==main&&!dialog.ShowInTaskbar&&!IsWindowEnabled(main.Handle));
                        Check("IH07 return POS state "+code,back.Enabled==(code=="IH26000001"));dialog.Close();await task.WaitAsync(TimeSpan.FromSeconds(5));
                    }
                    await page.ShowDetail("IH26MISS00");Check("IH07 missing detail explicit",ws.Status.Text.Contains("không tồn tại"));
                    var filter=new InvoiceFilter("Created",new(2001,1,1),new(2001,4,11),Search:"IH26");Check("IH09 admin read",(await service.ListAsync(admin,filter)).Count==50);try{await service.ListAsync(store,filter);Check("IH09 STORE denied",false);}catch(UnauthorizedAccessException){Check("IH09 STORE denied",true);}
                    using var pager=new OrbitPagination();pager.SetPage(0,50,50);Check("IH10 legacy known pager",!pager.Next.Enabled&&pager.Range.Text=="Hiển thị 1–50 / 50");pager.SetServerPage(0,50,50,"khách hàng");Check("IH10 legacy server pager",pager.Next.Enabled&&pager.Range.Text.Contains("khách hàng"));
                }
                catch(Exception e){Check("Runner exception",false,(e.InnerException??e).ToString());}
                finally{deadline.Stop();main.Dialogs.AbortAll();main.Dispose();Application.ExitThread();}
            };
            Application.Run(main);
        }
        finally
        {
            if(provisioned)Sql("BEGIN TRAN;DELETE dbo.HOADON WHERE MaHD LIKE 'IH26%' AND MaNV='IHC01';DELETE dbo.KHACHHANG WHERE MaKH='IV426';DELETE dbo.TAIKHOAN WHERE MaNV IN ('IHA01','IHC01','IHS01');DELETE dbo.NHANVIEN WHERE MaNV IN ('IHA01','IHC01','IHS01');COMMIT;");
            Check("Owned fixtures removed",Convert.ToInt32(Scalar("SELECT (SELECT COUNT(*) FROM dbo.HOADON WHERE MaHD LIKE 'IH26%')+(SELECT COUNT(*) FROM dbo.KHACHHANG WHERE MaKH='IV426')+(SELECT COUNT(*) FROM dbo.NHANVIEN WHERE MaNV IN ('IHA01','IHC01','IHS01'))"))==0);
            var after=(string)Scalar(snapshot)!;File.WriteAllText(Path.Combine(args[0],"after.json"),after);Check("Existing finances/stock/points unchanged",before==after);
            File.WriteAllText(Path.Combine(args[0],"results.json"),JsonSerializer.Serialize(new{Failed=failures,Database=db,Scope="Real WinForms component actions at125%, real Auth/runtime SQL, isolated open-order read fixtures. Controlled query seam only for delay/error/cancel/fallback. No physical input or visual acceptance.",Results=results},new JsonSerializerOptions{WriteIndented=true}));
        }
        return failures==0?0:1;
    }
}


