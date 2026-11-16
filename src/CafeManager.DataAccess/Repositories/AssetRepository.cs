using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Assets;
using CafeManager.Models.Requests.Assets;
using Microsoft.Data.SqlClient;
namespace CafeManager.DataAccess.Repositories;

public sealed class AssetRepository(ConnectionFactory factory)
{
    private readonly SalesRepository executor = new(factory);
    public Task<AssetContext> ContextAsync(AccountIdentity a, CancellationToken t) => executor.Call("sp_HRContext", CatalogRepository.Actor(a),
        async (r, ct) => { if (!await r.ReadAsync(ct)) throw new InvalidDataException(L.T("Thiếu nhận dạng DB.")); return new AssetContext(r.GetString(0), r.GetString(1), r.GetString(2)); }, t);
    public Task<AssetPage> ListAsync(AccountIdentity a, AssetFilter f, CancellationToken t) => executor.Call("sp_AssetList",
        [.. CatalogRepository.Actor(a), P("@Search", SqlDbType.NVarChar, f.Search, -1), P("@Skip", SqlDbType.Int, f.Skip)],
        async (r, ct) => { await r.ReadAsync(ct); var total = r.GetInt32(0); var at = r.GetDateTime(1); await r.NextResultAsync(ct); return new AssetPage(total, at, await SalesRepository.Rows(r, Map, ct)); }, t);
    public Task<AssetRow?> GetAsync(AccountIdentity a, string code, CancellationToken t) => executor.Call<AssetRow?>("sp_AssetGet",
        [.. CatalogRepository.Actor(a), P("@Code", SqlDbType.VarChar, code, -1)], async (r, ct) => await r.ReadAsync(ct) ? Map(r) : null, t);
    public Task<AssetReceipt> WriteAsync(AccountIdentity a, AssetWriteRequest request, CancellationToken t) => executor.Call("sp_AssetWrite",
        [.. CatalogRepository.Actor(a), P("@RequestId", SqlDbType.UniqueIdentifier, request.RequestId), P("@Action", SqlDbType.VarChar, request.Action, 10),
         P("@Code", SqlDbType.VarChar, request.Code, -1), P("@ExpectedRevision", SqlDbType.Binary, request.ExpectedRevision, 8),
         P("@Name", SqlDbType.NVarChar, request.Value?.Name, -1), P("@PurchasedOn", SqlDbType.Date, request.Value?.PurchasedOn.ToDateTime(TimeOnly.MinValue)),
         EmployeeRepository.Money("@Value", request.Value?.Value), P("@Months", SqlDbType.Int, request.Value?.Months)],
        async (r, ct) => { if (!await r.ReadAsync(ct)) throw new InvalidDataException(L.T("Thiếu kết quả tài sản.")); return Receipt(r); }, t, true);
    public Task<AssetReceipt?> ReceiptAsync(AccountIdentity a, Guid id, CancellationToken t) => executor.Call<AssetReceipt?>("sp_AssetReceipt",
        [.. CatalogRepository.Actor(a), P("@RequestId", SqlDbType.UniqueIdentifier, id)], async (r, ct) => await r.ReadAsync(ct) ? Receipt(r) : null, t);
    public Task<AssetExpensePage> ExpensesAsync(AccountIdentity a, string code, DateOnly from, DateOnly to, int skip, CancellationToken t) => executor.Call("sp_AssetExpenses",
        [.. CatalogRepository.Actor(a), P("@Code", SqlDbType.VarChar, code, -1), P("@From", SqlDbType.Date, from.ToDateTime(TimeOnly.MinValue)), P("@To", SqlDbType.Date, to.ToDateTime(TimeOnly.MinValue)), P("@Skip", SqlDbType.Int, skip)],
        async (r, ct) => { await r.ReadAsync(ct); var total = r.GetInt32(0); var amount = Money(r, 1); var at = r.GetDateTime(2); await r.NextResultAsync(ct);
            return new AssetExpensePage(total, amount, at, await SalesRepository.Rows(r, x => new AssetExpense(x.GetString(0).TrimEnd(), x.GetDateTime(1), x.GetString(2), x.GetDecimal(3), x.IsDBNull(4) ? null : x.GetString(4)), ct)); }, t);
    internal static decimal Money(SqlDataReader r, int ordinal)
    {
        try { return r.GetSqlDecimal(ordinal).Value; }
        catch (OverflowException) { throw new DataAccessException(new(Models.Errors.DataErrorKind.BusinessRule,53505,L.T("Tổng tiền vượt miền số ứng dụng có thể hiển thị."))); }
    }
    private static SqlParameter P(string n, SqlDbType k, object? v, int size = 0) => AuthRepository.Parameter(n,k,v,size);
    private static AssetRow Map(SqlDataReader r) => new(r.GetString(0).TrimEnd(),r.GetString(1),DateOnly.FromDateTime(r.GetDateTime(2)),r.GetDecimal(3),r.GetInt32(4),(byte[])r[5]);
    private static AssetReceipt Receipt(SqlDataReader r) => new(r.GetString(0).TrimEnd(),r.GetString(1),r.IsDBNull(2)?null:(byte[])r[2]);
}
