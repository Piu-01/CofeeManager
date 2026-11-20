using Microsoft.Data.SqlClient;

namespace CafeManager.DataAccess;

public sealed class ConnectionFactory
{
    private readonly string connectionString;

    public ConnectionFactory(string connectionString, int commandTimeoutSeconds = 30)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(commandTimeoutSeconds);
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
            throw new ArgumentException("Cần chọn database ứng dụng.", nameof(connectionString));
        this.connectionString = builder.ConnectionString;
        CommandTimeoutSeconds = commandTimeoutSeconds;
    }

    public int CommandTimeoutSeconds { get; }
    internal Func<SqlConnection, CancellationToken, Task>? SessionInitializer { get; init; }
    internal SqlConnection CreateConnection() => new(connectionString);

    internal async Task OpenAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await connection.OpenAsync(cancellationToken);
        if (SessionInitializer is not null) await SessionInitializer(connection, cancellationToken);
    }
}
