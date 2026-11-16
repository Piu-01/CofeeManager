using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace CafeManager.WinForms.Configuration;

public static class DevelopmentConnection
{
    public const string Database = "CoffeeManager_Development";
    public static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CoffeeManager", "development");
    private static string FilePath => Path.Combine(DirectoryPath, "connection.dpapi");
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("CoffeeManager.Development.Connection.v1");

    public static string Read()
    {
        var bytes = ProtectedData.Unprotect(File.ReadAllBytes(FilePath), Entropy, DataProtectionScope.CurrentUser);
        try
        {
            var value = Encoding.UTF8.GetString(bytes);
            var builder = new SqlConnectionStringBuilder(value);
            if (builder.DataSource != "localhost" || builder.InitialCatalog != Database || builder.IntegratedSecurity)
                throw new InvalidDataException("Invalid development target.");
            // Dev runtime dùng connection pool. Đo 15/11/2026: Pooling=False tốn ~334 ms mở kết nối
            // mỗi lượt gọi DB; harness cần EXECUTE AS USER thì tự đặt qua CAFEMANAGER_APP_CONNECTION.
            builder.Pooling = true;
            return builder.ConnectionString;
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    public static void Write(string value)
    {
        Directory.CreateDirectory(DirectoryPath);
        var bytes = Encoding.UTF8.GetBytes(new SqlConnectionStringBuilder(value) { Pooling = true }.ConnectionString);
        try { File.WriteAllBytes(FilePath, ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser)); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
}
