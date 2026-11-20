namespace CafeManager.Models.Requests;

// Do not serialize or log credential requests.
public sealed class LoginRequest(string username, string password)
{
    public string Username { get; } = username;
    public string Password { get; } = password;
    public override string ToString() => nameof(LoginRequest);
}
public sealed class BootstrapRequest
{
    public required string Mode { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string Confirmation { get; init; }
    public string? EmployeeCode { get; init; }
    public string? EmployeeName { get; init; }
    public string? Phone { get; init; }
    public byte[]? ExpectedStamp { get; init; }
    public override string ToString() => nameof(BootstrapRequest);
}
