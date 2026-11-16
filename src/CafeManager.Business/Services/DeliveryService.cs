using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests.Sales;

namespace CafeManager.Business.Services;

public sealed class DeliveryService(DeliveryRepository repository, PermissionService permissions)
{
    public static string? Next(string status) => status switch { "Cho giao" => "Dang giao", "Dang giao" => "Da giao", _ => null };
    private async Task<T> Run<T>(SessionContext s, Func<CancellationToken, Task<T>> action, CancellationToken t)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(s.Token, t);
        await permissions.DemandAsync(s, AppPermission.Sales, linked.Token);
        try { var result = await action(linked.Token); s.EnsureCurrent(); return result; }
        catch (DataAccessException ex) { if (ex.Error.Number == 51505) s.Revoke(); throw new UserOperationException(ex.Error); }
    }
    public Task<List<DeliveryRow>> ListAsync(SessionContext s, string q, string? delivery, string? status, int skip, CancellationToken t = default) =>
        Run(s, ct => repository.ListAsync(s.Identity, SalesService.Optional(q, 100) ?? "", delivery, status, skip, ct), t);
    public Task<InvoiceDetailDto?> GetAsync(SessionContext s, string code, CancellationToken t = default) =>
        Run(s, ct => repository.GetAsync(s.Identity, SalesService.Code(code, 10), ct), t);
    public Task<SalesWriteResult> ChangeAsync(SessionContext s, DeliveryRequest r, CancellationToken t = default)
    {
        SalesService.Revision(r.Revision);
        if (r.TargetStatus != Next(r.ExpectedStatus) || r.RequestId == Guid.Empty) throw new ArgumentException(L.T("Chỉ chuyển một bước kế tiếp."));
        return Run(s, ct => repository.ChangeAsync(s.Identity, r, ct), t);
    }
}
