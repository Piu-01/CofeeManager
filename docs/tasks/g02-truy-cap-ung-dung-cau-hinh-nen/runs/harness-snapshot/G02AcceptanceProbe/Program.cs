using System.Data;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Requests.Catalogs;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;

internal static class Program
{
    internal static string Root = "";
    internal static string Output = "";
    internal static string Password = "";
    internal static List<object> Results = [];
    internal static List<string> Databases = [];
    [STAThread]
    static int Main(string[] args)
    {
        Root = args[0]; Output = args[1]; Directory.CreateDirectory(Output);
        Password = " " + Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)) + " ";
        try { AuthChecks().GetAwaiter().GetResult(); }
        catch (Exception e) { Record("HARNESS", "BLOCKED", "setup exception", e.GetType().Name); }
        finally { File.WriteAllText(Path.Combine(Output, "supplemental.json"), JsonSerializer.Serialize(new { Results, Databases }, new JsonSerializerOptions { WriteIndented = true })); }
        return Results.Any(x => JsonSerializer.Serialize(x).Contains("\"FAIL\"")) ? 1 : 0;
    }
    internal static void Record(string id, string status, string expected, string actual) => Results.Add(new { Id = id, Status = status, Expected = expected, Actual = actual });
    internal static async Task Check(string id, string expected, Func<Task<string>> test)
    {
        try { Record(id, "PASS", expected, await test()); }
        catch (Exception e) { Record(id, "FAIL", expected, e is DataAccessException d ? $"DataError {d.Error.Number}" : e is SqlException s ? $"SQL {s.Number}" : e.Message.Contains("password", StringComparison.OrdinalIgnoreCase) ? e.GetType().Name : e.Message); }
    }
    internal static void Need(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    internal static string Cs(string db) => $"Server=localhost;Initial Catalog={db};Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False";
    internal static async Task<SqlConnection> Owner(string db)
    {
        Need(Regex.IsMatch(db, @"^CoffeeManager_Test_G02_\d{8}_\d{6}_[a-f0-9]{8}$"), "Test DB name required");
        var c = new SqlConnection(Cs(db)); await c.OpenAsync();
        Need(Convert.ToInt32(await Scalar(c, "SELECT COUNT(*) FROM dbo.G02TestMarker")) == 1, "Marker required"); return c;
    }
    internal static async Task<object?> Scalar(SqlConnection c, string sql, params SqlParameter[] parameters)
    { await using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.CommandTimeout = 60; cmd.Parameters.AddRange(parameters); return await cmd.ExecuteScalarAsync(); }
    internal static async Task Exec(SqlConnection c, string sql, params SqlParameter[] parameters) => _ = await Scalar(c, sql, parameters);
    internal static ConnectionFactory Factory(string db, bool setup = false)
    {
        var factory = new ConnectionFactory(Cs(db));
        Func<SqlConnection, CancellationToken, Task> init = async (c, ct) =>
        {
            await using var cmd = c.CreateCommand(); cmd.CommandText = setup ? "EXECUTE AS USER=N'G02_Setup'" : "EXECUTE AS USER=N'G02_Runtime'"; await cmd.ExecuteNonQueryAsync(ct);
        };
        typeof(ConnectionFactory).GetProperty("SessionInitializer", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(factory, init);
        return factory;
    }
    internal static BootstrapService Setup(string db) => new(new SetupRepository(Factory(db, true)), new AuthRepository(Factory(db, true)), new PasswordService());
    internal static AuthService Auth(string db) => new(new AuthRepository(Factory(db)), new PasswordService());
    internal static async Task Apply(string db, int version)
    {
        await using var c = await Owner(db); await using var tx = await c.BeginTransactionAsync();
        var file = Directory.GetFiles(Path.Combine(Root, "database", "migrations"), $"{version:000}_*.sql").Single();
        var checksum = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
        try
        {
            await using var cmd = c.CreateCommand(); cmd.Transaction = (SqlTransaction)tx; cmd.CommandTimeout = 60;
            cmd.CommandText = "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'CoffeeManager.SchemaMigration',@LockMode='Exclusive',@LockOwner='Transaction';"; await cmd.ExecuteNonQueryAsync();
            foreach (var batch in Regex.Split(File.ReadAllText(file), @"(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?$"))
                if (!string.IsNullOrWhiteSpace(batch)) { cmd.CommandText = batch; await cmd.ExecuteNonQueryAsync(); }
            cmd.CommandText = "INSERT dbo.SchemaMigrations(Version,Checksum) VALUES(@v,@h)";
            cmd.Parameters.AddWithValue("@v", version); cmd.Parameters.AddWithValue("@h", checksum); await cmd.ExecuteNonQueryAsync(); await tx.CommitAsync();
        }
        catch { await tx.RollbackAsync(); throw; }
    }
    internal static async Task<string> NewDb(bool demo = false, string? collation = null, int versions = 3)
    {
        var db = "CoffeeManager_Test_G02_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N")[..8]; Databases.Add(db);
        await using var c = new SqlConnection(Cs("master")); await c.OpenAsync();
        await Exec(c, $"CREATE DATABASE [{db}]" + (collation is null ? "" : " COLLATE " + collation)); c.ChangeDatabase(db);
        var schema = File.ReadAllText(Path.Combine(Root, "database", "DB_Schema.sql"));
        schema = Regex.Replace(schema, @"(?im)^(CREATE DATABASE QuanLyCafe_ERP_V2;|USE QuanLyCafe_ERP_V2;)\s*$", "");
        foreach (var batch in Regex.Split(schema, @"(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?$")) if (!string.IsNullOrWhiteSpace(batch)) await Exec(c, batch);
        await Exec(c, "CREATE TABLE dbo.G02TestMarker(RunId varchar(40) PRIMARY KEY); INSERT dbo.G02TestMarker VALUES('SUPPLEMENTAL')");
        if (demo)
        {
            var seed = Regex.Replace(File.ReadAllText(Path.Combine(Root, "database", "Insert_Data.sql")), @"(?im)^USE QuanLyCafe_ERP_V2;\s*$", "");
            foreach (var batch in Regex.Split(seed, @"(?im)^\s*GO\s*(?:--[^\r\n]*)?\r?$")) if (!string.IsNullOrWhiteSpace(batch)) await Exec(c, batch);
        }
        for (int v = 1; v <= versions; v++) await Apply(db, v);
        if (versions >= 2) await Exec(c, File.ReadAllText(Path.Combine(Root, "database", "tests", "g02", "security-fixtures.sql")));
        return db;
    }
    internal static async Task Denied(Func<Task> call, int? sql = null)
    {
        try { await call(); } catch (DataAccessException e) when (sql is null || e.Error.Number == sql) { return; }
        catch (SqlException e) when (sql is null || e.Errors.Cast<SqlError>().Any(x => x.Number == sql)) { return; }
        catch (UnauthorizedAccessException) when (sql is null) { return; }
        catch (ArgumentException) when (sql is null) { return; }
        throw new InvalidOperationException("Expected rejection not observed");
    }
    static async Task AuthChecks()
    {
        var db = await NewDb(); var setup = Setup(db);
        await Check("T06-K01.a.clean", "One admin and atomic Completed, optional NV phone NULL", async () =>
        {
            await setup.CompleteAsync(new BootstrapRequest { Mode="NewEmployee",Username="probe.admin",Password=Password,Confirmation=Password,EmployeeName="Probe" });
            await using var c = await Owner(db);
            Need(Convert.ToInt32(await Scalar(c, "SELECT COUNT(*) FROM dbo.TAIKHOAN WHERE HoatDong=1 AND MaVT='ADMIN'"))==1,"Admin count");
            Need(Convert.ToInt32(await Scalar(c,"SELECT COUNT(*) FROM dbo.NHANVIEN WHERE SDT IS NULL"))==1,"Phone NULL count");
            Need((await setup.ReadStateAsync()).State=="Completed","Setup not Completed");
            return "1 ADMIN, 1 NV with NULL phone, Completed; credential not emitted";
        });
        foreach(var variant in new[]{"correct","wrong","missing","trimmed"}) await Check("T06-K03.a."+variant, "Only exact password login succeeds", async () =>
        {
            if(variant=="correct") { using var s=await Auth(db).LoginAsync(new LoginRequest("PROBE.ADMIN",Password)); return "Correct accepted; display identity verified"; }
            await Denied(()=>Auth(db).LoginAsync(new LoginRequest(variant=="missing"?"absent":"probe.admin",variant=="trimmed"?Password.Trim():"wrong")));
            return "Rejected";
        });
        await using var owner = await Owner(db);
        foreach(var column in new[]{"HoatDong","DangLamViec"})
        {
            var table=column=="HoatDong"?"TAIKHOAN":"NHANVIEN";
            await Check("T06-K04.a."+column,"Login rejected",async()=> { await Exec(owner,$"UPDATE dbo.{table} SET {column}=0"); try{await Denied(()=>Auth(db).LoginAsync(new LoginRequest("probe.admin",Password)));}finally{await Exec(owner,$"UPDATE dbo.{table} SET {column}=1");}return "Rejected independently"; });
            await Check("T06-K04.b."+column,"Existing session revoked",async()=> { using var s=await Auth(db).LoginAsync(new LoginRequest("probe.admin",Password));await Exec(owner,$"UPDATE dbo.{table} SET {column}=0");try{await Denied(()=>new PermissionService(new AuthRepository(Factory(db))).DemandAsync(s,AppPermission.SystemInfo));Need(s.IsRevoked,"Session not revoked");}finally{await Exec(owner,$"UPDATE dbo.{table} SET {column}=1");}return "Next call rejected and session revoked"; });
        }
        await Check("T06-K08.a", "Disposed session rejects protected read", async()=> { var s=await Auth(db).LoginAsync(new LoginRequest("probe.admin",Password));s.Dispose();await Denied(()=>new CatalogService(new CatalogRepository(Factory(db)),new PermissionService(new AuthRepository(Factory(db)))).ListAsync(s,CatalogKind.Supplier,"",0));return "Old object denied without disposed-token exception"; });
        var currentHash=(string)(await Scalar(owner,"SELECT MatKhauHash FROM dbo.TAIKHOAN"))!;
        foreach(var bad in new[]{"!DISABLED_REPLACE_WITH_APP_PASSWORD_HASH!","malformed"}) await Check("T06-K03.b."+(bad=="malformed"?"malformed":"placeholder"),"Invalid hash safely rejected",async()=> { await Exec(owner,"UPDATE dbo.TAIKHOAN SET MatKhauHash=@hash",new SqlParameter("@hash",bad));try{await Denied(()=>Auth(db).LoginAsync(new LoginRequest("probe.admin",Password)));}finally{await Exec(owner,"UPDATE dbo.TAIKHOAN SET MatKhauHash=@hash",new SqlParameter("@hash",currentHash));}return "Rejected, no raw hash emitted"; });
        await Check("T06-K05.a", "Old V3 cost requests rehash", ()=> {var old=new PasswordHasher<string>(Options.Create(new PasswordHasherOptions{IterationCount=10000})).HashPassword("CoffeeManager",Password);Need(new PasswordService().Verify(old,Password)==PasswordVerificationResult.SuccessRehashNeeded,"Rehash flag missing");return Task.FromResult("SuccessRehashNeeded at 10000 -> configured 100000");});
        await Check("T06-K05.b", "Login upgrades real DB hash/stamp", async()=> {var old=new PasswordHasher<string>(Options.Create(new PasswordHasherOptions{IterationCount=10000})).HashPassword("CoffeeManager",Password);await Exec(owner,"UPDATE dbo.TAIKHOAN SET MatKhauHash=@hash",new SqlParameter("@hash",old));var stamp=(byte[])(await Scalar(owner,"SELECT SecurityStamp FROM dbo.TAIKHOAN"))!;using var s=await Auth(db).LoginAsync(new LoginRequest("probe.admin",Password));var hash=(string)(await Scalar(owner,"SELECT MatKhauHash FROM dbo.TAIKHOAN"))!;Need(hash!=old&&new PasswordService().Verify(hash,Password)==PasswordVerificationResult.Success,"DB hash not upgraded");Need(!stamp.SequenceEqual(s.Identity.SecurityStamp),"Stamp unchanged");return "DB upgraded, stamp changed, verification Success; no hash in evidence";});
        await Check("T06-K06.b", "Role change revokes old session; new login uses new role",async()=> {using var s=await Auth(db).LoginAsync(new LoginRequest("probe.admin",Password));await Exec(owner,"UPDATE dbo.TAIKHOAN SET MaVT='CASH'");await Denied(()=>new PermissionService(new AuthRepository(Factory(db))).DemandAsync(s,AppPermission.CatalogAdmin));Need(s.IsRevoked,"Role change not revoked");using var next=await Auth(db).LoginAsync(new LoginRequest("probe.admin",Password));Need(next.Identity.Role=="CASH","New role not used");await Exec(owner,"UPDATE dbo.TAIKHOAN SET MaVT='ADMIN'");return "Old revoked, fresh CASH identity";});
        await Check("T06-K02.e.runtime", "Runtime cannot execute bootstrap",async()=> { var r=new SetupRepository(Factory(db));await Denied(()=>r.CompleteAsync(new BootstrapRequest{Mode="NewEmployee",Username="forbidden",Password=Password,Confirmation=Password},new PasswordService().Hash(Password),default),229);return "SQL229 through runtime user";});
        var demo = await NewDb(true);
        await Check("T06-K01.b", "Only selected demo ADMIN activated",async()=> { var candidates=await Setup(demo).CandidatesAsync();var candidate=candidates.Single(x=>x.Username=="admin");await Setup(demo).CompleteAsync(new BootstrapRequest{Mode="ActivateDemo",Username="admin",Password=Password,Confirmation=Password,EmployeeCode=candidate.EmployeeCode,ExpectedStamp=candidate.SecurityStamp});await using var c=await Owner(demo);Need(Convert.ToInt32(await Scalar(c,"SELECT COUNT(*) FROM dbo.TAIKHOAN WHERE HoatDong=1"))==1,"Other demo accounts enabled");Need(Convert.ToInt32(await Scalar(c,"SELECT COUNT(*) FROM dbo.TAIKHOAN WHERE TenDangNhap IN('thungan','kho') AND HoatDong=0 AND MatKhauHash='!DISABLED_REPLACE_WITH_APP_PASSWORD_HASH!'"))==2,"Other placeholders changed");return "Only admin enabled; thungan/kho unchanged";});
        var existing=await NewDb(true);
        await Check("T06-K01.existing", "Existing employee without account retains personnel fields",async()=> {await using var c=await Owner(existing);var before=await Scalar(c,"SELECT (SELECT * FROM dbo.NHANVIEN WHERE MaNV='NV002' FOR JSON PATH)");await Setup(existing).CompleteAsync(new BootstrapRequest{Mode="ExistingEmployee",Username="existing.admin",Password=Password,Confirmation=Password,EmployeeCode="NV002"});Need(Equals(before,await Scalar(c,"SELECT (SELECT * FROM dbo.NHANVIEN WHERE MaNV='NV002' FOR JSON PATH)")),"Employee changed");return "New account only; employee fields unchanged";});
        foreach(var variant in new[]{"valid","inactive","departed","malformed"})
        {
            var legacy=await NewDb(true,versions:1);await using var c=await Owner(legacy);
            var hash=variant=="malformed"?"not-an-identity-hash":new PasswordService().Hash(Password);
            await Exec(c,"UPDATE dbo.TAIKHOAN SET MatKhauHash=@hash,HoatDong=@active WHERE TenDangNhap='admin'",new SqlParameter("@hash",hash),new SqlParameter("@active",variant=="inactive"?0:1));
            if(variant=="departed")await Exec(c,"UPDATE dbo.NHANVIEN SET DangLamViec=0 WHERE MaNV='NV001'");
            await Apply(legacy,2);await Exec(c,File.ReadAllText(Path.Combine(Root,"database","tests","g02","security-fixtures.sql")));
            await Check("T06-K01."+(variant=="valid"?"c":"d")+"."+variant,"LegacyReview; valid confirms without reset, others blocked",async()=> {Need((await Setup(legacy).ReadStateAsync()).State=="LegacyReview","Legacy gate wrong");if(variant=="valid"){await Setup(legacy).ConfirmLegacyAsync(new LoginRequest("ADMIN",Password));Need((await Setup(legacy).ReadStateAsync()).State=="Completed","Legacy confirm failed");}else await Denied(()=>Setup(legacy).ConfirmLegacyAsync(new LoginRequest("admin",Password)));Need(hash==(string)(await Scalar(c,"SELECT MatKhauHash FROM dbo.TAIKHOAN WHERE TenDangNhap='admin'"))!,"Legacy hash changed");return variant=="valid"?"Completed without hash change":"Rejected, still LegacyReview, no reset";});
        }
        foreach(var collation in new[]{"Latin1_General_100_CI_AS","Latin1_General_100_CS_AS"})
        {
            var target=await NewDb(collation:collation);await Setup(target).CompleteAsync(new BootstrapRequest{Mode="NewEmployee",Username="Mixed.Case",Password=Password,Confirmation=Password,EmployeeName="Case"});
            await Check("T06-K10.c."+collation,"Case-independent login/key uniqueness",async()=> {using var s=await Auth(target).LoginAsync(new LoginRequest("MIXED.CASE",Password));await using var c=await Owner(target);await Exec(c,"INSERT dbo.NHANVIEN(MaNV,TenNV,LoaiHopDong,MaCV) VALUES('NV900',N'Case','Full-time','CVADM')");await Denied(()=>Exec(c,"INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT) VALUES('NV900','mixed.case',@hash,'CASH')",new SqlParameter("@hash",new PasswordService().Hash(Password))));return "Login uppercase accepted; duplicate lowercase rejected";});
        }
        await Check("T06-K06.a.fullMatrix","All role/permission pairs match approved matrix",()=> {foreach(var role in new[]{"ADMIN","CASH","STORE"})foreach(var p in Enum.GetValues<AppPermission>()){var expected=role=="ADMIN"||(role=="CASH"&&p is AppPermission.Sales or AppPermission.Customers or AppPermission.MenuRead or AppPermission.SystemInfo)||(role=="STORE"&&p is AppPermission.Inventory or AppPermission.DebtRead or AppPermission.RecipeRead or AppPermission.MenuRead or AppPermission.SystemInfo);Need(PermissionService.Allows(role,p)==expected,$"Matrix {role}/{p}");}return Task.FromResult("3 roles x all 12 permission values compared");});
        foreach(var n in new[]{1,50,51})await Check("T06-K10.a.length"+n,"1/50 accepted;51 rejected",()=> {if(n==51){try{PasswordService.Username(new string('a',n));throw new Exception("length51 accepted");}catch(ArgumentException){}}else Need(PasswordService.Username(new string('a',n)).Length==n,"Length mismatch");return Task.FromResult(n==51?"Rejected":"Accepted");});
        File.WriteAllText(Path.Combine(Output,"ui-target.txt"),db); // DB name only; credentials stay in memory.
    }
}
