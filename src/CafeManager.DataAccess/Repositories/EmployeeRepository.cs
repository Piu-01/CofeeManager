using System.Data;
using System.Data.SqlTypes;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.HumanResources;
using CafeManager.Models.Requests.HumanResources;
using Microsoft.Data.SqlClient;
namespace CafeManager.DataAccess.Repositories;

public sealed class EmployeeRepository(ConnectionFactory factory)
{
    private readonly AuthRepository executor = new(factory);
    internal static SqlParameter P(string name, SqlDbType type, object? value, int size = 0) => AuthRepository.Parameter(name, type, value, size);
    internal static SqlParameter Money(string name, decimal? value) => new(name, SqlDbType.Decimal) { Precision = 38, Scale = 18, Value = value.HasValue ? new SqlDecimal(value.Value) : DBNull.Value };
    public async Task<HumanResourcesContext> ContextAsync(AccountIdentity a, CancellationToken t) =>
        (await executor.ReadAsync("sp_HRContext", CatalogRepository.Actor(a), r => new HumanResourcesContext(r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt32(3)), t)).Single();
    public Task<List<EmployeeRow>> ListAsync(AccountIdentity a, EmployeeFilter f, CancellationToken t) => executor.ReadAsync("sp_EmployeeList",
        [.. CatalogRepository.Actor(a), P("@Search", SqlDbType.NVarChar, f.Search, -1), P("@Skip", SqlDbType.Int, f.Skip),
        P("@Position", SqlDbType.VarChar, f.Position, 5), P("@Contract", SqlDbType.NVarChar, f.Contract, 20), P("@Employed", SqlDbType.Bit, f.Employed),
        P("@Role", SqlDbType.VarChar, f.Role, 5), P("@Active", SqlDbType.Bit, f.Active)], Map, t);
    public async Task<EmployeeRow?> GetAsync(AccountIdentity a, string code, CancellationToken t) =>
        (await executor.ReadAsync("sp_EmployeeGet", [.. CatalogRepository.Actor(a), P("@Code", SqlDbType.VarChar, code, -1)], Map, t)).SingleOrDefault();
    public Task<List<HumanResourcesLookup>> LookupsAsync(AccountIdentity a, CancellationToken t) => executor.ReadAsync("sp_EmployeeLookups", CatalogRepository.Actor(a), r => new HumanResourcesLookup(r.GetString(0).TrimEnd(), r.GetString(1), r.GetDecimal(2)), t);
    public async Task<string> WriteAsync(AccountIdentity a, string action, EmployeeRow? old, EmployeeRequest? value, bool? employed, Guid id, CancellationToken t) =>
        (await executor.ReadAsync("sp_EmployeeWrite", [.. CatalogRepository.Actor(a), P("@Action", SqlDbType.VarChar, action, 12), P("@RequestId", SqlDbType.UniqueIdentifier, id),
        P("@Code", SqlDbType.VarChar, old?.Code, -1), P("@ExpectedRevision", SqlDbType.Binary, old?.Revision, 8), P("@ExpectedAccountStamp", SqlDbType.Binary, old?.AccountStamp, 8),
        P("@Name", SqlDbType.NVarChar, value?.Name, -1), P("@Phone", SqlDbType.NVarChar, value?.Phone, -1), P("@Contract", SqlDbType.NVarChar, value?.Contract, -1),
        Money("@MonthlyRate", value?.MonthlyRate), Money("@HourlyRate", value?.HourlyRate), P("@Position", SqlDbType.VarChar, value?.Position, -1), P("@Employed", SqlDbType.Bit, employed)], r => r.GetString(0).TrimEnd(), t, true)).Single();
    public async Task<HumanResourcesReceipt?> ReceiptAsync(AccountIdentity a, Guid id, string operation, CancellationToken t) =>
        (await executor.ReadAsync("sp_HumanResourcesReceipt", [.. CatalogRepository.Actor(a), P("@RequestId", SqlDbType.UniqueIdentifier, id), P("@Operation", SqlDbType.VarChar, operation, 40)], r => new HumanResourcesReceipt(r.GetString(0).TrimEnd(), r.GetString(1)), t)).SingleOrDefault();
    private static EmployeeRow Map(SqlDataReader r) => new(r.GetString(0).TrimEnd(), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2),
        r.GetString(3), r.GetDecimal(4), r.GetDecimal(5), r.GetString(6).TrimEnd(), r.GetString(7), r.GetBoolean(8), (byte[])r[9],
        r.IsDBNull(10) ? null : r.GetInt32(10), r.IsDBNull(11) ? null : r.GetString(11), r.IsDBNull(12) ? null : r.GetString(12).TrimEnd(),
        r.IsDBNull(13) ? null : r.GetBoolean(13), r.IsDBNull(14) ? null : (byte[])r[14], r.GetInt32(15));
}
