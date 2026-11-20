using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Reports;
using CafeManager.Models.Dtos.Products;
using CafeManager.Models.Requests;
using CafeManager.Models.Requests.Sales;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Home;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

if(args.Length!=4||!System.Text.RegularExpressions.Regex.IsMatch(args[1],@"^CoffeeManager_Test_G07_\d{8}_\d{6}_[a-f0-9]{8}_demo$"))throw new ArgumentException("mode, exact test DB, marker, output required");
var mode=args[0];var database=args[1];var marker=args[2];var output=args[3];
var builder=new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=database,IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false};
await using var owner=new SqlConnection(builder.ConnectionString);await owner.OpenAsync();
async Task<object?> Scalar(string text,IEnumerable<(string,object)>? parameters=null)
{
 await using var c=owner.CreateCommand();c.CommandText=text;
 if(parameters is not null)foreach(var (name,value) in parameters)c.Parameters.AddWithValue(name,value);
 return await c.ExecuteScalarAsync();
}
if((string?)await Scalar("SELECT RunId FROM dbo.G07TestMarker")!=marker)throw new InvalidOperationException("Marker mismatch");
var result=new Dictionary<string,object?>{["Database"]=database,["Marker"]=marker,["Mode"]=mode,["StartedAt"]=DateTime.Now.ToString("O")};

ConnectionFactory Factory(string? name=null,int timeout=30)
{
 var f=new ConnectionFactory(new SqlConnectionStringBuilder(builder.ConnectionString){InitialCatalog=name??database}.ConnectionString,timeout);
 typeof(ConnectionFactory).GetProperty("SessionInitializer",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(f,(Func<SqlConnection,CancellationToken,Task>)(async(c,t)=>{await using var cmd=c.CreateCommand();cmd.CommandText="EXECUTE AS USER='ExtendedRuntime'";await cmd.ExecuteNonQueryAsync(t);}));
 return f;
}
var factory=Factory();var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);

if(mode=="prepare")
{
 await using(var c=owner.CreateCommand())
 {
  c.CommandText="DECLARE @s binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=1);EXEC dbo.sp_SetupBootstrap @Mode='ActivateDemo',@Username=N'admin',@NewHash=@hash,@MaNV='NV001',@ExpectedStamp=@s;CREATE USER ExtendedRuntime WITHOUT LOGIN;ALTER ROLE CafeManagerApp ADD MEMBER ExtendedRuntime;";
  c.Parameters.AddWithValue("@hash",new PasswordService().Hash(Guid.NewGuid().ToString("N")));await c.ExecuteNonQueryAsync();
 }
 using var setupSession=new SessionContext((await auth.ReadIdentityAsync(1))!);
 var materials=new MaterialService(new MaterialRepository(factory),permissions);var products=new ProductService(new ProductRepository(factory),permissions);
 if(Convert.ToInt32(await Scalar("SELECT (SELECT COUNT(*) FROM dbo.NGUYENVATLIEU WHERE MaNVL='NVL99')+(SELECT COUNT(*) FROM dbo.SANPHAM WHERE MaSP='SP999')"))!=0)throw new InvalidOperationException("Fixture codes exist");
 var material=await materials.SaveAsync(setupSession,new("NVL99","Analytics extended material","g"),null,Guid.NewGuid());
 await materials.OpenAsync(setupSession,new("NVL99",1000000,"Dashboard isolated fixture"),material.Revision!,Guid.NewGuid());
 var category=(await products.CategoriesAsync(setupSession))[0].Code;
 await products.SaveAsync(setupSession,new("SP999",new string('A',50),category,1000,"Dang ban",[new RecipeInput("NVL99",0.0001m)]),null,Guid.NewGuid());
 result["Bills"]=await Scalar("SELECT COUNT(*) FROM dbo.HOADON");
 result["Status"]="Prepared";
 await File.WriteAllTextAsync(output,JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
 return;
}

using var session=new SessionContext((await auth.ReadIdentityAsync(1))!);
var reports=new ReportService(new ReportRepository(factory),permissions);

if(mode=="bills")
{
 var sales=new SalesService(new SalesRepository(factory),permissions);
 async Task<string> NewOrder()
 {
  var code=await sales.AllocateAsync(session);await sales.CreateAsync(session,new(code,Guid.NewGuid(),new("Mang di"),"SP999",1,null));return code;
 }
 async Task Checkout(string code)
 {
  var order=(await sales.GetAsync(session,code))!;var preview=await sales.PreviewAsync(session,order.Header);
  await sales.CheckoutAsync(session,new(code,order.Header.Revision,Guid.NewGuid(),preview.Token,[new PaymentAllocation("PT001",preview.Payable)]));
 }
 var initial=Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM dbo.HOADON"));
 if(initial>=1000)throw new InvalidOperationException("Unexpected baseline size "+initial);
 var watch=Stopwatch.StartNew();
 for(var i=initial;i<10000;i++)
 {
  var code=await NewOrder();
  if(i<8000)await Checkout(code);
  else if(i>=9000)await sales.CancelAsync(session,(await sales.GetAsync(session,code))!.Header,Guid.NewGuid());
  if((i+1)%1000==0)Console.WriteLine($"Generated total {i+1} after {watch.Elapsed.TotalMinutes:F1} min");
 }
 var total=Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM dbo.HOADON"));
 if(total!=10000)throw new InvalidOperationException("Dataset count "+total);
 result["Bills"]=total;
 result["Distribution"]=await Scalar("SELECT (SELECT TrangThai,COUNT(*) Bills FROM dbo.HOADON GROUP BY TrangThai FOR JSON PATH)");
 result["GenerationMinutes"]=Math.Round(watch.Elapsed.TotalMinutes,2);
 result["Status"]="Generated";
 await File.WriteAllTextAsync(output,JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
 return;
}

// UI phases: empty (before bills), perf (10,000 bills) and error (missing DB).
var checks=new List<object>();var failures=0;
async Task Check(string id,string expected,Func<Task<object?>> test)
{
 var status="PASS";object? actual;
 try{actual=await test();}
 catch(Exception e){failures++;status="FAIL";actual=e.Message;}
 checks.Add(new{Id=id,Expected=expected,Status=status,Actual=actual});
 Console.WriteLine($"{id}: {status}");
 if(status=="FAIL")Console.WriteLine("  "+actual);
}
static void Assert(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
var errors=new List<string>();
Application.ThreadException+=(_,e)=>errors.Add("ThreadException: "+e.Exception);
AppDomain.CurrentDomain.UnhandledException+=(_,e)=>errors.Add("Unhandled: "+e.ExceptionObject);
Exception? failure=null;var measurements=new Dictionary<string,object?>();
var thread=new Thread(()=>
{
 Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
 using var main=new MainForm{ClientSize=new(1366,768)};
 AdminDashboardControl Build(ReportService service)=>new(service,new SessionContext(session.Identity));
 FieldInfo Field(string name)=>typeof(AdminDashboardControl).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)!;
 bool Ready(AdminDashboardControl d)=>(bool)Field("ready").GetValue(d)!;
 Label State(AdminDashboardControl d)=>(Label)Field("state").GetValue(d)!;
 string[] Values(AdminDashboardControl d)=>((System.Collections.IDictionary)Field("values").GetValue(d)!).Values.Cast<Label>().Select(l=>l.Text).ToArray();
 OrbitButton Button(AdminDashboardControl d,string name)=>(OrbitButton)Field(name).GetValue(d)!;
 async Task Wait(AdminDashboardControl d,int tries=3000){for(var i=0;i<tries&&!Ready(d);i++)await Task.Delay(10);if(!Ready(d))throw new TimeoutException("Dashboard not ready: "+State(d).Text);}
 (DateTime From,DateTime Until) Bounds(AdminDashboardControl d)
 {
  var p=Field("period").GetValue(d)!;
  var t=Nullable.GetUnderlyingType(p.GetType())??p.GetType();
  DateTime Item(string name)=>(DateTime)t.GetField(name)!.GetValue(p)!;
  return (Item("Item1"),Item("Item2"));
 }
 async Task<(int Count,decimal Amount)> BillTotals(DateTime from,DateTime until)
 {
  await using var c=owner.CreateCommand();
  c.CommandText="SELECT COUNT(*),COALESCE(SUM(ThanhTien),0) FROM dbo.HOADON WHERE TrangThai=N'Da thanh toan' AND NgayThanhToan>=@f AND NgayThanhToan<@u";
  c.Parameters.AddWithValue("@f",from);c.Parameters.AddWithValue("@u",until);
  await using var r=await c.ExecuteReaderAsync();await r.ReadAsync();return (r.GetInt32(0),r.GetDecimal(1));
 }
 string Money(decimal value)=>value.ToString("#,##0",CultureInfo.GetCultureInfo("vi-VN"))+" ₫";
 string Number(decimal value)=>value.ToString("#,##0",CultureInfo.GetCultureInfo("vi-VN"));
 void LayoutChecks(AdminDashboardControl d,string tag,List<object> output)
 {
  var metrics=(TableLayoutPanel)Field("metrics").GetValue(d)!;var charts=(TableLayoutPanel)Field("charts").GetValue(d)!;var bottom=(TableLayoutPanel)Field("bottom").GetValue(d)!;
  int Rule(TableLayoutPanel panel,bool kpi){var width=panel.ClientSize.Width*96F/d.DeviceDpi;return kpi?(width>=1080?4:width>=530?2:1):(width>=760?2:1);}
  var kpi=((System.Collections.IEnumerable)Field("cards").GetValue(d)!).Cast<OrbitSurface>().ToArray();
  var chartCards=((System.Collections.IEnumerable)Field("chartCards").GetValue(d)!).Cast<OrbitSurface>().ToArray();
  var bottomCards=((System.Collections.IEnumerable)Field("bottomCards").GetValue(d)!).Cast<OrbitSurface>().ToArray();
  Assert(metrics.ColumnCount==Rule(metrics,true),$"{tag}: KPI columns {metrics.ColumnCount} != {Rule(metrics,true)} ({metrics.ClientSize.Width}px)");
  Assert(charts.ColumnCount==Rule(charts,false),$"{tag}: chart columns {charts.ColumnCount} != {Rule(charts,false)}");
  Assert(kpi.Length==4&&kpi.All(c=>c.Width>0&&c.Height>0),"KPI card size");
  Assert(chartCards.Length==2&&chartCards.All(c=>c.Width>0&&c.Height>0),"Chart card size");
  Assert(bottomCards.Length==2&&bottomCards.All(c=>c.Width>0&&c.Height>0),"Bottom card size");
  Assert(kpi.All(c=>c.Left+c.Width<=metrics.ClientSize.Width+2),"KPI overflow");
  Assert(chartCards.All(c=>c.Left+c.Width<=charts.ClientSize.Width+2),"Chart overflow");
  Assert(bottomCards.All(c=>c.Left+c.Width<=bottom.ClientSize.Width+2),"Bottom overflow");
  var scroll=d.Controls[0];var page=scroll.Controls[0];
  Assert(page.Width<=scroll.ClientSize.Width+2,$"{tag}: page wider than viewport {page.Width}>{scroll.ClientSize.Width}");
  output.Add(new{Size=main.ClientSize,DeviceDpi=d.DeviceDpi,MetricsColumn=metrics.ColumnCount,ChartsColumn=charts.ColumnCount,Kpi=kpi.Select(c=>new{c.Left,c.Top,c.Width,c.Height}).ToArray(),Chart=chartCards.Select(c=>new{c.Width,c.Height}).ToArray(),PageWidth=page.Width,ScrollWidth=scroll.ClientSize.Width});
 }
 main.Shown+=async(_,_)=>
 {
  try
  {
   if(mode=="empty")
   {
    using var dashboard=Build(reports);
    ((Panel)typeof(MainForm).GetField("content",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(main)!).Controls.Add(dashboard);
    await Wait(dashboard);await Task.Yield();
    await Check("EMPTY-day-ready","ready with zero period",async()=>{await Task.Yield();return State(dashboard).Text;});
    await Check("EMPTY-day-zero","revenue 0 ₫, invoices 0, empty Top5 text",async()=>
    {
     var values=Values(dashboard);
     Assert(values[0]=="0 ₫"&&values[1]=="0",string.Join('|',values));
     var products=(TableLayoutPanel)Field("products").GetValue(dashboard)!;
     Assert(products.Controls.OfType<Label>().Any(l=>l.Text.Contains("Chưa có món bán")),"no empty text");
     return values;
    });
    var layouts=new List<object>();LayoutChecks(dashboard,"empty-1366",layouts);
    main.ClientSize=new(1280,720);await Task.Delay(400);LayoutChecks(dashboard,"empty-1280",layouts);
    main.ClientSize=new(1920,1080);await Task.Delay(500);LayoutChecks(dashboard,"empty-1920",layouts);
    Assert(((TableLayoutPanel)Field("metrics").GetValue(dashboard)!).ColumnCount==4,"1920 KPI columns");
    main.MinimumSize=new Size(320,320);main.ClientSize=new(600,720);await Task.Delay(500);LayoutChecks(dashboard,"empty-narrow",layouts);
    Assert(((TableLayoutPanel)Field("metrics").GetValue(dashboard)!).ColumnCount==1,"narrow KPI columns");
    main.ClientSize=new(1366,768);await Task.Delay(300);
    Button(dashboard,"monthly").PerformClick();await Wait(dashboard);await Task.Yield();
    await Check("EMPTY-month-zero","monthly period also empty",async()=>{var v=Values(dashboard);Assert(v[0]=="0 ₫"&&v[1]=="0",string.Join('|',v));return v;});
    measurements["Layouts"]=layouts;
   }
   else if(mode=="perf")
   {
    // SQL samples on the same 10,000-bill dataset.
    var sqlSamples=new List<object>();
    foreach(var month in new[]{false,true})
    {
     for(var round=0;round<3;round++)
     {
      var rows=0;var watch=Stopwatch.StartNew();
      await using var c=owner.CreateCommand();
      c.CommandText="EXEC dbo.sp_DashboardAnalyticsRead @Actor=@a,@Stamp=@s,@Month=@m";
      c.Parameters.AddWithValue("@a",session.Identity.AccountId);
      c.Parameters.Add("@s",SqlDbType.Binary,8).Value=session.Identity.SecurityStamp;
      c.Parameters.AddWithValue("@m",month);
      await using(var reader=await c.ExecuteReaderAsync()){do{while(await reader.ReadAsync())rows++;}while(await reader.NextResultAsync());}
      watch.Stop();sqlSamples.Add(new{Month=month,Round=round,ElapsedMs=Math.Round(watch.Elapsed.TotalMilliseconds,1),Rows=rows});
     }
    }
    measurements["SQL"]=sqlSamples;

    using var dashboard=Build(reports);
    ((Panel)typeof(MainForm).GetField("content",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(main)!).Controls.Add(dashboard);
    var startup=Stopwatch.StartNew();await Wait(dashboard);await Task.Yield();startup.Stop();
    var ui=new List<object>{new{Mode="startup-day",ElapsedMs=Math.Round(startup.Elapsed.TotalMilliseconds,1)}};
    await Check("UI-startup-day-under-3s","ready < 3000 ms",async()=>{var ms=startup.Elapsed.TotalMilliseconds;Assert(ms<3000,$"{ms:F0} ms");await Task.Yield();return Math.Round(ms,1);});
    await Check("UI-day-values-match-sql","revenue/invoices equal SQL aggregate",async()=>{var b=Bounds(dashboard);var (count,amount)=await BillTotals(b.From,b.Until);var v=Values(dashboard);Assert(v[0]==Money(amount)&&v[1]==Number(count),$"{v[0]} vs {Money(amount)}; {v[1]} vs {count}");return new{Revenue=v[0],Count=v[1],SqlCount=count};});
    foreach(var (button,modeName) in new[]{("monthly","monthly"),("today","today")})
    {
     var watch=Stopwatch.StartNew();Button(dashboard,button).PerformClick();await Wait(dashboard);await Task.Yield();watch.Stop();
     ui.Add(new{Mode=modeName,ElapsedMs=Math.Round(watch.Elapsed.TotalMilliseconds,1)});
     var ms=watch.Elapsed.TotalMilliseconds;
     await Check($"UI-{modeName}-under-3s","ready < 3000 ms",async()=>{Assert(ms<3000,$"{ms:F0} ms");await Task.Yield();return Math.Round(ms,1);});
     await Check($"UI-{modeName}-matches-sql","revenue/invoices equal SQL aggregate",async()=>{var b=Bounds(dashboard);var (count,amount)=await BillTotals(b.From,b.Until);var v=Values(dashboard);Assert(v[0]==Money(amount)&&v[1]==Number(count),$"{v[0]} vs {Money(amount)}; {v[1]} vs {count}");return new{Revenue=v[0],Count=v[1],SqlCount=count};});
    }
    measurements["UI"]=ui;

    var layouts=new List<object>();LayoutChecks(dashboard,"perf-1366",layouts);
    main.ClientSize=new(1280,720);await Task.Delay(500);LayoutChecks(dashboard,"perf-1280",layouts);
    main.ClientSize=new(1920,1080);await Task.Delay(500);LayoutChecks(dashboard,"perf-1920",layouts);
    Assert(((TableLayoutPanel)Field("metrics").GetValue(dashboard)!).ColumnCount==4,"1920 KPI columns");
    main.MinimumSize=new Size(320,320);main.ClientSize=new(600,720);await Task.Delay(500);LayoutChecks(dashboard,"perf-narrow",layouts);
    Assert(((TableLayoutPanel)Field("metrics").GetValue(dashboard)!).ColumnCount==1,"narrow KPI columns");
    main.ClientSize=new(1366,768);await Task.Delay(400);
    measurements["Layouts"]=layouts;

    // Top 5 and bars.
    await Check("UI-top5-sql-parity","top5 names/quantities and one bar per row",async()=>
    {
     var b=Bounds(dashboard);var expected=new List<string>();
     await using(var c=owner.CreateCommand())
     {
      c.CommandText="SELECT TOP 5 s.TenSP,SUM(l.SoLuong) Q FROM dbo.HOADON h JOIN dbo.CHITIET_HD l ON l.MaHD=h.MaHD JOIN dbo.SANPHAM s ON s.MaSP=l.MaSP WHERE h.TrangThai=N'Da thanh toan' AND h.NgayThanhToan>=@f AND h.NgayThanhToan<@u GROUP BY s.TenSP ORDER BY Q DESC,s.TenSP";
      c.Parameters.AddWithValue("@f",b.From);c.Parameters.AddWithValue("@u",b.Until);
      await using var r=await c.ExecuteReaderAsync();while(await r.ReadAsync())expected.Add(r.GetString(0)+" · "+Number(Convert.ToDecimal(r.GetValue(1),CultureInfo.InvariantCulture))+" ly");
     }
     var products=(TableLayoutPanel)Field("products").GetValue(dashboard)!;
     var actual=products.Controls.OfType<OrbitButton>().Select(x=>x.Text).ToArray();
     var bars=products.Controls.Cast<Control>().Count(x=>x.GetType().Name=="Bar");
     Assert(actual.SequenceEqual(expected),string.Join('|',actual)+" != "+string.Join('|',expected));
     Assert(bars==expected.Count,$"bars {bars} != {expected.Count}");
     return new{Top5=actual,Bars=bars};
    });

    // Late query/cancel: two rapid mode changes end in the last requested mode.
    await Check("UI-late-cancel-final-mode","rapid monthly→today settles on today with current values",async()=>
    {
     Button(dashboard,"monthly").PerformClick();Button(dashboard,"today").PerformClick();
     await Wait(dashboard);await Task.Yield();
     Assert(!(bool)Field("month").GetValue(dashboard)!,"month flag");
     var label=((Label)Field("periodLabel").GetValue(dashboard)!).Text;
     Assert(label.StartsWith("Hôm nay"),label);
     return label;
    });

    // Hide during load clears snapshot, dispose during load does not throw.
    await Check("UI-hide-during-load","values cleared, no ready state",async()=>
    {
     var hidden=Build(reports);((Panel)typeof(MainForm).GetField("content",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(main)!).Controls.Add(hidden);
     await Task.Delay(120);hidden.Visible=false;await Task.Delay(800);
     Assert(!Ready(hidden),"ready after hide");
     Assert(Values(hidden).All(v=>v=="—"),string.Join('|',Values(hidden)));
     return new{State=State(hidden).Text,Values=Values(hidden)};
    });
    await Check("UI-dispose-during-load","dispose mid-query without exception",async()=>
    {
     var doomed=Build(reports);((Panel)typeof(MainForm).GetField("content",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(main)!).Controls.Add(doomed);
     await Task.Delay(120);doomed.Dispose();await Task.Delay(1200);
     Assert(errors.Count==0,string.Join('|',errors));
     return new{Disposed=doomed.IsDisposed,Unhandled=errors.Count};
    });

    // Error state: connection points to a database that does not exist.
    await Check("UI-error-no-fake-zero","error message, values '—', refresh enabled",async()=>
    {
     var errorFactory=Factory("CoffeeManager_Test_G07_missing_db",5);
     var errorReports=new ReportService(new ReportRepository(errorFactory),new PermissionService(new AuthRepository(errorFactory)));
     var broken=Build(errorReports);
     ((Panel)typeof(MainForm).GetField("content",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(main)!).Controls.Add(broken);
     var settled=false;for(var i=0;i<3000;i++){var text=State(broken).Text;if(text.StartsWith("Chưa cập nhật")){settled=true;break;}await Task.Delay(10);}
     Assert(settled,"error state not reached: "+State(broken).Text);
     Assert(Values(broken).All(v=>v=="—"),string.Join('|',Values(broken)));
     Assert(Button(broken,"refresh").Enabled,"refresh disabled");
     return new{State=State(broken).Text,Values=Values(broken)};
    });

    // Sources: paging parity against the same period/snapshot.
    await Check("UI-sources-paging","revenue/product sources page and match totals",async()=>
    {
     var b=Bounds(dashboard);
     var page1=await reports.DashboardAnalyticsSourcesAsync(session,"revenue",b.From,b.Until);
     var page2=await reports.DashboardAnalyticsSourcesAsync(session,"revenue",b.From,b.Until,skip:50);
     var (count,_)=await BillTotals(b.From,b.Until);
     Assert(page1.TotalCount==count,$"source total {page1.TotalCount} != {count}");
     Assert(page1.Rows.Count==Math.Min(50,count),$"page1 {page1.Rows.Count}");
     Assert(page2.Rows.Count==Math.Max(0,Math.Min(50,count-50)),$"page2 {page2.Rows.Count}");
     var top=(TableLayoutPanel)Field("products").GetValue(dashboard)!;
     var topText=top.Controls.OfType<OrbitButton>().First().Text;
     var code=(string?)await Scalar($"SELECT TOP 1 l.MaSP FROM dbo.HOADON h JOIN dbo.CHITIET_HD l ON l.MaHD=h.MaHD WHERE h.TrangThai=N'Da thanh toan' AND h.NgayThanhToan>='{b.From:yyyy-MM-ddTHH:mm:ss}' AND h.NgayThanhToan<'{b.Until:yyyy-MM-ddTHH:mm:ss}' GROUP BY l.MaSP ORDER BY SUM(l.SoLuong) DESC,l.MaSP");
     Assert(code is not null,"top product code");
     var product=await reports.DashboardAnalyticsSourcesAsync(session,"product",b.From,b.Until,product:code);
     Assert(product.TotalCount>0,"product source empty");
     return new{RevenueTotal=page1.TotalCount,Page1=page1.Rows.Count,Page2=page2.Rows.Count,Product=code,ProductRows=product.TotalCount,TopText=topText};
    });
    Assert(errors.Count==0,string.Join('|',errors));
   }
  }
  catch(Exception e){failure=e;}
  finally{main.Close();}
 };
 Application.Run(main);
});
thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();
result["Checks"]=checks;result["Failures"]=failures;result["Measurements"]=measurements;result["Unhandled"]=errors;result["Error"]=failure?.ToString();
result["Status"]=failure is null&&failures==0?"PASS":"FAIL";
await File.WriteAllTextAsync(output,JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
if(failure is not null||failures!=0)Environment.ExitCode=1;
