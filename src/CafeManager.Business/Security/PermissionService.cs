using CafeManager.DataAccess.Repositories;

namespace CafeManager.Business.Security;

public enum AppPermission { MaterialRead, MaterialManage, MaterialOpen, CatalogAdmin, Sales, Customers, Inventory, DebtRead, DebtPay, Finance, FinancialReports, RecipeRead, MenuRead, SystemInfo }

public sealed class PermissionService(AuthRepository repository)
{
    public static bool Allows(string role, AppPermission permission) => role switch
    {
        "ADMIN" => true,
        "CASH" => permission is AppPermission.MaterialRead or AppPermission.Sales or AppPermission.Customers or AppPermission.MenuRead or AppPermission.SystemInfo,
        "STORE" => permission is AppPermission.MaterialRead or AppPermission.MaterialManage or AppPermission.Inventory or AppPermission.DebtRead or AppPermission.RecipeRead or AppPermission.MenuRead or AppPermission.SystemInfo,
        _ => false
    };
    public async Task DemandAsync(SessionContext session, AppPermission permission, CancellationToken token = default)
    {
        session.EnsureCurrent();
        var identity = await repository.ReadIdentityAsync(session.Identity.AccountId, token);
        session.EnsureCurrent();
        if (identity is null || !identity.Active || !identity.Employed || identity.Role != session.Identity.Role
            || !identity.SecurityStamp.SequenceEqual(session.Identity.SecurityStamp))
        {
            session.Revoke();
            throw new UnauthorizedAccessException("Tài khoản hoặc quyền đã thay đổi; đăng nhập lại.");
        }
        if (!Allows(identity.Role, permission)) throw new UnauthorizedAccessException("Bạn không có quyền thao tác này.");
    }
}
