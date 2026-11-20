using System.Text.Json;
using CafeManager.Models.Errors;

namespace CafeManager.DataAccess;

public sealed class DataOperationLog(string path)
{
    private readonly object gate = new();

    internal void Failure(string operation, string? sourceCode, string correlationId, DataError error)
    {
        // Only allowlisted metadata. Never serialize SQL, parameters, exception or connection.
        var line = JsonSerializer.Serialize(new
        {
            At = DateTimeOffset.UtcNow, Operation = operation,
            Source = sourceCode, CorrelationId = correlationId, error.Number, Kind = error.Kind.ToString()
        });
        try
        {
            lock (gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch (IOException) { /* Logging failure must not change a database outcome. */ }
        catch (UnauthorizedAccessException) { }
    }
}
