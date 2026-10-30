using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests;
using CafeManager.Models.Requests.Customers;
using CafeManager.Models.Requests.Sales;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

internal static class Program
{
    private const string Query="OO kiểm thử 20261025";
    private sealed class Gate { public int Mode, Count; public TaskCompletionSource Started=new(TaskCreationOptions.RunContinuationsAsynchronously); }
    private sealed record Fixture(SalesService Sales,SessionContext Session,Gate Gate,List<string> Codes,string Before,string Database);
    private static async Task<Fixture> Prepare(string folder)
    {
        var m=JsonDocument.Parse(File.ReadAllText(Path.Combine(folder,"database-reference.json"))).RootElement;
        var db=m.GetProperty("Database").GetString()!;
        if(m.GetProperty("Cleaned").GetBoolean()||db!="CoffeeManager_Test_G04_"+m.GetProperty("Marker").GetString()+"_checkout")throw new InvalidOperationException("Wrong isolated fixture");
        await using var owner=new SqlConnection($"Server=localhost;Initial Catalog={db};Integrated Security=True;TrustServerCertificate=True;Pooling=False");await owner.OpenAsync();
        await using var cmd=owner.CreateCommand();cmd.CommandText="SELECT RunId FROM dbo.G04TestMarker";if((string?)await cmd.ExecuteScalarAsync()!=m.GetProperty("Marker").GetString())throw new InvalidOperationException("Marker mismatch");
        var before=await Snapshot(db);
        var password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        cmd.CommandText="INSERT dbo.NHANVIEN(MaNV,TenNV,LoaiHopDong,MaCV) SELECT 'NPG02',N'Open orders fixture','Full-time',MIN(MaCV) FROM dbo.CHUCVU; INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT,HoatDong) VALUES('NPG02','pager.continue.fixture',@hash,'CASH',1);";cmd.Parameters.AddWithValue("@hash",new PasswordService().Hash(password));await cmd.ExecuteNonQueryAsync();
        var builder=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CHECKOUT_SQL_RUNTIME")??throw new InvalidOperationException("Runtime missing"));
        if(builder.InitialCatalog!=db||builder.IntegratedSecurity)throw new InvalidOperationException("Runtime target");
        var f=new ConnectionFactory(builder.ConnectionString);var auth=new AuthRepository(f);var permission=new PermissionService(auth);
        var session=await new AuthService(auth,new PasswordService()).LoginAsync(new LoginRequest("pager.continue.fixture",password));
        var queryFactory=new ConnectionFactory(builder.ConnectionString);var gate=new Gate();
        Func<SqlConnection,CancellationToken,Task> initialize=async(_,token)=>
        {
            if(gate.Mode==1)throw new OperationCanceledException("OO injected connection-read cancellation");
            if(gate.Mode==2&&Interlocked.Increment(ref gate.Count)==1){gate.Started.TrySetResult();await Task.Delay(1500,token);}
        };
        typeof(ConnectionFactory).GetProperty("SessionInitializer",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(queryFactory,initialize);
        var sales=new SalesService(new SalesRepository(queryFactory),permission);var codes=new List<string>();
        codes.AddRange((await sales.OpenAsync(session,Query,null,0)).Concat(await sales.OpenAsync(session,Query,null,50)).Select(x=>x.Code));
        if(codes.Count!=51)throw new InvalidOperationException("Expected51 existing open fixture orders");
        var after=await Snapshot(db);if(before!=after)throw new InvalidOperationException("Creating open fixtures changed payments/stock/consumption");
        File.WriteAllText(Path.Combine(folder,"fixture.json"),JsonSerializer.Serialize(new{Database=db,Customer="KH011",CustomerName=Query,Codes=codes,FinancialSnapshotUnchanged=true},new JsonSerializerOptions{WriteIndented=true}));
        return new(sales,session,gate,codes,before,db);
    }
    private static async Task<string> Snapshot(string db)
    {
        await using var c=new SqlConnection($"Server=localhost;Initial Catalog={db};Integrated Security=True;TrustServerCertificate=True;Pooling=False");await c.OpenAsync();await using var cmd=c.CreateCommand();
        cmd.CommandText="SELECT (SELECT (SELECT * FROM dbo.CHITIET_THANHTOAN ORDER BY MaTT FOR JSON PATH) Payments,(SELECT * FROM dbo.TIEUHAO_HOADON ORDER BY MaHD,MaNVL FOR JSON PATH) Consumption,(SELECT MaNVL,SoLuongTon FROM dbo.NGUYENVATLIEU ORDER BY MaNVL FOR JSON PATH) Stock,(SELECT * FROM dbo.LICHSU_DIEM ORDER BY MaGD FOR JSON PATH) PointsHistory FOR JSON PATH)";
        return (string)(await cmd.ExecuteScalarAsync())!;
    }
    [STAThread]private static int Main(string[] args)
    {
        var folder=args[0];var fixture=Task.Run(()=>Prepare(folder)).GetAwaiter().GetResult();using var session=fixture.Session;
        ApplicationConfiguration.Initialize();Application.SetColorMode(SystemColorMode.Dark);
        var type=typeof(MainForm).Assembly.GetType("CafeManager.WinForms.Modules.Sales.OpenOrdersDialog")!;
        using var form=(Form)Activator.CreateInstance(type,fixture.Sales,session)!;
        T Field<T>(string name)=>(T)type.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(form)!;
        void Set(string name,object value)=>type.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(form,value);
        void Call(string name)=>type.GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(form,null);
        Task Reload()=>(Task)type.GetMethod("Reload",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(form,null)!;
        var grid=Field<DataGridView>("grid");var search=Field<TextBox>("search");search.Text=Query;
        var results=new List<object>();var observations=new List<object>();var failed=0;var phase=0;var waiting=0;var running=false;
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void Case(string name,Action action){try{action();results.Add(new{Name=name,Outcome="PASS"});}catch(Exception ex){failed++;results.Add(new{Name=name,Outcome="FAIL",Error=(ex.InnerException??ex).ToString()});}}
        bool Inside(Control c){var r=c.RectangleToScreen(c.ClientRectangle);for(var p=c.Parent;p is not null;p=p.Parent)if(!p.RectangleToScreen(p.ClientRectangle).Contains(r))return false;return true;}
        void Bounds(string label)
        {
            foreach(var name in new[]{"grid","search","selection","pageInfo","previous","next","open","count"})Case(label+" bounds/"+name,()=>Check(Inside(Field<Control>(name)),name+" clipped"));
            Case(label+" full code",()=>
            {
                var cell=grid.Rows[0].Cells["Code"];var font=cell.InheritedStyle.Font!;var text=Convert.ToString(cell.FormattedValue)!;
                var measured=TextRenderer.MeasureText(text,font,Size.Empty,TextFormatFlags.NoPadding|TextFormatFlags.SingleLine).Width;
                var available=grid.GetCellDisplayRectangle(cell.ColumnIndex,0,false).Width-cell.InheritedStyle.Padding.Horizontal;
                observations.Add(new{Label=label,Dpi=form.DeviceDpi,Form=form.Size.ToString(),Code=text,Measured=measured,Available=available,Column=grid.Columns["Code"].Width,Minimum=grid.Columns["Code"].MinimumWidth});
                Check(available>=measured,"Code clipped "+available+"<"+measured);Check(grid.Columns["Code"].Frozen,"Code not frozen");
                Check(Field<Label>("selection").Text=="Đang chọn "+text,"Footer lost full code");
            });
        }
        void Save()=>File.WriteAllText(Path.Combine(folder,"results.json"),JsonSerializer.Serialize(new{Scope="Frozen real WinForms dialog, real AuthService/SQL; direct component events/reflection, no physical input or visual QA",Results=results,Failed=failed,Observations=observations},new JsonSerializerOptions{WriteIndented=true}));
        using var timer=new System.Windows.Forms.Timer{Interval=700};
        timer.Tick+=async(_,_)=>
        {
            if(running)return;running=true;
            try
            {
                if(phase==0&&!Field<bool>("ready")){if(++waiting<60)return;throw new InvalidOperationException("Initial load timeout: "+Field<Label>("state").Text);}
                switch(phase++)
                {
                    case 0:
                        Case("DPI125",()=>Check(form.DeviceDpi==120,"DPI="+form.DeviceDpi));
                        Case("SQL51 rows, page50/1, correct initial actions",()=>{Check(grid.Rows.Count==50&&Field<int>("total")==51,"Wrong initial count");Check(!Field<Button>("previous").Enabled&&Field<Button>("next").Enabled&&Field<Button>("open").Enabled,"Wrong initial actions");});
                        Bounds("Default");form.Size=new Size(1280,720);break;
                    case 1:Bounds("1280x720");form.Size=new Size(1366,768);break;
                    case 2:Bounds("1366x768");form.Size=new Size(740,620);break;
                    case 3:
                        Bounds("Narrow740x620");
                        Case("Narrow horizontal scroll keeps code visible",()=>{grid.HorizontalScrollingOffset=100;Check(grid.GetCellDisplayRectangle(0,0,false).Left>=0,"Frozen code scrolled out");});
                        form.Size=new Size(1280,720);Set("page",1);await Reload();break;
                    case 4:
                        Case("SQL second page has one unique row",()=>{Check(grid.Rows.Count==1&&Field<Label>("pageInfo").Text=="2 / 2","Paging");Check(Field<Button>("previous").Enabled&&!Field<Button>("next").Enabled,"Paging actions");});
                        Bounds("After rebind page2");
                        var single=(SalesOrderRow)grid.Rows[0].DataBoundItem!;
                        Case("Date/money/channel formatted from typed data",()=>{Check(grid.Rows[0].Cells["Created"].FormattedValue.ToString()==single.Created.ToString("dd/MM/yyyy HH:mm"),"Date");Check(grid.Rows[0].Cells["Payable"].FormattedValue.ToString()=="81.000 ₫","Money");Check(grid.Rows[0].Cells["Channel"].FormattedValue.ToString()=="Mang đi","Channel");});
                        search.Text=single.Code;Set("page",0);await Reload();break;
                    case 5:
                        Case("Exact invoice search and full footer code",()=>{Check(grid.Rows.Count==1,"Exact search");Check(Field<Label>("selection").Text.EndsWith(search.Text),"Wrong selected code");});
                        search.Text="OO_NO_RESULT_20261025";Set("page",0);await Reload();break;
                    case 6:
                        Case("Empty search disables opening and pagination",()=>Check(grid.Rows.Count==0&&!Field<Button>("open").Enabled&&!Field<Button>("next").Enabled&&!Field<Button>("previous").Enabled&&Field<Label>("count").Text.StartsWith("0 "),"Empty state"));
                        search.Text=Query;Set("page",99);await Reload();break;
                    case 7:
                        Case("Out-of-range page returns first actual page",()=>Check(Field<int>("page")==0&&grid.Rows.Count==50,"Fallback page"));
                        fixture.Gate.Mode=1;await Reload();fixture.Gate.Mode=0;
                        Case("Read fault retains query and rows but disables opening",()=>Check(search.Text==Query&&grid.Rows.Count==50&&!Field<Button>("open").Enabled&&Field<Label>("count").Text=="Chưa cập nhật"&&Field<Label>("state").Text.StartsWith("Chưa cập nhật"),"Error state"));
                        await Reload();break;
                    case 8:
                        fixture.Gate.Count=0;fixture.Gate.Mode=2;fixture.Gate.Started=new(TaskCreationOptions.RunContinuationsAsynchronously);
                        search.Text=Query;var first=Reload();await fixture.Gate.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));search.Text="OO_NO_RESULT_20261025";var second=Reload();await second;
                        Case("Controlled response inversion actually overlaps",()=>Check(!first.IsCompleted,"Delay did not invert responses"));
                        await first;fixture.Gate.Mode=0;
                        Case("Older response cannot replace latest empty result",()=>Check(grid.Rows.Count==0&&Field<bool>("ready")&&!Field<Button>("open").Enabled,"Stale response won"));
                        search.Text=Query;Set("page",0);await Reload();break;
                    case 9:
                        Case("Shared profiles remain readonly/payment editable",()=>{foreach(var p in new[]{OrbitGridProfile.Inventory,OrbitGridProfile.Catalogs}){using var old=new OrbitDataGridView{Profile=p};Check(old.ReadOnly,"Readonly profile changed");}using var payment=new OrbitDataGridView{Profile=OrbitGridProfile.Payment};Check(!payment.ReadOnly,"Payment locked");});
                        Case("Financial snapshot unchanged by reads",()=>Check(Task.Run(()=>Snapshot(fixture.Database)).GetAwaiter().GetResult()==fixture.Before,"Financial tables changed"));
                        var expected=((SalesOrderRow)grid.Rows[3].DataBoundItem!).Code;grid.CurrentCell=grid.Rows[3].Cells["Code"];
                        Call("SelectOrder");Case("Open selection returns exact chosen code",()=>Check((string?)type.GetProperty("SelectedCode")!.GetValue(form)==expected&&form.DialogResult==DialogResult.OK,"Selected wrong invoice"));
                        timer.Stop();break;
                }
            }
            catch(Exception ex){failed++;results.Add(new{Name="Harness/dialog phase "+phase,Outcome="FAIL",Error=ex.ToString()});timer.Stop();fixture.Gate.Mode=0;form.Close();}
            finally{Save();running=false;}
        };
        form.Shown+=(_,_)=>timer.Start();Application.Run(form);Save();Console.WriteLine($"OpenOrders {results.Count-failed}/{results.Count} PASS");return failed==0?0:1;
    }
}

