using CafeManager.Business.Security;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using Microsoft.AspNetCore.Identity;

namespace CafeManager.Business.Services;

public sealed class AuthService(AuthRepository repository, PasswordService passwords)
{
    public async Task<SessionContext> LoginAsync(LoginRequest request, CancellationToken token = default)
    {
        var key = PasswordService.Username(request.Username).ToLowerInvariant();
        if ((await repository.ReadStateAsync(token)).State != "Completed")
            throw new InvalidOperationException(L.T("Cần hoàn tất thiết lập quản trị bằng chế độ setup trước khi đăng nhập."));
        var account = await repository.ReadCredentialAsync(key, token: token);
        var verification = account is null ? PasswordVerificationResult.Failed : passwords.Verify(account.Hash, request.Password);
        if (account is null || verification == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException(L.T("Tên đăng nhập hoặc mật khẩu không đúng."));
        if (!account.Identity.Active || !account.Identity.Employed)
            throw new UnauthorizedAccessException(L.T("Tài khoản bị khóa hoặc nhân viên đã nghỉ việc."));
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            _ = await repository.RehashAsync(account, passwords.Hash(request.Password), token);
            var current = await repository.ReadCredentialAsync(key, token: token);
            if (current is null || passwords.Verify(current.Hash, request.Password) == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException(L.T("Thông tin xác thực đã thay đổi; đăng nhập lại."));
            account = current;
        }
        var final = await repository.ReadIdentityAsync(account.Identity.AccountId, token);
        token.ThrowIfCancellationRequested();
        if (final is null || !final.Active || !final.Employed || final.Role != account.Identity.Role
            || !final.SecurityStamp.SequenceEqual(account.Identity.SecurityStamp))
            throw new UnauthorizedAccessException(L.T("Thông tin xác thực đã thay đổi; đăng nhập lại."));
        return new SessionContext(final);
    }
}
