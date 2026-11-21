namespace CafeManager.Models.Requests.Inventory;

public sealed record MaterialRequest(string Code, string Name, string Unit, decimal? OpeningQuantity = null, string? OpeningSource = null);
