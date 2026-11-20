using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Inventory;
using Microsoft.Data.SqlClient;

namespace CafeManager.DataAccess.Repositories;

public sealed class MaterialRepository(ConnectionFactory factory)
{
    private readonly AuthRepository executor = new(factory);
    public Task<List<MaterialRow>> ListAsync(AccountIdentity actor, string search, int skip, CancellationToken token) =>
        executor.ReadAsync("sp_MaterialList", [.. CatalogRepository.Actor(actor), AuthRepository.Parameter("@Search", SqlDbType.NVarChar, search, -1), AuthRepository.Parameter("@Skip", SqlDbType.Int, skip)], Map, token);
    public async Task<MaterialRow?> GetAsync(AccountIdentity actor, string code, CancellationToken token) =>
        (await executor.ReadAsync("sp_MaterialGet", [.. CatalogRepository.Actor(actor), AuthRepository.Parameter("@Code", SqlDbType.VarChar, code, -1)], Map, token)).SingleOrDefault();
    public async Task<OperationResult> WriteAsync(AccountIdentity actor, string action, Guid requestId, string code, byte[]? revision,
        string? name, string? unit, decimal? quantity, string? source, CancellationToken token)
    {
        var procedure = action switch { "Create" => "sp_MaterialCreate", "Update" => "sp_MaterialUpdate", "Delete" => "sp_MaterialDelete", "Open" => "sp_MaterialOpen", _ => throw new ArgumentOutOfRangeException(nameof(action)) };
        return (await executor.ReadAsync(procedure, [.. CatalogRepository.Actor(actor), AuthRepository.Parameter("@RequestId", SqlDbType.UniqueIdentifier, requestId),
            AuthRepository.Parameter("@Code", SqlDbType.VarChar, code, -1), AuthRepository.Parameter("@ExpectedRevision", SqlDbType.Binary, revision, 8),
            AuthRepository.Parameter("@Name", SqlDbType.NVarChar, name, -1), AuthRepository.Parameter("@Unit", SqlDbType.NVarChar, unit, -1),
            new SqlParameter("@Quantity", SqlDbType.Decimal) { Precision = 38, Scale = 18, Value = quantity is null ? DBNull.Value : quantity.Value },
            AuthRepository.Parameter("@Source", SqlDbType.NVarChar, source, -1)], r => new OperationResult(r.GetString(0).TrimEnd(), r.IsDBNull(1) ? null : (byte[])r[1]), token, true)).Single();
    }
    public async Task<OperationReceipt?> ReceiptAsync(AccountIdentity actor, Guid requestId, CancellationToken token) =>
        (await executor.ReadAsync("sp_G03Receipt", [.. CatalogRepository.Actor(actor), AuthRepository.Parameter("@RequestId", SqlDbType.UniqueIdentifier, requestId)],
            r => new OperationReceipt(r.GetString(0).TrimEnd(), r.IsDBNull(1) ? null : (byte[])r[1], r.GetString(2), r.GetDateTime(3)), token)).SingleOrDefault();
    private static MaterialRow Map(SqlDataReader r) => new(r.GetString(0).TrimEnd(), r.GetString(1), r.GetString(2), r.GetDecimal(3), r.IsDBNull(4) ? null : r.GetString(4), (byte[])r[5], r.GetDateTime(6));
}
