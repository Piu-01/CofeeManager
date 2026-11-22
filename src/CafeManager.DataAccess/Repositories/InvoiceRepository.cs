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
        if (!await r.ReadAsync(ct))
        {
            var empty = new InvoiceDetailDto(order, null, null, null, null, null, 0, null, null, null, null, order.Promotions, []);
            return await ReadGiftsAsync(r, empty, ct);
        }
        var detail = new InvoiceDetailDto(order, SalesRepository.Text(r, 0), SalesRepository.Optional(r, 1), r.IsDBNull(2) ? null : r.GetInt32(2),
            r.IsDBNull(3) ? null : r.GetInt32(3), r.IsDBNull(4) ? null : r.GetInt32(4),
            r.GetInt32(8), r.GetDateTime(9), r.IsDBNull(5) ? null : r.GetDecimal(5),
            r.IsDBNull(6) ? null : r.GetDecimal(6), r.IsDBNull(7) ? null : r.GetInt32(7), order.Promotions, []);
        return await ReadGiftsAsync(r, detail, ct);
    }
    private static async Task<InvoiceDetailDto> ReadGiftsAsync(Microsoft.Data.SqlClient.SqlDataReader r,
        InvoiceDetailDto detail, CancellationToken ct)
    {
        var gifts = new List<CafeManager.Models.Dtos.Loyalty.PreviewGift>();
        if (await r.NextResultAsync(ct))
            while (await r.ReadAsync(ct))
                gifts.Add(new(SalesRepository.Text(r, 0), SalesRepository.Text(r, 1), SalesRepository.Text(r, 2),
                    SalesRepository.Text(r, 3), r.GetInt32(4)));
        return detail with { GiftProposals = gifts };
    }
}
