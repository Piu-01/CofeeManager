namespace CafeManager.Models.Requests.HumanResources;

public sealed record PayrollRequest(string Employee, int Month, int Year, decimal? Regular = null, decimal OverTime = 0,
    decimal OverTimePay = 0, decimal? Allowance = null, decimal Bonus = 0, decimal Deduction = 0);
public sealed record PayrollFilter(int Month, int Year, string Search = "", string? Status = null, int Skip = 0);
