using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests.Sales;

namespace CafeManager.DataAccess.Repositories;

public sealed class DeliveryRepository(ConnectionFactory factory)
{
    private readonly SalesRepository executor = new(factory);
    public Task<List<DeliveryRow>> ListAsync(AccountIdentity a, string search, string? delivery, string? status, int skip, CancellationToken t) =>
        executor.Call("sp_DeliveryList", [.. SalesRepository.Actor(a), SalesRepository.P("@Search", SqlDbType.NVarChar, search, 100),
            SalesRepository.P("@Delivery", SqlDbType.NVarChar, delivery, 20), SalesRepository.P("@Status", SqlDbType.NVarChar, status, 30), SalesRepository.P("@Skip", SqlDbType.Int, skip)],
            (r, ct) => SalesRepository.Rows(r, x => new DeliveryRow(SalesRepository.Text(x, 0), x.GetDateTime(1), SalesRepository.Text(x, 2), SalesRepository.Text(x, 3), SalesRepository.Text(x, 4),
                SalesRepository.Optional(x, 5), SalesRepository.Text(x, 6), SalesRepository.Text(x, 7), (byte[])x[8], x.GetInt32(9)), ct), t);
    public Task<InvoiceDetailDto?> GetAsync(AccountIdentity a, string code, CancellationToken t) =>
        executor.Call("sp_DeliveryGet", [.. SalesRepository.Actor(a), SalesRepository.P("@Code", SqlDbType.VarChar, code, -1)], InvoiceRepository.ReadDetail, t);
    public Task<SalesWriteResult> ChangeAsync(AccountIdentity a, DeliveryRequest r, CancellationToken t) =>
        executor.Write(a, "sp_ChuyenTrangThaiGiaoHang", r.Code, r.RequestId,
        [SalesRepository.P("@ExpectedRevision", SqlDbType.Binary, r.Revision, 8), SalesRepository.P("@ExpectedDeliveryStatus", SqlDbType.NVarChar, r.ExpectedStatus, 20),
            SalesRepository.P("@TargetStatus", SqlDbType.NVarChar, r.TargetStatus, 20)], t);
}
