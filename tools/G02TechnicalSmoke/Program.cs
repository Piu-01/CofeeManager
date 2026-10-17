using System.Security.Cryptography;
using System.Text.RegularExpressions;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using CafeManager.Models.Requests.Catalogs;
using CafeManager.Models.Dtos.Catalogs;
using Microsoft.Data.SqlClient;

if (args.Length != 1 || !Regex.IsMatch(args[0], @"^CoffeeManager_Test_G02_\d{8}_\d{6}_[a-f0-9]{8}$"))
    throw new InvalidOperationException("Explicit G02 test database required.");
var connectionString = $"Server=localhost;Initial Catalog={args[0]};Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
await using (var connection = new SqlConnection(connectionString))
{
    await connection.OpenAsync();
    await using var marker = new SqlCommand("SELECT COUNT(*) FROM dbo.G02TestMarker", connection);
    if (Convert.ToInt32(await marker.ExecuteScalarAsync()) != 1) throw new InvalidOperationException("Test marker required.");
}
var factory = new ConnectionFactory(connectionString);
var auth = new AuthRepository(factory);
var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
var setup = new BootstrapService(new SetupRepository(factory), auth, new PasswordService());
await setup.CompleteAsync(new BootstrapRequest { Mode = "NewEmployee", Username = "technical.admin", Password = password,
    Confirmation = password, EmployeeName = "Kiểm tra kỹ thuật G02" });
using var session = await new AuthService(auth, new PasswordService()).LoginAsync(new LoginRequest("TECHNICAL.ADMIN", password));
await new PermissionService(auth).DemandAsync(session, AppPermission.CatalogAdmin);
Console.WriteLine("TECHNICAL CHECK: setup -> login -> permission dependency is available (not acceptance).");
if ((await auth.ReadStateAsync()).SchemaVersion >= 3)
{
    var catalogs = new CatalogService(new CatalogRepository(factory), new PermissionService(auth));
    var created = await catalogs.SaveAsync(session, new ProductCategoryRequest("", "Cà phê · smoke", Guid.NewGuid()), null);
    var row = await catalogs.GetAsync(session, CatalogKind.ProductCategory, created.Code) ?? throw new InvalidOperationException("Catalog missing.");
    await catalogs.SaveAsync(session, new ProductCategoryRequest(row.Code, "Cà phê · smoke updated", Guid.NewGuid()), row.Revision);
    var methods = await new PaymentMethodService(new PaymentMethodRepository(factory), new PermissionService(auth)).SupportedAsync(session, PaymentPurpose.Checkout);
    if (methods.Count != 2) throw new InvalidOperationException("Clean manual methods missing.");
    Console.WriteLine("TECHNICAL CHECK: catalog create/read/update and supported-method selector are connected (not acceptance).");
}
