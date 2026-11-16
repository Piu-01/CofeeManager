using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Sales;

namespace CafeManager.DataAccess.Repositories;

public sealed partial class SalesRepository
{
    public Task<SalesWriteResult> RequestPaymentAsync(AccountIdentity a, string code, byte[] revision, Guid id, CancellationToken t) =>
        Write(a, "sp_YeuCauThanhToan", code, id, [P("@ExpectedRevision", SqlDbType.Binary, revision, 8)], t);
    public Task<SalesWriteResult> ReopenAsync(AccountIdentity a, string code, byte[] revision, Guid id, CancellationToken t) =>
        Write(a, "sp_MoLaiHoaDon", code, id, [P("@ExpectedRevision", SqlDbType.Binary, revision, 8)], t);
}
