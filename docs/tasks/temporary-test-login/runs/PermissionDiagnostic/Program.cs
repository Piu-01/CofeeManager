using CafeManager.WinForms.Configuration;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using Microsoft.Data.SqlClient;
using System.Text.Json;

const string database = "CoffeeManager_Test_G03_20261019_215457_29e552a9_demo";
const string marker = "20261019_215457_29e552a9";
var facts = new List<object>();
if (args.Contains("--provision")) {
    using var manifest = JsonDocument.Parse(File.ReadAllText(args[1]));
    var data = manifest.RootElement;
    if (data.GetProperty("Server").GetString() != "localhost" || data.GetProperty("Database").GetString() != database
        || data.GetProperty("Marker").GetString() != marker || data.GetProperty("Cleaned").GetBoolean()) throw new InvalidOperationException("Manifest mismatch");
}
var guard = new SqlConnectionStringBuilder { DataSource = "localhost", InitialCatalog = database, IntegratedSecurity = true, TrustServerCertificate = true };
await using (var c = new SqlConnection(guard.ConnectionString)) {
    await c.OpenAsync(); using var q = c.CreateCommand(); q.CommandText = "SELECT RunId FROM dbo.G03TestMarker";
    if (await q.ExecuteScalarAsync() as string != marker) throw new InvalidOperationException("Test marker mismatch");
}
facts.Add(new { Check = "Guard", Database = database, MarkerMatches = true });
var runtime = new SqlConnectionStringBuilder(DevelopmentConnection.Read()) { InitialCatalog = database };
if (args.Contains("--provision")) {
    string user;
    await using (var c = new SqlConnection(runtime.ConnectionString)) { await c.OpenAsync(); using var q = c.CreateCommand(); q.CommandText = "SELECT USER_NAME()"; user = (string)(await q.ExecuteScalarAsync())!; }
    if (string.IsNullOrWhiteSpace(user) || new[] { "dbo", "guest", "public", "sys", "INFORMATION_SCHEMA" }.Contains(user, StringComparer.OrdinalIgnoreCase)) throw new InvalidOperationException("Refusing broad/runtime owner principal");
    await using var c2 = new SqlConnection(guard.ConnectionString); await c2.OpenAsync();
    using var grant = c2.CreateCommand(); grant.CommandText = "IF DB_NAME()<>@Database OR (SELECT COUNT_BIG(*) FROM dbo.G03TestMarker)<>1 OR NOT EXISTS(SELECT 1 FROM dbo.G03TestMarker WHERE RunId=@Marker) THROW 50000,'Test target mismatch',1; IF NOT EXISTS(SELECT 1 FROM sys.database_principals WHERE name=@User AND type IN('S','U','G','E','X')) THROW 50000,'Runtime principal mismatch',1; DECLARE @sql nvarchar(max)=N'GRANT SELECT ON OBJECT::dbo.G03TestMarker TO '+QUOTENAME(@User)+N';'; EXEC sys.sp_executesql @sql;";
    grant.Parameters.AddWithValue("@Database", database); grant.Parameters.AddWithValue("@Marker", marker); grant.Parameters.AddWithValue("@User", user);
    await grant.ExecuteNonQueryAsync(); facts.Add(new { Check = "Provision", Database = database, User = user, Permission = "SELECT dbo.G03TestMarker only" });
}
await using (var c = new SqlConnection(runtime.ConnectionString)) {
    await c.OpenAsync(); using var q = c.CreateCommand();
    q.CommandText = "SELECT HAS_PERMS_BY_NAME(N'dbo.G03TestMarker',N'OBJECT',N'SELECT')";
    facts.Add(new { Check = "RuntimeMarkerSelectPermission", Value = await q.ExecuteScalarAsync() });
    q.CommandText = "SELECT RunId FROM dbo.G03TestMarker";
    try { _ = await q.ExecuteScalarAsync(); facts.Add(new { Check = "RuntimeReadMarker", Success = true }); }
    catch (SqlException ex) { facts.Add(new { Check = "RuntimeReadMarker", Success = false, Number = ex.Number, Message = ex.Message }); }
}
var repository = new AuthRepository(new ConnectionFactory(runtime.ConnectionString));
var state = await repository.ReadStateAsync();
facts.Add(new { Check = "RuntimeReadState", State = state.State, HasAdmin = state.AdminAccountId.HasValue });
if (state.AdminAccountId is int accountId) {
    var identity = await repository.ReadIdentityAsync(accountId);
    facts.Add(new { Check = "RuntimeReadAdminIdentity", Exists = identity != null, identity?.Role, identity?.Active, identity?.Employed });
}
if (args.Contains("--regression")) {
    var target = new TemporaryTestTarget(runtime.ConnectionString, database, marker);
    var identity = await target.ReadAdminIdentityAsync(default);
    using var session = new CafeManager.Business.Security.SessionContext(identity);
    var permission = new CafeManager.Business.Security.PermissionService(repository);
    await permission.DemandAsync(session, CafeManager.Business.Security.AppPermission.MaterialRead);
    facts.Add(new { Check = "ActualHelperAndBll", Success = identity.Role == "ADMIN" && !session.IsRevoked });
    bool blocked = false;
    try { await new TemporaryTestTarget(runtime.ConnectionString, database, "wrong-marker-regression").ReadAdminIdentityAsync(default); }
    catch (InvalidOperationException) { blocked = true; }
    if (!blocked) throw new Exception("Wrong marker accepted");
    facts.Add(new { Check = "WrongMarkerRejected", Success = blocked });
    facts.Add(await SessionUiProbe.Run(runtime.ConnectionString));
}
var output = args.Contains("--provision") ? args[2] : args[0];
File.WriteAllText(output, JsonSerializer.Serialize(facts, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(JsonSerializer.Serialize(facts));
