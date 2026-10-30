namespace CafeManager.Models.Requests.Finance;

public sealed record FinanceRequest(Guid RequestId, DateTime Date, string Category, decimal Amount, string? Note, string? Asset);
public sealed record FinanceFilter(DateTime From, DateTime To, string Search = "", string? Type = null, string? Origin = null, int Skip = 0);
