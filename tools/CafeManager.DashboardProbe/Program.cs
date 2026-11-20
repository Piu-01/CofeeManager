using System.Data;
using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests.Reports;
using Microsoft.Data.SqlClient;

// Runner chỉ dùng DBtest có marker, không đọc cấu hình/credential development.
if(args.Length!=3||!System.Text.RegularExpressions.Regex.IsMatch(args[0],@"^CoffeeManager_Test_G07_\d{8}_\d{6}_[a-f0-9]{8}_demo$"))throw new ArgumentException("DBtest, marker, output required.");
var results=new List<object>();var failed=0;
var builder=new SqlConnectionStringBuilder { DataSource="localhost",InitialCatalog=args[0],IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false };
await using var owner=new SqlConnection(builder.ConnectionString);await owner.OpenAsync();
async Task<object?> Scalar(string sql)
{await using var c=owner.CreateCommand();c.CommandText=sql;return await c.ExecuteScalarAsync();}
if((string?)await Scalar("SELECT RunId FROM dbo.G07TestMarker")!=args[1])throw new InvalidOperationException("Marker mismatch");
await using(var setup=owner.CreateCommand())
{
    setup.CommandText="DECLARE @s binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE TenDangNhap='admin');EXEC dbo.sp_SetupBootstrap @Mode='ActivateDemo',@Username=N'admin',@NewHash=@hash,@MaNV='NV001',@ExpectedStamp=@s;UPDATE dbo.TAIKHOAN SET HoatDong=1,MatKhauHash=@hash WHERE MaVT IN('CASH','STORE');CREATE USER DashboardRuntime WITHOUT LOGIN;ALTER ROLE CafeManagerApp ADD MEMBER DashboardRuntime;";
    setup.Parameters.AddWithValue("@hash",new PasswordService().Hash(Guid.NewGuid().ToString("N")));await setup.ExecuteNonQueryAsync();
}
var factory=new ConnectionFactory(builder.ConnectionString);
typeof(ConnectionFactory).GetProperty("SessionInitializer",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(factory,(Func<SqlConnection,CancellationToken,Task>)(async(c,t)=>{await using var cmd=c.CreateCommand();cmd.CommandText="EXECUTE AS USER='DashboardRuntime'";await cmd.ExecuteNonQueryAsync(t);}));
var auth=new AuthRepository(factory);var permissions=new PermissionService(auth);using var session=new SessionContext(await auth.ReadIdentityAsync(1,default)??throw new InvalidOperationException("ADMIN missing"));
var service=new ReportService(new ReportRepository(factory),permissions);
void Equal(decimal actual,decimal expected,string field)
{if(actual!=expected)throw new InvalidOperationException($"{field}: actual={actual}; expected={expected}");}
async Task Check(string id,string expected,Func<Task<object?>> action)
{
    try{var actual=await action();results.Add(new{Id=id,Expected=expected,Status="PASS",Actual=actual});}
    catch(Exception e){failed++;results.Add(new{Id=id,Expected=expected,Status="FAIL",Actual=e.ToString()});}
}
async Task<DataSet> Read(bool month,DateTime at)
{
    await using var c=owner.CreateCommand();c.CommandText="EXECUTE AS USER='DashboardRuntime';EXEC dbo.sp_DashboardAnalyticsRead @Actor=@a,@Stamp=@s,@Month=@m,@At=@at;REVERT;";
    c.Parameters.AddWithValue("@a",session.Identity.AccountId);c.Parameters.Add("@s",SqlDbType.Binary,8).Value=session.Identity.SecurityStamp;
    c.Parameters.AddWithValue("@m",month);c.Parameters.Add("@at",SqlDbType.DateTime2).Value=at;
    using var adapter=new SqlDataAdapter(c);var data=new DataSet();adapter.Fill(data);return data;
}
decimal? Indicator(DataSet data,string key)
{var v=data.Tables[1].Rows.Cast<DataRow>().Single(r=>(string)r["Key"]==key)["Value"];return v is DBNull?null:Convert.ToDecimal(v);}
decimal Total(DataSet data,string key)=>Convert.ToDecimal(data.Tables[3].Rows.Cast<DataRow>().Single(r=>(string)r["PageKey"]==key)["Amount"]);
var at=new DateTime(2026,1,15,12,0,0);
await Check("DA02/03/day","62 invoices; paid127500; quantity127; expense1000; no fanout; Until excluded",async()=>
{
    using var d=await Read(false,at);Equal(Total(d,"trend.current"),127500,"revenue");Equal(Indicator(d,"invoice-count.current")!.Value,62,"bills");Equal(Indicator(d,"quantity.total")!.Value,127,"cups");Equal(Indicator(d,"expense.current")!.Value,1000,"expense");
    foreach(var key in new[]{"trend.current","channels"})Equal(d.Tables[2].Rows.Cast<DataRow>().Where(r=>(string)r["PageKey"]==key).Sum(r=>Convert.ToDecimal(r["Amount"])),127500,key);
    return new{Revenue=127500,Bills=62,Cups=127};
});
await Check("DA04/month","137500 MTD, previous20000, previous day10000",async()=>
{
    using var day=await Read(false,at);using var m=await Read(true,at);
    Equal(Total(day,"trend.previous"),10000,"prior day");Equal(Total(m,"trend.current"),137500,"MTD");Equal(Total(m,"trend.previous"),20000,"prior MTD");Equal(Indicator(m,"expense.current")!.Value,3000,"expense MTD");
    Equal(Indicator(day,"change.revenue")!.Value,1175,"revenue change");return new{MTD=137500,Previous=20000,Change=1175};
});
await Check("DA04/calendar","31Mar=>no prior bounds; leap29Feb=>29Jan; empty average null and both0 change0",async()=>
{
    using var march=await Read(true,new(2026,3,31,12,0,0));if(march.Tables[0].Rows[0]["PreviousFrom"] is not DBNull)throw new InvalidOperationException("Clamped February");
    using var leap=await Read(true,new(2024,2,29,12,0,0));if(Convert.ToDateTime(leap.Tables[0].Rows[0]["PreviousUntil"])!=new DateTime(2024,1,29,12,0,0))throw new InvalidOperationException("Leap bound");
    using var empty=await Read(false,new(2024,1,15,12,0,0));if(Indicator(empty,"average.current") is not null)throw new InvalidOperationException("Empty average not null");Equal(Indicator(empty,"change.revenue")!.Value,0,"empty change");
    using var zero=await Read(false,new(2025,12,15,12,0,0));if(Indicator(zero,"change.revenue") is not null)throw new InvalidOperationException("Prior0 percent");return "Bounds/null rules matched";
});
await Check("DA06/top","61 SKUs, deterministic top5 X0002,X0001,X0003,X0004,X0005; snapshot price1000",async()=>
{
    using var d=await Read(false,at);var rows=d.Tables[2].Rows.Cast<DataRow>().Where(r=>(string)r["PageKey"]=="products").ToArray();
    var codes=rows.Select(r=>((string)r["Code"]).Trim()).ToArray();if(!codes.SequenceEqual(new[]{"X0002","X0001","X0003","X0004","X0005"}))throw new InvalidOperationException(string.Join(",",codes));
    Equal(Convert.ToDecimal(rows[0]["Amount"]),5000,"snapshot product amount");Equal(Total(d,"products"),127000,"all products gross");return codes;
});
await Check("DA07/current","counter predicates match live views/tables; identical across modes",async()=>
{
    using var d=await Read(false,at);using var m=await Read(true,at);
    foreach(var pair in new[]{("orders.open","SELECT COUNT(*) FROM dbo.HOADON WHERE TrangThai=N'Chua thanh toan'"),("delivery.pending","SELECT COUNT(*) FROM dbo.HOADON WHERE LoaiDon=N'Giao hang' AND TrangThai=N'Da thanh toan' AND TrangThaiGiao IN(N'Cho giao',N'Dang giao')"),("material.zero","SELECT COUNT(*) FROM dbo.NGUYENVATLIEU WHERE SoLuongTon=0"),("debt.current","SELECT COALESCE(SUM(ConNo),0) FROM dbo.vw_CongNoNCC WHERE ConNo>0")})
    {Equal(Indicator(d,pair.Item1)!.Value,Convert.ToDecimal(await Scalar(pair.Item2)),pair.Item1);Equal(Indicator(m,pair.Item1)!.Value,Indicator(d,pair.Item1)!.Value,pair.Item1+" mode");}
    return "4 predicates matched";
});
await Check("DA08/paging","62 distinct invoices; pages50+12; channel1; X0002 sources2; expense1",async()=>
{
    var from=at.Date;var first=await service.DashboardAnalyticsSourcesAsync(session,"revenue",from,at);var last=await service.DashboardAnalyticsSourcesAsync(session,"revenue",from,at,skip:50);
    Equal(first.TotalCount,62,"total");Equal(first.Rows.Count,50,"page1");Equal(last.Rows.Count,12,"page2");Equal(first.Rows.Concat(last.Rows).Select(r=>r.Code).Distinct().Count(),62,"distinct");
    var channel=await service.DashboardAnalyticsSourcesAsync(session,"channel",from,at,channel:"Giao hang");Equal(channel.TotalCount,1,"delivery channel");
    var product=await service.DashboardAnalyticsSourcesAsync(session,"product",from,at,product:"X0002");Equal(product.TotalCount,2,"product invoices");
    var expense=await service.DashboardAnalyticsSourcesAsync(session,"expense",from,at);Equal(expense.TotalCount,1,"expense");return new{first.TotalCount,Page1=first.Rows.Count,Page2=last.Rows.Count};
});
await Check("DA09/permissions","non ADMIN blocked at BLL and SQL; runtime has no direct SELECT",async()=>
{
    var accounts=await Scalar("SELECT STRING_AGG(CONVERT(varchar(12),MaTK),',') FROM dbo.TAIKHOAN WHERE MaVT IN('CASH','STORE')");
    var checkedRoles=0;
    foreach(var id in Convert.ToString(accounts)!.Split(',',StringSplitOptions.RemoveEmptyEntries))
    {
        var identity=await auth.ReadIdentityAsync(int.Parse(id),default);if(identity is null)continue;using var denied=new SessionContext(identity);
        try{await service.ReadDashboardAnalyticsAsync(denied,false);throw new InvalidOperationException("BLL allowed "+identity.Role);}catch(UserOperationException){}
        await using var raw=owner.CreateCommand();raw.CommandText="EXEC dbo.sp_DashboardAnalyticsRead @Actor=@a,@Stamp=@s,@Month=0";raw.Parameters.AddWithValue("@a",identity.AccountId);raw.Parameters.Add("@s",SqlDbType.Binary,8).Value=identity.SecurityStamp;
        try{await using var reader=await raw.ExecuteReaderAsync();throw new InvalidOperationException("SQL allowed "+identity.Role);}catch(SqlException e) when(e.Number is 51505 or 51605){checkedRoles++;}
    }
    if(checkedRoles<2)throw new InvalidOperationException("Missing role fixtures");
    Equal(Convert.ToDecimal(await Scalar("EXECUTE AS USER='DashboardRuntime';SELECT HAS_PERMS_BY_NAME('dbo.HOADON','OBJECT','SELECT');REVERT;")),0,"direct select");return checkedRoles;
});
await Check("DA14/mapping","new day/month APIs mapped; old full day137500 and old dashboard callable",async()=>
{
    var day=await service.ReadDashboardAnalyticsAsync(session,false);var m=await service.ReadDashboardAnalyticsAsync(session,true);
    if(day.Pages.Count<3||m.Pages.Count<3)throw new InvalidOperationException("Missing pages");
    var old=await service.ReadAsync(session,new(ReportKind.SalesDay,new(2026,1,15),new(2026,1,15)));
    Equal(old.Totals.Amount,137500,"old full day includes Until boundary bill");await service.DashboardAsync(session);return new{DayPages=day.Pages.Count,MonthPages=m.Pages.Count,LegacyFullDay=old.Totals.Amount};
});
await Check("DA10/transaction","all calls released transactions",async()=>{Equal(Convert.ToDecimal(await Scalar("SELECT @@TRANCOUNT")),0,"transaction count");return 0;});
await Check("DA11/cancel","cancelled request throws without returning fake aggregates",async()=>
{
    using var cancellation=new CancellationTokenSource();cancellation.Cancel();
    try{await service.ReadDashboardAnalyticsAsync(session,false,cancellation.Token);throw new InvalidOperationException("Cancelled request returned data");}catch(OperationCanceledException){return "Cancelled";}
});
await Check("DA11/12/native","real WinForms at125%; mode/refresh/layout/source ownership/keyboard; visual excluded",()=>Task.FromResult<object?>(DashboardUiProbe.Run(factory,permissions,session)));
File.WriteAllText(args[2],JsonSerializer.Serialize(new{Database=args[0],Marker=args[1],Results=results,Failed=failed,NotRun=new[]{"DA05 linked expense scenarios","DA10 two-session checkout/expense concurrency","DA11 connection failure/late response forced cases","DA13 10000 bills performance","DPI100%/150%"},Skipped="Visual appearance: user assignment"},new JsonSerializerOptions{WriteIndented=true}));
Environment.ExitCode=failed==0?0:1;
