using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Catalogs;

namespace CafeManager.Business.Services;

public sealed class PaymentMethodService(PaymentMethodRepository repository, PermissionService permissions)
{
    public async Task<List<NameLookup>> SupportedAsync(SessionContext session, PaymentPurpose purpose, CancellationToken token = default)
    {
        if (!Enum.IsDefined(purpose)) throw new ArgumentException("Mục đích phương thức không hợp lệ.");
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(session.Token, token);
        await permissions.DemandAsync(session, purpose == PaymentPurpose.Checkout ? AppPermission.Sales : AppPermission.Inventory, linked.Token);
        try { var result = await repository.SupportedAsync(session.Identity, purpose, linked.Token); session.EnsureCurrent(); return result; }
        catch (DataAccessException e) { if (e.Error.Number == 51505) session.Revoke(); throw new UserOperationException(e.Error); }
    }
    public async Task ValidateMethodAsync(SessionContext session, PaymentPurpose purpose, string code, CancellationToken token = default)
    {
        if (!(await SupportedAsync(session, purpose, token)).Any(p => p.Code == code))
            throw new ArgumentException("Phương thức không được hỗ trợ cho thao tác này; PT010 nằm ngoài phạm vi.");
    }
    public async Task<CatalogWriteResult> SetSupportAsync(SessionContext session, string code, bool supported, byte[] revision, CancellationToken token = default)
    {
        if (code == "PT010" && supported) throw new ArgumentException("PT010 không được hỗ trợ.");
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(session.Token, token);
        await permissions.DemandAsync(session, AppPermission.CatalogAdmin, linked.Token);
        try { var result = await repository.SetSupportAsync(session.Identity, code, supported, revision, linked.Token); session.EnsureCurrent(); return result; }
        catch (DataAccessException e) { if (e.Error.Number == 51505) session.Revoke(); throw new UserOperationException(e.Error); }
    }
}
