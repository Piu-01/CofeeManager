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
        LanguageService.Set(new Configuration.LanguagePreference().Read());
        // PerMonitorV2 khai báo trong csproj; layout chuẩn 96 DPI (100%), không ép 125%.
        ApplicationConfiguration.Initialize();
        // Áp dụng theme native trước khi tạo HWND, giữ nguyên control nhập giờ/bàn phím.
        Application.SetColorMode(SystemColorMode.Dark);
        var connectionString = Environment.GetEnvironmentVariable("CAFEMANAGER_APP_CONNECTION");
        AuthRepository? repository = null;
        var passwords = new PasswordService();
        string? configurationError = null;
        // Profile F5 chọn development rõ ràng; chạy exe thường không tự chuyển DB.
        if (string.IsNullOrWhiteSpace(connectionString)
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
                string Option(string name) { var index = Array.IndexOf(args, name); return index >= 0 && index + 1 < args.Length ? args[index + 1] : throw new ArgumentException(L.T("Setup cần --server và --database.")); }
                var server = Option("--server"); var database = Option("--database");
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
                { DataSource = server, InitialCatalog = database, IntegratedSecurity = true, Encrypt = true, TrustServerCertificate = server == "localhost" };
                var setupFactory = new ConnectionFactory(builder.ConnectionString);
                var setupRepository = new SetupRepository(setupFactory);
                setupRepository.CheckAccessAsync().GetAwaiter().GetResult();
                Application.Run(new BootstrapForm(new BootstrapService(setupRepository, new AuthRepository(setupFactory), passwords)));
            }
            catch (ArgumentException ex) { MessageBox.Show(ex.Message, L.T("Thiết lập quản trị")); }
            catch (DataAccessException ex) { MessageBox.Show(ex.Error.Message + L.T(" Người cài cần cấp role CafeManagerSetup cho Windows identity riêng."), L.T("Thiết lập quản trị")); }
            return;
        }
        var permissionService = repository is null ? null : new PermissionService(repository);
        var runtimeConnection = connectionString;
        ReportService Reports() => new(new ReportRepository(new ConnectionFactory(runtimeConnection!)),permissionService!);
        AssetService Assets() => new(new AssetRepository(new ConnectionFactory(runtimeConnection!)), permissionService!);
        PayrollService Payroll() => new(new PayrollRepository(new ConnectionFactory(runtimeConnection!)),new EmployeeRepository(new ConnectionFactory(runtimeConnection!)),permissionService!);
        var catalogService = repository is null ? null : new CatalogService(new CatalogRepository(new ConnectionFactory(runtimeConnection!)), permissionService!);
        UI.DocumentLinks DocumentLinks(SessionContext session) => new(new PurchaseService(new PurchaseRepository(new ConnectionFactory(runtimeConnection!)),permissionService!),new FinanceService(new FinanceRepository(new ConnectionFactory(runtimeConnection!)),permissionService!),new SupplierDebtService(new SupplierDebtRepository(new ConnectionFactory(runtimeConnection!)),permissionService!),session,Payroll(),Assets());
        Modules.Reports.ReportSourceNavigation ReportSources(SessionContext session) => new(session,DocumentLinks(session),new InvoiceService(new InvoiceRepository(new ConnectionFactory(runtimeConnection!)),permissionService!),new StockIssueService(new StockIssueRepository(new ConnectionFactory(runtimeConnection!)),permissionService!),new StocktakeService(new StocktakeRepository(new ConnectionFactory(runtimeConnection!)),permissionService!),new AttendanceService(new AttendanceRepository(new ConnectionFactory(runtimeConnection!)),permissionService!));
        UserControl Inventory(SessionContext session) => new Modules.Inventory.InventoryControl(new PurchaseService(new PurchaseRepository(new ConnectionFactory(runtimeConnection!)),permissionService!),new StockIssueService(new StockIssueRepository(new ConnectionFactory(runtimeConnection!)),permissionService!),new StocktakeService(new StocktakeRepository(new ConnectionFactory(runtimeConnection!)),permissionService!),session,new Configuration.PendingOperationFile(),DocumentLinks(session));
        var context = new ApplicationSessionContext(repository is null ? null : new AuthService(repository, passwords),
            permissionService, repository, configurationError, catalogService is null ? null : session => new Modules.Catalogs.CatalogsControl(catalogService, session),
            repository is null ? null : session => new Dictionary<Navigation.NavigationTarget, Func<UserControl>>
            {
                [Navigation.NavigationTarget.Home] = () => session.Identity.Role=="ADMIN"?new Modules.Home.AdminDashboardControl(Reports(),session,ReportSources(session)):new Modules.Home.SessionHomeControl(session.Identity),
                [Navigation.NavigationTarget.Reports] = () => new Modules.Reports.ReportsControl(Reports(),session,ReportSources(session)),
                [Navigation.NavigationTarget.Assets] = () => new Modules.Assets.AssetsControl(Assets(),session,DocumentLinks(session)),
                [Navigation.NavigationTarget.HumanResources] = () => new Modules.HumanResources.HumanResourcesControl(new EmployeeService(new EmployeeRepository(new ConnectionFactory(runtimeConnection!)),permissionService!),new EmployeeAccountService(new EmployeeAccountRepository(new ConnectionFactory(runtimeConnection!)),permissionService!,passwords),session,new AttendanceService(new AttendanceRepository(new ConnectionFactory(runtimeConnection!)),permissionService!),Payroll(),DocumentLinks(session)),
                [Navigation.NavigationTarget.Inventory] = () => Inventory(session),
                [Navigation.NavigationTarget.Debt] = () => new Modules.Finance.SupplierDebtsControl(new SupplierDebtService(new SupplierDebtRepository(new ConnectionFactory(runtimeConnection!)),permissionService!),session,new Configuration.PendingOperationFile(),DocumentLinks(session)),
                [Navigation.NavigationTarget.Finance] = () => new Modules.Finance.FinanceControl(new FinanceService(new FinanceRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), session, new Configuration.PendingOperationFile(), DocumentLinks(session), Assets()),
                [Navigation.NavigationTarget.Sales] = () =>
                {
                    var invoices = new InvoiceService(new InvoiceRepository(new ConnectionFactory(runtimeConnection!)), permissionService!);
                    var sales = new Modules.Sales.SalesControl(new SalesService(new SalesRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), new TableService(new TableRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), new CustomerService(new CustomerRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), new LoyaltyService(new LoyaltyRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), session, invoices);
                    return sales;
                },
                [Navigation.NavigationTarget.Delivery] = () => new Modules.Sales.DeliveryControl(new DeliveryService(new DeliveryRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), new SalesService(new SalesRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), new InvoiceService(new InvoiceRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), session),
                [Navigation.NavigationTarget.Invoices] = () => new Modules.Sales.InvoicesControl(new InvoiceService(new InvoiceRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), session),
                [Navigation.NavigationTarget.Loyalty] = () => new Modules.Loyalty.LoyaltyControl(new LoyaltyService(new LoyaltyRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), new CustomerService(new CustomerRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), new ProductService(new ProductRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), session),
                [Navigation.NavigationTarget.Customers] = () => new Modules.Customers.CustomersControl(new CustomerService(new CustomerRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), session, new LoyaltyService(new LoyaltyRepository(new ConnectionFactory(runtimeConnection!)), permissionService!)),
                [Navigation.NavigationTarget.Tables] = () => new Modules.Tables.TablesControl(new TableService(new TableRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), session),
                [Navigation.NavigationTarget.Products] = () => new Modules.Menu.ProductsControl(new ProductService(new ProductRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), session),
                [Navigation.NavigationTarget.Materials] = () => new Modules.Inventory.MaterialsControl(new MaterialService(new MaterialRepository(new ConnectionFactory(runtimeConnection!)), permissionService!), session)
            }, quickLogin: repository is null ? null : Configuration.DevelopmentQuickLogin.Create(connectionString));
        context.Start(); Application.Run(context);
    }
}

