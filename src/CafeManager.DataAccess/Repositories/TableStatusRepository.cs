using System.Data;
using CafeManager.Models.Dtos;
using Microsoft.Data.SqlClient;

namespace CafeManager.DataAccess.Repositories;

public sealed class TableStatusRepository(ConnectionFactory factory, DataOperationLog? log = null)
{
    public async Task<IReadOnlyList<TableStatusDto>> GetAsync(string? areaCode, CancellationToken cancellationToken)
    {
        var area = SqlParameters.Text("@MaKV", SqlDbType.Char, 5, areaCode);
        await using var connection = factory.CreateConnection();
        try
        {
            await factory.OpenAsync(connection, cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT MaBan,TenBan,MaKV,TrangThai,MaHD FROM dbo.vw_TrangThaiBan
                WHERE @MaKV IS NULL OR MaKV=@MaKV ORDER BY MaBan;
                """;
            command.CommandTimeout = factory.CommandTimeoutSeconds;
            command.Parameters.Add(area);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var rows = new List<TableStatusDto>();
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(new(reader.GetString(0).TrimEnd(), reader.GetString(1), reader.GetString(2).TrimEnd(),
                    reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4).TrimEnd()));
            return rows;
        }
        catch (SqlException exception)
        {
            var error = SqlErrorMapper.Map(exception, writing: false);
            log?.Failure("LoadTableStatus", null, Guid.NewGuid().ToString("N"), error);
            throw new DataAccessException(error);
        }
    }
}
