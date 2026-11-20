namespace CafeManager.Models.Dtos;

public sealed record AccountIdentity(int AccountId, string EmployeeCode, string EmployeeName,
    string Username, string Role, bool Active, bool Employed, byte[] SecurityStamp);
public sealed record AppSetupStatus(string State, int SchemaVersion, int? AdminAccountId);
public sealed record SetupCandidate(string EmployeeCode, string EmployeeName, int? AccountId,
    string? Username, string? Role, bool Eligible, byte[]? SecurityStamp);
