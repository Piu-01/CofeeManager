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
        // PerMonitorV2 khai báo trong csproj; layout chuẩn 96 DPI (100%), không ép 125%.
        ApplicationConfiguration.Initialize();
        // Áp dụng theme native trước khi tạo HWND, giữ nguyên control nhập giờ/bàn phím.
        Application.SetColorMode(SystemColorMode.Dark);
        var connectionString = Environment.GetEnvironmentVariable("CAFEMANAGER_APP_CONNECTION");
        AuthRepository? repository = null;
        var passwords = new PasswordService();
        string? configurationError = null;
        var testManifest = Environment.GetEnvironmentVariable("CAFEMANAGER_TEST_MANIFEST");
        // Chỉ profile Test mới đọc manifest; lỗi không được rơi về DB development.
        if (!string.IsNullOrWhiteSpace(testManifest))
        {
            connectionString = null;
#if DEBUG
            try { connectionString = Configuration.TemporaryTestLogin.ReadProfileConnection(testManifest); }
            catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException or System.Security.Cryptography.CryptographicException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
            { configurationError = "Không mở được profile Test. Kiểm tra manifest DB test và cấu hình local."; }
#else
            configurationError = "Profile CoffeeManager.Test chỉ dùng với bản Debug.";
#endif
        }
        // Profile F5 chọn development rõ ràng; chạy exe thường không tự chuyển DB.
        else if (string.IsNullOrWhiteSpace(connectionString)
            && Environment.GetEnvironmentVariable("CAFEMANAGER_USE_DEVELOPMENT") == "1")
        {
            try
            {
                connectionString = Configuration.DevelopmentConnection.Read();
            }
            catch (Exception ex) when (ex is IOException or System.Security.Cryptography.CryptographicException or ArgumentException or UnauthorizedAccessException)
            { configurationError = "Chưa có cấu hình development hợp lệ trên tài khoản Windows này."; }
        }
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                var factory = new ConnectionFactory(connectionString);
                repository = new AuthRepository(factory);
            }
            catch (ArgumentException) { configurationError = "Cấu hình kết nối chưa hợp lệ; kiểm tra cấu hình local."; }
        }
        else configurationError ??= "Chưa thiết lập kết nối. Cần cấu hình tài khoản ứng dụng trước khi tải dữ liệu.";
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
        var runtimeConnection = connectionString;
        var catalogService = repository is null ? null : new CatalogService(new CatalogRepository(new ConnectionFactory(runtimeConnection!)), permissionService!);
        var context = new ApplicationSessionContext(repository is null ? null : new AuthService(repository, passwords),
            permissionService, repository, configurationError, catalogService is null ? null : session => new Modules.Catalogs.CatalogsControl(catalogService, session),
            repository is null ? null : session => new Dictionary<Navigation.NavigationTarget, Func<UserControl>>
            {
                [Navigation.NavigationTarget.Loyalty] = () => new Modules.Loyalty.LoyaltyControl(new LoyaltyService(new LoyaltyRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), new CustomerService(new CustomerRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), session),
                [Navigation.NavigationTarget.Customers] = () => new Modules.Customers.CustomersControl(new CustomerService(new CustomerRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), session),
                [Navigation.NavigationTarget.Tables] = () => new Modules.Tables.TablesControl(new TableService(new TableRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), session),
                [Navigation.NavigationTarget.Products] = () => new Modules.Menu.ProductsControl(new ProductService(new ProductRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), session),
                [Navigation.NavigationTarget.Materials] = () => new Modules.Inventory.MaterialsControl(new MaterialService(new MaterialRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), session)
            });
#if DEBUG
        context.TemporaryTestLogin = Configuration.TemporaryTestLogin.Create(runtimeConnection);
#endif
        context.Start(); Application.Run(context);
    }
}
