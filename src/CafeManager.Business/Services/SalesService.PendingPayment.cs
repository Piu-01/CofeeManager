using CafeManager.Business.Security;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests.Sales;

namespace CafeManager.Business.Services;

public sealed partial class SalesService
{
    public Task<SalesWriteResult> RequestPaymentAsync(SessionContext s, SalesRevisionRequest r, CancellationToken t = default)
    {
        Revision(r.Revision);
        if (r.RequestId == Guid.Empty) throw new ArgumentException(L.T("Mã yêu cầu bắt buộc."));
        var code = Code(r.Code, 10);
        return Run(s, ct => repository.RequestPaymentAsync(s.Identity, code, r.Revision, r.RequestId, ct), t);
    }
    public Task<SalesWriteResult> ReopenAsync(SessionContext s, SalesRevisionRequest r, CancellationToken t = default)
    {
        Revision(r.Revision);
        if (r.RequestId == Guid.Empty) throw new ArgumentException(L.T("Mã yêu cầu bắt buộc."));
        // UI cũng ẩn nút; SQL vẫn là nơi quyết định quyền cuối cùng.
        if (s.Identity.Role != "ADMIN") throw new UnauthorizedAccessException(L.T("Bạn không có quyền thực hiện thao tác này."));
        var code = Code(r.Code, 10);
        return Run(s, ct => repository.ReopenAsync(s.Identity, code, r.Revision, r.RequestId, ct), t);
    }
}
