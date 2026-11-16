namespace CafeManager.Models.Requests.HumanResources;

public sealed record EmployeeRequest(string Name, string? Phone, string Contract, decimal MonthlyRate,
    decimal HourlyRate, string Position);
public sealed record EmployeeFilter(string Search = "", int Skip = 0, string? Position = null,
    string? Contract = null, bool? Employed = null, string? Role = null, bool? Active = null);
public sealed record AccountRequest(string Employee, byte[]? ExpectedStamp, string? Username, string Role, bool Active);
