#if DEBUG
using CafeManager.Business.Security;
using CafeManager.DataAccess.Repositories;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace CafeManager.WinForms.Configuration;

// Xóa cùng nút TEST trước bàn giao; không đọc password hoặc gọi LoginAsync.
internal static class TemporaryTestLogin
{
    public static string ReadProfileConnection(string manifestPath)
    {
        if (!Path.IsPathRooted(manifestPath))
        {
            // Tìm gốc solution từ exe hoặc working directory, không phụ thuộc đường dẫn máy.
            var root = FindSolutionRoot(AppContext.BaseDirectory) ?? FindSolutionRoot(Environment.CurrentDirectory)
                ?? throw new IOException("Không tìm thấy solution cho profile Test.");
            manifestPath = Path.Combine(root, manifestPath);
        }
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var data = manifest.RootElement;
        if (!data.TryGetProperty("Server", out var server) || server.GetString() != "localhost"
            || !data.TryGetProperty("Cleaned", out var cleaned) || cleaned.ValueKind != JsonValueKind.False
            || !data.TryGetProperty("Database", out var database) || database.ValueKind != JsonValueKind.String
            || !data.TryGetProperty("Marker", out var marker) || marker.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("Manifest profile Test không hợp lệ hoặc đã được dọn.");
        var target = new SqlConnectionStringBuilder(DevelopmentConnection.Read()) { InitialCatalog = database.GetString()! };
        _ = new TemporaryTestTarget(target.ConnectionString, target.InitialCatalog, marker.GetString()!);
        Environment.SetEnvironmentVariable("CAFEMANAGER_TEST_DATABASE", target.InitialCatalog);
        Environment.SetEnvironmentVariable("CAFEMANAGER_TEST_MARKER", marker.GetString());
        return target.ConnectionString;
    }

    private static string? FindSolutionRoot(string location)
    {
        for (var directory = new DirectoryInfo(location); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "CafeManager.slnx"))) return directory.FullName;
        return null;
    }

    public static Func<CancellationToken, Task<SessionContext>>? Create(string? connectionString)
    {
        if (Environment.GetEnvironmentVariable("CAFEMANAGER_TEST_LOGIN") != "1" || string.IsNullOrWhiteSpace(connectionString)) return null;
        var database = Environment.GetEnvironmentVariable("CAFEMANAGER_TEST_DATABASE");
        var marker = Environment.GetEnvironmentVariable("CAFEMANAGER_TEST_MARKER");
        if (string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(marker)) return null;
        TemporaryTestTarget target;
        try { target = new TemporaryTestTarget(connectionString, database, marker); }
        catch (ArgumentException) { return null; }
        return async token =>
        {
            var identity = await target.ReadAdminIdentityAsync(token);
            token.ThrowIfCancellationRequested();
            return new SessionContext(identity);
        };
    }
}
#endif
