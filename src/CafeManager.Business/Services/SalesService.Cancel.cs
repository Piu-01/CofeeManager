using CafeManager.Business.Security;
using CafeManager.Models.Dtos.Sales;

namespace CafeManager.Business.Services;

public sealed partial class SalesService
{
    public Task<SalesWriteResult> CancelAsync(SessionContext s, SalesOrderHeader h, Guid id, CancellationToken t = default)
    {
        Revision(h.Revision);
        return Run(s, ct => repository.CancelAsync(s.Identity, h.Code, h.Revision, id, ct), t);
    }
}
