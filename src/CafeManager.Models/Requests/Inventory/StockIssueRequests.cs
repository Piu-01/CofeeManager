namespace CafeManager.Models.Requests.Inventory;
public sealed record StockIssueLine(string Code,decimal Quantity);
public sealed record StockIssueRequest(Guid RequestId,DateTime Date,string Kind,string Reason,IReadOnlyList<StockIssueLine> Lines);
