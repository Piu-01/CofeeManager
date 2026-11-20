using System.Data;
using Microsoft.Data.SqlClient;

namespace CafeManager.DataAccess;

// Infrastructure sample for procedure tests; no transaction wrapped around SQL-owned transactions.
internal sealed class SqlProcedureExecutor(ConnectionFactory factory, DataOperationLog? log = null)
{
    public async Task ExecuteAsync(string procedure, IEnumerable<SqlParameter> parameters,
        string? sourceCode = null, CancellationToken cancellationToken = default,
        Func<SqlConnection, Task>? inspectSession = null)
    {
        await using var connection = factory.CreateConnection();
        var correlation = Guid.NewGuid().ToString("N");
        try
        {
            await factory.OpenAsync(connection, cancellationToken);
            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = procedure;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = factory.CommandTimeoutSeconds;
                foreach (var parameter in parameters) command.Parameters.Add(parameter);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            finally
            {
                // Test-only observer reads @@TRANCOUNT on this session before disposal.
                if (inspectSession is not null && connection.State == ConnectionState.Open)
                    await inspectSession(connection);
            }
        }
        catch (SqlException exception)
        {
            var error = SqlErrorMapper.Map(exception, writing: true);
            log?.Failure(procedure, sourceCode, correlation, error);
            throw new DataAccessException(error);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var error = new CafeManager.Models.Errors.DataError(
                CafeManager.Models.Errors.DataErrorKind.Cancelled, 0,
                "Đã dừng chờ ghi; đối chiếu mã nguồn trước khi gửi lại.", RequiresReconciliation: true);
            log?.Failure(procedure, sourceCode, correlation, error);
            throw new DataAccessException(error);
        }
    }
}
