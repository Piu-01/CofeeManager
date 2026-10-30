using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using Microsoft.Data.SqlClient;

if (args.Length != 2) throw new ArgumentException("Manifest and result path required.");
using var manifest = JsonDocument.Parse(File.ReadAllText(args[0]));
var m = manifest.RootElement;
var db = m.GetProperty("Database").GetString()!;
var marker = m.GetProperty("Marker").GetString()!;
if (db != "CoffeeManager_Test_G04_20261024_152759_12204643_manual" || marker != "20261024_152759_12204643" || m.GetProperty("Cleaned").GetBoolean()) throw new InvalidOperationException("Owned fixture required.");
await using var owner = new SqlConnection($"Server=localhost;Initial Catalog={db};Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False");
await owner.OpenAsync();
async Task<object?> Scalar(string sql) { await using var cmd = owner.CreateCommand(); cmd.CommandText=sql; return await cmd.ExecuteScalarAsync(); }
async Task Execute(string sql, string? hash=null) { await using var cmd=owner.CreateCommand(); cmd.CommandText=sql; if(hash is not null)cmd.Parameters.AddWithValue("@hash",hash); await cmd.ExecuteNonQueryAsync(); }
if ((string?)await Scalar("SELECT RunId FROM dbo.G04TestMarker") != marker || Convert.ToInt32(await Scalar("SELECT MAX(Version) FROM dbo.SchemaMigrations")) != 16) throw new InvalidOperationException("Marker/schema mismatch.");
if (Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM dbo.NHANVIEN WHERE MaNV IN('NVR01','NVR02','NVR03')")) != 0) throw new InvalidOperationException("Fixture codes already used.");
var runtime = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("G04_AUTH_RUNTIME") ?? throw new InvalidOperationException("Runtime profile required."));
if(runtime.DataSource != "localhost" || runtime.InitialCatalog != db || runtime.IntegratedSecurity)throw new InvalidOperationException("Expected actual runtime login on test DB.");
var auth = new AuthService(new AuthRepository(new ConnectionFactory(runtime.ConnectionString)),new PasswordService());
var permissions = new PermissionService(new AuthRepository(new ConnectionFactory(runtime.ConnectionString)));
var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
var results = new List<object>(); var failed=0; var provisioned=false;
async Task Case(string name, Func<Task> test) { try { await test(); results.Add(new {Name=name,Outcome="PASS"}); } catch(Exception ex) { failed++; results.Add(new {Name=name,Outcome="FAIL",Error=ex.GetType().Name,Message=ex.Message}); } }
void Check(bool value) { if(!value)throw new InvalidOperationException("Assertion failed."); }
async Task Reject(Func<Task> operation) { try {await operation();}catch(UnauthorizedAccessException){return;}throw new InvalidOperationException("Expected authentication/permission rejection."); }
Task<SessionContext> Login(string role="cash") => auth.LoginAsync(new LoginRequest("g04.reg."+role,password));
try {
    await Execute("""
      BEGIN TRANSACTION;
      INSERT dbo.NHANVIEN(MaNV,TenNV,LoaiHopDong,MaCV) SELECT x.Code,N'G04 auth regression','Full-time',(SELECT TOP(1) MaCV FROM dbo.CHUCVU ORDER BY MaCV) FROM (VALUES('NVR01'),('NVR02'),('NVR03'))x(Code);
      INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT,HoatDong) VALUES
      ('NVR01','g04.reg.admin',@hash,'ADMIN',1),('NVR02','g04.reg.cash',@hash,'CASH',1),('NVR03','g04.reg.store',@hash,'STORE',1);
      COMMIT;
      """,new PasswordService().Hash(password)); provisioned=true;
    foreach(var role in new[]{"admin","cash","store"}) await Case("Login correct "+role,async()=>{using var s=await Login(role);Check(s.Identity.Role==role.ToUpperInvariant());await permissions.DemandAsync(s,AppPermission.SystemInfo);});
    await Case("Wrong password rejected",()=>Reject(async()=>{using var s=await auth.LoginAsync(new LoginRequest("g04.reg.cash",password+"wrong"));}));
    await Case("Unknown account rejected",()=>Reject(async()=>{using var s=await auth.LoginAsync(new LoginRequest("g04.reg.absent",password));}));
    await Case("CASH sales allowed finance denied",async()=>{using var s=await Login();await permissions.DemandAsync(s,AppPermission.Sales);await Reject(()=>permissions.DemandAsync(s,AppPermission.Finance));Check(!s.IsRevoked);});
    await Case("STORE sales denied",async()=>{using var s=await Login("store");await Reject(()=>permissions.DemandAsync(s,AppPermission.Sales));});
    await Case("ADMIN sales and finance allowed",async()=>{using var s=await Login("admin");await permissions.DemandAsync(s,AppPermission.Sales);await permissions.DemandAsync(s,AppPermission.Finance);});
    await Case("Lock rejects login and revokes old session",async()=>{using var s=await Login();try{await Execute("UPDATE dbo.TAIKHOAN SET HoatDong=0 WHERE MaNV='NVR02'");await Reject(async()=>{using var n=await Login();});await Reject(()=>permissions.DemandAsync(s,AppPermission.Sales));Check(s.IsRevoked&&s.Token.IsCancellationRequested);}finally{await Execute("UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV='NVR02'");}});
    await Case("Employee left rejects login and revokes session",async()=>{using var s=await Login();try{await Execute("UPDATE dbo.NHANVIEN SET DangLamViec=0 WHERE MaNV='NVR02'");await Reject(async()=>{using var n=await Login();});await Reject(()=>permissions.DemandAsync(s,AppPermission.Sales));Check(s.IsRevoked);}finally{await Execute("UPDATE dbo.NHANVIEN SET DangLamViec=1 WHERE MaNV='NVR02'");}});
    await Case("Role change invalidates old security stamp",async()=>{using var s=await Login();try{await Execute("UPDATE dbo.TAIKHOAN SET MaVT='STORE' WHERE MaNV='NVR02'");await Reject(()=>permissions.DemandAsync(s,AppPermission.Sales));Check(s.IsRevoked);using var next=await Login();Check(next.Identity.Role=="STORE");await Reject(()=>permissions.DemandAsync(next,AppPermission.Sales));}finally{await Execute("UPDATE dbo.TAIKHOAN SET MaVT='CASH' WHERE MaNV='NVR02'");}});
    await Case("Password change rejects old password and old session",async()=>{using var s=await Login();try{await Execute("UPDATE dbo.TAIKHOAN SET MatKhauHash=@hash WHERE MaNV='NVR02'",new PasswordService().Hash(password+"changed"));await Reject(async()=>{using var n=await Login();});await Reject(()=>permissions.DemandAsync(s,AppPermission.Sales));Check(s.IsRevoked);using var changed=await auth.LoginAsync(new LoginRequest("g04.reg.cash",password+"changed"));Check(changed.Identity.Role=="CASH");}finally{await Execute("UPDATE dbo.TAIKHOAN SET MatKhauHash=@hash WHERE MaNV='NVR02'",new PasswordService().Hash(password));}});
    await Case("Logout disposal invalidates old session, relogin new ID",async()=>{var s=await Login();var id=s.Id;var token=s.Token;s.Dispose();Check(s.IsRevoked&&token.IsCancellationRequested);await Reject(()=>permissions.DemandAsync(s,AppPermission.Sales));using var next=await Login();Check(next.Id!=id&&!next.IsRevoked);});
    await Case("Cancelled login returns no session",async()=>{using var cancel=new CancellationTokenSource();cancel.Cancel();try{using var s=await auth.LoginAsync(new LoginRequest("g04.reg.cash",password),cancel.Token);}catch(OperationCanceledException){return;}throw new InvalidOperationException("Expected cancellation.");});
} finally {
    if(provisioned)await Execute("DELETE dbo.TAIKHOAN WHERE MaNV IN('NVR01','NVR02','NVR03'); DELETE dbo.NHANVIEN WHERE MaNV IN('NVR01','NVR02','NVR03');");
    var remaining=Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM dbo.NHANVIEN WHERE MaNV IN('NVR01','NVR02','NVR03')"));
    var state=Convert.ToString(await Scalar("DECLARE @t int=@@TRANCOUNT,@s int=XACT_STATE();SELECT CONCAT(@t,':',@s);"));
    await File.WriteAllTextAsync(args[1],JsonSerializer.Serialize(new{Database=db,Schema=16,Runtime="Existing application runtime login; no new server login",Results=results,Failed=failed,FixtureEmployeesRemaining=remaining,OwnerTransactionState=state,GuiScope="Separate; no UI credential input automated"},new JsonSerializerOptions{WriteIndented=true}));
    if(remaining!=0||state!="0:0")failed++;
}
Console.WriteLine($"Auth regression: {results.Count-failed}/{results.Count} PASS; fixture accounts removed.");
return failed==0?0:1;
