using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using CafeManager.WinForms.Configuration;
using Microsoft.Data.SqlClient;

internal static class Program
{
    private const string RuntimeLogin = "CoffeeManager_Development_Runtime";
    private const string SetupLogin = "CoffeeManager_Development_Setup";
    private static readonly string SecretsPath = Path.Combine(DevelopmentConnection.DirectoryPath, "accounts.dpapi");
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("CoffeeManager.Development.Accounts.v1");
    private static string Stage = "start";

    private static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 2 && args[0] == "--apply-mock-accounts")
            {
                var existing = Load();
                if (!existing.Ready) throw new InvalidOperationException("Initialize development first.");
                await ApplyMockAccounts(existing, args[1]);
                await Verify(existing);
                return 0;
            }
            if (args.Length == 0) args = ["--accounts"];
            if (args.SequenceEqual(["--accounts"]))
            {
                if (Console.IsOutputRedirected) throw new InvalidOperationException("Open directly in a local terminal; credential output cannot be redirected.");
                var accounts = Load();
                if (!accounts.Ready) throw new InvalidOperationException("Setup is not complete.");
                Console.WriteLine("Local development only; do not copy this output into chat or logs.");
                foreach (var account in Accounts(accounts)) Console.WriteLine($"{account.Username} ({account.Role}): {account.Password}");
                Console.WriteLine("Nhấn phím bất kỳ để đóng.");
                Console.ReadKey(intercept: true);
                Console.Clear();
                return 0;
            }
            if (args.Length != 1 || args[0] is not ("--initialize" or "--verify"))
                throw new ArgumentException("Use --initialize, --verify or --accounts.");
            var fresh = !File.Exists(SecretsPath);
            if (fresh && args[0] != "--initialize") throw new InvalidOperationException("Initialize development first.");
            var secrets = fresh ? new Secrets() : Load();
            if (fresh) Save(secrets);
            if (args[0] == "--initialize") await Initialize(secrets, fresh);
            if (fresh && args[0] == "--initialize")
                await ApplyMockAccounts(secrets, Path.Combine(AppContext.BaseDirectory, "accounts.json"));
            await Verify(secrets);
            if (args[0] == "--initialize")
            {
                DevelopmentConnection.Write(Connection(RuntimeLogin, secrets.RuntimePassword));
                secrets.Ready = true; Save(secrets);
                Console.WriteLine("Development configured. F5 profile: CoffeeManager.Development. Demo accounts: database/demo/README.md.");
            }
            return 0;
        }
        catch (Exception error)
        {
            // Không xuất exception message/connection/request có thể chứa credential.
            Console.Error.WriteLine($"Development setup stopped at {Stage}: {error.GetType().Name}" + (error is SqlException sql ? $" SQL {sql.Number}" : ""));
            return 1;
        }
    }

    private static string Connection(string login, string password) => new SqlConnectionStringBuilder
    {
        DataSource = "localhost", InitialCatalog = DevelopmentConnection.Database,
        UserID = login, Password = password, Encrypt = true, TrustServerCertificate = true, Pooling = false
    }.ConnectionString;

    private static async Task ApplyMockAccounts(Secrets secrets, string path)
    {
        Stage = "apply explicit mock account fixture";
        using var fixture = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        if (fixture.RootElement.GetProperty("database").GetString() != DevelopmentConnection.Database)
            throw new InvalidDataException("Mock fixture target mismatch.");
        var rows = fixture.RootElement.GetProperty("accounts").EnumerateArray().ToArray();
        var expected = new[] { ("admin", "ADMIN", "NV001"), ("thungan", "CASH", "NV004"), ("kho", "STORE", "NV010") };
        if (rows.Length != expected.Length) throw new InvalidDataException("Expected three mock accounts.");
        var passwords = new string[3];
        for (var i = 0; i < expected.Length; i++)
        {
            if (rows[i].GetProperty("username").GetString() != expected[i].Item1
                || rows[i].GetProperty("role").GetString() != expected[i].Item2
                || rows[i].GetProperty("employee").GetString() != expected[i].Item3)
                throw new InvalidDataException("Mock identity mismatch.");
            passwords[i] = rows[i].GetProperty("password").GetString() ?? "";
            if (passwords[i].Length is < 8 or > 128) throw new InvalidDataException("Invalid mock password length.");
        }
        using var owner = new SqlConnection($"Server=localhost;Initial Catalog={DevelopmentConnection.Database};Integrated Security=True;Encrypt=True;TrustServerCertificate=True");
        await owner.OpenAsync();
        using var transaction = owner.BeginTransaction();
        using (var guard = new SqlCommand("SELECT COUNT(*) FROM dbo.DevelopmentMarker WHERE Id=1 AND Application='CoffeeManager.Development.v1' AND SchemaReady=1;", owner, transaction))
            if (Convert.ToInt32(await guard.ExecuteScalarAsync()) != 1) throw new InvalidOperationException("Development marker required.");
        var hasher = new PasswordService();
        for (var i = 0; i < expected.Length; i++)
        {
            using var read = new SqlCommand("SELECT t.MatKhauHash FROM dbo.TAIKHOAN t WITH(UPDLOCK,HOLDLOCK) JOIN dbo.NHANVIEN n ON n.MaNV=t.MaNV WHERE t.TenDangNhap=@name AND t.MaVT=@role AND t.MaNV=@employee AND t.HoatDong=1 AND n.DangLamViec=1;", owner, transaction);
            read.Parameters.Add("@name", SqlDbType.VarChar, 50).Value = expected[i].Item1;
            read.Parameters.Add("@role", SqlDbType.VarChar, 10).Value = expected[i].Item2;
            read.Parameters.Add("@employee", SqlDbType.Char, 5).Value = expected[i].Item3;
            if (await read.ExecuteScalarAsync() is not string currentHash) throw new InvalidOperationException("Expected active mock identity missing.");
            if (hasher.Verify(currentHash, passwords[i]) == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
            {
                using var update = new SqlCommand("UPDATE dbo.TAIKHOAN SET MatKhauHash=@hash WHERE TenDangNhap=@name AND MaVT=@role AND MaNV=@employee;", owner, transaction);
                update.Parameters.Add("@hash", SqlDbType.VarChar, 255).Value = hasher.Hash(passwords[i]);
                update.Parameters.Add("@name", SqlDbType.VarChar, 50).Value = expected[i].Item1;
                update.Parameters.Add("@role", SqlDbType.VarChar, 10).Value = expected[i].Item2;
                update.Parameters.Add("@employee", SqlDbType.Char, 5).Value = expected[i].Item3;
                if (await update.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("Mock account update mismatch.");
            }
        }
        transaction.Commit();
        secrets.AdminPassword = passwords[0]; secrets.CashPassword = passwords[1]; secrets.StorePassword = passwords[2];
        Save(secrets);
        Console.WriteLine("Three mock account passwords synchronized; business data unchanged.");
    }

    private static async Task Initialize(Secrets secrets, bool fresh)
    {
        Stage = "development marker";
        using var owner = new SqlConnection($"Server=localhost;Initial Catalog={DevelopmentConnection.Database};Integrated Security=True;Encrypt=True;TrustServerCertificate=True");
        await owner.OpenAsync();
        using (var guard = new SqlCommand("SELECT COUNT(*) FROM dbo.DevelopmentMarker WHERE Id=1 AND Application='CoffeeManager.Development.v1' AND SchemaReady=1;", owner))
            if (Convert.ToInt32(await guard.ExecuteScalarAsync()) != 1) throw new InvalidOperationException("Development marker required.");
        Stage = "runtime principal";
        await Principal(owner, RuntimeLogin, secrets.RuntimePassword, "CafeManagerApp", fresh);
        var setupOwned = false;
        try
        {
            Stage = "temporary setup principal";
            await Principal(owner, SetupLogin, secrets.SetupPassword, "CafeManagerSetup", fresh);
            setupOwned = true;
            var setup = new SetupRepository(new ConnectionFactory(Connection(SetupLogin, secrets.SetupPassword)));
            await setup.CheckAccessAsync();
            var service = new BootstrapService(setup, new AuthRepository(new ConnectionFactory(Connection(SetupLogin, secrets.SetupPassword))), new PasswordService());
            var state = await service.ReadStateAsync();
            Stage = "bootstrap admin";
            if (state.State == "Pending")
            {
                var candidate = (await service.CandidatesAsync()).Single(c => c.EmployeeCode == "NV001" && c.Username == "admin" && c.Eligible);
                await service.CompleteAsync(new BootstrapRequest { Mode = "ActivateDemo", Username = "admin", Password = secrets.AdminPassword,
                    Confirmation = secrets.AdminPassword, EmployeeCode = candidate.EmployeeCode, ExpectedStamp = candidate.SecurityStamp });
            }
            else if (state.State != "Completed") throw new InvalidOperationException("Existing setup state requires review.");
        }
        finally
        {
            if (setupOwned)
            {
                using var cleanup = new SqlCommand($"DROP USER [{SetupLogin}]; DROP LOGIN [{SetupLogin}];", owner);
                await cleanup.ExecuteNonQueryAsync();
            }
        }
        Stage = "activate seeded staff on development only";
        // Chỉ kích hoạt placeholder của đúng2account seed; không reset account đã bật.
        using var transaction = owner.BeginTransaction();
        foreach (var account in Accounts(secrets).Where(a => a.Role != "ADMIN"))
        {
            using var command = new SqlCommand("""
                UPDATE dbo.TAIKHOAN SET MatKhauHash=@hash,HoatDong=1
                WHERE TenDangNhap=@name AND MaVT=@role AND MaNV=@employee AND HoatDong=0
                AND MatKhauHash COLLATE Latin1_General_100_BIN2='!DISABLED_REPLACE_WITH_APP_PASSWORD_HASH!';
                """, owner, transaction);
            command.Parameters.Add("@hash", SqlDbType.VarChar, 255).Value = new PasswordService().Hash(account.Password);
            command.Parameters.Add("@name", SqlDbType.VarChar, 50).Value = account.Username;
            command.Parameters.Add("@role", SqlDbType.VarChar, 10).Value = account.Role;
            command.Parameters.Add("@employee", SqlDbType.Char, 5).Value = account.Role == "CASH" ? "NV004" : "NV010";
            await command.ExecuteNonQueryAsync();
        }
        transaction.Commit();
    }

    private static async Task Principal(SqlConnection owner, string login, string password, string role, bool fresh)
    {
        using var command = new SqlCommand("""
            IF EXISTS(SELECT 1 FROM sys.server_principals WHERE name=@login)
            BEGIN
                IF @fresh=1 OR NOT EXISTS(SELECT 1 FROM sys.server_principals WHERE name=@login AND type='S' AND is_disabled=0)
                   OR IS_SRVROLEMEMBER('sysadmin',@login)=1 THROW 52901,'Existing development principal requires review.',1;
            END
            ELSE
            BEGIN
                DECLARE @create nvarchar(max)=N'CREATE LOGIN '+QUOTENAME(@login)+N' WITH PASSWORD='+QUOTENAME(@password,'''')+N',CHECK_POLICY=ON,CHECK_EXPIRATION=OFF;';
                EXEC sys.sp_executesql @create;
            END;
            IF USER_ID(@login) IS NOT NULL AND EXISTS(SELECT 1 FROM sys.database_principals WHERE name=@login AND sid<>SUSER_SID(@login))
                THROW 52901,'Development user mapping changed.',1;
            DECLARE @sql nvarchar(max)=N'';
            IF USER_ID(@login) IS NULL SET @sql=N'CREATE USER '+QUOTENAME(@login)+N' FOR LOGIN '+QUOTENAME(@login)+N';';
            SET @sql+=N'ALTER ROLE '+QUOTENAME(@role)+N' ADD MEMBER '+QUOTENAME(@login)+N';';
            EXEC sys.sp_executesql @sql;
            """, owner);
        command.Parameters.Add("@login", SqlDbType.NVarChar, 128).Value = login;
        command.Parameters.Add("@password", SqlDbType.NVarChar, 128).Value = password;
        command.Parameters.Add("@role", SqlDbType.NVarChar, 128).Value = role;
        command.Parameters.Add("@fresh", SqlDbType.Bit).Value = fresh;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task Verify(Secrets secrets)
    {
        Stage = "runtime identity and three-role authentication";
        var connection = Connection(RuntimeLogin, secrets.RuntimePassword);
        if (secrets.Ready)
        {
            var configured = new SqlConnectionStringBuilder(DevelopmentConnection.Read());
            if (configured.ConnectionString != new SqlConnectionStringBuilder(connection).ConnectionString)
                throw new InvalidOperationException("Development profile connection differs from provisioned runtime.");
            Console.WriteLine("DPAPI development profile connection PASS");
        }
        using (var sql = new SqlConnection(connection))
        {
            await sql.OpenAsync();
            using var check = new SqlCommand("SELECT CASE WHEN IS_SRVROLEMEMBER('sysadmin')=0 AND IS_MEMBER('db_owner')=0 AND IS_MEMBER('CafeManagerApp')=1 AND IS_MEMBER('CafeManagerSetup')=0 THEN 1 ELSE 0 END;", sql);
            if (Convert.ToInt32(await check.ExecuteScalarAsync()) != 1) throw new InvalidOperationException("Unexpected runtime permissions.");
            foreach (var forbidden in new[] { "SELECT COUNT(*) FROM dbo.TAIKHOAN;", "EXEC dbo.sp_SetupCheckAccess;" })
            {
                using var deniedCommand = new SqlCommand(forbidden, sql);
                try { await deniedCommand.ExecuteScalarAsync(); throw new InvalidOperationException("Runtime access was not restricted."); }
                catch (SqlException error) when (error.Number == 229) { }
            }
        }
        var repository = new AuthRepository(new ConnectionFactory(connection));
        var auth = new AuthService(repository, new PasswordService());
        var permissions = new PermissionService(repository);
        foreach (var account in Accounts(secrets))
        {
            using var session = await auth.LoginAsync(new LoginRequest(account.Username, account.Password));
            if (session.Identity.Role != account.Role) throw new InvalidOperationException("Role mismatch.");
            var denied = false;
            try { await permissions.DemandAsync(session, AppPermission.CatalogAdmin); }
            catch (UnauthorizedAccessException) { denied = true; }
            if (denied != (account.Role != "ADMIN")) throw new InvalidOperationException("Permission mismatch.");
            session.Revoke();
            try { session.EnsureCurrent(); throw new InvalidOperationException("Session was not revoked."); }
            catch (UnauthorizedAccessException) { }
            Console.WriteLine($"{account.Username}: {account.Role}, login/permission/session PASS");
        }
    }

    private static IEnumerable<(string Username, string Role, string Password)> Accounts(Secrets secrets) =>
        [("admin", "ADMIN", secrets.AdminPassword), ("thungan", "CASH", secrets.CashPassword), ("kho", "STORE", secrets.StorePassword)];
    private static Secrets Load()
    {
        var bytes = ProtectedData.Unprotect(File.ReadAllBytes(SecretsPath), Entropy, DataProtectionScope.CurrentUser);
        try { return JsonSerializer.Deserialize<Secrets>(bytes) ?? throw new InvalidDataException(); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    private static void Save(Secrets secrets)
    {
        Directory.CreateDirectory(DevelopmentConnection.DirectoryPath);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(secrets);
        try { File.WriteAllBytes(SecretsPath, ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser)); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    private sealed class Secrets
    {
        public bool Ready { get; set; }
        public string RuntimePassword { get; set; } = RandomPassword();
        public string SetupPassword { get; set; } = RandomPassword();
        public string AdminPassword { get; set; } = RandomPassword();
        public string CashPassword { get; set; } = RandomPassword();
        public string StorePassword { get; set; } = RandomPassword();
        private static string RandomPassword() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(18)) + "_cM9!";
        public override string ToString() => nameof(Secrets);
    }
}
