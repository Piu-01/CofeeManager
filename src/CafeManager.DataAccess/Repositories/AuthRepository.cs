using System.Data;
using CafeManager.Models.Dtos;
using Microsoft.Data.SqlClient;

namespace CafeManager.DataAccess.Repositories;

// Short-lived auth data only. Never bind this object to UI or put it in a session.
public sealed class AccountCredential(AccountIdentity identity, string hash)
{
    public AccountIdentity Identity { get; } = identity;
    public string Hash { get; } = hash;
    public override string ToString() => nameof(AccountCredential);
}

public sealed class AuthRepository(ConnectionFactory factory)
{
    public async Task<AppSetupStatus> ReadStateAsync(CancellationToken token = default)
    {
        var rows = await ReadAsync("sp_AppReadState", [], r => new AppSetupStatus(r.GetString(0),
            r.GetInt32(3), r.IsDBNull(2) ? null : r.GetInt32(2)), token);
        return rows.Single();
    }

    public async Task<AccountCredential?> ReadCredentialAsync(string key, bool setup = false, CancellationToken token = default)
    {
        var rows = await ReadAsync(setup ? "sp_SetupReadLegacyAccount" : "sp_AuthReadAccount",
            [Parameter("@LoginKey", SqlDbType.VarChar, key, 50)], r => new AccountCredential(Identity(r, 8), r.GetString(7)), token);
        return rows.SingleOrDefault();
    }

    public async Task<AccountIdentity?> ReadIdentityAsync(int id, CancellationToken token = default)
    {
        var rows = await ReadAsync("sp_AuthReadIdentity", [Parameter("@MaTK", SqlDbType.Int, id)], r => Identity(r, 7), token);
        return rows.SingleOrDefault();
    }

    public async Task<byte[]?> RehashAsync(AccountCredential old, string hash, CancellationToken token = default)
    {
        var rows = await ReadAsync("sp_AuthRehash", [Parameter("@MaTK", SqlDbType.Int, old.Identity.AccountId),
            Parameter("@ExpectedHash", SqlDbType.VarChar, old.Hash, 255), Parameter("@ExpectedStamp", SqlDbType.Binary, old.Identity.SecurityStamp, 8),
            Parameter("@NewHash", SqlDbType.VarChar, hash, 255)], r => (byte[])r[0], token, true);
        return rows.SingleOrDefault();
    }

    internal async Task<List<T>> ReadAsync<T>(string procedure, SqlParameter[] parameters,
        Func<SqlDataReader, T> map, CancellationToken token, bool writing = false)
    {
        await using var connection = factory.CreateConnection();
        try
        {
            await factory.OpenAsync(connection, token);
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo." + procedure;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = factory.CommandTimeoutSeconds;
            command.Parameters.AddRange(parameters);
            await using var reader = await command.ExecuteReaderAsync(token);
            var result = new List<T>();
            while (await reader.ReadAsync(token)) result.Add(map(reader));
            return result;
        }
        catch (SqlException e) { throw new DataAccessException(SqlErrorMapper.Map(e, writing)); }
    }

    internal static SqlParameter Parameter(string name, SqlDbType type, object? value, int size = 0) =>
        new(name, type) { Value = value ?? DBNull.Value, Size = size };
    private static AccountIdentity Identity(SqlDataReader r, int stamp) => new(r.GetInt32(0), r.GetString(1).TrimEnd(),
        r.GetString(2), r.GetString(3), r.GetString(4).TrimEnd(), r.GetBoolean(5), r.GetBoolean(6), (byte[])r[stamp]);
}
