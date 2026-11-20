using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

// Mọi lệnh probe đều giới hạn DB test của lượt; không nhận connection string tùy ý.
if (args.Length != 5 || !Regex.IsMatch(args[0], @"^CoffeeManager_Test_G08_\d{8}_\d{6}_[a-f0-9]{8}_(clean|demo)$")
    || args[3] is not ("technical" or "acceptance") || args[4] is not ("Clean" or "Demo"))
    throw new ArgumentException("Cần DBtest G08, marker, output, technical/acceptance và Clean/Demo.");
var evidence = new Dictionary<string, object?> { ["Kind"] = args[3], ["Mode"] = args[4] };
var builder = new SqlConnectionStringBuilder { DataSource = "localhost", InitialCatalog = args[0], IntegratedSecurity = true,
    Encrypt = true, TrustServerCertificate = true, Pooling = false };
try
{
    await using var owner = new SqlConnection(builder.ConnectionString);
    await owner.OpenAsync();
    await using var marker = owner.CreateCommand();
    marker.CommandText = "SELECT RunId FROM dbo.G08TestMarker";
    if ((string?)await marker.ExecuteScalarAsync() != args[1]) throw new InvalidOperationException("Marker mismatch.");
    await OperationProbe.Run(owner, builder.ConnectionString, args[4], evidence);
    if (args[3] == "acceptance") await E2EProbe.Run(owner, builder.ConnectionString, evidence);
    evidence["Status"] = "Succeeded";
}
catch (Exception e)
{
    // Không serialize exception chứa tham số truy vấn/password hoặc connection string.
    evidence["Status"] = "Failed"; evidence["ErrorType"] = e.GetType().Name;
    evidence["SqlNumber"] = e is SqlException sql ? sql.Number : null;
    Environment.ExitCode = 1;
}
finally { File.WriteAllText(args[2], JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true })); }
