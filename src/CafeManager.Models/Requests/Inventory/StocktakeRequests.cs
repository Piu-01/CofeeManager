namespace CafeManager.Models.Requests.Inventory;
public sealed record StocktakeCount(string Code,decimal? Quantity,string? Reason);
public sealed record StocktakeRequest(Guid RequestId,string Operation,string? Code,byte[]? Revision,List<string> Scope,List<StocktakeCount> Counts);
