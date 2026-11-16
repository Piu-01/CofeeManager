using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CafeManager.Models.Requests;
using Microsoft.Data.SqlClient;

namespace CafeManager.WinForms.Configuration;

internal sealed class DevelopmentQuickLogin
{
    internal static readonly string[] Roles = ["ADMIN", "CASH", "STORE"];
    private sealed record Credential(string Username, string Password)
    { public override string ToString() => nameof(Credential); }
    private sealed record Envelope(int Version, string Server, string Database, Dictionary<string, Credential> Accounts);
    private readonly string server, database, path;
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("CoffeeManager.Development.QuickLogin.v1");
    private DevelopmentQuickLogin(string server, string database)
    {
        this.server = server; this.database = database;
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{server.ToUpperInvariant()}\n{database.ToUpperInvariant()}")));
        path = Path.Combine(DevelopmentConnection.DirectoryPath, "quick-login", key + ".dpapi");
    }
    internal static DevelopmentQuickLogin? Create(string? connection)
    {
#if DEBUG
        if (Environment.GetEnvironmentVariable("CAFEMANAGER_USE_DEVELOPMENT") != "1"
            || Environment.GetEnvironmentVariable("CAFEMANAGER_DEV_QUICK_LOGIN") != "1" || string.IsNullOrWhiteSpace(connection)) return null;
        try
        {
            var target = new SqlConnectionStringBuilder(connection);
            if (string.Equals(target.DataSource, "localhost", StringComparison.OrdinalIgnoreCase)
                && string.Equals(target.InitialCatalog, DevelopmentConnection.Database, StringComparison.OrdinalIgnoreCase))
                return new DevelopmentQuickLogin("localhost", DevelopmentConnection.Database);
        }
        catch (ArgumentException) { }
#endif
        return null;
    }
    private Envelope Read()
    {
        if (!File.Exists(path)) return new(1, server, database, []);
        if (new FileInfo(path).Length > 65536) throw new InvalidDataException();
        var bytes = ProtectedData.Unprotect(File.ReadAllBytes(path), Entropy, DataProtectionScope.CurrentUser);
        try
        {
            var data = JsonSerializer.Deserialize<Envelope>(bytes);
            if (data is null || data.Version != 1 || data.Server != server || data.Database != database
                || data.Accounts is null || data.Accounts.Count > 3
                || data.Accounts.Any(p => !Roles.Contains(p.Key) || p.Value is null
                    || string.IsNullOrWhiteSpace(p.Value.Username) || p.Value.Username.Length > 50
                    || string.IsNullOrEmpty(p.Value.Password) || p.Value.Password.Length > 128)) throw new InvalidDataException();
            return data;
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    internal LoginRequest? Get(string role)
    {
        ValidateRole(role);
        try { var account = Read().Accounts.GetValueOrDefault(role); return account is null ? null : new LoginRequest(account.Username, account.Password); }
        catch (Exception ex) when (IsStorageError(ex)) { throw StorageError(); }
    }
    internal void Save(string role, LoginRequest request) => Update(role, request);
    internal void Delete(string role) => Update(role, null);
    private void Update(string role, LoginRequest? request)
    {
        ValidateRole(role);
        string? temporary = null;
        try
        {
            var data = Read();
            if (request is null) data.Accounts.Remove(role); else data.Accounts[role] = new(request.Username, request.Password);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(data);
            try
            {
                var encrypted = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
                temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllBytes(temporary, encrypted);
                File.Move(temporary, path, overwrite: true);
            }
            finally { CryptographicOperations.ZeroMemory(bytes); }
        }
        catch (Exception ex) when (IsStorageError(ex)) { throw StorageError(); }
        finally
        {
            if (temporary is not null)
                try { File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
    private static void ValidateRole(string role)
    { if (!Roles.Contains(role)) throw new ArgumentException(L.T("Vai trò DEV không hợp lệ.")); }
    private static bool IsStorageError(Exception ex) => ex is IOException or InvalidDataException or UnauthorizedAccessException or CryptographicException or JsonException;
    private static InvalidOperationException StorageError() => new(L.T("Không đọc/ghi được cấu hình đăng nhập nhanh. Kiểm tra quyền file hoặc xóa file cấu hình DEV bị hỏng rồi cấu hình lại."));
}
