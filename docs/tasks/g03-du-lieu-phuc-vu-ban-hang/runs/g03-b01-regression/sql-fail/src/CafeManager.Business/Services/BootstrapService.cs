using CafeManager.Business.Security;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos;
using CafeManager.Models.Requests;
using Microsoft.AspNetCore.Identity;

namespace CafeManager.Business.Services;

public sealed class BootstrapService(SetupRepository setup, AuthRepository auth, PasswordService passwords)
{
    public Task<AppSetupStatus> ReadStateAsync(CancellationToken token = default) => setup.ReadStateAsync(token);
    public Task<List<SetupCandidate>> CandidatesAsync(CancellationToken token = default) => setup.CandidatesAsync(token);
    public async Task CompleteAsync(BootstrapRequest request, CancellationToken token = default)
    {
        var username = PasswordService.Username(request.Username);
        if (request.Password.Length is < 8 or > 128 || request.Password != request.Confirmation)
            throw new ArgumentException("Mật khẩu cần 8–128 ký tự và xác nhận khớp; không bỏ dấu cách.");
        if (request.Mode == "NewEmployee" && (string.IsNullOrWhiteSpace(request.EmployeeName) || request.EmployeeName.Trim().Length > 50))
            throw new ArgumentException("Tên nhân viên bắt buộc, tối đa 50 ký tự.");
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(' ');
        if (phone is { Length: > 0 } && (phone.Length > 15 || phone.Any(c => c is < '0' or > '9')))
            throw new ArgumentException("Số điện thoại gồm tối đa 15 chữ số.");
        var normalized = new BootstrapRequest { Mode = request.Mode, Username = username,
            Password = request.Password, Confirmation = request.Confirmation, EmployeeCode = request.EmployeeCode,
            EmployeeName = request.EmployeeName?.Trim(), Phone = phone,
            ExpectedStamp = request.ExpectedStamp };
        await setup.CompleteAsync(normalized, passwords.Hash(request.Password), token);
    }
    public async Task ConfirmLegacyAsync(LoginRequest request, CancellationToken token = default)
    {
        var account = await auth.ReadCredentialAsync(PasswordService.Username(request.Username).ToLowerInvariant(), true, token);
        if (account is null || !account.Identity.Active || !account.Identity.Employed || account.Identity.Role != "ADMIN"
            || passwords.Verify(account.Hash, request.Password) == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Không xác nhận được admin hiện tại; cần quy trình khôi phục riêng nếu bị khóa.");
        await setup.ConfirmLegacyAsync(account, token);
    }
}
