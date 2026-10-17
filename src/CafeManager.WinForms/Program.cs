using CafeManager.Business.Services;
using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.WinForms.Forms;

namespace CafeManager.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var connectionString = Environment.GetEnvironmentVariable("CAFEMANAGER_APP_CONNECTION");
        AuthRepository? repository = null;
        var passwords = new PasswordService();
        string? configurationError = null;
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                var factory = new ConnectionFactory(connectionString);
                repository = new AuthRepository(factory);
            }
            catch (ArgumentException) { configurationError = "Cấu hình kết nối chưa hợp lệ; kiểm tra cấu hình local."; }
        }
        else configurationError = "Chưa thiết lập kết nối. Cần cấu hình tài khoản ứng dụng trước khi tải dữ liệu.";
        if (args.Contains("--setup", StringComparer.Ordinal))
        {
            try
            {
                string Option(string name) { var index = Array.IndexOf(args, name); return index >= 0 && index + 1 < args.Length ? args[index + 1] : throw new ArgumentException("Setup cần --server và --database."); }
                var server = Option("--server"); var database = Option("--database");
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
                { DataSource = server, InitialCatalog = database, IntegratedSecurity = true, Encrypt = true, TrustServerCertificate = server == "localhost" };
                var setupFactory = new ConnectionFactory(builder.ConnectionString);
                var setupRepository = new SetupRepository(setupFactory);
                setupRepository.CheckAccessAsync().GetAwaiter().GetResult();
                Application.Run(new BootstrapForm(new BootstrapService(setupRepository, new AuthRepository(setupFactory), passwords)));
            }
            catch (ArgumentException ex) { MessageBox.Show(ex.Message, "Thiết lập quản trị"); }
            catch (DataAccessException ex) { MessageBox.Show(ex.Error.Message + " Người cài cần cấp role CafeManagerSetup cho Windows identity riêng.", "Thiết lập quản trị"); }
            return;
        }
        var permissionService = repository is null ? null : new PermissionService(repository);
        var runtimeConnection = Environment.GetEnvironmentVariable("CAFEMANAGER_APP_CONNECTION");
        var catalogService = repository is null ? null : new CatalogService(new CatalogRepository(new ConnectionFactory(runtimeConnection!)), permissionService!);
        var context = new ApplicationSessionContext(repository is null ? null : new AuthService(repository, passwords),
            permissionService, repository, configurationError, catalogService is null ? null : session => new Modules.Catalogs.CatalogsControl(catalogService, session));
        context.Start(); Application.Run(context);
    }
}
