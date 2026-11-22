using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Loyalty;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Errors;
using CafeManager.Models.Requests.Sales;
using Microsoft.Data.SqlClient;

namespace CafeManager.DataAccess.Repositories;

public sealed partial class SalesRepository(ConnectionFactory factory)
{
    internal static SqlParameter P(string name, SqlDbType type, object? value, int size = 0) => AuthRepository.Parameter(name, type, value, size);
    internal static SqlParameter[] Actor(AccountIdentity a) => CatalogRepository.Actor(a);
    internal async Task<T> Call<T>(string procedure, SqlParameter[] parameters,
        Func<SqlDataReader, CancellationToken, Task<T>> read, CancellationToken token, bool writing = false)
    {
        await using var connection = factory.CreateConnection();
        var sent = false;
        try
        {
            await factory.OpenAsync(connection, token);
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo." + procedure; command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = factory.CommandTimeoutSeconds; command.Parameters.AddRange(parameters);
            sent = true;
            await using var reader = await command.ExecuteReaderAsync(token);
            return await read(reader, token);
        }
        catch (SqlException ex) { throw new DataAccessException(SqlErrorMapper.Map(ex, writing && sent)); }
        catch (OperationCanceledException) when (writing && sent)
        {
            throw new DataAccessException(new(DataErrorKind.Cancelled, 0,
                L.T("Đã dừng chờ ghi. Đối chiếu hóa đơn trước khi tiếp tục."), true));
        }
    }
    internal static string Text(SqlDataReader r, int i) => r.GetString(i).TrimEnd();
    internal static string? Optional(SqlDataReader r, int i) => r.IsDBNull(i) ? null : Text(r, i);
    internal static async Task<List<T>> Rows<T>(SqlDataReader r, Func<SqlDataReader, T> map, CancellationToken t)
    {
        var rows = new List<T>(); while (await r.ReadAsync(t)) rows.Add(map(r)); return rows;
    }
    internal static SalesOrderRow OrderRow(SqlDataReader r) => new(Text(r, 0), r.GetDateTime(1), Text(r, 2),
        Optional(r, 3), Optional(r, 4), r.GetDecimal(5), Text(r, 6), Optional(r, 7), (byte[])r[8], r.GetInt32(9));
    internal static SqlParameter[] Header(OrderHeaderInput h) =>
    [
        P("@Channel", SqlDbType.NVarChar, h.Channel, -1), P("@TableCode", SqlDbType.VarChar, h.TableCode, -1),
        .. EditableHeader(h)
    ];
    internal static SqlParameter[] EditableHeader(OrderHeaderInput h) =>
    [
        P("@Customer", SqlDbType.VarChar, h.Customer, -1), P("@Partner", SqlDbType.VarChar, h.Partner, -1),
        P("@Recipient", SqlDbType.NVarChar, h.Recipient, -1),
        P("@Phone", SqlDbType.VarChar, h.Phone, -1), P("@Address", SqlDbType.NVarChar, h.Address, -1), SqlParameters.Decimal("@Ship", h.Ship)
    ];
    internal Task<SalesWriteResult> Write(AccountIdentity a, string procedure, string code, Guid id,
        SqlParameter[] values, CancellationToken t) => Call(procedure,
        [.. Actor(a), P("@Code", SqlDbType.VarChar, code, -1), P("@RequestId", SqlDbType.UniqueIdentifier, id), .. values],
        async (r, ct) =>
        {
            if (!await r.ReadAsync(ct)) throw new InvalidDataException(L.T("SQL không trả receipt sau ghi."));
            return new SalesWriteResult(Text(r, 0), (byte[])r[1]);
        }, t, true);
    public Task<string> AllocateCodeAsync(CancellationToken t) => Call("sp_CapMa",
        [P("@Loai", SqlDbType.VarChar, "HOADON", 20)], async (r, ct) =>
        { if (!await r.ReadAsync(ct)) throw new InvalidDataException(L.T("Không cấp được mã.")); return Text(r, 1); }, t);
    public Task<SalesWriteResult> CreateAsync(AccountIdentity a, CreateOrderRequest request, CancellationToken t) =>
        Write(a, "sp_TaoHoaDon", request.Code, request.RequestId,
        [.. Header(request.Header), P("@Product", SqlDbType.VarChar, request.Product, -1),
            P("@Quantity", SqlDbType.BigInt, request.Quantity), P("@Note", SqlDbType.NVarChar, request.Note, -1)], t);
    public Task<SalesWriteResult> LineAsync(AccountIdentity a, OrderLineRequest request, CancellationToken t) =>
        Write(a, "sp_SuaDongHoaDon", request.Code, request.RequestId,
        [P("@ExpectedRevision", SqlDbType.Binary, request.Revision, 8), P("@Action", SqlDbType.VarChar, request.Action, 10),
            P("@LineId", SqlDbType.Int, request.LineId), P("@Product", SqlDbType.VarChar, request.Product, -1),
            P("@Quantity", SqlDbType.BigInt, request.Quantity), P("@Note", SqlDbType.NVarChar, request.Note, -1)], t);
    public Task<SalesWriteResult> HeaderAsync(AccountIdentity a, OrderHeaderRequest request, CancellationToken t) =>
        Write(a, "sp_SuaThongTinHoaDon", request.Code, request.RequestId,
        [P("@ExpectedRevision", SqlDbType.Binary, request.Revision, 8), .. EditableHeader(request.Header)], t);
    public Task<SalesOrderDto?> GetAsync(AccountIdentity a, string code, CancellationToken t) => Call("sp_SalesOrderGet",
        [.. Actor(a), P("@Code", SqlDbType.VarChar, code, -1)], ReadOrder, t);
    internal static async Task<SalesOrderDto?> ReadOrder(SqlDataReader r, CancellationToken t)
    {
        if (!await r.ReadAsync(t)) return null;
        var header = new SalesOrderHeader(Text(r, 0), r.GetDateTime(1), Text(r, 2), Optional(r, 3), Optional(r, 4),
            Optional(r, 5), Optional(r, 6), Text(r, 7), Optional(r, 8), Optional(r, 9), Optional(r, 10), Optional(r, 11),
            r.IsDBNull(12) ? null : r.GetDateTime(12), r.GetDecimal(13), r.GetDecimal(14), r.GetDecimal(15), r.GetDecimal(16),
            r.GetDecimal(17), r.GetDecimal(18), Text(r, 19), (byte[])r[20], r.GetBoolean(21), Optional(r, 22),
            r.IsDBNull(23) ? null : r.GetInt32(23), r.GetDateTime(24),
            r.IsDBNull(25) ? null : r.GetInt32(25), r.IsDBNull(26) ? null : (byte[])r[26], r.GetInt32(27));
        await r.NextResultAsync(t);
        var lines = await Rows(r, x => new SalesLine(x.GetInt32(0), Text(x, 1), Text(x, 2), x.GetInt32(3),
            x.GetDecimal(4), Optional(x, 5), x.GetDecimal(6)), t);
        await r.NextResultAsync(t);
        var payments = await Rows(r, x => new SalesPayment(x.GetInt32(0), Text(x, 1), Text(x, 2), x.GetDecimal(3), Optional(x, 4)), t);
        await r.NextResultAsync(t);
        var promotions = await Rows(r, x => new AppliedPromotion(Text(x, 0), Text(x, 1), x.GetInt32(2),
            x.GetDecimal(3), x.GetDecimal(4)), t);
        return new(header, lines, payments, promotions);
    }
    public Task<SalesReceipt?> ReconcileAsync(AccountIdentity a, Guid id, string code, string operation, CancellationToken t) =>
        Call("sp_SalesReconcile", [.. Actor(a), P("@RequestId", SqlDbType.UniqueIdentifier, id),
            P("@Code", SqlDbType.VarChar, code, -1), P("@Operation", SqlDbType.VarChar, operation, 40)], async (r, ct) =>
            await r.ReadAsync(ct) ? new SalesReceipt(Text(r, 0), r.IsDBNull(1) ? null : (byte[])r[1], Text(r, 2), r.GetDateTime(3)) : null, t);
    public Task<List<SalesOrderRow>> OpenAsync(AccountIdentity a, string search, string? channel, int skip, CancellationToken t) =>
        Call("sp_SalesOpenOrders", [.. Actor(a), P("@Search", SqlDbType.NVarChar, search, 100),
            P("@Channel", SqlDbType.NVarChar, channel, 20), P("@Skip", SqlDbType.Int, skip)], (r, ct) => Rows(r, OrderRow, ct), t);
    public Task<List<SalesMenuItem>> MenuAsync(AccountIdentity a, string search, string? category, int skip, CancellationToken t) =>
        Call("sp_SalesMenu", [.. Actor(a), P("@Search", SqlDbType.NVarChar, search, 100),
            P("@Category", SqlDbType.VarChar, category, 5), P("@Skip", SqlDbType.Int, skip)],
            (r, ct) => Rows(r, x => new SalesMenuItem(Text(x, 0), Text(x, 1), x.GetDecimal(2), Text(x, 3), x.GetBoolean(4)), ct), t);
    public Task<List<string>> NoteSuggestionsAsync(AccountIdentity a, string search, CancellationToken t) =>
        Call("sp_SalesNoteSuggestions", [.. Actor(a), P("@Search", SqlDbType.NVarChar, search, 100), P("@Take", SqlDbType.Int, 30)],
            (r, ct) => Rows(r, x => Text(x, 0), ct), t);
    public Task<List<SalesLookup>> CategoriesAsync(AccountIdentity a, CancellationToken t) =>
        Call("sp_SalesCategories", Actor(a), (r, ct) => Rows(r, x => new SalesLookup(Text(x, 0), Text(x, 1)), ct), t);
    public Task<List<SalesLookup>> PartnersAsync(AccountIdentity a, CancellationToken t) =>
        Call("sp_SalesPartners", Actor(a), (r, ct) => Rows(r, x => new SalesLookup(Text(x, 0), Text(x, 1), x.GetDecimal(2)), ct), t);
}
