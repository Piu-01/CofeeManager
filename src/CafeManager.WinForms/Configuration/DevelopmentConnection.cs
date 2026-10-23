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
            return value;
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    public static void Write(string value)
    {
        Directory.CreateDirectory(DirectoryPath);
        var bytes = Encoding.UTF8.GetBytes(value);
        try { File.WriteAllBytes(FilePath, ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser)); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
}
