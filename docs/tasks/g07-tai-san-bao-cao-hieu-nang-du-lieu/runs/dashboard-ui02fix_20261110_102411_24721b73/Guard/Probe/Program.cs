using System.Data;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using Microsoft.Data.SqlClient;
if(args.Length!=5)throw new ArgumentException("currentDB,guardDB,marker,output,phase");
foreach(var name in args.Take(2))if(!System.Text.RegularExpressions.Regex.IsMatch(name,@"^CoffeeManager_Test_G07_\d{8}_\d{6}_[a-f0-9]{8}_demo$"))throw new ArgumentException("Invalid DB");
string Connection(string db)=>new SqlConnectionStringBuilder{DataSource="localhost",InitialCatalog=db,IntegratedSecurity=true,Encrypt=true,TrustServerCertificate=true,Pooling=false}.ConnectionString;
await using var guard=new SqlConnection(Connection(args[1]));await guard.OpenAsync();await using(var c=guard.CreateCommand()){c.CommandText="SELECT RunId FROM dbo.G07TestMarker";if((string?)await c.ExecuteScalarAsync()!=args[2])throw new InvalidOperationException("Marker mismatch");}
var cases=new List<object>();
if(args[4]=="guard"){
 var auth=new AuthRepository(new ConnectionFactory(Connection(args[0])));using var session=new SessionContext((await auth.ReadIdentityAsync(1))!);var reports=new ReportService(new ReportRepository(new ConnectionFactory(Connection(args[1]))),new PermissionService(auth));
 int? number=null;string? message=null;try{await reports.ReadDashboardAnalyticsAsync(session,false);}catch(UserOperationException e){number=e.Error.Number;message=e.Message;}
 if(number!=53502||message is null||!message.Contains("028"))throw new InvalidOperationException("Missing schema guard "+number+" "+message);
 cases.Add(new{Id="DA11/schema027",Status="PASS",Number=number,Message=message});
 const string points="SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),(SELECT MaKH,DiemTichLuy FROM dbo.KHACHHANG ORDER BY MaKH FOR JSON PATH))),2)";
 await using var current=new SqlConnection(Connection(args[0]));await current.OpenAsync();await using var a=current.CreateCommand();a.CommandText=points;await using var z=guard.CreateCommand();z.CommandText=points;var first=await a.ExecuteScalarAsync();var second=await z.ExecuteScalarAsync();if(!Equals(first,second))throw new InvalidOperationException("Unexpected points mutation in no-customer fixture");cases.Add(new{Id="DA10/points",Status="PASS",Hash=first});
}else{
 await using(var c=guard.CreateCommand()){c.CommandText="DECLARE @s binary(8)=(SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE MaTK=1);EXEC dbo.sp_SetupBootstrap @Mode='ActivateDemo',@Username=N'admin',@NewHash=@hash,@MaNV='NV001',@ExpectedStamp=@s";c.Parameters.AddWithValue("@hash",new PasswordService().Hash(Guid.NewGuid().ToString("N")));await c.ExecuteNonQueryAsync();}
 var auth=new AuthRepository(new ConnectionFactory(Connection(args[1])));using var session=new SessionContext((await auth.ReadIdentityAsync(1))!);var reports=new ReportService(new ReportRepository(new ConnectionFactory(Connection(args[1]))),new PermissionService(auth));
 var from=new DateTime(2025,4,2);var until=from.AddDays(1);var source=await reports.DashboardAnalyticsSourcesAsync(session,"revenue",from,until);if(!source.Rows.Any(r=>r.Code=="HD20259999"&&r.SourceDate==until.AddSeconds(-1)&&r.Amount==25000))throw new InvalidOperationException("23:59:59 bill excluded");
 var before=await reports.DashboardAnalyticsSourcesAsync(session,"revenue",from,until.AddSeconds(-1));if(before.Rows.Any(r=>r.Code=="HD20259999"))throw new InvalidOperationException("Exact Until included");
 cases.Add(new{Id="DA02/235959",Status="PASS",Included=25000,ExactUntilExcluded=true});
}
File.WriteAllText(args[3],JsonSerializer.Serialize(cases,new JsonSerializerOptions{WriteIndented=true}));
