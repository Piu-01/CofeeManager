using CafeManager.Models.Dtos;

namespace CafeManager.Business.Security;

public sealed class SessionContext : IDisposable
{
    private readonly CancellationTokenSource cancellation = new();
    public SessionContext(AccountIdentity identity) { Identity = identity; Token = cancellation.Token; }
    public AccountIdentity Identity { get; }
    public Guid Id { get; } = Guid.NewGuid();
    public bool IsRevoked { get; private set; }
    public event Action? Revoked;
    public CancellationToken Token { get; }
    public void EnsureCurrent() { if (IsRevoked) throw new UnauthorizedAccessException("Phiên đã kết thúc; đăng nhập lại."); }
    public void Revoke() { if (!IsRevoked) { IsRevoked = true; cancellation.Cancel(); Revoked?.Invoke(); } }
    public void Dispose() { Revoke(); cancellation.Dispose(); }
}
