using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.Models.Errors;
namespace CafeManager.Business.Services;

internal static class HumanResourcesAccess
{
    internal static async Task<T> Run<T>(PermissionService permissions, SessionContext session, AppPermission permission,
        Func<CancellationToken, Task<T>> action, CancellationToken token)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(session.Token, token);
        try
        {
            await permissions.DemandAsync(session, permission, linked.Token);
            var result = await action(linked.Token);
            session.EnsureCurrent(); return result;
        }
        catch (UnauthorizedAccessException e) { throw new UserOperationException(new(DataErrorKind.Permission, session.IsRevoked ? 51505 : 51605, e.Message)); }
        catch (DataAccessException e) { if (e.Error.Number == 51505) session.Revoke(); throw new UserOperationException(e.Error); }
    }
}
