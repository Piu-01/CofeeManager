using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests.Sales;

namespace CafeManager.Business.Services;

public sealed class InvoiceService(InvoiceRepository repository, PermissionService permissions)
{
    private async Task<T> Run<T>(SessionContext s, Func<CancellationToken, Task<T>> action, CancellationToken t)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(s.Token, t);
        await permissions.DemandAsync(s, AppPermission.Sales, linked.Token);
        try { var result = await action(linked.Token); s.EnsureCurrent(); return result; }
        catch (DataAccessException ex) { if (ex.Error.Number == 51505) s.Revoke(); throw new UserOperationException(ex.Error); }
    }
    public Task<List<SalesOrderRow>> ListAsync(SessionContext s, InvoiceFilter filter, CancellationToken t = default)
    {
        if (filter.DateBasis is not ("Created" or "Paid") || filter.To < filter.From || filter.Skip < 0) throw new ArgumentException(L.T("Khoảng ngày/trang không hợp lệ."));
        filter = filter with { Search = SalesService.Optional(filter.Search, 100) ?? "", Customer = SalesService.Optional(filter.Customer, 5) };
        return Run(s, ct => repository.ListAsync(s.Identity, filter, ct), t);
    }
    public Task<InvoiceDetailDto?> DetailAsync(SessionContext s, string code, CancellationToken t = default) =>
        Run(s, ct => repository.DetailAsync(s.Identity, SalesService.Code(code, 10), ct), t);
    public Task DemandPrintAsync(SessionContext s, CancellationToken t = default) => permissions.DemandAsync(s, AppPermission.Sales, t);
}
