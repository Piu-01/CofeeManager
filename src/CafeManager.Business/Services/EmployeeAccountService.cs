using CafeManager.Business.Security;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests.HumanResources;
namespace CafeManager.Business.Services;

public sealed class EmployeeAccountService(EmployeeAccountRepository repository, PermissionService permissions, PasswordService passwords)
{
    public Task<string> WriteAsync(SessionContext session, string action, AccountRequest request, string? password, string? confirmation, Guid id, CancellationToken token = default)
    {
        if (action is not ("Create" or "Change" or "Reset") || request.Employee.Length != 5 || request.Role is not ("ADMIN" or "CASH" or "STORE")) throw new ArgumentException(L.T("Tài khoản hoặc thao tác không hợp lệ."));
        if (action == "Create") request = request with { Username = PasswordService.Username(request.Username ?? "") };
        if (action is "Create" or "Reset" && (password is null || password.Length is < 8 or > 128 || password != confirmation)) throw new ArgumentException(L.T("Mật khẩu cần8–128 ký tự, xác nhận khớp; giữ nguyên dấu cách."));
        var hash = action is "Create" or "Reset" ? passwords.Hash(password!) : null;
        return HumanResourcesAccess.Run(permissions, session, AppPermission.EmployeeManage, ct => repository.WriteAsync(session.Identity, action, request, hash, id, ct), token);
    }
}
