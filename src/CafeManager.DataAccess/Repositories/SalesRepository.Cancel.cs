using System.Data;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Sales;

namespace CafeManager.DataAccess.Repositories;

public sealed partial class SalesRepository
{
    public Task<SalesWriteResult> CancelAsync(AccountIdentity a, string code, byte[] revision, Guid id, CancellationToken t) =>
        Write(a, "sp_HuyHoaDon", code, id, [P("@ExpectedRevision", SqlDbType.Binary, revision, 8)], t);
}
