using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using CafeManager.Models.Requests.Sales;
using Microsoft.Data.SqlClient;

var manifest=JsonDocument.Parse(File.ReadAllText(args[0])).RootElement;
var db=manifest.GetProperty("Database").GetString()!;
if(manifest.GetProperty("Cleaned").GetBoolean()||db!="CoffeeManager_Test_G04_"+manifest.GetProperty("Marker").GetString()+"_checkout")throw new InvalidOperationException("Wrong fixture");
await using var owner=new SqlConnection($"Server=localhost;Initial Catalog={db};Integrated Security=True;TrustServerCertificate=True;Pooling=False");await owner.OpenAsync();
await using var cmd=owner.CreateCommand();cmd.CommandText="SELECT RunId FROM dbo.G04TestMarker";if((string?)await cmd.ExecuteScalarAsync()!=manifest.GetProperty("Marker").GetString())throw new InvalidOperationException("Marker mismatch");
var password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
cmd.CommandText="INSERT dbo.NHANVIEN(MaNV,TenNV,LoaiHopDong,MaCV) SELECT 'NDG01',N'G05 dependency fixture','Full-time',MIN(MaCV) FROM dbo.CHUCVU; INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT,HoatDong) VALUES('NDG01','delivery.g05.fixture',@hash,'CASH',1);";cmd.Parameters.AddWithValue("@hash",new PasswordService().Hash(password));await cmd.ExecuteNonQueryAsync();
var builder=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CHECKOUT_SQL_RUNTIME")??throw new InvalidOperationException("Runtime missing"));if(builder.InitialCatalog!=db||builder.IntegratedSecurity)throw new InvalidOperationException("Runtime target");
var f=new ConnectionFactory(builder.ConnectionString);var authRepo=new AuthRepository(f);using var session=await new AuthService(authRepo,new PasswordService()).LoginAsync(new LoginRequest("delivery.g05.fixture",password));
var sales=new SalesService(new SalesRepository(f),new PermissionService(authRepo));var rows=new List<object>();
foreach(var scenario in new[]{"G05 giao tiến độ chưa trả","G05 hủy đơn giao mở"})
{
    var code=await sales.AllocateAsync(session);await sales.CreateAsync(session,new(code,Guid.NewGuid(),new("Giao hang",Recipient:"G05 kiểm thử",Phone:"0900000026",Address:"Địa chỉ fixture DB test"),"SP001",1,scenario));
    rows.Add(new{Scenario=scenario,Code=code,Payable=81000,Customer="Vãng lai",Status="Chua thanh toan"});
}
File.WriteAllText(args[1],JsonSerializer.Serialize(new{Database=db,Cases=rows,Note="Created with real AuthService and SalesService in isolated test DB; these are fixtures, no acceptance cases passed yet"},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine("Prepared two delivery orders in isolated test DB; no checkout submitted.");

