using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests.Sales;

namespace CafeManager.DataAccess.Repositories;

public sealed class InvoiceRepository(ConnectionFactory factory)
{
    private readonly SalesRepository executor = new(factory);
    public Task<List<SalesOrderRow>> ListAsync(AccountIdentity a, InvoiceFilter f, CancellationToken t) =>
        executor.Call("sp_InvoiceList", [.. SalesRepository.Actor(a),
            SalesRepository.P("@DateBasis", SqlDbType.VarChar, f.DateBasis, 10),
            SalesRepository.P("@FromDate", SqlDbType.Date, f.From.ToDateTime(TimeOnly.MinValue)), SalesRepository.P("@ToDate", SqlDbType.Date, f.To.ToDateTime(TimeOnly.MinValue)),
            SalesRepository.P("@Status", SqlDbType.NVarChar, f.Status, 30), SalesRepository.P("@Channel", SqlDbType.NVarChar, f.Channel, 20),
            SalesRepository.P("@Customer", SqlDbType.VarChar, f.Customer, 5), SalesRepository.P("@Search", SqlDbType.NVarChar, f.Search, 100),
            SalesRepository.P("@Skip", SqlDbType.Int, f.Skip)], (r, ct) => SalesRepository.Rows(r, SalesRepository.OrderRow, ct), t);
    public Task<InvoiceDetailDto?> DetailAsync(AccountIdentity a, string code, CancellationToken t) =>
        executor.Call<InvoiceDetailDto?>("sp_InvoiceDetail", [.. SalesRepository.Actor(a), SalesRepository.P("@Code", SqlDbType.VarChar, code, -1)],
            ReadDetail, t);
    internal static async Task<InvoiceDetailDto?> ReadDetail(Microsoft.Data.SqlClient.SqlDataReader r, CancellationToken ct)
    {
        var order = await SalesRepository.ReadOrder(r, ct); if (order is null) return null;
        await r.NextResultAsync(ct);
        if (!await r.ReadAsync(ct)) return new(order, null, null, null, 0, null);
        return new(order, SalesRepository.Text(r, 0), SalesRepository.Optional(r, 1), r.IsDBNull(2) ? null : r.GetInt32(2), r.GetInt32(3), r.GetDateTime(4));
    }
}
